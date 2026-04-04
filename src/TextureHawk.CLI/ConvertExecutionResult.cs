namespace TextureHawk.CLI;

internal sealed class ConvertExecutionResult
{
    public required string InputPath { get; init; }

    public required string OutputPath { get; init; }

    public required string ProcessorId { get; init; }

    public IReadOnlyList<string> WrittenPaths { get; init; } = [];

    public bool Success { get; init; }

    public string? ErrorMessage { get; init; }
}