namespace OneBrain.Core.Recipes;

public enum RecipeEvidenceSourceKind
{
    ScreenshotBeforeRef,
    ScreenshotAfterRef,
    VisibleTreeRef,
    AccessibilityTreeRef,
    DomSnapshotRef,
    NetworkSummaryRedactedRef,
    DownloadedFileRef,
    UploadedFileRef,
    GeneratedFileRef,
    ExtractedDataRef,
    ValidationResultRef,
    ApprovalDecisionRef,
    PolicyDecisionRef,
    WorkitemStateRef,
    TimelineEventRef,
    ErrorTraceRef,
    HumanNoteRef,
    HandoffSummaryRef
}

public enum RecipeEvidenceSensitivity
{
    Public,
    Internal,
    Confidential,
    Sensitive,
    Secret,
    UnknownSensitive
}

public enum RecipeEvidenceCompleteness
{
    NotStarted,
    Partial,
    Complete,
    BlockedMissingRequiredEvidence,
    BlockedByRedactionPolicy,
    BlockedLiveRuntimeDisabled,
    BlockedByProtectedScope
}

public enum RecipeEvidenceCaptureMode
{
    FixtureOnly,
    ReferenceOnly,
    ManualAttachment,
    FutureBrowserRuntime,
    FutureDesktopRuntime,
    FutureConnectorRuntime
}

public enum RecipeEvidenceRedactionStatus
{
    NotRequired,
    Pending,
    Applied,
    BlockedRawPayloadExposure,
    BlockedSecretExposure,
    BlockedUnknownSensitive
}

public enum RecipeEvidenceSecretHandlingStatus
{
    NoSecretsPresent,
    SecretRefsOnly,
    Redacted,
    BlockedRawSecretValue
}

public enum RecipeSensitiveFieldCategory
{
    Secret,
    Credential,
    Token,
    PersonalData,
    FiscalData,
    PaymentData,
    LegalData,
    MarketplaceData,
    BusinessConfidential,
    UnknownSensitive
}

public enum RecipeStepEvidenceStatus
{
    NotStarted,
    Satisfied,
    MissingRequiredEvidence,
    BlockedByRedactionPolicy,
    BlockedLiveRuntimeDisabled,
    Failed
}

public enum RecipeValidationEvidenceStatus
{
    Passed,
    Failed,
    Blocked,
    Warning,
    NotRun
}

public enum RecipeFailureClass
{
    Business,
    Application,
    Policy,
    Validation,
    Perception,
    Locator,
    Auth,
    Challenge,
    Timeout,
    RateLimit,
    ExternalSystem,
    UnknownUnsafe
}

public enum RecipeFailureRecoveryHint
{
    None,
    RetryIfPolicyAllows,
    FixBusinessData,
    AddValidationEvidence,
    RequestHumanIntervention,
    RequestApproval,
    StopBlockedByPolicy,
    AbortUnsafe
}

public enum RecipeTimelineProjectionStatus
{
    NotProjected,
    Projected,
    Partial,
    BlockedMissingEvidence,
    BlockedByRedactionPolicy,
    BlockedLiveRuntimeDisabled
}

public enum RecipeTimelineEventKind
{
    RecipeRunCreated,
    RecipeReadinessEvaluated,
    RecipeRunStartedFixture,
    RecipeStepPlanned,
    RecipeStepValidated,
    RecipeStepBlocked,
    RecipeStepSucceeded,
    RecipeStepFailed,
    WorkitemQueued,
    WorkitemProcessing,
    WorkitemSucceeded,
    WorkitemRetryScheduled,
    WorkitemFailedBusiness,
    WorkitemFailedApplication,
    HumanInterventionRequested,
    ApprovalRequired,
    ApprovalRecordedRef,
    EvidenceCapturedRef,
    EvidenceMissing,
    RedactionApplied,
    RiskGateBlocked,
    ActionResolutionBlocked,
    RecipeRunCompleted,
    RecipeRunFailed,
    RecipeRunCancelled,
    HandoffCreated
}

