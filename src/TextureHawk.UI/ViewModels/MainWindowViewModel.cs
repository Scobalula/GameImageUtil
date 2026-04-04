using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using TextureHawk.Gpu;
using TextureHawk.Logging;
using TextureHawk.Processing;
using TextureHawk.Settings;
using TextureHawk.UI.Services;

namespace TextureHawk.UI.ViewModels;

/// <summary>
/// ViewModel for the main application window.
/// </summary>
public sealed partial class MainWindowViewModel : ViewModelBase
{
    private bool _isLoadingItemSettings;
    private bool _isRevertingGroupItem;
    private CancellationTokenSource? _processingCts;

    /// <summary>
    /// The currently selected processor for batch operations.
    /// </summary>
    [ObservableProperty]
    public partial IProcessor? SelectedProcessor { get; set; }

    /// <summary>
    /// The currently selected item in the texture list.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSelectedTextureItem))]
    public partial TextureItemViewModel? SelectedTextureItem { get; set; }

    /// <summary>
    /// The currently selected grouped processor item (syncs with SelectedProcessor).
    /// </summary>
    [ObservableProperty]
    public partial ProcessorGroupItem? SelectedGroupItem { get; set; }

    /// <summary>
    /// Output directory for processed files.
    /// </summary>
    [ObservableProperty]
    public partial string OutputDirectory { get; set; } = "";

    /// <summary>
    /// Selected output file format extension.
    /// </summary>
    [ObservableProperty]
    public partial string OutputFormat { get; set; }

    /// <summary>
    /// Status bar text describing the current operation.
    /// </summary>
    [ObservableProperty]
    public partial string StatusText { get; set; } = "Ready";

    /// <summary>
    /// Whether batch processing is currently in progress.
    /// </summary>
    [ObservableProperty]
    public partial bool IsProcessing { get; set; }

    /// <summary>
    /// Overall progress percentage (0–100) of the current batch.
    /// </summary>
    [ObservableProperty]
    public partial double Progress { get; set; }

    /// <summary>
    /// Number of items that have completed processing in the current batch.
    /// </summary>
    [ObservableProperty]
    public partial int CompletedCount { get; set; }

    /// <summary>
    /// Whether to show the drag-and-drop overlay hint.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowEmptyDropZone))]
    public partial bool ShowDropOverlay { get; set; }

    /// <summary>
    /// Suffix pattern appended to output file names.
    /// </summary>
    [ObservableProperty]
    public partial string OutputSuffix { get; set; }

    /// <summary>
    /// Selected GPU color-space mode for GPU-backed processors.
    /// </summary>
    [ObservableProperty]
    public partial string GpuColorSpace { get; set; }

    /// <summary>
    /// Whether the texture queue contains any items.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowEmptyDropZone))]
    public partial bool HasItems { get; set; }

    /// <summary>
    /// When enabled, files are processed immediately on drop/add instead of being queued.
    /// </summary>
    [ObservableProperty]
    public partial bool QuickConvert { get; set; }

    /// <summary>
    /// Whether the selected processor has any configurable parameters.
    /// </summary>
    [ObservableProperty]
    public partial bool HasParameters { get; set; }

    /// <summary>
    /// Whether the selected output format has any configurable write parameters.
    /// </summary>
    [ObservableProperty]
    public partial bool HasOutputParameters { get; set; }

    public event Action<IReadOnlyList<TextureItemViewModel>, bool>? SelectionSyncRequested;

    public MainWindowViewModel()
    {
        var settings = App.SettingsManager.Current;

        RefreshOutputFormatOptions();

        OutputFormat = ResolveSupportedOutputFormat(settings.DefaultOutputFormat);
        OutputDirectory = settings.OutputDirectory;
        OutputSuffix = settings.OutputSuffix;
        GpuColorSpace = ResolveGpuColorSpaceSelection(null, settings.DefaultGpuColorSpaceMode);

        TextureItems.CollectionChanged += OnTextureItemsChanged;

        RefreshProcessors();

        if (settings.LastProcessorId is { } lastId)
            SelectedProcessor = Processors.FirstOrDefault(p => p.Id == lastId);
        SelectedProcessor ??= Processors.FirstOrDefault(p => p.Id == "builtin.baseline-file-convert") ?? Processors.FirstOrDefault();

        AppLog.Info("MainVM", $"Initialized — processor: {SelectedProcessor?.Name ?? "none"}, {Processors.Count} processors loaded");
    }

    /// <summary>
    /// Items queued for processing.
    /// </summary>
    public ObservableCollection<TextureItemViewModel> TextureItems { get; } = [];

    /// <summary>
    /// Items currently selected in the queue.
    /// </summary>
    public ObservableCollection<TextureItemViewModel> SelectedTextureItems { get; } = [];

    /// <summary>
    /// All registered processors available for selection.
    /// </summary>
    public ObservableCollection<IProcessor> Processors { get; } = [];

    /// <summary>
    /// Grouped processor items for the ComboBox (headers + processors).
    /// </summary>
    public ObservableCollection<ProcessorGroupItem> GroupedProcessors { get; } = [];

    /// <summary>
    /// Parameters for the currently selected processor, with editable values.
    /// </summary>
    public ObservableCollection<ParameterItemViewModel> CurrentParameters { get; } = [];

    /// <summary>
    /// Parameters for the currently selected output format.
    /// </summary>
    public ObservableCollection<ParameterItemViewModel> CurrentOutputParameters { get; } = [];

    /// <summary>
    /// User-supplied values for processor parameters, keyed by parameter ID.
    /// </summary>
    public Dictionary<string, object> ParameterValues { get; } = [];

