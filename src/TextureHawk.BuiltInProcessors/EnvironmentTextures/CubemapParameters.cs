using TextureHawk.Processing;

namespace TextureHawk.BuiltInProcessors.EnvironmentTextures;

/// <summary>
/// Provides a collection of processor parameters that define orientation options for cubemap processing.
/// </summary>
internal class CubemapParameters
{
    public static IReadOnlyList<ProcessorParameter> InputLayoutParameter { get; } =
    [
        new ProcessorParameter
        {
            Id = "inputLayout",
            Name = "Input Layout",
            Description = "How the cubemap faces are arranged in the input image. Use Native for DDS cubemap textures with 6 array slices.",
            Type = ProcessorParameterType.Choice,
            DefaultValue = "Native",
            Options = ["Native", "HCross", "VCross", "HStrip", "VStrip"]
        }
    ];

    public static IReadOnlyList<ProcessorParameter> OutputLayoutParameter { get; } =
    [
        new ProcessorParameter
        {
            Id = "outputLayout",
            Name = "Output Layout",
            Description = "How the cubemap faces are arranged in the output. Cubemap produces a native DDS with 6 array slices. Other options flatten faces into a single 2D image. Individual writes each face as a separate file.",
            Type = ProcessorParameterType.Choice,
            DefaultValue = "Cubemap",
            Options = ["Cubemap", "HCross", "VCross", "HStrip", "VStrip", "Individual"]
        }
    ];

    /// <summary>
    /// Gets the default set of parameters used for cubemap-consuming processors.
    /// </summary>
    public static IReadOnlyList<ProcessorParameter> DefaultParameters { get; } =
    [
        new ProcessorParameter
        {
            Id = "outputWidth",
            Name = "Output Width",
            Description = "Width of the equirectangular output in pixels. Height will be half this value.",
            Type = ProcessorParameterType.Integer,
            DefaultValue = 4096,
            MinValue = 64,
            MaxValue = 16384
        },
        .. InputLayoutParameter,
        .. ProjectionParameters.OrientationParameters
    ];
}
