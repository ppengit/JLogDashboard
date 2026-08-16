# Changelog

All notable changes to this project are documented in this file.

## 1.0.0

- Hardened dashboard fault isolation so the fault-isolation endpoint filter is the outermost guard, containing configuration, authentication, model-binding, and handler failures.
- Preserved correct 400 responses for malformed search requests instead of converting client errors into 503 responses.
- Hardened configuration analysis so null BasicAuth, parser, and project entries no longer throw during host startup; a null project entry is reported as a configuration error.
- Hardened the Basic Auth guard so a null BasicAuth configuration is treated as disabled instead of failing every Dashboard request.
- Hardened startup diagnostics so logging failures cannot prevent the host application from starting.

## 0.1.8

- Added project-level parser configuration for common NLog layout strings, log4net PatternLayout strings, Serilog outputTemplate strings, custom delimited logs, and regex-based plain-text log layouts.
- Hardened explicit template parser fallback so missing layout/template values do not throw during dashboard queries.
- Improved parsing for log headers whose message body is emitted as following continuation lines.
- Reworked the Dashboard UI into a full-width operational view with project switching, filtering, automatic initial loading, internal scrolling, refresh, and pagination controls.
- Added current-page duplicate log grouping with count tags and moved source file display under the timestamp.
- Removed nginx-specific configuration generation from the Dashboard UI and API surface. Route, port, log directories, authentication, and parser rules are now documented as host-level configuration.
- Updated English and Chinese documentation and example configuration to describe explicit parser modes and the embedded/standalone hosting model.

## 0.1.7

- Added configuration severity analysis so JLogDashboard can distinguish fatal misconfiguration from advisory operational warnings.
- Added startup diagnostics that log Dashboard configuration errors and warnings once during host startup.
- Added dashboard misconfiguration isolation so fatal configuration errors return controlled `503` responses without breaking unrelated host endpoints.
- Added a runnable embedded sample project under `samples/JLogDashboard.SampleWeb`.
- Updated English and Chinese README / deployment / configuration docs to document sample usage and configuration-hardening behavior.

## 0.1.6

- Extended dashboard fault isolation so failures during handler-level service resolution are also contained within the dashboard route.
- Added regression tests for HTML fallback, plain-text fallback, project-enumeration failure tolerance, and cancellation propagation.
- Tightened endpoint-filter ordering so dashboard fault isolation wraps the dashboard auth filter as the outermost guard.

## 0.1.5

- Added dashboard fault isolation so common dashboard runtime failures stay within the dashboard route instead of breaking unrelated host endpoints.
- Added best-effort log reading so unreadable log files are skipped and remaining results can still be returned.
- Documented host-safety expectations for dashboard runtime failures in the English and Chinese deployment guides and READMEs.

## 0.1.4

- Reworked the README into a cleaner public project homepage for GitHub and NuGet readers.
- Added dedicated deployment, configuration, and FAQ documentation.
- Added bilingual README and user-facing documentation, including language switch links for GitHub and NuGet readers.
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
