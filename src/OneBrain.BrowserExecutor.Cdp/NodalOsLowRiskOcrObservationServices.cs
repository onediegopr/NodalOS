using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.BrowserExecutor.Cdp;

// M319-M321 — evidence-only OCR observation policy gate.
public sealed class NodalOsLowRiskOcrObservationEvaluator
{
    public NodalOsOcrObservationResult Evaluate(
        NodalOsOcrObservationRequest request,
        string recognizedText,
        string normalizedText,
        int detectorBoxesCount,
        int? editDistance,
        bool exactMatch,
        bool normalizedMatch,
        double? confidence,
        IReadOnlyList<string>? warnings = null)
    {
        if (request.RiskLevel != NodalOsOcrObservationRiskLevel.LowRiskOnly || !request.LowRiskOnly)
            return Reject(request, NodalOsOcrObservationDecision.RejectedNonLowRiskRequest, recognizedText, normalizedText, editDistance, confidence, exactMatch, normalizedMatch, warnings, "observation request is not low-risk only");

        if (request.AllowActions)
            return Reject(request, NodalOsOcrObservationDecision.RejectedActionsRequested, recognizedText, normalizedText, editDistance, confidence, exactMatch, normalizedMatch, warnings, "actions are blocked for OCR observation");

        if (request.AllowAuthority)
            return Reject(request, NodalOsOcrObservationDecision.RejectedAuthorityRequested, recognizedText, normalizedText, editDistance, confidence, exactMatch, normalizedMatch, warnings, "authority claims are blocked for OCR observation");

        if (request.ContainsSensitiveData)
            return Reject(request, NodalOsOcrObservationDecision.RejectedSensitiveData, recognizedText, normalizedText, editDistance, confidence, exactMatch, normalizedMatch, warnings, "sensitive OCR observations are blocked");

        if (request.ContainsDocumentData)
            return Reject(request, NodalOsOcrObservationDecision.RejectedDocumentData, recognizedText, normalizedText, editDistance, confidence, exactMatch, normalizedMatch, warnings, "document OCR observations are blocked in M319-M321");

        if (request.ContainsCredentials)
            return Reject(request, NodalOsOcrObservationDecision.RejectedCredentialData, recognizedText, normalizedText, editDistance, confidence, exactMatch, normalizedMatch, warnings, "credential/password OCR observations are blocked");

        if (request.FullScreen)
            return Reject(request, NodalOsOcrObservationDecision.RejectedFullScreen, recognizedText, normalizedText, editDistance, confidence, exactMatch, normalizedMatch, warnings, "full-screen OCR observation is blocked");

        if (request.RegionBounds.Width <= 0 || request.RegionBounds.Height <= 0)
            return Reject(request, NodalOsOcrObservationDecision.RejectedInvalidRegion, recognizedText, normalizedText, editDistance, confidence, exactMatch, normalizedMatch, warnings, "observation region bounds are invalid");

        if (request.SourceCategory is not (NodalOsOcrObservationSource.RealQaWindowRegion
            or NodalOsOcrObservationSource.InternalControlledScreenRegion
            or NodalOsOcrObservationSource.InternalControlledRealImage))
        {
            return Reject(request, NodalOsOcrObservationDecision.RejectedUnknownSource, recognizedText, normalizedText, editDistance, confidence, exactMatch, normalizedMatch, warnings, "unknown OCR observation source");
        }

        return new NodalOsOcrObservationResult(
            request.ObservationId,
            NodalOsOcrObservationDecision.AcceptedEvidenceOnly,
            Accepted: detectorBoxesCount >= 0,
            RecognizedText: recognizedText,
            NormalizedText: normalizedText,
            ExpectedText: request.ExpectedText,
            ExactMatch: exactMatch,
            NormalizedMatch: normalizedMatch,
            EditDistance: editDistance,
            Confidence: confidence,
            Warnings: warnings ?? [],
            NoAuthority: true,
            ActionAllowed: false,
            EvidenceOnly: true,
            RedactionStatus: "NoRedactionRequiredForControlledQaFixture",
            RawImagePersisted: false,
            ModelFamily: "PaddleOCR-ONNX",
            DictionaryPolicy: "OfficialSpaceToken",
            OfficialSpacePolicy: true,
            SoftmaxReapplied: false);
    }

    private static NodalOsOcrObservationResult Reject(
        NodalOsOcrObservationRequest request,
        NodalOsOcrObservationDecision decision,
        string recognizedText,
        string normalizedText,
        int? editDistance,
        double? confidence,
        bool exactMatch,
        bool normalizedMatch,
        IReadOnlyList<string>? warnings,
        string reason) =>
        new(
            request.ObservationId,
            decision,
            Accepted: false,
            RecognizedText: recognizedText,
            NormalizedText: normalizedText,
            ExpectedText: request.ExpectedText,
            ExactMatch: exactMatch,
            NormalizedMatch: normalizedMatch,
            EditDistance: editDistance,
            Confidence: confidence,
            Warnings: (warnings ?? []).Concat([BrowserCredentialRedactor.Redact(reason)]).ToArray(),
            NoAuthority: true,
            ActionAllowed: false,
            EvidenceOnly: true,
            RedactionStatus: "NoRedactionRequiredForControlledQaFixture",
            RawImagePersisted: false,
            ModelFamily: "PaddleOCR-ONNX",
            DictionaryPolicy: "OfficialSpaceToken",
            OfficialSpacePolicy: true,
            SoftmaxReapplied: false);
}
