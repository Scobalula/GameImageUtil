using Silk.NET.Vulkan;

namespace TextureHawk.Gpu.Vulkan;

/// <summary>
/// Tracks all Vulkan resources for a single input image bound as a sampled image in a compute dispatch.
/// </summary>
/// <remarks>
/// Input images are uploaded via a staging buffer, copied to a device-local VkImage,
/// and bound as combined image samplers so the GPU hardware can natively decode
/// any format (BC7, BC6H, HDR, etc.) without CPU-side decompression.
/// </remarks>
internal readonly record struct VkInputImage(
    Silk.NET.Vulkan.Image Image,
    DeviceMemory ImageMemory,
    ImageView View,
    Sampler Sampler,
    Silk.NET.Vulkan.Buffer StagingBuffer,
    DeviceMemory StagingMemory,
    int Width,
    int Height,
    int LayerCount,
    int BytesPerLayer);
