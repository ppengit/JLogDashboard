namespace JLogDashboard.Configuration;

/// <summary>
/// Aggregates fatal configuration errors and non-fatal operational warnings.
/// </summary>
public sealed class JLogDashboardConfigurationAnalysis
{
    public JLogDashboardConfigurationAnalysis(IEnumerable<JLogDashboardConfigurationIssue> issues)
    {
        Issues = issues.ToArray();
    }

    /// <summary>All discovered configuration issues.</summary>
    public IReadOnlyList<JLogDashboardConfigurationIssue> Issues { get; }

    /// <summary>Issues that block Dashboard requests until fixed.</summary>
    public IReadOnlyList<JLogDashboardConfigurationIssue> Errors
        => Issues.Where(issue => issue.Severity == JLogDashboardConfigurationIssueSeverity.Error).ToArray();

    /// <summary>Issues that should be surfaced to operators but do not block Dashboard requests.</summary>
    public IReadOnlyList<JLogDashboardConfigurationIssue> Warnings
        => Issues.Where(issue => issue.Severity == JLogDashboardConfigurationIssueSeverity.Warning).ToArray();

    /// <summary>Whether the current configuration contains any fatal Dashboard errors.</summary>
    public bool HasErrors => Errors.Count > 0;
}
