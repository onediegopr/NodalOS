using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.BrowserExecutor.Cdp;

public sealed class NodalOsAssistedVerificationPolicy
{
    private const double AcceptedDiffThreshold = 0.01d;

    public IReadOnlyList<NodalOsAssistedVerificationSignal> CreateSignals(NodalOsOcrFsmObservationResult observation)
    {
        var signals = new List<NodalOsAssistedVerificationSignal>();

        signals.AddRange(observation.ObservationContext.Select(r => new NodalOsAssistedVerificationSignal(
            SignalId: r.ObservationId,
            Kind: NodalOsAssistedVerificationSignalKind.OcrAuxiliaryEvidence,
            SupportsVerification: true,
            DiagnosticOnly: false,
            Rejected: false,
            Source: "OcrObservationContext",
            ExpectedText: r.ExpectedText,
            ObservedText: r.RecognizedText,
            NormalizedText: r.NormalizedText,
            ExactMatch: r.ExactMatch,
            NormalizedMatch: r.NormalizedMatch,
            EditDistance: r.EditDistance,
            ConfidenceBand: r.ConfidenceBand,
            RegionVerified: r.RegionVerified,
            ConfidenceGatePassed: r.ConfidenceGatePassed,
            FingerprintHashMatch: r.FingerprintHashMatch,
            DiffScore: r.DiffScore,
            NoAuthority: true,
            EvidenceOnly: true,
            ActionAllowed: false,
            Reason: r.Reason,
            SourceCategory: r.SourceCategory,
            CaptureMode: r.CaptureMode,
            WindowTitleOrSource: r.WindowTitleOrSource,
            ProcessOrSource: r.ProcessOrSource,
            RegionBounds: r.RegionBounds)));

        signals.AddRange(observation.Diagnostics.Select(r => new NodalOsAssistedVerificationSignal(
            SignalId: r.ObservationId,
            Kind: r.LedgerStatus == NodalOsOcrEvidenceLedgerStatus.RecordedDiagnosticRejected
                ? NodalOsAssistedVerificationSignalKind.Rejected
                : NodalOsAssistedVerificationSignalKind.DiagnosticOnly,
            SupportsVerification: false,
            DiagnosticOnly: true,
            Rejected: r.LedgerStatus == NodalOsOcrEvidenceLedgerStatus.RecordedDiagnosticRejected,
            Source: "OcrDiagnostics",
            ExpectedText: r.ExpectedText,
            ObservedText: r.RecognizedText,
            NormalizedText: r.NormalizedText,
            ExactMatch: r.ExactMatch,
            NormalizedMatch: r.NormalizedMatch,
            EditDistance: r.EditDistance,
            ConfidenceBand: r.ConfidenceBand,
            RegionVerified: r.RegionVerified,
            ConfidenceGatePassed: r.ConfidenceGatePassed,
            FingerprintHashMatch: r.FingerprintHashMatch,
            DiffScore: r.DiffScore,
            NoAuthority: true,
            EvidenceOnly: true,
            ActionAllowed: false,
            Reason: r.Reason,
            SourceCategory: r.SourceCategory,
            CaptureMode: r.CaptureMode,
            WindowTitleOrSource: r.WindowTitleOrSource,
            ProcessOrSource: r.ProcessOrSource,
            RegionBounds: r.RegionBounds)));

        return signals;
    }

