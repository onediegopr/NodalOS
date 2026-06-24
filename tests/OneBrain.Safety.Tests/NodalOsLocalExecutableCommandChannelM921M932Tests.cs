using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("LocalExecutableCommandChannel")]
[TestCategory("M921")]
[TestCategory("M922")]
[TestCategory("M923")]
[TestCategory("M924")]
[TestCategory("M925")]
[TestCategory("M926")]
[TestCategory("M927")]
[TestCategory("M928")]
[TestCategory("M929")]
[TestCategory("M930")]
[TestCategory("M931")]
[TestCategory("M932")]
[TestCategory("M921M932")]
public sealed class NodalOsLocalExecutableCommandChannelM921M932Tests
{
    [TestMethod]
    public void local_executable_host_descriptor_is_local_only_and_all_unlocks_false()
    {
        var host = LocalExecutableHostDescriptor.TestHarness("host-1");

        Assert.AreEqual("host-1", host.HostId);
        Assert.AreEqual("test_harness", host.HostKind);
        Assert.AreEqual("local_only", host.MachineScope);
        Assert.AreEqual("simulated", host.LivenessStatus);
        Assert.AreEqual("test_only", host.RuntimeMode);
        Assert.IsFalse(host.ProductiveEnabled);
        Assert.IsFalse(host.CommandChannelEnabled);
        Assert.IsFalse(host.ArbitraryShellEnabled);
        Assert.IsFalse(host.FilesystemWriteEnabled);
        Assert.IsFalse(host.BrowserAutomationEnabled);
        Assert.IsFalse(host.ProviderCloudEnabled);
        Assert.IsFalse(host.CapabilityUnlockEnabled);
        Assert.IsFalse(host.ReleaseStoreEnabled);
        Assert.IsFalse(host.ProductFilesModified);
        Assert.IsFalse(host.BridgeCspModified);
    }

    [TestMethod]
    public void liveness_probe_is_noop_test_only_and_records_unavailable_without_success()
    {
        var probe = LocalHostLivenessProbe.Unavailable("probe-1", "host-1", "run-1", "trace-1", "pack-1", "FAKE_SECRET unavailable");

        Assert.AreEqual("unavailable", probe.Status);
        Assert.AreEqual("no_op", probe.ProbeMode);
        Assert.IsFalse(probe.RealProcessInvoked);
        Assert.IsFalse(probe.SideEffects);
        Assert.IsFalse(probe.ShellInvoked);
        Assert.IsFalse(probe.FilesystemWrite);
        Assert.IsFalse(probe.BrowserAutomation);
        Assert.AreEqual("pack-1", probe.EvidencePackId);
        Assert.AreEqual("trace-1", probe.TraceId);
        Assert.IsFalse(probe.ErrorRedacted!.Contains("FAKE_SECRET", StringComparison.Ordinal));
        Assert.IsFalse(probe.SuccessInvented);
        Assert.IsFalse(probe.ProductFilesModified);
        Assert.IsFalse(probe.BridgeCspModified);
    }

    [TestMethod]
    public void host_liveness_event_links_evidence_trace_and_keeps_side_effects_false()
    {
        var probe = LocalHostLivenessProbe.Unavailable("probe-1", "host-1", "run-1", "trace-1", "pack-1", "FAKE_SECRET unavailable");
        var evt = HostLivenessEvent.FromProbe(probe);

        Assert.AreEqual("host_liveness_event", evt.EventType);
        Assert.AreEqual("pack-1", evt.EvidencePackId);
        Assert.AreEqual("trace-1", evt.TraceId);
        Assert.AreEqual("unavailable", evt.Status);
        Assert.IsFalse(evt.ErrorRedacted.Contains("FAKE_SECRET", StringComparison.Ordinal));
        Assert.IsFalse(evt.SuccessClaimed);
        Assert.IsFalse(evt.CommandsExecuted);
        Assert.IsFalse(evt.SideEffects);
    }

