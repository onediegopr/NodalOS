using System.Net;

namespace OneBrain.Pilot;

internal static class TeachNodalLocalDevHtmlRenderer
{
    internal static string Render(TeachNodalLocalDevOperatorSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        var steps = string.Join(Environment.NewLine, snapshot.Steps.Select((step, index) => $$$"""
          <article class="step" data-step-id="{{{Html(step.StepId)}}}">
            <span class="dot">{{{index + 1}}}</span><div class="step-card">
              <header><h3>{{{Html(step.Intent)}}}</h3><span class="chip">verified</span></header>
              <p class="meta">{{{Html(step.Action)}}} · {{{Html(step.Capability)}}} · {{{Html(step.Target)}}}</p>
              <dl><div><dt>Observed label</dt><dd>{{{Html(step.ObservedLabel)}}} <small>{{{Html(step.LabelSource)}}}</small></dd></div>
              <div><dt>State</dt><dd>{{{Short(step.BeforeFingerprint)}}} → {{{Short(step.AfterFingerprint)}}}</dd></div>
              <div><dt>Verification</dt><dd>{{{step.VerificationFacts}}} semantic facts</dd></div></dl>
              {{{List("Parameters", step.ParameterRefs)}}}{{{List("Selectors", step.SelectorRefs)}}}
              {{{(step.PromptInjectionObserved ? "<p class=\"warning\">Observed prompt-injection text remained evidence and received no control authority.</p>" : string.Empty)}}}
            </div>
          </article>
          """));
        var findings = snapshot.Findings.Count == 0
            ? "<p class=\"muted\">No material finding.</p>"
            : string.Join(Environment.NewLine, snapshot.Findings.Select(value => $"<p class=\"notice\">{Html(value)}</p>"));
        var evidence = snapshot.EvidenceRefs.Count == 0
            ? "<li>none</li>"
            : string.Join(Environment.NewLine, snapshot.EvidenceRefs.Select(value => $"<li>{Html(value)}</li>"));
        var badge = snapshot.Accepted ? "ok" : "blocked";

        return $$$"""
          <!doctype html><html lang="en"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><meta name="robots" content="noindex,nofollow"><title>NODAL OS · Teach NODAL</title>
          <style>:root{color-scheme:dark;font-family:Inter,ui-sans-serif,system-ui,sans-serif;background:#0d1117;color:#f5f7fa}*{box-sizing:border-box}body{margin:0}.shell{min-height:100vh;display:grid;grid-template-columns:250px minmax(0,1fr) 310px;grid-template-rows:auto 1fr auto;background:#0d1117}.top,.bottom{grid-column:1/-1;background:#161b22;border-color:#30363d}.top{padding:18px 24px;border-bottom:1px solid #30363d;display:flex;justify-content:space-between;gap:16px;align-items:center}.top h1{margin:0;font-size:18px}.badge,.chip{border:1px solid #4f7cff;border-radius:999px;padding:6px 10px;color:#b8c7ff}.badge.ok,.chip{border-color:#2ea043;color:#7ee787}.badge.blocked{border-color:#d29922;color:#e3b341}.side,.right{padding:20px;background:#161b22}.side{border-right:1px solid #30363d}.right{border-left:1px solid #30363d}.main{padding:24px;min-width:0}.card,.step-card{background:#1c2128;border:1px solid #30363d;border-radius:12px;padding:16px;margin-bottom:16px}.metric,dl div{display:flex;justify-content:space-between;gap:14px;padding:8px 0;border-bottom:1px solid #30363d}.metric strong,dd{text-align:right;word-break:break-word}dd{margin:0}.step{display:grid;grid-template-columns:34px 1fr;gap:12px}.dot{width:28px;height:28px;border-radius:50%;display:grid;place-items:center;background:#4f7cff;font-weight:800}.step-card header{display:flex;justify-content:space-between;gap:12px}.step-card h3{margin:0;font-size:16px}.meta,.muted,small{color:#8b949e}.warning,.notice{border-left:3px solid #d29922;padding:9px 11px;background:#251f12;color:#e3b341}.notice{border-left-color:#7c5cff;background:#171b28;color:#d7d0ff}.list{padding-left:18px}.preview{border:1px dashed #6e7681;border-radius:10px;padding:14px;background:#14181e}.bottom{padding:12px 24px;border-top:1px solid #30363d;color:#8b949e;font-size:13px}@media(max-width:940px){.shell{grid-template-columns:1fr}.top,.bottom{grid-column:1}.side,.right{border:0;border-bottom:1px solid #30363d}}</style></head>
          <body><div class="shell" data-nodal-os="teach-nodal" data-local-dev-only="true" data-read-only="true" data-fixture-only="true" data-capture-prep="true" data-product-authority="false">
            <header class="top"><div><h1>Teach NODAL · {{{Html(snapshot.Title)}}}</h1><div class="muted">Explicit opt-in → bounded observations → recipe draft → verified skill → Process Memory</div></div><span class="badge {{{badge}}}">{{{Html(snapshot.Decision)}}}</span></header>
            <aside class="side"><section data-section-id="capture-prep"><h2>Capture prep</h2><div class="metric"><span>State</span><strong>{{{Html(snapshot.CaptureState)}}}</strong></div><div class="metric"><span>Explicit opt-in</span><strong>{{{snapshot.CaptureOptInRecorded}}}</strong></div><div class="metric"><span>Application bound</span><strong>{{{snapshot.CaptureApplicationScopeBound}}}</strong></div><div class="metric"><span>Observations</span><strong>{{{snapshot.CaptureObservationCount}}}</strong></div><div class="metric"><span>Per-step approvals</span><strong>{{{snapshot.PerStepApprovalsRequested}}}</strong></div><div class="metric"><span>Compilable</span><strong>{{{snapshot.CaptureCanCompileVerifiedSkill}}}</strong></div></section><section data-section-id="demonstration"><h2>Demonstration</h2><div class="metric"><span>ID</span><strong>{{{Html(snapshot.DemonstrationId)}}}</strong></div><div class="metric"><span>Compilation</span><strong>{{{Html(snapshot.CompilationDecision)}}}</strong></div><div class="metric"><span>Prompt injection</span><strong>{{{snapshot.PromptInjectionObserved}}}</strong></div><h3>Evidence</h3><ul class="list">{{{evidence}}}</ul></section></aside>
            <main class="main"><section class="card" data-section-id="teaching-timeline"><h2>Teaching timeline</h2><div>{{{steps}}}</div></section>
              <section class="card" data-section-id="compiled-outputs"><h2>Compiled outputs</h2><div class="metric"><span>Recipe</span><strong>{{{Html(snapshot.RecipeId)}}} · {{{Html(snapshot.RecipeReviewState)}}}</strong></div><div class="metric"><span>Verified skill</span><strong>{{{Html(snapshot.SkillId)}}} · {{{Html(snapshot.SkillState)}}}</strong></div><div class="metric"><span>Transitions</span><strong>{{{snapshot.TransitionCount}}}</strong></div><div class="metric"><span>Skill fingerprint</span><strong>{{{Short(snapshot.SkillFingerprint)}}}</strong></div><div class="metric"><span>Process Memory</span><strong>{{{Html(snapshot.ProcessMemoryId)}}} · {{{Html(snapshot.ProcessMemoryStatus)}}}</strong></div></section>
              <section class="card" data-section-id="trusted-control"><h2>Trusted control</h2><div class="metric"><span>Observed text changed goal</span><strong>{{{snapshot.PromptInjectionModifiedGoal}}}</strong></div><div class="metric"><span>Observed text expanded scope</span><strong>{{{snapshot.PromptInjectionExpandedScope}}}</strong></div><div class="metric"><span>Observed text published</span><strong>{{{snapshot.PromptInjectionPublishedExternally}}}</strong></div><p class="muted">Observed UI text is data. It cannot rewrite the mission.</p></section>
              <section class="card" data-section-id="findings"><h2>Findings</h2>{{{findings}}}</section></main>
            <aside class="right" data-section-id="limits"><h2>Learning boundary</h2><div class="metric"><span>Fixture only</span><strong>{{{snapshot.FixtureOnly}}}</strong></div><div class="metric"><span>Raw input retained</span><strong>{{{snapshot.CaptureRawInputStored}}}</strong></div><div class="metric"><span>Global hooks</span><strong>{{{snapshot.CaptureGlobalHooksUsed}}}</strong></div><div class="metric"><span>Capture authority</span><strong>{{{snapshot.CaptureExecutionAuthorityGranted}}}</strong></div><div class="metric"><span>Live recorder</span><strong>{{{snapshot.LiveRecorderUsed}}}</strong></div><div class="metric"><span>Input hooks</span><strong>{{{snapshot.MouseOrKeyboardHooksUsed}}}</strong></div><div class="metric"><span>Raw screenshot/DOM</span><strong>{{{snapshot.RawScreenshotStored || snapshot.RawDomStored}}}</strong></div><div class="metric"><span>Network</span><strong>{{{snapshot.NetworkUsed}}}</strong></div><div class="metric"><span>Product authority</span><strong>{{{snapshot.ProductAuthorityGranted}}}</strong></div><h3>Next action preview</h3><div class="preview" data-section-id="disabled-action" data-action-id="{{{snapshot.DisabledActionId}}}" aria-disabled="true"><strong>{{{snapshot.DisabledActionState}}}</strong><p>{{{Html(snapshot.NextSafeStep)}}}</p><p class="muted">Required operator signal: {{{snapshot.RequiredOperatorSignal}}}</p></div></aside>
            <footer class="bottom">Local/dev read-only teaching review. Opt-in and application scope are modeled; no live adapter, global hooks, raw input, network, execution authority or production exposure.</footer>
          </div></body></html>
          """;
    }

    private static string List(string title, IReadOnlyList<string> values) => values.Count == 0
        ? string.Empty
        : $"<h4>{Html(title)}</h4><ul class=\"list\">{string.Join(Environment.NewLine, values.Select(value => $"<li>{Html(value)}</li>"))}</ul>";

    private static string Short(string? value) => string.IsNullOrWhiteSpace(value) ? "none" : value[..Math.Min(12, value.Length)];
    private static string Html(string? value) => WebUtility.HtmlEncode(value ?? string.Empty);
}
