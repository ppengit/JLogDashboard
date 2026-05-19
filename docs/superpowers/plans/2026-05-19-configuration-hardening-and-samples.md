# JLogDashboard 配置加固与示例完善实现计划

> **面向 AI 代理的工作者：** 必需子技能：使用 superpowers:subagent-driven-development（推荐）或 superpowers:executing-plans 逐任务实现此计划。步骤使用复选框（`- [ ]`）语法来跟踪进度。

**目标：** 为 JLogDashboard 增加分级配置校验、启动期诊断告警与误配降级能力，并补齐最小接入示例和生产部署示例，同时保持 Dashboard 故障不影响宿主主业务。

**架构：** 在现有 `JLogDashboardOptions` 之上补一层统一的配置分析结果，区分致命错误与风险告警。通过启动期诊断服务统一输出日志，通过 Dashboard 端点过滤器在致命误配时返回受控 `503`，而不是把异常传播到宿主。示例与文档保持独立于组件核心实现，不引入额外运行时依赖。

**技术栈：** .NET 8、ASP.NET Core Minimal API、xUnit、TestServer

---

### 任务 1：补齐配置分析与误配降级测试

**文件：**
- 修改：`tests/JLogDashboard.Tests/OptionsAndUtilityTests.cs`
- 修改：`tests/JLogDashboard.Tests/AspNetCoreDashboardTests.cs`

- [ ] **步骤 1：为配置分级分析编写失败测试**

```csharp
[Fact]
public void Analyze_ReturnsErrorsAndWarningsWithSeverity()
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
            new LogProjectOptions
            {
                Name = "orders",
                DirectoryPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N")),
                Provider = "custom"
            }
        }
    };

    var result = options.Analyze();

    Assert.Contains(result.Warnings, issue => issue.Message.Contains("change-me", StringComparison.OrdinalIgnoreCase));
    Assert.Contains(result.Warnings, issue => issue.Message.Contains("custom", StringComparison.OrdinalIgnoreCase));
}
```

- [ ] **步骤 2：运行测试验证失败**

运行：`dotnet test tests\JLogDashboard.Tests\JLogDashboard.Tests.csproj --configuration Release --filter "FullyQualifiedName~OptionsAndUtilityTests.Analyze_ReturnsErrorsAndWarningsWithSeverity"`

预期：FAIL，报错 `JLogDashboardOptions` 不包含 `Analyze`。

- [ ] **步骤 3：为误配降级编写失败测试**

```csharp
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
}
```

- [ ] **步骤 4：运行测试验证失败**

运行：`dotnet test tests\JLogDashboard.Tests\JLogDashboard.Tests.csproj --configuration Release --filter "FullyQualifiedName~AspNetCoreDashboardTests.DashboardMisconfiguration_ReturnsControlled503WithoutBreakingNonDashboardEndpoints"`

预期：FAIL，当前行为会返回 `401` 或 `200`，不会返回受控 `503`。

- [ ] **步骤 5：为启动期诊断编写失败测试**

```csharp
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
        configureLogging: logging => logging.AddProvider(new TestLoggerProvider(sink)));

    Assert.Contains(sink, entry => entry.Contains("configuration error", StringComparison.OrdinalIgnoreCase));
    Assert.Contains(sink, entry => entry.Contains("BasicAuth", StringComparison.OrdinalIgnoreCase));
}
```

- [ ] **步骤 6：运行测试验证失败**

运行：`dotnet test tests\JLogDashboard.Tests\JLogDashboard.Tests.csproj --configuration Release --filter "FullyQualifiedName~AspNetCoreDashboardTests.StartupDiagnostics_LogsWarningsAndErrorsOnce"`

预期：FAIL，当前没有统一的启动期诊断服务，也没有日志捕获点。

### 任务 2：实现配置分析、启动期诊断与误配降级

**文件：**
- 修改：`src/JLogDashboard/Configuration/JLogDashboardOptions.cs`
- 修改：`src/JLogDashboard/Configuration/BasicAuthOptions.cs`
- 修改：`src/JLogDashboard/AspNetCore/JLogDashboardServiceCollectionExtensions.cs`
- 修改：`src/JLogDashboard/AspNetCore/JLogDashboardEndpointRouteBuilderExtensions.cs`
- 修改：`src/JLogDashboard/AspNetCore/JLogDashboardFaultIsolationEndpointFilter.cs`
- 创建：`src/JLogDashboard/Configuration/JLogDashboardConfigurationIssue.cs`
- 创建：`src/JLogDashboard/Configuration/JLogDashboardConfigurationIssueSeverity.cs`
- 创建：`src/JLogDashboard/Configuration/JLogDashboardConfigurationAnalysis.cs`
- 创建：`src/JLogDashboard/AspNetCore/JLogDashboardConfigurationState.cs`
- 创建：`src/JLogDashboard/AspNetCore/JLogDashboardConfigurationEndpointFilter.cs`
- 创建：`src/JLogDashboard/AspNetCore/JLogDashboardStartupDiagnosticsHostedService.cs`

- [ ] **步骤 1：编写最少配置分析模型**

```csharp
namespace JLogDashboard.Configuration;

public enum JLogDashboardConfigurationIssueSeverity
{
    Warning,
    Error
}

public sealed record JLogDashboardConfigurationIssue(
    JLogDashboardConfigurationIssueSeverity Severity,
    string Message);
```

- [ ] **步骤 2：在 `JLogDashboardOptions` 中实现分级分析**

