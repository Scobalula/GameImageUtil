using System.Diagnostics;

namespace TextureHawk.UI.Services;

/// <summary>
/// Reveals files in the system file manager.
/// </summary>
public static class FileRevealService
{
    /// <summary>
    /// Opens the file manager and tries to highlight the file.
    /// </summary>
    public static void Reveal(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            return;

        var fullPath = Path.GetFullPath(filePath);
        if (!File.Exists(fullPath) && !Directory.Exists(fullPath))
            return;

        if (OperatingSystem.IsWindows())
        {
            StartDetached("explorer.exe", $"/select,\"{fullPath}\"");
            return;
        }

        if (OperatingSystem.IsLinux())
        {
            var fileUri = new Uri(fullPath).AbsoluteUri;
            if (TryStartDetached("dbus-send",
                    "--session --dest=org.freedesktop.FileManager1 --type=method_call /org/freedesktop/FileManager1 org.freedesktop.FileManager1.ShowItems " +
                    $"array:string:\"{fileUri}\" string:\"\""))
                return;

            var directory = Directory.Exists(fullPath) ? fullPath : (Path.GetDirectoryName(fullPath) ?? fullPath);
            StartDetached("xdg-open", $"\"{directory}\"");
            return;
        }

        if (OperatingSystem.IsMacOS())
        {
            StartDetached("open", $"-R \"{fullPath}\"");
            return;
        }

        var fallbackDirectory = Directory.Exists(fullPath) ? fullPath : (Path.GetDirectoryName(fullPath) ?? fullPath);
        StartDetached("xdg-open", $"\"{fallbackDirectory}\"");
    }

    private static bool TryStartDetached(string fileName, string arguments)
    {
        try
        {
            StartDetached(fileName, arguments);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static void StartDetached(string fileName, string arguments)
    {
        using var process = Process.Start(new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            UseShellExecute = false,
            CreateNoWindow = true
        });

        if (process is null)
            throw new InvalidOperationException($"Failed to start process {fileName}.");
    }
}
