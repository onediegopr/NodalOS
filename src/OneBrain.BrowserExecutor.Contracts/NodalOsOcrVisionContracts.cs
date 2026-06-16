namespace OneBrain.BrowserExecutor.Contracts;

public readonly record struct NodalOsOcrVisionProviderId(string Value);

public enum NodalOsOcrVisionProviderKind
{
    LocalOpenSource,
    LocalTesseract,
    LocalPaddleOcr,
    LocalEasyOcr,
    CloudDocumentAi,
    CloudAzureDocumentIntelligence,
    CloudGoogleDocumentAi,
    CloudGoogleVision,
    CloudAmazonTextract,
    CloudMistralOcr,
    CloudOpenAiVision,
    HumanReview,
    DisabledStub
}

public enum NodalOsOcrVisionProviderStatus
{
    Disabled,
    Enabled,
    Paused,
    Testing,
    ShadowOnly,
    Primary,
    Fallback,
    BlockedByPolicy,
    BlockedByBudget,
    BlockedByPrivacy,
    Degraded,
    Error
}

[Flags]
public enum NodalOsOcrVisionProviderCapability
{
    None = 0,
    PrintedText = 1 << 0,
    SimpleUiCrop = 1 << 1,
    ComplexLayout = 1 << 2,
    Tables = 1 << 3,
    Forms = 1 << 4,
    Invoices = 1 << 5,
    Receipts = 1 << 6,
    IdentityDocuments = 1 << 7,
    Handwriting = 1 << 8,
    MixedPrintedHandwriting = 1 << 9,
    LowQualityImage = 1 << 10,
    SkewedImage = 1 << 11,
    ScreenshotUi = 1 << 12,
    Pdf = 1 << 13,
    MultiPage = 1 << 14,
    BoundingBoxes = 1 << 15,
    StructuredJson = 1 << 16,
    MarkdownOutput = 1 << 17
}

public sealed record NodalOsOcrVisionProviderCostProfile(
    decimal MaxCostPerPage,
    decimal MaxCostPerImage,
    decimal DailyBudget,
    decimal MonthlyBudget,
    string Currency);

public sealed record NodalOsOcrVisionProviderPerformanceProfile(
    int MaxLatencyMs,
    double ExpectedConfidence,
    string ExpectedConfidenceBand);

public sealed record NodalOsOcrVisionProviderPrivacyProfile(
    bool ExternalDataTransfer,
    bool StoresInput,
    bool AllowsSensitive,
    bool RequiresRedaction,
    string PrivacyNote);

public sealed record NodalOsOcrVisionProviderPolicy(
    bool Enabled,
    bool RequiresOptIn,
    bool RequiresApiKey,
    bool ApiKeyConfigured,
    bool ExternalDataTransfer,
    bool AllowedForSensitive,
    bool AllowedForFullScreen,
    bool AllowedForCrops,
    decimal MaxCostPerPage,
    decimal MaxCostPerImage,
    decimal DailyBudget,
    decimal MonthlyBudget,
    double MinConfidence,
    int MaxLatencyMs,
    int Priority,
    IReadOnlyList<NodalOsOcrVisionProviderId> FallbackOrder,
    string DisabledReason,
    bool AuditRequired,
    bool RedactionRequired);

public sealed record NodalOsOcrVisionProviderConfiguration(
    NodalOsOcrVisionProviderId ProviderId,
    NodalOsOcrVisionProviderKind Kind,
    NodalOsOcrVisionProviderStatus Status,
    NodalOsOcrVisionProviderCapability Capabilities,
    NodalOsOcrVisionProviderPolicy Policy,
    NodalOsOcrVisionProviderCostProfile CostProfile,
    NodalOsOcrVisionProviderPerformanceProfile PerformanceProfile,
    NodalOsOcrVisionProviderPrivacyProfile PrivacyProfile,
    bool StoresSecrets,
    bool CallsRealApi,
    bool GrantsAuthority,
    bool Redacted)
{
    public bool RequiresExternalDataTransfer => Policy.ExternalDataTransfer || PrivacyProfile.ExternalDataTransfer;
    public bool IsCloud => RequiresExternalDataTransfer;
}

