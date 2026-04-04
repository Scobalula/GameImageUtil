using TextureHawk.Gpu;
using TextureHawk.Logging;
using TextureHawk.Processing;

namespace TextureHawk.BuiltInProcessors.EnvironmentTextures;

public sealed class EquirectToHemiOctaProcessor : IProcessor
{
    public string Id => "builtin.equirect-to-hemiocta";

    public string Name => "Equirectangular to Hemi-Octahedral";

    public string Description => "Convert an equirectangular environment map into a packed hemi-octahedral map.";

    public string Category => "Environment Textures";

    public string Icon => "🌍";

    public IReadOnlyList<ProcessorParameter> Parameters { get; } =
    [
        new ProcessorParameter
        {
            Id = "outputWidth",
            Name = "Output Width",
            Description = "Width of the hemi-octahedral output in pixels. Height will be half this value.",
            Type = ProcessorParameterType.Integer,
            DefaultValue = 4096,
            MinValue = 64,
            MaxValue = 16384
        },
        .. ProjectionParameters.OrientationParameters,
    ];

    public async ValueTask<ProcessingResult> ProcessAsync(ProcessorContext context, CancellationToken cancellationToken)
    {
        var source = context.TranslatorManager.Read(context.InputPath);
        AppLog.Info("EquirectToHemiOcta", $"Processing {source.Width}×{source.Height} ({source.Format})");

        var outputWidth = Math.Clamp(context.GetParameterValue("outputWidth", Math.Max(64, source.Width)), 64, 16384);
        var outputHeight = outputWidth / 2;
        var pushConstants = GpuShaderCache.GetLayout("equirect_to_hemiocta").CreateBuilder()
            .Set("outWidth", outputWidth)
            .Set("outHeight", outputHeight)
            .Set("upAxis", ProjectionConverters.ResolveUpAxis(context))
            .Set("mapX", ProjectionConverters.ResolveMapX(context))
            .Set("mapY", ProjectionConverters.ResolveMapY(context))
            .Set("mapZ", ProjectionConverters.ResolveMapZ(context))
            .Build();

        var result = await context.ComputeEngine.DispatchAsync(
            GpuShaderCache.GetShaderBytecode("equirect_to_hemiocta"),
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
