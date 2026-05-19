# Configuration Reference

[English](./configuration.md) | [简体中文](./zh-CN/configuration.md)

JLogDashboard can be configured through code or through the `JLogDashboard` configuration section.

## Top-Level Options

### `RoutePrefix`

- Type: `string`
- Default: `/jlog`
- Purpose: URL prefix for the dashboard page and its APIs.
- Example: `/ops-logs`

### `Culture`

- Type: `string`
- Default: `zh-CN`
- Built-in values: `zh-CN`, `en-US`

### `DefaultPageSize`

- Type: `int`
- Default: `50`
- Purpose: fallback page size when the request omits an invalid value.

### `MaxPageSize`

- Type: `int`
- Default: `500`
- Purpose: upper bound for a single query page size.

### `MaxFileBytes`

- Type: `long`
- Default: `10485760`
- Meaning: read at most 10 MB from the tail of each log file.

## `BasicAuth`

### `Enabled`

- Type: `bool`
- Default: `false`

### `Username`

- Type: `string`
- Required when `Enabled` is `true`

### `Password`

- Type: `string`
- Optional
- Intended mainly for local demos or simple internal setups

### `PasswordSha256`

- Type: `string`
- Optional
- Preferred for shared and production environments
- Value format: hexadecimal SHA-256 string

### `Realm`

- Type: `string`
- Default: `JLogDashboard`
- Purpose: browser Basic Auth prompt label

### `MaxFailedAttempts`

- Type: `int`
- Default: `5`

### `LockoutSeconds`

- Type: `int`
- Default: `300`

## `Projects`

Each configured project represents a searchable log root.

### `Name`

- Type: `string`
- Required
- Must be unique within the dashboard configuration

### `DirectoryPath`

- Type: `string`
- Required
- Path to the log directory

### `Provider`

- Type: `string`
- Common values: `auto`, `serilog`, `nlog`, `log4net`

### `FileSearchPattern`

- Type: `string`
- Example: `*.log`

### `Recursive`

- Type: `bool`
- Purpose: whether subdirectories should be included

## JSON Example

```json
{
  "Urls": "http://127.0.0.1:5088",
  "JLogDashboard": {
    "RoutePrefix": "/jlog",
    "Culture": "zh-CN",
    "DefaultPageSize": 50,
    "MaxPageSize": 500,
    "MaxFileBytes": 10485760,
    "BasicAuth": {
      "Enabled": true,
      "Username": "admin",
      "PasswordSha256": "replace-with-sha256-hex",
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
      }
    ]
  }
}
```

## Host-Level Settings

The standalone host also supports regular ASP.NET Core host configuration:

### `Urls`

- Type: `string`
- Example: `http://127.0.0.1:5088`
- Purpose: configures the bind address and port for `src/JLogDashboard.Host`

Equivalent environment variable:

```bash
ASPNETCORE_URLS=http://0.0.0.0:5099
```

Use `Urls` or `ASPNETCORE_URLS` when different environments require different ports or bind addresses.

## Environment Variable Example

```bash
ASPNETCORE_URLS=http://0.0.0.0:5099
JLogDashboard__RoutePrefix=/jlog
JLogDashboard__Culture=en-US
JLogDashboard__BasicAuth__Enabled=true
JLogDashboard__BasicAuth__Username=admin
JLogDashboard__BasicAuth__PasswordSha256=<sha256>
JLogDashboard__Projects__0__Name=orders
JLogDashboard__Projects__0__DirectoryPath=/var/log/orders
JLogDashboard__Projects__0__Provider=serilog
```

## Common Customization Examples

### Change the dashboard path

```json
{
  "JLogDashboard": {
    "RoutePrefix": "/ops-logs"
  }
}
```

Result:

```text
http://your-host/ops-logs
```

### Change the standalone host port

```json
{
  "Urls": "http://127.0.0.1:5099"
}
```

Result:

```text
http://127.0.0.1:5099/jlog
```

## Validation Rules

At startup, the host can report configuration warnings for:

- missing projects;
- invalid page-size limits;
- invalid `MaxFileBytes`;
- missing Basic Auth credentials when auth is enabled;
- default password `change-me`;
- duplicate project names.
