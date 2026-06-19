using System.Net;
using System.Text;
using OneBrain.AgentOperations.Contracts;

namespace OneBrain.AgentOperations.Core;

public sealed class NodalOsMissionControlVisualValidator
{
    private readonly NodalOsRedactionService redaction;

    public NodalOsMissionControlVisualValidator()
        : this(new NodalOsRedactionService())
    {
    }

    public NodalOsMissionControlVisualValidator(NodalOsRedactionService redaction) =>
        this.redaction = redaction;

    public NodalOsCoreRuntimeValidationResult ValidateLayoutSpec(NodalOsMissionControlLayoutSpec layoutSpec)
    {
        var errors = new List<string>();
        var warnings = new List<string>();

        AddRequired(errors, layoutSpec.LayoutSpecId, "LayoutSpecId is required.");
        if (!string.Equals(layoutSpec.ProjectOperationalName, "NODAL OS", StringComparison.Ordinal))
            errors.Add("Layout spec operational name must be NODAL OS.");
        if (layoutSpec.Breakpoints.Count != 4)
            errors.Add("Layout spec must define compact, standard, wide and ultrawide desktop breakpoints.");
        if (!layoutSpec.ReadOnlyBadgeAlwaysVisible)
            errors.Add("Read-only badge must remain visible across desktop breakpoints.");
        if (!layoutSpec.NoRuntimeBadgeAlwaysVisible)
            errors.Add("No-runtime badge must remain visible across desktop breakpoints.");
        if (!layoutSpec.DisabledControlsRemainVisible)
            errors.Add("Disabled controls must remain visible as disabled.");
        if (layoutSpec.ModalTrapAllowed)
            errors.Add("Layout spec cannot allow modal trap behavior.");
        if (layoutSpec.CanExecuteOrMutateState)
            errors.Add("Layout spec cannot execute or mutate state.");
        if (layoutSpec.CallsBrowserRuntime)
            errors.Add("Layout spec cannot call browser runtime.");
        if (layoutSpec.UsesExternalCssFramework)
            errors.Add("Layout spec cannot require an external CSS framework.");
        if (layoutSpec.CreatedAt == default)
            errors.Add("CreatedAt is required.");

        foreach (var breakpoint in layoutSpec.Breakpoints)
            ValidateBreakpoint(breakpoint, errors);

        return Result(errors, warnings);
    }

    public NodalOsCoreRuntimeValidationResult ValidateAcceptancePack(NodalOsMissionControlStaticUxAcceptancePack acceptancePack)
    {
        var errors = new List<string>();
        var warnings = new List<string>();

        AddRequired(errors, acceptancePack.AcceptancePackId, "AcceptancePackId is required.");
        AddRequired(errors, acceptancePack.RenderedPreviewArtifactRef, "RenderedPreviewArtifactRef is required.");
        if (!string.Equals(acceptancePack.ProjectOperationalName, "NODAL OS", StringComparison.Ordinal))
            errors.Add("Acceptance pack operational name must be NODAL OS.");
        if (!acceptancePack.ReadOnlyUi)
            errors.Add("Acceptance pack must mark UI as read-only.");
        if (acceptancePack.CanAuthorizeExecution)
            errors.Add("Acceptance pack cannot authorize execution.");
        if (acceptancePack.RuntimeExecutionAllowed)
            errors.Add("Acceptance pack cannot allow runtime execution.");
        if (acceptancePack.ProductiveFrontendAppIntroduced)
            errors.Add("Acceptance pack cannot introduce a productive app.");
        if (acceptancePack.CreatedAt == default)
            errors.Add("CreatedAt is required.");

        ValidateChecklist(acceptancePack.UxChecklistRedacted, "UxChecklistRedacted", errors);
        ValidateChecklist(acceptancePack.GuardrailChecklistRedacted, "GuardrailChecklistRedacted", errors);
        ValidateChecklist(acceptancePack.VisualDirectionChecklistRedacted, "VisualDirectionChecklistRedacted", errors);
        ValidateChecklist(acceptancePack.ContentChecklistRedacted, "ContentChecklistRedacted", errors);
        ValidateChecklist(acceptancePack.NamingChecklistRedacted, "NamingChecklistRedacted", errors);
        ValidateChecklist(acceptancePack.AccessibilityBasicsChecklistRedacted, "AccessibilityBasicsChecklistRedacted", errors);
        ValidateChecklist(acceptancePack.NextUxGapsRedacted, "NextUxGapsRedacted", errors);

        return Result(errors, warnings);
    }

