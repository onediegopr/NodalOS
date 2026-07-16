using System.Globalization;
using System.Net;
using System.Text;

namespace OneBrain.Pilot;

public static class MissionControlProductShellHtmlRenderer
{
    public static string Render(MissionControlProductShellSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        var timeline = RenderTimeline(snapshot.Timeline);
        var context = RenderContext(snapshot.Context);
        var evidence = RenderEvidence(snapshot.EvidenceRefs);
        var diagnostics = RenderDiagnostics(snapshot.Diagnostics);
        var fallback = string.IsNullOrWhiteSpace(snapshot.RecentFallback)
            ? "Sin fallback reciente"
            : snapshot.RecentFallback;
        var nextGate = snapshot.WorkspaceSelected
            ? "El workspace real quedó protegido y revalidado. El próximo gate es crear una misión real sobre este contexto y mantener cualquier mutación detrás de aprobación, snapshot, rollback y verificación."
            : "Seleccionar y persistir un workspace local real, generar un plan revisado y mantener los paths absolutos fuera de la superficie.";
        var workspaceAction = snapshot.WorkspaceSelected
            ? "Revisar o cambiar workspace"
            : "Seleccionar workspace local";

        const string template = """
<!doctype html>
<html lang="es">
<head>
  <meta charset="utf-8">
  <meta name="viewport" content="width=device-width, initial-scale=1">
  <title>NODAL OS — Mission Control</title>
  <style>
    :root {
      color-scheme: dark;
      --background: #0D1117;
      --panel: #161B22;
      --card: #1C2128;
      --border: #30363D;
      --text: #F5F7FA;
      --muted: #AAB4C0;
      --disabled: #6E7681;
      --blue: #4F7CFF;
      --violet: #7C5CFF;
      --positive: #00C2A8;
      --attention: #F0B45A;
      --blocked: #F06A6A;
    }
    * { box-sizing: border-box; }
    html, body { min-height: 100%; }
    body {
      margin: 0;
      background: var(--background);
      color: var(--text);
      font-family: Inter, Geist, Manrope, "Segoe UI", sans-serif;
      font-size: 14px;
    }
    a { color: inherit; text-decoration: none; }
    button { font: inherit; }
    .shell {
      min-height: 100vh;
      display: grid;
      grid-template-columns: 230px minmax(0, 1fr);
    }
    .sidebar {
      position: sticky;
      top: 0;
      height: 100vh;
      border-right: 1px solid var(--border);
      background: #10151C;
      padding: 24px 16px;
      display: flex;
      flex-direction: column;
      gap: 26px;
    }
    .brand { display: grid; gap: 5px; padding: 0 8px; }
    .brand-mark {
      width: 34px;
      height: 34px;
      border-radius: 10px;
      display: grid;
      place-items: center;
      background: var(--blue);
      color: white;
      font-weight: 900;
      letter-spacing: -.04em;
    }
    .brand strong { font-size: 15px; letter-spacing: .04em; }
    .brand span { color: var(--muted); font-size: 12px; }
    nav { display: grid; gap: 6px; }
    .nav-link {
      min-height: 38px;
      display: flex;
      align-items: center;
      gap: 10px;
      border: 1px solid transparent;
      border-radius: 10px;
      padding: 0 11px;
      color: var(--muted);
    }
    .nav-link:hover { border-color: var(--border); color: var(--text); }
    .nav-link.active { background: #1B2330; border-color: #35415A; color: var(--text); }
    .nav-dot { width: 7px; height: 7px; border-radius: 50%; background: var(--disabled); }
    .active .nav-dot { background: var(--blue); box-shadow: 0 0 0 4px rgba(79,124,255,.14); }
    .boundary {
      margin-top: auto;
      border: 1px solid var(--border);
      border-radius: 12px;
      background: var(--panel);
      padding: 12px;
      color: var(--muted);
      font-size: 12px;
      line-height: 1.5;
    }
    .boundary strong { color: var(--positive); display: block; margin-bottom: 4px; }
    .workspace { min-width: 0; padding: 22px 24px 30px; }
    .topbar {
      min-height: 66px;
      display: flex;
      align-items: center;
      gap: 18px;
      border: 1px solid var(--border);
      border-radius: 14px;
      background: var(--panel);
      padding: 12px 14px 12px 18px;
    }
    .topbar-main { min-width: 0; flex: 1; display: grid; gap: 4px; }
    .topbar-label, .metric-label, .context-label {
      color: var(--muted);
      font-size: 11px;
      text-transform: uppercase;
      letter-spacing: .1em;
    }
    .topbar-title { font-weight: 760; white-space: nowrap; overflow: hidden; text-overflow: ellipsis; }
    .topbar-meta { display: flex; flex-wrap: wrap; gap: 8px; align-items: center; color: var(--muted); font-size: 12px; }
    .actions { display: flex; gap: 8px; }
    .button {
      min-height: 36px;
      border: 1px solid var(--border);
      border-radius: 9px;
      padding: 0 13px;
      color: var(--disabled);
      background: var(--card);
    }
    .button.primary { color: #DDE5FF; border-color: #3E538C; background: #26375F; }
    .button:disabled { cursor: not-allowed; opacity: .72; }
    .mission-card {
      margin-top: 18px;
      border: 1px solid var(--border);
      border-radius: 16px;
      background: var(--panel);
      padding: 22px;
      display: grid;
      gap: 16px;
    }
    .eyebrow { color: #91A9FF; text-transform: uppercase; letter-spacing: .14em; font-size: 11px; font-weight: 800; }
    h1 { margin: 7px 0 8px; font-size: clamp(26px, 3vw, 38px); line-height: 1.1; letter-spacing: -.035em; }
    .mission-summary { color: var(--muted); max-width: 920px; line-height: 1.6; }
    .mission-metrics { display: grid; grid-template-columns: minmax(260px, 2fr) repeat(3, minmax(130px, 1fr)); gap: 10px; }
    .metric { border: 1px solid var(--border); border-radius: 12px; background: var(--card); padding: 12px; min-width: 0; }
    .metric-value { margin-top: 6px; font-weight: 700; overflow-wrap: anywhere; }
    .progress-track { height: 7px; margin-top: 10px; background: #0F141A; border: 1px solid #29313B; border-radius: 999px; overflow: hidden; }
    .progress-fill { display: block; height: 100%; background: var(--blue); }
    .content-grid { margin-top: 18px; display: grid; grid-template-columns: minmax(0, 1fr) 310px; gap: 18px; align-items: start; }
    .panel { border: 1px solid var(--border); border-radius: 16px; background: var(--panel); overflow: hidden; }
    .panel-header { padding: 16px 18px; border-bottom: 1px solid var(--border); display: flex; align-items: center; justify-content: space-between; gap: 12px; }
    .panel-header h2 { margin: 0; font-size: 14px; }
    .panel-header span { color: var(--muted); font-size: 12px; }
    .timeline { padding: 18px; }
    .timeline-item { position: relative; display: grid; grid-template-columns: 28px minmax(0, 1fr); gap: 12px; padding-bottom: 18px; }
    .timeline-item:last-child { padding-bottom: 0; }
    .timeline-item:not(:last-child)::after { content: ""; position: absolute; left: 13px; top: 27px; bottom: 0; width: 1px; background: var(--border); }
    .timeline-node { width: 28px; height: 28px; border-radius: 50%; display: grid; place-items: center; border: 1px solid var(--border); background: var(--card); color: var(--muted); font-weight: 800; z-index: 1; }
    .timeline-item.complete .timeline-node { border-color: rgba(0,194,168,.55); color: var(--positive); }
    .timeline-item.fallback .timeline-node { border-color: rgba(124,92,255,.65); color: #A996FF; }
    .timeline-item.attention .timeline-node { border-color: rgba(240,180,90,.65); color: var(--attention); }
    .timeline-item.blocked .timeline-node { border-color: rgba(240,106,106,.65); color: var(--blocked); }
    .timeline-body, .context-card { border: 1px solid var(--border); border-radius: 12px; background: var(--card); padding: 12px 14px; min-width: 0; }
    .timeline-title, .context-top { display: flex; align-items: center; justify-content: space-between; gap: 12px; font-weight: 720; }
    .timeline-detail, .context-detail { margin-top: 7px; color: var(--muted); line-height: 1.5; overflow-wrap: anywhere; }
    .refs { margin-top: 9px; display: flex; flex-wrap: wrap; gap: 6px; }
    .ref, .badge { display: inline-flex; align-items: center; min-height: 24px; border-radius: 999px; border: 1px solid var(--border); padding: 0 8px; font-size: 11px; color: var(--muted); background: #141A21; }
    .badge.complete, .badge.ready { color: var(--positive); border-color: rgba(0,194,168,.34); }
    .badge.fallback, .badge.active { color: #B9AAFF; border-color: rgba(124,92,255,.44); }
    .badge.attention { color: var(--attention); border-color: rgba(240,180,90,.4); }
    .badge.blocked { color: var(--blocked); border-color: rgba(240,106,106,.4); }
    .context-list { display: grid; gap: 9px; padding: 12px; }
    .context-value { margin-top: 6px; font-weight: 730; overflow-wrap: anywhere; }
    .fallback-note { margin: 12px; border: 1px solid rgba(124,92,255,.44); border-radius: 12px; background: rgba(124,92,255,.08); padding: 12px; color: #D9D1FF; line-height: 1.5; }
    .lower-grid { margin-top: 18px; display: grid; grid-template-columns: 1fr 1fr; gap: 18px; }
    .evidence-list { padding: 14px 18px 18px; display: flex; flex-wrap: wrap; gap: 8px; }
    .empty { color: var(--muted); padding: 18px; line-height: 1.55; }
    .workspace-cta { display: inline-flex; margin-top: 12px; border: 1px solid #405891; border-radius: 9px; padding: 9px 12px; color: #DFE7FF; background: #202A44; }
    details { margin-top: 18px; border: 1px solid var(--border); border-radius: 14px; background: var(--panel); }
    summary { cursor: pointer; padding: 15px 18px; font-weight: 720; }
    .diagnostics { border-top: 1px solid var(--border); padding: 12px 18px 16px; display: grid; gap: 7px; color: var(--muted); font-family: "Cascadia Code", Consolas, monospace; font-size: 12px; }
    .footer { margin-top: 18px; display: flex; justify-content: space-between; gap: 14px; color: var(--muted); font-size: 12px; }
    .footer a { color: #9CB0FF; }
    @media (max-width: 1040px) {
      .shell { grid-template-columns: 78px minmax(0, 1fr); }
      .brand strong, .brand span, .nav-link span, .boundary { display: none; }
      .sidebar { align-items: center; padding-inline: 10px; }
      .nav-link { justify-content: center; width: 44px; padding: 0; }
      .content-grid { grid-template-columns: 1fr; }
      .mission-metrics { grid-template-columns: 1fr 1fr; }
    }
    @media (max-width: 720px) {
      .shell { display: block; }
      .sidebar { position: static; height: auto; border-right: 0; border-bottom: 1px solid var(--border); flex-direction: row; align-items: center; overflow-x: auto; }
      .brand { display: flex; align-items: center; }
      nav { display: flex; }
      .workspace { padding: 14px; }
      .topbar { align-items: flex-start; flex-direction: column; }
      .actions { width: 100%; }
      .button { flex: 1; }
      .mission-metrics, .lower-grid { grid-template-columns: 1fr; }
    }
  </style>
</head>
<body data-nodal-os="mission-control-product-shell" data-local-only="@@LOCAL_ONLY@@" data-read-only="@@READ_ONLY@@" data-fixture-backed="@@FIXTURE_BACKED@@" data-workspace-selected="@@WORKSPACE_SELECTED@@" data-workspace-persisted="@@WORKSPACE_PERSISTED@@" data-product-authority="@@PRODUCT_AUTHORITY@@">
  <div class="shell">
    <aside class="sidebar">
      <div class="brand"><div class="brand-mark">N</div><div><strong>NODAL OS</strong><span>AI Mission Control</span></div></div>
      <nav aria-label="Navegación principal">
        <a class="nav-link active" href="/"><span class="nav-dot"></span><span>Mission Control</span></a>
        <a class="nav-link" href="#timeline"><span class="nav-dot"></span><span>Timeline</span></a>
        <a class="nav-link" href="/workspace/select"><span class="nav-dot"></span><span>Workspace</span></a>
        <a class="nav-link" href="/ai/config"><span class="nav-dot"></span><span>Models</span></a>
        <a class="nav-link" href="#evidence"><span class="nav-dot"></span><span>Evidence</span></a>
        <a class="nav-link" href="/guia"><span class="nav-dot"></span><span>Settings</span></a>
      </nav>
      <div class="boundary"><strong>Local-first</strong>Vista protegida por loopback. Sin secretos, cloud obligatorio ni autoridad de producción.</div>
    </aside>

    <main class="workspace">
      <header class="topbar" data-section-id="topbar">
        <div class="topbar-main"><div class="topbar-label">Misión actual</div><div class="topbar-title">@@GOAL@@</div><div class="topbar-meta"><span>@@MISSION_STATUS@@</span><span>•</span><span>@@PROGRESS@@%</span><span>•</span><span>@@ACTIVE_MODEL@@</span></div></div>
        <div class="actions"><button class="button" type="button" disabled>Pausar</button><button class="button primary" type="button" disabled>@@APPROVAL_STATE@@</button></div>
      </header>

      <section class="mission-card" data-section-id="mission">
        <div><div class="eyebrow">@@PRODUCT_MODE@@ / @@DECISION@@</div><h1>Una misión, un timeline, control visible.</h1><div class="mission-summary">El runtime, los modelos, los fallbacks, la verificación, el workspace protegido y la evidencia se proyectan aquí sin crear una segunda fuente de verdad.</div></div>
        <div class="mission-metrics">
          <div class="metric"><div class="metric-label">Progreso</div><div class="metric-value">@@PROGRESS@@% · @@CURRENT_STEP@@</div><div class="progress-track" aria-label="Progreso de misión"><span class="progress-fill" style="width: @@PROGRESS@@%"></span></div></div>
          <div class="metric"><div class="metric-label">Workspace</div><div class="metric-value">@@WORKSPACE_STATE@@</div></div>
          <div class="metric"><div class="metric-label">Modelo</div><div class="metric-value">@@ACTIVE_PROVIDER@@ / @@ACTIVE_MODEL@@</div></div>
          <div class="metric"><div class="metric-label">Control humano</div><div class="metric-value">@@APPROVAL_STATE@@</div></div>
        </div>
      </section>

      <div class="content-grid">
        <section class="panel" id="timeline" data-section-id="timeline"><div class="panel-header"><h2>Timeline de misión</h2><span>@@TIMELINE_COUNT@@ eventos</span></div><div class="timeline">@@TIMELINE@@</div></section>
        <aside class="panel" data-section-id="context"><div class="panel-header"><h2>Contexto activo</h2><span>runtime</span></div><div class="context-list">@@CONTEXT@@</div><div class="fallback-note"><strong>Último fallback</strong><br>@@FALLBACK@@</div></aside>
      </div>

      <div class="lower-grid">
        <section class="panel" id="evidence" data-section-id="evidence"><div class="panel-header"><h2>Evidencia</h2><span>refs redacted</span></div><div class="evidence-list">@@EVIDENCE@@</div></section>
        <section class="panel" data-section-id="next-step"><div class="panel-header"><h2>Próximo gate productivo</h2><span>P2</span></div><div class="empty">@@NEXT_GATE@@<br><a class="workspace-cta" href="/workspace/select">@@WORKSPACE_ACTION@@</a></div></section>
      </div>

      <details data-section-id="diagnostics"><summary>Diagnóstico técnico y eventos</summary><div class="diagnostics">@@DIAGNOSTICS@@</div></details>
      <div class="footer"><span>Local-only · read-only mission shell · sin product authority</span><a href="/pilot/legacy">Abrir laboratorio Pilot legado</a></div>
    </main>
  </div>
</body>
</html>
""";

        var progress = snapshot.ProgressPercent.ToString(CultureInfo.InvariantCulture);
        return template
            .Replace("@@LOCAL_ONLY@@", Bool(snapshot.LocalOnly), StringComparison.Ordinal)
            .Replace("@@READ_ONLY@@", Bool(snapshot.ReadOnly), StringComparison.Ordinal)
            .Replace("@@FIXTURE_BACKED@@", Bool(snapshot.FixtureBacked), StringComparison.Ordinal)
            .Replace("@@WORKSPACE_SELECTED@@", Bool(snapshot.WorkspaceSelected), StringComparison.Ordinal)
            .Replace("@@WORKSPACE_PERSISTED@@", Bool(snapshot.WorkspacePersisted), StringComparison.Ordinal)
            .Replace("@@PRODUCT_AUTHORITY@@", Bool(snapshot.ProductAuthorityGranted), StringComparison.Ordinal)
            .Replace("@@GOAL@@", H(snapshot.Goal), StringComparison.Ordinal)
            .Replace("@@MISSION_STATUS@@", H(snapshot.MissionStatus), StringComparison.Ordinal)
            .Replace("@@PROGRESS@@", progress, StringComparison.Ordinal)
            .Replace("@@ACTIVE_MODEL@@", H(snapshot.ActiveModel), StringComparison.Ordinal)
            .Replace("@@APPROVAL_STATE@@", H(snapshot.ApprovalState), StringComparison.Ordinal)
            .Replace("@@PRODUCT_MODE@@", H(snapshot.ProductMode), StringComparison.Ordinal)
            .Replace("@@DECISION@@", H(snapshot.Decision), StringComparison.Ordinal)
            .Replace("@@CURRENT_STEP@@", H(snapshot.CurrentStep), StringComparison.Ordinal)
            .Replace("@@WORKSPACE_STATE@@", H(snapshot.WorkspaceState), StringComparison.Ordinal)
            .Replace("@@ACTIVE_PROVIDER@@", H(snapshot.ActiveProvider), StringComparison.Ordinal)
            .Replace("@@TIMELINE_COUNT@@", snapshot.Timeline.Count.ToString(CultureInfo.InvariantCulture), StringComparison.Ordinal)
            .Replace("@@TIMELINE@@", timeline, StringComparison.Ordinal)
            .Replace("@@CONTEXT@@", context, StringComparison.Ordinal)
            .Replace("@@FALLBACK@@", H(fallback), StringComparison.Ordinal)
            .Replace("@@EVIDENCE@@", evidence, StringComparison.Ordinal)
            .Replace("@@NEXT_GATE@@", H(nextGate), StringComparison.Ordinal)
            .Replace("@@WORKSPACE_ACTION@@", H(workspaceAction), StringComparison.Ordinal)
            .Replace("@@DIAGNOSTICS@@", diagnostics, StringComparison.Ordinal);
    }