    public NodalOsAssistedVerificationResult Evaluate(NodalOsAssistedVerificationRequest request)
    {
        if (request.ActionRequested || request.ApprovalRequested)
            return Reject(request, NodalOsAssistedVerificationDecision.RejectedActionRequest, "assisted verification cannot request action or approval");
        if (request.ContainsSensitiveData || request.ContainsCredentials)
            return Reject(request, NodalOsAssistedVerificationDecision.RejectedSensitive, "sensitive or credential-bearing request is not allowed");
        if (request.ContainsDocumentData)
            return Reject(request, NodalOsAssistedVerificationDecision.RejectedDocument, "document-bearing request is not allowed");
        if (request.FullScreen)
            return Reject(request, NodalOsAssistedVerificationDecision.RejectedFullScreen, "full-screen request is not allowed");
        if (request.RiskLevel != NodalOsAssistedVerificationRiskLevel.Low || !request.LowRiskOnly)
            return Reject(request, NodalOsAssistedVerificationDecision.RejectedRiskTooHigh, "assisted verification is limited to low-risk requests");

        var ocrSignals = request.Signals.Where(s => s.Kind == NodalOsAssistedVerificationSignalKind.OcrAuxiliaryEvidence).ToArray();
        var rejectedSignals = request.Signals.Where(s => s.Kind == NodalOsAssistedVerificationSignalKind.Rejected).ToArray();
        var uncertainSignals = request.Signals.Where(s => s.Kind == NodalOsAssistedVerificationSignalKind.DiagnosticOnly).ToArray();
        var nonOcrSignals = request.Signals.Where(IsNonOcrCorroboratingSignal).ToArray();

        if (request.Signals.Any(s => s.Kind == NodalOsAssistedVerificationSignalKind.Rejected && s.Source.Contains("policy", StringComparison.OrdinalIgnoreCase)))
            return RejectWithDiagnostics(request, NodalOsAssistedVerificationDecision.RejectedPolicyViolation, request.Signals.Where(s => s.Kind == NodalOsAssistedVerificationSignalKind.Rejected).ToArray(), "policy-violation OCR signals are excluded from assisted verification");

        if (rejectedSignals.Length > 0 && ocrSignals.Length == 0)
            return RejectWithDiagnostics(request, NodalOsAssistedVerificationDecision.NotVerified, rejectedSignals, "rejected OCR evidence cannot support verification");
        if (uncertainSignals.Length > 0 && ocrSignals.Length == 0)
            return RejectWithDiagnostics(request, NodalOsAssistedVerificationDecision.NeedsMoreEvidence, uncertainSignals, "uncertain OCR evidence cannot support verification");

        if (ocrSignals.Length == 0)
            return Reject(request, NodalOsAssistedVerificationDecision.NotVerified, "at least one OCR auxiliary signal is required");

        if (ocrSignals.Any(s => !CanUseOcrSignal(s)))
            return RejectWithDiagnostics(request, NodalOsAssistedVerificationDecision.NotVerified, ocrSignals, "only accepted OCR auxiliary signals with verified region and confidence gate may support verification");

        if (nonOcrSignals.Length == 0)
            return RejectWithDiagnostics(request, NodalOsAssistedVerificationDecision.RejectedOcrOnly, ocrSignals, "assisted verification requires at least one non-OCR corroborating signal");

        if (!SignalsCorroborate(ocrSignals, nonOcrSignals))
            return NeedsMoreEvidence(request, ocrSignals.Concat(nonOcrSignals).ToArray(), "OCR and non-OCR signals do not corroborate the same expected value");

        var supportingSignals = ocrSignals.Concat(nonOcrSignals).ToArray();
        return new NodalOsAssistedVerificationResult(
            ResultId: $"assisted-verification:{request.RequestId}",
            Decision: NodalOsAssistedVerificationDecision.VerifiedLowRisk,
            SupportingSignals: supportingSignals,
            DiagnosticSignals: rejectedSignals.Concat(uncertainSignals).ToArray(),
            NonOcrCorroborationRequired: true,
            NonOcrCorroborationSatisfied: true,
            ReadOnlyObservationOnly: true,
            ActionsAllowed: false,
            CanProduceActionPlan: false,
            CanProduceSafeAction: false,
            CanApproveAction: false,
            CanApproveClick: false,
            CanApproveSubmit: false,
            CanApproveSend: false,
            CanApproveDelete: false,
            CanApprovePay: false,
            CanApproveSign: false,
            NoAuthority: true,
            EvidenceOnly: true,
            ProvenancePreserved: supportingSignals.All(HasProvenance),
            Reason: "low-risk verification passed with OCR auxiliary evidence plus corroborating non-OCR signal");
    }

    private static bool CanUseOcrSignal(NodalOsAssistedVerificationSignal signal) =>
        signal.SupportsVerification &&
        !signal.DiagnosticOnly &&
        !signal.Rejected &&
        signal.NoAuthority &&
        signal.EvidenceOnly &&
        !signal.ActionAllowed &&
        signal.RegionVerified &&
        signal.ConfidenceGatePassed &&
        signal.FingerprintHashMatch &&
        signal.ConfidenceBand is NodalOsOcrEvidenceConfidenceBand.High or NodalOsOcrEvidenceConfidenceBand.Medium &&
        (signal.DiffScore is null || signal.DiffScore <= AcceptedDiffThreshold);

    private static bool IsNonOcrCorroboratingSignal(NodalOsAssistedVerificationSignal signal) =>
        signal.Kind is not NodalOsAssistedVerificationSignalKind.OcrAuxiliaryEvidence and
            not NodalOsAssistedVerificationSignalKind.DiagnosticOnly and
            not NodalOsAssistedVerificationSignalKind.Rejected &&
        signal.SupportsVerification &&
        !signal.DiagnosticOnly &&
        !signal.Rejected &&
        signal.NoAuthority &&
        signal.EvidenceOnly &&
        !signal.ActionAllowed;