public sealed record NodalOsOcrVisionProviderRegistry(
    IReadOnlyList<NodalOsOcrVisionProviderConfiguration> Providers,
    bool StoresSecrets,
    bool CallsRealApi,
    bool GrantsAuthority,
    bool Redacted);

public enum NodalOsOcrEngineHint
{
    PaddleOcr,
    Tesseract,
    EasyOcr,
    DisabledStub
}

public enum NodalOsOcrLanguage
{
    Unknown,
    English,
    Spanish,
    Portuguese,
    MultiLanguage
}

public enum NodalOsOcrPurpose
{
    EvidenceDebug,
    TextExtraction,
    UiUnderstanding,
    AccessibilityFallback
}

public enum NodalOsOcrAuthorityFlag
{
    NoAuthority,
    InformationalOnly,
    CoreDecisionRequired
}

public enum NodalOsLocalOcrStatus
{
    Disabled,
    ReadyStub,
    CompletedStub,
    BlockedByRedaction,
    BlockedFullScreen,
    BlockedUnredactedCrop,
    LowConfidenceNeedsHuman,
    Error
}

public sealed record NodalOsOcrBoundingBox(int X, int Y, int Width, int Height);

public readonly record struct NodalOsOcrConfidence(double Value);

public sealed record NodalOsOcrTextBlock(
    string BlockId,
    string RedactedText,
    NodalOsOcrBoundingBox Bounds,
    NodalOsOcrConfidence Confidence,
    NodalOsOcrLanguage Language,
    bool Redacted);

public sealed record NodalOsOcrRedactionSummary(
    NodalOsGroundingRedactionStatus GroundingRedactionStatus,
    bool ScreenshotSafe,
    bool CropRedacted,
    bool ContainsSensitive,
    string Summary);

public enum NodalOsImageRedactionFindingKind
{
    PasswordField,
    CredentialLikeText,
    TokenLikeText,
    JwtLikeText,
    CookieLikeText,
    ApiKeyLikeText,
    EmailLikeText,
    PhoneLikeText,
    CreditCardLikeText,
    DocumentIdLikeText,
    SensitiveKeyword,
    UnknownSensitivePattern,
    LowConfidence,
    RedactionEngineUncertain
}

public enum NodalOsImageRedactionDecision
{
    Redacted,
    BlockedSensitive,
    RedactionFailed,
    CleanNoRedactionRequired
}

public sealed record NodalOsImageRedactionFinding(
    NodalOsImageRedactionFindingKind Kind,
    string RedactedPreview,
    NodalOsOcrBoundingBox? Bounds,
    double Confidence,
    bool BlocksOcr);

public sealed record NodalOsImageRedactionPolicy(
    bool AllowPersistence,
    bool AllowFullScreen,
    bool BlockSensitiveByDefault,
    bool FailClosedOnUncertainty,
    bool PersistRawImage,
    bool NoAuthority);

public sealed record NodalOsImageRedactionEvidence(
    string EvidenceId,
    IReadOnlyList<NodalOsGroundingEvidenceRef> EvidenceRefs,
    string RedactionSummary,
    string ModelOnlyHash,
    bool OriginalRawPersisted,
    bool Redacted);

public sealed record NodalOsImageCropRedactionRequest(
    string RequestId,
    NodalOsGroundingSnapshotId? GroundingSnapshotId,
    string? CropRef,
    byte[] SyntheticImageBytes,
    NodalOsOcrBoundingBox Bounds,
    string Source,
    NodalOsOcrVisionSensitivity Sensitivity,
    NodalOsOcrPurpose IntendedPurpose,
    bool AllowPersistence,
    bool AllowFullScreen,
    NodalOsImageRedactionPolicy Policy);

public sealed record NodalOsImageCropRedactionResult(
    string ResultId,
    NodalOsImageRedactionDecision Decision,
    bool CropRedacted,
    bool SafeForOcr,
    bool SafeForPersistence,
    IReadOnlyList<NodalOsImageRedactionFinding> Findings,
    string RedactedBytesRef,
    bool OriginalRawPersisted,
    NodalOsImageRedactionEvidence Evidence,
    NodalOsOcrConfidence Confidence,
    bool NoAuthority);

