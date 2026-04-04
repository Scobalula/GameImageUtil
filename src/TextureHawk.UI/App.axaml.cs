using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;
using RedFox.Graphics2D.IO;
using TextureHawk.BuiltInProcessors;
using TextureHawk.Gpu;
using TextureHawk.Gpu.Vulkan;
using TextureHawk.Logging;
using TextureHawk.Plugins;
using TextureHawk.Processing;
using TextureHawk.Settings;
using TextureHawk.UI.ViewModels;
using TextureHawk.UI.Views;

namespace TextureHawk.UI;

/// <summary>
/// Application entry point and global service host.
/// </summary>
/// <remarks>
/// Initializes the Vulkan compute engine, processor registry, plugin system,
/// and settings manager. A compatible GPU is required — the application will
/// not launch without one.
/// </remarks>
public class App : Application
{
    private static bool s_servicesInitialized;

    /// <summary>
    /// Global settings manager instance.
    /// </summary>
    public static SettingsManager SettingsManager { get; } = new();

    /// <summary>
    /// Registry of all available processors.
    /// </summary>
    public static ProcessorRegistry ProcessorRegistry { get; } = new();

    /// <summary>
    /// Manager for image format translators (PNG, DDS, BMP, TIFF, EXR).
    /// </summary>
    public static ImageTranslatorManager TranslatorManager { get; } = new();

    /// <summary>
    /// Processing pipeline for executing jobs.
    /// </summary>
    public static ProcessingPipeline Pipeline { get; private set; } = null!;

    /// <summary>
    /// Plugin loader for runtime plugin management.
    /// </summary>
    public static PluginLoader PluginLoader { get; private set; } = null!;

    /// <summary>
    /// The Vulkan compute engine. Guaranteed non-null after successful initialization.
    /// </summary>
    public static VulkanComputeEngine ComputeEngine { get; private set; } = null!;

