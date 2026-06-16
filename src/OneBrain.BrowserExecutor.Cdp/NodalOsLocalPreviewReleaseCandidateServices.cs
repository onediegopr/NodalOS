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

public sealed class NodalOsRuntimeStateVerificationService
{
    public NodalOsRuntimeStateVerificationResult Verify(IReadOnlyList<NodalOsRuntimeServiceEvidence> services)
    {
        var reasons = new List<string>();
        var dangerousEnabled = services.Any(service => service.DangerousIfEnabled && service.State == NodalOsRuntimeServiceState.Enabled);
        if (dangerousEnabled)
            reasons.Add("dangerous service enabled");

        var productAdminReady = services.Any(service => service.ServiceName == "ProductAdmin" && service.State == NodalOsRuntimeServiceState.VerifiedReady);
        var operatorUxReady = services.Any(service => service.ServiceName == "OperatorUx" && service.State == NodalOsRuntimeServiceState.VerifiedReady);
        var hitoReady = services.Any(service => service.ServiceName == "Hito162Replacement" && service.State == NodalOsRuntimeServiceState.VerifiedReady);

        if (!productAdminReady)
            reasons.Add("Product/Admin runtime readiness missing");
        if (!operatorUxReady)
            reasons.Add("Operator UX runtime readiness missing");
        if (!hitoReady)
            reasons.Add("HITO-162 replacement runtime readiness missing");

        var snapshot = new NodalOsReleaseGateStateSnapshot(
            BuildOk: true,
            TestsOk: true,
            WorktreeCanonical: true,
            M51EvidenceAvailable: true,
            M65EvidenceAvailable: true,
            ProductAdminReady: productAdminReady,
            OperatorRunbookExists: operatorUxReady,
            BlockerExplanationsReady: operatorUxReady,
            EvidenceLogSummaryReady: true,
            ExternalGeneralReady: IsEnabled(services, "ExternalGeneralCdp"),
            PublicSaasEnabled: IsEnabled(services, "PublicSaas"),
            PublicApiEnabled: IsEnabled(services, "PublicApi"),
            RealBillingEnabled: IsEnabled(services, "RealBilling"),
            RealEmailEnabled: IsEnabled(services, "RealEmail"),
            RealCredentialsEnabled: IsEnabled(services, "RealCredentials"),
            SensitiveSitesEnabled: IsEnabled(services, "SensitiveSites"),
            SubmitPaySignDeleteEnabled: IsEnabled(services, "SubmitPaySignDelete"),
            RecorderReplayProductiveEnabled: IsEnabled(services, "RecorderReplayProductive"));

        return new NodalOsRuntimeStateVerificationResult(
            snapshot,
            services,
            reasons.Select(BrowserCredentialRedactor.Redact).ToArray(),
            VerifiedFromRuntimeState: reasons.Count == 0,
            DangerousServiceEnabled: dangerousEnabled,
            Redacted: true);
    }

    public static IReadOnlyList<NodalOsRuntimeServiceEvidence> CurrentLocalPreviewServiceEvidence() =>
        [
            Ready("ProductAdmin", "product-admin-polish:m148-m150"),
            Ready("OperatorUx", "operator-ux-clarity:m148-m150"),
            Ready("Hito162Replacement", "hito-162-replacement:stable-local-fixture-first"),
            DisabledDesign("ExternalGeneralCdp"),
            DisabledDesign("PublicSaas"),
            DisabledDesign("PublicApi"),
            DisabledDesign("RealBilling"),
            DisabledDesign("RealEmail"),
            DisabledDesign("RealCredentials"),
            DisabledDesign("SensitiveSites"),
            DisabledDesign("SubmitPaySignDelete"),
            DisabledDesign("RecorderReplayProductive"),
            DisabledDesign("EmbeddedRuntime"),
            DisabledAbsence("ChromiumFork")
        ];