public enum NodalOsOcrVisionSensitivity
{
    None,
    Low,
    Medium,
    High,
    SensitiveSurface,
    Credentials,
    PersonalData,
    Payment,
    BlockedSensitive
}

public sealed record NodalOsLocalOcrRequest(
    string RequestId,
    NodalOsGroundingSnapshotId GroundingSnapshotId,
    string CropRef,
    string? ScreenshotRef,
    NodalOsOcrBoundingBox Region,
    IReadOnlyList<NodalOsOcrLanguage> LanguageHints,
    NodalOsOcrVisionDocumentType DocumentTypeHint,
    NodalOsOcrVisionSensitivity Sensitivity,
    NodalOsGroundingRedactionStatus RedactionStatus,
    decimal MaxCost,
    double MinConfidence,
    bool AllowCloudFallback,
    bool FullScreen,
    bool CropRedacted,
    NodalOsOcrPurpose Purpose,
    bool Redacted)
{
    public NodalOsImageCropRedactionResult? RedactionResult { get; init; }
}

public sealed record NodalOsLocalOcrResult(
    string ResultId,
    NodalOsOcrVisionProviderId ProviderId,
    NodalOsOcrEngineHint Engine,
    NodalOsLocalOcrStatus Status,
    IReadOnlyList<NodalOsOcrTextBlock> TextBlocks,
    NodalOsOcrConfidence Confidence,
    NodalOsOcrLanguage LanguageDetected,
    IReadOnlyList<string> Warnings,
    IReadOnlyList<NodalOsGroundingEvidenceRef> EvidenceRefs,
    NodalOsOcrRedactionSummary RedactionSummary,
    NodalOsOcrAuthorityFlag AuthorityFlag,
    bool RequiresHumanReview,
    bool CanApproveAction,
    bool CanClick,
    bool CanSubmit,
    bool CallsExternalApi,
    bool Redacted);

public enum NodalOsOcrVisionDocumentType
{
    Unknown,
    UiCrop,
    Screenshot,
    Table,
    Form,
    Invoice,
    Receipt,
    IdentityDocument,
    Handwriting,
    MixedPrintedHandwriting,
    Pdf
}

public enum NodalOsOcrVisionCaseClassification
{
    SimpleText,
    SimpleUiCrop,
    ScreenshotUi,
    ComplexLayout,
    Table,
    Form,
    Invoice,
    Receipt,
    IdentityDocument,
    Handwriting,
    MixedPrintedHandwriting,
    LowQualityImage,
    Blurred,
    Skewed,
    SensitiveSurface,
    Unknown
}

public enum NodalOsOcrVisionComplexity
{
    Low,
    Medium,
    High,
    VeryHigh,
    Unknown
}

public enum NodalOsOcrVisionQuality
{
    Good,
    Medium,
    Poor,
    RedactionFailed,
    SensitiveBlocked
}

public enum NodalOsOcrVisionRoutingReason
{
    NoOcrNeeded,
    LocalPaddlePreferred,
    TesseractFallback,
    CloudCandidateDisabled,
    VlmCandidateDisabled,
    SensitiveCloudBlocked,
    RedactionFailedBlocked,
    LowConfidenceNeedsHuman,
    BudgetExceeded,
    NoProviderAllowed,
    AskHuman,
    ProviderDisabled
}

public enum NodalOsOcrVisionRoutingStatus
{
    NoOcrNeeded,
    ProviderSelected,
    LocalFallbackSelected,
    AskHuman,
    Blocked,
    BlockedByBudget,
    BlockedByPrivacy,
    CloudDisabled,
    NeedsHumanReview
}

public sealed record NodalOsOcrVisionBudgetDecision(
    bool Allowed,
    decimal EstimatedCost,
    decimal MaxAllowedCost,
    string Reason);

public sealed record NodalOsOcrVisionFallbackPlan(
    IReadOnlyList<NodalOsOcrVisionProviderId> Providers,
    bool HumanReviewFallback,
    string Summary);

