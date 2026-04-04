namespace TextureHawk.Gpu;

/// <summary>
/// A single parameter in a compute shader (push constant or uniform buffer field).
/// </summary>
public sealed class ShaderParameter
{
    /// <summary>
    /// The name of the parameter as referenced in the shader source.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// The data type of this parameter.
    /// </summary>
    public required ShaderParameterType Type { get; init; }

    /// <summary>
    /// Byte offset of the parameter within the push constant or uniform buffer.
    /// </summary>
    public int Offset { get; init; }

    /// <summary>
    /// Size of the parameter in bytes.
    /// </summary>
    public int Size { get; init; }
}
