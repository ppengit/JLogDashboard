namespace JLogDashboard.AspNetCore;

/// <summary>
/// Describes a configured log project as returned by the Dashboard project API.
/// </summary>
public sealed class ProjectSummary
{
    /// <summary>The display name and query key of the project.</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>The expected log provider layout, such as serilog, nlog, log4net, or auto.</summary>
    public string Provider { get; init; } = string.Empty;

    /// <summary>The directory scanned for log files.</summary>
    public string DirectoryPath { get; init; } = string.Empty;

    /// <summary>Whether the configured directory currently exists.</summary>
    public bool Exists { get; init; }

    /// <summary>The file glob used when scanning the directory.</summary>
    public string FileSearchPattern { get; init; } = "*.log";

    /// <summary>Whether subdirectories are scanned.</summary>
    public bool Recursive { get; init; }
}
