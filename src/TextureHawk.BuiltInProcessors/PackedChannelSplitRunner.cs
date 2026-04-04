using RedFox.Graphics2D;
using TextureHawk.Gpu;
using TextureHawk.Logging;
using TextureHawk.Processing;

namespace TextureHawk.BuiltInProcessors;

/// <summary>
/// Shared GPU execution for named packed-channel split processors.
/// </summary>
internal static class PackedChannelSplitRunner
{
    internal static async ValueTask<ProcessingResult> RunAsync(ProcessorContext context, string processorTag, string shaderName, IReadOnlyList<string> suffixes, CancellationToken cancellationToken, string? outputPathOverride = null)
    {
        var source = context.TranslatorManager.Read(context.InputPath);
        var outputFormat = GpuImageFormats.ResolveWritableOutputFormat(source.Format);
        AppLog.Info(processorTag, $"Processing {source.Width}x{source.Height}");

        var pushConstants = GpuShaderCache.GetLayout(shaderName).CreateBuilder()
            .Set("outWidth", source.Width)
            .Set("outHeight", source.Height)
            .Set("srcWidth", source.Width)
            .Set("srcHeight", source.Height)
            .Build();

        var outputs = suffixes.Select(_ => (source.Width, source.Height, outputFormat)).ToArray();
        var images = await context.ComputeEngine.DispatchMultiAsync(GpuShaderCache.GetShaderBytecode(shaderName), GpuImageFormats.PrepareInputsForSampling([source], context.GpuColorSpaceMode), pushConstants, outputs, (1, 1, 1), cancellationToken: cancellationToken);
        return outputPathOverride is null ? ProcessingResult.Ok(images, suffixes) : ProcessingResult.Ok(images, suffixes, outputPathOverride);
    }
}