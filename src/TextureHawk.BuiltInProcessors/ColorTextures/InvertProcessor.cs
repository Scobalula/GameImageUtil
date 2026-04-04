using RedFox.Graphics2D;
using TextureHawk.Gpu;
using TextureHawk.Logging;
using TextureHawk.Processing;

namespace TextureHawk.BuiltInProcessors.ColorTextures;

/// <summary>
/// Inverts the color channels of an image on the GPU.
/// </summary>
/// <remarks>
/// Push constant layout: outWidth, outHeight, invertAlpha (2×int32 + 1×int32 = 12 bytes).
/// </remarks>
public sealed class InvertProcessor : IProcessor
{
    public string Id => nameof(InvertProcessor);

    public string Name => "Invert Colors";

    public string Description => "Invert the color channels of an image. Optionally inverts alpha.";

    public string Category => "Color Textures";

    public string Icon => "🔄";

    public IReadOnlyList<ProcessorParameter> Parameters { get; } =
    [
        new ProcessorParameter
        {
            Id = "invertAlpha",
            Name = "Invert Alpha",
            Description = "Also invert the alpha channel.",
            Type = ProcessorParameterType.Boolean,
            DefaultValue = false
        }
    ];

    public async ValueTask<ProcessingResult> ProcessAsync(ProcessorContext context, CancellationToken cancellationToken)
    {
        var invertAlpha = context.GetParameterValue("invertAlpha", false);
        var source = context.TranslatorManager.Read(context.InputPath);
        AppLog.Info("Invert", $"Processing {source.Width}×{source.Height} ({source.Format}), invertAlpha={invertAlpha}");

        var shader = GpuShaderCache.GetShaderBytecode("invert");
        var pushConstants = GpuShaderCache.GetLayout("invert").CreateBuilder()
            .Set("outWidth", source.Width)
            .Set("outHeight", source.Height)
            .Set("invertAlpha", invertAlpha ? 1 : 0)
            .Build();

        var result = await context.ComputeEngine.DispatchAsync(shader, GpuImageFormats.PrepareInputsForSampling([source], context.GpuColorSpaceMode), pushConstants, source.Width, source.Height, GpuImageFormats.ResolveWritableOutputFormat(source.Format), (1, 1, 1), cancellationToken);
        AppLog.Info("Invert", "Processing complete");
        return ProcessingResult.Ok(result);
    }
}
