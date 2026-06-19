using System.Text.Json;
using System.Text.Json.Serialization;
using OneBrain.AgentOperations.Contracts;

namespace OneBrain.AgentOperations.Core;

public sealed class NodalOsMissionControlGuidanceValidator
{
    private readonly NodalOsRedactionService redaction;

    public NodalOsMissionControlGuidanceValidator()
        : this(new NodalOsRedactionService())
    {
    }

    public NodalOsMissionControlGuidanceValidator(NodalOsRedactionService redaction) =>
        this.redaction = redaction;

    public NodalOsCoreRuntimeValidationResult ValidateEmptyState(NodalOsMissionControlEmptyState emptyState)
    {
        var errors = new List<string>();
        var warnings = new List<string>();

        AddRequired(errors, emptyState.EmptyStateId, "EmptyStateId is required.");
        AddRequired(errors, emptyState.TitleRedacted, "Empty state title is required.");
        AddRequired(errors, emptyState.ShortDescriptionRedacted, "Empty state short description is required.");
        AddRequired(errors, emptyState.UserFriendlyExplanationRedacted, "Empty state explanation is required.");
        AddRequired(errors, emptyState.RecommendedNextSafeStepRedacted, "Empty state next safe step is required.");
        if (emptyState.CanExecuteAction)
            errors.Add("Mission Control empty states cannot execute actions.");
        if (!emptyState.IsReadOnly)
            errors.Add("Mission Control empty states must remain read-only.");
        if (emptyState.CreatedAt == default)
            errors.Add("CreatedAt is required.");

        ValidateSafeText(errors, "TitleRedacted", emptyState.TitleRedacted);
        ValidateSafeText(errors, "ShortDescriptionRedacted", emptyState.ShortDescriptionRedacted);
        ValidateSafeText(errors, "UserFriendlyExplanationRedacted", emptyState.UserFriendlyExplanationRedacted);
        ValidateSafeText(errors, "RecommendedNextSafeStepRedacted", emptyState.RecommendedNextSafeStepRedacted);
        ValidateSafeText(errors, "DisabledActionLabelRedacted", emptyState.DisabledActionLabelRedacted);
        ValidateSafeText(errors, "DisabledReasonRedacted", emptyState.DisabledReasonRedacted);
        foreach (var guardrailRef in emptyState.GuardrailRefs)
            ValidateSafeText(errors, "GuardrailRefs", guardrailRef);

        return Result(errors, warnings);
    }

    public NodalOsCoreRuntimeValidationResult ValidateOnboardingStep(NodalOsMissionControlOnboardingStep step)
    {
        var errors = new List<string>();
        var warnings = new List<string>();

        AddRequired(errors, step.StepId, "StepId is required.");
        AddRequired(errors, step.TitleRedacted, "Onboarding title is required.");
        AddRequired(errors, step.ExplanationRedacted, "Onboarding explanation is required.");
        AddRequired(errors, step.SafeNextActionRedacted, "Onboarding safe next action is required.");
        if (!step.IsNoOp)
            errors.Add("Mission Control onboarding steps must be no-op.");
        if (!step.DismissReopenMockSafe)
            errors.Add("Mission Control onboarding dismiss/reopen must be mock-safe.");
        if (step.ProductivePersistenceAllowed)
            errors.Add("Mission Control onboarding cannot use productive persistence.");
        if (step.TelemetryAllowed)
            errors.Add("Mission Control onboarding cannot enable telemetry or analytics.");
        if (step.CloudCallAllowed)
            errors.Add("Mission Control onboarding cannot call cloud services.");
        if (step.LlmProviderCallAllowed)
            errors.Add("Mission Control onboarding cannot call LLM providers.");
        if (step.UpdatedAt == default)
            errors.Add("UpdatedAt is required.");

        ValidateSafeText(errors, "TitleRedacted", step.TitleRedacted);
        ValidateSafeText(errors, "ExplanationRedacted", step.ExplanationRedacted);
        ValidateSafeText(errors, "SafeNextActionRedacted", step.SafeNextActionRedacted);
        ValidateSafeText(errors, "DisabledFutureWorkExplanationRedacted", step.DisabledFutureWorkExplanationRedacted);

        return Result(errors, warnings);
    }

