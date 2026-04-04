using System.Reflection;
using CommunityToolkit.Mvvm.ComponentModel;
using TextureHawk.UI.Services;
using TextureHawk.Updates;

namespace TextureHawk.UI.ViewModels;

/// <summary>
/// Represents the view model for the application's About dialog.
/// </summary>
public sealed partial class AboutViewModel : ObservableObject
{
    /// <summary>
    /// Gets the most recent update information, or null if no updates have occurred.
    /// </summary>
    public UpdateInfo? LatestUpdate { get; private set; }

    /// <summary>
    /// Status message from the update check operation.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasUpdateStatus))]
    public partial string UpdateStatus { get; set; } = "";

    /// <summary>
    /// Whether an update check is currently in progress.
    /// </summary>
    [ObservableProperty]
    public partial bool IsCheckingForUpdate { get; set; }

    /// <summary>
    /// Application display name.
    /// </summary>
    public static string AppName => "TextureHawk";

    /// <summary>
    /// Current application version string.
    /// </summary>
    public static string Version => UpdateCheckService.CurrentVersion.ToString(3);

    /// <summary>
    /// Brief application description.
    /// </summary>
    public static string Description => "TextureHawk is a GPU-based image/texture processing tool.";

    /// <summary>
    /// Copyright notice.
    /// </summary>
    public static string Copyright => $"© {DateTime.Now.Year} Scobalula";

    /// <summary>
    /// Whether there is an update status message to display.
    /// </summary>
    public bool HasUpdateStatus => !string.IsNullOrEmpty(UpdateStatus);

    /// <summary>
    /// Asynchronously checks for available software updates and updates the status properties to reflect the result.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task CheckForUpdateAsync()
    {
        if (IsCheckingForUpdate)
            return;

        IsCheckingForUpdate = true;
        UpdateStatus = "Checking for updates...";

        try
        {
            LatestUpdate = await UpdateCheckService.CheckForUpdateAsync(default);
            UpdateStatus = LatestUpdate is not null ? $"Update available: {LatestUpdate.Version}" : "You're up to date!";
        }
        catch (Exception ex)
        {
            UpdateStatus = $"Failed to check: {ex.Message}";
        }
        finally
        {
            IsCheckingForUpdate = false;
        }
    }
}
