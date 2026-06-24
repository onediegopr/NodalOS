using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("LocalHostVisibleNoopSmokeHarness")]
[TestCategory("M969")]
[TestCategory("M970")]
[TestCategory("M971")]
[TestCategory("M972")]
[TestCategory("M973")]
[TestCategory("M974")]
[TestCategory("M975")]
[TestCategory("M976")]
[TestCategory("M977")]
[TestCategory("M978")]
[TestCategory("M979")]
[TestCategory("M980")]
[TestCategory("M969M980")]
public sealed class NodalOsLocalHostVisibleNoopSmokeHarnessM969M980Tests
{
    [TestMethod]
    public void harness_prep_is_protocol_only_test_only_and_noop_only()
    {
        var harness = LocalHostVisibleNoopSmokeHarnessPrep.Create();

        Assert.AreEqual("PrepOnly", harness.HarnessMode);
        Assert.AreEqual("ProtocolOnly", harness.ExecutionMode);
        Assert.AreEqual("TestOnly", harness.FoundationMode);
        Assert.AreEqual("NO-GO", harness.ManualQaExecution);
        Assert.AreEqual("NO-GO", harness.PcCommanderReal);
        Assert.AreEqual("NO-GO", harness.ProductiveRuntime);
        CollectionAssert.Contains(harness.FutureVisibleSurfaces.ToArray(), "local host visibility");
        CollectionAssert.Contains(harness.FutureVisibleSurfaces.ToArray(), "dangerous command blocked visibility");
        CollectionAssert.Contains(harness.AllowedCommandKinds.ToArray(), "SafeNoOp");
        CollectionAssert.Contains(harness.AllowedCommandKinds.ToArray(), "MetadataFixtureOnly");
        CollectionAssert.Contains(harness.ForbiddenCommandKinds.ToArray(), "shell");
        CollectionAssert.Contains(harness.ForbiddenCommandKinds.ToArray(), "filesystem scan");
        Assert.IsFalse(harness.HostRealCreated);
        Assert.IsFalse(harness.SmokeRealExecuted);
        Assert.IsFalse(harness.RealPortOpened);
        Assert.IsFalse(harness.BrowserAutomationReal);
        Assert.IsFalse(harness.ProductFilesModified);
        Assert.IsFalse(harness.BridgeCspModified);
    }

    [TestMethod]
    public void harness_descriptor_no_execution_boundary_measures_zero_side_effects()
    {
        var sink = new HarnessSideEffectSink();
        var descriptor = HarnessDescriptor.Create(sink);

        Assert.AreEqual("harness-m970-1", descriptor.HarnessId);
        Assert.AreEqual("PrepOnly", descriptor.HarnessMode);
        Assert.AreEqual("NOT_READY_EVIDENCE_PENDING", descriptor.ManualQaStatus);
        Assert.AreEqual("OPEN_BROWSER_RUNTIME_SMOKE_CLEANUP_EXTERNAL_QUARANTINED_VISIBLE", descriptor.CaveatStatus);
        Assert.AreEqual(0, descriptor.Boundary.ShellInvocations);
        Assert.AreEqual(0, descriptor.Boundary.FilesystemWriteInvocations);
        Assert.AreEqual(0, descriptor.Boundary.FilesystemReadRealInvocations);
        Assert.AreEqual(0, descriptor.Boundary.NetworkInvocations);
        Assert.AreEqual(0, descriptor.Boundary.BrowserAutomationInvocations);
        Assert.AreEqual(0, descriptor.Boundary.ProviderCloudInvocations);
        Assert.AreEqual(0, descriptor.Boundary.CredentialAccessInvocations);
        Assert.AreEqual(0, descriptor.Boundary.ProcessMutationInvocations);
        Assert.AreEqual(0, descriptor.Boundary.BridgeCspModifications);
        Assert.AreEqual(0, descriptor.Boundary.ProductFilesModifications);
        Assert.IsTrue(descriptor.Boundary.IsClean);

        sink.Record("shell");
        var dirty = HarnessDescriptor.Create(sink);
        Assert.IsFalse(dirty.Boundary.IsClean);
        Assert.AreEqual(1, dirty.Boundary.ShellInvocations);
    }

