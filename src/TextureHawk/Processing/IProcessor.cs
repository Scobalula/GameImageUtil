namespace TextureHawk.Processing;

/// <summary>
/// Defines a processor that transforms or produces images.
/// </summary>
/// <remarks>
/// Processors are the core building blocks of TextureHawk's pipeline.
/// Each processor declares its parameters and performs the actual image
/// transformation. Processors may invoke other
/// processors through the execution helpers on <see cref="ProcessorContext"/>.
/// </remarks>
public interface IProcessor
{
    /// <summary>
    /// Unique identifier for this processor (e.g., "builtin.format-convert").
    /// </summary>
    string Id { get; }

    /// <summary>
    /// Display name shown in the UI and CLI help.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Brief description of what this processor does.
    /// </summary>
    string Description { get; }

    /// <summary>
    /// Category for grouping in the UI (e.g., "Conversion", "Channels", "Normal Maps").
    /// </summary>
    string Category { get; }

    /// <summary>
    /// Emoji glyph shown next to the processor name in the UI.
    /// </summary>
    string Icon { get; }

    /// <summary>
    /// Parameters this processor accepts, used by the UI to render controls.
    /// </summary>
    IReadOnlyList<ProcessorParameter> Parameters { get; }

    /// <summary>
    /// Processes the input image(s) and returns the result.
    /// </summary>
    ValueTask<ProcessingResult> ProcessAsync(ProcessorContext context, CancellationToken cancellationToken);
}
