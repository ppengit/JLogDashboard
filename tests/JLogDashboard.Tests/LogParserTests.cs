using JLogDashboard.Parsing;
using JLogDashboard.Querying;

namespace JLogDashboard.Tests;

public sealed class LogParserTests
{
    [Fact]
    public void ParseEntries_GroupsSerilogExceptionContinuation()
    {
        var parser = LogParser.CreateDefault();
        var entries = parser.Parse(
            new LogParseContext("orders", "serilog", "orders.log"),
            new[]
            {
                "[2026-05-19 02:15:00 INF] Payment accepted for order 1001",
                "[2026-05-19 02:16:00 ERR] Checkout failed for order 1002",
                "System.InvalidOperationException: stock not enough",
                "   at Demo.Checkout.Submit() in Checkout.cs:line 42"
            }).ToArray();

        Assert.Equal(2, entries.Length);
        Assert.Equal(LogLevel.Information, entries[0].Level);
        Assert.Equal("Payment accepted for order 1001", entries[0].Message);
        Assert.Equal(LogLevel.Error, entries[1].Level);
        Assert.Contains("Checkout failed", entries[1].Message);
        Assert.Contains("InvalidOperationException", entries[1].Exception);
        Assert.Contains("Checkout.cs:line 42", entries[1].Exception);
    }

    [Fact]
    public void ParseEntries_DetectsNLogPipeLayout()
    {
        var parser = LogParser.CreateDefault();
        var entries = parser.Parse(
            new LogParseContext("billing", "nlog", "billing.log"),
            new[]
            {
                "2026-05-19 02:17:03.2451|WARN|Billing.Worker|Retry invoice INV-9"
            }).ToArray();

        var entry = Assert.Single(entries);
        Assert.Equal("billing", entry.Project);
        Assert.Equal(LogLevel.Warning, entry.Level);
        Assert.Equal("Billing.Worker", entry.Logger);
        Assert.Equal("Retry invoice INV-9", entry.Message);
        Assert.Equal(new DateTimeOffset(2026, 5, 19, 2, 17, 3, 245, TimeSpan.Zero), entry.Timestamp);
    }

    [Fact]
    public void ParseEntries_DetectsLog4NetPattern()
    {
        var parser = LogParser.CreateDefault();
        var entries = parser.Parse(
            new LogParseContext("legacy", "log4net", "legacy.log"),
            new[]
            {
                "2026-05-19 02:18:04,123 [12] ERROR Legacy.Service - Failed to sync customer"
            }).ToArray();

        var entry = Assert.Single(entries);
        Assert.Equal(LogLevel.Error, entry.Level);
        Assert.Equal("Legacy.Service", entry.Logger);
        Assert.Equal("Failed to sync customer", entry.Message);
    }
}
