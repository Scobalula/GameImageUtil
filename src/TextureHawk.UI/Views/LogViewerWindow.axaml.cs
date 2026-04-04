using System.Text;
using System.Diagnostics;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using TextureHawk.Logging;

namespace TextureHawk.UI.Views;

/// <summary>
/// Window displaying live application logs with copy and clear support.
/// </summary>
public partial class LogViewerWindow : Window
{
    private readonly StringBuilder _buffer = new();

    public LogViewerWindow()
    {
        InitializeComponent();
        AddHandler(KeyDownEvent, OnWindowKeyDown, RoutingStrategies.Tunnel);

        // Load existing entries
        foreach (var entry in AppLog.Entries)
            _buffer.AppendLine(entry.ToString());

        LogLocationText.Text = AppLog.LogDirectory is { } logDirectory
            ? $"Rolling files: {Path.Combine(logDirectory, AppLog.LogFilePattern ?? "texturehawk_*.log")}" 
            : "Rolling files: unavailable";

        LogText.Text = _buffer.ToString();

        AppLog.EntryAdded += OnEntryAdded;
        Closed += (_, _) => AppLog.EntryAdded -= OnEntryAdded;
    }

    private void OnEntryAdded(LogEntry entry)
    {
        Dispatcher.UIThread.Post(() =>
        {
            _buffer.AppendLine(entry.ToString());
            LogText.Text = _buffer.ToString();
            LogScroller.ScrollToEnd();
        });
    }

    private void OnClearClick(object? sender, RoutedEventArgs e)
    {
        _buffer.Clear();
        LogText.Text = string.Empty;
    }

    private async void OnCopyAllClick(object? sender, RoutedEventArgs e)
    {
        if (Clipboard is { } clipboard)
            await clipboard.SetTextAsync(LogText.Text ?? string.Empty);
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

    private void OnOpenLogFolderClick(object? sender, RoutedEventArgs e)
    {
        if (AppLog.LogDirectory is not { } directory || !Directory.Exists(directory))
            return;

        Process.Start(new ProcessStartInfo
        {
            FileName = directory,
            UseShellExecute = true
        });
    }
}
