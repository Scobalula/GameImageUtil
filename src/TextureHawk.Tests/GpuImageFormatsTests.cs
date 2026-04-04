using RedFox.Graphics2D;
using TextureHawk.Gpu;
using Xunit;

namespace TextureHawk.Tests;

public sealed class GpuImageFormatsTests
{
    [Fact]
    public void PrepareInputForSampling_TreatInputAsLinear_StripsSrgbMetadata()
    {
        var source = CreateImage(ImageFormat.R8G8B8A8UnormSrgb);

        var prepared = GpuImageFormats.PrepareInputForSampling(source, GpuColorSpaceMode.TreatInputAsLinear);

        Assert.NotSame(source, prepared);
        Assert.Equal(ImageFormat.R8G8B8A8Unorm, prepared.Format);
    }

    [Fact]
    public void PrepareInputForSampling_Auto_KeepsOriginalInstance()
    {
        var source = CreateImage(ImageFormat.BC7UnormSrgb);

        var prepared = GpuImageFormats.PrepareInputForSampling(source, GpuColorSpaceMode.Auto);

        Assert.Same(source, prepared);
    }

    [Fact]
    public void ResolveWritableOutputFormat_StripsSrgbAndFallsBackToRgba8()
    {
        var resolved = GpuImageFormats.ResolveWritableOutputFormat(ImageFormat.BC7UnormSrgb);

        Assert.Equal(ImageFormat.R8G8B8A8Unorm, resolved);
    }

    [Theory]
    [InlineData("auto", GpuColorSpaceMode.Auto)]
    [InlineData("srgb", GpuColorSpaceMode.TreatInputAsSrgb)]
    [InlineData("linear", GpuColorSpaceMode.TreatInputAsLinear)]
    [InlineData("Treat Input As sRGB", GpuColorSpaceMode.TreatInputAsSrgb)]
    public void GpuColorSpaceModeNames_TryParse_SupportsCliAndDisplayNames(string value, GpuColorSpaceMode expected)
    {
        var parsed = GpuColorSpaceModeNames.TryParse(value, out var actual);

        Assert.True(parsed);
        Assert.Equal(expected, actual);
    }

    private static Image CreateImage(ImageFormat format)
    {
        var image = new Image(1, 1, 1, 1, 1, format, false);
        image.Slices[0].Pixels.Span[0] = 17;
        return image;
    }
}