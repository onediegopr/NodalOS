using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.BrowserExecutor.Cdp;

// M205 — ONNX synthetic OCR readiness review.
// Evaluates the first end-to-end synthetic ONNX OCR run and decides whether to advance.
public sealed class NodalOsOnnxSyntheticOcrReadinessReview
{
    public NodalOsOnnxSyntheticOcrReport Evaluate(
        NodalOsOnnxOcrSyntheticInferenceResult inferenceResult,
        NodalOsOnnxOcrNormalizedResult normalizedResult,
        bool modelsVerified,
        bool sessionsLoaded)
    {
        if (inferenceResult is null) throw new ArgumentNullException(nameof(inferenceResult));
        if (normalizedResult is null) throw new ArgumentNullException(nameof(normalizedResult));

        var requirements = new List<NodalOsOnnxSyntheticOcrRequirement>();
        var warnings = new List<string>();

        requirements.Add(Req("models-verified", "Models verified", modelsVerified, "detection/recognition models verified", "models missing or unverified", blocks: true));
        requirements.Add(Req("sessions-loaded", "ONNX sessions load", sessionsLoaded, "ONNX Runtime sessions created", "session load failed", blocks: true));
        requirements.Add(Req("synthetic-crop", "Synthetic redacted crop prepared", inferenceResult.CallsRealOcr || inferenceResult.Status != NodalOsOnnxOcrInferenceStatus.ModelMissing, "redacted synthetic crop passed policy gate", "crop did not pass policy gate", blocks: true));
        requirements.Add(Req("detection-attempted", "Detection inference attempted", inferenceResult.DetectionResult.InferenceTimeMs.HasValue, "detection model executed", "detection not executed", blocks: false));
        requirements.Add(Req("recognition-attempted", "Recognition inference attempted if boxes exist", RecognitionAttempted(inferenceResult), "recognition executed where applicable", "recognition not executed", blocks: false));
        requirements.Add(Req("result-normalized", "Result normalized", true, "output normalized to internal OCR contract", "normalization missing", blocks: true));
        requirements.Add(Req("no-raw-persistence", "No raw persistence", !inferenceResult.RawPersisted && !normalizedResult.OriginalRawPersisted, "no raw image persisted", "raw persistence detected", blocks: true));
        requirements.Add(Req("no-full-screen", "No full-screen OCR", inferenceResult.Status != NodalOsOnnxOcrInferenceStatus.BlockedByFullScreen, "full-screen OCR blocked", "full-screen requested", blocks: true));
        requirements.Add(Req("no-sensitive", "No sensitive OCR", inferenceResult.Status != NodalOsOnnxOcrInferenceStatus.BlockedBySensitive, "sensitive OCR blocked", "sensitive surface requested", blocks: true));
        requirements.Add(Req("no-saas", "No SaaS OCR", !inferenceResult.CallsSaas && !normalizedResult.CallsSaas, "no SaaS call made", "SaaS call detected", blocks: true));
        requirements.Add(Req("no-authority", "OCR is not authority", inferenceResult.NoAuthority && normalizedResult.AuthorityFlag == NodalOsOcrAuthorityFlag.NoAuthority, "no-authority preserved", "authority violation", blocks: true));
        requirements.Add(Req("production-blocked", "Production public OCR blocked", !normalizedResult.ProductionEnabled, "production public OCR blocked", "production enabled", blocks: true));

        var blocked = requirements.Where(r => r.BlocksReadiness && !r.Satisfied).ToList();
        var decision = DetermineDecision(blocked, inferenceResult, normalizedResult, modelsVerified, sessionsLoaded);

        if (!modelsVerified)
            warnings.Add("models not verified; see paddleocr-onnx-model-verification-m197.md");

        if (!sessionsLoaded)
            warnings.Add("ONNX Runtime sessions failed to load");

        if (inferenceResult.Status == NodalOsOnnxOcrInferenceStatus.NoTextDetected)
            warnings.Add("detector found no text on synthetic fixture; more fixtures needed");

        if (inferenceResult.Status == NodalOsOnnxOcrInferenceStatus.LowConfidence)
            warnings.Add("low confidence detections; human review required");

        if (normalizedResult.RequiresHumanReview)
            warnings.Add("normalized result requires human review");

        return new NodalOsOnnxSyntheticOcrReport(
            $"onnx-readiness-{Guid.NewGuid():N}",
            decision,
            CanAttemptRedactedCropShadow(decision),
            CanContinueWithMoreFixtures(decision),
            requirements,
            modelsVerified,
            sessionsLoaded,
            inferenceResult.CallsRealOcr || inferenceResult.Status != NodalOsOnnxOcrInferenceStatus.ModelMissing,
            inferenceResult.DetectionResult.InferenceTimeMs.HasValue,
            RecognitionAttempted(inferenceResult),
            true,
            !inferenceResult.RawPersisted && !normalizedResult.OriginalRawPersisted,
            inferenceResult.Status != NodalOsOnnxOcrInferenceStatus.BlockedByFullScreen,
            inferenceResult.Status != NodalOsOnnxOcrInferenceStatus.BlockedBySensitive,
            !inferenceResult.CallsSaas && !normalizedResult.CallsSaas,
            inferenceResult.NoAuthority && normalizedResult.AuthorityFlag == NodalOsOcrAuthorityFlag.NoAuthority,
            !normalizedResult.ProductionEnabled,
            warnings,
            DateTimeOffset.UtcNow);
    }

