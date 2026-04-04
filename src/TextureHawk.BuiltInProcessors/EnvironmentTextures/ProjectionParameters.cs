using System;
using System.Collections.Generic;
using System.Text;
using TextureHawk.Processing;

namespace TextureHawk.BuiltInProcessors.EnvironmentTextures;

/// <summary>
/// Provides a collection of processor parameters that define orientation options for projection processing.
/// </summary>
internal class ProjectionParameters
{
    /// <summary>
    /// Gets the orientation parameters used for projection processing.
    /// </summary>
    public static IReadOnlyList<ProcessorParameter> OrientationParameters { get; } =
    [
        new ProcessorParameter
        {
            Id = "upAxis",
            Name = "Up Axis",
            Description = "Choose which axis points upward in the source environment orientation.",
            Type = ProcessorParameterType.Choice,
            DefaultValue = "Y",
            Options = ["X", "Y", "Z"]
        },
        new ProcessorParameter
        {
            Id = "mapX",
            Name = "Output X",
            Description = "Which source axis feeds the sampled X component.",
            Type = ProcessorParameterType.Choice,
            DefaultValue = "+X",
            Options = ["+X", "-X", "+Y", "-Y", "+Z", "-Z"]
        },
        new ProcessorParameter
        {
            Id = "mapY",
            Name = "Output Y",
            Description = "Which source axis feeds the sampled Y component.",
            Type = ProcessorParameterType.Choice,
            DefaultValue = "+Y",
            Options = ["+X", "-X", "+Y", "-Y", "+Z", "-Z"]
        },
        new ProcessorParameter
        {
            Id = "mapZ",
            Name = "Output Z",
            Description = "Which source axis feeds the sampled Z component.",
            Type = ProcessorParameterType.Choice,
            DefaultValue = "+Z",
            Options = ["+X", "-X", "+Y", "-Y", "+Z", "-Z"]
        }
    ];

}
