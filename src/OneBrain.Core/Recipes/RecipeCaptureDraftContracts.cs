namespace OneBrain.Core.Recipes;

public enum RecipeCaptureSessionStatus
{
    Draft,
    CapturingFixture,
    CaptureComplete,
    NeedsReview,
    BlockedRawSecretDetected,
    BlockedSensitiveAction,
    BlockedLiveRuntimeDisabled,
    BlockedRecorderDisabled,
    BlockedByPolicy,
    Cancelled
}

public enum RecipeCaptureMode
{
    FixtureOnly,
    ManualDescriptionOnly,
    ImportedTraceRefOnly,
    ReferenceOnly,
    FutureBrowserCaptureBlocked,
    FutureDesktopCaptureBlocked,
    FutureConnectorCaptureBlocked,
    Disabled
}

public enum RecipeCaptureSource
{
    ManualDescription,
    FixtureTraceRef,
    ImportedEvidenceRef,
    TemplateRef,
    LabSnapshotRef,
    FutureBrowserCaptureBlocked,
    FutureDesktopCaptureBlocked,
    FutureConnectorCaptureBlocked,
    Unknown
}

public enum RecipeCaptureSafetyStatus
{
    SafeForDraft,
    NeedsReview,
    BlockedRawSecretDetected,
    BlockedSensitiveAction,
    BlockedLiveRuntimeDisabled,
    BlockedRecorderDisabled,
    BlockedByPolicy,
    UnknownUnsafe
}

public enum RecipeCapturedStepKind
{
    NavigateDraft,
    ClickDraft,
    TypeDraft,
    SelectDraft,
    ExtractDraft,
    DownloadDraft,
    UploadDraft,
    SubmitDraft,
    WaitDraft,
    ValidateDraft,
    HumanInterventionDraft,
    ApprovalDraft,
    WorkitemDraft,
    TriggerObservationDraft,
    ConnectorDraft,
    DesktopActionDraft,
    BrowserActionDraft,
    UnknownDraft
}

public enum RecipeCapturedInputValueCategory
{
    Text,
    Number,
    Date,
    Url,
    FileRef,
    SelectorSummary,
    SecretLike,
    PersonalData,
    FiscalData,
    PaymentData,
    Credential,
    Unknown
}

public enum RecipeDraftGenerationStatus
{
    DraftCreated,
    NeedsReview,
    BlockedMissingLimits,
    BlockedMissingValidation,
    BlockedMissingEvidence,
    BlockedSensitiveAction,
    BlockedRawSecretDetected,
    BlockedLiveRuntimeDisabled,
    BlockedRecorderDisabled,
    NotRunReady
}

public enum RecipeCaptureWarningKind
{
    SecretLikeInput,
    PersonalDataInput,
    FiscalDataInput,
    PaymentDataInput,
    CredentialUse,
    TwoFactorOrCaptcha,
    SubmitAction,
    DeleteAction,
    PublicPosting,
    MarketplaceMutation,
    PriceOrStockChange,
    FileWrite,
    ExternalMutation,
    BrowserLiveAction,
    DesktopLiveAction,
    UnknownSensitiveAction,
    MissingValidation,
    MissingEvidence,
    MissingApprovalPath,
    MissingToolTrust,
    MissingSecretRef,
    LocatorAmbiguous,
    RelativeCoordinateUsed,
    AIFallbackSuggested,
    UnknownUnsafe
}

public sealed record RecipeCaptureSessionRef(string CaptureSessionId);

public sealed record RecipeCapturedStepRef(string StepId);

public sealed record RecipeCapturedInputRef(string InputId);

public sealed record RecipeCapturedLocatorRef(string LocatorId);

public sealed record RecipeCapturedEvidenceRef(
    string EvidenceRefId,
    RecipeEvidenceSourceKind SourceKind,
    bool ReferenceOnly = true,
    bool RawPayloadPresent = false,
    bool RequiresRealCapture = false)
{
    public bool SafeForDraft => ReferenceOnly && !RawPayloadPresent && !RequiresRealCapture;
}

public sealed record RecipeCaptureWarning(
    string WarningId,
    RecipeCaptureWarningKind Kind,
    RecipeReadinessIssueSeverity Severity,
    string RedactedSummary,
    string? SourceRef = null,
    bool BlocksRunReadyConversion = true);

public sealed record RecipeCaptureBlockingIssue(
    string IssueId,
    RecipeReadinessStatus Status,
    RecipeReadinessIssueSeverity Severity,
    string RedactedSummary,
    string? SourceRef = null);

