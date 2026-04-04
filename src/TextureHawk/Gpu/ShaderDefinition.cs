using RedFox.Graphics2D;

namespace TextureHawk.Gpu;

/// <summary>
/// Defines a compute shader with its parameters and resource bindings.
/// </summary>
/// <remarks>
/// Used to describe the interface between the CPU pipeline and GPU compute shaders.
/// The schema specifies input/output textures, uniform parameters, and dispatch dimensions.
/// </remarks>
public sealed class ShaderDefinition
{
    /// <summary>
    /// Unique name identifying this shader.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Path to the compiled shader (SPIR-V for Vulkan).
    /// </summary>
    public required string ShaderPath { get; init; }

    /// <summary>
    /// Shader parameters (uniforms/push constants).
    /// </summary>
    public IReadOnlyList<ShaderParameter> Parameters { get; init; } = [];

    /// <summary>
    /// Number of input image bindings.
    /// </summary>
    public int InputCount { get; init; } = 1;

    /// <summary>
    /// Work group size for compute dispatch.
    /// </summary>
    public (int X, int Y, int Z) WorkGroupSize { get; init; } = (16, 16, 1);
}
