using System.Text.Json;
using System.IO.Compression;
using TextureHawk.Processing;

namespace TextureHawk.Plugins;

/// <summary>
/// Manages loading and unloading of definition-based plugin packages.
/// </summary>
public sealed class PluginLoader
{
    private readonly Dictionary<string, LoadedPlugin> _plugins = [];
    private readonly ProcessorRegistry _registry;

    public PluginLoader(ProcessorRegistry registry)
    {
        _registry = registry;
    }

    /// <summary>
    /// Gets all currently loaded plugins.
    /// </summary>
    public IReadOnlyCollection<LoadedPlugin> LoadedPlugins => _plugins.Values;

    /// <summary>
    /// Loads a plugin from the specified directory.
    /// </summary>
    /// <param name="pluginDirectory">
    /// Path to the plugin directory containing a <c>plugin.json</c> manifest.
    /// </param>
    public LoadedPlugin LoadPlugin(string pluginDirectory)
    {
        var manifest = ReadManifest(pluginDirectory);

        if (_plugins.ContainsKey(manifest.Id))
            throw new InvalidOperationException($"Plugin '{manifest.Id}' is already loaded.");

        var processors = DefinedProcessor.LoadFromDirectory(
            Path.Combine(pluginDirectory, manifest.ProcessorsDirectory),
            Path.Combine(pluginDirectory, manifest.ShadersDirectory));

        if (processors.Count == 0)
            throw new InvalidOperationException($"Plugin '{manifest.Id}' did not contain any processor definitions.");

        foreach (var processor in processors)
            _registry.Register(processor);

        var loaded = new LoadedPlugin(manifest, processors, pluginDirectory);
        _plugins[manifest.Id] = loaded;
        return loaded;
    }

    /// <summary>
    /// Installs a plugin into the managed plugins directory and loads it.
    /// </summary>
    public LoadedPlugin InstallPlugin(string sourceDirectory, string managedPluginsDirectory)
    {
        var manifest = ReadManifest(sourceDirectory);
        var targetDirectory = Path.Combine(managedPluginsDirectory, manifest.Id);

        if (_plugins.ContainsKey(manifest.Id))
            UnloadPlugin(manifest.Id);

        if (Directory.Exists(targetDirectory))
            Directory.Delete(targetDirectory, true);

        CopyDirectory(sourceDirectory, targetDirectory);
        return LoadPlugin(targetDirectory);
    }

    /// <summary>
    /// Installs a plugin from a zip package into the managed plugins directory and loads it.
    /// </summary>
    public LoadedPlugin InstallPluginFromZip(string zipPath, string managedPluginsDirectory)
    {
        if (!File.Exists(zipPath))
            throw new FileNotFoundException($"Plugin package not found: {zipPath}");

        var tempRoot = Path.Combine(Path.GetTempPath(), "TextureHawk", "plugin-install", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempRoot);

        try
        {
            ZipFile.ExtractToDirectory(zipPath, tempRoot, true);
            var pluginDirectory = FindPluginRoot(tempRoot)
                ?? throw new InvalidOperationException($"Plugin package did not contain plugin.json: {zipPath}");
            return InstallPlugin(pluginDirectory, managedPluginsDirectory);
        }
        finally
        {
            if (Directory.Exists(tempRoot))
                Directory.Delete(tempRoot, true);
        }
    }

    /// <summary>
    /// Unloads a plugin and removes its processors.
    /// </summary>
    public bool UnloadPlugin(string pluginId)
    {
        if (!_plugins.Remove(pluginId, out var loaded))
            return false;

        foreach (var processor in loaded.Processors)
            _registry.Unregister(processor.Id);
        return true;
    }

    /// <summary>
    /// Removes a managed plugin package from disk after unloading it.
    /// </summary>
    public bool UninstallPlugin(string pluginId, string managedPluginsDirectory)
    {
        var hadLoadedPlugin = UnloadPlugin(pluginId);
        var path = Path.Combine(managedPluginsDirectory, pluginId);
        if (Directory.Exists(path))
        {
            Directory.Delete(path, true);
            return true;
        }

        return hadLoadedPlugin;
    }

    /// <summary>
    /// Scans a directory for plugin subdirectories and loads all found plugins.
    /// </summary>
    public List<LoadedPlugin> LoadAllFromDirectory(string pluginsDirectory)
    {
        var results = new List<LoadedPlugin>();
        if (!Directory.Exists(pluginsDirectory))
            return results;

        foreach (var dir in Directory.GetDirectories(pluginsDirectory))
        {
            if (!File.Exists(Path.Combine(dir, "plugin.json")))
                continue;

            try
            {
                results.Add(LoadPlugin(dir));
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Failed to load plugin from {dir}: {ex.Message}");
            }
        }
        return results;
    }

    private static PluginManifest ReadManifest(string pluginDirectory)
    {
        var manifestPath = Path.Combine(pluginDirectory, "plugin.json");
        if (!File.Exists(manifestPath))
            throw new FileNotFoundException($"Plugin manifest not found: {manifestPath}");

        var json = File.ReadAllText(manifestPath);
        var manifest = JsonSerializer.Deserialize<PluginManifest>(json)
            ?? throw new InvalidOperationException($"Failed to parse plugin manifest: {manifestPath}");

        if (manifest.SchemaVersion != 1)
            throw new InvalidOperationException($"Unsupported plugin schema version '{manifest.SchemaVersion}' in {manifestPath}");

        return manifest;
    }

    private static string? FindPluginRoot(string extractedDirectory)
    {
        if (File.Exists(Path.Combine(extractedDirectory, "plugin.json")))
            return extractedDirectory;

        return Directory
            .EnumerateFiles(extractedDirectory, "plugin.json", SearchOption.AllDirectories)
            .Select(Path.GetDirectoryName)
            .FirstOrDefault(path => path is not null);
    }

    private static void CopyDirectory(string sourceDirectory, string targetDirectory)
    {
        Directory.CreateDirectory(targetDirectory);

        foreach (var file in Directory.EnumerateFiles(sourceDirectory, "*", SearchOption.AllDirectories))
        {
            var relativePath = Path.GetRelativePath(sourceDirectory, file);
            if (relativePath.StartsWith("bin" + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
                || relativePath.StartsWith("obj" + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var targetPath = Path.Combine(targetDirectory, relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(targetPath)!);
            File.Copy(file, targetPath, true);
        }
    }
}
