using RedFox.Graphics2D;

namespace TextureHawk.Gpu;

/// <summary>
/// Abstraction for GPU compute operations on images.
/// </summary>
/// <remarks>
/// Implementations target GPU compute APIs such as Vulkan.
/// The compute engine handles uploading image data to the GPU, dispatching
/// compute shaders, and reading back results. Supports runtime device
/// enumeration and selection for multi-GPU systems.
/// </remarks>
public interface IComputeEngine : IDisposable
{
    /// <summary>
    /// Whether this engine is available on the current system.
    /// </summary>
    bool IsAvailable { get; }

    /// <summary>
    /// Display name of this compute engine (e.g., "Vulkan").
    /// </summary>
    string Name { get; }

    /// <summary>
    /// The currently selected GPU device, or null if none selected.
    /// </summary>
    GpuDevice? SelectedDevice { get; }

    /// <summary>
    /// Enumerates all GPU devices available for compute.
    /// </summary>
    IReadOnlyList<GpuDevice> EnumerateDevices();

    /// <summary>
    /// Selects a GPU device for compute operations.
    /// </summary>
    /// <returns>True if the device was successfully selected.</returns>
    bool SelectDevice(GpuDevice device);

    /// <summary>
    /// Starts a backend-owned frame capture for the current processor run.
    /// </summary>
    /// <param name="captureName">Human-readable name for the capture, when supported.</param>
    /// <returns>
    /// <see langword="true"/> when a capture was started and should later be ended with <see cref="CaptureEnd"/>;
    /// otherwise <see langword="false"/>.
    /// </returns>
    bool CaptureStart(string captureName);

    /// <summary>
    /// Ends the active backend-owned frame capture, if one is running.
    /// </summary>
    void CaptureEnd();

    /// <summary>
    /// Dispatches a compute shader with the given inputs and parameters.
    /// </summary>
    /// <param name="shader">The shader definition to execute.</param>
    /// <param name="inputs">Input images to bind.</param>
    /// <param name="parameters">Parameter values for the shader.</param>
    /// <param name="outputWidth">Width of the output image.</param>
    /// <param name="outputHeight">Height of the output image.</param>
    /// <param name="outputFormat">Format of the output image.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The processed output image.</returns>
    ValueTask<Image> DispatchAsync(ShaderDefinition shader, IReadOnlyList<Image> inputs, IReadOnlyDictionary<string, object> parameters, int outputWidth, int outputHeight, ImageFormat outputFormat, CancellationToken cancellationToken);

    /// <summary>
    /// Dispatches a compute shader from raw shader bytecode with explicit push constants.
    /// </summary>
    /// <param name="shaderBytecode">Compiled shader bytecode.</param>
    /// <param name="inputs">Input images to bind as storage buffers.</param>
    /// <param name="pushConstants">Raw push constant bytes (processor-defined layout).</param>
    /// <param name="outputWidth">Width of the output image.</param>
    /// <param name="outputHeight">Height of the output image.</param>
    /// <param name="outputFormat">Pixel format of the output image.</param>
    /// <param name="workGroupSize">Compute work group size. Uses (16, 16, 1) when default.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The processed output image.</returns>
    ValueTask<Image> DispatchAsync(byte[] shaderBytecode, IReadOnlyList<Image> inputs, byte[] pushConstants, int outputWidth, int outputHeight, ImageFormat outputFormat, (int X, int Y, int Z) workGroupSize, CancellationToken cancellationToken);

    /// <summary>
    /// Dispatches a compute shader that produces multiple output images.
    /// </summary>
    /// <remarks>
    /// Each output is bound as a separate storage buffer at consecutive descriptor bindings
    /// after the input sampler bindings. The work group is dispatched according to the
    /// maximum output dimensions by default.
    /// </remarks>
    /// <param name="shaderBytecode">Compiled shader bytecode.</param>
    /// <param name="inputs">Input images to bind as combined image samplers.</param>
    /// <param name="pushConstants">Raw push constant bytes (processor-defined layout).</param>
    /// <param name="outputs">Width, height, and format for each output image.</param>
    /// <param name="workGroupSize">Compute work group size. Uses (16, 16, 1) when default.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The processed output images.</returns>
    ValueTask<IReadOnlyList<Image>> DispatchMultiAsync(byte[] shaderBytecode, IReadOnlyList<Image> inputs, byte[] pushConstants, IReadOnlyList<(int Width, int Height, ImageFormat Format)> outputs, (int X, int Y, int Z) workGroupSize, CancellationToken cancellationToken);
}
