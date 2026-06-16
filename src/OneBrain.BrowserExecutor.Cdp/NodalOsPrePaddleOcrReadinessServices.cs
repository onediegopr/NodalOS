using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.BrowserExecutor.Cdp;

// M190 - pre-PaddleOCR readiness review.
// This is a readiness reviewer only. It never enables OCR and never claims strong OS
// isolation unless the isolation evidence explicitly proves it.
public sealed class NodalOsPrePaddleOcrReadinessReviewer
{
    public NodalOsPrePaddleOcrReadinessReport Review(NodalOsPrePaddleOcrReadinessReview review)
    {
        var requirements = BuildRequirements(review);
        var warnings = new List<string>();

        if (review.IsolationEvidence.NetworkIsolation != NodalOsLocalWorkerIsolationEnforcementLevel.Enforced)
            warnings.Add($"network isolation is {review.IsolationEvidence.NetworkIsolation}; strong OS network sandbox is not claimed");
        if (review.IsolationEvidence.FilesystemIsolation != NodalOsLocalWorkerIsolationEnforcementLevel.Enforced)
            warnings.Add($"filesystem isolation is {review.IsolationEvidence.FilesystemIsolation}; strong OS filesystem sandbox is not claimed");
        if (review.IsolationEvidence.ProcessIsolation != NodalOsLocalWorkerIsolationEnforcementLevel.Enforced)
            warnings.Add($"process isolation is {review.IsolationEvidence.ProcessIsolation}; strong OS process sandbox is not claimed");

        var decision = Decide(review, requirements);
        var reason = Reason(decision, review, requirements);

        return new NodalOsPrePaddleOcrReadinessReport(
            $"pre-paddleocr-readiness-{Guid.NewGuid():N}",
            decision,
            BrowserCredentialRedactor.Redact(reason),
            PixelRedactionV2Verified: PixelRedactionReady(review.PixelRedactionResult),
            RawNotPersisted: review.PixelRedactionResult?.OriginalRawPersisted == false && review.IsolationEvidence.NoRawPersistence,
            FullScreenBlocked: review.FullScreenBlocked,
            IpcAuthEnforcedOrModeled: review.IpcHealth.AuthTokenValid,
            MessageSizeLimitEnforced: review.MessageSizeLimitConfigured && review.IpcHealth.WithinSizeLimits,
            TimeoutKillBehaviorVerified: review.TimeoutKillBehaviorVerified && review.IpcHealth.WithinTimeoutLimits,
            ProcessBoundaryEvaluated: review.ProcessBoundaryEvaluated,
            ActivationGateStillBlocksRealOcr: review.ActivationGateStillBlocksRealOcr,
            SaaSStillDisabled: review.SaaSStillDisabled,
            HumanEscalationConfigured: review.HumanEscalationConfigured,
            RollbackPauseConfigured: review.RollbackPauseConfigured,
            EvaluationHarnessPassing: review.EvaluationHarnessPassing,
            review.IsolationEvidence.NetworkIsolation,
            review.IsolationEvidence.FilesystemIsolation,
            review.IsolationEvidence.ProcessIsolation,
            requirements,
            warnings,
            DateTimeOffset.UtcNow,
            NoAuthority: review.NoAuthority && review.IpcHealth.NoAuthority && review.IsolationEvidence.NoAuthority,
            Redacted: true);
    }

