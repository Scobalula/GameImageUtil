namespace TextureHawk.Gpu;

/// <summary>
/// Describes a GPU device available for compute operations.
/// </summary>
/// <remarks>
/// Used for runtime GPU selection. The <see cref="Name"/> and <see cref="DeviceId"/>
/// uniquely identify a device for settings persistence.
/// </remarks>
/// <param name="Name">Human-readable name of the GPU device.</param>
/// <param name="DeviceId">PCI device identifier.</param>
/// <param name="VendorId">PCI vendor identifier.</param>
/// <param name="DeviceType">Classification of the device (discrete, integrated, etc.).</param>
/// <param name="MemoryBytes">Total device-local memory in bytes.</param>
public readonly record struct GpuDevice(string Name, uint DeviceId, uint VendorId, GpuDeviceType DeviceType, ulong MemoryBytes);