    public string Redact(string? value) => redaction.RedactValue(value).Value;

    private void ValidateBreakpoint(NodalOsMissionControlLayoutBreakpoint breakpoint, List<string> errors)
    {
        if (breakpoint.MinimumWidthPx < 900)
            errors.Add("Desktop breakpoint minimum width must be at least 900px.");
        ValidateSafeText(errors, "TimelineDensityRedacted", breakpoint.TimelineDensityRedacted);
        ValidateSafeText(errors, "ApprovalCardBehaviorRedacted", breakpoint.ApprovalCardBehaviorRedacted);
        ValidateSafeText(errors, "EvidencePanelBehaviorRedacted", breakpoint.EvidencePanelBehaviorRedacted);
        ValidateSafeText(errors, "GuardrailExplainerBehaviorRedacted", breakpoint.GuardrailExplainerBehaviorRedacted);
        ValidateSafeText(errors, "OnboardingCardBehaviorRedacted", breakpoint.OnboardingCardBehaviorRedacted);
    }

    private void ValidateChecklist(IReadOnlyList<string> values, string fieldName, List<string> errors)
    {
        if (values.Count == 0)
            errors.Add($"{fieldName} must not be empty.");
        foreach (var value in values)
            ValidateSafeText(errors, fieldName, value);
    }

    private void ValidateSafeText(List<string> errors, string fieldName, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return;

        if (redaction.ContainsSensitiveField(fieldName, value) || redaction.ContainsSensitiveContent(value))
            errors.Add($"{fieldName} contains sensitive content and must be redacted before Mission Control visual output.");
    }

    private static void AddRequired(List<string> errors, string? value, string message)
    {
        if (string.IsNullOrWhiteSpace(value))
            errors.Add(message);
    }

    private static NodalOsCoreRuntimeValidationResult Result(List<string> errors, List<string> warnings) =>
        new()
        {
            IsValid = errors.Count == 0,
            RuntimeExecutionAllowed = false,
            RuntimeExecutionDeferred = true,
            RequiresGlobalPolicyEvaluation = true,
            RequiresEvidenceRedaction = true,
            Errors = errors.Distinct(StringComparer.Ordinal).ToArray(),
            Warnings = warnings.Distinct(StringComparer.Ordinal).ToArray()
        };
}

public sealed class NodalOsMissionControlVisualService
{
    private readonly NodalOsMissionControlVisualValidator validator;
    private readonly NodalOsMissionControlShellValidator shellValidator;
    private readonly NodalOsMissionControlGuidanceService guidanceService;

    public NodalOsMissionControlVisualService()
        : this(
            new NodalOsMissionControlVisualValidator(),
            new NodalOsMissionControlShellValidator(),
            new NodalOsMissionControlGuidanceService())
    {
    }

    public NodalOsMissionControlVisualService(
        NodalOsMissionControlVisualValidator validator,
        NodalOsMissionControlShellValidator shellValidator,
        NodalOsMissionControlGuidanceService guidanceService)
    {
        this.validator = validator;
        this.shellValidator = shellValidator;
        this.guidanceService = guidanceService;
    }