    /// <inheritdoc />
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);

        // Initialize logging
        var logDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "TextureHawk", "logs");
        AppLog.Initialize(logDir);
    }

    /// <inheritdoc />
    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.Exit += (_, _) =>
            {
                try
                {
                    SettingsManager.Save();
                }
                catch (Exception ex)
                {
                    AppLog.Warn("App", $"Failed to persist settings during exit: {ex.Message}");
                }
            };

            desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;

            var splash = new SplashWindow();
            desktop.MainWindow = splash;
            splash.Show();

            _ = InitializeAndLaunchMainWindowAsync(desktop, splash);
        }

        base.OnFrameworkInitializationCompleted();
    }

    private static async Task InitializeAndLaunchMainWindowAsync(IClassicDesktopStyleApplicationLifetime desktop, SplashWindow splash)
    {
        try
        {
            // Give the splash a paint cycle before expensive startup work begins.
            await Dispatcher.UIThread.InvokeAsync(static () => { }, DispatcherPriority.Render);
            await Task.Delay(30).ConfigureAwait(false);

            await Task.Run(InitializeCoreServices);

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                ApplyTheme(SettingsManager.Current.Theme);

                var mainWindow = new MainWindow
                {
                    DataContext = new MainWindowViewModel()
                };

                desktop.MainWindow = mainWindow;
                desktop.ShutdownMode = ShutdownMode.OnMainWindowClose;
                mainWindow.Show();
                splash.Close();
            });
        }
        catch (Exception ex)
        {
            AppLog.Error("App", "Startup initialization failed", ex);
            Console.Error.WriteLine($"[FATAL] Startup initialization failed: {ex.Message}");
            desktop.Shutdown(1);
        }
    }

    private static void InitializeCoreServices()
    {
        if (s_servicesInitialized)
            return;

        SettingsManager.Load();
        AppLog.Info("App", "Settings loaded");

        BuiltInRegistrar.RegisterAll(ProcessorRegistry, TranslatorManager);
        AppLog.Info("App", $"Registered {ProcessorRegistry.GetAll().Count} processors");

        InitializeComputeEngine();
        AppLog.Info("App", "Vulkan compute engine initialized");

        Pipeline = new ProcessingPipeline(
            ProcessorRegistry,
            TranslatorManager,
            ComputeEngine,
            () => SettingsManager.Current.OverwriteExisting,
            () => GpuColorSpaceModeNames.ParseOrDefault(SettingsManager.Current.DefaultGpuColorSpaceMode, GpuColorSpaceMode.Auto));
        PluginLoader = new PluginLoader(ProcessorRegistry);

        PluginLoader.LoadAllFromDirectory(SettingsManager.GetPluginsDirectory());
        foreach (var dir in SettingsManager.Current.PluginDirectories)
            PluginLoader.LoadAllFromDirectory(dir);

        foreach (var processor in DefinedProcessor.LoadFromDirectory(SettingsManager.GetDefinitionsDirectory()))
            ProcessorRegistry.Register(processor);
        foreach (var dir in SettingsManager.Current.ProcessorDefinitionDirectories)
            foreach (var processor in DefinedProcessor.LoadFromDirectory(dir))
                ProcessorRegistry.Register(processor);

        s_servicesInitialized = true;
    }

    /// <summary>
    /// Switches the active GPU device at runtime.
    /// </summary>
    /// <param name="device">The device to switch to.</param>
    /// <returns>True if the device was successfully selected.</returns>
    public static bool SwitchGpuDevice(GpuDevice device)
    {
        if (!ComputeEngine.SelectDevice(device))
            return false;

        SettingsManager.Current.PreferredGpuDeviceName = device.Name;
        SettingsManager.Current.PreferredGpuDeviceId = device.DeviceId;
        SettingsManager.Current.PreferredGpuVendorId = device.VendorId;
        SettingsManager.Save();
        AppLog.Info("App", $"Switched GPU to {device.Name} ({device.VendorId:X4}:{device.DeviceId:X4})");
        return true;
    }

    /// <summary>
    /// Applies the specified theme variant at runtime.
    /// </summary>
    /// <param name="theme">Theme name: "dark" or "light".</param>
    public static void ApplyTheme(string theme)
    {
        if (Current is not App app)
            return;

        app.RequestedThemeVariant = theme?.ToLowerInvariant() switch
        {
            "light" => ThemeVariant.Light,
            _ => ThemeVariant.Dark
        };
    }

    private static void InitializeComputeEngine()
    {
        try
        {
            var vulkan = new VulkanComputeEngine();
            if (!vulkan.IsAvailable || vulkan.EnumerateDevices().Count == 0)
            {
                vulkan.Dispose();
                ShowGpuError("No Vulkan-compatible GPU detected. TextureHawk requires a GPU with Vulkan support.");
                Environment.Exit(1);
                return;
            }

            var preferred = ResolvePreferredGpu(vulkan.EnumerateDevices(), SettingsManager.Current);
            if (preferred is { } device)
                vulkan.SelectDevice(device);

            ComputeEngine = vulkan;
            if (ComputeEngine.SelectedDevice is { } selected)
                AppLog.Info("App", $"Using GPU {selected.Name} ({selected.VendorId:X4}:{selected.DeviceId:X4})");
        }
        catch (Exception ex)
        {
            ShowGpuError($"Failed to initialize Vulkan: {ex.Message}\n\nTextureHawk requires a GPU with Vulkan support.");
            Environment.Exit(1);
        }
    }

    private static void ShowGpuError(string message)
    {
        Console.Error.WriteLine($"[FATAL] {message}");
    }

    private static GpuDevice? ResolvePreferredGpu(IReadOnlyList<GpuDevice> devices, AppSettings settings)
    {
        if (settings.PreferredGpuVendorId is { } vendorId && settings.PreferredGpuDeviceId is { } deviceId)
        {
            var exact = devices.FirstOrDefault(d => d.VendorId == vendorId && d.DeviceId == deviceId);
            if (exact != default)
                return exact;
        }

        if (settings.PreferredGpuDeviceName is { } preferredName)
        {
            var named = devices.FirstOrDefault(d => d.Name == preferredName);
            if (named != default)
                return named;
        }

        return null;
    }
}
