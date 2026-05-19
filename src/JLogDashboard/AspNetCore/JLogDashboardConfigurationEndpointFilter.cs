using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace JLogDashboard.AspNetCore;

internal sealed class JLogDashboardConfigurationEndpointFilter : IEndpointFilter
{
    private readonly JLogDashboardConfigurationState _state;
    private readonly ILogger<JLogDashboardConfigurationEndpointFilter> _logger;

    public JLogDashboardConfigurationEndpointFilter(
        JLogDashboardConfigurationState state,
        ILogger<JLogDashboardConfigurationEndpointFilter> logger)
    {
        _state = state;
        _logger = logger;
    }

    public ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        if (!_state.HasErrors)
        {
            return next(context);
        }

        _logger.LogError(
            "JLogDashboard blocked request for {Method} {Path} because the Dashboard configuration contains fatal errors.",
            context.HttpContext.Request.Method,
            context.HttpContext.Request.Path);

        return ValueTask.FromResult<object?>(DashboardFailureResponses.CreateMisconfiguredResponse(context.HttpContext));
    }
}
