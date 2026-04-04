using RedFox.Graphics2D;
using TextureHawk.Gpu;
using TextureHawk.Logging;
using TextureHawk.Processing;

namespace TextureHawk.BuiltInProcessors.EnvironmentTextures;

public sealed class HemiOctaToCubemapProcessor : IProcessor
{
    public string Id => "builtin.hemiocta-to-cubemap";

    public string Name => "Hemi-Octahedral to Cubemap";

    public string Description => "Convert a packed hemi-octahedral environment map into a cubemap.";

    public string Category => "Environment Textures";

    public string Icon => "🌍";

    public IReadOnlyList<ProcessorParameter> Parameters { get; } =
    [
        new ProcessorParameter
        {
            Id = "faceSize",
            Name = "Face Size",
            Description = "Width and height of each cubemap face in pixels.",
            Type = ProcessorParameterType.Integer,
            DefaultValue = 1024,
            MinValue = 16,
            MaxValue = 16384
        },
        .. CubemapParameters.OutputLayoutParameter,
        .. HemiOctParameters.LayoutParameters,
        .. ProjectionParameters.OrientationParameters,
    ];

    public async ValueTask<ProcessingResult> ProcessAsync(ProcessorContext context, CancellationToken cancellationToken)
    {
        var source = context.TranslatorManager.Read(context.InputPath);
        AppLog.Info("HemiOctaToCubemap", $"Processing {source.Width}×{source.Height} ({source.Format})");

        var (preparedSource, lowerHalf, hasLowerHalf, upperOnly) = ProjectionTransforms.ResolveHemiOctaSource(context, source);
        var faceSize = Math.Clamp(context.GetParameterValue("faceSize", 1024), 16, 16384);
        var outputFormat = GpuImageFormats.ResolveWritableOutputFormat(preparedSource.Format);
        var hasSecondInput = lowerHalf is not null;
        var (invMapX, invMapY, invMapZ) = ProjectionConverters.ComputeInverseMapping(
            ProjectionConverters.ResolveMapX(context),
            ProjectionConverters.ResolveMapY(context),
            ProjectionConverters.ResolveMapZ(context));
        var pushConstants = GpuShaderCache.GetLayout("hemiocta_to_cubemap").CreateBuilder()
            .Set("faceSize", faceSize)
            .Set("mapX", invMapX)
            .Set("mapY", invMapY)
            .Set("mapZ", invMapZ)
            .Set("hasLowerHalf", hasLowerHalf ? 1 : 0)
            .Set("upperOnly", upperOnly ? 1 : 0)
            .Set("hasSecondInput", hasSecondInput ? 1 : 0)
            .Build();

        var outputs = Enumerable.Range(0, 6)
            .Select(_ => (faceSize, faceSize, outputFormat))
            .ToArray();

        Image[] inputs = [preparedSource, lowerHalf ?? preparedSource];
        var faces = await context.ComputeEngine.DispatchMultiAsync(
            GpuShaderCache.GetShaderBytecode("hemiocta_to_cubemap"),
            GpuImageFormats.PrepareInputsForSampling(inputs, context.GpuColorSpaceMode),
            pushConstants,
            outputs,
			(1, 1, 1),
			cancellationToken: cancellationToken);

        var outputLayout = context.GetParameterValue("outputLayout", "Cubemap");
        return ProjectionTransforms.AssembleCubemapOutput(faces, outputLayout, outputFormat, faceSize);
    }
}
