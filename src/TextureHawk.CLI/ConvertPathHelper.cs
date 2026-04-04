namespace TextureHawk.CLI;

internal static class ConvertPathHelper
{
    public static string BuildDefaultOutputPath(string inputPath)
    {
        var directory = Path.GetDirectoryName(inputPath) ?? ".";
        var fileName = Path.GetFileNameWithoutExtension(inputPath);
        var extension = Path.GetExtension(inputPath);
        return Path.Combine(directory, $"{fileName}_converted{extension}");
    }
}