namespace TextureHawk.Logging;

/// <summary>
/// Immutable record representing a single log entry.
/// </summary>
/// <param name="Timestamp">UTC time the entry was created.</param>
/// <param name="Level">Severity level.</param>
/// <param name="Category">Source category (e.g. "Pipeline", "GPU").</param>
/// <param name="Message">Log message text.</param>
public sealed record LogEntry(DateTime Timestamp, LogLevel Level, string Category, string Message)
{
    /// <summary>
    /// Formats the entry as a single text line for display or file output.
    /// </summary>
    public override string ToString() =>
        $"[{Timestamp:HH:mm:ss.fff}] [{Level,-7}] [{Category}] {Message}";
}
