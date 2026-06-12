using System.Net;
using System.Text;
using OneBrain.Core.AI;
using OneBrain.Core.AppProfiles;
using OneBrain.Core.Approval;
using OneBrain.Core.Confidence;
using OneBrain.Core.History;
using OneBrain.Core.Memory;
using OneBrain.Core.Recording;
using OneBrain.Core.Recipes.Editing;

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
    {{PilotChrome("Home")}}
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
          <a class="button ghost" href="/ai/config">AI model router config</a>
          <a class="button ghost" href="/runs">Execution history</a>
          <a class="button ghost" href="/ai/audit">AI audit log</a>
          <a class="button ghost" href="/recipes">Recipe editor</a>
          <a class="button ghost" href="/variables">Variable manager</a>
          <a class="button ghost" href="/memory">Process memory</a>
          <a class="button ghost" href="/app-profiles">App profiles</a>
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

      <div class="card">
        <h2>AI model router</h2>
        <p>Configure OpenAI profile routing centrally. Pilot shows masked secrets and dry-run routing only; no provider call is made in this hito.</p>
        <p><a class="button ghost" href="/ai/config">Open AI config console</a></p>
      </div>

      <div class="card">
        <h2>History and audit</h2>
        <p>Browse local run history and AI routing audit decisions. Runtime artifacts remain local under artifacts/ and are not committed.</p>
        <p><a class="button ghost" href="/runs">Open run history</a> <a class="button ghost" href="/ai/audit">Open AI audit</a></p>
      </div>

      <div class="card">
        <h2>Recipes and variables</h2>
        <p>Inspect allowlisted recipes, create safe metadata drafts, review variables, and run linter checks before any promotion. Stable recipe JSON is not overwritten from Pilot.</p>
        <p><a class="button ghost" href="/recipes">Open recipe editor</a> <a class="button ghost" href="/variables">Open variable manager</a></p>
      </div>

      <div class="card">
        <h2>Process memory</h2>
        <p>Search learned/observed processes by text, tags, app, domain, status, and confidence. Retrieval is deterministic and suggestions never execute actions.</p>
        <p><a class="button ghost" href="/memory">Open process memory</a></p>
      </div>

      <div class="card">
        <h2>App profiles</h2>
        <p>Inspect safe app/site profiles, capabilities, risk policy, and external-fragile diagnostics. Profiles do not enable clicks, login, cookies, purchase, or payment by default.</p>
        <p><a class="button ghost" href="/app-profiles">Open app profiles</a></p>
      </div>

      <div class="card full">
        <h2>Execution result</h2>
        {{ExecutionStatusBlock(result)}}
        {{CopyPathField("Markdown report path", latestMarkdown)}}
        {{CopyPathField("HTML report path", latestHtml)}}
        {{CopyPathField("Artifacts folder", result?.ArtifactsFolder ?? "artifacts")}}
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
    {{PilotChrome("Recording")}}
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
    {{PilotChrome("Approvals")}}
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

    public static string RenderAIConfigConsole(IReadOnlyList<AIModelProfile> profiles, AIModelRouterResult? testResult = null)
    {
        return $$"""
<!doctype html>
<html lang="en">
<head>
  <meta charset="utf-8">
  <meta name="viewport" content="width=device-width, initial-scale=1">
  <title>ONE BRAIN Pilot - AI Config</title>
  <style>
    :root { --ink: #17211a; --muted: #5c6b60; --paper: #f5f1e7; --panel: #fffaf0; --line: #d7cdb7; --safe: #226b45; --risk: #8a352d; }
    body { margin: 0; color: var(--ink); font-family: "Aptos", "Segoe UI", sans-serif; background: linear-gradient(135deg, #f7f2df, #e4eadc); }
    main { max-width: 1180px; margin: 0 auto; padding: 40px 24px; }
    .card { background: rgba(255,250,240,.92); border: 1px solid var(--line); border-radius: 26px; padding: 24px; box-shadow: 0 20px 70px rgba(43,32,16,.14); margin-bottom: 18px; }
    h1 { font-family: Georgia, "Times New Roman", serif; font-size: clamp(38px, 6vw, 72px); line-height: .95; margin: 8px 0 12px; letter-spacing: -.045em; }
    p, li { color: var(--muted); line-height: 1.5; }
    table { width: 100%; border-collapse: collapse; background: var(--panel); border-radius: 18px; overflow: hidden; }
    th, td { padding: 11px 12px; border-bottom: 1px solid var(--line); text-align: left; vertical-align: top; }
    th { font-size: 11px; text-transform: uppercase; letter-spacing: .08em; background: #efe6d4; }
    .badge { display: inline-block; border-radius: 999px; padding: 5px 10px; font-weight: 800; font-size: 12px; background: #eadfca; }
    .safe { color: var(--safe); background: #dcebdd; }
    .risk { color: var(--risk); background: #f6e7e6; }
    button, .button { border: 0; border-radius: 999px; padding: 11px 16px; color: #fffaf0; background: var(--ink); font-weight: 800; text-decoration: none; }
    code { display: block; white-space: pre-wrap; border-radius: 16px; padding: 14px; background: #211f1a; color: #fbf1d5; }
  </style>
</head>
<body>
  <main>
    {{PilotChrome("AI Config")}}
    <section class="card">
      <p><span class="badge safe">OpenAI primary</span> <span class="badge safe">central router</span> <span class="badge safe">masked secrets</span> <span class="badge safe">dry-run only</span></p>
      <h1>AI model router config</h1>
      <p>All future provider calls must go through OneBrain.AI.ModelRouter. This console reads local environment configuration, masks keys, and runs deterministic routing tests without calling OpenAI.</p>
      <p><a class="button" href="/">Back to Pilot</a></p>
    </section>

    <section class="card">
      <h2>Official profiles</h2>
      <table>
        <thead>
          <tr><th>Profile</th><th>Provider</th><th>Model</th><th>Secret</th><th>Enabled</th><th>Monthly budget</th><th>Per task</th><th>Max risk</th><th>Fallback</th><th>Status</th><th>Usage logging</th></tr>
        </thead>
        <tbody>
          {{AIProfileRows(profiles)}}
        </tbody>
      </table>
    </section>

    <section class="card">
      <h2>Test configuration</h2>
      <form method="post" action="/ai/config/test">
        <p><button type="submit">Run dry-run routing test</button></p>
      </form>
      {{AIRoutingResultBlock(testResult)}}
      <p>No API key is displayed in full. Missing model/key/config fails closed before any future provider call.</p>
    </section>
  </main>
</body>
</html>
""";
    }

    public static string RenderRunHistory(IReadOnlyList<RunHistoryRecord> runs)
    {
        return $$"""
<!doctype html>
<html lang="en">
<head>
  <meta charset="utf-8">
  <meta name="viewport" content="width=device-width, initial-scale=1">
  <title>ONE BRAIN Pilot - Run History</title>
  <style>
    :root { --ink: #17211a; --muted: #5c6b60; --paper: #f5f1e7; --panel: #fffaf0; --line: #d7cdb7; --safe: #226b45; --risk: #8a352d; --warn: #9a5a10; }
    body { margin: 0; color: var(--ink); font-family: "Aptos", "Segoe UI", sans-serif; background: linear-gradient(135deg, #f7f2df, #e4eadc); }
    main { max-width: 1220px; margin: 0 auto; padding: 40px 24px; }
    .card { background: rgba(255,250,240,.92); border: 1px solid var(--line); border-radius: 26px; padding: 24px; box-shadow: 0 20px 70px rgba(43,32,16,.14); margin-bottom: 18px; }
    h1 { font-family: Georgia, "Times New Roman", serif; font-size: clamp(38px, 6vw, 72px); line-height: .95; margin: 8px 0 12px; letter-spacing: -.045em; }
    p, li { color: var(--muted); line-height: 1.5; }
    table { width: 100%; border-collapse: collapse; background: var(--panel); border-radius: 18px; overflow: hidden; }
    th, td { padding: 11px 12px; border-bottom: 1px solid var(--line); text-align: left; vertical-align: top; }
    th { font-size: 11px; text-transform: uppercase; letter-spacing: .08em; background: #efe6d4; }
    .badge { display: inline-block; border-radius: 999px; padding: 5px 10px; font-weight: 800; font-size: 12px; background: #eadfca; }
    .safe { color: var(--safe); background: #dcebdd; }
    .risk { color: var(--risk); background: #f6e7e6; }
    .warn { color: var(--warn); background: #f3e2bf; }
    .path { word-break: break-all; }
    .button { display: inline-block; border-radius: 999px; padding: 11px 16px; color: #fffaf0; background: var(--ink); font-weight: 800; text-decoration: none; }
  </style>
</head>
<body>
  <main>
    {{PilotChrome("Runs")}}
    <section class="card">
      <p><span class="badge safe">runtime artifacts local only</span> <span class="badge safe">no secrets stored</span> <span class="badge safe">0 clicks baseline</span></p>
      <h1>Execution history</h1>
      <p>Recent local runs with recipe/flow identifiers, safety counters, linked artifacts, approval/confidence/AI references, and sanitized errors. If no runtime history exists, Pilot shows safe fixture examples.</p>
      <p><a class="button" href="/">Back to Pilot</a></p>
    </section>

    <section class="card">
      <h2>Recent runs</h2>
      <table>
        <thead>
          <tr><th>Run</th><th>Status</th><th>Time</th><th>Source</th><th>Recipe / flow</th><th>Safety</th><th>Approval</th><th>Confidence / AI</th><th>Artifacts</th><th>Error</th></tr>
        </thead>
        <tbody>
          {{RunHistoryRows(runs)}}
        </tbody>
      </table>
    </section>
  </main>
</body>
</html>
""";
    }

    public static string RenderAIAuditLog(IReadOnlyList<AIAuditRecord> audits)
    {
        return $$"""
<!doctype html>
<html lang="en">
<head>
  <meta charset="utf-8">
  <meta name="viewport" content="width=device-width, initial-scale=1">
  <title>ONE BRAIN Pilot - AI Audit</title>
  <style>
    :root { --ink: #17211a; --muted: #5c6b60; --paper: #f5f1e7; --panel: #fffaf0; --line: #d7cdb7; --safe: #226b45; --risk: #8a352d; --warn: #9a5a10; }
    body { margin: 0; color: var(--ink); font-family: "Aptos", "Segoe UI", sans-serif; background: linear-gradient(135deg, #f7f2df, #e4eadc); }
    main { max-width: 1220px; margin: 0 auto; padding: 40px 24px; }
    .card { background: rgba(255,250,240,.92); border: 1px solid var(--line); border-radius: 26px; padding: 24px; box-shadow: 0 20px 70px rgba(43,32,16,.14); margin-bottom: 18px; }
    h1 { font-family: Georgia, "Times New Roman", serif; font-size: clamp(38px, 6vw, 72px); line-height: .95; margin: 8px 0 12px; letter-spacing: -.045em; }
    p, li { color: var(--muted); line-height: 1.5; }
    table { width: 100%; border-collapse: collapse; background: var(--panel); border-radius: 18px; overflow: hidden; }
    th, td { padding: 11px 12px; border-bottom: 1px solid var(--line); text-align: left; vertical-align: top; }
    th { font-size: 11px; text-transform: uppercase; letter-spacing: .08em; background: #efe6d4; }
    .badge { display: inline-block; border-radius: 999px; padding: 5px 10px; font-weight: 800; font-size: 12px; background: #eadfca; }
    .safe { color: var(--safe); background: #dcebdd; }
    .risk { color: var(--risk); background: #f6e7e6; }
    .warn { color: var(--warn); background: #f3e2bf; }
    .button { display: inline-block; border-radius: 999px; padding: 11px 16px; color: #fffaf0; background: var(--ink); font-weight: 800; text-decoration: none; }
  </style>
</head>
<body>
  <main>
    {{PilotChrome("AI Audit")}}
    <section class="card">
      <p><span class="badge safe">no provider call</span> <span class="badge safe">no prompts stored by default</span> <span class="badge safe">no API keys</span></p>
      <h1>AI usage audit</h1>
      <p>Audit entries explain which profile was recommended or used, why routing happened, whether fallback/budget applied, whether a step requires approval, and whether the decision failed closed. This hito records mock/local decisions only.</p>
      <p><a class="button" href="/">Back to Pilot</a> <a class="button" href="/ai/config">AI config</a></p>
    </section>

    <section class="card">
      <h2>AI routing decisions</h2>
      <table>
        <thead>
          <tr><th>Audit</th><th>Profile</th><th>Provider/model</th><th>Task/risk</th><th>Vision</th><th>Human approval</th><th>Fallback</th><th>Budget</th><th>Cost/tokens</th><th>Status/reason</th></tr>
        </thead>
        <tbody>
          {{AIAuditRows(audits)}}
        </tbody>
      </table>
    </section>
  </main>
</body>
</html>
""";
    }

    public static string RenderRecipeList(IReadOnlyList<RecipeEditorModel> recipes, string? message = null)
    {
        return $$"""
<!doctype html>
<html lang="en">
<head>
  <meta charset="utf-8">
  <meta name="viewport" content="width=device-width, initial-scale=1">
  <title>ONE BRAIN Pilot - Recipes</title>
  {{SharedPilotStyle()}}
</head>
<body>
  <main>
    {{PilotChrome("Recipes")}}
    <section class="card">
      <p><span class="badge safe">allowlist only</span> <span class="badge safe">drafts only</span> <span class="badge safe">no arbitrary commands</span></p>
      <h1>Recipe editor</h1>
      <p>Inspect allowlisted recipes and create safe metadata drafts. Pilot does not overwrite stable recipe JSON and does not execute from this editor.</p>
      <p><a class="button" href="/">Back to Pilot</a> <a class="button" href="/variables">Variable manager</a></p>
      {{MessageBlock(message)}}
    </section>
    <section class="card">
      <h2>Allowlisted recipes</h2>
      <table>
        <thead><tr><th>Recipe</th><th>Risk</th><th>Confidence</th><th>Path</th><th>Steps</th><th>Actions</th></tr></thead>
        <tbody>{{RecipeListRows(recipes)}}</tbody>
      </table>
    </section>
  </main>
</body>
</html>
""";
    }

    public static string RenderRecipeDetail(
        RecipeEditorModel recipe,
        RecipeValidationResult validation,
        IReadOnlyList<RecipeVariableDefinition> variables,
        RecipeDraft? draft = null,
        RecipeDraftArtifactWriteResult? draftWrite = null)
    {
        return $$"""
<!doctype html>
<html lang="en">
<head>
  <meta charset="utf-8">
  <meta name="viewport" content="width=device-width, initial-scale=1">
  <title>ONE BRAIN Pilot - Recipe Detail</title>
  {{SharedPilotStyle()}}
</head>
<body>
  <main>
    {{PilotChrome("Recipe Detail")}}
    <section class="card">
      <p><span class="badge safe">safe fields only</span> <span class="badge safe">draft artifact</span> <span class="badge risk">no action edit</span></p>
      <h1>{{Html(recipe.Title)}}</h1>
      <p>{{Html(recipe.Description)}}</p>
      <p><a class="button" href="/recipes">Back to recipes</a> <a class="button" href="/recipes/{{Html(recipe.RecipeId)}}/variables">Recipe variables</a></p>
      {{DraftResultBlock(draft, draftWrite)}}
    </section>
    <section class="grid">
      <div class="card">
        <h2>Metadata</h2>
        <div class="metric"><span>Recipe ID</span><strong>{{Html(recipe.RecipeId)}}</strong></div>
        <div class="metric"><span>Risk level</span><strong>{{Html(recipe.RiskLevel)}}</strong></div>
        <div class="metric"><span>Confidence</span><strong>{{Html(recipe.ConfidenceStatus)}}</strong></div>
        <div class="metric"><span>Path</span><strong class="path">{{Html(recipe.RecipePath)}}</strong></div>
      </div>
      <div class="card">
        <h2>Safe edit draft</h2>
        <form method="post" action="/recipes/{{Html(recipe.RecipeId)}}/edit">
          <label>Title<input name="title" value="{{Html(recipe.Title)}}"></label>
          <label>Description<textarea name="description">{{Html(recipe.Description)}}</textarea></label>
          <label>Tags CSV<input name="tags" value="{{Html(string.Join(", ", recipe.Tags))}}"></label>
          <label>Notes CSV<input name="notes" value="{{Html(string.Join(", ", recipe.Notes))}}"></label>
          <input type="hidden" name="unsafe.kind" value="">
          <p><button type="submit">Save draft candidate</button></p>
        </form>
        <p>Fields like step kind, args, paths, commands, browser actions, login, cookies, payment, purchase, submit and click are not editable freely.</p>
      </div>
    </section>
    <section class="card">
      <h2>Human-readable steps</h2>
      <table>
        <thead><tr><th>#</th><th>Step ID</th><th>Kind</th><th>Label</th><th>Risk</th><th>Approval</th></tr></thead>
        <tbody>{{RecipeStepRows(recipe.Steps)}}</tbody>
      </table>
    </section>
    <section class="grid">
      <div class="card">
        <h2>Validation result</h2>
        <p>Can run: <strong>{{validation.CanRun}}</strong> / Can promote: <strong>{{validation.CanPromote}}</strong></p>
        <table>
          <thead><tr><th>Severity</th><th>Code</th><th>Field</th><th>Message</th><th>Remediation</th></tr></thead>
          <tbody>{{ValidationRows(validation.Issues)}}</tbody>
        </table>
      </div>
      <div class="card">
        <h2>Variables</h2>
        <table>
          <thead><tr><th>Name</th><th>Type</th><th>Required</th><th>Sensitivity</th><th>Value</th></tr></thead>
          <tbody>{{VariableRows(variables)}}</tbody>
        </table>
      </div>
    </section>
  </main>
</body>
</html>
""";
    }

    public static string RenderVariables(IReadOnlyList<RecipeVariableDefinition> variables, string? title = null)
    {
        return $$"""
<!doctype html>
<html lang="en">
<head>
  <meta charset="utf-8">
  <meta name="viewport" content="width=device-width, initial-scale=1">
  <title>ONE BRAIN Pilot - Variables</title>
  {{SharedPilotStyle()}}
</head>
<body>
  <main>
    {{PilotChrome("Variables")}}
    <section class="card">
      <p><span class="badge safe">no execution</span> <span class="badge safe">masked sensitive values</span> <span class="badge risk">no normal secrets</span></p>
      <h1>{{Html(title ?? "Variable manager")}}</h1>
      <p>Inspect variables used by allowlisted recipes and candidate flows. Secret variables are never shown in full and should use environment/secret references, not plain recipe values.</p>
      <p><a class="button" href="/">Back to Pilot</a> <a class="button" href="/recipes">Recipe editor</a></p>
    </section>
    <section class="card">
      <h2>Variables</h2>
      <table>
        <thead><tr><th>Name</th><th>Type</th><th>Required</th><th>Default/example</th><th>Sensitivity</th><th>Rules</th></tr></thead>
        <tbody>{{VariableManagerRows(variables)}}</tbody>
      </table>
    </section>
  </main>
</body>
</html>
""";
    }

    public static string RenderProcessMemory(IReadOnlyList<ProcessMemoryEntry> entries, WorkflowRetrievalResult retrieval)
    {
        return $$"""
<!doctype html>
<html lang="en">
<head>
  <meta charset="utf-8">
  <meta name="viewport" content="width=device-width, initial-scale=1">
  <title>ONE BRAIN Pilot - Process Memory</title>
  {{SharedPilotStyle()}}
</head>
<body>
  <main>
    {{PilotChrome("Memory")}}
    <section class="card">
      <p><span class="badge safe">retrieval only</span> <span class="badge safe">no execution</span> <span class="badge safe">no embeddings</span> <span class="badge safe">no OpenAI call</span></p>
      <h1>Process memory</h1>
      <p>Search learned or observed processes by text, tag, app/site, domain, status, and confidence. This v0 uses deterministic scoring only and never executes a workflow.</p>
      <p><a class="button" href="/">Back to Pilot</a> <a class="button" href="/app-profiles">App profiles</a></p>
      <form method="get" action="/memory/search" class="grid">
        <label>Text<input name="q" value="{{Html(retrieval.Query.Text)}}"></label>
        <label>Tag<input name="tag" value="{{Html(string.Join(',', retrieval.Query.Tags ?? []))}}"></label>
        <label>App/site<input name="appOrSite" value="{{Html(retrieval.Query.AppOrSite)}}"></label>
        <label>Domain<input name="domain" value="{{Html(retrieval.Query.Domain)}}"></label>
        <label>Status<input name="status" value="{{Html(retrieval.Query.Status)}}"></label>
        <p><button type="submit">Search memory</button></p>
      </form>
    </section>
    <section class="grid">
      <div class="card">
        <h2>Memory entries</h2>
        <table>
          <thead><tr><th>Process</th><th>Status</th><th>App/site</th><th>Domain</th><th>Risk</th><th>Confidence</th><th>Tags</th></tr></thead>
          <tbody>{{ProcessMemoryRows(entries)}}</tbody>
        </table>
      </div>
      <div class="card">
        <h2>Retrieval matches</h2>
        <table>
          <thead><tr><th>Match</th><th>Score</th><th>Safe</th><th>Review</th><th>Links</th><th>Reasons</th></tr></thead>
          <tbody>{{WorkflowRetrievalRows(retrieval.Matches)}}</tbody>
        </table>
      </div>
    </section>
  </main>
</body>
</html>
""";
    }

    public static string RenderProcessMemoryDetail(ProcessMemoryEntry? entry)
    {
        if (entry == null)
            return RenderNotFound("Process memory entry not found", "/memory", "Back to memory");

        return $$"""
<!doctype html>
<html lang="en">
<head>
  <meta charset="utf-8">
  <meta name="viewport" content="width=device-width, initial-scale=1">
  <title>ONE BRAIN Pilot - Process Memory Detail</title>
  {{SharedPilotStyle()}}
</head>
<body>
  <main>
    {{PilotChrome("Memory Detail")}}
    <section class="card">
      <p><span class="badge safe">memory detail</span> <span class="badge safe">no execution</span> <span class="badge warn">human review for risky suggestions</span></p>
      <h1>{{Html(entry.Title)}}</h1>
      <p>{{Html(entry.Description)}}</p>
      <p><a class="button" href="/memory">Back to memory</a></p>
    </section>
    <section class="grid">
      <div class="card">
        <h2>Metadata</h2>
        <div class="metric"><span>ID</span><strong>{{Html(entry.Id)}}</strong></div>
        <div class="metric"><span>Status</span><strong>{{Html(entry.Status)}}</strong></div>
        <div class="metric"><span>Source</span><strong>{{Html(entry.Source)}}</strong></div>
        <div class="metric"><span>App/site</span><strong>{{Html(entry.AppOrSite)}}</strong></div>
        <div class="metric"><span>Domain</span><strong>{{Html(entry.Domain)}}</strong></div>
        <div class="metric"><span>Risk / confidence</span><strong>{{Html(entry.RiskLevel)}} / {{entry.ConfidenceScore}}</strong></div>
      </div>
      <div class="card">
        <h2>Links</h2>
        <ul>{{ProcessMemoryLinkRows(entry)}}</ul>
      </div>
    </section>
    <section class="grid">
      <div class="card">
        <h2>Step summary</h2>
        <ul>{{ListRows(entry.Summary.StepSummaries)}}</ul>
      </div>
      <div class="card">
        <h2>Evidence</h2>
        <ul>{{ProcessMemoryEvidenceRows(entry)}}</ul>
      </div>
    </section>
  </main>
</body>
</html>
""";
    }

    public static string RenderAppProfiles(IReadOnlyList<AppProfile> profiles)
    {
        return $$"""
<!doctype html>
<html lang="en">
<head>
  <meta charset="utf-8">
  <meta name="viewport" content="width=device-width, initial-scale=1">
  <title>ONE BRAIN Pilot - App Profiles</title>
  {{SharedPilotStyle()}}
</head>
<body>
  <main>
    {{PilotChrome("App Profiles")}}
    <section class="card">
      <p><span class="badge safe">profile manager v0</span> <span class="badge safe">no execution</span> <span class="badge risk">login/cookies/payment/purchase blocked</span></p>
      <h1>App profile manager</h1>
      <p>Inspect safe app/site profile fixtures and runtime profile artifacts. Profile changes do not enable unsafe actions by default and external-fragile profiles require diagnosticAllowed.</p>
      <p><a class="button" href="/">Back to Pilot</a> <a class="button" href="/memory">Process memory</a></p>
    </section>
    <section class="card">
      <h2>Profiles</h2>
      <table>
        <thead><tr><th>Profile</th><th>Kind</th><th>Status</th><th>Domain/process</th><th>Capabilities</th><th>Policy</th><th>Validation</th></tr></thead>
        <tbody>{{AppProfileRows(profiles)}}</tbody>
      </table>
    </section>
  </main>
</body>
</html>
""";
    }

    public static string RenderAppProfileDetail(AppProfile? profile)
    {
        if (profile == null)
            return RenderNotFound("App profile not found", "/app-profiles", "Back to app profiles");

        var validation = AppProfilePolicy.Validate(profile);
        return $$"""
<!doctype html>
<html lang="en">
<head>
  <meta charset="utf-8">
  <meta name="viewport" content="width=device-width, initial-scale=1">
  <title>ONE BRAIN Pilot - App Profile Detail</title>
  {{SharedPilotStyle()}}
</head>
<body>
  <main>
    {{PilotChrome("App Profile Detail")}}
    <section class="card">
      <p><span class="badge safe">read-only by default</span> <span class="badge safe">diagnostic policy visible</span> <span class="badge risk">unsafe actions blocked</span></p>
      <h1>{{Html(profile.Name)}}</h1>
      <p><a class="button" href="/app-profiles">Back to app profiles</a></p>
    </section>
    <section class="grid">
      <div class="card">
        <h2>Profile</h2>
        <div class="metric"><span>ID</span><strong>{{Html(profile.Id)}}</strong></div>
        <div class="metric"><span>Kind/status</span><strong>{{Html(profile.Kind)}} / {{Html(profile.Status)}}</strong></div>
        <div class="metric"><span>Domain</span><strong>{{Html(profile.SiteDomain ?? "-")}}</strong></div>
        <div class="metric"><span>Process</span><strong>{{Html(profile.ProcessName ?? "-")}}</strong></div>
        <div class="metric"><span>Version</span><strong>{{profile.Version.Version}} / {{Html(profile.Version.Status)}}</strong></div>
      </div>
      <div class="card">
        <h2>Risk policy</h2>
        <ul>
          <li>Read-only by default: {{profile.RiskPolicy.ReadOnlyByDefault}}</li>
          <li>Diagnostic allowed: {{profile.RiskPolicy.DiagnosticAllowed}}</li>
          <li>Submit requires approval: {{profile.RiskPolicy.RequiresApprovalForSubmit}}</li>
          <li>Blocks login: {{profile.RiskPolicy.BlocksLogin}}</li>
          <li>Blocks cookies: {{profile.RiskPolicy.BlocksCookies}}</li>
          <li>Blocks payment: {{profile.RiskPolicy.BlocksPayment}}</li>
          <li>Blocks purchase: {{profile.RiskPolicy.BlocksPurchase}}</li>
          <li>Allows safe click: {{profile.RiskPolicy.AllowsSafeClick}}</li>
        </ul>
      </div>
    </section>
    <section class="grid">
      <div class="card">
        <h2>Capabilities</h2>
        <ul>{{ListRows(profile.SupportedCapabilities)}}</ul>
      </div>
      <div class="card">
        <h2>Validation</h2>
        <p>Can activate: <strong>{{validation.CanActivate}}</strong> / Requires validation before promotion: <strong>{{validation.RequiresValidationBeforePromotion}}</strong></p>
        <table>
          <thead><tr><th>Severity</th><th>Code</th><th>Message</th><th>Remediation</th></tr></thead>
          <tbody>{{AppProfileIssueRows(validation.Issues)}}</tbody>
        </table>
      </div>
    </section>
  </main>
</body>
</html>
""";
    }

    private static string RenderNotFound(string title, string backPath, string backLabel)
    {
        return $$"""
<!doctype html>
<html lang="en">
<head>
  <meta charset="utf-8">
  <meta name="viewport" content="width=device-width, initial-scale=1">
  <title>ONE BRAIN Pilot - Not Found</title>
  {{SharedPilotStyle()}}
</head>
<body>
  <main>
    {{PilotChrome("Not Found")}}
    <section class="card">
      <p><span class="badge warn">not found</span> <span class="badge safe">no execution</span></p>
      <h1>{{Html(title)}}</h1>
      <p>The requested Pilot item does not exist in the current local fixtures or runtime artifacts.</p>
      <p><a class="button" href="{{Html(backPath)}}">{{Html(backLabel)}}</a> <a class="button" href="/">Back to Pilot</a></p>
    </section>
  </main>
</body>
</html>
""";
    }

    private static string PilotChrome(string currentPage)
    {
        return $$"""
<section class="card" aria-label="Pilot navigation and safety">
  <p><span class="badge safe">Current: {{Html(currentPage)}}</span> <span class="badge safe">local UI</span> <span class="badge safe">no auto-open</span></p>
  <nav style="display:flex;flex-wrap:wrap;gap:10px;margin:10px 0 14px">
    <a class="button" href="/">Home</a>
    <a class="button" href="/recipes">Recipes</a>
    <a class="button" href="/variables">Variables</a>
    <a class="button" href="/memory">Memory</a>
    <a class="button" href="/app-profiles">App profiles</a>
    <a class="button" href="/approvals/demo">Approvals</a>
    <a class="button" href="/runs">Runs</a>
    <a class="button" href="/ai/config">AI config</a>
    <a class="button" href="/ai/audit">AI audit</a>
  </nav>
  {{SafetyAlwaysVisible()}}
</section>
""";
    }

    private static string SafetyAlwaysVisible()
    {
        return """
<p aria-label="Always visible safety summary">
  <span class="badge safe">Safety always visible</span>
  <span class="badge safe">0 clicks</span>
  <span class="badge safe">0 cookies accepted</span>
  <span class="badge safe">0 login</span>
  <span class="badge safe">0 carrito</span>
  <span class="badge safe">0 compra</span>
  <span class="badge safe">0 pago</span>
</p>
""";
    }

    private static string ExecutionStatusBlock(PilotExecutionResult? result)
    {
        if (result == null)
            return "<p><span class=\"badge warn\">No UI flow executed yet</span> Choose a safe quick action or submit a task.</p>";

        var badgeClass = result.Success ? "safe" : "risk";
        var status = result.Success ? "OK" : "failed";
        return $$"""
<p><span class="badge {{badgeClass}}">Status {{Html(status)}}</span> <strong>{{Html(result.Status)}}</strong></p>
<p>Recipe: <span class="path">{{Html(result.RecipePath ?? "-")}}</span></p>
<p>Exit code: <strong>{{Html(result.ExitCode?.ToString() ?? "-")}}</strong></p>
""";
    }

    private static string CopyPathField(string label, string? value)
    {
        var displayValue = string.IsNullOrWhiteSpace(value) ? "-" : value;
        return $$"""
<label>{{Html(label)}} <small>Select and copy manually; Pilot never opens files automatically.</small>
  <input readonly value="{{Html(displayValue)}}">
</label>
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

    private static string AIProfileRows(IReadOnlyList<AIModelProfile> profiles)
    {
        var builder = new StringBuilder();
        foreach (var profile in profiles)
        {
            var status = BuildAIProfileStatus(profile);
            var statusClass = status == "configured" ? "safe" : "risk";
            builder.Append("<tr>")
                .Append("<td>").Append(Html(profile.DisplayName)).Append("<br><small>").Append(Html(profile.ProfileId)).Append("</small></td>")
                .Append("<td>").Append(Html(profile.Provider)).Append("</td>")
                .Append("<td>").Append(Html(string.IsNullOrWhiteSpace(profile.Model) ? "[not configured]" : profile.Model)).Append("</td>")
                .Append("<td>").Append(Html(profile.ApiKeyMasked)).Append("<br><small>").Append(Html(profile.ApiKeySecretName)).Append("</small></td>")
                .Append("<td>").Append(profile.Enabled ? "enabled" : "disabled").Append("</td>")
                .Append("<td>").Append(profile.MonthlyBudgetUsd.ToString("0.00")).Append("</td>")
                .Append("<td>").Append(profile.MaxCostPerTaskUsd.ToString("0.00")).Append("</td>")
                .Append("<td>").Append(Html(profile.MaxRiskLevel)).Append("</td>")
                .Append("<td>").Append(Html(string.IsNullOrWhiteSpace(profile.FallbackProfileId) ? "-" : profile.FallbackProfileId)).Append("</td>")
                .Append("<td><span class=\"badge ").Append(statusClass).Append("\">").Append(Html(status)).Append("</span></td>")
                .Append("<td>").Append(profile.UsageLoggingEnabled ? "enabled" : "disabled").Append("</td>")
                .Append("</tr>");
        }

        return builder.ToString();
    }

    private static string AIRoutingResultBlock(AIModelRouterResult? result)
    {
        if (result == null)
            return "<p>No dry-run test executed.</p>";

        return "<code>" + Html($"""
status={result.Decision.Status}
success={result.Decision.Success}
selectedProfile={result.Decision.SelectedProfileId ?? "-"}
reason={result.Decision.Reason}
failClosed={result.Decision.FailClosed}
wouldCallProvider={result.Decision.WouldCallProvider}
""") + "</code>";
    }

    private static string RunHistoryRows(IReadOnlyList<RunHistoryRecord> runs)
    {
        if (runs.Count == 0)
            return "<tr><td colspan=\"10\">No run history found.</td></tr>";

        var builder = new StringBuilder();
        foreach (var run in runs)
        {
            var statusClass = run.Status is "succeeded" or "diagnostic" ? "safe" : run.Status == "blocked" ? "warn" : "risk";
            builder.Append("<tr>")
                .Append("<td>").Append(Html(run.RunId)).Append("</td>")
                .Append("<td><span class=\"badge ").Append(statusClass).Append("\">").Append(Html(run.Status)).Append("</span></td>")
                .Append("<td>").Append(Html(run.StartedAtUtc)).Append("<br>").Append(Html(run.EndedAtUtc ?? "-")).Append("</td>")
                .Append("<td>").Append(Html(run.Source)).Append("</td>")
                .Append("<td>").Append(Html(run.RecipeId ?? run.CandidateFlowId ?? "-")).Append("</td>")
                .Append("<td>").Append(Safety(run.SafetyCounters)).Append("</td>")
                .Append("<td>").Append(Html(run.ApprovalDecisionId ?? run.ApprovalRequestId ?? "-")).Append("</td>")
                .Append("<td>").Append(Html(run.ConfidenceId ?? "-")).Append("<br>").Append(Html(run.AiRoutingDecisionId ?? "-")).Append("</td>")
                .Append("<td class=\"path\">").Append(Html(string.Join("\n", run.ArtifactPaths))).Append("</td>")
                .Append("<td>").Append(Html(string.IsNullOrWhiteSpace(run.ErrorSummary) ? "-" : run.ErrorSummary)).Append("</td>")
                .Append("</tr>");
        }

        return builder.ToString();
    }

    private static string AIAuditRows(IReadOnlyList<AIAuditRecord> audits)
    {
        if (audits.Count == 0)
            return "<tr><td colspan=\"10\">No AI audit logs found.</td></tr>";

        var builder = new StringBuilder();
        foreach (var audit in audits)
        {
            var statusClass = audit.ResultStatus is "routed" or "mocked" ? "safe" : "risk";
            builder.Append("<tr>")
                .Append("<td>").Append(Html(audit.AiAuditId)).Append("<br>").Append(Html(audit.TimestampUtc)).Append("</td>")
                .Append("<td>").Append(Html(audit.RecommendedProfileId ?? "-")).Append("<br>used: ").Append(Html(audit.UsedProfileId ?? "-")).Append("</td>")
                .Append("<td>").Append(Html(audit.Provider)).Append("<br>").Append(Html(audit.Model)).Append("</td>")
                .Append("<td>").Append(Html(audit.TaskType)).Append("<br>").Append(Html(audit.RiskLevel)).Append("</td>")
                .Append("<td>").Append(audit.RequiresVision ? "true" : "false").Append("</td>")
                .Append("<td>").Append(audit.RequiresHumanApproval ? "true" : "false").Append("</td>")
                .Append("<td>").Append(audit.FallbackUsed ? Html($"{audit.FallbackFrom} -> {audit.FallbackTo}") : "-").Append("</td>")
                .Append("<td>").Append(Html(audit.BudgetDecision)).Append("</td>")
                .Append("<td>est ").Append(audit.EstimatedCostUsd.ToString("0.0000")).Append("<br>actual ").Append(audit.ActualCostUsd?.ToString("0.0000") ?? "-").Append("<br>tokens ").Append(Html($"{audit.TokensIn?.ToString() ?? "-"} / {audit.TokensOut?.ToString() ?? "-"}")).Append("</td>")
                .Append("<td><span class=\"badge ").Append(statusClass).Append("\">").Append(Html(audit.ResultStatus)).Append("</span><br>").Append(Html(audit.Reason)).Append("</td>")
                .Append("</tr>");
        }

        return builder.ToString();
    }

    private static string RecipeListRows(IReadOnlyList<RecipeEditorModel> recipes)
    {
        if (recipes.Count == 0)
            return "<tr><td colspan=\"6\">No allowlisted recipes found.</td></tr>";

        var builder = new StringBuilder();
        foreach (var recipe in recipes)
        {
            builder.Append("<tr>")
                .Append("<td>").Append(Html(recipe.Title)).Append("<br><small>").Append(Html(recipe.RecipeId)).Append("</small></td>")
                .Append("<td>").Append(Html(recipe.RiskLevel)).Append("</td>")
                .Append("<td>").Append(Html(recipe.ConfidenceStatus)).Append("</td>")
                .Append("<td class=\"path\">").Append(Html(recipe.RecipePath)).Append("</td>")
                .Append("<td>").Append(recipe.Steps.Count).Append("</td>")
                .Append("<td><a class=\"button\" href=\"/recipes/").Append(Html(recipe.RecipeId)).Append("\">Open</a></td>")
                .Append("</tr>");
        }

        return builder.ToString();
    }

    private static string RecipeStepRows(IReadOnlyList<RecipeEditorStepSummary> steps)
    {
        var builder = new StringBuilder();
        foreach (var step in steps)
        {
            var riskClass = step.RiskLevel == "high" ? "risk" : "safe";
            builder.Append("<tr>")
                .Append("<td>").Append(step.StepNumber).Append("</td>")
                .Append("<td>").Append(Html(step.StepId ?? "-")).Append("</td>")
                .Append("<td>").Append(Html(step.Kind)).Append("</td>")
                .Append("<td>").Append(Html(step.HumanLabel)).Append("</td>")
                .Append("<td><span class=\"badge ").Append(riskClass).Append("\">").Append(Html(step.RiskLevel)).Append("</span></td>")
                .Append("<td>").Append(step.RequiresApproval ? "true" : "false").Append("</td>")
                .Append("</tr>");
        }

        return builder.ToString();
    }

    private static string ValidationRows(IReadOnlyList<RecipeValidationIssue> issues)
    {
        if (issues.Count == 0)
            return "<tr><td colspan=\"5\">No validation issues.</td></tr>";

        var builder = new StringBuilder();
        foreach (var issue in issues)
        {
            var severityClass = issue.Severity is "blocked" or "error" ? "risk" : issue.Severity == "warning" ? "warn" : "safe";
            builder.Append("<tr>")
                .Append("<td><span class=\"badge ").Append(severityClass).Append("\">").Append(Html(issue.Severity)).Append("</span></td>")
                .Append("<td>").Append(Html(issue.Code)).Append("</td>")
                .Append("<td>").Append(Html(issue.FieldPath)).Append("</td>")
                .Append("<td>").Append(Html(issue.Message)).Append("</td>")
                .Append("<td>").Append(Html(issue.Remediation)).Append("</td>")
                .Append("</tr>");
        }

        return builder.ToString();
    }

    private static string VariableRows(IReadOnlyList<RecipeVariableDefinition> variables)
    {
        if (variables.Count == 0)
            return "<tr><td colspan=\"5\">No variables detected.</td></tr>";

        var builder = new StringBuilder();
        foreach (var variable in variables)
        {
            builder.Append("<tr>")
                .Append("<td>").Append(Html(variable.Name)).Append("</td>")
                .Append("<td>").Append(Html(variable.Type)).Append("</td>")
                .Append("<td>").Append(variable.Required ? "true" : "false").Append("</td>")
                .Append("<td>").Append(Html(variable.Sensitivity)).Append("</td>")
                .Append("<td>").Append(Html(RecipeVariableManager.DisplayValue(variable))).Append("</td>")
                .Append("</tr>");
        }

        return builder.ToString();
    }

    private static string VariableManagerRows(IReadOnlyList<RecipeVariableDefinition> variables)
    {
        if (variables.Count == 0)
            return "<tr><td colspan=\"6\">No variables detected.</td></tr>";

        var builder = new StringBuilder();
        foreach (var variable in variables)
        {
            builder.Append("<tr>")
                .Append("<td>").Append(Html(variable.Name)).Append("</td>")
                .Append("<td>").Append(Html(variable.Type)).Append("</td>")
                .Append("<td>").Append(variable.Required ? "true" : "false").Append("</td>")
                .Append("<td>").Append(Html(RecipeVariableManager.DisplayValue(variable))).Append("</td>")
                .Append("<td>").Append(Html(variable.Sensitivity)).Append(variable.Redacted ? " / redacted" : "").Append("</td>")
                .Append("<td>").Append(Html(variable.Regex ?? "-")).Append("</td>")
                .Append("</tr>");
        }

        return builder.ToString();
    }

    private static string ProcessMemoryRows(IReadOnlyList<ProcessMemoryEntry> entries)
    {
        if (entries.Count == 0)
            return "<tr><td colspan=\"7\">No process memory entries found.</td></tr>";

        var builder = new StringBuilder();
        foreach (var entry in entries)
        {
            var statusClass = entry.Status is ProcessMemoryStatuses.Stable or ProcessMemoryStatuses.Supervised ? "safe" :
                entry.Status is ProcessMemoryStatuses.Rejected or ProcessMemoryStatuses.Archived ? "risk" : "warn";
            builder.Append("<tr>")
                .Append("<td><a href=\"/memory/").Append(Html(entry.Id)).Append("\">").Append(Html(entry.Title)).Append("</a><br><small>").Append(Html(entry.Id)).Append("</small></td>")
                .Append("<td><span class=\"badge ").Append(statusClass).Append("\">").Append(Html(entry.Status)).Append("</span></td>")
                .Append("<td>").Append(Html(entry.AppOrSite)).Append("</td>")
                .Append("<td>").Append(Html(entry.Domain)).Append("</td>")
                .Append("<td>").Append(Html(entry.RiskLevel)).Append("</td>")
                .Append("<td>").Append(entry.ConfidenceScore).Append("</td>")
                .Append("<td>").Append(Html(string.Join(", ", entry.Tags))).Append("</td>")
                .Append("</tr>");
        }

        return builder.ToString();
    }

    private static string WorkflowRetrievalRows(IReadOnlyList<WorkflowRetrievalMatch> matches)
    {
        if (matches.Count == 0)
            return "<tr><td colspan=\"6\">No retrieval matches yet.</td></tr>";

        var builder = new StringBuilder();
        foreach (var match in matches)
        {
            var safeClass = match.SafeToSuggest ? "safe" : "risk";
            builder.Append("<tr>")
                .Append("<td><a href=\"/memory/").Append(Html(match.ProcessMemoryId)).Append("\">").Append(Html(match.Title)).Append("</a><br><small>").Append(Html(match.ProcessMemoryId)).Append("</small></td>")
                .Append("<td>").Append(match.Score.ToString("0.00")).Append("</td>")
                .Append("<td><span class=\"badge ").Append(safeClass).Append("\">").Append(match.SafeToSuggest ? "safe" : "not safe").Append("</span></td>")
                .Append("<td>").Append(match.RequiresHumanReview ? "true" : "false").Append("</td>")
                .Append("<td>").Append(Html(FirstNonEmpty(match.RecipeId, match.CandidateFlowId, match.TimelineId, "-"))).Append("</td>")
                .Append("<td>").Append(Html(string.Join("; ", match.Reasons))).Append("</td>")
                .Append("</tr>");
        }

        return builder.ToString();
    }

    private static string ProcessMemoryLinkRows(ProcessMemoryEntry entry)
    {
        var rows = new[]
        {
            ("recordingSessionId", entry.Links.RecordingSessionId),
            ("timelineId", entry.Links.TimelineId),
            ("candidateFlowId", entry.Links.CandidateFlowId),
            ("recipeDraftId", entry.Links.RecipeDraftId),
            ("recipeId", entry.Links.RecipeId),
            ("approvalRequestId", entry.Links.ApprovalRequestId),
            ("approvalDecisionId", entry.Links.ApprovalDecisionId),
            ("runId", entry.Links.RunId),
            ("aiAuditId", entry.Links.AiAuditId),
            ("confidenceId", entry.Links.ConfidenceId)
        };

        var builder = new StringBuilder();
        foreach (var (name, value) in rows.Where(row => !string.IsNullOrWhiteSpace(row.Item2)))
            builder.Append("<li>").Append(Html(name)).Append(": ").Append(Html(value)).Append("</li>");

        return builder.Length == 0 ? "<li>No linked artifacts.</li>" : builder.ToString();
    }

    private static string ProcessMemoryEvidenceRows(ProcessMemoryEntry entry)
    {
        var builder = new StringBuilder();
        foreach (var link in entry.EvidenceLinks)
            builder.Append("<li>").Append(Html(link.Kind)).Append(": <span class=\"path\">").Append(Html(link.RelativePath)).Append("</span> - ").Append(Html(link.Label)).Append("</li>");
        foreach (var path in entry.ArtifactPaths)
            builder.Append("<li>artifact: <span class=\"path\">").Append(Html(path)).Append("</span></li>");

        return builder.Length == 0 ? "<li>No evidence links.</li>" : builder.ToString();
    }

    private static string AppProfileRows(IReadOnlyList<AppProfile> profiles)
    {
        if (profiles.Count == 0)
            return "<tr><td colspan=\"7\">No app profiles found.</td></tr>";

        var builder = new StringBuilder();
        foreach (var profile in profiles)
        {
            var validation = AppProfilePolicy.Validate(profile);
            var statusClass = validation.CanActivate ? "safe" : "risk";
            builder.Append("<tr>")
                .Append("<td><a href=\"/app-profiles/").Append(Html(profile.Id)).Append("\">").Append(Html(profile.Name)).Append("</a><br><small>").Append(Html(profile.Id)).Append("</small></td>")
                .Append("<td>").Append(Html(profile.Kind)).Append("</td>")
                .Append("<td>").Append(Html(profile.Status)).Append("</td>")
                .Append("<td>").Append(Html(profile.SiteDomain ?? profile.ProcessName ?? "-")).Append("</td>")
                .Append("<td>").Append(Html(string.Join(", ", profile.SupportedCapabilities))).Append("</td>")
                .Append("<td>").Append(Html($"readOnly={profile.RiskPolicy.ReadOnlyByDefault}; diagnostic={profile.RiskPolicy.DiagnosticAllowed}; loginBlocked={profile.RiskPolicy.BlocksLogin}; paymentBlocked={profile.RiskPolicy.BlocksPayment}; purchaseBlocked={profile.RiskPolicy.BlocksPurchase}")).Append("</td>")
                .Append("<td><span class=\"badge ").Append(statusClass).Append("\">").Append(validation.CanActivate ? "valid" : "blocked").Append("</span></td>")
                .Append("</tr>");
        }

        return builder.ToString();
    }

    private static string AppProfileIssueRows(IReadOnlyList<AppProfileValidationIssue> issues)
    {
        if (issues.Count == 0)
            return "<tr><td colspan=\"4\">No validation issues.</td></tr>";

        var builder = new StringBuilder();
        foreach (var issue in issues)
        {
            var severityClass = issue.Severity is "blocked" or "error" ? "risk" : issue.Severity == "warning" ? "warn" : "safe";
            builder.Append("<tr>")
                .Append("<td><span class=\"badge ").Append(severityClass).Append("\">").Append(Html(issue.Severity)).Append("</span></td>")
                .Append("<td>").Append(Html(issue.Code)).Append("</td>")
                .Append("<td>").Append(Html(issue.Message)).Append("</td>")
                .Append("<td>").Append(Html(issue.Remediation)).Append("</td>")
                .Append("</tr>");
        }

        return builder.ToString();
    }

    private static string ListRows(IEnumerable<string> items)
    {
        var builder = new StringBuilder();
        foreach (var item in items)
            builder.Append("<li>").Append(Html(item)).Append("</li>");

        return builder.Length == 0 ? "<li>-</li>" : builder.ToString();
    }

    private static string DraftResultBlock(RecipeDraft? draft, RecipeDraftArtifactWriteResult? write)
    {
        if (draft == null)
            return "";

        var status = write?.Success == true ? "Draft saved" : "Draft rejected";
        var detail = write?.Success == true ? write.RelativePath : write?.Error ?? string.Join("; ", draft.ValidationNotes);
        return "<p><span class=\"badge " + (write?.Success == true ? "safe" : "risk") + "\">" + Html(status) + "</span> " + Html(detail) + "</p>";
    }

    private static string MessageBlock(string? message)
    {
        return string.IsNullOrWhiteSpace(message) ? "" : "<p><span class=\"badge warn\">" + Html(message) + "</span></p>";
    }

    private static string SharedPilotStyle()
    {
        return """
<style>
  :root { --ink: #17211a; --muted: #5c6b60; --paper: #f5f1e7; --panel: #fffaf0; --line: #d7cdb7; --safe: #226b45; --risk: #8a352d; --warn: #9a5a10; }
  body { margin: 0; color: var(--ink); font-family: "Aptos", "Segoe UI", sans-serif; background: linear-gradient(135deg, #f7f2df, #e4eadc); }
  main { max-width: 1220px; margin: 0 auto; padding: 40px 24px; }
  .card { background: rgba(255,250,240,.92); border: 1px solid var(--line); border-radius: 26px; padding: 24px; box-shadow: 0 20px 70px rgba(43,32,16,.14); margin-bottom: 18px; }
  h1 { font-family: Georgia, "Times New Roman", serif; font-size: clamp(38px, 6vw, 72px); line-height: .95; margin: 8px 0 12px; letter-spacing: -.045em; }
  p, li { color: var(--muted); line-height: 1.5; }
  table { width: 100%; border-collapse: collapse; background: var(--panel); border-radius: 18px; overflow: hidden; }
  th, td { padding: 11px 12px; border-bottom: 1px solid var(--line); text-align: left; vertical-align: top; }
  th { font-size: 11px; text-transform: uppercase; letter-spacing: .08em; background: #efe6d4; }
  .grid { display: grid; grid-template-columns: repeat(2, minmax(0, 1fr)); gap: 14px; }
  .badge { display: inline-block; border-radius: 999px; padding: 5px 10px; font-weight: 800; font-size: 12px; background: #eadfca; }
  .safe { color: var(--safe); background: #dcebdd; }
  .risk { color: var(--risk); background: #f6e7e6; }
  .warn { color: var(--warn); background: #f3e2bf; }
  .path { word-break: break-all; }
  .metric { border-bottom: 1px solid var(--line); padding: 10px 0; }
  .metric strong { display: block; font-size: 18px; color: var(--ink); }
  textarea, input { width: 100%; border: 1px solid var(--line); border-radius: 14px; padding: 10px; background: #fffdf6; font: inherit; margin: 6px 0 10px; }
  button, .button { display: inline-block; border: 0; border-radius: 999px; padding: 11px 16px; color: #fffaf0; background: var(--ink); font-weight: 800; text-decoration: none; }
  @media (max-width: 780px) { .grid { grid-template-columns: 1fr; } }
</style>
""";
    }

    private static string Safety(RunSafetyCounters counters)
    {
        return Html($"clicks={counters.Clicks}; cookies={counters.CookiesAccepted}; login={counters.Login}; cart={counters.Cart}; purchase={counters.Purchase}; payment={counters.Payment}");
    }

    private static string FirstNonEmpty(params string?[] values)
    {
        return values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value)) ?? "";
    }

    private static string BuildAIProfileStatus(AIModelProfile profile)
    {
        if (!profile.Enabled)
            return "disabled";
        if (string.IsNullOrWhiteSpace(profile.Model))
            return "missing_model";
        if (!profile.ApiKeyConfigured && !string.Equals(profile.Provider, AIProviderKinds.Mock, StringComparison.OrdinalIgnoreCase))
            return "missing_key";
        return "configured";
    }

    private static string Html(string? value)
    {
        return WebUtility.HtmlEncode(value ?? "");
    }
}
