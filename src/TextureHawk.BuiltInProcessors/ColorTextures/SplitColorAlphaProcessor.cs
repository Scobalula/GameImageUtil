using TextureHawk.Processing;

namespace TextureHawk.BuiltInProcessors.ColorTextures;

/// <summary>
/// Splits a texture into RGB color and grayscale alpha outputs.
/// </summary>
public sealed class SplitColorAlphaProcessor : IProcessor
{
    public string Id => nameof(SplitColorAlphaProcessor);
    public string Name => "Split Color/Alpha";
    public string Description => "Split RGB color and alpha into two outputs named _rgb and _alpha.";
    public string Category => "Color Textures";
    public string Icon => "✂";
    public IReadOnlyList<ProcessorParameter> Parameters => [];

    public ValueTask<ProcessingResult> ProcessAsync(ProcessorContext context, CancellationToken cancellationToken)
        => PackedChannelSplitRunner.RunAsync(context, "SplitColorAlpha", "split_2", ["_rgb", "_alpha"], cancellationToken);
}
