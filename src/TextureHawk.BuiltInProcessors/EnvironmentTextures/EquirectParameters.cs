using System;
using System.Collections.Generic;
using System.Text;
using TextureHawk.Processing;

namespace TextureHawk.BuiltInProcessors.EnvironmentTextures
{
    internal class EquirectParameters
    {
        public static IReadOnlyList<ProcessorParameter> SizeParameters =>
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
            }
        ];
    }
}
