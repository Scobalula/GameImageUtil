using RedFox.Graphics2D;
using TextureHawk.Gpu;
using TextureHawk.Logging;
using TextureHawk.Processing;

namespace TextureHawk.BuiltInProcessors.EnvironmentTextures;


public sealed class HemiOctaToEquirectProcessor : IProcessor
{
    /// <inheritdoc/>
    public string Id => nameof(HemiOctaToEquirectProcessor);

    /// <inheritdoc/>
    public string Name => "Hemi-Octahedral to Equirectangular";

    /// <inheritdoc/>
    public string Description => "Convert a packed hemi-octahedral environment map into an equirectangular map.";

    /// <inheritdoc/>
    public string Category => "Environment Textures";

    /// <inheritdoc/>
    public string Icon => "🌍";

    /// <inheritdoc/>
    public IReadOnlyList<ProcessorParameter> Parameters { get; } =
    [
        .. EquirectParameters.SizeParameters,
        .. HemiOctParameters.LayoutParameters,
        .. ProjectionParameters.OrientationParameters,
    ];

    /// <inheritdoc/>
    public async ValueTask<ProcessingResult> ProcessAsync(ProcessorContext context, CancellationToken cancellationToken)
    {
        var source = context.TranslatorManager.Read(context.InputPath);
        AppLog.Info("HemiOctaToEquirect", $"Processing {source.Width}×{source.Height} ({source.Format})");

        var (preparedSource, lowerHalf, hasLowerHalf, upperOnly) = ProjectionTransforms.ResolveHemiOctaSource(context, source);
        var outputWidth = Math.Clamp(context.GetParameterValue("outputWidth", Math.Max(64, preparedSource.Width)), 64, 16384);
        var outputHeight = outputWidth / 2;
        var hasSecondInput = lowerHalf is not null;
        var (invMapX, invMapY, invMapZ) = ProjectionConverters.ComputeInverseMapping(
            ProjectionConverters.ResolveMapX(context),
            ProjectionConverters.ResolveMapY(context),
            ProjectionConverters.ResolveMapZ(context));
        var pushConstants = GpuShaderCache.GetLayout("hemiocta_to_equirect").CreateBuilder()
            .Set("outWidth", outputWidth)
            .Set("outHeight", outputHeight)
            .Set("upAxis", ProjectionConverters.ResolveUpAxis(context))
            .Set("mapX", invMapX)
            .Set("mapY", invMapY)
            .Set("mapZ", invMapZ)
            .Set("hasLowerHalf", hasLowerHalf ? 1 : 0)
            .Set("upperOnly", upperOnly ? 1 : 0)
            .Set("hasSecondInput", hasSecondInput ? 1 : 0)
            .Build();

        Image[] inputs = [preparedSource, lowerHalf ?? preparedSource];
        var result = await context.ComputeEngine.DispatchAsync(
            GpuShaderCache.GetShaderBytecode("hemiocta_to_equirect"),
            GpuImageFormats.PrepareInputsForSampling(inputs, context.GpuColorSpaceMode),
            pushConstants,
            outputWidth,
            outputHeight,
            GpuImageFormats.ResolveWritableOutputFormat(preparedSource.Format),
            (1, 1, 1),
            cancellationToken: cancellationToken);

        return ProcessingResult.Ok(result);
    }
}