    public NodalOsMissionControlLayoutSpec CreateResponsiveDesktopLayoutSpec() =>
        new()
        {
            LayoutSpecId = "mission-control-layout-spec-m489-m491",
            ProjectOperationalName = "NODAL OS",
            Breakpoints =
            [
                Breakpoint(NodalOsMissionControlDesktopBreakpointKind.CompactDesktop, 1024, NodalOsMissionControlDensityMode.Compact, NodalOsMissionControlPanelBehavior.Collapsed, NodalOsMissionControlPanelBehavior.Collapsed, NodalOsMissionControlPanelBehavior.Expanded, "Timeline uses compact markers with concise event copy.", "Approval cards stack above evidence summary.", "Evidence panel collapses to ref chips.", "Guardrail explainers show compact badges.", "Onboarding cards collapse into a horizontal rail."),
                Breakpoint(NodalOsMissionControlDesktopBreakpointKind.StandardDesktop, 1280, NodalOsMissionControlDensityMode.Comfortable, NodalOsMissionControlPanelBehavior.Expanded, NodalOsMissionControlPanelBehavior.Expanded, NodalOsMissionControlPanelBehavior.Expanded, "Timeline stays central with readable spacing.", "Approval cards stay in the right command panel.", "Evidence refs remain visible below approval context.", "Guardrail explainers stay visible in right panel.", "Onboarding cards appear under the active mission summary."),
                Breakpoint(NodalOsMissionControlDesktopBreakpointKind.WideDesktop, 1600, NodalOsMissionControlDensityMode.Comfortable, NodalOsMissionControlPanelBehavior.Expanded, NodalOsMissionControlPanelBehavior.Expanded, NodalOsMissionControlPanelBehavior.Expanded, "Timeline expands with evidence refs inline.", "Approval cards include affected resources and disabled reasons.", "Evidence panel shows safe metadata columns.", "Guardrail explainers use two-column grouping.", "Onboarding cards sit beside empty states."),
                Breakpoint(NodalOsMissionControlDesktopBreakpointKind.UltrawideControlRoom, 2200, NodalOsMissionControlDensityMode.DenseLogHeavy, NodalOsMissionControlPanelBehavior.Expanded, NodalOsMissionControlPanelBehavior.Expanded, NodalOsMissionControlPanelBehavior.Expanded, "Timeline and LOG preview can share a dense control-room rhythm.", "Approval cards pin high-severity items first.", "Evidence panel stays ref-only with metadata density.", "Guardrail explainers remain always visible.", "Onboarding cards use compact status tiles.")
            ],
            ReadOnlyBadgeAlwaysVisible = true,
            NoRuntimeBadgeAlwaysVisible = true,
            DisabledControlsRemainVisible = true,
            ModalTrapAllowed = false,
            CanExecuteOrMutateState = false,
            CallsBrowserRuntime = false,
            UsesExternalCssFramework = false,
            CreatedAt = DateTimeOffset.UtcNow
        };

    public NodalOsMissionControlStaticUxAcceptancePack CreateStaticUxAcceptancePack(string renderedPreviewArtifactRef) =>
        new()
        {
            AcceptancePackId = "mission-control-static-ux-acceptance-m489-m491",
            ProjectOperationalName = "NODAL OS",
            RenderedPreviewArtifactRef = validator.Redact(renderedPreviewArtifactRef),
            UxChecklistRedacted =
            [
                "Mission Control purpose is understandable within the first screen.",
                "Timeline, approvals, evidence and LOG preview are all visible without execution.",
                "Disabled controls explain why they are unavailable."
            ],
            GuardrailChecklistRedacted =
            [
                "Read-only badge is always visible.",
                "No runtime badge is always visible.",
                "No cloud and no LLM indicators are visible.",
                "Guardrail explainers do not unlock execution."
            ],
            VisualDirectionChecklistRedacted =
            [
                "Dark-first visual direction is applied.",
                "Mission Control aesthetic is premium and clean.",
                "The shell avoids classic ERP, task-board and workflow-designer patterns."
            ],
            ContentChecklistRedacted =
            [
                "Approval copy is disabled and non-authoritative.",
                "Evidence is ref-only.",
                "Observability explains blocked state and next safe step."
            ],
            NamingChecklistRedacted =
            [
                "NODAL OS is the operational project name.",
                "Forbidden historical names are absent from new rendered surfaces."
            ],
            AccessibilityBasicsChecklistRedacted =
            [
                "Semantic headings are present.",
                "Disabled controls expose aria-disabled.",
                "Contrast uses dark panels with high-contrast text."
            ],
            NextUxGapsRedacted =
            [
                "Static visual QA can be expanded with screenshot review.",
                "No productive app shell exists yet.",
                "Real interaction remains future-gated."
            ],
            ReadOnlyUi = true,
            CanAuthorizeExecution = false,
            RuntimeExecutionAllowed = false,
            ProductiveFrontendAppIntroduced = false,
            CreatedAt = DateTimeOffset.UtcNow
        };

