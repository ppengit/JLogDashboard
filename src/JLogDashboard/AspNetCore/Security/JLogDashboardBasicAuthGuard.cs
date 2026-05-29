using System.Collections.Concurrent;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using JLogDashboard.Configuration;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace JLogDashboard.AspNetCore.Security;

internal sealed class JLogDashboardBasicAuthGuard
{
    private readonly ConcurrentDictionary<string, BasicAuthState> _states = new(StringComparer.Ordinal);
    private readonly TimeProvider _timeProvider;

    public JLogDashboardBasicAuthGuard()
        : this(TimeProvider.System)
    {
    }

    public JLogDashboardBasicAuthGuard(TimeProvider timeProvider)
    {
        _timeProvider = timeProvider;
    }

    public BasicAuthDecision Authenticate(HttpContext context, JLogDashboardOptions options)
    {
        if (!options.BasicAuth.Enabled)
        {
            return BasicAuthDecision.Allow;
        }

        var clientKey = GetClientKey(context);
        var state = _states.GetOrAdd(clientKey, _ => new BasicAuthState());
        var now = _timeProvider.GetUtcNow();

        lock (state)
        {
            if (state.LockedUntil is not null && state.LockedUntil > now)
            {
                return BasicAuthDecision.LockedOut(state.LockedUntil.Value - now);
            }

            var credentialsState = ReadCredentials(context.Request.Headers.Authorization, out var username, out var password);
            if (credentialsState == BasicAuthCredentialsState.Missing)
            {
                // Missing credentials should challenge the client, but not poison the lockout counter.
                return BasicAuthDecision.Challenge;
            }

            if (credentialsState == BasicAuthCredentialsState.Invalid
                || !CredentialsMatch(options.BasicAuth, username, password))
            {
                return Reject(state, now, options.BasicAuth);
            }

            state.FailedAttempts = 0;
            state.LockedUntil = null;
            return BasicAuthDecision.Allow;
        }
    }

    private static BasicAuthDecision Reject(BasicAuthState state, DateTimeOffset now, BasicAuthOptions options)
    {
        state.FailedAttempts++;
        if (state.FailedAttempts >= options.MaxFailedAttempts)
        {
            state.LockedUntil = now.AddSeconds(options.LockoutSeconds);
            return BasicAuthDecision.LockedOut(TimeSpan.FromSeconds(options.LockoutSeconds));
        }

        return BasicAuthDecision.Challenge;
    }

    private static BasicAuthCredentialsState ReadCredentials(
        StringValues authorization,
        out string username,
        out string password)
    {
        username = string.Empty;
        password = string.Empty;

        var header = authorization.ToString();
        if (string.IsNullOrWhiteSpace(header))
        {
            return BasicAuthCredentialsState.Missing;
        }

        if (!header.StartsWith("Basic ", StringComparison.OrdinalIgnoreCase))
        {
            return BasicAuthCredentialsState.Invalid;
        }

        string decoded;
        try
        {
            var token = header["Basic ".Length..].Trim();
            decoded = Encoding.UTF8.GetString(Convert.FromBase64String(token));
        }
        catch (FormatException)
        {
            return BasicAuthCredentialsState.Invalid;
        }

        var separator = decoded.IndexOf(':', StringComparison.Ordinal);
        if (separator <= 0)
        {
            return BasicAuthCredentialsState.Invalid;
        }

        username = decoded[..separator];
        password = decoded[(separator + 1)..];
        return BasicAuthCredentialsState.Valid;
    }

    private static bool CredentialsMatch(BasicAuthOptions options, string username, string password)
    {
        if (!FixedTimeEquals(username, options.Username))
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(options.PasswordSha256))
        {
            var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(password))).ToLowerInvariant();
            return FixedTimeEquals(hash, options.PasswordSha256.Trim().ToLowerInvariant());
        }

        return FixedTimeEquals(password, options.Password);
    }

    private static bool FixedTimeEquals(string left, string right)
    {
        var leftBytes = Encoding.UTF8.GetBytes(left);
        var rightBytes = Encoding.UTF8.GetBytes(right);
        return CryptographicOperations.FixedTimeEquals(leftBytes, rightBytes);
    }

    private static string GetClientKey(HttpContext context)
    {
        // Behind a reverse proxy, X-Forwarded-For is the stable identifier for basic rate limiting.
        if (context.Request.Headers.TryGetValue("X-Forwarded-For", out var forwardedFor))
        {
            var first = forwardedFor.ToString()
                .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
                .FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(first))
            {
                return first;
            }
        }

        return context.Connection.RemoteIpAddress?.ToString() ?? IPAddress.Loopback.ToString();
    }

    private enum BasicAuthCredentialsState
    {
        Missing,
        Invalid,
        Valid
    }
}
