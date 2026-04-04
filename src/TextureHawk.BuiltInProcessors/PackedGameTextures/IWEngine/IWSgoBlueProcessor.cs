using TextureHawk.Processing;

namespace TextureHawk.BuiltInProcessors.PackedGameTextures.IWEngine;

/// <summary>
/// Splits MW2R-style blue packed S/G/O textures into separate outputs.
/// </summary>
public sealed class IWSgoBlueProcessor : IProcessor
{
    /// <inheritdoc/>
    public string Id => nameof(IWSgoBlueProcessor);

    /// <inheritdoc/>
    public string Name => "IW S/G/O (Blue Image)";

    /// <inheritdoc/>
    public string Description => "Split packed blue-image S/G/O textures into _s, _g, and _o outputs.";

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
            Description = "Use the legacy GameImageUtil CoD name sanitization before writing outputs.",
            Type = ProcessorParameterType.Boolean,
            DefaultValue = true
        }
    ];

    /// <inheritdoc/>
    public ValueTask<ProcessingResult> ProcessAsync(ProcessorContext context, CancellationToken cancellationToken)
    {
        var outputPathOverride = context.GetParameterValue("sanitizeOutputName", true)
            ? LegacyOutputNaming.BuildOutputPath(context, LegacyOutputNaming.ReplaceLegacyDelimiters(Path.GetFileNameWithoutExtension(context.InputPath)))
            : null;
        return PackedChannelSplitRunner.RunAsync(context, "CoDSgoBlue", "split_3", ["_s", "_g", "_o"], cancellationToken, outputPathOverride);
    }
}
