using RedFox.Graphics2D;
using TextureHawk.Gpu;
using TextureHawk.Logging;
using TextureHawk.Processing;

namespace TextureHawk.BuiltInProcessors;

/// <summary>
/// Splits packed channel textures into named output maps used by several legacy game scripts.
/// </summary>
public sealed class PackedChannelSplitProcessor : IProcessor
{
    /// <inheritdoc />
    public string Id => "builtin.packed-channel-split";

    /// <inheritdoc />
    public string Name => "Packed Channel Split";

    /// <inheritdoc />
    public string Description => "Split RGBA, color/alpha, or CoD packed S/G/O textures into multiple outputs on the GPU.";

    /// <inheritdoc />
    public string Category => "Game Textures";

    /// <inheritdoc />
    public string Icon => "📦";

    /// <inheritdoc />
    public IReadOnlyList<ProcessorParameter> Parameters { get; } =
    [
        new ProcessorParameter
        {
            Id = "preset",
            Name = "Preset",
            Description = "Choose the packed texture layout to split.",
            Type = ProcessorParameterType.Choice,
            DefaultValue = "Split RGBA",
            Options = ["Split RGBA", "Split Color/Alpha", "CoD Spec/Gloss", "CoD S/G/O Blue", "CoD S/G/O Green"]
        }
    ];

    /// <inheritdoc />

    /// <inheritdoc />

    /// <inheritdoc />
    public async ValueTask<ProcessingResult> ProcessAsync(ProcessorContext context, CancellationToken cancellationToken)
    {
        var source = context.TranslatorManager.Read(context.InputPath);
        var preset = context.GetParameterValue("preset", "Split RGBA");
        var outputFormat = GpuImageFormats.ResolveWritableOutputFormat(source.Format);
        var (shaderName, suffixes) = preset switch
        {
            "Split Color/Alpha" => ("split_2", new[] { "_rgb", "_alpha" }),
            "CoD Spec/Gloss" => ("split_2", new[] { "_s", "_g" }),
            "CoD S/G/O Blue" => ("split_3", new[] { "_s", "_g", "_o" }),
            "CoD S/G/O Green" => ("split_3", new[] { "_s", "_o", "_g" }),
            _ => ("split_4", new[] { "_r", "_g", "_b", "_a" })
        };

        AppLog.Info("PackedSplit", $"Processing {source.Width}x{source.Height} with preset '{preset}'");

        var pushConstants = GpuShaderCache.GetLayout(shaderName).CreateBuilder()
            .Set("outWidth", source.Width)
            .Set("outHeight", source.Height)
            .Set("srcWidth", source.Width)
            .Set("srcHeight", source.Height)
            .Build();

        var outputs = suffixes.Select(_ => (source.Width, source.Height, outputFormat)).ToArray();
        var images = await context.ComputeEngine.DispatchMultiAsync(GpuShaderCache.GetShaderBytecode(shaderName), GpuImageFormats.PrepareInputsForSampling([source], context.GpuColorSpaceMode), pushConstants, outputs, (1, 1, 1), cancellationToken: cancellationToken);
        return ProcessingResult.Ok(images, suffixes);
    }
}
