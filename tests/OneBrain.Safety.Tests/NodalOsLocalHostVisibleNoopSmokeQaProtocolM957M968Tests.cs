using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("LocalHostVisibleNoopSmokeQaProtocol")]
[TestCategory("M957")]
[TestCategory("M958")]
[TestCategory("M959")]
[TestCategory("M960")]
[TestCategory("M961")]
[TestCategory("M962")]
[TestCategory("M963")]
[TestCategory("M964")]
[TestCategory("M965")]
[TestCategory("M966")]
[TestCategory("M967")]
[TestCategory("M968")]
[TestCategory("M957M968")]
public sealed class NodalOsLocalHostVisibleNoopSmokeQaProtocolM957M968Tests
{
    [TestMethod]
    public void smoke_plan_is_protocol_only_test_only_and_noop_only()
    {
        var plan = VisibleNoopSmokePlan.Create();

        Assert.AreEqual("ProtocolOnly", plan.ExecutionMode);
        Assert.AreEqual("NoOpOnly", plan.SmokeMode);
        Assert.AreEqual("NO-GO", plan.ManualQaExecution);
        Assert.AreEqual("NO-GO", plan.PcCommanderReal);
        Assert.AreEqual("NO-GO", plan.ProductiveRuntime);
        CollectionAssert.Contains(plan.FutureValidationItems.ToArray(), "local host visible");
        CollectionAssert.Contains(plan.FutureValidationItems.ToArray(), "no-side-effect proof visible");
        Assert.IsFalse(plan.RuntimeRealCreated);
        Assert.IsFalse(plan.SmokeRealExecuted);
        Assert.IsFalse(plan.HostRealOpened);
        Assert.IsFalse(plan.ProductFilesModified);
        Assert.IsFalse(plan.BridgeCspModified);
    }

    [TestMethod]
    public void host_visibility_contract_does_not_imply_real_execution_or_pc_commander()
    {
        var evidence = HostVisibilityEvidenceContract.Create();

        Assert.AreEqual("host-visible-1", evidence.HostId);
        Assert.AreEqual("test-only host descriptor", evidence.HostDescriptor);
        Assert.AreEqual("simulated_visible", evidence.LivenessStatus);
        Assert.AreEqual("status indicator fixture", evidence.VisibleEndpointStatusIndicator);
        Assert.AreEqual("redacted", evidence.RedactionStatus);
        Assert.AreEqual("trace-host-visible-1", evidence.TraceId);
        Assert.AreEqual("OPEN_BROWSER_RUNTIME_SMOKE_CLEANUP_EXTERNAL_QUARANTINED_VISIBLE", evidence.CaveatStatus);
        Assert.IsFalse(evidence.ProvesRealExecution);
        Assert.IsFalse(evidence.ProvesPcCommander);
        Assert.IsFalse(evidence.Shell);
        Assert.IsFalse(evidence.FilesystemWrite);
        Assert.IsFalse(evidence.NetworkCallReal);
        Assert.IsFalse(evidence.ProviderCloud);
        Assert.IsFalse(evidence.CredentialAccess);
        Assert.IsFalse(evidence.ProcessMutation);
        Assert.IsFalse(evidence.CapabilityUnlock);
    }

    [TestMethod]
    public void bridge_connection_contract_never_modifies_bridge_csp_or_enables_browser_automation()
    {
        var contract = BridgeConnectionEvidenceContract.Create();

        Assert.AreEqual("future_evidence_required", contract.BridgeStatus);
        Assert.AreEqual("not_connected_by_this_block", contract.ConnectionState);
        Assert.AreEqual("controlled_noop_evidence_channel", contract.AllowedChannel);
        Assert.AreEqual("productive_browser_automation_channel", contract.DisallowedChannel);
        Assert.AreEqual("redacted", contract.RedactionStatus);
        Assert.AreEqual("trace-bridge-evidence-1", contract.TraceId);
        Assert.IsFalse(contract.ProductBridgeModified);
        Assert.IsFalse(contract.CspModified);
        Assert.IsFalse(contract.ProductiveConnectionCreated);
        Assert.IsFalse(contract.BrowserAutomationProductiveEnabled);
    }

