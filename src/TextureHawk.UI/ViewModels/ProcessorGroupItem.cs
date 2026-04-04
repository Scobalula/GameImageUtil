namespace TextureHawk.UI.ViewModels;

/// <summary>
/// Represents a processor entry or category header in the grouped processor list.
/// </summary>
public sealed class ProcessorGroupItem
{
    /// <summary>
    /// Display text — processor name or category header.
    /// </summary>
    public required string DisplayName { get; init; }

    /// <summary>
    /// Emoji icon for this processor (empty for headers).
    /// </summary>
    public string Icon { get; init; } = "";

    /// <summary>
    /// Whether this item is a non-selectable category header.
    /// </summary>
    public bool IsHeader { get; init; }

    /// <summary>
    /// The processor reference, or <see langword="null"/> for category headers.
    /// </summary>
    public TextureHawk.Processing.IProcessor? Processor { get; init; }
}
