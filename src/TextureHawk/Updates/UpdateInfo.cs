namespace TextureHawk.Updates;

/// <summary>
/// Information about an available update.
/// </summary>
public sealed class UpdateInfo
{
    /// <summary>
    /// The version string of the available update.
    /// </summary>
    public required string Version { get; init; }

    /// <summary>
    /// Release notes or changelog.
    /// </summary>
    public string ReleaseNotes { get; init; } = "";

    /// <summary>
    /// URL to download the update or view the release.
    /// </summary>
    public required string Url { get; init; }

    /// <summary>
    /// When the release was published.
    /// </summary>
    public DateTimeOffset PublishedAt { get; init; }

    /// <summary>
    /// Whether this is a pre-release version.
    /// </summary>
    public bool IsPreRelease { get; init; }
}