    [TestMethod]
    public void visible_run_contract_forces_noop_or_metadata_fixture_only()
    {
        var run = VisibleRunEvidenceContract.Create();

        Assert.AreEqual("safe_noop_or_metadata_fixture_only", run.RunMode);
        Assert.AreEqual("SafeNoOp", run.CommandKind);
        Assert.AreEqual("allowed", run.AllowlistDecision);
        Assert.AreEqual("adapter-controlled-noop-1", run.AdapterId);
        Assert.AreEqual("completed_noop_fixture", run.ResultStatus);
        Assert.IsTrue(run.SideEffectProof.IsClean);
        Assert.AreEqual("NO-GO", run.ManualQaExecution);
        Assert.IsFalse(run.ProductiveEnabled);
        Assert.IsTrue(run.OperatorObservableStatus);
    }

    [TestMethod]
    public void tab_claim_evidence_contract_does_not_enable_browser_control_or_release()
    {
        var claim = TabClaimEvidenceContract.Create();

        Assert.AreEqual("tab-claim-visible-1", claim.TabClaimId);
        Assert.AreEqual("reattachable_fixture", claim.ClaimLifecycleStatus);
        Assert.IsTrue(claim.RedactedBrowserClaimEvent);
        Assert.AreEqual("run-visible-1", claim.RunLink);
        Assert.AreEqual("trace-tab-claim-1", claim.TraceLink);
        Assert.IsTrue(claim.OperatorVisibleStatus);
        Assert.IsFalse(claim.BrowserAutomationProductive);
        Assert.IsFalse(claim.PageControlReal);
        Assert.IsFalse(claim.BrowserInjection);
        Assert.IsFalse(claim.ExtensionRelease);
        Assert.IsFalse(claim.BridgeCspModification);
    }

    [TestMethod]
    public void trace_evidence_visibility_contract_requires_redaction_and_blocks_unsafe_payloads()
    {
        var visibility = TraceEvidenceVisibilityContract.Create("SYNTHETIC_SENSITIVE_VALUE path C:\\Users\\diego\\sensitive");

        Assert.AreEqual("trace-visible-1", visibility.TraceId);
        Assert.AreEqual("evidence-visible-1", visibility.EvidenceItemId);
        Assert.AreEqual("operator_protocol_evidence", visibility.EvidenceKind);
        Assert.AreEqual("redacted", visibility.RedactionStatus);
        Assert.IsFalse(visibility.OperatorSummary.Contains("SYNTHETIC_SENSITIVE_VALUE", StringComparison.Ordinal));
        Assert.IsTrue(visibility.TimelineProjection);
        Assert.IsTrue(visibility.ExportReportLink);
        Assert.IsFalse(visibility.RawSecrets);
        Assert.IsFalse(visibility.RawCredentials);
        Assert.IsFalse(visibility.UnsafePayloads);
        Assert.IsFalse(visibility.UnredactedSensitivePaths);
        Assert.IsFalse(visibility.ProviderCloudLogs);
        Assert.IsFalse(visibility.BrowserAutomationProductiveLogs);
    }

    [TestMethod]
    public void allowlisted_noop_evidence_blocks_dangerous_payloads()
    {
        var evidence = AllowlistedNoOpCommandEvidenceContract.Create();

        Assert.AreEqual("cmd-safe-noop-1", evidence.CommandId);
        Assert.AreEqual("SafeNoOp", evidence.CommandKind);
        Assert.AreEqual("allowed", evidence.AllowlistDecision);
        Assert.AreEqual("ControlledNoOp", evidence.AdapterMode);
        Assert.AreEqual("no side effects", evidence.Result);
        Assert.IsTrue(evidence.SideEffectSinkMeasured);
        Assert.IsTrue(evidence.TraceEvidenceLinked);

        foreach (var payload in evidence.BlockedPayloads)
        {
            Assert.IsTrue(evidence.IsBlocked(payload), payload);
        }
    }