public enum RecipeTimelineEventSeverity
{
    Info,
    Warning,
    Blocking,
    Failed
}

public enum RecipeTimelineEventSource
{
    System,
    Operator,
    Policy,
    FutureRuntime,
    Fixture
}

public sealed record RecipeEvidencePackRef(string EvidencePackId);

public sealed record RecipeEvidenceItemRef(string EvidenceItemId, RecipeEvidenceSourceKind SourceKind);

public sealed record RecipeArtifactRef(
    string ArtifactId,
    string ArtifactKind,
    string RefId,
    bool RawBytesEmbedded = false,
    bool RealFileProbeRequired = false);

public sealed record RecipeTimelineEventRef(string EventId);

public sealed record RecipeStepEvidenceRef(string StepEvidenceId);

public sealed record RecipeValidationEvidenceRef(string ValidationEvidenceId);

public sealed record RecipeFailureEvidenceRef(string FailureEvidenceId);

public sealed record RecipeSensitiveFieldSummary(
    RecipeSensitiveFieldCategory Category,
    string RedactedSummary,
    string? RefId = null,
    bool RawValuePresent = false);

public sealed record RecipeEvidenceRedactionSummary(
    bool RedactionApplied,
    string? RedactionPolicyRef,
    IReadOnlyList<RecipeSensitiveFieldSummary> SensitiveFields,
    IReadOnlyList<string> SecretRefs,
    RecipeEvidenceSecretHandlingStatus SecretHandlingStatus,
    bool RawPayloadExposed = false,
    bool EvidenceSafeForHandoff = true,
    bool EvidenceSafeForTimeline = true,
    string? BlockedByRedactionReason = null)
{
    public bool HasRawSecretExposure =>
        SecretHandlingStatus == RecipeEvidenceSecretHandlingStatus.BlockedRawSecretValue ||
        SensitiveFields.Any(summary => summary.RawValuePresent) ||
        RawPayloadExposed;
}

public sealed record RecipeEvidenceItem(
    string EvidenceItemId,
    RecipeEvidenceSourceKind SourceKind,
    string RefId,
    string RedactedSummary,
    RecipeEvidenceSensitivity Sensitivity,
    RecipeEvidenceCaptureMode CaptureMode,
    RecipeEvidenceRedactionStatus RedactionStatus,
    bool RawPayloadEmbedded = false,
    bool RequiresRealCapture = false,
    string? HashRef = null)
{
    public bool LiveRuntimeEnabled => false;
    public bool IsReferenceOnly => !RawPayloadEmbedded && !RequiresRealCapture;
}

public sealed record RecipeStepEvidence(
    string StepEvidenceId,
    string StepId,
    string RunId,
    string BlockId,
    int SequenceNumber,
    string ActionIntentSummary,
    string TargetSummary,
    IReadOnlyList<string> BeforeStateRefs,
    IReadOnlyList<string> AfterStateRefs,
    IReadOnlyList<string> ValidationRefs,
    IReadOnlyList<string> ArtifactRefs,
    IReadOnlyList<string> ApprovalRefs,
    IReadOnlyList<string> PolicyDecisionRefs,
    RecipeEvidenceRedactionStatus RedactionStatus,
    RecipeStepEvidenceStatus ResultStatus,
    string? FailureEvidenceRef = null)
{
    public bool LiveRuntimeEnabled => false;
    public bool ActionAuthorityGranted => false;
}

public sealed record RecipeStepEvidenceRequirement(
    string RequirementId,
    RecipeBlockType BlockType,
    bool RequiresBeforeStateRef = false,
    bool RequiresAfterStateRef = false,
    bool RequiresDownloadedFileRef = false,
    bool RequiresValidationEvidenceRef = false,
    bool RequiresWorkitemStateEvidenceRef = false,
    bool RequiresHumanNoteOrApprovalRef = false,
    bool RequiresPolicyDecisionRef = false,
    bool AllowsFutureOnlyRefs = false);

