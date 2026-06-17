using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.BrowserExecutor.Cdp;

// M204 — ONNX OCR result normalizer.
// Maps detection/recognition outputs into redacted text blocks, evidence summary, timeline card, and human review policy.
public sealed class NodalOsOnnxOcrResultNormalizer
{
    private readonly NodalOsOnnxOcrTextBlockMapper _blockMapper = new();
    private readonly NodalOsOnnxOcrHumanReviewPolicy _humanReviewPolicy = new();
    private readonly NodalOsOnnxOcrEvidenceBuilder _evidenceBuilder = new();
    private readonly NodalOsOnnxOcrTimelineAdapter _timelineAdapter = new();

    public NodalOsOnnxOcrNormalizedResult Normalize(
        NodalOsOnnxOcrSyntheticInferenceResult inferenceResult,
        NodalOsPixelRedactionResult redactionResult)
    {
        var textBlocks = _blockMapper.Map(inferenceResult.DetectionResult, inferenceResult.RecognitionResults);
        var humanReview = _humanReviewPolicy.Evaluate(inferenceResult, textBlocks);
        var status = MapStatus(inferenceResult.Status, humanReview.RequiresHumanReview);
        var confidence = ComputeConfidence(textBlocks);

        var providerId = new NodalOsOcrVisionProviderId("local-paddleocr-onnx");
        var warnings = new List<string>(inferenceResult.Warnings)
        {
            "ONNX OCR result is informational only; never authoritative",
            "production public OCR remains blocked"
        };

        if (humanReview.RequiresHumanReview)
        {
            warnings.Add("human review required");
        }

        var evidenceRefs = new List<NodalOsGroundingEvidenceRef>
        {
            new($"ev-{Guid.NewGuid():N}", redactionResult.Evidence.EvidenceId, Redacted: true)
        };

        return new NodalOsOnnxOcrNormalizedResult(
            $"norm-{Guid.NewGuid():N}",
            providerId,
            inferenceResult.Evidence.ModelManifestRefs.FirstOrDefault() ?? "paddleocr-onnx",
            inferenceResult.DetectionResult.ModelVersion,
            inferenceResult.RecognitionResults.FirstOrDefault()?.ModelVersion ?? "n/a",
            status,
            textBlocks,
            confidence,
            NodalOsOcrLanguage.English,
            warnings,
            evidenceRefs,
            new NodalOsOcrRedactionSummary(
                NodalOsGroundingRedactionStatus.RedactedSafe,
                ScreenshotSafe: true,
                CropRedacted: true,
                ContainsSensitive: false,
                "redacted synthetic crop; no raw persistence"),
            NodalOsOcrAuthorityFlag.NoAuthority,
            humanReview.RequiresHumanReview,
            CanApproveAction: false,
            CanClick: false,
            CanSubmit: false,
            CallsExternalApi: false,
            CallsSaas: false,
            ProductionEnabled: false,
            OriginalRawPersisted: false,
            NoAuthority: true,
            Redacted: true);
    }

    public NodalOsOnnxOcrEvidenceSummary BuildEvidenceSummary(
        NodalOsOnnxOcrSyntheticInferenceResult inferenceResult,
        NodalOsPixelRedactionResult redactionResult) =>
        _evidenceBuilder.Build(inferenceResult, redactionResult);

    public NodalOsOnnxOcrTimelineEvidenceCard AdaptToTimeline(
        NodalOsOnnxOcrNormalizedResult normalizedResult) =>
        _timelineAdapter.Adapt(normalizedResult);

