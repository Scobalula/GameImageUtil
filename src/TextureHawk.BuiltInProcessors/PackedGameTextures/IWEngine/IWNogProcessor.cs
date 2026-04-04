using TextureHawk.Gpu;
using TextureHawk.Logging;
using TextureHawk.Processing;

namespace TextureHawk.BuiltInProcessors.PackedGameTextures.CallOfDuty;

/// <summary>
/// Unpacks Call of Duty packed NOG textures into gloss, occlusion, and reconstructed normal maps.
/// </summary>
public sealed class IWNogProcessor : IAutoDetectProcessor
{
    /// <inheritdoc />
    public string Id => nameof(IWNogProcessor);

    /// <inheritdoc />
    public string Name => "IW NOG Unpack";

    /// <inheritdoc />
    public string Description => "Splits NormalOct/Gloss/Occulsion textures into separate outputs.";

    /// <inheritdoc />
    public string Category => "IW Engine";

    /// <inheritdoc />
    public string Icon => "🎮";

    /// <inheritdoc />
    public IReadOnlyList<ProcessorParameter> Parameters { get; } =
    [
        new ProcessorParameter
        {
            Id = "sanitizeOutputName",
            Name = "Sanitize Output Name",
            Description = "Use the legacy script's output name cleanup before writing _g, _o, and _n outputs.",
            Type = ProcessorParameterType.Boolean,
            DefaultValue = true
        }
    ];

    /// <inheritdoc />
    public bool CanConvert(string inputPath)
    {
        var name = Path.GetFileNameWithoutExtension(inputPath);
        return name.Contains("_nog", StringComparison.OrdinalIgnoreCase)
            || name.Contains("@nog", StringComparison.OrdinalIgnoreCase)
            || (name.Contains("_n&", StringComparison.OrdinalIgnoreCase) && name.Contains('~'));
    }

    /// <inheritdoc />
    public async ValueTask<ProcessingResult> ProcessAsync(ProcessorContext context, CancellationToken cancellationToken)
    {
        var source = context.TranslatorManager.Read(context.InputPath);
        var sanitizeOutputName = context.GetParameterValue("sanitizeOutputName", true);
        AppLog.Info("CoDNog", $"Processing {source.Width}x{source.Height}");

        var outputFormat = GpuImageFormats.ResolveWritableOutputFormat(source.Format);
        var pushConstants = GpuShaderCache.GetLayout("cod_nog_unpack").CreateBuilder()
            .Set("outWidth", source.Width)
            .Set("outHeight", source.Height)
            .Set("srcWidth", source.Width)
            .Set("srcHeight", source.Height)
            .Build();

        var outputs = new[]
        {
            (source.Width, source.Height, outputFormat),
            (source.Width, source.Height, outputFormat),
            (source.Width, source.Height, outputFormat)
        };

        var images = await context.ComputeEngine.DispatchMultiAsync(GpuShaderCache.GetShaderBytecode("cod_nog_unpack"), GpuImageFormats.PrepareInputsForSampling([source], context.GpuColorSpaceMode), pushConstants, outputs, (16, 16, 1), cancellationToken);
        var outputPathOverride = sanitizeOutputName
            ? LegacyOutputNaming.BuildOutputPath(context, LegacyOutputNaming.StripAtTokens(Path.GetFileNameWithoutExtension(context.InputPath), "_n&", "_n_"))
            : null;
        return outputPathOverride is null
            ? ProcessingResult.Ok(images, ["_g", "_o", "_n"])
            : ProcessingResult.Ok(images, ["_g", "_o", "_n"], outputPathOverride);
    }
}
