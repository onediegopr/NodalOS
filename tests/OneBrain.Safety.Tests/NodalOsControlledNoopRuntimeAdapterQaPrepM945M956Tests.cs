using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("ControlledNoopRuntimeAdapterQaPrep")]
[TestCategory("M945")]
[TestCategory("M946")]
[TestCategory("M947")]
[TestCategory("M948")]
[TestCategory("M949")]
[TestCategory("M950")]
[TestCategory("M951")]
[TestCategory("M952")]
[TestCategory("M953")]
[TestCategory("M954")]
[TestCategory("M955")]
[TestCategory("M956")]
[TestCategory("M945M956")]
public sealed class NodalOsControlledNoopRuntimeAdapterQaPrepM945M956Tests
{
    [TestMethod]
    public void controlled_noop_adapter_accepts_only_safe_noop_test_only()
    {
        var allowed = ControlledNoopRuntimeAdapter.Evaluate(ControlledAdapterCommand.SafeNoOp());
        var denied = ControlledNoopRuntimeAdapter.Evaluate(ControlledAdapterCommand.Unknown());

        Assert.AreEqual("allowed_noop", allowed.Decision);
        Assert.AreEqual("SafeNoOp", allowed.CommandKind);
        Assert.AreEqual("TestOnly", allowed.ExecutionMode);
        Assert.AreEqual("ControlledNoOp", allowed.AdapterMode);
        Assert.AreEqual("None", allowed.SideEffects);
        Assert.AreEqual("blocked", denied.Decision);
        Assert.IsFalse(allowed.ShellInvocation);
        Assert.IsFalse(allowed.FilesystemWrite);
        Assert.IsFalse(allowed.FilesystemReadReal);
        Assert.IsFalse(allowed.NetworkCall);
        Assert.IsFalse(allowed.BrowserAutomation);
        Assert.IsFalse(allowed.ProviderCall);
        Assert.IsFalse(allowed.CredentialAccess);
        Assert.IsFalse(allowed.ProcessMutation);
        Assert.IsFalse(allowed.CapabilityUnlock);
        Assert.IsFalse(allowed.ProductFilesTouched);
        Assert.IsFalse(allowed.BridgeCspTouched);
    }

    [TestMethod]
    public void runtime_adapter_descriptor_binds_trace_evidence_and_keeps_runtime_no_go()
    {
        var descriptor = RuntimeAdapterDescriptor.Create();

        Assert.AreEqual("adapter-controlled-noop-1", descriptor.AdapterId);
        Assert.AreEqual("ControlledNoOp", descriptor.AdapterMode);
        CollectionAssert.Contains(descriptor.AllowedCommandKinds.ToArray(), "SafeNoOp");
        CollectionAssert.Contains(descriptor.DeniedCommandKinds.ToArray(), "Unknown");
        CollectionAssert.Contains(descriptor.DeniedCommandKinds.ToArray(), "DangerousCommand");
        Assert.AreEqual("TestOnly", descriptor.ExecutionMode);
        Assert.AreEqual("NoSideEffects", descriptor.SideEffectPolicy);
        Assert.AreEqual("trace-adapter-1", descriptor.TraceId);
        Assert.IsFalse(descriptor.TraceRedacted.Contains("FAKE_SECRET", StringComparison.Ordinal));
        Assert.AreEqual("NO-GO", descriptor.RuntimeUnlockStatus);
        Assert.IsFalse(descriptor.ProductiveEnabled);
    }

    [TestMethod]
    public void runtime_adapter_no_side_effect_proof_reads_measured_sink_zero()
    {
        var sink = new AdapterSideEffectSink();
        var proof = RuntimeAdapterNoSideEffectProof.FromSink(sink);

        Assert.AreEqual(0, proof.SideEffectSinkInvocations);
        Assert.AreEqual(0, proof.ShellInvocations);
        Assert.AreEqual(0, proof.FilesystemWriteInvocations);
        Assert.AreEqual(0, proof.NetworkInvocations);
        Assert.AreEqual(0, proof.BrowserAutomationInvocations);
        Assert.AreEqual(0, proof.ProviderCloudInvocations);
        Assert.AreEqual(0, proof.CredentialAccessInvocations);
        Assert.AreEqual(0, proof.ProcessMutationInvocations);
        Assert.IsTrue(proof.IsClean);

        sink.RecordShellInvocation();
        var dirty = RuntimeAdapterNoSideEffectProof.FromSink(sink);
        Assert.AreEqual(1, dirty.SideEffectSinkInvocations);
        Assert.IsFalse(dirty.IsClean);
    }

