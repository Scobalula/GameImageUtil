using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using TextureHawk.Logging;
using TextureHawk.UI.ViewModels;

namespace TextureHawk.UI.Views;

/// <summary>
/// Settings window for editing application preferences.
/// </summary>
public partial class SettingsWindow : Window
{
    public SettingsWindow()
    {
        InitializeComponent();
        AddHandler(KeyDownEvent, OnWindowKeyDown, RoutingStrategies.Tunnel);
    }

    private void OnSaveClick(object? sender, RoutedEventArgs e)
    {
        SaveAndClose();
    }

    private void OnCancelClick(object? sender, RoutedEventArgs e)
    {
        Close();
    }

    private void OnWindowKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            Close();
            e.Handled = true;
            return;
        }

        if (e.Key == Key.Enter && e.KeyModifiers == KeyModifiers.None)
        {
            SaveAndClose();
            e.Handled = true;
        }
    }

    private async void OnLoadPluginClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not SettingsViewModel vm)
            return;

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

        if (files.Count == 0)
            return;

        var path = files[0].TryGetLocalPath();
        if (path is null)
            return;

        var trust = new PluginTrustPromptWindow
        {
            DataContext = new PluginTrustPromptViewModel(path)
        };

        var trusted = await trust.ShowDialog<bool>(this);
        if (!trusted)
            return;

        try
        {
            vm.LoadPluginFromZip(path);
            AppLog.Info("Plugins", $"Installed plugin package from settings: {path}");
        }
        catch (Exception ex)
        {
            AppLog.Error("Plugins", $"Failed to install plugin package from settings: {path}", ex);
        }
    }

    private void OnUnloadPluginClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not SettingsViewModel vm || sender is not Button { Tag: string pluginId })
            return;

        if (vm.UnloadPlugin(pluginId))
            AppLog.Info("Plugins", $"Unloaded plugin from settings: {pluginId}");
    }

    private void SaveAndClose()
    {
        if (DataContext is SettingsViewModel vm)
            vm.Save();

        Close();
    }
}