public sealed record RecipeCapturedInput(
    string InputId,
    string LabelOrRoleSummary,
    RecipeCapturedInputValueCategory ValueCategory,
    RecipeEvidenceRedactionStatus ValueRedactionStatus,
    string? VariableSuggestionRef,
    bool RawValuePresent = false,
    bool SecretLike = false,
    bool PersonalData = false,
    bool FiscalData = false,
    bool PaymentData = false,
    string OperatorVisibleSummary = "")
{
    public bool SecretValueStored => false;
    public bool SafeForDraft => !RawValuePresent && !SecretLike && ValueRedactionStatus is RecipeEvidenceRedactionStatus.Applied or RecipeEvidenceRedactionStatus.NotRequired;
}

public sealed record RecipeCapturedLocator(
    string LocatorId,
    RecipeLocatorStrategy Strategy,
    RecipeLocatorConfidence Confidence,
    RecipeLocatorDriftStatus DriftStatus,
    string RedactedSelectorOrTargetSummary,
    IReadOnlyList<string> EvidenceRefs,
    int FallbackOrder,
    RecipeLocatorSafetyStatus SafetyStatus,
    RecipeLocatorReplayEligibility ReplayEligibility,
    string? RepairSuggestionRef = null)
{
    public bool LiveReplayEnabled => false;
    public bool LiveRepairApplyEnabled => false;
    public bool ActionAuthorityGranted => false;
    public bool ReferenceOnly => EvidenceRefs.Count > 0 && ReplayEligibility is RecipeLocatorReplayEligibility.FixtureOnly or RecipeLocatorReplayEligibility.PreviewOnly or RecipeLocatorReplayEligibility.ManualReviewOnly or RecipeLocatorReplayEligibility.NotEligible;
}

public sealed record RecipeCapturedObservationSummary(
    string SummaryId,
    string RedactedSummary,
    IReadOnlyList<string> EvidenceRefs,
    bool ReferenceOnly = true,
    bool RawPayloadPresent = false);

public sealed record RecipeDraftVariableSuggestion(
    string SuggestionId,
    string Name,
    RecipeCapturedInputValueCategory ValueCategory,
    RecipeEvidenceRedactionStatus RedactionStatus,
    bool Required,
    bool SecretRefOnly = false,
    string? SourceInputRef = null)
{
    public bool RawValueStored => false;
}

public sealed record RecipeDraftValidationSuggestion(
    string SuggestionId,
    RecipeValidationKind Kind,
    RecipeValidationSeverity Severity,
    string AppliesToStepRef,
    bool RequiredForReadiness = true,
    string? EvidenceRef = null);

public sealed record RecipeDraftEvidenceSuggestion(
    string SuggestionId,
    RecipeEvidenceSourceKind SourceKind,
    string AppliesToStepRef,
    bool ReferenceOnly = true,
    bool RequiredForReadiness = true);

public sealed record RecipeDraftApprovalSuggestion(
    string SuggestionId,
    RecipeHumanInterventionKind InterventionKind,
    RecipeRiskLevel RiskLevel,
    IReadOnlySet<SensitiveActionCategory> SensitiveCategories,
    bool RequiredForReadiness = true);

public sealed record RecipeDraftBlockSuggestion(
    string SuggestionId,
    RecipeBlockType BlockType,
    RecipeCapturedStepKind SourceStepKind,
    string Label,
    string IntentSummary,
    IReadOnlyList<string> ValidationSuggestionRefs,
    IReadOnlyList<string> EvidenceSuggestionRefs,
    IReadOnlyList<string> ApprovalSuggestionRefs,
    RecipeRiskLevel RiskLevel)
{
    public bool CanExecute => false;
    public bool LiveRuntimeEnabled => false;
}

public sealed record RecipeDraftTemplateMapping(
    string MappingId,
    string? TemplateId,
    RecipeTemplateCategory Category,
    RecipeTemplateSystem System,
    RecipeTemplateRuntimeEligibility RuntimeEligibility,
    RecipeTemplateReadiness? TemplateReadiness,
    string RedactedSummary,
    bool RequiresCompositeReadiness = true)
{
    public bool CanOverrideCompositeReadiness => false;
    public bool LiveRuntimeEnabled => false;
}