    public NodalOsCoreRuntimeValidationResult ValidateGuardrailExplainer(NodalOsMissionControlGuardrailExplainer explainer)
    {
        var errors = new List<string>();
        var warnings = new List<string>();

        AddRequired(errors, explainer.GuardrailId, "GuardrailId is required.");
        AddRequired(errors, explainer.TitleRedacted, "Guardrail title is required.");
        AddRequired(errors, explainer.PlainLanguageExplanationRedacted, "Guardrail plain language explanation is required.");
        AddRequired(errors, explainer.TechnicalReasonRedacted, "Guardrail technical reason is required.");
        AddRequired(errors, explainer.AffectedSurfaceRedacted, "Guardrail affected surface is required.");
        AddRequired(errors, explainer.WhatUserCanDoNowRedacted, "Guardrail current user option is required.");
        AddRequired(errors, explainer.WhatIsDeferredRedacted, "Guardrail deferred work explanation is required.");
        if (explainer.CanUnlockExecution)
            errors.Add("Guardrail explainers cannot unlock execution.");
        if (explainer.CanChangePolicy)
            errors.Add("Guardrail explainers cannot change policy.");
        if (explainer.CanMutateRegistry)
            errors.Add("Guardrail explainers cannot mutate registry.");
        if (explainer.CanCreateException)
            errors.Add("Guardrail explainers cannot create exceptions.");
        if (explainer.CreatedAt == default)
            errors.Add("CreatedAt is required.");

        ValidateSafeText(errors, "TitleRedacted", explainer.TitleRedacted);
        ValidateSafeText(errors, "PlainLanguageExplanationRedacted", explainer.PlainLanguageExplanationRedacted);
        ValidateSafeText(errors, "TechnicalReasonRedacted", explainer.TechnicalReasonRedacted);
        ValidateSafeText(errors, "AffectedSurfaceRedacted", explainer.AffectedSurfaceRedacted);
        ValidateSafeText(errors, "WhatUserCanDoNowRedacted", explainer.WhatUserCanDoNowRedacted);
        ValidateSafeText(errors, "WhatIsDeferredRedacted", explainer.WhatIsDeferredRedacted);
        foreach (var referenceId in explainer.ReferenceIds)
            ValidateSafeText(errors, "ReferenceIds", referenceId);

        return Result(errors, warnings);
    }

    public string Redact(string? value) => redaction.RedactValue(value).Value;