public sealed record RecipeStepEvidenceResult(
    bool Satisfied,
    RecipeStepEvidenceStatus Status,
    IReadOnlyList<string> MissingRefs,
    IReadOnlyList<string> Reasons)
{
    public bool LiveRuntimeEnabled => false;
    public bool ActionAuthorityGranted => false;
}

public sealed record RecipeValidationEvidence(
    string ValidationEvidenceId,
    RecipeValidationKind ValidationKind,
    string ExpectedValueSummary,
    string ActualValueSummary,
    IReadOnlyList<string> SourceEvidenceRefs,
    RecipeValidationEvidenceStatus Status,
    RecipeValidationSeverity BlockingSeverity,
    RecipeEvidenceRedactionStatus RedactionStatus,
    string? FailureReason = null)
{
    public bool RawSecretValueExposed => RedactionStatus == RecipeEvidenceRedactionStatus.BlockedSecretExposure;
}

public sealed record RecipeValidationEvidenceRequirement(
    string RequirementId,
    RecipeValidationKind ValidationKind,
    RecipeValidationSeverity Severity,
    bool RequiredForCompleteness = true);

public sealed record RecipeFailureEvidence(
    string FailureEvidenceId,
    WorkitemFailureType FailureType,
    RecipeFailureClass FailureClass,
    string? FailedStepId,
    string? FailedBlockId,
    IReadOnlyList<string> ObservedStateRefs,
    IReadOnlyList<string> ValidationRefs,
    IReadOnlyList<string> PolicyDecisionRefs,
    IReadOnlyList<string> RetryDecisionRefs,
    bool HumanInterventionRequired,
    RecipeFailureRecoveryHint RecoveryHint,
    string SafeNextAction,
    RecipeEvidenceRedactionStatus RedactionStatus)
{
    public bool IsBlocking =>
        FailureClass == RecipeFailureClass.UnknownUnsafe ||
        RecoveryHint is RecipeFailureRecoveryHint.RequestHumanIntervention or RecipeFailureRecoveryHint.StopBlockedByPolicy or RecipeFailureRecoveryHint.AbortUnsafe;

    public bool SuggestsBypass => SafeNextAction.Contains("bypass", StringComparison.OrdinalIgnoreCase);
}

public sealed record RecipeTimelineEvent(
    string EventId,
    string RunId,
    string RecipeId,
    RecipeTimelineEventKind EventKind,
    RecipeTimelineProjectionStatus Status,
    string Summary,
    IReadOnlyList<string> EvidenceRefs,
    IReadOnlyList<string> ValidationRefs,
    IReadOnlyList<string> ApprovalRefs,
    IReadOnlyList<string> RiskGateRefs,
    IReadOnlyList<string> RedactionRefs,
    DateTimeOffset? Timestamp,
    RecipeTimelineEventSeverity Severity,
    RecipeTimelineEventSource Source,
    string? BlockId = null,
    string? StepId = null,
    string? WorkitemId = null)
{
    public bool LiveRuntimeEnabled => false;
    public bool ActionAuthorityGranted => false;
}

public sealed record RecipeTimelineProjection(
    string ProjectionId,
    string RunId,
    string RecipeId,
    RecipeTimelineProjectionStatus Status,
    IReadOnlyList<RecipeTimelineEvent> Events,
    string RedactedSummary,
    bool RedactionApplied,
    bool UsesExistingTimelineRefs = true)
{
    public bool LiveRuntimeEnabled => false;
}

