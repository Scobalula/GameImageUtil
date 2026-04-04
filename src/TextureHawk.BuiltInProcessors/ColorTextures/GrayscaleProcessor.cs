using RedFox.Graphics2D;
using TextureHawk.Gpu;
using TextureHawk.Logging;
using TextureHawk.Processing;

namespace TextureHawk.BuiltInProcessors.ColorTextures;

/// <summary>
/// Converts an image to grayscale on the GPU using Rec.709 luminance weights.
/// </summary>
/// <remarks>
/// Push constant layout: outWidth, outHeight (2×int32 = 8 bytes).
/// </remarks>
public sealed class GrayscaleProcessor : IProcessor
{
    /// </inheritdoc />
    public string Id => nameof(GrayscaleProcessor);

    /// </inheritdoc />
    public string Name => "Grayscale";

    /// </inheritdoc />
    public string Description => "Convert an image to grayscale using Rec.709 luminance weights.";

    /// </inheritdoc />
    public string Category => "Color Textures";

    /// </inheritdoc />
    public string Icon => "🩶";

    /// </inheritdoc />
    public IReadOnlyList<ProcessorParameter> Parameters { get; } = [];

    /// </inheritdoc />
    public async ValueTask<ProcessingResult> ProcessAsync(ProcessorContext context, CancellationToken cancellationToken)
    {
        var source = context.TranslatorManager.Read(context.InputPath);
        AppLog.Info("Grayscale", $"Processing {source.Width}×{source.Height} ({source.Format})");

        var shader = GpuShaderCache.GetShaderBytecode("grayscale");
        var pushConstants = GpuShaderCache.GetLayout("grayscale").CreateBuilder()
            .Set("outWidth", source.Width)
            .Set("outHeight", source.Height)
            .Build();

        var result = await context.ComputeEngine.DispatchAsync(shader, GpuImageFormats.PrepareInputsForSampling([source], context.GpuColorSpaceMode), pushConstants, source.Width, source.Height, GpuImageFormats.ResolveWritableOutputFormat(source.Format), (1, 1, 1), cancellationToken);
        AppLog.Info("Grayscale", "Processing complete");
        return ProcessingResult.Ok(result);
    }
}
