namespace TextureHawk.Gpu;

/// <summary>
/// Controls how GPU-backed processors interpret SRGB-capable input formats.
/// </summary>
/// <remarks>
/// Auto honors the source image format metadata. TreatInputAsSrgb forces SRGB
/// sampling semantics even when metadata is missing. TreatInputAsLinear strips
/// SRGB sampling semantics and samples the raw UNORM data instead.
/// </remarks>
public enum GpuColorSpaceMode
{
    /// <summary>
    /// Preserve the source format metadata and let the GPU sample accordingly.
    /// </summary>
    Auto,

    /// <summary>
    /// Reinterpret SRGB-capable inputs as SRGB before sampling.
    /// </summary>
    TreatInputAsSrgb,

    /// <summary>
    /// Reinterpret SRGB-capable inputs as linear UNORM before sampling.
    /// </summary>
    TreatInputAsLinear
}