    [TestMethod]
    public void observable_output_contract_redacts_secrets_and_rejects_unsafe_outputs()
    {
        var output = HarnessObservableOutputContract.Create("sk_test_NODAL_OS_FAKE_KEY Authorization: Bearer FAKE_TOKEN C:\\Users\\diego\\secret");

        Assert.AreEqual("host-visible-indicator", output.HostVisibleIndicator);
        Assert.AreEqual("liveness-visible-indicator", output.LivenessIndicator);
        Assert.AreEqual("run-harness-1", output.RunId);
        Assert.AreEqual("cmd-safe-noop-1", output.CommandId);
        Assert.AreEqual("adapter-controlled-noop-1", output.AdapterId);
        Assert.AreEqual("completed_noop_fixture", output.SafeNoOpResult);
        Assert.AreEqual("completed_metadata_fixture", output.MetadataFixtureResult);
        Assert.AreEqual("blocked_dangerous_fixture", output.DangerousBlockedResult);
        Assert.AreEqual("redacted", output.RedactionStatus);
        Assert.IsFalse(output.OperatorNote.Contains("sk_test_NODAL_OS_FAKE_KEY", StringComparison.Ordinal));
        Assert.IsFalse(output.OperatorNote.Contains("FAKE_TOKEN", StringComparison.Ordinal));
        Assert.IsFalse(output.RawSecrets);
        Assert.IsFalse(output.RawCredentials);
        Assert.IsFalse(output.FullUnredactedSensitivePaths);
        Assert.IsFalse(output.UnsafePayloads);
        Assert.IsFalse(output.ShellOutputReal);
        Assert.IsFalse(output.FilesystemScanOutputReal);
        Assert.IsFalse(output.ProviderCloudLogsReal);
        Assert.IsFalse(output.BrowserAutomationProductiveLogs);
    }

    [TestMethod]
    public void human_evidence_capture_gate_ready_but_manual_qa_execution_no_go()
    {
        var gate = HumanEvidenceCaptureGate.Create();

        Assert.AreEqual("READY", gate.GateStatus);
        Assert.AreEqual("NO-GO", gate.ManualQaExecution);
        Assert.AreEqual("NOT_READY_EVIDENCE_PENDING", gate.ManualQaTrigger);
        CollectionAssert.Contains(gate.RequiredEvidence.ToArray(), "host visible evidence");
        CollectionAssert.Contains(gate.RequiredEvidence.ToArray(), "external caveat documented");
        CollectionAssert.Contains(gate.AllowedStates.ToArray(), "EVIDENCE_PENDING");
        CollectionAssert.Contains(gate.AllowedStates.ToArray(), "FUTURE_QA_GO_ELIGIBLE");
        Assert.IsFalse(gate.CanDeclareQaReady);
        Assert.IsFalse(gate.HumanEvidenceInvented);
    }

    [TestMethod]
    public void evidence_capture_intake_schema_rejects_unsafe_claims_and_unknown_types()
    {
        var schema = EvidenceCaptureIntakeSchema.Create();

        CollectionAssert.Contains(schema.AllowedCaptureTypes.ToArray(), "host_visible");
        CollectionAssert.Contains(schema.AllowedCaptureTypes.ToArray(), "external_caveat");
        Assert.IsTrue(schema.Accepts("trace_visible", "redacted operator note"));
        Assert.IsFalse(schema.Accepts("unknown", "redacted operator note"));
        Assert.IsFalse(schema.Accepts("host_visible", "manual QA passed"));
        Assert.IsFalse(schema.Accepts("host_visible", "PC Commander real ready"));
        Assert.IsFalse(schema.Accepts("host_visible", "productive runtime enabled"));
        Assert.IsFalse(schema.Accepts("host_visible", "Authorization: Bearer FAKE_TOKEN"));
        Assert.IsFalse(schema.Accepts("host_visible", "-----BEGIN PRIVATE KEY-----"));
    }

