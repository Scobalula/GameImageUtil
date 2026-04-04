using RedFox.Graphics2D;
using TextureHawk.CLI;
using TextureHawk.Processing;
using Xunit;

namespace TextureHawk.Tests;

public sealed class ProcessorParameterBinderTests
{
    [Fact]
    public void TryBind_ConvertsSupportedParameterTypes()
    {
        var processor = new ParameterProcessor();
        var rawValues = new Dictionary<string, object?>
        {
            ["enabled"] = "yes",
            ["iterations"] = "4",
            ["threshold"] = "0.5",
            ["mode"] = "high",
            ["targetFormat"] = "R8G8B8A8Unorm",
            ["maskPath"] = "mask.png"
        };

        var success = ProcessorParameterBinder.TryBind(processor, rawValues, out var values, out var error);

        Assert.True(success);
        Assert.Null(error);
        Assert.True((bool)values["enabled"]);
        Assert.Equal(4, values["iterations"]);
        Assert.Equal(0.5f, (float)values["threshold"]);
        Assert.Equal("High", values["mode"]);
        Assert.Equal(ImageFormat.R8G8B8A8Unorm, values["targetFormat"]);
        Assert.Equal("mask.png", values["maskPath"]);
    }

    [Fact]
    public void TryBind_RejectsUnknownAndOutOfRangeValues()
    {
        var processor = new ParameterProcessor();

        var unknownSuccess = ProcessorParameterBinder.TryBind(processor, new Dictionary<string, object?> { ["unknown"] = "1" }, out _, out var unknownError);
        var rangeSuccess = ProcessorParameterBinder.TryBind(processor, new Dictionary<string, object?> { ["iterations"] = "99" }, out _, out var rangeError);

        Assert.False(unknownSuccess);
        Assert.Contains("does not define parameter 'unknown'", unknownError, StringComparison.Ordinal);
        Assert.False(rangeSuccess);
        Assert.Contains("less than or equal to 8", rangeError, StringComparison.Ordinal);
    }

    private sealed class ParameterProcessor : IProcessor
    {
        public string Id => "test.parameters";
        public string Name => "Parameters";
        public string Description => "parameter binding";
        public string Category => "test";
        public string Icon => "P";
        public IReadOnlyList<ProcessorParameter> Parameters =>
        [
            new ProcessorParameter
            {
                Id = "enabled",
                Name = "Enabled",
                Description = "toggles the feature",
                Type = ProcessorParameterType.Boolean,
                DefaultValue = false
            },
            new ProcessorParameter
            {
                Id = "iterations",
                Name = "Iterations",
                Description = "number of steps",
                Type = ProcessorParameterType.Integer,
                DefaultValue = 1,
                MinValue = 1,
                MaxValue = 8
            },
            new ProcessorParameter
            {
                Id = "threshold",
                Name = "Threshold",
                Description = "float threshold",
                Type = ProcessorParameterType.Float,
                DefaultValue = 0.25f,
                MinValue = 0.0f,
                MaxValue = 1.0f
            },
            new ProcessorParameter
            {
                Id = "mode",
                Name = "Mode",
                Description = "quality mode",
                Type = ProcessorParameterType.Choice,
                DefaultValue = "Low",
                Options = ["Low", "High"]
            },
            new ProcessorParameter
            {
                Id = "targetFormat",
                Name = "Target Format",
                Description = "output image format",
                Type = ProcessorParameterType.ImageFormat,
                DefaultValue = ImageFormat.R8G8B8A8Unorm
            },
            new ProcessorParameter
            {
                Id = "maskPath",
                Name = "Mask Path",
                Description = "optional mask file",
                Type = ProcessorParameterType.FilePath
            }
        ];

        public ValueTask<ProcessingResult> ProcessAsync(ProcessorContext context, CancellationToken cancellationToken)
        {
            return ValueTask.FromResult(ProcessingResult.Ok());
        }
    }
}