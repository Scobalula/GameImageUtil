namespace TextureHawk.Gpu.Vulkan;

/// <summary>
/// Wrapper around a Vulkan buffer handle for type safety within the compute engine.
/// </summary>
/// <remarks>
/// Prevents accidental misuse of raw <see cref="Silk.NET.Vulkan.Buffer"/> handles
/// by providing a distinct type for internal buffer management.
/// </remarks>
/// <param name="Handle">The underlying Vulkan buffer handle.</param>
internal readonly record struct VkBuffer(Silk.NET.Vulkan.Buffer Handle);
