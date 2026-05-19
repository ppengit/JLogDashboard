using JLogDashboard.Configuration;
using JLogDashboard.AspNetCore.Security;
using JLogDashboard.Localization;
using JLogDashboard.Parsing;
using JLogDashboard.Querying;
using JLogDashboard.ReverseProxy;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace JLogDashboard.AspNetCore;

/// <summary>
/// Provides dependency injection helpers for JLogDashboard.
/// </summary>
public static class JLogDashboardServiceCollectionExtensions
{
    /// <summary>
    /// Registers JLogDashboard services with code-based options.
    /// </summary>
    public static IServiceCollection AddJLogDashboard(
        this IServiceCollection services,
        Action<JLogDashboardOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        var options = new JLogDashboardOptions();
        configure(options);
        return services.AddJLogDashboard(options);
    }

    /// <summary>
    /// Registers JLogDashboard services from the <c>JLogDashboard</c> configuration section.
    /// </summary>
    public static IServiceCollection AddJLogDashboard(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var options = new JLogDashboardOptions();
        configuration.GetSection("JLogDashboard").Bind(options);
        return services.AddJLogDashboard(options);
    }

    /// <summary>
    /// Registers JLogDashboard services with an already constructed options instance.
    /// </summary>
    public static IServiceCollection AddJLogDashboard(
        this IServiceCollection services,
        JLogDashboardOptions options)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(options);

        var analysis = options.Analyze();

        services.AddSingleton(options);
        services.AddSingleton(analysis);
        services.AddSingleton(new JLogDashboardConfigurationState(analysis));
        services.AddSingleton(LogParser.CreateDefault());
        services.AddSingleton<ILogQueryService, FileLogQueryService>();
        services.AddSingleton(DashboardLocalizer.CreateDefault());
        services.AddSingleton<NginxConfigGenerator>();
        services.AddSingleton<JLogDashboardBasicAuthGuard>();
        services.AddHostedService<JLogDashboardStartupDiagnosticsHostedService>();
        services.AddScoped<JLogDashboardConfigurationEndpointFilter>();
        services.AddScoped<JLogDashboardBasicAuthEndpointFilter>();
        services.AddScoped<JLogDashboardFaultIsolationEndpointFilter>();
        return services;
    }
}