    private static bool SignalsCorroborate(
        IReadOnlyList<NodalOsAssistedVerificationSignal> ocrSignals,
        IReadOnlyList<NodalOsAssistedVerificationSignal> nonOcrSignals)
    {
        foreach (var ocr in ocrSignals)
        {
            var ocrValue = NormalizeForComparison(ocr.ExpectedText) ??
                           NormalizeForComparison(ocr.ObservedText) ??
                           NormalizeForComparison(ocr.NormalizedText);
            if (ocrValue is null)
                continue;

            foreach (var signal in nonOcrSignals)
            {
                var corroboratingValue = NormalizeForComparison(signal.ExpectedText) ??
                                         NormalizeForComparison(signal.ObservedText) ??
                                         NormalizeForComparison(signal.NormalizedText);
                if (corroboratingValue is null)
                    continue;

                if (string.Equals(ocrValue, corroboratingValue, StringComparison.Ordinal))
                    return true;
            }
        }

        return false;
    }

    private static string? NormalizeForComparison(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        return string.Concat(value.Where(c => !char.IsWhiteSpace(c))).ToUpperInvariant();
    }

    private static bool HasProvenance(NodalOsAssistedVerificationSignal signal) =>
        signal.Kind != NodalOsAssistedVerificationSignalKind.OcrAuxiliaryEvidence ||
        (signal.SourceCategory is not null &&
         !string.IsNullOrWhiteSpace(signal.CaptureMode) &&
         !string.IsNullOrWhiteSpace(signal.WindowTitleOrSource) &&
         !string.IsNullOrWhiteSpace(signal.ProcessOrSource) &&
         signal.RegionBounds is not null);

    private static NodalOsAssistedVerificationResult Reject(
        NodalOsAssistedVerificationRequest request,
        NodalOsAssistedVerificationDecision decision,
        string reason) =>
        new(
            ResultId: $"assisted-verification:{request.RequestId}",
            Decision: decision,
            SupportingSignals: Array.Empty<NodalOsAssistedVerificationSignal>(),
            DiagnosticSignals: request.Signals.ToArray(),
            NonOcrCorroborationRequired: true,
            NonOcrCorroborationSatisfied: false,
            ReadOnlyObservationOnly: true,
            ActionsAllowed: false,
            CanProduceActionPlan: false,
            CanProduceSafeAction: false,
            CanApproveAction: false,
            CanApproveClick: false,
            CanApproveSubmit: false,
            CanApproveSend: false,
            CanApproveDelete: false,
            CanApprovePay: false,
            CanApproveSign: false,
            NoAuthority: true,
            EvidenceOnly: true,
            ProvenancePreserved: request.Signals.Where(s => s.Kind == NodalOsAssistedVerificationSignalKind.OcrAuxiliaryEvidence).All(HasProvenance),
            Reason: reason);

    private static NodalOsAssistedVerificationResult RejectWithDiagnostics(
        NodalOsAssistedVerificationRequest request,
        NodalOsAssistedVerificationDecision decision,
        IReadOnlyList<NodalOsAssistedVerificationSignal> diagnostics,
        string reason) =>
        new(
            ResultId: $"assisted-verification:{request.RequestId}",
            Decision: decision,
            SupportingSignals: Array.Empty<NodalOsAssistedVerificationSignal>(),
            DiagnosticSignals: diagnostics,
            NonOcrCorroborationRequired: true,
            NonOcrCorroborationSatisfied: false,
            ReadOnlyObservationOnly: true,
            ActionsAllowed: false,
            CanProduceActionPlan: false,
            CanProduceSafeAction: false,
            CanApproveAction: false,
            CanApproveClick: false,
            CanApproveSubmit: false,
            CanApproveSend: false,
            CanApproveDelete: false,
            CanApprovePay: false,
            CanApproveSign: false,
            NoAuthority: true,
            EvidenceOnly: true,
            ProvenancePreserved: diagnostics.Where(s => s.Kind == NodalOsAssistedVerificationSignalKind.OcrAuxiliaryEvidence).All(HasProvenance),
            Reason: reason);

    private static NodalOsAssistedVerificationResult NeedsMoreEvidence(
        NodalOsAssistedVerificationRequest request,
        IReadOnlyList<NodalOsAssistedVerificationSignal> diagnostics,
        string reason) =>
        new(
            ResultId: $"assisted-verification:{request.RequestId}",
            Decision: NodalOsAssistedVerificationDecision.NeedsMoreEvidence,
            SupportingSignals: Array.Empty<NodalOsAssistedVerificationSignal>(),
            DiagnosticSignals: diagnostics,
            NonOcrCorroborationRequired: true,
            NonOcrCorroborationSatisfied: false,
            ReadOnlyObservationOnly: true,
            ActionsAllowed: false,
            CanProduceActionPlan: false,
            CanProduceSafeAction: false,
            CanApproveAction: false,
            CanApproveClick: false,
            CanApproveSubmit: false,
            CanApproveSend: false,
            CanApproveDelete: false,
            CanApprovePay: false,
            CanApproveSign: false,
            NoAuthority: true,
            EvidenceOnly: true,
            ProvenancePreserved: diagnostics.Where(s => s.Kind == NodalOsAssistedVerificationSignalKind.OcrAuxiliaryEvidence).All(HasProvenance),
            Reason: reason);
}