    [TestMethod]
    public void evidence_redaction_leak_guard_blocks_representative_fake_payloads()
    {
        var guard = EvidenceRedactionLeakGuard.Create();
        var payloads = new[]
        {
            "Authorization: Bearer FAKE_TOKEN_FOR_TEST_ONLY",
            "password=FAKE_PASSWORD_FOR_TEST_ONLY",
            "-----BEGIN PRIVATE KEY----- FAKE PRIVATE KEY -----END PRIVATE KEY-----",
            "DefaultEndpointsProtocol=https;AccountKey=FAKE_CONNECTION_STRING",
            "Cookie: session=FAKE_BROWSER_SESSION",
            "C:\\Users\\diego\\OneDrive\\secret\\file.txt",
            "powershell Remove-Item C:\\temp\\x",
            "OPENAI_API_KEY=FAKE_PROVIDER_KEY"
        };

        foreach (var payload in payloads)
        {
            var result = guard.Review(payload);
            Assert.AreEqual("blocked", result.ReviewStatus, payload);
            Assert.AreEqual("redacted", result.RedactionStatus, payload);
            Assert.IsFalse(result.SafeSummary.Contains("FAKE_TOKEN", StringComparison.Ordinal), payload);
            Assert.IsFalse(result.SafeSummary.Contains("FAKE_PASSWORD", StringComparison.Ordinal), payload);
            Assert.IsFalse(result.SafeSummary.Contains("PRIVATE KEY", StringComparison.Ordinal), payload);
            Assert.IsFalse(result.SafeSummary.Contains("Remove-Item", StringComparison.Ordinal), payload);
        }

        var safe = guard.Review("host visible indicator present");
        Assert.AreEqual("accepted_for_review", safe.ReviewStatus);
        Assert.AreEqual("clean", safe.RedactionStatus);
    }

    [TestMethod]
    public void operator_capture_checklist_pack_blocks_unsafe_captures()
    {
        var checklist = OperatorCaptureChecklistPack.Create();

        CollectionAssert.Contains(checklist.BeforeStartingCapture.ToArray(), "repo/branch/head correctos");
        CollectionAssert.Contains(checklist.BeforeStartingCapture.ToArray(), "external smoke caveat documented");
        CollectionAssert.Contains(checklist.AllowedCaptures.ToArray(), "host visible summary");
        CollectionAssert.Contains(checklist.ForbiddenCaptures.ToArray(), "secret values");
        CollectionAssert.Contains(checklist.ForbiddenCaptures.ToArray(), "shell output real");
        CollectionAssert.Contains(checklist.RequiredScreenshotsLogSummaries.ToArray(), "host visible indicator summary");
        CollectionAssert.Contains(checklist.AbortImmediatelyIf.ToArray(), "Bridge/CSP changed");
        CollectionAssert.Contains(checklist.PassEvidencePackageRequirements.ToArray(), "redaction checklist completed");
        Assert.AreEqual("operator confirms protocol-only evidence capture; no manual QA execution", checklist.FinalOperatorStatement);
    }

    [TestMethod]
    public void manual_qa_preflight_abort_matrix_aborts_on_scope_or_safety_drift()
    {
        var matrix = ManualQaPreflightAbortMatrix.Create();
        var abortCases = new[]
        {
            "wrong worktree",
            "wrong branch",
            "dirty worktree not explained",
            "HEAD not expected and not documented",
            "product files changed",
            "Bridge/CSP changed",
            "shell route present",
            "filesystem write route present",
            "network/provider route present",
            "browser automation productive route present",
            "credential access route present",
            "dangerous command not blocked",
            "redaction proof missing",
            "no-side-effect proof missing",
            "external caveat hidden",
            "operator cannot capture required evidence"
        };

        foreach (var abortCase in abortCases)
        {
            Assert.IsTrue(matrix.ShouldAbort(abortCase), abortCase);
        }

        Assert.AreEqual("NO-GO", matrix.ManualQaExecution);
        Assert.AreEqual("NOT_READY_EVIDENCE_PENDING", matrix.ManualQaTrigger);
    }

