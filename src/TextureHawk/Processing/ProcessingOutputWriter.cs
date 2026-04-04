using RedFox.Graphics2D.IO;

namespace TextureHawk.Processing;

/// <summary>
/// Writes processor results using the translator registry and pipeline overwrite rules.
/// </summary>
internal static class ProcessingOutputWriter
{
    public static ProcessingResult Write(ImageTranslatorManager translatorManager, string outputPath, ProcessingResult result, bool overwriteExisting, IReadOnlyDictionary<string, object> parameterValues)
    {
        var baseOutputPath = result.OutputPathOverride ?? outputPath;
        var outputDirectory = Path.GetDirectoryName(baseOutputPath);
        if (!string.IsNullOrEmpty(outputDirectory))
            Directory.CreateDirectory(outputDirectory);

        if (result.Images is { Count: > 1 } images)
        {
            for (var i = 0; i < images.Count; i++)
            {
                var suffix = result.OutputSuffixes is not null && i < result.OutputSuffixes.Count
                    ? result.OutputSuffixes[i]
                    : $"_{i}";
                var indexedPath = InsertSuffix(baseOutputPath, suffix);
                if (!CanWriteOutput(indexedPath, overwriteExisting, out var writeError))
                    return ProcessingResult.Fail(writeError ?? "Output write blocked.");

                translatorManager.Write(indexedPath, OutputWriteParameterCatalog.PrepareImageForWrite(indexedPath, images[i], parameterValues));
            }

            return result;
        }

        if (result.Image is not null)
        {
            if (!CanWriteOutput(baseOutputPath, overwriteExisting, out var writeError))
                return ProcessingResult.Fail(writeError ?? "Output write blocked.");

            translatorManager.Write(baseOutputPath, OutputWriteParameterCatalog.PrepareImageForWrite(baseOutputPath, result.Image, parameterValues));
        }

        return result;
    }

    private static string InsertSuffix(string path, string suffix)
    {
        var directory = Path.GetDirectoryName(path) ?? ".";
        var name = Path.GetFileNameWithoutExtension(path);
        var extension = Path.GetExtension(path);
        return Path.Combine(directory, $"{name}{suffix}{extension}");
    }

    private static bool CanWriteOutput(string outputPath, bool overwriteExisting, out string? error)
    {
        if (!File.Exists(outputPath) || overwriteExisting)
        {
            error = null;
            return true;
        }

        error = $"Output file already exists: {outputPath}";
        return false;
    }
}
