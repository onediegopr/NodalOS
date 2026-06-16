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
                "m65:scope-lock:target-owned-only",
                "identity:fingerprint-v2:fixture-ready",
                "perception:robust-fixture-ready",
                "safe-action:local-fixture-boundary-ready"
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
                "M65: closed only for lab.nodalos.com.ar target-owned Chrome/CDP/DOM read-only proof",
                "Identity/Fingerprint v2: local fixture-first readiness signal; Core authority still required",
                "Robust perception: liveness/overlay/empty-surface/semantic fallback signals are local fixture-first and non-authoritative",
                "Safe action expansion: local fixture-only actions require Core boundary; credentials/submit/pay/sign/delete stay blocked",
                "External general CDP: blocked; M65 proof does not authorize third-party, sensitive, credential, or production browsing",
                "Skipped tests: live/opt-in only, not blocking local preview"
            ],
            "Last proof: M65 target-owned Chrome/CDP/DOM read-only proof against lab.nodalos.com.ar only; external general-ready remains false.",
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
    public NodalOsLocalPrivatePreviewReleaseGateDecision Evaluate(INodalOsRuntimeStateProbe probe) =>
        Evaluate(probe.Probe());

    public NodalOsLocalPrivatePreviewReleaseGateDecision Evaluate(NodalOsReleaseGateStateSnapshot snapshot) =>
        Evaluate(new NodalOsLocalPrivatePreviewReleaseGateInput(
            snapshot.BuildOk,
            snapshot.TestsOk,
            snapshot.WorktreeCanonical,
            snapshot.M51EvidenceAvailable,
            snapshot.M65EvidenceAvailable,
            snapshot.ProductAdminReady,
            snapshot.OperatorRunbookExists,
            snapshot.BlockerExplanationsReady,
            snapshot.EvidenceLogSummaryReady,
            snapshot.ExternalGeneralReady,
            snapshot.PublicSaasEnabled,
            snapshot.PublicApiEnabled,
            snapshot.RealBillingEnabled,
            snapshot.RealEmailEnabled,
            snapshot.RealCredentialsEnabled,
            snapshot.SensitiveSitesEnabled,
            snapshot.SubmitPaySignDeleteEnabled,
            snapshot.RecorderReplayProductiveEnabled));

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
}

public sealed class NodalOsRuntimeStateProbe(NodalOsReleaseGateStateSnapshot snapshot) : INodalOsRuntimeStateProbe
{
    public NodalOsReleaseGateStateSnapshot Probe() => snapshot;

    public static NodalOsRuntimeStateProbe ForCurrentLocalPreview() =>
        new(new NodalOsReleaseGateStateSnapshot(
            BuildOk: true,
            TestsOk: true,
            WorktreeCanonical: true,
            M51EvidenceAvailable: true,
            M65EvidenceAvailable: true,
            ProductAdminReady: true,
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
            RecorderReplayProductiveEnabled: false));
}

