namespace JLogDashboard.Configuration;

/// <summary>
/// Configures the Dashboard route, query limits, authentication, and log projects.
/// </summary>
public sealed class JLogDashboardOptions
{
    /// <summary>The URL prefix used for the Dashboard and its APIs.</summary>
    public string RoutePrefix { get; set; } = "/jlog";

    /// <summary>The UI culture. Built-in values include <c>zh-CN</c> and <c>en-US</c>.</summary>
    public string Culture { get; set; } = "zh-CN";

    /// <summary>The page size used when a request omits or passes an invalid page size.</summary>
    public int DefaultPageSize { get; set; } = 50;

    /// <summary>The maximum allowed page size for a single query.</summary>
    public int MaxPageSize { get; set; } = 500;

    /// <summary>The maximum number of bytes read from the tail of each log file.</summary>
    public long MaxFileBytes { get; set; } = 10 * 1024 * 1024;

    /// <summary>Basic Auth settings for the Dashboard route.</summary>
    public BasicAuthOptions BasicAuth { get; set; } = new();

    /// <summary>The configured projects whose log files can be searched.</summary>
    public List<LogProjectOptions> Projects { get; } = new();

    /// <summary>Returns human-readable configuration validation errors.</summary>
    public IEnumerable<string> Validate()
    {
        if (DefaultPageSize <= 0)
        {
            yield return "DefaultPageSize must be greater than 0.";
        }

        if (MaxPageSize < DefaultPageSize)
        {
            yield return "MaxPageSize must be greater than or equal to DefaultPageSize.";
        }

        if (MaxFileBytes <= 0)
        {
            yield return "MaxFileBytes must be greater than 0.";
        }

        if (BasicAuth.Enabled)
        {
            if (string.IsNullOrWhiteSpace(BasicAuth.Username))
            {
                yield return "BasicAuth Username is required when BasicAuth is enabled.";
            }

            if (string.IsNullOrWhiteSpace(BasicAuth.Password)
                && string.IsNullOrWhiteSpace(BasicAuth.PasswordSha256))
            {
                yield return "BasicAuth Password or PasswordSha256 is required when BasicAuth is enabled.";
            }

            if (string.Equals(BasicAuth.Password, "change-me", StringComparison.Ordinal))
            {
                yield return "BasicAuth Password uses the default value 'change-me'. Change it before exposing the Dashboard.";
            }

            if (BasicAuth.MaxFailedAttempts <= 0)
            {
                yield return "BasicAuth MaxFailedAttempts must be greater than 0.";
            }

            if (BasicAuth.LockoutSeconds <= 0)
            {
                yield return "BasicAuth LockoutSeconds must be greater than 0.";
            }
        }

        if (Projects.Count == 0)
        {
            yield return "At least one log project must be configured.";
        }

        foreach (var project in Projects)
        {
            if (string.IsNullOrWhiteSpace(project.Name))
            {
                yield return "Project Name is required.";
            }

            if (string.IsNullOrWhiteSpace(project.DirectoryPath))
            {
                yield return $"Project '{project.Name}' DirectoryPath is required.";
            }
        }

        foreach (var duplicate in Projects
                     .Where(project => !string.IsNullOrWhiteSpace(project.Name))
                     .GroupBy(project => project.Name, StringComparer.OrdinalIgnoreCase)
                     .Where(group => group.Count() > 1)
                     .Select(group => group.Key))
        {
            yield return $"Project name '{duplicate}' is duplicate.";
        }
    }
}
