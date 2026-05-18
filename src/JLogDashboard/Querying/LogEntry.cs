namespace JLogDashboard.Querying;

/// <summary>
/// Represents one parsed log entry.
/// </summary>
public sealed class LogEntry
{
    /// <summary>The configured project that owns the log file.</summary>
    public string Project { get; init; } = string.Empty;

    /// <summary>The configured or detected provider layout.</summary>
    public string Provider { get; init; } = string.Empty;

    /// <summary>The source log file path.</summary>
    public string SourcePath { get; init; } = string.Empty;

    /// <summary>The timestamp parsed from the log line.</summary>
    public DateTimeOffset Timestamp { get; init; }

    /// <summary>The normalized log level.</summary>
    public LogLevel Level { get; init; }

    /// <summary>The logger or category name when present in the layout.</summary>
    public string Logger { get; init; } = string.Empty;

    /// <summary>The main log message.</summary>
    public string Message { get; init; } = string.Empty;

    /// <summary>Continuation lines associated with the entry, commonly exception stack traces.</summary>
    public string Exception { get; init; } = string.Empty;

    /// <summary>The line number within the parsed tail window.</summary>
    public int LineNumber { get; init; }
}
