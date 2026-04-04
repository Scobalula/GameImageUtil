using RedFox.Graphics2D;
using TextureHawk.Gpu;

namespace TextureHawk.CLI;

internal sealed class UnavailableComputeEngine(string message) : IComputeEngine
{
    public bool IsAvailable => false;

    public string Name => "Unavailable GPU backend";

    public GpuDevice? SelectedDevice => null;

    public IReadOnlyList<GpuDevice> EnumerateDevices() => [];

    public bool SelectDevice(GpuDevice device) => false;

    public bool CaptureStart(string captureName) => false;

    public void CaptureEnd()
    {
    }

    public ValueTask<Image> DispatchAsync(ShaderDefinition shader, IReadOnlyList<Image> inputs, IReadOnlyDictionary<string, object> parameters, int outputWidth, int outputHeight, ImageFormat outputFormat, CancellationToken cancellationToken)
    {
        throw new InvalidOperationException(message);
    }

    public ValueTask<Image> DispatchAsync(byte[] shaderBytecode, IReadOnlyList<Image> inputs, byte[] pushConstants, int outputWidth, int outputHeight, ImageFormat outputFormat, (int X, int Y, int Z) workGroupSize, CancellationToken cancellationToken)
    {
        throw new InvalidOperationException(message);
    }

    public ValueTask<IReadOnlyList<Image>> DispatchMultiAsync(byte[] shaderBytecode, IReadOnlyList<Image> inputs, byte[] pushConstants, IReadOnlyList<(int Width, int Height, ImageFormat Format)> outputs, (int X, int Y, int Z) workGroupSize, CancellationToken cancellationToken)
    {
        throw new InvalidOperationException(message);
    }

    public void Dispose()
    {
    }
}