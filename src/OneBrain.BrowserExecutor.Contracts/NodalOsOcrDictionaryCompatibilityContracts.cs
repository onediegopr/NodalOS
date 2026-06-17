namespace OneBrain.BrowserExecutor.Contracts;

// M221 - OCR dictionary / CTC compatibility contracts.
// Recognition must not claim positive text unless the model output class count and dictionary manifest match.

public enum NodalOsOcrDictionaryCompatibilityStatus
{
    Compatible,
    MissingDictionary,
    ClassCountMismatch,
    DictionaryUnverified,
    Unknown
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