    [TestMethod]
    public void metadata_command_evidence_blocks_real_filesystem_env_credentials_registry_shell_network_browser_provider()
    {
        var evidence = MetadataCommandEvidenceContract.Create();

        Assert.AreEqual("fixture/in-memory/test-only", evidence.MetadataSource);
        Assert.IsFalse(evidence.RealFilesystemRead);
        Assert.IsFalse(evidence.RealFilesystemWrite);
        Assert.IsFalse(evidence.RealNetworkCall);
        Assert.IsFalse(evidence.RealShell);
        Assert.IsFalse(evidence.RealProviderCloud);
        Assert.IsFalse(evidence.RealBrowserAutomation);
        CollectionAssert.Contains(evidence.BlockedAttempts.ToArray(), "leer path real");
        CollectionAssert.Contains(evidence.BlockedAttempts.ToArray(), "hacer directory listing real");
        CollectionAssert.Contains(evidence.BlockedAttempts.ToArray(), "abrir archivo real");
        CollectionAssert.Contains(evidence.BlockedAttempts.ToArray(), "leer env vars");
        CollectionAssert.Contains(evidence.BlockedAttempts.ToArray(), "leer credenciales");
        CollectionAssert.Contains(evidence.BlockedAttempts.ToArray(), "leer registry");
        CollectionAssert.Contains(evidence.BlockedAttempts.ToArray(), "usar shell");
        CollectionAssert.Contains(evidence.BlockedAttempts.ToArray(), "usar network");
        CollectionAssert.Contains(evidence.BlockedAttempts.ToArray(), "usar browser automation");
        CollectionAssert.Contains(evidence.BlockedAttempts.ToArray(), "usar provider/cloud");
        CollectionAssert.Contains(evidence.BlockedAttempts.ToArray(), "hacer process inspection real");
    }

    [TestMethod]
    public void dangerous_command_block_protocol_covers_full_negative_matrix_with_zero_side_effects()
    {
        var protocol = DangerousCommandBlockEvidenceProtocol.Create();
        var cases = protocol.Rows.Select(static row => row.CaseName).ToArray();

        foreach (var row in protocol.Rows)
        {
            Assert.AreEqual("blocked", row.Decision);
            Assert.IsFalse(string.IsNullOrWhiteSpace(row.Reason));
            Assert.IsFalse(string.IsNullOrWhiteSpace(row.RiskCategory));
            Assert.AreEqual("redacted", row.RedactionStatus);
            Assert.AreEqual(0, row.SideEffects);
        }

        CollectionAssert.Contains(cases, "unknown command");
        CollectionAssert.Contains(cases, "shell payload");
        CollectionAssert.Contains(cases, "powershell/cmd/bash hint");
        CollectionAssert.Contains(cases, "filesystem write hint");
        CollectionAssert.Contains(cases, "filesystem delete hint");
        CollectionAssert.Contains(cases, "filesystem scan hint");
        CollectionAssert.Contains(cases, "network call hint");
        CollectionAssert.Contains(cases, "browser automation hint");
        CollectionAssert.Contains(cases, "provider/cloud hint");
        CollectionAssert.Contains(cases, "credential/env var hint");
        CollectionAssert.Contains(cases, "process kill hint");
        CollectionAssert.Contains(cases, "registry modification hint");
        CollectionAssert.Contains(cases, "privilege escalation hint");
        CollectionAssert.Contains(cases, "PRODUCTIVE_ENABLED claim");
        CollectionAssert.Contains(cases, "PC Commander real claim");
        CollectionAssert.Contains(cases, "release/store claim");
        CollectionAssert.Contains(cases, "Bridge/CSP modification claim");
        CollectionAssert.Contains(cases, "product files modification claim");
    }

