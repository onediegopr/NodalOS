using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("PermissiveBrowserClaim")]
[TestCategory("M909")]
[TestCategory("M910")]
[TestCategory("M911")]
[TestCategory("M912")]
[TestCategory("M913")]
[TestCategory("M914")]
[TestCategory("M915")]
[TestCategory("M916")]
[TestCategory("M917")]
[TestCategory("M918")]
[TestCategory("M919")]
[TestCategory("M920")]
[TestCategory("M909M920")]
public sealed class NodalOsPermissiveBrowserClaimM909M920Tests
{
    [TestMethod]
    public void browser_capability_map_defaults_to_permissive_observational_mode()
    {
        var tab = BrowserTabDescriptor.Create("tab-1", "win-1", "https://example.test/path?token=raw", "Sensitive title FAKE_SECRET");
        var map = BrowserCapabilityMap.ForTab(tab.TabId, runId: "run-1");

        Assert.AreEqual("permissive", map.Mode);
        Assert.IsTrue(map.CanReadTitle);
        Assert.IsTrue(map.CanReadUrl);
        Assert.IsTrue(map.CanObserveVisibleDom);
        Assert.IsTrue(map.CanTrackNavigation);
        Assert.IsTrue(map.CanTrackReload);
        Assert.IsTrue(map.CanEmitTraceEvents);
        Assert.IsTrue(map.CanResumeAfterReload);
        Assert.IsFalse(map.BlockingEnabled);
        Assert.IsFalse(map.PerActionApprovalRequired);
        Assert.IsFalse(map.BrowserInjectionShieldEnabled);
        Assert.IsFalse(map.WebRiskFilterEnabled);
        Assert.IsFalse(map.ApprovalPromptsEnabled);
        Assert.IsFalse(map.BrowserAutomationProductiveUnlock);
        Assert.IsFalse(map.ProviderCloudUnlock);
        Assert.IsFalse(map.ProductFilesModified);
        Assert.IsFalse(map.BridgeCspModified);
        Assert.AreEqual("redacted", tab.RedactionState);
        Assert.IsFalse(tab.Url.Contains("token=raw", StringComparison.Ordinal));
        Assert.IsFalse(tab.Title.Contains("FAKE_SECRET", StringComparison.Ordinal));
    }

    [TestMethod]
    public void tab_claim_lifecycle_is_non_blocking_and_preserves_lost_stale_reattach_states()
    {
        var store = new TabClaimStore();
        var claim = store.ClaimTabForRun("run-1", "tab-1", "win-1", "trace-1");

        Assert.AreEqual("active", claim.ClaimStatus);
        Assert.IsFalse(claim.ApprovalRequired);
        Assert.IsFalse(claim.BlockingUxEnabled);
        Assert.IsFalse(claim.BrowserAutomationProductiveUnlock);
        Assert.AreEqual(claim.ClaimId, store.GetActiveClaimForRun("run-1")!.ClaimId);

        var updated = store.UpdateTabClaim(claim.ClaimId, "https://example.test/page", "Example");
        Assert.AreEqual("active", updated.ClaimStatus);
        Assert.IsFalse(updated.ApprovalRequired);

        var lost = store.MarkTabLost(claim.ClaimId);
        Assert.AreEqual("lost", lost.ClaimStatus);
        Assert.IsFalse(lost.SuccessInvented);

        var stale = store.MarkTabStale(claim.ClaimId);
        Assert.AreEqual("stale", stale.ClaimStatus);
        Assert.IsFalse(stale.SuccessInvented);

        var reattached = store.ReattachTabClaim(claim.ClaimId, "tab-2");
        Assert.AreEqual("reattached", reattached.ClaimStatus);
        Assert.AreEqual("tab-2", reattached.TabId);

        var released = store.ReleaseTabClaim(claim.ClaimId);
        Assert.AreEqual("released", released.ClaimStatus);
        Assert.AreEqual(1, store.ListClaimsForRun("run-1").Count);
    }

