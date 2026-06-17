using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.BrowserExecutor.Cdp;

public sealed class NodalOsOcrDictionaryCompatibilityService
{
    public const int PaddleOcrV4EnglishRecognizerClassCount = 97;
    public const int PaddleOcrV4EnglishExpectedCharsetCount = PaddleOcrV4EnglishRecognizerClassCount - 1;
    public const string PaddleOcrV4EnglishDictionaryId = "paddleocr-en-ppocrv4-rec-ctc-dictionary";
    public const string PaddleOcrV4EnglishDictionaryRelativePath = "tools/ocr-worker/models/onnx/dictionaries/paddleocr-ppocrv4-en-dict.txt";

    public NodalOsOcrDictionaryManifest CreateCurrentAsciiManifest(bool verified = false)
    {
        return new NodalOsOcrDictionaryManifest(
            "en-ascii",
            "en",
            NodalOsOnnxOcrCharacterDictionary.EnglishAscii.Count,
            BlankTokenCount: 1,
            SourceRef: "embedded-offline-fixture-ascii-subset",
            ExpectedSha256: null,
            verified,
            NoAuthority: true);
    }

    public NodalOsOcrDictionaryManifestEntry CreatePaddleOcrV4EnglishManifestEntryWithoutApprovedSource()
    {
        return new NodalOsOcrDictionaryManifestEntry(
            PaddleOcrV4EnglishDictionaryId,
            NodalOsOcrDictionaryRole.RecognitionCtcCharset,
            ExpectedFileName: "paddleocr-ppocrv4-en-dict.txt",
            PaddleOcrV4EnglishDictionaryRelativePath,
            ExpectedCharsetCount: PaddleOcrV4EnglishExpectedCharsetCount,
            ExpectedRecognizerClassCount: PaddleOcrV4EnglishRecognizerClassCount,
            NodalOsOcrDictionaryBlankTokenPolicy.BlankAppendedAtEnd,
            CtcBlankIndex: PaddleOcrV4EnglishExpectedCharsetCount,
            NewlineHandling: "UTF-8 one token per line; CRLF/LF normalized before hashing and loading",
            SourceUrl: null,
            SourceRef: "not-selected; no approved source/hash exists in current manifest or M200-M237 reports",
            ExpectedSha256: null,
            ExpectedSizeBytes: null,
            NodalOsOcrDictionaryAvailabilityStatus.SourceNotSelected,
            Gitignored: true,
            Committed: false,
            NoAuthority: true);
    }

    public NodalOsOcrDictionaryAcquisitionPlan CreateSourceSelectionAcquisitionPlan(
        NodalOsOcrDictionaryManifestEntry entry)
    {
        return new NodalOsOcrDictionaryAcquisitionPlan(
            $"dict-acquisition-plan-{Guid.NewGuid():N}",
            entry,
            SourceApproved: false,
            DownloadAllowed: false,
            PlannedScripts:
            [
                "tools/ocr-worker/models/onnx/download-dictionaries.ps1",
                "tools/ocr-worker/models/onnx/verify-dictionaries.ps1",
                "tools/ocr-worker/models/onnx/rollback-dictionaries.ps1"
            ],
            Commands:
            [
                "pwsh -NoProfile -ExecutionPolicy Bypass -File tools/ocr-worker/models/onnx/download-dictionaries.ps1 -Confirm",
                "pwsh -NoProfile -ExecutionPolicy Bypass -File tools/ocr-worker/models/onnx/verify-dictionaries.ps1"
            ],
            Decision: "READY_FOR_DICTIONARY_SOURCE_SELECTION",
            NoSaas: true,
            NoAuthority: true,
            Reason: "dictionary source URL, SHA-256, and expected size must be selected before any download");
    }

