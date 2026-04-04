using RedFox.Graphics2D;
using RedFox.Graphics2D.IO;
using TextureHawk.CLI;
using TextureHawk.Gpu;
using TextureHawk.Processing;
using TextureHawk.Settings;
using Xunit;

namespace TextureHawk.Tests;

public sealed class CliServicesTests : IDisposable
{
    private readonly string _rootDirectory;

    public CliServicesTests()
    {
        _rootDirectory = Path.Combine(Path.GetTempPath(), $"TextureHawk-CliServices-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_rootDirectory);
    }

    [Fact]
    public async Task ExecuteConvertAsync_UsesDefaultOutputPathWhenRequestDoesNotSpecifyOne()
    {
        var inputPath = Path.Combine(_rootDirectory, "input.png");
        File.WriteAllBytes(inputPath, [1, 2, 3]);

        var processor = new RecordingProcessor();
        var registry = new ProcessorRegistry();
        registry.Register(processor);
        using var services = CreateServices(registry);

        var result = await services.ExecuteConvertAsync(new ConvertRequest
        {
            InputPath = inputPath,
            ProcessorId = processor.Id,
            ParameterValues = new Dictionary<string, object>()
        });

        var expectedOutputPath = Path.Combine(_rootDirectory, "input_converted.png");
        Assert.True(result.Success);
        Assert.Equal(expectedOutputPath, result.OutputPath);
        Assert.NotNull(processor.LastContext);
        Assert.Equal(inputPath, processor.LastContext!.InputPath);
        Assert.Equal(expectedOutputPath, processor.LastContext.OutputPath);
    }

    [Fact]
    public void DetectProcessorId_ReturnsFirstMatchingAutoDetectProcessor()
    {
        var registry = new ProcessorRegistry();
        registry.Register(new CsvAutoDetectProcessor());
        using var services = CreateServices(registry);

        var processorId = services.DetectProcessorId(Path.Combine(_rootDirectory, "textures.csv"));

        Assert.Equal(CsvAutoDetectProcessor.ProcessorIdValue, processorId);
    }

    public void Dispose()
    {
        if (Directory.Exists(_rootDirectory))
            Directory.Delete(_rootDirectory, recursive: true);
    }

    private CliServices CreateServices(ProcessorRegistry registry)
    {
        var settingsManager = new SettingsManager(Path.Combine(_rootDirectory, "settings.json"));
        settingsManager.Current.CheckForUpdates = false;
        return new CliServices(new Version(1, 0, 0), settingsManager, registry, new ImageTranslatorManager(), new TestComputeEngine());
    }

    private sealed class RecordingProcessor : IProcessor
    {
        public string Id => "test.recording";
        public string Name => "Recording";
        public string Description => "records context";
        public string Category => "test";
        public string Icon => "R";
        public IReadOnlyList<ProcessorParameter> Parameters => [];
        public ProcessorContext? LastContext { get; private set; }

        public ValueTask<ProcessingResult> ProcessAsync(ProcessorContext context, CancellationToken cancellationToken)
        {
            LastContext = context;
            return ValueTask.FromResult(ProcessingResult.Ok());
        }
    }

    private sealed class CsvAutoDetectProcessor : IAutoDetectProcessor
    {
        public const string ProcessorIdValue = "test.auto-csv";

        public string Id => ProcessorIdValue;
        public string Name => "CSV Auto";
        public string Description => "auto-detect csv inputs";
        public string Category => "test";
        public string Icon => "A";
        public IReadOnlyList<ProcessorParameter> Parameters => [];

        public bool CanConvert(string inputPath)
        {
            return string.Equals(Path.GetExtension(inputPath), ".csv", StringComparison.OrdinalIgnoreCase);
        }

        public ValueTask<ProcessingResult> ProcessAsync(ProcessorContext context, CancellationToken cancellationToken)
        {
            return ValueTask.FromResult(ProcessingResult.Ok());
        }
    }

    private sealed class TestComputeEngine : IComputeEngine
    {
        public bool IsAvailable => true;
        public string Name => "Test";
        public GpuDevice? SelectedDevice => null;

        public IReadOnlyList<GpuDevice> EnumerateDevices() => [];

        public bool SelectDevice(GpuDevice device) => true;

        public bool CaptureStart(string captureName) => false;

        public void CaptureEnd()
        {
        }

        public ValueTask<Image> DispatchAsync(ShaderDefinition shader, IReadOnlyList<Image> inputs, IReadOnlyDictionary<string, object> parameters, int outputWidth, int outputHeight, ImageFormat outputFormat, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public ValueTask<Image> DispatchAsync(byte[] shaderBytecode, IReadOnlyList<Image> inputs, byte[] pushConstants, int outputWidth, int outputHeight, ImageFormat outputFormat, (int X, int Y, int Z) workGroupSize, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public ValueTask<IReadOnlyList<Image>> DispatchMultiAsync(byte[] shaderBytecode, IReadOnlyList<Image> inputs, byte[] pushConstants, IReadOnlyList<(int Width, int Height, ImageFormat Format)> outputs, (int X, int Y, int Z) workGroupSize, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public void Dispose()
        {
        }
    }
}