    [TestMethod]
    public void browser_claim_events_cover_lifecycle_are_redacted_and_link_run_trace()
    {
        var events = BrowserClaimEventFactory.CreateLifecycle("run-1", "tab-1", "claim-1", "trace-1");
        var eventTypes = events.Select(static x => x.EventType).ToArray();

        CollectionAssert.Contains(eventTypes, "tab_detected");
        CollectionAssert.Contains(eventTypes, "tab_claimed");
        CollectionAssert.Contains(eventTypes, "tab_claim_updated");
        CollectionAssert.Contains(eventTypes, "tab_released");
        CollectionAssert.Contains(eventTypes, "tab_lost");
        CollectionAssert.Contains(eventTypes, "tab_reattached");
        CollectionAssert.Contains(eventTypes, "tab_navigation_observed");
        CollectionAssert.Contains(eventTypes, "tab_reload_observed");
        CollectionAssert.Contains(eventTypes, "capability_map_updated");

        foreach (var evt in events)
        {
            Assert.AreEqual("run-1", evt.RunId);
            Assert.AreEqual("trace-1", evt.TraceId);
            Assert.IsTrue(evt.PayloadRedacted);
            Assert.IsFalse(evt.RedactedSummary.Contains("FAKE_SECRET", StringComparison.Ordinal));
        }
    }

    [TestMethod]
    public void run_evidence_pack_records_execution_mode_explicitly_and_never_unlocks_runtime()
    {
        var claimEvent = BrowserClaimEventFactory.Create("tab_claimed", "run-1", "tab-1", "claim-1", "trace-1");
        var pack = RunEvidencePack.Create("run-1", "trace-1", "claim-1", "fake");
        pack.Append(claimEvent);
        pack.RecordError("redacted error");
        pack.Complete("redacted final summary");

        Assert.AreEqual("run-1", pack.RunId);
        Assert.AreEqual("trace-1", pack.TraceId);
        Assert.AreEqual("claim-1", pack.TabClaimId);
        Assert.AreEqual("completed", pack.Status);
        Assert.AreEqual("fake", pack.ExecutionMode);
        Assert.AreEqual(1, pack.EventsCount);
        Assert.AreEqual(1, pack.ErrorsCount);
        Assert.IsTrue(pack.FinalSummary!.Contains("redacted", StringComparison.Ordinal));
        Assert.IsFalse(pack.RealExecutionClaimed);
        Assert.IsFalse(pack.ProviderCloudUnlock);
        Assert.IsFalse(pack.FilesystemBrowserCapabilityUnlock);
        Assert.IsFalse(pack.ContainsSecrets);
    }

    [TestMethod]
    public void evidence_pack_go_no_go_guard_rejects_false_claims()
    {
        var guard = EvidencePackGoNoGoGuard.Default();

        Assert.IsFalse(guard.ArtifactExportReadyClaimAllowed);
        Assert.IsFalse(guard.ProductiveRuntimeClaimAllowed);
        Assert.IsFalse(guard.ProviderCloudClaimAllowed);
        Assert.IsFalse(guard.FilesystemWriteClaimAllowed);
        Assert.IsFalse(guard.BrowserAutomationClaimAllowed);
        Assert.IsFalse(guard.CapabilityUnlockClaimAllowed);
        Assert.IsFalse(guard.ReleaseStoreClaimAllowed);
        Assert.IsFalse(guard.PerActionApprovalClaimAllowed);
    }

    [TestMethod]
    public void run_trace_records_ordered_redacted_replayable_events_without_execution()
    {
        var trace = RunTraceRecord.Create("run-1", resumable: true);
        trace.Append("system", "trace_started", "FAKE_SECRET_SHOULD_REDACT", tabId: "tab-1", claimId: "claim-1");
        trace.Append("extension", "tab_claimed", "safe payload", tabId: "tab-1", claimId: "claim-1");

        Assert.IsTrue(trace.Replayable);
        Assert.IsTrue(trace.Resumable);
        Assert.AreEqual(2, trace.EventCount);
        Assert.AreEqual(1, trace.Events[0].SequenceNumber);
        Assert.AreEqual(2, trace.Events[1].SequenceNumber);
        Assert.IsTrue(trace.Events.All(static evt => evt.PayloadRedacted));
        Assert.IsFalse(trace.Events.Any(static evt => evt.Payload.Contains("FAKE_SECRET", StringComparison.Ordinal)));
        Assert.IsFalse(trace.ExecutedAnything);
        Assert.IsFalse(trace.BrowserAutomationProductiveUnlock);
    }