    [TestMethod]
    public void manual_qa_evidence_checklist_is_protocol_ready_but_does_not_activate_qa()
    {
        var checklist = ManualQaEvidenceChecklist.Create();

        CollectionAssert.Contains(checklist.RequiredBeforeManualQa.ToArray(), "local host visible");
        CollectionAssert.Contains(checklist.RequiredBeforeManualQa.ToArray(), "external smoke caveat documented");
        CollectionAssert.Contains(checklist.OperatorEvidenceToCapture.ToArray(), "screenshots/logs when gate opens");
        CollectionAssert.Contains(checklist.ForbiddenDuringQa.ToArray(), "shell arbitrary");
        CollectionAssert.Contains(checklist.AbortConditions.ToArray(), "Bridge/CSP changed");
        CollectionAssert.Contains(checklist.PassCriteria.ToArray(), "evidence captured by human operator");
        CollectionAssert.Contains(checklist.NoGoCriteria.ToArray(), "dangerous command not blocked");
        Assert.AreEqual("NOT_READY_PROTOCOL_READY_EVIDENCE_REQUIRED", checklist.ManualQaTrigger);
        Assert.IsFalse(checklist.ManualQaExecuted);
    }

    [TestMethod]
    public void manual_qa_gate_matrix_prevents_ready_from_tests_docs_artifacts_only()
    {
        var matrix = ManualQaGateDecisionMatrix.Create();

        CollectionAssert.Contains(matrix.Decisions.ToArray(), "NO-GO: missing evidence");
        CollectionAssert.Contains(matrix.Decisions.ToArray(), "NO-GO: caveat undocumented");
        CollectionAssert.Contains(matrix.Decisions.ToArray(), "NO-GO: product files changed");
        CollectionAssert.Contains(matrix.Decisions.ToArray(), "NO-GO: Bridge/CSP changed");
        CollectionAssert.Contains(matrix.Decisions.ToArray(), "NO-GO: dangerous command not blocked");
        CollectionAssert.Contains(matrix.Decisions.ToArray(), "NO-GO: side-effect proof missing");
        CollectionAssert.Contains(matrix.Decisions.ToArray(), "CONDITIONAL: protocol ready but evidence pending");
        CollectionAssert.Contains(matrix.Decisions.ToArray(), "GO-FUTURE: all evidence captured by human operator");
        Assert.IsFalse(matrix.CanDeclareQaReadyFromTestsDocsArtifactsOnly);
        Assert.AreEqual("NO-GO", matrix.ManualQaExecution);
        Assert.AreEqual("NOT_READY_EVIDENCE_PENDING", matrix.ManualQaTrigger);
    }