public sealed record RecipeEvidencePack(
    string EvidencePackId,
    string RecipeId,
    string RecipeVersion,
    string RunId,
    string? MissionIdRef,
    IReadOnlyList<string> WorkitemRefs,
    IReadOnlyList<string> StepEvidenceRefs,
    IReadOnlyList<string> ValidationEvidenceRefs,
    IReadOnlyList<string> ApprovalRefs,
    IReadOnlyList<string> TimelineEventRefs,
    IReadOnlyList<string> ArtifactRefs,
    string? RedactionReportRef,
    RecipeEvidenceSensitivity SensitivitySummary,
    RecipeEvidenceCompleteness CompletenessStatus,
    RecipeEvidenceCaptureMode CaptureMode,
    DateTimeOffset? CreatedAt,
    RecipeRunMode SourceRuntimeMode,
    string? FailureSummary,
    RecipeEvidenceRedactionSummary RedactionSummary)
{
    public bool LiveBrowserRuntimeEnabled => false;
    public bool LiveDesktopRuntimeEnabled => false;
    public bool LiveRuntimeEnabled => false;
    public bool ActionAuthorityGranted => false;
    public bool ReferenceOnly => CaptureMode is RecipeEvidenceCaptureMode.FixtureOnly or RecipeEvidenceCaptureMode.ReferenceOnly or RecipeEvidenceCaptureMode.ManualAttachment;
}

public sealed record RecipeEvidenceHandoffSummary(
    string RecipeId,
    string RecipeVersion,
    string RunId,
    RecipeRunStatus Status,
    RecipeEvidenceCompleteness EvidenceCompleteness,
    RecipeTimelineProjectionStatus TimelineProjectionStatus,
    IReadOnlyList<string> BlockingIssues,
    string SafeNextAction,
    string OperatorVisibleSummary,
    RecipeEvidenceRedactionSummary RedactionSummary,
    IReadOnlyList<string> ArtifactRefs,
    IReadOnlyList<string> RawDataOmitted)
{
    public bool IncludesRawPayloads => false;
    public bool IncludesSecretValues => false;
    public bool SafeByDefault => !RedactionSummary.HasRawSecretExposure && RedactionSummary.EvidenceSafeForHandoff;
}

public sealed record RecipeEvidenceExportSummary(
    string RecipeId,
    string RecipeVersion,
    string RunId,
    RecipeEvidenceCompleteness EvidenceCompleteness,
    RecipeTimelineProjectionStatus TimelineProjectionStatus,
    IReadOnlyList<string> ArtifactRefs,
    RecipeEvidenceRedactionSummary RedactionSummary,
    IReadOnlyList<string> NotIncludedRawDataOmitted)
{
    public bool IncludesRawPayloads => false;
    public bool IncludesSecretValues => false;
}

public static class RecipeEvidencePolicy
{
    public static RecipeStepEvidenceRequirement RequirementFor(RecipeBlockType blockType) =>
        blockType switch
        {
            RecipeBlockType.BrowserAction => new("browser-action-state-refs", blockType, RequiresBeforeStateRef: true, RequiresAfterStateRef: true, RequiresValidationEvidenceRef: true),
            RecipeBlockType.FileDownloadEvidence => new("file-download-evidence-refs", blockType, RequiresDownloadedFileRef: true, RequiresValidationEvidenceRef: true),
            RecipeBlockType.WorkitemUpdate => new("workitem-state-evidence-ref", blockType, RequiresWorkitemStateEvidenceRef: true),
            RecipeBlockType.HumanIntervention => new("human-note-approval-ref", blockType, RequiresHumanNoteOrApprovalRef: true),
            RecipeBlockType.ConnectorDraft => new("connector-draft-policy-secret-refs", blockType, RequiresPolicyDecisionRef: true),
            RecipeBlockType.DesktopActionDraft => new("desktop-draft-future-refs-only", blockType, RequiresPolicyDecisionRef: true, AllowsFutureOnlyRefs: true),
            _ => new("generic-evidence-ref", blockType)
        };