public sealed record NodalOsOcrVisionRoutingRequest(
    string RequestId,
    bool DomCdpUiaSufficient,
    bool ScreenshotHashDiffOnly,
    NodalOsBrowserGroundingSnapshot? GroundingSnapshot,
    string? CropRef,
    bool CropRedacted,
    bool FullScreen,
    NodalOsOcrVisionCaseClassification CaseClassification,
    NodalOsOcrVisionDocumentType DocumentType,
    NodalOsOcrVisionComplexity Complexity,
    NodalOsOcrVisionQuality Quality,
    NodalOsOcrVisionSensitivity Sensitivity,
    decimal MaxEstimatedCost,
    double RequiredConfidence,
    bool AllowsCloud,
    bool Redacted)
{
    public NodalOsImageCropRedactionResult? RedactionResult { get; init; }
}

public sealed record NodalOsOcrVisionRoutingDecision(
    string DecisionId,
    NodalOsOcrVisionRoutingStatus Status,
    NodalOsOcrVisionProviderId? SelectedProviderId,
    NodalOsOcrVisionFallbackPlan FallbackPlan,
    NodalOsOcrVisionRoutingReason Reason,
    NodalOsOcrVisionBudgetDecision BudgetDecision,
    decimal EstimatedCost,
    string ExpectedConfidenceBand,
    NodalOsGroundingRisk Risk,
    string PrivacyNote,
    bool RequiresHumanApproval,
    bool NoAuthority,
    string BlockedReason,
    IReadOnlyList<NodalOsGroundingEvidenceRef> EvidenceRefs,
    bool CallsRealSaas,
    bool Redacted);

public enum NodalOsOcrVisionApiKeyState
{
    Missing,
    PlaceholderConfigured,
    SecretVaultRequired,
    Disabled
}

public enum NodalOsOcrVisionAdminDecisionKind
{
    Viewed,
    ModelOnlyUpdated,
    Paused,
    Resumed,
    Reordered,
    BlockedRealApiKey,
    BlockedExecutableProvider,
    NoAuthority
}

public sealed record NodalOsOcrVisionProviderBudgetSettings(
    decimal DailyBudget,
    decimal MonthlyBudget,
    decimal MaxCostPerPage,
    decimal MaxCostPerImage,
    string Currency,
    bool ModelOnly);

public sealed record NodalOsOcrVisionProviderAdminView(
    NodalOsOcrVisionProviderId ProviderId,
    string DisplayName,
    NodalOsOcrVisionProviderKind Kind,
    NodalOsOcrVisionProviderStatus Status,
    bool Enabled,
    bool Paused,
    bool DisabledByDefault,
    NodalOsOcrVisionProviderCapability Capabilities,
    NodalOsOcrVisionProviderCostProfile CostProfile,
    NodalOsOcrVisionProviderPerformanceProfile PerformanceProfile,
    NodalOsOcrVisionProviderPrivacyProfile PrivacyProfile,
    bool RequiresApiKey,
    NodalOsOcrVisionApiKeyState ApiKeyState,
    bool ExternalDataTransfer,
    bool AllowedForSensitive,
    bool AllowedForCrops,
    bool AllowedForFullScreen,
    int Priority,
    IReadOnlyList<NodalOsOcrVisionProviderId> FallbackOrder,
    NodalOsOcrVisionProviderBudgetSettings BudgetSettings,
    double MinConfidence,
    int MaxLatencyMs,
    IReadOnlyList<string> BlockedReasons,
    string LastEvaluationSummary,
    bool Executable,
    bool GrantsAuthority,
    bool Redacted);

public sealed record NodalOsOcrVisionProviderRoutingRuleView(
    string RuleId,
    string CaseSummary,
    NodalOsOcrVisionRoutingReason Reason,
    IReadOnlyList<NodalOsOcrVisionProviderId> PreferredProviders,
    IReadOnlyList<NodalOsOcrVisionProviderId> FallbackProviders,
    bool RequiresHumanReview,
    bool NoAuthority);

