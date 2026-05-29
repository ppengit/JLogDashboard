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

    [Fact]
    public void ParseEntries_UsesDelimitedProjectParserConfiguration()
    {
        var parser = LogParser.CreateDefault();
        var entries = parser.Parse(
            new LogParseContext(
                "custom",
                "custom",
                "custom.log",
                new LogParserOptions
                {
                    Mode = "delimited",
                    Delimiter = "||",
                    TimestampIndex = 0,
                    LoggerIndex = 1,
                    LevelIndex = 2,
                    MessageIndex = 3,
                    TimestampFormat = "yyyy/MM/dd HH:mm:ss"
                }),
            new[]
            {
                "2026/05/29 08:30:01||Orders.Api||ERROR||failed order 1002"
            }).ToArray();

        var entry = Assert.Single(entries);
        Assert.Equal(LogLevel.Error, entry.Level);
        Assert.Equal("Orders.Api", entry.Logger);
        Assert.Equal("failed order 1002", entry.Message);
        Assert.Equal(new DateTimeOffset(2026, 5, 29, 8, 30, 1, TimeSpan.Zero), entry.Timestamp);
    }

    [Fact]
    public void ParseEntries_UsesFirstContinuationAsMessageWhenDelimitedMessageFieldIsMissing()
    {
        var parser = LogParser.CreateDefault();
        var entries = parser.Parse(
            new LogParseContext(
                "api.backend",
                "nlog",
                "app.log",
                new LogParserOptions
                {
                    Mode = "delimited",
                    Delimiter = "|",
                    TimestampIndex = 0,
                    LevelIndex = 2,
                    LoggerIndex = 3,
                    MessageIndex = 4
                }),
            new[]
            {
                "---------------------------------------",
                "2026-05-29 09:16:40.3914|0|ERROR|Api.Backend.Filters.ApiExceptionFilter",
                "Object reference not set to an instance of an object. System.NullReferenceException: Object reference not set to an instance of an object.",
                "   at HD.Application.AgencyArticleApp.GetArticleWithTags(String articleId) in D:\\Solution\\hd\\AgencyArticleApp.cs:line 155",
                "",
                "---------------------------------------"
            }).ToArray();

        var entry = Assert.Single(entries);
        Assert.Equal(LogLevel.Error, entry.Level);
        Assert.Equal("Api.Backend.Filters.ApiExceptionFilter", entry.Logger);
        Assert.Equal("Object reference not set to an instance of an object. System.NullReferenceException: Object reference not set to an instance of an object.", entry.Message);
        Assert.Contains("AgencyArticleApp.cs:line 155", entry.Exception);
        Assert.DoesNotContain("---------------------------------------", entry.Exception);
    }

    [Fact]
    public void ParseEntries_UsesNLogLayoutWithInlineMessage()
    {
        var parser = LogParser.CreateDefault();
        var entries = parser.Parse(
            new LogParseContext(
                "billing",
                "nlog",
                "billing.log",
                new LogParserOptions
                {
                    Mode = "nlog-layout",
                    Layout = "${longdate}|${level}|${logger}|${message}${exception:format=tostring}"
                }),
            new[]
            {
                "2026-05-29 08:30:01.1234|WARN|Billing.Worker|Retry invoice INV-9"
            }).ToArray();

        var entry = Assert.Single(entries);
        Assert.Equal(LogLevel.Warning, entry.Level);
        Assert.Equal("Billing.Worker", entry.Logger);
        Assert.Equal("Retry invoice INV-9", entry.Message);
        Assert.Equal(new DateTimeOffset(2026, 5, 29, 8, 30, 1, 123, TimeSpan.Zero), entry.Timestamp);
    }

    [Fact]
    public void ParseEntries_UsesNLogLayoutWithNewlineMessage()
    {
        var parser = LogParser.CreateDefault();
        var entries = parser.Parse(
            new LogParseContext(
                "api.backend",
                "nlog",
                "app.log",
                new LogParserOptions
                {
                    Mode = "nlog-layout",
                    Layout = "${longdate}|${event-properties:item=EventId}|${level}|${logger}${newline}${message}${exception:format=tostring}"
                }),
            new[]
            {
                "---------------------------------------",
                "2026-05-29 09:16:40.3914|0|ERROR|Api.Backend.Filters.ApiExceptionFilter",
                "Object reference not set to an instance of an object. System.NullReferenceException: Object reference not set to an instance of an object.",
                "   at HD.Application.AgencyArticleApp.GetArticleWithTags(String articleId) in D:\\Solution\\hd\\AgencyArticleApp.cs:line 155",
                "|url: https://api.example.com/api/m/AgencyArticle/GetArticleWithTags|action: GetArticleWithTags|Api.Backend.Filters.ApiExceptionFilter.OnException",
                "",
                "---------------------------------------"
            }).ToArray();

        var entry = Assert.Single(entries);
        Assert.Equal(LogLevel.Error, entry.Level);
        Assert.Equal("Api.Backend.Filters.ApiExceptionFilter", entry.Logger);
        Assert.Equal("Object reference not set to an instance of an object. System.NullReferenceException: Object reference not set to an instance of an object.", entry.Message);
        Assert.Contains("AgencyArticleApp.cs:line 155", entry.Exception);
        Assert.Contains("api.example.com", entry.Exception);
        Assert.DoesNotContain("---------------------------------------", entry.Exception);
    }

    [Fact]
    public void ParseEntries_UsesLog4NetPatternLayoutWithExceptionContinuation()
    {
        var parser = LogParser.CreateDefault();
        var entries = parser.Parse(
            new LogParseContext(
                "legacy",
                "log4net",
                "legacy.log",
                new LogParserOptions
                {
                    Mode = "log4net-pattern",
                    Layout = "%date{yyyy-MM-dd HH:mm:ss,fff} [%thread] %-5level %logger - %message%newline%exception"
                }),
            new[]
            {
                "2026-05-29 08:30:01,456 [12] ERROR Legacy.Service - Failed to sync customer",
                "System.InvalidOperationException: remote service unavailable",
                "   at Legacy.Service.Sync() in D:\\Legacy\\Service.cs:line 42"
            }).ToArray();

        var entry = Assert.Single(entries);
        Assert.Equal(LogLevel.Error, entry.Level);
        Assert.Equal("Legacy.Service", entry.Logger);
        Assert.Equal("Failed to sync customer", entry.Message);
        Assert.Contains("InvalidOperationException", entry.Exception);
        Assert.Equal(new DateTimeOffset(2026, 5, 29, 8, 30, 1, 456, TimeSpan.Zero), entry.Timestamp);
    }

    [Fact]
    public void ParseEntries_UsesSerilogOutputTemplateWithSourceContextAndExceptionContinuation()
    {
        var parser = LogParser.CreateDefault();
        var entries = parser.Parse(
            new LogParseContext(
                "orders",
                "serilog",
                "orders.log",
                new LogParserOptions
                {
                    Mode = "serilog-template",
                    Layout = "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {SourceContext} {Message:lj}{NewLine}{Exception}"
                }),
            new[]
            {
                "2026-05-29 08:30:01.789 [ERR] Orders.Api Checkout failed for order 1002",
                "System.InvalidOperationException: stock not enough",
                "   at Orders.Api.Checkout.Submit() in D:\\Orders\\Checkout.cs:line 42"
            }).ToArray();

        var entry = Assert.Single(entries);
        Assert.Equal(LogLevel.Error, entry.Level);
        Assert.Equal("Orders.Api", entry.Logger);
        Assert.Equal("Checkout failed for order 1002", entry.Message);
        Assert.Contains("InvalidOperationException", entry.Exception);
        Assert.Equal(new DateTimeOffset(2026, 5, 29, 8, 30, 1, 789, TimeSpan.Zero), entry.Timestamp);
    }

    [Theory]
    [InlineData("log4net-pattern", "")]
    [InlineData("log4net-pattern", null)]
    [InlineData("serilog-template", "")]
    [InlineData("serilog-template", null)]
    public void ParseEntries_FallsBackWhenExplicitTemplateLayoutIsEmpty(string mode, string? layout)
    {
        var parser = LogParser.CreateDefault();
        var entries = parser.Parse(
            new LogParseContext(
                "orders",
                "serilog",
                "orders.log",
                new LogParserOptions { Mode = mode, Layout = layout! }),
            new[]
            {
                "[2026-05-19 02:15:00 INF] Payment accepted for order 1001"
            }).ToArray();

        var entry = Assert.Single(entries);
        Assert.Equal(LogLevel.Information, entry.Level);
        Assert.Equal("Payment accepted for order 1001", entry.Message);
    }

    [Fact]
    public void ParseEntries_UsesRegexProjectParserConfiguration()
    {
        var parser = LogParser.CreateDefault();
        var entries = parser.Parse(
            new LogParseContext(
                "custom",
                "custom",
                "custom.log",
                new LogParserOptions
                {
                    Mode = "regex",
                    Pattern = @"^(?<level>\w+) (?<timestamp>\d{14}) (?<logger>[^:]+): (?<message>.*)$",
                    TimestampFormat = "yyyyMMddHHmmss"
                }),
            new[]
            {
                "WARN 20260529083204 Billing.Worker: retry invoice INV-9"
            }).ToArray();

        var entry = Assert.Single(entries);
        Assert.Equal(LogLevel.Warning, entry.Level);
        Assert.Equal("Billing.Worker", entry.Logger);
        Assert.Equal("retry invoice INV-9", entry.Message);
        Assert.Equal(new DateTimeOffset(2026, 5, 29, 8, 32, 4, TimeSpan.Zero), entry.Timestamp);
    }
}
