using CommunityToolkit.Mvvm.ComponentModel;

namespace TextureHawk.UI.ViewModels;

/// <summary>
/// ViewModel for a single plugin item in the plugin manager.
/// </summary>
public sealed partial class PluginItemViewModel : ObservableObject
{
    /// <summary>
    /// Unique plugin identifier.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Human-readable plugin name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Plugin version string.
    /// </summary>
    public required string Version { get; init; }

    /// <summary>
    /// Plugin author or organization.
    /// </summary>
    public string Author { get; init; } = "";

    /// <summary>
    /// Brief description of the plugin.
    /// </summary>
    public string Description { get; init; } = "";

    /// <summary>
    /// Filesystem directory the plugin was loaded from.
    /// </summary>
    public string Directory { get; init; } = "";

    /// <summary>
    /// Number of processors contributed by the package.
    /// </summary>
    public int ProcessorCount { get; init; }
}
