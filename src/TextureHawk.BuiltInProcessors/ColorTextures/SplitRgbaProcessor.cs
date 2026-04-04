using TextureHawk.Processing;

namespace TextureHawk.BuiltInProcessors.ColorTextures;

/// <summary>
/// Splits a texture into separate R, G, B, and A grayscale outputs.
/// </summary>
public sealed class SplitRgbaProcessor : IProcessor
{
    public string Id => nameof(SplitRgbaProcessor);
    public string Name => "Split All Channels";
    public string Description => "Split RGBA into four grayscale outputs named _r, _g, _b, and _a.";
    public string Category => "Color Textures";
    public string Icon => "✂";
    public IReadOnlyList<ProcessorParameter> Parameters => [];

    public ValueTask<ProcessingResult> ProcessAsync(ProcessorContext context, CancellationToken cancellationToken)
        => PackedChannelSplitRunner.RunAsync(context, "SplitRGBA", "split_4", ["_r", "_g", "_b", "_a"], cancellationToken);
}
