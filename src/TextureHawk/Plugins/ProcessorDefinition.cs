namespace TextureHawk.Plugins;

/// <summary>
/// Format-agnostic definition of a processor loaded from a configuration file.
/// </summary>
/// <remarks>
/// Describes a processor that can be loaded from external definition files
/// (e.g., TOML, or future formats) without requiring compiled code.
/// A definition either references a base processor with overridden defaults,
/// or specifies a compute shader path for GPU execution.
/// <para>Example TOML definition:</para>
/// <code>
/// id = "custom.my-filter"
/// name = "My Custom Filter"
/// description = "Applies a custom filter"
/// category = "Custom"
/// basedOn = "builtin.format-convert"
///
/// [defaults]
/// targetFormat = "BC7Unorm"
///
/// [[parameters]]
/// id = "strength"
/// name = "Strength"
/// type = "float"
/// default = 1.0
/// min = 0.0
/// max = 2.0
/// </code>
/// </remarks>
public sealed class ProcessorDefinition
{
    /// <summary>
    /// Unique identifier for this processor (e.g., "custom.my-filter").
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Display name shown in the UI and CLI help.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Brief description of what this processor does.
    /// </summary>
    public string Description { get; init; } = "";

    /// <summary>
    /// Category for grouping in the UI (e.g., "Custom", "Conversion").
    /// </summary>
    public string Category { get; init; } = "Custom";

    /// <summary>
    /// Emoji glyph shown next to the processor name in the UI.
    /// </summary>
    public string Icon { get; init; } = "🔧";

    /// <summary>
    /// Path to a SPIR-V compute shader, or null if this processor delegates to a base processor.
    /// </summary>
    public string? ShaderPath { get; init; }

    /// <summary>
    /// The ID of another processor to delegate to with overridden defaults.
    /// </summary>
    public string? BasedOnProcessorId { get; init; }

    /// <summary>
    /// User-configurable parameters exposed by this processor.
    /// </summary>
    public List<ParameterDefinition> Parameters { get; init; } = [];

    /// <summary>
    /// Parameter ID used to resolve output width for shader-based processors.
    /// </summary>
    public string? OutputWidthParameterId { get; init; }

    /// <summary>
    /// Parameter ID used to resolve output height for shader-based processors.
    /// </summary>
    public string? OutputHeightParameterId { get; init; }

    /// <summary>
    /// Compute shader workgroup size on X.
    /// </summary>
    public int WorkGroupSizeX { get; init; } = 16;

    /// <summary>
    /// Compute shader workgroup size on Y.
    /// </summary>
    public int WorkGroupSizeY { get; init; } = 16;

    /// <summary>
    /// Compute shader workgroup size on Z.
    /// </summary>
    public int WorkGroupSizeZ { get; init; } = 1;

    /// <summary>
    /// Default parameter values applied before user-supplied values.
    /// </summary>
    public Dictionary<string, object?> Defaults { get; init; } = [];
}
