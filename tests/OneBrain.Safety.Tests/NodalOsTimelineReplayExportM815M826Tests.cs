using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.Safety.Tests.SimulatedRuntime;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("TimelineReplayExportDeterminism")]
[TestCategory("M815")]
[TestCategory("M816")]
[TestCategory("M817")]
[TestCategory("M818")]
[TestCategory("M819")]
[TestCategory("M820")]
[TestCategory("M821")]
[TestCategory("M822")]
[TestCategory("M823")]
[TestCategory("M824")]
[TestCategory("M825")]
[TestCategory("M826")]
public sealed class NodalOsTimelineReplayExportM815M826Tests
{
    private const string TimelineContractPath = "artifacts/agent-operations/m815/timeline-projection-contract.json";
    private const string RoundtripPath = "artifacts/agent-operations/m816/evidence-timeline-roundtrip-consistency.json";
    private const string ReplayGuardPath = "artifacts/agent-operations/m818/replay-guard-contract.json";
    private const string ExportPath = "artifacts/agent-operations/m821/audit-export-contract.json";
    private const string DeterminismPath = "artifacts/agent-operations/m824/cross-run-determinism-contract.json";
    private const string FinalPath = "artifacts/agent-operations/m815-m826/evidence-timeline-replay-export-determinism-go-no-go.json";
    private const string ProductBridgeCspPath = "artifacts/agent-operations/m826/product-bridge-csp-unchanged-proof.json";
    private const string NextMilestonePath = "artifacts/agent-operations/m826/next-macro-milestone-recommendation.json";

    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(Environment.CurrentDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "OneBrain.slnx")))
            dir = dir.Parent;

        return dir?.FullName ?? Environment.CurrentDirectory;
    }

    private static string FullPath(string relativePath) => Path.Combine(RepoRoot(), relativePath);
    private static string ReadAll(string relativePath) => File.ReadAllText(FullPath(relativePath));

    [TestMethod]
    public void EvidenceLedgerProjectsToTimelineEvents()
    {
        var route = new SimulatedCapabilityRouter().Route("local_provider_model");
        var timeline = new SimulatedTimelineProjector().Project(route);

        Assert.AreEqual(route.LedgerEvents.Count, timeline.Count);
        AssertTimelineClean(timeline);
        CollectionAssert.Contains(timeline.Select(x => x.EventKind).ToArray(), "SIMULATED_ROUTE_EVALUATED");
        CollectionAssert.Contains(timeline.Select(x => x.EventKind).ToArray(), "SIMULATED_POLICY_DECISION_RECORDED");
        CollectionAssert.Contains(timeline.Select(x => x.EventKind).ToArray(), "SIMULATED_EVIDENCE_ENVELOPE_CREATED");
        CollectionAssert.Contains(timeline.Select(x => x.EventKind).ToArray(), "SIMULATED_REDACTION_PROOF_CREATED");
        CollectionAssert.Contains(timeline.Select(x => x.EventKind).ToArray(), "SIMULATED_NO_EXECUTION_PROOF_CREATED");
        Assert.IsTrue(timeline.All(x => x.SourceEvidenceId == route.EvidenceEnvelope.EnvelopeId));
        Assert.IsTrue(timeline.All(x => x.DecisionType == SimulatedPolicyDecisionType.AllowSimulatedDryRun.ToString()));
        Assert.IsTrue(timeline.Select(x => x.SequenceIndex).SequenceEqual(Enumerable.Range(0, timeline.Count)));
    }

    [TestMethod]
    public void EvidenceTimelineRoundtripPreservesLedgerEvidenceOrderingRedactionAndNoExecution()
    {
        var route = new SimulatedCapabilityRouter().Route("provider_cloud_live_call");
        var summary = new SimulatedTimelineRoundtrip().Roundtrip(route);

        Assert.AreEqual(route.EvidenceEnvelope.EnvelopeId, summary.EvidenceEnvelopeId);
        Assert.AreEqual(route.LedgerEvents.Count, summary.LedgerEventCount);
        Assert.AreEqual(route.LedgerEvents.Count, summary.TimelineEventCount);
        Assert.IsTrue(summary.AllLedgerEventsProjected);
        Assert.IsTrue(summary.AllTimelineEventsRedacted);
        Assert.IsTrue(summary.NoExecutionProofPreserved);
        Assert.IsTrue(summary.RedactionProofPreserved);
        Assert.IsTrue(summary.OrderingPreserved);
        Assert.IsTrue(summary.ReplaySafe);
        Assert.IsFalse(summary.ExecutionPerformed);
        Assert.IsFalse(summary.ProductiveRuntime);
        Assert.AreEqual(0, summary.SideEffectSinkInvocations);
    }

    [TestMethod]
    public void TimelineRoundtripCoversDecisionTypesAndCapabilityClasses()
    {
        var router = new SimulatedCapabilityRouter();
        var approval = new SimulatedManualApprovalBoundary();
        var roundtrip = new SimulatedTimelineRoundtrip();
        var summaries = new[]
        {
            roundtrip.Roundtrip(router.Route("local_provider_model")),
            roundtrip.Roundtrip(router.Route("provider_cloud_live_call")),
            roundtrip.Roundtrip(router.Route("unknown_future_capability")),
            roundtrip.Roundtrip(router.Route(SimulatedRuntimeRoutingMatrix.PolicyViolationCapability)),
            roundtrip.Roundtrip(router.Route(SimulatedRuntimeRoutingMatrix.ManualApprovalCapability)),
            roundtrip.Roundtrip(approval.Decide("local_provider_model", SimulatedApprovalStatus.ApprovalGrantedSimulated)),
            roundtrip.Roundtrip(approval.Decide("local_provider_model", SimulatedApprovalStatus.ApprovalDeniedSimulated)),
            roundtrip.Roundtrip(approval.Decide("local_provider_model", SimulatedApprovalStatus.ApprovalExpiredSimulated)),
            roundtrip.Roundtrip(approval.Decide("local_provider_model", SimulatedApprovalStatus.ApprovalInvalidSimulated))
        };

        foreach (var expected in new[]
        {
            SimulatedPolicyDecisionType.AllowSimulatedDryRun.ToString(),
            SimulatedPolicyDecisionType.DenyDenylistedCapability.ToString(),
            SimulatedPolicyDecisionType.DenyUnsupportedCapability.ToString(),
            SimulatedPolicyDecisionType.DenyPolicyViolation.ToString(),
            SimulatedPolicyDecisionType.RequireManualApprovalSimulated.ToString(),
            SimulatedApprovalStatus.ApprovalGrantedSimulated.ToString(),
            SimulatedApprovalStatus.ApprovalDeniedSimulated.ToString(),
            SimulatedApprovalStatus.ApprovalExpiredSimulated.ToString(),
            SimulatedApprovalStatus.ApprovalInvalidSimulated.ToString()
        })
        {
            Assert.IsTrue(summaries.Any(x => x.DecisionType == expected), expected);
        }

        Assert.IsTrue(summaries.All(x => !x.ExecutionPerformed));
        Assert.IsTrue(summaries.All(x => !x.ProductiveRuntime));
        Assert.IsTrue(summaries.SelectMany(x => x.TimelineEvents).All(x => !x.ExecutionPerformed && !x.ProductiveRuntime));
    }

    [TestMethod]
    public void ReplayGuardIsAuditOnlyAndNeverInvokesExecutionSurfaces()
    {
        var summary = new SimulatedTimelineRoundtrip().Roundtrip(new SimulatedCapabilityRouter().Route("local_provider_model"));
        var replay = new SimulatedReplayGuard().Evaluate(summary);

        Assert.AreEqual(ReplayMode.AuditOnlyInMemory, replay.ReplayMode);
        Assert.IsTrue(replay.ReplayAllowed);
        Assert.IsFalse(replay.ReplayExecuted);
        AssertCleanReplay(replay);
        CollectionAssert.Contains(replay.ReasonCodes.ToArray(), "replay_audit_only_in_memory");
        CollectionAssert.Contains(replay.ReasonCodes.ToArray(), "replay_execution_prohibited");
        CollectionAssert.Contains(replay.ReasonCodes.ToArray(), "replay_executor_invocation_blocked");
    }

    [TestMethod]
    public void ApprovalReplayOverrideGuardBlocksDangerousAndTerminalOutcomes()
    {
        var boundary = new SimulatedManualApprovalBoundary();
        var roundtrip = new SimulatedTimelineRoundtrip();
        var replayGuard = new SimulatedReplayGuard();

        var denylisted = roundtrip.Roundtrip(boundary.Decide("provider_cloud_live_call", SimulatedApprovalStatus.ApprovalGrantedSimulated));
        var unsupported = roundtrip.Roundtrip(boundary.Decide("unknown_future_capability", SimulatedApprovalStatus.ApprovalGrantedSimulated));
        var policyViolation = roundtrip.Roundtrip(boundary.Decide(SimulatedRuntimeRoutingMatrix.PolicyViolationCapability, SimulatedApprovalStatus.ApprovalGrantedSimulated));
        var denied = roundtrip.Roundtrip(boundary.Decide("local_provider_model", SimulatedApprovalStatus.ApprovalDeniedSimulated));
        var expired = roundtrip.Roundtrip(boundary.Decide("local_provider_model", SimulatedApprovalStatus.ApprovalExpiredSimulated));
        var invalid = roundtrip.Roundtrip(boundary.Decide("local_provider_model", SimulatedApprovalStatus.ApprovalInvalidSimulated));

        foreach (var replay in new[]
        {
            replayGuard.Evaluate(denylisted, "approval-provider_cloud_live_call"),
            replayGuard.Evaluate(unsupported, "approval-unknown_future_capability"),
            replayGuard.Evaluate(policyViolation, "approval-simulated_policy_violation"),
            replayGuard.Evaluate(denied, "approval-local_provider_model"),
            replayGuard.Evaluate(expired, "approval-local_provider_model"),
            replayGuard.Evaluate(invalid, "approval-local_provider_model")
        })
        {
            AssertCleanReplay(replay);
            Assert.IsFalse(replay.ReplayExecuted);
        }
    }

    [TestMethod]
    public void ReplayTamperAndIdempotencyGuardDeniesTamperingAndAllowsDuplicateAuditOnly()
    {
        var summary = new SimulatedTimelineRoundtrip().Roundtrip(new SimulatedCapabilityRouter().Route("local_provider_model"));
        var guard = new SimulatedReplayGuard();

        var first = guard.Evaluate(summary);
        var duplicate = guard.Evaluate(summary);
        Assert.AreEqual(first.SourceRoundtripId, duplicate.SourceRoundtripId);
        Assert.AreEqual(ReplayMode.AuditOnlyInMemory, duplicate.ReplayMode);
        AssertCleanReplay(duplicate);

        foreach (var replay in new[]
        {
            guard.DenyMissingEvidence(summary),
            guard.DenyMismatchedApprovalRequest(summary),
            guard.DenyTamperedLedger(summary, "replay_modified_reason_code_denied"),
            guard.DenyTamperedLedger(summary, "replay_modified_decision_type_denied"),
            guard.DenyTamperedLedger(summary, "replay_modified_evidence_id_denied"),
            guard.DenyTamperedLedger(summary, "replay_modified_no_execution_ref_denied"),
            guard.DenyTamperedLedger(summary, "replay_modified_redaction_ref_denied"),
            guard.DenyTamperedLedger(summary, "replay_reordered_critical_events_denied"),
            guard.DenyTamperedLedger(summary, "replay_injected_execution_true_denied"),
            guard.DenyTamperedLedger(summary, "replay_injected_productive_true_denied"),
            guard.DenyTamperedLedger(summary, "replay_injected_product_bridge_csp_true_denied")
        })
        {
            Assert.IsFalse(replay.ReplayAllowed);
            AssertCleanReplay(replay);
        }
    }

    [TestMethod]
    public void AuditExportPackageLinksRoundtripReplayRefsAndKeepsSafeRedactedNoExecution()
    {
        var summary = new SimulatedTimelineRoundtrip().Roundtrip(new SimulatedCapabilityRouter().Route("local_provider_model"));
        var replay = new SimulatedReplayGuard().Evaluate(summary);
        var package = new SimulatedAuditExporter().Export(summary, replay);

        Assert.AreEqual(summary.RoundtripId, package.SourceRoundtripId);
        Assert.AreEqual(summary.SourceDecisionId, package.SourceDecisionId);
        CollectionAssert.Contains(package.EvidenceEnvelopeRefs.ToArray(), summary.EvidenceEnvelopeId);
        Assert.AreEqual(summary.TimelineEventCount, package.LedgerEventRefs.Count);
        Assert.AreEqual(summary.TimelineEventCount, package.TimelineEventRefs.Count);
        CollectionAssert.Contains(package.ReplayGuardRefs.ToArray(), replay.ReplayId);
        AssertCleanExport(package);
        Assert.IsTrue(package.ExportSafe);
        Assert.IsTrue(package.Redacted);
    }

    [TestMethod]
    public void AuditExportMatrixCoversRoutesApprovalsReplayAndTamperDenied()
    {
        var router = new SimulatedCapabilityRouter();
        var approval = new SimulatedManualApprovalBoundary();
        var roundtrip = new SimulatedTimelineRoundtrip();
        var replayGuard = new SimulatedReplayGuard();
        var exporter = new SimulatedAuditExporter();
        var summaries = new[]
        {
            roundtrip.Roundtrip(router.Route("local_provider_model")),
            roundtrip.Roundtrip(router.Route("provider_cloud_live_call")),
            roundtrip.Roundtrip(router.Route("unknown_future_capability")),
            roundtrip.Roundtrip(router.Route(SimulatedRuntimeRoutingMatrix.PolicyViolationCapability)),
            roundtrip.Roundtrip(router.Route(SimulatedRuntimeRoutingMatrix.ManualApprovalCapability)),
            roundtrip.Roundtrip(approval.Decide("local_provider_model", SimulatedApprovalStatus.ApprovalGrantedSimulated)),
            roundtrip.Roundtrip(approval.Decide("local_provider_model", SimulatedApprovalStatus.ApprovalDeniedSimulated)),
            roundtrip.Roundtrip(approval.Decide("local_provider_model", SimulatedApprovalStatus.ApprovalExpiredSimulated)),
            roundtrip.Roundtrip(approval.Decide("local_provider_model", SimulatedApprovalStatus.ApprovalInvalidSimulated))
        };

        foreach (var summary in summaries)
            AssertCleanExport(exporter.Export(summary, replayGuard.Evaluate(summary)));

        var tampered = replayGuard.DenyTamperedLedger(summaries[0]);
        var tamperedExport = exporter.Export(summaries[0], tampered);
        AssertCleanExport(tamperedExport);
        Assert.IsTrue(tamperedExport.ExportSafe);
    }

    [TestMethod]
    public void AuditExportNegativeRedactionGuardRejectsDangerousInjectedFlags()
    {
        var summary = new SimulatedTimelineRoundtrip().Roundtrip(new SimulatedCapabilityRouter().Route("local_provider_model"));
        var replay = new SimulatedReplayGuard().Evaluate(summary);
        var exporter = new SimulatedAuditExporter();

        foreach (var injected in new[]
        {
            "executionPerformed",
            "productiveRuntime",
            "providerCloudInvoked",
            "filesystemWritePerformed",
            "browserAutomationPerformed",
            "capabilityUnlocked",
            "publicReleasePerformed",
            "storeSubmissionPerformed",
            "signedPublicZipCreated",
            "productFilesModified",
            "bridgeCspModified"
        })
        {
            var package = exporter.ExportWithInjectedDangerousFlag(summary, replay, injected);
            Assert.IsFalse(package.ExportSafe, injected);
            AssertCleanExport(package);
        }
    }

    [TestMethod]
    public void CrossRunDeterminismIgnoresGeneratedIdsAndPreservesLogicalResult()
    {
        static DeterminismSignature Signature()
        {
            var summary = new SimulatedTimelineRoundtrip().Roundtrip(new SimulatedCapabilityRouter().Route("local_provider_model"));
            var replay = new SimulatedReplayGuard().Evaluate(summary);
            var export = new SimulatedAuditExporter().Export(summary, replay);
            return SimulatedDeterminism.Capture(summary, replay, export, "allowed_fake_executor");
        }

        AssertSignaturesEqual(Signature(), Signature());
    }

    [TestMethod]
    public void DeterminismCoversDeniedApprovalReplayAndExportFixtures()
    {
        var router = new SimulatedCapabilityRouter();
        var approval = new SimulatedManualApprovalBoundary();
        var roundtrip = new SimulatedTimelineRoundtrip();
        var replayGuard = new SimulatedReplayGuard();
        var exporter = new SimulatedAuditExporter();

        foreach (var summary in new[]
        {
            roundtrip.Roundtrip(router.Route("provider_cloud_live_call")),
            roundtrip.Roundtrip(router.Route("unknown_future_capability")),
            roundtrip.Roundtrip(router.Route(SimulatedRuntimeRoutingMatrix.PolicyViolationCapability)),
            roundtrip.Roundtrip(approval.Decide("local_provider_model", SimulatedApprovalStatus.ApprovalGrantedSimulated)),
            roundtrip.Roundtrip(approval.Decide("local_provider_model", SimulatedApprovalStatus.ApprovalDeniedSimulated))
        })
        {
            var one = SimulatedDeterminism.Capture(summary, replayGuard.Evaluate(summary), exporter.Export(summary, replayGuard.Evaluate(summary)), "none_or_fake_only");
            var two = SimulatedDeterminism.Capture(summary, replayGuard.Evaluate(summary), exporter.Export(summary, replayGuard.Evaluate(summary)), "none_or_fake_only");
            AssertSignaturesEqual(one, two);
            CollectionAssert.Contains(one.NoExecutionFlags.ToArray(), "actual:false");
            CollectionAssert.Contains(one.RedactionFlags.ToArray(), "secrets:false");
        }
    }

    [TestMethod]
    public void ArtifactsExistAndFinalDecisionIsReady()
    {
        foreach (var path in new[] { TimelineContractPath, RoundtripPath, ReplayGuardPath, ExportPath, DeterminismPath, FinalPath })
            Assert.IsTrue(File.Exists(FullPath(path)), path);

        var final = ReadAll(FinalPath);
        StringAssert.Contains(final, "SIMULATED_EVIDENCE_TIMELINE_REPLAY_EXPORT_DETERMINISM_READY");
        StringAssert.Contains(final, "\"runtimeProductiveExecution\": \"DISABLED\"");
        StringAssert.Contains(final, "\"productiveEnabled\": \"PROHIBITED\"");
    }

    [TestMethod]
    public void ProductRuntimeReleaseStoreBridgeAndCspRemainBlocked()
    {
        var final = ReadAll(FinalPath);
        var productBridge = ReadAll(ProductBridgeCspPath);

        StringAssert.Contains(final, "\"providerCloudLiveCalls\": \"DISABLED\"");
        StringAssert.Contains(final, "\"filesystemBrowserCapabilityUnlock\": \"DISABLED\"");
        StringAssert.Contains(final, "\"publicRelease\": \"NO-GO\"");
        StringAssert.Contains(final, "\"chromeWebStore\": \"NO-GO\"");
        StringAssert.Contains(productBridge, "\"productFilesModified\": false");
        StringAssert.Contains(productBridge, "\"bridgeCspModified\": false");
    }

    [TestMethod]
    public void NextMilestoneRecommendsGovernanceSnapshot()
    {
        var content = ReadAll(NextMilestonePath);

        StringAssert.Contains(content, "M827-M838");
        StringAssert.Contains(content, "Simulated Runtime Governance Snapshot + Readiness Scorecard + Pre-Audit Consolidation");
        StringAssert.Contains(content, "\"productiveUnlockAllowed\": false");
    }

    private static void AssertTimelineClean(IReadOnlyList<TimelineEvent> timeline)
    {
        Assert.IsTrue(timeline.All(x => x.ReplaySafe));
        Assert.IsTrue(timeline.All(x => !x.ExecutionPerformed));
        Assert.IsTrue(timeline.All(x => !x.ProductiveRuntime));
        Assert.IsTrue(timeline.All(x => x.RedactedSummary.StartsWith("REDACTED:", StringComparison.Ordinal)));
    }

    private static void AssertCleanReplay(ReplayGuardResult replay)
    {
        Assert.IsFalse(replay.ReplayExecuted);
        Assert.IsFalse(replay.ExecutorInvoked);
        Assert.IsFalse(replay.ProviderCloudInvoked);
        Assert.IsFalse(replay.FilesystemWritePerformed);
        Assert.IsFalse(replay.BrowserAutomationPerformed);
        Assert.IsFalse(replay.CapabilityUnlocked);
        Assert.IsFalse(replay.PublicReleasePerformed);
        Assert.IsFalse(replay.StoreSubmissionPerformed);
        Assert.IsFalse(replay.SignedPublicZipCreated);
        Assert.IsFalse(replay.ProductFilesModified);
        Assert.IsFalse(replay.BridgeCspModified);
        Assert.IsFalse(replay.ProductiveEnabled);
        Assert.AreEqual(0, replay.SideEffectSinkInvocations);
        Assert.IsFalse(replay.NoExecutionProof.ProductiveEnabled);
    }

    private static void AssertCleanExport(AuditExportPackage package)
    {
        Assert.IsTrue(package.Redacted);
        Assert.IsFalse(package.ContainsSecrets);
        Assert.IsFalse(package.ContainsCredentials);
        Assert.IsFalse(package.ContainsTokens);
        Assert.IsFalse(package.ContainsRawLogs);
        Assert.IsFalse(package.ContainsBrowserSessionData);
        Assert.IsFalse(package.ExecutionPerformed);
        Assert.IsFalse(package.ProductiveRuntime);
        Assert.IsFalse(package.ProviderCloudInvoked);
        Assert.IsFalse(package.FilesystemWritePerformed);
        Assert.IsFalse(package.BrowserAutomationPerformed);
        Assert.IsFalse(package.CapabilityUnlocked);
        Assert.IsFalse(package.PublicReleasePerformed);
        Assert.IsFalse(package.StoreSubmissionPerformed);
        Assert.IsFalse(package.SignedPublicZipCreated);
        Assert.IsFalse(package.ProductFilesModified);
        Assert.IsFalse(package.BridgeCspModified);
    }

    private static void AssertSignaturesEqual(DeterminismSignature expected, DeterminismSignature actual)
    {
        Assert.AreEqual(expected.DecisionType, actual.DecisionType);
        Assert.AreEqual(expected.SourceCapability, actual.SourceCapability);
        Assert.AreEqual(expected.SelectedExecutorCategory, actual.SelectedExecutorCategory);
        CollectionAssert.AreEqual(expected.ReasonCodes.ToArray(), actual.ReasonCodes.ToArray());
        CollectionAssert.AreEqual(expected.EvidenceFlags.ToArray(), actual.EvidenceFlags.ToArray());
        CollectionAssert.AreEqual(expected.LedgerEventKinds.ToArray(), actual.LedgerEventKinds.ToArray());
        CollectionAssert.AreEqual(expected.TimelineEventKinds.ToArray(), actual.TimelineEventKinds.ToArray());
        CollectionAssert.AreEqual(expected.NoExecutionFlags.ToArray(), actual.NoExecutionFlags.ToArray());
        CollectionAssert.AreEqual(expected.RedactionFlags.ToArray(), actual.RedactionFlags.ToArray());
        Assert.AreEqual(expected.ReplayMode, actual.ReplayMode);
        Assert.AreEqual(expected.ExportSafe, actual.ExportSafe);
    }
}
