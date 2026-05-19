# JLogDashboard

[![Build](https://github.com/ppengit/JLogDashboard/actions/workflows/build.yml/badge.svg)](https://github.com/ppengit/JLogDashboard/actions/workflows/build.yml)
[![NuGet](https://img.shields.io/nuget/v/JLogDashboard)](https://www.nuget.org/packages/JLogDashboard)
[![License](https://img.shields.io/badge/license-MIT-blue.svg)](https://github.com/ppengit/JLogDashboard/blob/main/LICENSE)

[English](https://github.com/ppengit/JLogDashboard/blob/main/README.md) | [简体中文](https://github.com/ppengit/JLogDashboard/blob/main/README.zh-CN.md)

JLogDashboard 是一个轻量级的 ASP.NET Core 日志看板，用于查看多个项目中的 NLog、log4net 和 Serilog 文件日志。

它适合常见的运维场景：多套 .NET 服务部署在同一台机器上，日志仍然落地到本地文件，工程师需要一个简单直接的界面来查看近期日志、过滤噪声、定位异常堆栈，而不想额外引入一整套独立的日志平台。

## 为什么使用 JLogDashboard

- 轻量：不依赖数据库、不做后台索引、不需要前端构建链。
- 实用：面向常见的 .NET 文件日志场景，接入成本低。
- 易部署：既可以嵌入现有 ASP.NET Core 应用，也可以独立部署成单独的日志看板。
- 默认更安全：内置 Basic Auth 和轻量级失败锁定机制，减少被随意探测和暴力尝试的风险。

## 功能特性

- 支持配置多个项目的日志目录。
- 兼容常见的 NLog、log4net、Serilog 文本日志格式。
- 支持按项目、级别、关键字、排除关键字、时间范围筛选。
- 对大文件采用尾部窗口读取，避免一次性加载整份日志。
- 对未匹配的续行内容自动做异常堆栈归并。
- 内置适用于 ASP.NET Core 的 Dashboard UI。
- 提供独立宿主，可单独监听端口或放在反向代理之后。
- 内置 Basic Auth，并对同一客户端的重复失败尝试进行临时锁定。
- 内置 `zh-CN` 与 `en-US` 界面文本资源。
- Dashboard 内置 nginx 配置生成功能。

## 适用场景

JLogDashboard 适合以下情况：

- 应用已经将日志写入本地文件；
- 团队需要一个轻量的内部日志看板，而不是完整的可观测性平台；
- 希望快速自托管一个日志查看界面；
- 更偏向直接读取文件系统，而不是引入日志采集和集中式管道。

它并不是 Elasticsearch、Loki、Seq、Splunk 这类集中式日志分析系统的替代品。

## 安装

```bash
dotnet add package JLogDashboard
```

NuGet 包地址：https://www.nuget.org/packages/JLogDashboard

## 快速开始

### 方式 1：嵌入到 ASP.NET Core 应用

```csharp
using JLogDashboard.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddJLogDashboard(options =>
{
    options.RoutePrefix = "/jlog";
    options.MaxFileBytes = 10 * 1024 * 1024;

    options.BasicAuth.Enabled = true;
    options.BasicAuth.Username = "admin";
    options.BasicAuth.PasswordSha256 = "replace-with-sha256-hex";

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

访问 `https://your-domain/jlog`。

### 方式 2：使用独立宿主运行

仓库内提供了 `src/JLogDashboard.Host`，适合需要单独开放端口或放在反向代理后的场景。

```bash
dotnet run --project src/JLogDashboard.Host
```

默认开发地址：

```text
http://localhost:5088/jlog
```

独立宿主的监听地址和端口可以通过 `appsettings.json` 中的 `Urls` 或环境变量 `ASPNETCORE_URLS` 自定义。看板访问路径可以通过 `JLogDashboard:RoutePrefix` 自定义。

示例：

```bash
ASPNETCORE_URLS=http://0.0.0.0:5099 dotnet run --project src/JLogDashboard.Host
```

生产风格示例配置见 [examples/appsettings.sample.json](https://github.com/ppengit/JLogDashboard/blob/main/examples/appsettings.sample.json)。

## 文档导航

- [中文部署指南](https://github.com/ppengit/JLogDashboard/blob/main/docs/zh-CN/deployment.md)
- [中文配置参考](https://github.com/ppengit/JLogDashboard/blob/main/docs/zh-CN/configuration.md)
- [中文常见问题](https://github.com/ppengit/JLogDashboard/blob/main/docs/zh-CN/faq.md)
- [Deployment Guide](https://github.com/ppengit/JLogDashboard/blob/main/docs/deployment.md)
- [Configuration Reference](https://github.com/ppengit/JLogDashboard/blob/main/docs/configuration.md)
- [FAQ](https://github.com/ppengit/JLogDashboard/blob/main/docs/faq.md)
- [更新日志](https://github.com/ppengit/JLogDashboard/blob/main/CHANGELOG.md)
- [贡献指南](https://github.com/ppengit/JLogDashboard/blob/main/CONTRIBUTING.md)
- [安全策略](https://github.com/ppengit/JLogDashboard/blob/main/SECURITY.md)
- [示例配置](https://github.com/ppengit/JLogDashboard/blob/main/examples/appsettings.sample.json)

## 安全说明

- 当看板可能被私网外访问时，应配合 HTTPS、VPN 或可信反向代理使用。
- 在共享环境中优先使用 `PasswordSha256`，不要长期保留明文密码。
- 匿名请求会返回 `401` 质询，但不会消耗锁定次数。
- 同一客户端的重复错误凭据会触发临时锁定。
- 部署在 nginx 或其他反向代理之后时，需正确转发 `X-Forwarded-For`，让锁定机制识别真实客户端。
- 不要把日志目录指向过于宽泛的父目录，以免暴露无关的敏感文件或配置文件。
- Dashboard 运行时故障会限制在看板路由内部。常见的日志读取失败或端点异常不应影响宿主应用的其他业务接口。

## 反向代理

Dashboard 内置 nginx 配置生成功能。典型配置如下：

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

## 限制说明

- 仅面向文件日志查看，不提供集中式存储或查询索引。
- 设计目标是运维排障和快速查看，不是长期分析或告警平台。
- 内置认证机制故意保持轻量，应视为内部工具防护层，而不是完整身份系统。

## 其他资源

- [维护者发版指南](https://github.com/ppengit/JLogDashboard/blob/main/docs/maintainers/releasing.md)

## 开发

```bash
dotnet restore
dotnet build JLogDashboard.sln
dotnet test JLogDashboard.sln
dotnet pack src/JLogDashboard/JLogDashboard.csproj -c Release -o artifacts/packages
```

## 许可证

[MIT](https://github.com/ppengit/JLogDashboard/blob/main/LICENSE)
