namespace JLogDashboard.Parsing;

/// <summary>
/// Carries source metadata used while parsing one log file.
/// </summary>
public sealed record LogParseContext(
    string Project,
    string Provider,
    string SourcePath,
    LogParserOptions? ParserOptions = null);