    /// <summary>
    /// Whether an image is currently selected for editing.
    /// </summary>
    public bool HasSelectedTextureItem => SelectedTextureItem is not null;

    /// <summary>
    /// Whether to show the empty-state drop hint instead of the drag overlay.
    /// </summary>
    public bool ShowEmptyDropZone => !HasItems && !ShowDropOverlay;

    /// <summary>
    /// Available output format extensions.
    /// </summary>
    public ObservableCollection<string> OutputFormatOptions { get; } = [];

    /// <summary>
    /// Available GPU color-space options.
    /// </summary>
    public IReadOnlyList<string> GpuColorSpaceOptions { get; } = GpuColorSpaceModeNames.Options;

    public bool TrySelectProcessorHotkeySlot(int slot)
    {
        var settings = App.SettingsManager.Current;
        EnsureProcessorHotkeySlots(settings);

        if (slot < 1 || slot > settings.ProcessorHotkeySlots.Count)
            return false;

        var processorId = settings.ProcessorHotkeySlots[slot - 1];
        if (string.IsNullOrWhiteSpace(processorId))
        {
            StatusText = $"No mode bound to Ctrl+{slot}";
            return false;
        }

        var processor = Processors.FirstOrDefault(candidate => candidate.Id == processorId);
        if (processor is null)
        {
            StatusText = $"Bound mode for Ctrl+{slot} is no longer available";
            return false;
        }

        SelectedProcessor = processor;
        StatusText = $"Selected mode {processor.Name} via Ctrl+{slot}";
        AppLog.Info("MainVM", $"Selected processor slot {slot}: {processor.Id}");
        return true;
    }

    public bool TryBindSelectedProcessorHotkeySlot(int slot)
    {
        if (SelectedProcessor is null)
            return false;

        var settings = App.SettingsManager.Current;
        EnsureProcessorHotkeySlots(settings);

        if (slot < 1 || slot > settings.ProcessorHotkeySlots.Count)
            return false;

        settings.ProcessorHotkeySlots[slot - 1] = SelectedProcessor.Id;
        App.SettingsManager.Save();
        StatusText = $"Bound Ctrl+{slot} to {SelectedProcessor.Name}";
        AppLog.Info("MainVM", $"Bound processor slot {slot} to {SelectedProcessor.Id}");
        return true;
    }

    public bool TryAutoDetectProcessorForSelection()
    {
        var inputPath = SelectedTextureItems.LastOrDefault()?.FilePath ?? SelectedTextureItem?.FilePath;
        if (string.IsNullOrWhiteSpace(inputPath))
        {
            StatusText = "Select an item before auto-detecting a mode";
            return false;
        }

        var processor = Processors
            .OfType<IAutoDetectProcessor>()
            .FirstOrDefault(candidate => candidate.CanConvert(inputPath));

        if (processor is null)
        {
            StatusText = $"No auto-detected mode matched {Path.GetFileName(inputPath)}";
            return false;
        }

        SelectedProcessor = processor;
        StatusText = $"Auto-detected mode {processor.Name}";
        AppLog.Info("MainVM", $"Auto-detected processor {processor.Id} for {inputPath}");
        return true;
    }

    partial void OnOutputDirectoryChanged(string value)
    {
        var overrideValue = value.Trim();
        SaveSelectedItemOutputSettings(item => item.OutputDirectoryOverride = overrideValue);
        PersistSelectedProcessorDefaults();
    }

    partial void OnOutputFormatChanged(string value)
    {
        if (!string.Equals(value, NormalizeExtension(value), StringComparison.Ordinal))
        {
            OutputFormat = NormalizeExtension(value);
            return;
        }

        SaveSelectedItemOutputSettings(item => item.OutputFormatOverride = string.IsNullOrEmpty(value) ? null : value);
        if (!_isLoadingItemSettings)
            ReloadParameterEditorForCurrentContext();
        PersistSelectedProcessorDefaults();
    }

    partial void OnOutputSuffixChanged(string value)
    {
        SaveSelectedItemOutputSettings(item => item.OutputSuffixOverride = value.Trim());
        PersistSelectedProcessorDefaults();
    }

    partial void OnGpuColorSpaceChanged(string value)
    {
        var normalized = ResolveGpuColorSpaceSelection(value, App.SettingsManager.Current.DefaultGpuColorSpaceMode);
        if (!string.Equals(value, normalized, StringComparison.Ordinal))
        {
            GpuColorSpace = normalized;
            return;
        }

        SaveSelectedItemSettings(item => item.GpuColorSpaceOverride = value);
        PersistSelectedProcessorDefaults();
    }

    partial void OnSelectedProcessorChanged(IProcessor? value)
    {
        App.SettingsManager.Current.LastProcessorId = value?.Id;

        if (value is not null && !_isLoadingItemSettings && SelectedTextureItem is null)
            LoadOutputEditorForProcessor(value);

        if (!_isLoadingItemSettings && value is not null)
        {
            foreach (var item in EnumerateEditableSelection())
            {
                item.ProcessorOverrideId = value.Id;
                item.ProcessorDisplayName = value.Name;
                item.ParameterOverrides = CreateParameterSnapshot(value, defaults: GetProcessorDefaults(value.Id));
            }
        }

        LoadParameterEditor(value, SelectedTextureItem?.ParameterOverrides);

        // Sync grouped item to match
        if (value is not null && (SelectedGroupItem?.Processor?.Id != value.Id))
        {
            var match = GroupedProcessors.FirstOrDefault(g => !g.IsHeader && g.Processor?.Id == value.Id);
            if (match is not null)
                SelectedGroupItem = match;
        }
    }

