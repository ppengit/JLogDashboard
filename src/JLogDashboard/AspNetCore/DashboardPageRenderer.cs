using System.Net;
using System.Text;

namespace JLogDashboard.AspNetCore;

internal static class DashboardPageRenderer
{
    public static string Render(DashboardPageModel model)
    {
        var apiRoutePrefix = string.IsNullOrWhiteSpace(model.RoutePrefix) ? string.Empty : model.RoutePrefix;
        var displayRoutePrefix = string.IsNullOrWhiteSpace(apiRoutePrefix) ? "/" : apiRoutePrefix;
        var upstreamUrl = model.Origin;
        var projects = model.Projects.Count == 0
            ? $"<option value=\"\">{H(model.Text["AllProjects"])}</option>"
            : string.Join(Environment.NewLine, model.Projects.Select(project =>
                $"<option value=\"{WebUtility.HtmlEncode(project)}\">{WebUtility.HtmlEncode(project)}</option>"));

        return $$"""
<!doctype html>
<html lang="{{WebUtility.HtmlEncode(model.Culture.StartsWith("zh", StringComparison.OrdinalIgnoreCase) ? "zh-CN" : "en")}}">
<head>
  <meta charset="utf-8">
  <meta name="viewport" content="width=device-width, initial-scale=1">
  <title>JLogDashboard</title>
  <style>
    :root {
      color-scheme: light;
      --bg: #f5f7f8;
      --panel: #ffffff;
      --ink: #1f2933;
      --muted: #65717c;
      --line: #d8dee5;
      --accent: #0f766e;
      --danger: #b42318;
      --warning: #a16207;
      --info: #2563eb;
      --shadow: 0 10px 30px rgba(16, 24, 40, .08);
    }
    * { box-sizing: border-box; }
    body {
      margin: 0;
      font-family: "Segoe UI", "Microsoft YaHei", sans-serif;
      background: var(--bg);
      color: var(--ink);
    }
    header {
      display: flex;
      justify-content: space-between;
      gap: 24px;
      align-items: flex-start;
      padding: 24px clamp(16px, 4vw, 40px);
      border-bottom: 1px solid var(--line);
      background: #fbfcfd;
    }
    h1 {
      margin: 0;
      font-size: clamp(24px, 3vw, 34px);
      letter-spacing: 0;
    }
    .subtitle {
      margin: 6px 0 0;
      color: var(--muted);
      font-size: 14px;
    }
    main {
      display: grid;
      grid-template-columns: minmax(280px, 360px) minmax(0, 1fr);
      gap: 18px;
      padding: 18px clamp(16px, 4vw, 40px) 32px;
    }
    section {
      background: var(--panel);
      border: 1px solid var(--line);
      border-radius: 8px;
      box-shadow: var(--shadow);
    }
    .filters, .tools { padding: 16px; }
    .stack { display: grid; gap: 12px; }
    label {
      display: grid;
      gap: 6px;
      color: var(--muted);
      font-size: 12px;
      font-weight: 600;
      text-transform: uppercase;
    }
    input, select, textarea {
      width: 100%;
      border: 1px solid var(--line);
      border-radius: 6px;
      padding: 10px 11px;
      color: var(--ink);
      background: #fff;
      font: inherit;
      font-size: 14px;
    }
    textarea {
      min-height: 210px;
      resize: vertical;
      font-family: "Cascadia Mono", Consolas, monospace;
      font-size: 12px;
      line-height: 1.55;
    }
    .levels {
      display: grid;
      grid-template-columns: repeat(2, minmax(0, 1fr));
      gap: 8px;
    }
    .levels label {
      display: flex;
      align-items: center;
      gap: 8px;
      padding: 8px 10px;
      border: 1px solid var(--line);
      border-radius: 6px;
      text-transform: none;
      font-size: 13px;
    }
    .levels input { width: auto; }
    button {
      border: 0;
      border-radius: 6px;
      padding: 10px 12px;
      background: var(--accent);
      color: #fff;
      font-weight: 700;
      cursor: pointer;
    }
    button.secondary {
      background: #e7eef0;
      color: var(--ink);
    }
    .split {
      display: grid;
      grid-template-columns: 1fr 1fr;
      gap: 10px;
    }
    .logs {
      overflow: hidden;
    }
    .logs-toolbar {
      display: flex;
      justify-content: space-between;
      align-items: center;
      padding: 14px 16px;
      border-bottom: 1px solid var(--line);
    }
    .table-wrap {
      overflow: auto;
      max-height: calc(100vh - 180px);
    }
    table {
      width: 100%;
      border-collapse: collapse;
      min-width: 860px;
    }
    th, td {
      padding: 10px 12px;
      border-bottom: 1px solid var(--line);
      text-align: left;
      vertical-align: top;
      font-size: 13px;
    }
    th {
      position: sticky;
      top: 0;
      z-index: 1;
      background: #f8fafb;
      color: var(--muted);
      font-size: 12px;
      text-transform: uppercase;
    }
    .badge {
      display: inline-flex;
      align-items: center;
      min-width: 72px;
      justify-content: center;
      border-radius: 999px;
      padding: 3px 8px;
      font-size: 12px;
      font-weight: 700;
      background: #edf2f7;
    }
    .level-Error, .level-Fatal { color: var(--danger); background: #fff0ee; }
    .level-Warning { color: var(--warning); background: #fff8db; }
    .level-Information { color: var(--info); background: #eef5ff; }
    .message { max-width: 620px; white-space: pre-wrap; word-break: break-word; }
    .empty {
      padding: 42px 18px;
      color: var(--muted);
      text-align: center;
    }
    @media (max-width: 940px) {
      header { flex-direction: column; }
      main { grid-template-columns: 1fr; }
      .table-wrap { max-height: none; }
    }
  </style>
</head>
<body>
  <header>
    <div>
      <h1>JLogDashboard</h1>
      <p class="subtitle">{{H(model.Text["Subtitle"])}}</p>
    </div>
    <button id="refresh-button" type="button">{{H(model.Text["Refresh"])}}</button>
  </header>
  <main>
    <div class="stack">
      <section class="filters stack">
        <label>{{H(model.Text["Project"])}}
          <select id="project-input">
            <option value="">{{H(model.Text["AllProjects"])}}</option>
            {{projects}}
          </select>
        </label>
        <label>{{H(model.Text["SearchText"])}}
          <input id="search-input" placeholder="{{H(model.Text["PlaceholderSearch"])}}">
        </label>
        <label>{{H(model.Text["ExcludeText"])}}
          <input id="exclude-input" placeholder="{{H(model.Text["PlaceholderExclude"])}}">
        </label>
        <div class="levels" id="levels-input">
          <label><input type="checkbox" value="Trace"> Trace</label>
          <label><input type="checkbox" value="Debug"> Debug</label>
          <label><input type="checkbox" value="Information"> Info</label>
          <label><input type="checkbox" value="Warning"> Warn</label>
          <label><input type="checkbox" value="Error" checked> Error</label>
          <label><input type="checkbox" value="Fatal" checked> Fatal</label>
        </div>
        <div class="split">
          <label>{{H(model.Text["PageSize"])}}
            <input id="page-size-input" type="number" min="10" max="500" value="50">
          </label>
          <label>{{H(model.Text["Route"])}}
            <input id="route-prefix-input" value="{{WebUtility.HtmlEncode(displayRoutePrefix)}}">
          </label>
        </div>
        <button id="search-button" type="button">{{H(model.Text["Search"])}}</button>
      </section>

      <section class="tools stack">
        <label>{{H(model.Text["ServerName"])}}
          <input id="server-name-input" value="logs.example.com">
        </label>
        <label>{{H(model.Text["UpstreamUrl"])}}
          <input id="upstream-url-input" value="{{WebUtility.HtmlEncode(upstreamUrl)}}">
        </label>
        <label>{{H(model.Text["BasePath"])}}
          <input id="base-path-input" value="{{WebUtility.HtmlEncode(displayRoutePrefix)}}">
        </label>
        <div class="split">
          <button id="generate-nginx-button" type="button" class="secondary">{{H(model.Text["GenerateNginx"])}}</button>
          <button id="copy-nginx-button" type="button">{{H(model.Text["CopyNginx"])}}</button>
        </div>
        <textarea id="nginx-output" spellcheck="false"></textarea>
      </section>
    </div>

    <section class="logs">
      <div class="logs-toolbar">
        <strong>{{H(model.Text["LogEntries"])}}</strong>
        <span id="result-count" class="subtitle">0 {{H(model.Text["Results"])}}</span>
      </div>
      <div class="table-wrap">
        <table>
          <thead>
            <tr>
              <th>{{H(model.Text["Time"])}}</th>
              <th>{{H(model.Text["Project"])}}</th>
              <th>{{H(model.Text["Level"])}}</th>
              <th>{{H(model.Text["Logger"])}}</th>
              <th>{{H(model.Text["Message"])}}</th>
              <th>{{H(model.Text["File"])}}</th>
            </tr>
          </thead>
          <tbody id="log-body">
            <tr><td class="empty" colspan="6">{{H(model.Text["RunSearch"])}}</td></tr>
          </tbody>
        </table>
      </div>
    </section>
  </main>
  <script>
    const routePrefix = {{ToJavaScriptString(apiRoutePrefix)}};
    const resultsText = {{ToJavaScriptString(model.Text["Results"])}};
    const noMatchesText = {{ToJavaScriptString(model.Text["NoMatches"])}};
    const body = document.getElementById('log-body');
    const count = document.getElementById('result-count');
    const selectedLevels = () => [...document.querySelectorAll('#levels-input input:checked')].map(x => x.value);
    const escapeHtml = value => String(value ?? '').replace(/[&<>"']/g, ch => ({'&':'&amp;','<':'&lt;','>':'&gt;','"':'&quot;',"'":'&#39;'}[ch]));

    async function searchLogs() {
      const payload = {
        project: document.getElementById('project-input').value || null,
        levels: selectedLevels(),
        searchText: document.getElementById('search-input').value || null,
        excludeText: document.getElementById('exclude-input').value || null,
        page: 1,
        pageSize: Number(document.getElementById('page-size-input').value || 50)
      };
      const response = await fetch(`${routePrefix}/api/search`, {
        method: 'POST',
        headers: { 'content-type': 'application/json' },
        body: JSON.stringify(payload)
      });
      const result = await response.json();
      count.textContent = `${result.total} ${resultsText}`;
      if (!result.items || result.items.length === 0) {
        body.innerHTML = `<tr><td class="empty" colspan="6">${escapeHtml(noMatchesText)}</td></tr>`;
        return;
      }
      body.innerHTML = result.items.map(item => `
        <tr>
          <td>${escapeHtml(item.timestamp)}</td>
          <td>${escapeHtml(item.project)}</td>
          <td><span class="badge level-${escapeHtml(item.level)}">${escapeHtml(item.level)}</span></td>
          <td>${escapeHtml(item.logger)}</td>
          <td class="message">${escapeHtml(item.message)}${item.exception ? '<br><br>' + escapeHtml(item.exception) : ''}</td>
          <td>${escapeHtml(item.sourcePath)}</td>
        </tr>`).join('');
    }

    async function generateNginx() {
      const params = new URLSearchParams({
        serverName: document.getElementById('server-name-input').value,
        upstreamUrl: document.getElementById('upstream-url-input').value,
        basePath: document.getElementById('base-path-input').value
      });
      const response = await fetch(`${routePrefix}/api/nginx?${params}`);
      document.getElementById('nginx-output').value = await response.text();
    }

    document.getElementById('search-button').addEventListener('click', searchLogs);
    document.getElementById('refresh-button').addEventListener('click', searchLogs);
    document.getElementById('generate-nginx-button').addEventListener('click', generateNginx);
    document.getElementById('copy-nginx-button').addEventListener('click', async () => {
      const output = document.getElementById('nginx-output');
      if (!output.value) await generateNginx();
      await navigator.clipboard.writeText(output.value);
    });
    generateNginx();
  </script>
</body>
</html>
""";
    }

    private static string ToJavaScriptString(string value)
        => "\"" + value.Replace("\\", "\\\\", StringComparison.Ordinal).Replace("\"", "\\\"", StringComparison.Ordinal) + "\"";

    private static string H(string value) => WebUtility.HtmlEncode(value);
}