    private static NodalOsOnnxOcrNormalizedStatus MapStatus(NodalOsOnnxOcrInferenceStatus status, bool requiresHumanReview)
    {
        var baseStatus = status switch
        {
            NodalOsOnnxOcrInferenceStatus.Success => NodalOsOnnxOcrNormalizedStatus.Completed,
            NodalOsOnnxOcrInferenceStatus.NoTextDetected => NodalOsOnnxOcrNormalizedStatus.NoTextDetected,
            NodalOsOnnxOcrInferenceStatus.LowConfidence => NodalOsOnnxOcrNormalizedStatus.LowConfidence,
            NodalOsOnnxOcrInferenceStatus.BlockedByPolicy => NodalOsOnnxOcrNormalizedStatus.BlockedByPolicy,
            NodalOsOnnxOcrInferenceStatus.BlockedBySensitive => NodalOsOnnxOcrNormalizedStatus.BlockedByPolicy,
            NodalOsOnnxOcrInferenceStatus.BlockedByFullScreen => NodalOsOnnxOcrNormalizedStatus.BlockedByPolicy,
            NodalOsOnnxOcrInferenceStatus.BlockedByRawPersistence => NodalOsOnnxOcrNormalizedStatus.BlockedByPolicy,
            NodalOsOnnxOcrInferenceStatus.BlockedByRedaction => NodalOsOnnxOcrNormalizedStatus.BlockedByPolicy,
            NodalOsOnnxOcrInferenceStatus.ModelMissing => NodalOsOnnxOcrNormalizedStatus.ModelMissing,
            NodalOsOnnxOcrInferenceStatus.ModelUnverified => NodalOsOnnxOcrNormalizedStatus.ModelUnverified,
            NodalOsOnnxOcrInferenceStatus.PreProcessingFailed => NodalOsOnnxOcrNormalizedStatus.PreProcessingFailed,
            NodalOsOnnxOcrInferenceStatus.DetectionFailed => NodalOsOnnxOcrNormalizedStatus.DetectionFailed,
            NodalOsOnnxOcrInferenceStatus.RecognitionFailed => NodalOsOnnxOcrNormalizedStatus.RecognitionFailed,
            _ => NodalOsOnnxOcrNormalizedStatus.Failed
        };

        if (requiresHumanReview && baseStatus is NodalOsOnnxOcrNormalizedStatus.Completed or NodalOsOnnxOcrNormalizedStatus.LowConfidence)
            return NodalOsOnnxOcrNormalizedStatus.RequiresHumanReview;

        return baseStatus;
    }

    private static NodalOsOcrConfidence ComputeConfidence(IReadOnlyList<NodalOsOnnxOcrNormalizedTextBlock> blocks)
    {
        if (blocks.Count == 0)
            return new NodalOsOcrConfidence(0.0);

        return new NodalOsOcrConfidence(blocks.Average(b => b.Confidence.Value));
    }
}

public sealed class NodalOsOnnxOcrTextBlockMapper
{
    public IReadOnlyList<NodalOsOnnxOcrNormalizedTextBlock> Map(
        NodalOsOnnxOcrDetectionInferenceResult detection,
        IReadOnlyList<NodalOsOnnxOcrRecognitionInferenceResult> recognitions)
    {
        var blocks = new List<NodalOsOnnxOcrNormalizedTextBlock>();
        var recByBox = new Dictionary<string, NodalOsOnnxOcrRecognitionInferenceResult>();

        for (var i = 0; i < recognitions.Count && i < detection.TextBoxes.Count; i++)
        {
            recByBox[detection.TextBoxes[i].BoxId] = recognitions[i];
        }

        foreach (var box in detection.TextBoxes.Where(b => b.Valid))
        {
            var rec = recByBox.GetValueOrDefault(box.BoxId);
            var candidate = rec?.Candidates.FirstOrDefault();
            var text = candidate?.Text ?? "[unrecognized]";
            var confidence = candidate?.Confidence ?? box.Confidence;
            var lowConfidence = candidate?.LowConfidence ?? confidence < 0.6;

            blocks.Add(new NodalOsOnnxOcrNormalizedTextBlock(
                box.BoxId,
                BrowserCredentialRedactor.Redact(text),
                new NodalOsOcrBoundingBox(box.CropX, box.CropY, box.CropWidth, box.CropHeight),
                new NodalOsOcrConfidence(confidence),
                NodalOsOcrLanguage.English,
                lowConfidence,
                Redacted: true));
        }

        return blocks;
    }
}