    [TestMethod]
    public void replay_summary_reconstructs_audit_only_without_executor_or_provider_unlock()
    {
        var trace = RunTraceRecord.Create("run-1", resumable: true);
        trace.Append("system", "trace_started", "safe payload", tabId: "tab-1", claimId: "claim-1");
        trace.Append("extension", "tab_claimed", "safe payload", tabId: "tab-1", claimId: "claim-1");
        var modelTrace = ModelTraceRecord.Fake("run-1");
        var costTrace = CostTraceRecord.Unknown("run-1", modelTrace.ModelTraceId);

        var summary = ReplaySummaryBuilder.Build(trace, "claim-1", modelTrace, costTrace);

        Assert.AreEqual("run-1", summary.RunId);
        Assert.AreEqual("claim-1", summary.TabClaimId);
        Assert.AreEqual(2, summary.OrderedEvents.Count);
        Assert.AreEqual(modelTrace.ModelTraceId, summary.ModelTraceId);
        Assert.AreEqual(costTrace.CostTraceId, summary.CostTraceId);
        Assert.AreEqual("reconstructed_audit_only", summary.FinalStatus);
        Assert.IsFalse(summary.Reexecuted);
        Assert.IsFalse(summary.ExecutorSelected);
        Assert.IsFalse(summary.ProviderCloudUnlock);
        Assert.IsFalse(summary.FilesystemBrowserCapabilityUnlock);
    }

    [TestMethod]
    public void resume_marker_is_explicit_and_does_not_invent_success_or_execute()
    {
        var trace = RunTraceRecord.Create("run-1", resumable: true);
        trace.Append("system", "trace_started", "safe payload", tabId: "tab-1", claimId: "claim-1");

        var marker = ResumeMarker.Create(trace, "claim-1", "tab-1");
        var blocked = ResumeMarker.Blocked("run-2", "trace-2", "missing required context");

        Assert.IsTrue(marker.Resumable);
        Assert.AreEqual(trace.LastEventId, marker.LastEventId);
        Assert.AreEqual("claim-1", marker.ClaimId);
        Assert.IsFalse(marker.Executed);
        Assert.IsFalse(marker.BrowserAutomationProductiveUnlock);
        Assert.IsFalse(blocked.Resumable);
        Assert.AreEqual("missing required context", blocked.BlockedReason);
        Assert.IsFalse(blocked.SuccessInvented);
    }

    [TestMethod]
    public void model_trace_marks_fake_or_cloud_disabled_without_provider_unlock_or_local_gateways()
    {
        var fake = ModelTraceRecord.Fake("run-1");
        var disabled = ModelTraceRecord.CloudDisabled("run-2");

        Assert.AreEqual("fake", fake.ProviderMode);
        Assert.IsFalse(fake.RealProviderInvoked);
        Assert.IsTrue(fake.FakeProviderInvoked);
        Assert.AreEqual("cloud_disabled", disabled.ProviderMode);
        Assert.IsFalse(disabled.RealProviderInvoked);
        Assert.IsFalse(disabled.ProviderCloudUnlock);
        Assert.IsFalse(disabled.ContainsApiKeys);
        Assert.IsFalse(disabled.OllamaAdded);
        Assert.IsFalse(disabled.LmStudioAdded);
        Assert.IsFalse(disabled.JanAdded);
        Assert.IsFalse(disabled.LocalModelGatewayAdded);
    }

    [TestMethod]
    public void cost_trace_is_estimate_only_and_does_not_create_billing_or_managed_ai()
    {
        var modelTrace = ModelTraceRecord.Fake("run-1");
        var cost = CostTraceRecord.Unknown("run-1", modelTrace.ModelTraceId);

        Assert.IsTrue(cost.IsEstimate);
        Assert.AreEqual("unknown", cost.InputTokensEstimated);
        Assert.AreEqual("not_available", cost.EstimatedCost);
        Assert.AreEqual(modelTrace.ModelTraceId, cost.ModelTraceId);
        Assert.IsFalse(cost.BillingCreated);
        Assert.IsFalse(cost.AdminPanelCreated);
        Assert.IsFalse(cost.QuotaSystemCreated);
        Assert.IsFalse(cost.ManagedAiUnlock);
        Assert.IsFalse(cost.ContainsSecrets);
    }

