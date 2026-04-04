using TextureHawk.Processing;

namespace TextureHawk.BuiltInProcessors;

/// <summary>
/// Recreates legacy GameImageUtil output name sanitization while preserving the user's configured output suffix and extension.
/// </summary>
internal static class LegacyOutputNaming
{
    internal static string BuildOutputPath(ProcessorContext context, string sanitizedBaseName)
    {
        var configuredDirectory = Path.GetDirectoryName(context.OutputPath) ?? ".";
        var configuredExtension = Path.GetExtension(context.OutputPath);
        var configuredBaseName = Path.GetFileNameWithoutExtension(context.OutputPath);
        var inputBaseName = Path.GetFileNameWithoutExtension(context.InputPath);
        var configuredTail = configuredBaseName.StartsWith(inputBaseName, StringComparison.OrdinalIgnoreCase)
            ? configuredBaseName[inputBaseName.Length..]
            : string.Empty;

        return Path.Combine(configuredDirectory, sanitizedBaseName + configuredTail + configuredExtension);
    }

    internal static string ReplaceLegacyDelimiters(string baseName)
    {
        return string.Join("_", baseName.Split(["~"], StringSplitOptions.None))
            .Replace('~', '_')
            .Replace('&', '_')
            .Replace('$', '_')
            .Replace('-', '_');
    }

    internal static string StripAtTokens(string baseName, params string[] tokens)
    {
        return baseName.Split(tokens, StringSplitOptions.None)[0];
    }

    internal static string ReplaceLegacyWw2Delimiters(string baseName)
    {
        return string.Join("_", baseName.Replace("~&-", string.Empty).Split(["~"], StringSplitOptions.None))
            .Replace('~', '_')
            .Replace('&', '_')
            .Replace('$', '_')
            .Replace('-', '_');
    }
}