public sealed class NodalOsOnnxOcrHumanReviewPolicy
{
    public NodalOsOnnxOcrHumanReviewDecision Evaluate(
        NodalOsOnnxOcrSyntheticInferenceResult inferenceResult,
        IReadOnlyList<NodalOsOnnxOcrNormalizedTextBlock> textBlocks)
    {
        var rules = new List<NodalOsOnnxOcrHumanReviewRule>
        {
            Rule("no-text", "No text detected", inferenceResult.Status == NodalOsOnnxOcrInferenceStatus.NoTextDetected, "detector found no text regions"),
            Rule("low-confidence", "Low confidence", inferenceResult.Status == NodalOsOnnxOcrInferenceStatus.LowConfidence || textBlocks.Any(b => b.LowConfidence), "one or more text blocks have low confidence"),
            Rule("recognition-failed", "Recognition failure", inferenceResult.Status == NodalOsOnnxOcrInferenceStatus.RecognitionFailed, "all recognition attempts failed"),
            Rule("ambiguous-boxes", "Ambiguous boxes", inferenceResult.DetectionResult.TextBoxes.Count > 50, "unusually high box count"),
            Rule("sensitive-indication", "Sensitive indication", inferenceResult.Status == NodalOsOnnxOcrInferenceStatus.BlockedBySensitive, "sensitive surface blocked"),
            Rule("model-mismatch", "Model mismatch", inferenceResult.Status == NodalOsOnnxOcrInferenceStatus.ModelUnverified, "model verification failed")
        };

        var triggered = rules.Where(r => r.Triggered).ToList();
        var requiresHumanReview = triggered.Count > 0;
        var reason = requiresHumanReview
            ? string.Join("; ", triggered.Select(r => r.Reason))
            : "no human review triggers";

        return new NodalOsOnnxOcrHumanReviewDecision(
            $"hr-{Guid.NewGuid():N}",
            requiresHumanReview,
            rules,
            BrowserCredentialRedactor.Redact(reason));
    }

    private static NodalOsOnnxOcrHumanReviewRule Rule(string id, string name, bool triggered, string reason)
    {
        return new NodalOsOnnxOcrHumanReviewRule(
            $"hr-rule-{id}-{Guid.NewGuid():N}",
            name,
            triggered,
            BrowserCredentialRedactor.Redact(reason));
    }
}

public sealed class NodalOsOnnxOcrEvidenceBuilder
{
    public NodalOsOnnxOcrEvidenceSummary Build(
        NodalOsOnnxOcrSyntheticInferenceResult inferenceResult,
        NodalOsPixelRedactionResult redactionResult)
    {
        var textBlockCount = inferenceResult.DetectionResult.TextBoxes.Count;
        var avgConfidence = textBlockCount > 0
            ? inferenceResult.RecognitionResults.SelectMany(r => r.Candidates).DefaultIfEmpty().Average(c => c?.Confidence ?? 0.0)
            : 0.0;

        return new NodalOsOnnxOcrEvidenceSummary(
            $"ev-summary-{Guid.NewGuid():N}",
            redactionResult.RedactedImageHash,
            inferenceResult.Evidence.ModelManifestRefs,
            inferenceResult.Evidence.TotalInferenceTimeMs,
            textBlockCount,
            avgConfidence,
            OriginalRawPersisted: false,
            CallsSaas: false,
            ProductionEnabled: false,
            NoAuthority: true,
            $"ONNX synthetic OCR: {textBlockCount} text blocks; avg confidence {avgConfidence:F2}; redacted");
    }
}

public sealed class NodalOsOnnxOcrTimelineAdapter
{
    public NodalOsOnnxOcrTimelineEvidenceCard Adapt(NodalOsOnnxOcrNormalizedResult normalizedResult)
    {
        return new NodalOsOnnxOcrTimelineEvidenceCard(
            $"timeline-{Guid.NewGuid():N}",
            normalizedResult.ResultId,
            $"ONNX OCR {normalizedResult.Status}: {normalizedResult.TextBlocks.Count} block(s); human review={normalizedResult.RequiresHumanReview}",
            normalizedResult.EvidenceRefs,
            DateTimeOffset.UtcNow,
            normalizedResult.CallsSaas,
            normalizedResult.ProductionEnabled,
            normalizedResult.OriginalRawPersisted,
            normalizedResult.AuthorityFlag == NodalOsOcrAuthorityFlag.NoAuthority,
            normalizedResult.Redacted);
    }
}