public sealed record RecipeCapturedStep(
    string StepId,
    int Sequence,
    RecipeCapturedStepKind Kind,
    string IntentSummary,
    string TargetSummary,
    IReadOnlyList<RecipeCapturedInput> Inputs,
    IReadOnlyList<RecipeCapturedLocator> Locators,
    IReadOnlyList<RecipeCapturedEvidenceRef> EvidenceRefs,
    IReadOnlyList<string> ValidationSuggestionRefs,
    IReadOnlyList<string> RiskWarningRefs,
    IReadOnlyList<string> SecretWarningRefs,
    IReadOnlyList<string> ApprovalHumanSuggestionRefs,
    IReadOnlyList<string> ToolTrustRefs,
    IReadOnlyList<string> SecretRefs,
    IReadOnlyList<string> TemplateMappingRefs,
    RecipeCaptureSafetyStatus ReadinessStatus,
    IReadOnlyList<string> BlockedReasons,
    RecipeSafeNextAction SafeNextAction)
{
    public bool CanExecute => false;
    public bool LiveRuntimeEnabled => false;
    public bool ActionAuthorityGranted => false;
    public bool ContainsRawInputValues => Inputs.Any(i => i.RawValuePresent);
}

public sealed record RecipeDraftRef(string DraftId);

public sealed record RecipeDraft(
    string DraftId,
    string DisplayName,
    string CaptureSessionId,
    IReadOnlyList<RecipeDraftBlockSuggestion> BlockSuggestions,
    IReadOnlyList<RecipeDraftVariableSuggestion> VariableSuggestions,
    IReadOnlyList<RecipeDraftValidationSuggestion> ValidationSuggestions,
    IReadOnlyList<RecipeDraftEvidenceSuggestion> EvidenceSuggestions,
    IReadOnlyList<RecipeDraftApprovalSuggestion> ApprovalSuggestions,
    IReadOnlyList<RecipeDraftTemplateMapping> TemplateMappings,
    RecipeRiskLevel RiskLevel,
    IReadOnlyList<RecipeCaptureWarning> Warnings,
    IReadOnlyList<RecipeCaptureBlockingIssue> BlockingIssues,
    RecipeSafeNextAction SafeNextAction,
    bool ReviewRequired = true)
{
    public bool RunReady => false;
    public bool LiveReady => false;
    public bool CanExecute => false;
    public bool RawSecretsStored => false;
}

public sealed record RecipeDraftReadiness(
    bool IsReadyForDraft,
    bool IsRunReady,
    RecipeDraftGenerationStatus Status,
    IReadOnlyList<RecipeCaptureBlockingIssue> BlockingIssues,
    IReadOnlyList<RecipeCaptureWarning> Warnings,
    RecipeSafeNextAction SafeNextAction,
    string OperatorSummary)
{
    public bool LiveRuntimeEnabled => false;
    public bool ActionAuthorityGranted => false;
}

public sealed record RecipeDraftGenerationResult(
    RecipeDraftGenerationStatus Status,
    RecipeDraft Draft,
    RecipeDraftReadiness Readiness,
    RecipeDefinition? SuggestedRecipeDefinition,
    bool ReviewRequired = true)
{
    public bool CreatesRunReadyRecipe => false;
    public bool LiveRuntimeEnabled => false;
    public bool RecorderReplayEnabled => false;
}

public sealed record RecipeCaptureReadiness(
    bool IsReadyForDraft,
    bool IsRunReady,
    RecipeCaptureSessionStatus Status,
    IReadOnlyList<RecipeCaptureBlockingIssue> BlockingIssues,
    IReadOnlyList<RecipeCaptureWarning> Warnings,
    string OperatorSummary)
{
    public bool LiveRuntimeEnabled => false;
    public bool RecorderEnabled => false;
    public bool ReplayEnabled => false;
}

public sealed record RecipeCaptureSession(
    string CaptureSessionId,
    string DisplayName,
    string SourceSummary,
    RecipeTemplateCategory? TargetCategory,
    RecipeTemplateSystem? TargetSystem,
    RecipeTemplateRegion? TargetRegion,
    IReadOnlyList<RecipeTemplateCountry> TargetCountries,
    RecipeCaptureMode SourceMode,
    RecipeCaptureSafetyStatus SafetyStatus,
    RecipeCaptureReadiness Readiness,
    string OperatorIntentSummary,
    IReadOnlyList<RecipeCapturedStepRef> CapturedStepRefs,
    IReadOnlyList<RecipeCapturedEvidenceRef> CapturedEvidenceRefs,
    IReadOnlyList<RecipeCaptureWarning> DetectedSecretWarnings,
    IReadOnlyList<RecipeCaptureWarning> DetectedRiskWarnings,
    RecipeDraftRef? SuggestedRecipeDraftRef,
    IReadOnlyList<RecipeDraftTemplateMapping> SuggestedTemplateMappings,
    string? RedactionSummaryRef,
    IReadOnlyList<string> TimelineRefs,
    IReadOnlyList<RecipeLabSnapshotRef> LabSnapshotRefs,
    DateTimeOffset? CreatedAt = null,
    DateTimeOffset? UpdatedAt = null)
{
    public bool RecorderEnabled => false;
    public bool ReplayEnabled => false;
    public bool LiveRuntimeEnabled => false;
    public bool CanProduceRunReadyRecipe => false;
    public bool CanRecordLiveBrowserDesktop => false;
}

