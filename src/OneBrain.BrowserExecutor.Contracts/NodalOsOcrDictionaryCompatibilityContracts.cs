namespace OneBrain.BrowserExecutor.Contracts;

// M221 - OCR dictionary / CTC compatibility contracts.
// Recognition must not claim positive text unless the model output class count and dictionary manifest match.

public enum NodalOsOcrDictionaryCompatibilityStatus
{
    Compatible,
    MissingDictionary,
    ClassCountMismatch,
    DictionaryUnverified,
    HashMismatch,
    SourceNotSelected,
    UnexpectedCommittedDictionary,
    Unknown
}

public enum NodalOsOcrDictionaryRole
{
    RecognitionCtcCharset
}

public enum NodalOsOcrDictionaryAvailabilityStatus
{
    PresentAndVerified,
    Missing,
    SourceNotSelected,
    HashMismatch,
    SizeMismatch,
    CountMismatch,
    Unverified,
    IgnoredCorrectly,
    UnexpectedCommittedDictionary,
    Unknown
}

public enum NodalOsOcrDictionaryBlankTokenPolicy
{
    BlankAppendedAtEnd,
    BlankAtIndexZero,
    Unknown
}

public enum NodalOsOcrDictionaryReadinessDecision
{
    ReadyForDictionarySourceSelection,
    ReadyForDictionaryDownload,
    ReadyForManualDictionarySourceApproval,
    ReadyForSyntheticTextDecodeFixtures,
    ReadyForMoreSyntheticFixtures,
    BlockedByDictionarySource,
    BlockedByDictionaryClassCountMismatch,
    BlockedByDictionaryCountMismatch,
    BlockedByDictionaryHashMismatch,
    BlockedByDictionaryHashPinning,
    NotReady
}

public enum NodalOsOcrDictionarySourceAuditStatus
{
    SourceSelected,
    SourceCandidateFoundNeedsHash,
    NoApprovedSourceFound,
    SourceRejectedCountMismatch,
    SourceRejectedUnofficial,
    SourceRejectedUnpinnable
}

public enum NodalOsRecognizerTokenPolicy
{
    DictionaryCharsOnly,
    DictionaryPlusBlankAtEnd,
    DictionaryPlusBlankAtStart,
    DictionaryPlusBlankAndUnknown,
    DictionaryPlusBlankAndSpace,
    DictionaryPlusBlankAndPadding,
    DictionaryPlusTwoSpecialTokens,
    ModelDictionaryMismatch,
    Unknown
}

public enum NodalOsRecognizerSpecialTokenPolicy
{
    None,
    BlankAtStart,
    BlankAtEnd,
    BlankAndUnknown,
    BlankAndPadding,
    BlankAndSpace,
    TwoSpecialTokens,
    Unknown
}

public enum NodalOsRecognizerClassSemanticsDecision
{
    ReadyForSyntheticTextDecodeFixtures,
    ReadyForApprovedTokenPolicyDecode,
    ReadyForRecognizerModelDictionarySourceReview,
    BlockedByTokenPolicyUnknown,
    BlockedByDictionaryModelMismatch,
    NotReady
}

public enum NodalOsCtcDecodePolicyExperimentStatus
{
    ApprovedDecodeAllowed,
    HypothesisOnlyDecodeBlocked,
    ClassCountMismatch,
    UnsupportedBlankIndex,
    LowConfidenceRequiresHumanReview,
    EmptyDecode,
    NotRun
}

public sealed record NodalOsOcrDictionaryManifest(
    string DictionaryId,
    string Language,
    int CharacterCount,
    int BlankTokenCount,
    string SourceRef,
    string? ExpectedSha256,
    bool Verified,
    bool NoAuthority);

public sealed record NodalOsOcrDictionaryManifestEntry(
    string DictionaryId,
    NodalOsOcrDictionaryRole Role,
    string ExpectedFileName,
    string ExpectedRelativePath,
    int ExpectedCharsetCount,
    int ExpectedRecognizerClassCount,
    NodalOsOcrDictionaryBlankTokenPolicy BlankTokenPolicy,
    int CtcBlankIndex,
    string NewlineHandling,
    string? SourceUrl,
    string SourceRef,
    string? ExpectedSha256,
    long? ExpectedSizeBytes,
    NodalOsOcrDictionaryAvailabilityStatus AcquisitionStatus,
    bool Gitignored,
    bool Committed,
    bool NoAuthority);

public sealed record NodalOsOcrDictionaryAcquisitionPlan(
    string PlanId,
    NodalOsOcrDictionaryManifestEntry ManifestEntry,
    bool SourceApproved,
    bool DownloadAllowed,
    IReadOnlyList<string> PlannedScripts,
    IReadOnlyList<string> Commands,
    string Decision,
    bool NoSaas,
    bool NoAuthority,
    string Reason);

public sealed record NodalOsOcrDictionarySourceCandidate(
    string SourceId,
    string UrlOrRef,
    string Provider,
    string Repository,
    string License,
    int ExpectedCharacterCount,
    bool BlankIncluded,
    bool Official,
    bool Verifiable,
    string? Sha256,
    long? SizeBytes,
    bool CompatibleWithRecognizerClassCount,
    string Risk);

public sealed record NodalOsOcrDictionarySourceSelectionReport(
    string ReportId,
    NodalOsOcrDictionarySourceAuditStatus Status,
    IReadOnlyList<NodalOsOcrDictionarySourceCandidate> Candidates,
    NodalOsOcrDictionarySourceCandidate? SelectedSource,
    bool HashPinned,
    bool SizePinned,
    bool NoDecodeAttempted,
    bool NoAuthority,
    string Reason);

