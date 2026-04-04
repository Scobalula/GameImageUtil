using RedFox.Graphics2D;
using TextureHawk.Gpu;
using TextureHawk.Logging;
using TextureHawk.Processing;

namespace TextureHawk.BuiltInProcessors.NormalMapping;

/// <summary>
/// Flips the Y (green) channel of a normal map on the GPU.
/// </summary>
/// <remarks>
/// Different engines use different normal map conventions (OpenGL vs DirectX).
/// OpenGL uses Y-up, DirectX uses Y-down. This processor inverts the green channel
/// to convert between the two conventions. Optionally reconstructs the Z (blue)
/// channel from X and Y for two-channel normal maps.
/// Push constant layout: outWidth, outHeight, srcWidth, srcHeight, reconstructZ (5×int32 = 20 bytes).
/// </remarks>
public sealed class NormalMapFlipYProcessor : IProcessor
{
    /// <inheritdoc />
    public string Id => "builtin.normalmap-flipy";

    /// <inheritdoc />
    public string Name => "Normal Map Flip Y";

    /// <inheritdoc />
    public string Description => "Flip the Y (green) channel of a normal map (OpenGL ↔ DirectX convention).";

    /// <inheritdoc />
    public string Category => "Normal Mapping";

    /// <inheritdoc />
    public string Icon => "↕";

    /// <inheritdoc />
    public IReadOnlyList<ProcessorParameter> Parameters { get; } =
    [
        new ProcessorParameter
        {
            Id = "reconstructZ",
            Name = "Reconstruct Z",
            Description = "Reconstruct the blue (Z) channel from X and Y (for two-channel normal maps).",
            Type = ProcessorParameterType.Boolean,
            DefaultValue = false
        }
    ];

    /// <inheritdoc />
    public async ValueTask<ProcessingResult> ProcessAsync(ProcessorContext context, CancellationToken cancellationToken)
    {
        var reconstructZ = context.GetParameterValue("reconstructZ", false);
        var source = context.TranslatorManager.Read(context.InputPath);
        AppLog.Info("FlipY", $"Processing {source.Width}×{source.Height} ({source.Format}), reconstructZ={reconstructZ}");

        var spirv = GpuShaderCache.GetShaderBytecode("normalmap_flipy");
        var pushConstants = GpuShaderCache.GetLayout("normalmap_flipy").CreateBuilder()
            .Set("outWidth", source.Width)
            .Set("outHeight", source.Height)
            .Set("srcWidth", source.Width)
            .Set("srcHeight", source.Height)
            .Set("reconstructZ", reconstructZ ? 1 : 0)
            .Build();

        var result = await context.ComputeEngine.DispatchAsync(spirv, GpuImageFormats.PrepareInputsForSampling([source], context.GpuColorSpaceMode), pushConstants, source.Width, source.Height, GpuImageFormats.ResolveWritableOutputFormat(source.Format), (1, 1, 1), cancellationToken);
        AppLog.Info("FlipY", "Processing complete");
        return ProcessingResult.Ok(result);
    }
}
