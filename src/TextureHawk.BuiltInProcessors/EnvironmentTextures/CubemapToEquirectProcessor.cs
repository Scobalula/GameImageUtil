using RedFox.Graphics2D;
using TextureHawk.Gpu;
using TextureHawk.Logging;
using TextureHawk.Processing;

namespace TextureHawk.BuiltInProcessors.EnvironmentTextures;

/// <summary>
/// Converts a cubemap image to an equirectangular (lat-long) projection on the GPU.
/// </summary>
/// <remarks>
/// For each pixel in the output equirectangular image, the compute shader computes
/// the corresponding 3D direction vector, determines which cubemap face to sample,
/// and performs bilinear interpolation. Uses the DDS/DirectX face ordering:
/// +X, -X, +Y, -Y, +Z, -Z (array indices 0–5).
/// Shader uses one sampled cubemap at binding 0 and one storage image output at binding 1.
/// Push constant layout: outWidth, outHeight, upAxis, mapX, mapY, mapZ (6×int32 = 24 bytes).
/// </remarks>
public sealed class CubemapToEquirectProcessor : IProcessor
{
    /// <inheritdoc />
    public string Id => "builtin.cubemap-to-equirect";

    /// <inheritdoc />
    public string Name => "Cubemap to Equirectangular";

    /// <inheritdoc />
    public string Description => "Convert a cubemap to an equirectangular (lat-long) panorama projection.";

    /// <inheritdoc />
    public string Category => "Environment Textures";

    /// <inheritdoc />
    public string Icon => "🌍";

    /// <inheritdoc />
    public IReadOnlyList<ProcessorParameter> Parameters => CubemapParameters.DefaultParameters;

    /// <inheritdoc />
    public async ValueTask<ProcessingResult> ProcessAsync(ProcessorContext context, CancellationToken cancellationToken)
    {
        var primary = context.TranslatorManager.Read(context.InputPath);
        AppLog.Info("CubemapToEquirect", $"Processing {primary.Width}×{primary.Height} ({primary.Format})");
        var source = ProjectionTransforms.PrepareCubemapSource(context, primary);

        if (!source.IsCubemap || source.Slices.Length < 6)
            return ProcessingResult.Fail("Input must be a cubemap with 6 faces.");

        if (source.Width != source.Height)
            return ProcessingResult.Fail("Cubemap faces must be square.");

        var outputWidth = context.GetParameterValue("outputWidth", 4096);
        var outputHeight = outputWidth / 2;
        var pushConstants = GpuShaderCache.GetLayout("cubemap_to_equirect").CreateBuilder()
            .Set("outWidth", outputWidth)
            .Set("outHeight", outputHeight)
            .Set("upAxis", ProjectionConverters.ResolveUpAxis(context))
            .Set("mapX", ProjectionConverters.ResolveMapX(context))
            .Set("mapY", ProjectionConverters.ResolveMapY(context))
            .Set("mapZ", ProjectionConverters.ResolveMapZ(context))
            .Build();

        var result = await context.ComputeEngine.DispatchAsync(
            GpuShaderCache.GetShaderBytecode("cubemap_to_equirect"),
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
