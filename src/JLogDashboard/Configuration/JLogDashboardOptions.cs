namespace JLogDashboard.Configuration;

/// <summary>
/// Configures the Dashboard route, query limits, authentication, and log projects.
/// </summary>
public sealed class JLogDashboardOptions
{
    private static readonly string[] SupportedProviders = ["auto", "serilog", "nlog", "log4net"];
    private static readonly string[] SupportedParserModes = ["auto", "delimited", "regex", "nlog-layout", "log4net-pattern", "serilog-template"];

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

    /// <summary>Analyzes the current Dashboard configuration and classifies issues by severity.</summary>
    public JLogDashboardConfigurationAnalysis Analyze()
    {
        var issues = new List<JLogDashboardConfigurationIssue>();

        if (DefaultPageSize <= 0)
        {
            issues.Add(Error("DefaultPageSize must be greater than 0."));
        }

        if (MaxPageSize < DefaultPageSize)
        {
            issues.Add(Error("MaxPageSize must be greater than or equal to DefaultPageSize."));
        }

        if (MaxFileBytes <= 0)
        {
            issues.Add(Error("MaxFileBytes must be greater than 0."));
        }

        if (string.IsNullOrWhiteSpace(RoutePrefix))
        {
            issues.Add(Warning("RoutePrefix is empty. JLogDashboard will be mapped at the application root."));
        }

        if (!string.Equals(Culture, "zh-CN", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(Culture, "en-US", StringComparison.OrdinalIgnoreCase))
        {
            issues.Add(Warning($"Culture '{Culture}' is not built in. JLogDashboard will fall back to English text resources."));
        }

        if (BasicAuth.Enabled)
        {
            if (string.IsNullOrWhiteSpace(BasicAuth.Username))
            {
                issues.Add(Error("BasicAuth Username is required when BasicAuth is enabled."));
            }

            if (string.IsNullOrWhiteSpace(BasicAuth.Password)
                && string.IsNullOrWhiteSpace(BasicAuth.PasswordSha256))
            {
                issues.Add(Error("BasicAuth Password or PasswordSha256 is required when BasicAuth is enabled."));
            }

            if (!string.IsNullOrWhiteSpace(BasicAuth.Password)
                && !string.IsNullOrWhiteSpace(BasicAuth.PasswordSha256))
            {
                issues.Add(Warning("BasicAuth Password and PasswordSha256 are both configured. PasswordSha256 will take precedence."));
            }

            if (string.Equals(BasicAuth.Password, "change-me", StringComparison.Ordinal))
            {
                issues.Add(Warning("BasicAuth Password uses the default value 'change-me'. Change it before exposing the Dashboard."));
            }

            if (!string.IsNullOrWhiteSpace(BasicAuth.Password))
            {
                issues.Add(Warning("BasicAuth Password is stored in plain text. Prefer PasswordSha256 in shared or production environments."));
            }

            if (!string.IsNullOrWhiteSpace(BasicAuth.PasswordSha256)
                && !BasicAuth.IsPasswordSha256Hex())
            {
                issues.Add(Error("BasicAuth PasswordSha256 must be a 64-character hexadecimal SHA-256 string."));
            }

            if (BasicAuth.MaxFailedAttempts <= 0)
            {
                issues.Add(Error("BasicAuth MaxFailedAttempts must be greater than 0."));
            }

            if (BasicAuth.LockoutSeconds <= 0)
            {
                issues.Add(Error("BasicAuth LockoutSeconds must be greater than 0."));
            }
        }
        else
        {
            issues.Add(Warning("BasicAuth is disabled. Do not expose the Dashboard directly to untrusted networks."));
        }

        if (Projects.Count == 0)
        {
            issues.Add(Error("At least one log project must be configured."));
        }

        foreach (var project in Projects)
        {
            if (string.IsNullOrWhiteSpace(project.Name))
            {
                issues.Add(Error("Project Name is required."));
            }

            if (string.IsNullOrWhiteSpace(project.DirectoryPath))
            {
                issues.Add(Error($"Project '{project.Name}' DirectoryPath is required."));
            }
            else if (!Directory.Exists(project.DirectoryPath))
            {
                issues.Add(Warning($"Project '{project.Name}' directory '{project.DirectoryPath}' does not exist at startup."));
            }

            if (string.IsNullOrWhiteSpace(project.Provider))
            {
                issues.Add(Warning($"Project '{project.Name}' Provider is empty. JLogDashboard will treat it as 'auto'."));
            }
            else if (!SupportedProviders.Contains(project.Provider.Trim(), StringComparer.OrdinalIgnoreCase))
            {
                issues.Add(Warning(
                    $"Project '{project.Name}' Provider '{project.Provider}' is not a built-in value. JLogDashboard will still attempt generic parsing."));
            }

            if (string.IsNullOrWhiteSpace(project.FileSearchPattern))
            {
                issues.Add(Warning($"Project '{project.Name}' FileSearchPattern is empty. JLogDashboard will fall back to '*.log'."));
            }

            var parserMode = string.IsNullOrWhiteSpace(project.Parser.Mode)
                ? "auto"
                : project.Parser.Mode.Trim();
            if (!SupportedParserModes.Contains(parserMode, StringComparer.OrdinalIgnoreCase))
            {
                issues.Add(Warning(
                    $"Project '{project.Name}' Parser Mode '{project.Parser.Mode}' is not supported. JLogDashboard will fall back to built-in parsing."));
            }
            else if (string.Equals(parserMode, "delimited", StringComparison.OrdinalIgnoreCase)
                     && string.IsNullOrEmpty(project.Parser.Delimiter))
            {
                issues.Add(Warning($"Project '{project.Name}' Parser Delimiter is empty. JLogDashboard will use '|'."));
            }
            else if (string.Equals(parserMode, "regex", StringComparison.OrdinalIgnoreCase)
                     && string.IsNullOrWhiteSpace(project.Parser.Pattern))
            {
                issues.Add(Warning($"Project '{project.Name}' Parser Pattern is empty. JLogDashboard will fall back to built-in parsing."));
            }
            else if (string.Equals(parserMode, "nlog-layout", StringComparison.OrdinalIgnoreCase)
                     && string.IsNullOrWhiteSpace(project.Parser.Layout))
            {
                issues.Add(Warning($"Project '{project.Name}' Parser Layout is empty. JLogDashboard will fall back to built-in parsing."));
            }
            else if (string.Equals(parserMode, "log4net-pattern", StringComparison.OrdinalIgnoreCase)
                     && string.IsNullOrWhiteSpace(project.Parser.Layout))
            {
                issues.Add(Warning($"Project '{project.Name}' Parser Layout is empty. JLogDashboard will fall back to built-in parsing."));
            }
            else if (string.Equals(parserMode, "serilog-template", StringComparison.OrdinalIgnoreCase)
                     && string.IsNullOrWhiteSpace(project.Parser.Layout))
            {
                issues.Add(Warning($"Project '{project.Name}' Parser Layout is empty. JLogDashboard will fall back to built-in parsing."));
            }
        }

        foreach (var duplicate in Projects
                     .Where(project => !string.IsNullOrWhiteSpace(project.Name))
                     .GroupBy(project => project.Name, StringComparer.OrdinalIgnoreCase)
                     .Where(group => group.Count() > 1)
                     .Select(group => group.Key))
        {
            issues.Add(Error($"Project name '{duplicate}' is duplicate."));
        }

        return new JLogDashboardConfigurationAnalysis(issues);
    }

    /// <summary>Returns human-readable configuration validation errors.</summary>
    public IEnumerable<string> Validate() => Analyze().Issues.Select(issue => issue.Message);

    private static JLogDashboardConfigurationIssue Error(string message)
        => new(JLogDashboardConfigurationIssueSeverity.Error, message);

    private static JLogDashboardConfigurationIssue Warning(string message)
        => new(JLogDashboardConfigurationIssueSeverity.Warning, message);
}
