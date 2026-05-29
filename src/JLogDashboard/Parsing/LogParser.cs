using System.Collections.Concurrent;
using System.Globalization;
using System.Text;
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

    private static readonly TimeSpan CustomRegexTimeout = TimeSpan.FromSeconds(1);
    private static readonly ConcurrentDictionary<string, NLogLayoutPattern> NLogLayoutPatternCache = new(StringComparer.Ordinal);
    private static readonly ConcurrentDictionary<string, TextLayoutPattern> Log4NetLayoutPatternCache = new(StringComparer.Ordinal);
    private static readonly ConcurrentDictionary<string, TextLayoutPattern> SerilogTemplatePatternCache = new(StringComparer.Ordinal);
    private static readonly Regex NLogLayoutTokenRegex = new(
        @"\$\{(?<body>[^{}]+)\}",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);
    private static readonly Regex NLogNewlineRegex = new(
        @"\$\{newline(?::[^}]*)?\}",
        RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
    private static readonly Regex Log4NetPatternTokenRegex = new(
        @"%(?:[-+]?\d+)?(?:\.\d+)?(?<word>[A-Za-z]+)(?:\{(?<option>[^}]*)\})?",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);
    private static readonly Regex Log4NetNewlineRegex = new(
        @"%(?:n|newline)\b",
        RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
    private static readonly Regex SerilogTemplateTokenRegex = new(
        @"\{(?<body>[^{}]+)\}",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);
    private static readonly Regex SerilogNewlineRegex = new(
        @"\{NewLine(?::[^}]*)?\}",
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
        if (TryParseConfiguredLine(context, line, lineNumber, out parsed))
        {
            return true;
        }

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

    private static bool TryParseConfiguredLine(
        LogParseContext context,
        string line,
        int lineNumber,
        out LogEntryBuilder parsed)
    {
        var options = context.ParserOptions;
        var mode = options?.Mode?.Trim().ToLowerInvariant();
        if (options is null || string.IsNullOrWhiteSpace(mode) || mode == "auto")
        {
            parsed = null!;
            return false;
        }

        return mode switch
        {
            "delimited" => TryParseDelimitedLine(context, options, line, lineNumber, out parsed),
            "regex" => TryParseRegexLine(context, options, line, lineNumber, out parsed),
            "nlog-layout" => TryParseNLogLayoutLine(context, options, line, lineNumber, out parsed),
            "log4net-pattern" when !string.IsNullOrWhiteSpace(options.Layout) => TryParseTextLayoutLine(
                context,
                options,
                line,
                lineNumber,
                Log4NetLayoutPatternCache.GetOrAdd(options.Layout, CreateLog4NetLayoutPattern),
                out parsed),
            "serilog-template" when !string.IsNullOrWhiteSpace(options.Layout) => TryParseTextLayoutLine(
                context,
                options,
                line,
                lineNumber,
                SerilogTemplatePatternCache.GetOrAdd(options.Layout, CreateSerilogTemplatePattern),
                out parsed),
            _ => ReturnFalse(out parsed)
        };
    }

    private static bool TryParseDelimitedLine(
        LogParseContext context,
        LogParserOptions options,
        string line,
        int lineNumber,
        out LogEntryBuilder parsed)
    {
        var delimiter = string.IsNullOrEmpty(options.Delimiter) ? "|" : options.Delimiter;
        var fields = line.Split(delimiter, StringSplitOptions.None);

        if (!TryGetField(fields, options.TimestampIndex, out var timestamp)
            || !TryGetField(fields, options.LevelIndex, out var level))
        {
            parsed = null!;
            return false;
        }

        var message = TryGetField(fields, options.MessageIndex, out var messageValue)
            ? messageValue
            : string.Empty;
        var logger = options.LoggerIndex is { } loggerIndex && TryGetField(fields, loggerIndex, out var loggerValue)
            ? loggerValue
            : string.Empty;
        var exception = options.ExceptionIndex is { } exceptionIndex && TryGetField(fields, exceptionIndex, out var exceptionValue)
            ? exceptionValue
            : string.Empty;

        if (!TryParseTimestamp(timestamp, options.TimestampFormat, out var parsedTimestamp))
        {
            parsed = null!;
            return false;
        }

        parsed = new LogEntryBuilder(
            context.Project,
            NormalizeProvider(context.Provider),
            context.SourcePath,
            parsedTimestamp,
            ParseLevel(level),
            logger,
            message,
            lineNumber,
            exception);
        return true;
    }

    private static bool TryParseNLogLayoutLine(
        LogParseContext context,
        LogParserOptions options,
        string line,
        int lineNumber,
        out LogEntryBuilder parsed)
    {
        if (string.IsNullOrWhiteSpace(options.Layout))
        {
            parsed = null!;
            return false;
        }

        var pattern = NLogLayoutPatternCache.GetOrAdd(options.Layout, CreateNLogLayoutPattern);
        if (!pattern.IsValid)
        {
            parsed = null!;
            return false;
        }

        Match match;
        try
        {
            match = pattern.Regex!.Match(line);
        }
        catch (RegexMatchTimeoutException)
        {
            parsed = null!;
            return false;
        }

        if (!match.Success
            || !TryGetGroup(match, "timestamp", out var timestamp)
            || !TryGetGroup(match, "level", out var level))
        {
            parsed = null!;
            return false;
        }

        var logger = TryGetGroup(match, "logger", out var loggerValue) ? loggerValue : string.Empty;
        var message = TryGetGroup(match, "message", out var messageValue) ? messageValue : string.Empty;
        var exception = TryGetGroup(match, "exception", out var exceptionValue) ? exceptionValue : string.Empty;
        var timestampFormat = string.IsNullOrWhiteSpace(options.TimestampFormat)
            ? pattern.TimestampFormat
            : options.TimestampFormat;

        if (!TryParseTimestamp(timestamp, timestampFormat, out var parsedTimestamp))
        {
            parsed = null!;
            return false;
        }

        parsed = new LogEntryBuilder(
            context.Project,
            NormalizeProvider(context.Provider),
            context.SourcePath,
            parsedTimestamp,
            ParseLevel(level),
            logger,
            message,
            lineNumber,
            exception);
        return true;
    }

    private static bool TryParseTextLayoutLine(
        LogParseContext context,
        LogParserOptions options,
        string line,
        int lineNumber,
        TextLayoutPattern pattern,
        out LogEntryBuilder parsed)
    {
        if (string.IsNullOrWhiteSpace(options.Layout) || !pattern.IsValid)
        {
            parsed = null!;
            return false;
        }

        Match match;
        try
        {
            match = pattern.Regex!.Match(line);
        }
        catch (RegexMatchTimeoutException)
        {
            parsed = null!;
            return false;
        }

        if (!match.Success
            || !TryGetGroup(match, "timestamp", out var timestamp)
            || !TryGetGroup(match, "level", out var level))
        {
            parsed = null!;
            return false;
        }

        var logger = TryGetGroup(match, "logger", out var loggerValue) ? loggerValue : string.Empty;
        var message = TryGetGroup(match, "message", out var messageValue) ? messageValue : string.Empty;
        var exception = TryGetGroup(match, "exception", out var exceptionValue) ? exceptionValue : string.Empty;
        var timestampFormat = string.IsNullOrWhiteSpace(options.TimestampFormat)
            ? pattern.TimestampFormat
            : options.TimestampFormat;

        if (!TryParseTimestamp(timestamp, timestampFormat, out var parsedTimestamp))
        {
            parsed = null!;
            return false;
        }

        parsed = new LogEntryBuilder(
            context.Project,
            NormalizeProvider(context.Provider),
            context.SourcePath,
            parsedTimestamp,
            ParseLevel(level),
            logger,
            message,
            lineNumber,
            exception);
        return true;
    }

    private static bool TryParseRegexLine(
        LogParseContext context,
        LogParserOptions options,
        string line,
        int lineNumber,
        out LogEntryBuilder parsed)
    {
        if (string.IsNullOrWhiteSpace(options.Pattern))
        {
            parsed = null!;
            return false;
        }

        Match match;
        try
        {
            match = Regex.Match(
                line,
                options.Pattern,
                RegexOptions.CultureInvariant,
                CustomRegexTimeout);
        }
        catch (ArgumentException)
        {
            parsed = null!;
            return false;
        }
        catch (RegexMatchTimeoutException)
        {
            parsed = null!;
            return false;
        }

        if (!match.Success
            || !TryGetGroup(match, "timestamp", out var timestamp)
            || !TryGetGroup(match, "level", out var level)
            || !TryGetGroup(match, "message", out var message))
        {
            parsed = null!;
            return false;
        }

        var logger = TryGetGroup(match, "logger", out var loggerValue) ? loggerValue : string.Empty;
        var exception = TryGetGroup(match, "exception", out var exceptionValue) ? exceptionValue : string.Empty;

        if (!TryParseTimestamp(timestamp, options.TimestampFormat, out var parsedTimestamp))
        {
            parsed = null!;
            return false;
        }

        parsed = new LogEntryBuilder(
            context.Project,
            NormalizeProvider(context.Provider),
            context.SourcePath,
            parsedTimestamp,
            ParseLevel(level),
            logger,
            message,
            lineNumber,
            exception);
        return true;
    }

    private static bool TryGetField(string[] fields, int index, out string value)
    {
        if (index < 0 || index >= fields.Length)
        {
            value = string.Empty;
            return false;
        }

        value = fields[index].Trim();
        return !string.IsNullOrWhiteSpace(value);
    }

    private static bool TryGetGroup(Match match, string name, out string value)
    {
        var group = match.Groups[name];
        if (!group.Success)
        {
            value = string.Empty;
            return false;
        }

        value = group.Value.Trim();
        return !string.IsNullOrWhiteSpace(value);
    }

    private static bool ReturnFalse(out LogEntryBuilder parsed)
    {
        parsed = null!;
        return false;
    }

    private static NLogLayoutPattern CreateNLogLayoutPattern(string layout)
    {
        var newlineMatch = NLogNewlineRegex.Match(layout);
        var headerLayout = newlineMatch.Success ? layout[..newlineMatch.Index] : layout;
        if (string.IsNullOrWhiteSpace(headerLayout))
        {
            return NLogLayoutPattern.Invalid;
        }

        var parts = ParseNLogLayoutParts(headerLayout);
        RemoveAdjacentExceptionAfterMessage(parts);

        var regexBuilder = new StringBuilder("^");
        var capturedFields = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var hasTimestamp = false;
        var hasLevel = false;
        string? timestampFormat = null;

        for (var i = 0; i < parts.Count; i++)
        {
            var part = parts[i];
            if (part.Literal is { } literal)
            {
                regexBuilder.Append(Regex.Escape(literal));
                continue;
            }

            var fieldName = part.Field switch
            {
                NLogLayoutField.Timestamp => "timestamp",
                NLogLayoutField.Level => "level",
                NLogLayoutField.Logger => "logger",
                NLogLayoutField.Message => "message",
                NLogLayoutField.Exception => "exception",
                _ => string.Empty
            };
            var hasLaterLiteral = parts.Skip(i + 1).Any(next => !string.IsNullOrEmpty(next.Literal));
            var valuePattern = FieldValuePattern(part.Field, hasLaterLiteral);

            if (string.IsNullOrEmpty(fieldName) || !capturedFields.Add(fieldName))
            {
                regexBuilder.Append(valuePattern);
                continue;
            }

            regexBuilder.Append("(?<").Append(fieldName).Append(">").Append(valuePattern).Append(')');
            hasTimestamp |= part.Field == NLogLayoutField.Timestamp;
            hasLevel |= part.Field == NLogLayoutField.Level;
            timestampFormat ??= part.TimestampFormat;
        }

        regexBuilder.Append('$');

        if (!hasTimestamp || !hasLevel)
        {
            return NLogLayoutPattern.Invalid;
        }

        try
        {
            return new NLogLayoutPattern(
                isValid: true,
                new Regex(regexBuilder.ToString(), RegexOptions.CultureInvariant, CustomRegexTimeout),
                timestampFormat);
        }
        catch (ArgumentException)
        {
            return NLogLayoutPattern.Invalid;
        }
    }

    private static List<NLogLayoutPart> ParseNLogLayoutParts(string layout)
    {
        var parts = new List<NLogLayoutPart>();
        var currentIndex = 0;
        foreach (Match match in NLogLayoutTokenRegex.Matches(layout))
        {
            if (match.Index > currentIndex)
            {
                parts.Add(NLogLayoutPart.ForLiteral(layout[currentIndex..match.Index]));
            }

            parts.Add(NLogLayoutPart.ForToken(match.Groups["body"].Value));
            currentIndex = match.Index + match.Length;
        }

        if (currentIndex < layout.Length)
        {
            parts.Add(NLogLayoutPart.ForLiteral(layout[currentIndex..]));
        }

        return parts;
    }

    private static void RemoveAdjacentExceptionAfterMessage(List<NLogLayoutPart> parts)
    {
        for (var i = parts.Count - 1; i > 0; i--)
        {
            if (parts[i].Field == NLogLayoutField.Exception && parts[i - 1].Field == NLogLayoutField.Message)
            {
                parts.RemoveAt(i);
            }
        }
    }

    private static string FieldValuePattern(NLogLayoutField field, bool hasLaterLiteral)
    {
        if (field == NLogLayoutField.Ignored)
        {
            return hasLaterLiteral ? ".*?" : ".*";
        }

        return hasLaterLiteral ? "[^\\r\\n]+?" : "[^\\r\\n]*";
    }

    private static TextLayoutPattern CreateLog4NetLayoutPattern(string layout)
    {
        var newlineMatch = Log4NetNewlineRegex.Match(layout);
        var headerLayout = newlineMatch.Success ? layout[..newlineMatch.Index] : layout;
        if (string.IsNullOrWhiteSpace(headerLayout))
        {
            return TextLayoutPattern.Invalid;
        }

        var parts = ParseLog4NetPatternParts(headerLayout);
        RemoveAdjacentExceptionAfterMessage(parts);
        return CreateTextLayoutPattern(parts);
    }

    private static TextLayoutPattern CreateSerilogTemplatePattern(string layout)
    {
        var newlineMatch = SerilogNewlineRegex.Match(layout);
        var headerLayout = newlineMatch.Success ? layout[..newlineMatch.Index] : layout;
        if (string.IsNullOrWhiteSpace(headerLayout))
        {
            return TextLayoutPattern.Invalid;
        }

        var parts = ParseSerilogTemplateParts(headerLayout);
        RemoveAdjacentExceptionAfterMessage(parts);
        return CreateTextLayoutPattern(parts);
    }

    private static TextLayoutPattern CreateTextLayoutPattern(List<TextLayoutPart> parts)
    {
        var regexBuilder = new StringBuilder("^");
        var capturedFields = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var hasTimestamp = false;
        var hasLevel = false;
        string? timestampFormat = null;

        for (var i = 0; i < parts.Count; i++)
        {
            var part = parts[i];
            if (part.Literal is { } literal)
            {
                regexBuilder.Append(Regex.Escape(literal));
                continue;
            }

            var fieldName = part.Field switch
            {
                TextLayoutField.Timestamp => "timestamp",
                TextLayoutField.Level => "level",
                TextLayoutField.Logger => "logger",
                TextLayoutField.Message => "message",
                TextLayoutField.Exception => "exception",
                _ => string.Empty
            };
            var hasLaterLiteral = parts.Skip(i + 1).Any(next => !string.IsNullOrEmpty(next.Literal));
            var valuePattern = FieldValuePattern(part.Field, hasLaterLiteral);

            if (string.IsNullOrEmpty(fieldName) || !capturedFields.Add(fieldName))
            {
                regexBuilder.Append(valuePattern);
                continue;
            }

            regexBuilder.Append("(?<").Append(fieldName).Append(">").Append(valuePattern).Append(')');
            hasTimestamp |= part.Field == TextLayoutField.Timestamp;
            hasLevel |= part.Field == TextLayoutField.Level;
            timestampFormat ??= part.TimestampFormat;
        }

        regexBuilder.Append('$');

        if (!hasTimestamp || !hasLevel)
        {
            return TextLayoutPattern.Invalid;
        }

        try
        {
            return new TextLayoutPattern(
                isValid: true,
                new Regex(regexBuilder.ToString(), RegexOptions.CultureInvariant, CustomRegexTimeout),
                timestampFormat);
        }
        catch (ArgumentException)
        {
            return TextLayoutPattern.Invalid;
        }
    }

    private static List<TextLayoutPart> ParseLog4NetPatternParts(string layout)
    {
        var parts = new List<TextLayoutPart>();
        var currentIndex = 0;
        foreach (Match match in Log4NetPatternTokenRegex.Matches(layout))
        {
            if (match.Index > currentIndex)
            {
                parts.Add(TextLayoutPart.ForLiteral(layout[currentIndex..match.Index]));
            }

            parts.Add(TextLayoutPart.ForField(
                ToLog4NetLayoutField(match.Groups["word"].Value),
                match.Groups["option"].Success ? match.Groups["option"].Value : null));
            currentIndex = match.Index + match.Length;
        }

        if (currentIndex < layout.Length)
        {
            parts.Add(TextLayoutPart.ForLiteral(layout[currentIndex..]));
        }

        return parts;
    }

    private static List<TextLayoutPart> ParseSerilogTemplateParts(string layout)
    {
        var parts = new List<TextLayoutPart>();
        var currentIndex = 0;
        foreach (Match match in SerilogTemplateTokenRegex.Matches(layout))
        {
            if (match.Index > currentIndex)
            {
                parts.Add(TextLayoutPart.ForLiteral(layout[currentIndex..match.Index]));
            }

            var body = match.Groups["body"].Value;
            var separatorIndex = body.IndexOf(':', StringComparison.Ordinal);
            var propertyName = separatorIndex < 0 ? body : body[..separatorIndex];
            var format = separatorIndex < 0 ? null : body[(separatorIndex + 1)..];
            parts.Add(TextLayoutPart.ForField(ToSerilogTemplateField(propertyName), format));
            currentIndex = match.Index + match.Length;
        }

        if (currentIndex < layout.Length)
        {
            parts.Add(TextLayoutPart.ForLiteral(layout[currentIndex..]));
        }

        return parts;
    }

    private static void RemoveAdjacentExceptionAfterMessage(List<TextLayoutPart> parts)
    {
        for (var i = parts.Count - 1; i > 0; i--)
        {
            if (parts[i].Field == TextLayoutField.Exception && parts[i - 1].Field == TextLayoutField.Message)
            {
                parts.RemoveAt(i);
            }
        }
    }

    private static string FieldValuePattern(TextLayoutField field, bool hasLaterLiteral)
    {
        if (field == TextLayoutField.Ignored)
        {
            return hasLaterLiteral ? ".*?" : ".*";
        }

        return hasLaterLiteral ? "[^\\r\\n]+?" : "[^\\r\\n]*";
    }

    private static TextLayoutField ToLog4NetLayoutField(string word)
        => word.Trim().ToLowerInvariant() switch
        {
            "d" or "date" or "utcdate" => TextLayoutField.Timestamp,
            "p" or "level" => TextLayoutField.Level,
            "c" or "logger" => TextLayoutField.Logger,
            "m" or "msg" or "message" => TextLayoutField.Message,
            "ex" or "exception" => TextLayoutField.Exception,
            _ => TextLayoutField.Ignored
        };

    private static TextLayoutField ToSerilogTemplateField(string propertyName)
        => propertyName.Trim() switch
        {
            "Timestamp" => TextLayoutField.Timestamp,
            "Level" => TextLayoutField.Level,
            "SourceContext" => TextLayoutField.Logger,
            "Message" => TextLayoutField.Message,
            "Exception" => TextLayoutField.Exception,
            _ => TextLayoutField.Ignored
        };

    private static NLogLayoutField ToNLogLayoutField(string rendererName)
        => rendererName.Trim().ToLowerInvariant() switch
        {
            "longdate" or "date" => NLogLayoutField.Timestamp,
            "level" => NLogLayoutField.Level,
            "logger" => NLogLayoutField.Logger,
            "message" => NLogLayoutField.Message,
            "exception" => NLogLayoutField.Exception,
            _ => NLogLayoutField.Ignored
        };

    private static string RendererName(string rendererBody)
    {
        var separatorIndex = rendererBody.IndexOf(':', StringComparison.Ordinal);
        return separatorIndex < 0 ? rendererBody : rendererBody[..separatorIndex];
    }

    private static string? ExtractNLogOption(string rendererBody, string optionName)
    {
        var marker = optionName + "=";
        var start = rendererBody.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
        if (start < 0)
        {
            return null;
        }

        start += marker.Length;
        var builder = new StringBuilder();
        var escaping = false;
        for (var i = start; i < rendererBody.Length; i++)
        {
            var character = rendererBody[i];
            if (escaping)
            {
                builder.Append(character);
                escaping = false;
                continue;
            }

            if (character == '\\')
            {
                escaping = true;
                continue;
            }

            if (character == ':')
            {
                break;
            }

            builder.Append(character);
        }

        var value = builder.ToString().Trim();
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    private static string NormalizeProvider(string provider)
        => string.IsNullOrWhiteSpace(provider) ? "auto" : provider.Trim().ToLowerInvariant();

    private static DateTimeOffset ParseTimestamp(string value, string? configuredFormat = null)
    {
        if (TryParseTimestamp(value, configuredFormat, out var timestamp))
        {
            return timestamp;
        }

        return new DateTimeOffset(DateTime.Parse(value.Trim(), CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces), TimeSpan.Zero);
    }

    private static bool TryParseTimestamp(string value, string? configuredFormat, out DateTimeOffset timestamp)
    {
        var normalized = value.Trim().Replace(',', '.');
        var builtInFormats = new[]
        {
            "yyyy-MM-dd HH:mm:ss",
            "yyyy-MM-dd HH:mm:ss.FFFFFFF"
        };
        var formats = string.IsNullOrWhiteSpace(configuredFormat)
            ? builtInFormats
            : new[] { configuredFormat }.Concat(builtInFormats).ToArray();

        if (!DateTime.TryParseExact(
                normalized,
                formats,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AllowWhiteSpaces,
                out var parsed))
        {
            if (!DateTime.TryParse(normalized, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out parsed))
            {
                timestamp = default;
                return false;
            }
        }

        var utc = DateTime.SpecifyKind(parsed, DateTimeKind.Unspecified);
        utc = new DateTime(utc.Ticks - utc.Ticks % TimeSpan.TicksPerMillisecond, DateTimeKind.Unspecified);
        timestamp = new DateTimeOffset(utc, TimeSpan.Zero);
        return true;
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
            int lineNumber,
            string exception = "")
        {
            Project = project;
            Provider = provider;
            SourcePath = sourcePath;
            Timestamp = timestamp;
            Level = level;
            Logger = logger;
            Message = message;
            LineNumber = lineNumber;
            if (!string.IsNullOrWhiteSpace(exception))
            {
                _continuationLines.Add(exception);
            }
        }

        private string Project { get; }

        private string Provider { get; }

        private string SourcePath { get; }

        private DateTimeOffset Timestamp { get; }

        private LogLevel Level { get; }

        private string Logger { get; }

        private string Message { get; set; }

        private int LineNumber { get; }

        public void AppendContinuation(string line)
        {
            if (string.IsNullOrWhiteSpace(line) || IsSeparatorLine(line))
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(Message))
            {
                Message = line.Trim();
            }
            else
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

        private static bool IsSeparatorLine(string line)
        {
            var trimmed = line.Trim();
            return trimmed.Length >= 3 && trimmed.All(character => character == '-');
        }
    }

    private sealed class NLogLayoutPattern
    {
        public static readonly NLogLayoutPattern Invalid = new(false, null, null);

        public NLogLayoutPattern(bool isValid, Regex? regex, string? timestampFormat)
        {
            IsValid = isValid;
            Regex = regex;
            TimestampFormat = timestampFormat;
        }

        public bool IsValid { get; }

        public Regex? Regex { get; }

        public string? TimestampFormat { get; }
    }

    private sealed class NLogLayoutPart
    {
        private NLogLayoutPart(string? literal, NLogLayoutField field, string? timestampFormat)
        {
            Literal = literal;
            Field = field;
            TimestampFormat = timestampFormat;
        }

        public string? Literal { get; }

        public NLogLayoutField Field { get; }

        public string? TimestampFormat { get; }

        public static NLogLayoutPart ForLiteral(string literal)
            => new(literal, NLogLayoutField.None, null);

        public static NLogLayoutPart ForToken(string rendererBody)
        {
            var rendererName = RendererName(rendererBody);
            var field = ToNLogLayoutField(rendererName);
            var timestampFormat = field == NLogLayoutField.Timestamp && rendererName.Equals("date", StringComparison.OrdinalIgnoreCase)
                ? ExtractNLogOption(rendererBody, "format")
                : null;
            return new NLogLayoutPart(null, field, timestampFormat);
        }
    }

    private enum NLogLayoutField
    {
        None,
        Timestamp,
        Level,
        Logger,
        Message,
        Exception,
        Ignored
    }

    private sealed class TextLayoutPattern
    {
        public static readonly TextLayoutPattern Invalid = new(false, null, null);

        public TextLayoutPattern(bool isValid, Regex? regex, string? timestampFormat)
        {
            IsValid = isValid;
            Regex = regex;
            TimestampFormat = timestampFormat;
        }

        public bool IsValid { get; }

        public Regex? Regex { get; }

        public string? TimestampFormat { get; }
    }

    private sealed class TextLayoutPart
    {
        private TextLayoutPart(string? literal, TextLayoutField field, string? timestampFormat)
        {
            Literal = literal;
            Field = field;
            TimestampFormat = timestampFormat;
        }

        public string? Literal { get; }

        public TextLayoutField Field { get; }

        public string? TimestampFormat { get; }

        public static TextLayoutPart ForLiteral(string literal)
            => new(literal, TextLayoutField.None, null);

        public static TextLayoutPart ForField(TextLayoutField field, string? format)
            => new(null, field, field == TextLayoutField.Timestamp ? format : null);
    }

    private enum TextLayoutField
    {
        None,
        Timestamp,
        Level,
        Logger,
        Message,
        Exception,
        Ignored
    }
}
