using CommunityToolkit.Mvvm.ComponentModel;
using TextureHawk.Gpu;
using TextureHawk.UI.Services;

namespace TextureHawk.UI.ViewModels;

/// <summary>
/// ViewModel for the global settings window.
/// </summary>
/// <remarks>
/// Manages user preferences including output settings, update behavior,
/// GPU device selection, and appearance. Writes changes to the settings
/// file and applies GPU device changes at runtime via <see cref="App.SwitchGpuDevice"/>.
/// </remarks>
public sealed partial class SettingsViewModel : ObservableObject
{
    /// <summary>
    /// Default output directory for processed files.
    /// </summary>
    [ObservableProperty]
    public partial string OutputDirectory { get; set; }

    /// <summary>
    /// Whether new images should use a stored processor-specific output directory when available.
    /// </summary>
    [ObservableProperty]
    public partial bool UseProcessorDefaultOutputDirectory { get; set; }

    /// <summary>
    /// Whether to overwrite existing output files without prompting.
    /// </summary>
    [ObservableProperty]
    public partial bool OverwriteExisting { get; set; }

    /// <summary>
    /// Default output file format extension.
    /// </summary>
    [ObservableProperty]
    public partial string DefaultOutputFormat { get; set; }

    /// <summary>
    /// Whether new images should use a stored processor-specific output format when available.
    /// </summary>
    [ObservableProperty]
    public partial bool UseProcessorDefaultOutputFormat { get; set; }

    /// <summary>
    /// Whether to check for updates on startup.
    /// </summary>
    [ObservableProperty]
    public partial bool CheckForUpdates { get; set; }

    /// <summary>
    /// Whether to include pre-release versions in update checks.
    /// </summary>
    [ObservableProperty]
    public partial bool IncludePreReleases { get; set; }

    /// <summary>
    /// Maximum number of parallel processing jobs.
    /// </summary>
    [ObservableProperty]
    public partial int MaxParallelJobs { get; set; }

    /// <summary>
    /// UI theme preference.
    /// </summary>
    [ObservableProperty]
    public partial string Theme { get; set; }

    /// <summary>
    /// Output filename suffix pattern.
    /// </summary>
    [ObservableProperty]
    public partial string OutputSuffix { get; set; }

    /// <summary>
    /// Whether new images should use a stored processor-specific output suffix when available.
    /// </summary>
    [ObservableProperty]
    public partial bool UseProcessorDefaultOutputSuffix { get; set; }

    /// <summary>
    /// Default GPU color-space mode for GPU-backed processors.
    /// </summary>
    [ObservableProperty]
    public partial string DefaultGpuColorSpaceMode { get; set; }

    /// <summary>
    /// Whether new images should use a stored processor-specific GPU color-space mode when available.
    /// </summary>
    [ObservableProperty]
    public partial bool UseProcessorDefaultGpuColorSpaceMode { get; set; }

    /// <summary>
    /// The currently selected GPU device.
    /// </summary>
    [ObservableProperty]
    public partial GpuDevice? SelectedGpu { get; set; }

    /// <summary>
    /// List of currently loaded plugins.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasPlugins))]
    public partial List<PluginItemViewModel> Plugins { get; set; } = [];

    /// <summary>
    /// Whether plugin configuration changed while the dialog is open.
    /// </summary>
    [ObservableProperty]
    public partial bool HasPluginChanges { get; set; }

    /// <summary>
    /// Initializes the ViewModel from current application settings.
    /// </summary>
    public SettingsViewModel()
    {
        var s = App.SettingsManager.Current;
        OutputDirectory = s.OutputDirectory;
        UseProcessorDefaultOutputDirectory = s.UseProcessorDefaultOutputDirectory;
        OverwriteExisting = s.OverwriteExisting;
        CheckForUpdates = s.CheckForUpdates;
        IncludePreReleases = s.IncludePreReleases;
        MaxParallelJobs = s.MaxParallelJobs;
        Theme = s.Theme;
        OutputSuffix = s.OutputSuffix;
        UseProcessorDefaultOutputSuffix = s.UseProcessorDefaultOutputSuffix;
        DefaultGpuColorSpaceMode = GpuColorSpaceModeNames.NormalizeOrDefault(s.DefaultGpuColorSpaceMode, GpuColorSpaceMode.Auto);
        UseProcessorDefaultGpuColorSpaceMode = s.UseProcessorDefaultGpuColorSpaceMode;

        AvailableGpus = App.ComputeEngine.EnumerateDevices();
        SelectedGpu = ResolveSelectedGpu(s) ?? App.ComputeEngine.SelectedDevice;

        OutputFormatOptions = ImageFormatRegistryService.GetWritableExtensions(App.TranslatorManager);
        DefaultOutputFormat = ResolveSupportedOutputFormat(s.DefaultOutputFormat);
        UseProcessorDefaultOutputFormat = s.UseProcessorDefaultOutputFormat;

        RefreshPlugins();
    }

