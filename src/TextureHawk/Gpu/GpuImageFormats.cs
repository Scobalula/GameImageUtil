using RedFox.Graphics2D;
using RedFox.Graphics2D.Codecs;

namespace TextureHawk.Gpu;

/// <summary>
/// Centralizes GPU image format handling for sampling and writable outputs.
/// </summary>
public static class GpuImageFormats
{
    /// <summary>
    /// Determines whether an image format carries SRGB sampling semantics.
    /// </summary>
    public static bool IsSrgbFormat(ImageFormat format)
    {
        return format is ImageFormat.R8G8B8A8UnormSrgb
            or ImageFormat.B8G8R8A8UnormSrgb
            or ImageFormat.BC1UnormSrgb
            or ImageFormat.BC2UnormSrgb
            or ImageFormat.BC3UnormSrgb
            or ImageFormat.BC7UnormSrgb;
    }

    /// <summary>
    /// Resolves the writable compute output format based on the desired final format.
    /// </summary>
    public static ImageFormat ResolveWritableOutputFormat(ImageFormat preferredFormat)
    {
        var writablePreferredFormat = ResolveLinearCounterpart(preferredFormat);
        if (IsHdrLikeFormat(writablePreferredFormat))
            return writablePreferredFormat == ImageFormat.R32G32B32A32Float
                ? ImageFormat.R32G32B32A32Float
                : ImageFormat.R16G16B16A16Float;

        return writablePreferredFormat switch
        {
            ImageFormat.R8G8B8A8Unorm => writablePreferredFormat,
            ImageFormat.R8G8B8A8Snorm => writablePreferredFormat,
            ImageFormat.R16G16B16A16Unorm => writablePreferredFormat,
            ImageFormat.R16G16B16A16Snorm => writablePreferredFormat,
            ImageFormat.R16G16B16A16Float => writablePreferredFormat,
            ImageFormat.R32G32B32A32Float => writablePreferredFormat,
            _ => ImageFormat.R8G8B8A8Unorm
        };
    }

    /// <summary>
    /// Reinterprets a single input image according to the selected GPU color-space mode.
    /// </summary>
    public static Image PrepareInputForSampling(Image source, GpuColorSpaceMode mode)
    {
        var targetFormat = mode switch
        {
            GpuColorSpaceMode.TreatInputAsSrgb => ResolveSrgbCounterpart(source.Format),
            GpuColorSpaceMode.TreatInputAsLinear => ResolveLinearCounterpart(source.Format),
            _ => source.Format
        };

        if (targetFormat == source.Format)
            return source;

        source.Format = targetFormat;
        return source;
    }

    /// <summary>
    /// Reinterprets all input images according to the selected GPU color-space mode.
    /// </summary>
    public static IReadOnlyList<Image> PrepareInputsForSampling(IReadOnlyList<Image> inputs, GpuColorSpaceMode mode)
    {
        Image[]? prepared = null;

        for (var i = 0; i < inputs.Count; i++)
        {
            var converted = PrepareInputForSampling(inputs[i], mode);
            if (ReferenceEquals(converted, inputs[i]))
            {
                if (prepared is not null)
                    prepared[i] = inputs[i];

                continue;
            }

            prepared ??= CreatePreparedCopy(inputs, i);
            prepared[i] = converted;
        }

        return prepared ?? inputs;
    }

    private static Image[] CreatePreparedCopy(IReadOnlyList<Image> inputs, int copyCount)
    {
        var prepared = new Image[inputs.Count];
        for (var i = 0; i < copyCount; i++)
            prepared[i] = inputs[i];

        return prepared;
    }

    private static ImageFormat ResolveSrgbCounterpart(ImageFormat format)
    {
        return format switch
        {
            ImageFormat.R8G8B8A8Unorm => ImageFormat.R8G8B8A8UnormSrgb,
            ImageFormat.B8G8R8A8Unorm => ImageFormat.B8G8R8A8UnormSrgb,
            ImageFormat.BC1Unorm => ImageFormat.BC1UnormSrgb,
            ImageFormat.BC2Unorm => ImageFormat.BC2UnormSrgb,
            ImageFormat.BC3Unorm => ImageFormat.BC3UnormSrgb,
            ImageFormat.BC7Unorm => ImageFormat.BC7UnormSrgb,
            _ => format
        };
    }

    private static ImageFormat ResolveLinearCounterpart(ImageFormat format)
    {
        return format switch
        {
            ImageFormat.R8G8B8A8UnormSrgb => ImageFormat.R8G8B8A8Unorm,
            ImageFormat.B8G8R8A8UnormSrgb => ImageFormat.B8G8R8A8Unorm,
            ImageFormat.BC1UnormSrgb => ImageFormat.BC1Unorm,
            ImageFormat.BC2UnormSrgb => ImageFormat.BC2Unorm,
            ImageFormat.BC3UnormSrgb => ImageFormat.BC3Unorm,
            ImageFormat.BC7UnormSrgb => ImageFormat.BC7Unorm,
            _ => format
        };
    }

    private static bool IsHdrLikeFormat(ImageFormat format)
    {
        return format switch
        {
            ImageFormat.R32G32B32A32Float => true,
            ImageFormat.R32G32B32Float => true,
            ImageFormat.R16G16B16A16Float => true,
            ImageFormat.R32G32Float => true,
            ImageFormat.R32Float => true,
            ImageFormat.R16Float => true,
            ImageFormat.R11G11B10Float => true,
            ImageFormat.R9G9B9E5SharedExp => true,
            ImageFormat.BC6HUF16 => true,
            ImageFormat.BC6HSF16 => true,
            _ => false
        };
    }
}