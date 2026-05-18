namespace JLogDashboard.AspNetCore.Security;

internal sealed class BasicAuthState
{
    public int FailedAttempts { get; set; }

    public DateTimeOffset? LockedUntil { get; set; }
}
