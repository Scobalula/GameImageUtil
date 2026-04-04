using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using TextureHawk.UI.ViewModels;

namespace TextureHawk.UI.Views;

/// <summary>
/// About window displaying application info and update check.
/// </summary>
public partial class AboutWindow : Window
{
    public AboutWindow()
    {
        InitializeComponent();
        AddHandler(KeyDownEvent, OnWindowKeyDown, RoutingStrategies.Tunnel);
    }

    private async void OnCheckUpdateClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is AboutViewModel vm)
        {
            await vm.CheckForUpdateAsync();
            if (vm.LatestUpdate is not null)
                await new UpdateNotificationWindow { DataContext = new UpdateNotificationViewModel(vm.LatestUpdate) }.ShowDialog(this);
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
