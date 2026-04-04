using System.Text.Json;

namespace TextureHawk.CLI;

internal sealed class PipeRequest
{
    public string? RequestId { get; init; }

    public string? Command { get; init; }

    public string? InputPath { get; init; }

    public string? OutputPath { get; init; }

    public string? FilePath { get; init; }

    public string? ProcessorId { get; init; }

    public string? TargetFormat { get; init; }

    public string? GpuColorSpaceMode { get; init; }

    public IReadOnlyList<string>? AdditionalInputPaths { get; init; }

    public Dictionary<string, JsonElement>? Parameters { get; init; }
}