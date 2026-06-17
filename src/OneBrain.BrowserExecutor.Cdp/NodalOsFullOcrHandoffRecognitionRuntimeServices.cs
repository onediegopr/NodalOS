using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.BrowserExecutor.Cdp;

public sealed class NodalOsFullOcrRuntimeDecisionService
{
    public NodalOsFullOcrRuntimeDecisionReport Decide(NodalOsFullOcrRuntimeDecisionInput input)
    {
        if (!input.ModelsVerified ||
            !input.RiskyOcrNeverRanInProcess ||
            !input.ParentSurvived ||
            !input.TempCleanup ||
            !input.NoRawPersistence ||
            !input.NoFullScreen ||
            !input.NoSensitive ||
            !input.NoSaas ||
            !input.NoAuthority)
        {
            return Report(NodalOsFullOcrRuntimeDecision.NotReady, input, "runtime safety/readiness gates not satisfied");
        }

        var handoffFailure = input.HandoffResults.Any(r => r.Status is
            NodalOsFullOcrHandoffProbeStatus.BlockedByInvalidBox or
            NodalOsFullOcrHandoffProbeStatus.BlockedByOutOfBoundsCrop or
            NodalOsFullOcrHandoffProbeStatus.BlockedByEmptyCrop or
            NodalOsFullOcrHandoffProbeStatus.BlockedByRecognizerTensorShape);

        if (handoffFailure)
            return Report(NodalOsFullOcrRuntimeDecision.ReadyForHandoffFix, input, "box/crop handoff blocks before recognizer runtime");

        var recognizerCrash = input.RecognizerResults.Any(r => r.Status == NodalOsRecognizerRuntimeProbeStatus.NativeRuntimeCrashContained) ||
                              input.HandoffResults.Any(r => r.Status == NodalOsFullOcrHandoffProbeStatus.NativeRuntimeCrashContained);
        var recognizerSucceeded = input.RecognizerResults.Any(r => r.Status is
            NodalOsRecognizerRuntimeProbeStatus.RunSucceeded or
            NodalOsRecognizerRuntimeProbeStatus.OutputMetadataCaptured) ||
            input.HandoffResults.Any(r => r.Status == NodalOsFullOcrHandoffProbeStatus.RecognizerRunSucceeded);

        if (recognizerCrash && !recognizerSucceeded)
            return Report(NodalOsFullOcrRuntimeDecision.ReadyForRecognizerRuntimeExperiment, input, "recognizer session.Run crashes on safe tensors/crops");

        if (input.DictionaryCompatibility.Status is NodalOsOcrDictionaryCompatibilityStatus.ClassCountMismatch or
            NodalOsOcrDictionaryCompatibilityStatus.MissingDictionary or
            NodalOsOcrDictionaryCompatibilityStatus.DictionaryUnverified)
        {
            return recognizerSucceeded
                ? Report(NodalOsFullOcrRuntimeDecision.ReadyForDictionaryCompletion, input, "recognizer runtime works but dictionary/CTC compatibility blocks decode")
                : Report(NodalOsFullOcrRuntimeDecision.BlockedByDictionary, input, "dictionary is incompatible and recognizer runtime has no positive run evidence");
        }

        if (recognizerCrash)
            return Report(NodalOsFullOcrRuntimeDecision.BlockedByModelRuntime, input, "recognizer runtime remains crash-prone");

        if (!recognizerSucceeded && input.RecognizerResults.Count > 0)
            return Report(NodalOsFullOcrRuntimeDecision.ReadyForRecognizerModelReplacement, input, "recognizer runtime did not produce usable output metadata");

        return Report(NodalOsFullOcrRuntimeDecision.ReadyForPostProcessingFix, input, "runtime and dictionary gates are clear; remaining issue is downstream postprocessing");
    }

    private static NodalOsFullOcrRuntimeDecisionReport Report(
        NodalOsFullOcrRuntimeDecision decision,
        NodalOsFullOcrRuntimeDecisionInput input,
        string reason)
    {
        var recognizerSucceeded = input.RecognizerResults.Any(r => r.Status is
            NodalOsRecognizerRuntimeProbeStatus.RunSucceeded or
            NodalOsRecognizerRuntimeProbeStatus.OutputMetadataCaptured) ||
            input.HandoffResults.Any(r => r.Status == NodalOsFullOcrHandoffProbeStatus.RecognizerRunSucceeded);
        var handoffFailure = input.HandoffResults.Any(r => r.Status is
            NodalOsFullOcrHandoffProbeStatus.BlockedByInvalidBox or
            NodalOsFullOcrHandoffProbeStatus.BlockedByOutOfBoundsCrop or
            NodalOsFullOcrHandoffProbeStatus.BlockedByEmptyCrop or
            NodalOsFullOcrHandoffProbeStatus.BlockedByRecognizerTensorShape);
        var recognizerCrash = input.RecognizerResults.Any(r => r.Status == NodalOsRecognizerRuntimeProbeStatus.NativeRuntimeCrashContained) ||
                              input.HandoffResults.Any(r => r.Status == NodalOsFullOcrHandoffProbeStatus.NativeRuntimeCrashContained);

        return new NodalOsFullOcrRuntimeDecisionReport(
            $"full-ocr-runtime-decision-{Guid.NewGuid():N}",
            decision,
            ShadowModeBlocked: true,
            ProductiveOcrBlocked: true,
            DetectorIsolatedRunSucceeded: input.HandoffResults.Any(r => r.DetectorRunSucceeded),
            DetectorPostProcessingProducedBoxes: input.HandoffResults.Any(r => r.DetectorPostProcessingReached && r.BoxWidth > 0 && r.BoxHeight > 0),
            handoffFailure,
            recognizerCrash,
            recognizerSucceeded,
            input.DictionaryCompatibility.Status is NodalOsOcrDictionaryCompatibilityStatus.ClassCountMismatch,
            input.NoAuthority && input.HandoffResults.All(r => r.NoAuthority) && input.RecognizerResults.All(r => r.NoAuthority),
            BrowserCredentialRedactor.Redact(reason));
    }
}
