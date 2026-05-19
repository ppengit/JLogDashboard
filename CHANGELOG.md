# Changelog

All notable changes to this project are documented in this file.

## 0.1.4

- Reworked the README into a cleaner public project homepage for GitHub and NuGet readers.
- Added dedicated deployment, configuration, and FAQ documentation.
- Clarified standalone host port, bind-address, and route-prefix customization guidance.
- Updated maintainer release documentation to match the current NuGet Trusted Publishing context.

## 0.1.3

- Refined the public documentation structure for GitHub and NuGet presentation.
- Improved repository collaboration materials, including issue and pull request templates.
- Clarified maintainer release guidance for GitHub Actions and NuGet Trusted Publishing.

## 0.1.2

- Fixed NuGet Trusted Publishing configuration and successfully published the package.
- Updated Basic Auth lockout behavior so anonymous requests return `401` without consuming lockout attempts.

## 0.1.0

- Initial public release of JLogDashboard.
- Added file-log parsing support for NLog, log4net, and Serilog.
- Added multi-project configuration, filtering, large-file tail reading, and exception stack grouping.
- Added ASP.NET Core Dashboard integration, standalone host, Basic Auth, and nginx configuration generation.
