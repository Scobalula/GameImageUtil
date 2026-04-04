namespace TextureHawk.Updates;

/// <summary>
/// Abstraction for checking for application updates from various sources.
/// </summary>
/// <remarks>
/// Implementations may check GitHub releases, a custom API, Cloudflare Workers, etc.
/// The mechanism is intentionally decoupled from any specific provider.
/// </remarks>
public interface IUpdateProvider
{
    /// <summary>
    /// Display name of this update source.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Checks if a newer version is available.
    /// </summary>
    /// <param name="currentVersion">The currently running version.</param>
    /// <param name="includePreRelease">Whether to include pre-release versions.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Update info if available, null if up to date.</returns>
    Task<UpdateInfo?> CheckForUpdateAsync(Version currentVersion, bool includePreRelease, CancellationToken cancellationToken);
}
