using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace TextureHawk.UI.Views;

/// <summary>
/// Trust confirmation dialog displayed before installing plugin packages.
/// </summary>
public partial class PluginTrustPromptWindow : Window
{
    public PluginTrustPromptWindow()
    {
        InitializeComponent();
        AddHandler(KeyDownEvent, OnWindowKeyDown, RoutingStrategies.Tunnel);
    }

    private void OnTrustAndInstallClick(object? sender, RoutedEventArgs e)
    {
        Close(true);
    }

    private void OnCancelClick(object? sender, RoutedEventArgs e)
    {
        Close(false);
    }

    private void OnWindowKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key != Key.Escape)
            return;

        Close(false);
        e.Handled = true;
    }
}