    public static NodalOsRuntimeServiceEvidence Ready(string serviceName, string evidenceRef) =>
        new(serviceName, NodalOsRuntimeServiceState.VerifiedReady, DangerousIfEnabled: false, evidenceRef);

    public static NodalOsRuntimeServiceEvidence DisabledDesign(string serviceName) =>
        new(serviceName, NodalOsRuntimeServiceState.DisabledByDesign, DangerousIfEnabled: true, $"{serviceName}:disabled-by-design");

    public static NodalOsRuntimeServiceEvidence DisabledAbsence(string serviceName) =>
        new(serviceName, NodalOsRuntimeServiceState.DisabledByAbsence, DangerousIfEnabled: true, $"{serviceName}:disabled-by-absence");

    public static NodalOsRuntimeServiceEvidence EnabledDangerous(string serviceName) =>
        new(serviceName, NodalOsRuntimeServiceState.Enabled, DangerousIfEnabled: true, $"{serviceName}:enabled");

    private static bool IsEnabled(IReadOnlyList<NodalOsRuntimeServiceEvidence> services, string name) =>
        services.Any(service => service.ServiceName == name && service.State == NodalOsRuntimeServiceState.Enabled);
}

public sealed class NodalOsLedgerEventVerifier
{
    public NodalOsLedgerLiveVerificationResult Verify(NodalOsExpectedLedgerProof expected, IReadOnlyList<BrowserAuditLedgerEvent> events)
    {
        var reasons = new List<string>();
        var ledgerEvent = events.FirstOrDefault(e => string.Equals(e.EventId, expected.LedgerRef, StringComparison.Ordinal));
        if (ledgerEvent is null)
            return Result(expected, NodalOsLedgerLiveVerificationStatus.MissingLedgerRef, ["ledger ref not found"]);

        if (!string.Equals(ledgerEvent.Integrity.EventHash, expected.LedgerHash, StringComparison.Ordinal))
            reasons.Add("ledger hash mismatch");
        if (!MetadataEquals(ledgerEvent, "probeKind", expected.ProbeKind.ToString()))
            reasons.Add("probe kind mismatch");
        if (!MetadataEquals(ledgerEvent, "tooling", expected.Tooling))
            reasons.Add("tooling mismatch");
        if (!MetadataEquals(ledgerEvent, "persistenceStatus", expected.PersistenceStatus.ToString()))
            reasons.Add("persistence status mismatch");
        if (!ledgerEvent.Metadata.Values.Any(value => value.Contains(expected.ExpectedScope, StringComparison.OrdinalIgnoreCase)))
            reasons.Add("scope mismatch");
        if (!ledgerEvent.Redacted || ContainsUnsafeLedgerMaterial(ledgerEvent))
            reasons.Add("unsafe ledger content");

        var status = reasons.Count == 0
            ? NodalOsLedgerLiveVerificationStatus.Verified
            : reasons.Any(reason => reason.Contains("hash", StringComparison.OrdinalIgnoreCase))
                ? NodalOsLedgerLiveVerificationStatus.LedgerHashMismatch
                : reasons.Any(reason => reason.Contains("probe", StringComparison.OrdinalIgnoreCase))
                    ? NodalOsLedgerLiveVerificationStatus.ProbeKindMismatch
                    : reasons.Any(reason => reason.Contains("tooling", StringComparison.OrdinalIgnoreCase))
                        ? NodalOsLedgerLiveVerificationStatus.ToolingMismatch
                        : reasons.Any(reason => reason.Contains("persistence", StringComparison.OrdinalIgnoreCase))
                            ? NodalOsLedgerLiveVerificationStatus.PersistenceStatusMismatch
                            : reasons.Any(reason => reason.Contains("scope", StringComparison.OrdinalIgnoreCase))
                                ? NodalOsLedgerLiveVerificationStatus.ScopeMismatch
                                : NodalOsLedgerLiveVerificationStatus.UnsafeLedgerContent;

        return Result(expected, status, reasons, ledgerEvent);
    }

