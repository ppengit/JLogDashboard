using JLogDashboard.Configuration;
using JLogDashboard.AspNetCore.Security;
using JLogDashboard.Localization;
using JLogDashboard.Querying;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace JLogDashboard.AspNetCore;

/// <summary>
/// Provides endpoint mapping helpers for the JLogDashboard UI and APIs.
/// </summary>
public static class JLogDashboardEndpointRouteBuilderExtensions
{
    private static readonly JsonSerializerOptions DashboardJsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    /// <summary>
    /// Maps the Dashboard HTML page and JSON APIs under <see cref="JLogDashboardOptions.RoutePrefix"/>.
    /// </summary>
    public static IEndpointRouteBuilder MapJLogDashboard(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var options = endpoints.ServiceProvider.GetRequiredService<JLogDashboardOptions>();
        var routePrefix = NormalizeRoutePrefix(options.RoutePrefix);

        var group = endpoints.MapGroup(routePrefix);
        group.AddEndpointFilter<JLogDashboardConfigurationEndpointFilter>();
        group.AddEndpointFilter<JLogDashboardFaultIsolationEndpointFilter>();
        group.AddEndpointFilter<JLogDashboardBasicAuthEndpointFilter>();

        group.MapGet(string.Empty, (HttpContext context) =>
        {
            var localizer = context.RequestServices.GetRequiredService<DashboardLocalizer>();
            var html = DashboardPageRenderer.Render(new DashboardPageModel(
                routePrefix,
                options.Culture,
                options.Projects.Select(project => project.Name).ToArray(),
                CreateTextCatalog(localizer, options.Culture)));
            return Results.Content(html, "text/html; charset=utf-8");
        });

        group.MapPost(
            "/api/search",
            async (HttpContext context, LogQuery query, CancellationToken cancellationToken)
                => Results.Json(
                    await context.RequestServices.GetRequiredService<ILogQueryService>()
                        .SearchAsync(query, cancellationToken)
                        .ConfigureAwait(false),
                    DashboardJsonOptions));

        group.MapGet("/api/projects", (JLogDashboardOptions currentOptions) =>
        {
            var projects = currentOptions.Projects.Select(project => new ProjectSummary
            {
                Name = project.Name,
                Provider = project.Provider,
                DirectoryPath = project.DirectoryPath,
                Exists = Directory.Exists(project.DirectoryPath),
                FileSearchPattern = project.FileSearchPattern,
                Recursive = project.Recursive
            }).ToArray();

            return Results.Json(projects, DashboardJsonOptions);
        });

        return endpoints;
    }

    private static string NormalizeRoutePrefix(string routePrefix)
    {
        if (string.IsNullOrWhiteSpace(routePrefix) || routePrefix == "/")
        {
            return string.Empty;
        }

        return "/" + routePrefix.Trim().Trim('/');
    }

    private static IReadOnlyDictionary<string, string> CreateTextCatalog(DashboardLocalizer localizer, string culture)
    {
        var keys = new[]
        {
            "Subtitle",
            "Refresh",
            "Project",
            "AllProjects",
            "SearchText",
            "ExcludeText",
            "PageSize",
            "Search",
            "LogEntries",
            "Results",
            "Page",
            "PreviousPage",
            "NextPage",
            "Time",
            "Level",
            "Logger",
            "Message",
            "File",
            "RunSearch",
            "NoMatches",
            "Loading",
            "SearchFailed",
            "PlaceholderSearch",
            "PlaceholderExclude",
            "Occurrences"
        };

        return keys.ToDictionary(key => key, key => localizer.Translate(culture, key), StringComparer.OrdinalIgnoreCase);
    }
}
