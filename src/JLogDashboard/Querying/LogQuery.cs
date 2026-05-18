namespace JLogDashboard.Querying;

/// <summary>
/// Describes filters for a log search request.
/// </summary>
public sealed class LogQuery
{
    /// <summary>Optional project name filter.</summary>
    public string? Project { get; set; }

    /// <summary>Optional level filters. Empty means all levels.</summary>
    public List<LogLevel> Levels { get; } = new();

    /// <summary>Optional text that must appear in the entry.</summary>
    public string? SearchText { get; set; }

    /// <summary>Optional text that must not appear in the entry.</summary>
    public string? ExcludeText { get; set; }

    /// <summary>Optional inclusive start timestamp.</summary>
    public DateTimeOffset? From { get; set; }

    /// <summary>Optional inclusive end timestamp.</summary>
    public DateTimeOffset? To { get; set; }

    /// <summary>The 1-based page index.</summary>
    public int Page { get; set; } = 1;

    /// <summary>The requested page size.</summary>
    public int PageSize { get; set; } = 50;
}
