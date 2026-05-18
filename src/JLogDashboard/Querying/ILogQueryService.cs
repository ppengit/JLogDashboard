namespace JLogDashboard.Querying;

/// <summary>
/// Provides log search operations for the Dashboard API.
/// </summary>
public interface ILogQueryService
{
    /// <summary>Searches log entries using the supplied filters.</summary>
    Task<LogQueryResult> SearchAsync(LogQuery query, CancellationToken cancellationToken = default);
}
