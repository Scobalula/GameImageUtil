namespace TextureHawk.CLI;

internal sealed class PipeResponse
{
    public string? RequestId { get; init; }

    public required string Command { get; init; }

    public bool Success { get; init; }

    public int ExitCode { get; init; }

    public string? Error { get; init; }

    public object? Data { get; init; }
}