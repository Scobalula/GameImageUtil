using TextureHawk.Processing;

namespace TextureHawk.BuiltInProcessors.PackedGameTextures.IWEngine;

/// <summary>
/// Splits WW2-style green packed S/G/O textures into separate outputs.
/// </summary>
public sealed class IWSgoGreenProcessor : IProcessor
{
    /// <inheritdoc/>
    public string Id => nameof(IWSgoGreenProcessor);

    /// <inheritdoc/>
    public string Name => "IW S/G/O (Green Image)";

    /// <inheritdoc/>
    public string Description => "Split packed green-image S/G/O textures into _s, _o, and _g outputs.";

    /// <inheritdoc/>
    public string Category => "IW Engine";

    /// <inheritdoc/>
    public string Icon => "🎮";

    /// <inheritdoc/>
    public IReadOnlyList<ProcessorParameter> Parameters { get; } =
    [
        new ProcessorParameter
        {
            Id = "sanitizeOutputName",
            Name = "Sanitize Output Name",
            Description = "Use the legacy World War II name sanitization before writing outputs.",
            Type = ProcessorParameterType.Boolean,
            DefaultValue = true
        }
    ];


    /// <inheritdoc/>
    public ValueTask<ProcessingResult> ProcessAsync(ProcessorContext context, CancellationToken cancellationToken)
    {
        var sanitizeOutputName = context.GetParameterValue("sanitizeOutputName", true);
        var outputPathOverride = sanitizeOutputName ? LegacyOutputNaming.BuildOutputPath(context, LegacyOutputNaming.ReplaceLegacyWw2Delimiters(Path.GetFileNameWithoutExtension(context.InputPath))) : null;

        return PackedChannelSplitRunner.RunAsync(context, "CoDSgoGreen", "split_3", ["_s", "_o", "_g"], cancellationToken, outputPathOverride);
    }
}