public static class RecipeCaptureSafetyPolicy
{
    public static RecipeCaptureReadiness EvaluateSession(
        RecipeCaptureSession session,
        IReadOnlyList<RecipeCapturedStep> steps)
    {
        var blocking = new List<RecipeCaptureBlockingIssue>();
        var warnings = new List<RecipeCaptureWarning>();

        if (session.SourceMode is RecipeCaptureMode.FutureBrowserCaptureBlocked or RecipeCaptureMode.FutureDesktopCaptureBlocked or RecipeCaptureMode.FutureConnectorCaptureBlocked)
            blocking.Add(Block("capture-live-mode-blocked", RecipeReadinessStatus.BlockedLiveRuntimeDisabled, "Future browser, desktop, or connector capture is blocked in Phase 9.", session.CaptureSessionId));

        if (session.SourceMode == RecipeCaptureMode.Disabled)
            blocking.Add(Block("capture-disabled", RecipeReadinessStatus.BlockedByProtectedScope, "Capture session is disabled by policy.", session.CaptureSessionId));

        foreach (var evidence in session.CapturedEvidenceRefs)
        {
            if (!evidence.SafeForDraft)
                blocking.Add(Block("capture-evidence-not-reference-only", RecipeReadinessStatus.BlockedMissingEvidencePolicy, "Capture evidence must be reference-only and cannot require real capture.", evidence.EvidenceRefId));
        }

        foreach (var step in steps)
            EvaluateStep(step, blocking, warnings);

        warnings.AddRange(session.DetectedRiskWarnings);
        warnings.AddRange(session.DetectedSecretWarnings);

        foreach (var warning in warnings.Where(w => w.BlocksRunReadyConversion))
            blocking.Add(Block($"capture-warning-blocks-run-ready:{warning.WarningId}", ToReadinessStatus(warning.Kind), warning.RedactedSummary, warning.SourceRef));

        var status = blocking.Count == 0 ? RecipeCaptureSessionStatus.CaptureComplete : ToSessionStatus(blocking[0]);
        return new(
            IsReadyForDraft: blocking.All(i => i.IssueId.StartsWith("capture-warning-blocks-run-ready:", StringComparison.Ordinal)),
            IsRunReady: false,
            status,
            blocking,
            warnings,
            blocking.Count == 0 ? "Capture draft is safe for review only; it is not run-ready." : "Capture draft remains blocked for run-ready conversion.");
    }

    public static RecipeDraftReadiness EvaluateDraft(RecipeDraft draft)
    {
        var blocking = draft.BlockingIssues.ToList();
        var warnings = draft.Warnings.ToList();

        foreach (var mapping in draft.TemplateMappings)
        {
            if (!mapping.RequiresCompositeReadiness || mapping.CanOverrideCompositeReadiness)
                blocking.Add(Block("capture-template-readiness-override-blocked", RecipeReadinessStatus.BlockedByProtectedScope, "Template mapping cannot override composite readiness.", mapping.MappingId));

            if (mapping.TemplateReadiness is { IsReady: false })
                blocking.Add(Block("capture-template-composite-readiness-blocked", mapping.TemplateReadiness.CanonicalReadinessStatus, "Template mapping remains blocked by composite readiness.", mapping.MappingId));
        }

        if (draft.ValidationSuggestions.Count == 0)
            blocking.Add(Block("capture-missing-validation-suggestion", RecipeReadinessStatus.BlockedMissingValidation, "Capture draft requires validation suggestions before run-ready conversion.", draft.DraftId));

        if (draft.EvidenceSuggestions.Count == 0)
            blocking.Add(Block("capture-missing-evidence-suggestion", RecipeReadinessStatus.BlockedMissingEvidencePolicy, "Capture draft requires evidence suggestions before run-ready conversion.", draft.DraftId));

        if (draft.ApprovalSuggestions.Count == 0 && draft.Warnings.Any(w => IsSensitiveWarning(w.Kind)))
            blocking.Add(Block("capture-missing-approval-suggestion", RecipeReadinessStatus.BlockedMissingApprovalPolicy, "Sensitive capture draft requires approval or human intervention suggestion.", draft.DraftId));

        var status = ToDraftStatus(blocking);
        return new(
            IsReadyForDraft: true,
            IsRunReady: false,
            status,
            blocking,
            warnings,
            draft.SafeNextAction,
            blocking.Count == 0 ? "Draft is review-ready only; run-ready conversion is disabled." : "Draft requires review and cannot become run-ready.");
    }

