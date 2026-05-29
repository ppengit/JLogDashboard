using JLogDashboard.Configuration;
using JLogDashboard.Localization;
using JLogDashboard.AspNetCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace JLogDashboard.Tests;

public sealed class OptionsAndUtilityTests
{
    [Fact]
    public void Validate_ReturnsActionableErrorsForInvalidProjectConfiguration()
    {
        var options = new JLogDashboardOptions
        {
            Projects =
            {
                new LogProjectOptions { Name = "orders", DirectoryPath = "" },
                new LogProjectOptions { Name = "orders", DirectoryPath = "C:\\logs\\orders" }
            }
        };

        var errors = options.Validate().ToArray();

        Assert.Contains(errors, error => error.Contains("DirectoryPath", StringComparison.Ordinal));
        Assert.Contains(errors, error => error.Contains("duplicate", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Validate_ReturnsActionableErrorsForIncompleteBasicAuthConfiguration()
    {
        var options = new JLogDashboardOptions
        {
            BasicAuth =
            {
                Enabled = true,
                Username = "admin"
            },
            Projects =
            {
                new LogProjectOptions { Name = "orders", DirectoryPath = "C:\\logs\\orders" }
            }
        };

        var errors = options.Validate().ToArray();

        Assert.Contains(errors, error => error.Contains("Password", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Validate_WarnsWhenBasicAuthUsesDefaultPassword()
    {
        var options = new JLogDashboardOptions
        {
            BasicAuth =
            {
                Enabled = true,
                Username = "admin",
                Password = "change-me"
            },
            Projects =
            {
                new LogProjectOptions { Name = "orders", DirectoryPath = "C:\\logs\\orders" }
            }
        };

        var errors = options.Validate().ToArray();

        Assert.Contains(errors, error => error.Contains("change-me", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Analyze_ReturnsErrorsAndWarningsWithSeverity()
    {
        var missingDirectory = Path.Combine(Path.GetTempPath(), "jlog-missing-" + Guid.NewGuid().ToString("N"));
        var options = new JLogDashboardOptions
        {
            BasicAuth =
            {
                Enabled = true,
                Username = "admin",
                PasswordSha256 = "invalid-hash"
            },
            Projects =
            {
                new LogProjectOptions
                {
                    Name = "orders",
                    DirectoryPath = missingDirectory,
                    Provider = "custom"
                }
            }
        };

        var analysis = options.Analyze();

        Assert.Contains(
            analysis.Errors,
            issue => issue.Message.Contains("PasswordSha256", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(
            analysis.Warnings,
            issue => issue.Message.Contains("custom", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(
            analysis.Warnings,
            issue => issue.Message.Contains("does not exist", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Analyze_AcceptsExplicitLog4NetAndSerilogParserModes()
    {
        using var root = new TemporaryDirectory();
        var options = new JLogDashboardOptions
        {
            Projects =
            {
                new LogProjectOptions
                {
                    Name = "legacy",
                    DirectoryPath = root.Path,
                    Provider = "log4net",
                    Parser =
                    {
                        Mode = "log4net-pattern",
                        Layout = "%date %-5level %logger - %message%newline%exception"
                    }
                },
                new LogProjectOptions
                {
                    Name = "orders",
                    DirectoryPath = root.Path,
                    Provider = "serilog",
                    Parser =
                    {
                        Mode = "serilog-template",
                        Layout = "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {SourceContext} {Message:lj}{NewLine}{Exception}"
                    }
                }
            }
        };

        var analysis = options.Analyze();

        Assert.DoesNotContain(
            analysis.Warnings,
            issue => issue.Message.Contains("not supported", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(
            analysis.Warnings,
            issue => issue.Message.Contains("Parser Layout is empty", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Analyze_WarnsWhenExplicitTemplateParserLayoutIsEmpty()
    {
        using var root = new TemporaryDirectory();
        var options = new JLogDashboardOptions
        {
            Projects =
            {
                new LogProjectOptions
                {
                    Name = "orders",
                    DirectoryPath = root.Path,
                    Provider = "serilog",
                    Parser = { Mode = "serilog-template" }
                }
            }
        };

        var analysis = options.Analyze();

        Assert.Contains(
            analysis.Warnings,
            issue => issue.Message.Contains("Parser Layout is empty", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void AddJLogDashboard_BindsOptionsFromConfigurationSection()
    {
        var values = new Dictionary<string, string?>
        {
            ["JLogDashboard:RoutePrefix"] = "/ops",
            ["JLogDashboard:BasicAuth:Enabled"] = "true",
            ["JLogDashboard:BasicAuth:Username"] = "admin",
            ["JLogDashboard:BasicAuth:PasswordSha256"] = "abc",
            ["JLogDashboard:Projects:0:Name"] = "orders",
            ["JLogDashboard:Projects:0:DirectoryPath"] = "C:\\logs\\orders",
            ["JLogDashboard:Projects:0:Provider"] = "serilog",
            ["JLogDashboard:Projects:0:Parser:Mode"] = "delimited",
            ["JLogDashboard:Projects:0:Parser:Delimiter"] = "||",
            ["JLogDashboard:Projects:0:Parser:Layout"] = "${longdate}|${level}|${logger}|${message}",
            ["JLogDashboard:Projects:0:Parser:TimestampIndex"] = "0",
            ["JLogDashboard:Projects:0:Parser:LevelIndex"] = "2",
            ["JLogDashboard:Projects:0:Parser:MessageIndex"] = "3",
            ["JLogDashboard:Projects:1:Name"] = "billing",
            ["JLogDashboard:Projects:1:DirectoryPath"] = "C:\\logs\\billing",
            ["JLogDashboard:Projects:1:Provider"] = "nlog"
        };
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(values).Build();
        var services = new ServiceCollection();

        services.AddJLogDashboard(configuration);

        var options = services.BuildServiceProvider().GetRequiredService<JLogDashboardOptions>();
        Assert.Equal("/ops", options.RoutePrefix);
        Assert.True(options.BasicAuth.Enabled);
        Assert.Equal("admin", options.BasicAuth.Username);
        Assert.Equal(new[] { "orders", "billing" }, options.Projects.Select(project => project.Name).ToArray());
        Assert.Equal("delimited", options.Projects[0].Parser.Mode);
        Assert.Equal("||", options.Projects[0].Parser.Delimiter);
        Assert.Equal("${longdate}|${level}|${logger}|${message}", options.Projects[0].Parser.Layout);
        Assert.Equal(2, options.Projects[0].Parser.LevelIndex);
    }

    [Fact]
    public void Translate_FallsBackToEnglishWhenCultureIsUnknown()
    {
        var catalog = DashboardLocalizer.CreateDefault();

        Assert.Equal("日志项目", catalog.Translate("zh-CN", "Projects"));
        Assert.Equal("Log projects", catalog.Translate("fr-FR", "Projects"));
    }

    [Fact]
    public void Repository_ContainsRunnableSampleWebProject()
    {
        var repositoryRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
        var projectPath = Path.Combine(repositoryRoot, "samples", "JLogDashboard.SampleWeb", "JLogDashboard.SampleWeb.csproj");

        Assert.True(File.Exists(projectPath));
    }

    private sealed class TemporaryDirectory : IDisposable
    {
        public TemporaryDirectory()
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "jlog-options-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path);
        }

        public string Path { get; }

        public void Dispose()
        {
            if (Directory.Exists(Path))
            {
                Directory.Delete(Path, recursive: true);
            }
        }
    }
}
