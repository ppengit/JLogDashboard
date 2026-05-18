using JLogDashboard.Configuration;
using Microsoft.AspNetCore.Http;

namespace JLogDashboard.AspNetCore.Security;

internal sealed class JLogDashboardBasicAuthEndpointFilter : IEndpointFilter
{
    private readonly JLogDashboardOptions _options;
    private readonly JLogDashboardBasicAuthGuard _guard;

    public JLogDashboardBasicAuthEndpointFilter(
        JLogDashboardOptions options,
        JLogDashboardBasicAuthGuard guard)
    {
        _options = options;
        _guard = guard;
    }

    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var decision = _guard.Authenticate(context.HttpContext, _options);
        if (decision.Kind == BasicAuthDecisionKind.Allow)
        {
            return await next(context).ConfigureAwait(false);
        }

        if (decision.Kind == BasicAuthDecisionKind.LockedOut)
        {
            var retryAfter = Math.Max(1, (int)Math.Ceiling((decision.RetryAfter ?? TimeSpan.FromSeconds(1)).TotalSeconds));
            context.HttpContext.Response.Headers.RetryAfter = retryAfter.ToString();
            return Results.StatusCode(StatusCodes.Status429TooManyRequests);
        }

        context.HttpContext.Response.Headers.WWWAuthenticate = $"Basic realm=\"{_options.BasicAuth.Realm}\"";
        return Results.Unauthorized();
    }
}
