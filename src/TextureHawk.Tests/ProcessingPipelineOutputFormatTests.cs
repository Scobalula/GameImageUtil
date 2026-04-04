using RedFox.Graphics2D;
using RedFox.Graphics2D.IO;
using TextureHawk.Gpu;
using TextureHawk.Processing;
using Xunit;

namespace TextureHawk.Tests;

public sealed class ProcessingPipelineOutputFormatTests
{
    [Fact]
    public async Task ProcessAsync_ConvertsDdsOutputToRequestedImageFormatBeforeWriting()
    {
        var translator = new RecordingTranslator(".dds");
        var translatorManager = new ImageTranslatorManager();
        translatorManager.Register(translator);

        var registry = new ProcessorRegistry();
        var processor = new TestImageProcessor();
        registry.Register(processor);

        var pipeline = new ProcessingPipeline(registry, translatorManager, new TestComputeEngine());
        var tempDirectory = Path.Combine(Path.GetTempPath(), $"TextureHawk-Pipeline-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDirectory);

        try
        {
            var result = await pipeline.ProcessAsync(
                new ProcessingJob
                {
                    InputPath = Path.Combine(tempDirectory, "input.png"),
                    OutputPath = Path.Combine(tempDirectory, "output.dds"),
                    ProcessorId = processor.Id,
                    ParameterValues = new Dictionary<string, object>
                    {
                        [OutputWriteParameterCatalog.DdsImageFormatParameterId] = ImageFormat.BC3Unorm
                    }
                },
                progress: null,
                cancellationToken: CancellationToken.None);

            Assert.True(result.Success);
            Assert.NotNull(translator.LastWrittenImage);
            Assert.Equal(ImageFormat.BC3Unorm, translator.LastWrittenImage!.Format);
        }
        finally
        {
            if (Directory.Exists(tempDirectory))
                Directory.Delete(tempDirectory, recursive: true);
        }
    }

    private sealed class TestImageProcessor : IProcessor
    {
        public string Id => "tests.pipeline.image";
        public string Name => "Test Image Processor";
        public string Description => "Returns a simple image";
        public string Category => "Tests";
        public string Icon => "T";
        public IReadOnlyList<ProcessorParameter> Parameters => [];

        public ValueTask<ProcessingResult> ProcessAsync(ProcessorContext context, CancellationToken cancellationToken)
        {
            var image = new Image(4, 4, 1, 1, 1, ImageFormat.R8G8B8A8Unorm);
            return ValueTask.FromResult(ProcessingResult.Ok(image));
        }
    }

    private sealed class RecordingTranslator : ImageTranslator
    {
        private readonly string _extension;

        public RecordingTranslator(string extension)
        {
            _extension = extension;
        }

        public Image? LastWrittenImage { get; private set; }

        public override string Name => "Recording";
        public override bool CanRead => false;
        public override bool CanWrite => true;
        public override IReadOnlyList<string> Extensions => [_extension];

        public override Image Read(Stream stream) => throw new NotSupportedException();

        public override void Write(Stream stream, Image image)
        {
            LastWrittenImage = new Image(
                image.Width,
                image.Height,
                image.Depth,
                image.ArraySize,
                image.MipLevels,
                image.Format,
                image.IsCubemap,
                image.PixelData.ToArray());
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
        public void CaptureEnd() { }
        public ValueTask<Image> DispatchAsync(ShaderDefinition shader, IReadOnlyList<Image> inputs, IReadOnlyDictionary<string, object> parameters, int outputWidth, int outputHeight, ImageFormat outputFormat, CancellationToken cancellationToken) => throw new NotSupportedException();
        public ValueTask<Image> DispatchAsync(byte[] shaderBytecode, IReadOnlyList<Image> inputs, byte[] pushConstants, int outputWidth, int outputHeight, ImageFormat outputFormat, (int X, int Y, int Z) workGroupSize, CancellationToken cancellationToken) => throw new NotSupportedException();
        public ValueTask<IReadOnlyList<Image>> DispatchMultiAsync(byte[] shaderBytecode, IReadOnlyList<Image> inputs, byte[] pushConstants, IReadOnlyList<(int Width, int Height, ImageFormat Format)> outputs, (int X, int Y, int Z) workGroupSize, CancellationToken cancellationToken) => throw new NotSupportedException();
        public void Dispose() { }
    }
}
