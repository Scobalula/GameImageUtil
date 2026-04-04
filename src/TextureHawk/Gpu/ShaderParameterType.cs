namespace TextureHawk.Gpu;

/// <summary>
/// Data types for shader parameters.
/// </summary>
public enum ShaderParameterType
{
    /// <summary>
    /// A single 32-bit floating-point value.
    /// </summary>
    Float,

    /// <summary>
    /// A two-component 32-bit float vector.
    /// </summary>
    Float2,

    /// <summary>
    /// A three-component 32-bit float vector.
    /// </summary>
    Float3,

    /// <summary>
    /// A four-component 32-bit float vector.
    /// </summary>
    Float4,

    /// <summary>
    /// A single 32-bit signed integer.
    /// </summary>
    Int,

    /// <summary>
    /// A two-component 32-bit signed integer vector.
    /// </summary>
    Int2,

    /// <summary>
    /// A three-component 32-bit signed integer vector.
    /// </summary>
    Int3,

    /// <summary>
    /// A four-component 32-bit signed integer vector.
    /// </summary>
    Int4,

    /// <summary>
    /// A single 32-bit unsigned integer.
    /// </summary>
    UInt,

    /// <summary>
    /// A 4x4 floating-point matrix.
    /// </summary>
    Mat4
}
