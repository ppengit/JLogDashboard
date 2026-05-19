using JLogDashboard.Configuration;

namespace JLogDashboard.AspNetCore;

/// <summary>
/// Holds the analyzed Dashboard configuration state for endpoint guards and startup diagnostics.
/// </summary>
internal sealed class JLogDashboardConfigurationState
{
    public JLogDashboardConfigurationState(JLogDashboardConfigurationAnalysis analysis)
    {
        Analysis = analysis;
    }

    public JLogDashboardConfigurationAnalysis Analysis { get; }

    public bool HasErrors => Analysis.HasErrors;
}
