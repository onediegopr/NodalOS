using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.BrowserExecutor.Cdp;

public sealed class NodalOsOcrEvidenceAdapter
{
    private const double AcceptedDiffThreshold = 0.01d;

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

        if (acceptance == NodalOsOcrObservationAcceptanceState.AcceptedEvidence &&
            !IsFingerprintAccepted(envelope))
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
                BrowserCredentialRedactor.Redact("accepted OCR evidence requires matching fingerprint and diff score within threshold"),
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
        var expectedFingerprint = envelope.Result.RegionVerification?.ExpectedFingerprint;
        var observedFingerprint = envelope.CaptureFingerprint;
        var fingerprintHashMatch = expectedFingerprint is not null &&
                                   observedFingerprint is not null &&
                                   string.Equals(expectedFingerprint.Sha256, observedFingerprint.Sha256, StringComparison.Ordinal);
        var diffScore = envelope.Result.RegionVerification?.DiffScore ?? envelope.DiffScore;
        var confidenceBand = ResolveConfidenceBand(envelope, fingerprintHashMatch, diffScore);

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
            envelope.CaptureFingerprint)
        {
            FingerprintHashMatch = fingerprintHashMatch,
            DiffScore = diffScore,
            DarkPixelRatio = observedFingerprint?.DarkPixelRatio,
            MeanRgb = observedFingerprint is null
                ? null
                : (observedFingerprint.AverageRed, observedFingerprint.AverageGreen, observedFingerprint.AverageBlue),
            SampleSignature = observedFingerprint?.SampleSignature,
            ConfidenceBand = confidenceBand
        };
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

    private static bool IsFingerprintAccepted(NodalOsOcrEvidenceEnvelope envelope)
    {
        var expectedFingerprint = envelope.Result.RegionVerification?.ExpectedFingerprint;
        var observedFingerprint = envelope.CaptureFingerprint;
        if (expectedFingerprint is null || observedFingerprint is null)
            return false;

        if (!string.Equals(expectedFingerprint.Sha256, observedFingerprint.Sha256, StringComparison.Ordinal))
            return false;

        var diffScore = envelope.Result.RegionVerification?.DiffScore ?? envelope.DiffScore;
        return diffScore is null || diffScore <= AcceptedDiffThreshold;
    }

    private static NodalOsOcrEvidenceConfidenceBand ResolveConfidenceBand(
        NodalOsOcrEvidenceEnvelope envelope,
        bool fingerprintHashMatch,
        double? diffScore)
    {
        if (envelope.AcceptanceState != NodalOsOcrObservationAcceptanceState.AcceptedEvidence ||
            !envelope.RegionVerified ||
            !envelope.ConfidenceGatePassed ||
            !fingerprintHashMatch ||
            (diffScore is not null && diffScore > AcceptedDiffThreshold))
        {
            return envelope.AcceptanceState switch
            {
                NodalOsOcrObservationAcceptanceState.AcceptedEvidence => NodalOsOcrEvidenceConfidenceBand.Rejected,
                NodalOsOcrObservationAcceptanceState.UncertainRequiresExpansion => NodalOsOcrEvidenceConfidenceBand.Low,
                _ => NodalOsOcrEvidenceConfidenceBand.Rejected
            };
        }

        var confidence = envelope.Result.Confidence ?? 0d;
        if (confidence >= 0.95d)
            return NodalOsOcrEvidenceConfidenceBand.High;
        if (confidence >= 0.75d)
            return NodalOsOcrEvidenceConfidenceBand.Medium;
        return NodalOsOcrEvidenceConfidenceBand.Low;
    }
}

public sealed class NodalOsOcrEvidenceRuntimePolicyGate
{
    private readonly NodalOsOcrEvidenceAdapter adapter = new();

    public NodalOsOcrEvidencePolicyEvaluation Evaluate(
        NodalOsOcrEvidenceEnvelope envelope,
        string? artifactRef = null) =>
        adapter.EvaluateAndMap(envelope, artifactRef);
}

public sealed class NodalOsOcrEvidenceAuditConsumer
{
    private readonly NodalOsOcrEvidenceRuntimePolicyGate gate = new();

    public NodalOsOcrEvidencePolicyEvaluation Consume(
        BrowserPersistentAuditLedger auditLedger,
        NodalOsOcrEvidenceEnvelope envelope,
        string? artifactRef = null)
    {
        var evaluation = gate.Evaluate(envelope, artifactRef);
        if (evaluation.LedgerStatus == NodalOsOcrEvidenceLedgerStatus.RejectedPolicyViolation)
        {
            var rejectionEvent = BrowserPersistentAuditLedger.Create(
                BrowserAuditLedgerEventKind.OcrEvidencePolicyViolationRejected,
                runId: BuildSafeAuditId("ocr-run", envelope.ObservationId),
                actionId: BuildSafeAuditId("ocr-evidence", envelope.ObservationId),
                correlationId: BuildSafeAuditId("ocr-corr", envelope.ObservationId),
                profileId: "ocrprofile",
                sessionId: "ocrsession",
                consentId: null,
                secretId: null,
                providerKind: null,
                decision: evaluation.LedgerStatus.ToString(),
                reason: evaluation.Reason,
                metadata: BuildPolicyViolationMetadata(envelope));
            auditLedger.Append(rejectionEvent);
            return evaluation;
        }

        if (!evaluation.CanRegister || evaluation.Entry is null)
            return evaluation;

        var auditEvent = BrowserPersistentAuditLedger.Create(
            ResolveKind(evaluation.LedgerStatus),
            runId: BuildSafeAuditId("ocr-run", evaluation.Entry.ObservationId),
            actionId: BuildSafeAuditId("ocr-evidence", evaluation.Entry.EvidenceId),
            correlationId: BuildSafeAuditId("ocr-corr", evaluation.Entry.ObservationId),
            profileId: "ocrprofile",
            sessionId: "ocrsession",
            consentId: null,
            secretId: null,
            providerKind: null,
            decision: evaluation.LedgerStatus.ToString(),
            reason: evaluation.Reason,
            metadata: BuildMetadata(evaluation.Entry));

        auditLedger.Append(auditEvent);
        return evaluation;
    }