    private static IReadOnlyList<NodalOsPrePaddleOcrRequirement> BuildRequirements(NodalOsPrePaddleOcrReadinessReview review)
    {
        var modeledIsolation =
            review.IsolationEvidence.NetworkIsolation is NodalOsLocalWorkerIsolationEnforcementLevel.Modeled or NodalOsLocalWorkerIsolationEnforcementLevel.Observed or NodalOsLocalWorkerIsolationEnforcementLevel.NotEnforced ||
            review.IsolationEvidence.FilesystemIsolation is NodalOsLocalWorkerIsolationEnforcementLevel.Modeled or NodalOsLocalWorkerIsolationEnforcementLevel.Observed or NodalOsLocalWorkerIsolationEnforcementLevel.NotEnforced ||
            review.IsolationEvidence.ProcessIsolation is NodalOsLocalWorkerIsolationEnforcementLevel.Modeled or NodalOsLocalWorkerIsolationEnforcementLevel.Observed or NodalOsLocalWorkerIsolationEnforcementLevel.NotEnforced;

        return
        [
            Req("pixel-redaction-v2", "Pixel redaction V2 verified", PixelRedactionReady(review.PixelRedactionResult), false, review.PixelRedactionResult?.Evidence.EvidenceId ?? "missing"),
            Req("raw-not-persisted", "Original raw image is not persisted", review.PixelRedactionResult?.OriginalRawPersisted == false && review.IsolationEvidence.NoRawPersistence, false, review.PixelRedactionResult?.Evidence.EvidenceId ?? "missing"),
            Req("full-screen-blocked", "Full-screen OCR remains blocked", review.FullScreenBlocked, false, "policy:full-screen-blocked"),
            Req("ipc-auth", "IPC auth token is required and valid", review.IpcHealth.AuthTokenValid, false, review.IpcHealth.HealthId),
            Req("ipc-size", "IPC message size limit is configured and enforced", review.MessageSizeLimitConfigured && review.IpcHealth.WithinSizeLimits, false, review.IpcHealth.HealthId),
            Req("ipc-timeout", "IPC timeout/kill behavior is verified", review.TimeoutKillBehaviorVerified && review.IpcHealth.WithinTimeoutLimits, false, review.IpcHealth.HealthId),
            Req("process-boundary", "Process boundary was evaluated", review.ProcessBoundaryEvaluated, modeledIsolation, review.IsolationEvidence.EvidenceId),
            Req("network-honesty", "Network isolation status is explicitly reported", review.IsolationEvidence.NetworkIsolation != NodalOsLocalWorkerIsolationEnforcementLevel.Enforced || review.IsolationEvidence.NoNetworkIntentObserved, review.IsolationEvidence.NetworkIsolation != NodalOsLocalWorkerIsolationEnforcementLevel.Enforced, review.IsolationEvidence.EvidenceId),
            Req("filesystem-honesty", "Filesystem isolation status is explicitly reported", review.IsolationEvidence.FilesystemIsolation != NodalOsLocalWorkerIsolationEnforcementLevel.Enforced || review.IsolationEvidence.NoFilesystemWriteObserved, review.IsolationEvidence.FilesystemIsolation != NodalOsLocalWorkerIsolationEnforcementLevel.Enforced, review.IsolationEvidence.EvidenceId),
            Req("activation-gate", "Activation gate still blocks real OCR", review.ActivationGateStillBlocksRealOcr, false, "activation-gate:real-ocr-false"),
            Req("saas-disabled", "SaaS OCR remains disabled", review.SaaSStillDisabled, false, "activation-gate:saas-false"),
            Req("human-escalation", "Human escalation is configured", review.HumanEscalationConfigured, false, "recovery:human-escalation"),
            Req("rollback-pause", "Rollback/pause is configured", review.RollbackPauseConfigured, false, "ops:rollback-pause"),
            Req("evaluation-harness", "Evaluation harness still passes", review.EvaluationHarnessPassing, false, "evaluation:harness"),
            Req("no-authority", "OCR readiness has no action authority", review.NoAuthority && review.IpcHealth.NoAuthority && review.IsolationEvidence.NoAuthority, false, "authority:core-only")
        ];
    }

