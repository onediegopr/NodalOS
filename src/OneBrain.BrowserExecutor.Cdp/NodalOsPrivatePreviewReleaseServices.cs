using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.BrowserExecutor.Cdp;

public sealed class NodalOsProductAdminPrivatePreviewHardeningService
{
    public NodalOsProductAdminPrivatePreviewHardeningReport BuildDefaultReport() =>
        new(
            "NODAL OS",
            [
                NodalOsProductAdminPrivatePreviewStatus.LocalPrivatePreviewReady,
                NodalOsProductAdminPrivatePreviewStatus.ExternalGeneralBlocked,
                NodalOsProductAdminPrivatePreviewStatus.SensitiveSurfaceBlocked,
                NodalOsProductAdminPrivatePreviewStatus.CredentialsBlocked,
                NodalOsProductAdminPrivatePreviewStatus.SubmitMutationBlocked,
                NodalOsProductAdminPrivatePreviewStatus.PublicSaasBlocked,
                NodalOsProductAdminPrivatePreviewStatus.BillingEmailBlocked,
                NodalOsProductAdminPrivatePreviewStatus.RecorderReplayBlocked
            ],
            "M51 closed: HTTP read-only target-owned scope only",
            "M65 closed: target-owned Chrome/CDP/DOM read-only scope only",
            UiAdminAuthorityBlocked: true,
            CoreAuthorityRequired: true,
            ExternalGeneralReady: false,
            PublicSaasBlocked: true,
            PublicApiBlocked: true,
            BillingEmailBlocked: true,
            CredentialsBlocked: true,
            SensitiveSurfacesBlocked: true,
            SubmitPaySignDeleteBlocked: true,
            RecorderReplayBlocked: true,
            [
                "Run internal local private preview only",
                "Use Core-governed private local API and readiness dashboard",
                "Keep external general CDP blocked until separate evidence exists",
                "Escalate any credential/login/payment/delete request to blocker explanation"
            ],
            [
                "m51:http-readonly-ledger:redacted",
                "m65:cdp-ledger:audit-ledger-edb3e2fbb0a0446788dae17a269c0058",
                "m65:scope-lock:target-owned-only"
            ],
            Redacted: true);
}

public sealed class NodalOsOperatorUxReadinessService
{
    public NodalOsOperatorUxReadinessSummary BuildDefaultSummary() =>
        new(
            "NODAL OS",
            "ReadyWithRestrictions for internal local private preview",
            [
                "open local Product/Admin shell",
                "review readiness dashboard",
                "review private local API status",
                "review diagnostics and evidence refs",
                "file local issue triage report"
            ],
            [
                "external CDP general-ready claim",
                "third-party or sensitive sites",
                "real credentials",
                "submit/pay/sign/delete",
                "public SaaS or public API",
                "billing/email real",
                "productive recorder/replay"
            ],
            [
                "M51: closed HTTP read-only target-owned proof",
                "M65: closed target-owned Chrome/CDP/DOM read-only proof",
                "External general CDP: blocked",
                "Skipped tests: live/opt-in only, not blocking local preview"
            ],
            "Last proof: M65 target-owned Chrome/CDP/DOM read-only proof against lab.nodalos.com.ar, persisted redacted ledger",
            [
                "audit-ledger-edb3e2fbb0a0446788dae17a269c0058",
                "61f52af1eebf08d59a24e5fbb72e70acf0038e7a329bff6599a0ac00c757f03e"
            ],
            [
                "public SaaS blocked",
                "public API blocked",
                "external CDP general-ready blocked",
                "billing/email real blocked",
                "real credentials blocked",
                "sensitive sites blocked",
                "submit/pay/sign/delete blocked"
            ],
            "Start internal local private preview with restrictions and record issues locally.",
            "If credential/login/payment/delete/sensitive surface appears, stop and use operator blocker explanation.",
            "medium-local-preview",
            Redacted: true);
}