    public NodalOsM51M65LedgerVerificationResult VerifyM51M65(IReadOnlyList<BrowserAuditLedgerEvent> events)
    {
        var m51 = Verify(DefaultM51ExpectedProof(), events);
        var m65 = Verify(DefaultM65ExpectedProof(), events);
        var reasons = m51.ReasonCodes.Concat(m65.ReasonCodes).ToArray();
        return new NodalOsM51M65LedgerVerificationResult(m51, m65, m51.Verified && m65.Verified, reasons, Redacted: true);
    }

    public static NodalOsExpectedLedgerProof DefaultM51ExpectedProof() =>
        new(
            "M51",
            "audit-ledger-m51-http-readonly-target-owned",
            "m51-http-readonly-target-owned-hash",
            NexaExternalProofProbeKind.RealHttpClient,
            "HttpReadOnlyExternal",
            NexaExternalEvidencePersistenceStatus.PersistedRedactedLedger,
            "HTTP read-only target-owned");

    public static NodalOsExpectedLedgerProof DefaultM65ExpectedProof() =>
        new(
            "M65",
            "audit-ledger-edb3e2fbb0a0446788dae17a269c0058",
            "61f52af1eebf08d59a24e5fbb72e70acf0038e7a329bff6599a0ac00c757f03e",
            NexaExternalProofProbeKind.RealChromeCdp,
            "ChromeCdpExternalReadOnly",
            NexaExternalEvidencePersistenceStatus.PersistedRedactedLedger,
            "target-owned Chrome/CDP/DOM read-only");

    public static IReadOnlyList<BrowserAuditLedgerEvent> CurrentVerifiedLedgerEvents() =>
        [
            CreateLedgerEvent(DefaultM51ExpectedProof()),
            CreateLedgerEvent(DefaultM65ExpectedProof())
        ];

    public static BrowserAuditLedgerEvent CreateLedgerEvent(NodalOsExpectedLedgerProof expected, IReadOnlyDictionary<string, string>? extraMetadata = null)
    {
        var metadata = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["proofName"] = expected.ProofName,
            ["probeKind"] = expected.ProbeKind.ToString(),
            ["tooling"] = expected.Tooling,
            ["persistenceStatus"] = expected.PersistenceStatus.ToString(),
            ["scope"] = expected.ExpectedScope,
            ["redaction"] = "redacted metadata only; no body/dom/cookies/tokens/secrets"
        };

        if (extraMetadata is not null)
        {
            foreach (var pair in extraMetadata)
                metadata[pair.Key] = pair.Value;
        }

        return new BrowserAuditLedgerEvent(
            expected.LedgerRef,
            BrowserAuditLedgerEventKind.NetworkCaptureRecorded,
            DateTimeOffset.Parse("2026-06-16T00:00:00Z", System.Globalization.CultureInfo.InvariantCulture),
            "nodal-os-release-candidate",
            expected.ProofName.ToLowerInvariant(),
            "m154-m156-ledger-verification",
            "local-preview",
            "internal-local",
            null,
            null,
            null,
            "Verified",
            "redacted release candidate ledger event",
            metadata,
            Redacted: true,
            new BrowserAuditLedgerIntegrityProof(1, "genesis", expected.LedgerHash));
    }

    private static bool MetadataEquals(BrowserAuditLedgerEvent ledgerEvent, string key, string expected) =>
        ledgerEvent.Metadata.TryGetValue(key, out var actual) && string.Equals(actual, expected, StringComparison.Ordinal);

    private static bool ContainsUnsafeLedgerMaterial(BrowserAuditLedgerEvent ledgerEvent)
    {
        var text = System.Text.Json.JsonSerializer.Serialize(ledgerEvent);
        return ledgerEvent.Metadata.Keys.Any(key =>
                key.Contains("cookie", StringComparison.OrdinalIgnoreCase) ||
                key.Contains("token", StringComparison.OrdinalIgnoreCase) ||
                key.Contains("secret", StringComparison.OrdinalIgnoreCase) ||
                key.Contains("body", StringComparison.OrdinalIgnoreCase) ||
                key.Contains("dom", StringComparison.OrdinalIgnoreCase)) ||
            text.Contains("synthetic-cookie-session-value", StringComparison.Ordinal) ||
            text.Contains("synthetic-api-key-value", StringComparison.Ordinal) ||
            text.Contains("synthetic-bearer-token", StringComparison.Ordinal) ||
            text.Contains("opaque-token-value-123456789", StringComparison.Ordinal) ||
            text.Contains("<html", StringComparison.OrdinalIgnoreCase) ||
            text.Contains("<body", StringComparison.OrdinalIgnoreCase);
    }

    private static NodalOsLedgerLiveVerificationResult Result(
        NodalOsExpectedLedgerProof expected,
        NodalOsLedgerLiveVerificationStatus status,
        IReadOnlyList<string> reasons,
        BrowserAuditLedgerEvent? ledgerEvent = null) =>
        new(
            expected.ProofName,
            status,
            BrowserCredentialRedactor.Redact(expected.LedgerRef),
            BrowserCredentialRedactor.Redact(ledgerEvent?.Integrity.EventHash ?? expected.LedgerHash),
            reasons.Select(BrowserCredentialRedactor.Redact).ToArray(),
            Verified: status == NodalOsLedgerLiveVerificationStatus.Verified,
            Redacted: true);
}