    private static NodalOsPrePaddleOcrReadinessDecision Decide(
        NodalOsPrePaddleOcrReadinessReview review,
        IReadOnlyList<NodalOsPrePaddleOcrRequirement> requirements)
    {
        if (!PixelRedactionReady(review.PixelRedactionResult))
            return NodalOsPrePaddleOcrReadinessDecision.BlockedByRedaction;
        if (review.PixelRedactionResult?.OriginalRawPersisted != false || !review.IsolationEvidence.NoRawPersistence)
            return NodalOsPrePaddleOcrReadinessDecision.BlockedByRawPersistenceRisk;
        if (!review.IpcHealth.AuthTokenValid || !review.MessageSizeLimitConfigured || !review.IpcHealth.WithinSizeLimits || !review.TimeoutKillBehaviorVerified || !review.IpcHealth.WithinTimeoutLimits)
            return NodalOsPrePaddleOcrReadinessDecision.BlockedByIpcSecurity;
        if (!review.ProcessBoundaryEvaluated)
            return NodalOsPrePaddleOcrReadinessDecision.BlockedByIsolation;
        if (!review.IsolationEvidence.NoNetworkIntentObserved)
            return NodalOsPrePaddleOcrReadinessDecision.BlockedByNetworkRisk;
        if (!review.IsolationEvidence.NoFilesystemWriteObserved)
            return NodalOsPrePaddleOcrReadinessDecision.BlockedByRawPersistenceRisk;
        if (!review.NoAuthority || !review.IpcHealth.NoAuthority || !review.IsolationEvidence.NoAuthority || !review.ActivationGateStillBlocksRealOcr)
            return NodalOsPrePaddleOcrReadinessDecision.BlockedByAuthorityRisk;
        if (!requirements.All(requirement => requirement.Passed))
            return NodalOsPrePaddleOcrReadinessDecision.NotReadyForPaddleOcr;

        var strongIsolation =
            review.IsolationEvidence.NetworkIsolation == NodalOsLocalWorkerIsolationEnforcementLevel.Enforced &&
            review.IsolationEvidence.FilesystemIsolation == NodalOsLocalWorkerIsolationEnforcementLevel.Enforced &&
            review.IsolationEvidence.ProcessIsolation == NodalOsLocalWorkerIsolationEnforcementLevel.Enforced;

        return strongIsolation
            ? NodalOsPrePaddleOcrReadinessDecision.ReadyForPaddleOcrSyntheticInstallPlan
            : NodalOsPrePaddleOcrReadinessDecision.ReadyForPaddleOcrDesignOnly;
    }

    private static string Reason(
        NodalOsPrePaddleOcrReadinessDecision decision,
        NodalOsPrePaddleOcrReadinessReview review,
        IReadOnlyList<NodalOsPrePaddleOcrRequirement> requirements)
    {
        if (decision is NodalOsPrePaddleOcrReadinessDecision.ReadyForPaddleOcrDesignOnly)
            return "pixel redaction and IPC controls pass, but OS isolation is modeled/observed rather than strongly enforced; design-only readiness, not real OCR readiness";
        if (decision is NodalOsPrePaddleOcrReadinessDecision.ReadyForPaddleOcrSyntheticInstallPlan)
            return "pixel redaction, IPC controls, and isolation evidence pass for a future synthetic install plan only; real OCR remains disabled";

        var failed = requirements.FirstOrDefault(requirement => !requirement.Passed);
        return failed is null
            ? $"blocked by {decision}; real OCR remains disabled"
            : $"blocked by {decision}; failed requirement: {failed.RequirementId} ({failed.Name})";
    }

    private static bool PixelRedactionReady(NodalOsPixelRedactionResult? result) =>
        NodalOsPixelImageRedactor.IsValidForRealOcr(result) &&
        result?.SafeForPersistence == true &&
        result.OriginalRawPersisted == false;

    private static NodalOsPrePaddleOcrRequirement Req(string id, string name, bool passed, bool modeledOnly, string evidenceRef) =>
        new(id, name, passed, modeledOnly, BrowserCredentialRedactor.Redact(evidenceRef));
}
