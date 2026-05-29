namespace JLogDashboard.AspNetCore;

internal sealed record DashboardPageModel(
    string RoutePrefix,
    string Culture,
    IReadOnlyList<string> Projects,
    IReadOnlyDictionary<string, string> Text);