public sealed class NodalOsLocalPrivatePreviewReleaseGate
{
    public NodalOsLocalPrivatePreviewReleaseGateDecision Evaluate(NodalOsLocalPrivatePreviewReleaseGateInput input)
    {
        var reasons = new List<string>();
        if (!input.CanonicalWorktreeOk)
            reasons.Add("canonical worktree required");
        if (!input.BuildOk || !input.TestsOk)
            reasons.Add("build and full test suite must pass");
        if (!input.M51ClosedHttpScope)
            reasons.Add("M51 HTTP target-owned scope evidence required");
        if (!input.M65ClosedLimitedCdpScope)
            reasons.Add("M65 limited target-owned Chrome/CDP scope evidence required");
        if (!input.ProductAdminLocalReady)
            reasons.Add("Product/Admin local readiness required");
        if (!input.OperatorRunbookExists)
            reasons.Add("operator runbook required");
        if (!input.BlockerExplanationsReady)
            reasons.Add("operator blocker explanations required");
        if (!input.EvidenceLogSummaryReady)
            reasons.Add("evidence/log summary required");

        var dangerous = input.ExternalGeneralReady || input.PublicSaasEnabled || input.PublicApiEnabled ||
            input.RealBillingEnabled || input.RealEmailEnabled || input.RealCredentialsEnabled ||
            input.SensitiveSitesEnabled || input.SubmitPaySignDeleteEnabled || input.RecorderReplayProductiveEnabled;
        if (dangerous)
            reasons.Add("dangerous surfaces must remain blocked");

        var status = !input.CanonicalWorktreeOk
            ? NodalOsLocalPrivatePreviewReleaseGateStatus.BlockedByWorktree
            : input.ExternalGeneralReady
                ? NodalOsLocalPrivatePreviewReleaseGateStatus.BlockedByExternalGeneral
                : dangerous
                    ? NodalOsLocalPrivatePreviewReleaseGateStatus.BlockedBySecurity
                    : (!input.M51ClosedHttpScope || !input.M65ClosedLimitedCdpScope || !input.EvidenceLogSummaryReady)
                        ? NodalOsLocalPrivatePreviewReleaseGateStatus.BlockedByMissingEvidence
                        : !input.ProductAdminLocalReady
                            ? NodalOsLocalPrivatePreviewReleaseGateStatus.BlockedByProductAdmin
                            : reasons.Count == 0
                                ? NodalOsLocalPrivatePreviewReleaseGateStatus.ReadyWithRestrictions
                                : NodalOsLocalPrivatePreviewReleaseGateStatus.NotReady;

        return new NodalOsLocalPrivatePreviewReleaseGateDecision(
            status,
            NodalOsLocalPrivatePreviewScope.InternalLocalPrivatePreviewOnly,
            reasons.Select(BrowserCredentialRedactor.Redact).ToArray(),
            [
                "internal local Product/Admin preview",
                "private local API in-process",
                "local diagnostics/evidence review",
                "local issue triage"
            ],
            [
                "public SaaS",
                "public API",
                "external CDP general-ready",
                "real billing/email",
                "real credentials",
                "sensitive sites",
                "submit/pay/sign/delete",
                "productive recorder/replay"
            ],
            [
                "m51:http-readonly-target-owned",
                "m65:target-owned-cdp-ledger:audit-ledger-edb3e2fbb0a0446788dae17a269c0058",
                "scope-lock:external-cdp-general-false"
            ],
            status == NodalOsLocalPrivatePreviewReleaseGateStatus.ReadyWithRestrictions
                ? "Begin internal local private preview with documented restrictions."
                : "Fix release gate blockers before starting local private preview.",
            ReadyWithRestrictions: status == NodalOsLocalPrivatePreviewReleaseGateStatus.ReadyWithRestrictions,
            ExternalGeneralReady: false,
            PublicSaasStillDisabled: !input.PublicSaasEnabled,
            PublicApiStillDisabled: !input.PublicApiEnabled,
            RealBillingStillDisabled: !input.RealBillingEnabled,
            RealEmailStillDisabled: !input.RealEmailEnabled,
            RealCredentialsStillBlocked: !input.RealCredentialsEnabled,
            SensitiveSurfacesStillBlocked: !input.SensitiveSitesEnabled,
            SubmitPaySignDeleteStillBlocked: !input.SubmitPaySignDeleteEnabled,
            RecorderReplayProductiveStillBlocked: !input.RecorderReplayProductiveEnabled,
            Redacted: true);
    }

    public static NodalOsLocalPrivatePreviewReleaseGateInput SafeInput() =>
        new(
            BuildOk: true,
            TestsOk: true,
            CanonicalWorktreeOk: true,
            M51ClosedHttpScope: true,
            M65ClosedLimitedCdpScope: true,
            ProductAdminLocalReady: true,
            OperatorRunbookExists: true,
            BlockerExplanationsReady: true,
            EvidenceLogSummaryReady: true,
            ExternalGeneralReady: false,
            PublicSaasEnabled: false,
            PublicApiEnabled: false,
            RealBillingEnabled: false,
            RealEmailEnabled: false,
            RealCredentialsEnabled: false,
            SensitiveSitesEnabled: false,
            SubmitPaySignDeleteEnabled: false,
            RecorderReplayProductiveEnabled: false);
}
