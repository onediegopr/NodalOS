using System.Net;
using OneBrain.AgentOperations.Core.Runtime;

namespace OneBrain.Pilot;

public static class RuntimeInspectorHtmlRenderer
{
    public static string Render(NodalOsSelectiveRuntimeFixtureResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        var inspector = result.Inspector;
        var timeline = string.Join(Environment.NewLine, result.Timeline.Select(item =>
            $"<li><span>{Html(item.CreatedAt.ToString("HH:mm:ss"))}</span><strong>{Html(item.Kind.ToString())}</strong><p>{Html(item.SummaryRedacted)}</p></li>"));
        var capabilities = string.Join(Environment.NewLine, inspector.Capabilities.Select(item => $"<li>{Html(item)}</li>"));
        var providers = string.Join(Environment.NewLine, inspector.Providers.Select(item => $"<li>{Html(item)}</li>"));
        var fallbacks = inspector.RecentFallbacks.Count == 0
            ? "<p class=\"muted\">No fallback was required.</p>"
            : string.Join(Environment.NewLine, inspector.RecentFallbacks.Select(item => $"<p class=\"notice\">↺ {Html(item)}</p>"));

        return $$"""
            <!doctype html>
            <html lang="en">
            <head>
              <meta charset="utf-8">
              <meta name="viewport" content="width=device-width,initial-scale=1">
              <meta name="robots" content="noindex,nofollow">
              <title>NODAL OS Runtime Inspector</title>
              <style>
                :root{color-scheme:dark;font-family:Inter,ui-sans-serif,system-ui,sans-serif;background:#0d1117;color:#f5f7fa}
                *{box-sizing:border-box}body{margin:0;background:#0d1117}.shell{min-height:100vh;display:grid;grid-template-columns:230px minmax(0,1fr) 300px;grid-template-rows:auto 1fr auto}.top{grid-column:1/-1;padding:18px 24px;border-bottom:1px solid #30363d;background:#161b22;display:flex;justify-content:space-between;gap:16px;align-items:center}.top h1{font-size:18px;margin:0}.badge{padding:6px 10px;border:1px solid #4f7cff;border-radius:999px;color:#b8c7ff}.side,.right{padding:20px;border-right:1px solid #30363d;background:#161b22}.right{border-right:0;border-left:1px solid #30363d}.main{padding:24px;min-width:0}.card{background:#1c2128;border:1px solid #30363d;border-radius:12px;padding:16px;margin-bottom:16px}.metric{display:flex;justify-content:space-between;gap:16px;padding:8px 0;border-bottom:1px solid #30363d}.metric:last-child{border-bottom:0}.progress{height:8px;background:#30363d;border-radius:999px;overflow:hidden}.progress>span{display:block;height:100%;background:linear-gradient(90deg,#4f7cff,#7c5cff)}ul{padding-left:18px}.timeline{list-style:none;padding:0}.timeline li{position:relative;padding:0 0 18px 24px;border-left:2px solid #30363d}.timeline li:before{content:"";position:absolute;left:-6px;top:4px;width:10px;height:10px;border-radius:50%;background:#4f7cff}.timeline span{color:#aab4c0;font-size:12px}.timeline strong{display:block;margin-top:2px}.timeline p{margin:4px 0;color:#aab4c0}.notice{border-left:3px solid #7c5cff;padding:8px 10px;background:#171b28;color:#d7d0ff}.muted{color:#6e7681}.bottom{grid-column:1/-1;padding:12px 24px;border-top:1px solid #30363d;background:#161b22;color:#aab4c0;font-size:13px}@media(max-width:900px){.shell{grid-template-columns:1fr}.top,.bottom{grid-column:1}.side,.right{border:0;border-bottom:1px solid #30363d}.right{border-top:1px solid #30363d}}
              </style>
            </head>
            <body>
              <div class="shell" data-nodal-os="runtime-inspector" data-local-dev-only="true" data-read-only="true">
                <header class="top">
                  <div><h1>{{Html(inspector.Goal)}}</h1><div class="muted">{{Html(inspector.MissionId)}} · {{Html(inspector.RunId)}}</div></div>
                  <span class="badge">{{Html(inspector.MissionStatus)}} · {{Math.Round(inspector.Progress * 100)}}%</span>
                </header>
                <aside class="side" data-section-id="mission">
                  <h2>Mission</h2>
                  <div class="metric"><span>Current step</span><strong>{{Html(inspector.CurrentStep ?? "none")}}</strong></div>
                  <div class="metric"><span>Logical model</span><strong>{{Html(inspector.LogicalModel ?? "none")}}</strong></div>
                  <div class="metric"><span>Provider</span><strong>{{Html(inspector.ActiveProvider ?? "none")}}</strong></div>
                  <div class="progress"><span style="width:{{Math.Clamp(inspector.Progress * 100, 0, 100).ToString("0.##", System.Globalization.CultureInfo.InvariantCulture)}}%"></span></div>
                  <h3>Plan</h3><ul>{{string.Join(Environment.NewLine, inspector.PlanSteps.Select(item => $"<li>{Html(item)}</li>"))}}</ul>
                </aside>
                <main class="main" data-section-id="timeline">
                  <section class="card"><h2>Timeline</h2><ol class="timeline">{{timeline}}</ol></section>
                  <section class="card" data-section-id="fallback"><h2>Continuity</h2>{{fallbacks}}</section>
                  <section class="card"><h2>Evidence</h2><ul>{{string.Join(Environment.NewLine, inspector.EvidenceRefs.Select(item => $"<li>{Html(item)}</li>"))}}</ul></section>
                </main>
                <aside class="right" data-section-id="runtime">
                  <h2>Runtime</h2>
                  <div class="metric"><span>Browser</span><strong>{{Html(inspector.Browser.Runtime)}}</strong></div>
                  <div class="metric"><span>Browser state</span><strong>{{Html(inspector.Browser.State)}}</strong></div>
                  <div class="metric"><span>Secrets</span><strong>excluded</strong></div>
                  <h3>Capabilities</h3><ul>{{capabilities}}</ul>
                  <h3>Providers</h3><ul>{{providers}}</ul>
                </aside>
                <footer class="bottom">Local/dev read-only inspector. No scripts, forms, external resources, network provider calls or product authority.</footer>
              </div>
            </body>
            </html>
            """;
    }

    private static string Html(string? value) => WebUtility.HtmlEncode(value ?? string.Empty);
}
