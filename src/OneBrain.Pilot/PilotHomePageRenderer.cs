using System.Net;
using System.Text;
using OneBrain.Core.Approval;
using OneBrain.Core.Confidence;
using OneBrain.Core.Recording;

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
          <a class="button ghost" href="/recording/demo">Start recording demo/shadow</a>
          <a class="button ghost" href="/approvals/demo">Review approval demo</a>
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

      <div class="card">
        <h2>Observe and learn</h2>
        <p>Recording/shadow mode v0 is fixture-backed in Pilot. It shows candidate timeline and human annotations without real playback or sensitive actions.</p>
        <p><a class="button ghost" href="/recording/demo">Open recording timeline demo</a></p>
      </div>

      <div class="card">
        <h2>Approval and confidence</h2>
        <p>Review a supervised business-flow fixture. Approve/reject records an audit decision only; no send, submit, login, purchase, payment, script, or playback is executed.</p>
        <p><a class="button ghost" href="/approvals/demo">Open approval demo</a></p>
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

    public static string RenderRecordingDemo(RecipeTimeline timeline)
    {
        return $$"""
<!doctype html>
<html lang="en">
<head>
  <meta charset="utf-8">
  <meta name="viewport" content="width=device-width, initial-scale=1">
  <title>ONE BRAIN Pilot - Recording Shadow Demo</title>
  <style>
    :root { --ink: #17211a; --muted: #5c6b60; --paper: #f5f1e7; --panel: #fffaf0; --line: #d7cdb7; --accent: #e66b2d; --safe: #226b45; --risk: #8a352d; }
    body { margin: 0; color: var(--ink); font-family: "Aptos", "Segoe UI", sans-serif; background: linear-gradient(135deg, #f7f2df, #e4eadc); }
    main { max-width: 1120px; margin: 0 auto; padding: 40px 24px; }
    .card { background: rgba(255,250,240,.9); border: 1px solid var(--line); border-radius: 26px; padding: 24px; box-shadow: 0 20px 70px rgba(43,32,16,.14); margin-bottom: 18px; }
    h1 { font-family: Georgia, "Times New Roman", serif; font-size: clamp(38px, 6vw, 72px); line-height: .95; margin: 8px 0 12px; letter-spacing: -.045em; }
    h2 { margin-top: 0; }
    p, li { color: var(--muted); line-height: 1.5; }
    .badge { display: inline-block; border-radius: 999px; padding: 5px 10px; font-weight: 800; font-size: 12px; background: #eadfca; }
    .safe { color: var(--safe); background: #dcebdd; }
    .risk { color: var(--risk); background: #f6e7e6; }
    table { width: 100%; border-collapse: collapse; background: var(--panel); border-radius: 18px; overflow: hidden; }
    th, td { padding: 12px 14px; border-bottom: 1px solid var(--line); text-align: left; vertical-align: top; }
    th { font-size: 11px; text-transform: uppercase; letter-spacing: .08em; background: #efe6d4; }
    textarea, select, input { width: 100%; border: 1px solid var(--line); border-radius: 14px; padding: 10px; background: #fffdf6; font: inherit; }
    button, .button { border: 0; border-radius: 999px; padding: 11px 16px; color: #fffaf0; background: var(--ink); font-weight: 800; text-decoration: none; }
    .grid { display: grid; grid-template-columns: repeat(2, minmax(0, 1fr)); gap: 14px; }
    @media (max-width: 780px) { .grid { grid-template-columns: 1fr; } }
  </style>
</head>
<body>
  <main>
    <section class="card">
      <p><span class="badge safe">shadow mode</span> <span class="badge safe">no playback</span> <span class="badge safe">no clicks</span></p>
      <h1>Recording timeline demo</h1>
      <p>This is a local fixture/mock for the first observe-and-learn UX. It does not capture secrets, execute actions, replay actions, or generate executable recipes.</p>
      <p><a class="button" href="/">Back to Pilot</a></p>
    </section>

    <section class="card">
      <h2>Timeline</h2>
      <table>
        <thead>
          <tr><th>Step</th><th>Offset</th><th>Event</th><th>Window/app</th><th>Element</th><th>Confidence</th><th>Suggested label</th><th>Risk</th><th>Approval</th></tr>
        </thead>
        <tbody>
          {{TimelineRows(timeline)}}
        </tbody>
      </table>
    </section>

    <section class="card">
      <h2>Human annotations</h2>
      <div class="grid">
        <form method="post" action="/recording/demo/annotate">
          <label>Step number<input name="stepNumber" value="1"></label>
          <label>Annotation type
            <select name="annotationType">
              <option value="search_customer">este bloque es buscar cliente</option>
              <option value="prepare_message">este bloque es preparar mensaje</option>
              <option value="requires_approval">este paso requiere aprobacion</option>
              <option value="variable">este dato debe ser variable</option>
              <option value="ignore">este paso se puede ignorar</option>
              <option value="sensitive">este paso es sensible</option>
              <option value="free_note">nota libre</option>
            </select>
          </label>
          <label>Note<textarea name="text">nota libre</textarea></label>
          <p><button type="submit">Preview annotation</button></p>
        </form>
        <div>
          <h3>Current fixture annotations</h3>
          <ul>{{AnnotationRows(timeline)}}</ul>
        </div>
      </div>
    </section>
  </main>
</body>
</html>
""";
    }

    public static string RenderApprovalDemo(ApprovalRequest request, RecipeConfidenceProfile confidence, ApprovalDecision? decision = null)
    {
        var decisionStatus = decision == null ? "pending_decision" : decision.Decision;
        var decisionReason = decision == null ? "-" : decision.Reason;

        return $$"""
<!doctype html>
<html lang="en">
<head>
  <meta charset="utf-8">
  <meta name="viewport" content="width=device-width, initial-scale=1">
  <title>ONE BRAIN Pilot - Approval Demo</title>
  <style>
    :root { --ink: #17211a; --muted: #5c6b60; --paper: #f5f1e7; --panel: #fffaf0; --line: #d7cdb7; --accent: #e66b2d; --safe: #226b45; --risk: #8a352d; }
    body { margin: 0; color: var(--ink); font-family: "Aptos", "Segoe UI", sans-serif; background: linear-gradient(135deg, #f7f2df, #e4eadc); }
    main { max-width: 1120px; margin: 0 auto; padding: 40px 24px; }
    .card { background: rgba(255,250,240,.92); border: 1px solid var(--line); border-radius: 26px; padding: 24px; box-shadow: 0 20px 70px rgba(43,32,16,.14); margin-bottom: 18px; }
    h1 { font-family: Georgia, "Times New Roman", serif; font-size: clamp(38px, 6vw, 72px); line-height: .95; margin: 8px 0 12px; letter-spacing: -.045em; }
    h2 { margin-top: 0; }
    p, li { color: var(--muted); line-height: 1.5; }
    .badge { display: inline-block; border-radius: 999px; padding: 5px 10px; font-weight: 800; font-size: 12px; background: #eadfca; }
    .safe { color: var(--safe); background: #dcebdd; }
    .risk { color: var(--risk); background: #f6e7e6; }
    .grid { display: grid; grid-template-columns: repeat(2, minmax(0, 1fr)); gap: 14px; }
    .metric { border-bottom: 1px solid var(--line); padding: 10px 0; }
    .metric strong { display: block; font-size: 20px; color: var(--ink); }
    textarea, input { width: 100%; border: 1px solid var(--line); border-radius: 14px; padding: 10px; background: #fffdf6; font: inherit; }
    button, .button { border: 0; border-radius: 999px; padding: 11px 16px; color: #fffaf0; background: var(--ink); font-weight: 800; text-decoration: none; }
    button.reject { background: var(--risk); }
    code { display: block; white-space: pre-wrap; border-radius: 16px; padding: 14px; background: #211f1a; color: #fbf1d5; }
    @media (max-width: 780px) { .grid { grid-template-columns: 1fr; } }
  </style>
</head>
<body>
  <main>
    <section class="card">
      <p><span class="badge risk">approval required</span> <span class="badge safe">audit only</span> <span class="badge safe">no execution</span></p>
      <h1>Approval demo</h1>
      <p>This supervised business-flow fixture prepares a message preview and stops before send. Approval/rejection is recorded for audit only; no action executor runs in this hito.</p>
      <p><a class="button" href="/">Back to Pilot</a></p>
    </section>

    <section class="grid">
      <div class="card">
        <h2>Pending approval</h2>
        <div class="metric"><span>Request</span><strong>{{Html(request.ApprovalRequestId)}}</strong></div>
        <div class="metric"><span>Action kind</span><strong>{{Html(request.ActionKind)}}</strong></div>
        <div class="metric"><span>Risk</span><strong>{{Html(request.RiskLevel)}}</strong></div>
        <div class="metric"><span>Fail closed</span><strong>{{request.FailClosed}}</strong></div>
        <div class="metric"><span>Requires approval</span><strong>{{request.RequiresApproval}}</strong></div>
        <p>{{Html(request.Description)}}</p>
        <code>{{Html(request.Preview)}}</code>
      </div>

      <div class="card">
        <h2>Recipe confidence</h2>
        <div class="metric"><span>Candidate flow</span><strong>{{Html(confidence.CandidateFlowId)}}</strong></div>
        <div class="metric"><span>Status</span><strong>{{Html(confidence.Status)}}</strong></div>
        <div class="metric"><span>Confidence score</span><strong>{{confidence.ConfidenceScore}}</strong></div>
        <div class="metric"><span>Risk level</span><strong>{{Html(confidence.RiskLevel)}}</strong></div>
        <div class="metric"><span>Runs</span><strong>{{confidence.Runs}}</strong></div>
        <p>Critical candidate flows remain blocked until explicit human approval policy exists. This demo does not create an executable recipe.</p>
      </div>
    </section>

    <section class="card">
      <h2>Human decision</h2>
      <form method="post" action="/approvals/demo/decide">
        <label>Reason required for reject<textarea name="reason">No enviar todavia; requiere revision humana.</textarea></label>
        <p>
          <button type="submit" name="decision" value="approved">Approve fixture decision</button>
          <button class="reject" type="submit" name="decision" value="rejected">Reject fixture decision</button>
        </p>
      </form>
      <p>Decision status: <strong>{{Html(decisionStatus)}}</strong></p>
      <p>Decision reason: <strong>{{Html(decisionReason)}}</strong></p>
      <p><span class="badge safe">ExecutionAllowed=false</span> <span class="badge safe">0 clicks</span> <span class="badge safe">0 cookies</span> <span class="badge safe">0 login</span> <span class="badge safe">0 cart</span> <span class="badge safe">0 purchase</span> <span class="badge safe">0 payment</span></p>
    </section>
  </main>
</body>
</html>
""";
    }

    private static string TimelineRows(RecipeTimeline timeline)
    {
        var builder = new StringBuilder();
        foreach (var step in timeline.Steps)
        {
            var riskClass = step.RiskLevel == "high" ? "risk" : "safe";
            builder.Append("<tr>")
                .Append("<td>").Append(step.StepNumber).Append("</td>")
                .Append("<td>").Append(step.OffsetMs).Append("ms</td>")
                .Append("<td>").Append(Html(step.EventType)).Append("</td>")
                .Append("<td>").Append(Html(step.WindowOrApp)).Append("</td>")
                .Append("<td>").Append(Html(step.ElementSummary)).Append("</td>")
                .Append("<td>").Append(step.Confidence.ToString("0.00")).Append("</td>")
                .Append("<td>").Append(Html(step.SuggestedActionLabel)).Append("</td>")
                .Append("<td><span class=\"badge ").Append(riskClass).Append("\">").Append(Html(step.RiskLevel)).Append("</span></td>")
                .Append("<td>").Append(step.RequiresApproval ? "true" : "false").Append("</td>")
                .Append("</tr>");
        }

        return builder.ToString();
    }

    private static string AnnotationRows(RecipeTimeline timeline)
    {
        var builder = new StringBuilder();
        foreach (var annotation in timeline.Annotations)
        {
            builder.Append("<li>")
                .Append(Html(annotation.AnnotationType))
                .Append(" step ")
                .Append(Html(annotation.StepNumber?.ToString() ?? "-"))
                .Append(": ")
                .Append(Html(annotation.Text))
                .Append("</li>");
        }

        return builder.Length == 0 ? "<li>No annotations yet.</li>" : builder.ToString();
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
