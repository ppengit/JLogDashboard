# JLogDashboard

[![Build](https://github.com/ppengit/JLogDashboard/actions/workflows/build.yml/badge.svg)](https://github.com/ppengit/JLogDashboard/actions/workflows/build.yml)
[![NuGet](https://img.shields.io/nuget/v/JLogDashboard)](https://www.nuget.org/packages/JLogDashboard)
[![License](https://img.shields.io/badge/license-MIT-blue.svg)](https://github.com/ppengit/JLogDashboard/blob/main/LICENSE)

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
- Project, level, keyword, exclude-keyword, and time-range filtering.
- Tail-window reading for large files to avoid loading entire log files into memory.
- Exception stack trace grouping for unmatched continuation lines.
- Built-in Dashboard UI for ASP.NET Core applications.
- Standalone host for separate port and reverse-proxy deployments.
- Built-in Basic Auth with per-client failed-attempt lockout.
- Built-in `zh-CN` and `en-US` UI text resources.
- nginx configuration generator inside the Dashboard.

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

Open `https://your-domain/jlog`.

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
- [Changelog](https://github.com/ppengit/JLogDashboard/blob/main/CHANGELOG.md)
- [Contributing](https://github.com/ppengit/JLogDashboard/blob/main/CONTRIBUTING.md)
- [Security Policy](https://github.com/ppengit/JLogDashboard/blob/main/SECURITY.md)
- [Example Configuration](https://github.com/ppengit/JLogDashboard/blob/main/examples/appsettings.sample.json)

## Security Notes

- Use HTTPS, VPN, or a trusted internal reverse proxy when the dashboard is reachable outside a private network.
- Prefer `PasswordSha256` over plain-text passwords in any shared environment.
- Anonymous requests return `401` challenges and do not consume lockout attempts.
- Repeated invalid credentials trigger a temporary lockout for the same client IP.
- When deployed behind nginx or another reverse proxy, forward `X-Forwarded-For` so the lockout mechanism can identify the actual client.
- Do not point log directories at broad parent folders that may contain unrelated secrets or configuration files.

## Reverse Proxy

The Dashboard includes an nginx configuration generator. A typical configuration looks like this:

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

## Limitations

- File-based log viewing only; there is no centralized storage or query index.
- Designed for operational inspection, not long-term analytics or alerting.
- Built-in authentication is intentionally simple and should be treated like an internal-tool guard, not a full identity system.

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
