using TextureHawk.Updates;

namespace TextureHawk.UI.ViewModels;

public sealed class UpdateNotificationViewModel(UpdateInfo update)
{
    public UpdateInfo Update { get; } = update;

    public string Title => $"Update Available: {Update.Version}";

    public string ReleaseNotes => string.IsNullOrWhiteSpace(Update.ReleaseNotes)
        ? "No release notes were provided."
        : Update.ReleaseNotes;

    public string PublishedText => Update.PublishedAt == default
        ? string.Empty
        : $"Published {Update.PublishedAt:yyyy-MM-dd}";

    public bool HasPublishedText => !string.IsNullOrEmpty(PublishedText);

    public bool IsSkippedBySettings => string.Equals(App.SettingsManager.Current.SkippedUpdateVersion, Update.Version, StringComparison.OrdinalIgnoreCase);
}