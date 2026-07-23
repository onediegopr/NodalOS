using System.Net;

namespace OneBrain.Pilot;

public static class NodalOsTeachNodalProductHtmlRenderer
{
    public static string Render(NodalOsTeachNodalProductSnapshot snapshot, string token)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentException.ThrowIfNullOrWhiteSpace(token);

        var stateClass = snapshot.State switch
        {
            NodalOsTeachNodalProductState.ReviewReady or NodalOsTeachNodalProductState.Saved => "ready",
            NodalOsTeachNodalProductState.FailedClosed => "blocked",
            _ => "attention"
        };
        var findings = snapshot.Findings.Count == 0
            ? "<div class=\"muted\">Sin findings activos.</div>"
            : $"<ul>{string.Join(string.Empty, snapshot.Findings.Select(value => $"<li>{H(value)}</li>"))}</ul>";
        var saved = snapshot.SavedDrafts.Count == 0
            ? "<div class=\"muted\">Todavía no hay drafts guardados.</div>"
            : string.Join(string.Empty, snapshot.SavedDrafts.Select(value =>
                $"<article class=\"saved\"><strong>{H(value.Title)}</strong><span>{H(value.AppProfileId)} · v{value.Version} · {value.StepCount} pasos</span></article>"));
        var main = snapshot.State == NodalOsTeachNodalProductState.FailedClosed
            ? RenderFailedClosed(token)
            : snapshot.Proposal is not null
                ? RenderProposal(snapshot.Proposal, token, snapshot.State)
                : snapshot.Bound
                    ? RenderCapture(snapshot, token)
                    : RenderBind(token);

