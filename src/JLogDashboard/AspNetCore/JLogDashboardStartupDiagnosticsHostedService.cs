using JLogDashboard.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace JLogDashboard.AspNetCore;

internal sealed class JLogDashboardStartupDiagnosticsHostedService : IHostedService
{
    private readonly JLogDashboardConfigurationState _state;
    private readonly ILogger<JLogDashboardStartupDiagnosticsHostedService> _logger;

    public JLogDashboardStartupDiagnosticsHostedService(
        JLogDashboardConfigurationState state,
        ILogger<JLogDashboardStartupDiagnosticsHostedService> logger)
    {
        _state = state;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        foreach (var issue in _state.Analysis.Errors)
        {
            _logger.LogError("JLogDashboard configuration error: {Message}", issue.Message);
        }

        foreach (var issue in _state.Analysis.Warnings)
        {
            _logger.LogWarning("JLogDashboard configuration warning: {Message}", issue.Message);
        }

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
