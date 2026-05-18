using System.Globalization;
using System.Text.RegularExpressions;
using JLogDashboard.Querying;

namespace JLogDashboard.Parsing;

/// <summary>
/// Parses common NLog, log4net, and Serilog text log layouts into structured entries.
/// </summary>
public sealed class LogParser
{
    private static readonly Regex SerilogRegex = new(
        @"^\[(?<timestamp>\d{4}-\d{2}-\d{2}\s+\d{2}:\d{2}:\d{2}(?:[,.]\d{1,7})?)\s+(?<level>[A-Za-z]{3,11})\]\s*(?<message>.*)$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex NLogPipeRegex = new(
        @"^(?<timestamp>\d{4}-\d{2}-\d{2}\s+\d{2}:\d{2}:\d{2}(?:[,.]\d{1,7})?)\|(?<level>[^|]+)\|(?<logger>[^|]*)\|(?<message>.*)$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex Log4NetRegex = new(
        @"^(?<timestamp>\d{4}-\d{2}-\d{2}\s+\d{2}:\d{2}:\d{2}(?:[,.]\d{1,7})?)\s+\[[^\]]+\]\s+(?<level>\w+)\s+(?<logger>.*?)\s+-\s+(?<message>.*)$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex SimpleRegex = new(
        @"^(?<timestamp>\d{4}-\d{2}-\d{2}\s+\d{2}:\d{2}:\d{2}(?:[,.]\d{1,7})?)\s+(?<level>TRACE|DEBUG|INFO|WARN|ERROR|FATAL|INF|WRN|ERR|DBG|FTL)\s+(?<message>.*)$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

    /// <summary>Creates the default parser with built-in layout recognizers.</summary>
    public static LogParser CreateDefault() => new();

    /// <summary>
    /// Parses log lines and groups continuation lines, such as exception stack traces, with the previous entry.
    /// </summary>
    public IEnumerable<LogEntry> Parse(LogParseContext context, IEnumerable<string> lines)
    {
        LogEntryBuilder? current = null;
        var lineNumber = 0;

        foreach (var line in lines)
        {
            lineNumber++;
            if (TryParseLine(context, line, lineNumber, out var parsed))
            {
                if (current is not null)
                {
                    yield return current.Build();
                }

                current = parsed;
                continue;
            }

            if (current is not null)
            {
                current.AppendContinuation(line);
            }
        }

        if (current is not null)
        {
            yield return current.Build();
        }
    }

    private static bool TryParseLine(
        LogParseContext context,
        string line,
        int lineNumber,
        out LogEntryBuilder parsed)
    {
        foreach (var matcher in new[] { SerilogRegex, NLogPipeRegex, Log4NetRegex, SimpleRegex })
        {
            var match = matcher.Match(line);
            if (!match.Success)
            {
                continue;
            }

            parsed = new LogEntryBuilder(
                context.Project,
                NormalizeProvider(context.Provider),
                context.SourcePath,
                ParseTimestamp(match.Groups["timestamp"].Value),
                ParseLevel(match.Groups["level"].Value),
                match.Groups["logger"].Success ? match.Groups["logger"].Value.Trim() : string.Empty,
                match.Groups["message"].Value.Trim(),
                lineNumber);
            return true;
        }

        parsed = null!;
        return false;
    }

    private static string NormalizeProvider(string provider)
        => string.IsNullOrWhiteSpace(provider) ? "auto" : provider.Trim().ToLowerInvariant();

    private static DateTimeOffset ParseTimestamp(string value)
    {
        var normalized = value.Trim().Replace(',', '.');
        var formats = new[]
        {
            "yyyy-MM-dd HH:mm:ss",
            "yyyy-MM-dd HH:mm:ss.FFFFFFF"
        };

        if (!DateTime.TryParseExact(
                normalized,
                formats,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AllowWhiteSpaces,
                out var timestamp))
        {
            timestamp = DateTime.Parse(normalized, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces);
        }

        var utc = DateTime.SpecifyKind(timestamp, DateTimeKind.Unspecified);
        utc = new DateTime(utc.Ticks - utc.Ticks % TimeSpan.TicksPerMillisecond, DateTimeKind.Unspecified);
        return new DateTimeOffset(utc, TimeSpan.Zero);
    }

    private static LogLevel ParseLevel(string value)
    {
        return value.Trim().ToUpperInvariant() switch
        {
            "TRACE" or "TRC" or "VRB" or "VERBOSE" => LogLevel.Trace,
            "DEBUG" or "DBG" => LogLevel.Debug,
            "INFO" or "INF" or "INFORMATION" => LogLevel.Information,
            "WARN" or "WRN" or "WARNING" => LogLevel.Warning,
            "ERROR" or "ERR" => LogLevel.Error,
            "FATAL" or "FTL" or "CRITICAL" => LogLevel.Fatal,
            _ => LogLevel.Information
        };
    }

    private sealed class LogEntryBuilder
    {
        private readonly List<string> _continuationLines = new();

        public LogEntryBuilder(
            string project,
            string provider,
            string sourcePath,
            DateTimeOffset timestamp,
            LogLevel level,
            string logger,
            string message,
            int lineNumber)
        {
            Project = project;
            Provider = provider;
            SourcePath = sourcePath;
            Timestamp = timestamp;
            Level = level;
            Logger = logger;
            Message = message;
            LineNumber = lineNumber;
        }

        private string Project { get; }

        private string Provider { get; }

        private string SourcePath { get; }

        private DateTimeOffset Timestamp { get; }

        private LogLevel Level { get; }

        private string Logger { get; }

        private string Message { get; }

        private int LineNumber { get; }

        public void AppendContinuation(string line)
        {
            if (!string.IsNullOrWhiteSpace(line))
            {
                _continuationLines.Add(line);
            }
        }

        public LogEntry Build()
        {
            return new LogEntry
            {
                Project = Project,
                Provider = Provider,
                SourcePath = SourcePath,
                Timestamp = Timestamp,
                Level = Level,
                Logger = Logger,
                Message = Message,
                Exception = _continuationLines.Count == 0 ? string.Empty : string.Join(Environment.NewLine, _continuationLines),
                LineNumber = LineNumber
            };
        }
    }
}
