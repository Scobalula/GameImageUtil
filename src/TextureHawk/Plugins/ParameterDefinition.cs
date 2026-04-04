namespace TextureHawk.Plugins;

/// <summary>
/// Format-agnostic parameter definition within a <see cref="ProcessorDefinition"/>.
/// </summary>
/// <remarks>
/// Describes a single configurable parameter for an externally defined processor.
/// The <see cref="Type"/> string is parsed at load time to map to
/// <see cref="Processing.ProcessorParameterType"/>.
/// </remarks>
public sealed class ParameterDefinition
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
    /// Brief description shown as tooltip or help text.
    /// </summary>
    public string Description { get; init; } = "";

    /// <summary>
    /// The data type string (e.g., "float", "int", "bool", "choice").
    /// </summary>
    public required string Type { get; init; }

    /// <summary>
    /// Default value when the user has not specified one.
    /// </summary>
    public object? Default { get; init; }

    /// <summary>
    /// Minimum allowed value for numeric parameters.
    /// </summary>
    public object? Min { get; init; }

    /// <summary>
    /// Maximum allowed value for numeric parameters.
    /// </summary>
    public object? Max { get; init; }

    /// <summary>
    /// List of valid options for choice parameters.
    /// </summary>
    public List<string>? Options { get; init; }
}
