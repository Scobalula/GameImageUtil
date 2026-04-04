using TextureHawk.Processing;

namespace TextureHawk.BuiltInProcessors.EnvironmentTextures;

internal static class ProjectionConverters
{
    internal static int ResolveUpAxis(ProcessorContext context) => UpAxisToIndex(context.GetParameterValue("upAxis", "Y"));

    internal static int ResolveMapX(ProcessorContext context) => AxisMapToIndex(context.GetParameterValue("mapX", "+X"));

    internal static int ResolveMapY(ProcessorContext context) => AxisMapToIndex(context.GetParameterValue("mapY", "+Y"));

    internal static int ResolveMapZ(ProcessorContext context) => AxisMapToIndex(context.GetParameterValue("mapZ", "+Z"));

    /// <summary>
    /// Computes the inverse axis mapping for the given forward mapping.
    /// Forward mapping (mapX, mapY, mapZ) defines a signed permutation matrix M.
    /// The inverse M⁻¹ is needed when decoding: if encode applies M, decode must apply M⁻¹.
    /// </summary>
    internal static (int InvMapX, int InvMapY, int InvMapZ) ComputeInverseMapping(int mapX, int mapY, int mapZ)
    {
        // Each mapN value encodes: axis index = value / 2, sign = (value % 2 == 0) ? +1 : -1
        // Forward: output[i] = sign * input[axis]
        // Inverse: input[axis] = sign * output[i]  (sign is its own inverse)
        Span<int> inverse = stackalloc int[3];
        ReadOnlySpan<int> forward = [mapX, mapY, mapZ];

        for (var i = 0; i < 3; i++)
        {
            var sourceAxis = forward[i] / 2;   // 0=X, 1=Y, 2=Z
            var isNegated = forward[i] % 2 != 0;
            // In the inverse, sourceAxis's output is i (optionally negated)
            inverse[sourceAxis] = isNegated ? (i * 2 + 1) : (i * 2);
        }

        return (inverse[0], inverse[1], inverse[2]);
    }

    internal static int UpAxisToIndex(string value) => value switch
    {
        "X" => 0,
        "Z" => 2,
        _ => 1
    };

    internal static int AxisMapToIndex(string value) => value switch
    {
        "+X" => 0,
        "-X" => 1,
        "+Y" => 2,
        "-Y" => 3,
        "+Z" => 4,
        "-Z" => 5,
        _ => 0
    };
}
