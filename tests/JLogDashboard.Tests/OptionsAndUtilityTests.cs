using JLogDashboard.Configuration;
using JLogDashboard.Localization;
using JLogDashboard.ReverseProxy;
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
    }

    [Fact]
    public void Generate_ProducesCopyReadyNginxReverseProxyConfig()
    {
        var generator = new NginxConfigGenerator();

        var config = generator.Generate(new NginxConfigRequest
        {
            ServerName = "logs.example.com",
            UpstreamUrl = "http://127.0.0.1:5088",
            BasePath = "/jlog"
        });

        Assert.Contains("server_name logs.example.com;", config);
        Assert.Contains("location /jlog/", config);
        Assert.Contains("proxy_pass http://127.0.0.1:5088;", config);
        Assert.Contains("X-Forwarded-Proto", config);
    }

    [Fact]
    public void Translate_FallsBackToEnglishWhenCultureIsUnknown()
    {
        var catalog = DashboardLocalizer.CreateDefault();

        Assert.Equal("日志项目", catalog.Translate("zh-CN", "Projects"));
        Assert.Equal("Log projects", catalog.Translate("fr-FR", "Projects"));
    }
}