    public static RecipeStepEvidenceResult EvaluateStepEvidence(RecipeBlock block, RecipeStepEvidence evidence, bool meaningfulActionRequiresStateRefs = true)
    {
        var requirement = RequirementFor(block.BlockType);
        var missing = new List<string>();
        var reasons = new List<string>();

        if (meaningfulActionRequiresStateRefs && requirement.RequiresBeforeStateRef && evidence.BeforeStateRefs.Count == 0)
            missing.Add("before-state-ref");

        if (meaningfulActionRequiresStateRefs && requirement.RequiresAfterStateRef && evidence.AfterStateRefs.Count == 0)
            missing.Add("after-state-ref");

        if (requirement.RequiresDownloadedFileRef && !evidence.ArtifactRefs.Any(r => r.Contains("download", StringComparison.OrdinalIgnoreCase) || r.Contains("file", StringComparison.OrdinalIgnoreCase)))
            missing.Add("downloaded-file-ref");

        if (requirement.RequiresValidationEvidenceRef && evidence.ValidationRefs.Count == 0)
            missing.Add("validation-evidence-ref");

        if (requirement.RequiresWorkitemStateEvidenceRef && !evidence.ArtifactRefs.Any(r => r.Contains("workitem", StringComparison.OrdinalIgnoreCase)))
            missing.Add("workitem-state-evidence-ref");

        if (requirement.RequiresHumanNoteOrApprovalRef && evidence.ApprovalRefs.Count == 0 && !evidence.ArtifactRefs.Any(r => r.Contains("human-note", StringComparison.OrdinalIgnoreCase)))
            missing.Add("human-note-or-approval-ref");

        if (requirement.RequiresPolicyDecisionRef && evidence.PolicyDecisionRefs.Count == 0)
            missing.Add("policy-decision-ref");

        if (block.BlockType == RecipeBlockType.DesktopActionDraft)
            reasons.Add("DesktopActionDraft evidence is planned/future metadata only.");

        var satisfied = missing.Count == 0;
        return new RecipeStepEvidenceResult(
            satisfied,
            satisfied ? RecipeStepEvidenceStatus.Satisfied : RecipeStepEvidenceStatus.MissingRequiredEvidence,
            missing,
            reasons);
    }

    public static RecipeEvidenceCompleteness EvaluatePackCompleteness(
        RecipeEvidencePack pack,
        IReadOnlyList<RecipeStepEvidenceResult> stepResults,
        IReadOnlyList<RecipeValidationEvidence> validationEvidence)
    {
        if (pack.CaptureMode is RecipeEvidenceCaptureMode.FutureBrowserRuntime or RecipeEvidenceCaptureMode.FutureDesktopRuntime)
            return RecipeEvidenceCompleteness.BlockedLiveRuntimeDisabled;

        if (pack.RedactionSummary.HasRawSecretExposure || !pack.RedactionSummary.EvidenceSafeForHandoff || !pack.RedactionSummary.EvidenceSafeForTimeline)
            return RecipeEvidenceCompleteness.BlockedByRedactionPolicy;

        if (stepResults.Any(r => !r.Satisfied) || validationEvidence.Any(v => v.Status is RecipeValidationEvidenceStatus.Blocked or RecipeValidationEvidenceStatus.NotRun && v.BlockingSeverity == RecipeValidationSeverity.Blocking))
            return RecipeEvidenceCompleteness.BlockedMissingRequiredEvidence;

        return RecipeEvidenceCompleteness.Complete;
    }

    public static RecipeEvidenceHandoffSummary CreateHandoffSummary(
        RecipeEvidencePack pack,
        RecipeTimelineProjection projection,
        IReadOnlyList<string>? blockingIssues = null) =>
        new(
            pack.RecipeId,
            pack.RecipeVersion,
            pack.RunId,
            pack.CompletenessStatus == RecipeEvidenceCompleteness.Complete ? RecipeRunStatus.Succeeded : RecipeRunStatus.BlockedByPolicy,
            pack.CompletenessStatus,
            projection.Status,
            blockingIssues ?? [],
            pack.CompletenessStatus == RecipeEvidenceCompleteness.Complete ? "Proceed with fixture-safe review." : "Stop and request human review.",
            "Recipe evidence summary contains references and redacted summaries only.",
            pack.RedactionSummary,
            pack.ArtifactRefs,
            ["raw screenshots", "raw DOM", "raw accessibility tree", "HAR/network logs", "secret values", "raw payloads"]);
}

