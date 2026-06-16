using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.BrowserExecutor.Cdp;

public sealed class NodalOsLocalPreviewReleaseCandidateFreezeService
{
    public NodalOsReleaseCandidateFreeze Freeze(NodalOsLocalPreviewReleaseCandidate candidate)
    {
        var blockers = new List<NodalOsReleaseCandidateBlocker>();
        var reasons = new List<string>();

        void Add(string code, string description)
        {
            blockers.Add(new NodalOsReleaseCandidateBlocker(code, BrowserCredentialRedactor.Redact(description), BlocksReleaseCandidate: true));
            reasons.Add(code);
        }

        if (!candidate.WorktreeCanonical)
            Add("worktree-mismatch", "canonical worktree required");
        if (!candidate.TestsOk)
            Add("tests-not-ok", "restore/build/full test suite must pass");
        if (!candidate.SkippedCategoriesOk)
            Add("skipped-categories-not-ok", "skipped test categories must match the allowlist");
        if (!candidate.M51M65EvidenceVerified || candidate.EvidenceRefs.Count == 0)
            Add("missing-evidence", "M51/M65 ledger and release evidence must be verified");
        if (!candidate.ProductAdminStable)
            Add("product-admin-not-stable", "Product/Admin polish must be stable");
        if (!candidate.OperatorUxStable)
            Add("operator-ux-not-stable", "Operator UX clarity must be stable");
        if (!candidate.Hito162ReplacementStable)
            Add("hito-162-not-stable", "HITO-162 replacement must remain stable local fixture-first");
        if (candidate.HasHighOrCriticalIssues)
            Add("high-critical-issue-open", "high or critical issue blocks release candidate freeze");

        var scopeInflation = candidate.Scope.ExternalGeneralCdpReady ||
            candidate.Scope.ProductionEnabled ||
            candidate.Scope.PublicSaasEnabled ||
            candidate.Scope.PublicApiEnabled ||
            candidate.Scope.BillingEmailEnabled ||
            candidate.Scope.RealCredentialsEnabled ||
            candidate.Scope.SensitiveSitesEnabled ||
            candidate.Scope.SubmitPaySignDeleteEnabled ||
            candidate.Scope.RecorderReplayProductiveEnabled ||
            candidate.Scope.DeniedScope.Count == 0 ||
            !candidate.Scope.DeniedScope.Any(item => item.Contains("production", StringComparison.OrdinalIgnoreCase)) ||
            !candidate.Scope.DeniedScope.Any(item => item.Contains("SaaS", StringComparison.OrdinalIgnoreCase));

        if (scopeInflation)
            Add("scope-inflation", "production/SaaS/API/billing/email/credentials/sensitive/submit/recorder/external general CDP must stay denied");

        var decision = !candidate.WorktreeCanonical
            ? NodalOsReleaseCandidateDecision.BlockedByWorktree
            : !candidate.TestsOk || !candidate.SkippedCategoriesOk
                ? NodalOsReleaseCandidateDecision.BlockedByTests
                : scopeInflation
                    ? NodalOsReleaseCandidateDecision.BlockedByScopeInflation
                    : candidate.HasHighOrCriticalIssues
                        ? NodalOsReleaseCandidateDecision.BlockedBySecurity
                        : !candidate.M51M65EvidenceVerified || candidate.EvidenceRefs.Count == 0
                            ? NodalOsReleaseCandidateDecision.BlockedByMissingEvidence
                            : blockers.Count == 0
                                ? NodalOsReleaseCandidateDecision.FrozenReadyForExternalAudit
                                : NodalOsReleaseCandidateDecision.BlockedBySecurity;

        var state = decision switch
        {
            NodalOsReleaseCandidateDecision.FrozenReadyForExternalAudit => NodalOsLocalPreviewReleaseCandidateState.FrozenReadyForExternalAudit,
            NodalOsReleaseCandidateDecision.BlockedByWorktree => NodalOsLocalPreviewReleaseCandidateState.BlockedByWorktree,
            NodalOsReleaseCandidateDecision.BlockedByTests => NodalOsLocalPreviewReleaseCandidateState.BlockedByTests,
            NodalOsReleaseCandidateDecision.BlockedByScopeInflation => NodalOsLocalPreviewReleaseCandidateState.BlockedByScopeInflation,
            NodalOsReleaseCandidateDecision.BlockedByMissingEvidence => NodalOsLocalPreviewReleaseCandidateState.BlockedByMissingEvidence,
            _ => NodalOsLocalPreviewReleaseCandidateState.BlockedBySecurity
        };

        return new NodalOsReleaseCandidateFreeze(
            state,
            decision,
            candidate,
            blockers,
            reasons.Select(BrowserCredentialRedactor.Redact).ToArray(),
            ReadyForClaudeAudit: decision == NodalOsReleaseCandidateDecision.FrozenReadyForExternalAudit,
            ScopeInflationDetected: scopeInflation,
            Redacted: true);
    }

    public static NodalOsLocalPreviewReleaseCandidate DefaultCandidate(string commit) =>
        new(
            "NODAL OS",
            commit,
            @"C:\Users\diego\OneDrive\PERSONAL\ONE Brain\Codigo-m12-audit",
            "origin/chrome-lab-001-extension-local-ai-bridge",
            "Local Private Preview Release Candidate",
            "M51 closed: HTTP read-only target-owned proof with persisted ledger",
            "M65 closed: target-owned Chrome/CDP/DOM read-only proof with persisted ledger",
            "HITO-162 replacement stable local fixture-first",
            "Product/Admin stable",
            "Operator UX stable",
            TestsOk: true,
            SkippedCategoriesOk: true,
            WorktreeCanonical: true,
            M51M65EvidenceVerified: true,
            ProductAdminStable: true,
            OperatorUxStable: true,
            Hito162ReplacementStable: true,
            HasHighOrCriticalIssues: false,
            DefaultScope(),
            [
                "m51:http-readonly-target-owned-ledger:verified",
                "m65:target-owned-cdp-dom-ledger:verified",
                "release-gate:ReadyWithRestrictions",
                "hito-162-replacement:stable-local-fixture-first",
                "private-preview-runs:m124-m150:redacted"
            ],
            [
                "m124-m126",
                "m127-m129",
                "m148-m150"
            ],
            Redacted: true);

    public static NodalOsReleaseCandidateScope DefaultScope() =>
        new(
            [
                "Product/Admin local",
                "Operator UX local",
                "readiness dashboard",
                "diagnostics/evidence review",
                "issue triage local",
                "private local API in-process",
                "local fixture-first HITO-162 replacement signals"
            ],
            [
                "production",
                "public SaaS",
                "public API real",
                "billing/email real",
                "real credentials",
                "sensitive sites",
                "submit/pay/sign/delete",
                "productive recorder/replay",
                "external CDP general-ready",
                "new external targets without dedicated evidence"
            ],
            ExternalGeneralCdpReady: false,
            ProductionEnabled: false,
            PublicSaasEnabled: false,
            PublicApiEnabled: false,
            BillingEmailEnabled: false,
            RealCredentialsEnabled: false,
            SensitiveSitesEnabled: false,
            SubmitPaySignDeleteEnabled: false,
            RecorderReplayProductiveEnabled: false);
}
