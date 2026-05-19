namespace JLogDashboard.Configuration;

/// <summary>
/// Configures the built-in Basic Auth gate and lightweight failed-login lockout.
/// </summary>
public sealed class BasicAuthOptions
{
    /// <summary>Whether Basic Auth is required for Dashboard pages and APIs.</summary>
    public bool Enabled { get; set; }

    /// <summary>The allowed Basic Auth username.</summary>
    public string Username { get; set; } = string.Empty;

    // Plain-text passwords are convenient for local demos. Prefer PasswordSha256 in shared environments.
    /// <summary>The allowed plain-text password. Prefer <see cref="PasswordSha256"/> outside local demos.</summary>
    public string Password { get; set; } = string.Empty;

    /// <summary>The SHA-256 hash of the allowed password, encoded as lowercase or uppercase hexadecimal.</summary>
    public string PasswordSha256 { get; set; } = string.Empty;

    /// <summary>The Basic Auth realm shown by browsers in the login prompt.</summary>
    public string Realm { get; set; } = "JLogDashboard";

    /// <summary>The number of failed attempts allowed before the client is locked out.</summary>
    public int MaxFailedAttempts { get; set; } = 5;

    /// <summary>The lockout duration in seconds after too many failed attempts.</summary>
    public int LockoutSeconds { get; set; } = 300;

    /// <summary>Returns whether <see cref="PasswordSha256"/> looks like a full SHA-256 hex string.</summary>
    public bool IsPasswordSha256Hex()
    {
        if (string.IsNullOrWhiteSpace(PasswordSha256) || PasswordSha256.Length != 64)
        {
            return false;
        }

        return PasswordSha256.All(static character => Uri.IsHexDigit(character));
    }
}
