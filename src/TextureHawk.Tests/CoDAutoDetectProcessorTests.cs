using TextureHawk.BuiltInProcessors;
using Xunit;

namespace TextureHawk.Tests;

public sealed class CoDAutoDetectProcessorTests
{
    [Theory]
    [InlineData("weapon_nog.png")]
    [InlineData("weapon@nog.dds")]
    [InlineData("weapon_n&bar~detail.tif")]
    public void CoDNogProcessor_CanConvertRecognizesKnownPatterns(string fileName)
    {
        var processor = new CoDNogProcessor();

        var result = processor.CanConvert(fileName);

        Assert.True(result);
    }
}