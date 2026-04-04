using System.Text.Json;
using System.Text.Json.Serialization;
using TextureHawk.Gpu;

namespace TextureHawk.Settings;

/// <summary>
/// Manages loading and saving of <see cref="AppSettings"/> to a JSON file.
/// </summary>
/// <remarks>
/// Settings are stored in the user's application data directory under
/// <c>TextureHawk/settings.json</c>. The directory is created automatically
/// if it does not exist.
/// </remarks>
public sealed class SettingsManager
{
    private static readonly JsonSerializerOptions s_jsonOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault
    };

    private readonly string _settingsPath;

    public SettingsManager(string? settingsPath = null)
    {
        _settingsPath = settingsPath ?? GetDefaultSettingsPath();
    }

    /// <summary>
    /// The current settings instance.
    /// </summary>
    public AppSettings Current { get; private set; } = new();

    /// <summary>
    /// The file path where settings are stored.
    /// </summary>
    public string SettingsPath => _settingsPath;

    /// <summary>
    /// Loads settings from disk. If the file doesn't exist, uses defaults.
    /// </summary>
    public AppSettings Load()
    {
        if (!File.Exists(_settingsPath))
        {
            Current = new AppSettings();
            return Current;
        }

        try
        {
            var json = File.ReadAllText(_settingsPath);
            Current = JsonSerializer.Deserialize<AppSettings>(json, s_jsonOptions) ?? new AppSettings();
            Normalize(Current);
        }
        catch
        {
            Current = new AppSettings();
        }

        return Current;
    }

    /// <summary>
    /// Saves the current settings to disk.
    /// </summary>
    public void Save()
    {
        var directory = Path.GetDirectoryName(_settingsPath);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        var json = JsonSerializer.Serialize(Current, s_jsonOptions);
        File.WriteAllText(_settingsPath, json);
    }

    /// <summary>
    /// Gets the plugins directory path relative to the settings file.
    /// </summary>
    public string GetPluginsDirectory()
    {
        var settingsDir = Path.GetDirectoryName(_settingsPath) ?? ".";
        return Path.Combine(settingsDir, "plugins");
    }

    /// <summary>
    /// Gets the processor definitions directory path relative to the settings file.
    /// </summary>
    public string GetDefinitionsDirectory()
    {
        var settingsDir = Path.GetDirectoryName(_settingsPath) ?? ".";
        return Path.Combine(settingsDir, "processors");
    }

    private static string GetDefaultSettingsPath()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        return Path.Combine(appData, "TextureHawk", "settings.json");
    }

    private static void Normalize(AppSettings settings)
    {
        settings.OutputDirectory = NormalizeDirectory(settings.OutputDirectory);
        settings.DefaultGpuColorSpaceMode = GpuColorSpaceModeNames.NormalizeOrDefault(settings.DefaultGpuColorSpaceMode, GpuColorSpaceMode.Auto);

        foreach (var defaults in settings.ProcessorDefaults.Values)
        {
            defaults.OutputDirectory = NormalizeProcessorDirectory(defaults.OutputDirectory);
            defaults.OutputFormat = NormalizeOptionalValue(defaults.OutputFormat);
            defaults.OutputSuffix = NormalizeProcessorSuffix(defaults.OutputSuffix);
            defaults.GpuColorSpaceMode = NormalizeProcessorGpuColorSpaceMode(defaults.GpuColorSpaceMode);

            var pathParameters = defaults.ParameterValues
                .Where(pair => LooksLikePath(pair.Value))
                .Select(pair => pair.Key)
                .ToList();

            foreach (var key in pathParameters)
                defaults.ParameterValues.Remove(key);
        }
    }

    private static string NormalizeDirectory(string? directory)
    {
        if (string.IsNullOrWhiteSpace(directory))
            return "";

        var trimmed = directory.Trim();
        return Directory.Exists(trimmed) ? trimmed : "";
    }

    private static string? NormalizeProcessorDirectory(string? directory)
    {
        if (directory is null)
            return null;

        if (directory.Length == 0)
            return string.Empty;

        if (string.IsNullOrWhiteSpace(directory))
            return null;

        var trimmed = directory.Trim();
        return Directory.Exists(trimmed) ? trimmed : null;
    }

    private static string? NormalizeOptionalValue(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static string? NormalizeProcessorSuffix(string? value)
    {
        if (value is null)
            return null;

        return value.Length == 0
            ? string.Empty
            : string.IsNullOrWhiteSpace(value)
                ? null
                : value.Trim();
    }

    private static string? NormalizeProcessorGpuColorSpaceMode(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        return GpuColorSpaceModeNames.NormalizeOrDefault(value, GpuColorSpaceMode.Auto);
    }

    private static bool LooksLikePath(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;

        if (value.IndexOfAny(Path.GetInvalidPathChars()) >= 0)
            return false;

        return Path.IsPathRooted(value)
            || value.Contains(Path.DirectorySeparatorChar)
            || value.Contains(Path.AltDirectorySeparatorChar);
    }
}
