# 贡献指南

感谢关注 JLogDashboard。这个项目的目标是保持轻量、实用、易部署，优先解决「快速查看服务器上的 .NET 文件日志」这个核心问题。

## 本地开发

```bash
dotnet restore
dotnet build JLogDashboard.sln
dotnet test JLogDashboard.sln
```

## 提交前检查

- 新功能或行为变更需要补充测试。
- 修改日志解析逻辑时，请至少覆盖一种真实日志格式样例。
- 修改 Dashboard UI 时，请确认页面仍然不依赖前端构建工具。
- 修改认证逻辑时，请覆盖 401、成功认证和失败锁定场景。
- 运行 `dotnet test JLogDashboard.sln`，确保测试通过。

## 设计原则

- 轻量：不引入数据库、复杂索引服务或前端构建链，除非收益非常明确。
- 实用：优先支持常见日志格式、常见部署方式和常见排查路径。
- 安全：Dashboard 可能暴露异常堆栈、路径和业务信息，默认建议开启 Basic Auth 并放在 HTTPS 或内网后面。
- 兼容：公共 API 变更需要考虑 NuGet 用户升级成本。

## Commit Message

建议使用 Conventional Commits：

```text
feat(parser): support more serilog timestamp formats
fix(auth): avoid leaking credential validation details
docs(readme): add nginx deployment notes
```

## 发布

NuGet 发布通过 GitHub Actions Trusted Publishing 完成。普通 PR 不会发布包；创建 `v*` tag 或手动触发 workflow 并选择发布才会进入 `production` 环境。
