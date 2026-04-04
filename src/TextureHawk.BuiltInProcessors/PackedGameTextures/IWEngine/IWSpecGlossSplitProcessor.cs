using TextureHawk.Processing;

namespace TextureHawk.BuiltInProcessors.PackedGameTextures.CallOfDuty;

/// <summary>
/// Splits a CoD spec/gloss texture into spec color and gloss grayscale outputs.
/// </summary>
public sealed class IWSpecGlossSplitProcessor : IProcessor
{
    /// <inheritdoc/>
    public string Id => nameof(IWSpecGlossSplitProcessor);

    /// <inheritdoc/>
    public string Name => "IW Spec/Gloss";

    /// <inheritdoc/>
    public string Description => "Split IW spec/gloss textures into _s and _g outputs.";

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
        return PackedChannelSplitRunner.RunAsync(context, "CoDSpecGloss", "split_2", ["_s", "_g"], cancellationToken, outputPathOverride);
    }
}