    public static RecipeDraftGenerationResult GenerateDraft(
        RecipeCaptureSession session,
        IReadOnlyList<RecipeCapturedStep> steps,
        IReadOnlyList<RecipeDraftTemplateMapping>? templateMappings = null)
    {
        var sessionReadiness = EvaluateSession(session, steps);
        var warnings = sessionReadiness.Warnings.ToList();
        var blocking = sessionReadiness.BlockingIssues.ToList();

        var blockSuggestions = steps.Select(step => new RecipeDraftBlockSuggestion(
            $"block-suggestion:{step.StepId}",
            ToBlockType(step.Kind),
            step.Kind,
            $"Captured {step.Kind}",
            step.IntentSummary,
            step.ValidationSuggestionRefs,
            step.EvidenceRefs.Select(e => e.EvidenceRefId).ToArray(),
            step.ApprovalHumanSuggestionRefs,
            ToRiskLevel(step))).ToArray();

        var validationSuggestions = steps
            .Where(step => step.ValidationSuggestionRefs.Count > 0)
            .Select(step => new RecipeDraftValidationSuggestion($"validation-suggestion:{step.StepId}", RecipeValidationKind.EvidenceRefExists, RecipeValidationSeverity.Blocking, step.StepId))
            .ToArray();

        var evidenceSuggestions = steps
            .SelectMany(step => step.EvidenceRefs.Select(evidence => new RecipeDraftEvidenceSuggestion($"evidence-suggestion:{evidence.EvidenceRefId}", evidence.SourceKind, step.StepId)))
            .ToArray();

        var approvals = steps
            .Where(step => step.ApprovalHumanSuggestionRefs.Count > 0 || IsSensitiveStep(step.Kind))
            .Select(step => new RecipeDraftApprovalSuggestion($"approval-suggestion:{step.StepId}", ToInterventionKind(step.Kind), ToRiskLevel(step), ToSensitiveCategories(step).ToHashSet()))
            .ToArray();

        var variables = steps.SelectMany(step => step.Inputs.Select(input => new RecipeDraftVariableSuggestion(
            $"variable-suggestion:{input.InputId}",
            SanitizeVariableName(input.LabelOrRoleSummary),
            input.ValueCategory,
            input.ValueRedactionStatus,
            Required: true,
            SecretRefOnly: input.SecretLike,
            input.InputId))).ToArray();

        var draft = new RecipeDraft(
            $"draft:{session.CaptureSessionId}",
            $"Draft from {session.DisplayName}",
            session.CaptureSessionId,
            blockSuggestions,
            variables,
            validationSuggestions,
            evidenceSuggestions,
            approvals,
            templateMappings ?? session.SuggestedTemplateMappings,
            RiskLevel: blockSuggestions.Select(b => b.RiskLevel).DefaultIfEmpty(RecipeRiskLevel.Low).Max(),
            warnings,
            blocking,
            new RecipeSafeNextAction(RecipeSafeNextActionKind.RequestMoreEvidence, "Review capture draft, add missing refs, and run composite readiness."),
            ReviewRequired: true);

        var readiness = EvaluateDraft(draft);
        var finalStatus = readiness.Status == RecipeDraftGenerationStatus.DraftCreated ? RecipeDraftGenerationStatus.NeedsReview : readiness.Status;
        return new(finalStatus, draft, readiness, SuggestedRecipeDefinition: null, ReviewRequired: true);
    }

    public static RecipeReadinessStatus ToReadinessStatus(RecipeCaptureWarningKind kind) =>
        kind switch
        {
            RecipeCaptureWarningKind.MissingValidation => RecipeReadinessStatus.BlockedMissingValidation,
            RecipeCaptureWarningKind.MissingEvidence => RecipeReadinessStatus.BlockedMissingEvidencePolicy,
            RecipeCaptureWarningKind.MissingApprovalPath => RecipeReadinessStatus.BlockedMissingApprovalPolicy,
            RecipeCaptureWarningKind.MissingToolTrust => RecipeReadinessStatus.BlockedMissingToolTrust,
            RecipeCaptureWarningKind.MissingSecretRef => RecipeReadinessStatus.BlockedMissingSecretReference,
            RecipeCaptureWarningKind.BrowserLiveAction or RecipeCaptureWarningKind.DesktopLiveAction => RecipeReadinessStatus.BlockedLiveRuntimeDisabled,
            _ => RecipeReadinessStatus.BlockedRiskGate
        };