    [TestMethod]
    public void human_evidence_review_result_contract_blocks_manual_qa_passed_without_reviewed_evidence()
    {
        var contract = HumanEvidenceReviewResultContract.Create();

        CollectionAssert.Contains(contract.AllowedStates.ToArray(), "REVIEW_NOT_STARTED");
        CollectionAssert.Contains(contract.AllowedStates.ToArray(), "REVIEW_ACCEPTED_FOR_FUTURE_MANUAL_QA_GATE");
        Assert.IsFalse(contract.AllowsClaim("manual QA passed"));
        Assert.IsFalse(contract.AllowsClaim("runtime ready"));
        Assert.IsFalse(contract.AllowsClaim("PC commander ready"));
        Assert.IsFalse(contract.AllowsClaim("release ready"));
        Assert.IsFalse(contract.AllowsClaim("productive enabled"));
    }

    [TestMethod]
    public void qa_trigger_reevaluation_guard_keeps_evidence_pending_and_no_go_boundaries()
    {
        var guard = QaTriggerReEvaluationGuard.Create();

        Assert.AreEqual("NOT_READY_EVIDENCE_PENDING", guard.ManualQaTrigger);
        Assert.AreEqual("NO-GO", guard.ManualQaExecution);
        Assert.AreEqual("READY", guard.HumanEvidenceCaptureGate);
        Assert.AreEqual("READY", guard.HarnessPrep);
        Assert.AreEqual("NO-GO", guard.PcCommanderReal);
        Assert.AreEqual("NO-GO", guard.ProductiveRuntime);
        Assert.IsFalse(guard.FutureEligibleWithoutHumanEvidence);
    }

    [TestMethod]
    public void post_harness_audit_recommendation_pack_requests_claude_audit_before_real_execution()
    {
        var pack = PostHarnessAuditRecommendationPack.Create();

        Assert.AreEqual("PEDIR AUDITORIA CLAUDE", pack.Recommendation);
        CollectionAssert.Contains(pack.BeforeJumpsTo.ToArray(), "manual QA execution");
        CollectionAssert.Contains(pack.BeforeJumpsTo.ToArray(), "runtime real");
        CollectionAssert.Contains(pack.AuditMustReview.ToArray(), "tests superficiales/tautologicos");
        CollectionAssert.Contains(pack.AuditMustReview.ToArray(), "BrowserRuntimeSmoke caveat");
        CollectionAssert.Contains(pack.AuditMustReview.ToArray(), "riesgos de convertir harness en ejecucion");
    }

    [TestMethod]
    public void m980_artifacts_exist_and_preserve_no_go_boundaries()
    {
        var root = FindRepositoryRoot();
        var required = new[]
        {
            "artifacts/agent-operations/m969/local-host-visible-noop-smoke-harness-prep.json",
            "artifacts/agent-operations/m970/harness-descriptor-no-execution-boundary.json",
            "artifacts/agent-operations/m971/harness-observable-output-contract.json",
            "artifacts/agent-operations/m972/human-evidence-capture-gate.json",
            "artifacts/agent-operations/m973/evidence-capture-intake-schema.json",
            "artifacts/agent-operations/m974/evidence-redaction-leak-guard.json",
            "artifacts/agent-operations/m975/operator-capture-checklist-pack.json",
            "artifacts/agent-operations/m976/manual-qa-preflight-abort-matrix.json",
            "artifacts/agent-operations/m977/human-evidence-review-result-contract.json",
            "artifacts/agent-operations/m978/qa-trigger-reevaluation-guard.json",
            "artifacts/agent-operations/m979/post-harness-audit-recommendation-pack.json",
            "artifacts/agent-operations/m980/local-host-visible-noop-smoke-harness-final-report.json",
            "artifacts/agent-operations/m969-m980/local-host-visible-noop-smoke-harness-go-no-go.json",
            "docs/reports/m980-local-host-visible-noop-smoke-harness-evidence-gate.md"
        };

        foreach (var path in required)
        {
            Assert.IsTrue(File.Exists(Path.Combine(root, path)), path);
        }
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "OneBrain.slnx")))
        {
            directory = directory.Parent;
        }

        Assert.IsNotNull(directory, "Repository root with OneBrain.slnx was not found.");
        return directory.FullName;
    }
}