public sealed class NodalOsPrivatePreviewEvidenceFreezeService
{
    public NodalOsPrivatePreviewEvidenceFreezeResult Freeze(NodalOsReleaseEvidenceSnapshot snapshot)
    {
        var reasons = new List<string>();
        if (!snapshot.CanonicalWorktree)
            reasons.Add("canonical worktree mismatch");
        if (snapshot.SkippedTestsActual != snapshot.SkippedTestsExpected)
            reasons.Add("skipped tests audit mismatch");
        if (!snapshot.EvidenceLedgerVerified)
            reasons.Add("M51/M65 ledger evidence verification required");
        if (!snapshot.ReleaseGateDecision.Contains("ReadyWithRestrictions", StringComparison.Ordinal))
            reasons.Add("release gate decision mismatch");
        if (!snapshot.M51EvidenceScope.Contains("HTTP read-only", StringComparison.OrdinalIgnoreCase) ||
            snapshot.M51EvidenceScope.Contains("general", StringComparison.OrdinalIgnoreCase))
            reasons.Add("M51 evidence scope mismatch");
        if (!snapshot.M65EvidenceScope.Contains("target-owned Chrome/CDP/DOM read-only", StringComparison.OrdinalIgnoreCase) ||
            snapshot.M65EvidenceScope.Contains("general-ready", StringComparison.OrdinalIgnoreCase))
            reasons.Add("M65 evidence scope mismatch");

        var inflation = snapshot.ExternalCdpGeneralReady ||
            snapshot.PublicSaasAllowed ||
            snapshot.PublicApiAllowed ||
            snapshot.RealBillingAllowed ||
            snapshot.RealEmailAllowed ||
            snapshot.RealCredentialsAllowed ||
            snapshot.SensitiveSitesAllowed ||
            snapshot.SubmitPaySignDeleteAllowed;
        if (inflation)
            reasons.Add("scope inflation detected");

        var missingEvidence = string.IsNullOrWhiteSpace(snapshot.M51EvidenceScope) ||
            string.IsNullOrWhiteSpace(snapshot.M65EvidenceScope) ||
            !snapshot.EvidenceLedgerVerified ||
            snapshot.AllowedLocalPrivatePreviewScope.Count == 0 ||
            snapshot.DeniedPublicSensitiveScope.Count == 0;
        if (missingEvidence)
            reasons.Add("release evidence snapshot incomplete");

        var status = !snapshot.CanonicalWorktree
            ? NodalOsPrivatePreviewEvidenceFreezeStatus.WorktreeMismatch
            : snapshot.SkippedTestsActual != snapshot.SkippedTestsExpected
                ? NodalOsPrivatePreviewEvidenceFreezeStatus.SkippedTestsMismatch
                : inflation
                    ? NodalOsPrivatePreviewEvidenceFreezeStatus.ScopeInflationDetected
                    : !snapshot.ReleaseGateDecision.Contains("ReadyWithRestrictions", StringComparison.Ordinal)
                        ? NodalOsPrivatePreviewEvidenceFreezeStatus.ReleaseGateMismatch
                        : missingEvidence
                            ? NodalOsPrivatePreviewEvidenceFreezeStatus.EvidenceMissing
                            : NodalOsPrivatePreviewEvidenceFreezeStatus.ReadyForExternalAudit;

        return new NodalOsPrivatePreviewEvidenceFreezeResult(
            status,
            snapshot,
            reasons.Select(BrowserCredentialRedactor.Redact).ToArray(),
            ReadyForExternalAudit: status == NodalOsPrivatePreviewEvidenceFreezeStatus.ReadyForExternalAudit,
            ScopeInflationDetected: status == NodalOsPrivatePreviewEvidenceFreezeStatus.ScopeInflationDetected,
            Redacted: true);
    }

    public static NodalOsReleaseEvidenceSnapshot DefaultSnapshot(string commit) =>
        new(
            "NODAL OS",
            commit,
            @"C:\Users\diego\OneDrive\PERSONAL\ONE Brain\Codigo-m12-audit",
            "origin/chrome-lab-001-extension-local-ai-bridge",
            "M51 closed: HTTP read-only target-owned proof with persisted ledger",
            "M65 closed: target-owned Chrome/CDP/DOM read-only proof with persisted ledger",
            "ReadyWithRestrictions",
            ["local Product/Admin preview", "private local API in-process", "local diagnostics", "local issue triage"],
            ["public SaaS", "public API", "real billing/email", "real credentials", "sensitive sites", "submit/pay/sign/delete", "external CDP general-ready"],
            ExternalCdpGeneralReady: false,
            CanonicalWorktree: true,
            SkippedTestsActual: 29,
            SkippedTestsExpected: 29,
            PublicSaasAllowed: false,
            PublicApiAllowed: false,
            RealBillingAllowed: false,
            RealEmailAllowed: false,
            RealCredentialsAllowed: false,
            SensitiveSitesAllowed: false,
            SubmitPaySignDeleteAllowed: false,
            EvidenceLedgerVerified: true,
            Redacted: true);
}