    private void ValidateSafeText(List<string> errors, string fieldName, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return;

        if (redaction.ContainsSensitiveField(fieldName, value) || redaction.ContainsSensitiveContent(value))
            errors.Add($"{fieldName} contains sensitive content and must be redacted before Mission Control guidance display.");
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

public sealed class NodalOsMissionControlGuidanceService
{
    private readonly NodalOsMissionControlGuidanceValidator validator;

    public NodalOsMissionControlGuidanceService()
        : this(new NodalOsMissionControlGuidanceValidator())
    {
    }

    public NodalOsMissionControlGuidanceService(NodalOsMissionControlGuidanceValidator validator) =>
        this.validator = validator;

    public IReadOnlyList<NodalOsMissionControlEmptyState> CreateDefaultEmptyStates() =>
    [
        EmptyState(NodalOsMissionControlEmptyStateKind.NoMissionSelected, "No mission selected", "Pick a mission to inspect.", "Mission Control waits until a mission context is selected before showing timeline, approvals and evidence.", "Select a mission from a safe local preview list.", "Select mission", "Mission selection is preview-only in this block.", ["read-only-mode"]),
        EmptyState(NodalOsMissionControlEmptyStateKind.NoActiveMission, "No active mission", "There is no active mission in this preview.", "NODAL OS can explain available surfaces without starting runtime work.", "Review onboarding or load a fixture-safe mission preview.", "Start mission", "Runtime execution is not available.", ["no-runtime-execution"], NodalOsMissionControlGuidanceSeverity.Attention, true),
        EmptyState(NodalOsMissionControlEmptyStateKind.NoTimelineEvents, "Timeline has no events", "No canonical events are available for this mission.", "The timeline is reconstructed from safe events; empty means nothing has been registered for this preview context.", "Create or load fixture-safe timeline data.", null, null, ["timeline-read-only"]),
        EmptyState(NodalOsMissionControlEmptyStateKind.NoApprovalsPending, "No approvals pending", "There are no approval cards to review.", "Approval Display remains visible so the user understands where future decisions will appear.", "Continue observing timeline and evidence refs.", null, null, ["approval-no-authority"]),
        EmptyState(NodalOsMissionControlEmptyStateKind.NoEvidenceAvailable, "No evidence available", "No evidence refs are attached yet.", "Evidence remains ref-only; raw payloads, screenshots, DOM and network bodies are not displayed inline.", "Inspect timeline events or observability summary for expected evidence.", null, null, ["evidence-ref-only"]),
        EmptyState(NodalOsMissionControlEmptyStateKind.NoObservabilityReportYet, "No LOG report yet", "Observability preview has no report for this context.", "The LOG surface is read-only and does not collect telemetry or call external services.", "Open a fixture-safe observability preview when available.", "Generate report", "Report generation is contract-only here.", ["observability-read-only"]),
        EmptyState(NodalOsMissionControlEmptyStateKind.NoWorkspaceSelected, "No workspace selected", "Workspace context is not selected.", "Workspace selection is future-only and cannot mutate files in this block.", "Use Mission Control preview without workspace mutation.", "Select workspace", "Filesystem mutation is blocked.", ["no-filesystem-mutation"]),
        EmptyState(NodalOsMissionControlEmptyStateKind.NoUiInteractionHistory, "No interaction history", "No local no-op UI intents have been captured.", "Interactions are tracked only as no-op UI events; they cannot authorize or execute work.", "Use a disabled preview control to capture a no-op intent.", null, null, ["interaction-no-op"]),
        EmptyState(NodalOsMissionControlEmptyStateKind.NoApprovalDraft, "No approval draft", "No local approval decision draft exists.", "Drafts are local, mock-safe and non-authoritative.", "Create a draft for review without submitting execution.", "Submit approval", "Approval decisions cannot authorize execution.", ["approval-draft-no-authority"]),
        EmptyState(NodalOsMissionControlEmptyStateKind.NoSelectedEvidenceRef, "No evidence ref selected", "Select a safe evidence ref to inspect metadata.", "Evidence detail remains ref-only and redacted.", "Choose an evidence ref from the list.", null, null, ["evidence-ref-only"]),
        EmptyState(NodalOsMissionControlEmptyStateKind.NoGuardrailWarnings, "No guardrail warnings", "No active warning is attached to this surface.", "Core guardrails still remain active even when there is no warning badge.", "Continue in read-only preview mode.", null, null, ["read-only-mode"]),
        EmptyState(NodalOsMissionControlEmptyStateKind.RuntimeUnavailableByDesign, "Runtime unavailable by design", "Execution is intentionally unavailable.", "The positive execution authorization gate has not been implemented.", "Review why execution is blocked in Guardrails.", "Run", "Runtime execution requires a future gate.", ["positive-gate-missing"], NodalOsMissionControlGuidanceSeverity.Blocked, true),
        EmptyState(NodalOsMissionControlEmptyStateKind.LlmNotConfiguredByDesign, "LLM not configured by design", "Provider calls are disabled in this block.", "BYOK and assignment are future tracks and cannot be called by Mission Control guidance.", "Use static guidance and fixture-safe reports.", "Connect model", "Provider calls are not implemented.", ["no-llm-provider-calls"], NodalOsMissionControlGuidanceSeverity.Attention, true),
        EmptyState(NodalOsMissionControlEmptyStateKind.CloudSyncDisabledByDesign, "Cloud sync disabled by design", "No cloud sync is available.", "Cloud, licensing and BYOK remain blocked until legacy sensitive subsystem quarantine is resolved.", "Keep work local-first.", "Enable sync", "Cloud sync is deferred.", ["no-cloud-sync"], NodalOsMissionControlGuidanceSeverity.Attention, true),
        EmptyState(NodalOsMissionControlEmptyStateKind.BrowserAutomationDeferredByDesign, "Browser automation deferred", "Browser automation is not connected to Mission Control.", "Browser runtime remains disconnected and runtime-gated.", "Use evidence/timeline previews only.", "Open browser automation", "Browser automation is deferred.", ["no-browser-automation"], NodalOsMissionControlGuidanceSeverity.Blocked, true)
    ];

    public IReadOnlyList<NodalOsMissionControlOnboardingStep> CreateDefaultOnboarding(
        NodalOsMissionControlGuidanceLevel level = NodalOsMissionControlGuidanceLevel.Normal) =>
    [
        Onboarding("what-is-mission", NodalOsMissionControlOnboardingTarget.MissionControl, "What is a mission?", "A mission is the user-visible container for intent, state, approvals, timeline and evidence.", "Start by reading mission status and guardrails.", level),
        Onboarding("timeline-meaning", NodalOsMissionControlOnboardingTarget.Timeline, "What Timeline shows", "Timeline is a read-only projection of canonical events ordered by time.", "Use Timeline to understand what happened before touching approvals.", level),
        Onboarding("approval-display-meaning", NodalOsMissionControlOnboardingTarget.Approvals, "What Approval Display means", "Approval cards explain requested decisions, risk and evidence without executing anything.", "Inspect risk, resources and evidence before drafting a decision.", level),
        Onboarding("approval-no-authority", NodalOsMissionControlOnboardingTarget.Approvals, "Why approval does not execute yet", "Approval UI is non-authoritative until a future positive execution gate exists.", "Draft decisions locally; do not expect runtime changes.", level, "Execution remains deferred until policy, approval, evidence, verification and boundary checks are composed."),
        Onboarding("evidence-ref-only", NodalOsMissionControlOnboardingTarget.Evidence, "Evidence is ref-only", "Evidence surfaces show safe references and metadata, not raw payloads.", "Use refs to trace context without exposing bodies, cookies, headers or secrets.", level),
        Onboarding("observability-log", NodalOsMissionControlOnboardingTarget.ObservabilityLog, "What LOG explains", "LOG preview explains registry, events, approvals, evidence, warnings and blocked actions.", "Use it to copy a future technical report after export wiring exists.", level),
        Onboarding("runtime-blocked", NodalOsMissionControlOnboardingTarget.RuntimeFuture, "Why runtime is blocked", "Runtime execution is blocked because the positive gate is intentionally missing.", "Review guardrail explainers before any runtime planning.", level),
        Onboarding("llm-byok-future", NodalOsMissionControlOnboardingTarget.LlmByokFuture, "Why LLM/BYOK is future", "Provider configuration and calls are not implemented in Mission Control guidance.", "Continue with static, local-first guidance.", level),
        Onboarding("cloud-disabled", NodalOsMissionControlOnboardingTarget.Guardrails, "Why cloud is disabled", "Cloud sync remains disabled until security and legacy quarantine blockers are resolved.", "Keep evidence and guidance local-first.", level),
        Onboarding("before-real-execution", NodalOsMissionControlOnboardingTarget.RuntimeFuture, "What comes before real execution", "Execution requires classifier hardening, policy, approval, evidence, verification, rollback and jail checks.", "Treat current controls as preview and planning surfaces only.", level)
    ];

    public IReadOnlyList<NodalOsMissionControlGuardrailExplainer> CreateDefaultGuardrailExplainers() =>
    [
        Explainer("read-only-mode", "Read-only mode", "This surface explains state without changing it.", "Mission Control preview is display and guidance only.", "Mission Control", "Review context, timeline, approvals and evidence.", "Runtime mutation remains deferred.", NodalOsMissionControlGuardrailBlockingCategory.Informational),
        Explainer("no-runtime-execution", "No runtime execution", "No action can run from this surface.", "The positive execution authorization gate is not implemented.", "All Mission Control controls", "Inspect and draft only.", "Execution bridge implementation.", NodalOsMissionControlGuardrailBlockingCategory.BlocksRuntime, NodalOsMissionControlGuidanceSeverity.Blocked),
        Explainer("no-browser-automation", "No browser automation", "Browser automation is disconnected.", "Mission Control cannot reference BrowserExecutor.Cdp directly.", "Timeline, approvals and evidence", "Use read-only evidence refs.", "Browser runtime needs separate audit and gate.", NodalOsMissionControlGuardrailBlockingCategory.BlocksBrowserAutomation, NodalOsMissionControlGuidanceSeverity.Blocked),
        Explainer("no-cloud-sync", "No cloud sync", "Cloud sync is disabled.", "Local-first mode is required until quarantine and policy work is complete.", "Workspace and exports", "Keep handoff/export local and redacted.", "Cloud, licensing and device activation.", NodalOsMissionControlGuardrailBlockingCategory.BlocksCloud, NodalOsMissionControlGuidanceSeverity.Attention),
        Explainer("no-llm-provider-calls", "No LLM provider calls", "Guidance does not call a model provider.", "BYOK/provider routing is a future track.", "Onboarding and guardrails", "Use static guidance.", "Provider config, prompt governance and assignment.", NodalOsMissionControlGuardrailBlockingCategory.BlocksLlmByok, NodalOsMissionControlGuidanceSeverity.Attention),
        Explainer("no-filesystem-mutation", "No filesystem mutation", "Guidance cannot change files.", "Workspace operations are not wired here.", "Workspace future", "Read static previews only.", "Controlled file operation v2.", NodalOsMissionControlGuardrailBlockingCategory.BlocksRuntime, NodalOsMissionControlGuidanceSeverity.Attention),
        Explainer("no-shell-subprocess", "No shell or subprocess", "Guidance cannot spawn processes.", "Shell/subprocess execution is outside this no-runtime block.", "All guidance surfaces", "Use report text only.", "Execution bridge and jail policy.", NodalOsMissionControlGuardrailBlockingCategory.BlocksRuntime, NodalOsMissionControlGuidanceSeverity.Blocked),
        Explainer("approval-no-authority", "Approval has no authority", "Approval Display can explain and draft decisions, not execute them.", "Approval decision alone is insufficient without a positive gate.", "Approval Display", "Draft, request explanation or defer locally.", "Policy/evidence/verification bridge.", NodalOsMissionControlGuardrailBlockingCategory.BlocksRuntime, NodalOsMissionControlGuidanceSeverity.Blocked),
        Explainer("evidence-ref-only", "Evidence is ref-only", "Evidence shows safe references, not raw payloads.", "Raw DOM, network bodies, cookies, headers and screenshots inline are blocked.", "Evidence view", "Inspect safe metadata.", "Evidence registry expansion after audit.", NodalOsMissionControlGuardrailBlockingCategory.Informational),
        Explainer("redaction-applied", "Redaction applied", "Sensitive strings are removed from guidance outputs.", "Common redaction sanitizes text and sensitive metadata keys.", "Reports, onboarding and explainers", "Share only redacted outputs.", "Further redaction tests for future exports.", NodalOsMissionControlGuardrailBlockingCategory.Informational),
        Explainer("positive-gate-missing", "Positive execution gate missing", "Nothing can authorize execution yet.", "The gate combining policy, approval, evidence, verification, rollback and jail checks is future work.", "Runtime bridge future", "Stay in preview mode.", "Positive gate milestone.", NodalOsMissionControlGuardrailBlockingCategory.BlocksRuntime, NodalOsMissionControlGuidanceSeverity.Critical),
        Explainer("recipe-risk-hardening-required", "Recipe risk hardening required", "Automation runtime remains blocked until risk classification is fail-closed.", "Ambiguous/dangerous instructions must not be downgraded by benign keywords.", "Automation future", "Do not execute recipes.", "Runtime-gated classifier hardening.", NodalOsMissionControlGuardrailBlockingCategory.BlocksRuntime, NodalOsMissionControlGuidanceSeverity.Critical),
        Explainer("browser-runtime-disconnected", "Browser runtime disconnected", "Browser runtime exists outside Mission Control core.", "Dependency boundary tests prevent direct connection.", "Browser automation future", "Use no-runtime previews.", "Dedicated runtime audit and gate.", NodalOsMissionControlGuardrailBlockingCategory.BlocksBrowserAutomation, NodalOsMissionControlGuidanceSeverity.Blocked),
        Explainer("legacy-sensitive-quarantine", "Legacy sensitive subsystem quarantine", "Cloud, licensing and BYOK stay blocked until sensitive legacy subsystems are isolated or removed.", "Legacy billing, email, identity, admin and config domains are quarantine blockers.", "Cloud and BYOK future", "Keep local-first mode.", "Quarantine/remove/exclude plan.", NodalOsMissionControlGuardrailBlockingCategory.BlocksCloud, NodalOsMissionControlGuidanceSeverity.Critical),
        Explainer("human-handoff", "Human handoff", "When the system cannot safely continue, it explains the blocker and user options.", "Handoff is explicit and does not authorize runtime work.", "Approvals and blocked states", "Pause, request explanation, change instruction or copy technical log when available.", "Runtime continuation remains future-gated.", NodalOsMissionControlGuardrailBlockingCategory.Informational),
        Explainer("blocked-action-disabled-button", "Disabled button explanation", "Disabled controls show what would be possible later and why it is unavailable now.", "Disabled controls prevent accidental execution or policy bypass.", "Mission Control controls", "Read the disabled reason and continue safely.", "Future implementation after gate and audits.", NodalOsMissionControlGuardrailBlockingCategory.Informational)
    ];

    public NodalOsMissionControlOnboardingStep DismissOnboardingStep(NodalOsMissionControlOnboardingStep step) =>
        Sanitize(step) with
        {
            State = NodalOsMissionControlOnboardingState.DismissedMock,
            IsNoOp = true,
            DismissReopenMockSafe = true,
            ProductivePersistenceAllowed = false,
            TelemetryAllowed = false,
            CloudCallAllowed = false,
            LlmProviderCallAllowed = false,
            UpdatedAt = DateTimeOffset.UtcNow
        };

    public NodalOsMissionControlOnboardingStep ReopenOnboardingStep(NodalOsMissionControlOnboardingStep step) =>
        Sanitize(step) with
        {
            State = NodalOsMissionControlOnboardingState.ReopenedMock,
            IsNoOp = true,
            DismissReopenMockSafe = true,
            ProductivePersistenceAllowed = false,
            TelemetryAllowed = false,
            CloudCallAllowed = false,
            LlmProviderCallAllowed = false,
            UpdatedAt = DateTimeOffset.UtcNow
        };

    public NodalOsMissionControlEmptyState Sanitize(NodalOsMissionControlEmptyState emptyState) =>
        emptyState with
        {
            TitleRedacted = validator.Redact(emptyState.TitleRedacted),
            ShortDescriptionRedacted = validator.Redact(emptyState.ShortDescriptionRedacted),
            UserFriendlyExplanationRedacted = validator.Redact(emptyState.UserFriendlyExplanationRedacted),
            RecommendedNextSafeStepRedacted = validator.Redact(emptyState.RecommendedNextSafeStepRedacted),
            DisabledActionLabelRedacted = validator.Redact(emptyState.DisabledActionLabelRedacted),
            DisabledReasonRedacted = validator.Redact(emptyState.DisabledReasonRedacted),
            GuardrailRefs = emptyState.GuardrailRefs.Select(validator.Redact).ToArray(),
            CanExecuteAction = false,
            IsReadOnly = true
        };

    public NodalOsMissionControlOnboardingStep Sanitize(NodalOsMissionControlOnboardingStep step) =>
        step with
        {
            TitleRedacted = validator.Redact(step.TitleRedacted),
            ExplanationRedacted = validator.Redact(step.ExplanationRedacted),
            SafeNextActionRedacted = validator.Redact(step.SafeNextActionRedacted),
            DisabledFutureWorkExplanationRedacted = validator.Redact(step.DisabledFutureWorkExplanationRedacted),
            IsNoOp = true,
            DismissReopenMockSafe = true,
            ProductivePersistenceAllowed = false,
            TelemetryAllowed = false,
            CloudCallAllowed = false,
            LlmProviderCallAllowed = false
        };

    public NodalOsMissionControlGuardrailExplainer Sanitize(NodalOsMissionControlGuardrailExplainer explainer) =>
        explainer with
        {
            TitleRedacted = validator.Redact(explainer.TitleRedacted),
            PlainLanguageExplanationRedacted = validator.Redact(explainer.PlainLanguageExplanationRedacted),
            TechnicalReasonRedacted = validator.Redact(explainer.TechnicalReasonRedacted),
            AffectedSurfaceRedacted = validator.Redact(explainer.AffectedSurfaceRedacted),
            WhatUserCanDoNowRedacted = validator.Redact(explainer.WhatUserCanDoNowRedacted),
            WhatIsDeferredRedacted = validator.Redact(explainer.WhatIsDeferredRedacted),
            ReferenceIds = explainer.ReferenceIds.Select(validator.Redact).ToArray(),
            CanUnlockExecution = false,
            CanChangePolicy = false,
            CanMutateRegistry = false,
            CanCreateException = false
        };

    private NodalOsMissionControlEmptyState EmptyState(
        NodalOsMissionControlEmptyStateKind kind,
        string title,
        string shortDescription,
        string explanation,
        string nextSafeStep,
        string? disabledActionLabel,
        string? disabledReason,
        IReadOnlyList<string> guardrailRefs,
        NodalOsMissionControlGuidanceSeverity severity = NodalOsMissionControlGuidanceSeverity.Informational,
        bool requiresAttention = false) =>
        Sanitize(new NodalOsMissionControlEmptyState
        {
            EmptyStateId = $"empty-state-{kind.ToString().ToLowerInvariant()}",
            Kind = kind,
            TitleRedacted = title,
            ShortDescriptionRedacted = shortDescription,
            UserFriendlyExplanationRedacted = explanation,
            RecommendedNextSafeStepRedacted = nextSafeStep,
            DisabledActionLabelRedacted = disabledActionLabel,
            DisabledReasonRedacted = disabledReason,
            GuardrailRefs = guardrailRefs,
            Severity = severity,
            RequiresAttention = requiresAttention,
            CanExecuteAction = false,
            IsReadOnly = true,
            CreatedAt = DateTimeOffset.UtcNow
        });

    private NodalOsMissionControlOnboardingStep Onboarding(
        string stepId,
        NodalOsMissionControlOnboardingTarget target,
        string title,
        string explanation,
        string safeNextAction,
        NodalOsMissionControlGuidanceLevel level,
        string? disabledFutureWorkExplanation = null) =>
        Sanitize(new NodalOsMissionControlOnboardingStep
        {
            StepId = stepId,
            Target = target,
            TitleRedacted = title,
            ExplanationRedacted = explanation,
            SafeNextActionRedacted = safeNextAction,
            DisabledFutureWorkExplanationRedacted = disabledFutureWorkExplanation,
            GuidanceLevel = level,
            State = NodalOsMissionControlOnboardingState.Active,
            IsNoOp = true,
            DismissReopenMockSafe = true,
            ProductivePersistenceAllowed = false,
            TelemetryAllowed = false,
            CloudCallAllowed = false,
            LlmProviderCallAllowed = false,
            UpdatedAt = DateTimeOffset.UtcNow
        });

    private NodalOsMissionControlGuardrailExplainer Explainer(
        string guardrailId,
        string title,
        string plainLanguage,
        string technicalReason,
        string affectedSurface,
        string userCanDoNow,
        string deferred,
        NodalOsMissionControlGuardrailBlockingCategory blockingCategory,
        NodalOsMissionControlGuidanceSeverity severity = NodalOsMissionControlGuidanceSeverity.Informational,
        IReadOnlyList<string>? references = null) =>
        Sanitize(new NodalOsMissionControlGuardrailExplainer
        {
            GuardrailId = guardrailId,
            TitleRedacted = title,
            PlainLanguageExplanationRedacted = plainLanguage,
            TechnicalReasonRedacted = technicalReason,
            AffectedSurfaceRedacted = affectedSurface,
            WhatUserCanDoNowRedacted = userCanDoNow,
            WhatIsDeferredRedacted = deferred,
            Severity = severity,
            BlockingCategory = blockingCategory,
            ReferenceIds = references ?? [],
            CanUnlockExecution = false,
            CanChangePolicy = false,
            CanMutateRegistry = false,
            CanCreateException = false,
            CreatedAt = DateTimeOffset.UtcNow
        });
}

public sealed class NodalOsMissionControlGuidanceJsonSerializer
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
        WriteIndented = true
    };