```csharp
public JLogDashboardConfigurationAnalysis Analyze()
{
    var issues = new List<JLogDashboardConfigurationIssue>();
    // 追加 Error / Warning
    return new JLogDashboardConfigurationAnalysis(issues);
}
```

- [ ] **步骤 3：补充最少规则**

```csharp
if (BasicAuth.Enabled && !IsSha256Hex(BasicAuth.PasswordSha256))
{
    issues.Add(new(JLogDashboardConfigurationIssueSeverity.Error,
        "BasicAuth PasswordSha256 must be a 64-character hexadecimal SHA-256 string."));
}

if (!BasicAuth.Enabled)
{
    issues.Add(new(JLogDashboardConfigurationIssueSeverity.Warning,
        "BasicAuth is disabled. Do not expose the Dashboard directly to untrusted networks."));
}
```

- [ ] **步骤 4：添加启动期诊断服务**

```csharp
internal sealed class JLogDashboardStartupDiagnosticsHostedService : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        // 逐条记录 error / warning
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
```

- [ ] **步骤 5：添加 Dashboard 配置状态过滤器**

```csharp
internal sealed class JLogDashboardConfigurationEndpointFilter : IEndpointFilter
{
    public ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        if (_state.HasErrors)
        {
            return ValueTask.FromResult<object?>(
                DashboardFailureResponses.CreateMisconfiguredResponse(context.HttpContext));
        }

        return next(context);
    }
}
```

- [ ] **步骤 6：统一失败响应帮助逻辑**

```csharp
internal static class DashboardFailureResponses
{
    public static IResult CreateUnavailableResponse(HttpContext context) { ... }
    public static IResult CreateMisconfiguredResponse(HttpContext context) { ... }
}
```

- [ ] **步骤 7：运行针对性测试验证通过**

运行：`dotnet test tests\JLogDashboard.Tests\JLogDashboard.Tests.csproj --configuration Release --filter "FullyQualifiedName~OptionsAndUtilityTests|FullyQualifiedName~AspNetCoreDashboardTests"`

预期：新增与受影响测试全部 PASS。

### 任务 3：补齐最小接入示例与生产部署材料

**文件：**
- 创建：`samples/JLogDashboard.SampleWeb/JLogDashboard.SampleWeb.csproj`
- 创建：`samples/JLogDashboard.SampleWeb/Program.cs`
- 创建：`samples/JLogDashboard.SampleWeb/appsettings.json`
- 修改：`JLogDashboard.sln`
- 修改：`README.md`
- 修改：`README.zh-CN.md`
- 修改：`docs/deployment.md`
- 修改：`docs/zh-CN/deployment.md`
- 修改：`docs/configuration.md`
- 修改：`docs/zh-CN/configuration.md`

- [ ] **步骤 1：为示例项目存在性编写失败测试**

```csharp
[Fact]
public void Repository_ContainsRunnableSampleWebProject()
{
    var projectPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "samples", "JLogDashboard.SampleWeb", "JLogDashboard.SampleWeb.csproj"));
    Assert.True(File.Exists(projectPath));
}
```

- [ ] **步骤 2：运行测试验证失败**

运行：`dotnet test tests\JLogDashboard.Tests\JLogDashboard.Tests.csproj --configuration Release --filter "FullyQualifiedName~Repository_ContainsRunnableSampleWebProject"`

预期：FAIL，当前不存在 `samples/JLogDashboard.SampleWeb`。

- [ ] **步骤 3：创建最小可运行示例**

```csharp
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddJLogDashboard(builder.Configuration);
var app = builder.Build();
app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
app.MapJLogDashboard();
app.Run();
```

- [ ] **步骤 4：补充双语文档中的“最小示例 / 生产部署 / 配置风险”章节**

```markdown
## Production checklist

- Keep Basic Auth enabled.
- Prefer `PasswordSha256` over plain-text `Password`.
- Place the Dashboard behind nginx or another trusted reverse proxy.
- Treat startup configuration errors as a blocked Dashboard, not as a host crash.
```

- [ ] **步骤 5：运行样例与文档存在性测试验证通过**

运行：`dotnet test tests\JLogDashboard.Tests\JLogDashboard.Tests.csproj --configuration Release --filter "FullyQualifiedName~Repository_ContainsRunnableSampleWebProject"`

预期：PASS。

### 任务 4：全量验证与收尾

**文件：**
- 修改：`CHANGELOG.md`
- 修改：`src/JLogDashboard/JLogDashboard.csproj`

- [ ] **步骤 1：更新版本与发布说明**

```xml
<VersionPrefix>0.1.7</VersionPrefix>
<PackageReleaseNotes>Added configuration severity analysis, startup diagnostics, dashboard misconfiguration isolation, and runnable sample host.</PackageReleaseNotes>
```

- [ ] **步骤 2：运行完整构建**

运行：`dotnet build JLogDashboard.sln --configuration Release`

预期：`0 Error`。

- [ ] **步骤 3：运行完整测试**

运行：`dotnet test JLogDashboard.sln --configuration Release --no-build`

预期：所有测试 PASS。

- [ ] **步骤 4：运行打包验证**

运行：`dotnet pack src\JLogDashboard\JLogDashboard.csproj --configuration Release --no-build --output artifacts\packages`

预期：生成新的 `.nupkg`。

- [ ] **步骤 5：核对工作区与最终产物**

运行：`git status --short --branch`

预期：仅包含本次预期改动，且无意外产物。