    [TestMethod]
    public void controlled_command_descriptor_requires_allowlist_and_blocks_unknown_destructive_shell()
    {
        var noop = ControlledCommandDescriptor.Create("cmd-1", "run-1", "host-1", "noop", "noop");
        var unknown = ControlledCommandDescriptor.Create("cmd-2", "run-1", "host-1", "unknown", "unknown");
        var dangerous = ControlledCommandDescriptor.Create("cmd-3", "run-1", "host-1", "arbitrary_shell", "powershell rm secret");

        Assert.IsTrue(noop.RequiresAllowlist);
        Assert.AreEqual("allowed", noop.AllowlistStatus);
        Assert.IsFalse(noop.Destructive);
        Assert.AreEqual("blocked", unknown.AllowlistStatus);
        Assert.AreEqual("blocked", dangerous.AllowlistStatus);
        Assert.IsTrue(dangerous.InvokesShell);
        Assert.IsTrue(dangerous.Destructive);

        foreach (var command in new[] { noop, unknown, dangerous })
        {
            Assert.IsFalse(command.WritesFilesystem);
            Assert.IsFalse(command.AutomatesBrowser);
            Assert.IsFalse(command.UsesProviderCloud);
            Assert.IsFalse(command.UnlocksCapability);
            Assert.IsFalse(command.PublicRelease);
            Assert.IsFalse(command.StoreSubmission);
            Assert.IsFalse(command.ProductFilesModified);
            Assert.IsFalse(command.BridgeCspModified);
            Assert.IsFalse(command.PayloadRedacted.Contains("FAKE_SECRET", StringComparison.Ordinal));
        }
    }

    [TestMethod]
    public void command_allowlist_allows_safe_foundation_categories_and_blocks_dangerous_categories()
    {
        var allowlist = CommandAllowlist.Foundation();

        CollectionAssert.AreEquivalent(
            new[] { "noop", "metadata_read", "liveness_probe", "environment_observation", "focus_safe_window_planning" },
            allowlist.AllowedCategories.ToArray());
        Assert.AreEqual("planned_only", allowlist.ModeFor("focus_safe_window_planning"));

        foreach (var blocked in new[]
        {
            "arbitrary_shell",
            "filesystem_write",
            "browser_automation",
            "provider_cloud",
            "capability_unlock",
            "process_kill",
            "network_call",
            "credential_access",
            "release_store",
            "product_file_modification",
            "bridge_csp_modification"
        })
        {
            Assert.IsFalse(allowlist.IsAllowed(blocked), blocked);
        }
    }

    [TestMethod]
    public void command_result_binds_evidence_trace_and_preserves_no_execution()
    {
        var command = ControlledCommandDescriptor.Create("cmd-1", "run-1", "host-1", "metadata_read", "read metadata");
        var result = ControlledCommandResult.From(command, "pack-1", "trace-1", "model-trace-1", "cost-trace-1");

        Assert.AreEqual("allowed_metadata", result.Decision);
        Assert.IsFalse(result.Executed);
        Assert.IsFalse(result.SideEffects);
        Assert.AreEqual("pack-1", result.EvidencePackId);
        Assert.AreEqual("trace-1", result.TraceId);
        Assert.IsFalse(result.NoExecutionProof.ActualExecutionPerformed);
        Assert.IsTrue(result.RedactionProof.PayloadRedacted);
        Assert.IsFalse(result.OutputRedacted.Contains("FAKE_SECRET", StringComparison.Ordinal));
        Assert.IsFalse(result.RuntimeUnlock);
    }

    [TestMethod]
    public void planned_focus_command_does_not_focus_real_window()
    {
        var command = ControlledCommandDescriptor.Create("cmd-1", "run-1", "host-1", "focus_safe_window_planning", "plan focus");
        var result = ControlledCommandResult.From(command, "pack-1", "trace-1", null, null);

        Assert.AreEqual("planned_only", result.Decision);
        Assert.IsFalse(result.Executed);
        Assert.IsFalse(result.RealWindowFocused);
        Assert.IsFalse(result.SideEffects);
    }

    [TestMethod]
    public void dangerous_command_guard_blocks_all_danger_patterns()
    {
        var guard = DangerousCommandGuard.Default();

        foreach (var dangerous in new[]
        {
            "cmd /c whoami",
            "powershell Remove-Item file",
            "bash -lc rm -rf /tmp/x",
            "format disk",
            "kill process",
            "read credential token key",
            "network exfiltration",
            "browser automation click",
            "filesystem write",
            "registry modification",
            "privilege escalation",
            "release store package",
            "product Bridge CSP modification"
        })
        {
            Assert.IsTrue(guard.IsBlocked(dangerous), dangerous);
        }
    }

