using CommunityToolkit.Mvvm.ComponentModel;

namespace TextureHawk.UI.ViewModels;

/// <summary>
/// ViewModel for the plugin manager window.
/// </summary>
public sealed partial class PluginManagerViewModel : ObservableObject
{
    [ObservableProperty]
    public partial bool HasChanges { get; set; }

    /// <summary>
    /// List of currently loaded plugins.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasPlugins))]
    public partial List<PluginItemViewModel> Plugins { get; set; } = [];

    /// <summary>
    /// Initializes the ViewModel and loads the current plugin list.
    /// </summary>
    public PluginManagerViewModel()
    {
        RefreshPlugins();
    }

    /// <summary>
    /// Managed plugin storage directory.
    /// </summary>
    public string ManagedPluginsDirectory => App.SettingsManager.GetPluginsDirectory();

    /// <summary>
    /// Whether any plugins are currently loaded.
    /// </summary>
    public bool HasPlugins => Plugins.Count > 0;

    /// <summary>
    /// Refreshes the plugin list from the <see cref="PluginLoader"/>.
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
    /// Unloads a plugin by ID and refreshes the list.
    /// </summary>
    /// <returns><see langword="true"/> if the plugin was found and unloaded.</returns>
    public bool UnloadPlugin(string pluginId)
    {
        var result = App.PluginLoader.UninstallPlugin(pluginId, ManagedPluginsDirectory);
        if (result)
        {
            HasChanges = true;
            RefreshPlugins();
        }

        return result;
    }

    /// <summary>
    /// Loads a plugin from the specified zip package and refreshes the list.
    /// </summary>
    public void LoadPluginFromZip(string zipPath)
    {
        App.PluginLoader.InstallPluginFromZip(zipPath, ManagedPluginsDirectory);
        HasChanges = true;
        RefreshPlugins();
    }
}
