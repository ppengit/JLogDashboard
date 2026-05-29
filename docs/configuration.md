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

### `Parser`

Optional parser settings for custom text log layouts. If omitted or set to `auto`, JLogDashboard uses the built-in recognizers for common Serilog, NLog, log4net, and simple text layouts.

#### `Parser.Mode`

- Type: `string`
- Default: `auto`
- Supported values: `auto`, `nlog-layout`, `log4net-pattern`, `serilog-template`, `delimited`, `regex`

#### `Parser.Layout`

- Type: `string`
- Used when `Mode` is `nlog-layout`, `log4net-pattern`, or `serilog-template`
- Purpose: parse common logging-framework templates without requiring field indexes
- Required semantic fields: timestamp and level
- Optional semantic fields: logger/source context, message, exception, newline
- Other metadata fields are treated as ignored fields in the log header
- These parsers are pragmatic file-log readers, not full implementations of every NLog renderer, log4net conversion pattern, or Serilog property formatter

NLog layout example:

```text
${longdate}|${event-properties:item=EventId}|${level}|${logger}${newline}${message}${exception:format=tostring}
```

Supported NLog fields include `${longdate}`, `${date:format=...}`, `${level}`, `${logger}`, `${message}`, `${exception}`, and `${newline}`. Other renderers, such as `${event-properties:item=EventId}` or `${threadid}`, are treated as ignored header fields.

log4net PatternLayout example:

```text
%date{yyyy-MM-dd HH:mm:ss,fff} [%thread] %-5level %logger - %message%newline%exception
```

Supported log4net fields include `%date`, `%level`, `%logger`, `%message`, `%exception`, `%newline`, and their common short forms such as `%d`, `%p`, `%c`, `%m`, `%ex`, and `%n`. Width modifiers such as `%-5level` are accepted.

Serilog outputTemplate example:

```text
{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {SourceContext} {Message:lj}{NewLine}{Exception}
```

Supported Serilog fields include `{Timestamp:...}`, `{Level}`, `{SourceContext}`, `{Message}`, `{Exception}`, and `{NewLine}`. Additional properties in the header are treated as ignored fields.

#### `Parser.Delimiter`

- Type: `string`
- Default: `|`
- Used when `Mode` is `delimited`
- Example: `||`

#### `Parser.TimestampIndex`, `Parser.LevelIndex`, `Parser.LoggerIndex`, `Parser.MessageIndex`, `Parser.ExceptionIndex`

- Type: `int` or `int?`
- Used when `Mode` is `delimited`
- Indexes are zero-based
- `LoggerIndex` and `ExceptionIndex` can be omitted
- `MessageIndex` may point to a missing field when the first continuation line contains the message body; in that case JLogDashboard promotes the first continuation line to `Message` and keeps the remaining continuation lines as `Exception`.

#### `Parser.Pattern`

- Type: `string`
- Used when `Mode` is `regex`
- Required named groups: `timestamp`, `level`, `message`
- Optional named groups: `logger`, `exception`

#### `Parser.TimestampFormat`

- Type: `string`
- Optional
- Example: `yyyy/MM/dd HH:mm:ss`

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
        "Recursive": false,
        "Parser": {
          "Mode": "serilog-template",
          "Layout": "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {SourceContext} {Message:lj}{NewLine}{Exception}"
        }
      },
      {
        "Name": "api.backend",
        "DirectoryPath": "/var/log/api.backend",
        "Provider": "nlog",
        "FileSearchPattern": "*.log",
        "Recursive": false,
        "Parser": {
          "Mode": "nlog-layout",
          "Layout": "${longdate}|${event-properties:item=EventId}|${level}|${logger}${newline}${message}${exception:format=tostring}"
        }
      },
      {
        "Name": "legacy",
        "DirectoryPath": "/var/log/legacy",
        "Provider": "log4net",
        "FileSearchPattern": "*.log",
        "Recursive": false,
        "Parser": {
          "Mode": "log4net-pattern",
          "Layout": "%date{yyyy-MM-dd HH:mm:ss,fff} [%thread] %-5level %logger - %message%newline%exception"
        }
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
JLogDashboard__Projects__0__Parser__Mode=serilog-template
JLogDashboard__Projects__0__Parser__Layout={Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {SourceContext} {Message:lj}{NewLine}{Exception}
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

At startup, JLogDashboard reports configuration issues as either warnings or fatal errors.

Fatal errors block Dashboard requests with controlled `503` responses, but they do not crash the host process. Warnings do not block the Dashboard and are intended to help operators catch risky configuration before external exposure.

Fatal errors include:

- missing projects;
- invalid page-size limits;
- invalid `MaxFileBytes`;
- missing Basic Auth credentials when auth is enabled;
- invalid `PasswordSha256` format;
- duplicate project names.

Warnings can include:

- default password `change-me`;
- plain-text Basic Auth password usage;
- Basic Auth disabled;
- unknown `Provider` values;
- unsupported `Parser.Mode` values;
- empty custom parser delimiter, regex pattern, or framework layout/template;
- missing project directories at startup;
- empty `FileSearchPattern`;
- non-built-in UI culture values.