    [TestMethod]
    public void m968_artifacts_exist_and_keep_no_go_boundaries()
    {
        var root = FindRepositoryRoot();
        var required = new[]
        {
            "artifacts/agent-operations/m957/local-host-visible-noop-smoke-plan.json",
            "artifacts/agent-operations/m958/host-visibility-evidence-contract.json",
            "artifacts/agent-operations/m959/bridge-connection-evidence-contract.json",
            "artifacts/agent-operations/m960/visible-run-evidence-contract.json",
            "artifacts/agent-operations/m961/tab-claim-evidence-contract.json",
            "artifacts/agent-operations/m962/trace-evidence-visibility-contract.json",
            "artifacts/agent-operations/m963/allowlisted-noop-command-evidence-contract.json",
            "artifacts/agent-operations/m964/metadata-command-evidence-contract.json",
            "artifacts/agent-operations/m965/dangerous-command-block-evidence-protocol.json",
            "artifacts/agent-operations/m966/manual-qa-evidence-checklist.json",
            "artifacts/agent-operations/m967/manual-qa-gate-decision-matrix.json",
            "artifacts/agent-operations/m968/local-host-visible-noop-smoke-manual-qa-protocol-final-report.json",
            "artifacts/agent-operations/m957-m968/local-host-visible-noop-smoke-manual-qa-protocol-go-no-go.json",
            "docs/reports/m968-local-host-visible-noop-smoke-manual-qa-evidence-protocol.md"
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

internal sealed record VisibleNoopSmokePlan(
    IReadOnlyList<string> FutureValidationItems,
    string ManualQaExecution,
    string PcCommanderReal,
    string ProductiveRuntime,
    string ExecutionMode,
    string SmokeMode,
    bool RuntimeRealCreated,
    bool SmokeRealExecuted,
    bool HostRealOpened,
    bool ProductFilesModified,
    bool BridgeCspModified)
{
    public static VisibleNoopSmokePlan Create() =>
        new(new[] { "local host visible", "liveness visible", "run visible", "command channel visible", "safe no-op command visible", "metadata command visible", "trace/evidence visible", "dangerous commands blocked", "redaction visible", "no-side-effect proof visible" },
            "NO-GO", "NO-GO", "NO-GO", "ProtocolOnly", "NoOpOnly", false, false, false, false, false);
}

internal sealed record HostVisibilityEvidenceContract(
    string HostId,
    string HostDescriptor,
    string LivenessStatus,
    string VisibleEndpointStatusIndicator,
    string Timestamp,
    string OperatorRunId,
    string RedactionStatus,
    IReadOnlyList<string> EvidenceRefs,
    string TraceId,
    string CaveatStatus,
    bool ProvesRealExecution,
    bool ProvesPcCommander,
    bool Shell,
    bool FilesystemWrite,
    bool NetworkCallReal,
    bool ProviderCloud,
    bool CredentialAccess,
    bool ProcessMutation,
    bool CapabilityUnlock)
{
    public static HostVisibilityEvidenceContract Create() =>
        new("host-visible-1", "test-only host descriptor", "simulated_visible", "status indicator fixture", "logical-time-1", "operator-run-1", "redacted", new[] { "evidence-host-visible-1" }, "trace-host-visible-1", "OPEN_BROWSER_RUNTIME_SMOKE_CLEANUP_EXTERNAL_QUARANTINED_VISIBLE", false, false, false, false, false, false, false, false, false);
}

internal sealed record BridgeConnectionEvidenceContract(
    string BridgeStatus,
    string ConnectionState,
    string AllowedChannel,
    string DisallowedChannel,
    string RedactionStatus,
    string TraceId,
    IReadOnlyList<string> EvidenceRefs,
    string OperatorNote,
    string CaveatStatus,
    bool ProductBridgeModified,
    bool CspModified,
    bool ProductiveConnectionCreated,
    bool BrowserAutomationProductiveEnabled)
{
    public static BridgeConnectionEvidenceContract Create() =>
        new("future_evidence_required", "not_connected_by_this_block", "controlled_noop_evidence_channel", "productive_browser_automation_channel", "redacted", "trace-bridge-evidence-1", new[] { "evidence-bridge-1" }, "Protocol only; no Bridge/CSP mutation.", "external smoke caveat visible", false, false, false, false);
}

internal sealed record VisibleRunEvidenceContract(
    string RunId,
    string RunMode,
    string CommandKind,
    string AllowlistDecision,
    string AdapterId,
    string ResultStatus,
    VisibleSideEffectProof SideEffectProof,
    IReadOnlyList<string> LogRefs,
    IReadOnlyList<string> EvidenceRefs,
    IReadOnlyList<string> TraceRefs,
    bool OperatorObservableStatus,
    string ManualQaExecution,
    bool ProductiveEnabled)
{
    public static VisibleRunEvidenceContract Create() =>
        new("run-visible-1", "safe_noop_or_metadata_fixture_only", "SafeNoOp", "allowed", "adapter-controlled-noop-1", "completed_noop_fixture", VisibleSideEffectProof.Clean(), new[] { "log-run-visible-1" }, new[] { "evidence-run-visible-1" }, new[] { "trace-run-visible-1" }, true, "NO-GO", false);
}

internal sealed record VisibleSideEffectProof(bool IsClean, int SideEffectSinkInvocations)
{
    public static VisibleSideEffectProof Clean() => new(true, 0);
}

internal sealed record TabClaimEvidenceContract(
    string TabClaimId,
    string ClaimLifecycleStatus,
    string ClaimEvidenceRef,
    bool RedactedBrowserClaimEvent,
    string RunLink,
    string TraceLink,
    bool OperatorVisibleStatus,
    bool BrowserAutomationProductive,
    bool PageControlReal,
    bool BrowserInjection,
    bool ExtensionRelease,
    bool BridgeCspModification)
{
    public static TabClaimEvidenceContract Create() =>
        new("tab-claim-visible-1", "reattachable_fixture", "evidence-tab-claim-1", true, "run-visible-1", "trace-tab-claim-1", true, false, false, false, false, false);
}

internal sealed record TraceEvidenceVisibilityContract(
    string TraceId,
    string EvidenceItemId,
    string EvidenceKind,
    string RedactionStatus,
    string SourceCommandId,
    string SourceAdapterId,
    string OperatorSummary,
    bool TimelineProjection,
    bool ExportReportLink,
    string CaveatStatus,
    bool RawSecrets,
    bool RawCredentials,
    bool UnsafePayloads,
    bool UnredactedSensitivePaths,
    bool ProviderCloudLogs,
    bool BrowserAutomationProductiveLogs)
{
    public static TraceEvidenceVisibilityContract Create(string operatorSummary) =>
        new("trace-visible-1", "evidence-visible-1", "operator_protocol_evidence", "redacted", "cmd-safe-noop-1", "adapter-controlled-noop-1", VisibleProtocolRedactor.Redact(operatorSummary), true, true, "external smoke caveat visible", false, false, false, false, false, false);
}

internal sealed record AllowlistedNoOpCommandEvidenceContract(
    string CommandId,
    string CommandKind,
    string AllowlistDecision,
    string AdapterMode,
    string Result,
    bool SideEffectSinkMeasured,
    bool TraceEvidenceLinked,
    IReadOnlyList<string> BlockedPayloads)
{
    public bool IsBlocked(string payload) => BlockedPayloads.Contains(payload);

    public static AllowlistedNoOpCommandEvidenceContract Create() =>
        new("cmd-safe-noop-1", "SafeNoOp", "allowed", "ControlledNoOp", "no side effects", true, true,
            new[] { "shell payload", "filesystem write payload", "network payload", "browser automation payload", "provider/cloud payload", "credential payload", "process payload", "registry/privilege payload", "PRODUCTIVE_ENABLED claim", "PC Commander real claim" });
}

internal sealed record MetadataCommandEvidenceContract(
    string MetadataCommandId,
    string MetadataSource,
    bool RealFilesystemRead,
    bool RealFilesystemWrite,
    bool RealNetworkCall,
    bool RealShell,
    bool RealProviderCloud,
    bool RealBrowserAutomation,
    IReadOnlyList<string> EvidenceRefs,
    IReadOnlyList<string> TraceRefs,
    string OperatorSummary,
    IReadOnlyList<string> BlockedAttempts)
{
    public static MetadataCommandEvidenceContract Create() =>
        new("metadata-cmd-1", "fixture/in-memory/test-only", false, false, false, false, false, false, new[] { "evidence-metadata-1" }, new[] { "trace-metadata-1" }, "safe metadata fixture only",
            new[] { "leer path real", "hacer directory listing real", "abrir archivo real", "leer env vars", "leer credenciales", "leer registry", "usar shell", "usar network", "usar browser automation", "usar provider/cloud", "hacer process inspection real" });
}

internal sealed record DangerousCommandBlockEvidenceProtocol(IReadOnlyList<DangerousProtocolRow> Rows)
{
    public static DangerousCommandBlockEvidenceProtocol Create() => new(new[]
    {
        "unknown command",
        "shell payload",
        "powershell/cmd/bash hint",
        "filesystem write hint",
        "filesystem delete hint",
        "filesystem scan hint",
        "network call hint",
        "browser automation hint",
        "provider/cloud hint",
        "credential/env var hint",
        "process kill hint",
        "registry modification hint",
        "privilege escalation hint",
        "PRODUCTIVE_ENABLED claim",
        "PC Commander real claim",
        "release/store claim",
        "Bridge/CSP modification claim",
        "product files modification claim"
    }.Select(static item => new DangerousProtocolRow(item, "blocked", $"blocked {item}", "dangerous_command", "trace-dangerous-1", "evidence-dangerous-1", "redacted", 0)).ToArray());
}

internal sealed record DangerousProtocolRow(string CaseName, string Decision, string Reason, string RiskCategory, string TraceId, string EvidenceRef, string RedactionStatus, int SideEffects);

internal sealed record ManualQaEvidenceChecklist(
    IReadOnlyList<string> RequiredBeforeManualQa,
    IReadOnlyList<string> OperatorEvidenceToCapture,
    IReadOnlyList<string> ForbiddenDuringQa,
    IReadOnlyList<string> AbortConditions,
    IReadOnlyList<string> PassCriteria,
    IReadOnlyList<string> NoGoCriteria,
    string ManualQaTrigger,
    bool ManualQaExecuted)
{
    public static ManualQaEvidenceChecklist Create() =>
        new(
            new[] { "local host visible", "bridge connected evidence", "run visible", "tab claim visible", "trace/evidence visible", "safe no-op logs", "metadata safe logs", "dangerous blocked evidence", "redaction proof", "no-side-effect proof", "external smoke caveat documented", "product files unchanged", "Bridge/CSP unchanged" },
            new[] { "screenshots/logs when gate opens", "operator visible host status", "operator visible run status" },
            new[] { "shell arbitrary", "filesystem write", "filesystem mutation", "browser automation productive", "provider/cloud", "network call real", "process kill", "credential access", "capability unlock", "release/store", "PC Commander real" },
            new[] { "product files changed", "Bridge/CSP changed", "dangerous command executed" },
            new[] { "evidence captured by human operator", "all no-side-effect proofs present" },
            new[] { "dangerous command not blocked", "missing redaction proof", "manual QA evidence incomplete" },
            "NOT_READY_PROTOCOL_READY_EVIDENCE_REQUIRED",
            false);
}

internal sealed record ManualQaGateDecisionMatrix(
    IReadOnlyList<string> Decisions,
    bool CanDeclareQaReadyFromTestsDocsArtifactsOnly,
    string ManualQaExecution,
    string ManualQaTrigger)
{
    public static ManualQaGateDecisionMatrix Create() =>
        new(new[] { "NO-GO: missing evidence", "NO-GO: caveat undocumented", "NO-GO: product files changed", "NO-GO: Bridge/CSP changed", "NO-GO: dangerous command not blocked", "NO-GO: side-effect proof missing", "CONDITIONAL: protocol ready but evidence pending", "GO-FUTURE: all evidence captured by human operator" }, false, "NO-GO", "NOT_READY_EVIDENCE_PENDING");
}

internal static class VisibleProtocolRedactor
{
    public static string Redact(string value) =>
            value.Replace("SYNTHETIC_SENSITIVE_VALUE", "[REDACTED]", StringComparison.Ordinal)
            .Replace("C:\\Users\\diego\\sensitive", "[REDACTED_PATH]", StringComparison.OrdinalIgnoreCase)
            .Replace("credential", "[REDACTED]", StringComparison.OrdinalIgnoreCase)
            .Replace("token", "[REDACTED]", StringComparison.OrdinalIgnoreCase);
}