public sealed record NodalOsOcrVisionAdminSettings(
    string SettingsId,
    IReadOnlyList<NodalOsOcrVisionProviderAdminView> Providers,
    IReadOnlyList<NodalOsOcrVisionProviderRoutingRuleView> RoutingRules,
    bool StoresApiKeys,
    bool CallsRealApi,
    bool ProviderExecutable,
    bool GrantsAuthority,
    bool Redacted);

public sealed record NodalOsOcrVisionProviderToggleRequest(
    NodalOsOcrVisionProviderId ProviderId,
    bool Enabled,
    NodalOsOcrVisionApiKeyState ApiKeyState,
    bool ModelOnly,
    string Reason);

public sealed record NodalOsOcrVisionProviderPriorityUpdate(
    NodalOsOcrVisionProviderId ProviderId,
    int Priority,
    IReadOnlyList<NodalOsOcrVisionProviderId> FallbackOrder,
    bool ModelOnly);

public sealed record NodalOsOcrVisionProviderPauseRequest(
    NodalOsOcrVisionProviderId ProviderId,
    bool Pause,
    string Reason,
    bool ModelOnly);

public sealed record NodalOsOcrVisionAdminDecision(
    string DecisionId,
    NodalOsOcrVisionAdminDecisionKind Decision,
    NodalOsOcrVisionProviderId ProviderId,
    string Reason,
    bool StoresApiKeys,
    bool CallsRealApi,
    bool ProviderExecutable,
    bool GrantsAuthority,
    bool Redacted);

public enum NodalOsSaasOcrConnectorBlockReason
{
    CannotRunWithoutOptIn,
    CannotRunWithoutSecretVault,
    CannotRunOnSensitiveByDefault,
    CannotRunIfBudgetMissing,
    CannotRunIfRedactionFailed,
    CannotRunInProductionCurrentPhase,
    DisabledByDefault
}

public sealed record NodalOsSaasOcrProviderExecutionProbe(
    NodalOsOcrVisionProviderId ProviderId,
    NodalOsOcrVisionProviderKind Kind,
    bool WouldCallHttp,
    bool StoresSecret,
    bool RefusesExecution,
    IReadOnlyList<NodalOsSaasOcrConnectorBlockReason> BlockReasons,
    bool NoAuthority,
    bool Redacted);

public sealed record NodalOsOcrVisionExpectedOutput(
    NodalOsOcrVisionRoutingStatus ExpectedStatus,
    NodalOsOcrVisionRoutingReason ExpectedReason,
    string? ExpectedProviderId,
    bool RequiresHumanEscalation,
    bool NoAuthority);

public sealed record NodalOsOcrVisionEvaluationMetric(
    string Name,
    string Value,
    bool Passed);

public sealed record NodalOsOcrVisionEvaluationFixture(
    string FixtureId,
    string Description,
    NodalOsOcrVisionCaseClassification CaseClassification,
    NodalOsOcrVisionComplexity Complexity,
    NodalOsOcrVisionQuality Quality,
    NodalOsOcrVisionSensitivity Sensitivity,
    bool DomCdpUiaSufficient,
    bool ScreenshotHashDiffOnly,
    bool CropRedacted,
    bool FullScreen,
    decimal Budget,
    NodalOsGroundingRedactionStatus RedactionStatus,
    NodalOsOcrVisionExpectedOutput ExpectedOutput,
    bool Synthetic,
    bool Redacted);

public sealed record NodalOsOcrVisionEvaluationCase(
    string CaseId,
    NodalOsOcrVisionEvaluationFixture Fixture,
    NodalOsOcrVisionRoutingRequest RoutingRequest);

public sealed record NodalOsOcrVisionProviderScore(
    NodalOsOcrVisionProviderId ProviderId,
    string ExpectedConfidenceBand,
    string ExpectedLatencyBand,
    decimal EstimatedCost,
    NodalOsGroundingRisk PrivacyRisk,
    bool Selected,
    bool Fallback,
    bool NoAuthority);

