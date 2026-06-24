using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("SafeNoopMetadataOperatorBridge")]
[TestCategory("M933")]
[TestCategory("M934")]
[TestCategory("M935")]
[TestCategory("M936")]
[TestCategory("M937")]
[TestCategory("M938")]
[TestCategory("M939")]
[TestCategory("M940")]
[TestCategory("M941")]
[TestCategory("M942")]
[TestCategory("M943")]
[TestCategory("M944")]
[TestCategory("M933M944")]
public sealed class NodalOsSafeNoopMetadataOperatorBridgeM933M944Tests
{
    [TestMethod]
    public void safe_noop_runner_is_test_only_observable_and_has_no_side_effects()
    {
        var runner = SafeNoOpCommandRunner.Create("runner-1", "host-1", "run-1", "cmd-1", "pack-1", "trace-1");

        Assert.AreEqual("test_only", runner.RunnerMode);
        Assert.AreEqual("noop", runner.CommandCategory);
        Assert.AreEqual("allowed", runner.AllowlistStatus);
        Assert.IsTrue(runner.ObservableResultProduced);
        Assert.IsFalse(runner.ExecutedRealAction);
        Assert.IsFalse(runner.SideEffects);
        Assert.IsFalse(runner.FilesystemWrite);
        Assert.IsFalse(runner.ShellInvoked);
        Assert.IsFalse(runner.BrowserAutomation);
        Assert.IsFalse(runner.ProviderCloud);
        Assert.IsFalse(runner.NetworkCall);
        Assert.IsFalse(runner.ProcessKill);
        Assert.IsFalse(runner.CredentialAccess);
        Assert.IsFalse(runner.CapabilityUnlock);
        Assert.IsFalse(runner.ReleaseStore);
        Assert.IsFalse(runner.ProductFilesModified);
        Assert.IsFalse(runner.BridgeCspModified);
        Assert.AreEqual("pack-1", runner.EvidencePackId);
        Assert.AreEqual("trace-1", runner.TraceId);
    }

    [TestMethod]
    public void noop_command_result_links_trace_events_and_never_marks_real_execution()
    {
        var result = NoOpCommandResult.Completed("runner-1", "cmd-1", "run-1", "host-1", "pack-1", "trace-1", "FAKE_SECRET output");

        Assert.AreEqual("completed_noop", result.Status);
        Assert.AreEqual("allowed_noop", result.CommandDecision);
        Assert.IsFalse(result.OutputRedacted.Contains("FAKE_SECRET", StringComparison.Ordinal));
        Assert.AreEqual("pack-1", result.EvidencePackId);
        Assert.AreEqual("trace-1", result.TraceId);
        CollectionAssert.Contains(result.TraceEvents.Select(static evt => evt.EventType).ToArray(), "command_noop_started");
        CollectionAssert.Contains(result.TraceEvents.Select(static evt => evt.EventType).ToArray(), "command_noop_completed");
        Assert.IsTrue(result.NoExecutionProof.Present);
        Assert.IsTrue(result.RedactionProof.Present);
        Assert.IsTrue(result.RedactionProof.Redacted);
        Assert.IsFalse(result.NoExecutionProof.ExecutedRealAction);
    }

    [TestMethod]
    public void noop_guard_rejects_false_runtime_and_pc_commander_claims()
    {
        var guard = NoOpClaimsGuard.Create();

        Assert.IsFalse(guard.PcCommanderReadyClaimAllowed);
        Assert.IsFalse(guard.RealCommandExecutionClaimAllowed);
        Assert.IsFalse(guard.ShellEnabledClaimAllowed);
        Assert.IsFalse(guard.FilesystemWriteClaimAllowed);
        Assert.IsFalse(guard.BrowserAutomationClaimAllowed);
        Assert.IsFalse(guard.ProviderCloudClaimAllowed);
        Assert.IsFalse(guard.ProcessKillClaimAllowed);
        Assert.IsFalse(guard.NetworkCallClaimAllowed);
        Assert.IsFalse(guard.CredentialAccessClaimAllowed);
        Assert.IsFalse(guard.ReleaseStoreClaimAllowed);
    }

