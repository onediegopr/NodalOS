using System.Net;
using System.Text;

namespace OneBrain.Pilot;

public static class PilotHomePageRenderer
{
    public static string Render(PilotPlan? plan = null, PilotExecutionResult? result = null)
    {
        var taskValue = plan?.Intent.OriginalText ?? "";
        var recipeLabel = plan?.Intent.Recipe?.Label ?? "No recipe selected";
        var status = result?.Status ?? (plan?.Intent.IsMatch == true ? "plan_ready" : "idle");
        var latestMarkdown = result?.LatestMarkdownPath ?? "";
        var latestHtml = result?.LatestHtmlPath ?? "";

        return $$"""
<!doctype html>
<html lang="en">
<head>
  <meta charset="utf-8">
  <meta name="viewport" content="width=device-width, initial-scale=1">
  <title>ONE BRAIN Pilot</title>
  <style>
    :root {
      --ink: #17211a;
      --muted: #5c6b60;
      --paper: #f5f1e7;
      --panel: rgba(255, 252, 242, 0.86);
      --line: #d7cdb7;
      --accent: #e66b2d;
      --safe: #226b45;
      --warn: #9a5a10;
      --shadow: 0 24px 80px rgba(43, 32, 16, 0.16);
    }
    * { box-sizing: border-box; }
    body {
      margin: 0;
      min-height: 100vh;
      color: var(--ink);
      font-family: "Aptos", "Segoe UI", sans-serif;
      background:
        radial-gradient(circle at 10% 10%, rgba(230, 107, 45, 0.16), transparent 30rem),
        linear-gradient(135deg, #f7f2df 0%, #e8efe4 52%, #dfe6d7 100%);
    }
    main { max-width: 1180px; margin: 0 auto; padding: 42px 24px 56px; }
    .hero {
      display: grid;
      grid-template-columns: 1.15fr 0.85fr;
      gap: 28px;
      align-items: stretch;
    }
    .card {
      background: var(--panel);
      border: 1px solid var(--line);
      box-shadow: var(--shadow);
      border-radius: 28px;
      padding: 28px;
      backdrop-filter: blur(14px);
    }
    .eyebrow {
      color: var(--accent);
      font-size: 12px;
      font-weight: 800;
      letter-spacing: 0.16em;
      text-transform: uppercase;
    }
    h1 {
      margin: 12px 0 14px;
      font-family: Georgia, "Times New Roman", serif;
      font-size: clamp(42px, 7vw, 84px);
      line-height: 0.92;
      letter-spacing: -0.055em;
    }
    h2 { margin: 0 0 14px; font-size: 18px; letter-spacing: -0.02em; }
    p { color: var(--muted); line-height: 1.55; }
    textarea {
      width: 100%;
      min-height: 110px;
      resize: vertical;
      border: 1px solid #cbbf9f;
      border-radius: 20px;
      padding: 18px;
      color: var(--ink);
      background: #fffaf0;
      font: inherit;
      outline: none;
    }
    textarea:focus { border-color: var(--accent); box-shadow: 0 0 0 4px rgba(230, 107, 45, 0.16); }
    .actions { display: flex; flex-wrap: wrap; gap: 12px; margin-top: 16px; }
    button, .button {
      border: 0;
      border-radius: 999px;
      padding: 12px 18px;
      color: #fffaf0;
      background: var(--ink);
      font-weight: 800;
      cursor: pointer;
      text-decoration: none;
    }
    button.secondary { background: #6a755d; }
    button.ghost { color: var(--ink); background: #efe6d4; border: 1px solid var(--line); }
    .quick-grid { display: grid; gap: 12px; }
    .quick-grid form { margin: 0; }
    .quick-grid button { width: 100%; text-align: left; }
    .grid { display: grid; grid-template-columns: repeat(2, minmax(0, 1fr)); gap: 18px; margin-top: 22px; }
    .full { grid-column: 1 / -1; }
    .badge {
      display: inline-flex;
      align-items: center;
      border-radius: 999px;
      padding: 7px 11px;
      font-size: 12px;
      font-weight: 800;
      background: #e9dec8;
      color: var(--ink);
    }
    .badge.safe { background: #dcebdd; color: var(--safe); }
    .badge.warn { background: #f3e2bf; color: var(--warn); }
    ol, ul { padding-left: 21px; }
    li { margin: 8px 0; color: var(--muted); }
    code {
      display: block;
      overflow: auto;
      border-radius: 16px;
      padding: 14px;
      background: #211f1a;
      color: #fbf1d5;
      white-space: pre-wrap;
    }
    .metric { display: grid; gap: 4px; padding: 12px 0; border-bottom: 1px solid var(--line); }
    .metric strong { font-size: 22px; }
    .path { word-break: break-all; color: var(--ink); }
    @media (max-width: 820px) {
      .hero, .grid { grid-template-columns: 1fr; }
    }
  </style>
</head>
<body>
  <main>
    <section class="hero">
      <div class="card">
        <div class="eyebrow">ONE BRAIN Pilot / local read-only shell</div>
        <h1>Plan first. Execute only allowlisted recipes.</h1>
        <p>Write a task or use a quick action. Pilot maps intent with rules, shows a safe plan, then runs only demo/report recipes from the hard allowlist.</p>
        <form method="post" action="/plan">
          <label for="task"><strong>Task</strong></label>
          <textarea id="task" name="task" placeholder="mostrame la demo">{{Html(taskValue)}}</textarea>
          <div class="actions">
            <button type="submit">Show safe plan</button>
            <button class="secondary" type="submit" formaction="/run">Run allowlisted recipe</button>
            <a class="button ghost" href="#safety">Ver safety guarantees</a>
          </div>
        </form>
      </div>
      <div class="card">
        <h2>Quick actions</h2>
        <div class="quick-grid">
          {{QuickAction("Comparar productos demo", "mostrame la demo")}}
          {{QuickAction("Generar reporte Markdown", "quiero reporte markdown demo")}}
          {{QuickAction("Generar reporte HTML", "genera html demo")}}
          {{QuickAction("Ver safety guarantees", "ver safety guarantees")}}
        </div>
      </div>
    </section>

    <section class="grid">
      <div class="card">
        <h2>Intent status</h2>
        <div class="metric"><span>Status</span><strong>{{Html(status)}}</strong></div>
        <div class="metric"><span>Recipe</span><strong>{{Html(recipeLabel)}}</strong></div>
        <div class="metric"><span>Reason</span><strong>{{Html(plan?.Intent.Reason ?? "waiting_for_task")}}</strong></div>
      </div>

      <div id="safety" class="card">
        <h2>Safety summary</h2>
        <p><span class="badge safe">0 clicks</span> <span class="badge safe">0 cookies</span> <span class="badge safe">0 login</span> <span class="badge safe">0 carrito</span> <span class="badge safe">0 compra</span> <span class="badge safe">0 pago</span></p>
        <p>No autonomous free-agent mode. No arbitrary commands. No browser auto-open. No HTML auto-open.</p>
      </div>

      <div class="card">
        <h2>Safe plan</h2>
        {{PlanList(plan)}}
      </div>

      <div class="card">
        <h2>Blocked capabilities</h2>
        {{BlockedList(plan)}}
      </div>

      <div class="card full">
        <h2>Execution result</h2>
        <p>Exit code: <strong>{{Html(result?.ExitCode?.ToString() ?? "-")}}</strong></p>
        <p>Markdown: <span class="path">{{Html(latestMarkdown.Length == 0 ? "-" : latestMarkdown)}}</span></p>
        <p>HTML: <span class="path">{{Html(latestHtml.Length == 0 ? "-" : latestHtml)}}</span></p>
        <p>Artifacts: <span class="path">{{Html(result?.ArtifactsFolder ?? "artifacts")}}</span></p>
        {{OutputBlock(result)}}
      </div>
    </section>
  </main>
</body>
</html>
""";
    }