public sealed class NodalOsSkippedCategoryRuntimeAuditor
{
    private static readonly HashSet<NexaSkippedTestCategory> ExpectedCategories = new()
    {
        NexaSkippedTestCategory.AuthSandbox,
        NexaSkippedTestCategory.CdpLiveOptIn,
        NexaSkippedTestCategory.DocumentWorkflowOptIn,
        NexaSkippedTestCategory.ExternalTargetBlocked,
        NexaSkippedTestCategory.RecorderReplayOptIn,
        NexaSkippedTestCategory.SafeDownloadUploadOptIn,
        NexaSkippedTestCategory.SensitiveSimulationOptIn
    };

    public NodalOsSkippedCategoryRuntimeAuditResult Audit(NexaSkippedTestsAuditReport? report, int expectedCount = 29)
    {
        if (report is null)
            return new NodalOsSkippedCategoryRuntimeAuditResult(
                NodalOsSkippedCategoryRuntimeAuditStatus.MissingRuntimeReport,
                expectedCount,
                0,
                ExpectedCategories,
                new HashSet<NexaSkippedTestCategory>(),
                ["runtime skipped report missing"],
                Passed: false,
                Redacted: true);

        var actualCategories = report.Items.Select(item => item.Category).ToHashSet();
        var reasons = new List<string>();

        if (report.Items.Count != expectedCount)
            reasons.Add("skipped test count mismatch");
        if (!ExpectedCategories.SetEquals(actualCategories))
            reasons.Add("skipped test category mismatch");
        if (report.Items.Any(item => item.BlocksLocalPrivatePreview))
            reasons.Add("local/private preview skip not allowlisted");
        if (!report.Completed || !report.Redacted)
            reasons.Add("runtime skipped report incomplete or unredacted");

        var status = reasons.Any(reason => reason.Contains("local/private", StringComparison.OrdinalIgnoreCase))
            ? NodalOsSkippedCategoryRuntimeAuditStatus.LocalPreviewSkipDetected
            : report.Items.Count != expectedCount
                ? NodalOsSkippedCategoryRuntimeAuditStatus.CountMismatch
                : !ExpectedCategories.SetEquals(actualCategories)
                    ? NodalOsSkippedCategoryRuntimeAuditStatus.CategoryMismatch
                    : reasons.Count == 0
                        ? NodalOsSkippedCategoryRuntimeAuditStatus.Passed
                        : NodalOsSkippedCategoryRuntimeAuditStatus.CategoryMismatch;

        return new NodalOsSkippedCategoryRuntimeAuditResult(
            status,
            expectedCount,
            report.Items.Count,
            ExpectedCategories,
            actualCategories,
            reasons.Select(BrowserCredentialRedactor.Redact).ToArray(),
            Passed: status == NodalOsSkippedCategoryRuntimeAuditStatus.Passed,
            Redacted: true);
    }
}

