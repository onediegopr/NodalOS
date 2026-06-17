namespace OneBrain.BrowserExecutor.Contracts;

// M271-M273 - PaddleOCR extra-class root cause + decoder policy resolution.
//
// Root cause (officially evidenced): the "+1" extra class observed on both PP-OCRv4 (dictionary 95 -> 97)
// and PP-OCRv5 (dictionary 436 -> 438) is the SPACE character appended by PaddleOCR `use_space_char: true`.
// PaddleOCR ppocr/postprocess/rec_postprocess.py BaseRecLabelDecode.__init__ appends " " to the character
// list when use_space_char is true, AFTER reading the dictionary file; CTCLabelDecode.add_special_char then
// prepends 'blank' at index 0. Final charset = ['blank'] + dictionary[1..N] + [' '] => N + 2 classes.
//
// These contracts capture the resolved semantics and a hypothesis-only experimental decoder harness.
// They never promote OCR to readiness, never claim decode success, and stay no-authority.

public enum NodalOsPaddleOcrSpaceTokenRootCauseStatus
{
    SpaceTokenFromUseSpaceCharConfirmed,
    Unresolved
}

public enum NodalOsPaddleOcrExtraClassDecodePolicyKind
{
    // Officially evidenced: the extra class is the space character (use_space_char).
    OfficialSpaceToken,
    // Hypothesis-only alternatives that previous milestones could not approve.
    HypothesisIgnoreExtraClass,
    HypothesisUnknownToken,
    HypothesisPaddingToken,
    // Official blank-only policy: cannot decode because it under-counts by one class.
    BlankOnlyClassCountMismatch
}

// Charset layout under PaddleOCR CTC + use_space_char.
public sealed record NodalOsPaddleOcrCharsetLayout(
    string LayoutId,
    string ModelFamily,
    int BlankIndex,
    int DictionaryStartIndex,
    int DictionaryEndIndexInclusive,
    int SpaceIndex,
    int DictionaryTokenCount,
    int TotalClassCount,
    bool UseSpaceChar,
    string Formula,
    string EvidenceSource);

public sealed record NodalOsPaddleOcrSpaceTokenRootCause(
    string RootCauseId,
    NodalOsPaddleOcrSpaceTokenRootCauseStatus Status,
    NodalOsPaddleOcrCharsetLayout PpOcrV4Layout,
    NodalOsPaddleOcrCharsetLayout PpOcrV5Layout,
    bool AppliesToBothFamilies,
    bool OutputIsSoftmaxProbabilities,
    string OutputAxisOrder,
    bool ExtraClassResolved,
    string ExtraClassMeaning,
    IReadOnlyList<string> OfficialEvidence,
    IReadOnlyList<string> BehavioralEvidence,
    bool NoAuthority,
    string Reason);

// Per-timestep top-2 evidence for the extra (space) class.
public sealed record NodalOsPaddleOcrTimestepTopK(
    int Timestep,
    int ArgmaxIndex,
    double ArgmaxProbability,
    int RunnerUpIndex,
    double RunnerUpProbability,
    double SpaceClassProbability,
    bool SpaceIsTopTwo);

public sealed record NodalOsPaddleOcrDecodePolicyExperiment(
    string ExperimentId,
    NodalOsPaddleOcrExtraClassDecodePolicyKind PolicyKind,
    bool EvidenceApproved,
    bool HypothesisOnly,
    string FixtureId,
    int Timesteps,
    int ClassCount,
    bool DecodeAttempted,
    string DecodedText,
    double MeanConfidence,
    int SpaceTokenEmissions,
    bool ClassCountMismatch,
    IReadOnlyList<NodalOsPaddleOcrTimestepTopK> TopK,
    bool ReadinessPromoted,
    bool ProductiveOcr,
    bool ShadowMode,
    bool NoRawPersistence,
    bool NoSensitive,
    bool NoAuthority,
    string Reason);

public sealed record NodalOsPaddleOcrSpaceTokenDecisionReport(
    string ReportId,
    NodalOsPaddleOcrSpaceTokenRootCause RootCause,
    IReadOnlyList<NodalOsPaddleOcrDecodePolicyExperiment> Experiments,
    NodalOsPaddleOcrExtraClassDecision Decision,
    bool ExtraClassSemanticsResolved,
    bool ApprovedPolicyIsSpaceToken,
    bool DecodeSuccessClaimed,
    bool ProductiveOcrBlocked,
    bool ShadowModeBlocked,
    bool NoRawPersistence,
    bool NoFullScreen,
    bool NoSensitive,
    bool NoSaas,
    bool NoAuthority,
    string Reason);
