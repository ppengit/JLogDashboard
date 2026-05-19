# 常见问题

[English](../faq.md) | [简体中文](./faq.md)

## JLogDashboard 会把日志存进数据库吗？

不会。它直接从文件系统读取日志文件。

## 支持集中式日志索引吗？

不支持。JLogDashboard 的定位就是轻量，目标是直接查看文件日志，而不是构建完整的可观测性流水线。

## 支持哪些日志框架？

主要面向以下常见文本日志格式：

- NLog
- log4net
- Serilog

如果其他纯文本日志格式与已支持模式足够接近，也可以读取。

## 可以同时查看多个应用吗？

可以。只要在 `JLogDashboard:Projects` 下配置多个项目，每个项目指向不同的日志目录即可。

## 可以单独跑在一个端口上吗？

可以。仓库提供了 `src/JLogDashboard.Host` 用于独立宿主部署，Dashboard UI 里也带有 nginx 配置辅助。

## 怎么修改端口和访问路径？

通过 `Urls` 或 `ASPNETCORE_URLS` 修改监听地址和端口，通过 `JLogDashboard:RoutePrefix` 修改看板访问路径。

示例：

```json
{
  "Urls": "http://127.0.0.1:5099",
  "JLogDashboard": {
    "RoutePrefix": "/ops-logs"
  }
}
```

效果：

```text
http://127.0.0.1:5099/ops-logs
```

## 外网可访问时，只靠 Basic Auth 够吗？

它只能算轻量级访问门槛。若服务可被公网访问，仍然应配合 HTTPS、VPN 或可信反向代理，并优先使用哈希密码。

## 为什么匿名请求返回 `401`，却不会触发锁定？

这是有意设计。匿名探测应该收到质询，而真正消耗锁定次数的是重复提交错误凭据的请求。

## 为什么大日志文件里很旧的内容看不到？

因为 JLogDashboard 默认只读取 `MaxFileBytes` 指定的尾部窗口，避免大文件占用过多内存。

## 支持自定义 URL 路径吗？

支持。设置 `RoutePrefix` 即可，例如：

```json
{
  "JLogDashboard": {
    "RoutePrefix": "/ops-logs"
  }
}
```

## 需要单独的前端构建流程吗？

不需要。Dashboard UI 不依赖独立的前端构建链。