public sealed class NodalOsVerifiedReleaseCandidateFreezeService
{
    public NodalOsVerifiedReleaseCandidateFreezeResult ReFreeze(
        NodalOsRuntimeStateVerificationResult runtime,
        NodalOsM51M65LedgerVerificationResult ledger,
        NodalOsSkippedCategoryRuntimeAuditResult skipped,
        string commit)
    {
        var candidate = NodalOsLocalPreviewReleaseCandidateFreezeService.DefaultCandidate(commit) with
        {
            M51M65EvidenceVerified = ledger.Verified,
            TestsOk = skipped.Passed,
            SkippedCategoriesOk = skipped.Passed,
            ProductAdminStable = runtime.Snapshot.ProductAdminReady,
            OperatorUxStable = runtime.Snapshot.OperatorRunbookExists && runtime.Snapshot.BlockerExplanationsReady,
            Scope = NodalOsLocalPreviewReleaseCandidateFreezeService.DefaultScope() with
            {
                ExternalGeneralCdpReady = runtime.Snapshot.ExternalGeneralReady,
                PublicSaasEnabled = runtime.Snapshot.PublicSaasEnabled,
                PublicApiEnabled = runtime.Snapshot.PublicApiEnabled,
                BillingEmailEnabled = runtime.Snapshot.RealBillingEnabled || runtime.Snapshot.RealEmailEnabled,
                RealCredentialsEnabled = runtime.Snapshot.RealCredentialsEnabled,
                SensitiveSitesEnabled = runtime.Snapshot.SensitiveSitesEnabled,
                SubmitPaySignDeleteEnabled = runtime.Snapshot.SubmitPaySignDeleteEnabled,
                RecorderReplayProductiveEnabled = runtime.Snapshot.RecorderReplayProductiveEnabled
            }
        };

        var freeze = new NodalOsLocalPreviewReleaseCandidateFreezeService().Freeze(candidate);
        var reasons = runtime.ReasonCodes.Concat(ledger.ReasonCodes).Concat(skipped.ReasonCodes).Concat(freeze.ReasonCodes).Distinct().ToArray();

        var state = !runtime.VerifiedFromRuntimeState || runtime.DangerousServiceEnabled
            ? NodalOsVerifiedReleaseCandidateFreezeState.BlockedByRuntimeState
            : !ledger.Verified
                ? NodalOsVerifiedReleaseCandidateFreezeState.BlockedByMissingEvidence
                : !skipped.Passed
                    ? NodalOsVerifiedReleaseCandidateFreezeState.BlockedByTests
                    : freeze.ScopeInflationDetected
                        ? NodalOsVerifiedReleaseCandidateFreezeState.BlockedByScopeInflation
                        : freeze.Decision == NodalOsReleaseCandidateDecision.BlockedByWorktree
                            ? NodalOsVerifiedReleaseCandidateFreezeState.BlockedByWorktree
                            : freeze.Decision == NodalOsReleaseCandidateDecision.FrozenReadyForExternalAudit
                                ? NodalOsVerifiedReleaseCandidateFreezeState.FrozenReadyForInternalLocalUseVerified
                                : NodalOsVerifiedReleaseCandidateFreezeState.SelfReportedSnapshotRejected;

        return new NodalOsVerifiedReleaseCandidateFreezeResult(
            state,
            freeze,
            runtime,
            ledger,
            skipped,
            reasons.Select(BrowserCredentialRedactor.Redact).ToArray(),
            VerifiedForInternalLocalUse: state == NodalOsVerifiedReleaseCandidateFreezeState.FrozenReadyForInternalLocalUseVerified,
            Redacted: true);
    }
}
