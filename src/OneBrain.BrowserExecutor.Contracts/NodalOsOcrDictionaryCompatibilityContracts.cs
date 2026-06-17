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
    ReadyForSyntheticTextDecodeFixtures,
    ReadyForMoreSyntheticFixtures,
    BlockedByDictionaryClassCountMismatch,
    BlockedByDictionaryHashMismatch,
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
