using RedFox.Graphics2D;
using RedFox.Graphics2D.IO;
using RedFox.Graphics2D.Png;
using TextureHawk.BuiltInProcessors.ColorTextures;
using TextureHawk.Gpu;
using TextureHawk.Processing;
using Xunit;

namespace TextureHawk.Tests;

public sealed class SpecularToMetalnessProcessorTests : IDisposable
{
    private readonly string _rootDirectory;

    public SpecularToMetalnessProcessorTests()
    {
        _rootDirectory = Path.Combine(Path.GetTempPath(), $"TextureHawk-SpecularToMetalness-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_rootDirectory);
    }

    [Fact]
    public async Task ProcessAsync_PackMetalnessInAlpha_ReturnsSinglePackedOutput()
    {
        var translatorManager = CreateTranslatorManager();
        var (albedoPath, specularPath) = CreateInputImages(translatorManager);
        var fusedImage = CreateImage();
        var packedImage = CreateImage();
        var computeEngine = new RecordingComputeEngine([fusedImage, packedImage]);
        var processor = new SpecularToMetalnessProcessor();
        var context = CreateContext(translatorManager, computeEngine, albedoPath, specularPath, packMetalnessInAlpha: true);

        var result = await processor.ProcessAsync(context);

        Assert.True(result.Success);
        Assert.Null(result.Images);
        Assert.Null(result.OutputSuffixes);
        Assert.Same(packedImage, result.Image);
    }

    [Fact]
    public async Task ProcessAsync_DefaultMode_ReturnsFusedAndMetalnessOutputs()
    {
        var translatorManager = CreateTranslatorManager();
        var (albedoPath, specularPath) = CreateInputImages(translatorManager);
        var fusedImage = CreateImage();
        var metalnessImage = CreateImage();
        var computeEngine = new RecordingComputeEngine([fusedImage, metalnessImage]);
        var processor = new SpecularToMetalnessProcessor();
        var context = CreateContext(translatorManager, computeEngine, albedoPath, specularPath, packMetalnessInAlpha: false);

        var result = await processor.ProcessAsync(context);

        Assert.True(result.Success);
        Assert.NotNull(result.Images);
        Assert.Equal(2, result.Images.Count);
        Assert.Same(fusedImage, result.Images[0]);
        Assert.Same(metalnessImage, result.Images[1]);
        Assert.Equal(["_f", "_m"], result.OutputSuffixes);
    }

    public void Dispose()
    {
        if (Directory.Exists(_rootDirectory))
            Directory.Delete(_rootDirectory, recursive: true);
    }

    private ProcessorContext CreateContext(ImageTranslatorManager translatorManager, IComputeEngine computeEngine, string albedoPath, string specularPath, bool packMetalnessInAlpha)
    {
        return new ProcessorContext
        {
            InputPath = albedoPath,
            OutputPath = Path.Combine(_rootDirectory, "output.png"),
            ParameterValues = new Dictionary<string, object>
            {
                ["specPath"] = specularPath,
                ["packMetalnessInAlpha"] = packMetalnessInAlpha
            },
            GpuColorSpaceMode = GpuColorSpaceMode.Auto,
            Registry = new ProcessorRegistry(),
            TranslatorManager = translatorManager,
            ComputeEngine = computeEngine,
            OverwriteExisting = true
        };
    }

    private (string AlbedoPath, string SpecularPath) CreateInputImages(ImageTranslatorManager translatorManager)
    {
        var albedoPath = Path.Combine(_rootDirectory, "albedo.png");
        var specularPath = Path.Combine(_rootDirectory, "specular.png");
        translatorManager.Write(albedoPath, CreateImage());
        translatorManager.Write(specularPath, CreateImage());
        return (albedoPath, specularPath);
    }

    private static ImageTranslatorManager CreateTranslatorManager()
    {
        var translatorManager = new ImageTranslatorManager();
        translatorManager.Register(new PngImageTranslator());
        return translatorManager;
    }

    private static Image CreateImage()
    {
        return new Image(2, 2, 1, 1, 1, ImageFormat.R8G8B8A8Unorm, false);
    }

    private sealed class RecordingComputeEngine(IReadOnlyList<Image> images) : IComputeEngine
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
            return ValueTask.FromResult(images);
        }

        public void Dispose()
        {
        }
    }
}
