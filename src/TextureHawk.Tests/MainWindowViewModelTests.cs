using System.Reflection;
using RedFox.Graphics2D;
using RedFox.Graphics2D.IO;
using RedFox.Graphics2D.Png;
using TextureHawk.Gpu;
using TextureHawk.Processing;
using TextureHawk.Settings;
using TextureHawk.UI;
using TextureHawk.UI.ViewModels;
using Xunit;

namespace TextureHawk.Tests;

[Collection("AppState")]
public sealed class MainWindowViewModelTests : IDisposable
{
    private static readonly FieldInfo s_settingsPathField = typeof(SettingsManager).GetField("_settingsPath", BindingFlags.Instance | BindingFlags.NonPublic)
        ?? throw new InvalidOperationException("SettingsManager._settingsPath field was not found.");

    private readonly string _rootDirectory;
    private readonly string _originalSettingsPath;
    private readonly SavedSettingsSnapshot _savedSettings;
    private readonly List<string> _registeredProcessorIds = [];

    public MainWindowViewModelTests()
    {
        _rootDirectory = Path.Combine(Path.GetTempPath(), $"TextureHawk-MainVM-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_rootDirectory);

        _originalSettingsPath = App.SettingsManager.SettingsPath;
        s_settingsPathField.SetValue(App.SettingsManager, Path.Combine(_rootDirectory, "settings.json"));

        App.TranslatorManager.Register(new PngImageTranslator());
        App.TranslatorManager.Register(new DdsImageTranslator());

        _savedSettings = CaptureSettings(App.SettingsManager.Current);
        ResetSettings(App.SettingsManager.Current);
    }

    [Fact]
    public void EditingSelectedItemOutputSuffix_PersistsProcessorDefaultsForFutureItems()
    {
        var processor = RegisterProcessor();
        var firstVm = CreateViewModel(processor);
        firstVm.AddFiles([CreateInputFile("first.png")]);

        firstVm.OutputSuffix = "_persisted";

        Assert.Equal("_persisted", App.SettingsManager.Current.ProcessorDefaults[processor.Id].OutputSuffix);

        var secondVm = CreateViewModel(processor);
        secondVm.AddFiles([CreateInputFile("second.png")]);

        var item = Assert.Single(secondVm.TextureItems);
        Assert.Equal("_persisted", item.OutputSuffixOverride);
        Assert.EndsWith("second_persisted.png", item.OutputPath, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void EditingSelectedItemParameter_PersistsProcessorDefaultsForFutureItems()
    {
        var processor = RegisterProcessor();
        var firstVm = CreateViewModel(processor);
        firstVm.AddFiles([CreateInputFile("parameter-first.png")]);

        var parameter = Assert.Single(firstVm.CurrentParameters);
        parameter.StringValue = "persisted-mode";

        Assert.Equal("persisted-mode", App.SettingsManager.Current.ProcessorDefaults[processor.Id].ParameterValues["mode"]);

        var secondVm = CreateViewModel(processor);
        secondVm.AddFiles([CreateInputFile("parameter-second.png")]);

        var item = Assert.Single(secondVm.TextureItems);
        Assert.NotNull(item.ParameterOverrides);
        Assert.Equal("persisted-mode", item.ParameterOverrides!["mode"]);
    }

    [Fact]
    public void SwitchingToDdsOutput_ShowsAndPreservesDdsImageFormatParameter()
    {
        var processor = RegisterProcessor();
        var viewModel = CreateViewModel(processor);
        viewModel.AddFiles([CreateInputFile("dds-parameter.png")]);

        viewModel.OutputFormat = ".dds";

        var ddsParameter = Assert.Single(viewModel.CurrentParameters.Where(p => p.Id == OutputWriteParameterCatalog.DdsImageFormatParameterId));
        Assert.True(ddsParameter.IsImageFormat);
        Assert.Contains(nameof(ImageFormat.BC1Unorm), ddsParameter.Options);
        Assert.DoesNotContain(nameof(ImageFormat.R32G32B32A32Float), ddsParameter.Options);

        ddsParameter.StringValue = nameof(ImageFormat.BC3Unorm);

        viewModel.OutputFormat = ".png";
        Assert.DoesNotContain(viewModel.CurrentParameters, p => p.Id == OutputWriteParameterCatalog.DdsImageFormatParameterId);

        viewModel.OutputFormat = ".dds";
        ddsParameter = Assert.Single(viewModel.CurrentParameters.Where(p => p.Id == OutputWriteParameterCatalog.DdsImageFormatParameterId));
        Assert.Equal(nameof(ImageFormat.BC3Unorm), ddsParameter.StringValue);
    }

    public void Dispose()
    {
        foreach (var processorId in _registeredProcessorIds)
            App.ProcessorRegistry.Unregister(processorId);

        RestoreSettings(App.SettingsManager.Current, _savedSettings);
        s_settingsPathField.SetValue(App.SettingsManager, _originalSettingsPath);

        if (Directory.Exists(_rootDirectory))
            Directory.Delete(_rootDirectory, recursive: true);
    }

    private TestProcessor RegisterProcessor()
    {
        var processor = new TestProcessor($"test.processor.{Guid.NewGuid():N}");
        App.ProcessorRegistry.Register(processor);
        _registeredProcessorIds.Add(processor.Id);
        return processor;
    }

    private MainWindowViewModel CreateViewModel(TestProcessor processor)
    {
        App.SettingsManager.Current.LastProcessorId = processor.Id;
        var viewModel = new MainWindowViewModel
        {
            SelectedProcessor = processor
        };

        return viewModel;
    }

    private string CreateInputFile(string fileName)
    {
        var path = Path.Combine(_rootDirectory, fileName);
        File.WriteAllBytes(path, [1, 2, 3]);
        return path;
    }

    private static SavedSettingsSnapshot CaptureSettings(AppSettings settings)
    {
        return new SavedSettingsSnapshot(
            settings.OutputDirectory,
            settings.UseProcessorDefaultOutputDirectory,
            settings.OverwriteExisting,
            settings.DefaultOutputFormat,
            settings.UseProcessorDefaultOutputFormat,
            settings.LastProcessorId,
            settings.OutputSuffix,
            settings.UseProcessorDefaultOutputSuffix,
            settings.DefaultGpuColorSpaceMode,
            settings.UseProcessorDefaultGpuColorSpaceMode,
            CloneProcessorDefaults(settings.ProcessorDefaults));
    }

    private static void ResetSettings(AppSettings settings)
    {
        settings.OutputDirectory = string.Empty;
        settings.UseProcessorDefaultOutputDirectory = true;
        settings.OverwriteExisting = true;
        settings.DefaultOutputFormat = ".png";
        settings.UseProcessorDefaultOutputFormat = true;
        settings.LastProcessorId = null;
        settings.OutputSuffix = string.Empty;
        settings.UseProcessorDefaultOutputSuffix = true;
        settings.DefaultGpuColorSpaceMode = GpuColorSpaceModeNames.Auto;
        settings.UseProcessorDefaultGpuColorSpaceMode = true;
        settings.ProcessorDefaults.Clear();
    }

    private static void RestoreSettings(AppSettings settings, SavedSettingsSnapshot snapshot)
    {
        settings.OutputDirectory = snapshot.OutputDirectory;
        settings.UseProcessorDefaultOutputDirectory = snapshot.UseProcessorDefaultOutputDirectory;
        settings.OverwriteExisting = snapshot.OverwriteExisting;
        settings.DefaultOutputFormat = snapshot.DefaultOutputFormat;
        settings.UseProcessorDefaultOutputFormat = snapshot.UseProcessorDefaultOutputFormat;
        settings.LastProcessorId = snapshot.LastProcessorId;
        settings.OutputSuffix = snapshot.OutputSuffix;
        settings.UseProcessorDefaultOutputSuffix = snapshot.UseProcessorDefaultOutputSuffix;
        settings.DefaultGpuColorSpaceMode = snapshot.DefaultGpuColorSpaceMode;
        settings.UseProcessorDefaultGpuColorSpaceMode = snapshot.UseProcessorDefaultGpuColorSpaceMode;
        settings.ProcessorDefaults.Clear();
        foreach (var pair in snapshot.ProcessorDefaults)
            settings.ProcessorDefaults[pair.Key] = pair.Value;
    }

    private static Dictionary<string, ProcessorDefaults> CloneProcessorDefaults(Dictionary<string, ProcessorDefaults> defaults)
    {
        return defaults.ToDictionary(
            pair => pair.Key,
            pair => new ProcessorDefaults
            {
                OutputDirectory = pair.Value.OutputDirectory,
                OutputFormat = pair.Value.OutputFormat,
                OutputSuffix = pair.Value.OutputSuffix,
                GpuColorSpaceMode = pair.Value.GpuColorSpaceMode,
                ParameterValues = new Dictionary<string, string>(pair.Value.ParameterValues, StringComparer.Ordinal)
            },
            StringComparer.Ordinal);
    }

    private sealed class TestProcessor : IProcessor
    {
        public TestProcessor(string id)
        {
            Id = id;
        }

        public string Id { get; }
        public string Name => "Test Processor";
        public string Description => "Test processor";
        public string Category => "Tests";
        public string Icon => "T";
        public IReadOnlyList<ProcessorParameter> Parameters { get; } =
        [
            new ProcessorParameter
            {
                Id = "mode",
                Name = "Mode",
                Description = "Mode parameter",
                Type = ProcessorParameterType.String,
                DefaultValue = "default-mode"
            }
        ];

        public ValueTask<ProcessingResult> ProcessAsync(ProcessorContext context, CancellationToken cancellationToken)
        {
            return ValueTask.FromResult(ProcessingResult.Ok());
        }
    }

    private sealed record SavedSettingsSnapshot(
        string OutputDirectory,
        bool UseProcessorDefaultOutputDirectory,
        bool OverwriteExisting,
        string DefaultOutputFormat,
        bool UseProcessorDefaultOutputFormat,
        string? LastProcessorId,
        string OutputSuffix,
        bool UseProcessorDefaultOutputSuffix,
        string DefaultGpuColorSpaceMode,
        bool UseProcessorDefaultGpuColorSpaceMode,
        Dictionary<string, ProcessorDefaults> ProcessorDefaults);
}
