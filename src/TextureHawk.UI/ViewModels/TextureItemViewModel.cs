using CommunityToolkit.Mvvm.ComponentModel;
using TextureHawk.Processing;

namespace TextureHawk.UI.ViewModels;

/// <summary>
/// ViewModel for a single texture item in the processing queue.
/// </summary>
public sealed partial class TextureItemViewModel : ObservableObject
{
    /// <summary>
    /// Absolute path to the source texture file.
    /// </summary>
    [ObservableProperty]
    public partial string FilePath { get; set; }

    /// <summary>
    /// Display name (file name without directory).
    /// </summary>
    [ObservableProperty]
    public partial string FileName { get; set; }

    /// <summary>
    /// Current processing status text.
    /// </summary>
    [ObservableProperty]
    public partial string Status { get; set; } = "Pending";

    /// <summary>
    /// Resolved output file path.
    /// </summary>
    [ObservableProperty]
    public partial string OutputPath { get; set; } = "";

    /// <summary>
    /// Whether this item is currently being processed.
    /// </summary>
    [ObservableProperty]
    public partial bool IsProcessing { get; set; }

    /// <summary>
    /// Whether processing completed successfully.
    /// </summary>
    [ObservableProperty]
    public partial bool IsCompleted { get; set; }

    /// <summary>
    /// Whether processing failed.
    /// </summary>
    [ObservableProperty]
    public partial bool HasFailed { get; set; }

    /// <summary>
    /// Error message if processing failed, otherwise <see langword="null"/>.
    /// </summary>
    [ObservableProperty]
    public partial string? ErrorMessage { get; set; }

    /// <summary>
    /// Per-item processor override ID, or <see langword="null"/> to use the global processor.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasProcessorOverride))]
    public partial string? ProcessorOverrideId { get; set; }

    /// <summary>
    /// Display name of the per-item processor override.
    /// </summary>
    [ObservableProperty]
    public partial string ProcessorDisplayName { get; set; } = "";

    /// <summary>
    /// Whether this item has a per-item processor override.
    /// </summary>
    public bool HasProcessorOverride => ProcessorOverrideId is not null;

    /// <summary>
    /// Per-item parameter value overrides, or <see langword="null"/> to use the global parameters.
    /// </summary>
    public Dictionary<string, object>? ParameterOverrides { get; set; }

    /// <summary>
    /// Per-item output file extension override, or <see langword="null"/> to use the app default.
    /// </summary>
    [ObservableProperty]
    public partial string? OutputFormatOverride { get; set; }

    /// <summary>
    /// Per-item output directory override, or <see langword="null"/> to use the app default.
    /// </summary>
    [ObservableProperty]
    public partial string? OutputDirectoryOverride { get; set; }

    /// <summary>
    /// Per-item output suffix override, or <see langword="null"/> to use the app default.
    /// </summary>
    [ObservableProperty]
    public partial string? OutputSuffixOverride { get; set; }

    /// <summary>
    /// Per-item GPU color-space override, or <see langword="null"/> to use the processor or app default.
    /// </summary>
    [ObservableProperty]
    public partial string? GpuColorSpaceOverride { get; set; }

    /// <summary>
    /// Creates a new texture item for the specified file.
    /// </summary>
    public TextureItemViewModel(string filePath)
    {
        FilePath = filePath;
        FileName = Path.GetFileName(filePath);
    }

    partial void OnFilePathChanged(string value)
    {
        FileName = Path.GetFileName(value);
    }

    /// <summary>
    /// The uppercase file extension without the leading dot (e.g., "PNG", "DDS").
    /// </summary>
    public string FileExtension => Path.GetExtension(FilePath).ToUpperInvariant().TrimStart('.');

    /// <summary>
    /// Updates status from a processing job.
    /// </summary>
    public void UpdateFromJob(ProcessingJob job)
    {
        Status = job.Status.ToString();
        IsProcessing = job.Status is ProcessingJobStatus.Loading or ProcessingJobStatus.Processing or ProcessingJobStatus.Writing;
        IsCompleted = job.Status == ProcessingJobStatus.Completed;
        HasFailed = job.Status == ProcessingJobStatus.Failed;
        ErrorMessage = job.ErrorMessage;
    }
}
