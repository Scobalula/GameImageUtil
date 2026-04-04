using RedFox.Graphics2D;
using TextureHawk.Gpu;
using TextureHawk.Logging;
using TextureHawk.Processing;

namespace TextureHawk.BuiltInProcessors.NormalMapping;

/// <summary>
/// Reconstructs the Z (blue) channel of a normal map from X and Y on the GPU.
/// </summary>
/// <remarks>
/// Two-channel (XY) normal maps store only X and Y and rely on the runtime to
/// derive Z as <c>sqrt(1 - x² - y²)</c>. This processor reconstructs Z into the
/// blue channel, producing a full three-channel normal map suitable for engines or
/// formats that require all three components.
/// Push constant layout: outWidth, outHeight, srcWidth, srcHeight (4×int32 = 16 bytes).
/// </remarks>
public sealed class NormalMapReconstructZProcessor : IProcessor
{
    /// <inheritdoc />
    public string Id => nameof(NormalMapReconstructZProcessor);

    /// <inheritdoc />
    public string Name => "Normal Map Reconstruct Z";

    /// <inheritdoc />
    public string Description => "Reconstruct the Z (blue) channel from a two-channel (XY) normal map.";

    /// <inheritdoc />
    public string Category => "Normal Mapping";

    /// <inheritdoc />
    public string Icon => "🧭";

    /// <inheritdoc />
    public IReadOnlyList<ProcessorParameter> Parameters { get; } =
    [
        new ProcessorParameter
        {
            Id = "xyLayout",
            Name = "XY Layout",
            Description = "Choose where the packed X and Y channels are stored in the source texture.",
            Type = ProcessorParameterType.Choice,
            DefaultValue = "Red/Green",
            Options = ["Red/Green", "Alpha/Green"]
        }
    ];

    /// <inheritdoc />
    public async ValueTask<ProcessingResult> ProcessAsync(ProcessorContext context, CancellationToken cancellationToken)
    {
        var source = context.TranslatorManager.Read(context.InputPath);
        AppLog.Info("ReconstructZ", $"Processing {source.Width}×{source.Height} ({source.Format})");

        var spirv = GpuShaderCache.GetShaderBytecode("normalmap_reconstruct_z");
        var xyLayout = context.GetParameterValue("xyLayout", "Red/Green");
        var pushConstants = GpuShaderCache.GetLayout("normalmap_reconstruct_z").CreateBuilder()
            .Set("outWidth", source.Width)
            .Set("outHeight", source.Height)
            .Set("srcWidth", source.Width)
            .Set("srcHeight", source.Height)
            .Set("xSource", xyLayout == "Alpha/Green" ? 3 : 0)
            .Set("ySource", 1)
            .Build();

        var result = await context.ComputeEngine.DispatchAsync(spirv, GpuImageFormats.PrepareInputsForSampling([source], context.GpuColorSpaceMode), pushConstants, source.Width, source.Height, GpuImageFormats.ResolveWritableOutputFormat(source.Format), (1, 1, 1), cancellationToken: cancellationToken);
        AppLog.Info("ReconstructZ", "Processing complete");
        return ProcessingResult.Ok(result);
    }
}
