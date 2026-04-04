namespace TextureHawk.Gpu;

/// <summary>
/// Converts GPU color-space modes to and from persisted or user-facing names.
/// </summary>
public static class GpuColorSpaceModeNames
{
    /// <summary>
    /// Display name for <see cref="GpuColorSpaceMode.Auto"/>.
    /// </summary>
    public const string Auto = "Auto";

    /// <summary>
    /// Display name for <see cref="GpuColorSpaceMode.TreatInputAsSrgb"/>.
    /// </summary>
    public const string TreatInputAsSrgb = "Treat Input As sRGB";

    /// <summary>
    /// Display name for <see cref="GpuColorSpaceMode.TreatInputAsLinear"/>.
    /// </summary>
    public const string TreatInputAsLinear = "Treat Input As Linear";

    /// <summary>
    /// All supported user-facing options in display order.
    /// </summary>
    public static IReadOnlyList<string> Options { get; } = [Auto, TreatInputAsSrgb, TreatInputAsLinear];

    /// <summary>
    /// Converts a mode to its display name.
    /// </summary>
    public static string ToDisplayName(GpuColorSpaceMode mode)
    {
        return mode switch
        {
            GpuColorSpaceMode.TreatInputAsSrgb => TreatInputAsSrgb,
            GpuColorSpaceMode.TreatInputAsLinear => TreatInputAsLinear,
            _ => Auto
        };
    }

    /// <summary>
    /// Parses a persisted, CLI, or display value into a mode.
    /// </summary>
    public static bool TryParse(string? value, out GpuColorSpaceMode mode)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            mode = GpuColorSpaceMode.Auto;
            return false;
        }

        var normalized = Normalize(value);
        switch (normalized)
        {
            case "auto":
            case "honorinputmetadata":
            case "honorformat":
                mode = GpuColorSpaceMode.Auto;
                return true;

            case "treatinputassrgb":
            case "srgb":
            case "forcesrgb":
                mode = GpuColorSpaceMode.TreatInputAsSrgb;
                return true;

            case "treatinputaslinear":
            case "linear":
            case "unorm":
            case "stripsrgb":
            case "forcelinear":
                mode = GpuColorSpaceMode.TreatInputAsLinear;
                return true;

            default:
                mode = GpuColorSpaceMode.Auto;
                return false;
        }
    }

    /// <summary>
    /// Parses a value into a mode, returning the fallback when parsing fails.
    /// </summary>
    public static GpuColorSpaceMode ParseOrDefault(string? value, GpuColorSpaceMode fallback)
    {
        return TryParse(value, out var mode) ? mode : fallback;
    }

    /// <summary>
    /// Normalizes an arbitrary value into a persisted display name.
    /// </summary>
    public static string NormalizeOrDefault(string? value, GpuColorSpaceMode fallback)
    {
        return ToDisplayName(ParseOrDefault(value, fallback));
    }

    private static string Normalize(string value)
    {
        return new string(value.Trim().Where(char.IsLetterOrDigit).ToArray()).ToLowerInvariant();
    }
}