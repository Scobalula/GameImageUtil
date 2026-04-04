using RedFox.Graphics2D.IO;
using Spectre.Console;
using TextureHawk.BuiltInProcessors;
using TextureHawk.Gpu;
using TextureHawk.Gpu.Vulkan;
using TextureHawk.Plugins;
using TextureHawk.Processing;
using TextureHawk.Settings;
using TextureHawk.Updates;

namespace TextureHawk.CLI;

internal sealed class CliServices : IDisposable
{
    public const string DefaultProcessorId = "builtin.format-convert";

    private readonly IComputeEngine _computeEngine;
    private readonly ProcessingPipeline _pipeline;

    internal CliServices(Version version, SettingsManager settingsManager, ProcessorRegistry processorRegistry, ImageTranslatorManager translatorManager, IComputeEngine computeEngine)
    {
        Version = version;
        VersionText = $"{version.Major}.{version.Minor}.{version.Build}";
        SettingsManager = settingsManager;
        ProcessorRegistry = processorRegistry;
        TranslatorManager = translatorManager;
        _computeEngine = computeEngine;
        _pipeline = new ProcessingPipeline(
            ProcessorRegistry,
            TranslatorManager,
            _computeEngine,
            () => SettingsManager.Current.OverwriteExisting,
            GetDefaultGpuColorSpaceMode);
    }

    public Version Version { get; }

    public string VersionText { get; }

    public SettingsManager SettingsManager { get; }

    public ProcessorRegistry ProcessorRegistry { get; }

    public ImageTranslatorManager TranslatorManager { get; }

    public static CliServices Create(Version version)
    {
        var settingsManager = new SettingsManager();
        settingsManager.Load();

        var processorRegistry = new ProcessorRegistry();
        var translatorManager = new ImageTranslatorManager();
        BuiltInRegistrar.RegisterAll(processorRegistry, translatorManager);

        var services = new CliServices(version, settingsManager, processorRegistry, translatorManager, CreateComputeEngine(settingsManager.Current));
        services.LoadExtensions();
        return services;
    }

    public IReadOnlyCollection<IProcessor> GetAllProcessors() => ProcessorRegistry.GetAll();

    public IProcessor? GetProcessor(string processorId) => ProcessorRegistry.GetProcessor(processorId);

    public string DetectProcessorId(string inputPath)
    {
        foreach (var processor in ProcessorRegistry.GetAll().OfType<IAutoDetectProcessor>())
        {
            if (processor.CanConvert(inputPath))
                return processor.Id;
        }

        return DefaultProcessorId;
    }

    public string GetDefaultOutputPath(string inputPath) => ConvertPathHelper.BuildDefaultOutputPath(inputPath);

    public GpuColorSpaceMode GetDefaultGpuColorSpaceMode()
    {
        return GpuColorSpaceModeNames.ParseOrDefault(SettingsManager.Current.DefaultGpuColorSpaceMode, GpuColorSpaceMode.Auto);
    }

    public bool TryParseGpuColorSpaceMode(string? rawValue, out GpuColorSpaceMode? mode, out string? error)
    {
        if (string.IsNullOrWhiteSpace(rawValue))
        {
            mode = null;
            error = null;
            return true;
        }

        if (GpuColorSpaceModeNames.TryParse(rawValue, out var parsedMode))
        {
            mode = parsedMode;
            error = null;
            return true;
        }

        mode = null;
        error = $"Unsupported GPU color-space mode '{rawValue}'. Use auto, srgb, or linear.";
        return false;
    }

    public ImageInfoResult ReadImageInfo(string filePath)
    {
        var image = TranslatorManager.Read(filePath);
        return new ImageInfoResult
        {
            FilePath = Path.GetFullPath(filePath),
            Width = image.Width,
            Height = image.Height,
            Depth = image.Depth,
            ArraySize = image.ArraySize,
            MipLevels = image.MipLevels,
            Format = image.Format.ToString(),
            IsCubemap = image.IsCubemap,
            SliceCount = image.SliceCount
        };
    }

    public async Task<ConvertExecutionResult> ExecuteConvertAsync(ConvertRequest request, CancellationToken cancellationToken)
    {
        var inputPath = Path.GetFullPath(request.InputPath);
        var outputPath = string.IsNullOrWhiteSpace(request.OutputPath)
            ? GetDefaultOutputPath(inputPath)
            : Path.GetFullPath(request.OutputPath);

        if (!File.Exists(inputPath))
            return CreateFailure(inputPath, outputPath, request.ProcessorId, $"Input file not found: {inputPath}");

        var processor = GetProcessor(request.ProcessorId);
        if (processor is null)
            return CreateFailure(inputPath, outputPath, request.ProcessorId, $"Processor '{request.ProcessorId}' not found.");

        var additionalInputPaths = request.AdditionalInputPaths
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .Select(Path.GetFullPath)
            .ToList();

        var job = new ProcessingJob
        {
            InputPath = inputPath,
            OutputPath = outputPath,
            ProcessorId = processor.Id,
            ParameterValues = new Dictionary<string, object>(request.ParameterValues, StringComparer.OrdinalIgnoreCase),
            AdditionalInputPaths = additionalInputPaths,
            GpuColorSpaceMode = request.GpuColorSpaceMode
        };

        var result = await _pipeline.ProcessAsync(job, progress: null, cancellationToken);
        var writtenPaths = result.Success ? BuildWrittenPaths(result, outputPath) : [];
        return new ConvertExecutionResult
        {
            InputPath = inputPath,
            OutputPath = result.OutputPathOverride ?? outputPath,
            ProcessorId = processor.Id,
            WrittenPaths = writtenPaths,
            Success = result.Success,
            ErrorMessage = result.ErrorMessage
        };
    }

