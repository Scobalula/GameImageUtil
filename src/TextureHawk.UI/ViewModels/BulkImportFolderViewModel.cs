using CommunityToolkit.Mvvm.ComponentModel;

namespace TextureHawk.UI.ViewModels;

/// <summary>
/// ViewModel for folder bulk import preview and options.
/// </summary>
public sealed partial class BulkImportFolderViewModel : ObservableObject
{
    [ObservableProperty]
    public partial bool IncludeSubfolders { get; set; } = true;

    [ObservableProperty]
    public partial bool SupportedFormatsOnly { get; set; } = true;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanImport))]
    public partial bool IsEnumerating { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanImport))]
    public partial int CandidateCount { get; set; }

    [ObservableProperty]
    public partial string StatusText { get; set; } = "Preparing scan...";

    public BulkImportFolderViewModel(string folderPath)
    {
        FolderPath = folderPath;
    }

    /// <summary>
    /// Folder selected for bulk import.
    /// </summary>
    public string FolderPath { get; }

    /// <summary>
    /// Whether import action is currently allowed.
    /// </summary>
    public bool CanImport => !IsEnumerating && CandidateCount > 0;
}
