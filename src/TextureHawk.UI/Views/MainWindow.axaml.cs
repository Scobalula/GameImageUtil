using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Selection;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Avalonia.VisualTree;
using System.Diagnostics;
using TextureHawk.Logging;
using TextureHawk.UI.Services;
using TextureHawk.UI.ViewModels;

namespace TextureHawk.UI.Views;

/// <summary>
/// Main application window handling drag-and-drop, file selection, and batch processing.
/// </summary>
public partial class MainWindow : Window
{
    private bool _startupUpdateCheckCompleted;
    private bool _syncingSelection;
    private MainWindowViewModel? _subscribedViewModel;

    private MainWindowViewModel ViewModel => (MainWindowViewModel)DataContext!;

    public MainWindow()
    {
        InitializeComponent();
        QueueDropSurface.AddHandler(DragDrop.DragEnterEvent, OnDragEnter);
        QueueDropSurface.AddHandler(DragDrop.DragOverEvent, OnDragOver);
        QueueDropSurface.AddHandler(DragDrop.DragLeaveEvent, OnDragLeave);
        QueueDropSurface.AddHandler(DragDrop.DropEvent, OnDrop);
        Opened += OnOpened;
        DataContextChanged += OnDataContextChanged;
        AddHandler(KeyDownEvent, OnKeyDown, RoutingStrategies.Tunnel);
        ProcessorComboBox.ContainerPrepared += OnProcessorComboBoxContainerPrepared;
    }

    private async void OnOpened(object? sender, EventArgs e)
    {
        if (_startupUpdateCheckCompleted || !App.SettingsManager.Current.CheckForUpdates)
            return;

        _startupUpdateCheckCompleted = true;

        try
        {
            var update = await UpdateCheckService.CheckForUpdateAsync(default);
            if (update is not null
                && !string.Equals(App.SettingsManager.Current.SkippedUpdateVersion, update.Version, StringComparison.OrdinalIgnoreCase))
            {
                await new UpdateNotificationWindow { DataContext = new UpdateNotificationViewModel(update) }.ShowDialog(this);
            }
        }
        catch (Exception ex)
        {
            AppLog.Warn("Updates", $"Startup update check failed: {ex.Message}");
        }
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Delete && !e.KeyModifiers.HasFlag(KeyModifiers.Control) && CanDeleteQueueSelection(e.Source))
        {
            if (ViewModel.RemoveSelectedItems())
                e.Handled = true;
            return;
        }

        if (e.Key == Key.D && e.KeyModifiers == (KeyModifiers.Control | KeyModifiers.Shift) && CanOperateOnQueueSelection(e.Source))
        {
            if (ViewModel.TryAutoDetectProcessorForSelection())
                e.Handled = true;
            return;
        }

        var slot = MapDigitKeyToSlot(e.Key);
        if (slot is null || !e.KeyModifiers.HasFlag(KeyModifiers.Control))
            return;

        var handled = e.KeyModifiers.HasFlag(KeyModifiers.Shift)
            ? ViewModel.TryBindSelectedProcessorHotkeySlot(slot.Value)
            : ViewModel.TrySelectProcessorHotkeySlot(slot.Value);

