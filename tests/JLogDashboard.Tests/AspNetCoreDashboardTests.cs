using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using JLogDashboard.AspNetCore;
using JLogDashboard.Configuration;
using JLogDashboard.Localization;
using JLogDashboard.Querying;
using JLogDashboard.ReverseProxy;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Routing;

namespace JLogDashboard.Tests;

public sealed class AspNetCoreDashboardTests
{
    private static readonly JsonSerializerOptions DashboardJsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    [Fact]
    public async Task MapJLogDashboard_ServesDashboardHtmlWithConfigurationInputs()
    {
        using var workspace = new TemporaryLogWorkspace();
        var client = CreateClient(workspace);

        var html = await client.GetStringAsync("/ops-logs");

        Assert.Contains("JLogDashboard", html);
        Assert.Contains("server-name-input", html);
        Assert.Contains("upstream-url-input", html);
        Assert.Contains("base-path-input", html);
        Assert.Contains("copy-nginx-button", html);
    }

    [Fact]
    public async Task MapJLogDashboard_WhenMappedAtRoot_UsesRootRelativeApiPaths()
    {
        using var workspace = new TemporaryLogWorkspace();
        var client = CreateClient(workspace, options => options.RoutePrefix = "/");

        var html = await client.GetStringAsync("/");

        Assert.Contains("const routePrefix = \"\";", html);
        Assert.DoesNotContain("//api/search", html);
    }