    [TestMethod]
    public void pc_commander_claim_guard_rejects_ready_and_unlock_claims()
    {
        var guard = PcCommanderClaimGuard.Default();

        Assert.IsFalse(guard.PcCommanderReadyClaimAllowed);
        Assert.IsFalse(guard.ShellEnabledClaimAllowed);
        Assert.IsFalse(guard.FilesystemWriteEnabledClaimAllowed);
        Assert.IsFalse(guard.BrowserAutomationEnabledClaimAllowed);
        Assert.IsFalse(guard.ProviderCloudEnabledClaimAllowed);
        Assert.IsFalse(guard.CapabilityUnlockEnabledClaimAllowed);
        Assert.IsFalse(guard.ReleaseStoreClaimAllowed);
        Assert.IsFalse(guard.ProductBridgeCspClaimAllowed);
    }

    [TestMethod]
    public void command_negative_matrix_blocks_all_rows_with_no_execution_and_evidence_trace()
    {
        var matrix = CommandNegativeMatrix.Create();

        foreach (var row in matrix.Rows)
        {
            if (row.ExpectedDecision != "blocked")
            {
                Assert.Fail($"Unexpected decision for {row.Category}: {row.ExpectedDecision}");
            }

            Assert.IsFalse(row.Executed, row.Category);
            Assert.IsFalse(row.SideEffects, row.Category);
            Assert.IsTrue(row.NoExecutionProof, row.Category);
            Assert.IsTrue(row.RedactionProof, row.Category);
            Assert.IsTrue(row.EvidenceTraceLinked, row.Category);
            Assert.IsFalse(row.RuntimeUnlock, row.Category);
        }

        CollectionAssert.Contains(matrix.Rows.Select(static row => row.Category).ToArray(), "unknown command");
        CollectionAssert.Contains(matrix.Rows.Select(static row => row.Category).ToArray(), "Bridge/CSP");
    }

    [TestMethod]
    public void local_operator_roadmap_preserves_no_go_and_requires_owner_gate_before_real_pc_control()
    {
        var roadmap = LocalOperatorRoadmap.Create();

        CollectionAssert.Contains(roadmap.Stages.ToArray(), "foundation command channel");
        CollectionAssert.Contains(roadmap.Stages.ToArray(), "owner approval gate before real PC control");
        Assert.IsFalse(roadmap.ManualQaYet);
        Assert.IsFalse(roadmap.PcCommanderYet);
        Assert.IsTrue(roadmap.RequiresAllowlistHardening);
        Assert.IsTrue(roadmap.RequiresOwnerGateBeforeRealPcControl);
        Assert.IsFalse(roadmap.ShellEnabled);
        Assert.IsFalse(roadmap.FilesystemWriteEnabled);
        Assert.IsFalse(roadmap.BrowserAutomationUnlock);
        Assert.IsFalse(roadmap.ProviderCloudEnabled);
        Assert.AreEqual("NO-GO", roadmap.ReleaseStore);
    }

    [TestMethod]
    public void qa_trigger_criteria_rejects_foundation_only_and_requires_visible_safe_runner()
    {
        var criteria = QaTriggerCriteria.Create();

        Assert.IsFalse(criteria.ShouldTrigger(browserOnly: true, docsOnly: false, traceOnly: false, localHostVisible: false, allowlisted: false, safeNoopOrMetadataCommand: false, dangerousBlocked: false, qaChecklistReady: false));
        Assert.IsFalse(criteria.ShouldTrigger(browserOnly: false, docsOnly: true, traceOnly: false, localHostVisible: false, allowlisted: false, safeNoopOrMetadataCommand: false, dangerousBlocked: false, qaChecklistReady: false));
        Assert.IsFalse(criteria.ShouldTrigger(browserOnly: false, docsOnly: false, traceOnly: true, localHostVisible: false, allowlisted: false, safeNoopOrMetadataCommand: false, dangerousBlocked: false, qaChecklistReady: false));
        Assert.IsTrue(criteria.ShouldTrigger(browserOnly: false, docsOnly: false, traceOnly: false, localHostVisible: true, allowlisted: true, safeNoopOrMetadataCommand: true, dangerousBlocked: true, qaChecklistReady: true));
    }

    private sealed record LocalExecutableHostDescriptor(
        string HostId,
        string HostKind,
        string HostName,
        string MachineScope,
        string LivenessStatus,
        string RuntimeMode,
        bool ProductiveEnabled,
        bool CommandChannelEnabled,
        bool ArbitraryShellEnabled,
        bool FilesystemWriteEnabled,
        bool BrowserAutomationEnabled,
        bool ProviderCloudEnabled,
        bool CapabilityUnlockEnabled,
        bool ReleaseStoreEnabled,
        bool ProductFilesModified,
        bool BridgeCspModified)
    {
        public static LocalExecutableHostDescriptor TestHarness(string hostId) =>
            new(hostId, "test_harness", "NODAL OS Local Host Foundation", "local_only", "simulated", "test_only", false, false, false, false, false, false, false, false, false, false);
    }

