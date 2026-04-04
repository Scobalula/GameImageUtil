namespace TextureHawk.Logging;

/// <summary>
/// Severity levels for log entries.
/// </summary>
public enum LogLevel
{
    /// <summary>Verbose diagnostic information.</summary>
    Debug,

    /// <summary>General informational messages.</summary>
    Info,

    /// <summary>Potential issues that do not prevent operation.</summary>
    Warning,

    /// <summary>Errors that affect operation but allow recovery.</summary>
    Error
}
