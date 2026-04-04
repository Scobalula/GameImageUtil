using System;
using System.Collections.Generic;
using System.Text;
using TextureHawk.Processing;

namespace TextureHawk.BuiltInProcessors.EnvironmentTextures;

internal class HemiOctParameters
{
    internal static IReadOnlyList<ProcessorParameter> LayoutParameters { get; } =
    [
        new ProcessorParameter
        {
            Id = "sourceLayout",
            Name = "Source Layout",
            Description = "Use Packed when the primary input already stores upper and lower hemispheres side by side. Use Upper Only when the primary input contains only the upper half.",
            Type = ProcessorParameterType.Choice,
            DefaultValue = "Packed",
            Options = ["Packed", "Upper Only"]
        },
        new ProcessorParameter
        {
            Id = "hasLowerHalf",
            Name = "Has Lower Half",
            Description = "Disable this when no lower hemisphere data exists. Missing lower pixels will be black.",
            Type = ProcessorParameterType.Boolean,
            DefaultValue = true
        },
        new ProcessorParameter
        {
            Id = "lowerHalfPath",
            Name = "Lower Half File",
            Description = "Optional separate file containing the lower hemisphere.",
            Type = ProcessorParameterType.FilePath,
            DefaultValue = ""
        }
    ];
}