    internal static RecipeCaptureBlockingIssue Block(string id, RecipeReadinessStatus status, string summary, string? sourceRef = null) =>
        new(id, status, RecipeReadinessIssueSeverity.Blocking, summary, sourceRef);

    internal static RecipeCaptureWarning Warning(string id, RecipeCaptureWarningKind kind, string summary, string? sourceRef = null, bool blocksRunReady = true) =>
        new(id, kind, RecipeReadinessIssueSeverity.Warning, summary, sourceRef, blocksRunReady);

    private static void EvaluateStep(RecipeCapturedStep step, List<RecipeCaptureBlockingIssue> blocking, List<RecipeCaptureWarning> warnings)
    {
        if (step.Kind is RecipeCapturedStepKind.BrowserActionDraft)
            warnings.Add(Warning("capture-browser-draft-live-blocked", RecipeCaptureWarningKind.BrowserLiveAction, "BrowserActionDraft remains draft-only and live-blocked.", step.StepId));

        if (step.Kind is RecipeCapturedStepKind.DesktopActionDraft)
            warnings.Add(Warning("capture-desktop-draft-live-blocked", RecipeCaptureWarningKind.DesktopLiveAction, "DesktopActionDraft remains draft-only and live-blocked.", step.StepId));

        if (step.Kind is RecipeCapturedStepKind.UnknownDraft)
            warnings.Add(Warning("capture-unknown-draft-blocked", RecipeCaptureWarningKind.UnknownUnsafe, "UnknownDraft is blocked until manually reviewed.", step.StepId));

        if (step.Kind is RecipeCapturedStepKind.SubmitDraft)
            warnings.Add(Warning("capture-submit-approval-required", RecipeCaptureWarningKind.SubmitAction, "Submit-like captured draft requires approval or human intervention.", step.StepId));

        foreach (var input in step.Inputs)
        {
            if (input.RawValuePresent)
                blocking.Add(Block("capture-raw-input-detected", RecipeReadinessStatus.BlockedRiskGate, "Captured input cannot include raw values.", input.InputId));

            if (input.SecretLike || input.ValueCategory is RecipeCapturedInputValueCategory.SecretLike or RecipeCapturedInputValueCategory.Credential)
                warnings.Add(Warning("capture-secret-like-input", RecipeCaptureWarningKind.SecretLikeInput, "Secret-like input must become a secret ref and blocks run-ready conversion.", input.InputId));

            if (input.PersonalData)
                warnings.Add(Warning("capture-personal-data-input", RecipeCaptureWarningKind.PersonalDataInput, "Personal data input requires redaction and review.", input.InputId));

            if (input.FiscalData)
                warnings.Add(Warning("capture-fiscal-data-input", RecipeCaptureWarningKind.FiscalDataInput, "Fiscal data input requires human review.", input.InputId));

            if (input.PaymentData)
                warnings.Add(Warning("capture-payment-data-input", RecipeCaptureWarningKind.PaymentDataInput, "Payment data input requires human review.", input.InputId));
        }

        foreach (var locator in step.Locators)
        {
            if (locator.Strategy == RecipeLocatorStrategy.RelativeCoordinate)
                warnings.Add(Warning("capture-relative-coordinate-warning", RecipeCaptureWarningKind.RelativeCoordinateUsed, "RelativeCoordinate is last-resort and requires manual review.", locator.LocatorId));

            if (locator.Strategy == RecipeLocatorStrategy.AIFallback)
                warnings.Add(Warning("capture-ai-fallback-warning", RecipeCaptureWarningKind.AIFallbackSuggested, "AI fallback locator requires policy and human review for sensitive actions.", locator.LocatorId));

            if (locator.DriftStatus is RecipeLocatorDriftStatus.Ambiguous or RecipeLocatorDriftStatus.Broken or RecipeLocatorDriftStatus.Unsafe ||
                locator.SafetyStatus is RecipeLocatorSafetyStatus.UnknownUnsafe or RecipeLocatorSafetyStatus.BlockedMissingEvidence)
                warnings.Add(Warning("capture-locator-ambiguous", RecipeCaptureWarningKind.LocatorAmbiguous, "Captured locator needs repair review.", locator.LocatorId));
        }

        foreach (var evidence in step.EvidenceRefs)
        {
            if (!evidence.SafeForDraft)
                blocking.Add(Block("capture-step-evidence-not-reference-only", RecipeReadinessStatus.BlockedMissingEvidencePolicy, "Captured step evidence must be reference-only.", evidence.EvidenceRefId));
        }

        if (step.ValidationSuggestionRefs.Count == 0)
            warnings.Add(Warning("capture-missing-validation", RecipeCaptureWarningKind.MissingValidation, "Captured step needs validation suggestion.", step.StepId));

        if (step.EvidenceRefs.Count == 0)
            warnings.Add(Warning("capture-missing-evidence", RecipeCaptureWarningKind.MissingEvidence, "Captured step needs evidence refs.", step.StepId));
    }