    private sealed record BrowserTabDescriptor(
        string TabId,
        string WindowId,
        string Url,
        string Origin,
        string Title,
        string Status,
        string LastSeenAt,
        string Source,
        string RedactionState)
    {
        public static BrowserTabDescriptor Create(string tabId, string windowId, string url, string title) =>
            new(
                tabId,
                windowId,
                Redactor.Redact(url),
                "https://example.test",
                Redactor.Redact(title),
                "observed",
                "logical-1",
                "test-only-foundation",
                "redacted");
    }

    private sealed record BrowserCapabilityMap(
        string TabId,
        string? RunId,
        bool CanReadTitle,
        bool CanReadUrl,
        bool CanObserveVisibleDom,
        bool CanTrackNavigation,
        bool CanTrackReload,
        bool CanEmitTraceEvents,
        bool CanResumeAfterReload,
        string Mode,
        bool BlockingEnabled,
        bool PerActionApprovalRequired,
        bool BrowserInjectionShieldEnabled,
        bool WebRiskFilterEnabled,
        bool ApprovalPromptsEnabled,
        bool BrowserAutomationProductiveUnlock,
        bool ProviderCloudUnlock,
        bool ProductFilesModified,
        bool BridgeCspModified)
    {
        public static BrowserCapabilityMap ForTab(string tabId, string? runId) =>
            new(tabId, runId, true, true, true, true, true, true, true, "permissive", false, false, false, false, false, false, false, false, false);
    }

    private sealed class TabClaimStore
    {
        private readonly List<TabClaimRecord> _claims = [];

        public TabClaimRecord ClaimTabForRun(string runId, string tabId, string windowId, string traceId)
        {
            var claim = new TabClaimRecord($"claim-{_claims.Count + 1}", runId, tabId, windowId, "logical-1", null, "active", null, null, null, traceId, false, false, false, false);
            _claims.Add(claim);
            return claim;
        }

        public TabClaimRecord UpdateTabClaim(string claimId, string url, string title) =>
            Replace(claimId, claim => claim with { LastSeenUrl = Redactor.Redact(url), LastSeenTitle = Redactor.Redact(title), ClaimStatus = "active" });

        public TabClaimRecord ReleaseTabClaim(string claimId) =>
            Replace(claimId, claim => claim with { ClaimStatus = "released", ReleasedAt = "logical-2" });

        public TabClaimRecord MarkTabLost(string claimId) =>
            Replace(claimId, claim => claim with { ClaimStatus = "lost", SuccessInvented = false });

        public TabClaimRecord MarkTabStale(string claimId) =>
            Replace(claimId, claim => claim with { ClaimStatus = "stale", SuccessInvented = false });

        public TabClaimRecord ReattachTabClaim(string claimId, string tabId) =>
            Replace(claimId, claim => claim with { ClaimStatus = "reattached", TabId = tabId, SuccessInvented = false });

        public IReadOnlyList<TabClaimRecord> ListClaimsForRun(string runId) => _claims.Where(claim => claim.RunId == runId).ToArray();

        public TabClaimRecord? GetActiveClaimForRun(string runId) => _claims.LastOrDefault(claim => claim.RunId == runId && claim.ClaimStatus == "active");

        private TabClaimRecord Replace(string claimId, Func<TabClaimRecord, TabClaimRecord> update)
        {
            var index = _claims.FindIndex(claim => claim.ClaimId == claimId);
            Assert.AreNotEqual(-1, index);
            _claims[index] = update(_claims[index]);
            return _claims[index];
        }
    }

    private sealed record TabClaimRecord(
        string ClaimId,
        string RunId,
        string TabId,
        string WindowId,
        string ClaimedAt,
        string? ReleasedAt,
        string ClaimStatus,
        string? LastSeenUrl,
        string? LastSeenTitle,
        string? LastSeenOrigin,
        string TraceId,
        bool ApprovalRequired,
        bool BlockingUxEnabled,
        bool BrowserAutomationProductiveUnlock,
        bool SuccessInvented);

