namespace JLogDashboard.Configuration;

/// <summary>
/// Configures one project's log directory and file matching behavior.
/// </summary>
public sealed class LogProjectOptions
{
    /// <summary>The display name and query key of the project.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>The directory that contains log files.</summary>
    public string DirectoryPath { get; set; } = string.Empty;

    /// <summary>The expected log provider layout: <c>serilog</c>, <c>nlog</c>, <c>log4net</c>, or <c>auto</c>.</summary>
    public string Provider { get; set; } = "auto";

    /// <summary>The glob pattern used to find log files.</summary>
    public string FileSearchPattern { get; set; } = "*.log";

    /// <summary>Whether subdirectories should be scanned.</summary>
    public bool Recursive { get; set; }
}
