using RedFox.Graphics2D;
using TextureHawk.Gpu;
using TextureHawk.Logging;
using TextureHawk.Processing;

namespace TextureHawk.BuiltInProcessors.ColorTextures;

/// <summary>
/// Processes albedo and specular texture images to generate fused albedo/specular and metalness.
/// </summary>
public sealed class SpecularToMetalnessProcessor : IProcessor
{
    private const string FusedSuffix = "_f";
    private const string MetalnessSuffix = "_m";

    /// <inheritdoc />
    public string Id => nameof(SpecularToMetalnessProcessor);

    /// <inheritdoc />
    public string Name => "Specular -> Metalness";

    /// <inheritdoc />
    public string Description => "Convert albedo + specular to fused albedo/specular and metalness outputs.";

    /// <inheritdoc />
    public string Category => "Color Textures";

    /// <inheritdoc />
    public string Icon => "🎚";

    /// <inheritdoc />
    public IReadOnlyList<ProcessorParameter> Parameters { get; } =
    [
        new ProcessorParameter
        {
            Id = "specPath",
            Name = "Specular Path",
            Description = "Path to the specular image.",
            Type = ProcessorParameterType.FilePath,
            DefaultValue = ""
        },
        new ProcessorParameter
        {
            Id = "insulatorSpecRange",
            Name = "Insulator Spec Range",
            Description = "Used to define insulator/values that remain albedo. Some games may require this set to higher value like 0.21.",
            Type = ProcessorParameterType.Float,
            DefaultValue = 0.1,
            MinValue = 0.0,
            MaxValue = 1.0
        },
        new ProcessorParameter
        {
            Id = "packMetalnessInAlpha",
            Name = "Pack Metalness Into Alpha",
            Description = "When enabled, outputs a single packed albedo+metalness texture instead of separate fused and metalness files.",
            Type = ProcessorParameterType.Boolean,
            DefaultValue = false
        }
    ];

    /// <inheritdoc />
    public async ValueTask<ProcessingResult> ProcessAsync(ProcessorContext context, CancellationToken cancellationToken)
    {
        var albedo = context.TranslatorManager.Read(context.InputPath);
        var specular = ResolveSpecular(context);
        if (specular is null)
            return ProcessingResult.Fail("Specular image is required. Set Specular Path or enable Auto Find Specular.");

        var insulatorSpecRange = Math.Clamp(context.GetParameterValue("insulatorSpecRange", 0.1f), 0.0f, 1.0f);
        var fp16MinValue = Math.Clamp((float)context.GetParameterValue("fp16MinValue", 0.00006103515625), 0.0f, 0.01f);
        var packMetalnessInAlpha = context.GetParameterValue("packMetalnessInAlpha", false);

        AppLog.Info("CoDAlbedoSpecToMetalness", $"Processing {albedo.Width}x{albedo.Height} with spec {specular.Width}x{specular.Height}");

        var outputFormat = GpuImageFormats.ResolveWritableOutputFormat(albedo.Format);
        var pushConstants = GpuShaderCache.GetLayout("cod_albedo_spec_to_metalness").CreateBuilder()
            .Set("outWidth", albedo.Width)
            .Set("outHeight", albedo.Height)
            .Set("albedoWidth", albedo.Width)
            .Set("albedoHeight", albedo.Height)
            .Set("specWidth", specular.Width)
            .Set("specHeight", specular.Height)
            .Set("insulatorSpecRange", insulatorSpecRange)
            .Set("fp16MinValue", (float)Half.Epsilon)
            .Set("packMetalnessInAlpha", packMetalnessInAlpha ? 1 : 0)
            .Build();

        var outputs = new[]
        {
            (albedo.Width, albedo.Height, outputFormat),
            (albedo.Width, albedo.Height, outputFormat)
        };

        var images = await context.ComputeEngine.DispatchMultiAsync(GpuShaderCache.GetShaderBytecode("cod_albedo_spec_to_metalness"), GpuImageFormats.PrepareInputsForSampling([albedo, specular], context.GpuColorSpaceMode), pushConstants, outputs, (1, 1, 1), cancellationToken);
        return packMetalnessInAlpha ? ProcessingResult.Ok(images[1]) : ProcessingResult.Ok(images, [FusedSuffix, MetalnessSuffix]);
    }

    private static Image? ResolveSpecular(ProcessorContext context)
    {
        var configuredSpecPath = context.GetParameterValue("specPath", string.Empty);
        if (!string.IsNullOrWhiteSpace(configuredSpecPath) && File.Exists(configuredSpecPath))
            return context.TranslatorManager.Read(configuredSpecPath);

        if (!context.GetParameterValue("autoFindSpec", true))
            return null;

        var suffix = context.GetParameterValue("specSuffix", "_s");
        return TranslatorFileSearch.TryLoadSiblingImage(context.TranslatorManager, context.InputPath, suffix);
    }

}
