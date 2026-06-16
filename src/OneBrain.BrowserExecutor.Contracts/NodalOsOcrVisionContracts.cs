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
    bool Redacted);

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
    bool Redacted);

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
    bool Redacted);

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
