# Releasing JLogDashboard

This document describes the current release process for JLogDashboard.

## Overview

JLogDashboard publishes NuGet packages through GitHub Actions and NuGet Trusted Publishing.

The repository workflow file is:

```text
.github/workflows/build.yml
```

The release trigger is:

- push a tag matching `v*`; or
- manually run the workflow with `publish=true`.

## Trusted Publishing Configuration

Current publishing context:

- Package ID: `JLogDashboard`
- Package owner: `pp_nuget`
- GitHub repository owner: `ppengit`
- GitHub repository: `JLogDashboard`
- Workflow: `build.yml`
- Environment: `production`

Important:

- `NuGet/login@v1` must use the Trusted Publishing policy creator account in the `user` input.
- For this repository, the correct value is `penjay`.
- Do not replace it with the NuGet package owner (`pp_nuget`) or the GitHub repository owner (`ppengit`).

## Release Checklist

Before creating a release tag:

1. Confirm the working tree is clean.
2. Run:

```bash
dotnet build JLogDashboard.sln --configuration Release
dotnet test JLogDashboard.sln --configuration Release
dotnet pack src/JLogDashboard/JLogDashboard.csproj --configuration Release --no-build --output artifacts/packages
```

3. Update:

- `src/JLogDashboard/JLogDashboard.csproj`
- `CHANGELOG.md`
- any user-facing documentation affected by the release

4. Commit changes to `main`.
5. Create and push the version tag.

Example:

```bash
git tag v0.1.3
git push origin main
git push origin v0.1.3
```

## Verification

After pushing the tag, verify:

1. The GitHub Actions tag run succeeds, including the `publish` job.
2. The NuGet package page is available.
3. The NuGet flat container index includes the version.

Useful URLs:

- GitHub Actions: `https://github.com/ppengit/JLogDashboard/actions/workflows/build.yml`
- NuGet package page: `https://www.nuget.org/packages/JLogDashboard`
- NuGet flat container index: `https://api.nuget.org/v3-flatcontainer/jlogdashboard/index.json`

## Notes

- The workflow sets `FORCE_JAVASCRIPT_ACTIONS_TO_NODE24=true` to reduce runtime deprecation noise on GitHub-hosted runners.
- Keep release notes user-facing. Internal CI repairs should only be mentioned when they affected consumers or blocked delivery.
