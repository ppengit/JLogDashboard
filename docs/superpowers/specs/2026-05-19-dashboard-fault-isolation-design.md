# Dashboard Fault Isolation Design

## Goal

Ensure JLogDashboard failures stay inside the dashboard boundary and do not break the host application's normal business endpoints or startup path for common runtime faults.

## Scope

This design covers non-fatal runtime faults inside JLogDashboard, including:

- log directory enumeration failures;
- single log file read failures;
- dashboard endpoint execution failures;
- configuration issues that should degrade dashboard behavior without crashing the host.

This design does not claim recovery from process-fatal conditions such as `OutOfMemoryException`, process termination, or host-level infrastructure failures.

## Desired Behavior

### Host safety

- Mapping JLogDashboard must not introduce failure behavior into unrelated host endpoints.
- If a dashboard request fails, the failure must be contained to the dashboard route.
- The host application must remain able to serve non-dashboard endpoints during dashboard faults.

### Dashboard safety

- Dashboard HTML and API endpoints should return controlled error responses instead of unhandled exceptions.
- Log search should use best-effort behavior: one broken project or file must not fail the entire search request.
- Faults should be logged through the host logger so operators can diagnose the issue.

## Recommended Design

### Option A: Endpoint-level isolation plus best-effort file reading

Add a dedicated dashboard endpoint filter that:

- wraps dashboard endpoint execution in `try/catch`;
- logs the exception;
- returns a controlled response based on request type:
  - HTML endpoint: friendly dashboard error page with `503`;
  - JSON API: structured JSON error payload with `503`;
  - text endpoint: plain-text error response with `503`.

Also update `FileLogQueryService` to isolate per-project and per-file failures:

- if one project directory enumeration fails, skip that project and continue;
- if one file read fails, skip that file and continue;
- keep successful results from remaining files;
- only treat explicit request cancellation as a real throw-through case.

This is the recommended option because it keeps the existing public API small, preserves current integration style, and directly addresses the real blast radius.

### Option B: Global middleware

Introduce a middleware dedicated to the dashboard route prefix and catch all dashboard exceptions there.

This would also work, but it is less aligned with the current package shape because the package currently exposes endpoint mapping extensions rather than a full middleware pipeline component.

### Option C: Fail closed at startup

If configuration is invalid, disable dashboard endpoint mapping entirely.

This reduces some runtime risk but does not solve runtime file-system faults and is more invasive for current consumers. It is better treated as a future enhancement, not the first isolation layer.

## Recommendation

Implement Option A now.

## Affected Files

- `src/JLogDashboard/AspNetCore/JLogDashboardEndpointRouteBuilderExtensions.cs`
  - attach a new fault-isolation filter to the dashboard route group
- `src/JLogDashboard/AspNetCore/`
  - add a dedicated endpoint filter and response helper types if needed
- `src/JLogDashboard/Querying/FileLogQueryService.cs`
  - catch per-project and per-file exceptions, continue best-effort search
- `tests/JLogDashboard.Tests/AspNetCoreDashboardTests.cs`
  - verify dashboard failures do not break business endpoints
  - verify dashboard APIs return controlled responses on query failures
- `tests/JLogDashboard.Tests/LogQueryServiceTests.cs`
  - verify partial file failures do not fail the whole search
- `README.md`
- `README.zh-CN.md`
- `docs/deployment.md`
- `docs/zh-CN/deployment.md`
  - briefly document fault-isolation expectations

## Error Response Shape

For JSON endpoints, return a small structured payload:

```json
{
  "error": "DashboardUnavailable",
  "message": "JLogDashboard request failed."
}
```

Do not include stack traces or sensitive file-system details in the response body.

## Logging

- Log caught exceptions with enough context to identify endpoint purpose.
- Do not log Basic Auth credential contents or raw request secrets.

## Test Strategy

1. Add a failing dashboard dependency in tests and prove:
   - `/ops-logs/api/search` returns controlled `503`;
   - a normal business endpoint such as `/health` still returns `200`.
2. Add a log query service test where one file or project fails but another succeeds, and confirm partial results are still returned.
3. Verify cancellation still throws normally instead of being swallowed as a generic runtime fault.

## Success Criteria

- Dashboard runtime exceptions no longer escape as unhandled exceptions to the host pipeline.
- Normal host endpoints remain available during dashboard faults.
- Log search degrades gracefully when some log sources are unreadable.
- Existing functional tests still pass, and new isolation tests pass.
