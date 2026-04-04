using RedFox.Graphics2D;
using TextureHawk.Gpu;
using TextureHawk.Logging;
using TextureHawk.Processing;

namespace TextureHawk.BuiltInProcessors.ColorTextures;

/// <summary>
/// Rearranges (swizzles) the channels of an image on the GPU.
/// </summary>
/// <remarks>
/// Each output channel can be mapped to any input channel (R, G, B, A)
/// or to a constant value (0 or 1). Useful for converting between
/// different channel packing conventions.
/// Push constant layout: outWidth, outHeight, srcWidth, srcHeight,
/// redSource, greenSource, blueSource, alphaSource (8×int32 = 32 bytes).
/// Source indices: 0=R, 1=G, 2=B, 3=A, 4=Zero, 5=One.
/// </remarks>
public sealed class ChannelSwizzleProcessor : IProcessor
{
    /// <inheritdoc />
    public string Id => "builtin.channel-swizzle";

    /// <inheritdoc />
    public string Name => "Channel Swizzle";

    /// <inheritdoc />
    public string Description => "Rearrange RGBA channels (e.g., swap R↔B, set alpha to 1).";

    /// <inheritdoc />
    public string Category => "Channels";

    /// <inheritdoc />
    public string Icon => "🔀";

    /// <inheritdoc />

    /// <inheritdoc />

    private static readonly string[] s_channelOptions = ["Red", "Green", "Blue", "Alpha", "Zero", "One"];

    /// <inheritdoc />
    public IReadOnlyList<ProcessorParameter> Parameters { get; } =
    [
        new ProcessorParameter { Id = "red",   Name = "Red Output",   Description = "Source for output red channel.",   Type = ProcessorParameterType.Choice, DefaultValue = "Red",   Options = s_channelOptions },
        new ProcessorParameter { Id = "green", Name = "Green Output", Description = "Source for output green channel.", Type = ProcessorParameterType.Choice, DefaultValue = "Green", Options = s_channelOptions },
        new ProcessorParameter { Id = "blue",  Name = "Blue Output",  Description = "Source for output blue channel.",  Type = ProcessorParameterType.Choice, DefaultValue = "Blue",  Options = s_channelOptions },
        new ProcessorParameter { Id = "alpha", Name = "Alpha Output", Description = "Source for output alpha channel.", Type = ProcessorParameterType.Choice, DefaultValue = "Alpha", Options = s_channelOptions },
    ];

    /// <inheritdoc />
    public async ValueTask<ProcessingResult> ProcessAsync(ProcessorContext context, CancellationToken cancellationToken)
    {
        var source = context.TranslatorManager.Read(context.InputPath);
        AppLog.Info("ChannelSwizzle", $"Processing {source.Width}×{source.Height} ({source.Format})");

        var spirv = GpuShaderCache.GetShaderBytecode("channel_swizzle");
        var pushConstants = GpuShaderCache.GetLayout("channel_swizzle").CreateBuilder()
            .Set("outWidth", source.Width)
            .Set("outHeight", source.Height)
            .Set("srcWidth", source.Width)
            .Set("srcHeight", source.Height)
            .Set("redSource", ChannelNameToIndex(context.GetParameterValue("red", "Red")))
            .Set("greenSource", ChannelNameToIndex(context.GetParameterValue("green", "Green")))
            .Set("blueSource", ChannelNameToIndex(context.GetParameterValue("blue", "Blue")))
            .Set("alphaSource", ChannelNameToIndex(context.GetParameterValue("alpha", "Alpha")))
            .Build();

        var result = await context.ComputeEngine.DispatchAsync(spirv, GpuImageFormats.PrepareInputsForSampling([source], context.GpuColorSpaceMode), pushConstants, source.Width, source.Height, GpuImageFormats.ResolveWritableOutputFormat(source.Format), (1, 1, 1), cancellationToken);
        return ProcessingResult.Ok(result);
    }

    private static int ChannelNameToIndex(string name) => name switch
    {
        "Red" => 0,
        "Green" => 1,
        "Blue" => 2,
        "Alpha" => 3,
        "Zero" => 4,
        "One" => 5,
        _ => 0
    };
}
