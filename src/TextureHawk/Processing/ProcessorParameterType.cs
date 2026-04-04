namespace TextureHawk.Processing;

/// <summary>
/// Defines the type of a processor parameter for UI rendering and validation.
/// </summary>
public enum ProcessorParameterType
{
    /// <summary>
    /// A boolean toggle (true/false).
    /// </summary>
    Boolean,

    /// <summary>
    /// An integer value with optional min/max bounds.
    /// </summary>
    Integer,

    /// <summary>
    /// A floating-point value with optional min/max bounds.
    /// </summary>
    Float,

    /// <summary>
    /// A free-form text value.
    /// </summary>
    String,

    /// <summary>
    /// A selection from a predefined list of options.
    /// </summary>
    Choice,

    /// <summary>
    /// An image format selection (maps to <see cref="RedFox.Graphics2D.ImageFormat"/>).
    /// </summary>
    ImageFormat,

    /// <summary>
    /// A file path for additional input files.
    /// </summary>
    FilePath
}
