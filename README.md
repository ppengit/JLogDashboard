# JLogDashboard

JLogDashboard 是一个轻量级 ASP.NET Core 日志看板，用于查看 NLog、log4net、Serilog 写出的文件日志。它面向「一台服务器上有多个 .NET 项目」的场景，提供多项目目录配置、日志级别筛选、关键字搜索、排除噪声、异常堆栈查看、Basic Auth 保护和一键生成 nginx 反向代理配置。

## 特性

- 支持多个项目日志目录，每个项目可独立配置日志目录、文件匹配规则和是否递归扫描。
- 兼容常见 NLog、log4net、Serilog 文本布局，未匹配的堆栈行会自动归并到上一条日志。
- 支持按项目、级别、关键字、排除关键字和时间范围查询。
- 读取大文件时只读取尾部窗口，避免超大日志文件拖垮内存。
- 内置 Dashboard，可通过 `AddJLogDashboard` / `MapJLogDashboard` 像 Hangfire Dashboard 一样接入业务系统。
- 提供独立 Host，可单独绑定端口和域名，不需要和业务系统部署在同一个进程。
- 内置 Basic Auth，并提供基于客户端 IP 的失败次数锁定，适合内网或反向代理后的轻量保护。
- Dashboard 内提供域名、端口、路径输入框，可一键生成并复制 nginx 配置。
- 内置中英文基础文案，后续可继续扩展 i18n 字典。

## 安装

```bash
dotnet add package JLogDashboard
```

## 在业务系统中接入

```csharp
using JLogDashboard.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddJLogDashboard(options =>
{
    options.RoutePrefix = "/jlog";
    options.MaxFileBytes = 10 * 1024 * 1024;
    options.BasicAuth.Enabled = true;
    options.BasicAuth.Username = "admin";
    options.BasicAuth.PasswordSha256 = "替换为 SHA-256 小写十六进制值";
    options.Projects.Add(new()
    {
        Name = "orders",
        DirectoryPath = "/var/log/orders",
        Provider = "serilog",
        FileSearchPattern = "*.log",
        Recursive = false
    });
});

var app = builder.Build();
app.MapJLogDashboard();
app.Run();
```

访问：`https://your-domain/jlog`。

## 独立 Host

仓库包含 `src/JLogDashboard.Host`，适合把日志看板单独部署到一个端口，再通过 nginx 暴露给团队使用。

```bash
dotnet run --project src/JLogDashboard.Host
```

默认开发地址：`http://localhost:5088/jlog`。

生产环境建议通过环境变量覆盖敏感配置：

```bash
JLogDashboard__BasicAuth__Enabled=true
JLogDashboard__BasicAuth__Username=admin
JLogDashboard__BasicAuth__PasswordSha256=<sha256>
ASPNETCORE_URLS=http://127.0.0.1:5088
```

## 配置示例

```json
{
  "JLogDashboard": {
    "RoutePrefix": "/jlog",
    "Culture": "zh-CN",
    "DefaultPageSize": 50,
    "MaxPageSize": 500,
    "MaxFileBytes": 10485760,
    "BasicAuth": {
      "Enabled": true,
      "Username": "admin",
      "Password": "",
      "PasswordSha256": "替换为 SHA-256 小写十六进制值",
      "Realm": "JLogDashboard",
      "MaxFailedAttempts": 5,
      "LockoutSeconds": 300
    },
    "Projects": [
      {
        "Name": "orders",
        "DirectoryPath": "/var/log/orders",
        "Provider": "serilog",
        "FileSearchPattern": "*.log",
        "Recursive": false
      },
      {
        "Name": "billing",
        "DirectoryPath": "/var/log/billing",
        "Provider": "nlog",
        "FileSearchPattern": "*.log",
        "Recursive": true
      }
    ]
  }
}
```

## 安全建议

- Basic Auth 适合作为轻量访问门禁，不建议裸露在公网 HTTP 下使用。
- 外网可访问时，请务必放在 HTTPS 或内网 VPN 后面。
- 生产环境优先使用 `PasswordSha256`，不要把明文密码提交到仓库。
- 连续错误凭据会触发短时间锁定，默认同一客户端 IP 失败 5 次后锁定 300 秒；匿名访问只会收到 `401` 质询，不会消耗锁定次数。
- 如果部署在 nginx 后面，请正确转发 `X-Forwarded-For`，否则失败锁定只能看到代理 IP。

## nginx 配置

Dashboard 页面内置 nginx 配置生成器，可以根据页面输入的域名、上游地址和路径生成配置。示例：

```nginx
server {
    listen 80;
    server_name logs.example.com;

    location /jlog/ {
        proxy_pass http://127.0.0.1:5088;
        proxy_http_version 1.1;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection "upgrade";
    }
}
```

## NuGet 发布

本仓库使用 NuGet Trusted Publishing，无需长期保存 API Key。当前策略信息：

- Package ID: `JLogDashboard`
- Package owner: `pp_nuget`
- GitHub repository owner: `ppengit`
- GitHub repository: `JLogDashboard`
- Workflow: `build.yml`
- Environment: `production`

普通 push 和 PR 只运行构建测试。发布版本时创建 `v*` tag 或手动触发 GitHub Actions，工作流会进入 `production` 环境并使用 NuGet OIDC 短期凭据发布包。

注意：`NuGet/login@v1` 的 `user` 参数需要填写 Trusted Publishing 策略创建者账号。当前仓库应填写 `penjay`，而不是包 owner `pp_nuget` 或 GitHub 仓库 owner `ppengit`。

## 本地开发

```bash
dotnet restore
dotnet build JLogDashboard.sln
dotnet test JLogDashboard.sln
dotnet pack src/JLogDashboard/JLogDashboard.csproj -c Release -o artifacts/packages
```

## 许可证

[MIT](./LICENSE)
