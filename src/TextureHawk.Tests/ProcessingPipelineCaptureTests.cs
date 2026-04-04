using RedFox.Graphics2D;
using RedFox.Graphics2D.IO;
using TextureHawk.Gpu;
using TextureHawk.Processing;
using Xunit;

namespace TextureHawk.Tests;

public sealed class ProcessingPipelineCaptureTests
{
    [Fact]
    public async Task ProcessAsync_StartsAndEndsCapture_WhenBackendStartsCapture()
    {
        var processor = new TestProcessor(_ => ValueTask.FromResult(new ProcessingResult { Success = true }));
        var pipeline = CreatePipeline(processor, new TestComputeEngine { CaptureStartResult = true });

        var result = await pipeline.ProcessAsync(CreateJob(processor.Id));

        Assert.True(result.Success);
        Assert.Equal(1, ((TestComputeEngine)GetComputeEngine(pipeline)).CaptureStartCalls);
        Assert.Equal(1, ((TestComputeEngine)GetComputeEngine(pipeline)).CaptureEndCalls);
        Assert.Equal(processor.Name, ((TestComputeEngine)GetComputeEngine(pipeline)).LastCaptureName);
    }

    [Fact]
    public async Task ProcessAsync_DoesNotEndCapture_WhenBackendDeclinesCapture()
    {
        var processor = new TestProcessor(_ => ValueTask.FromResult(new ProcessingResult { Success = true }));
        var computeEngine = new TestComputeEngine { CaptureStartResult = false };
        var pipeline = CreatePipeline(processor, computeEngine);

        var result = await pipeline.ProcessAsync(CreateJob(processor.Id));

        Assert.True(result.Success);
        Assert.Equal(1, computeEngine.CaptureStartCalls);
        Assert.Equal(0, computeEngine.CaptureEndCalls);
    }

    [Fact]
    public async Task ProcessAsync_EndsCapture_WhenProcessorThrows()
    {
        var processor = new TestProcessor(_ => throw new InvalidOperationException("boom"));
        var computeEngine = new TestComputeEngine { CaptureStartResult = true };
        var pipeline = CreatePipeline(processor, computeEngine);

        var result = await pipeline.ProcessAsync(CreateJob(processor.Id));

        Assert.False(result.Success);
        Assert.Equal("boom", result.ErrorMessage);
        Assert.Equal(1, computeEngine.CaptureEndCalls);
    }

    [Fact]
    public async Task ProcessAsync_Continues_WhenCaptureStartThrows()
    {
        var processor = new TestProcessor(_ => ValueTask.FromResult(new ProcessingResult { Success = true }));
        var computeEngine = new TestComputeEngine { ThrowOnCaptureStart = true };
        var pipeline = CreatePipeline(processor, computeEngine);

        var result = await pipeline.ProcessAsync(CreateJob(processor.Id));

        Assert.True(result.Success);
        Assert.Equal(1, computeEngine.CaptureStartCalls);
        Assert.Equal(0, computeEngine.CaptureEndCalls);
    }

    [Fact]
    public async Task ProcessAsync_UsesPipelineDefaultGpuColorSpaceMode()
    {
        var observedMode = GpuColorSpaceMode.Auto;
        var processor = new TestProcessor(context =>
        {
            observedMode = context.GpuColorSpaceMode;
            return ValueTask.FromResult(new ProcessingResult { Success = true });
        });
        var pipeline = CreatePipeline(processor, new TestComputeEngine(), GpuColorSpaceMode.TreatInputAsSrgb);

        var result = await pipeline.ProcessAsync(CreateJob(processor.Id));

        Assert.True(result.Success);
        Assert.Equal(GpuColorSpaceMode.TreatInputAsSrgb, observedMode);
    }

