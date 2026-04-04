using RedFox.Graphics2D;

namespace TextureHawk.Processing;

/// <summary>
/// Defines output-format-specific write parameters and prepares images before writing.
/// </summary>
public static class OutputWriteParameterCatalog
{
    public const string DdsImageFormatParameterId = "imageFormat";

    private static readonly IReadOnlyList<ProcessorParameter> s_ddsParameters =
    [
        new ProcessorParameter
        {
            Id = DdsImageFormatParameterId,
            Name = "Image Format",
            Description = "DDS output format to encode before writing.",
            Type = ProcessorParameterType.ImageFormat,
            DefaultValue = ImageFormat.BC1Unorm,
            Options =
            [
                nameof(ImageFormat.BC1Unorm),
                nameof(ImageFormat.BC1UnormSrgb),
                nameof(ImageFormat.BC2Unorm),
                nameof(ImageFormat.BC2UnormSrgb),
                nameof(ImageFormat.BC3Unorm),
                nameof(ImageFormat.BC3UnormSrgb),
                nameof(ImageFormat.BC7Unorm),
                nameof(ImageFormat.BC7UnormSrgb),
                nameof(ImageFormat.R8G8B8A8Unorm),
                nameof(ImageFormat.R8G8B8A8UnormSrgb)
            ]
        }
    ];

    public static IReadOnlyList<ProcessorParameter> GetAllParameters() => s_ddsParameters;

    public static IReadOnlyList<ProcessorParameter> GetParametersForOutputFormat(string? outputFormat)
    {
        return NormalizeExtension(outputFormat) switch
        {
            ".dds" => s_ddsParameters,
            _ => []
        };
    }

    public static Image PrepareImageForWrite(string outputPath, Image image, IReadOnlyDictionary<string, object> parameterValues)
    {
        ArgumentNullException.ThrowIfNull(image);
        ArgumentNullException.ThrowIfNull(parameterValues);

        if (!string.Equals(NormalizeExtension(Path.GetExtension(outputPath)), ".dds", StringComparison.OrdinalIgnoreCase))
            return image;

        if (!TryResolveImageFormat(parameterValues, DdsImageFormatParameterId, out var targetFormat))
            return image;

        if (image.Format == targetFormat)
            return image;

        var converted = new Image(
            image.Width,
            image.Height,
            image.Depth,
            image.ArraySize,
            image.MipLevels,
            image.Format,
            image.IsCubemap,
            image.PixelData.ToArray());

        converted.Convert(targetFormat);
        return converted;
    }

    private static bool TryResolveImageFormat(IReadOnlyDictionary<string, object> parameterValues, string parameterId, out ImageFormat format)
    {
        if (parameterValues.TryGetValue(parameterId, out var value))
        {
            if (value is ImageFormat imageFormat)
            {
                format = imageFormat;
                return true;
            }

            if (value is string raw && Enum.TryParse(raw, true, out imageFormat))
            {
                format = imageFormat;
                return true;
            }
        }

        format = default;
        return false;
    }

    private static string NormalizeExtension(string? extension)
    {
        if (string.IsNullOrWhiteSpace(extension))
            return string.Empty;

        extension = extension.Trim();
        if (!extension.StartsWith('.'))
            extension = $".{extension}";

        return extension.ToLowerInvariant();
    }
}