        if (handled)
            e.Handled = true;
    }

    private static int? MapDigitKeyToSlot(Key key)
    {
        return key switch
        {
            Key.D1 or Key.NumPad1 => 1,
            Key.D2 or Key.NumPad2 => 2,
            Key.D3 or Key.NumPad3 => 3,
            Key.D4 or Key.NumPad4 => 4,
            Key.D5 or Key.NumPad5 => 5,
            Key.D6 or Key.NumPad6 => 6,
            _ => null
        };
    }

    private static bool CanDeleteQueueSelection(object? eventSource)
    {
        return CanOperateOnQueueSelection(eventSource);
    }

    private static bool CanOperateOnQueueSelection(object? eventSource)
    {
        return eventSource is not TextBox
            && eventSource is not NumericUpDown
            && eventSource is not ComboBox;
    }

    private void OnDragEnter(object? sender, DragEventArgs e)
    {
        UpdateDragState(e);
        e.Handled = true;
    }

    private void OnDragOver(object? sender, DragEventArgs e)
    {
        UpdateDragState(e);
        e.Handled = true;
    }

    private void OnDragLeave(object? sender, DragEventArgs e)
    {
        ViewModel.ShowDropOverlay = false;
        e.Handled = true;
    }

    private async void OnDrop(object? sender, DragEventArgs e)
    {
        ViewModel.ShowDropOverlay = false;
        if (e.Data.GetFiles() is { } files)
        {
            var paths = files
                .Select(f => f.TryGetLocalPath())
                .Where(p => p is not null)
                .Cast<string>()
                .ToList();

            var droppedFiles = paths.Where(File.Exists).ToList();
            var droppedDirectories = GetRootDirectories(paths);

            if (droppedDirectories.Count > 0)
            {
                foreach (var directory in droppedDirectories)
                {
                    var importWindow = new BulkImportFolderWindow(directory);
                    var confirmed = await importWindow.ShowDialog<bool>(this);
                    if (!confirmed)
                        break;

                    if (importWindow.ImportedFiles.Count > 0)
                        ViewModel.AddFiles(importWindow.ImportedFiles);
                }

                e.Handled = true;
                return;
            }

            if (droppedFiles.Count > 0)
                ViewModel.AddFiles(droppedFiles);
        }

        e.Handled = true;
    }

    private static List<string> GetRootDirectories(IEnumerable<string> candidatePaths)
    {
        var directories = candidatePaths
            .Where(Directory.Exists)
            .Select(Path.GetFullPath)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(path => path.Length)
            .ToList();

        var roots = new List<string>(directories.Count);
        foreach (var directory in directories)
        {
            if (roots.Any(root => IsParentOrSame(root, directory)))
                continue;

            roots.Add(directory);
        }

        return roots;
    }

    private static bool IsParentOrSame(string parent, string child)
    {
        if (string.Equals(parent, child, StringComparison.OrdinalIgnoreCase))
            return true;

        if (!child.StartsWith(parent, StringComparison.OrdinalIgnoreCase))
            return false;

        var separatorIndex = parent.Length;
        return separatorIndex < child.Length
            && (child[separatorIndex] == Path.DirectorySeparatorChar || child[separatorIndex] == Path.AltDirectorySeparatorChar);
    }

    private void UpdateDragState(DragEventArgs e)
    {
        var hasFiles = e.Data.Contains(DataFormats.Files);
        ViewModel.ShowDropOverlay = hasFiles;
        e.DragEffects = hasFiles ? DragDropEffects.Copy : DragDropEffects.None;
    }

    private void OnTextureListSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_syncingSelection || sender is not ListBox listBox)
            return;

        var selectedItems = listBox.SelectedItems?
            .OfType<TextureItemViewModel>()
            .ToList()
            ?? [];

        ViewModel.SetSelectedItemsFromView(selectedItems);
    }

    private void OnTextureListDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (e.Source is not Control sourceControl)
            return;

        var item = sourceControl.FindAncestorOfType<ListBoxItem>()?.DataContext as TextureItemViewModel
            ?? ViewModel.SelectedTextureItem;
        if (item is null)
            return;

        FileRevealService.Reveal(item.FilePath);
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (_subscribedViewModel is not null)
            _subscribedViewModel.SelectionSyncRequested -= OnSelectionSyncRequested;

        if (DataContext is not MainWindowViewModel viewModel)
            return;

        _subscribedViewModel = viewModel;
        viewModel.SelectionSyncRequested += OnSelectionSyncRequested;
        SyncSelectionFromViewModel(viewModel.SelectedTextureItems, scrollIntoView: false);
    }

    private void OnSelectionSyncRequested(IReadOnlyList<TextureItemViewModel> items, bool scrollIntoView)
    {
        SyncSelectionFromViewModel(items, scrollIntoView);
    }

    private void SyncSelectionFromViewModel(IReadOnlyList<TextureItemViewModel> items, bool scrollIntoView)
    {
        if (TextureListBox.SelectedItems is null)
            return;

        if (HasSameSelection(TextureListBox.SelectedItems, items))
            return;

        _syncingSelection = true;
        try
        {
            TextureListBox.SelectedItems.Clear();
            foreach (var item in items)
                TextureListBox.SelectedItems.Add(item);

            if (scrollIntoView && items.Count > 0)
                TextureListBox.ScrollIntoView(items[^1]);
        }
        finally
        {
            _syncingSelection = false;
        }
    }

    private static bool HasSameSelection(System.Collections.IList selectedItems, IReadOnlyList<TextureItemViewModel> items)
    {
        if (selectedItems.Count != items.Count)
            return false;

        for (var i = 0; i < items.Count; i++)
        {
            if (!selectedItems.Contains(items[i]))
                return false;
        }

        return true;
    }

    private async void OnAddFilesClick(object? sender, RoutedEventArgs e)
    {
        await OpenFilePicker();
    }

    private async Task OpenFilePicker()
    {
        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Select Files",
            AllowMultiple = true,
            FileTypeFilter = BuildImageFileTypeFilter()
        });

        var paths = files
            .Select(f => f.TryGetLocalPath())
            .Where(p => p is not null)
            .Cast<string>();
        ViewModel.AddFiles(paths);
    }

    private async void OnProcessAllClick(object? sender, RoutedEventArgs e)
    {
        await ViewModel.ProcessAllAsync(default);
    }

    private async void OnProcessSelectedClick(object? sender, RoutedEventArgs e)
    {
        await ViewModel.ProcessSelectedAsync(default);
    }

    private void OnCancelClick(object? sender, RoutedEventArgs e)
    {
        ViewModel.CancelProcessing();
    }

    private void OnClearAllClick(object? sender, RoutedEventArgs e)
    {
        ViewModel.ClearAll();
    }

    private void OnRemoveItemClick(object? sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: TextureItemViewModel item })
            ViewModel.RemoveItem(item);
    }

    private async void OnBrowseOutputDirClick(object? sender, RoutedEventArgs e)
    {
        var folders = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "Select Output Directory",
            AllowMultiple = false
        });

        if (folders.Count > 0)
        {
            var path = folders[0].TryGetLocalPath();
            if (path is not null)
                ViewModel.OutputDirectory = path;
        }
    }

    private async void OnSettingsClick(object? sender, RoutedEventArgs e)
    {
        var settingsViewModel = new SettingsViewModel();
        var window = new SettingsWindow { DataContext = settingsViewModel };
        await window.ShowDialog(this);

        if (settingsViewModel.HasPluginChanges)
            ViewModel.RefreshProcessors();

        ViewModel.RefreshSettings();
    }

    private async void OnCheckUpdatesClick(object? sender, RoutedEventArgs e)
    {
        var vm = new AboutViewModel();
        var about = new AboutWindow { DataContext = vm };
        _ = vm.CheckForUpdateAsync();
        await about.ShowDialog(this);
    }

    private void OnAboutClick(object? sender, RoutedEventArgs e)
    {
        var window = new AboutWindow { DataContext = new AboutViewModel() };
        window.ShowDialog(this);
    }

    private void OnShowLogClick(object? sender, RoutedEventArgs e)
    {
        var window = new LogViewerWindow();
        window.Show(this);
    }

    private async void OnDropzoneClick(object? sender, PointerPressedEventArgs e)
    {
        await OpenFilePicker();
    }

    private void OnParameterDragEnter(object? sender, DragEventArgs e)
    {
        UpdateParameterDragState(e);
    }

    private void OnParameterDragOver(object? sender, DragEventArgs e)
    {
        UpdateParameterDragState(e);
    }

    private void OnParameterDrop(object? sender, DragEventArgs e)
    {
        if (sender is not TextBox { Tag: ParameterItemViewModel param })
            return;
        if (!e.Data.Contains(DataFormats.Files))
            return;
        if (e.Data.GetFiles() is not { } files)
            return;

        foreach (var file in files)
        {
            if (file.TryGetLocalPath() is string path)
            {
                param.StringValue = path;
                e.Handled = true;
                break;
            }
        }
    }

    private static void UpdateParameterDragState(DragEventArgs e)
    {
        var hasFiles = e.Data.Contains(DataFormats.Files);
        e.DragEffects = hasFiles ? DragDropEffects.Copy : DragDropEffects.None;
        e.Handled = true;
    }

    private async void OnBrowseParameterFileClick(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: ParameterItemViewModel param })
            return;

        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Select File",
            AllowMultiple = false,
            FileTypeFilter = BuildImageFileTypeFilter()
        });

        if (files.Count > 0)
        {
            var path = files[0].TryGetLocalPath();
            if (path is not null)
                param.StringValue = path;
        }
    }

    private void OnProcessorComboBoxContainerPrepared(object? sender, ContainerPreparedEventArgs e)
    {
        if (e.Container.DataContext is ProcessorGroupItem { IsHeader: true })
        {
            e.Container.IsHitTestVisible = false;
            e.Container.Focusable = false;
        }
        else
        {
            e.Container.IsHitTestVisible = true;
            e.Container.Focusable = true;
        }
    }

    private static IReadOnlyList<FilePickerFileType> BuildImageFileTypeFilter()
    {
        var imagePatterns = ImageFormatRegistryService.GetReadPatterns(App.TranslatorManager);
        if (imagePatterns.Count == 0)
            imagePatterns = ["*.*"];

        return
        [
            new FilePickerFileType("Image Files")
            {
                Patterns = imagePatterns
            },
            new FilePickerFileType("All Files")
            {
                Patterns = ["*.*"]
            }
        ];
    }
}
