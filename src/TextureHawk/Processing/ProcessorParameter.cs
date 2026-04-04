namespace TextureHawk.Processing;

/// <summary>
/// Describes a configurable parameter for an <see cref="IProcessor"/>.
/// </summary>
/// <remarks>
/// Used by the UI and CLI to dynamically render input controls
/// and validate user-supplied values before processing.
/// </remarks>
public sealed class ProcessorParameter
{
    /// <summary>
    /// Unique identifier for this parameter within its processor.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Human-readable display name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Brief description shown as a tooltip or help text.
    /// </summary>
    public required string Description { get; init; }

    /// <summary>
    /// The data type of this parameter.
    /// </summary>
    public required ProcessorParameterType Type { get; init; }

    /// <summary>
    /// Default value when the user hasn't specified one.
    /// </summary>
    public object? DefaultValue { get; init; }

    /// <summary>
    /// Minimum allowed value for <see cref="ProcessorParameterType.Integer"/>
    /// and <see cref="ProcessorParameterType.Float"/> parameters.
    /// </summary>
    public object? MinValue { get; init; }

    /// <summary>
    /// Maximum allowed value for <see cref="ProcessorParameterType.Integer"/>
    /// and <see cref="ProcessorParameterType.Float"/> parameters.
    /// </summary>
    public object? MaxValue { get; init; }

    /// <summary>
    /// Available options for <see cref="ProcessorParameterType.Choice"/> and
    /// <see cref="ProcessorParameterType.ImageFormat"/> parameters.
    /// </summary>
    public IReadOnlyList<string>? Options { get; init; }
}
