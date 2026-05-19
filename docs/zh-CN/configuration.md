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
        "Recursive": false
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

启动时，宿主可能会输出以下配置告警：

- 未配置任何项目；
- 页大小限制无效；
- `MaxFileBytes` 非法；
- 启用 Basic Auth 但未提供凭据；
- 仍在使用默认密码 `change-me`；
- 项目名称重复。