public static class RecipeFailureEvidenceFactory
{
    public static RecipeFailureEvidence Create(
        string failureEvidenceId,
        WorkitemFailureType failureType,
        string? stepId = null,
        string? blockId = null)
    {
        var failureClass = failureType switch
        {
            WorkitemFailureType.Business => RecipeFailureClass.Business,
            WorkitemFailureType.Application => RecipeFailureClass.Application,
            WorkitemFailureType.Policy => RecipeFailureClass.Policy,
            WorkitemFailureType.Validation => RecipeFailureClass.Validation,
            WorkitemFailureType.Perception => RecipeFailureClass.Perception,
            WorkitemFailureType.Locator => RecipeFailureClass.Locator,
            WorkitemFailureType.Auth => RecipeFailureClass.Auth,
            WorkitemFailureType.Challenge => RecipeFailureClass.Challenge,
            WorkitemFailureType.Timeout => RecipeFailureClass.Timeout,
            WorkitemFailureType.RateLimit => RecipeFailureClass.RateLimit,
            WorkitemFailureType.ExternalSystem => RecipeFailureClass.ExternalSystem,
            _ => RecipeFailureClass.UnknownUnsafe
        };

        var hint = failureClass switch
        {
            RecipeFailureClass.Auth or RecipeFailureClass.Challenge => RecipeFailureRecoveryHint.RequestHumanIntervention,
            RecipeFailureClass.Policy => RecipeFailureRecoveryHint.StopBlockedByPolicy,
            RecipeFailureClass.UnknownUnsafe => RecipeFailureRecoveryHint.AbortUnsafe,
            RecipeFailureClass.Validation => RecipeFailureRecoveryHint.AddValidationEvidence,
            RecipeFailureClass.Business => RecipeFailureRecoveryHint.FixBusinessData,
            _ => RecipeFailureRecoveryHint.RetryIfPolicyAllows
        };

        var human = hint == RecipeFailureRecoveryHint.RequestHumanIntervention;
        var safeNextAction = hint switch
        {
            RecipeFailureRecoveryHint.RequestHumanIntervention => "Request human intervention; do not solve or automate challenge or authentication.",
            RecipeFailureRecoveryHint.StopBlockedByPolicy => "Stop; policy blocked and no override is available in this phase.",
            RecipeFailureRecoveryHint.AbortUnsafe => "Abort; unknown unsafe state.",
            RecipeFailureRecoveryHint.AddValidationEvidence => "Add fixture validation evidence before continuing.",
            _ => "Follow retry policy representation only."
        };

        return new RecipeFailureEvidence(
            failureEvidenceId,
            failureType,
            failureClass,
            stepId,
            blockId,
            ObservedStateRefs: [],
            ValidationRefs: [],
            PolicyDecisionRefs: [],
            RetryDecisionRefs: [],
            HumanInterventionRequired: human,
            RecoveryHint: hint,
            safeNextAction,
            RecipeEvidenceRedactionStatus.Applied);
    }
}

public static class RecipeTimelineProjector
{
    public static RecipeTimelineEvent FromReadinessIssue(
        string eventId,
        string runId,
        string recipeId,
        RecipeReadinessIssue issue,
        DateTimeOffset? timestamp = null)
    {
        var kind = issue.Status switch
        {
            RecipeReadinessStatus.BlockedRiskGate => RecipeTimelineEventKind.RiskGateBlocked,
            RecipeReadinessStatus.BlockedActionResolutionPolicy => RecipeTimelineEventKind.ActionResolutionBlocked,
            RecipeReadinessStatus.BlockedLiveRuntimeDisabled => RecipeTimelineEventKind.RecipeStepBlocked,
            _ => RecipeTimelineEventKind.RecipeReadinessEvaluated
        };

        return Blocking(eventId, runId, recipeId, kind, issue.Message, timestamp, blockId: issue.BlockId);
    }

