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
    {
        // Keep dashboard failures inside the dashboard surface and match the endpoint response shape.
        if (IsPlainTextApiRequest(context))
        {
            return Results.Text(
                "JLogDashboard request failed.",
                "text/plain; charset=utf-8",
                statusCode: StatusCodes.Status503ServiceUnavailable);
        }

        if (IsJsonApiRequest(context))
        {
            return Results.Json(
                new DashboardErrorResponse
                {
                    Error = "DashboardUnavailable",
                    Message = "JLogDashboard request failed."
                },
                statusCode: StatusCodes.Status503ServiceUnavailable);
        }

        if (AcceptsHtml(context))
        {
            const string html = """
<!DOCTYPE html>
<html lang="en">
<head>
  <meta charset="utf-8" />
  <meta name="viewport" content="width=device-width, initial-scale=1" />
  <title>JLogDashboard unavailable</title>
</head>
<body>
  <h1>JLogDashboard is temporarily unavailable.</h1>
  <p>The dashboard request failed, but the host application is still running.</p>
</body>
</html>
""";

            return Results.Content(html, "text/html; charset=utf-8", statusCode: StatusCodes.Status503ServiceUnavailable);
        }

        return Results.Json(
            new DashboardErrorResponse(),
            statusCode: StatusCodes.Status503ServiceUnavailable);
    }

    private static bool IsJsonApiRequest(HttpContext context)
        => context.Request.Path.Value?.Contains("/api/", StringComparison.OrdinalIgnoreCase) == true;

    private static bool IsPlainTextApiRequest(HttpContext context)
        => context.Request.Path.Value?.EndsWith("/api/nginx", StringComparison.OrdinalIgnoreCase) == true;

    private static bool AcceptsHtml(HttpContext context)
    {
        var accept = context.Request.Headers.Accept.ToString();
        return string.IsNullOrWhiteSpace(accept)
               || accept.Contains("text/html", StringComparison.OrdinalIgnoreCase)
               || accept.Contains("*/*", StringComparison.OrdinalIgnoreCase);
    }
}
