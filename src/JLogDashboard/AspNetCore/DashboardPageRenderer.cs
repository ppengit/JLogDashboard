using System.Net;
using System.Text;

namespace JLogDashboard.AspNetCore;

internal static class DashboardPageRenderer
{
    public static string Render(DashboardPageModel model)
    {
        var apiRoutePrefix = string.IsNullOrWhiteSpace(model.RoutePrefix) ? string.Empty : model.RoutePrefix;
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
      --page: #eef2f4;
      --surface: #ffffff;
      --surface-soft: #f7f9fa;
      --surface-strong: #111827;
      --ink: #18212b;
      --muted: #65727f;
      --subtle: #8a97a3;
      --line: #d9e0e6;
      --line-soft: #e9eef2;
      --accent: #0f766e;
      --accent-strong: #0b5f59;
      --danger: #b42318;
      --warning: #946200;
      --info: #1d5fbf;
      --ok: #116149;
      --focus: #1d9a8a;
      --shadow: 0 18px 46px rgba(31, 41, 51, .10);
    }
    * { box-sizing: border-box; }
    html { height: 100%; }
    body {
      margin: 0;
      height: 100vh;
      min-height: 100vh;
      font-family: "Segoe UI", "Microsoft YaHei", "PingFang SC", sans-serif;
      background: var(--page);
      color: var(--ink);
      overflow: hidden;
    }
    button, input, select {
      font: inherit;
    }
    button:focus-visible,
    input:focus-visible,
    select:focus-visible {
      outline: 3px solid rgba(29, 154, 138, .22);
      outline-offset: 2px;
      border-color: var(--focus);
    }
    .app-shell {
      display: grid;
      grid-template-rows: 64px minmax(0, 1fr);
      height: 100vh;
      min-height: 100vh;
      min-width: 0;
      overflow: hidden;
    }
    .topbar {
      display: flex;
      align-items: center;
      justify-content: space-between;
      gap: 18px;
      padding: 0 20px;
      border-bottom: 1px solid var(--line);
      background: rgba(255, 255, 255, .92);
      backdrop-filter: blur(12px);
    }
    .brand {
      display: flex;
      align-items: center;
      min-width: 0;
      gap: 12px;
    }
    .brand-mark {
      display: grid;
      place-items: center;
      width: 36px;
      height: 36px;
      border-radius: 8px;
      background: var(--surface-strong);
      color: #f8fafc;
      font-size: 13px;
      font-weight: 800;
      letter-spacing: 0;
      box-shadow: inset 0 -1px 0 rgba(255, 255, 255, .16);
    }
    h1 {
      margin: 0;
      font-size: 20px;
      line-height: 1.1;
      letter-spacing: 0;
    }
    .subtitle {
      margin: 4px 0 0;
      color: var(--muted);
      font-size: 13px;
      line-height: 1.3;
    }
    .top-actions {
      display: flex;
      align-items: center;
      gap: 10px;
      flex-shrink: 0;
    }
    .metric {
      min-width: 110px;
      padding: 7px 10px;
      border: 1px solid var(--line);
      border-radius: 7px;
      background: var(--surface-soft);
      color: var(--muted);
      font-size: 13px;
      text-align: center;
    }
    .workspace {
      display: grid;
      grid-template-columns: minmax(280px, 340px) minmax(0, 1fr);
      gap: 12px;
      height: calc(100vh - 64px);
      min-height: 0;
      min-width: 0;
      overflow: hidden;
      padding: 12px;
    }
    .filter-rail,
    .log-panel {
      min-width: 0;
      min-height: 0;
      border: 1px solid var(--line);
      border-radius: 8px;
      background: var(--surface);
      box-shadow: var(--shadow);
    }
    .filter-rail {
      display: grid;
      grid-template-rows: auto minmax(0, 1fr) auto;
      overflow: hidden;
    }
    .rail-header,
    .logs-toolbar,
    .pager {
      background: linear-gradient(180deg, #fbfcfd, #f6f8f9);
    }
    .rail-header {
      padding: 14px 16px;
      border-bottom: 1px solid var(--line-soft);
    }
    .section-title {
      margin: 0;
      font-size: 13px;
      font-weight: 800;
      letter-spacing: 0;
    }
    .rail-scroll {
      display: grid;
      align-content: start;
      gap: 14px;
      overflow: auto;
      padding: 14px 16px;
    }
    .field {
      display: grid;
      gap: 7px;
      color: var(--muted);
      font-size: 12px;
      font-weight: 700;
      text-transform: uppercase;
    }
    .field-control {
      width: 100%;
      min-height: 38px;
      border: 1px solid var(--line);
      border-radius: 7px;
      padding: 8px 10px;
      color: var(--ink);
      background: #fff;
      font-size: 14px;
      transition: border-color .14s ease, box-shadow .14s ease, background .14s ease;
    }
    .field-control:hover {
      border-color: #b9c4ce;
      background: #fcfdfd;
    }
    .level-fieldset {
      margin: 0;
      padding: 0;
      border: 0;
    }
    .level-legend {
      margin: 0 0 8px;
      color: var(--muted);
      font-size: 12px;
      font-weight: 800;
      text-transform: uppercase;
    }
    .levels {
      display: grid;
      grid-template-columns: repeat(2, minmax(0, 1fr));
      gap: 8px;
    }
    .level-toggle {
      display: flex;
      align-items: center;
      gap: 8px;
      min-height: 36px;
      padding: 8px 10px;
      border: 1px solid var(--line);
      border-radius: 7px;
      background: var(--surface-soft);
      color: var(--ink);
      font-size: 13px;
      font-weight: 700;
      cursor: pointer;
      transition: border-color .14s ease, background .14s ease, color .14s ease;
    }
    .level-toggle:hover {
      border-color: #b9c4ce;
      background: #fff;
    }
    .level-toggle input {
      width: 15px;
      height: 15px;
      accent-color: var(--accent);
      flex: 0 0 auto;
    }
    .rail-actions {
      display: grid;
      grid-template-columns: 1fr;
      gap: 10px;
      padding: 14px 16px;
      border-top: 1px solid var(--line-soft);
      background: #fbfcfd;
    }
    .button {
      min-height: 38px;
      border: 1px solid transparent;
      border-radius: 7px;
      padding: 9px 13px;
      font-weight: 800;
      cursor: pointer;
      transition: background .14s ease, border-color .14s ease, color .14s ease, transform .08s ease, opacity .14s ease;
    }
    .button:active:not(:disabled) {
      transform: translateY(1px);
    }
    .button:disabled {
      cursor: not-allowed;
      opacity: .58;
    }
    .button-primary {
      background: var(--accent);
      color: #fff;
      box-shadow: 0 8px 18px rgba(15, 118, 110, .18);
    }
    .button-primary:hover:not(:disabled) {
      background: var(--accent-strong);
    }
    .button-secondary {
      border-color: var(--line);
      background: #fff;
      color: var(--ink);
    }
    .button-secondary:hover:not(:disabled) {
      background: var(--surface-soft);
      border-color: #b9c4ce;
    }
    .log-panel {
      display: grid;
      grid-template-rows: auto auto minmax(0, 1fr) auto;
      overflow: hidden;
    }
    .logs-toolbar {
      display: flex;
      justify-content: space-between;
      align-items: center;
      gap: 12px;
      padding: 13px 16px;
      border-bottom: 1px solid var(--line-soft);
    }
    .logs-title {
      display: flex;
      align-items: baseline;
      gap: 10px;
      min-width: 0;
    }
    .logs-title strong {
      font-size: 14px;
    }
    .status-line {
      min-height: 34px;
      padding: 8px 16px;
      border-bottom: 1px solid var(--line-soft);
      color: var(--muted);
      background: #fcfdfd;
      font-size: 13px;
    }
    .status-line[data-state="error"] {
      color: var(--danger);
      background: #fff7f6;
    }
    .status-line[data-state="loading"] {
      color: var(--accent-strong);
    }
    .table-wrap {
      overflow: auto;
      min-height: 0;
      background: #fff;
    }
    table {
      width: 100%;
      min-width: 980px;
      border-collapse: separate;
      border-spacing: 0;
    }
    th, td {
      padding: 9px 12px;
      border-bottom: 1px solid var(--line-soft);
      text-align: left;
      vertical-align: top;
      font-size: 13px;
      line-height: 1.45;
    }
    th {
      position: sticky;
      top: 0;
      z-index: 1;
      background: #f7f9fa;
      color: var(--muted);
      font-size: 11px;
      font-weight: 800;
      text-transform: uppercase;
      box-shadow: inset 0 -1px 0 var(--line);
    }
    tbody tr {
      transition: background .12s ease;
    }
    tbody tr:hover {
      background: #f9fbfb;
    }
    .time-cell {
      width: 178px;
      color: #334155;
      font-variant-numeric: tabular-nums;
      white-space: nowrap;
    }
    .timestamp {
      color: #334155;
    }
    .source-file {
      margin-top: 3px;
      max-width: 178px;
      color: var(--muted);
      font-size: 12px;
      line-height: 1.35;
      overflow: hidden;
      text-overflow: ellipsis;
      white-space: nowrap;
    }
    .project-cell {
      width: 128px;
      font-weight: 700;
      color: #273441;
      white-space: nowrap;
    }
    .logger-cell {
      width: 230px;
      max-width: 260px;
      color: #394856;
      word-break: break-word;
    }
    .message {
      min-width: 360px;
      word-break: break-word;
    }
    .message-main {
      color: var(--ink);
      font-weight: 600;
      white-space: pre-wrap;
      word-break: break-word;
    }
    .message-head {
      display: flex;
      align-items: flex-start;
      gap: 8px;
    }
    .message-head .message-main {
      min-width: 0;
      flex: 1 1 auto;
    }
    .count-tag {
      display: inline-flex;
      align-items: center;
      justify-content: center;
      flex: 0 0 auto;
      min-width: 52px;
      border: 1px solid #cfe2df;
      border-radius: 999px;
      padding: 2px 8px;
      background: #eef8f6;
      color: var(--accent-strong);
      font-size: 11px;
      font-weight: 900;
      line-height: 1.5;
      white-space: nowrap;
    }
    .exception {
      margin: 8px 0 0;
      padding: 9px 10px;
      border-left: 3px solid #e6b7b2;
      border-radius: 0 6px 6px 0;
      background: #fff8f7;
      color: #5f2a25;
      font-family: Consolas, "Cascadia Mono", monospace;
      font-size: 12px;
      line-height: 1.45;
      white-space: pre-wrap;
    }
    .badge {
      display: inline-flex;
      align-items: center;
      justify-content: center;
      min-width: 76px;
      border-radius: 999px;
      padding: 3px 9px;
      font-size: 11px;
      font-weight: 900;
      letter-spacing: 0;
      background: #edf2f7;
      color: #334155;
      white-space: nowrap;
    }
    .level-Trace { color: #475569; background: #f1f5f9; }
    .level-Debug { color: #31506c; background: #eaf2f8; }
    .level-Information { color: var(--info); background: #eef5ff; }
    .level-Warning { color: var(--warning); background: #fff8db; }
    .level-Error { color: var(--danger); background: #fff0ee; }
    .level-Fatal { color: #7f1d1d; background: #fee2e2; }
    .empty {
      height: 240px;
      padding: 54px 18px;
      color: var(--muted);
      text-align: center;
      font-size: 14px;
    }
    .pager {
      display: flex;
      align-items: center;
      justify-content: flex-end;
      gap: 9px;
      min-height: 54px;
      padding: 10px 16px;
      border-top: 1px solid var(--line);
    }
    .pager label {
      display: inline-flex;
      align-items: center;
      gap: 7px;
      color: var(--muted);
      font-size: 12px;
      font-weight: 800;
      text-transform: uppercase;
    }
    .pager input {
      width: 72px;
      min-height: 34px;
      text-align: center;
    }
    .page-total {
      min-width: 44px;
      color: var(--muted);
      font-size: 13px;
    }
    @media (max-width: 980px) {
      body { overflow: auto; }
      .app-shell {
        grid-template-rows: auto minmax(0, 1fr);
        height: auto;
        min-height: 100vh;
        overflow: visible;
      }
      .topbar {
        align-items: flex-start;
        padding: 12px;
        flex-direction: column;
      }
      .top-actions {
        display: grid;
        grid-template-columns: 1fr;
        width: 100%;
      }
      .metric {
        min-width: 0;
      }
      .top-actions .button {
        width: 100%;
      }
      .workspace {
        grid-template-columns: 1fr;
        height: auto;
        min-height: auto;
        overflow: visible;
      }
      .filter-rail {
        grid-template-rows: auto auto auto;
      }
      .rail-scroll {
        overflow: visible;
      }
      .log-panel {
        min-height: 640px;
      }
    }
    @media (max-width: 640px) {
      .workspace {
        padding: 8px;
        gap: 8px;
      }
      .levels {
        grid-template-columns: 1fr;
      }
      .logs-toolbar,
      .pager {
        align-items: stretch;
        flex-direction: column;
      }
      .pager .button,
      .pager label {
        width: 100%;
      }
      .pager input {
        flex: 1;
      }
    }
    @media (prefers-reduced-motion: reduce) {
      *, *::before, *::after {
        transition: none !important;
      }
    }
  </style>
</head>
<body>
  <div class="app-shell">
    <header class="topbar">
      <div class="brand">
        <div class="brand-mark" aria-hidden="true">JL</div>
        <div>
          <h1>JLogDashboard</h1>
          <p class="subtitle">{{H(model.Text["Subtitle"])}}</p>
        </div>
      </div>
      <div class="top-actions">
        <span id="result-count" class="metric">0 {{H(model.Text["Results"])}}</span>
        <button id="refresh-button" class="button button-secondary" type="button">{{H(model.Text["Refresh"])}}</button>
      </div>
    </header>

    <main class="workspace">
      <aside class="filter-rail" aria-label="{{H(model.Text["Search"])}}">
        <div class="rail-header">
          <p class="section-title">{{H(model.Text["Search"])}}</p>
        </div>
        <div class="rail-scroll">
          <label class="field">{{H(model.Text["Project"])}}
            <select id="project-input" class="field-control">
              <option value="">{{H(model.Text["AllProjects"])}}</option>
              {{projects}}
            </select>
          </label>
          <label class="field">{{H(model.Text["SearchText"])}}
            <input id="search-input" class="field-control" placeholder="{{H(model.Text["PlaceholderSearch"])}}">
          </label>
          <label class="field">{{H(model.Text["ExcludeText"])}}
            <input id="exclude-input" class="field-control" placeholder="{{H(model.Text["PlaceholderExclude"])}}">
          </label>
          <fieldset class="level-fieldset">
            <legend class="level-legend">{{H(model.Text["Level"])}}</legend>
            <div class="levels" id="levels-input">
              <label class="level-toggle"><input type="checkbox" value="Trace"> Trace</label>
              <label class="level-toggle"><input type="checkbox" value="Debug"> Debug</label>
              <label class="level-toggle"><input type="checkbox" value="Information"> Info</label>
              <label class="level-toggle"><input type="checkbox" value="Warning"> Warn</label>
              <label class="level-toggle"><input type="checkbox" value="Error" checked> Error</label>
              <label class="level-toggle"><input type="checkbox" value="Fatal" checked> Fatal</label>
            </div>
          </fieldset>
          <label class="field">{{H(model.Text["PageSize"])}}
            <input id="page-size-input" class="field-control" type="number" min="10" max="500" value="50">
          </label>
        </div>
        <div class="rail-actions">
          <button id="search-button" class="button button-primary" type="button">{{H(model.Text["Search"])}}</button>
        </div>
      </aside>

      <section class="log-panel" aria-label="{{H(model.Text["LogEntries"])}}">
        <div class="logs-toolbar">
          <div class="logs-title">
            <strong>{{H(model.Text["LogEntries"])}}</strong>
          </div>
        </div>
        <div id="status-line" class="status-line" role="status">{{H(model.Text["RunSearch"])}}</div>
        <div class="table-wrap">
          <table>
            <thead>
              <tr>
                <th>{{H(model.Text["Time"])}}</th>
                <th>{{H(model.Text["Project"])}}</th>
                <th>{{H(model.Text["Level"])}}</th>
                <th>{{H(model.Text["Logger"])}}</th>
                <th>{{H(model.Text["Message"])}}</th>
              </tr>
            </thead>
            <tbody id="log-body">
              <tr><td class="empty" colspan="5">{{H(model.Text["RunSearch"])}}</td></tr>
            </tbody>
          </table>
        </div>
        <div class="pager">
          <button id="prev-page-button" class="button button-secondary" type="button">{{H(model.Text["PreviousPage"])}}</button>
          <label>{{H(model.Text["Page"])}}
            <input id="page-input" class="field-control" type="number" min="1" value="1">
          </label>
          <span id="page-count" class="page-total">/ 1</span>
          <button id="next-page-button" class="button button-secondary" type="button">{{H(model.Text["NextPage"])}}</button>
        </div>
      </section>
    </main>
  </div>

  <script>
    const routePrefix = {{ToJavaScriptString(apiRoutePrefix)}};
    const apiBaseUrl = `${window.location.origin}${routePrefix}`;
    const resultsText = {{ToJavaScriptString(model.Text["Results"])}};
    const noMatchesText = {{ToJavaScriptString(model.Text["NoMatches"])}};
    const runSearchText = {{ToJavaScriptString(model.Text["RunSearch"])}};
    const loadingText = {{ToJavaScriptString(model.Text["Loading"])}};
    const searchFailedText = {{ToJavaScriptString(model.Text["SearchFailed"])}};
    const occurrencesText = {{ToJavaScriptString(model.Text["Occurrences"])}};
    const body = document.getElementById('log-body');
    const count = document.getElementById('result-count');
    const statusLine = document.getElementById('status-line');
    const pageInput = document.getElementById('page-input');
    const pageCount = document.getElementById('page-count');
    const searchButton = document.getElementById('search-button');
    const refreshButton = document.getElementById('refresh-button');
    const prevPageButton = document.getElementById('prev-page-button');
    const nextPageButton = document.getElementById('next-page-button');
    let currentPage = 1;
    let totalPages = 1;
    const selectedLevels = () => [...document.querySelectorAll('#levels-input input:checked')].map(x => x.value);
    const escapeHtml = value => String(value ?? '').replace(/[&<>"']/g, ch => ({'&':'&amp;','<':'&lt;','>':'&gt;','"':'&quot;',"'":'&#39;'}[ch]));
    const pageSize = () => Math.max(1, Number(document.getElementById('page-size-input').value || 50));
    const shortFileName = value => {
      const text = String(value ?? '');
      const parts = text.split(/[\\/]/);
      return parts[parts.length - 1] || text;
    };
    const formatTimestamp = value => String(value ?? '').replace('T', ' ').replace(/\+00:00$/, '');

    function aggregationKey(item) {
      return [
        item.project,
        item.level,
        item.logger,
        item.message,
        item.exception,
        item.sourcePath
      ].map(value => String(value ?? '')).join('\u001f');
    }

    function aggregateCurrentPageItems(items) {
      const groups = new Map();
      for (const item of items) {
        const key = aggregationKey(item);
        const existing = groups.get(key);
        if (!existing) {
          groups.set(key, {
            ...item,
            count: 1,
            firstTimestamp: item.timestamp,
            lastTimestamp: item.timestamp
          });
          continue;
        }

        existing.count += 1;
        const currentTime = String(item.timestamp ?? '');
        if (currentTime > String(existing.firstTimestamp ?? '')) {
          existing.firstTimestamp = item.timestamp;
        }
        if (currentTime < String(existing.lastTimestamp ?? '')) {
          existing.lastTimestamp = item.timestamp;
        }
      }

      return [...groups.values()];
    }

    function formatTimestampRange(item) {
      const first = formatTimestamp(item.firstTimestamp ?? item.timestamp);
      const last = formatTimestamp(item.lastTimestamp ?? item.timestamp);
      return item.count > 1 && first !== last ? `${first} ~ ${last}` : first;
    }

    function setBusy(isBusy) {
      searchButton.disabled = isBusy;
      refreshButton.disabled = isBusy;
      if (isBusy) {
        statusLine.dataset.state = 'loading';
        statusLine.textContent = loadingText;
      } else if (statusLine.dataset.state === 'loading') {
        statusLine.dataset.state = '';
      }
    }

    function setStatus(text, state = '') {
      statusLine.textContent = text;
      statusLine.dataset.state = state;
    }

    function updatePager(result) {
      currentPage = Math.max(1, Number(result.page || currentPage));
      totalPages = Math.max(1, Math.ceil(Number(result.total || 0) / Math.max(1, Number(result.pageSize || pageSize()))));
      pageInput.value = currentPage;
      pageInput.max = totalPages;
      pageCount.textContent = `/ ${totalPages}`;
      prevPageButton.disabled = currentPage <= 1;
      nextPageButton.disabled = currentPage >= totalPages;
    }

    function renderRows(items) {
      body.innerHTML = items.map(item => `
        <tr>
          <td class="time-cell"><div class="timestamp">${escapeHtml(formatTimestampRange(item))}</div><div class="source-file" title="${escapeHtml(item.sourcePath)}">${escapeHtml(shortFileName(item.sourcePath))}</div></td>
          <td class="project-cell">${escapeHtml(item.project)}</td>
          <td><span class="badge level-${escapeHtml(item.level)}">${escapeHtml(item.level)}</span></td>
          <td class="logger-cell">${escapeHtml(item.logger)}</td>
          <td class="message"><div class="message-head"><div class="message-main">${escapeHtml(item.message)}</div>${item.count > 1 ? `<span class="count-tag">${item.count} ${occurrencesText}</span>` : ''}</div>${item.exception ? `<pre class="exception">${escapeHtml(item.exception)}</pre>` : ''}</td>
        </tr>`).join('');
    }

    async function searchLogs(resetPage = false) {
      if (resetPage) currentPage = 1;
      setBusy(true);
      const payload = {
        project: document.getElementById('project-input').value || null,
        levels: selectedLevels(),
        searchText: document.getElementById('search-input').value || null,
        excludeText: document.getElementById('exclude-input').value || null,
        page: currentPage,
        pageSize: pageSize()
      };

      try {
        const response = await fetch(`${apiBaseUrl}/api/search`, {
          method: 'POST',
          headers: { 'content-type': 'application/json' },
          credentials: 'same-origin',
          body: JSON.stringify(payload)
        });
        if (!response.ok) {
          throw new Error(`HTTP ${response.status}`);
        }
        const result = await response.json();
        count.textContent = `${result.total} ${resultsText}`;
        updatePager(result);
        if (!result.items || result.items.length === 0) {
          body.innerHTML = `<tr><td class="empty" colspan="5">${escapeHtml(noMatchesText)}</td></tr>`;
          setStatus(noMatchesText);
          return;
        }
        renderRows(aggregateCurrentPageItems(result.items));
        setStatus(`${result.total} ${resultsText}`);
      } catch {
        body.innerHTML = `<tr><td class="empty" colspan="5">${escapeHtml(searchFailedText)}</td></tr>`;
        setStatus(searchFailedText, 'error');
      } finally {
        setBusy(false);
      }
    }

    setStatus(runSearchText);
    document.getElementById('search-button').addEventListener('click', () => searchLogs(true));
    document.getElementById('refresh-button').addEventListener('click', () => searchLogs(false));
    prevPageButton.addEventListener('click', () => {
      if (currentPage > 1) {
        currentPage -= 1;
        searchLogs(false);
      }
    });
    nextPageButton.addEventListener('click', () => {
      if (currentPage < totalPages) {
        currentPage += 1;
        searchLogs(false);
      }
    });
    pageInput.addEventListener('change', () => {
      currentPage = Math.min(totalPages, Math.max(1, Number(pageInput.value || 1)));
      searchLogs(false);
    });
    searchLogs(true);
  </script>
</body>
</html>
""";
    }

    private static string ToJavaScriptString(string value)
        => "\"" + value.Replace("\\", "\\\\", StringComparison.Ordinal).Replace("\"", "\\\"", StringComparison.Ordinal) + "\"";

    private static string H(string value) => WebUtility.HtmlEncode(value);
}
