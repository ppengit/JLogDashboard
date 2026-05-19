using JLogDashboard.AspNetCore;
using JLogDashboard.Configuration;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRouting();
builder.Services.AddJLogDashboard(builder.Configuration);

var app = builder.Build();

var dashboardOptions = app.Services.GetRequiredService<JLogDashboardOptions>();

app.MapJLogDashboard();
if (!string.IsNullOrWhiteSpace(dashboardOptions.RoutePrefix) && dashboardOptions.RoutePrefix != "/")
{
    app.MapGet("/", () => Results.Redirect(dashboardOptions.RoutePrefix));
}

app.Run();