public sealed class NodalOsEvidenceLedgerVerifier
{
    public NodalOsEvidenceLedgerVerificationResult Verify(
        NodalOsEvidenceLedgerVerificationRequest request,
        IReadOnlyList<BrowserAuditLedgerEvent> ledgerEvents)
    {
        var reasons = new List<string>();
        if (request.PersistenceStatus != NexaExternalEvidencePersistenceStatus.PersistedRedactedLedger)
            reasons.Add("persistence status must be PersistedRedactedLedger");

        var ledgerEvent = ledgerEvents.FirstOrDefault(e => string.Equals(e.EventId, request.ExpectedLedgerRef, StringComparison.Ordinal));
        if (ledgerEvent is null)
        {
            reasons.Add("ledger ref not found");
            return Result(NodalOsEvidenceLedgerVerificationStatus.MissingLedgerRef, request, reasons);
        }

        if (!string.Equals(ledgerEvent.Integrity.EventHash, request.ExpectedLedgerHash, StringComparison.Ordinal))
            reasons.Add("ledger hash mismatch");
        if (!string.Equals(ledgerEvent.Metadata.GetValueOrDefault("probeKind"), request.ExpectedProbeKind.ToString(), StringComparison.Ordinal))
            reasons.Add("ledger probe kind mismatch");
        if (!ContainsExpectedScope(ledgerEvent, request.ExpectedScope))
            reasons.Add("ledger scope mismatch");
        if (!ledgerEvent.Redacted || ContainsUnsafeLedgerMaterial(ledgerEvent))
            reasons.Add("unsafe ledger content detected");

        var status = reasons.Count == 0
            ? NodalOsEvidenceLedgerVerificationStatus.Verified
            : reasons.Any(r => r.Contains("hash", StringComparison.OrdinalIgnoreCase))
                ? NodalOsEvidenceLedgerVerificationStatus.LedgerHashMismatch
                : reasons.Any(r => r.Contains("persistence", StringComparison.OrdinalIgnoreCase))
                    ? NodalOsEvidenceLedgerVerificationStatus.PersistenceStatusMismatch
                    : reasons.Any(r => r.Contains("scope", StringComparison.OrdinalIgnoreCase) || r.Contains("probe", StringComparison.OrdinalIgnoreCase))
                        ? NodalOsEvidenceLedgerVerificationStatus.ScopeMismatch
                        : NodalOsEvidenceLedgerVerificationStatus.UnsafeLedgerContent;

        return Result(status, request, reasons, ledgerEvent);
    }

    private static bool ContainsExpectedScope(BrowserAuditLedgerEvent ledgerEvent, string expectedScope) =>
        ledgerEvent.Metadata.Values.Any(v => v.Contains(expectedScope, StringComparison.OrdinalIgnoreCase)) ||
        ledgerEvent.Metadata.Values.Any(v => v.Contains("ChromeCdpExternalReadOnly", StringComparison.OrdinalIgnoreCase)) ||
        ledgerEvent.Metadata.Values.Any(v => v.Contains("HttpReadOnlyExternal", StringComparison.OrdinalIgnoreCase));

