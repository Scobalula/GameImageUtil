using RedFox.Graphics2D;
using TextureHawk.Gpu;
using TextureHawk.Logging;
using TextureHawk.Processing;

namespace TextureHawk.BuiltInProcessors.ColorTextures;

/// <summary>
/// Unpacks Call of Duty packed albedo/specular textures from fused or split mask layouts.
/// </summary>
public sealed class MetalnessToSpecularProcessor : IAutoDetectProcessor
{
    /// <inheritdoc />
    public string Id => nameof(MetalnessToSpecularProcessor);

    /// <inheritdoc />
    public string Name => "Metalness -> Specular";

    /// <inheritdoc />
    public string Description => "Convert fused albedo/specular and metalness to albedo + specular outputs.";

    /// <inheritdoc />
    public string Category => "Color Textures";

    /// <inheritdoc />
    public string Icon => "🎚";

    /// <inheritdoc />
    public IReadOnlyList<ProcessorParameter> Parameters { get; } =
    [
        new ProcessorParameter
        {
            Id = "maskPath",
            Name = "Mask Path",
            Description = "Path to the mask image. Leave empty to assume the alpha of the source image.",
            Type = ProcessorParameterType.FilePath,
            DefaultValue = ""
        }
    ];

    /// <inheritdoc />
    public bool CanConvert(string inputPath)
    {
        var name = Path.GetFileNameWithoutExtension(inputPath);
        return name.EndsWith("_cs", StringComparison.OrdinalIgnoreCase)
            || name.Contains("@cs", StringComparison.OrdinalIgnoreCase);
    }

    /// <inheritdoc />
    public async ValueTask<ProcessingResult> ProcessAsync(ProcessorContext context, CancellationToken cancellationToken)
    {
        var source = context.TranslatorManager.Read(context.InputPath);
        var useSeparateMask = false;
        var mask = source;
        var maskPath = context.GetParameterValue("maskPath", "");

        if (!string.IsNullOrWhiteSpace(maskPath))
        {
            if (!File.Exists(maskPath))
                return ProcessingResult.Fail($"Mask image not found: {maskPath}");

            mask = context.TranslatorManager.Read(maskPath);
            useSeparateMask = true;
        }

        AppLog.Info("CoDCS", $"Processing {source.Width}x{source.Height} in mode");

        var outputFormat = GpuImageFormats.ResolveWritableOutputFormat(source.Format);
        var pushConstants = GpuShaderCache.GetLayout("cod_cs_unpack").CreateBuilder()
            .Set("outWidth", source.Width)
            .Set("outHeight", source.Height)
            .Set("srcWidth", source.Width)
            .Set("srcHeight", source.Height)
            .Set("maskWidth", mask.Width)
            .Set("maskHeight", mask.Height)
            .Set("useSeparateMask", useSeparateMask ? 1 : 0)
            .Build();

        var outputs = new[]
        {
            (source.Width, source.Height, outputFormat),
            (source.Width, source.Height, outputFormat)
        };

        var images = await context.ComputeEngine.DispatchMultiAsync(GpuShaderCache.GetShaderBytecode("cod_cs_unpack"), GpuImageFormats.PrepareInputsForSampling([source, mask], context.GpuColorSpaceMode), pushConstants, outputs, (16, 16, 1), cancellationToken);

        return ProcessingResult.Ok(images, ["_c", "_s"]);
    }

}
