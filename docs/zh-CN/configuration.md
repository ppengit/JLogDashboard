# 配置参考

[English](../configuration.md) | [简体中文](./configuration.md)

JLogDashboard 支持通过代码配置，也支持通过 `JLogDashboard` 配置节进行绑定。

## 顶层选项

### `RoutePrefix`

- 类型：`string`
- 默认值：`/jlog`
- 用途：Dashboard 页面和相关 API 的访问路径前缀
- 示例：`/ops-logs`

### `Culture`

- 类型：`string`
- 默认值：`zh-CN`
- 内置值：`zh-CN`、`en-US`

### `DefaultPageSize`

- 类型：`int`
- 默认值：`50`
- 用途：请求未提供有效页大小时的回退值

### `MaxPageSize`

- 类型：`int`
- 默认值：`500`
- 用途：单次查询允许的最大页大小

### `MaxFileBytes`

- 类型：`long`
- 默认值：`10485760`
- 含义：每次最多从日志文件尾部读取 10 MB 内容

## `BasicAuth`

### `Enabled`

- 类型：`bool`
- 默认值：`false`

### `Username`

- 类型：`string`
- 当 `Enabled` 为 `true` 时必填

### `Password`

- 类型：`string`
- 可选
- 更适合本地演示或简单内网环境

### `PasswordSha256`

- 类型：`string`
- 可选
- 更适合共享环境和生产环境
- 值格式：十六进制 SHA-256 字符串

### `Realm`

- 类型：`string`
- 默认值：`JLogDashboard`
- 用途：浏览器 Basic Auth 弹窗中的提示标签

### `MaxFailedAttempts`

- 类型：`int`
- 默认值：`5`

### `LockoutSeconds`

- 类型：`int`
- 默认值：`300`

## `Projects`

每个配置项代表一个可检索的日志根目录。

### `Name`

- 类型：`string`
- 必填
- 在同一个 Dashboard 配置中必须唯一

### `DirectoryPath`

- 类型：`string`
- 必填
- 日志目录路径

### `Provider`

- 类型：`string`
- 常见值：`auto`、`serilog`、`nlog`、`log4net`

### `FileSearchPattern`

- 类型：`string`
- 示例：`*.log`

### `Recursive`

- 类型：`bool`
- 用途：是否包含子目录

### `Parser`

用于配置自定义文本日志格式。未配置或 `Mode` 为 `auto` 时，JLogDashboard 会使用内置识别器解析常见的 Serilog、NLog、log4net 和简单文本格式。

#### `Parser.Mode`

- 类型：`string`
- 默认值：`auto`
- 支持值：`auto`、`nlog-layout`、`log4net-pattern`、`serilog-template`、`delimited`、`regex`

#### `Parser.Layout`

- 类型：`string`
- 当 `Mode` 为 `nlog-layout`、`log4net-pattern` 或 `serilog-template` 时使用
- 用途：直接根据常见日志框架模板解析日志，避免再配置字段下标
- 必需语义字段：时间和级别
- 可选语义字段：logger/source context、消息、异常、换行
- 其他元数据字段会作为日志头中的忽略字段处理
- 这些解析器是面向文件日志查看的实用解析能力，不是完整复刻所有 NLog renderer、log4net conversion pattern 或 Serilog property formatter

NLog layout 示例：

```text
${longdate}|${event-properties:item=EventId}|${level}|${logger}${newline}${message}${exception:format=tostring}
```

支持的 NLog 字段包括 `${longdate}`、`${date:format=...}`、`${level}`、`${logger}`、`${message}`、`${exception}`、`${newline}`。其他 renderer，例如 `${event-properties:item=EventId}` 或 `${threadid}`，会作为日志头中的忽略字段处理。

log4net PatternLayout 示例：

```text
%date{yyyy-MM-dd HH:mm:ss,fff} [%thread] %-5level %logger - %message%newline%exception
```

支持的 log4net 字段包括 `%date`、`%level`、`%logger`、`%message`、`%exception`、`%newline`，以及 `%d`、`%p`、`%c`、`%m`、`%ex`、`%n` 等常见短写形式。`%-5level` 这类宽度修饰也可以识别。

Serilog outputTemplate 示例：

```text
{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {SourceContext} {Message:lj}{NewLine}{Exception}
```

支持的 Serilog 字段包括 `{Timestamp:...}`、`{Level}`、`{SourceContext}`、`{Message}`、`{Exception}` 和 `{NewLine}`。日志头中的其他属性会作为忽略字段处理。

#### `Parser.Delimiter`

- 类型：`string`
- 默认值：`|`
- 当 `Mode` 为 `delimited` 时使用
- 示例：`||`

#### `Parser.TimestampIndex`、`Parser.LevelIndex`、`Parser.LoggerIndex`、`Parser.MessageIndex`、`Parser.ExceptionIndex`

- 类型：`int` 或 `int?`
- 当 `Mode` 为 `delimited` 时使用
- 下标从 0 开始
- `LoggerIndex` 和 `ExceptionIndex` 可省略
- 当消息正文位于日志头之后的第一条续行时，`MessageIndex` 可以指向一个不存在的字段；此时 JLogDashboard 会把第一条续行提升为 `Message`，其余续行归入 `Exception`。

#### `Parser.Pattern`

- 类型：`string`
- 当 `Mode` 为 `regex` 时使用
- 必需命名分组：`timestamp`、`level`、`message`
- 可选命名分组：`logger`、`exception`

#### `Parser.TimestampFormat`

- 类型：`string`
- 可选
- 示例：`yyyy/MM/dd HH:mm:ss`

## JSON 示例

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

## 宿主级配置

独立宿主同样支持常规 ASP.NET Core 宿主配置。

### `Urls`

- 类型：`string`
- 示例：`http://127.0.0.1:5088`
- 用途：配置 `src/JLogDashboard.Host` 的监听地址和端口

等价环境变量：

```bash
ASPNETCORE_URLS=http://0.0.0.0:5099
```

当不同环境需要使用不同端口或不同绑定地址时，应通过 `Urls` 或 `ASPNETCORE_URLS` 明确指定。

## 环境变量示例

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

## 常见自定义示例

### 修改 Dashboard 路径

```json
{
  "JLogDashboard": {
    "RoutePrefix": "/ops-logs"
  }
}
```

效果：

```text
http://your-host/ops-logs
```

### 修改独立宿主端口

```json
{
  "Urls": "http://127.0.0.1:5099"
}
```

效果：

```text
http://127.0.0.1:5099/jlog
```

## 校验规则

启动时，JLogDashboard 会把配置问题区分为 warning 和 fatal error。

fatal error 会让 Dashboard 请求返回受控 `503`，但不会导致宿主进程启动失败。warning 不会阻断 Dashboard，只是为了尽早提示高风险配置。

fatal error 包括：

- 未配置任何项目；
- 页大小限制无效；
- `MaxFileBytes` 非法；
- 启用 Basic Auth 但未提供凭据；
- `PasswordSha256` 格式非法；
- 项目名称重复。

warning 可能包括：

- 仍在使用默认密码 `change-me`；
- 仍使用明文 Basic Auth 密码；
- Basic Auth 已关闭；
- `Provider` 使用了非内置值；
- `Parser.Mode` 使用了非支持值；
- 自定义解析器的分隔符、正则表达式或框架 layout/template 为空；
- 启动时日志目录不存在；
- `FileSearchPattern` 为空；
- 使用了非内置界面文化。
