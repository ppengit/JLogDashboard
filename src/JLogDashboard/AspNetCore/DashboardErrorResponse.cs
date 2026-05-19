namespace JLogDashboard.AspNetCore;

internal sealed class DashboardErrorResponse
{
    public string Error { get; init; } = "DashboardUnavailable";

    public string Message { get; init; } = "JLogDashboard request failed.";
}