    public async Task TryWriteUpdateNotificationAsync(IAnsiConsole console)
    {
        if (!SettingsManager.Current.CheckForUpdates)
            return;

        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
            var provider = new GitHubUpdateProvider("TextureHawk", "TextureHawk");
            var update = await provider.CheckForUpdateAsync(Version, SettingsManager.Current.IncludePreReleases, cts.Token);
            if (update is null)
                return;

            console.MarkupLine($"[yellow]Update available:[/] {Markup.Escape(update.Version)} [grey]->[/] {Markup.Escape(update.Url)}");
            console.WriteLine();
        }
        catch
        {
            // Ignore update failures in the CLI.
        }
    }

    public void Dispose()
    {
        _computeEngine.Dispose();
    }

    private static ConvertExecutionResult CreateFailure(string inputPath, string outputPath, string processorId, string error)
    {
        return new ConvertExecutionResult
        {
            InputPath = inputPath,
            OutputPath = outputPath,
            ProcessorId = processorId,
            Success = false,
            ErrorMessage = error
        };
    }

    private static IReadOnlyList<string> BuildWrittenPaths(ProcessingResult result, string outputPath)
    {
        var baseOutputPath = result.OutputPathOverride ?? outputPath;
        if (result.Images is { Count: > 1 } images)
        {
            var paths = new List<string>(images.Count);
            for (var i = 0; i < images.Count; i++)
            {
                var suffix = result.OutputSuffixes is not null && i < result.OutputSuffixes.Count
                    ? result.OutputSuffixes[i]
                    : $"_{i}";
                paths.Add(InsertSuffix(baseOutputPath, suffix));
            }

            return paths;
        }

        return result.Image is null ? [] : [baseOutputPath];
    }

    private void LoadExtensions()
    {
        var pluginLoader = new PluginLoader(ProcessorRegistry);
        pluginLoader.LoadAllFromDirectory(SettingsManager.GetPluginsDirectory());
        foreach (var directory in SettingsManager.Current.PluginDirectories)
            pluginLoader.LoadAllFromDirectory(directory);

        foreach (var processor in DefinedProcessor.LoadFromDirectory(SettingsManager.GetDefinitionsDirectory()))
            ProcessorRegistry.Register(processor);

        foreach (var directory in SettingsManager.Current.ProcessorDefinitionDirectories)
        {
            foreach (var processor in DefinedProcessor.LoadFromDirectory(directory))
                ProcessorRegistry.Register(processor);
        }
    }

    private static IComputeEngine CreateComputeEngine(AppSettings settings)
    {
        try
        {
            var computeEngine = new VulkanComputeEngine();
            if (!computeEngine.IsAvailable)
            {
                computeEngine.Dispose();
                return new UnavailableComputeEngine("No Vulkan-compatible GPU detected. GPU-backed processors are unavailable in this session.");
            }

            var devices = computeEngine.EnumerateDevices();
            if (devices.Count == 0)
            {
                computeEngine.Dispose();
                return new UnavailableComputeEngine("No Vulkan-compatible GPU detected. GPU-backed processors are unavailable in this session.");
            }

            var preferredDevice = devices.FirstOrDefault(device =>
                (settings.PreferredGpuDeviceId is null || device.DeviceId == settings.PreferredGpuDeviceId)
                && (settings.PreferredGpuVendorId is null || device.VendorId == settings.PreferredGpuVendorId)
                && (string.IsNullOrWhiteSpace(settings.PreferredGpuDeviceName)
                    || string.Equals(device.Name, settings.PreferredGpuDeviceName, StringComparison.OrdinalIgnoreCase)));

            if (preferredDevice != default)
                computeEngine.SelectDevice(preferredDevice);
            else if (computeEngine.SelectedDevice is null)
                computeEngine.SelectDevice(devices[0]);

            return computeEngine;
        }
        catch (Exception ex)
        {
            return new UnavailableComputeEngine($"No GPU backend available: {ex.Message}");
        }
    }

    private static string InsertSuffix(string path, string suffix)
    {
        var directory = Path.GetDirectoryName(path) ?? ".";
        var name = Path.GetFileNameWithoutExtension(path);
        var extension = Path.GetExtension(path);
        return Path.Combine(directory, $"{name}{suffix}{extension}");
    }
}
