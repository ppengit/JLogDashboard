namespace JLogDashboard.AspNetCore;

internal sealed record DashboardPageModel(
    string RoutePrefix,
    string Culture,
    string Origin,
    IReadOnlyList<string> Projects,
    IReadOnlyDictionary<string, string> Text);
