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
        IReadOnlyList<string>? warnings = null,
        NodalOsOcrRegionVerificationResult? verificationResult = null,
        NodalOsOcrObservationConfidenceGateResult? confidenceGateResult = null,
        NodalOsOcrObservationAcceptanceState? acceptanceState = null,
        string? rejectionReason = null)
    {
        if (request.RiskLevel != NodalOsOcrObservationRiskLevel.LowRiskOnly || !request.LowRiskOnly)
            return Reject(request, NodalOsOcrObservationDecision.RejectedNonLowRiskRequest, recognizedText, normalizedText, editDistance, confidence, exactMatch, normalizedMatch, warnings, "observation request is not low-risk only", verificationResult, confidenceGateResult, acceptanceState, rejectionReason);

        if (request.AllowActions)
            return Reject(request, NodalOsOcrObservationDecision.RejectedActionsRequested, recognizedText, normalizedText, editDistance, confidence, exactMatch, normalizedMatch, warnings, "actions are blocked for OCR observation", verificationResult, confidenceGateResult, acceptanceState, rejectionReason);

        if (request.AllowAuthority)
            return Reject(request, NodalOsOcrObservationDecision.RejectedAuthorityRequested, recognizedText, normalizedText, editDistance, confidence, exactMatch, normalizedMatch, warnings, "authority claims are blocked for OCR observation", verificationResult, confidenceGateResult, acceptanceState, rejectionReason);

        if (request.ContainsSensitiveData)
            return Reject(request, NodalOsOcrObservationDecision.RejectedSensitiveData, recognizedText, normalizedText, editDistance, confidence, exactMatch, normalizedMatch, warnings, "sensitive OCR observations are blocked", verificationResult, confidenceGateResult, acceptanceState, rejectionReason);

        if (request.ContainsDocumentData)
            return Reject(request, NodalOsOcrObservationDecision.RejectedDocumentData, recognizedText, normalizedText, editDistance, confidence, exactMatch, normalizedMatch, warnings, "document OCR observations are blocked in M319-M321", verificationResult, confidenceGateResult, acceptanceState, rejectionReason);

        if (request.ContainsCredentials)
            return Reject(request, NodalOsOcrObservationDecision.RejectedCredentialData, recognizedText, normalizedText, editDistance, confidence, exactMatch, normalizedMatch, warnings, "credential/password OCR observations are blocked", verificationResult, confidenceGateResult, acceptanceState, rejectionReason);

        if (request.FullScreen)
            return Reject(request, NodalOsOcrObservationDecision.RejectedFullScreen, recognizedText, normalizedText, editDistance, confidence, exactMatch, normalizedMatch, warnings, "full-screen OCR observation is blocked", verificationResult, confidenceGateResult, acceptanceState, rejectionReason);

        if (request.RegionBounds.Width <= 0 || request.RegionBounds.Height <= 0)
            return Reject(request, NodalOsOcrObservationDecision.RejectedInvalidRegion, recognizedText, normalizedText, editDistance, confidence, exactMatch, normalizedMatch, warnings, "observation region bounds are invalid", verificationResult, confidenceGateResult, acceptanceState, rejectionReason);

        if (request.SourceCategory is not (NodalOsOcrObservationSource.RealQaWindowRegion
            or NodalOsOcrObservationSource.InternalControlledScreenRegion
            or NodalOsOcrObservationSource.InternalControlledRealImage))
        {
            return Reject(request, NodalOsOcrObservationDecision.RejectedUnknownSource, recognizedText, normalizedText, editDistance, confidence, exactMatch, normalizedMatch, warnings, "unknown OCR observation source", verificationResult, confidenceGateResult, acceptanceState, rejectionReason);
        }

        var resolvedAcceptance = acceptanceState ?? NodalOsOcrObservationAcceptanceState.AcceptedEvidence;
        var accepted = resolvedAcceptance == NodalOsOcrObservationAcceptanceState.AcceptedEvidence &&
                       detectorBoxesCount >= 0 &&
                       (verificationResult?.RegionVerified ?? true) &&
                       (confidenceGateResult?.Passed ?? true);

        return BuildResult(
            request.ObservationId,
            NodalOsOcrObservationDecision.AcceptedEvidenceOnly,
            Accepted: accepted,
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
            SoftmaxReapplied: false,
            verificationResult,
            confidenceGateResult,
            resolvedAcceptance,
            rejectionReason);
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
        string reason,
        NodalOsOcrRegionVerificationResult? verificationResult,
        NodalOsOcrObservationConfidenceGateResult? confidenceGateResult,
        NodalOsOcrObservationAcceptanceState? acceptanceState,
        string? rejectionReason) =>
        BuildResult(
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
            SoftmaxReapplied: false,
            verificationResult,
            confidenceGateResult,
            acceptanceState ?? MapAcceptance(decision, verificationResult, confidenceGateResult),
            rejectionReason ?? BrowserCredentialRedactor.Redact(reason));

    private static NodalOsOcrObservationResult BuildResult(
        string observationId,
        NodalOsOcrObservationDecision policyDecision,
        bool Accepted,
        string RecognizedText,
        string NormalizedText,
        string? ExpectedText,
        bool ExactMatch,
        bool NormalizedMatch,
        int? EditDistance,
        double? Confidence,
        IReadOnlyList<string> Warnings,
        bool NoAuthority,
        bool ActionAllowed,
        bool EvidenceOnly,
        string RedactionStatus,
        bool RawImagePersisted,
        string ModelFamily,
        string DictionaryPolicy,
        bool OfficialSpacePolicy,
        bool SoftmaxReapplied,
        NodalOsOcrRegionVerificationResult? verificationResult,
        NodalOsOcrObservationConfidenceGateResult? confidenceGateResult,
        NodalOsOcrObservationAcceptanceState acceptanceState,
        string? rejectionReason)
    {
        return new NodalOsOcrObservationResult(
            observationId,
            policyDecision,
            Accepted,
            RecognizedText,
            NormalizedText,
            ExpectedText,
            ExactMatch,
            NormalizedMatch,
            EditDistance,
            Confidence,
            Warnings,
            NoAuthority,
            ActionAllowed,
            EvidenceOnly,
            RedactionStatus,
            RawImagePersisted,
            ModelFamily,
            DictionaryPolicy,
            OfficialSpacePolicy,
            SoftmaxReapplied)
        {
            RegionVerified = verificationResult?.RegionVerified ?? false,
            IsolationState = verificationResult?.IsolationState ?? NodalOsWindowIsolationState.NotEvaluated,
            ConfidenceGatePassed = confidenceGateResult?.Passed ?? false,
            AcceptanceState = acceptanceState,
            RejectionReason = rejectionReason ?? string.Empty,
            DiffScore = verificationResult?.DiffScore,
            CaptureFingerprint = verificationResult?.ObservedFingerprint,
            RegionVerification = verificationResult,
            ConfidenceGate = confidenceGateResult
        };
    }

    private static NodalOsOcrObservationAcceptanceState MapAcceptance(
        NodalOsOcrObservationDecision decision,
        NodalOsOcrRegionVerificationResult? verificationResult,
        NodalOsOcrObservationConfidenceGateResult? confidenceGateResult)
    {
        if (decision == NodalOsOcrObservationDecision.RejectedFullScreen)
            return NodalOsOcrObservationAcceptanceState.RejectedFullScreen;
        if (decision == NodalOsOcrObservationDecision.RejectedSensitiveData ||
            decision == NodalOsOcrObservationDecision.RejectedDocumentData ||
            decision == NodalOsOcrObservationDecision.RejectedCredentialData)
        {
            return NodalOsOcrObservationAcceptanceState.RejectedSensitive;
        }

        if (verificationResult is not null)
        {
            return verificationResult.IsolationState switch
            {
                NodalOsWindowIsolationState.WrongWindowDetected => NodalOsOcrObservationAcceptanceState.RejectedWrongWindow,
                NodalOsWindowIsolationState.VisibleButNotForeground => NodalOsOcrObservationAcceptanceState.RejectedNotForeground,
                NodalOsWindowIsolationState.BoundsMismatch => NodalOsOcrObservationAcceptanceState.RejectedBoundsMismatch,
                _ when confidenceGateResult is { Attempted: true, Passed: false } => NodalOsOcrObservationAcceptanceState.RejectedLowConfidence,
                _ => NodalOsOcrObservationAcceptanceState.UncertainRequiresExpansion
            };
        }

        return confidenceGateResult is { Attempted: true, Passed: false }
            ? NodalOsOcrObservationAcceptanceState.RejectedLowConfidence
            : NodalOsOcrObservationAcceptanceState.UncertainRequiresExpansion;
    }
}
