using System.Reflection;
using TextureHawk.Updates;

namespace TextureHawk.UI.Services;

internal static class UpdateCheckService
{
    public static Version CurrentVersion => Assembly.GetEntryAssembly()?.GetName().Version ?? new Version(0, 1, 0);

    public static async Task<UpdateInfo?> CheckForUpdateAsync(CancellationToken cancellationToken)
    {
        var provider = App.SettingsManager.Current.UpdateProvider switch
        {
            "github" => new GitHubUpdateProvider("Scobalula", "TextureHawk"),
            _ => new GitHubUpdateProvider("Scobalula", "TextureHawk")
        };

        return await provider.CheckForUpdateAsync(CurrentVersion, App.SettingsManager.Current.IncludePreReleases, cancellationToken);
    }
}