namespace JLogDashboard.ReverseProxy;

/// <summary>
/// Input model for nginx reverse proxy configuration generation.
/// </summary>
public sealed class NginxConfigRequest
{
    /// <summary>The public hostname, such as <c>logs.example.com</c>.</summary>
    public string ServerName { get; set; } = "_";

    /// <summary>The upstream Dashboard origin, such as <c>http://127.0.0.1:5088</c>.</summary>
    public string UpstreamUrl { get; set; } = "http://127.0.0.1:5088";

    /// <summary>The public path prefix, such as <c>/jlog</c>.</summary>
    public string BasePath { get; set; } = "/jlog";
}
