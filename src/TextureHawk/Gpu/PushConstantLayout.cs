using System.Text.RegularExpressions;

namespace TextureHawk.Gpu;

/// <summary>
/// Describes the layout of a GLSL <c>layout(push_constant) uniform</c> block,
/// parsed from shader source code.
/// </summary>
/// <remarks>
/// Extracts field names, types, and byte offsets using std430 alignment rules.
/// Use <see cref="CreateBuilder"/> to populate push constant data by field name
/// instead of fragile manual byte offsets.
/// </remarks>
public sealed partial class PushConstantLayout
{
    private readonly Dictionary<string, PushConstantField> _fieldsByName;

    /// <summary>
    /// Ordered list of push constant fields.
    /// </summary>
    public IReadOnlyList<PushConstantField> Fields { get; }

    /// <summary>
    /// Total size in bytes of the push constant block.
    /// </summary>
    public int TotalSize { get; }

    private PushConstantLayout(IReadOnlyList<PushConstantField> fields, int totalSize)
    {
        Fields = fields;
        TotalSize = totalSize;
        _fieldsByName = new Dictionary<string, PushConstantField>(fields.Count, StringComparer.Ordinal);
        foreach (var field in fields)
            _fieldsByName[field.Name] = field;
    }

    /// <summary>
    /// Gets a field by its GLSL name.
    /// </summary>
    /// <exception cref="KeyNotFoundException">Thrown when the field name does not exist in the layout.</exception>
    public PushConstantField GetField(string name) =>
        _fieldsByName.TryGetValue(name, out var field)
            ? field
            : throw new KeyNotFoundException(
                $"Push constant field '{name}' not found. Available: {string.Join(", ", _fieldsByName.Keys)}");

    /// <summary>
    /// Creates a <see cref="PushConstantBuilder"/> for populating push constant values by name.
    /// </summary>
    public PushConstantBuilder CreateBuilder() => new(this);

    /// <summary>
    /// Parses the <c>layout(push_constant) uniform</c> block from GLSL source.
    /// </summary>
    /// <param name="glslSource">Complete GLSL shader source code.</param>
    /// <returns>
    /// A <see cref="PushConstantLayout"/> with extracted fields, or an empty layout
    /// if no push_constant block is found.
    /// </returns>
    public static PushConstantLayout Parse(string glslSource)
    {
        var match = PushConstantBlockRegex().Match(glslSource);
        if (!match.Success)
            return new PushConstantLayout([], 0);

        var body = match.Groups[1].Value;
        var fields = new List<PushConstantField>();
        var offset = 0;

        foreach (Match fieldMatch in FieldRegex().Matches(body))
        {
            var typeName = fieldMatch.Groups[1].Value;
            var fieldName = fieldMatch.Groups[2].Value;

            var (size, alignment) = GetStd430Layout(typeName);

            // Align offset to the field's natural alignment
            offset = (offset + alignment - 1) & ~(alignment - 1);

            fields.Add(new PushConstantField
            {
                Name = fieldName,
                Offset = offset,
                Size = size,
                Type = MapGlslType(typeName)
            });

            offset += size;
        }

        return new PushConstantLayout(fields, offset);
    }

    private static (int Size, int Alignment) GetStd430Layout(string typeName) => typeName switch
    {
        "int" or "uint" or "float" => (4, 4),
        "vec2" or "ivec2" or "uvec2" => (8, 8),
        "vec3" or "ivec3" or "uvec3" => (12, 16),
        "vec4" or "ivec4" or "uvec4" => (16, 16),
        "mat4" => (64, 16),
        _ => (4, 4)
    };

    private static ShaderParameterType MapGlslType(string typeName) => typeName switch
    {
        "int" => ShaderParameterType.Int,
        "uint" => ShaderParameterType.UInt,
        "float" => ShaderParameterType.Float,
        "vec2" => ShaderParameterType.Float2,
        "vec3" => ShaderParameterType.Float3,
        "vec4" => ShaderParameterType.Float4,
        "ivec2" => ShaderParameterType.Int2,
        "ivec3" => ShaderParameterType.Int3,
        "ivec4" => ShaderParameterType.Int4,
        "mat4" => ShaderParameterType.Mat4,
        _ => ShaderParameterType.Int
    };

    [GeneratedRegex(@"layout\s*\(\s*push_constant\s*\)\s*uniform\s+\w+\s*\{([^}]+)\}", RegexOptions.Singleline)]
    private static partial Regex PushConstantBlockRegex();

    [GeneratedRegex(@"(\w+)\s+(\w+)\s*;")]
    private static partial Regex FieldRegex();
}