    private static string RenderTimeline(IReadOnlyList<MissionControlProductTimelineItem> items)
    {
        if (items.Count == 0)
            return "<div class=\"empty\">Todavía no hay eventos para esta misión.</div>";

        var builder = new StringBuilder();
        foreach (var item in items)
        {
            builder.Append("<article class=\"timeline-item ")
                .Append(H(item.State))
                .Append("\" data-timeline-state=\"")
                .Append(H(item.State))
                .Append("\"><div class=\"timeline-node\">")
                .Append(StateIcon(item.State))
                .Append("</div><div class=\"timeline-body\"><div class=\"timeline-title\"><span>")
                .Append(H(item.Title))
                .Append("</span><span class=\"badge ")
                .Append(H(item.State))
                .Append("\">#")
                .Append(item.Sequence)
                .Append("</span></div><div class=\"timeline-detail\">")
                .Append(H(item.Detail))
                .Append("</div>");

            if (item.EvidenceRefs.Count > 0)
            {
                builder.Append("<div class=\"refs\">");
                foreach (var reference in item.EvidenceRefs)
                    builder.Append("<span class=\"ref\">").Append(H(reference)).Append("</span>");
                builder.Append("</div>");
            }

            builder.Append("</div></article>");
        }
        return builder.ToString();
    }