internal sealed record LocalHostVisibleNoopSmokeHarnessPrep(
    string HarnessMode,
    string ExecutionMode,
    string FoundationMode,
    IReadOnlyList<string> FutureVisibleSurfaces,
    IReadOnlyList<string> AllowedCommandKinds,
    IReadOnlyList<string> ForbiddenCommandKinds,
    string ManualQaExecution,
    string ProductiveRuntime,
    string PcCommanderReal,
    bool HostRealCreated,
    bool SmokeRealExecuted,
    bool RealPortOpened,
    bool BrowserAutomationReal,
    bool ProductFilesModified,
    bool BridgeCspModified)
{
    public static LocalHostVisibleNoopSmokeHarnessPrep Create() =>
        new(
            "PrepOnly",
            "ProtocolOnly",
            "TestOnly",
            new[] { "local host visibility", "liveness visibility", "safe no-op command visibility", "metadata fixture command visibility", "trace/evidence visibility", "dangerous command blocked visibility", "operator-visible logs", "redaction status", "no-side-effect proof visibility" },
            new[] { "SafeNoOp", "MetadataFixtureOnly" },
            new[] { "shell", "filesystem write", "filesystem scan", "network", "browser automation", "provider/cloud", "credentials", "process mutation", "release/store" },
            "NO-GO",
            "NO-GO",
            "NO-GO",
            false,
            false,
            false,
            false,
            false,
            false);
}

internal sealed class HarnessSideEffectSink
{
    private readonly Dictionary<string, int> invocations = new(StringComparer.OrdinalIgnoreCase);

    public void Record(string kind) => invocations[kind] = Get(kind) + 1;

    public int Get(string kind) => invocations.TryGetValue(kind, out var value) ? value : 0;
}

internal sealed record HarnessDescriptor(
    string HarnessId,
    string HarnessMode,
    IReadOnlyList<string> ExpectedVisibleSurfaces,
    IReadOnlyList<string> AllowedFakeTestFixtures,
    IReadOnlyList<string> ForbiddenRealCapabilities,
    NoExecutionBoundary Boundary,
    IReadOnlyList<string> EvidenceRefs,
    IReadOnlyList<string> TraceRefs,
    string ManualQaStatus,
    string CaveatStatus)
{
    public static HarnessDescriptor Create(HarnessSideEffectSink sink) =>
        new(
            "harness-m970-1",
            "PrepOnly",
            new[] { "host visible indicator", "liveness indicator", "operator-visible logs" },
            new[] { "SafeNoOp", "MetadataFixtureOnly" },
            new[] { "shell", "filesystem", "network", "browser automation", "provider/cloud", "credentials", "process mutation", "Bridge/CSP", "product files" },
            NoExecutionBoundary.From(sink),
            new[] { "evidence-harness-m970-1" },
            new[] { "trace-harness-m970-1" },
            "NOT_READY_EVIDENCE_PENDING",
            "OPEN_BROWSER_RUNTIME_SMOKE_CLEANUP_EXTERNAL_QUARANTINED_VISIBLE");
}

internal sealed record NoExecutionBoundary(
    int ShellInvocations,
    int FilesystemWriteInvocations,
    int FilesystemReadRealInvocations,
    int NetworkInvocations,
    int BrowserAutomationInvocations,
    int ProviderCloudInvocations,
    int CredentialAccessInvocations,
    int ProcessMutationInvocations,
    int BridgeCspModifications,
    int ProductFilesModifications)
{
    public bool IsClean =>
        ShellInvocations == 0 &&
        FilesystemWriteInvocations == 0 &&
        FilesystemReadRealInvocations == 0 &&
        NetworkInvocations == 0 &&
        BrowserAutomationInvocations == 0 &&
        ProviderCloudInvocations == 0 &&
        CredentialAccessInvocations == 0 &&
        ProcessMutationInvocations == 0 &&
        BridgeCspModifications == 0 &&
        ProductFilesModifications == 0;

    public static NoExecutionBoundary From(HarnessSideEffectSink sink) =>
        new(
            sink.Get("shell"),
            sink.Get("filesystem_write"),
            sink.Get("filesystem_read_real"),
            sink.Get("network"),
            sink.Get("browser_automation"),
            sink.Get("provider_cloud"),
            sink.Get("credential_access"),
            sink.Get("process_mutation"),
            sink.Get("bridge_csp"),
            sink.Get("product_files"));
}

