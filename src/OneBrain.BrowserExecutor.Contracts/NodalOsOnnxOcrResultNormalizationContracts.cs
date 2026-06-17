namespace OneBrain.BrowserExecutor.Contracts;

// M204 — ONNX OCR result normalization / evidence / timeline compatibility contracts.
// Converts raw ONNX pipeline output into redacted, no-authority evidence compatible with the timeline.

public enum NodalOsOnnxOcrNormalizedStatus
{
    Completed,
    NoTextDetected,
    LowConfidence,
    RequiresHumanReview,
    BlockedByPolicy,
    ModelMissing,
    ModelUnverified,
    PreProcessingFailed,
    DetectionFailed,
    RecognitionFailed,
    Failed
}

public sealed record NodalOsOnnxOcrNormalizedTextBlock(
    string BlockId,
    string RedactedText,
    NodalOsOcrBoundingBox Bounds,
    NodalOsOcrConfidence Confidence,
    NodalOsOcrLanguage Language,
    bool LowConfidence,
    bool Redacted);

public sealed record NodalOsOnnxOcrNormalizedResult(
    string ResultId,
    NodalOsOcrVisionProviderId ProviderId,
    string ModelSetId,
    string DetectionModelVersion,
    string RecognitionModelVersion,
    NodalOsOnnxOcrNormalizedStatus Status,
    IReadOnlyList<NodalOsOnnxOcrNormalizedTextBlock> TextBlocks,
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
    bool CallsSaas,
    bool ProductionEnabled,
    bool OriginalRawPersisted,
    bool NoAuthority,
    bool Redacted);

public sealed record NodalOsOnnxOcrTimelineEvidenceCard(
    string CardId,
    string SourceResultId,
    string Summary,
    IReadOnlyList<NodalOsGroundingEvidenceRef> EvidenceRefs,
    DateTimeOffset CreatedAtUtc,
    bool CallsSaas,
    bool ProductionEnabled,
    bool OriginalRawPersisted,
    bool NoAuthority,
    bool Redacted);

public sealed record NodalOsOnnxOcrHumanReviewRule(
    string RuleId,
    string Name,
    bool Triggered,
    string Reason);

public sealed record NodalOsOnnxOcrHumanReviewDecision(
    string DecisionId,
    bool RequiresHumanReview,
    IReadOnlyList<NodalOsOnnxOcrHumanReviewRule> Rules,
    string Reason);

public sealed record NodalOsOnnxOcrEvidenceSummary(
    string EvidenceId,
    string RedactedCropHash,
    IReadOnlyList<string> ModelManifestRefs,
    long? TotalInferenceTimeMs,
    int TextBlockCount,
    double AverageConfidence,
    bool OriginalRawPersisted,
    bool CallsSaas,
    bool ProductionEnabled,
    bool NoAuthority,
    string Summary);
