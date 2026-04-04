using Avalonia;

namespace TextureHawk.UI;

/// <summary>
/// Entry point for the TextureHawk desktop application.
/// </summary>
public static class Program
{
    /// <summary>
    /// Application entry point.
    /// </summary>
    [STAThread]
    public static void Main(string[] args) => BuildAvaloniaApp()
        .StartWithClassicDesktopLifetime(args);

    /// <summary>
    /// Configures and builds the Avalonia application.
    /// </summary>
    public static AppBuilder BuildAvaloniaApp() => AppBuilder
        .Configure<App>()
        .UsePlatformDetect()
        .WithInterFont()
        .LogToTrace();
}
