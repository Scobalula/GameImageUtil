using RedFox.Graphics2D;

namespace TextureHawk.Processing;

/// <summary>
/// The result of a processor execution.
/// </summary>
public sealed class ProcessingResult
{
    /// <summary>
    /// Whether processing completed successfully.
    /// </summary>
    public required bool Success { get; init; }

    /// <summary>
    /// The primary output image, or null if processing failed.
    /// </summary>
    public Image? Image { get; init; }

    /// <summary>
    /// Additional output images for multi-output processors (e.g., image splitters).
    /// </summary>
    /// <remarks>
    /// When set, the pipeline writes each image as a separate file with an index suffix.
    /// The primary <see cref="Image"/> is always written as the first output.
    /// </remarks>
    public IReadOnlyList<Image>? Images { get; init; }

    /// <summary>
    /// Optional suffixes for multi-output file names.
    /// </summary>
    public IReadOnlyList<string>? OutputSuffixes { get; init; }

    /// <summary>
    /// Optional output path override used as the base path for writing result images.
    /// </summary>
    public string? OutputPathOverride { get; init; }

    /// <summary>
    /// Error message if processing failed.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Creates a successful result with no direct image output.
    /// </summary>
    public static ProcessingResult Ok() => new()
    {
        Success = true
    };

    /// <summary>
    /// Creates a successful result with the given output image.
    /// </summary>
    public static ProcessingResult Ok(Image image) => new()
    {
        Success = true,
        Image = image
    };

    /// <summary>
    /// Creates a successful result with the given output image and explicit output path.
    /// </summary>
    public static ProcessingResult Ok(Image image, string outputPathOverride) => new()
    {
        Success = true,
        Image = image,
        OutputPathOverride = outputPathOverride
    };

    /// <summary>
    /// Creates a successful result with multiple output images.
    /// </summary>
    public static ProcessingResult Ok(IReadOnlyList<Image> images) => new()
    {
        Success = true,
        Image = images.Count > 0 ? images[0] : null,
        Images = images
    };

    /// <summary>
    /// Creates a successful result with multiple output images and explicit suffixes.
    /// </summary>
    public static ProcessingResult Ok(IReadOnlyList<Image> images, IReadOnlyList<string> outputSuffixes) => new()
    {
        Success = true,
        Image = images.Count > 0 ? images[0] : null,
        Images = images,
        OutputSuffixes = outputSuffixes
    };

    /// <summary>
    /// Creates a successful result with multiple output images, suffixes, and an explicit base output path.
    /// </summary>
    public static ProcessingResult Ok(IReadOnlyList<Image> images, IReadOnlyList<string> outputSuffixes, string outputPathOverride) => new()
    {
        Success = true,
        Image = images.Count > 0 ? images[0] : null,
        Images = images,
        OutputSuffixes = outputSuffixes,
        OutputPathOverride = outputPathOverride
    };

    /// <summary>
    /// Creates a failed result with the given error message.
    /// </summary>
    public static ProcessingResult Fail(string error) => new()
    {
        Success = false,
        ErrorMessage = error
    };
}
