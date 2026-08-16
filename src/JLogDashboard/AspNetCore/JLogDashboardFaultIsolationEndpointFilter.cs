using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace JLogDashboard.AspNetCore;

internal sealed class JLogDashboardFaultIsolationEndpointFilter : IEndpointFilter
{
    private readonly ILogger<JLogDashboardFaultIsolationEndpointFilter> _logger;

    public JLogDashboardFaultIsolationEndpointFilter(ILogger<JLogDashboardFaultIsolationEndpointFilter> logger)
    {
        _logger = logger;
    }

    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        try
        {
            return await next(context).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (context.HttpContext.RequestAborted.IsCancellationRequested)
        {
            throw;
        }
        catch (BadHttpRequestException)
        {
            // Malformed requests are expected client errors (400), not Dashboard faults.
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "JLogDashboard request failed for {Method} {Path}.",
                context.HttpContext.Request.Method,
                context.HttpContext.Request.Path);

            return CreateFailureResponse(context.HttpContext);
        }
    }

    private static IResult CreateFailureResponse(HttpContext context)
        => DashboardFailureResponses.CreateUnavailableResponse(context);
}
