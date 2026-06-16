using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.BrowserExecutor.Cdp;

// M199 — Offline synthetic ONNX OCR readiness gate.
// Honest decision: only ReadyForOnnxSyntheticRun when models verified AND pre/post-processing ready.
public sealed class NodalOsOnnxOcrOfflineReadinessGate
{
    private readonly NodalOsPaddleOcrOnnxModelReadinessService _modelReadiness = new();

    public NodalOsOnnxOcrOfflineReadinessReport Evaluate(
        NodalOsPaddleOcrOnnxModelManifest manifest,
        string repositoryRoot,
        bool licenseAccepted,
        bool preProcessorReady,
        bool detectorPostProcessorReady,
        bool recognizerPostProcessorReady,
        NodalOsOnnxOcrSyntheticFixtureSet? fixtureSet = null,
        NodalOsOnnxModelSessionReadiness? sessionReadiness = null)
    {
        var requirements = new List<NodalOsOnnxOcrOfflineReadinessRequirement>();
        var warnings = new List<string>();

        var onnxRuntimeAvailable = IsOnnxRuntimePackageAvailable();
        requirements.Add(Requirement("onnx-runtime", "ONNX Runtime .NET package available", onnxRuntimeAvailable, "ONNX Runtime package missing", blocks: true));

        var modelReadiness = _modelReadiness.Evaluate(manifest, repositoryRoot, licenseAccepted);
        requirements.Add(Requirement("license-review", "license reviewed/accepted", licenseAccepted, "license not accepted", blocks: true));
        requirements.Add(Requirement("detection-model", "detection-model", modelReadiness.Status == NodalOsPaddleOcrOnnxModelStatus.Verified && IsDetectionModelVerified(manifest, repositoryRoot, licenseAccepted), modelReadiness.Reason, blocks: true));
        requirements.Add(Requirement("recognition-model", "recognition-model", modelReadiness.Status == NodalOsPaddleOcrOnnxModelStatus.Verified && IsRecognitionModelVerified(manifest, repositoryRoot, licenseAccepted), modelReadiness.Reason, blocks: true));

        var shapesKnown = fixtureSet?.KnownShapes ?? false;
        requirements.Add(Requirement("model-shapes", "model-shapes", shapesKnown, "model shapes unknown; use verified fixture set", blocks: true));

        requirements.Add(Requirement("preprocessor", "preprocessor", preProcessorReady, "pre-processor not ready", blocks: true));
        requirements.Add(Requirement("detector-postprocessor", "detector-postprocessor", detectorPostProcessorReady, "detector post-processor not ready", blocks: true));
        requirements.Add(Requirement("recognizer-postprocessor", "recognizer-postprocessor", recognizerPostProcessorReady, "recognizer post-processor not ready", blocks: true));

        var sessionReady = sessionReadiness?.Ready ?? false;
        requirements.Add(Requirement("session-smoke", "session-smoke", sessionReady, sessionReadiness?.Reason ?? "session smoke not run", blocks: false));

        requirements.Add(Requirement("no-raw-persistence", "no raw persistence", true, "raw persistence blocked by policy", blocks: true));
        requirements.Add(Requirement("no-fullscreen", "no full-screen OCR", true, "full-screen OCR blocked by policy", blocks: true));
        requirements.Add(Requirement("no-sensitive", "no sensitive OCR", true, "sensitive OCR blocked by policy", blocks: true));
        requirements.Add(Requirement("no-saas", "no SaaS OCR", true, "SaaS OCR blocked by policy", blocks: true));
        requirements.Add(Requirement("no-authority", "OCR is not authority", true, "OCR must never be authority", blocks: true));

        var blocked = requirements.Where(r => r.BlocksReadiness && !r.Satisfied).ToList();

        var decision = DetermineDecision(blocked, modelReadiness, shapesKnown, preProcessorReady, detectorPostProcessorReady, recognizerPostProcessorReady, sessionReadiness);
        var canAttempt = decision == NodalOsOnnxOcrOfflineReadinessDecision.ReadyForOnnxSyntheticRun;

        if (modelReadiness.Status == NodalOsPaddleOcrOnnxModelStatus.Missing)
        {
            warnings.Add("model files missing; see paddleocr-onnx-model-acquisition-plan-m195.md and paddleocr-onnx-model-verification-m197.md");
        }
        else if (modelReadiness.Status != NodalOsPaddleOcrOnnxModelStatus.Verified)
        {
            warnings.Add($"model readiness: {modelReadiness.Status} — {modelReadiness.Reason}");
        }

        if (!shapesKnown)
        {
            warnings.Add("model shapes unknown; offline synthetic run requires verified fixture set");
        }

        if (sessionReadiness is not null && !sessionReadiness.Ready)
        {
            warnings.Add($"session smoke: {sessionReadiness.Status} — {sessionReadiness.Reason}");
        }

        return new NodalOsOnnxOcrOfflineReadinessReport(
            $"offline-readiness-{Guid.NewGuid():N}",
            decision,
            canAttempt,
            requirements,
            onnxRuntimeAvailable,
            IsDetectionModelVerified(manifest, repositoryRoot, licenseAccepted),
            IsRecognitionModelVerified(manifest, repositoryRoot, licenseAccepted),
            shapesKnown,
            preProcessorReady,
            detectorPostProcessorReady,
            recognizerPostProcessorReady,
            PixelRedactionV2Required: true,
            NoRawPersistence: true,
            NoFullScreen: true,
            NoSensitive: true,
            NoSaas: true,
            NoAuthority: true,
            ProductionPublicOcrBlocked: true,
            warnings,
            DateTimeOffset.UtcNow);
    }