public sealed record NodalOsOcrVisionEvaluationResult(
    string ResultId,
    NodalOsOcrVisionEvaluationFixture Fixture,
    NodalOsOcrVisionRoutingDecision Decision,
    IReadOnlyList<NodalOsOcrVisionProviderScore> ProviderScores,
    IReadOnlyList<NodalOsOcrVisionEvaluationMetric> Metrics,
    bool Passed,
    bool CallsRealOcr,
    bool CallsRealSaas,
    bool NoAuthority,
    bool Redacted);

public sealed record NodalOsOcrVisionBenchmarkReport(
    string ReportId,
    DateTimeOffset CreatedAtUtc,
    IReadOnlyList<NodalOsOcrVisionEvaluationResult> Results,
    int TotalCases,
    int PassedCases,
    bool CallsRealOcr,
    bool CallsRealSaas,
    bool NoAuthority,
    bool Redacted);

public enum NodalOsOcrVisionActivationState
{
    ModelOnly,
    ShadowEvaluation,
    LocalWorkerAvailable,
    LocalWorkerEnabledForSynthetic,
    LocalWorkerEnabledForRedactedCrops,
    SaasProviderConfigured,
    SaasProviderShadowOnly,
    SaasProviderEnabledForApprovedDocs,
    BlockedByPolicy,
    BlockedByPrivacy,
    BlockedByBudget,
    BlockedByMissingAudit
}

public enum NodalOsLocalOcrWorkerHealthStatus
{
    NotInstalled,
    InstalledButDisabled,
    Available,
    Degraded,
    Error,
    VersionMismatch,
    BlockedByPolicy
}

[Flags]
public enum NodalOsLocalOcrWorkerCapability
{
    None = 0,
    RedactedCrops = 1 << 0,
    SyntheticFixtures = 1 << 1,
    PrintedText = 1 << 2,
    BoundingBoxes = 1 << 3,
    MultiLanguage = 1 << 4,
    JsonContract = 1 << 5
}

public enum NodalOsLocalOcrWorkerError
{
    None,
    WorkerNotInstalled,
    WorkerDisabled,
    RedactionFailed,
    FullScreenBlocked,
    SensitiveSurfaceBlocked,
    ImageTooLarge,
    PageLimitExceeded,
    LatencyLimitExceeded,
    EngineNotAllowed,
    PolicyBlocked,
    VersionMismatch
}

public sealed record NodalOsLocalOcrWorkerRuntimeProfile(
    string RuntimeKind,
    string Transport,
    string Version,
    int MaxLatencyMs,
    int MaxMemoryMb,
    bool PythonCoupledToCore,
    bool InvokesExternalProcess);

public sealed record NodalOsLocalOcrWorkerInvocationPolicy(
    bool OnlyRedactedCropsByDefault,
    bool AllowFullScreen,
    bool AllowSensitiveSurfaces,
    int MaxImageWidth,
    int MaxImageHeight,
    int MaxPages,
    int MaxLatencyMs,
    int MaxMemoryMb,
    IReadOnlyList<NodalOsOcrEngineHint> AllowedEngines,
    bool PersistRawImages,
    bool RedactedEvidenceOnly,
    bool NoAuthority);

public sealed record NodalOsLocalOcrWorkerHealth(
    NodalOsLocalOcrWorkerHealthStatus Status,
    bool Installed,
    bool Enabled,
    bool Available,
    string Reason,
    bool InvokesExternalProcess,
    bool StoresSecrets,
    bool NoAuthority);

public sealed record NodalOsLocalOcrWorkerContract(
    string WorkerId,
    NodalOsOcrVisionActivationState ActivationState,
    NodalOsLocalOcrWorkerHealth Health,
    NodalOsLocalOcrWorkerCapability Capabilities,
    NodalOsLocalOcrWorkerRuntimeProfile RuntimeProfile,
    NodalOsLocalOcrWorkerInvocationPolicy InvocationPolicy,
    bool JsonBoundary,
    bool CorePythonDecoupled,
    bool CallsRealOcr,
    bool CallsRealSaas,
    bool NoAuthority);