    private static string RenderContext(IReadOnlyList<MissionControlProductContextItem> items)
    {
        var builder = new StringBuilder();
        foreach (var item in items)
        {
            builder.Append("<article class=\"context-card\" data-context-id=\"")
                .Append(H(item.Id))
                .Append("\"><div class=\"context-top\"><span class=\"context-label\">")
                .Append(H(item.Label))
                .Append("</span><span class=\"badge ")
                .Append(H(item.State))
                .Append("\">")
                .Append(H(item.State))
                .Append("</span></div><div class=\"context-value\">")
                .Append(H(item.Value))
                .Append("</div><div class=\"context-detail\">")
                .Append(H(item.Detail))
                .Append("</div></article>");
        }
        return builder.ToString();
    }

    private static string RenderEvidence(IReadOnlyList<string> evidenceRefs) =>
        evidenceRefs.Count == 0
            ? "<span class=\"ref\">Sin evidencia</span>"
            : string.Join(string.Empty, evidenceRefs.Select(reference => $"<span class=\"ref\">{H(reference)}</span>"));

    private static string RenderDiagnostics(IReadOnlyList<string> diagnostics) =>
        diagnostics.Count == 0
            ? "<span>sin diagnóstico</span>"
            : string.Join(string.Empty, diagnostics.Select(value => $"<span>{H(value)}</span>"));

    private static string StateIcon(string state) => state switch
    {
        "complete" => "✓",
        "fallback" => "↺",
        "attention" => "!",
        "blocked" => "×",
        _ => "•"
    };

    private static string H(string? value) => WebUtility.HtmlEncode(value ?? string.Empty);

    private static string Bool(bool value) => value ? "true" : "false";
}
