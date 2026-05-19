namespace JLogDashboard.Configuration;

/// <summary>
/// Describes one actionable Dashboard configuration issue.
/// </summary>
public sealed record JLogDashboardConfigurationIssue(
    JLogDashboardConfigurationIssueSeverity Severity,
    string Message);