public sealed record NodalOsLocalOcrWorkerRequest(
    string RequestId,
    NodalOsGroundingSnapshotId GroundingSnapshotId,
    string CropRef,
    string? ScreenshotRef,
    NodalOsOcrBoundingBox Region,
    NodalOsOcrEngineHint Engine,
    NodalOsOcrVisionSensitivity Sensitivity,
    NodalOsGroundingRedactionStatus RedactionStatus,
    bool Synthetic,
    bool FullScreen,
    bool CropRedacted,
    int ImageWidth,
    int ImageHeight,
    int Pages,
    int MaxLatencyMs,
    bool PersistRawImage,
    bool Redacted)
{
    public NodalOsImageCropRedactionResult? RedactionResult { get; init; }
}

public sealed record NodalOsLocalOcrWorkerResponse(
    string ResponseId,
    NodalOsLocalOcrWorkerHealthStatus WorkerStatus,
    NodalOsLocalOcrWorkerError Error,
    IReadOnlyList<NodalOsOcrTextBlock> TextBlocks,
    NodalOsOcrConfidence Confidence,
    IReadOnlyList<string> Warnings,
    IReadOnlyList<NodalOsGroundingEvidenceRef> EvidenceRefs,
    bool RequiresHumanReview,
    bool InvokedExternalProcess,
    bool CallsRealOcr,
    bool CallsRealSaas,
    bool CanApproveAction,
    bool CanClick,
    bool CanSubmit,
    bool NoAuthority,
    bool Redacted);

public enum NodalOsOcrActivationScopeKind
{
    SyntheticOnly,
    RedactedCropShadow,
    ControlledLocalUse,
    SaasApprovedDocs,
    BlockedCurrentPhase
}

public enum NodalOsOcrActivationDecisionKind
{
    BlockedByDefault,
    BlockedByMissingOptIn,
    BlockedByMissingWorker,
    BlockedByRedaction,
    BlockedBySensitivePolicy,
    BlockedByBudget,
    BlockedByPrivacy,
    BlockedByMissingAudit,
    BlockedByNoAuthorityViolation,
    ReadyForSyntheticOnly,
    ReadyForRedactedCropShadow,
    ReadyForControlledLocalUse
}

public sealed record NodalOsOcrActivationScope(
    NodalOsOcrActivationScopeKind Kind,
    bool LocalOnly,
    bool AllowsSaas,
    bool AllowsFullScreen,
    bool AllowsSensitive,
    string Description);

public sealed record NodalOsOcrActivationAuditEvidence(
    bool Present,
    IReadOnlyList<NodalOsGroundingEvidenceRef> EvidenceRefs,
    string Summary,
    bool Redacted);

public sealed record NodalOsOcrActivationRequirement(
    string RequirementId,
    bool Satisfied,
    string Evidence,
    string MissingReason);

public sealed record NodalOsOcrActivationReadiness(
    NodalOsOcrVisionProviderId ProviderId,
    NodalOsOcrVisionProviderKind ProviderKind,
    NodalOsOcrActivationScope Scope,
    bool ProviderExplicitlyEnabled,
    bool LocalWorkerInstalled,
    bool LocalWorkerAvailable,
    bool OptIn,
    bool RedactionGatePassed,
    bool SensitivePolicyPassed,
    bool FullScreenDisabledOrApproved,
    bool BudgetConfigured,
    bool PrivacyProfileAccepted,
    NodalOsOcrActivationAuditEvidence AuditEvidence,
    bool NoAuthorityConfirmed,
    bool HumanEscalationPolicyConfigured,
    bool EvaluationHarnessPassed,
    bool RollbackPauseConfigured,
    bool CurrentPhaseAllowsSaasReal,
    bool Redacted)
{
    public bool RequiresExternalDataTransfer { get; init; }
}

public sealed record NodalOsOcrActivationDecision(
    string DecisionId,
    NodalOsOcrActivationDecisionKind Decision,
    NodalOsOcrVisionActivationState ActivationState,
    IReadOnlyList<NodalOsOcrActivationRequirement> Requirements,
    string Reason,
    bool RealOcrEnabled,
    bool RealSaasEnabled,
    bool NoAuthority,
    bool RequiresHumanReview,
    bool Redacted);
