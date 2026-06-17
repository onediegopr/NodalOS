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

public enum NodalOsPaddleOcrDecodePolicyExperimentStatus
{
    Approved,
    HypothesisOnly,
    Rejected,
    DecodeBlocked,
    DecodeAttempted,
    DecodeSucceeded,
    DecodeEmpty,
    LowConfidence
}

public enum NodalOsPaddleOcrExtraClassDecision
{
    ReadyForApprovedDecodePolicy,
    ReadyForSyntheticTextDecodeFixtures,
    ReadyForManualDecodePolicyApproval,
    ReadyForRecognizerModelReplacement,
    BlockedByExtraClassSemantics,
    BlockedByDecodePolicyRisk,
    NotReady
}

public enum NodalOsOcrRecognizerReplacementDecision
{
    ReadyForClaudeExtraClassAudit,
    ReadyForManualDecodePolicyApproval,
    ReadyForRecognizerModelReplacementSearch,
    ReadyForAlternativeLocalOcrFamilyReview,
    BlockedByExtraClassSemantics,
    NotReady
}

public enum NodalOsOcrRecognizerReplacementStrategyStatus
{
    Recommended,
    ViableNeedsApproval,
    ViableNeedsResearch,
    RejectedHighRisk,
    RejectedSaas,
    Blocked
}

public enum NodalOsDictionaryParserPolicy
{
    PreserveRawLineSegments,
    PreserveSpaceDropTerminalEmpty,
    TrimAndDropEmpty,
    PaddleOcrReadLinesStripNewline,
    Unknown
}

public enum NodalOsRecognizerDictionaryPairDecision
{
    ReadyForDictionaryPinning,
    ReadyForManualSourceReview,
    ReadyForRecognizerModelDictionaryPairReplacement,
    BlockedByModelDictionaryPairMismatch,
    BlockedByDictionarySourceAmbiguity,
    NotReady
}

public enum NodalOsRecognizerDictionaryPairCandidateDecision
{
    CandidateAcceptedForAcquisition,
    CandidateNeedsManualReview,
    RejectedNoExplicitDictionary,
    RejectedCountMismatch,
    RejectedNoOnnx,
    RejectedUnpinnable,
    RejectedUnofficial,
    RejectedModelRisk
}

public enum NodalOsRecognizerDictionaryPairReplacementDecision
{
    ReadyForRecognizerDictionaryPairAcquisition,
    ReadyForManualPairApproval,
    BlockedByNoCompatibleRecognizerDictionaryPair,
    BlockedByUnpinnablePair,
    NotReady
}

public enum NodalOsAlternativeRecognizerCandidateDecision
{
    CandidateAcceptedForAcquisition,
    CandidateNeedsRuntimeProbe,
    CandidateNeedsManualReview,
    RejectedExtraClassUnresolved,
    RejectedNoExplicitDictionary,
    RejectedNoOnnx,
    RejectedUnpinnable,
    RejectedUnofficial,
    RejectedTooHighRisk
}

public enum NodalOsAlternativeRecognizerFamilyDecision
{
    ReadyForCleanRecognizerPairAcquisition,
    ReadyForAlternativeLocalOcrFamilyReview,
    ReadyForTesseractLocalFallbackReview,
    BlockedByNoCleanRecognizerPair,
    NotReady
}

public enum NodalOsExtraClassArgmaxProbeStatus
{
    ExtraClassNeverArgmax,
    ExtraClassArgmaxObserved,
    ExtraClassProbabilityNegligible,
    ExtraClassProbabilityNonTrivial,
    ProbeBlockedByInvalidInput,
    ProbeRuntimeFailed,
    ProbeTimedOut
}

public enum NodalOsExtraClassRiskClassification
{
    IgnoredExtraClassCandidate,
    BlockedByExtraClassArgmaxObserved,
    ManualReviewRequired,
    BlockedByUnexpectedClassCount,
    NotReady
}