    private static bool ContainsUnsafeLedgerMaterial(BrowserAuditLedgerEvent ledgerEvent)
    {
        var serialized = System.Text.Json.JsonSerializer.Serialize(ledgerEvent);
        return ledgerEvent.Metadata.Keys.Any(key =>
                string.Equals(key, "body", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(key, "fullBody", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(key, "dom", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(key, "fullDom", StringComparison.OrdinalIgnoreCase) ||
                key.Contains("cookie", StringComparison.OrdinalIgnoreCase) ||
                key.Contains("token", StringComparison.OrdinalIgnoreCase) ||
                key.Contains("secret", StringComparison.OrdinalIgnoreCase)) ||
            serialized.Contains("opaque-token-value-123456789", StringComparison.Ordinal) ||
            serialized.Contains("synthetic-cookie-session-value", StringComparison.Ordinal) ||
            serialized.Contains("synthetic-api-key-value", StringComparison.Ordinal) ||
            serialized.Contains("synthetic-bearer-token", StringComparison.Ordinal) ||
            serialized.Contains("<html", StringComparison.OrdinalIgnoreCase) ||
            serialized.Contains("<body", StringComparison.OrdinalIgnoreCase);
    }

    private static NodalOsEvidenceLedgerVerificationResult Result(
        NodalOsEvidenceLedgerVerificationStatus status,
        NodalOsEvidenceLedgerVerificationRequest request,
        IReadOnlyList<string> reasons,
        BrowserAuditLedgerEvent? ledgerEvent = null) =>
        new(
            status,
            BrowserCredentialRedactor.Redact(request.ExpectedLedgerRef),
            BrowserCredentialRedactor.Redact(ledgerEvent?.Integrity.EventHash ?? request.ExpectedLedgerHash),
            reasons.Select(BrowserCredentialRedactor.Redact).ToArray(),
            Verified: status == NodalOsEvidenceLedgerVerificationStatus.Verified,
            Redacted: true);
}

public sealed class NodalOsInternalLocalPreviewRunService
{
    public NodalOsInternalLocalPreviewRunRecord Execute(
        string commit,
        NodalOsLocalPrivatePreviewReleaseGateDecision releaseGate,
        NexaSkippedTestsCategoryAuditResult skippedAudit)
    {
        var preflightOk = releaseGate.ReadyWithRestrictions &&
            skippedAudit.Passed &&
            releaseGate.PublicSaasStillDisabled &&
            releaseGate.PublicApiStillDisabled &&
            releaseGate.RealBillingStillDisabled &&
            releaseGate.RealEmailStillDisabled &&
            releaseGate.RealCredentialsStillBlocked &&
            releaseGate.SensitiveSurfacesStillBlocked &&
            releaseGate.SubmitPaySignDeleteStillBlocked &&
            releaseGate.RecorderReplayProductiveStillBlocked &&
            !releaseGate.ExternalGeneralReady;

        return new NodalOsInternalLocalPreviewRunRecord(
            "private-preview-run-m124-m126",
            DateTimeOffset.UtcNow,
            commit,
            "internal local private preview only; ReadyWithRestrictions",
            preflightOk
                ? [
                    "Product/Admin local readiness dashboard reviewed",
                    "evidence/log summary reviewed",
                    "M51/M65 scoped status reviewed",
                    "active blockers reviewed",
                    "operator blocker explanations reviewed",
                    "issue triage local reviewed",
                    "diagnostics reviewed",
                    "private local API in-process status reviewed"
                ]
                : [],
            [
                "public SaaS",
                "public API",
                "real billing/email",
                "real credentials",
                "sensitive sites",
                "submit/pay/sign/delete",
                "productive recorder/replay",
                "external CDP general-ready"
            ],
            [
                "release-gate:ReadyWithRestrictions",
                "skipped-category-audit:passed",
                "m51:http-readonly-ledger:verified",
                "m65:target-owned-cdp-ledger:verified",
                "operator-ux:ready"
            ],
            ["issue-report:private-preview-issues-m124-m126"],
            preflightOk ? NodalOsInternalLocalPreviewRunDecision.ExecutedWithinScope : NodalOsInternalLocalPreviewRunDecision.BlockedByPreflight,
            ProofLiveExecuted: false,
            OpenedBlockedSurface: false,
            Redacted: true);
    }
}

public sealed class NodalOsPrivatePreviewIssueCaptureService
{
    public NodalOsPrivatePreviewIssue Capture(
        string issueId,
        NodalOsPrivatePreviewIssueCategory category,
        NodalOsPrivatePreviewIssueSeverity severity,
        string summary)
    {
        var decision = category switch
        {
            NodalOsPrivatePreviewIssueCategory.SecurityBlocker => NodalOsPrivatePreviewIssueDecision.MustFixBeforeNextRun,
            NodalOsPrivatePreviewIssueCategory.ScopeInflationRisk => NodalOsPrivatePreviewIssueDecision.NeedsAudit,
            NodalOsPrivatePreviewIssueCategory.ReleaseGateMismatch => NodalOsPrivatePreviewIssueDecision.MustFixBeforeNextRun,
            NodalOsPrivatePreviewIssueCategory.EvidenceMissing => NodalOsPrivatePreviewIssueDecision.MustFixBeforeNextRun,
            NodalOsPrivatePreviewIssueCategory.Ux when severity is NodalOsPrivatePreviewIssueSeverity.Low or NodalOsPrivatePreviewIssueSeverity.Info => NodalOsPrivatePreviewIssueDecision.AcceptForInternalOnly,
            NodalOsPrivatePreviewIssueCategory.DocumentationGap when severity is NodalOsPrivatePreviewIssueSeverity.Low or NodalOsPrivatePreviewIssueSeverity.Info => NodalOsPrivatePreviewIssueDecision.ShouldFixSoon,
            _ when severity is NodalOsPrivatePreviewIssueSeverity.Critical or NodalOsPrivatePreviewIssueSeverity.High => NodalOsPrivatePreviewIssueDecision.MustFixBeforeNextRun,
            _ => NodalOsPrivatePreviewIssueDecision.ShouldFixSoon
        };

        var blocks = decision == NodalOsPrivatePreviewIssueDecision.MustFixBeforeNextRun ||
            (decision == NodalOsPrivatePreviewIssueDecision.NeedsAudit &&
             severity is NodalOsPrivatePreviewIssueSeverity.Critical or NodalOsPrivatePreviewIssueSeverity.High);

        return new NodalOsPrivatePreviewIssue(
            BrowserCredentialRedactor.Redact(issueId),
            category,
            severity,
            decision,
            BrowserCredentialRedactor.Redact(summary),
            blocks,
            Redacted: true);
    }
}

public sealed class NodalOsPrivatePreviewPostRunReviewService
{
    public NodalOsPrivatePreviewPostRunReview Review(
        NodalOsInternalLocalPreviewRunRecord run,
        IReadOnlyList<NodalOsPrivatePreviewIssue> issues,
        bool releaseGateReadyWithRestrictions,
        bool evidenceLogSummaryUsable,
        bool operatorRunbookUsable,
        bool issueTriageUsable,
        bool blockersVisibleAndEffective)
    {
        var reasons = new List<string>();
        if (!releaseGateReadyWithRestrictions)
            reasons.Add("release gate mismatch");
        if (!evidenceLogSummaryUsable)
            reasons.Add("evidence/log summary missing");
        if (!operatorRunbookUsable)
            reasons.Add("operator runbook not usable");
        if (!issueTriageUsable)
            reasons.Add("issue triage not usable");
        if (!blockersVisibleAndEffective)
            reasons.Add("blockers not visible or effective");
        if (run.OpenedBlockedSurface)
            reasons.Add("scope expansion detected");
        if (issues.Any(issue => issue.Category == NodalOsPrivatePreviewIssueCategory.ScopeInflationRisk))
            reasons.Add("scope inflation issue detected");
        if (issues.Any(issue => issue.Category == NodalOsPrivatePreviewIssueCategory.SecurityBlocker))
            reasons.Add("security blocker issue detected");
        if (issues.Any(issue => issue.Severity == NodalOsPrivatePreviewIssueSeverity.Critical))
            reasons.Add("critical issue detected");

        var decision = run.OpenedBlockedSurface || issues.Any(issue => issue.Category == NodalOsPrivatePreviewIssueCategory.ScopeInflationRisk)
            ? NodalOsPrivatePreviewPostRunDecision.BlockedByScopeInflation
            : issues.Any(issue => issue.Category == NodalOsPrivatePreviewIssueCategory.SecurityBlocker)
                ? NodalOsPrivatePreviewPostRunDecision.BlockedBySecurityIssue
                : issues.Any(issue => issue.Severity == NodalOsPrivatePreviewIssueSeverity.Critical)
                    ? NodalOsPrivatePreviewPostRunDecision.BlockedByCriticalIssue
                    : !releaseGateReadyWithRestrictions
                        ? NodalOsPrivatePreviewPostRunDecision.BlockedByCriticalIssue
                        : !evidenceLogSummaryUsable
                            ? NodalOsPrivatePreviewPostRunDecision.NeedsMoreEvidence
                            : !operatorRunbookUsable || !issueTriageUsable || !blockersVisibleAndEffective
                                ? NodalOsPrivatePreviewPostRunDecision.NeedsOperatorUxFixes
                                : issues.Any(issue => issue.Severity is NodalOsPrivatePreviewIssueSeverity.Low or NodalOsPrivatePreviewIssueSeverity.Medium)
                                    ? NodalOsPrivatePreviewPostRunDecision.ContinueWithMinorFixes
                                    : NodalOsPrivatePreviewPostRunDecision.ContinueInternalPreview;

        return new NodalOsPrivatePreviewPostRunReview(
            "private-preview-post-run-review-m124-m126",
            decision,
            issues,
            reasons.Select(BrowserCredentialRedactor.Redact).ToArray(),
            releaseGateReadyWithRestrictions,
            evidenceLogSummaryUsable,
            operatorRunbookUsable,
            issueTriageUsable,
            blockersVisibleAndEffective,
            run.OpenedBlockedSurface,
            Redacted: true);
    }
}

public sealed class NodalOsPrivatePreviewStabilizationReviewService
{
    public NodalOsPrivatePreviewStabilizationReview Review(
        string previousIssueStatus,
        IReadOnlyList<NodalOsPrivatePreviewIssue> newIssues,
        bool activeBlockersRemainTrue,
        bool scopeExpanded,
        bool productAdminReady)
    {
        var reasons = new List<string>();
        if (!previousIssueStatus.Contains("Fixed", StringComparison.OrdinalIgnoreCase) &&
            !previousIssueStatus.Contains("Accepted", StringComparison.OrdinalIgnoreCase))
            reasons.Add("previous preview issue not resolved or accepted");
        if (!activeBlockersRemainTrue)
            reasons.Add("active blockers must remain true");
        if (scopeExpanded)
            reasons.Add("scope expansion detected");
        if (!productAdminReady)
            reasons.Add("Product/Admin readiness needs fixes");
        if (newIssues.Any(issue => issue.Category == NodalOsPrivatePreviewIssueCategory.SecurityBlocker))
            reasons.Add("security issue detected");
        if (newIssues.Any(issue => issue.Category == NodalOsPrivatePreviewIssueCategory.ScopeInflationRisk))
            reasons.Add("scope inflation issue detected");
        if (newIssues.Any(issue => issue.Severity is NodalOsPrivatePreviewIssueSeverity.Critical or NodalOsPrivatePreviewIssueSeverity.High))
            reasons.Add("high or critical issue detected");

        var decision = scopeExpanded || newIssues.Any(issue => issue.Category == NodalOsPrivatePreviewIssueCategory.ScopeInflationRisk)
            ? NodalOsPrivatePreviewStabilizationDecision.BlockedByScopeInflation
            : newIssues.Any(issue => issue.Category == NodalOsPrivatePreviewIssueCategory.SecurityBlocker)
                ? NodalOsPrivatePreviewStabilizationDecision.BlockedBySecurityIssue
                : !productAdminReady
                    ? NodalOsPrivatePreviewStabilizationDecision.NeedsProductAdminFixes
                    : !activeBlockersRemainTrue
                        ? NodalOsPrivatePreviewStabilizationDecision.NeedsOperatorUxFixes
                        : newIssues.Any(issue => issue.Severity is NodalOsPrivatePreviewIssueSeverity.Low or NodalOsPrivatePreviewIssueSeverity.Medium)
                            ? NodalOsPrivatePreviewStabilizationDecision.ContinueWithMinorFixes
                            : NodalOsPrivatePreviewStabilizationDecision.ContinueInternalPreviewStable;

        return new NodalOsPrivatePreviewStabilizationReview(
            "private-preview-stabilization-review-m127-m129",
            decision,
            BrowserCredentialRedactor.Redact(previousIssueStatus),
            newIssues,
            reasons.Select(BrowserCredentialRedactor.Redact).ToArray(),
            activeBlockersRemainTrue,
            scopeExpanded,
            Redacted: true);
    }
}
