using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using TextureHawk.UI.Services;
using TextureHawk.UI.ViewModels;

namespace TextureHawk.UI.Views;

/// <summary>
/// Dialog that previews folder imports and enumerates candidate files before commit.
/// </summary>
public partial class BulkImportFolderWindow : Window
{
    private readonly HashSet<string> _supportedExtensions;

    private CancellationTokenSource? _scanCts;
    private List<string> _scannedFiles = [];

    private BulkImportFolderViewModel ViewModel => (BulkImportFolderViewModel)DataContext!;

    public BulkImportFolderWindow() : this(string.Empty)
    {
    }

    public BulkImportFolderWindow(string folderPath)
    {
        _supportedExtensions = new HashSet<string>(ImageFormatRegistryService.GetReadableExtensions(App.TranslatorManager), StringComparer.OrdinalIgnoreCase);
        DataContext = new BulkImportFolderViewModel(folderPath);
        InitializeComponent();
        Opened += OnOpened;
        AddHandler(KeyDownEvent, OnWindowKeyDown, RoutingStrategies.Tunnel);
    }

    /// <summary>
    /// Files selected for import when the dialog is accepted.
    /// </summary>
    public IReadOnlyList<string> ImportedFiles { get; private set; } = Array.Empty<string>();

    private async void OnOpened(object? sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(ViewModel.FolderPath) || !Directory.Exists(ViewModel.FolderPath))
        {
            ViewModel.StatusText = "No folder selected.";
            ViewModel.CandidateCount = 0;
            ViewModel.IsEnumerating = false;
            return;
        }

        await StartScanAsync();
    }

    private async void OnRescanClick(object? sender, RoutedEventArgs e)
    {
        await StartScanAsync();
    }

    private async void OnOptionsChanged(object? sender, RoutedEventArgs e)
    {
        if (!IsLoaded)
            return;

        await StartScanAsync();
    }

    private void OnImportClick(object? sender, RoutedEventArgs e)
    {
        ImportedFiles = _scannedFiles;
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

    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);
        _scanCts?.Cancel();
        _scanCts?.Dispose();
        _scanCts = null;
    }

    private async Task StartScanAsync()
    {
        _scanCts?.Cancel();
        _scanCts?.Dispose();
        _scanCts = new CancellationTokenSource();
        var ct = _scanCts.Token;
        var scanOptions = new FolderScanOptions(
            ViewModel.FolderPath,
            ViewModel.IncludeSubfolders,
            ViewModel.SupportedFormatsOnly);
        var progress = new Progress<int>(snapshot =>
        {
            ViewModel.CandidateCount = snapshot;
            ViewModel.StatusText = $"Scanning... {snapshot} candidate file(s)";
        });

        ViewModel.IsEnumerating = true;
        ViewModel.CandidateCount = 0;
        ViewModel.StatusText = "Enumerating files...";

        try
        {
            var files = await Task.Run(() => EnumerateFolder(scanOptions, _supportedExtensions, progress, ct), ct);
            if (ct.IsCancellationRequested)
                return;

            _scannedFiles = files;
            ViewModel.CandidateCount = files.Count;
            ViewModel.StatusText = files.Count == 0
                ? "No matching files found for current options."
                : $"Enumeration complete. Found {files.Count} file(s).";
        }
        catch (OperationCanceledException)
        {
            ViewModel.StatusText = "Enumeration cancelled.";
        }
        finally
        {
            if (!ct.IsCancellationRequested)
                ViewModel.IsEnumerating = false;
        }
    }

    private static List<string> EnumerateFolder(FolderScanOptions options, IReadOnlySet<string> supportedExtensions, IProgress<int>? progress, CancellationToken ct)
    {
        var files = new List<string>();
        var pending = new Stack<string>();
        pending.Push(options.FolderPath);

        var scannedDirs = 0;
        while (pending.Count > 0)
        {
            ct.ThrowIfCancellationRequested();
            var current = pending.Pop();
            scannedDirs++;

            try
            {
                foreach (var file in Directory.EnumerateFiles(current))
                {
                    ct.ThrowIfCancellationRequested();
                    if (options.SupportedFormatsOnly && !supportedExtensions.Contains(Path.GetExtension(file)))
                        continue;
                    files.Add(file);
                }

                if (options.IncludeSubfolders)
                {
                    foreach (var child in Directory.EnumerateDirectories(current))
                        pending.Push(child);
                }
            }
            catch (UnauthorizedAccessException)
            {
                continue;
            }
            catch (IOException)
            {
                continue;
            }

            if ((scannedDirs % 10) == 0)
                progress?.Report(files.Count);
        }

        return files;
    }

    private readonly record struct FolderScanOptions(string FolderPath, bool IncludeSubfolders, bool SupportedFormatsOnly);
}