    private static RecipeCaptureSessionStatus ToSessionStatus(RecipeCaptureBlockingIssue issue) =>
        issue.IssueId switch
        {
            "capture-live-mode-blocked" => RecipeCaptureSessionStatus.BlockedLiveRuntimeDisabled,
            "capture-disabled" => RecipeCaptureSessionStatus.BlockedByPolicy,
            "capture-raw-input-detected" => RecipeCaptureSessionStatus.BlockedRawSecretDetected,
            _ => RecipeCaptureSessionStatus.BlockedByPolicy
        };

    private static RecipeDraftGenerationStatus ToDraftStatus(IReadOnlyList<RecipeCaptureBlockingIssue> blocking)
    {
        if (blocking.Count == 0) return RecipeDraftGenerationStatus.NeedsReview;
        if (blocking.Any(i => i.IssueId.Contains("raw", StringComparison.OrdinalIgnoreCase) || i.IssueId.Contains("secret", StringComparison.OrdinalIgnoreCase))) return RecipeDraftGenerationStatus.BlockedRawSecretDetected;
        if (blocking.Any(i => i.Status == RecipeReadinessStatus.BlockedMissingValidation)) return RecipeDraftGenerationStatus.BlockedMissingValidation;
        if (blocking.Any(i => i.Status == RecipeReadinessStatus.BlockedMissingEvidencePolicy)) return RecipeDraftGenerationStatus.BlockedMissingEvidence;
        if (blocking.Any(i => i.Status == RecipeReadinessStatus.BlockedLiveRuntimeDisabled)) return RecipeDraftGenerationStatus.BlockedLiveRuntimeDisabled;
        return RecipeDraftGenerationStatus.NotRunReady;
    }

    private static bool IsSensitiveWarning(RecipeCaptureWarningKind kind) =>
        kind is RecipeCaptureWarningKind.SecretLikeInput or RecipeCaptureWarningKind.PersonalDataInput or RecipeCaptureWarningKind.FiscalDataInput or RecipeCaptureWarningKind.PaymentDataInput or RecipeCaptureWarningKind.CredentialUse or RecipeCaptureWarningKind.TwoFactorOrCaptcha or RecipeCaptureWarningKind.SubmitAction or RecipeCaptureWarningKind.DeleteAction or RecipeCaptureWarningKind.PublicPosting or RecipeCaptureWarningKind.MarketplaceMutation or RecipeCaptureWarningKind.PriceOrStockChange or RecipeCaptureWarningKind.FileWrite or RecipeCaptureWarningKind.ExternalMutation or RecipeCaptureWarningKind.UnknownSensitiveAction;

    private static bool IsSensitiveStep(RecipeCapturedStepKind kind) =>
        kind is RecipeCapturedStepKind.SubmitDraft or RecipeCapturedStepKind.UploadDraft or RecipeCapturedStepKind.ConnectorDraft or RecipeCapturedStepKind.DesktopActionDraft or RecipeCapturedStepKind.BrowserActionDraft or RecipeCapturedStepKind.UnknownDraft;

    private static RecipeBlockType ToBlockType(RecipeCapturedStepKind kind) =>
        kind switch
        {
            RecipeCapturedStepKind.ExtractDraft => RecipeBlockType.Extract,
            RecipeCapturedStepKind.DownloadDraft => RecipeBlockType.FileDownloadEvidence,
            RecipeCapturedStepKind.ValidateDraft => RecipeBlockType.Validate,
            RecipeCapturedStepKind.HumanInterventionDraft => RecipeBlockType.HumanIntervention,
            RecipeCapturedStepKind.ApprovalDraft => RecipeBlockType.Approval,
            RecipeCapturedStepKind.WorkitemDraft => RecipeBlockType.WorkitemCreateNextStage,
            RecipeCapturedStepKind.TriggerObservationDraft => RecipeBlockType.Wait,
            RecipeCapturedStepKind.ConnectorDraft => RecipeBlockType.ConnectorDraft,
            RecipeCapturedStepKind.DesktopActionDraft => RecipeBlockType.DesktopActionDraft,
            RecipeCapturedStepKind.BrowserActionDraft => RecipeBlockType.BrowserAction,
            RecipeCapturedStepKind.WaitDraft => RecipeBlockType.Wait,
            _ => RecipeBlockType.BrowserGoal
        };