    [Fact]
    public async Task SearchEndpoint_ReturnsFilteredLogEntries()
    {
        using var workspace = new TemporaryLogWorkspace();
        workspace.CreateProject("orders", "orders.log",
            "[2026-05-19 02:16:00 ERR] Checkout failed for order 1002");
        var client = CreateClient(workspace);

        var response = await client.PostAsJsonAsync("/ops-logs/api/search", new LogQuery
        {
            Levels = { LogLevel.Error },
            SearchText = "Checkout",
            PageSize = 10
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<LogQueryResult>(DashboardJsonOptions);
        Assert.NotNull(result);
        var item = Assert.Single(result.Items);
        Assert.Equal("orders", item.Project);
        Assert.Equal(LogLevel.Error, item.Level);
        Assert.Contains("Checkout failed", item.Message);
    }

    [Fact]
    public async Task SearchEndpoint_AcceptsAndReturnsStringLogLevelsForDashboardJavaScript()
    {
        using var workspace = new TemporaryLogWorkspace();
        workspace.CreateProject("orders", "orders.log",
            "[2026-05-19 02:16:00 ERR] Checkout failed for order 1002");
        var client = CreateClient(workspace);

        var response = await client.PostAsJsonAsync("/ops-logs/api/search", new
        {
            levels = new[] { "Error" },
            searchText = "Checkout",
            pageSize = 10
        });

        var json = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("\"level\":\"Error\"", json);
    }

    [Fact]
    public async Task NginxEndpoint_UsesRequestInputs()
    {
        using var workspace = new TemporaryLogWorkspace();
        var client = CreateClient(workspace);

        var config = await client.GetStringAsync(
            "/ops-logs/api/nginx?serverName=logs.example.com&upstreamUrl=http://127.0.0.1:5099&basePath=/ops-logs");

        Assert.Contains("server_name logs.example.com;", config);
        Assert.Contains("proxy_pass http://127.0.0.1:5099;", config);
        Assert.Contains("location /ops-logs/", config);
    }

    [Fact]
    public async Task ProjectsEndpoint_ExposesConfiguredProjects()
    {
        using var workspace = new TemporaryLogWorkspace();
        workspace.CreateProject("orders", "orders.log", "[2026-05-19 02:16:00 INF] ready");
        var client = CreateClient(workspace);

        var projects = await client.GetFromJsonAsync<ProjectSummary[]>("/ops-logs/api/projects");

        var project = Assert.Single(projects ?? Array.Empty<ProjectSummary>());
        Assert.Equal("orders", project.Name);
        Assert.Equal("serilog", project.Provider);
        Assert.True(project.Exists);
    }

    [Fact]
    public async Task BasicAuth_WhenEnabled_RejectsAnonymousDashboardRequests()
    {
        using var workspace = new TemporaryLogWorkspace();
        var client = CreateClient(workspace, options =>
        {
            options.BasicAuth.Enabled = true;
            options.BasicAuth.Username = "ops";
            options.BasicAuth.Password = "secret";
        });

        var response = await client.GetAsync("/ops-logs");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("Basic", response.Headers.WwwAuthenticate.Single().Scheme);
        Assert.Contains("JLogDashboard", response.Headers.WwwAuthenticate.Single().Parameter);
    }

    [Fact]
    public async Task BasicAuth_WhenEnabled_AllowsValidCredentials()
    {
        using var workspace = new TemporaryLogWorkspace();
        var client = CreateClient(workspace, options =>
        {
            options.BasicAuth.Enabled = true;
            options.BasicAuth.Username = "ops";
            options.BasicAuth.Password = "secret";
        });
        client.DefaultRequestHeaders.Authorization = CreateBasicAuthHeader("ops", "secret");

        var response = await client.GetAsync("/ops-logs");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("JLogDashboard", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task BasicAuth_WhenPasswordHashIsConfigured_AllowsValidCredentialsWithoutPlainTextPassword()
    {
        using var workspace = new TemporaryLogWorkspace();
        var client = CreateClient(workspace, options =>
        {
            options.BasicAuth.Enabled = true;
            options.BasicAuth.Username = "ops";
            options.BasicAuth.PasswordSha256 = ToSha256Hex("secret");
        });
        client.DefaultRequestHeaders.Authorization = CreateBasicAuthHeader("ops", "secret");

        var response = await client.GetAsync("/ops-logs/api/projects");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task BasicAuth_DoesNotLockClientAfterAnonymousChallenges()
    {
        using var workspace = new TemporaryLogWorkspace();
        var client = CreateClient(workspace, options =>
        {
            options.BasicAuth.Enabled = true;
            options.BasicAuth.Username = "ops";
            options.BasicAuth.Password = "secret";
            options.BasicAuth.MaxFailedAttempts = 2;
            options.BasicAuth.LockoutSeconds = 60;
        });

        var anonymousRequest1 = new HttpRequestMessage(HttpMethod.Get, "/ops-logs");
        anonymousRequest1.Headers.Add("X-Forwarded-For", "203.0.113.20");
        var anonymousRequest2 = new HttpRequestMessage(HttpMethod.Get, "/ops-logs");
        anonymousRequest2.Headers.Add("X-Forwarded-For", "203.0.113.20");
        var validRequest = new HttpRequestMessage(HttpMethod.Get, "/ops-logs");
        validRequest.Headers.Add("X-Forwarded-For", "203.0.113.20");
        validRequest.Headers.Authorization = CreateBasicAuthHeader("ops", "secret");

        var first = await client.SendAsync(anonymousRequest1);
        var second = await client.SendAsync(anonymousRequest2);
        var valid = await client.SendAsync(validRequest);

        Assert.Equal(HttpStatusCode.Unauthorized, first.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, second.StatusCode);
        Assert.Equal(HttpStatusCode.OK, valid.StatusCode);
    }

    [Fact]
    public async Task BasicAuth_LocksClientAfterRepeatedFailures()
    {
        using var workspace = new TemporaryLogWorkspace();
        var client = CreateClient(workspace, options =>
        {
            options.BasicAuth.Enabled = true;
            options.BasicAuth.Username = "ops";
            options.BasicAuth.Password = "secret";
            options.BasicAuth.MaxFailedAttempts = 2;
            options.BasicAuth.LockoutSeconds = 60;
        });
        client.DefaultRequestHeaders.Authorization = CreateBasicAuthHeader("ops", "wrong");

        var first = await client.GetAsync("/ops-logs");
        var second = await client.GetAsync("/ops-logs");

        Assert.Equal(HttpStatusCode.Unauthorized, first.StatusCode);
        Assert.Equal((HttpStatusCode)429, second.StatusCode);
        Assert.True(second.Headers.RetryAfter?.Delta?.TotalSeconds > 0);
    }

    [Fact]
    public async Task DashboardFailure_DoesNotBreakNonDashboardEndpoints()
    {
        using var workspace = new TemporaryLogWorkspace();
        var client = CreateClient(
            workspace,
            configureServices: services => services.AddSingleton<ILogQueryService, ThrowingLogQueryService>(),
            configureEndpoints: endpoints => endpoints.MapGet("/health", () => Results.Ok(new { status = "ok" })));

        var dashboardResponse = await client.PostAsJsonAsync("/ops-logs/api/search", new LogQuery { PageSize = 10 });
        var healthResponse = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.ServiceUnavailable, dashboardResponse.StatusCode);
        Assert.Contains("DashboardUnavailable", await dashboardResponse.Content.ReadAsStringAsync());
        Assert.Equal(HttpStatusCode.OK, healthResponse.StatusCode);
        Assert.Contains("ok", await healthResponse.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task DashboardHtmlFailure_ReturnsControlledHtmlResponse()
    {
        using var workspace = new TemporaryLogWorkspace();
        var client = CreateClient(
            workspace,
            configureServices: services => services.AddSingleton<DashboardLocalizer>(_ => throw new InvalidOperationException("simulated dashboard page failure")));

        var response = await client.GetAsync("/ops-logs");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Equal("text/html; charset=utf-8", response.Content.Headers.ContentType?.ToString());
        Assert.Contains("temporarily unavailable", html, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DashboardTextFailure_ReturnsControlledPlainTextResponse()
    {
        using var workspace = new TemporaryLogWorkspace();
        var client = CreateClient(
            workspace,
            configureServices: services => services.AddSingleton<NginxConfigGenerator>(_ => throw new InvalidOperationException("simulated nginx generation failure")));

        var response = await client.GetAsync("/ops-logs/api/nginx");
        var text = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Equal("text/plain; charset=utf-8", response.Content.Headers.ContentType?.ToString());
        Assert.Contains("JLogDashboard request failed.", text);
    }

    [Fact]
    public async Task DashboardMisconfiguration_ReturnsControlled503WithoutBreakingNonDashboardEndpoints()
    {
        using var workspace = new TemporaryLogWorkspace();
        var client = CreateClient(
            workspace,
            options =>
            {
                options.BasicAuth.Enabled = true;
                options.BasicAuth.Username = "ops";
                options.BasicAuth.PasswordSha256 = "invalid-hash";
            },
            configureEndpoints: endpoints => endpoints.MapGet("/health", () => Results.Ok(new { status = "ok" })));

        var dashboardResponse = await client.GetAsync("/ops-logs");
        var healthResponse = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.ServiceUnavailable, dashboardResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, healthResponse.StatusCode);
        Assert.Contains("ok", await healthResponse.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task StartupDiagnostics_LogsWarningsAndErrorsOnce()
    {
        using var workspace = new TemporaryLogWorkspace();
        var sink = new List<string>();
        using var client = CreateClient(
            workspace,
            options =>
            {
                options.BasicAuth.Enabled = false;
                options.Projects.Clear();
            },
            configureLogging: logging => logging.Services.AddSingleton<Microsoft.Extensions.Logging.ILoggerProvider>(
                new TestLoggerProvider(sink)));

        // Trigger host startup and dashboard route resolution.
        _ = await client.GetAsync("/ops-logs");

        Assert.Contains(
            sink,
            entry => entry.Contains("configuration error", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(
            sink,
            entry => entry.Contains("BasicAuth", StringComparison.OrdinalIgnoreCase));
    }

    private static HttpClient CreateClient(
        TemporaryLogWorkspace workspace,
        Action<JLogDashboardOptions>? configure = null,
        Action<IServiceCollection>? configureServices = null,
        Action<IEndpointRouteBuilder>? configureEndpoints = null,
        Action<Microsoft.Extensions.Logging.ILoggingBuilder>? configureLogging = null)
    {
        var builder = new WebHostBuilder()
            .ConfigureServices(services =>
            {
                services.AddRouting();
                services.AddJLogDashboard(options =>
                {
                    options.RoutePrefix = "/ops-logs";
                    options.Projects.Add(new LogProjectOptions
                    {
                        Name = "orders",
                        DirectoryPath = workspace.GetProjectPath("orders"),
                        Provider = "serilog"
                    });
                    configure?.Invoke(options);
                });
                configureServices?.Invoke(services);
            })
            .ConfigureLogging(logging => configureLogging?.Invoke(logging))
            .Configure(app =>
            {
                app.UseRouting();
                app.UseEndpoints(endpoints =>
                {
                    configureEndpoints?.Invoke(endpoints);
                    endpoints.MapJLogDashboard();
                });
            });

        return new TestServer(builder).CreateClient();
    }

    private static AuthenticationHeaderValue CreateBasicAuthHeader(string username, string password)
    {
        var token = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{username}:{password}"));
        return new AuthenticationHeaderValue("Basic", token);
    }

    private static string ToSha256Hex(string value)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();

    private sealed class ThrowingLogQueryService : ILogQueryService
    {
        public Task<LogQueryResult> SearchAsync(LogQuery query, CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("simulated dashboard failure");
    }

    private sealed class TestLoggerProvider : Microsoft.Extensions.Logging.ILoggerProvider
    {
        private readonly List<string> _sink;

        public TestLoggerProvider(List<string> sink)
        {
            _sink = sink;
        }

        public Microsoft.Extensions.Logging.ILogger CreateLogger(string categoryName) => new TestLogger(_sink, categoryName);

        public void Dispose()
        {
        }
    }

    private sealed class TestLogger : Microsoft.Extensions.Logging.ILogger
    {
        private readonly List<string> _sink;
        private readonly string _categoryName;

        public TestLogger(List<string> sink, string categoryName)
        {
            _sink = sink;
            _categoryName = categoryName;
        }

        public IDisposable BeginScope<TState>(TState state) where TState : notnull => NullScope.Instance;

        public bool IsEnabled(Microsoft.Extensions.Logging.LogLevel logLevel) => true;

        public void Log<TState>(
            Microsoft.Extensions.Logging.LogLevel logLevel,
            Microsoft.Extensions.Logging.EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            _sink.Add($"{logLevel}|{_categoryName}|{formatter(state, exception)}");
        }
    }

    private sealed class NullScope : IDisposable
    {
        public static NullScope Instance { get; } = new();

        public void Dispose()
        {
        }
    }

    private sealed class TemporaryLogWorkspace : IDisposable
    {
        private readonly string _root = Path.Combine(Path.GetTempPath(), "jlog-aspnet-" + Guid.NewGuid().ToString("N"));

        public TemporaryLogWorkspace()
        {
            CreateProject("orders", "orders.log", "[2026-05-19 02:15:00 INF] workspace ready");
        }

        public string CreateProject(string name, string fileName, params string[] lines)
        {
            var directory = GetProjectPath(name);
            Directory.CreateDirectory(directory);
            File.WriteAllLines(Path.Combine(directory, fileName), lines);
            return directory;
        }

        public string GetProjectPath(string name) => Path.Combine(_root, name);

        public void Dispose()
        {
            if (Directory.Exists(_root))
            {
                Directory.Delete(_root, recursive: true);
            }
        }
    }
}