public sealed record NodalOsOcrDictionaryAcquisitionGateReport(
    string GateId,
    NodalOsOcrDictionaryReadinessDecision Decision,
    NodalOsOcrDictionarySourceSelectionReport SourceSelection,
    bool SourcePinned,
    bool HashPinned,
    bool SizePinned,
    bool CharacterCountCompatible,
    bool ScriptsActive,
    bool DownloadExecuted,
    bool RollbackTouchesOnnxModels,
    bool ProductiveOcrBlocked,
    bool ShadowModeBlocked,
    bool NoAuthority,
    string Reason);

public sealed record NodalOsRecognizerClassMapping(
    string MappingId,
    NodalOsRecognizerTokenPolicy TokenPolicy,
    int DictionaryTokenCount,
    int ModelClassCount,
    int ExpectedClassCount,
    int? BlankIndex,
    int? UnknownIndex,
    int? PaddingIndex,
    int? SpaceIndex,
    bool Compatible,
    bool DecodeAllowed,
    string EvidenceSource,
    string RiskLevel,
    string Reason);

public sealed record NodalOsRecognizerClassSemantics(
    string SemanticsId,
    int ModelClassCount,
    int DictionaryTokenCount,
    IReadOnlyList<NodalOsRecognizerClassMapping> CandidateMappings,
    NodalOsRecognizerClassMapping? SelectedMapping,
    NodalOsRecognizerClassSemanticsDecision Decision,
    bool DecodeAllowed,
    bool NoAuthority,
    string Reason);

public sealed record NodalOsCtcDecodePolicyCandidate(
    string CandidateId,
    NodalOsRecognizerTokenPolicy TokenPolicy,
    NodalOsRecognizerSpecialTokenPolicy SpecialTokenPolicy,
    int DictionaryTokenCount,
    int ModelClassCount,
    int? BlankIndex,
    int? ExtraTokenIndex,
    bool EvidenceApproved,
    bool HypothesisOnly,
    string EvidenceSource,
    string Reason);

public sealed record NodalOsCtcDecodePolicyExperimentResult(
    string ResultId,
    NodalOsCtcDecodePolicyCandidate Candidate,
    NodalOsCtcDecodePolicyExperimentStatus Status,
    bool DecodeAttempted,
    bool DecodeAllowed,
    string? DecodedText,
    double? Confidence,
    bool RequiresHumanReview,
    bool NoRawPersistence,
    bool NoSensitive,
    bool NoAuthority,
    string Reason);

public sealed record NodalOsRecognizerTokenPolicyDecisionReport(
    string ReportId,
    NodalOsRecognizerClassSemantics Semantics,
    IReadOnlyList<NodalOsCtcDecodePolicyExperimentResult> ExperimentResults,
    NodalOsRecognizerClassSemanticsDecision Decision,
    bool ProductiveOcrBlocked,
    bool ShadowModeBlocked,
    bool NoRawPersistence,
    bool NoFullScreen,
    bool NoSensitive,
    bool NoSaas,
    bool NoAuthority,
    string Reason);

public sealed record NodalOsOcrCtcDecoderCompatibility(
    string CompatibilityId,
    int RecognizerOutputClassCount,
    int DictionaryClassCountIncludingBlank,
    bool BatchTimeClassAxesInferred,
    string InferredAxes,
    NodalOsOcrDictionaryCompatibilityStatus Status,
    bool DecodeAllowed,
    bool NoAuthority,
    string Reason);

public sealed record NodalOsOcrDictionaryCompatibilityResult(
    string ResultId,
    NodalOsOcrDictionaryCompatibilityStatus Status,
    NodalOsOcrDictionaryManifest? Dictionary,
    NodalOsOcrCtcDecoderCompatibility CtcDecoderCompatibility,
    bool RecognitionSuccessAllowed,
    bool RequiresHumanReview,
    bool NoAuthority,
    string Reason);

public sealed record NodalOsOcrDictionaryReadinessReport(
    string ReportId,
    NodalOsOcrDictionaryReadinessDecision Decision,
    NodalOsOcrDictionaryManifestEntry ManifestEntry,
    NodalOsOcrDictionaryCompatibilityResult Compatibility,
    NodalOsOcrDictionaryAcquisitionPlan AcquisitionPlan,
    bool DictionaryPresent,
    bool HashVerified,
    bool DecodeAttempted,
    bool ProductiveOcrBlocked,
    bool ShadowModeBlocked,
    bool NoRawPersistence,
    bool NoFullScreen,
    bool NoSensitive,
    bool NoSaas,
    bool NoAuthority,
    string Reason);

public sealed record NodalOsGuardedSyntheticTextOcrRetryReadinessInput(
    bool DetectionModelVerified,
    bool RecognitionModelVerified,
    bool ClassificationModelVerified,
    bool OnnxModelsTracked,
    IReadOnlyList<NodalOsGuardedSyntheticTextOcrProbeResult> ProbeResults,
    NodalOsDetectorCompatibilityDiagnosis Detection,
    NodalOsRecognizerCompatibilityDiagnosis Recognition,
    NodalOsOcrDictionaryCompatibilityResult DictionaryCompatibility,
    bool GuardExists,
    bool ParentSurvivedCrash,
    bool ChildCleanupWorks,
    bool TempCleanupWorks,
    bool NoFullScreen,
    bool NoSensitive);