    [TestMethod]
    public void metadata_read_runner_allows_safe_metadata_and_blocks_sensitive_metadata()
    {
        var allowed = MetadataReadRunner.ForKind("host_liveness_status");
        var blocked = MetadataReadRunner.ForKind("browser_session_data");

        Assert.AreEqual("safe_metadata_only", allowed.ReadScope);
        Assert.AreEqual("read_only_if_currently_allowed", allowed.RunnerMode);
        CollectionAssert.Contains(MetadataReadRunner.AllowedMetadataKinds.ToArray(), "host_liveness_status");
        CollectionAssert.Contains(MetadataReadRunner.AllowedMetadataKinds.ToArray(), "command_channel_status");
        CollectionAssert.Contains(MetadataReadRunner.AllowedMetadataKinds.ToArray(), "allowlist_summary");
        CollectionAssert.Contains(MetadataReadRunner.AllowedMetadataKinds.ToArray(), "run_trace_summary");
        CollectionAssert.Contains(MetadataReadRunner.AllowedMetadataKinds.ToArray(), "evidence_pack_summary");
        CollectionAssert.Contains(MetadataReadRunner.AllowedMetadataKinds.ToArray(), "browser_claim_summary");
        CollectionAssert.Contains(MetadataReadRunner.AllowedMetadataKinds.ToArray(), "freeze_baseline_status");
        CollectionAssert.Contains(MetadataReadRunner.AllowedMetadataKinds.ToArray(), "caveat_status");
        Assert.IsTrue(allowed.Allowed);
        Assert.IsFalse(allowed.ReadsFilesystemContent);
        Assert.IsFalse(allowed.WritesFilesystem);
        Assert.IsFalse(allowed.ShellInvoked);
        Assert.IsFalse(allowed.BrowserAutomation);
        Assert.IsFalse(allowed.ProviderCloud);
        Assert.IsFalse(allowed.NetworkCall);
        Assert.IsFalse(allowed.ProcessKill);
        Assert.IsFalse(allowed.CredentialAccess);
        Assert.IsFalse(allowed.CapabilityUnlock);
        Assert.IsFalse(allowed.ReleaseStore);
        Assert.IsFalse(allowed.ProductFilesModified);
        Assert.IsFalse(allowed.BridgeCspModified);
        Assert.IsFalse(blocked.Allowed);
    }

    [TestMethod]
    public void metadata_read_result_completes_allowed_metadata_and_blocks_forbidden_metadata()
    {
        var allowed = MetadataReadResult.FromRunner(MetadataReadRunner.ForKind("allowlist_summary"), "FAKE_SECRET allowlist ok");
        var blocked = MetadataReadResult.FromRunner(MetadataReadRunner.ForKind("file_contents"), "raw file contents");

        Assert.AreEqual("completed_metadata_read", allowed.Status);
        Assert.AreEqual("allowed_metadata", allowed.CommandDecision);
        Assert.IsFalse(allowed.OutputRedacted.Contains("FAKE_SECRET", StringComparison.Ordinal));
        Assert.AreEqual("pack-metadata", allowed.EvidencePackId);
        Assert.AreEqual("trace-metadata", allowed.TraceId);
        Assert.AreEqual("metadata_read_completed", allowed.TraceEvent.EventType);
        Assert.IsTrue(allowed.NoExecutionProof.Present);
        Assert.IsTrue(allowed.RedactionProof.Present);
        Assert.IsFalse(allowed.ReadFileContents);
        Assert.IsFalse(allowed.StoredSecrets);
        Assert.AreEqual("blocked", blocked.Status);
        Assert.AreEqual("blocked", blocked.CommandDecision);
    }

    [TestMethod]
    public void metadata_negative_matrix_blocks_sensitive_rows_with_evidence_trace()
    {
        var matrix = MetadataNegativeMatrix.Create();
        var categories = matrix.Rows.Select(static row => row.MetadataKind).ToArray();

        foreach (var row in matrix.Rows)
        {
            if (row.ExpectedDecision != "blocked")
            {
                Assert.Fail($"Unexpected metadata decision for {row.MetadataKind}: {row.ExpectedDecision}");
            }

            Assert.IsFalse(row.Executed);
            Assert.IsFalse(row.FilesystemWrite);
            Assert.IsFalse(row.ShellInvoked);
            Assert.IsFalse(row.BrowserAutomation);
            Assert.IsFalse(row.ProviderCloud);
            Assert.IsFalse(row.CredentialAccess);
            Assert.IsTrue(row.NoExecutionProof);
            Assert.IsTrue(row.RedactionProof);
            Assert.IsTrue(row.EvidenceTraceLinked);
        }

        CollectionAssert.Contains(categories, "file contents");
        CollectionAssert.Contains(categories, "secrets");
        CollectionAssert.Contains(categories, "credentials");
        CollectionAssert.Contains(categories, "tokens");
        CollectionAssert.Contains(categories, "cookies");
        CollectionAssert.Contains(categories, "browser session data");
        CollectionAssert.Contains(categories, "full env vars");
        CollectionAssert.Contains(categories, "process memory");
        CollectionAssert.Contains(categories, "registry secrets");
        CollectionAssert.Contains(categories, "network credentials");
        CollectionAssert.Contains(categories, "private keys");
        CollectionAssert.Contains(categories, "provider keys");
        CollectionAssert.Contains(categories, "raw logs");
    }