internal sealed record HarnessObservableOutputContract(
    string HostVisibleIndicator,
    string LivenessIndicator,
    string RunId,
    string CommandId,
    string AdapterId,
    string SafeNoOpResult,
    string MetadataFixtureResult,
    string DangerousBlockedResult,
    string TraceId,
    string EvidenceId,
    string RedactionStatus,
    string OperatorNote,
    string Timestamp,
    string CaveatStatus,
    bool RawSecrets,
    bool RawCredentials,
    bool FullUnredactedSensitivePaths,
    bool UnsafePayloads,
    bool ShellOutputReal,
    bool FilesystemScanOutputReal,
    bool ProviderCloudLogsReal,
    bool BrowserAutomationProductiveLogs)
{
    public static HarnessObservableOutputContract Create(string operatorNote) =>
        new(
            "host-visible-indicator",
            "liveness-visible-indicator",
            "run-harness-1",
            "cmd-safe-noop-1",
            "adapter-controlled-noop-1",
            "completed_noop_fixture",
            "completed_metadata_fixture",
            "blocked_dangerous_fixture",
            "trace-harness-output-1",
            "evidence-harness-output-1",
            "redacted",
            HarnessEvidenceRedactor.Redact(operatorNote),
            "logical-time-1",
            "external smoke caveat visible",
            false,
            false,
            false,
            false,
            false,
            false,
            false,
            false);
}

internal sealed record HumanEvidenceCaptureGate(
    string GateStatus,
    IReadOnlyList<string> RequiredEvidence,
    IReadOnlyList<string> AllowedStates,
    string ManualQaExecution,
    string ManualQaTrigger,
    bool CanDeclareQaReady,
    bool HumanEvidenceInvented)
{
    public static HumanEvidenceCaptureGate Create() =>
        new(
            "READY",
            new[] { "host visible evidence", "bridge connected evidence", "run visible evidence", "tab claim visible evidence", "trace/evidence visible evidence", "safe no-op logs", "metadata fixture logs", "dangerous blocked evidence", "redaction proof", "no-side-effect proof", "product files unchanged proof", "Bridge/CSP unchanged proof", "external caveat documented" },
            new[] { "EVIDENCE_PENDING", "EVIDENCE_INCOMPLETE_NO_GO", "EVIDENCE_ACCEPTED_FOR_REVIEW", "EVIDENCE_REJECTED_SCOPE_DRIFT", "EVIDENCE_REJECTED_UNSAFE", "FUTURE_QA_GO_ELIGIBLE" },
            "NO-GO",
            "NOT_READY_EVIDENCE_PENDING",
            false,
            false);
}

internal sealed record EvidenceCaptureIntakeSchema(IReadOnlyList<string> AllowedCaptureTypes, IReadOnlyList<string> RejectedClaims)
{
    public bool Accepts(string captureType, string operatorNote) =>
        AllowedCaptureTypes.Contains(captureType, StringComparer.Ordinal) &&
        !RejectedClaims.Any(claim => operatorNote.Contains(claim, StringComparison.OrdinalIgnoreCase)) &&
        EvidenceRedactionLeakGuard.Create().Review(operatorNote).ReviewStatus != "blocked";

    public static EvidenceCaptureIntakeSchema Create() =>
        new(
            new[] { "host_visible", "bridge_connected", "run_visible", "tab_claim_visible", "trace_visible", "evidence_visible", "safe_noop_log", "metadata_fixture_log", "dangerous_blocked", "redaction_proof", "no_side_effect_proof", "product_files_unchanged", "bridge_csp_unchanged", "external_caveat" },
            new[] { "manual QA passed", "productive runtime", "PC Commander real", "runtime ready", "release ready" });
}