    private sealed record BrowserClaimEvent(
        string EventId,
        string? RunId,
        string TabId,
        string? ClaimId,
        string EventType,
        int Sequence,
        string RedactedSummary,
        bool PayloadRedacted,
        string Severity,
        string? TraceId);

    private static class BrowserClaimEventFactory
    {
        private static readonly string[] Lifecycle =
        [
            "tab_detected",
            "tab_claimed",
            "tab_claim_updated",
            "tab_released",
            "tab_lost",
            "tab_reattached",
            "tab_navigation_observed",
            "tab_reload_observed",
            "capability_map_updated"
        ];

        public static BrowserClaimEvent Create(string eventType, string? runId, string tabId, string? claimId, string? traceId) =>
            new($"event-{eventType}", runId, tabId, claimId, eventType, 1, Redactor.Redact("safe summary FAKE_SECRET"), true, "info", traceId);

        public static IReadOnlyList<BrowserClaimEvent> CreateLifecycle(string runId, string tabId, string claimId, string traceId) =>
            Lifecycle.Select((eventType, index) => new BrowserClaimEvent($"event-{index + 1}", runId, tabId, claimId, eventType, index + 1, Redactor.Redact("safe summary FAKE_SECRET"), true, "info", traceId)).ToArray();
    }

    private sealed class RunEvidencePack
    {
        private readonly List<BrowserClaimEvent> _events = [];
        private readonly List<string> _errors = [];

        private RunEvidencePack(string runId, string traceId, string? tabClaimId, string executionMode)
        {
            RunId = runId;
            TraceId = traceId;
            TabClaimId = tabClaimId;
            ExecutionMode = executionMode;
        }

        public string RunId { get; }
        public string TraceId { get; }
        public string? TabClaimId { get; }
        public string Status { get; private set; } = "running";
        public string ExecutionMode { get; }
        public int EventsCount => _events.Count;
        public int ErrorsCount => _errors.Count;
        public string? FinalSummary { get; private set; }
        public bool RealExecutionClaimed => false;
        public bool ProviderCloudUnlock => false;
        public bool FilesystemBrowserCapabilityUnlock => false;
        public bool ContainsSecrets => false;

        public static RunEvidencePack Create(string runId, string traceId, string? tabClaimId, string executionMode) => new(runId, traceId, tabClaimId, executionMode);

        public void Append(BrowserClaimEvent evt) => _events.Add(evt);

        public void RecordError(string error) => _errors.Add(Redactor.Redact(error));

        public void Complete(string summary)
        {
            Status = "completed";
            FinalSummary = Redactor.Redact(summary);
        }
    }

    private sealed record EvidencePackGoNoGoGuard(
        bool ArtifactExportReadyClaimAllowed,
        bool ProductiveRuntimeClaimAllowed,
        bool ProviderCloudClaimAllowed,
        bool FilesystemWriteClaimAllowed,
        bool BrowserAutomationClaimAllowed,
        bool CapabilityUnlockClaimAllowed,
        bool ReleaseStoreClaimAllowed,
        bool PerActionApprovalClaimAllowed)
    {
        public static EvidencePackGoNoGoGuard Default() => new(false, false, false, false, false, false, false, false);
    }

    private sealed class RunTraceRecord
    {
        private readonly List<RunTraceEvent> _events = [];

        private RunTraceRecord(string runId, bool resumable)
        {
            TraceId = $"trace-{runId}";
            RunId = runId;
            Resumable = resumable;
        }

        public string TraceId { get; }
        public string RunId { get; }
        public bool Replayable => _events.Count >= 2;
        public bool Resumable { get; }
        public int EventCount => _events.Count;
        public string? LastEventId => _events.LastOrDefault()?.TraceEventId;
        public IReadOnlyList<RunTraceEvent> Events => _events;
        public bool ExecutedAnything => false;
        public bool BrowserAutomationProductiveUnlock => false;

        public static RunTraceRecord Create(string runId, bool resumable) => new(runId, resumable);

        public void Append(string actor, string eventType, string payload, string? tabId, string? claimId) =>
            _events.Add(new RunTraceEvent($"trace-event-{_events.Count + 1}", TraceId, RunId, _events.Count + 1, eventType, actor, tabId, claimId, Redactor.Redact(payload), true, "info", "audit-only"));
    }

