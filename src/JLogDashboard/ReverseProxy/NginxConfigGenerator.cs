using System.Text;

namespace JLogDashboard.ReverseProxy;

/// <summary>
/// Generates copy-ready nginx reverse proxy configuration for the Dashboard.
/// </summary>
public sealed class NginxConfigGenerator
{
    /// <summary>Generates an nginx server block from the supplied request.</summary>
    public string Generate(NginxConfigRequest request)
    {
        var serverName = string.IsNullOrWhiteSpace(request.ServerName) ? "_" : request.ServerName.Trim();
        var upstream = string.IsNullOrWhiteSpace(request.UpstreamUrl) ? "http://127.0.0.1:5088" : request.UpstreamUrl.Trim().TrimEnd('/');
        var basePath = NormalizeBasePath(request.BasePath);

        var builder = new StringBuilder();
        builder.AppendLine("server {");
        builder.AppendLine("    listen 80;");
        builder.AppendLine($"    server_name {serverName};");
        builder.AppendLine();
        builder.AppendLine($"    location {basePath}/ {{");
        builder.AppendLine($"        proxy_pass {upstream};");
        builder.AppendLine("        proxy_http_version 1.1;");
        builder.AppendLine("        proxy_set_header Host $host;");
        builder.AppendLine("        proxy_set_header X-Real-IP $remote_addr;");
        builder.AppendLine("        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;");
        builder.AppendLine("        proxy_set_header X-Forwarded-Proto $scheme;");
        builder.AppendLine("        proxy_set_header Upgrade $http_upgrade;");
        builder.AppendLine("        proxy_set_header Connection \"upgrade\";");
        builder.AppendLine("    }");
        builder.AppendLine("}");
        return builder.ToString();
    }

    private static string NormalizeBasePath(string? basePath)
    {
        if (string.IsNullOrWhiteSpace(basePath) || basePath == "/")
        {
            return string.Empty;
        }

        return "/" + basePath.Trim().Trim('/');
    }
}