public enum NodalOsExtraClassDecodePolicyReadiness
{
    ReadyForManualIgnoredExtraClassPolicyApproval,
    ReadyForClaudeExtraClassAuditWithProbeEvidence,
    BlockedByExtraClassArgmaxObserved,
    BlockedByExtraClassNontrivialProbability,
    ReadyForRecognizerModelReplacement,
    NotReady
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

public sealed record NodalOsDictionaryRawLineAnalysis(
    string AnalysisId,
    string SourceId,
    string UrlOrRef,
    string BranchOrTag,
    string License,
    long RawByteSize,
    string Sha256,
    int RawLineSegmentCount,
    int TokenCountPreservingEmptyLines,
    int TokenCountTrimmingEmptyLines,
    int TokenCountPreservingSpaceDroppingTerminalEmpty,
    int PaddleOcrParserTokenCount,
    int SpaceOnlyLineCount,
    int EmptyLineCount,
    bool HasFinalNewline,
    bool HasBom,
    bool HasComments,
    bool HasSignificantSpaceToken,
    bool HasSignificantEmptyTokenUnderPaddlePolicy,
    bool CountBecomes96UnderDocumentedParser,
    string FirstToken,
    string LastSignificantToken,
    string Reason);

public sealed record NodalOsRecognizerDictionaryPair(
    string PairId,
    string RecognizerModelId,
    string RecognizerModelPath,
    string RecognizerModelSha256,
    int RecognizerOutputClassCount,
    string DictionarySourceId,
    int DictionaryTokenCountUnderPaddlePolicy,
    int DictionaryTokenCountPreservingRawSegments,
    int CtcBlankIndex,
    NodalOsDictionaryParserPolicy ParserPolicy,
    bool SourceOfficial,
    bool SourceHashPinned,
    bool SourceSizePinned,
    bool NoAuthority);

public sealed record NodalOsRecognizerDictionaryPairCompatibility(
    string CompatibilityId,
    NodalOsRecognizerDictionaryPair Pair,
    IReadOnlyList<NodalOsDictionaryRawLineAnalysis> SourceAnalyses,
    NodalOsRecognizerDictionaryPairDecision Decision,
    bool FoundVerified96TokenSource,
    bool ParserLosesSpaceToken,
    bool ParserLosesEmptyToken,
    bool OnnxMetadataMatchesDictionary,
    bool DecodeAllowed,
    bool ProductiveOcrBlocked,
    bool ShadowModeBlocked,
    bool NoRawPersistence,
    bool NoFullScreen,
    bool NoSensitive,
    bool NoSaas,
    bool NoAuthority,
    string Reason);

public sealed record NodalOsRecognizerDictionaryPairCandidateSource(
    string SourceId,
    string Provider,
    string ModelUrlOrRef,
    string? DictionaryUrlOrRef,
    string License,
    string Provenance,
    bool Official,
    bool Verifiable);

public sealed record NodalOsRecognizerDictionaryPairCandidate(
    string CandidateId,
    NodalOsRecognizerDictionaryPairCandidateSource Source,
    string ModelFileName,
    string DictionaryFileName,
    string ModelTypeVersion,
    string Language,
    string ExpectedInputShape,
    int? ExpectedOutputClassCount,
    int? DictionaryTokenCount,
    int CtcBlankIndex,
    bool OnnxAvailable,
    bool ModelHashPinned,
    bool ModelSizePinned,
    string? ModelSha256,
    long? ModelSizeBytes,
    bool DictionaryExplicit,
    bool DictionaryHashPinned,
    bool DictionarySizePinned,
    string? DictionarySha256,
    long? DictionarySizeBytes,
    bool CompatibleWithOnnxRuntime1221,
    string Risks);

public sealed record NodalOsRecognizerDictionaryPairCandidateAudit(
    string AuditId,
    NodalOsRecognizerDictionaryPairCandidate Candidate,
    NodalOsRecognizerDictionaryPairCandidateDecision Decision,
    bool DecodeAttempted,
    bool NoAuthority,
    string Reason);

public sealed record NodalOsRecognizerDictionaryPairCompatibilityMatrix(
    string MatrixId,
    IReadOnlyList<NodalOsRecognizerDictionaryPairCandidateAudit> CandidateAudits,
    NodalOsRecognizerDictionaryPairCandidateAudit? SelectedCandidate,
    NodalOsRecognizerDictionaryPairReplacementDecision Decision,
    bool ProductiveOcrBlocked,
    bool ShadowModeBlocked,
    bool DecodeBlocked,
    bool NoRawPersistence,
    bool NoFullScreen,
    bool NoSensitive,
    bool NoSaas,
    bool NoAuthority,
    string Reason);

public sealed record NodalOsPaddleOcrExtraClassCandidate(
    string CandidateId,
    string Name,
    string ExpectedClassCountFormula,
    bool SupportsPpOcrV4Pattern,
    bool SupportsPpOcrV5Pattern,
    bool EvidenceApproved,
    string EvidenceSource,
    string Risk,
    bool DecodeAllowed,
    string Reason);

public sealed record NodalOsPaddleOcrExtraClassSemantics(
    string SemanticsId,
    int PpOcrV4DictionaryTokenCount,
    int PpOcrV4BlankCount,
    int PpOcrV4ObservedClassCount,
    int PpOcrV5DictionaryTokenCount,
    int PpOcrV5BlankCount,
    int PpOcrV5ObservedClassCount,
    IReadOnlyList<NodalOsPaddleOcrExtraClassCandidate> Candidates,
    NodalOsPaddleOcrExtraClassCandidate? ApprovedCandidate,
    bool OfficialBlankOnlyPolicyInsufficient,
    bool ExtraClassSemanticsResolved,
    bool DecodeAllowed,
    bool NoAuthority,
    string Reason);

public sealed record NodalOsPaddleOcrDecodeClassPolicy(
    string PolicyId,
    string Name,
    int DictionaryTokenCount,
    int BlankIndex,
    int ModelClassCount,
    int ExpectedClassCount,
    int? ExtraClassIndex,
    bool EvidenceApproved,
    bool HypothesisOnly,
    string EvidenceSource,
    bool AllowsDecode,
    string Reason);

public sealed record NodalOsPaddleOcrDecodePolicyApproval(
    string ApprovalId,
    NodalOsPaddleOcrDecodeClassPolicy Policy,
    NodalOsPaddleOcrDecodePolicyExperimentStatus Status,
    bool DecodeAttempted,
    bool DecodeAllowed,
    string? DecodedText,
    double? Confidence,
    bool RequiresHumanReview,
    bool NoRawPersistence,
    bool NoSensitive,
    bool NoAuthority,
    string Reason);

public sealed record NodalOsPaddleOcrDecodePolicyDecisionReport(
    string ReportId,
    NodalOsPaddleOcrExtraClassSemantics Semantics,
    IReadOnlyList<NodalOsPaddleOcrDecodePolicyApproval> PolicyApprovals,
    NodalOsPaddleOcrExtraClassDecision Decision,
    bool ProductiveOcrBlocked,
    bool ShadowModeBlocked,
    bool DecodeSuccessClaimed,
    bool NoRawPersistence,
    bool NoFullScreen,
    bool NoSensitive,
    bool NoSaas,
    bool NoAuthority,
    string Reason);

public sealed record NodalOsOcrModelFamilyCandidate(
    string CandidateId,
    string Name,
    string SourceQuality,
    bool LocalOffline,
    bool OnnxRuntimeSupported,
    bool DictionaryExplicit,
    bool ClassCountClear,
    bool NoSaas,
    bool NoAuthorityCompatible,
    string Risk,
    string Maintenance);

public sealed record NodalOsOcrRecognizerReplacementStrategy(
    string StrategyId,
    string Name,
    NodalOsOcrModelFamilyCandidate Candidate,
    NodalOsOcrRecognizerReplacementStrategyStatus Status,
    int Rank,
    bool RequiresManualApproval,
    bool DecodeAutoApproved,
    bool ProductiveOcrBlocked,
    bool ShadowModeBlocked,
    bool NoAuthority,
    string Reason);

public sealed record NodalOsOcrRecognizerReplacementStrategyMatrix(
    string MatrixId,
    IReadOnlyList<NodalOsOcrRecognizerReplacementStrategy> Strategies,
    NodalOsOcrRecognizerReplacementStrategy RecommendedStrategy,
    NodalOsOcrRecognizerReplacementDecision Decision,
    bool ExtraClassUnresolved,
    bool ProductiveOcrBlocked,
    bool ShadowModeBlocked,
    bool DecodeBlocked,
    bool NoRawPersistence,
    bool NoFullScreen,
    bool NoSensitive,
    bool NoSaas,
    bool NoAuthority,
    string Reason);

public sealed record NodalOsAlternativeRecognizerCandidate(
    string CandidateId,
    string Provider,
    string ModelUrlOrRef,
    string? DictionaryUrlOrRef,
    string? ConfigUrlOrRef,
    string License,
    string Provenance,
    bool Official,
    bool OnnxAvailable,
    bool DictionaryExplicit,
    int? OutputClassCount,
    int? DictionaryTokenCount,
    string BlankOrSpecialTokenPolicy,
    bool HashSizePinnable,
    bool LocalOffline,
    string ExpectedRuntimeRisk,
    string ImplementationImpact,
    string PrivacySecurityRisk,
    bool ExtraClassUnresolved,
    bool NoSaas,
    bool NoAuthority);

public sealed record NodalOsAlternativeRecognizerCandidateAudit(
    string AuditId,
    NodalOsAlternativeRecognizerCandidate Candidate,
    NodalOsAlternativeRecognizerCandidateDecision Decision,
    bool DecodeAttempted,
    bool DownloadExecuted,
    bool ProductiveOcrBlocked,
    bool ShadowModeBlocked,
    bool NoAuthority,
    string Reason);

public sealed record NodalOsCleanRecognizerCompatibilityMatrix(
    string MatrixId,
    IReadOnlyList<NodalOsAlternativeRecognizerCandidateAudit> CandidateAudits,
    NodalOsAlternativeRecognizerCandidateAudit? SelectedCandidate,
    NodalOsAlternativeRecognizerFamilyDecision Decision,
    bool PpOcrV5IgnoredExtraClassAutoSelected,
    bool ProductiveOcrBlocked,
    bool ShadowModeBlocked,
    bool DecodeBlocked,
    bool DownloadExecuted,
    bool NoRawPersistence,
    bool NoFullScreen,
    bool NoSensitive,
    bool NoSaas,
    bool NoAuthority,
    string Reason);

public sealed record NodalOsExtraClassProbabilitySummary(
    int ExtraClassIndex,
    int OutputClassCount,
    int Timesteps,
    int ExtraClassArgmaxCount,
    double ExtraClassMaxProbability,
    double ExtraClassAverageProbability,
    int BlankIndex,
    int BlankArgmaxCount,
    IReadOnlyList<int> DominantClassIndexes);

public sealed record NodalOsExtraClassArgmaxProbeResult(
    string ProbeId,
    string FixtureId,
    string FixtureGroup,
    NodalOsExtraClassArgmaxProbeStatus Status,
    IReadOnlyList<int> OutputShape,
    NodalOsExtraClassProbabilitySummary ProbabilitySummary,
    bool RanOutOfProcess,
    bool ParentSurvived,
    bool TempCleanup,
    bool RawPersisted,
    bool Sensitive,
    bool FullScreen,
    bool CallsSaas,
    bool NoAuthority,
    string Reason);

public sealed record NodalOsIgnoredExtraClassPolicyCandidate(
    string CandidateId,
    NodalOsExtraClassRiskClassification RiskClassification,
    double NegligibleProbabilityThreshold,
    bool DecodeApproved,
    bool RequiresManualApproval,
    bool ProductiveOcrBlocked,
    bool ShadowModeBlocked,
    bool NoAuthority,
    string Reason);

public sealed record NodalOsExtraClassArgmaxProbeDecisionReport(
    string ReportId,
    IReadOnlyList<NodalOsExtraClassArgmaxProbeResult> ProbeResults,
    NodalOsIgnoredExtraClassPolicyCandidate PolicyCandidate,
    NodalOsExtraClassDecodePolicyReadiness Decision,
    bool DecodeAttempted,
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
