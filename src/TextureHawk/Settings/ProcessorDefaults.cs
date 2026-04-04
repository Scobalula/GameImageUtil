using System.Text.Json.Serialization;

namespace TextureHawk.Settings;

/// <summary>
/// Persisted default values for a specific processor.
/// </summary>
public sealed class ProcessorDefaults
{
    /// <summary>
    /// Default output directory for this processor. <see langword="null"/> uses the app default, and an empty string means same directory as the input.
    /// </summary>
    [JsonPropertyName("outputDirectory")]
    public string? OutputDirectory { get; set; }

    /// <summary>
    /// Default output extension for this processor. <see langword="null"/> uses the app default.
    /// </summary>
    [JsonPropertyName("outputFormat")]
    public string? OutputFormat { get; set; }

    /// <summary>
    /// Default output suffix for this processor. <see langword="null"/> uses the app default, and an empty string means no suffix.
    /// </summary>
    [JsonPropertyName("outputSuffix")]
    public string? OutputSuffix { get; set; }

    /// <summary>
    /// Default GPU color-space mode for this processor. <see langword="null"/> uses the app default.
    /// </summary>
    [JsonPropertyName("gpuColorSpaceMode")]
    public string? GpuColorSpaceMode { get; set; }

    /// <summary>
    /// Default parameter values for this processor, serialized as invariant strings.
    /// </summary>
    [JsonPropertyName("parameterValues")]
    public Dictionary<string, string> ParameterValues { get; set; } = [];
}