    partial void OnSelectedGroupItemChanged(ProcessorGroupItem? value)
    {
        if (_isRevertingGroupItem || value is null)
            return;

        // Revert if a non-selectable header was somehow selected
        if (value.IsHeader)
        {
            _isRevertingGroupItem = true;
            try
            {
                var match = GroupedProcessors.FirstOrDefault(g => !g.IsHeader && g.Processor?.Id == SelectedProcessor?.Id)
                         ?? GroupedProcessors.FirstOrDefault(g => !g.IsHeader);
                SelectedGroupItem = match;
            }
            finally
            {
                _isRevertingGroupItem = false;
            }
            return;
        }

        if (value.Processor is { } processor)
            SelectedProcessor = processor;
    }

    partial void OnSelectedTextureItemChanged(TextureItemViewModel? value)
    {
        _isLoadingItemSettings = true;
        try
        {
            if (value is null)
                return;

            var processor = ResolveProcessorForItem(value);
            EnsureItemSettings(value, processor);
            LoadOutputEditor(value);

            if (SelectedProcessor?.Id != processor.Id)
                SelectedProcessor = processor;

            LoadParameterEditor(processor, value.ParameterOverrides);
            value.ParameterOverrides = new Dictionary<string, object>(ParameterValues, StringComparer.Ordinal);
        }
        finally
        {
            _isLoadingItemSettings = false;
        }
    }

    private void OnTextureItemsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        HasItems = TextureItems.Count > 0;

