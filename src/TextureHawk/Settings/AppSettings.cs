using System.Text.Json.Serialization;

namespace TextureHawk.Settings;

/// <summary>
/// Global application settings, persisted as JSON.
/// </summary>
public sealed class AppSettings
{
    /// <summary>
    /// Default output directory for processed files. Empty means same directory as input.
    /// </summary>
    [JsonPropertyName("outputDirectory")]
    public string OutputDirectory { get; set; } = "";

    /// <summary>
    /// Whether new jobs should prefer per-processor output directories when one is stored.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    [JsonPropertyName("useProcessorDefaultOutputDirectory")]
    public bool UseProcessorDefaultOutputDirectory { get; set; } = true;

    /// <summary>
    /// Whether to overwrite existing output files without prompting.
    /// </summary>
    [JsonPropertyName("overwriteExisting")]
    public bool OverwriteExisting { get; set; }

    /// <summary>
    /// The default output format extension (e.g., ".dds", ".png").
    /// </summary>
    [JsonPropertyName("defaultOutputFormat")]
    public string DefaultOutputFormat { get; set; } = "";

    /// <summary>
    /// Whether new jobs should prefer per-processor output formats when one is stored.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    [JsonPropertyName("useProcessorDefaultOutputFormat")]
    public bool UseProcessorDefaultOutputFormat { get; set; } = true;

    /// <summary>
    /// The last-used processor ID, restored on app restart.
    /// </summary>
    [JsonPropertyName("lastProcessorId")]
    public string? LastProcessorId { get; set; }

    /// <summary>
    /// Whether to check for updates on startup.
    /// </summary>
    [JsonPropertyName("checkForUpdates")]
    public bool CheckForUpdates { get; set; } = true;

    /// <summary>
    /// Whether to include pre-release versions in update checks.
    /// </summary>
    [JsonPropertyName("includePreReleases")]
    public bool IncludePreReleases { get; set; }

    /// <summary>
    /// Version string ignored by startup update notifications.
    /// </summary>
    [JsonPropertyName("skippedUpdateVersion")]
    public string? SkippedUpdateVersion { get; set; }

    /// <summary>
    /// Maximum number of parallel processing jobs.
    /// </summary>
    [JsonPropertyName("maxParallelJobs")]
    public int MaxParallelJobs { get; set; } = 1;

    /// <summary>
    /// Directories to scan for plugins on startup.
    /// </summary>
    [JsonPropertyName("pluginDirectories")]
    public List<string> PluginDirectories { get; set; } = [];

    /// <summary>
    /// Directories to scan for external processor definition files.
    /// </summary>
    [JsonPropertyName("processorDefinitionDirectories")]
    public List<string> ProcessorDefinitionDirectories { get; set; } = [];

    /// <summary>
    /// The update provider to use: "github", "custom", etc.
    /// </summary>
    [JsonPropertyName("updateProvider")]
    public string UpdateProvider { get; set; } = "github";

    /// <summary>
    /// Custom update URL when <see cref="UpdateProvider"/> is "custom".
    /// </summary>
    [JsonPropertyName("customUpdateUrl")]
    public string CustomUpdateUrl { get; set; } = "";

    /// <summary>
    /// UI theme preference: "dark" or "light".
    /// </summary>
    [JsonPropertyName("theme")]
    public string Theme { get; set; } = "dark";

    /// <summary>
    /// Preserve the output filename suffix pattern (e.g., "_converted").
    /// </summary>
    [JsonPropertyName("outputSuffix")]
    public string OutputSuffix { get; set; } = "";

    /// <summary>
    /// Whether new jobs should prefer per-processor output suffixes when one is stored.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    [JsonPropertyName("useProcessorDefaultOutputSuffix")]
    public bool UseProcessorDefaultOutputSuffix { get; set; } = true;

    /// <summary>
    /// Default GPU color-space mode applied when a job does not override it.
    /// </summary>
    [JsonPropertyName("defaultGpuColorSpaceMode")]
    public string DefaultGpuColorSpaceMode { get; set; } = "Auto";

    /// <summary>
    /// Whether new jobs should prefer per-processor GPU color-space modes when one is stored.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    [JsonPropertyName("useProcessorDefaultGpuColorSpaceMode")]
    public bool UseProcessorDefaultGpuColorSpaceMode { get; set; } = true;

    /// <summary>
    /// Preferred GPU device name for compute operations. Null means auto-select.
    /// </summary>
    [JsonPropertyName("preferredGpuDeviceName")]
    public string? PreferredGpuDeviceName { get; set; }

    /// <summary>
    /// Preferred GPU PCI device identifier for compute operations.
    /// </summary>
    [JsonPropertyName("preferredGpuDeviceId")]
    public uint? PreferredGpuDeviceId { get; set; }

    /// <summary>
    /// Preferred GPU PCI vendor identifier for compute operations.
    /// </summary>
    [JsonPropertyName("preferredGpuVendorId")]
    public uint? PreferredGpuVendorId { get; set; }

    /// <summary>
    /// Processor IDs bound to quick-select hotkey slots 1-6.
    /// </summary>
    [JsonPropertyName("processorHotkeySlots")]
    public List<string?> ProcessorHotkeySlots { get; set; } = [null, null, null, null, null, null];

    /// <summary>
    /// Last-used defaults keyed by processor ID.
    /// </summary>
    [JsonPropertyName("processorDefaults")]
    public Dictionary<string, ProcessorDefaults> ProcessorDefaults { get; set; } = [];
}
