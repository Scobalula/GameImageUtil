using RedFox.Graphics2D;
using TextureHawk.Gpu;
using TextureHawk.Logging;
using TextureHawk.Processing;

namespace TextureHawk.BuiltInProcessors;

/// <summary>
/// Merges an RGB source with a separate alpha channel source on the GPU.
/// </summary>
/// <remarks>
/// Merges a primary RGB image with a configurable alpha source image.
/// Shader uses 3 SSBOs: binding 0 = RGB, binding 1 = Alpha, binding 2 = Output.
/// Push constant layout: outWidth, outHeight, srcWidth, srcHeight,
/// alphaSourceChannel (5×int32 = 20 bytes).
/// Channel indices: 0=R, 1=G, 2=B, 3=A, 4=Luminance.
/// </remarks>
public sealed class ChannelMergeProcessor : IProcessor
{
    /// <inheritdoc />
    public string Id => "builtin.channel-merge";

    /// <inheritdoc />
    public string Name => "Channel Merge (RGB + Alpha)";

    /// <inheritdoc />
    public string Description => "Combine an RGB image with a separate alpha channel image.";

    /// <inheritdoc />
    public string Category => "Channels";

    /// <inheritdoc />
    public string Icon => "🎚";

    /// <inheritdoc />

    /// <inheritdoc />

    /// <inheritdoc />
    public IReadOnlyList<ProcessorParameter> Parameters { get; } =
    [
        new ProcessorParameter
        {
            Id = "alphaSource",
            Name = "Alpha Source Channel",
            Description = "Which channel of the alpha image to use as the alpha channel.",
            Type = ProcessorParameterType.Choice,
            DefaultValue = "Red",
            Options = ["Red", "Green", "Blue", "Alpha", "Luminance"]
        },
        new ProcessorParameter
        {
            Id = "alphaPath",
            Name = "Alpha Path",
            Description = "Optional explicit alpha image path. Leave empty to use auto-detected companion file.",
            Type = ProcessorParameterType.FilePath,
            DefaultValue = ""
        }
    ];

    /// <inheritdoc />
    public async ValueTask<ProcessingResult> ProcessAsync(ProcessorContext context, CancellationToken cancellationToken)
    {
        var rgb = context.TranslatorManager.Read(context.InputPath);
        AppLog.Info("ChannelMerge", $"Processing {rgb.Width}×{rgb.Height} ({rgb.Format})");

        var alphaFile = context.GetParameterValue("alphaPath", string.Empty);

        if (string.IsNullOrWhiteSpace(alphaFile))
            return ProcessingResult.Fail("Alpha channel image is required. Set Alpha Path or enable Auto Find Alpha.");

        var alpha = context.TranslatorManager.Read(alphaFile);

        var alphaSource = context.GetParameterValue("alphaSource", "Red");
        var alphaChannelIndex = alphaSource switch
        {
            "Green" => 1,
            "Blue" => 2,
            "Alpha" => 3,
            "Luminance" => 4,
            _ => 0 // Red
        };

        var spirv = GpuShaderCache.GetShaderBytecode("channel_merge");
        var pushConstants = GpuShaderCache.GetLayout("channel_merge").CreateBuilder()
            .Set("outWidth", rgb.Width)
            .Set("outHeight", rgb.Height)
            .Set("srcWidth", rgb.Width)
            .Set("srcHeight", rgb.Height)
            .Set("alphaSourceChannel", alphaChannelIndex)
            .Build();

        var result = await context.ComputeEngine.DispatchAsync(spirv, GpuImageFormats.PrepareInputsForSampling([rgb, alpha], context.GpuColorSpaceMode), pushConstants, rgb.Width, rgb.Height, GpuImageFormats.ResolveWritableOutputFormat(rgb.Format), (1, 1, 1), cancellationToken);
        return ProcessingResult.Ok(result);
    }
}