    public NodalOsOcrDictionaryCompatibilityResult EvaluateManifestEntry(
        NodalOsOcrDictionaryManifestEntry entry,
        int? actualCharsetCount,
        string? actualSha256,
        long? actualSizeBytes,
        bool actualCommitted)
    {
        if (actualCommitted && entry.Gitignored)
        {
            var ctc = Ctc(
                entry.ExpectedRecognizerClassCount,
                dictionaryClassCountIncludingBlank: 0,
                NodalOsOcrDictionaryCompatibilityStatus.UnexpectedCommittedDictionary,
                decodeAllowed: false,
                "dictionary is unexpectedly committed while policy requires gitignored runtime acquisition");
            return Result(NodalOsOcrDictionaryCompatibilityStatus.UnexpectedCommittedDictionary, null, ctc, ctc.Reason);
        }

        if (entry.ExpectedSha256 is not null &&
            actualSha256 is not null &&
            !string.Equals(entry.ExpectedSha256, actualSha256, StringComparison.OrdinalIgnoreCase))
        {
            var ctc = Ctc(
                entry.ExpectedRecognizerClassCount,
                (actualCharsetCount ?? 0) + 1,
                NodalOsOcrDictionaryCompatibilityStatus.HashMismatch,
                decodeAllowed: false,
                "dictionary SHA-256 does not match manifest");
            return Result(NodalOsOcrDictionaryCompatibilityStatus.HashMismatch, null, ctc, ctc.Reason);
        }

        if (actualCharsetCount is not null && actualCharsetCount.Value != entry.ExpectedCharsetCount)
        {
            var ctc = Ctc(
                entry.ExpectedRecognizerClassCount,
                actualCharsetCount.Value + 1,
                NodalOsOcrDictionaryCompatibilityStatus.ClassCountMismatch,
                decodeAllowed: false,
                $"dictionary charset count {actualCharsetCount.Value} does not match expected {entry.ExpectedCharsetCount}");
            return Result(NodalOsOcrDictionaryCompatibilityStatus.ClassCountMismatch, null, ctc, ctc.Reason);
        }

        if (entry.ExpectedSizeBytes is not null &&
            actualSizeBytes is not null &&
            entry.ExpectedSizeBytes.Value != actualSizeBytes.Value)
        {
            var ctc = Ctc(
                entry.ExpectedRecognizerClassCount,
                (actualCharsetCount ?? entry.ExpectedCharsetCount) + 1,
                NodalOsOcrDictionaryCompatibilityStatus.DictionaryUnverified,
                decodeAllowed: false,
                "dictionary size does not match manifest");
            return Result(NodalOsOcrDictionaryCompatibilityStatus.DictionaryUnverified, null, ctc, ctc.Reason);
        }

        if (entry.SourceUrl is null || entry.ExpectedSha256 is null || entry.ExpectedSizeBytes is null)
        {
            var ctc = Ctc(
                entry.ExpectedRecognizerClassCount,
                entry.ExpectedCharsetCount + 1,
                NodalOsOcrDictionaryCompatibilityStatus.SourceNotSelected,
                decodeAllowed: false,
                "dictionary source, hash, or size is not approved");
            return Result(NodalOsOcrDictionaryCompatibilityStatus.SourceNotSelected, null, ctc, ctc.Reason);
        }

        var manifest = new NodalOsOcrDictionaryManifest(
            entry.DictionaryId,
            "en",
            entry.ExpectedCharsetCount,
            BlankTokenCount: 1,
            entry.SourceRef,
            entry.ExpectedSha256,
            Verified: true,
            entry.NoAuthority);

        return Evaluate(manifest, entry.ExpectedRecognizerClassCount);
    }

