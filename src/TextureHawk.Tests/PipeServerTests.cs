using System.Globalization;
using System.Text.Json;
using RedFox.Graphics2D;
using RedFox.Graphics2D.IO;
using TextureHawk.CLI;
using TextureHawk.Gpu;
using TextureHawk.Processing;
using TextureHawk.Settings;
using Xunit;

namespace TextureHawk.Tests;

public sealed class PipeServerTests : IDisposable
{
    private readonly string _rootDirectory;

    public PipeServerTests()
    {
        _rootDirectory = Path.Combine(Path.GetTempPath(), $"TextureHawk-PipeServer-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_rootDirectory);
    }

    [Fact]
    public async Task RunAsync_ProcessesConvertAndExitRequests()
    {
        var inputPath = Path.Combine(_rootDirectory, "input.png");
        File.WriteAllBytes(inputPath, [1, 2, 3]);

        var registry = new ProcessorRegistry();
        registry.Register(new RecordingProcessor());
        using var services = CreateServices(registry);
        using var reader = new StringReader($"{{\"requestId\":\"1\",\"command\":\"convert\",\"inputPath\":\"{EscapeJson(inputPath)}\",\"processorId\":\"test.recording\"}}\n{{\"requestId\":\"2\",\"command\":\"exit\"}}\n");
        using var writer = new StringWriter(CultureInfo.InvariantCulture);

        var exitCode = await new PipeServer(services, reader, writer).RunAsync();

        Assert.Equal(0, exitCode);

        using var outputReader = new StringReader(writer.ToString());
        using var convertResponse = JsonDocument.Parse(outputReader.ReadLine()!);
        using var exitResponse = JsonDocument.Parse(outputReader.ReadLine()!);

        Assert.True(convertResponse.RootElement.GetProperty("success").GetBoolean());
        Assert.Equal("convert", convertResponse.RootElement.GetProperty("command").GetString());
        Assert.Equal("1", convertResponse.RootElement.GetProperty("requestId").GetString());

        var convertData = convertResponse.RootElement.GetProperty("data");
        Assert.Equal(inputPath, convertData.GetProperty("inputPath").GetString());
        Assert.Equal(Path.Combine(_rootDirectory, "input_converted.png"), convertData.GetProperty("outputPath").GetString());
        Assert.Equal("test.recording", convertData.GetProperty("processorId").GetString());

        Assert.True(exitResponse.RootElement.GetProperty("success").GetBoolean());
        Assert.Equal("exit", exitResponse.RootElement.GetProperty("command").GetString());
        Assert.Equal("2", exitResponse.RootElement.GetProperty("requestId").GetString());
    }

    [Fact]
    public async Task RunAsync_InvalidJsonReturnsErrorBeforeContinuing()
    {
        using var services = CreateServices(new ProcessorRegistry());
        using var reader = new StringReader("not-json\n{\"requestId\":\"2\",\"command\":\"exit\"}\n");
        using var writer = new StringWriter(CultureInfo.InvariantCulture);

        var exitCode = await new PipeServer(services, reader, writer).RunAsync();

        Assert.Equal(0, exitCode);

        using var outputReader = new StringReader(writer.ToString());
        using var invalidResponse = JsonDocument.Parse(outputReader.ReadLine()!);
        using var exitResponse = JsonDocument.Parse(outputReader.ReadLine()!);

        Assert.False(invalidResponse.RootElement.GetProperty("success").GetBoolean());
        Assert.Equal("invalid", invalidResponse.RootElement.GetProperty("command").GetString());
        Assert.Contains("Invalid JSON request", invalidResponse.RootElement.GetProperty("error").GetString(), StringComparison.Ordinal);

        Assert.True(exitResponse.RootElement.GetProperty("success").GetBoolean());
        Assert.Equal("exit", exitResponse.RootElement.GetProperty("command").GetString());
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

    private static string EscapeJson(string value)
    {
        return value.Replace("\\", "\\\\", StringComparison.Ordinal).Replace("\"", "\\\"", StringComparison.Ordinal);
    }

    private sealed class RecordingProcessor : IProcessor
    {
        public string Id => "test.recording";
        public string Name => "Recording";
        public string Description => "records context";
        public string Category => "test";
        public string Icon => "R";
        public IReadOnlyList<ProcessorParameter> Parameters => [];

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