    private static string QuickAction(string label, string task)
    {
        return $$"""
<form method="post" action="/plan">
  <input type="hidden" name="task" value="{{Html(task)}}">
  <button type="submit" formaction="/plan">{{Html(label)}} / plan</button>
  <button type="submit" formaction="/run" class="secondary">{{Html(label)}} / run</button>
</form>
""";
    }

    private static string PlanList(PilotPlan? plan)
    {
        if (plan == null)
            return "<p>No plan yet.</p>";

        var builder = new StringBuilder("<ol>");
        foreach (var step in plan.Steps)
            builder.Append("<li>").Append(Html(step)).Append("</li>");
        builder.Append("</ol>");
        return builder.ToString();
    }

    private static string BlockedList(PilotPlan? plan)
    {
        var blocked = plan?.BlockedCapabilities ?? Array.Empty<string>();
        var builder = new StringBuilder("<ul>");
        foreach (var capability in blocked)
            builder.Append("<li>").Append(Html(capability)).Append("</li>");
        builder.Append("</ul>");
        return builder.ToString();
    }

    private static string OutputBlock(PilotExecutionResult? result)
    {
        if (result == null)
            return "<p>No recipe executed.</p>";

        var output = string.IsNullOrWhiteSpace(result.StandardError)
            ? result.StandardOutput
            : result.StandardOutput + Environment.NewLine + result.StandardError;

        if (string.IsNullOrWhiteSpace(output))
            return "<p>No process output.</p>";

        return "<code>" + Html(output.Trim()) + "</code>";
    }

    private static string Html(string? value)
    {
        return WebUtility.HtmlEncode(value ?? "");
    }
}
