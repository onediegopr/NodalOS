using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.BrowserExecutor.Cdp;

public sealed class NodalOsOcrDictionaryCompatibilityService
{
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