    public NodalOsOcrDictionaryReadinessReport DecideReadiness(
        NodalOsOcrDictionaryManifestEntry entry,
        NodalOsOcrDictionaryCompatibilityResult compatibility,
        NodalOsOcrDictionaryAcquisitionPlan acquisitionPlan,
        bool dictionaryPresent,
        bool hashVerified,
        bool decodeAttempted)
    {
        var decision = Decide(entry, compatibility, acquisitionPlan, hashVerified, decodeAttempted);

        return new NodalOsOcrDictionaryReadinessReport(
            $"dictionary-readiness-{Guid.NewGuid():N}",
            decision,
            entry,
            compatibility,
            acquisitionPlan,
            dictionaryPresent,
            hashVerified,
            decodeAttempted,
            ProductiveOcrBlocked: true,
            ShadowModeBlocked: true,
            NoRawPersistence: true,
            NoFullScreen: true,
            NoSensitive: true,
            NoSaas: true,
            NoAuthority: entry.NoAuthority && compatibility.NoAuthority && acquisitionPlan.NoAuthority,
            Reason: BrowserCredentialRedactor.Redact(
                $"{decision}; dictionary={compatibility.Status}; sourceApproved={acquisitionPlan.SourceApproved}; decodeAttempted={decodeAttempted}"));
    }

    public NodalOsOcrDictionaryCompatibilityResult Evaluate(
        NodalOsOcrDictionaryManifest? dictionary,
        int recognizerOutputClassCount,
        string inferredAxes = "time,batch,class")
    {
        if (dictionary is null)
        {
            var ctc = Ctc(
                recognizerOutputClassCount,
                dictionaryClassCountIncludingBlank: 0,
                NodalOsOcrDictionaryCompatibilityStatus.MissingDictionary,
                decodeAllowed: false,
                "no dictionary manifest available");
            return Result(NodalOsOcrDictionaryCompatibilityStatus.MissingDictionary, null, ctc, "dictionary manifest missing");
        }

        var dictionaryClassCount = dictionary.CharacterCount + dictionary.BlankTokenCount;
        if (dictionaryClassCount != recognizerOutputClassCount)
        {
            var ctc = Ctc(
                recognizerOutputClassCount,
                dictionaryClassCount,
                NodalOsOcrDictionaryCompatibilityStatus.ClassCountMismatch,
                decodeAllowed: false,
                $"recognizer class count {recognizerOutputClassCount} does not match dictionary+blank {dictionaryClassCount}");
            return Result(NodalOsOcrDictionaryCompatibilityStatus.ClassCountMismatch, dictionary, ctc, ctc.Reason);
        }

        if (!dictionary.Verified)
        {
            var ctc = Ctc(
                recognizerOutputClassCount,
                dictionaryClassCount,
                NodalOsOcrDictionaryCompatibilityStatus.DictionaryUnverified,
                decodeAllowed: false,
                "dictionary source/hash is not verified");
            return Result(NodalOsOcrDictionaryCompatibilityStatus.DictionaryUnverified, dictionary, ctc, ctc.Reason);
        }

        var compatible = new NodalOsOcrCtcDecoderCompatibility(
            $"ctc-compat-{Guid.NewGuid():N}",
            recognizerOutputClassCount,
            dictionaryClassCount,
            BatchTimeClassAxesInferred: true,
            inferredAxes,
            NodalOsOcrDictionaryCompatibilityStatus.Compatible,
            DecodeAllowed: true,
            NoAuthority: true,
            "dictionary class count matches recognizer output; decode allowed but remains no-authority");

        return new NodalOsOcrDictionaryCompatibilityResult(
            $"dict-compat-{Guid.NewGuid():N}",
            NodalOsOcrDictionaryCompatibilityStatus.Compatible,
            dictionary,
            compatible,
            RecognitionSuccessAllowed: true,
            RequiresHumanReview: true,
            NoAuthority: true,
            compatible.Reason);
    }