    public string RenderStaticUxPreview(
        NodalOsMissionControlShellPreview shell,
        IReadOnlyList<NodalOsMissionControlEmptyState>? emptyStates = null,
        IReadOnlyList<NodalOsMissionControlOnboardingStep>? onboardingSteps = null,
        IReadOnlyList<NodalOsMissionControlGuardrailExplainer>? guardrailExplainers = null,
        NodalOsMissionControlLayoutSpec? layoutSpec = null)
    {
        var shellValidation = shellValidator.ValidateShell(shell);
        if (!shellValidation.IsValid)
            throw new InvalidOperationException(string.Join(" | ", shellValidation.Errors));

        var layout = layoutSpec ?? CreateResponsiveDesktopLayoutSpec();
        var layoutValidation = validator.ValidateLayoutSpec(layout);
        if (!layoutValidation.IsValid)
            throw new InvalidOperationException(string.Join(" | ", layoutValidation.Errors));

        var empties = emptyStates ?? guidanceService.CreateDefaultEmptyStates();
        var onboarding = onboardingSteps ?? guidanceService.CreateDefaultOnboarding();
        var explainers = guardrailExplainers ?? guidanceService.CreateDefaultGuardrailExplainers();
        var primaryEmpty = empties.First(state => state.Kind == NodalOsMissionControlEmptyStateKind.RuntimeUnavailableByDesign);
        var primaryOnboarding = onboarding.Take(4).ToArray();
        var primaryExplainers = explainers.Take(6).ToArray();

        var builder = new StringBuilder();
        builder.AppendLine("<!doctype html>");
        builder.AppendLine("<html lang=\"en\">");
        builder.AppendLine("<head>");
        builder.AppendLine("<meta charset=\"utf-8\">");
        builder.AppendLine("<meta name=\"viewport\" content=\"width=device-width, initial-scale=1\">");
        builder.AppendLine("<title>NODAL OS Mission Control Static UX Preview</title>");
        builder.AppendLine("<style>");
        builder.AppendLine(":root{--bg:#0D1117;--panel:#161B22;--card:#1C2128;--line:#30363D;--text:#F5F7FA;--muted:#AAB4C0;--disabled:#6E7681;--blue:#4F7CFF;--violet:#7C5CFF;--aqua:#00C2A8;--amber:#F2C94C;--red:#FF647C;--glow:0 30px 90px rgba(79,124,255,.18);}");
        builder.AppendLine("*{box-sizing:border-box}html{background:var(--bg)}body{margin:0;color:var(--text);font-family:'Aptos Display','Segoe UI Variable Display','Segoe UI',sans-serif;background:radial-gradient(circle at 8% 8%,rgba(79,124,255,.24),transparent 28%),radial-gradient(circle at 92% 16%,rgba(0,194,168,.14),transparent 30%),linear-gradient(135deg,#0D1117,#090C11 70%);}");
        builder.AppendLine(".mc-shell{min-height:100vh;display:grid;grid-template-columns:236px minmax(620px,1fr) 380px;grid-template-rows:82px minmax(560px,1fr) 226px;gap:16px;padding:18px;}");
        builder.AppendLine(".panel{border:1px solid var(--line);background:linear-gradient(180deg,rgba(22,27,34,.94),rgba(13,17,23,.92));border-radius:28px;box-shadow:var(--glow);overflow:hidden}.top{grid-column:1/4;display:flex;align-items:center;justify-content:space-between;padding:0 24px}.brand{font-size:12px;letter-spacing:.2em;text-transform:uppercase;color:var(--muted)}.mission{font-size:25px;font-weight:760;letter-spacing:-.02em}.status{font-size:13px;color:var(--muted)}.badge-row{display:flex;gap:8px;flex-wrap:wrap;justify-content:flex-end}.badge{border:1px solid var(--line);border-radius:999px;padding:7px 10px;font-size:12px;color:var(--muted);background:rgba(28,33,40,.72)}.badge.hot{border:0;color:#fff;background:linear-gradient(135deg,var(--blue),var(--violet))}.badge.safe{color:#DDFCF6;border-color:rgba(0,194,168,.35)}");
        builder.AppendLine(".side{padding:18px}.nav{display:grid;gap:9px;margin-top:16px}.nav-item{display:flex;align-items:center;justify-content:space-between;padding:12px 13px;border-radius:15px;background:rgba(28,33,40,.68);color:var(--muted);border:1px solid transparent}.nav-item.active{color:#fff;border-color:rgba(79,124,255,.5);background:linear-gradient(135deg,rgba(79,124,255,.22),rgba(124,92,255,.12))}.nav-item.disabled{color:var(--disabled)}");
        builder.AppendLine(".main{padding:22px;display:grid;grid-template-rows:auto 1fr;gap:16px}.hero{display:grid;grid-template-columns:1fr 260px;gap:16px}.headline{font-size:44px;line-height:.96;letter-spacing:-.055em;margin:0}.copy{color:var(--muted);line-height:1.55}.progress-wrap{height:11px;border-radius:999px;background:#090C11;border:1px solid var(--line);overflow:hidden}.progress-bar{display:block;height:100%;width:" + shell.TopBar.ProgressPercent + "%;background:linear-gradient(90deg,var(--blue),var(--violet),var(--aqua))}.next-card{padding:16px;border-radius:22px;background:rgba(28,33,40,.78);border:1px solid var(--line)}");
        builder.AppendLine(".mission-grid{display:grid;grid-template-columns:1.08fr .92fr;gap:16px;min-height:0}.timeline-card,.evidence-card,.approval-card,.guide-card,.log-card{background:rgba(28,33,40,.78);border:1px solid var(--line);border-radius:22px;padding:16px}.timeline-vertical{position:relative;display:grid;gap:14px;margin-top:12px}.timeline-vertical:before{content:'';position:absolute;left:8px;top:6px;bottom:6px;width:2px;background:linear-gradient(var(--blue),var(--violet),var(--aqua))}.timeline-event{position:relative;padding-left:30px}.timeline-event:before{content:'';position:absolute;left:1px;top:5px;width:16px;height:16px;border-radius:50%;background:var(--bg);border:3px solid var(--blue);box-shadow:0 0 0 5px rgba(79,124,255,.13)}.event-title{font-weight:720}.event-meta,.mono{font-family:'Cascadia Mono','Consolas',monospace;font-size:12px;color:#C9D7E8}.severity{display:inline-flex;border-radius:999px;padding:4px 8px;font-size:11px;background:rgba(242,201,76,.12);color:var(--amber);border:1px solid rgba(242,201,76,.32)}");
        builder.AppendLine(".right{padding:18px;display:grid;gap:14px;overflow:auto}.approval-card.critical{border-color:rgba(255,100,124,.46)}.option{display:inline-flex;margin:4px 5px 0 0;padding:9px 11px;border-radius:12px;border:1px solid var(--line);color:var(--disabled);background:#11161D}.option[aria-disabled='true']{cursor:not-allowed}.guardrail{border-left:3px solid var(--aqua);padding:10px 12px;background:rgba(0,194,168,.07);border-radius:14px;margin-top:8px}.empty-state{border:1px dashed rgba(170,180,192,.38);background:rgba(13,17,23,.54);border-radius:20px;padding:15px}.onboarding{display:grid;grid-template-columns:repeat(2,minmax(0,1fr));gap:10px}.log{grid-column:1/4;padding:18px;display:grid;grid-template-columns:1.25fr .75fr;gap:14px}.log-lines{display:grid;gap:8px}.log-line{font-family:'Cascadia Mono','Consolas',monospace;font-size:12px;color:#BFD0E4;padding:8px 10px;border-radius:12px;background:#0B0F15;border:1px solid rgba(48,54,61,.8)}");
        builder.AppendLine("@media(max-width:1180px){.mc-shell{grid-template-columns:84px minmax(0,1fr);grid-template-rows:auto auto auto auto}.top,.log{grid-column:1/3}.right{grid-column:1/3}.side{grid-row:2/4}.nav-item span:last-child{display:none}.mission-grid,.hero,.log{grid-template-columns:1fr}}@media(min-width:1600px){.mc-shell{grid-template-columns:260px minmax(760px,1fr) 430px}.headline{font-size:56px}.mission-grid{grid-template-columns:1.15fr .85fr}}@media(min-width:2200px){.mc-shell{grid-template-columns:300px minmax(1000px,1fr) 520px;grid-template-rows:90px minmax(650px,1fr) 250px}.timeline-vertical{gap:10px}.log-lines{grid-template-columns:repeat(2,minmax(0,1fr))}}");
        builder.AppendLine("</style>");
        builder.AppendLine("</head>");
        builder.AppendLine("<body>");
        builder.AppendLine("<main class=\"mc-shell\" data-read-only=\"true\" data-runtime=\"blocked\" data-can-authorize-execution=\"false\">");
        builder.AppendLine($"<header class=\"panel top\"><div><div class=\"brand\">{Html(shell.ProjectOperationalName)} / Mission Control</div><div class=\"mission\">{Html(shell.TopBar.MissionTitleRedacted)}</div><div class=\"status\">{Html(shell.TopBar.OverallStatusRedacted)} / progress {shell.TopBar.ProgressPercent}%</div></div><div class=\"badge-row\"><span class=\"badge hot\">Read-only preview</span><span class=\"badge\">No runtime</span><span class=\"badge\">No cloud</span><span class=\"badge\">No LLM</span><span class=\"badge safe\">Evidence ref-only</span></div></header>");
        builder.AppendLine("<aside class=\"panel side\"><div class=\"brand\">Control surfaces</div><nav class=\"nav\" aria-label=\"Mission Control sections\">");
        AppendNav(builder, "Mission Control", active: true);
        AppendNav(builder, "Timeline", active: false);
        AppendNav(builder, "Approvals", active: false);
        AppendNav(builder, "Evidence", active: false);
        AppendNav(builder, "Observability / LOG", active: false);
        AppendNav(builder, "Guardrails", active: false);
        AppendNav(builder, "Onboarding", active: false);
        AppendNav(builder, "Settings future", active: false, disabled: true);
        builder.AppendLine("</nav></aside>");
        builder.AppendLine("<section class=\"panel main\"><div class=\"hero\"><div><p class=\"brand\">Active mission</p><h1 class=\"headline\">Observe the mission. Do not execute it.</h1><p class=\"copy\">" + Html(shell.Workspace.SummaryRedacted) + "</p><div class=\"progress-wrap\" aria-label=\"Mission progress\"><span class=\"progress-bar\"></span></div></div><div class=\"next-card\"><div class=\"brand\">Next safe step</div><p>" + Html(primaryEmpty.RecommendedNextSafeStepRedacted) + "</p><div class=\"empty-state\"><strong>" + Html(primaryEmpty.TitleRedacted) + "</strong><p class=\"copy\">" + Html(primaryEmpty.UserFriendlyExplanationRedacted) + "</p></div></div></div>");
        builder.AppendLine("<div class=\"mission-grid\"><article class=\"timeline-card\"><div class=\"brand\">Vertical timeline</div><h2>Canonical event sequence</h2><div class=\"timeline-vertical\">");
        foreach (var entry in shell.Timeline.Entries)
            builder.AppendLine($"<section class=\"timeline-event\"><div class=\"severity\">{Html(entry.Severity.ToString())}</div><div class=\"event-title\">{Html(entry.TitleRedacted)}</div><p class=\"copy\">{Html(entry.MessageRedacted)}</p><div class=\"event-meta\">registry {Html(entry.ExecutionRegistryEntryId ?? "unlinked")} / evidence {entry.EvidenceRefs.Count}</div></section>");
        builder.AppendLine("</div></article><article class=\"evidence-card\"><div class=\"brand\">Evidence refs</div><h2>Reference-only evidence</h2>");
        foreach (var evidence in shell.Evidence.EvidenceRefs.Take(5))
            builder.AppendLine($"<div class=\"mono\">{Html(evidence.KindRedacted)} / {Html(evidence.RefRedacted)} / inline=false</div>");
        builder.AppendLine("</article></div></section>");
        builder.AppendLine("<aside class=\"panel right\"><div class=\"brand\">Approval display</div>");
        foreach (var card in shell.ApprovalDisplay.Cards)
        {
            builder.AppendLine($"<article class=\"approval-card critical\"><span class=\"severity\">{Html(card.Severity.ToString())}</span><h2>{Html(card.TitleRedacted)}</h2><p class=\"copy\">{Html(card.FullExplanationRedacted)}</p><div class=\"mono\">action {Html(card.RequestedAction.ToString())}</div><div class=\"mono\">resources {Html(string.Join(", ", card.AffectedResourcesRedacted))}</div><div class=\"mono\">can authorize execution: false</div></article>");
        }
        builder.AppendLine("<section class=\"guide-card\"><h3>Disabled options</h3>");
        foreach (var option in shell.ApprovalDisplay.ActionOptions)
            builder.AppendLine($"<span class=\"option\" aria-disabled=\"true\">{Html(option.LabelRedacted)} / disabled</span>");
        builder.AppendLine("<p class=\"copy\">Disabled because the positive execution gate is not implemented.</p></section>");
        builder.AppendLine("<section class=\"guide-card\"><h3>Guardrail explainers</h3>");
        foreach (var explainer in primaryExplainers)
            builder.AppendLine($"<div class=\"guardrail\"><strong>{Html(explainer.TitleRedacted)}</strong><div class=\"copy\">{Html(explainer.PlainLanguageExplanationRedacted)}</div></div>");
        builder.AppendLine("</section></aside>");
        builder.AppendLine("<section class=\"panel log\"><article class=\"log-card\"><div class=\"brand\">Observability / LOG preview</div><h2>Redacted technical report surface</h2><div class=\"log-lines\">");
        foreach (var line in shell.Observability.LinesRedacted)
            builder.AppendLine($"<div class=\"log-line\">{Html(line)}</div>");
        builder.AppendLine("</div></article><article class=\"guide-card\"><div class=\"brand\">Contextual onboarding</div><div class=\"onboarding\">");
        foreach (var step in primaryOnboarding)
            builder.AppendLine($"<div class=\"empty-state\"><strong>{Html(step.TitleRedacted)}</strong><p class=\"copy\">{Html(step.ExplanationRedacted)}</p></div>");
        builder.AppendLine("</div></article></section>");
        builder.AppendLine("</main>");
        builder.AppendLine("</body>");
        builder.AppendLine("</html>");

        return builder.ToString();
    }

