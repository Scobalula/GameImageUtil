using Silk.NET.Vulkan;

namespace TextureHawk.Gpu.Vulkan;

/// <summary>
/// Tracks all Vulkan resources for a single compute output image.
/// </summary>
/// <remarks>
/// Output images are written directly by the compute shader as storage images,
/// then copied into a host-visible staging buffer for CPU readback.
/// </remarks>
internal readonly record struct VkOutputImage(
    Silk.NET.Vulkan.Image Image,
    DeviceMemory ImageMemory,
    ImageView View,
    VkBuffer StagingBuffer,
    DeviceMemory StagingMemory,
    ulong StagingSize,
    int Width,
    int Height);
