namespace JLogDashboard.Parsing;

/// <summary>
/// Configures optional project-specific parsing for log layouts that are not covered by built-in recognizers.
/// </summary>
public sealed class LogParserOptions
{
    /// <summary>
    /// Parser mode. Supported values are <c>auto</c>, <c>delimited</c>, <c>regex</c>, <c>nlog-layout</c>,
    /// <c>log4net-pattern</c>, and <c>serilog-template</c>.
    /// </summary>
    public string Mode { get; set; } = "auto";

    /// <summary>
    /// The layout or template used when <see cref="Mode"/> is <c>nlog-layout</c>, <c>log4net-pattern</c>, or <c>serilog-template</c>.
    /// Supported fields include timestamp/date, level, logger/source context, message, exception, newline, and ignored metadata fields.
    /// </summary>
    public string Layout { get; set; } = string.Empty;

    /// <summary>The delimiter used when <see cref="Mode"/> is <c>delimited</c>.</summary>
    public string Delimiter { get; set; } = "|";

    /// <summary>The regex pattern used when <see cref="Mode"/> is <c>regex</c>. Named groups: timestamp, level, logger, message, exception.</summary>
    public string Pattern { get; set; } = string.Empty;

    /// <summary>The timestamp field index for delimited logs.</summary>
    public int TimestampIndex { get; set; }

    /// <summary>The level field index for delimited logs.</summary>
    public int LevelIndex { get; set; } = 1;

    /// <summary>The logger field index for delimited logs. Leave unset when the layout has no logger field.</summary>
    public int? LoggerIndex { get; set; }

    /// <summary>The message field index for delimited logs.</summary>
    public int MessageIndex { get; set; } = 2;

    /// <summary>The exception field index for delimited logs. Leave unset when exception details are emitted as continuation lines.</summary>
    public int? ExceptionIndex { get; set; }

    /// <summary>
    /// Optional timestamp format used before the built-in timestamp formats are attempted.
    /// </summary>
    public string TimestampFormat { get; set; } = string.Empty;
}