    [Fact]
    public async Task ProcessAsync_UsesJobGpuColorSpaceOverride_WhenProvided()
    {
        var observedMode = GpuColorSpaceMode.Auto;
        var processor = new TestProcessor(context =>
        {
            observedMode = context.GpuColorSpaceMode;
            return ValueTask.FromResult(new ProcessingResult { Success = true });
        });
        var pipeline = CreatePipeline(processor, new TestComputeEngine(), GpuColorSpaceMode.TreatInputAsSrgb);
        var job = CreateJob(processor.Id);
        job = new ProcessingJob
        {
            InputPath = job.InputPath,
            OutputPath = job.OutputPath,
            ProcessorId = job.ProcessorId,
            ParameterValues = job.ParameterValues,
            AdditionalInputPaths = job.AdditionalInputPaths,
            GpuColorSpaceMode = GpuColorSpaceMode.TreatInputAsLinear
        };

        var result = await pipeline.ProcessAsync(job);

        Assert.True(result.Success);
        Assert.Equal(GpuColorSpaceMode.TreatInputAsLinear, observedMode);
    }

    private static ProcessingPipeline CreatePipeline(IProcessor processor, TestComputeEngine computeEngine)
    {
        return CreatePipeline(processor, computeEngine, GpuColorSpaceMode.Auto);
    }

    private static ProcessingPipeline CreatePipeline(IProcessor processor, TestComputeEngine computeEngine, GpuColorSpaceMode gpuColorSpaceMode)
    {
        var registry = new ProcessorRegistry();
        registry.Register(processor);
        return new ProcessingPipeline(registry, new ImageTranslatorManager(), computeEngine, () => false, () => gpuColorSpaceMode);
    }

    private static ProcessingJob CreateJob(string processorId)
    {
        return new ProcessingJob
        {
            InputPath = "input.png",
            OutputPath = "output.png",
            ProcessorId = processorId
        };
    }

    private static IComputeEngine GetComputeEngine(ProcessingPipeline pipeline)
    {
        var field = typeof(ProcessingPipeline).GetField("_computeEngine", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        return (IComputeEngine)field!.GetValue(pipeline)!;
    }

    private sealed class TestProcessor : IProcessor
    {
        private readonly Func<ProcessorContext, ValueTask<ProcessingResult>> _run;

        public TestProcessor(Func<ProcessorContext, ValueTask<ProcessingResult>> run)
        {
            _run = run;
        }

        public string Id => "test.processor";
        public string Name => "Test Processor";
        public string Description => "test";
        public string Category => "test";
        public string Icon => "T";
        public IReadOnlyList<ProcessorParameter> Parameters => [];

        public ValueTask<ProcessingResult> ProcessAsync(ProcessorContext context, CancellationToken cancellationToken) => _run(context);
    }

    private sealed class TestComputeEngine : IComputeEngine
    {
        public bool CaptureStartResult { get; set; }
        public bool ThrowOnCaptureStart { get; set; }
        public int CaptureStartCalls { get; private set; }
        public int CaptureEndCalls { get; private set; }
        public string? LastCaptureName { get; private set; }

        public bool IsAvailable => true;
        public string Name => "Test";
        public GpuDevice? SelectedDevice => null;

        public bool SelectDevice(GpuDevice device) => true;
        public IReadOnlyList<GpuDevice> EnumerateDevices() => [];

        public bool CaptureStart(string captureName)
        {
            CaptureStartCalls++;
            LastCaptureName = captureName;
            if (ThrowOnCaptureStart)
                throw new InvalidOperationException("capture start failed");

            return CaptureStartResult;
        }

        public void CaptureEnd()
        {
            CaptureEndCalls++;
        }

        public ValueTask<Image> DispatchAsync(ShaderDefinition shader, IReadOnlyList<Image> inputs, IReadOnlyDictionary<string, object> parameters, int outputWidth, int outputHeight, ImageFormat outputFormat, CancellationToken cancellationToken) => throw new NotSupportedException();
        public ValueTask<Image> DispatchAsync(byte[] shaderBytecode, IReadOnlyList<Image> inputs, byte[] pushConstants, int outputWidth, int outputHeight, ImageFormat outputFormat, (int X, int Y, int Z) workGroupSize, CancellationToken cancellationToken) => throw new NotSupportedException();
        public ValueTask<IReadOnlyList<Image>> DispatchMultiAsync(byte[] shaderBytecode, IReadOnlyList<Image> inputs, byte[] pushConstants, IReadOnlyList<(int Width, int Height, ImageFormat Format)> outputs, (int X, int Y, int Z) workGroupSize, CancellationToken cancellationToken) => throw new NotSupportedException();
        public void Dispose() { }
    }
}
