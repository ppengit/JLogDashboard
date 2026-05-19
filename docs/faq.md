# FAQ

[English](./faq.md) | [简体中文](./zh-CN/faq.md)

## Does JLogDashboard store logs in a database?

No. It reads log files directly from the filesystem.

## Does it support centralized log indexing?

No. JLogDashboard is intentionally lightweight. It is designed for direct file inspection rather than full observability pipelines.

## Which logging frameworks are supported?

The dashboard targets common text log layouts from:

- NLog
- log4net
- Serilog

It can also read other plain-text logs when the format is close enough to the supported patterns.

## Can I use it with multiple applications?

Yes. Configure multiple projects under `JLogDashboard:Projects`, each pointing to a different log directory.

## Can I run it on a separate port?

Yes. The repository includes `src/JLogDashboard.Host` for standalone hosting, and the Dashboard UI also includes nginx configuration assistance.

## How do I change the port or URL path?

Use `Urls` or `ASPNETCORE_URLS` for the bind address and port, and `JLogDashboard:RoutePrefix` for the dashboard path.

Example:

```json
{
  "Urls": "http://127.0.0.1:5099",
  "JLogDashboard": {
    "RoutePrefix": "/ops-logs"
  }
}
```

Result:

```text
http://127.0.0.1:5099/ops-logs
```

## Is Basic Auth enough for internet exposure?

It is only a lightweight access gate. For internet-reachable deployments, place the dashboard behind HTTPS, VPN, or a trusted reverse proxy and prefer hashed passwords.

## Why do anonymous requests return `401` but not trigger lockout?

That behavior is intentional. Anonymous probes should receive a challenge, while repeated invalid credentials are what consume lockout attempts.

## Why are very old log lines missing from a large file?

JLogDashboard reads only the tail window defined by `MaxFileBytes` so large files do not consume excessive memory.

## Can I customize the URL path?

Yes. Set `RoutePrefix`, for example:

```json
{
  "JLogDashboard": {
    "RoutePrefix": "/ops-logs"
  }
}
```

## Does it require a frontend build process?

No. The dashboard UI is served without a separate frontend build chain.
