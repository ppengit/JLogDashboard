# Security Policy

JLogDashboard is an operational tool for viewing application log files. Those logs may contain stack traces, machine paths, request parameters, identifiers, or other sensitive information. Treat the dashboard as an internal operations surface, not as a public-facing product endpoint.

## Supported Versions

The project is currently in the `0.x` phase. Security fixes are provided for the latest published version only.

## Reporting A Vulnerability

Please do not disclose vulnerability details in a public GitHub issue.

Report security concerns through one of the following channels:

- Email: `peng.it@qq.com`
- GitHub repository: https://github.com/ppengit/JLogDashboard

When reporting, include:

- affected version or commit;
- reproduction steps;
- impact assessment;
- any suggested mitigation, if available.

## Deployment Recommendations

- Enable `BasicAuth.Enabled` in non-local environments.
- Prefer `PasswordSha256` over plain-text passwords.
- Expose the dashboard through HTTPS, VPN, or a trusted internal reverse proxy.
- Forward `X-Forwarded-For` when deployed behind nginx or another proxy so lockout behavior applies to the actual client.
- Restrict configured log directories to the intended log root instead of broad parent folders.
- Treat log access permissions on the host machine as part of the dashboard security boundary.
