using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.BrowserExecutor.Cdp;

public sealed class NodalOsOcrEvidenceAdapter
{
    public NodalOsOcrEvidencePolicyEvaluation EvaluateAndMap(
        NodalOsOcrEvidenceEnvelope envelope,
        string? artifactRef = null)
    {
        var policyViolation = ValidateEnvelope(envelope);
        if (policyViolation is not null)
        {
            return new NodalOsOcrEvidencePolicyEvaluation(
                NodalOsOcrEvidenceLedgerStatus.RejectedPolicyViolation,
                CanRegister: false,
                CanBeAcceptedEvidence: false,
                CanAttachAsAuxiliaryEvidence: false,
                CanAttachAsDiagnosticEvidence: false,
                CanAuthorizeAction: false,
                CanApproveClick: false,
                CanApproveSubmit: false,
                CanApproveSend: false,
                CanApproveDelete: false,
                CanApprovePay: false,
                CanApproveSign: false,
                BrowserCredentialRedactor.Redact(policyViolation),
                Entry: null);
        }

        var acceptance = envelope.AcceptanceState;
        if (acceptance == NodalOsOcrObservationAcceptanceState.AcceptedEvidence &&
            (!envelope.RegionVerified || !envelope.ConfidenceGatePassed || !envelope.Result.Accepted))
        {
            return new NodalOsOcrEvidencePolicyEvaluation(
                NodalOsOcrEvidenceLedgerStatus.RejectedPolicyViolation,
                CanRegister: false,
                CanBeAcceptedEvidence: false,
                CanAttachAsAuxiliaryEvidence: false,
                CanAttachAsDiagnosticEvidence: false,
                CanAuthorizeAction: false,
                CanApproveClick: false,
                CanApproveSubmit: false,
                CanApproveSend: false,
                CanApproveDelete: false,
                CanApprovePay: false,
                CanApproveSign: false,
                BrowserCredentialRedactor.Redact("accepted OCR evidence requires accepted result, region verification, and confidence gate"),
                Entry: null);
        }

        var entry = BuildEntry(envelope, artifactRef);
        return acceptance switch
        {
            NodalOsOcrObservationAcceptanceState.AcceptedEvidence => BuildAccepted(entry),
            NodalOsOcrObservationAcceptanceState.UncertainRequiresExpansion => BuildDiagnostic(
                entry with { EvidenceUse = NodalOsOcrEvidenceUse.DiagnosticOnly },
                NodalOsOcrEvidenceLedgerStatus.RecordedDiagnosticUncertain,
                "OCR uncertain evidence recorded as diagnostic only"),
            _ => BuildDiagnostic(
                entry with { EvidenceUse = NodalOsOcrEvidenceUse.DiagnosticOnly },
                NodalOsOcrEvidenceLedgerStatus.RecordedDiagnosticRejected,
                "OCR rejected evidence recorded as diagnostic only")
        };
    }

    private static string? ValidateEnvelope(NodalOsOcrEvidenceEnvelope envelope)
    {
        if (envelope.Result.ActionAllowed)
            return "OCR evidence cannot set actionAllowed=true";
        if (!envelope.Result.NoAuthority)
            return "OCR evidence must remain no-authority";
        if (!envelope.Result.EvidenceOnly)
            return "OCR evidence must remain evidence-only";
        if (envelope.Result.SoftmaxReapplied)
            return "OCR evidence cannot come from softmax reapplied output";
        if (!envelope.Result.OfficialSpacePolicy)
            return "OCR evidence must preserve official space policy";
        if (string.IsNullOrWhiteSpace(envelope.ObservationId))
            return "observationId is required";
        if (envelope.RegionBounds.Width <= 0 || envelope.RegionBounds.Height <= 0)
            return "region bounds are required";

        return null;
    }

    private static NodalOsOcrEvidenceLedgerEntry BuildEntry(
        NodalOsOcrEvidenceEnvelope envelope,
        string? artifactRef)
    {
        return new NodalOsOcrEvidenceLedgerEntry(
            envelope.ObservationId,
            $"ocr-evidence:{envelope.ObservationId}",
            envelope.Timestamp,
            NodalOsOcrEvidenceSource.Ocr,
            NodalOsOcrEvidenceKind.OcrObservationEvidence,
            envelope.AcceptanceState == NodalOsOcrObservationAcceptanceState.AcceptedEvidence
                ? NodalOsOcrEvidenceUse.AuxiliaryOnly
                : NodalOsOcrEvidenceUse.DiagnosticOnly,
            NodalOsOcrEvidenceAuthority.NoAuthority,
            envelope.SourceCategory,
            envelope.CaptureMode,
            envelope.WindowTitleOrSource,
            envelope.ProcessOrSource,
            envelope.RegionBounds,
            envelope.Result.RecognizedText,
            envelope.Result.NormalizedText,
            envelope.Result.ExpectedText,
            envelope.Result.ExactMatch,
            envelope.Result.NormalizedMatch,
            envelope.Result.EditDistance,
            envelope.Result.Confidence,
            envelope.RegionVerified,
            envelope.ConfidenceGatePassed,
            envelope.AcceptanceState,
            envelope.RejectionReason,
            envelope.Result.NoAuthority,
            envelope.Result.EvidenceOnly,
            envelope.Result.ActionAllowed,
            envelope.Result.SoftmaxReapplied,
            envelope.Result.OfficialSpacePolicy,
            envelope.Result.ModelFamily,
            envelope.Result.DictionaryPolicy,
            envelope.Result.RawImagePersisted,
            envelope.Result.RedactionStatus,
            envelope.Result.Warnings,
            artifactRef,
            envelope.Result.RegionVerification,
            envelope.Result.ConfidenceGate,
            envelope.CaptureFingerprint);
    }

    private static NodalOsOcrEvidencePolicyEvaluation BuildAccepted(NodalOsOcrEvidenceLedgerEntry entry) =>
        new(
            NodalOsOcrEvidenceLedgerStatus.AcceptedAuxiliary,
            CanRegister: true,
            CanBeAcceptedEvidence: true,
            CanAttachAsAuxiliaryEvidence: true,
            CanAttachAsDiagnosticEvidence: false,
            CanAuthorizeAction: false,
            CanApproveClick: false,
            CanApproveSubmit: false,
            CanApproveSend: false,
            CanApproveDelete: false,
            CanApprovePay: false,
            CanApproveSign: false,
            "accepted OCR evidence can attach as auxiliary evidence only",
            entry);

    private static NodalOsOcrEvidencePolicyEvaluation BuildDiagnostic(
        NodalOsOcrEvidenceLedgerEntry entry,
        NodalOsOcrEvidenceLedgerStatus status,
        string reason) =>
        new(
            status,
            CanRegister: true,
            CanBeAcceptedEvidence: false,
            CanAttachAsAuxiliaryEvidence: false,
            CanAttachAsDiagnosticEvidence: true,
            CanAuthorizeAction: false,
            CanApproveClick: false,
            CanApproveSubmit: false,
            CanApproveSend: false,
            CanApproveDelete: false,
            CanApprovePay: false,
            CanApproveSign: false,
            reason,
            entry);
}

public sealed class NodalOsOcrEvidenceRuntimePolicyGate
{
    private readonly NodalOsOcrEvidenceAdapter adapter = new();

    public NodalOsOcrEvidencePolicyEvaluation Evaluate(
        NodalOsOcrEvidenceEnvelope envelope,
        string? artifactRef = null) =>
        adapter.EvaluateAndMap(envelope, artifactRef);
}
