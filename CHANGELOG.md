# 更新日志

## 0.1.1

- 修复 NuGet Trusted Publishing 登录用户名，改为策略创建者 `ppengit`。
- 调整 Basic Auth 锁定策略，匿名访问只返回 `401`，错误凭据才累计失败次数。

## 0.1.0

- 初始化 JLogDashboard 项目结构。
- 添加 NLog、log4net、Serilog 文件日志解析。
- 添加多项目日志目录配置、筛选查询和大文件尾部读取。
- 添加 ASP.NET Core Dashboard、查询 API、项目 API 和 nginx 配置生成 API。
- 添加 Basic Auth 与失败次数锁定。
- 添加独立 Host、NuGet 元数据和 GitHub Actions Trusted Publishing 工作流。