        const string template = """
<!doctype html>
<html lang="es">
<head>
  <meta charset="utf-8">
  <meta name="viewport" content="width=device-width, initial-scale=1">
  <title>NODAL OS — Teach NODAL</title>
  <style>
    :root{color-scheme:dark;--bg:#0D1117;--panel:#161B22;--card:#1C2128;--border:#30363D;--text:#F5F7FA;--muted:#AAB4C0;--blue:#4F7CFF;--ok:#00C2A8;--warn:#F0B45A;--bad:#F06A6A}
    *{box-sizing:border-box}body{margin:0;min-height:100vh;background:var(--bg);color:var(--text);font:14px/1.5 Inter,Geist,Manrope,"Segoe UI",sans-serif}a{color:#AFC0FF;text-decoration:none}
    .shell{min-height:100vh;display:grid;grid-template-columns:220px minmax(0,1fr)}aside{border-right:1px solid var(--border);padding:24px 16px;background:#10151C;display:flex;flex-direction:column;gap:24px}.brand{font-weight:900;letter-spacing:.08em}.brand span{display:block;color:var(--muted);font-size:12px;font-weight:500;letter-spacing:0}.nav{display:grid;gap:6px}.nav a{padding:9px 11px;border:1px solid transparent;border-radius:9px;color:var(--muted)}.nav a.active{background:#1B2330;border-color:#35415A;color:var(--text)}.boundary{margin-top:auto;border:1px solid var(--border);border-radius:12px;background:var(--panel);padding:12px;color:var(--muted);font-size:12px}.boundary strong{color:var(--ok);display:block}
    main{padding:24px;max-width:1240px;width:100%;margin:0 auto}.hero,.card{border:1px solid var(--border);border-radius:16px;background:var(--panel)}.hero{padding:22px;display:flex;justify-content:space-between;gap:18px}.eyebrow{color:#91A9FF;text-transform:uppercase;letter-spacing:.13em;font-size:11px;font-weight:850}h1{margin:7px 0;font-size:34px;letter-spacing:-.04em}h2{margin:0;font-size:15px}.muted{color:var(--muted)}.badge{display:inline-flex;border:1px solid var(--border);border-radius:999px;padding:5px 9px;font-size:11px;height:max-content}.badge.ready{color:var(--ok);border-color:rgba(0,194,168,.45)}.badge.attention{color:var(--warn);border-color:rgba(240,180,90,.45)}.badge.blocked{color:var(--bad);border-color:rgba(240,106,106,.45)}
    .grid{margin-top:18px;display:grid;grid-template-columns:minmax(0,1.45fr) minmax(300px,.7fr);gap:18px;align-items:start}.stack{display:grid;gap:18px}.card{padding:18px}.card-head{display:flex;justify-content:space-between;gap:12px;align-items:center;padding-bottom:13px;border-bottom:1px solid var(--border)}form{display:grid;gap:13px;margin-top:15px}fieldset{border:1px solid var(--border);border-radius:12px;padding:13px;display:grid;gap:11px}legend{color:#C8D4FF;font-weight:800;padding:0 7px}.fields{display:grid;grid-template-columns:1fr 1fr;gap:11px}label{display:grid;gap:6px;color:var(--muted);font-size:12px}label.full{grid-column:1/-1}input,select,textarea{width:100%;border:1px solid var(--border);border-radius:9px;background:#0F141A;color:var(--text);padding:9px 10px;font:inherit}textarea{min-height:84px;resize:vertical}.check{display:flex;align-items:center;gap:8px}.check input{width:auto}.actions{display:flex;flex-wrap:wrap;gap:9px}button{min-height:39px;border-radius:9px;padding:0 13px;font:inherit;font-weight:800;cursor:pointer}.primary{border:1px solid #405891;background:#26375F;color:#EEF2FF}.secondary{border:1px solid var(--border);background:var(--card);color:var(--text)}.danger{border:1px solid rgba(240,106,106,.45);background:rgba(240,106,106,.08);color:#FFB5B5}.notice{border-left:3px solid var(--warn);background:#251F12;color:#EAC77A;padding:11px 12px;border-radius:8px}.notice.ok{border-color:var(--ok);background:rgba(0,194,168,.08);color:#B6F0E6}.notice.blocked{border-color:var(--bad);background:rgba(240,106,106,.08);color:#FFB5B5}.metrics{display:grid;gap:9px;margin-top:13px}.metric,.saved,.step{border:1px solid var(--border);border-radius:10px;background:var(--card);padding:10px}.metric span,.saved span{display:block;color:var(--muted);font-size:11px}.saved{display:grid;gap:4px;margin-top:8px}.steps{display:grid;gap:10px;margin-top:13px}.step-head{display:flex;justify-content:space-between;gap:10px}.step code{display:block;color:#C7D3E0;margin-top:7px;overflow-wrap:anywhere}.step-copy{color:var(--muted);margin-top:7px}.footer{margin-top:18px;display:flex;justify-content:space-between;gap:12px;color:var(--muted);font-size:12px}@media(max-width:900px){.shell{display:block}aside{border-right:0;border-bottom:1px solid var(--border)}.grid,.fields{grid-template-columns:1fr}}
  </style>
</head>
<body data-nodal-os="teach-nodal-product-surface" data-state="@@STATE@@" data-bound="@@BOUND@@" data-video-stored="false" data-audio-stored="false" data-raw-input-stored="false" data-global-hooks="false" data-replay-enabled="false" data-execution-authority="false" data-product-authority="false">
<div class="shell">
  <aside>
    <div class="brand">NODAL OS<span>Teach once · review before reuse</span></div>
    <nav class="nav"><a href="/">Mission Control</a><a href="/workspace/select">Workspace</a><a href="/mission/new">Mission</a><a href="/models/config">Models</a><a class="active" href="/teach">Teach NODAL</a></nav>
    <div class="boundary"><strong>Review-only</strong>Una aplicación por vez. Sin video/audio persistido, hooks globales, replay o autoridad implícita.</div>
  </aside>
  <main>
    <section class="hero"><div><div class="eyebrow">Semantic workflow recorder</div><h1>Mostralo una vez. Revisalo antes de guardarlo.</h1><div class="muted">Teach NODAL observa snapshots UIA antes y después de cada paso. La intención se escribe o dicta con Windows (Win + H). No guarda teclas, coordenadas, video, audio ni screenshots.</div></div><span class="badge @@STATE_CLASS@@">@@STATE@@</span></section>
    <div class="grid">
      <div class="stack">@@MAIN@@</div>
      <aside class="stack">
        <section class="card"><div class="card-head"><h2>Límites activos</h2><span class="badge ready">fail-closed</span></div><div class="metrics">
          <div class="metric"><span>Aplicación</span><strong>@@BOUND_APP@@</strong></div>
          <div class="metric"><span>Proceso</span><strong>@@BOUND_PROCESS@@</strong></div>
          <div class="metric"><span>Perfil</span><strong>@@PROFILE@@</strong></div>
          <div class="metric"><span>Pasos observados</span><strong>@@COUNT@@</strong></div>
          <div class="metric"><span>Replay</span><strong>Deshabilitado</strong></div>
          <div class="metric"><span>Autoridad</span><strong>Ninguna</strong></div>
        </div></section>
        <section class="card"><div class="card-head"><h2>Findings</h2><span class="badge">redacted</span></div>@@FINDINGS@@</section>
        <section class="card"><div class="card-head"><h2>Drafts locales</h2><span class="badge">review-only</span></div>@@SAVED@@</section>
      </aside>
    </div>
    <div class="footer"><span>Local-first · UIA semántico · no scripts · no replay</span><a href="/">Volver a Mission Control</a></div>
  </main>
</div>
</body></html>
""";