    [TestMethod]
    public void local_operator_evidence_bridge_links_command_result_without_touching_real_bridge_or_csp()
    {
        var commandResult = NoOpCommandResult.Completed("runner-1", "cmd-1", "run-1", "host-1", "pack-1", "trace-1", "ok");
        var bridge = LocalOperatorEvidenceBridge.FromCommandResult(commandResult);

        Assert.AreEqual(commandResult.ResultId, bridge.CommandResultId);
        Assert.AreEqual("pack-1", bridge.EvidencePackId);
        Assert.AreEqual("trace-1", bridge.TraceId);
        Assert.AreEqual("test_only", bridge.BridgeMode);
        Assert.IsFalse(bridge.RealRuntimeBridge);
        Assert.IsFalse(bridge.ProductBridgeModified);
        Assert.IsFalse(bridge.CspModified);
        Assert.IsFalse(bridge.OutputRedacted.Contains("FAKE_SECRET", StringComparison.Ordinal));
        Assert.IsTrue(bridge.NoExecutionProof.Present);
        Assert.IsTrue(bridge.RedactionProof.Present);
    }

    [TestMethod]
    public void operator_evidence_timeline_summary_reconstructs_without_unlocking_runtime()
    {
        var summary = OperatorEvidenceTimelineSummary.Create();

        Assert.IsTrue(summary.IncludesHostDescriptor);
        Assert.IsTrue(summary.IncludesLivenessProbe);
        Assert.IsTrue(summary.IncludesCommandDescriptor);
        Assert.IsTrue(summary.IncludesCommandResult);
        Assert.IsTrue(summary.IncludesEvidencePack);
        Assert.IsTrue(summary.IncludesTraceEvents);
        Assert.IsTrue(summary.IncludesMetadataResult);
        Assert.IsTrue(summary.IncludesCaveatStatus);
        Assert.IsTrue(summary.MarksNoExecution);
        Assert.IsFalse(summary.Executes);
        Assert.IsFalse(summary.ShellInvoked);
        Assert.IsFalse(summary.FilesystemWrite);
        Assert.IsFalse(summary.BrowserAutomation);
        Assert.IsFalse(summary.ProviderCloud);
        Assert.IsFalse(summary.ReleaseStore);
        Assert.IsFalse(summary.RuntimeUnlocked);
    }

    [TestMethod]
    public void evidence_bridge_guard_rejects_false_claims()
    {
        var guard = EvidenceBridgeClaimsGuard.Create();

        Assert.IsFalse(guard.BrowserBridgeModifiedClaimAllowed);
        Assert.IsFalse(guard.CspModifiedClaimAllowed);
        Assert.IsFalse(guard.RealCommandExecutionClaimAllowed);
        Assert.IsFalse(guard.PcCommanderQaReadyClaimAllowed);
        Assert.IsFalse(guard.FilesystemWriteClaimAllowed);
        Assert.IsFalse(guard.ShellClaimAllowed);
        Assert.IsFalse(guard.ProviderCloudClaimAllowed);
        Assert.IsFalse(guard.BrowserAutomationClaimAllowed);
    }

    [TestMethod]
    public void qa_trigger_recheck_remains_not_ready_for_foundation_only()
    {
        var recheck = QaTriggerRecheck.FoundationOnly();

        Assert.AreEqual("QA_TRIGGER_NOT_READY_FOUNDATION_ONLY", recheck.Status);
        Assert.IsFalse(recheck.RealHostVisible);
        Assert.IsFalse(recheck.RealBridgeConnected);
        Assert.IsFalse(recheck.RealSafeCommandExecution);
        Assert.IsTrue(recheck.DangerousCommandsBlocked);
        Assert.IsFalse(recheck.QaChecklistReady);
        Assert.IsTrue(recheck.FoundationOnlyStatusRecorded);
    }

