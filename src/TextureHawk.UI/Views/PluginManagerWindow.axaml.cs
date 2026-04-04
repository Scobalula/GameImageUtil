using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using TextureHawk.Logging;
using TextureHawk.UI.ViewModels;

namespace TextureHawk.UI.Views;

/// <summary>
/// Window for loading, unloading, and managing plugins at runtime.
/// </summary>
public partial class PluginManagerWindow : Window
{
    private PluginManagerViewModel ViewModel => (PluginManagerViewModel)DataContext!;

    public PluginManagerWindow()
    {
        InitializeComponent();
        AddHandler(KeyDownEvent, OnWindowKeyDown, RoutingStrategies.Tunnel);
    }

    private async void OnLoadPluginClick(object? sender, RoutedEventArgs e)
    {
        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Select Plugin Package Zip",
            AllowMultiple = false,
            FileTypeFilter =
            [
                new FilePickerFileType("Plugin Package")
                {
                    Patterns = ["*.zip"]
                },
                new FilePickerFileType("All Files")
                {
                    Patterns = ["*.*"]
                }
            ]
        });

        if (files.Count > 0)
        {
            var path = files[0].TryGetLocalPath();
            if (path is not null)
            {
                var trust = new PluginTrustPromptWindow
                {
                    DataContext = new PluginTrustPromptViewModel(path)
                };

                var trusted = await trust.ShowDialog<bool>(this);
                if (!trusted)
                    return;

                try
                {
                    ViewModel.LoadPluginFromZip(path);
                    AppLog.Info("Plugins", $"Installed plugin package from: {path}");
                }
                catch (Exception ex)
                {
                    AppLog.Error("Plugins", $"Failed to install plugin package from {path}", ex);
                }
            }
        }
    }

    private void OnUnloadPluginClick(object? sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: string pluginId })
        {
            ViewModel.UnloadPlugin(pluginId);
            AppLog.Info("Plugins", $"Unloaded plugin: {pluginId}");
        }
    }

    private void OnCloseClick(object? sender, RoutedEventArgs e)
    {
        Close();
    }

    private void OnWindowKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key != Key.Escape)
            return;

        Close();
        e.Handled = true;
    }
}
