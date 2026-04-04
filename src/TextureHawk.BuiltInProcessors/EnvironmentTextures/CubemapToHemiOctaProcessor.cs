using TextureHawk.Gpu;
using TextureHawk.Logging;
using TextureHawk.Processing;

namespace TextureHawk.BuiltInProcessors.EnvironmentTextures;

public sealed class CubemapToHemiOctaProcessor : IProcessor
{
    public string Id => "builtin.cubemap-to-hemiocta";

    public string Name => "Cubemap to Hemi-Octahedral";

    public string Description => "Convert a cubemap into a packed hemi-octahedral environment map.";

    public string Category => "Environment Textures";

    /// <inheritdoc />
    public string Icon => "🌍";

    public IReadOnlyList<ProcessorParameter> Parameters => CubemapParameters.DefaultParameters;

    public async ValueTask<ProcessingResult> ProcessAsync(ProcessorContext context, CancellationToken cancellationToken)
    {
        var source = ProjectionTransforms.PrepareCubemapSource(context, context.TranslatorManager.Read(context.InputPath));
        AppLog.Info("CubemapToHemiOcta", $"Processing {source.Width}×{source.Height} ({source.Format})");

        if (!source.IsCubemap || source.Slices.Length < 6)
            return ProcessingResult.Fail("Input must be a cubemap with 6 faces.");

        var outputWidth = Math.Clamp(context.GetParameterValue("outputWidth", 4096), 64, 16384);
        var outputHeight = outputWidth / 2;
        var pushConstants = GpuShaderCache.GetLayout("cubemap_to_hemiocta").CreateBuilder()
            .Set("outWidth", outputWidth)
            .Set("outHeight", outputHeight)
            .Set("mapX", ProjectionConverters.ResolveMapX(context))
            .Set("mapY", ProjectionConverters.ResolveMapY(context))
            .Set("mapZ", ProjectionConverters.ResolveMapZ(context))
            .Build();

        var result = await context.ComputeEngine.DispatchAsync(
            GpuShaderCache.GetShaderBytecode("cubemap_to_hemiocta"),
            GpuImageFormats.PrepareInputsForSampling([source], context.GpuColorSpaceMode),
            pushConstants,
            outputWidth,
            outputHeight,
            GpuImageFormats.ResolveWritableOutputFormat(source.Format),
            (1, 1, 1),
            cancellationToken: cancellationToken);

        return ProcessingResult.Ok(result);
    }
}
