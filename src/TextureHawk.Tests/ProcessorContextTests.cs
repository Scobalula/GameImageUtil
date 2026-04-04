using RedFox.Graphics2D;
using RedFox.Graphics2D.IO;
using TextureHawk.Gpu;
using TextureHawk.Processing;
using Xunit;

namespace TextureHawk.Tests;

public sealed class ProcessorContextTests
{
    [Theory]
    [InlineData("true", true)]
    [InlineData("false", false)]
    [InlineData("1", true)]
    [InlineData("0", false)]
    public void GetParameterValue_ConvertsBooleanValuesWithoutExceptions(object rawValue, bool expected)
    {
        var context = CreateContext(new Dictionary<string, object> { ["flag"] = rawValue });

        var actual = context.GetParameterValue("flag", false);

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void GetParameterValue_ConvertsIntegerStringWithoutExceptions()
    {
        var context = CreateContext(new Dictionary<string, object> { ["width"] = "512" });

        var actual = context.GetParameterValue("width", 0);

        Assert.Equal(512, actual);
    }

    [Fact]
    public void GetParameterValue_ConvertsFloatStringWithoutExceptions()
    {
        var context = CreateContext(new Dictionary<string, object> { ["brightness"] = "0.25" });

        var actual = context.GetParameterValue("brightness", 0.0f);

        Assert.Equal(0.25f, actual);
    }

    [Fact]
    public void GetParameterValue_ConvertsEnumStringWithoutExceptions()
    {
        var context = CreateContext(new Dictionary<string, object> { ["targetFormat"] = "R8G8B8A8Unorm" });

        var actual = context.GetParameterValue("targetFormat", ImageFormat.R16G16B16A16Float);

        Assert.Equal(ImageFormat.R8G8B8A8Unorm, actual);
    }

    [Fact]
    public void GetParameterValue_ReturnsDefaultForUnsupportedValue()
    {
        var context = CreateContext(new Dictionary<string, object> { ["width"] = "12.5" });

        var actual = context.GetParameterValue("width", 256);

        Assert.Equal(256, actual);
    }

    [Fact]
    public async Task ExecuteProcessorAsync_UsesInheritedServicesAndOverrides()
    {
        var registry = new ProcessorRegistry();
        var childProcessor = new RecordingProcessor();
        registry.Register(childProcessor);
        var translatorManager = new ImageTranslatorManager();
        var context = new ProcessorContext
        {
            InputPath = "input.png",
            OutputPath = "output.png",
            ParameterValues = new Dictionary<string, object>(),
            GpuColorSpaceMode = GpuColorSpaceMode.TreatInputAsLinear,
            Registry = registry,
            TranslatorManager = translatorManager,
            ComputeEngine = new TestComputeEngine(),
            OverwriteExisting = true
        };

        var result = await context.ExecuteProcessorAsync(
            "child.processor",
            "nested.png",
            "nested-output.png",
            new Dictionary<string, object> { ["flag"] = 1 },
            gpuColorSpaceMode: GpuColorSpaceMode.TreatInputAsSrgb);

        Assert.True(result.Success);
        Assert.NotNull(childProcessor.LastContext);
        Assert.Equal("nested.png", childProcessor.LastContext!.InputPath);
        Assert.Equal("nested-output.png", childProcessor.LastContext.OutputPath);
        Assert.Equal(GpuColorSpaceMode.TreatInputAsSrgb, childProcessor.LastContext.GpuColorSpaceMode);
        Assert.True(childProcessor.LastContext.OverwriteExisting);
        Assert.Same(registry, childProcessor.LastContext.Registry);
        Assert.Same(translatorManager, childProcessor.LastContext.TranslatorManager);
        Assert.Equal(1, childProcessor.LastContext.GetParameterValue("flag", 0));
    }

    [Fact]
    public async Task ExecuteProcessorAsync_GenericOverload_ResolvesProcessorByType()
    {
        var registry = new ProcessorRegistry();
        var childProcessor = new RecordingProcessor();
        registry.Register(childProcessor);
        var context = new ProcessorContext
        {
            InputPath = "input.png",
            OutputPath = "output.png",
            ParameterValues = new Dictionary<string, object>(),
            GpuColorSpaceMode = GpuColorSpaceMode.Auto,
            Registry = registry,
            TranslatorManager = new ImageTranslatorManager(),
            ComputeEngine = new TestComputeEngine(),
            OverwriteExisting = true
        };

        var result = await context.ExecuteProcessorAsync<RecordingProcessor>("nested.png", "nested-output.png");

        Assert.True(result.Success);
        Assert.NotNull(childProcessor.LastContext);
        Assert.Equal("nested.png", childProcessor.LastContext!.InputPath);
    }

    [Fact]
    public async Task ExecuteProcessorAsync_WithoutExplicitOutputPath_UsesDerivedNestedOutputPath()
    {
        var registry = new ProcessorRegistry();
        var childProcessor = new RecordingProcessor();
        registry.Register(childProcessor);
        var context = new ProcessorContext
        {
            InputPath = Path.Combine("root", "data", "textures.csv"),
            OutputPath = Path.Combine("root", "data", "textures_converted.png"),
            ParameterValues = new Dictionary<string, object>(),
            GpuColorSpaceMode = GpuColorSpaceMode.TreatInputAsLinear,
            Registry = registry,
            TranslatorManager = new ImageTranslatorManager(),
            ComputeEngine = new TestComputeEngine(),
            OverwriteExisting = true
        };

        var result = await context.ExecuteProcessorAsync(childProcessor.Id, Path.Combine("root", "_images", "normal.png"), gpuColorSpaceMode: GpuColorSpaceMode.TreatInputAsSrgb);

        Assert.True(result.Success);
        Assert.NotNull(childProcessor.LastContext);
        Assert.Equal(Path.Combine("root", "_images", "normal.png"), childProcessor.LastContext!.InputPath);
        Assert.Equal(Path.Combine("root", "_images", "normal_converted.png"), childProcessor.LastContext.OutputPath);
        Assert.Equal(GpuColorSpaceMode.TreatInputAsSrgb, childProcessor.LastContext.GpuColorSpaceMode);
    }

    [Fact]
    public async Task ExecuteProcessorAsync_RepeatedCalls_UseSharedPipelineDefaultsAndPerCallOverrides()
    {
        var registry = new ProcessorRegistry();
        var childProcessor = new RecordingProcessor();
        registry.Register(childProcessor);
        var context = new ProcessorContext
        {
            InputPath = "input.png",
            OutputPath = "output.png",
            ParameterValues = new Dictionary<string, object>(),
            GpuColorSpaceMode = GpuColorSpaceMode.TreatInputAsLinear,
            Registry = registry,
            TranslatorManager = new ImageTranslatorManager(),
            ComputeEngine = new TestComputeEngine(),
            OverwriteExisting = true
        };

        var firstResult = await context.ExecuteProcessorAsync(
            childProcessor.Id,
            "first.png",
            "first-out.png",
            new Dictionary<string, object> { ["index"] = 1 });
        var secondResult = await context.ExecuteProcessorAsync(
            childProcessor.Id,
            "second.png",
            "second-out.png",
            gpuColorSpaceMode: GpuColorSpaceMode.TreatInputAsSrgb);

        Assert.True(firstResult.Success);
        Assert.True(secondResult.Success);
        Assert.Equal(2, childProcessor.ObservedContexts.Count);
        Assert.Equal("first.png", childProcessor.ObservedContexts[0].InputPath);
        Assert.Equal("first-out.png", childProcessor.ObservedContexts[0].OutputPath);
        Assert.Equal(GpuColorSpaceMode.TreatInputAsLinear, childProcessor.ObservedContexts[0].GpuColorSpaceMode);
        Assert.Equal(1, childProcessor.ObservedContexts[0].GetParameterValue("index", 0));
        Assert.Equal("second.png", childProcessor.ObservedContexts[1].InputPath);
        Assert.Equal("second-out.png", childProcessor.ObservedContexts[1].OutputPath);
        Assert.Equal(GpuColorSpaceMode.TreatInputAsSrgb, childProcessor.ObservedContexts[1].GpuColorSpaceMode);
    }

    private static ProcessorContext CreateContext(IReadOnlyDictionary<string, object> values)
    {
        return new ProcessorContext
        {
            InputPath = "input.png",
            OutputPath = "output.png",
            ParameterValues = values,
            GpuColorSpaceMode = GpuColorSpaceMode.Auto,
            Registry = new ProcessorRegistry(),
            TranslatorManager = new ImageTranslatorManager(),
            ComputeEngine = new TestComputeEngine(),
            OverwriteExisting = false
        };
    }

    private sealed class RecordingProcessor : IProcessor
    {
        public string Id => "child.processor";
        public string Name => "Child";
        public string Description => "child";
        public string Category => "test";
        public string Icon => "C";
        public IReadOnlyList<ProcessorParameter> Parameters => [];
        public List<ProcessorContext> ObservedContexts { get; } = [];
        public ProcessorContext? LastContext { get; private set; }

        public ValueTask<ProcessingResult> ProcessAsync(ProcessorContext context, CancellationToken cancellationToken)
        {
            ObservedContexts.Add(context);
            LastContext = context;
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