internal sealed record EvidenceRedactionLeakGuard(IReadOnlyList<string> BlockedPatterns)
{
    public LeakReviewResult Review(string payload)
    {
        var matched = BlockedPatterns.FirstOrDefault(pattern => payload.Contains(pattern, StringComparison.OrdinalIgnoreCase));
        if (matched is null)
        {
            return new LeakReviewResult("clean", string.Empty, HarnessEvidenceRedactor.Redact(payload), "accepted_for_review");
        }

        return new LeakReviewResult("redacted", matched, HarnessEvidenceRedactor.Redact(payload), "blocked");
    }

    public static EvidenceRedactionLeakGuard Create() =>
        new(new[] { "api_key", "OPENAI_API_KEY", "token", "password", "Authorization:", "Bearer", "PRIVATE KEY", "AccountKey", "Cookie:", "session=", "C:\\Users\\diego", "Remove-Item", "powershell", "cmd.exe", "bash", "provider key", "browser session" });
}

internal sealed record LeakReviewResult(string RedactionStatus, string BlockedReason, string SafeSummary, string ReviewStatus);

internal sealed record OperatorCaptureChecklistPack(
    IReadOnlyList<string> BeforeStartingCapture,
    IReadOnlyList<string> AllowedCaptures,
    IReadOnlyList<string> ForbiddenCaptures,
    IReadOnlyList<string> RequiredScreenshotsLogSummaries,
    IReadOnlyList<string> RedactionChecklist,
    IReadOnlyList<string> AbortImmediatelyIf,
    IReadOnlyList<string> PassEvidencePackageRequirements,
    string FinalOperatorStatement)
{
    public static OperatorCaptureChecklistPack Create() =>
        new(
            new[] { "repo/branch/head correctos", "worktree limpio", "host/bridge/run/tab visible only if gate opens", "no product files changed", "Bridge/CSP unchanged", "dangerous commands blocked", "no provider/cloud", "no filesystem write", "no shell", "external smoke caveat documented" },
            new[] { "host visible summary", "liveness visible summary", "safe no-op log summary", "metadata fixture log summary", "dangerous blocked summary" },
            new[] { "secret values", "credentials", "raw tokens", "full sensitive logs", "shell output real", "filesystem write evidence", "provider/cloud live call evidence", "browser automation productiva", "PC Commander real execution", "release/store evidence" },
            new[] { "host visible indicator summary", "trace/evidence summary", "dangerous blocked summary" },
            new[] { "redact tokens", "redact sensitive paths", "redact credentials" },
            new[] { "product files changed", "Bridge/CSP changed", "dangerous command executed" },
            new[] { "redaction checklist completed", "no-side-effect proof included", "caveat documented" },
            "operator confirms protocol-only evidence capture; no manual QA execution");
}

internal sealed record ManualQaPreflightAbortMatrix(IReadOnlySet<string> AbortCases, string ManualQaExecution, string ManualQaTrigger)
{
    public bool ShouldAbort(string condition) => AbortCases.Contains(condition);

    public static ManualQaPreflightAbortMatrix Create() =>
        new(new HashSet<string>(StringComparer.Ordinal)
        {
            "wrong worktree",
            "wrong branch",
            "dirty worktree not explained",
            "HEAD not expected and not documented",
            "product files changed",
            "Bridge/CSP changed",
            "shell route present",
            "filesystem write route present",
            "network/provider route present",
            "browser automation productive route present",
            "credential access route present",
            "dangerous command not blocked",
            "redaction proof missing",
            "no-side-effect proof missing",
            "external caveat hidden",
            "operator cannot capture required evidence"
        }, "NO-GO", "NOT_READY_EVIDENCE_PENDING");
}

internal sealed record HumanEvidenceReviewResultContract(IReadOnlyList<string> AllowedStates, IReadOnlyList<string> BlockedClaims)
{
    public bool AllowsClaim(string claim) => !BlockedClaims.Contains(claim, StringComparer.OrdinalIgnoreCase);

