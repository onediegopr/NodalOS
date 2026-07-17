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
        var onboarding = RenderQuickStart(snapshot);
        var onboardingComplete = snapshot.WorkspaceSelected && snapshot.RealMissionDraft &&
            (snapshot.ActionVerified || snapshot.ActionRolledBack) && snapshot.ModelConnectionVerified;
        var (nextStage, nextStep, nextAction, nextHref) = NextAction(snapshot);
        var actionCandidate = RenderActionCandidate(snapshot);
        var controlAction = RenderControlAction(snapshot);

        const string template = """
<!doctype html>
<html lang="es">
<head>
  <meta charset="utf-8">
  <meta name="viewport" content="width=device-width, initial-scale=1">
  <title>NODAL OS — Mission Control</title>
  <style>
    :root{color-scheme:dark;--background:#0D1117;--panel:#161B22;--card:#1C2128;--border:#30363D;--text:#F5F7FA;--muted:#AAB4C0;--disabled:#6E7681;--blue:#4F7CFF;--violet:#7C5CFF;--positive:#00C2A8;--attention:#F0B45A;--blocked:#F06A6A}*{box-sizing:border-box}html,body{min-height:100%}body{margin:0;background:var(--background);color:var(--text);font-family:Inter,Geist,Manrope,"Segoe UI",sans-serif;font-size:14px}a{color:inherit;text-decoration:none}button{font:inherit}.shell{min-height:100vh;display:grid;grid-template-columns:230px minmax(0,1fr)}.sidebar{position:sticky;top:0;height:100vh;border-right:1px solid var(--border);background:#10151C;padding:24px 16px;display:flex;flex-direction:column;gap:26px}.brand{display:grid;gap:5px;padding:0 8px}.brand-mark{width:34px;height:34px;border-radius:10px;display:grid;place-items:center;background:var(--blue);color:white;font-weight:900;letter-spacing:-.04em}.brand strong{font-size:15px;letter-spacing:.04em}.brand span{color:var(--muted);font-size:12px}nav{display:grid;gap:6px}.nav-link{min-height:38px;display:flex;align-items:center;gap:10px;border:1px solid transparent;border-radius:10px;padding:0 11px;color:var(--muted)}.nav-link:hover{border-color:var(--border);color:var(--text)}.nav-link.active{background:#1B2330;border-color:#35415A;color:var(--text)}.nav-dot{width:7px;height:7px;border-radius:50%;background:var(--disabled)}.active .nav-dot{background:var(--blue);box-shadow:0 0 0 4px rgba(79,124,255,.14)}.boundary{margin-top:auto;border:1px solid var(--border);border-radius:12px;background:var(--panel);padding:12px;color:var(--muted);font-size:12px;line-height:1.5}.boundary strong{color:var(--positive);display:block;margin-bottom:4px}.workspace{min-width:0;padding:22px 24px 30px}.topbar{min-height:66px;display:flex;align-items:center;gap:18px;border:1px solid var(--border);border-radius:14px;background:var(--panel);padding:12px 14px 12px 18px}.topbar-main{min-width:0;flex:1;display:grid;gap:4px}.topbar-label,.metric-label,.context-label{color:var(--muted);font-size:11px;text-transform:uppercase;letter-spacing:.1em}.topbar-title{font-weight:760;white-space:nowrap;overflow:hidden;text-overflow:ellipsis}.topbar-meta{display:flex;flex-wrap:wrap;gap:8px;align-items:center;color:var(--muted);font-size:12px}.actions{display:flex;gap:8px}.button{min-height:36px;border:1px solid var(--border);border-radius:9px;padding:0 13px;color:var(--disabled);background:var(--card);display:inline-flex;align-items:center;justify-content:center}.button.primary{color:#DDE5FF;border-color:#3E538C;background:#26375F}.button:disabled{cursor:not-allowed;opacity:.72}.mission-card{margin-top:18px;border:1px solid var(--border);border-radius:16px;background:var(--panel);padding:22px;display:grid;gap:16px}.eyebrow{color:#91A9FF;text-transform:uppercase;letter-spacing:.14em;font-size:11px;font-weight:800}h1{margin:7px 0 8px;font-size:clamp(26px,3vw,38px);line-height:1.1;letter-spacing:-.035em}.mission-summary{color:var(--muted);max-width:920px;line-height:1.6}.mission-metrics{display:grid;grid-template-columns:minmax(260px,2fr) repeat(3,minmax(130px,1fr));gap:10px}.metric{border:1px solid var(--border);border-radius:12px;background:var(--card);padding:12px;min-width:0}.metric-value{margin-top:6px;font-weight:700;overflow-wrap:anywhere}.progress-track{height:7px;margin-top:10px;background:#0F141A;border:1px solid #29313B;border-radius:999px;overflow:hidden}.progress-fill{display:block;height:100%;background:var(--blue)}.content-grid{margin-top:18px;display:grid;grid-template-columns:minmax(0,1fr) 310px;gap:18px;align-items:start}.panel{border:1px solid var(--border);border-radius:16px;background:var(--panel);overflow:hidden}.panel-header{padding:16px 18px;border-bottom:1px solid var(--border);display:flex;align-items:center;justify-content:space-between;gap:12px}.panel-header h2{margin:0;font-size:14px}.panel-header span{color:var(--muted);font-size:12px}.timeline{padding:18px}.timeline-item{position:relative;display:grid;grid-template-columns:28px minmax(0,1fr);gap:12px;padding-bottom:18px}.timeline-item:last-child{padding-bottom:0}.timeline-item:not(:last-child)::after{content:"";position:absolute;left:13px;top:27px;bottom:0;width:1px;background:var(--border)}.timeline-node{width:28px;height:28px;border-radius:50%;display:grid;place-items:center;border:1px solid var(--border);background:var(--card);color:var(--muted);font-weight:800;z-index:1}.timeline-item.complete .timeline-node{border-color:rgba(0,194,168,.55);color:var(--positive)}.timeline-item.fallback .timeline-node{border-color:rgba(124,92,255,.65);color:#A996FF}.timeline-item.attention .timeline-node{border-color:rgba(240,180,90,.65);color:var(--attention)}.timeline-item.blocked .timeline-node{border-color:rgba(240,106,106,.65);color:var(--blocked)}.timeline-body,.context-card{border:1px solid var(--border);border-radius:12px;background:var(--card);padding:12px 14px;min-width:0}.timeline-title,.context-top{display:flex;align-items:center;justify-content:space-between;gap:12px;font-weight:720}.timeline-detail,.context-detail{margin-top:7px;color:var(--muted);line-height:1.5;overflow-wrap:anywhere}.refs{margin-top:9px;display:flex;flex-wrap:wrap;gap:6px}.ref,.badge{display:inline-flex;align-items:center;min-height:24px;border-radius:999px;border:1px solid var(--border);padding:0 8px;font-size:11px;color:var(--muted);background:#141A21}.badge.complete,.badge.ready{color:var(--positive);border-color:rgba(0,194,168,.34)}.badge.fallback,.badge.active{color:#B9AAFF;border-color:rgba(124,92,255,.44)}.badge.attention{color:var(--attention);border-color:rgba(240,180,90,.4)}.badge.blocked{color:var(--blocked);border-color:rgba(240,106,106,.4)}.context-list{display:grid;gap:9px;padding:12px}.context-value{margin-top:6px;font-weight:730;overflow-wrap:anywhere}.fallback-note{margin:12px;border:1px solid rgba(124,92,255,.44);border-radius:12px;background:rgba(124,92,255,.08);padding:12px;color:#D9D1FF;line-height:1.5}.lower-grid{margin-top:18px;display:grid;grid-template-columns:1fr 1fr;gap:18px}.evidence-list{padding:14px 18px 18px;display:flex;flex-wrap:wrap;gap:8px}.empty{color:var(--muted);padding:18px;line-height:1.55}.product-cta{display:inline-flex;margin-top:12px;border:1px solid #405891;border-radius:9px;padding:9px 12px;color:#DFE7FF;background:#202A44}.candidate-box{margin:0 18px 18px;border:1px solid rgba(240,180,90,.34);border-radius:12px;background:rgba(240,180,90,.06);padding:12px;color:var(--muted);line-height:1.5}.candidate-box.verified{border-color:rgba(0,194,168,.34);background:rgba(0,194,168,.06)}.candidate-box.blocked{border-color:rgba(240,106,106,.34);background:rgba(240,106,106,.06)}.candidate-box strong{color:var(--text);display:block;margin-bottom:5px;overflow-wrap:anywhere}details{margin-top:18px;border:1px solid var(--border);border-radius:14px;background:var(--panel)}summary{cursor:pointer;padding:15px 18px;font-weight:720}.diagnostics{border-top:1px solid var(--border);padding:12px 18px 16px;display:grid;gap:7px;color:var(--muted);font-family:"Cascadia Code",Consolas,monospace;font-size:12px}.footer{margin-top:18px;display:flex;justify-content:space-between;gap:14px;color:var(--muted);font-size:12px}.footer a{color:#9CB0FF}@media(max-width:1040px){.shell{grid-template-columns:78px minmax(0,1fr)}.brand strong,.brand span,.nav-link span,.boundary{display:none}.sidebar{align-items:center;padding-inline:10px}.nav-link{justify-content:center;width:44px;padding:0}.content-grid{grid-template-columns:1fr}.mission-metrics{grid-template-columns:1fr 1fr}}@media(max-width:720px){.shell{display:block}.sidebar{position:static;height:auto;border-right:0;border-bottom:1px solid var(--border);flex-direction:row;align-items:center;overflow-x:auto}.brand{display:flex;align-items:center}nav{display:flex}.workspace{padding:14px}.topbar{align-items:flex-start;flex-direction:column}.actions{width:100%}.button{flex:1}.mission-metrics,.lower-grid{grid-template-columns:1fr}}
  </style>
</head>
<body data-nodal-os="mission-control-product-shell" data-local-only="@@LOCAL_ONLY@@" data-read-only="@@READ_ONLY@@" data-fixture-backed="@@FIXTURE_BACKED@@" data-workspace-selected="@@WORKSPACE_SELECTED@@" data-workspace-persisted="@@WORKSPACE_PERSISTED@@" data-real-mission-draft="@@REAL_MISSION_DRAFT@@" data-mission-draft-persisted="@@MISSION_DRAFT_PERSISTED@@" data-action-execution-enabled="@@ACTION_EXECUTION_ENABLED@@" data-action-approval-available="@@ACTION_APPROVAL_AVAILABLE@@" data-action-execution-state="@@ACTION_EXECUTION_STATE@@" data-action-executed="@@ACTION_EXECUTED@@" data-action-verified="@@ACTION_VERIFIED@@" data-rollback-available="@@ROLLBACK_AVAILABLE@@" data-action-rolled-back="@@ACTION_ROLLED_BACK@@" data-byok-configured="@@BYOK_CONFIGURED@@" data-model-connection-verified="@@MODEL_CONNECTION_VERIFIED@@" data-model-fallback-applied="@@MODEL_FALLBACK_APPLIED@@" data-product-authority="@@PRODUCT_AUTHORITY@@" data-onboarding-complete="@@ONBOARDING_COMPLETE@@">
  <div class="shell">
    <aside class="sidebar">
      <div class="brand"><div class="brand-mark">N</div><div><strong>NODAL OS</strong><span>AI Mission Control</span></div></div>
      <nav aria-label="Navegación principal"><a class="nav-link active" href="/"><span class="nav-dot"></span><span>Mission Control</span></a><a class="nav-link" href="#timeline"><span class="nav-dot"></span><span>Timeline</span></a><a class="nav-link" href="/workspace/select"><span class="nav-dot"></span><span>Workspace</span></a><a class="nav-link" href="/mission/new"><span class="nav-dot"></span><span>Mission</span></a><a class="nav-link" href="/mission/execution"><span class="nav-dot"></span><span>Approval</span></a><a class="nav-link" href="/models/config"><span class="nav-dot"></span><span>Models</span></a><a class="nav-link" href="#evidence"><span class="nav-dot"></span><span>Evidence</span></a><span class="nav-link" aria-disabled="true"><span class="nav-dot"></span><span>Settings</span></span></nav>
      <div class="boundary"><strong>Local-first</strong>Vista protegida por loopback. Sin secretos, cloud obligatorio ni autoridad de producción.</div>
    </aside>
    <main class="workspace">
      <header class="topbar" data-section-id="topbar"><div class="topbar-main"><div class="topbar-label">Misión actual</div><div class="topbar-title">@@GOAL@@</div><div class="topbar-meta"><span>@@MISSION_STATUS@@</span><span>•</span><span>@@PROGRESS@@%</span><span>•</span><span>@@ACTIVE_MODEL@@</span></div></div><div class="actions"><button class="button" type="button" disabled>Pausar</button>@@CONTROL_ACTION@@</div></header>
      <section class="mission-card" data-section-id="mission"><div><div class="eyebrow">@@PRODUCT_MODE@@ / @@DECISION@@</div><h1>Una misión, un timeline, control visible.</h1><div class="mission-summary">El runtime, los modelos, los fallbacks, la aprobación, la ejecución verificada, el workspace protegido y la evidencia se proyectan aquí sin crear una segunda fuente de verdad.</div></div><div class="mission-metrics"><div class="metric"><div class="metric-label">Progreso</div><div class="metric-value">@@PROGRESS@@% · @@CURRENT_STEP@@</div><div class="progress-track" aria-label="Progreso de misión"><span class="progress-fill" style="width:@@PROGRESS@@%"></span></div></div><div class="metric"><div class="metric-label">Workspace</div><div class="metric-value">@@WORKSPACE_STATE@@</div></div><div class="metric"><div class="metric-label">Modelo</div><div class="metric-value">@@ACTIVE_PROVIDER@@ / @@ACTIVE_MODEL@@</div></div><div class="metric"><div class="metric-label">Control humano</div><div class="metric-value">@@APPROVAL_STATE@@</div></div></div></section>
      <div class="content-grid"><section class="panel" id="timeline" data-section-id="timeline"><div class="panel-header"><h2>Timeline de misión</h2><span>@@TIMELINE_COUNT@@ eventos</span></div><div class="timeline">@@TIMELINE@@</div></section><aside class="panel" data-section-id="context"><div class="panel-header"><h2>Ruta rápida</h2><span>estado real</span></div>@@ONBOARDING@@<div class="panel-header"><h2>Contexto activo</h2><span>runtime</span></div><div class="context-list">@@CONTEXT@@</div><div class="fallback-note"><strong>Último fallback</strong><br>@@FALLBACK@@</div>@@ACTION_CANDIDATE@@</aside></div>
      <div class="lower-grid"><section class="panel" id="evidence" data-section-id="evidence"><div class="panel-header"><h2>Evidencia</h2><span>refs redacted</span></div><div class="evidence-list">@@EVIDENCE@@</div></section><section class="panel" data-section-id="resume"><div class="panel-header"><h2>Continuar misión</h2><span>@@NEXT_STAGE@@</span></div><div class="empty">@@NEXT_STEP@@<br><a class="product-cta" href="@@NEXT_HREF@@">@@NEXT_ACTION@@</a></div></section></div>
      <details data-section-id="diagnostics"><summary>Diagnóstico técnico y eventos</summary><div class="diagnostics">@@DIAGNOSTICS@@</div></details><div class="footer"><span>Local-only · bounded mission control · sin product authority</span></div>
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
            .Replace("@@REAL_MISSION_DRAFT@@", Bool(snapshot.RealMissionDraft), StringComparison.Ordinal)
            .Replace("@@MISSION_DRAFT_PERSISTED@@", Bool(snapshot.MissionDraftPersisted), StringComparison.Ordinal)
            .Replace("@@ACTION_EXECUTION_ENABLED@@", Bool(snapshot.ActionExecutionEnabled), StringComparison.Ordinal)
            .Replace("@@ACTION_APPROVAL_AVAILABLE@@", Bool(snapshot.ActionApprovalAvailable), StringComparison.Ordinal)
            .Replace("@@ACTION_EXECUTION_STATE@@", H(snapshot.ActionExecutionState ?? "not-configured"), StringComparison.Ordinal)
            .Replace("@@ACTION_EXECUTED@@", Bool(snapshot.ActionExecuted), StringComparison.Ordinal)
            .Replace("@@ACTION_VERIFIED@@", Bool(snapshot.ActionVerified), StringComparison.Ordinal)
            .Replace("@@ROLLBACK_AVAILABLE@@", Bool(snapshot.ActionRollbackAvailable), StringComparison.Ordinal)
            .Replace("@@ACTION_ROLLED_BACK@@", Bool(snapshot.ActionRolledBack), StringComparison.Ordinal)
            .Replace("@@BYOK_CONFIGURED@@", Bool(snapshot.ByokConfigured), StringComparison.Ordinal)
            .Replace("@@MODEL_CONNECTION_VERIFIED@@", Bool(snapshot.ModelConnectionVerified), StringComparison.Ordinal)
            .Replace("@@MODEL_FALLBACK_APPLIED@@", Bool(snapshot.ModelFallbackApplied), StringComparison.Ordinal)
            .Replace("@@PRODUCT_AUTHORITY@@", Bool(snapshot.ProductAuthorityGranted), StringComparison.Ordinal)
            .Replace("@@ONBOARDING_COMPLETE@@", Bool(onboardingComplete), StringComparison.Ordinal)
            .Replace("@@GOAL@@", H(snapshot.Goal), StringComparison.Ordinal)
            .Replace("@@MISSION_STATUS@@", H(snapshot.MissionStatus), StringComparison.Ordinal)
            .Replace("@@PROGRESS@@", progress, StringComparison.Ordinal)
            .Replace("@@ACTIVE_MODEL@@", H(snapshot.ActiveModel), StringComparison.Ordinal)
            .Replace("@@APPROVAL_STATE@@", H(snapshot.ApprovalState), StringComparison.Ordinal)
            .Replace("@@CONTROL_ACTION@@", controlAction, StringComparison.Ordinal)
            .Replace("@@PRODUCT_MODE@@", H(snapshot.ProductMode), StringComparison.Ordinal)
            .Replace("@@DECISION@@", H(snapshot.Decision), StringComparison.Ordinal)
            .Replace("@@CURRENT_STEP@@", H(snapshot.CurrentStep), StringComparison.Ordinal)
            .Replace("@@WORKSPACE_STATE@@", H(snapshot.WorkspaceState), StringComparison.Ordinal)
            .Replace("@@ACTIVE_PROVIDER@@", H(snapshot.ActiveProvider), StringComparison.Ordinal)
            .Replace("@@TIMELINE_COUNT@@", snapshot.Timeline.Count.ToString(CultureInfo.InvariantCulture), StringComparison.Ordinal)
            .Replace("@@TIMELINE@@", timeline, StringComparison.Ordinal)
            .Replace("@@ONBOARDING@@", onboarding, StringComparison.Ordinal)
            .Replace("@@CONTEXT@@", context, StringComparison.Ordinal)
            .Replace("@@FALLBACK@@", H(fallback), StringComparison.Ordinal)
            .Replace("@@ACTION_CANDIDATE@@", actionCandidate, StringComparison.Ordinal)
            .Replace("@@EVIDENCE@@", evidence, StringComparison.Ordinal)
            .Replace("@@NEXT_STAGE@@", H(nextStage), StringComparison.Ordinal)
            .Replace("@@NEXT_STEP@@", H(nextStep), StringComparison.Ordinal)
            .Replace("@@NEXT_HREF@@", H(nextHref), StringComparison.Ordinal)
            .Replace("@@NEXT_ACTION@@", H(nextAction), StringComparison.Ordinal)
            .Replace("@@DIAGNOSTICS@@", diagnostics, StringComparison.Ordinal);
    }

    private static (string Stage, string Text, string Action, string Href) NextAction(MissionControlProductShellSnapshot snapshot)
    {
        if (!snapshot.WorkspaceSelected)
            return ("Inicio", "Seleccionar y persistir un workspace local real manteniendo los paths absolutos fuera de la superficie.", "Seleccionar workspace local", "/workspace/select");
        if (!snapshot.RealMissionDraft)
            return ("Misión", "Crear una misión real, revisar su plan y preparar una acción reversible sin mutar el proyecto.", "Crear misión real", "/mission/new");
        if (snapshot.ActionExecutionState == "CandidateStale")
            return ("Revisión requerida", "El candidato quedó desactualizado porque cambió su precondición. NODAL no ejecutará nada hasta que revises y regeneres la misión.", "Revisar y regenerar misión", "/mission/new");
        if (snapshot.ActionExecutionState == "ResultChanged")
            return ("Resultado modificado", "El resultado cambió después de la verificación. El rollback automático permanece deshabilitado para no sobrescribir trabajo posterior.", "Inspeccionar resultado", "/mission/execution");
        if (snapshot.ActionExecutionState == "FailedClosed")
            return ("Ejecución detenida", "La operación se cerró de forma segura. Revisá la causa redacted y el estado actual antes de volver a intentarlo.", "Revisar causa", "/mission/execution");
        if (snapshot.ActionApprovalAvailable)
            return ("Aprobación", $"El candidato {snapshot.ActionCandidateKind} para {snapshot.ActionCandidateTarget} está listo. Una única aprobación exacta habilita la operación controlada, verificación y restore plan.", "Revisar, aprobar y ejecutar", "/mission/execution");
        if (snapshot.ActionRolledBack)
            return ("Misión restaurada", "El rollback quedó verificado. La misión puede recrearse o revisarse antes de una nueva acción.", "Revisar misión", "/mission/new");
        if (snapshot.ActionVerified && snapshot.ActionRollbackAvailable && !snapshot.ModelConnectionVerified)
            return ("Modelos", "La primera acción real quedó ejecutada y verificada con rollback disponible. Configurá BYOK y verificá una llamada real bajo la política autorizada.", "Configurar y probar modelos", "/models/config");
        if (snapshot.ActionVerified && snapshot.ModelConnectionVerified)
            return ("Continuidad", "El loop local y el paquete private-beta están listos. Podés iniciar la siguiente misión y conservar el mismo workspace, modelo y evidencia verificable.", "Crear siguiente misión", "/mission/new");
        return ("Revisión", "La misión y el candidato reversible están persistidos. Revisá la operación controlada antes de continuar.", "Abrir ejecución controlada", "/mission/execution");
    }

    private static string RenderQuickStart(MissionControlProductShellSnapshot snapshot)
    {
        var actionComplete = snapshot.ActionVerified || snapshot.ActionRolledBack;
        var steps = new[]
        {
            (Id: "workspace", Label: "1. Workspace", Complete: snapshot.WorkspaceSelected, Available: true, Href: "/workspace/select"),
            (Id: "mission", Label: "2. Misión", Complete: snapshot.RealMissionDraft, Available: snapshot.WorkspaceSelected, Href: "/mission/new"),
            (Id: "execution", Label: "3. Acción verificada", Complete: actionComplete, Available: snapshot.RealMissionDraft, Href: "/mission/execution"),
            (Id: "model", Label: "4. Modelo conectado", Complete: snapshot.ModelConnectionVerified, Available: actionComplete, Href: "/models/config")
        };
        var builder = new StringBuilder("<div class='context-list' data-section-id='quick-start'>");
        foreach (var step in steps)
        {
            var state = step.Complete ? "complete" : step.Available ? "attention" : "neutral";
            builder.Append("<article class='context-card' data-onboarding-step='")
                .Append(H(step.Id)).Append("'><div class='context-top'><span class='context-label'>")
                .Append(H(step.Label)).Append("</span><span class='badge ").Append(state).Append("'>")
                .Append(step.Complete ? "listo" : step.Available ? "continuar" : "pendiente")
                .Append("</span></div>");
            if (!step.Complete && step.Available)
                builder.Append("<a class='product-cta' href='").Append(H(step.Href)).Append("'>Abrir</a>");
            builder.Append("</article>");
        }
        return builder.Append("</div>").ToString();
    }

    private static string RenderControlAction(MissionControlProductShellSnapshot snapshot)
    {
        if (snapshot.ActionExecutionState == "CandidateStale")
            return "<a class=\"button primary\" href=\"/mission/new\">Revisar misión</a>";
        if (snapshot.ActionExecutionState == "ResultChanged")
            return "<a class=\"button primary\" href=\"/mission/execution\">Inspeccionar resultado</a>";
        if (snapshot.ActionExecutionState == "FailedClosed")
            return "<a class=\"button primary\" href=\"/mission/execution\">Revisar causa</a>";
        if (snapshot.ActionApprovalAvailable)
            return "<a class=\"button primary\" href=\"/mission/execution\">Revisar y aprobar</a>";
        if (snapshot.ActionExecuted)
            return "<a class=\"button primary\" href=\"/mission/execution\">Ver ejecución</a>";
        return $"<button class=\"button primary\" type=\"button\" disabled>{H(snapshot.ApprovalState)}</button>";
    }

    private static string RenderActionCandidate(MissionControlProductShellSnapshot snapshot)
    {
        if (!snapshot.RealMissionDraft || string.IsNullOrWhiteSpace(snapshot.ActionCandidateTarget))
            return string.Empty;

        var className = snapshot.ActionVerified || snapshot.ActionRolledBack
            ? "candidate-box verified"
            : snapshot.ActionExecutionState is "CandidateStale" or "ResultChanged" or "FailedClosed"
                ? "candidate-box blocked"
                : "candidate-box";
        var detail = snapshot.ActionRolledBack
            ? "La acción y su rollback pasaron verificación exacta."
            : snapshot.ActionVerified
                ? $"Ejecución verificada. Rollback disponible: {Bool(snapshot.ActionRollbackAvailable)}."
                : snapshot.ActionExecutionState switch
                {
                    "CandidateStale" => "La precondición cambió; la ejecución permanece bloqueada hasta revisar y regenerar la misión.",
                    "ResultChanged" => "El resultado verificado cambió; el rollback automático está deshabilitado para preservar el estado actual.",
                    "FailedClosed" => "La ejecución se detuvo de forma segura; revisá la causa antes de intentar una nueva operación.",
                    _ when snapshot.ActionApprovalAvailable => "Reversible, exact-hash scoped y listo para una aprobación de misión.",
                    _ => $"Estado de ejecución: {H(snapshot.ActionExecutionState ?? "not-configured")}."
                };
        return $"<div class=\"{className}\" data-section-id=\"action-candidate\"><strong>{H(snapshot.ActionCandidateKind)} · {H(snapshot.ActionCandidateTarget)}</strong>{detail}<br><a class=\"product-cta\" href=\"/mission/execution\">Abrir detalle controlado</a></div>";
    }

    private static string RenderTimeline(IReadOnlyList<MissionControlProductTimelineItem> items)
    {
        if (items.Count == 0)
            return "<div class=\"empty\">Todavía no hay eventos para esta misión.</div>";
        var builder = new StringBuilder();
        foreach (var item in items)
        {
            builder.Append("<article class=\"timeline-item ").Append(H(item.State)).Append("\" data-timeline-state=\"").Append(H(item.State)).Append("\"><div class=\"timeline-node\">").Append(StateIcon(item.State)).Append("</div><div class=\"timeline-body\"><div class=\"timeline-title\"><span>").Append(H(item.Title)).Append("</span><span class=\"badge ").Append(H(item.State)).Append("\">#").Append(item.Sequence).Append("</span></div><div class=\"timeline-detail\">").Append(H(item.Detail)).Append("</div>");
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
            builder.Append("<article class=\"context-card\" data-context-id=\"").Append(H(item.Id)).Append("\"><div class=\"context-top\"><span class=\"context-label\">").Append(H(item.Label)).Append("</span><span class=\"badge ").Append(H(item.State)).Append("\">").Append(H(item.State)).Append("</span></div><div class=\"context-value\">").Append(H(item.Value)).Append("</div><div class=\"context-detail\">").Append(H(item.Detail)).Append("</div></article>");
        }
        return builder.ToString();
    }

    private static string RenderEvidence(IReadOnlyList<string> evidenceRefs) =>
        evidenceRefs.Count == 0 ? "<span class=\"ref\">Sin evidencia</span>" : string.Join(string.Empty, evidenceRefs.Select(reference => $"<span class=\"ref\">{H(reference)}</span>"));

    private static string RenderDiagnostics(IReadOnlyList<string> diagnostics) =>
        diagnostics.Count == 0 ? "<span>sin diagnóstico</span>" : string.Join(string.Empty, diagnostics.Select(value => $"<span>{H(value)}</span>"));

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
