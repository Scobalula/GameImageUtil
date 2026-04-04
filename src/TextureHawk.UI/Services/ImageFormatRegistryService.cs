using RedFox.Graphics2D.IO;
using TextureHawk.UI.ViewModels;

namespace TextureHawk.UI.Services;

/// <summary>
/// Provides normalized image format metadata from translator registry data.
/// </summary>
public static class ImageFormatRegistryService
{
    /// <summary>
    /// Gets readable file extension patterns for file pickers (for example "*.png").
    /// </summary>
    public static IReadOnlyList<string> GetReadPatterns(ImageTranslatorManager translatorManager)
    {
        return GetExtensions(translatorManager, requireRead: true, requireWrite: false)
            .Select(ext => $"*{ext}")
            .ToList();
    }

    /// <summary>
    /// Gets readable file extensions (for example ".png").
    /// </summary>
    public static IReadOnlyList<string> GetReadableExtensions(ImageTranslatorManager translatorManager)
    {
        return GetExtensions(translatorManager, requireRead: true, requireWrite: false);
    }

    /// <summary>
    /// Gets writable file extensions (for example ".dds").
    /// </summary>
    public static IReadOnlyList<string> GetWritableExtensions(ImageTranslatorManager translatorManager)
    {
        return GetExtensions(translatorManager, requireRead: false, requireWrite: true);
    }

    /// <summary>
    /// Builds combo box options for writable output formats.
    /// </summary>
    public static IReadOnlyList<FormatOptionViewModel> GetWritableFormatOptions(ImageTranslatorManager translatorManager)
    {
        return GetWritableExtensions(translatorManager)
            .Select(CreateOption)
            .ToList();
    }

    private static IReadOnlyList<string> GetExtensions(ImageTranslatorManager translatorManager, bool requireRead, bool requireWrite)
    {
        return translatorManager.Translators
            .Where(translator => (!requireRead || translator.CanRead) && (!requireWrite || translator.CanWrite))
            .SelectMany(translator => translator.Extensions)
            .Where(extension => !string.IsNullOrWhiteSpace(extension))
            .Select(NormalizeExtension)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(extension => extension, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static string NormalizeExtension(string extension)
    {
        extension = extension.Trim();
        if (!extension.StartsWith(".", StringComparison.Ordinal))
            extension = $".{extension}";

        return extension.ToLowerInvariant();
    }

    private static FormatOptionViewModel CreateOption(string extension)
    {
        var normalized = NormalizeExtension(extension);
        return new FormatOptionViewModel
        {
            Extension = normalized,
            Icon = GetExtensionIcon(normalized),
            DisplayName = normalized.ToUpperInvariant().TrimStart('.')
        };
    }

    private static string GetExtensionIcon(string extension)
    {
        return extension switch
        {
            ".dds" => "🧊",
            ".exr" => "🌈",
            ".png" => "🖼",
            ".bmp" => "📄",
            ".tif" or ".tiff" => "📚",
            _ => "🗂"
        };
    }
}