    public static HumanEvidenceReviewResultContract Create() =>
        new(
            new[] { "REVIEW_NOT_STARTED", "REVIEW_BLOCKED_MISSING_EVIDENCE", "REVIEW_BLOCKED_UNSAFE_EVIDENCE", "REVIEW_BLOCKED_SCOPE_DRIFT", "REVIEW_ACCEPTED_PROTOCOL_ONLY", "REVIEW_ACCEPTED_FOR_FUTURE_MANUAL_QA_GATE", "REVIEW_REQUIRES_AUDIT" },
            new[] { "manual QA passed", "runtime ready", "PC commander ready", "release ready", "productive enabled" });
}

internal sealed record QaTriggerReEvaluationGuard(
    string ManualQaTrigger,
    string ManualQaExecution,
    string HumanEvidenceCaptureGate,
    string HarnessPrep,
    string PcCommanderReal,
    string ProductiveRuntime,
    bool FutureEligibleWithoutHumanEvidence)
{
    public static QaTriggerReEvaluationGuard Create() =>
        new("NOT_READY_EVIDENCE_PENDING", "NO-GO", "READY", "READY", "NO-GO", "NO-GO", false);
}

internal sealed record PostHarnessAuditRecommendationPack(string Recommendation, IReadOnlyList<string> BeforeJumpsTo, IReadOnlyList<string> AuditMustReview)
{
    public static PostHarnessAuditRecommendationPack Create() =>
        new(
            "PEDIR AUDITORIA CLAUDE",
            new[] { "manual QA execution", "runtime real", "PC Commander real", "host real interactive smoke", "browser/Bridge changes", "product files changes", "release/store" },
            new[] { "scope drift", "test-only vs runtime real wording", "manual QA gate", "no-side-effect proofs", "dangerous command blocking", "redaction/leak guard", "product files unchanged", "Bridge/CSP unchanged", "BrowserRuntimeSmoke caveat", "contradicciones entre artifacts", "tests superficiales/tautologicos", "riesgos de convertir harness en ejecucion" });
}

internal static class HarnessEvidenceRedactor
{
    public static string Redact(string value) =>
        value.Replace("sk_test_NODAL_OS_FAKE_KEY", "[REDACTED_API_KEY]", StringComparison.Ordinal)
            .Replace("FAKE_TOKEN_FOR_TEST_ONLY", "[REDACTED_TOKEN]", StringComparison.Ordinal)
            .Replace("FAKE_TOKEN", "[REDACTED_TOKEN]", StringComparison.Ordinal)
            .Replace("FAKE_PASSWORD_FOR_TEST_ONLY", "[REDACTED_PASSWORD]", StringComparison.Ordinal)
            .Replace("FAKE_CONNECTION_STRING", "[REDACTED_CONNECTION_STRING]", StringComparison.Ordinal)
            .Replace("FAKE_BROWSER_SESSION", "[REDACTED_COOKIE]", StringComparison.Ordinal)
            .Replace("-----BEGIN PRIVATE KEY-----", "[REDACTED_PRIVATE_KEY_BEGIN]", StringComparison.Ordinal)
            .Replace("-----END PRIVATE KEY-----", "[REDACTED_PRIVATE_KEY_END]", StringComparison.Ordinal)
            .Replace("PRIVATE KEY", "[REDACTED_PRIVATE_KEY]", StringComparison.Ordinal)
            .Replace("Authorization: Bearer", "Authorization: [REDACTED]", StringComparison.OrdinalIgnoreCase)
            .Replace("password=", "password=[REDACTED]", StringComparison.OrdinalIgnoreCase)
            .Replace("C:\\Users\\diego\\OneDrive\\secret\\file.txt", "[REDACTED_PATH]", StringComparison.OrdinalIgnoreCase)
            .Replace("C:\\Users\\diego\\secret", "[REDACTED_PATH]", StringComparison.OrdinalIgnoreCase)
            .Replace("Remove-Item", "[REDACTED_DANGEROUS_COMMAND]", StringComparison.OrdinalIgnoreCase)
            .Replace("OPENAI_API_KEY", "[REDACTED_PROVIDER_KEY]", StringComparison.OrdinalIgnoreCase);
}
