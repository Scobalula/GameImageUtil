using RedFox.Graphics2D;
using TextureHawk.Gpu;
using TextureHawk.Logging;
using TextureHawk.Processing;

namespace TextureHawk.BuiltInProcessors.EnvironmentTextures;

public sealed class EquirectToCubemapProcessor : IProcessor
{
    /// <inheritdoc />
    public string Id => "builtin.equirect-to-cubemap";

    /// <inheritdoc />
    public string Name => "Equirectangular to Cubemap";

    /// <inheritdoc />
    public string Description => "Convert an equirectangular environment map into a cubemap.";

    /// <inheritdoc />
    public string Category => "Environment Textures";

    /// <inheritdoc />
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
        .. ProjectionParameters.OrientationParameters,
    ];

    public async ValueTask<ProcessingResult> ProcessAsync(ProcessorContext context, CancellationToken cancellationToken)
    {
        var source = context.TranslatorManager.Read(context.InputPath);
        AppLog.Info("EquirectToCubemap", $"Processing {source.Width}×{source.Height} ({source.Format})");

        var faceSize = Math.Clamp(context.GetParameterValue("faceSize", Math.Max(16, source.Width / 4)), 16, 16384);
        var outputFormat = GpuImageFormats.ResolveWritableOutputFormat(source.Format);
        var (invMapX, invMapY, invMapZ) = ProjectionConverters.ComputeInverseMapping(
            ProjectionConverters.ResolveMapX(context),
            ProjectionConverters.ResolveMapY(context),
            ProjectionConverters.ResolveMapZ(context));
        var pushConstants = GpuShaderCache.GetLayout("equirect_to_cubemap").CreateBuilder()
            .Set("faceSize", faceSize)
            .Set("upAxis", ProjectionConverters.ResolveUpAxis(context))
            .Set("mapX", invMapX)
            .Set("mapY", invMapY)
            .Set("mapZ", invMapZ)
            .Build();

        var outputs = Enumerable.Range(0, 6)
            .Select(_ => (faceSize, faceSize, outputFormat))
            .ToArray();

        var faces = await context.ComputeEngine.DispatchMultiAsync(
            GpuShaderCache.GetShaderBytecode("equirect_to_cubemap"),
            GpuImageFormats.PrepareInputsForSampling([source], context.GpuColorSpaceMode),
            pushConstants,
            outputs,
            (16, 16, 1),
			cancellationToken: cancellationToken);

        var outputLayout = context.GetParameterValue("outputLayout", "Cubemap");
        return ProjectionTransforms.AssembleCubemapOutput(faces, outputLayout, outputFormat, faceSize);
    }
}
