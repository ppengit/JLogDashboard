using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Hosting;

namespace JLogDashboard.Tests;

public sealed class EndpointFilterOrderTests
{
    // Locks in the ASP.NET Core minimal-API filter ordering contract that the Dashboard
    // fault-isolation guard relies on: filters run in registration order, so the first
    // registered filter is the outermost wrapper.
    [Fact]
    public async Task EndpointFilters_RunInRegistrationOrder()
    {
        var order = new List<string>();
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();

        var app = builder.Build();
        app.MapGet("/probe", () => Results.Ok("ok"))
            .AddEndpointFilter(async (context, next) =>
            {
                order.Add("first-registered");
                return await next(context);
            })
            .AddEndpointFilter(async (context, next) =>
            {
                order.Add("second-registered");
                return await next(context);
            });

        await app.StartAsync();
        try
        {
            var client = app.GetTestClient();
            var response = await client.GetAsync("/probe");
            response.EnsureSuccessStatusCode();
        }
        finally
        {
            await app.StopAsync();
        }

        Assert.Equal(new[] { "first-registered", "second-registered" }, order);
    }
}
