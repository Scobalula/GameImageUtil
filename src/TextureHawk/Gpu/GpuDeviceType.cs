namespace TextureHawk.Gpu;

/// <summary>
/// Classification of GPU device types.
/// </summary>
public enum GpuDeviceType
{
    /// <summary>
    /// An unrecognized or uncategorized device type.
    /// </summary>
    Other,

    /// <summary>
    /// An integrated GPU sharing system memory (e.g., Intel UHD, AMD APU).
    /// </summary>
    IntegratedGpu,

    /// <summary>
    /// A discrete GPU with dedicated memory.
    /// </summary>
    DiscreteGpu,

    /// <summary>
    /// A virtual GPU provided by a hypervisor or cloud environment.
    /// </summary>
    VirtualGpu,

    /// <summary>
    /// A CPU-based software rasterizer (e.g., SwiftShader, lavapipe).
    /// </summary>
    Cpu
}
