using Microsoft.AspNetCore.Http;

namespace JLogDashboard.AspNetCore;

internal static class DashboardFailureResponses
{
    public static IResult CreateUnavailableResponse(HttpContext context)
    {
        // Keep dashboard failures inside the dashboard surface and match the endpoint response shape.
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

    public static IResult CreateMisconfiguredResponse(HttpContext context)
    {
        if (IsJsonApiRequest(context))
        {
            return Results.Json(
                new DashboardErrorResponse
                {
                    Error = "DashboardMisconfigured",
                    Message = "JLogDashboard is misconfigured. Fix the Dashboard configuration and try again."
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
  <title>JLogDashboard misconfigured</title>
</head>
<body>
  <h1>JLogDashboard is misconfigured.</h1>
  <p>Fix the Dashboard configuration and try again. The host application is still running.</p>
</body>
</html>
""";

            return Results.Content(html, "text/html; charset=utf-8", statusCode: StatusCodes.Status503ServiceUnavailable);
        }

        return Results.Json(
            new DashboardErrorResponse
            {
                Error = "DashboardMisconfigured",
                Message = "JLogDashboard is misconfigured. Fix the Dashboard configuration and try again."
            },
            statusCode: StatusCodes.Status503ServiceUnavailable);
    }

    private static bool IsJsonApiRequest(HttpContext context)
        => context.Request.Path.Value?.Contains("/api/", StringComparison.OrdinalIgnoreCase) == true;

    private static bool AcceptsHtml(HttpContext context)
    {
        var accept = context.Request.Headers.Accept.ToString();
        return string.IsNullOrWhiteSpace(accept)
               || accept.Contains("text/html", StringComparison.OrdinalIgnoreCase)
               || accept.Contains("*/*", StringComparison.OrdinalIgnoreCase);
    }
}
