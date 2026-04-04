namespace TextureHawk.Plugins;

/// <summary>
/// Represents a loaded plugin with its runtime context.
/// </summary>
/// <param name="Manifest">The parsed plugin manifest from <c>plugin.json</c>.</param>
/// <param name="Processors">Processors loaded from the package definition files.</param>
/// <param name="Directory">The directory the plugin was loaded from.</param>
public sealed record LoadedPlugin(PluginManifest Manifest, IReadOnlyList<DefinedProcessor> Processors, string Directory);