        if (SelectedTextureItem is not null && !TextureItems.Contains(SelectedTextureItem))
            SelectedTextureItem = TextureItems.LastOrDefault();
    }

    /// <summary>
    /// Reloads the processor list from the <see cref="ProcessorRegistry"/>,
    /// preserving the current selection and per-item overrides when possible.
    /// </summary>
    public void RefreshProcessors()
    {
        var previousId = SelectedProcessor?.Id;
        var currentItem = SelectedTextureItem;
        Processors.Clear();
        GroupedProcessors.Clear();
        foreach (var processor in App.ProcessorRegistry.GetAll().OrderBy(p => p.Category).ThenBy(p => p.Name))
            Processors.Add(processor);

        // Build grouped list with category headers
        string? lastCategory = null;
        foreach (var processor in Processors)
        {
            if (processor.Category != lastCategory)
            {
                GroupedProcessors.Add(new ProcessorGroupItem
                {
                    DisplayName = processor.Category.ToUpperInvariant(),
                    IsHeader = true
                });
                lastCategory = processor.Category;
            }

            GroupedProcessors.Add(new ProcessorGroupItem
            {
                DisplayName = processor.Name,
                Icon = processor.Icon,
                Processor = processor
            });
        }

        // Restore processor selection
        _isLoadingItemSettings = true;
        try
        {
            if (previousId is not null)
                SelectedProcessor = Processors.FirstOrDefault(p => p.Id == previousId);
            SelectedProcessor ??= Processors.FirstOrDefault(p => p.Id == "builtin.baseline-file-convert") ?? Processors.FirstOrDefault();

            // Restore per-item parameter overrides if an item is selected
            if (currentItem is not null)
            {
                var processor = ResolveProcessorForItem(currentItem);
                EnsureItemSettings(currentItem, processor);
                SelectedProcessor = processor;
                LoadParameterEditor(processor, currentItem.ParameterOverrides);
            }
        }
        finally
        {
            _isLoadingItemSettings = false;
        }

        RefreshItemOutputPaths();

        AppLog.Debug("MainVM", $"Refreshed processors: {Processors.Count} processors, {GroupedProcessors.Count} grouped items");
    }

    public void RefreshSettings()
    {
        var settings = App.SettingsManager.Current;

        RefreshOutputFormatOptions();

        if (SelectedTextureItem is { } item)
        {
            var processor = ResolveProcessorForItem(item);

            _isLoadingItemSettings = true;
            try
            {
                LoadOutputEditor(item);

                if (SelectedProcessor?.Id != processor.Id)
                    SelectedProcessor = processor;
                else
                    LoadParameterEditor(processor, item.ParameterOverrides);
            }
            finally
            {
                _isLoadingItemSettings = false;
            }

            return;
        }

        if (SelectedProcessor is { } processorSelection)
        {
            LoadOutputEditorForProcessor(processorSelection);
            LoadParameterEditor(processorSelection);
            return;
        }

        _isLoadingItemSettings = true;
        try
        {
            OutputFormat = ResolveSupportedOutputFormat(settings.DefaultOutputFormat);
            OutputDirectory = settings.OutputDirectory;
            OutputSuffix = settings.OutputSuffix;
            GpuColorSpace = ResolveGpuColorSpaceSelection(null, settings.DefaultGpuColorSpaceMode);
        }
        finally
        {
            _isLoadingItemSettings = false;
        }
    }

    /// <summary>
    /// Adds files to the processing queue, skipping duplicates and non-existent paths.
    /// When <see cref="QuickConvert"/> is active, files are processed immediately instead.
    /// </summary>
    public void AddFiles(IEnumerable<string> filePaths)
    {
        var paths = filePaths.Where(File.Exists).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        var addedItems = new List<TextureItemViewModel>(paths.Count);
        TextureItemViewModel? lastAddedItem = null;

        foreach (var path in paths)
        {
            if (TextureItems.Any(t => t.FilePath == path))
                continue;

            var item = new TextureItemViewModel(path);
            var processor = ResolveInitialProcessor(path);
            ApplyItemDefaults(item, processor);

            item.OutputPath = ResolveOutputPath(item);
            TextureItems.Add(item);
            addedItems.Add(item);
            lastAddedItem = item;
        }

        AppLog.Info("MainVM", $"Added {addedItems.Count} file(s), total queued: {TextureItems.Count}");
        StatusText = $"{TextureItems.Count} file(s) queued";

        if (lastAddedItem is not null)
        {
            SelectedTextureItem = lastAddedItem;
            SetSelectedItems([lastAddedItem]);
        }

        if (QuickConvert && addedItems.Count > 0)
        {
            if (IsProcessing)
            {
                StatusText = $"Queued {addedItems.Count} new file(s); finish current batch before quick convert";
                return;
            }

            _ = ProcessItemsAsync(addedItems, default);
        }
    }

    /// <summary>
    /// Removes a single item from the processing queue.
    /// </summary>
    public void RemoveItem(TextureItemViewModel item)
    {
        TextureItems.Remove(item);
        AppLog.Debug("MainVM", $"Removed item: {item.FileName}");
    }

    public bool RemoveSelectedItems()
    {
        var items = EnumerateEditableSelection().Distinct().ToList();
        if (items.Count == 0)
            return false;

        var firstIndex = TextureItems
            .Select((item, index) => (item, index))
            .Where(pair => items.Contains(pair.item))
            .Select(pair => pair.index)
            .DefaultIfEmpty(-1)
            .Min();

        foreach (var item in items)
            TextureItems.Remove(item);

        SelectedTextureItems.Clear();

        if (TextureItems.Count > 0)
        {
            var nextIndex = Math.Clamp(firstIndex, 0, TextureItems.Count - 1);
            SetSelectedItems([TextureItems[nextIndex]]);
        }
        else
        {
            SelectedTextureItem = null;
        }

        AppLog.Debug("MainVM", $"Removed {items.Count} selected item(s)");
        return true;
    }

    /// <summary>
    /// Clears all items from the processing queue and resets status.
    /// </summary>
    public void ClearAll()
    {
        TextureItems.Clear();
        StatusText = "Ready";
        CompletedCount = 0;
        Progress = 0;
        AppLog.Info("MainVM", "Queue cleared");
    }

    /// <summary>
    /// Cancels the current processing batch if running.
    /// </summary>
    public void CancelProcessing()
    {
        _processingCts?.Cancel();
        AppLog.Info("MainVM", "Cancellation requested");
    }

    /// <summary>
    /// Processes all queued items sequentially using the selected processor.
    /// </summary>
    public async Task ProcessAllAsync(CancellationToken cancellationToken)
    {
        await ProcessItemsAsync([.. TextureItems], cancellationToken);
    }

    /// <summary>
    /// Processes only the currently selected item using the selected processor.
    /// </summary>
    public async Task ProcessSelectedAsync(CancellationToken cancellationToken)
    {
        if (SelectedTextureItem is not { } item)
        {
            AppLog.Warn("MainVM", "No item selected for processing");
            return;
        }

        await ProcessItemsAsync([item], cancellationToken);
    }

    private async Task ProcessItemsAsync(List<TextureItemViewModel> items, CancellationToken cancellationToken)
    {
        if (SelectedProcessor is null || items.Count == 0)
            return;

        _processingCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var ct = _processingCts.Token;

        IsProcessing = true;
        CompletedCount = 0;
        Progress = 0;

        var total = items.Count;
        AppLog.Info("Pipeline", $"Starting batch — {total} items, processor: {SelectedProcessor.Id}");
        var jobs = new List<ProcessingJob>(total);

        foreach (var item in items)
        {
            var outputPath = ResolveOutputPath(item);
            item.OutputPath = outputPath;
            item.Status = "Pending";
            item.IsProcessing = false;
            item.IsCompleted = false;
            item.HasFailed = false;
            item.ErrorMessage = null;

            jobs.Add(new ProcessingJob
            {
                InputPath = item.FilePath,
                OutputPath = outputPath,
                ProcessorId = item.ProcessorOverrideId ?? SelectedProcessor.Id,
                ParameterValues = CreateParameterSnapshot(
                    ResolveProcessorForItem(item),
                    item.ParameterOverrides,
                    GetProcessorDefaults(item.ProcessorOverrideId ?? SelectedProcessor.Id)),
                GpuColorSpaceMode = ResolveJobGpuColorSpaceMode(item, item.ProcessorOverrideId ?? SelectedProcessor.Id)
            });
        }

        for (var i = 0; i < jobs.Count; i++)
        {
            if (ct.IsCancellationRequested)
                break;

            var item = items[i];
            var job = jobs[i];

            item.IsProcessing = true;
            StatusText = $"Processing {i + 1}/{total}: {item.FileName}";
            AppLog.Info("Pipeline", $"Processing [{i + 1}/{total}] {item.FileName}");

            var statusProgress = new Progress<ProcessingJobStatus>(s => item.UpdateFromJob(job));
            try
            {
                await Task.Run(() => App.Pipeline.ProcessAsync(job, statusProgress, ct), ct)
                    .ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                AppLog.Warn("Pipeline", "Processing cancelled by user");
                break;
            }
            catch (Exception ex)
            {
                AppLog.Error("Pipeline", $"Failed processing {item.FileName}", ex);
            }

            // Update UI on captured context
            await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
            {
                item.UpdateFromJob(job);
                if (job.Status == ProcessingJobStatus.Completed)
                    CompletedCount++;
                Progress = (double)(i + 1) / total * 100;
            });
        }

        await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
        {
            IsProcessing = false;
            var cancelled = ct.IsCancellationRequested;
            StatusText = cancelled
                ? $"Cancelled — {CompletedCount}/{total} completed"
                : $"Done — {CompletedCount}/{total} succeeded";
            AppLog.Info("Pipeline", StatusText);
        });

        _processingCts?.Dispose();
        _processingCts = null;
    }

    private void LoadParameterEditor(IProcessor? processor, IReadOnlyDictionary<string, object>? sourceValues = null)
    {
        // Save and restore _isLoadingItemSettings so ParameterItemViewModel constructors cannot fire
        // PersistSelectedProcessorDefaults mid-loop. Without this guard the partial ParameterValues dict
        // written on the first iteration mutates the processorDefaults object (same reference) and destroys
        // stored values for all subsequent parameters, causing them to revert to their hard-coded defaults.
        var wasLoading = _isLoadingItemSettings;
        _isLoadingItemSettings = true;
        try
        {
            CurrentParameters.Clear();
            CurrentOutputParameters.Clear();
            ParameterValues.Clear();

            var activeProcessor = processor;
            if (activeProcessor is null)
            {
                HasParameters = false;
                HasOutputParameters = false;
                return;
            }

            var knownParameters = GetKnownParameters(activeProcessor);
            var activeOutputParameters = GetActiveParameters(activeProcessor, ResolveParameterEditorOutputFormat(activeProcessor));

            // Always load processor defaults, which act as fallback when per-item overrides are missing or stale.
            var processorDefaults = GetProcessorDefaults(activeProcessor.Id);

            foreach (var param in knownParameters)
            {
                if (TryResolveParameterValue(sourceValues, param, processorDefaults, out var value))
                    ParameterValues[param.Id] = value;
            }

            AddParameterEditors(CurrentParameters, activeProcessor.Parameters);
            AddParameterEditors(CurrentOutputParameters, activeOutputParameters);

            HasParameters = CurrentParameters.Count > 0;
            HasOutputParameters = CurrentOutputParameters.Count > 0;
        }
        finally
        {
            _isLoadingItemSettings = wasLoading;
        }
    }

    private void AddParameterEditors(ObservableCollection<ParameterItemViewModel> target, IEnumerable<ProcessorParameter> parameters)
    {
        foreach (var param in parameters)
        {
            target.Add(new ParameterItemViewModel(param, ParameterValues.TryGetValue(param.Id, out var currentValue) ? currentValue : param.DefaultValue, (id, value) =>
            {
                ParameterValues[id] = value;
                if (!_isLoadingItemSettings)
                {
                    foreach (var selectedItem in EnumerateEditableSelection())
                    {
                        selectedItem.ParameterOverrides ??= [];
                        selectedItem.ParameterOverrides[id] = value;
                    }

                    PersistSelectedProcessorDefaults();
                }
            }));
        }
    }

    private void ReloadParameterEditorForCurrentContext()
    {
        var processor = SelectedTextureItem is not null
            ? ResolveProcessorForItem(SelectedTextureItem)
            : SelectedProcessor;
        if (processor is null)
            return;

        var sourceValues = SelectedTextureItem?.ParameterOverrides
            ?? new Dictionary<string, object>(ParameterValues, StringComparer.Ordinal);

        LoadParameterEditor(processor, sourceValues);

        if (SelectedTextureItem is not null)
            SelectedTextureItem.ParameterOverrides = new Dictionary<string, object>(ParameterValues, StringComparer.Ordinal);
    }

    private IProcessor ResolveProcessorForItem(TextureItemViewModel item)
    {
        if (item.ProcessorOverrideId is { } overrideId)
        {
            var overrideProcessor = Processors.FirstOrDefault(p => p.Id == overrideId);
            if (overrideProcessor is not null)
                return overrideProcessor;
        }

        var defaultId = App.SettingsManager.Current.LastProcessorId ?? "builtin.baseline-file-convert";
        return Processors.FirstOrDefault(p => p.Id == defaultId)
            ?? Processors.FirstOrDefault()
            ?? throw new InvalidOperationException("No processors are registered.");
    }

    private void EnsureItemSettings(TextureItemViewModel item, IProcessor processor)
    {
        item.ProcessorOverrideId = processor.Id;
        item.ProcessorDisplayName = processor.Name;
        item.ParameterOverrides = FilterParameterOverrides(processor, item.ParameterOverrides);
    }

    private IProcessor ResolveInitialProcessor(string inputPath)
    {
        var detected = Processors
            .OfType<IAutoDetectProcessor>()
            .FirstOrDefault(candidate => candidate.CanConvert(inputPath));
        if (detected is not null)
            return detected;

        return SelectedProcessor
            ?? Processors.FirstOrDefault(p => p.Id == App.SettingsManager.Current.LastProcessorId)
            ?? Processors.FirstOrDefault(p => p.Id == "builtin.baseline-file-convert")
            ?? Processors.FirstOrDefault()
            ?? throw new InvalidOperationException("No processors are registered.");
    }

    private void ApplyItemDefaults(TextureItemViewModel item, IProcessor processor)
    {
        item.ProcessorOverrideId = processor.Id;
        item.ProcessorDisplayName = processor.Name;
        item.ParameterOverrides = CreateParameterSnapshot(processor, defaults: GetProcessorDefaults(processor.Id));
        item.OutputFormatOverride = ResolveEffectiveOutputFormat(null, processor.Id);
        item.OutputDirectoryOverride = ResolveEffectiveOutputDirectory(null, processor.Id);
        item.OutputSuffixOverride = ResolveEffectiveOutputSuffix(null, processor.Id);
        item.GpuColorSpaceOverride = ResolveEffectiveGpuColorSpaceSelection(null, processor.Id);
    }

    private void LoadOutputEditor(TextureItemViewModel item)
    {
        var processorId = ResolveProcessorForItem(item).Id;
        OutputFormat = ResolveEffectiveOutputFormat(item, processorId);
        OutputDirectory = ResolveEffectiveOutputDirectory(item, processorId);
        OutputSuffix = ResolveEffectiveOutputSuffix(item, processorId);
        GpuColorSpace = ResolveEffectiveGpuColorSpaceSelection(item, processorId);
        item.OutputPath = ResolveOutputPath(item);
    }

    private void LoadOutputEditorForProcessor(IProcessor processor)
    {
        _isLoadingItemSettings = true;
        try
        {
            OutputFormat = ResolveEffectiveOutputFormat(null, processor.Id);
            OutputDirectory = ResolveEffectiveOutputDirectory(null, processor.Id);
            OutputSuffix = ResolveEffectiveOutputSuffix(null, processor.Id);
            GpuColorSpace = ResolveEffectiveGpuColorSpaceSelection(null, processor.Id);
        }
        finally
        {
            _isLoadingItemSettings = false;
        }
    }

    private void SaveSelectedItemOutputSettings(Action<TextureItemViewModel> apply)
    {
        SaveSelectedItemSettings(apply, updateOutputPath: true);
    }

    private void SaveSelectedItemSettings(Action<TextureItemViewModel> apply)
    {
        SaveSelectedItemSettings(apply, updateOutputPath: false);
    }

    private void SaveSelectedItemSettings(Action<TextureItemViewModel> apply, bool updateOutputPath)
    {
        if (_isLoadingItemSettings)
            return;

        foreach (var item in EnumerateEditableSelection())
        {
            apply(item);
            if (updateOutputPath)
                item.OutputPath = ResolveOutputPath(item);
        }
    }

    public void SetSelectedItems(IReadOnlyList<TextureItemViewModel> items, bool syncView = true)
    {
        ApplySelectedItems(items);

        if (syncView)
            SelectionSyncRequested?.Invoke([.. SelectedTextureItems], false);
    }

    public void SetSelectedItemsFromView(IReadOnlyList<TextureItemViewModel> items)
    {
        ApplySelectedItems(items);
    }

    private void ApplySelectedItems(IReadOnlyList<TextureItemViewModel> items)
    {
        SelectedTextureItems.Clear();
        foreach (var item in items)
            SelectedTextureItems.Add(item);

        var primary = SelectedTextureItems.LastOrDefault();
        if (!ReferenceEquals(SelectedTextureItem, primary))
            SelectedTextureItem = primary;
    }

    private ObservableCollection<TextureItemViewModel> EnumerateEditableSelection()
    {
        if (SelectedTextureItems.Count > 0)
            return SelectedTextureItems;

        return SelectedTextureItem is { } item ? [item] : [];
    }

    private void RefreshItemOutputPaths()
    {
        if (Processors.Count == 0)
            return;

        foreach (var item in TextureItems)
            item.OutputPath = ResolveOutputPath(item);
    }

    private static bool TryResolveParameterValue(IReadOnlyDictionary<string, object>? values, ProcessorParameter parameter, ProcessorDefaults? defaults, out object resolvedValue)
    {
        if (values is not null && values.TryGetValue(parameter.Id, out var value) && value is not null)
        {
            resolvedValue = value;
            return true;
        }

        if (TryResolveStoredParameterValue(parameter, defaults, out var stored))
        {
            resolvedValue = stored;
            return true;
        }

        if (parameter.DefaultValue is not null)
        {
            resolvedValue = parameter.DefaultValue;
            return true;
        }

        resolvedValue = null!;
        return false;
    }

    private static Dictionary<string, object> CreateParameterSnapshot(IProcessor processor, IReadOnlyDictionary<string, object>? sourceValues = null, ProcessorDefaults? defaults = null)
    {
        var values = new Dictionary<string, object>(StringComparer.Ordinal);
        foreach (var param in GetKnownParameters(processor))
        {
            if (TryResolveParameterValue(sourceValues, param, defaults, out var value))
                values[param.Id] = value;
        }

        return values;
    }

    private static Dictionary<string, object> FilterParameterOverrides(IProcessor processor, Dictionary<string, object>? sourceValues)
    {
        var filtered = new Dictionary<string, object>(StringComparer.Ordinal);
        if (sourceValues is null)
            return filtered;

        foreach (var parameter in GetKnownParameters(processor))
        {
            if (sourceValues.TryGetValue(parameter.Id, out var value) && value is not null)
                filtered[parameter.Id] = value;
        }

        return filtered;
    }

    private static void EnsureProcessorHotkeySlots(TextureHawk.Settings.AppSettings settings)
    {
        while (settings.ProcessorHotkeySlots.Count < 6)
            settings.ProcessorHotkeySlots.Add(null);
    }

    private string ResolveOutputPath(TextureItemViewModel item)
    {
        var processorId = ResolveProcessorForItem(item).Id;
        var outputDirectory = ResolveEffectiveOutputDirectory(item, processorId);
        var outputSuffix = ResolveEffectiveOutputSuffix(item, processorId);
        var outputFormat = ResolveEffectiveOutputFormat(item, processorId);
        var dir = string.IsNullOrEmpty(outputDirectory)
            ? Path.GetDirectoryName(item.FilePath) ?? "."
            : outputDirectory;

        var nameWithoutExt = Path.GetFileNameWithoutExtension(item.FilePath);
        return Path.Combine(dir, $"{nameWithoutExt}{outputSuffix}{outputFormat}");
    }

    private void RefreshOutputFormatOptions()
    {
        OutputFormatOptions.Clear();
        foreach (var extension in ImageFormatRegistryService.GetWritableExtensions(App.TranslatorManager))
            OutputFormatOptions.Add(extension);
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
        if (!extension.StartsWith('.'))
            extension = $".{extension}";

        return extension.ToLowerInvariant();
    }

    private string ResolveEffectiveOutputFormat(TextureItemViewModel? item, string processorId)
    {
        var settings = App.SettingsManager.Current;
        var defaults = GetProcessorDefaults(processorId);
        if (item?.OutputFormatOverride is { } itemOverride)
            return ResolveSupportedOutputFormat(itemOverride);

        var resolved = settings.UseProcessorDefaultOutputFormat
            ? defaults?.OutputFormat ?? settings.DefaultOutputFormat
            : settings.DefaultOutputFormat;
        return ResolveSupportedOutputFormat(resolved);
    }

    private static string ResolveEffectiveOutputDirectory(TextureItemViewModel? item, string processorId)
    {
        var settings = App.SettingsManager.Current;
        var defaults = GetProcessorDefaults(processorId);
        if (item?.OutputDirectoryOverride is { } itemOverride)
            return itemOverride;

        return settings.UseProcessorDefaultOutputDirectory
            ? defaults?.OutputDirectory ?? settings.OutputDirectory
            : settings.OutputDirectory;
    }

    private static string ResolveEffectiveOutputSuffix(TextureItemViewModel? item, string processorId)
    {
        var settings = App.SettingsManager.Current;
        var defaults = GetProcessorDefaults(processorId);
        if (item?.OutputSuffixOverride is { } itemOverride)
            return itemOverride;

        return settings.UseProcessorDefaultOutputSuffix
            ? defaults?.OutputSuffix ?? settings.OutputSuffix
            : settings.OutputSuffix;
    }

    private static string ResolveEffectiveGpuColorSpaceSelection(TextureItemViewModel? item, string processorId)
    {
        var settings = App.SettingsManager.Current;
        var defaults = GetProcessorDefaults(processorId);
        if (item?.GpuColorSpaceOverride is { } itemOverride)
            return ResolveGpuColorSpaceSelection(itemOverride, settings.DefaultGpuColorSpaceMode);

        var processorSelection = settings.UseProcessorDefaultGpuColorSpaceMode
            ? defaults?.GpuColorSpaceMode
            : null;
        return ResolveGpuColorSpaceSelection(processorSelection, settings.DefaultGpuColorSpaceMode);
    }

    private static string ResolveGpuColorSpaceSelection(string? selection, string? fallbackSelection)
    {
        var fallbackMode = GpuColorSpaceModeNames.ParseOrDefault(fallbackSelection, GpuColorSpaceMode.Auto);
        return GpuColorSpaceModeNames.NormalizeOrDefault(selection, fallbackMode);
    }

    private static GpuColorSpaceMode ResolveJobGpuColorSpaceMode(TextureItemViewModel item, string processorId)
    {
        var settings = App.SettingsManager.Current;
        var defaults = GetProcessorDefaults(processorId);
        if (item.GpuColorSpaceOverride is { } itemOverride)
            return GpuColorSpaceModeNames.ParseOrDefault(itemOverride, GpuColorSpaceMode.Auto);

        var selection = settings.UseProcessorDefaultGpuColorSpaceMode
            ? defaults?.GpuColorSpaceMode ?? settings.DefaultGpuColorSpaceMode
            : settings.DefaultGpuColorSpaceMode;
        return GpuColorSpaceModeNames.ParseOrDefault(selection, GpuColorSpaceMode.Auto);
    }

    private static ProcessorDefaults? GetProcessorDefaults(string processorId)
    {
        var settings = App.SettingsManager.Current;
        return settings.ProcessorDefaults.TryGetValue(processorId, out var defaults)
            ? defaults
            : null;
    }

    private void PersistSelectedProcessorDefaults()
    {
        if (_isLoadingItemSettings || SelectedProcessor is null)
            return;

        var settings = App.SettingsManager.Current;
        var outputDirectory = NormalizeProcessorDirectoryOverride(OutputDirectory, settings.OutputDirectory);
        var outputSuffix = NormalizeProcessorOptionalTextOverride(OutputSuffix, settings.OutputSuffix, StringComparison.Ordinal);
        var outputFormat = NormalizeProcessorTextOverride(ResolveSupportedOutputFormat(OutputFormat), ResolveSupportedOutputFormat(settings.DefaultOutputFormat), StringComparison.OrdinalIgnoreCase);
        var gpuColorSpace = NormalizeProcessorTextOverride(ResolveGpuColorSpaceSelection(GpuColorSpace, settings.DefaultGpuColorSpaceMode), GpuColorSpaceModeNames.NormalizeOrDefault(settings.DefaultGpuColorSpaceMode, GpuColorSpaceMode.Auto), StringComparison.Ordinal);
        var parameterValues = SerializeParameterValues(SelectedProcessor, ParameterValues);

        if (outputDirectory is null && outputSuffix is null && outputFormat is null && gpuColorSpace is null && parameterValues.Count == 0)
        {
            settings.ProcessorDefaults.Remove(SelectedProcessor.Id);
            App.SettingsManager.Save();
            return;
        }

        if (!settings.ProcessorDefaults.TryGetValue(SelectedProcessor.Id, out var defaults))
        {
            defaults = new ProcessorDefaults();
            settings.ProcessorDefaults[SelectedProcessor.Id] = defaults;
        }

        defaults.OutputDirectory = outputDirectory;
        defaults.OutputSuffix = outputSuffix;
        defaults.OutputFormat = outputFormat;
        defaults.GpuColorSpaceMode = gpuColorSpace;
        defaults.ParameterValues = parameterValues;

        App.SettingsManager.Save();
    }

    private static string? NormalizeProcessorDirectoryOverride(string value, string fallbackValue)
    {
        var normalizedValue = value.Trim();
        var normalizedFallback = string.IsNullOrWhiteSpace(fallbackValue) ? string.Empty : fallbackValue.Trim();
        return string.Equals(normalizedValue, normalizedFallback, StringComparison.OrdinalIgnoreCase)
            ? null
            : normalizedValue;
    }

    private static string? NormalizeProcessorTextOverride(string value, string fallbackValue, StringComparison comparison)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var normalizedValue = value.Trim();
        var normalizedFallback = fallbackValue.Trim();
        return string.Equals(normalizedValue, normalizedFallback, comparison)
            ? null
            : normalizedValue;
    }

    private static string? NormalizeProcessorOptionalTextOverride(string value, string fallbackValue, StringComparison comparison)
    {
        var normalizedValue = value.Trim();
        var normalizedFallback = fallbackValue.Trim();
        return string.Equals(normalizedValue, normalizedFallback, comparison)
            ? null
            : normalizedValue;
    }

    private static Dictionary<string, string> SerializeParameterValues(IProcessor processor, Dictionary<string, object> values)
    {
        var serialized = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var parameter in GetKnownParameters(processor))
        {
            if (!values.TryGetValue(parameter.Id, out var value) || value is null)
                continue;

            if (parameter.Type == ProcessorParameterType.FilePath)
                continue;

            var serializedValue = SerializeParameterValue(value, parameter.Type);
            if (LooksLikePath(serializedValue))
                continue;

            serialized[parameter.Id] = serializedValue;
        }

        return serialized;
    }

    private static IReadOnlyList<ProcessorParameter> GetKnownParameters(IProcessor processor)
    {
        return MergeParameters(processor.Parameters, OutputWriteParameterCatalog.GetAllParameters());
    }

    private static IReadOnlyList<ProcessorParameter> GetActiveParameters(IProcessor processor, string outputFormat)
    {
        return OutputWriteParameterCatalog.GetParametersForOutputFormat(outputFormat);
    }

    private string ResolveParameterEditorOutputFormat(IProcessor processor)
    {
        if (SelectedTextureItem is { } selectedItem)
            return ResolveEffectiveOutputFormat(selectedItem, processor.Id);

        return ResolveSupportedOutputFormat(OutputFormat);
    }

    private static IReadOnlyList<ProcessorParameter> MergeParameters(IEnumerable<ProcessorParameter> primary, IEnumerable<ProcessorParameter> secondary)
    {
        var merged = new List<ProcessorParameter>();
        var seen = new HashSet<string>(StringComparer.Ordinal);

        foreach (var parameter in primary.Concat(secondary))
        {
            if (seen.Add(parameter.Id))
                merged.Add(parameter);
        }

        return merged;
    }

    private static string SerializeParameterValue(object value, ProcessorParameterType parameterType)
    {
        return parameterType switch
        {
            ProcessorParameterType.Boolean => value is bool b ? b ? "true" : "false" : "false",
            ProcessorParameterType.Integer => Convert.ToInt32(value, CultureInfo.InvariantCulture).ToString(CultureInfo.InvariantCulture),
            ProcessorParameterType.Float => Convert.ToSingle(value, CultureInfo.InvariantCulture).ToString(CultureInfo.InvariantCulture),
            _ => value.ToString() ?? string.Empty
        };
    }

    private static bool TryResolveStoredParameterValue(ProcessorParameter parameter, ProcessorDefaults? defaults, out object resolved)
    {
        if (parameter.Type == ProcessorParameterType.FilePath)
        {
            resolved = null!;
            return false;
        }

        if (defaults is null || !defaults.ParameterValues.TryGetValue(parameter.Id, out var serialized))
        {
            resolved = null!;
            return false;
        }

        switch (parameter.Type)
        {
            case ProcessorParameterType.Boolean:
                if (bool.TryParse(serialized, out var boolValue))
                {
                    resolved = boolValue;
                    return true;
                }
                break;

            case ProcessorParameterType.Integer:
                if (int.TryParse(serialized, NumberStyles.Integer, CultureInfo.InvariantCulture, out var intValue))
                {
                    resolved = intValue;
                    return true;
                }
                break;

            case ProcessorParameterType.Float:
                if (float.TryParse(serialized, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out var floatValue))
                {
                    resolved = floatValue;
                    return true;
                }
                break;

            case ProcessorParameterType.Choice:
                if (parameter.Options is { Count: > 0 })
                {
                    var matched = parameter.Options.FirstOrDefault(option =>
                        string.Equals(option, serialized, StringComparison.OrdinalIgnoreCase));

                    resolved = matched ?? parameter.Options[0];
                    return true;
                }

                resolved = serialized;
                return true;

            case ProcessorParameterType.ImageFormat:
                if (Enum.TryParse(serialized, true, out RedFox.Graphics2D.ImageFormat imageFormat))
                {
                    resolved = imageFormat;
                    return true;
                }
                break;

            default:
                resolved = serialized;
                return true;
        }

        resolved = null!;
        return false;
    }

    private static bool LooksLikePath(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;

        if (value.IndexOfAny(Path.GetInvalidPathChars()) >= 0)
            return false;

        return Path.IsPathRooted(value)
            || value.Contains(Path.DirectorySeparatorChar)
            || value.Contains(Path.AltDirectorySeparatorChar);
    }
}
