using JLogDashboard.Configuration;
using JLogDashboard.Parsing;
using JLogDashboard.Querying;

namespace JLogDashboard.Tests;

public sealed class LogQueryServiceTests
{
    [Fact]
    public async Task SearchAsync_FiltersAcrossMultipleProjectsAndExcludesNoise()
    {
        using var workspace = new TemporaryLogWorkspace();
        var orders = workspace.CreateProject("orders", "orders.log",
            "[2026-05-19 02:15:00 INF] HealthCheck passed",
            "[2026-05-19 02:16:00 ERR] Checkout failed for order 1002");
        var billing = workspace.CreateProject("billing", "billing.log",
            "2026-05-19 02:17:03.2451|WARN|Billing.Worker|Retry invoice INV-9",
            "2026-05-19 02:18:03.2451|ERROR|Billing.Worker|Invoice INV-9 failed");

        var options = new JLogDashboardOptions
        {
            Projects =
            {
                new LogProjectOptions { Name = "orders", DirectoryPath = orders, Provider = "serilog" },
                new LogProjectOptions { Name = "billing", DirectoryPath = billing, Provider = "nlog" }
            }
        };
        var service = new FileLogQueryService(options, LogParser.CreateDefault());

        var result = await service.SearchAsync(new LogQuery
        {
            Levels = { LogLevel.Error },
            ExcludeText = "HealthCheck",
            Page = 1,
            PageSize = 10
        });

        Assert.Equal(2, result.Total);
        Assert.Equal(new[] { "billing", "orders" }, result.Items.Select(x => x.Project).ToArray());
        Assert.All(result.Items, item => Assert.Equal(LogLevel.Error, item.Level));
        Assert.DoesNotContain(result.Items, item => item.Message.Contains("HealthCheck", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task SearchAsync_RespectsTailLimitForLargeFiles()
    {
        using var workspace = new TemporaryLogWorkspace();
        var projectPath = workspace.CreateProject("large", "large.log",
            Enumerable.Range(0, 500).Select(i => $"[2026-05-19 01:00:{i % 60:00} INF] old line {i}")
                .Concat(new[] { "[2026-05-19 03:00:00 ERR] recent failure" }).ToArray());

        var options = new JLogDashboardOptions
        {
            MaxFileBytes = 512,
            Projects =
            {
                new LogProjectOptions { Name = "large", DirectoryPath = projectPath, Provider = "serilog" }
            }
        };
        var service = new FileLogQueryService(options, LogParser.CreateDefault());

        var result = await service.SearchAsync(new LogQuery { SearchText = "recent", PageSize = 20 });

        var entry = Assert.Single(result.Items);
        Assert.Equal(LogLevel.Error, entry.Level);
        Assert.Equal("recent failure", entry.Message);
    }

    [Fact]
    public async Task SearchAsync_UsesProjectParserConfiguration()
    {
        using var workspace = new TemporaryLogWorkspace();
        var projectPath = workspace.CreateProject("custom", "custom.log",
            "2026/05/29 08:30:01||Orders.Api||ERROR||failed order 1002");

        var options = new JLogDashboardOptions
        {
            Projects =
            {
                new LogProjectOptions
                {
                    Name = "custom",
                    DirectoryPath = projectPath,
                    Provider = "custom",
                    Parser =
                    {
                        Mode = "delimited",
                        Delimiter = "||",
                        TimestampIndex = 0,
                        LoggerIndex = 1,
                        LevelIndex = 2,
                        MessageIndex = 3,
                        TimestampFormat = "yyyy/MM/dd HH:mm:ss"
                    }
                }
            }
        };
        var service = new FileLogQueryService(options, LogParser.CreateDefault());

        var result = await service.SearchAsync(new LogQuery { Levels = { LogLevel.Error }, PageSize = 10 });

        var entry = Assert.Single(result.Items);
        Assert.Equal("custom", entry.Project);
        Assert.Equal("Orders.Api", entry.Logger);
        Assert.Equal("failed order 1002", entry.Message);
    }

    [Fact]
    public async Task SearchAsync_SkipsUnreadableFilesAndReturnsRemainingResults()
    {
        using var workspace = new TemporaryLogWorkspace();
        var projectPath = workspace.CreateProject("orders", "orders.log",
            "[2026-05-19 02:16:00 ERR] good file entry");
        var brokenPath = Path.Combine(projectPath, "broken.log");
        File.WriteAllText(brokenPath, "[2026-05-19 02:17:00 ERR] broken file entry");

        var options = new JLogDashboardOptions
        {
            Projects =
            {
                new LogProjectOptions { Name = "orders", DirectoryPath = projectPath, Provider = "serilog" }
            }
        };
        var service = new TestableFileLogQueryService(options, LogParser.CreateDefault(), brokenPath);

        var result = await service.SearchAsync(new LogQuery { Levels = { LogLevel.Error }, PageSize = 10 });

        var entry = Assert.Single(result.Items);
        Assert.Equal("good file entry", entry.Message);
    }

    [Fact]
    public async Task SearchAsync_SkipsProjectsWhoseEnumerationFailsAndReturnsRemainingResults()
    {
        using var workspace = new TemporaryLogWorkspace();
        var goodProjectPath = workspace.CreateProject("good", "good.log",
            "[2026-05-19 02:16:00 ERR] surviving entry");
        var brokenProjectPath = workspace.CreateProject("broken", "broken.log",
            "[2026-05-19 02:17:00 ERR] broken entry");

        var options = new JLogDashboardOptions
        {
            Projects =
            {
                new LogProjectOptions { Name = "broken", DirectoryPath = brokenProjectPath, Provider = "serilog" },
                new LogProjectOptions { Name = "good", DirectoryPath = goodProjectPath, Provider = "serilog" }
            }
        };
        var service = new TestableFileLogQueryService(options, LogParser.CreateDefault(), brokenProjectPath, brokenEnumeration: true);

        var result = await service.SearchAsync(new LogQuery { Levels = { LogLevel.Error }, PageSize = 10 });

        var entry = Assert.Single(result.Items);
        Assert.Equal("good", entry.Project);
        Assert.Equal("surviving entry", entry.Message);
    }

    [Fact]
    public async Task SearchAsync_DoesNotSwallowCancellation()
    {
        using var workspace = new TemporaryLogWorkspace();
        var projectPath = workspace.CreateProject("orders", "orders.log",
            "[2026-05-19 02:16:00 ERR] good file entry");

        var options = new JLogDashboardOptions
        {
            Projects =
            {
                new LogProjectOptions { Name = "orders", DirectoryPath = projectPath, Provider = "serilog" }
            }
        };
        var service = new TestableFileLogQueryService(options, LogParser.CreateDefault(), projectPath, cancelRead: true);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            service.SearchAsync(new LogQuery { Levels = { LogLevel.Error }, PageSize = 10 }, cts.Token));
    }

    private sealed class TestableFileLogQueryService : FileLogQueryService
    {
        private readonly string _brokenPath;
        private readonly bool _brokenEnumeration;
        private readonly bool _cancelRead;

        public TestableFileLogQueryService(
            JLogDashboardOptions options,
            LogParser parser,
            string brokenPath,
            bool brokenEnumeration = false,
            bool cancelRead = false)
            : base(options, parser)
        {
            _brokenPath = brokenPath;
            _brokenEnumeration = brokenEnumeration;
            _cancelRead = cancelRead;
        }

        protected override IEnumerable<string> EnumerateLogFilesCore(LogProjectOptions project)
        {
            if (_brokenEnumeration && string.Equals(project.DirectoryPath, _brokenPath, StringComparison.OrdinalIgnoreCase))
            {
                throw new IOException("simulated project enumeration failure");
            }

            return base.EnumerateLogFilesCore(project);
        }

        protected override Task<IReadOnlyList<string>> ReadTailLinesCoreAsync(
            string filePath,
            long maxFileBytes,
            CancellationToken cancellationToken)
        {
            if (_cancelRead)
            {
                cancellationToken.ThrowIfCancellationRequested();
            }

            if (string.Equals(filePath, _brokenPath, StringComparison.OrdinalIgnoreCase))
            {
                throw new IOException("simulated unreadable log file");
            }

            return base.ReadTailLinesCoreAsync(filePath, maxFileBytes, cancellationToken);
        }
    }

    private sealed class TemporaryLogWorkspace : IDisposable
    {
        private readonly string _root = Path.Combine(Path.GetTempPath(), "jlog-" + Guid.NewGuid().ToString("N"));

        public string CreateProject(string name, string fileName, params string[] lines)
        {
            var directory = Path.Combine(_root, name);
            Directory.CreateDirectory(directory);
            File.WriteAllLines(Path.Combine(directory, fileName), lines);
            return directory;
        }

        public void Dispose()
        {
            if (Directory.Exists(_root))
            {
                Directory.Delete(_root, recursive: true);
            }
        }
    }
}