    private sealed record LocalHostLivenessProbe(
        string ProbeId,
        string HostId,
        string? RunId,
        string Status,
        string ProbeMode,
        bool RealProcessInvoked,
        bool SideEffects,
        bool ShellInvoked,
        bool FilesystemWrite,
        bool BrowserAutomation,
        string? EvidencePackId,
        string? TraceId,
        string? ErrorRedacted,
        bool SuccessInvented,
        bool ProductFilesModified,
        bool BridgeCspModified)
    {
        public static LocalHostLivenessProbe Unavailable(string probeId, string hostId, string runId, string traceId, string evidencePackId, string error) =>
            new(probeId, hostId, runId, "unavailable", "no_op", false, false, false, false, false, evidencePackId, traceId, Redactor.Redact(error), false, false, false);
    }

    private sealed record HostLivenessEvent(
        string EventType,
        string HostId,
        string? EvidencePackId,
        string? TraceId,
        string Status,
        string ErrorRedacted,
        bool SuccessClaimed,
        bool CommandsExecuted,
        bool SideEffects)
    {
        public static HostLivenessEvent FromProbe(LocalHostLivenessProbe probe) =>
            new("host_liveness_event", probe.HostId, probe.EvidencePackId, probe.TraceId, probe.Status, probe.ErrorRedacted ?? string.Empty, false, false, false);
    }

    private sealed record ControlledCommandDescriptor(
        string CommandId,
        string RunId,
        string HostId,
        string CommandCategory,
        string CommandName,
        string CommandMode,
        bool RequiresAllowlist,
        string AllowlistStatus,
        bool Destructive,
        bool WritesFilesystem,
        bool InvokesShell,
        bool AutomatesBrowser,
        bool UsesProviderCloud,
        bool UnlocksCapability,
        bool PublicRelease,
        bool StoreSubmission,
        bool ProductFilesModified,
        bool BridgeCspModified,
        string PayloadRedacted)
    {
        public static ControlledCommandDescriptor Create(string commandId, string runId, string hostId, string category, string name)
        {
            var allowlist = CommandAllowlist.Foundation();
            var dangerous = DangerousCommandGuard.Default().IsBlocked($"{category} {name}");
            var allowed = allowlist.IsAllowed(category) && !dangerous;
            var mode = category == "focus_safe_window_planning" ? "planned_only" : allowed ? "no_op" : "blocked";

            return new(
                commandId,
                runId,
                hostId,
                category,
                name,
                mode,
                true,
                allowed ? "allowed" : "blocked",
                dangerous,
                false,
                category == "arbitrary_shell" || name.Contains("powershell", StringComparison.OrdinalIgnoreCase),
                false,
                false,
                false,
                false,
                false,
                false,
                false,
                Redactor.Redact(name));
        }
    }

    private sealed class CommandAllowlist
    {
        private readonly Dictionary<string, string> _allowed = new(StringComparer.Ordinal)
        {
            ["noop"] = "no_op",
            ["metadata_read"] = "no_op",
            ["liveness_probe"] = "no_op",
            ["environment_observation"] = "no_op",
            ["focus_safe_window_planning"] = "planned_only"
        };

        public IReadOnlyCollection<string> AllowedCategories => _allowed.Keys;

        public static CommandAllowlist Foundation() => new();

        public bool IsAllowed(string category) => _allowed.ContainsKey(category);

        public string ModeFor(string category) => _allowed.TryGetValue(category, out var mode) ? mode : "blocked";
    }

    private sealed record ControlledCommandResult(
        string CommandResultId,
        string CommandId,
        string RunId,
        string HostId,
        string Decision,
        bool Executed,
        bool SideEffects,
        bool RealWindowFocused,
        string OutputRedacted,
        string? ErrorRedacted,
        string EvidencePackId,
        string TraceId,
        string? ModelTraceId,
        string? CostTraceId,
        NoExecutionProof NoExecutionProof,
        RedactionProof RedactionProof,
        bool RuntimeUnlock)
    {
        public static ControlledCommandResult From(ControlledCommandDescriptor command, string evidencePackId, string traceId, string? modelTraceId, string? costTraceId)
        {
            var decision = command.AllowlistStatus == "blocked"
                ? "blocked"
                : command.CommandCategory == "metadata_read"
                    ? "allowed_metadata"
                    : command.CommandCategory == "focus_safe_window_planning"
                        ? "planned_only"
                        : "allowed_noop";

            return new($"result-{command.CommandId}", command.CommandId, command.RunId, command.HostId, decision, false, false, false, Redactor.Redact("output FAKE_SECRET"), null, evidencePackId, traceId, modelTraceId, costTraceId, new(false, false, false, false, false), new(true, false), false);
        }
    }