    private static RecipeHumanInterventionKind ToInterventionKind(RecipeCapturedStepKind kind) =>
        kind switch
        {
            RecipeCapturedStepKind.SubmitDraft => RecipeHumanInterventionKind.OperatorChoiceRequired,
            RecipeCapturedStepKind.ConnectorDraft => RecipeHumanInterventionKind.ExternalSystemMutationReview,
            RecipeCapturedStepKind.DesktopActionDraft or RecipeCapturedStepKind.BrowserActionDraft => RecipeHumanInterventionKind.PolicyBlockedReview,
            _ => RecipeHumanInterventionKind.ManualCheckpoint
        };

    private static RecipeRiskLevel ToRiskLevel(RecipeCapturedStep step)
    {
        if (step.Kind is RecipeCapturedStepKind.SubmitDraft or RecipeCapturedStepKind.ConnectorDraft or RecipeCapturedStepKind.DesktopActionDraft or RecipeCapturedStepKind.BrowserActionDraft or RecipeCapturedStepKind.UnknownDraft)
            return RecipeRiskLevel.High;

        if (step.Inputs.Any(i => i.SecretLike || i.FiscalData || i.PaymentData))
            return RecipeRiskLevel.High;

        if (step.Inputs.Any(i => i.PersonalData))
            return RecipeRiskLevel.Medium;

        return RecipeRiskLevel.Low;
    }

    private static IEnumerable<SensitiveActionCategory> ToSensitiveCategories(RecipeCapturedStep step)
    {
        if (step.Inputs.Any(i => i.SecretLike)) yield return SensitiveActionCategory.SecretHandling;
        if (step.Inputs.Any(i => i.PaymentData)) yield return SensitiveActionCategory.Payment;
        if (step.Inputs.Any(i => i.FiscalData)) yield return SensitiveActionCategory.FiscalOrLegalSubmission;
        if (step.Inputs.Any(i => i.PersonalData)) yield return SensitiveActionCategory.PersonalDataHandling;
        if (step.Kind == RecipeCapturedStepKind.SubmitDraft) yield return SensitiveActionCategory.ExternalSystemMutation;
        if (step.Kind == RecipeCapturedStepKind.BrowserActionDraft) yield return SensitiveActionCategory.BrowserLiveAction;
        if (step.Kind == RecipeCapturedStepKind.DesktopActionDraft) yield return SensitiveActionCategory.DesktopLiveAction;
        if (step.Kind == RecipeCapturedStepKind.UnknownDraft) yield return SensitiveActionCategory.UnknownSensitiveAction;
    }

    private static string SanitizeVariableName(string label)
    {
        var chars = label.ToLowerInvariant().Select(c => char.IsLetterOrDigit(c) ? c : '_').ToArray();
        var name = new string(chars).Trim('_');
        return string.IsNullOrWhiteSpace(name) ? "captured_value" : name;
    }
}

public static class RecipeCaptureTemplateMapper
{
    public static RecipeDraftTemplateMapping MapToTemplate(
        RecipeCaptureSession session,
        RecipeTemplateCatalog catalog,
        RecipeTemplateReadinessContext readinessContext)
    {
        var candidate = catalog.Templates.FirstOrDefault(t =>
            session.TargetSystem.HasValue && t.System == session.TargetSystem.Value) ??
            catalog.Templates.FirstOrDefault(t => session.TargetCategory.HasValue && t.Category == session.TargetCategory.Value);

        if (candidate is null)
        {
            return new(
                $"mapping:{session.CaptureSessionId}:unknown",
                TemplateId: null,
                session.TargetCategory ?? RecipeTemplateCategory.Unknown,
                session.TargetSystem ?? RecipeTemplateSystem.Unknown,
                RecipeTemplateRuntimeEligibility.FutureGated,
                TemplateReadiness: null,
                "No matching template; keep capture draft blocked for manual review.");
        }

        var readiness = RecipeTemplateReadinessEvaluator.Evaluate(candidate, readinessContext);
        return new(
            $"mapping:{session.CaptureSessionId}:{candidate.TemplateId}",
            candidate.TemplateId,
            candidate.Category,
            candidate.System,
            candidate.RuntimeEligibility,
            readiness,
            $"Capture draft maps to template {candidate.TemplateId} with composite readiness preserved.");
    }
}
