# JLogDashboard

[![Build](https://github.com/ppengit/JLogDashboard/actions/workflows/build.yml/badge.svg)](https://github.com/ppengit/JLogDashboard/actions/workflows/build.yml)
[![NuGet](https://img.shields.io/nuget/v/JLogDashboard)](https://www.nuget.org/packages/JLogDashboard)
[![License](https://img.shields.io/badge/license-MIT-blue.svg)](https://github.com/ppengit/JLogDashboard/blob/main/LICENSE)

[English](https://github.com/ppengit/JLogDashboard/blob/main/README.md) | [简体中文](https://github.com/ppengit/JLogDashboard/blob/main/README.zh-CN.md)

JLogDashboard is a lightweight ASP.NET Core dashboard for viewing NLog, log4net, and Serilog file logs across multiple projects.

It is designed for the common operational scenario where multiple .NET services write plain-text log files on one server, and engineers need a simple way to inspect recent logs, filter noise, and review exception stacks without introducing a separate log platform.

## Why JLogDashboard

- Lightweight: no database, no background indexing service, no frontend build chain.
- Practical: supports common file-based .NET logging setups with minimal integration work.
- Deployable: can be embedded into an existing ASP.NET Core application or hosted as a standalone dashboard.
- Safer by default: includes built-in Basic Auth and a lightweight lockout mechanism for repeated failed credentials.

## Features

- Multi-project log directory configuration.
- Compatible with common NLog, log4net, and Serilog text layouts.
- Explicit parser modes for NLog layout strings, log4net PatternLayout strings, Serilog outputTemplate strings, delimited logs, and regex logs.
- Project, level, keyword, exclude-keyword, and pagination filtering.
- Tail-window reading for large files to avoid loading entire log files into memory.
- Exception stack trace grouping for unmatched continuation lines.
- Built-in full-width Dashboard UI with project switching, filters, paging, refresh, internal scrolling, automatic initial loading, and current-page duplicate grouping with count tags.
- Standalone host for separate port and reverse-proxy deployments.
- Built-in Basic Auth with per-client failed-attempt lockout.
- Built-in `zh-CN` and `en-US` UI text resources.

## When To Use

JLogDashboard is a good fit when:

- your applications already write logs to local files;
- your team needs a lightweight internal dashboard instead of a full observability stack;
- you want a self-hosted log viewer that can be deployed in minutes;
- you prefer direct filesystem access over log shipping and centralized ingestion.

It is not intended to replace Elasticsearch, Loki, Seq, Splunk, or similar systems for large-scale centralized log analytics.

## Installation

```bash
dotnet add package JLogDashboard
```

NuGet package: https://www.nuget.org/packages/JLogDashboard

## Quick Start

### Option 1: Embed into an ASP.NET Core application

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
        Name = "api.backend",
        DirectoryPath = "/var/log/api.backend",
        Provider = "nlog",
        FileSearchPattern = "*.log",
        Recursive = false,
        Parser =
        {
            Mode = "nlog-layout",
            Layout = "${longdate}|${event-properties:item=EventId}|${level}|${logger}${newline}${message}${exception:format=tostring}"
        }
    });
});

var app = builder.Build();

app.MapJLogDashboard();
app.Run();
```

Open `https://your-domain/jlog`.

Runnable repository sample: [`samples/JLogDashboard.SampleWeb`](https://github.com/ppengit/JLogDashboard/tree/main/samples/JLogDashboard.SampleWeb) (`admin` / `sample-password`)

### Parser configuration examples

Dashboard runtime actions stay focused on browsing and filtering logs. Parser rules, log directories, authentication, ports, and route paths are configured in the host application through `AddJLogDashboard(...)` or the `JLogDashboard` configuration section.

Use explicit parser modes when the built-in `auto` recognizers cannot reliably identify your log structure:

```csharp
options.Projects.Add(new()
{
    Name = "api.nlog",
    DirectoryPath = "/var/log/api.nlog",
    Provider = "nlog",
    Parser =
    {
        Mode = "nlog-layout",
        Layout = "${longdate}|${event-properties:item=EventId}|${level}|${logger}${newline}${message}${exception:format=tostring}"
    }
});

options.Projects.Add(new()
{
    Name = "legacy.log4net",
    DirectoryPath = "/var/log/legacy",
    Provider = "log4net",
    Parser =
    {
        Mode = "log4net-pattern",
        Layout = "%date{yyyy-MM-dd HH:mm:ss,fff} [%thread] %-5level %logger - %message%newline%exception"
    }
});

options.Projects.Add(new()
{
    Name = "orders.serilog",
    DirectoryPath = "/var/log/orders",
    Provider = "serilog",
    Parser =
    {
        Mode = "serilog-template",
        Layout = "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {SourceContext} {Message:lj}{NewLine}{Exception}"
    }
});
```

These template parsers intentionally support the common file-log subset needed for browsing: timestamp, level, logger/source context, message, exception, newline, and ignored metadata fields. For unusual formats, use `delimited` or `regex`.

### Option 2: Run the standalone host

The repository includes `src/JLogDashboard.Host` for teams that want to expose the dashboard on a dedicated port or behind a reverse proxy.

```bash
dotnet run --project src/JLogDashboard.Host
```

Default development URL:

```text
http://localhost:5088/jlog
```

The standalone host bind address and port can be customized through `Urls` in `appsettings.json` or the `ASPNETCORE_URLS` environment variable. The dashboard path can be customized through `JLogDashboard:RoutePrefix`.

Example:

```bash
ASPNETCORE_URLS=http://0.0.0.0:5099 dotnet run --project src/JLogDashboard.Host
```

For a production-style sample, see [examples/appsettings.sample.json](https://github.com/ppengit/JLogDashboard/blob/main/examples/appsettings.sample.json).

## Documentation Map

- [Deployment Guide](https://github.com/ppengit/JLogDashboard/blob/main/docs/deployment.md)
- [Configuration Reference](https://github.com/ppengit/JLogDashboard/blob/main/docs/configuration.md)
- [FAQ](https://github.com/ppengit/JLogDashboard/blob/main/docs/faq.md)
- [中文文档入口](https://github.com/ppengit/JLogDashboard/blob/main/README.zh-CN.md)
- [部署指南（中文）](https://github.com/ppengit/JLogDashboard/blob/main/docs/zh-CN/deployment.md)
- [配置参考（中文）](https://github.com/ppengit/JLogDashboard/blob/main/docs/zh-CN/configuration.md)
- [常见问题（中文）](https://github.com/ppengit/JLogDashboard/blob/main/docs/zh-CN/faq.md)
- [Changelog](https://github.com/ppengit/JLogDashboard/blob/main/CHANGELOG.md)
- [Contributing](https://github.com/ppengit/JLogDashboard/blob/main/CONTRIBUTING.md)
- [Security Policy](https://github.com/ppengit/JLogDashboard/blob/main/SECURITY.md)
- [Example Configuration](https://github.com/ppengit/JLogDashboard/blob/main/examples/appsettings.sample.json)

## Security Notes

- Use HTTPS, VPN, or a trusted internal reverse proxy when the dashboard is reachable outside a private network.
- Prefer `PasswordSha256` over plain-text passwords in any shared environment.
- Startup diagnostics classify configuration issues into warnings and fatal errors. Fatal dashboard misconfiguration blocks dashboard requests with controlled `503` responses without crashing the host application.
- Anonymous requests return `401` challenges and do not consume lockout attempts.
- Repeated invalid credentials trigger a temporary lockout for the same client IP.
- When deployed behind a reverse proxy, forward `X-Forwarded-For` so the lockout mechanism can identify the actual client.
- Do not point log directories at broad parent folders that may contain unrelated secrets or configuration files.
- Dashboard runtime faults are isolated to the dashboard route. Common log-read and endpoint failures should not break unrelated host application endpoints.

## Reverse Proxy

JLogDashboard does not generate reverse-proxy configuration in the Dashboard UI. Mount the Dashboard in the host application with `RoutePrefix`, then configure your infrastructure separately. For internet-reachable deployments, use HTTPS, VPN, or a trusted internal proxy and preserve `X-Forwarded-For` when you rely on the built-in lockout mechanism.

## Limitations

- File-based log viewing only; there is no centralized storage or query index.
- Designed for operational inspection, not long-term analytics or alerting.
- Built-in authentication is intentionally simple and should be treated like an internal-tool guard, not a full identity system.
- Layout/template parsing is a pragmatic subset of NLog, log4net, and Serilog formatting, not a full reimplementation of every renderer or property formatter.
- Duplicate grouping is a Dashboard UI presentation feature for the current page. The search API and pagination totals still represent raw log entries.

## Additional Resources

- [Maintainer Release Guide](https://github.com/ppengit/JLogDashboard/blob/main/docs/maintainers/releasing.md)

## Development

```bash
dotnet restore
dotnet build JLogDashboard.sln
dotnet test JLogDashboard.sln
dotnet pack src/JLogDashboard/JLogDashboard.csproj -c Release -o artifacts/packages
```

## License

[MIT](https://github.com/ppengit/JLogDashboard/blob/main/LICENSE)
