using RedFox.Graphics2D;
using TextureHawk.Gpu;
using TextureHawk.Logging;
using TextureHawk.Processing;

namespace TextureHawk.BuiltInProcessors;

/// <summary>
/// Resizes an image using bilinear interpolation on the GPU.
/// </summary>
/// <remarks>
/// Supports both absolute pixel dimensions and relative scaling.
/// The resize is performed via a Vulkan compute shader using bilinear sampling.
/// Push constant layout: outWidth, outHeight, srcWidth, srcHeight (4×int32 = 16 bytes).
/// </remarks>
public sealed class ResizeProcessor : IProcessor
{
    /// <inheritdoc />
    public string Id => "builtin.resize";

    /// <inheritdoc />
    public string Name => "Resize";

    /// <inheritdoc />
    public string Description => "Resize image to specified dimensions.";

    /// <inheritdoc />
    public string Category => "Transform";

    /// <inheritdoc />
    public string Icon => "📐";

    /// <inheritdoc />

    /// <inheritdoc />

    /// <inheritdoc />
    public IReadOnlyList<ProcessorParameter> Parameters { get; } =
    [
        new ProcessorParameter
        {
            Id = "width",
            Name = "Width",
            Description = "Output width in pixels (0 = auto from height + aspect ratio).",
            Type = ProcessorParameterType.Integer,
            DefaultValue = 0,
            MinValue = 0,
            MaxValue = 16384
        },
        new ProcessorParameter
        {
            Id = "height",
            Name = "Height",
            Description = "Output height in pixels (0 = auto from width + aspect ratio).",
            Type = ProcessorParameterType.Integer,
            DefaultValue = 0,
            MinValue = 0,
            MaxValue = 16384
        },
        new ProcessorParameter
        {
            Id = "scale",
            Name = "Scale Factor",
            Description = "Scale factor (e.g., 0.5 = half size). Used when width and height are 0.",
            Type = ProcessorParameterType.Float,
            DefaultValue = 1.0f,
            MinValue = 0.01f,
            MaxValue = 16.0f
        }
    ];

    /// <inheritdoc />
    public async ValueTask<ProcessingResult> ProcessAsync(ProcessorContext context, CancellationToken cancellationToken)
    {
        var source = context.TranslatorManager.Read(context.InputPath);
        AppLog.Info("Resize", $"Processing {source.Width}×{source.Height} ({source.Format})");
        var targetWidth = context.GetParameterValue("width", 0);
        var targetHeight = context.GetParameterValue("height", 0);
        var scale = context.GetParameterValue("scale", 1.0f);

        // Resolve dimensions
        if (targetWidth <= 0 && targetHeight <= 0)
        {
            targetWidth = (int)(source.Width * scale);
            targetHeight = (int)(source.Height * scale);
        }
        else if (targetWidth <= 0)
        {
            targetWidth = (int)(targetHeight * ((float)source.Width / source.Height));
        }
        else if (targetHeight <= 0)
        {
            targetHeight = (int)(targetWidth * ((float)source.Height / source.Width));
        }

        targetWidth = Math.Clamp(targetWidth, 1, 16384);
        targetHeight = Math.Clamp(targetHeight, 1, 16384);

        if (targetWidth == source.Width && targetHeight == source.Height)
            return ProcessingResult.Ok(source);

        var spirv = GpuShaderCache.GetShaderBytecode("resize");
        var pushConstants = GpuShaderCache.GetLayout("resize").CreateBuilder()
            .Set("outWidth", targetWidth)
            .Set("outHeight", targetHeight)
            .Set("srcWidth", source.Width)
            .Set("srcHeight", source.Height)
            .Build();

        var result = await context.ComputeEngine.DispatchAsync(spirv, GpuImageFormats.PrepareInputsForSampling([source], context.GpuColorSpaceMode), pushConstants, targetWidth, targetHeight, GpuImageFormats.ResolveWritableOutputFormat(source.Format), (1, 1, 1), cancellationToken: cancellationToken);
        return ProcessingResult.Ok(result);
    }
}
