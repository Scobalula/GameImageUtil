namespace TextureHawk.Gpu;

/// <summary>
/// Describes a single field within a GLSL push constant block.
/// </summary>
/// <remarks>
/// Offset and size follow std430 layout rules as used by Vulkan push constants.
/// </remarks>
public readonly struct PushConstantField
{
    /// <summary>
    /// Field name as declared in the GLSL source.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Byte offset of the field within the push constant block.
    /// </summary>
    public required int Offset { get; init; }

    /// <summary>
    /// Size of the field in bytes.
    /// </summary>
    public required int Size { get; init; }

    /// <summary>
    /// Data type of the field.
    /// </summary>
    public required ShaderParameterType Type { get; init; }
}
