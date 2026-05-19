# 部署指南

[English](../deployment.md) | [简体中文](./deployment.md)

本文说明 JLogDashboard 的常见部署方式。

## 部署模式

JLogDashboard 主要支持两种实用部署方式：

1. 嵌入现有 ASP.NET Core 应用。
2. 独立运行 Dashboard 进程，并通过反向代理暴露。

如果 Dashboard 只是某个现有服务的附属能力，适合嵌入部署；如果你希望独立运维、独立生命周期或独立端口，更适合独立宿主部署。

## 嵌入式部署

在现有 ASP.NET Core 应用中直接引用 NuGet 包：

```csharp
using JLogDashboard.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddJLogDashboard(options =>
{
    options.RoutePrefix = "/jlog";
    options.BasicAuth.Enabled = true;
    options.BasicAuth.Username = "admin";
    options.BasicAuth.PasswordSha256 = "replace-with-sha256-hex";
    options.Projects.Add(new()
    {
        Name = "orders",
        DirectoryPath = "/var/log/orders",
        Provider = "serilog"
    });
});

var app = builder.Build();
app.MapJLogDashboard();
app.Run();
```

典型访问地址：

```text
https://your-domain/jlog
```

仓库内示例：[`samples/JLogDashboard.SampleWeb`](https://github.com/ppengit/JLogDashboard/tree/main/samples/JLogDashboard.SampleWeb)（账号 `admin`，密码 `sample-password`）

## 独立宿主部署

仓库中包含 `src/JLogDashboard.Host`，适合将日志看板与业务应用分开运行。

本地运行：

```bash
dotnet run --project src/JLogDashboard.Host
```

默认开发地址：

```text
http://localhost:5088/jlog
```

示例配置见 [examples/appsettings.sample.json](https://github.com/ppengit/JLogDashboard/blob/main/examples/appsettings.sample.json)。

## 自定义端口与路径

独立宿主不要求固定端口，也不要求固定路径。

通过 `Urls` 或 `ASPNETCORE_URLS` 修改监听地址和端口：

```json
{
  "Urls": "http://0.0.0.0:5099"
}
```

或者：

```bash
ASPNETCORE_URLS=http://0.0.0.0:5099
```

通过 `JLogDashboard:RoutePrefix` 修改看板路径：

```json
{
  "JLogDashboard": {
    "RoutePrefix": "/ops-logs"
  }
}
```

最终地址示例：

```text
http://your-host:5099/ops-logs
```

## 通过 nginx 反向代理

当需要友好的域名或统一入口时，推荐放在 nginx 后面：

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

## 生产环境检查清单

- 启用 `BasicAuth.Enabled`。
- 优先使用 `PasswordSha256`，不要依赖明文密码。
- 使用 HTTPS，或将 Dashboard 放在 VPN / 可信内网代理之后。
- 使用反向代理时正确转发 `X-Forwarded-For`。
- 明确配置 `Urls` 和 `RoutePrefix`，避免环境间地址暴露不一致。
- 将文件系统读取权限限制在目标日志目录内。
- 独立宿主部署时显式设置 `ASPNETCORE_URLS`。

## 运维说明

- JLogDashboard 直接读取日志文件，不维护独立索引。
- 大文件只会读取由 `MaxFileBytes` 控制的尾部窗口。
- 它的目标是快速查看和排障，不是集中式长期分析平台。
- 常见的 Dashboard 运行时故障会被限制在看板路由内部，不应影响宿主应用的其他业务接口继续提供服务。
- fatal 级别的 Dashboard 配置错误也会被限制在看板路由内部。运维上应将启动期 error 日志理解为“Dashboard 被阻断”，而不是“宿主应用已崩溃”。
