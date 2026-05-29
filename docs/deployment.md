# Deployment Guide

[English](./deployment.md) | [简体中文](./zh-CN/deployment.md)

This guide covers the common deployment patterns for JLogDashboard.

## Deployment Modes

JLogDashboard supports two practical deployment modes:

1. Embedded into an existing ASP.NET Core application.
2. Hosted as a standalone dashboard process and exposed through a reverse proxy.

Choose the embedded mode when the dashboard is only needed inside an existing service. Choose the standalone mode when you want an isolated operational endpoint, separate lifecycle, or a dedicated port.

## Embedded Deployment

Use the NuGet package inside an existing ASP.NET Core application:

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

Typical access path:

```text
https://your-domain/jlog
```

Repository sample: [`samples/JLogDashboard.SampleWeb`](https://github.com/ppengit/JLogDashboard/tree/main/samples/JLogDashboard.SampleWeb) (`admin` / `sample-password`)

## Standalone Host Deployment

The repository contains `src/JLogDashboard.Host`, which is useful when you want to run the dashboard separately from your business applications.

Run locally:

```bash
dotnet run --project src/JLogDashboard.Host
```

Default development address:

```text
http://localhost:5088/jlog
```

For configuration examples, see [examples/appsettings.sample.json](https://github.com/ppengit/JLogDashboard/blob/main/examples/appsettings.sample.json).

## Custom Port And Path

The standalone host does not require a fixed port or fixed path.

Use `Urls` or `ASPNETCORE_URLS` to change the bind address and port:

```json
{
  "Urls": "http://0.0.0.0:5099"
}
```

Or:

```bash
ASPNETCORE_URLS=http://0.0.0.0:5099
```

Use `JLogDashboard:RoutePrefix` to change the dashboard path:

```json
{
  "JLogDashboard": {
    "RoutePrefix": "/ops-logs"
  }
}
```

Resulting address example:

```text
http://your-host:5099/ops-logs
```

## Reverse Proxy And HTTPS

JLogDashboard is mounted in the ASP.NET Core host by `RoutePrefix`; reverse-proxy configuration belongs to your infrastructure rather than the Dashboard UI. When the dashboard is reachable through a public or shared network, use HTTPS, VPN, or a trusted internal proxy.

If you use the built-in Basic Auth lockout mechanism behind a reverse proxy, preserve `X-Forwarded-For` so repeated failed attempts are counted against the actual client instead of the proxy address.

## Production Checklist

- Enable `BasicAuth.Enabled`.
- Prefer `PasswordSha256` over plain-text passwords.
- Use HTTPS or place the dashboard behind VPN or a trusted internal proxy.
- Forward `X-Forwarded-For` when using a reverse proxy.
- Keep `Urls` and `RoutePrefix` explicit so each environment exposes the intended address only.
- Restrict filesystem access so the dashboard only reads intended log directories.
- Set `ASPNETCORE_URLS` explicitly for standalone deployments.

## Operational Notes

- JLogDashboard reads from log files directly and does not maintain a separate index.
- Large files are read from the tail window defined by `MaxFileBytes`.
- The dashboard is intended for inspection and troubleshooting, not for centralized long-term analytics.
- Common dashboard runtime faults are isolated to the dashboard route so unrelated host endpoints can continue serving requests.
- Fatal dashboard misconfiguration is also isolated to the dashboard route. Operators should treat startup error logs as a blocked dashboard, not as a host crash.
