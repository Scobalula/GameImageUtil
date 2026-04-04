using RedFox.Graphics2D;
using RedFox.Graphics2D.IO;
using RedFox.Graphics2D.Png;
using TextureHawk.BuiltInProcessors;
using TextureHawk.BuiltInProcessors.ColorTextures;
using TextureHawk.BuiltInProcessors.PackedGameTextures.CallOfDuty;
using TextureHawk.Gpu;
using TextureHawk.Processing;
using Xunit;

namespace TextureHawk.Tests;

public sealed class CoDCsvProcessorTests : IDisposable
{
    private readonly string _rootDirectory;

    public CoDCsvProcessorTests()
    {
        _rootDirectory = Path.Combine(Path.GetTempPath(), $"TextureHawk-CoDCsv-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_rootDirectory);
    }

    [Fact]
    public async Task ProcessAsync_ResolvesImagesDirectoriesAndRunsNestedProcessors()
    {
        var csvDirectory = Directory.CreateDirectory(Path.Combine(_rootDirectory, "data")).FullName;
        var localImagesDirectory = Directory.CreateDirectory(Path.Combine(csvDirectory, "_images")).FullName;
        var parentImagesDirectory = Directory.CreateDirectory(Path.Combine(_rootDirectory, "_images")).FullName;
        var colorPath = Path.Combine(localImagesDirectory, "local_color.png");
        var nogPath = Path.Combine(parentImagesDirectory, "parent_nog.png");
        File.WriteAllBytes(colorPath, [1, 2, 3]);
        File.WriteAllBytes(nogPath, [4, 5, 6]);

        var csvPath = Path.Combine(csvDirectory, "textures.csv");
        File.WriteAllText(csvPath, "colormap,nogmap\r\nlocal_color,parent_nog\r\n");

        var colorProcessor = new RecordingProcessor("builtin.cod-cs");
        var nogProcessor = new RecordingProcessor("builtin.cod-nog");
        var registry = new ProcessorRegistry();
        registry.Register(colorProcessor);
        registry.Register(nogProcessor);

        var translatorManager = new ImageTranslatorManager();
        translatorManager.Register(new PngImageTranslator());

        var processor = new CoDCsvProcessor();
        var context = new ProcessorContext
        {
            InputPath = csvPath,
            OutputPath = Path.Combine(csvDirectory, "textures_converted.png"),
            ParameterValues = new Dictionary<string, object>(),
            GpuColorSpaceMode = GpuColorSpaceMode.Auto,
            Registry = registry,
            TranslatorManager = translatorManager,
            ComputeEngine = new TestComputeEngine(),
            OverwriteExisting = true
        };

        var result = await processor.ProcessAsync(context);

        Assert.True(result.Success);
        Assert.Equal(colorPath, colorProcessor.ObservedContexts.Single().InputPath);
        Assert.Equal(nogPath, nogProcessor.ObservedContexts.Single().InputPath);
        Assert.Equal(Path.Combine(localImagesDirectory, "local_color_converted.png"), colorProcessor.ObservedContexts.Single().OutputPath);
        Assert.Equal(Path.Combine(parentImagesDirectory, "parent_nog_converted.png"), nogProcessor.ObservedContexts.Single().OutputPath);
    }

    [Fact]
    public async Task ProcessAsync_ContinuesAcrossMultipleRows()
    {
        var csvDirectory = Directory.CreateDirectory(Path.Combine(_rootDirectory, "batch-data")).FullName;
        var localImagesDirectory = Directory.CreateDirectory(Path.Combine(csvDirectory, "_images")).FullName;
        var parentImagesDirectory = Directory.CreateDirectory(Path.Combine(_rootDirectory, "_images")).FullName;
        var colorPathA = Path.Combine(localImagesDirectory, "local_color_a.png");
        var colorPathB = Path.Combine(localImagesDirectory, "local_color_b.png");
        var nogPathA = Path.Combine(parentImagesDirectory, "parent_nog_a.png");
        var nogPathB = Path.Combine(parentImagesDirectory, "parent_nog_b.png");
        File.WriteAllBytes(colorPathA, [1, 2, 3]);
        File.WriteAllBytes(colorPathB, [4, 5, 6]);
        File.WriteAllBytes(nogPathA, [7, 8, 9]);
        File.WriteAllBytes(nogPathB, [10, 11, 12]);

        var csvPath = Path.Combine(csvDirectory, "textures.csv");
        File.WriteAllText(csvPath, "colormap,nogmap\r\nlocal_color_a,parent_nog_a\r\nlocal_color_b,parent_nog_b\r\n");

        var colorProcessor = new RecordingProcessor(nameof(MetalnessToSpecularProcessor));
        var nogProcessor = new RecordingProcessor(IWNogProcessor.ProcessorId);
        var registry = new ProcessorRegistry();
        registry.Register(colorProcessor);
        registry.Register(nogProcessor);

        var translatorManager = new ImageTranslatorManager();
        translatorManager.Register(new PngImageTranslator());

        var processor = new CoDCsvProcessor();
        var context = new ProcessorContext
        {
            InputPath = csvPath,
            OutputPath = Path.Combine(csvDirectory, "textures_converted.png"),
            ParameterValues = new Dictionary<string, object>(),
            GpuColorSpaceMode = GpuColorSpaceMode.Auto,
            Registry = registry,
            TranslatorManager = translatorManager,
            ComputeEngine = new TestComputeEngine(),
            OverwriteExisting = true
        };

        var result = await processor.ProcessAsync(context);

        Assert.True(result.Success);
        Assert.Equal(2, colorProcessor.ObservedContexts.Count);
        Assert.Equal(2, nogProcessor.ObservedContexts.Count);
        Assert.Equal(colorPathA, colorProcessor.ObservedContexts[0].InputPath);
        Assert.Equal(colorPathB, colorProcessor.ObservedContexts[1].InputPath);
        Assert.Equal(nogPathA, nogProcessor.ObservedContexts[0].InputPath);
        Assert.Equal(nogPathB, nogProcessor.ObservedContexts[1].InputPath);
    }

    [Fact]
    public void CanConvert_ReturnsTrueForCompatibleCsvHeader()
    {
        var csvPath = Path.Combine(_rootDirectory, "textures.csv");
        File.WriteAllText(csvPath, "colormap,nogmap\r\n");

        var processor = new CoDCsvProcessor();

        var result = processor.CanConvert(csvPath);

        Assert.True(result);
    }

    public void Dispose()
    {
        if (Directory.Exists(_rootDirectory))
            Directory.Delete(_rootDirectory, recursive: true);
    }

    private sealed class RecordingProcessor : IProcessor
    {
        private readonly string _id;

        public RecordingProcessor(string id)
        {
            _id = id;
        }

        public string Id => _id;
        public string Name => _id;
        public string Description => _id;
        public string Category => "test";
        public string Icon => "T";
        public IReadOnlyList<ProcessorParameter> Parameters => [];
        public List<ProcessorContext> ObservedContexts { get; } = [];

        public ValueTask<ProcessingResult> ProcessAsync(ProcessorContext context, CancellationToken cancellationToken)
        {
            ObservedContexts.Add(context);
            return ValueTask.FromResult(ProcessingResult.Ok());
        }
    }

    private sealed class TestComputeEngine : IComputeEngine
    {
        public bool IsAvailable => true;
        public string Name => "Test";
        public GpuDevice? SelectedDevice => null;

        public bool SelectDevice(GpuDevice device) => true;
        public IReadOnlyList<GpuDevice> EnumerateDevices() => [];
        public bool CaptureStart(string captureName) => false;
        public void CaptureEnd() { }
        public ValueTask<Image> DispatchAsync(ShaderDefinition shader, IReadOnlyList<Image> inputs, IReadOnlyDictionary<string, object> parameters, int outputWidth, int outputHeight, ImageFormat outputFormat, CancellationToken cancellationToken) => throw new NotSupportedException();
        public ValueTask<Image> DispatchAsync(byte[] shaderBytecode, IReadOnlyList<Image> inputs, byte[] pushConstants, int outputWidth, int outputHeight, ImageFormat outputFormat, (int X, int Y, int Z) workGroupSize, CancellationToken cancellationToken) => throw new NotSupportedException();
        public ValueTask<IReadOnlyList<Image>> DispatchMultiAsync(byte[] shaderBytecode, IReadOnlyList<Image> inputs, byte[] pushConstants, IReadOnlyList<(int Width, int Height, ImageFormat Format)> outputs, (int X, int Y, int Z) workGroupSize, CancellationToken cancellationToken) => throw new NotSupportedException();
        public void Dispose() { }
    }
}