    [TestMethod]
    public void local_operator_run_packet_links_all_foundation_inputs_without_real_runtime()
    {
        var packet = LocalOperatorRunPacket.Create();

        Assert.IsTrue(packet.HasHostDescriptor);
        Assert.IsTrue(packet.HasLivenessProbeStatus);
        Assert.IsTrue(packet.HasCommandChannelDescriptor);
        Assert.IsTrue(packet.HasAllowlistDecision);
        Assert.IsTrue(packet.HasSafeNoopRunnerResult);
        Assert.IsTrue(packet.HasMetadataReadRunnerResult);
        Assert.IsTrue(packet.HasControlledNoopRuntimeAdapterResult);
        Assert.IsTrue(packet.EvidenceRefs.Count > 0);
        Assert.IsTrue(packet.TraceIds.Count > 0);
        Assert.IsTrue(packet.HasNegativeGuardSummary);
        Assert.AreEqual("QA_TRIGGER_NOT_READY_FOUNDATION_ONLY", packet.QaTriggerStatus);
        Assert.IsFalse(packet.RealRuntimeExecuted);
        Assert.IsFalse(packet.ProductFilesModified);
        Assert.IsFalse(packet.BridgeCspModified);
    }

    [TestMethod]
    public void local_operator_log_contract_is_redacted_and_verifiable()
    {
        var log = LocalOperatorLogEntry.Create("FAKE_SECRET command output");

        Assert.AreEqual("run-qa-1", log.RunId);
        Assert.AreEqual("cmd-noop-1", log.CommandId);
        Assert.AreEqual("adapter-controlled-noop-1", log.AdapterId);
        Assert.AreEqual("SafeNoOp", log.CommandKind);
        Assert.AreEqual("allowed_noop", log.Decision);
        Assert.IsTrue(log.SideEffectProof.IsClean);
        Assert.IsTrue(log.EvidenceRefs.Count > 0);
        Assert.AreEqual("trace-qa-1", log.TraceId);
        Assert.AreEqual("redacted", log.RedactionStatus);
        Assert.IsTrue(log.DangerousBlockedStatus);
        Assert.IsFalse(log.Result.Contains("FAKE_SECRET", StringComparison.Ordinal));
        Assert.IsFalse(log.ContainsRawCredentials);
        Assert.IsFalse(log.ContainsSensitiveFullPath);
    }

    [TestMethod]
    public void local_operator_evidence_qa_packet_is_prepared_but_manual_qa_not_ready()
    {
        var packet = LocalOperatorEvidenceQaPacket.Create();

        Assert.AreEqual("NOT_READY_CRITERIA_DEFINED_PREPARED", packet.ManualQaTrigger);
        CollectionAssert.Contains(packet.RequiredEvidence.ToArray(), "local host visible");
        CollectionAssert.Contains(packet.FutureSafeCommands.ToArray(), "controlled no-op");
        CollectionAssert.Contains(packet.DangerousCommandsBlocked.ToArray(), "shell payload");
        CollectionAssert.Contains(packet.QaReadyCriteria.ToArray(), "QA checklist complete");
        CollectionAssert.Contains(packet.QaNoGoCriteria.ToArray(), "missing bridge connected evidence");
        Assert.IsFalse(packet.ManualQaExecuted);
    }