    private sealed record RunTraceEvent(
        string TraceEventId,
        string TraceId,
        string RunId,
        int SequenceNumber,
        string EventType,
        string Actor,
        string? TabId,
        string? ClaimId,
        string Payload,
        bool PayloadRedacted,
        string Severity,
        string ReplayHint);

    private sealed record ReplaySummary(
        string RunId,
        string? TabClaimId,
        IReadOnlyList<RunTraceEvent> OrderedEvents,
        string? ModelTraceId,
        string? CostTraceId,
        string FinalStatus,
        bool Reexecuted,
        bool ExecutorSelected,
        bool ProviderCloudUnlock,
        bool FilesystemBrowserCapabilityUnlock);

    private static class ReplaySummaryBuilder
    {
        public static ReplaySummary Build(RunTraceRecord trace, string? tabClaimId, ModelTraceRecord? modelTrace, CostTraceRecord? costTrace) =>
            new(trace.RunId, tabClaimId, trace.Events.OrderBy(static evt => evt.SequenceNumber).ToArray(), modelTrace?.ModelTraceId, costTrace?.CostTraceId, "reconstructed_audit_only", false, false, false, false);
    }

    private sealed record ResumeMarker(
        string ResumeMarkerId,
        string RunId,
        string TraceId,
        string? LastEventId,
        string? ClaimId,
        string? TabId,
        bool Resumable,
        string ResumeReason,
        string? BlockedReason,
        bool Executed,
        bool BrowserAutomationProductiveUnlock,
        bool SuccessInvented)
    {
        public static ResumeMarker Create(RunTraceRecord trace, string? claimId, string? tabId) =>
            new($"resume-{trace.RunId}", trace.RunId, trace.TraceId, trace.LastEventId, claimId, tabId, trace.Resumable, "trace context present", null, false, false, false);

        public static ResumeMarker Blocked(string runId, string traceId, string reason) =>
            new($"resume-{runId}", runId, traceId, null, null, null, false, "blocked", reason, false, false, false);
    }

    private sealed record ModelTraceRecord(
        string ModelTraceId,
        string RunId,
        string Provider,
        string Model,
        string ProviderMode,
        bool RealProviderInvoked,
        bool FakeProviderInvoked,
        bool FallbackUsed,
        string? FallbackReason,
        string Status,
        bool ProviderCloudUnlock,
        bool ContainsApiKeys,
        bool OllamaAdded,
        bool LmStudioAdded,
        bool JanAdded,
        bool LocalModelGatewayAdded)
    {
        public static ModelTraceRecord Fake(string runId) =>
            new($"model-trace-{runId}", runId, "fake", "fake-model", "fake", false, true, false, null, "completed", false, false, false, false, false, false);

        public static ModelTraceRecord CloudDisabled(string runId) =>
            new($"model-trace-{runId}", runId, "cloud-disabled", "not-selected", "cloud_disabled", false, false, true, "provider cloud disabled", "blocked", false, false, false, false, false, false);
    }

    private sealed record CostTraceRecord(
        string CostTraceId,
        string RunId,
        string? ModelTraceId,
        string Provider,
        string Model,
        string InputTokensEstimated,
        string OutputTokensEstimated,
        string TotalTokensEstimated,
        string EstimatedCost,
        string Currency,
        bool IsEstimate,
        bool BillingCreated,
        bool AdminPanelCreated,
        bool QuotaSystemCreated,
        bool ManagedAiUnlock,
        bool ContainsSecrets)
    {
        public static CostTraceRecord Unknown(string runId, string? modelTraceId) =>
            new($"cost-trace-{runId}", runId, modelTraceId, "fake", "fake-model", "unknown", "unknown", "unknown", "not_available", "not_available", true, false, false, false, false, false);
    }

    private static class Redactor
    {
        public static string Redact(string value) =>
            value.Replace("FAKE_SECRET_SHOULD_REDACT", "[REDACTED]", StringComparison.Ordinal)
                .Replace("FAKE_SECRET", "[REDACTED]", StringComparison.Ordinal)
                .Replace("token=raw", "token=[REDACTED]", StringComparison.Ordinal);
    }
}
