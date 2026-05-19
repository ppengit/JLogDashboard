using System.Text;
using JLogDashboard.Configuration;
using JLogDashboard.Parsing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace JLogDashboard.Querying;

/// <summary>
/// Searches configured log files directly from disk.
/// </summary>
public class FileLogQueryService : ILogQueryService
{
    private readonly JLogDashboardOptions _options;
    private readonly LogParser _parser;
    private readonly ILogger<FileLogQueryService> _logger;

    /// <summary>Creates a file-backed query service.</summary>
    public FileLogQueryService(
        JLogDashboardOptions options,
        LogParser parser,
        ILogger<FileLogQueryService>? logger = null)
    {
        _options = options;
        _parser = parser;
        _logger = logger ?? NullLogger<FileLogQueryService>.Instance;
    }

    /// <inheritdoc />
    public async Task<LogQueryResult> SearchAsync(LogQuery query, CancellationToken cancellationToken = default)
    {
        var page = Math.Max(1, query.Page);
        var pageSize = NormalizePageSize(query.PageSize);
        var entries = new List<LogEntry>();

        foreach (var project in SelectProjects(query))
        {
            if (!Directory.Exists(project.DirectoryPath))
            {
                continue;
            }

            string[] files;
            try
            {
                files = EnumerateLogFilesCore(project).ToArray();
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "JLogDashboard skipped project {ProjectName} because log file enumeration failed for {DirectoryPath}.",
                    project.Name,
                    project.DirectoryPath);
                continue;
            }

            foreach (var file in files)
            {
                cancellationToken.ThrowIfCancellationRequested();
                try
                {
                    // One unreadable log file should not make the whole dashboard query unavailable.
                    var lines = await ReadTailLinesCoreAsync(file, _options.MaxFileBytes, cancellationToken).ConfigureAwait(false);
                    entries.AddRange(_parser.Parse(new LogParseContext(project.Name, project.Provider, file), lines));
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(
                        ex,
                        "JLogDashboard skipped file {FilePath} in project {ProjectName} because it could not be read.",
                        file,
                        project.Name);
                }
            }
        }

        var filtered = entries
            .Where(entry => Matches(query, entry))
            .OrderByDescending(entry => entry.Timestamp)
            .ThenBy(entry => entry.Project, StringComparer.OrdinalIgnoreCase)
            .ThenBy(entry => entry.SourcePath, StringComparer.OrdinalIgnoreCase)
            .ThenByDescending(entry => entry.LineNumber)
            .ToArray();

        return new LogQueryResult
        {
            Total = filtered.Length,
            Page = page,
            PageSize = pageSize,
            Items = filtered.Skip((page - 1) * pageSize).Take(pageSize).ToArray()
        };
    }

    private int NormalizePageSize(int requested)
    {
        if (requested <= 0)
        {
            return _options.DefaultPageSize;
        }

        return Math.Min(requested, _options.MaxPageSize);
    }

    private IEnumerable<LogProjectOptions> SelectProjects(LogQuery query)
    {
        if (string.IsNullOrWhiteSpace(query.Project))
        {
            return _options.Projects;
        }

        return _options.Projects.Where(project => string.Equals(project.Name, query.Project, StringComparison.OrdinalIgnoreCase));
    }

    protected virtual IEnumerable<string> EnumerateLogFilesCore(LogProjectOptions project) => EnumerateLogFiles(project);

    private static IEnumerable<string> EnumerateLogFiles(LogProjectOptions project)
    {
        var pattern = string.IsNullOrWhiteSpace(project.FileSearchPattern) ? "*.log" : project.FileSearchPattern;
        var option = project.Recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
        return Directory.EnumerateFiles(project.DirectoryPath, pattern, option);
    }

    protected virtual Task<IReadOnlyList<string>> ReadTailLinesCoreAsync(
        string filePath,
        long maxFileBytes,
        CancellationToken cancellationToken)
        => ReadTailLinesAsync(filePath, maxFileBytes, cancellationToken);

    private static async Task<IReadOnlyList<string>> ReadTailLinesAsync(
        string filePath,
        long maxFileBytes,
        CancellationToken cancellationToken)
    {
        await using var stream = new FileStream(
            filePath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.ReadWrite | FileShare.Delete,
            bufferSize: 64 * 1024,
            useAsync: true);

        var start = 0L;
        var discardFirstLine = false;
        if (stream.Length > maxFileBytes)
        {
            // Large log files are read from the tail so a single old file cannot exhaust memory.
            start = Math.Max(0, stream.Length - maxFileBytes);
            discardFirstLine = start > 0;
            stream.Seek(start, SeekOrigin.Begin);
        }

        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
        var lines = new List<string>();
        var isFirst = true;
        while (!reader.EndOfStream)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var line = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false);
            if (line is null)
            {
                break;
            }

            if (discardFirstLine && isFirst)
            {
                isFirst = false;
                continue;
            }

            isFirst = false;
            lines.Add(line);
        }

        return lines;
    }

    private static bool Matches(LogQuery query, LogEntry entry)
    {
        if (query.Levels.Count > 0 && !query.Levels.Contains(entry.Level))
        {
            return false;
        }

        if (query.From is not null && entry.Timestamp < query.From.Value)
        {
            return false;
        }

        if (query.To is not null && entry.Timestamp > query.To.Value)
        {
            return false;
        }

        if (!ContainsText(entry, query.SearchText))
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(query.ExcludeText) && ContainsText(entry, query.ExcludeText))
        {
            return false;
        }

        return true;
    }

    private static bool ContainsText(LogEntry entry, string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return true;
        }

        return entry.Message.Contains(text, StringComparison.OrdinalIgnoreCase)
               || entry.Exception.Contains(text, StringComparison.OrdinalIgnoreCase)
               || entry.Logger.Contains(text, StringComparison.OrdinalIgnoreCase)
               || entry.Project.Contains(text, StringComparison.OrdinalIgnoreCase)
               || entry.SourcePath.Contains(text, StringComparison.OrdinalIgnoreCase);
    }
}