    [TestMethod]
    public void dangerous_command_block_evidence_blocks_required_negative_matrix()
    {
        var matrix = DangerousCommandBlockEvidence.Create();
        var cases = matrix.Rows.Select(static row => row.CaseName).ToArray();

        foreach (var row in matrix.Rows)
        {
            Assert.AreEqual("blocked", row.Decision);
            Assert.IsFalse(row.Executed);
            Assert.IsTrue(row.NoExecutionProof);
            Assert.IsTrue(row.RedactionProof);
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
    public void qa_prep_checklist_defines_criteria_but_does_not_trigger_manual_qa()
    {
        var checklist = LocalOperatorQaPrepChecklist.Create();

        CollectionAssert.Contains(checklist.Criteria.ToArray(), "local executable/host visible");
        CollectionAssert.Contains(checklist.Criteria.ToArray(), "bridge connected");
        CollectionAssert.Contains(checklist.Criteria.ToArray(), "safe no-op executable with logs");
        CollectionAssert.Contains(checklist.Criteria.ToArray(), "metadata command safe with logs");
        CollectionAssert.Contains(checklist.Criteria.ToArray(), "dangerous commands blocked");
        CollectionAssert.Contains(checklist.Criteria.ToArray(), "redaction verified");
        Assert.IsFalse(checklist.ManualQaTriggered);
        Assert.IsFalse(checklist.ProductFilesModified);
        Assert.IsFalse(checklist.BridgeCspModified);
        Assert.IsFalse(checklist.ProviderCloud);
        Assert.IsFalse(checklist.FilesystemWrite);
        Assert.IsFalse(checklist.ArbitraryShell);
    }

    [TestMethod]
    public void manual_qa_trigger_gate_remains_not_ready_without_complete_real_evidence()
    {
        var gate = ManualQaTriggerGate.EvaluateIncomplete();

        Assert.AreEqual("GO_QA_PREP_ARTIFACTS_READY", gate.PrepDecision);
        Assert.AreEqual("NO_GO_MANUAL_QA_EXECUTION", gate.ManualQaDecision);
        Assert.IsFalse(gate.HostLocalRealVisible);
        Assert.IsFalse(gate.BridgeConnectedEvidence);
        Assert.IsFalse(gate.RunVisibleEvidence);
        Assert.IsFalse(gate.TabClaimVisibleEvidence);
        Assert.IsFalse(gate.TraceEvidenceVisible);
        Assert.IsFalse(gate.SafeNoopLogs);
        Assert.IsFalse(gate.MetadataSafeLogs);
        Assert.IsTrue(gate.DangerousBlockedEvidence);
        Assert.IsFalse(gate.QaChecklistComplete);
        Assert.IsTrue(gate.RedactionProof);
        Assert.IsTrue(gate.NoSideEffectProof);
        Assert.IsTrue(gate.ExternalSmokeCaveatDocumented);
    }

    [TestMethod]
    public void operator_readiness_matrix_keeps_real_runtime_and_release_no_go()
    {
        var matrix = OperatorReadinessMatrix.Create();

        Assert.AreEqual("foundation/test-only ready", matrix.FoundationStatus);
        Assert.AreEqual("adapter no-op ready", matrix.AdapterNoopStatus);
        Assert.AreEqual("evidence packet ready", matrix.EvidencePacketStatus);
        Assert.AreEqual("QA prep ready", matrix.QaPrepStatus);
        Assert.AreEqual("manual QA not ready", matrix.ManualQaStatus);
        Assert.AreEqual("PC Commander real no-go", matrix.PcCommanderRealStatus);
        Assert.AreEqual("productive runtime no-go", matrix.ProductiveRuntimeStatus);
        Assert.AreEqual("release/store no-go", matrix.ReleaseStoreStatus);
        Assert.AreEqual(100, matrix.ControlledNoopRuntimeAdapterPercent);
        Assert.AreEqual(0, matrix.ProductiveRuntimeUnlockPercent);
        Assert.AreEqual(0, matrix.ProviderCloudPercent);
        Assert.AreEqual(0, matrix.FilesystemBrowserCapabilityUnlockPercent);
        Assert.AreEqual(0, matrix.PublicReleasePercent);
        Assert.AreEqual(95, matrix.FullSuiteConfidencePercent);
    }

    [TestMethod]
    public void next_runtime_step_recommends_visible_noop_smoke_plan_only()
    {
        var next = NextRuntimeStepRecommendation.Create();

        Assert.AreEqual("M957-M968 - Local Host Visible No-Op Smoke Plan + Manual QA Evidence Protocol", next.NextMilestone);
        Assert.IsTrue(next.PreparesVisibleNoopSmokeProtocol);
        Assert.IsFalse(next.EnablesDangerousExecution);
        Assert.IsFalse(next.EnablesShell);
        Assert.IsFalse(next.EnablesFilesystemWrite);
        Assert.IsFalse(next.EnablesBrowserAutomation);
        Assert.IsFalse(next.EnablesProviderCloud);
        Assert.IsFalse(next.TriggersManualQaNow);
    }

    [TestMethod]
    public void m956_artifacts_exist_and_preserve_boundaries()
    {
        var root = FindRepositoryRoot();
        var required = new[]
        {
            "artifacts/agent-operations/m945/controlled-noop-runtime-adapter-foundation.json",
            "artifacts/agent-operations/m946/runtime-adapter-descriptor-trace-binding.json",
            "artifacts/agent-operations/m947/runtime-adapter-no-side-effect-proof.json",
            "artifacts/agent-operations/m948/local-operator-run-packet-foundation.json",
            "artifacts/agent-operations/m949/local-operator-log-contract.json",
            "artifacts/agent-operations/m950/local-operator-evidence-qa-packet.json",
            "artifacts/agent-operations/m951/dangerous-command-block-evidence.json",
            "artifacts/agent-operations/m952/local-operator-qa-prep-checklist.json",
            "artifacts/agent-operations/m953/manual-qa-trigger-gate.json",
            "artifacts/agent-operations/m954/operator-readiness-matrix.json",
            "artifacts/agent-operations/m955/next-runtime-step-recommendation.json",
            "artifacts/agent-operations/m956/controlled-noop-runtime-adapter-local-operator-qa-prep-final-report.json",
            "artifacts/agent-operations/m945-m956/controlled-noop-runtime-adapter-local-operator-qa-prep-go-no-go.json",
            "docs/reports/m956-controlled-noop-runtime-adapter-local-operator-qa-prep.md"
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

internal sealed record ControlledAdapterCommand(string CommandKind, string ExecutionMode, string AdapterMode, string SideEffects)
{
    public static ControlledAdapterCommand SafeNoOp() => new("SafeNoOp", "TestOnly", "ControlledNoOp", "None");

    public static ControlledAdapterCommand Unknown() => new("Unknown", "Unknown", "Unknown", "Unknown");
}

internal sealed record ControlledAdapterResult(
    string Decision,
    string CommandKind,
    string ExecutionMode,
    string AdapterMode,
    string SideEffects,
    bool ShellInvocation,
    bool FilesystemWrite,
    bool FilesystemReadReal,
    bool NetworkCall,
    bool BrowserAutomation,
    bool ProviderCall,
    bool CredentialAccess,
    bool ProcessMutation,
    bool CapabilityUnlock,
    bool ProductFilesTouched,
    bool BridgeCspTouched);

internal static class ControlledNoopRuntimeAdapter
{
    public static ControlledAdapterResult Evaluate(ControlledAdapterCommand command)
    {
        var allowed = command.CommandKind == "SafeNoOp" && command.ExecutionMode == "TestOnly" && command.AdapterMode == "ControlledNoOp" && command.SideEffects == "None";
        return new ControlledAdapterResult(allowed ? "allowed_noop" : "blocked", command.CommandKind, command.ExecutionMode, command.AdapterMode, command.SideEffects, false, false, false, false, false, false, false, false, false, false, false);
    }
}

internal sealed record RuntimeAdapterDescriptor(
    string AdapterId,
    string AdapterMode,
    IReadOnlyList<string> AllowedCommandKinds,
    IReadOnlyList<string> DeniedCommandKinds,
    string ExecutionMode,
    string SideEffectPolicy,
    IReadOnlyList<string> EvidenceRefs,
    string TraceId,
    string TraceRedacted,
    string RuntimeUnlockStatus,
    bool ProductiveEnabled)
{
    public static RuntimeAdapterDescriptor Create() =>
        new("adapter-controlled-noop-1", "ControlledNoOp", new[] { "SafeNoOp" }, new[] { "Unknown", "DangerousCommand", "Shell", "FilesystemWrite" }, "TestOnly", "NoSideEffects", new[] { "evidence-adapter-1" }, "trace-adapter-1", AdapterRedactor.Redact("FAKE_SECRET trace"), "NO-GO", false);
}

internal sealed class AdapterSideEffectSink
{
    public int ShellInvocations { get; private set; }

    public int FilesystemWriteInvocations { get; private set; }

    public int NetworkInvocations { get; private set; }

    public int BrowserAutomationInvocations { get; private set; }

    public int ProviderCloudInvocations { get; private set; }

    public int CredentialAccessInvocations { get; private set; }

    public int ProcessMutationInvocations { get; private set; }

    public int TotalInvocations => ShellInvocations + FilesystemWriteInvocations + NetworkInvocations + BrowserAutomationInvocations + ProviderCloudInvocations + CredentialAccessInvocations + ProcessMutationInvocations;

    public void RecordShellInvocation() => ShellInvocations++;
}

internal sealed record RuntimeAdapterNoSideEffectProof(
    int SideEffectSinkInvocations,
    int ShellInvocations,
    int FilesystemWriteInvocations,
    int NetworkInvocations,
    int BrowserAutomationInvocations,
    int ProviderCloudInvocations,
    int CredentialAccessInvocations,
    int ProcessMutationInvocations)
{
    public bool IsClean => SideEffectSinkInvocations == 0;

    public static RuntimeAdapterNoSideEffectProof FromSink(AdapterSideEffectSink sink) =>
        new(sink.TotalInvocations, sink.ShellInvocations, sink.FilesystemWriteInvocations, sink.NetworkInvocations, sink.BrowserAutomationInvocations, sink.ProviderCloudInvocations, sink.CredentialAccessInvocations, sink.ProcessMutationInvocations);
}

internal sealed record LocalOperatorRunPacket(
    bool HasHostDescriptor,
    bool HasLivenessProbeStatus,
    bool HasCommandChannelDescriptor,
    bool HasAllowlistDecision,
    bool HasSafeNoopRunnerResult,
    bool HasMetadataReadRunnerResult,
    bool HasControlledNoopRuntimeAdapterResult,
    IReadOnlyList<string> EvidenceRefs,
    IReadOnlyList<string> TraceIds,
    bool HasNegativeGuardSummary,
    string QaTriggerStatus,
    bool RealRuntimeExecuted,
    bool ProductFilesModified,
    bool BridgeCspModified)
{
    public static LocalOperatorRunPacket Create() =>
        new(true, true, true, true, true, true, true, new[] { "evidence-run-1" }, new[] { "trace-run-1" }, true, "QA_TRIGGER_NOT_READY_FOUNDATION_ONLY", false, false, false);
}

internal sealed record LocalOperatorLogEntry(
    string Timestamp,
    string RunId,
    string CommandId,
    string AdapterId,
    string CommandKind,
    string Decision,
    string Result,
    RuntimeAdapterNoSideEffectProof SideEffectProof,
    IReadOnlyList<string> EvidenceRefs,
    string TraceId,
    string RedactionStatus,
    bool DangerousBlockedStatus,
    bool ContainsRawCredentials,
    bool ContainsSensitiveFullPath)
{
    public static LocalOperatorLogEntry Create(string result) =>
        new("logical-time-1", "run-qa-1", "cmd-noop-1", "adapter-controlled-noop-1", "SafeNoOp", "allowed_noop", AdapterRedactor.Redact(result), RuntimeAdapterNoSideEffectProof.FromSink(new AdapterSideEffectSink()), new[] { "evidence-log-1" }, "trace-qa-1", "redacted", true, false, false);
}

internal sealed record LocalOperatorEvidenceQaPacket(
    string ManualQaTrigger,
    IReadOnlyList<string> RequiredEvidence,
    IReadOnlyList<string> FutureSafeCommands,
    IReadOnlyList<string> DangerousCommandsBlocked,
    IReadOnlyList<string> QaReadyCriteria,
    IReadOnlyList<string> QaNoGoCriteria,
    bool ManualQaExecuted)
{
    public static LocalOperatorEvidenceQaPacket Create() =>
        new("NOT_READY_CRITERIA_DEFINED_PREPARED", new[] { "local host visible", "bridge connected evidence", "trace/evidence visible" }, new[] { "controlled no-op", "safe metadata read" }, new[] { "shell payload", "filesystem write", "network call" }, new[] { "QA checklist complete", "dangerous commands blocked" }, new[] { "missing bridge connected evidence", "missing no-side-effect proof" }, false);
}

internal sealed record DangerousCommandBlockEvidence(IReadOnlyList<DangerousCommandBlockRow> Rows)
{
    public static DangerousCommandBlockEvidence Create() => new(new[]
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
    }.Select(static name => new DangerousCommandBlockRow(name, "blocked", false, true, true)).ToArray());
}

internal sealed record DangerousCommandBlockRow(string CaseName, string Decision, bool Executed, bool NoExecutionProof, bool RedactionProof);

internal sealed record LocalOperatorQaPrepChecklist(
    IReadOnlyList<string> Criteria,
    bool ManualQaTriggered,
    bool ProductFilesModified,
    bool BridgeCspModified,
    bool ProviderCloud,
    bool FilesystemWrite,
    bool ArbitraryShell)
{
    public static LocalOperatorQaPrepChecklist Create() =>
        new(new[] { "local executable/host visible", "bridge connected", "run visible", "tab claim visible", "trace/evidence visible", "command channel allowlisted", "safe no-op executable with logs", "metadata command safe with logs", "dangerous commands blocked", "redaction verified", "operator report generated" }, false, false, false, false, false, false);
}

internal sealed record ManualQaTriggerGate(
    string PrepDecision,
    string ManualQaDecision,
    bool HostLocalRealVisible,
    bool BridgeConnectedEvidence,
    bool RunVisibleEvidence,
    bool TabClaimVisibleEvidence,
    bool TraceEvidenceVisible,
    bool SafeNoopLogs,
    bool MetadataSafeLogs,
    bool DangerousBlockedEvidence,
    bool QaChecklistComplete,
    bool RedactionProof,
    bool NoSideEffectProof,
    bool ExternalSmokeCaveatDocumented)
{
    public static ManualQaTriggerGate EvaluateIncomplete() =>
        new("GO_QA_PREP_ARTIFACTS_READY", "NO_GO_MANUAL_QA_EXECUTION", false, false, false, false, false, false, false, true, false, true, true, true);
}

internal sealed record OperatorReadinessMatrix(
    string FoundationStatus,
    string AdapterNoopStatus,
    string EvidencePacketStatus,
    string QaPrepStatus,
    string ManualQaStatus,
    string PcCommanderRealStatus,
    string ProductiveRuntimeStatus,
    string ReleaseStoreStatus,
    int ControlledNoopRuntimeAdapterPercent,
    int ProductiveRuntimeUnlockPercent,
    int ProviderCloudPercent,
    int FilesystemBrowserCapabilityUnlockPercent,
    int PublicReleasePercent,
    int FullSuiteConfidencePercent)
{
    public static OperatorReadinessMatrix Create() =>
        new("foundation/test-only ready", "adapter no-op ready", "evidence packet ready", "QA prep ready", "manual QA not ready", "PC Commander real no-go", "productive runtime no-go", "release/store no-go", 100, 0, 0, 0, 0, 95);
}

internal sealed record NextRuntimeStepRecommendation(
    string NextMilestone,
    bool PreparesVisibleNoopSmokeProtocol,
    bool EnablesDangerousExecution,
    bool EnablesShell,
    bool EnablesFilesystemWrite,
    bool EnablesBrowserAutomation,
    bool EnablesProviderCloud,
    bool TriggersManualQaNow)
{
    public static NextRuntimeStepRecommendation Create() =>
        new("M957-M968 - Local Host Visible No-Op Smoke Plan + Manual QA Evidence Protocol", true, false, false, false, false, false, false);
}

internal static class AdapterRedactor
{
    public static string Redact(string value) =>
        value.Replace("FAKE_SECRET", "[REDACTED]", StringComparison.Ordinal)
            .Replace("credential", "[REDACTED]", StringComparison.OrdinalIgnoreCase)
            .Replace("token", "[REDACTED]", StringComparison.OrdinalIgnoreCase);
}