    private static NodalOsOcrCtcDecoderCompatibility Ctc(
        int recognizerOutputClassCount,
        int dictionaryClassCountIncludingBlank,
        NodalOsOcrDictionaryCompatibilityStatus status,
        bool decodeAllowed,
        string reason) =>
        new(
            $"ctc-compat-{Guid.NewGuid():N}",
            recognizerOutputClassCount,
            dictionaryClassCountIncludingBlank,
            BatchTimeClassAxesInferred: recognizerOutputClassCount > 0,
            InferredAxes: recognizerOutputClassCount > 0 ? "time,batch,class" : "unknown",
            status,
            decodeAllowed,
            NoAuthority: true,
            BrowserCredentialRedactor.Redact(reason));

    private static NodalOsOcrDictionaryCompatibilityResult Result(
        NodalOsOcrDictionaryCompatibilityStatus status,
        NodalOsOcrDictionaryManifest? dictionary,
        NodalOsOcrCtcDecoderCompatibility ctc,
        string reason) =>
        new(
            $"dict-compat-{Guid.NewGuid():N}",
            status,
            dictionary,
            ctc,
            RecognitionSuccessAllowed: false,
            RequiresHumanReview: true,
            NoAuthority: true,
            BrowserCredentialRedactor.Redact(reason));

    private static NodalOsOcrDictionaryReadinessDecision Decide(
        NodalOsOcrDictionaryManifestEntry entry,
        NodalOsOcrDictionaryCompatibilityResult compatibility,
        NodalOsOcrDictionaryAcquisitionPlan acquisitionPlan,
        bool hashVerified,
        bool decodeAttempted)
    {
        if (decodeAttempted && !compatibility.CtcDecoderCompatibility.DecodeAllowed)
            return NodalOsOcrDictionaryReadinessDecision.BlockedByDictionaryClassCountMismatch;

        if (compatibility.Status == NodalOsOcrDictionaryCompatibilityStatus.HashMismatch)
            return NodalOsOcrDictionaryReadinessDecision.BlockedByDictionaryHashMismatch;

        if (!entry.NoAuthority || !compatibility.NoAuthority || !acquisitionPlan.NoAuthority)
            return NodalOsOcrDictionaryReadinessDecision.NotReady;

        if (!acquisitionPlan.SourceApproved ||
            entry.SourceUrl is null ||
            entry.ExpectedSha256 is null ||
            entry.ExpectedSizeBytes is null ||
            compatibility.Status == NodalOsOcrDictionaryCompatibilityStatus.SourceNotSelected)
            return NodalOsOcrDictionaryReadinessDecision.ReadyForDictionarySourceSelection;

        if (compatibility.Status == NodalOsOcrDictionaryCompatibilityStatus.ClassCountMismatch)
            return NodalOsOcrDictionaryReadinessDecision.BlockedByDictionaryClassCountMismatch;

        if (compatibility.Status == NodalOsOcrDictionaryCompatibilityStatus.Compatible && hashVerified)
            return NodalOsOcrDictionaryReadinessDecision.ReadyForSyntheticTextDecodeFixtures;

        return NodalOsOcrDictionaryReadinessDecision.NotReady;
    }
}

public sealed class NodalOsGuardedSyntheticTextOcrRetryReadinessReview
{
    public NodalOsGuardedSyntheticTextOcrReadinessReport Evaluate(NodalOsGuardedSyntheticTextOcrRetryReadinessInput input)
    {
        var riskyInProcess = input.ProbeResults.Any(r => r.RanInProcess);
        var raw = input.ProbeResults.Any(r => r.RawPersisted);
        var saas = input.ProbeResults.Any(r => r.CallsSaas);
        var noAuthority = input.ProbeResults.All(r => r.NoAuthority) &&
                          input.DictionaryCompatibility.NoAuthority &&
                          input.DictionaryCompatibility.CtcDecoderCompatibility.NoAuthority;

        var decision = Decide(input, riskyInProcess, raw, saas, noAuthority);

        return new NodalOsGuardedSyntheticTextOcrReadinessReport(
            $"guarded-text-retry-readiness-{Guid.NewGuid():N}",
            decision,
            input.GuardExists,
            RiskyTextNeverRanInProcess: !riskyInProcess,
            input.ParentSurvivedCrash,
            input.ChildCleanupWorks,
            input.TempCleanupWorks,
            NoRawPersistence: !raw,
            input.NoFullScreen,
            input.NoSensitive,
            NoSaas: !saas,
            noAuthority,
            DetectionDiagnosed: true,
            RecognitionDiagnosedOrUnreachable: input.Recognition.Reachable || input.Recognition.Status == NodalOsGuardedSyntheticTextOcrProbeStatus.NoTextDetected,
            DictionaryStatusDocumented: input.DictionaryCompatibility.Status != NodalOsOcrDictionaryCompatibilityStatus.Unknown,
            ModelCompatibilityDocumented: true,
            ShadowModeBlocked: decision != NodalOsGuardedSyntheticTextOcrReadinessDecision.ReadyForSyntheticPositiveRecognition,
            ProductionPublicOcrBlocked: true,
            Reason: BrowserCredentialRedactor.Redact(Reason(decision, input)));
    }

