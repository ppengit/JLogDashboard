# Contributing

Thanks for your interest in JLogDashboard.

The project aims to stay lightweight, practical, and easy to deploy. Changes should improve real operational usability without turning the package into a large observability platform.

## Principles

- Lightweight first: do not introduce databases, background indexing services, or frontend build chains unless the gain is clear and the maintenance cost is justified.
- Practical over theoretical: optimize for common .NET file-log scenarios and straightforward operations workflows.
- Secure by default: treat the Dashboard as an internal operational surface that may expose stack traces, paths, and business identifiers.
- Compatibility matters: public API changes should consider NuGet consumers and upgrade friction.

## Development Setup

```bash
dotnet restore
dotnet build JLogDashboard.sln
dotnet test JLogDashboard.sln
```

## Pull Request Expectations

- Keep changes focused and explain the operational value of the change.
- Add or update tests for new behavior and bug fixes.
- Preserve the no-frontend-build-chain approach unless there is a compelling architectural reason to change it.
- Update documentation when configuration, deployment, security posture, or public behavior changes.

## Testing Expectations

Before submitting a pull request, verify:

- `dotnet build JLogDashboard.sln --configuration Release`
- `dotnet test JLogDashboard.sln --configuration Release`

When relevant, also verify:

- Dashboard UI still renders without a separate frontend toolchain.
- Basic Auth behavior still covers anonymous access, valid credentials, and lockout behavior.
- Log parsing changes cover at least one real-world sample format.

## Versioning And Release Notes

- Use clear, user-facing release notes.
- Prefer release note entries that describe functional or operational impact, not internal housekeeping unless it affected consumers.
- If a version was only an internal recovery step and never became the intended public release, avoid over-emphasizing it in public-facing summaries.

## Commit Message Guidance

Conventional Commits are recommended:

```text
feat(parser): support more serilog timestamp formats
fix(auth): avoid locking out anonymous dashboard probes
docs(readme): clarify standalone host deployment
chore(ci): update actions runtime
```

## Security

If you find a security issue, do not open a public issue with exploit details. Follow [SECURITY.md](./SECURITY.md).

## Release Process

Maintainers should refer to [docs/maintainers/releasing.md](./docs/maintainers/releasing.md) for the current GitHub Actions and NuGet Trusted Publishing workflow.