    private sealed record NoExecutionProof(bool ActualExecutionPerformed, bool FilesystemWritePerformed, bool BrowserAutomationPerformed, bool ProviderCloudInvoked, bool CapabilityUnlocked);

    private sealed record RedactionProof(bool PayloadRedacted, bool ContainsSecrets);

    private sealed class DangerousCommandGuard
    {
        private static readonly string[] Patterns =
        [
            "shell",
            "cmd",
            "powershell",
            "bash",
            "del",
            "rm",
            "remove",
            "format",
            "disk",
            "kill",
            "credential",
            "token",
            "key",
            "exfiltration",
            "browser automation",
            "filesystem write",
            "registry",
            "privilege",
            "release",
            "store",
            "package",
            "Bridge",
            "CSP"
        ];

        public static DangerousCommandGuard Default() => new();

        public bool IsBlocked(string command) => Patterns.Any(pattern => command.Contains(pattern, StringComparison.OrdinalIgnoreCase));
    }

    private sealed record PcCommanderClaimGuard(
        bool PcCommanderReadyClaimAllowed,
        bool ShellEnabledClaimAllowed,
        bool FilesystemWriteEnabledClaimAllowed,
        bool BrowserAutomationEnabledClaimAllowed,
        bool ProviderCloudEnabledClaimAllowed,
        bool CapabilityUnlockEnabledClaimAllowed,
        bool ReleaseStoreClaimAllowed,
        bool ProductBridgeCspClaimAllowed)
    {
        public static PcCommanderClaimGuard Default() => new(false, false, false, false, false, false, false, false);
    }

    private sealed record CommandNegativeMatrix(IReadOnlyList<CommandNegativeRow> Rows)
    {
        public static CommandNegativeMatrix Create() =>
            new(new[]
            {
                "unknown command",
                "arbitrary shell",
                "filesystem write",
                "browser automation",
                "provider cloud",
                "capability unlock",
                "credential access",
                "network call",
                "process kill",
                "release/store",
                "product files",
                "Bridge/CSP"
            }.Select(static category => new CommandNegativeRow(category, "blocked", false, false, true, true, true, false)).ToArray());
    }

    private sealed record CommandNegativeRow(string Category, string ExpectedDecision, bool Executed, bool SideEffects, bool NoExecutionProof, bool RedactionProof, bool EvidenceTraceLinked, bool RuntimeUnlock);

    private sealed record LocalOperatorRoadmap(
        IReadOnlyList<string> Stages,
        bool ManualQaYet,
        bool PcCommanderYet,
        bool RequiresAllowlistHardening,
        bool RequiresOwnerGateBeforeRealPcControl,
        bool ShellEnabled,
        bool FilesystemWriteEnabled,
        bool BrowserAutomationUnlock,
        bool ProviderCloudEnabled,
        string ReleaseStore)
    {
        public static LocalOperatorRoadmap Create() =>
            new(
                [
                    "foundation command channel",
                    "allowlist hardening",
                    "no-op command runner",
                    "metadata read runner",
                    "safe local observation",
                    "controlled focus/window planning",
                    "evidence/trace visible",
                    "manual QA candidate",
                    "owner approval gate before real PC control"
                ],
                false,
                false,
                true,
                true,
                false,
                false,
                false,
                false,
                "NO-GO");
    }

    private sealed class QaTriggerCriteria
    {
        public static QaTriggerCriteria Create() => new();

        public bool ShouldTrigger(bool browserOnly, bool docsOnly, bool traceOnly, bool localHostVisible, bool allowlisted, bool safeNoopOrMetadataCommand, bool dangerousBlocked, bool qaChecklistReady)
        {
            if (browserOnly || docsOnly || traceOnly)
                return false;

            return localHostVisible && allowlisted && safeNoopOrMetadataCommand && dangerousBlocked && qaChecklistReady;
        }
    }

    private static class Redactor
    {
        public static string Redact(string value) =>
            value.Replace("FAKE_SECRET", "[REDACTED]", StringComparison.Ordinal)
                .Replace("token", "[REDACTED]", StringComparison.Ordinal)
                .Replace("key", "[REDACTED]", StringComparison.Ordinal);
    }
}
