using System.Collections.Concurrent;
using Serilog;
using Serilog.Events;

namespace TextureHawk.Logging;

/// <summary>
/// Application-wide logger backed by Serilog with an in-memory ring buffer for UI display.
/// </summary>
/// <remarks>
/// Thread-safe. The in-memory buffer retains up to <see cref="MaxEntries"/> entries;
/// older entries are discarded. Subscribe to <see cref="EntryAdded"/> for real-time UI updates.
/// Call <see cref="Initialize"/> at startup to configure Serilog file sink.
/// </remarks>
public static class AppLog
{
    /// <summary>Maximum number of entries retained in memory.</summary>
    public const int MaxEntries = 5000;

    private static readonly ConcurrentQueue<LogEntry> _entries = new();
    private static int _count;

    /// <summary>Directory where rolling log files are stored.</summary>
    public static string? LogDirectory { get; private set; }

    /// <summary>File pattern used for rolling log files.</summary>
    public static string? LogFilePattern { get; private set; }

    /// <summary>
    /// Raised when a new entry is added. Subscribers must marshal to the UI thread if needed.
    /// </summary>
    public static event Action<LogEntry>? EntryAdded;

    /// <summary>
    /// All retained log entries (newest last).
    /// </summary>
    public static IEnumerable<LogEntry> Entries => _entries;

    /// <summary>
    /// Initializes Serilog with a file sink in the specified directory.
    /// </summary>
    /// <param name="logDirectory">Directory where log files are written.</param>
    public static void Initialize(string logDirectory)
    {
        Directory.CreateDirectory(logDirectory);
        var logPath = Path.Combine(logDirectory, "texturehawk_.log");
        LogDirectory = logDirectory;
        LogFilePattern = "texturehawk_*.log";

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.File(logPath,
                rollingInterval: RollingInterval.Day,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}",
                retainedFileCountLimit: 7)
            .CreateLogger();

        Info("Log", $"Logging initialized — {logDirectory}");
    }

    /// <summary>Logs a debug message.</summary>
    public static void Debug(string category, string message) => Write(LogLevel.Debug, category, message);

    /// <summary>Logs an info message.</summary>
    public static void Info(string category, string message) => Write(LogLevel.Info, category, message);

    /// <summary>Logs a warning message.</summary>
    public static void Warn(string category, string message) => Write(LogLevel.Warning, category, message);

    /// <summary>Logs an error message.</summary>
    public static void Error(string category, string message) => Write(LogLevel.Error, category, message);

    /// <summary>Logs an error with exception details.</summary>
    public static void Error(string category, string message, Exception ex) =>
        Write(LogLevel.Error, category, $"{message}: {ex.Message}");

    /// <summary>
    /// Flushes and closes all Serilog sinks.
    /// </summary>
    public static void Shutdown()
    {
        Log.CloseAndFlush();
    }

    private static void Write(LogLevel level, string category, string message)
    {
        var entry = new LogEntry(DateTime.UtcNow, level, category, message);

        _entries.Enqueue(entry);
        if (Interlocked.Increment(ref _count) > MaxEntries)
        {
            _entries.TryDequeue(out _);
            Interlocked.Decrement(ref _count);
        }

        // Forward to Serilog
        var serilogLevel = level switch
        {
            LogLevel.Debug => LogEventLevel.Debug,
            LogLevel.Info => LogEventLevel.Information,
            LogLevel.Warning => LogEventLevel.Warning,
            LogLevel.Error => LogEventLevel.Error,
            _ => LogEventLevel.Information
        };
        Log.ForContext("SourceContext", category).Write(serilogLevel, "[{Category}] {Message}", category, message);

        EntryAdded?.Invoke(entry);
    }
}
