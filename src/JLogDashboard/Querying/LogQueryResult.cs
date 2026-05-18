namespace JLogDashboard.Querying;

/// <summary>
/// Contains paged log search results.
/// </summary>
public sealed class LogQueryResult
{
    /// <summary>The total number of entries matching the query before paging.</summary>
    public int Total { get; init; }

    /// <summary>The returned 1-based page index.</summary>
    public int Page { get; init; }

    /// <summary>The effective page size.</summary>
    public int PageSize { get; init; }

    /// <summary>The entries in the requested page.</summary>
    public IReadOnlyList<LogEntry> Items { get; init; } = Array.Empty<LogEntry>();
}