    [TestMethod]
    public void next_operator_step_recommends_controlled_noop_adapter_without_manual_qa()
    {
        var recommendation = NextOperatorStepRecommendation.Create();

        Assert.AreEqual("M945-M956 - Controlled No-Op Runtime Adapter + Local Operator QA Prep", recommendation.NextMilestone);
        Assert.IsFalse(recommendation.TriggersManualQaYet);
        Assert.IsFalse(recommendation.ShellEnabled);
        Assert.IsFalse(recommendation.FilesystemWriteEnabled);
        Assert.IsFalse(recommendation.BrowserAutomationEnabled);
        Assert.IsFalse(recommendation.ProviderCloudEnabled);
        Assert.IsFalse(recommendation.ReleaseStoreEnabled);
        Assert.IsFalse(recommendation.ProductBridgeCspModified);
    }

    [TestMethod]
    public void m944_artifact_contracts_exist_and_preserve_no_go_boundaries()
    {
        var required = new[]
        {
            "artifacts/agent-operations/m933/safe-noop-command-runner-contract.json",
            "artifacts/agent-operations/m934/noop-command-result-trace-event.json",
            "artifacts/agent-operations/m935/noop-negative-guard.json",
            "artifacts/agent-operations/m936/metadata-read-runner-contract.json",
            "artifacts/agent-operations/m937/metadata-read-result-evidence-binding.json",
            "artifacts/agent-operations/m938/metadata-read-negative-matrix.json",
            "artifacts/agent-operations/m939/local-operator-evidence-bridge-contract.json",
            "artifacts/agent-operations/m940/operator-evidence-timeline-summary.json",
            "artifacts/agent-operations/m941/operator-evidence-bridge-go-no-go-guard.json",
            "artifacts/agent-operations/m942/qa-trigger-recheck.json",
            "artifacts/agent-operations/m943/next-operator-step-recommendation.json",
            "artifacts/agent-operations/m944/safe-noop-metadata-runner-local-operator-evidence-bridge-final-report.json",
            "artifacts/agent-operations/m933-m944/safe-noop-metadata-operator-evidence-bridge-go-no-go.json",
            "docs/reports/m944-safe-noop-metadata-runner-local-operator-evidence-bridge.md"
        };
        var root = FindRepositoryRoot();

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

internal sealed record SafeNoOpCommandRunner(
    string RunnerId,
    string HostId,
    string RunId,
    string CommandId,
    string RunnerMode,
    string CommandCategory,
    string AllowlistStatus,
    bool ObservableResultProduced,
    bool ExecutedRealAction,
    bool SideEffects,
    bool FilesystemWrite,
    bool ShellInvoked,
    bool BrowserAutomation,
    bool ProviderCloud,
    bool NetworkCall,
    bool ProcessKill,
    bool CredentialAccess,
    bool CapabilityUnlock,
    bool ReleaseStore,
    bool ProductFilesModified,
    bool BridgeCspModified,
    string EvidencePackId,
    string TraceId)
{
    public static SafeNoOpCommandRunner Create(string runnerId, string hostId, string runId, string commandId, string evidencePackId, string traceId) =>
        new(runnerId, hostId, runId, commandId, "test_only", "noop", "allowed", true, false, false, false, false, false, false, false, false, false, false, false, false, false, evidencePackId, traceId);
}

internal sealed record NoOpCommandResult(
    string ResultId,
    string RunnerId,
    string CommandId,
    string RunId,
    string HostId,
    string Status,
    string OutputRedacted,
    string? ErrorRedacted,
    string EvidencePackId,
    string TraceId,
    IReadOnlyList<OperatorTraceEvent> TraceEvents,
    OperatorNoExecutionProof NoExecutionProof,
    OperatorRedactionProof RedactionProof,
    string CommandDecision)
{
    public static NoOpCommandResult Completed(string runnerId, string commandId, string runId, string hostId, string evidencePackId, string traceId, string output) =>
        new("noop-result-1", runnerId, commandId, runId, hostId, "completed_noop", OperatorRedactor.Redact(output), null, evidencePackId, traceId,
            new[] { new OperatorTraceEvent("event-1", "command_noop_started"), new OperatorTraceEvent("event-2", "command_noop_completed") },
            OperatorNoExecutionProof.Clean(), OperatorRedactionProof.FromPayload(output), "allowed_noop");
}

internal sealed record OperatorTraceEvent(string TraceEventId, string EventType);

internal sealed record OperatorNoExecutionProof(bool Present, bool ExecutedRealAction, bool FilesystemWrite, bool ShellInvoked, bool BrowserAutomation, bool ProviderCloud)
{
    public static OperatorNoExecutionProof Clean() => new(true, false, false, false, false, false);
}

internal sealed record OperatorRedactionProof(bool Present, bool Scanned, bool Redacted)
{
    public static OperatorRedactionProof FromPayload(string payload) => new(true, true, OperatorRedactor.Redact(payload) != payload);
}

internal sealed record NoOpClaimsGuard(
    bool PcCommanderReadyClaimAllowed,
    bool RealCommandExecutionClaimAllowed,
    bool ShellEnabledClaimAllowed,
    bool FilesystemWriteClaimAllowed,
    bool BrowserAutomationClaimAllowed,
    bool ProviderCloudClaimAllowed,
    bool ProcessKillClaimAllowed,
    bool NetworkCallClaimAllowed,
    bool CredentialAccessClaimAllowed,
    bool ReleaseStoreClaimAllowed)
{
    public static NoOpClaimsGuard Create() => new(false, false, false, false, false, false, false, false, false, false);
}

internal sealed record MetadataReadRunner(
    string RunnerId,
    string MetadataKind,
    string RunnerMode,
    string ReadScope,
    bool Allowed,
    bool ReadsFilesystemContent,
    bool WritesFilesystem,
    bool ShellInvoked,
    bool BrowserAutomation,
    bool ProviderCloud,
    bool NetworkCall,
    bool ProcessKill,
    bool CredentialAccess,
    bool CapabilityUnlock,
    bool ReleaseStore,
    bool ProductFilesModified,
    bool BridgeCspModified)
{
    public static IReadOnlyList<string> AllowedMetadataKinds { get; } = new[]
    {
        "host_liveness_status",
        "command_channel_status",
        "allowlist_summary",
        "run_trace_summary",
        "evidence_pack_summary",
        "browser_claim_summary",
        "freeze_baseline_status",
        "caveat_status"
    };

    public static IReadOnlyList<string> BlockedMetadataKinds { get; } = new[]
    {
        "file_contents",
        "secrets",
        "credentials",
        "tokens",
        "cookies",
        "browser_session_data",
        "environment_variables_full",
        "process_memory",
        "network_credentials",
        "registry_secrets"
    };

    public static MetadataReadRunner ForKind(string metadataKind) =>
        new("metadata-runner-1", metadataKind, "read_only_if_currently_allowed", "safe_metadata_only", AllowedMetadataKinds.Contains(metadataKind),
            false, false, false, false, false, false, false, false, false, false, false, false);
}

internal sealed record MetadataReadResult(
    string ResultId,
    string RunnerId,
    string CommandId,
    string RunId,
    string HostId,
    string MetadataKind,
    string Status,
    string OutputRedacted,
    string EvidencePackId,
    string TraceId,
    OperatorTraceEvent TraceEvent,
    OperatorNoExecutionProof NoExecutionProof,
    OperatorRedactionProof RedactionProof,
    string CommandDecision,
    bool ReadFileContents,
    bool StoredSecrets)
{
    public static MetadataReadResult FromRunner(MetadataReadRunner runner, string output)
    {
        var allowed = runner.Allowed;
        return new("metadata-result-1", runner.RunnerId, "cmd-metadata", "run-metadata", "host-metadata", runner.MetadataKind,
            allowed ? "completed_metadata_read" : "blocked", OperatorRedactor.Redact(output), "pack-metadata", "trace-metadata",
            new OperatorTraceEvent("metadata-event-1", allowed ? "metadata_read_completed" : "metadata_read_blocked"),
            OperatorNoExecutionProof.Clean(), OperatorRedactionProof.FromPayload(output), allowed ? "allowed_metadata" : "blocked", false, false);
    }
}

internal sealed record MetadataNegativeMatrix(IReadOnlyList<MetadataNegativeRow> Rows)
{
    public static MetadataNegativeMatrix Create() => new(new[]
    {
        "file contents",
        "secrets",
        "credentials",
        "tokens",
        "cookies",
        "browser session data",
        "full env vars",
        "process memory",
        "registry secrets",
        "network credentials",
        "private keys",
        "provider keys",
        "raw logs"
    }.Select(static kind => new MetadataNegativeRow(kind, "blocked", false, false, false, false, false, false, true, true, true)).ToArray());
}

internal sealed record MetadataNegativeRow(
    string MetadataKind,
    string ExpectedDecision,
    bool Executed,
    bool FilesystemWrite,
    bool ShellInvoked,
    bool BrowserAutomation,
    bool ProviderCloud,
    bool CredentialAccess,
    bool NoExecutionProof,
    bool RedactionProof,
    bool EvidenceTraceLinked);

internal sealed record LocalOperatorEvidenceBridge(
    string BridgeId,
    string CommandResultId,
    string EvidencePackId,
    string TraceId,
    string BridgeMode,
    bool RealRuntimeBridge,
    bool ProductBridgeModified,
    bool CspModified,
    string OutputRedacted,
    OperatorNoExecutionProof NoExecutionProof,
    OperatorRedactionProof RedactionProof)
{
    public static LocalOperatorEvidenceBridge FromCommandResult(NoOpCommandResult result) =>
        new("operator-evidence-bridge-1", result.ResultId, result.EvidencePackId, result.TraceId, "test_only", false, false, false, OperatorRedactor.Redact("bridge output"), OperatorNoExecutionProof.Clean(), OperatorRedactionProof.FromPayload("bridge output"));
}

internal sealed record OperatorEvidenceTimelineSummary(
    bool IncludesHostDescriptor,
    bool IncludesLivenessProbe,
    bool IncludesCommandDescriptor,
    bool IncludesCommandResult,
    bool IncludesEvidencePack,
    bool IncludesTraceEvents,
    bool IncludesMetadataResult,
    bool IncludesCaveatStatus,
    bool MarksNoExecution,
    bool Executes,
    bool ShellInvoked,
    bool FilesystemWrite,
    bool BrowserAutomation,
    bool ProviderCloud,
    bool ReleaseStore,
    bool RuntimeUnlocked)
{
    public static OperatorEvidenceTimelineSummary Create() => new(true, true, true, true, true, true, true, true, true, false, false, false, false, false, false, false);
}

internal sealed record EvidenceBridgeClaimsGuard(
    bool BrowserBridgeModifiedClaimAllowed,
    bool CspModifiedClaimAllowed,
    bool RealCommandExecutionClaimAllowed,
    bool PcCommanderQaReadyClaimAllowed,
    bool FilesystemWriteClaimAllowed,
    bool ShellClaimAllowed,
    bool ProviderCloudClaimAllowed,
    bool BrowserAutomationClaimAllowed)
{
    public static EvidenceBridgeClaimsGuard Create() => new(false, false, false, false, false, false, false, false);
}

internal sealed record QaTriggerRecheck(
    string Status,
    bool RealHostVisible,
    bool RealBridgeConnected,
    bool RealSafeCommandExecution,
    bool DangerousCommandsBlocked,
    bool QaChecklistReady,
    bool FoundationOnlyStatusRecorded)
{
    public static QaTriggerRecheck FoundationOnly() => new("QA_TRIGGER_NOT_READY_FOUNDATION_ONLY", false, false, false, true, false, true);
}

internal sealed record NextOperatorStepRecommendation(
    string NextMilestone,
    bool TriggersManualQaYet,
    bool ShellEnabled,
    bool FilesystemWriteEnabled,
    bool BrowserAutomationEnabled,
    bool ProviderCloudEnabled,
    bool ReleaseStoreEnabled,
    bool ProductBridgeCspModified)
{
    public static NextOperatorStepRecommendation Create() =>
        new("M945-M956 - Controlled No-Op Runtime Adapter + Local Operator QA Prep", false, false, false, false, false, false, false);
}

internal static class OperatorRedactor
{
    public static string Redact(string value) =>
        value.Replace("FAKE_SECRET", "[REDACTED]", StringComparison.Ordinal)
            .Replace("token", "[REDACTED]", StringComparison.OrdinalIgnoreCase)
            .Replace("credential", "[REDACTED]", StringComparison.OrdinalIgnoreCase)
            .Replace("cookie", "[REDACTED]", StringComparison.OrdinalIgnoreCase);
}
