using RedFox.Graphics2D;
using RedFox.Graphics2D.IO;

namespace TextureHawk.BuiltInProcessors;

/// <summary>
/// Finds companion files by suffix using translator-registered file extensions.
/// </summary>
internal static class TranslatorFileSearch
{
    public static Image? TryLoadSiblingImage(ImageTranslatorManager translatorManager, string sourcePath, string siblingSuffix)
    {
        var directory = Path.GetDirectoryName(sourcePath) ?? ".";
        var baseName = Path.GetFileNameWithoutExtension(sourcePath);
        var currentExtension = NormalizeExtension(Path.GetExtension(sourcePath));

        var extensions = translatorManager.Translators
            .Where(translator => translator.CanRead)
            .SelectMany(translator => translator.Extensions)
            .Select(NormalizeExtension)
            .Where(static extension => extension.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(static extension => extension, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (currentExtension.Length > 0)
            extensions.RemoveAll(extension => string.Equals(extension, currentExtension, StringComparison.OrdinalIgnoreCase));

        if (currentExtension.Length > 0)
            extensions.Insert(0, currentExtension);

        foreach (var extension in extensions)
        {
            var candidatePath = Path.Combine(directory, baseName + siblingSuffix + extension);
            if (!File.Exists(candidatePath))
                continue;

            try
            {
                return translatorManager.Read(candidatePath);
            }
            catch
            {
                // Skip unsupported/invalid files and continue searching.
            }
        }

        return null;
    }

    private static string NormalizeExtension(string? extension)
    {
        if (string.IsNullOrWhiteSpace(extension))
            return string.Empty;

        var trimmed = extension.Trim();
        if (!trimmed.StartsWith(".", StringComparison.Ordinal))
            trimmed = $".{trimmed}";

        return trimmed.ToLowerInvariant();
    }
}
