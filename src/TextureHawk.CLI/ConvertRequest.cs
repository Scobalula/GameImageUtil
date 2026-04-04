using TextureHawk.Gpu;

namespace TextureHawk.CLI;

internal sealed class ConvertRequest
{
    public required string InputPath { get; init; }

    public string? OutputPath { get; init; }

    public required string ProcessorId { get; init; }

    public IReadOnlyDictionary<string, object> ParameterValues { get; init; } = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyList<string> AdditionalInputPaths { get; init; } = [];

    public GpuColorSpaceMode? GpuColorSpaceMode { get; init; }
}