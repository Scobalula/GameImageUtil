using System.Text.Json.Serialization;

namespace TextureHawk.Plugins;

/// <summary>
/// JSON manifest describing a plugin package.
/// </summary>
/// <remarks>
/// Each plugin directory must contain a <c>plugin.json</c> manifest
/// that describes the plugin metadata and where its processor definitions live.
/// </remarks>
public sealed class PluginManifest
{
    /// <summary>
    /// Manifest schema version.
    /// </summary>
    [JsonPropertyName("schemaVersion")]
    public int SchemaVersion { get; init; } = 1;

    /// <summary>
    /// Unique identifier for this plugin (e.g., "com.example.myplugin").
    /// </summary>
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    /// <summary>
    /// Human-readable display name.
    /// </summary>
    [JsonPropertyName("name")]
    public required string Name { get; init; }

    /// <summary>
    /// Semantic version string (e.g., "1.0.0").
    /// </summary>
    [JsonPropertyName("version")]
    public required string Version { get; init; }

    /// <summary>
    /// Author or organization that created the plugin.
    /// </summary>
    [JsonPropertyName("author")]
    public string Author { get; init; } = "Unknown";

    /// <summary>
    /// Brief description of what the plugin provides.
    /// </summary>
    [JsonPropertyName("description")]
    public string Description { get; init; } = "";

    /// <summary>
    /// Relative directory containing processor definition files.
    /// </summary>
    [JsonPropertyName("processorsDirectory")]
    public string ProcessorsDirectory { get; init; } = "processors";

    /// <summary>
    /// Relative directory containing plugin shader source files.
    /// </summary>
    [JsonPropertyName("shadersDirectory")]
    public string ShadersDirectory { get; init; } = "shaders";
}