    private static NodalOsGuardedSyntheticTextOcrReadinessDecision Decide(
        NodalOsGuardedSyntheticTextOcrRetryReadinessInput input,
        bool riskyInProcess,
        bool raw,
        bool saas,
        bool noAuthority)
    {
        if (!input.DetectionModelVerified || !input.RecognitionModelVerified || !input.ClassificationModelVerified || input.OnnxModelsTracked)
            return NodalOsGuardedSyntheticTextOcrReadinessDecision.NotReady;

        if (!input.GuardExists || riskyInProcess || !input.ParentSurvivedCrash || !input.ChildCleanupWorks ||
            !input.TempCleanupWorks || raw || saas || !noAuthority || !input.NoFullScreen || !input.NoSensitive)
            return NodalOsGuardedSyntheticTextOcrReadinessDecision.NotReady;

        if (input.Detection.Status == NodalOsGuardedSyntheticTextOcrProbeStatus.NativeRuntimeCrashContained ||
            input.ProbeResults.Any(r => r.Status == NodalOsGuardedSyntheticTextOcrProbeStatus.NativeRuntimeCrashContained))
            return NodalOsGuardedSyntheticTextOcrReadinessDecision.BlockedByModelRuntime;

        if (input.Detection.Status == NodalOsGuardedSyntheticTextOcrProbeStatus.BlockedByPostProcessing)
            return NodalOsGuardedSyntheticTextOcrReadinessDecision.BlockedByPostProcessing;

        if (input.DictionaryCompatibility.Status is NodalOsOcrDictionaryCompatibilityStatus.MissingDictionary
            or NodalOsOcrDictionaryCompatibilityStatus.ClassCountMismatch
            or NodalOsOcrDictionaryCompatibilityStatus.DictionaryUnverified)
            return NodalOsGuardedSyntheticTextOcrReadinessDecision.ReadyForDictionaryCompletion;

        if (input.Recognition.Status == NodalOsGuardedSyntheticTextOcrProbeStatus.PositiveRecognition)
            return NodalOsGuardedSyntheticTextOcrReadinessDecision.ReadyForSyntheticPositiveRecognition;

        if (input.Detection.Status == NodalOsGuardedSyntheticTextOcrProbeStatus.PositiveDetection)
            return NodalOsGuardedSyntheticTextOcrReadinessDecision.ReadyForMoreSyntheticFixtures;

        return NodalOsGuardedSyntheticTextOcrReadinessDecision.ReadyForMoreSyntheticFixtures;
    }

    private static string Reason(
        NodalOsGuardedSyntheticTextOcrReadinessDecision decision,
        NodalOsGuardedSyntheticTextOcrRetryReadinessInput input) =>
        $"{decision}; detection={input.Detection.Status}; recognition={input.Recognition.Status}; dictionary={input.DictionaryCompatibility.Status}; shadow remains blocked";
}