    private static NodalOsMissionControlLayoutBreakpoint Breakpoint(
        NodalOsMissionControlDesktopBreakpointKind kind,
        int minimumWidthPx,
        NodalOsMissionControlDensityMode density,
        NodalOsMissionControlPanelBehavior sidebar,
        NodalOsMissionControlPanelBehavior right,
        NodalOsMissionControlPanelBehavior bottom,
        string timelineDensity,
        string approvalCardBehavior,
        string evidencePanelBehavior,
        string guardrailExplainerBehavior,
        string onboardingCardBehavior) =>
        new()
        {
            BreakpointKind = kind,
            MinimumWidthPx = minimumWidthPx,
            DensityMode = density,
            SidebarBehavior = sidebar,
            RightPanelBehavior = right,
            BottomLogPanelBehavior = bottom,
            TimelineDensityRedacted = timelineDensity,
            ApprovalCardBehaviorRedacted = approvalCardBehavior,
            EvidencePanelBehaviorRedacted = evidencePanelBehavior,
            GuardrailExplainerBehaviorRedacted = guardrailExplainerBehavior,
            OnboardingCardBehaviorRedacted = onboardingCardBehavior
        };

    private static void AppendNav(StringBuilder builder, string label, bool active, bool disabled = false) =>
        builder.AppendLine($"<div class=\"nav-item {(active ? "active" : string.Empty)} {(disabled ? "disabled" : string.Empty)}\"><span>{Html(label)}</span><span>{(disabled ? "future" : "ready")}</span></div>");

    private static string Html(string? value) =>
        WebUtility.HtmlEncode(value ?? string.Empty);
}

public static class NodalOsMissionControlVisualFixtures
{
    public static NodalOsMissionControlLayoutSpec LayoutSpec() =>
        new NodalOsMissionControlVisualService().CreateResponsiveDesktopLayoutSpec();

    public static NodalOsMissionControlStaticUxAcceptancePack AcceptancePack() =>
        new NodalOsMissionControlVisualService().CreateStaticUxAcceptancePack(
            "artifacts/agent-operations/m491/mission-control-static-ux-preview.html");

    public static string StaticPreviewHtml() =>
        new NodalOsMissionControlVisualService().RenderStaticUxPreview(
            NodalOsMissionControlShellFixtures.ShellPreview());
}

