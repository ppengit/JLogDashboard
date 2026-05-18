namespace JLogDashboard.Querying;

/// <summary>
/// Normalized log levels used by JLogDashboard filters and responses.
/// </summary>
public enum LogLevel
{
    /// <summary>Trace-level diagnostics.</summary>
    Trace = 0,
    /// <summary>Debug-level diagnostics.</summary>
    Debug = 1,
    /// <summary>Informational events.</summary>
    Information = 2,
    /// <summary>Warning events.</summary>
    Warning = 3,
    /// <summary>Error events.</summary>
    Error = 4,
    /// <summary>Fatal or critical events.</summary>
    Fatal = 5
}