    private static BrowserAuditLedgerEventKind ResolveKind(NodalOsOcrEvidenceLedgerStatus status) => status switch
    {
        NodalOsOcrEvidenceLedgerStatus.AcceptedAuxiliary => BrowserAuditLedgerEventKind.OcrEvidenceAuxiliaryRecorded,
        NodalOsOcrEvidenceLedgerStatus.RecordedDiagnosticRejected => BrowserAuditLedgerEventKind.OcrEvidenceDiagnosticRejectedRecorded,
        NodalOsOcrEvidenceLedgerStatus.RecordedDiagnosticUncertain => BrowserAuditLedgerEventKind.OcrEvidenceDiagnosticUncertainRecorded,
        _ => BrowserAuditLedgerEventKind.OcrEvidencePolicyViolationRejected
    };

    private static IReadOnlyDictionary<string, string> BuildMetadata(NodalOsOcrEvidenceLedgerEntry entry)
    {
        return new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["source"] = entry.Source.ToString(),
            ["sourceCategory"] = entry.SourceCategory.ToString(),
            ["captureMode"] = entry.CaptureMode,
            ["evidenceUse"] = entry.EvidenceUse.ToString(),
            ["acceptanceState"] = entry.AcceptanceState.ToString(),
            ["recognizedText"] = entry.RecognizedText,
            ["normalizedText"] = entry.NormalizedText,
            ["expectedText"] = entry.ExpectedText ?? string.Empty,
            ["exactMatch"] = entry.ExactMatch.ToString(),
            ["normalizedMatch"] = entry.NormalizedMatch.ToString(),
            ["editDistance"] = entry.EditDistance?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty,
            ["confidence"] = entry.Confidence?.ToString("F4", System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty,
            ["regionVerified"] = entry.RegionVerified.ToString(),
            ["confidenceGatePassed"] = entry.ConfidenceGatePassed.ToString(),
            ["fingerprintHashMatch"] = entry.FingerprintHashMatch.ToString(),
            ["diffScore"] = entry.DiffScore?.ToString("F4", System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty,
            ["darkPixelRatio"] = entry.DarkPixelRatio?.ToString("F4", System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty,
            ["meanRgb"] = entry.MeanRgb is null
                ? string.Empty
                : $"{entry.MeanRgb.Value.Red:F1},{entry.MeanRgb.Value.Green:F1},{entry.MeanRgb.Value.Blue:F1}",
            ["sampleSignature"] = entry.SampleSignature ?? string.Empty,
            ["confidenceBand"] = entry.ConfidenceBand.ToString(),
            ["noAuthority"] = entry.NoAuthority.ToString(),
            ["evidenceOnly"] = entry.EvidenceOnly.ToString(),
            ["actionAllowed"] = entry.ActionAllowed.ToString(),
            ["officialSpacePolicy"] = entry.OfficialSpacePolicy.ToString(),
            ["softmaxReapplied"] = entry.SoftmaxReapplied.ToString(),
            ["artifactRef"] = entry.ArtifactRef ?? string.Empty,
            ["regionBounds"] = $"{entry.RegionBounds.X},{entry.RegionBounds.Y},{entry.RegionBounds.Width},{entry.RegionBounds.Height}",
            ["warnings"] = string.Join(" | ", entry.Warnings)
        };
    }

    private static IReadOnlyDictionary<string, string> BuildPolicyViolationMetadata(NodalOsOcrEvidenceEnvelope envelope)
    {
        return new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["source"] = NodalOsOcrEvidenceSource.Ocr.ToString(),
            ["sourceCategory"] = envelope.SourceCategory.ToString(),
            ["captureMode"] = envelope.CaptureMode,
            ["acceptanceState"] = envelope.AcceptanceState.ToString(),
            ["actionAllowed"] = envelope.Result.ActionAllowed.ToString(),
            ["noAuthority"] = envelope.Result.NoAuthority.ToString(),
            ["evidenceOnly"] = envelope.Result.EvidenceOnly.ToString(),
            ["officialSpacePolicy"] = envelope.Result.OfficialSpacePolicy.ToString(),
            ["softmaxReapplied"] = envelope.Result.SoftmaxReapplied.ToString(),
            ["regionBounds"] = $"{envelope.RegionBounds.X},{envelope.RegionBounds.Y},{envelope.RegionBounds.Width},{envelope.RegionBounds.Height}"
        };
    }

    private static string BuildSafeAuditId(string prefix, string source)
    {
        var bytes = System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(source));
        var hash = Convert.ToHexString(bytes).ToLowerInvariant()[..16];
        return $"{prefix}-{hash}";
    }
}
