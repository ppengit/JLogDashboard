# 安全策略

JLogDashboard 用于查看日志文件，日志中可能包含异常堆栈、机器路径、请求参数或业务标识。请按内部工具对待，不要在没有访问控制的情况下暴露到公网。

## 支持版本

当前项目处于 `0.x` 初始阶段，只维护最新版本。

## 报告安全问题

请不要在公开 Issue 中披露安全漏洞细节。可以通过以下方式联系维护者：

- 邮箱：peng.it@qq.com
- GitHub 仓库：https://github.com/ppengit/JLogDashboard

报告时建议包含：

- 受影响版本或 commit；
- 复现步骤；
- 影响范围；
- 临时缓解建议（如有）。

## 部署建议

- 生产环境开启 `BasicAuth.Enabled`。
- 优先使用 `PasswordSha256`，避免提交明文密码。
- 使用 HTTPS、内网、VPN 或可信反向代理暴露 Dashboard。
- nginx 后面部署时，请转发 `X-Forwarded-For`，否则失败锁定只能识别代理 IP。
- 不要把日志目录配置到包含敏感配置文件的上级目录。
