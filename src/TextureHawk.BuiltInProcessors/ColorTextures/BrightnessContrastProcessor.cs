using RedFox.Graphics2D;
using TextureHawk.Gpu;
using TextureHawk.Logging;
using TextureHawk.Processing;

namespace TextureHawk.BuiltInProcessors.ColorTextures;

/// <summary>
/// Provides an image processor that adjusts the brightness and contrast of an image using configurable parameters.
/// </summary>
public sealed class BrightnessContrastProcessor : IProcessor
{
    /// </inheritdoc/>
    public string Id => nameof(BrightnessContrastProcessor);

    /// </inheritdoc/>
    public string Name => "Brightness / Contrast";

    /// </inheritdoc/>
    public string Description => "Adjust brightness and contrast of an image.";

    /// </inheritdoc/>
    public string Category => "Color Textures";

    /// </inheritdoc/>
    public string Icon => "☀️";

    /// </inheritdoc/>
    public IReadOnlyList<ProcessorParameter> Parameters { get; } =
    [
        new ProcessorParameter
        {
            Id = "brightness",
            Name = "Brightness",
            Description = "Brightness offset (-1.0 to 1.0). Positive values brighten, negative darken.",
            Type = ProcessorParameterType.Float,
            DefaultValue = 0.0f
        },
        new ProcessorParameter
        {
            Id = "contrast",
            Name = "Contrast",
            Description = "Contrast multiplier (0.0 = flat grey, 1.0 = unchanged, 2.0 = double contrast).",
            Type = ProcessorParameterType.Float,
            DefaultValue = 1.0f
        }
    ];

    /// </inheritdoc/>
    public async ValueTask<ProcessingResult> ProcessAsync(ProcessorContext context, CancellationToken cancellationToken)
    {
        var brightness = context.GetParameterValue("brightness", 0.0f);
        var contrast = context.GetParameterValue("contrast", 1.0f);
        var source = context.TranslatorManager.Read(context.InputPath);
        AppLog.Info("BrightnessContrast", $"Processing {source.Width}×{source.Height} ({source.Format}), brightness={brightness:F2}, contrast={contrast:F2}");

        var shader = GpuShaderCache.GetShaderBytecode("brightness_contrast");
        var pushConstants = GpuShaderCache.GetLayout("brightness_contrast").CreateBuilder()
            .Set("outWidth", source.Width)
            .Set("outHeight", source.Height)
            .Set("brightness", brightness)
            .Set("contrast", contrast)
            .Build();

        var result = await context.ComputeEngine.DispatchAsync(shader, GpuImageFormats.PrepareInputsForSampling([source], context.GpuColorSpaceMode), pushConstants, source.Width, source.Height, GpuImageFormats.ResolveWritableOutputFormat(source.Format), (1, 1, 1), cancellationToken);
        AppLog.Info("BrightnessContrast", "Processing complete");
        return ProcessingResult.Ok(result);
    }
}
