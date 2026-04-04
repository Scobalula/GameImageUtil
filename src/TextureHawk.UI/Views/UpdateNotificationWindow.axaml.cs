using System.Diagnostics;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using TextureHawk.UI.ViewModels;

namespace TextureHawk.UI.Views;

public partial class UpdateNotificationWindow : Window
{
    public UpdateNotificationWindow()
    {
        InitializeComponent();
        AddHandler(KeyDownEvent, OnWindowKeyDown, RoutingStrategies.Tunnel);
    }

    private void OnOpenReleaseClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not UpdateNotificationViewModel vm)
            return;

        Process.Start(new ProcessStartInfo
        {
            FileName = vm.Update.Url,
            UseShellExecute = true
        });

        Close();
    }

    private void OnSkipVersionClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not UpdateNotificationViewModel vm)
            return;

        App.SettingsManager.Current.SkippedUpdateVersion = vm.Update.Version;
        App.SettingsManager.Save();
        Close();
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