namespace JLogDashboard.Localization;

/// <summary>
/// Provides simple built-in translations for Dashboard UI text.
/// </summary>
public sealed class DashboardLocalizer
{
    private readonly IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> _catalogs;

    /// <summary>Creates a localizer from culture-specific text catalogs.</summary>
    public DashboardLocalizer(IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> catalogs)
    {
        _catalogs = catalogs;
    }

    /// <summary>Creates the built-in English and Simplified Chinese localizer.</summary>
    public static DashboardLocalizer CreateDefault()
    {
        return new DashboardLocalizer(new Dictionary<string, IReadOnlyDictionary<string, string>>(StringComparer.OrdinalIgnoreCase)
        {
            ["en-US"] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["Projects"] = "Log projects",
                ["Subtitle"] = "NLog, log4net, and Serilog file log dashboard for .NET",
                ["Search"] = "Search",
                ["Level"] = "Level",
                ["Message"] = "Message",
                ["Exception"] = "Exception",
                ["Refresh"] = "Refresh",
                ["Project"] = "Project",
                ["AllProjects"] = "All projects",
                ["SearchText"] = "Search text",
                ["ExcludeText"] = "Exclude text",
                ["PageSize"] = "Page size",
                ["LogEntries"] = "Log entries",
                ["Results"] = "results",
                ["Page"] = "Page",
                ["PreviousPage"] = "Prev",
                ["NextPage"] = "Next",
                ["Time"] = "Time",
                ["Logger"] = "Logger",
                ["File"] = "File",
                ["RunSearch"] = "Run a search to inspect logs.",
                ["NoMatches"] = "No log entries matched current filters.",
                ["Loading"] = "Loading log entries...",
                ["SearchFailed"] = "Log search failed. Check dashboard availability and retry.",
                ["PlaceholderSearch"] = "exception, order id, trace id",
                ["PlaceholderExclude"] = "healthcheck, heartbeat",
                ["Occurrences"] = "times"
            },
            ["zh-CN"] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["Projects"] = "日志项目",
                ["Subtitle"] = ".NET NLog、log4net、Serilog 文件日志看板",
                ["Search"] = "搜索",
                ["Level"] = "级别",
                ["Message"] = "消息",
                ["Exception"] = "异常",
                ["Refresh"] = "刷新",
                ["Project"] = "项目",
                ["AllProjects"] = "全部项目",
                ["SearchText"] = "搜索文本",
                ["ExcludeText"] = "排除文本",
                ["PageSize"] = "每页数量",
                ["LogEntries"] = "日志列表",
                ["Results"] = "条结果",
                ["Page"] = "页码",
                ["PreviousPage"] = "上一页",
                ["NextPage"] = "下一页",
                ["Time"] = "时间",
                ["Logger"] = "日志器",
                ["File"] = "文件",
                ["RunSearch"] = "执行搜索后查看日志。",
                ["NoMatches"] = "当前筛选条件没有匹配日志。",
                ["Loading"] = "正在加载日志...",
                ["SearchFailed"] = "日志搜索失败，请检查 Dashboard 状态后重试。",
                ["PlaceholderSearch"] = "异常、订单号、TraceId",
                ["PlaceholderExclude"] = "健康检查、心跳",
                ["Occurrences"] = "次"
            }
        });
    }

    /// <summary>Translates a key for the requested culture, falling back to English and then the key itself.</summary>
    public string Translate(string culture, string key)
    {
        if (_catalogs.TryGetValue(culture, out var catalog) && catalog.TryGetValue(key, out var value))
        {
            return value;
        }

        return _catalogs.TryGetValue("en-US", out var fallback) && fallback.TryGetValue(key, out var fallbackValue)
            ? fallbackValue
            : key;
    }
}
