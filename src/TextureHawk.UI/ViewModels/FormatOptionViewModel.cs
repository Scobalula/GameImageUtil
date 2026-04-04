namespace TextureHawk.UI.ViewModels;

/// <summary>
/// Represents a display-ready image format option with icon and extension.
/// </summary>
public sealed class FormatOptionViewModel
{
    /// <summary>
    /// Format extension value, including leading dot (for example ".png").
    /// </summary>
    public required string Extension { get; init; }

    /// <summary>
    /// Short icon shown in format combo boxes.
    /// </summary>
    public required string Icon { get; init; }

    /// <summary>
    /// Human-friendly display label.
    /// </summary>
    public required string DisplayName { get; init; }

    /// <inheritdoc />
    public override string ToString() => Extension;
}