    private static NodalOsOnnxOcrOfflineReadinessDecision DetermineDecision(
        List<NodalOsOnnxOcrOfflineReadinessRequirement> blocked,
        NodalOsPaddleOcrOnnxModelReadinessDetail modelReadiness,
        bool shapesKnown,
        bool preProcessorReady,
        bool detectorPostProcessorReady,
        bool recognizerPostProcessorReady,
        NodalOsOnnxModelSessionReadiness? sessionReadiness)
    {
        if (blocked.Any(r => r.Name.Contains("license")))
            return NodalOsOnnxOcrOfflineReadinessDecision.BlockedByPolicy;

        if (blocked.Any(r => r.Name is "detection-model" or "recognition-model"))
        {
            return modelReadiness.Status switch
            {
                NodalOsPaddleOcrOnnxModelStatus.Missing => NodalOsOnnxOcrOfflineReadinessDecision.ModelMissing,
                NodalOsPaddleOcrOnnxModelStatus.BlockedByLicense => NodalOsOnnxOcrOfflineReadinessDecision.BlockedByPolicy,
                NodalOsPaddleOcrOnnxModelStatus.BlockedBySize => NodalOsOnnxOcrOfflineReadinessDecision.BlockedByPolicy,
                _ => NodalOsOnnxOcrOfflineReadinessDecision.ModelUnverified
            };
        }

        if (blocked.Any(r => r.Name == "model-shapes"))
            return NodalOsOnnxOcrOfflineReadinessDecision.UnsupportedModelShape;

        if (blocked.Any(r => r.Name == "preprocessor"))
            return NodalOsOnnxOcrOfflineReadinessDecision.PreProcessingIncomplete;

        if (blocked.Any(r => r.Name is "detector-postprocessor" or "recognizer-postprocessor"))
            return NodalOsOnnxOcrOfflineReadinessDecision.PostProcessingIncomplete;

        if (sessionReadiness is not null && !sessionReadiness.Ready)
            return NodalOsOnnxOcrOfflineReadinessDecision.SessionLoadFailed;

        if (blocked.Any(r => r.Name.Contains("redaction")))
            return NodalOsOnnxOcrOfflineReadinessDecision.BlockedByRedaction;

        if (blocked.Count > 0)
            return NodalOsOnnxOcrOfflineReadinessDecision.BlockedByPolicy;

        return NodalOsOnnxOcrOfflineReadinessDecision.ReadyForOnnxSyntheticRun;
    }

    private static NodalOsOnnxOcrOfflineReadinessRequirement Requirement(
        string id,
        string name,
        bool satisfied,
        string reason,
        bool blocks)
    {
        return new NodalOsOnnxOcrOfflineReadinessRequirement(
            $"req-{id}-{Guid.NewGuid():N}",
            name,
            satisfied,
            satisfied ? string.Empty : BrowserCredentialRedactor.Redact(reason),
            blocks);
    }

    private static bool IsDetectionModelVerified(NodalOsPaddleOcrOnnxModelManifest manifest, string repositoryRoot, bool licenseAccepted)
    {
        var verifier = new NodalOsPaddleOcrOnnxModelVerifierService();
        var detection = manifest.Models.FirstOrDefault(m => m.Kind == NodalOsPaddleOcrOnnxModelKind.TextDetection);
        if (detection is null)
            return false;
        return verifier.Verify(detection, repositoryRoot, licenseAccepted).Status == NodalOsPaddleOcrOnnxModelStatus.Verified;
    }

    private static bool IsRecognitionModelVerified(NodalOsPaddleOcrOnnxModelManifest manifest, string repositoryRoot, bool licenseAccepted)
    {
        var verifier = new NodalOsPaddleOcrOnnxModelVerifierService();
        var recognition = manifest.Models.FirstOrDefault(m => m.Kind == NodalOsPaddleOcrOnnxModelKind.TextRecognition);
        if (recognition is null)
            return false;
        return verifier.Verify(recognition, repositoryRoot, licenseAccepted).Status == NodalOsPaddleOcrOnnxModelStatus.Verified;
    }

    private static bool IsOnnxRuntimePackageAvailable()
    {
        try
        {
            _ = typeof(Microsoft.ML.OnnxRuntime.SessionOptions);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
