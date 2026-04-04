using TextureHawk.Logging;
using TextureHawk.Processing;

namespace TextureHawk.BuiltInProcessors.Basics;

/// <summary>
/// Provides a processor that converts the file or container format of an input file using the selected output
/// extension.
/// </summary>
public sealed class FileConvertProcessor : IProcessor
{
    /// <inheritdoc />
    public string Id => nameof(FileConvertProcessor);

    /// <inheritdoc />
    public string Name => "File Convert";

    /// <inheritdoc />
    public string Description => "Convert only the file/container format (for example PNG to DDS) using the selected output extension.";

    /// <inheritdoc />
    public string Category => "Basics";

    /// <inheritdoc />
    public string Icon => "🧾";

    /// <inheritdoc />
    public IReadOnlyList<ProcessorParameter> Parameters => [];

    /// <inheritdoc />
    public ValueTask<ProcessingResult> ProcessAsync(ProcessorContext context, CancellationToken cancellationToken)
    {
        AppLog.Debug("BaselineConvert", "Passing image through unchanged for file-format conversion");
        return ValueTask.FromResult(ProcessingResult.Ok(context.TranslatorManager.Read(context.InputPath)));
    }
}
