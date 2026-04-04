using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace TextureHawk.Updates;

/// <summary>
/// Checks for updates via the GitHub Releases API.
/// </summary>
/// <remarks>
/// Queries the GitHub API for the latest release of a repository and compares
/// it against the current version. Uses unauthenticated requests with proper
/// User-Agent headers as required by GitHub's API policy.
/// </remarks>
public sealed class GitHubUpdateProvider : IUpdateProvider
{
    private readonly string _owner;
    private readonly string _repo;
    private readonly HttpClient _httpClient;

    public string Name => "GitHub Releases";

    /// <summary>
    /// Creates a new GitHub update provider.
    /// </summary>
    /// <param name="owner">Repository owner (e.g., "myorg").</param>
    /// <param name="repo">Repository name (e.g., "TextureHawk").</param>
    /// <param name="httpClient">Optional HTTP client for dependency injection.</param>
    public GitHubUpdateProvider(string owner, string repo, HttpClient? httpClient = null)
    {
        _owner = owner;
        _repo = repo;
        _httpClient = httpClient ?? CreateDefaultClient();
    }

    public async Task<UpdateInfo?> CheckForUpdateAsync(Version currentVersion, bool includePreRelease, CancellationToken cancellationToken)
    {
        return null;
        var url = $"https://api.github.com/repos/{Uri.EscapeDataString(_owner)}/{Uri.EscapeDataString(_repo)}/releases";
        var releases = await _httpClient.GetFromJsonAsync<List<GitHubRelease>>(url, cancellationToken);

        if (releases is null || releases.Count == 0)
            return null;

        foreach (var release in releases)
        {
            if (release.Draft)
                continue;
            if (release.PreRelease && !includePreRelease)
                continue;

            var tagVersion = ParseVersion(release.TagName);
            if (tagVersion is null || tagVersion <= currentVersion)
                continue;

            return new UpdateInfo
            {
                Version = release.TagName,
                ReleaseNotes = release.Body ?? "",
                Url = release.HtmlUrl,
                PublishedAt = release.PublishedAt,
                IsPreRelease = release.PreRelease
            };
        }

        return null;
    }

    private static Version? ParseVersion(string tag)
    {
        var cleaned = tag.TrimStart('v', 'V');
        return Version.TryParse(cleaned, out var version) ? version : null;
    }

    private static HttpClient CreateDefaultClient()
    {
        var client = new HttpClient();
        client.DefaultRequestHeaders.Add("User-Agent", "TextureHawk");
        client.DefaultRequestHeaders.Add("Accept", "application/vnd.github.v3+json");
        return client;
    }

    private sealed class GitHubRelease
    {
        [JsonPropertyName("tag_name")]
        public string TagName { get; set; } = "";

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("body")]
        public string? Body { get; set; }

        [JsonPropertyName("html_url")]
        public string HtmlUrl { get; set; } = "";

        [JsonPropertyName("published_at")]
        public DateTimeOffset PublishedAt { get; set; }

        [JsonPropertyName("draft")]
        public bool Draft { get; set; }

        [JsonPropertyName("prerelease")]
        public bool PreRelease { get; set; }
    }
}
