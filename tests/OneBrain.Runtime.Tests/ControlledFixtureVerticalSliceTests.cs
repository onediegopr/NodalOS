using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.AgentOperations.Contracts;
using OneBrain.AgentOperations.Core.Runtime;
using OneBrain.Core.Execution;

namespace OneBrain.Runtime.Tests;

[TestClass]
public sealed class ControlledFixtureVerticalSliceTests
{
    [TestMethod]
    [TestCategory("AiriSelectiveRuntime")]
    [TestCategory("MvpVerticalSlice")]
    public async Task ControlledFixtureCompletesWorkspaceMissionApprovalActionVerificationTimelineAndHandoff()
    {
        var result = await new NodalOsControlledFixtureVerticalSliceScenario().RunAsync();

        Assert.AreEqual(result.Workspace.WorkspaceId, result.MissionBinding.WorkspaceId);
        Assert.AreEqual(result.Runtime.Plan.MissionId, result.MissionBinding.MissionId);
        Assert.AreEqual(MissionStatus.Completed, result.Runtime.Mission.Status);
        Assert.AreEqual(NodalOsExecutionRegistryState.Completed, result.RegistryEntry.State);
        Assert.AreEqual(NodalOsApprovalDecisionKind.Approve, result.MissionAuthorizationDecision.DecisionKind);
        Assert.IsTrue(result.ControlledAction.Success);
        Assert.AreEqual(StepState.Succeeded, result.ControlledAction.FinalState);
        Assert.IsTrue(result.ControlledAction.VerificationResult?.Success);
        Assert.IsTrue(result.Timeline.Any(item => item.Kind == NodalOsCoreEventKind.ApprovalRequired));
        Assert.IsTrue(result.Timeline.Any(item => item.Kind == NodalOsCoreEventKind.ApprovalGranted));
        Assert.IsTrue(result.Timeline.Any(item => item.Kind == NodalOsCoreEventKind.ExecutionCompleted));
        Assert.IsTrue(result.Timeline.Any(item => item.Kind == NodalOsCoreEventKind.EvidenceAttached));
        Assert.IsTrue(result.Handoff.VerifiesEvidenceContent);
        Assert.IsFalse(result.Handoff.IsAuthoritative);
        Assert.IsFalse(result.Handoff.Executable);
    }

    [TestMethod]
    [TestCategory("AiriSelectiveRuntime")]
    [TestCategory("MvpVerticalSlice")]
    public async Task MissionLevelAuthorizationIsReusedWithoutPerStepApprovalChurn()
    {
        var result = await new NodalOsControlledFixtureVerticalSliceScenario().RunAsync();

        Assert.IsTrue(result.MissionAuthorizationReused);
        Assert.IsFalse(result.AdditionalStepApprovalRequested);
        Assert.IsFalse(result.Runtime.ApprovalRequested);
        Assert.AreEqual(
            result.MissionAuthorizationDecision.DecisionId,
            result.ControlledAction.Ledger.Entries
                .Where(entry => entry.ApprovalDecisionId is not null)
                .Select(entry => entry.ApprovalDecisionId)
                .Distinct(StringComparer.Ordinal)
                .Single());
        Assert.AreEqual(1, result.Timeline.Count(item => item.Kind == NodalOsCoreEventKind.ApprovalRequired));
        Assert.AreEqual(1, result.Timeline.Count(item => item.Kind == NodalOsCoreEventKind.ApprovalGranted));
    }

    [TestMethod]
    [TestCategory("AiriSelectiveRuntime")]
    [TestCategory("MvpVerticalSlice")]
    public async Task ControlledActionUsesSafeExecutionFsmAndProducesVerificationDigest()
    {
        var result = await new NodalOsControlledFixtureVerticalSliceScenario().RunAsync();

        CollectionAssert.AreEqual(
            new[] { StepState.Validated, StepState.Bound, StepState.Executing, StepState.Verifying, StepState.Succeeded },
            result.ControlledAction.Ledger.Entries.Select(entry => entry.ToState).ToArray());
        Assert.AreEqual(StepTransition.Verified, result.ControlledAction.Ledger.Entries[^1].Event);
        Assert.AreEqual("safe-execution-fsm-transition-digest", result.ControlledActionEvidence.Kind);
        Assert.AreEqual(NodalOsEvidenceBridgeAuthority.SupportsVerificationOnly, result.ControlledActionEvidence.Authority);
        Assert.IsFalse(string.IsNullOrWhiteSpace(result.ControlledActionEvidence.Hash));
        Assert.AreEqual(64, result.ControlledActionEvidence.Hash?.Length);
    }

    [TestMethod]
    [TestCategory("AiriSelectiveRuntime")]
    [TestCategory("MvpVerticalSlice")]
    public async Task SliceRemainsFixtureSafeAndHandoffIsStaticRedactedOutput()
    {
        var result = await new NodalOsControlledFixtureVerticalSliceScenario().RunAsync();
        var snapshot = result.ToInspectorSnapshot();

        Assert.IsTrue(snapshot.LocalDevOnly);
        Assert.IsTrue(snapshot.ReadOnly);
        Assert.IsTrue(snapshot.SecretsExcluded);
        Assert.IsFalse(snapshot.RealFilesystemTouched);
        Assert.IsFalse(snapshot.NetworkUsed);
        Assert.IsFalse(snapshot.ProductAuthorityGranted);
        Assert.IsTrue(snapshot.ControlledActionVerified);
        Assert.AreEqual("Completed", snapshot.MissionStatus);
        Assert.AreEqual("Completed", snapshot.RegistryState);
        Assert.AreEqual("Approve", snapshot.ApprovalStatus);
        Assert.AreEqual("Succeeded", snapshot.ControlledActionState);
        Assert.IsTrue(result.HandoffRender.Deterministic);
        Assert.IsFalse(result.HandoffRender.ContainsRawPayload);
        Assert.IsFalse(result.HandoffRender.ContainsExternalResource);
        StringAssert.Contains(result.HandoffRender.MarkdownRedacted, "Verified Mission Handoff");
        StringAssert.Contains(result.HandoffRender.HtmlRedacted, "data-nodal-os=\"verified-mission-handoff\"");
        Assert.IsFalse(result.HandoffRender.HtmlRedacted.Contains("<script", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(result.HandoffRender.HtmlRedacted.Contains("http://", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(result.HandoffRender.HtmlRedacted.Contains("https://", StringComparison.OrdinalIgnoreCase));
    }
}