    /// <summary>
    /// All available GPU devices.
    /// </summary>
    public IReadOnlyList<GpuDevice> AvailableGpus { get; }

    /// <summary>
    /// Available output format extensions.
    /// </summary>
    public IReadOnlyList<string> OutputFormatOptions { get; }

    /// <summary>
    /// Available GPU color-space options.
    /// </summary>
    public IReadOnlyList<string> GpuColorSpaceOptions { get; } = GpuColorSpaceModeNames.Options;

    /// <summary>
    /// Available theme options.
    /// </summary>
    public IReadOnlyList<string> ThemeOptions { get; } = ["dark", "light"];

    /// <summary>
    /// Managed plugin storage directory.
    /// </summary>
    public string ManagedPluginsDirectory => App.SettingsManager.GetPluginsDirectory();

    /// <summary>
    /// Whether any plugins are currently loaded.
    /// </summary>
    public bool HasPlugins => Plugins.Count > 0;

    /// <summary>
    /// Saves all settings and applies GPU device and theme changes at runtime.
    /// </summary>
    public void Save()
    {
        var s = App.SettingsManager.Current;
        s.OutputDirectory = OutputDirectory.Trim();
        s.UseProcessorDefaultOutputDirectory = UseProcessorDefaultOutputDirectory;
        s.OverwriteExisting = OverwriteExisting;
        s.DefaultOutputFormat = ResolveSupportedOutputFormat(DefaultOutputFormat);
        s.UseProcessorDefaultOutputFormat = UseProcessorDefaultOutputFormat;
        s.CheckForUpdates = CheckForUpdates;
        s.IncludePreReleases = IncludePreReleases;
        s.MaxParallelJobs = MaxParallelJobs;
        s.Theme = Theme;
        s.OutputSuffix = OutputSuffix.Trim();
        s.UseProcessorDefaultOutputSuffix = UseProcessorDefaultOutputSuffix;
        s.DefaultGpuColorSpaceMode = GpuColorSpaceModeNames.NormalizeOrDefault(DefaultGpuColorSpaceMode, GpuColorSpaceMode.Auto);
        s.UseProcessorDefaultGpuColorSpaceMode = UseProcessorDefaultGpuColorSpaceMode;

        if (SelectedGpu is { } gpu)
            App.SwitchGpuDevice(gpu);

        App.ApplyTheme(Theme);
        App.SettingsManager.Save();
    }

    /// <summary>
    /// Refreshes the plugin list from the loaded plugin registry.
    /// </summary>
    public void RefreshPlugins()
    {
        Plugins = App.PluginLoader.LoadedPlugins
            .Select(p => new PluginItemViewModel
            {
                Id = p.Manifest.Id,
                Name = p.Manifest.Name,
                Version = p.Manifest.Version,
                Author = p.Manifest.Author,
                Description = p.Manifest.Description,
                Directory = p.Directory,
                ProcessorCount = p.Processors.Count
            })
            .ToList();
    }

    /// <summary>
    /// Unloads a plugin by ID.
    /// </summary>
    public bool UnloadPlugin(string pluginId)
    {
        var result = App.PluginLoader.UninstallPlugin(pluginId, ManagedPluginsDirectory);
        if (!result)
            return false;

        HasPluginChanges = true;
        RefreshPlugins();
        return true;
    }

    /// <summary>
    /// Loads and installs a plugin package zip.
    /// </summary>
    public void LoadPluginFromZip(string zipPath)
    {
        App.PluginLoader.InstallPluginFromZip(zipPath, ManagedPluginsDirectory);
        HasPluginChanges = true;
        RefreshPlugins();
    }

    private GpuDevice? ResolveSelectedGpu(TextureHawk.Settings.AppSettings settings)
    {
        if (settings.PreferredGpuVendorId is { } vendorId && settings.PreferredGpuDeviceId is { } deviceId)
        {
            var exact = AvailableGpus.FirstOrDefault(d => d.VendorId == vendorId && d.DeviceId == deviceId);
            if (exact != default)
                return exact;
        }

        if (settings.PreferredGpuDeviceName is { } preferred)
        {
            var named = AvailableGpus.FirstOrDefault(d => d.Name == preferred);
            if (named != default)
                return named;
        }

        return null;
    }

    private string ResolveSupportedOutputFormat(string? format)
    {
        var normalized = NormalizeExtension(format);
        var match = OutputFormatOptions.FirstOrDefault(option => string.Equals(option, normalized, StringComparison.OrdinalIgnoreCase));
        return match ?? OutputFormatOptions.FirstOrDefault() ?? string.Empty;
    }

    private static string NormalizeExtension(string? extension)
    {
        if (string.IsNullOrWhiteSpace(extension))
            return string.Empty;

        extension = extension.Trim();
        if (!extension.StartsWith(".", StringComparison.Ordinal))
            extension = $".{extension}";

        return extension.ToLowerInvariant();
    }
}