    private readonly NodalOsMissionControlGuidanceService service;

    public NodalOsMissionControlGuidanceJsonSerializer()
        : this(new NodalOsMissionControlGuidanceService())
    {
    }

    public NodalOsMissionControlGuidanceJsonSerializer(NodalOsMissionControlGuidanceService service) =>
        this.service = service;

    public string SerializeEmptyState(NodalOsMissionControlEmptyState emptyState) =>
        JsonSerializer.Serialize(service.Sanitize(emptyState), Options);

    public string SerializeOnboardingStep(NodalOsMissionControlOnboardingStep step) =>
        JsonSerializer.Serialize(service.Sanitize(step), Options);

    public string SerializeGuardrailExplainer(NodalOsMissionControlGuardrailExplainer explainer) =>
        JsonSerializer.Serialize(service.Sanitize(explainer), Options);
}

public static class NodalOsMissionControlGuidanceFixtures
{
    public static IReadOnlyList<NodalOsMissionControlEmptyState> EmptyStates() =>
        new NodalOsMissionControlGuidanceService().CreateDefaultEmptyStates();

    public static IReadOnlyList<NodalOsMissionControlOnboardingStep> OnboardingSteps() =>
        new NodalOsMissionControlGuidanceService().CreateDefaultOnboarding();

    public static IReadOnlyList<NodalOsMissionControlGuardrailExplainer> GuardrailExplainers() =>
        new NodalOsMissionControlGuidanceService().CreateDefaultGuardrailExplainers();
}
