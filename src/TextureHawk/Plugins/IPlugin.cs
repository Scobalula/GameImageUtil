using TextureHawk.Processing;

namespace TextureHawk.Plugins;

/// <summary>
/// Interface that plugins must implement to provide custom processors.
/// </summary>
/// <remarks>
/// Plugins are loaded via <see cref="PluginLoader"/> using isolated AssemblyLoadContexts,
/// enabling hot-reload and unload at runtime without restarting the application.
/// </remarks>
public interface IPlugin
{
    /// <summary>
    /// Unique identifier for this plugin.
    /// </summary>
    string Id { get; }

    /// <summary>
    /// Display name of the plugin.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Version string (e.g., "1.0.0").
    /// </summary>
    string Version { get; }

    /// <summary>
    /// Author of the plugin.
    /// </summary>
    string Author { get; }

    /// <summary>
    /// Brief description.
    /// </summary>
    string Description { get; }

    /// <summary>
    /// Called when the plugin is loaded. Register processors and resources here.
    /// </summary>
    void Initialize(ProcessorRegistry registry);

    /// <summary>
    /// Called when the plugin is unloaded. Clean up resources here.
    /// </summary>
    void Shutdown();

    /// <summary>
    /// Returns all processors provided by this plugin.
    /// </summary>
    IReadOnlyList<IProcessor> GetProcessors();
}