    private static bool RecognitionAttempted(NodalOsOnnxOcrSyntheticInferenceResult inferenceResult)
    {
        if (inferenceResult.DetectionResult.TextBoxes.Count == 0)
            return true; // No boxes => recognition not required.
        return inferenceResult.RecognitionResults.Count > 0;
    }

    private static NodalOsOnnxSyntheticOcrReadinessDecision DetermineDecision(
        List<NodalOsOnnxSyntheticOcrRequirement> blocked,
        NodalOsOnnxOcrSyntheticInferenceResult inferenceResult,
        NodalOsOnnxOcrNormalizedResult normalizedResult,
        bool modelsVerified,
        bool sessionsLoaded)
    {
        if (!modelsVerified)
            return NodalOsOnnxSyntheticOcrReadinessDecision.BlockedByModelRuntime;

        if (!sessionsLoaded)
            return NodalOsOnnxSyntheticOcrReadinessDecision.BlockedByModelRuntime;

        if (inferenceResult.Status == NodalOsOnnxOcrInferenceStatus.BlockedByModelRuntime)
            return NodalOsOnnxSyntheticOcrReadinessDecision.BlockedByModelRuntime;

        if (blocked.Any(r => r.Name.Contains("pre-processing") || r.Name.Contains("crop")))
            return NodalOsOnnxSyntheticOcrReadinessDecision.BlockedByPreProcessing;

        if (blocked.Any(r => r.Name.Contains("post-processing") || r.Name.Contains("normalization")))
            return NodalOsOnnxSyntheticOcrReadinessDecision.BlockedByPostProcessing;

        if (blocked.Any(r => r.Name.Contains("raw") || r.Name.Contains("authority") || r.Name.Contains("saas") || r.Name.Contains("production")))
            return NodalOsOnnxSyntheticOcrReadinessDecision.BlockedByEvidenceRisk;

        if (inferenceResult.Status == NodalOsOnnxOcrInferenceStatus.NoTextDetected)
            return NodalOsOnnxSyntheticOcrReadinessDecision.ReadyForMoreSyntheticFixtures;

        if (inferenceResult.Status == NodalOsOnnxOcrInferenceStatus.LowConfidence || normalizedResult.RequiresHumanReview)
            return NodalOsOnnxSyntheticOcrReadinessDecision.BlockedByLowConfidence;

        if (blocked.Count > 0)
            return NodalOsOnnxSyntheticOcrReadinessDecision.NotReady;

        if (inferenceResult.DetectionResult.TextBoxes.Count > 0 &&
            inferenceResult.RecognitionResults.Any(r => r.Status == NodalOsOnnxOcrInferenceStatus.Success))
        {
            return NodalOsOnnxSyntheticOcrReadinessDecision.ReadyForRedactedCropShadow;
        }

        return NodalOsOnnxSyntheticOcrReadinessDecision.ReadyForMoreSyntheticFixtures;
    }

    private static bool CanAttemptRedactedCropShadow(NodalOsOnnxSyntheticOcrReadinessDecision decision)
    {
        return decision == NodalOsOnnxSyntheticOcrReadinessDecision.ReadyForRedactedCropShadow;
    }

    private static bool CanContinueWithMoreFixtures(NodalOsOnnxSyntheticOcrReadinessDecision decision)
    {
        return decision is NodalOsOnnxSyntheticOcrReadinessDecision.ReadyForMoreSyntheticFixtures
            or NodalOsOnnxSyntheticOcrReadinessDecision.ReadyForRedactedCropShadow;
    }

    private static NodalOsOnnxSyntheticOcrRequirement Req(
        string id,
        string name,
        bool satisfied,
        string evidence,
        string missingReason,
        bool blocks)
    {
        return new NodalOsOnnxSyntheticOcrRequirement(
            $"req-{id}-{Guid.NewGuid():N}",
            name,
            satisfied,
            BrowserCredentialRedactor.Redact(evidence),
            BrowserCredentialRedactor.Redact(missingReason),
            blocks);
    }
}
