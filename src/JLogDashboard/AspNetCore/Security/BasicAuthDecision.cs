namespace JLogDashboard.AspNetCore.Security;

internal sealed record BasicAuthDecision(BasicAuthDecisionKind Kind, TimeSpan? RetryAfter = null)
{
    public static BasicAuthDecision Allow { get; } = new(BasicAuthDecisionKind.Allow);

    public static BasicAuthDecision Challenge { get; } = new(BasicAuthDecisionKind.Challenge);

    public static BasicAuthDecision LockedOut(TimeSpan retryAfter) => new(BasicAuthDecisionKind.LockedOut, retryAfter);
}

internal enum BasicAuthDecisionKind
{
    Allow,
    Challenge,
    LockedOut
}