    public static RecipeTimelineEvent FromValidationEvidence(
        string eventId,
        string runId,
        string recipeId,
        RecipeValidationEvidence validation,
        DateTimeOffset? timestamp = null)
    {
        var failed = validation.Status is RecipeValidationEvidenceStatus.Failed or RecipeValidationEvidenceStatus.Blocked;
        return new RecipeTimelineEvent(
            eventId,
            runId,
            recipeId,
            failed ? RecipeTimelineEventKind.RecipeStepFailed : RecipeTimelineEventKind.RecipeStepValidated,
            failed ? RecipeTimelineProjectionStatus.Projected : RecipeTimelineProjectionStatus.Projected,
            validation.FailureReason ?? $"Validation {validation.ValidationKind} {validation.Status}.",
            validation.SourceEvidenceRefs,
            [validation.ValidationEvidenceId],
            ApprovalRefs: [],
            RiskGateRefs: [],
            RedactionRefs: validation.RedactionStatus == RecipeEvidenceRedactionStatus.Applied ? ["redaction.applied"] : [],
            timestamp,
            failed ? RecipeTimelineEventSeverity.Blocking : RecipeTimelineEventSeverity.Info,
            RecipeTimelineEventSource.Fixture);
    }

    public static RecipeTimelineEvent HumanIntervention(
        string eventId,
        string runId,
        string recipeId,
        string summary,
        IReadOnlyList<string>? evidenceRefs = null,
        DateTimeOffset? timestamp = null) =>
        Blocking(eventId, runId, recipeId, RecipeTimelineEventKind.HumanInterventionRequested, summary, timestamp, evidenceRefs ?? []);

    public static RecipeTimelineEvent EvidenceMissing(
        string eventId,
        string runId,
        string recipeId,
        string summary,
        DateTimeOffset? timestamp = null) =>
        Blocking(eventId, runId, recipeId, RecipeTimelineEventKind.EvidenceMissing, summary, timestamp);

    public static RecipeTimelineEvent RedactionApplied(
        string eventId,
        string runId,
        string recipeId,
        IReadOnlyList<string> redactionRefs,
        DateTimeOffset? timestamp = null) =>
        new(
            eventId,
            runId,
            recipeId,
            RecipeTimelineEventKind.RedactionApplied,
            RecipeTimelineProjectionStatus.Projected,
            "Redaction applied to recipe evidence summaries.",
            EvidenceRefs: [],
            ValidationRefs: [],
            ApprovalRefs: [],
            RiskGateRefs: [],
            RedactionRefs: redactionRefs,
            timestamp,
            RecipeTimelineEventSeverity.Info,
            RecipeTimelineEventSource.Policy);

    public static RecipeTimelineProjection CreateProjection(
        string projectionId,
        string runId,
        string recipeId,
        IReadOnlyList<RecipeTimelineEvent> events,
        string redactedSummary)
    {
        var status = events.Any(e => e.Status == RecipeTimelineProjectionStatus.BlockedLiveRuntimeDisabled)
            ? RecipeTimelineProjectionStatus.BlockedLiveRuntimeDisabled
            : events.Any(e => e.EventKind == RecipeTimelineEventKind.EvidenceMissing)
                ? RecipeTimelineProjectionStatus.BlockedMissingEvidence
                : RecipeTimelineProjectionStatus.Projected;

        return new RecipeTimelineProjection(projectionId, runId, recipeId, status, events, redactedSummary, RedactionApplied: true);
    }

    private static RecipeTimelineEvent Blocking(
        string eventId,
        string runId,
        string recipeId,
        RecipeTimelineEventKind kind,
        string summary,
        DateTimeOffset? timestamp,
        IReadOnlyList<string>? evidenceRefs = null,
        string? blockId = null) =>
        new(
            eventId,
            runId,
            recipeId,
            kind,
            RecipeTimelineProjectionStatus.Projected,
            summary,
            evidenceRefs ?? [],
            ValidationRefs: [],
            ApprovalRefs: [],
            RiskGateRefs: kind == RecipeTimelineEventKind.RiskGateBlocked ? ["risk.gate.blocked"] : [],
            RedactionRefs: [],
            timestamp,
            RecipeTimelineEventSeverity.Blocking,
            RecipeTimelineEventSource.Policy,
            BlockId: blockId);
}