        return template
            .Replace("@@STATE@@", H(snapshot.State.ToString()), StringComparison.Ordinal)
            .Replace("@@STATE_CLASS@@", stateClass, StringComparison.Ordinal)
            .Replace("@@BOUND@@", Bool(snapshot.Bound), StringComparison.Ordinal)
            .Replace("@@BOUND_APP@@", H(snapshot.BoundApplication), StringComparison.Ordinal)
            .Replace("@@BOUND_PROCESS@@", H(snapshot.BoundProcess), StringComparison.Ordinal)
            .Replace("@@PROFILE@@", H(snapshot.AppProfileId), StringComparison.Ordinal)
            .Replace("@@COUNT@@", snapshot.ObservationCount.ToString(System.Globalization.CultureInfo.InvariantCulture), StringComparison.Ordinal)
            .Replace("@@MAIN@@", main, StringComparison.Ordinal)
            .Replace("@@FINDINGS@@", findings, StringComparison.Ordinal)
            .Replace("@@SAVED@@", saved, StringComparison.Ordinal);
    }

    private static string RenderBind(string token) => $"""
<section class="card">
  <div class="card-head"><h2>1. Vincular una aplicación</h2><span class="badge attention">2 segundos</span></div>
  <div class="notice">Después de confirmar, cambiá a la aplicación que querés enseñar. NODAL vinculará únicamente la ventana que esté en foreground al finalizar la cuenta.</div>
  <form method="post" action="{NodalOsTeachNodalProductEndpointMapper.BindRoute}">
    <input type="hidden" name="{NodalOsTeachNodalProductEndpointMapper.TokenField}" value="{H(token)}">
    <div class="fields">
      <label class="full">Nombre del workflow<input name="workflowTitle" maxlength="180" required placeholder="Ej: Preparar y guardar un informe mensual"></label>
      <label>Perfil de aplicación<input name="appProfileName" maxlength="80" required placeholder="Ej: excel-reporting"></label>
    </div>
    <div class="actions"><button class="primary" type="submit">Vincular aplicación foreground</button></div>
  </form>
</section>
<section class="card"><div class="card-head"><h2>Qué se captura</h2><span class="badge ready">mínimo</span></div><div class="metrics">
  <div class="metric"><span>Sí</span><strong>Estado UIA antes/después, rol, label redactado, intención y referencias de variables</strong></div>
  <div class="metric"><span>No</span><strong>Video, audio, texto crudo, coordenadas, screenshots, DOM, secretos o hooks globales</strong></div>
</div></section>
""";

    private static string RenderCapture(NodalOsTeachNodalProductSnapshot snapshot, string token)
    {
        var finish = snapshot.ObservationCount == 0
            ? string.Empty
            : $"""<form method="post" action="{NodalOsTeachNodalProductEndpointMapper.FinishRoute}"><input type="hidden" name="{NodalOsTeachNodalProductEndpointMapper.TokenField}" value="{H(token)}"><button class="secondary" type="submit">Terminar y abrir propuesta</button></form>""";
        return $"""
<section class="card">
  <div class="card-head"><h2>2. Grabar un paso semántico</h2><span class="badge ready">{snapshot.ObservationCount} pasos</span></div>
  <div class="notice">Al enviar: tenés 2 segundos para cambiar a la aplicación y luego 8 segundos para realizar exactamente una acción. Mantené esa aplicación en foreground hasta terminar.</div>
  <form method="post" action="{NodalOsTeachNodalProductEndpointMapper.CaptureRoute}">
    <input type="hidden" name="{NodalOsTeachNodalProductEndpointMapper.TokenField}" value="{H(token)}">
    <div class="fields">
      <label>Acción<select name="kind"><option>Click</option><option>Type</option><option>Select</option><option>Navigate</option><option>Wait</option></select></label>
      <label>Rol esperado<select name="targetRole"><option>Button</option><option>Edit</option><option>ComboBox</option><option>ListItem</option><option>Link</option><option>Document</option></select></label>
      <label class="full">Intención del paso · podés dictar con Win + H<textarea name="intent" maxlength="300" required placeholder="Ej: Guardar el documento dentro del flujo ya autorizado."></textarea></label>
      <label>Label visible exacto<input name="targetLabel" maxlength="240" required placeholder="Ej: Guardar"></label>
      <label>Variable para Type<input name="parameterName" maxlength="40" placeholder="VALUE"></label>
      <label class="full">Referencia del valor<input name="parameterReference" maxlength="180" placeholder="variable-ref:VALUE"></label>
      <label class="check full"><input type="checkbox" name="secretByReference"> El valor es sensible y usa una referencia opaca secret-ref:</label>
    </div>
    <div class="actions"><button class="primary" type="submit">Capturar este paso</button></div>
  </form>
  <div class="actions">{finish}<form method="post" action="{NodalOsTeachNodalProductEndpointMapper.DiscardRoute}"><input type="hidden" name="{NodalOsTeachNodalProductEndpointMapper.TokenField}" value="{H(token)}"><button class="danger" type="submit">Descartar sesión</button></form></div>
</section>
<section class="card"><div class="card-head"><h2>Estado de sesión</h2><span class="badge">application-scoped</span></div><div class="metrics"><div class="metric"><span>Aplicación</span><strong>{H(snapshot.BoundApplication)}</strong></div><div class="metric"><span>Pasos</span><strong>{snapshot.ObservationCount} / 12</strong></div></div></section>
""";
    }

    private static string RenderProposal(
        NodalOsTeachNodalProductProposal proposal,
        string token,
        NodalOsTeachNodalProductState state)
    {
        var reviewReady = state == NodalOsTeachNodalProductState.ReviewReady;
        var steps = string.Join(string.Empty, proposal.Steps.Select(step => reviewReady
            ? $"""
<article class="step">
  <div class="step-head"><strong>{H(step.StepId)} · {H(step.Kind)}</strong><span class="badge {(step.Verified ? "ready" : "attention")}">{(step.Verified ? "verified" : "review")}</span></div>
  <label>Intención<textarea name="stepIntent_{H(step.StepId)}" maxlength="300">{H(step.Intent)}</textarea></label>
  <label>Target visible<input name="stepTarget_{H(step.StepId)}" maxlength="240" value="{H(step.TargetLabel)}"></label>
  <code>{H(step.TargetRole)} · {H(Short(step.BeforeFingerprint))} → {H(Short(step.AfterFingerprint))}</code>
</article>
"""
            : $"""
<article class="step">
  <div class="step-head"><strong>{H(step.StepId)} · {H(step.Kind)}</strong><span class="badge attention">review-only</span></div>
  <div class="step-copy"><strong>Intención:</strong> {H(step.Intent)}</div>
  <div class="step-copy"><strong>Target:</strong> {H(step.TargetLabel)} · {H(step.TargetRole)}</div>
  <code>{H(Short(step.BeforeFingerprint))} → {H(Short(step.AfterFingerprint))}</code>
</article>
"""));
        var save = reviewReady && proposal.SaveAllowed
            ? $"""<form method="post" action="{NodalOsTeachNodalProductEndpointMapper.SaveRoute}"><input type="hidden" name="{NodalOsTeachNodalProductEndpointMapper.TokenField}" value="{H(token)}"><button class="primary" type="submit">{(proposal.Kind == NodalOsTeachNodalProposalKind.UpdateCandidate ? "Guardar nueva versión del draft" : "Guardar draft local")}</button></form>"""
            : string.Empty;
        var savedNotice = state == NodalOsTeachNodalProductState.Saved
            ? "<div class=\"notice ok\">Draft guardado localmente. Continúa review-only y no puede ejecutarse. Para enseñar otro workflow, descartá esta vista y empezá una sesión nueva.</div>"
            : string.Empty;
        var reviewForm = reviewReady
            ? $"""
<form method="post" action="{NodalOsTeachNodalProductEndpointMapper.ReviewRoute}">
  <input type="hidden" name="{NodalOsTeachNodalProductEndpointMapper.TokenField}" value="{H(token)}">
  <input type="hidden" name="proposalVersion" value="{proposal.Version}">
  <input type="hidden" name="proposalUpdatedAtUtc" value="{H(proposal.UpdatedAtUtc.ToUniversalTime().ToString("O", System.Globalization.CultureInfo.InvariantCulture))}">
  <div class="fields">
    <label class="full">Título<input name="proposalTitle" maxlength="180" value="{H(proposal.Title)}" required></label>
    <label class="full">Resumen<textarea name="proposalSummary" maxlength="500" required>{H(proposal.Summary)}</textarea></label>
  </div>
  <div class="steps">{steps}</div>
  <div class="actions"><button class="secondary" type="submit">Aplicar correcciones al draft</button></div>
</form>
"""
            : $"""<div class="metrics"><div class="metric"><span>Título</span><strong>{H(proposal.Title)}</strong></div><div class="metric"><span>Resumen</span><strong>{H(proposal.Summary)}</strong></div></div><div class="steps">{steps}</div>""";
        return $"""
<section class="card">
  <div class="card-head"><h2>3. Revisar propuesta</h2><span class="badge {(proposal.SaveAllowed ? "ready" : "blocked")}">{H(proposal.Kind.ToString())}</span></div>
  {savedNotice}
  <div class="notice">La propuesta es editable antes de guardar. Guardarla no habilita replay, scripts ni ejecución. Toda corrección invalida la metadata de verificación previa y requiere re-verificación futura.</div>
  {reviewForm}
  <div class="actions">{save}<form method="post" action="{NodalOsTeachNodalProductEndpointMapper.DiscardRoute}"><input type="hidden" name="{NodalOsTeachNodalProductEndpointMapper.TokenField}" value="{H(token)}"><button class="danger" type="submit">Descartar y empezar de nuevo</button></form></div>
</section>
<section class="card"><div class="card-head"><h2>Contrato de salida</h2><span class="badge">review-only</span></div><div class="metrics">
  <div class="metric"><span>Compilación</span><strong>{H(proposal.CompilationDecision)} · {H(proposal.CompilationCode)}</strong></div>
  <div class="metric"><span>Scripts</span><strong>No incluidos</strong></div>
  <div class="metric"><span>Replay</span><strong>Deshabilitado</strong></div>
  <div class="metric"><span>Autoridad</span><strong>Ninguna</strong></div>
</div></section>
""";
    }

    private static string RenderFailedClosed(string token) => $"""
<section class="card">
  <div class="card-head"><h2>Sesión detenida</h2><span class="badge blocked">failed closed</span></div>
  <div class="notice blocked">La aplicación y la propuesta activas fueron liberadas. No se puede continuar, guardar ni capturar hasta descartar esta sesión.</div>
  <form method="post" action="{NodalOsTeachNodalProductEndpointMapper.DiscardRoute}">
    <input type="hidden" name="{NodalOsTeachNodalProductEndpointMapper.TokenField}" value="{H(token)}">
    <div class="actions"><button class="danger" type="submit">Descartar y volver al inicio</button></div>
  </form>
</section>
""";

    private static string H(string? value) => WebUtility.HtmlEncode(value ?? string.Empty);
    private static string Bool(bool value) => value ? "true" : "false";
    private static string Short(string? value) => string.IsNullOrWhiteSpace(value) ? "not available" : value[..Math.Min(18, value.Length)];
}
