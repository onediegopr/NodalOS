using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.AgentOperations.Contracts;
using OneBrain.AgentOperations.Core.Runtime;

namespace OneBrain.Runtime.Tests;

[TestClass]
[TestCategory("AiriSelectiveRuntime")]
[TestCategory("MvpVerticalSlice")]
[TestCategory("ControlledFileOperation")]
public sealed class NodalOsTestOwnedFileUpdateMissionScenarioTests
{
    [TestMethod]
    public async Task MissionCompletesExactHashUpdateWithApprovalSnapshotVerificationEvidenceAndCleanup()
    {
        var result = await new NodalOsTestOwnedFileUpdateMissionScenario().RunAsync();
        var snapshot = result.ToSnapshot();

        Assert.AreEqual(MissionStatus.Completed, result.Mission.Status);
        Assert.AreEqual(NodalOsExecutionRegistryState.Completed, result.RegistryEntry.State);
        Assert.AreEqual(NodalOsApprovalDecisionKind.Approve, result.MissionAuthorizationDecision.DecisionKind);
        Assert.IsTrue(result.SeedCreateAction.Success);
        Assert.IsTrue(result.FileUpdateAction.Success);
        Assert.AreEqual(NodalOsTestOwnedFileUpdateDecision.UpdatedAndVerified, result.FileUpdateAction.Decision);
        Assert.IsTrue(result.FileUpdateAction.PreconditionMatched);
        Assert.IsTrue(result.FileUpdateAction.SnapshotCreated);
        Assert.IsTrue(result.FileUpdateAction.AtomicReplaceUsed);
        Assert.IsTrue(result.FileUpdateAction.Verified);
        Assert.IsTrue(result.FileUpdateAction.RollbackAvailable);
        Assert.IsNotNull(result.FileUpdateAction.RestorePlan);
        Assert.IsFalse(result.FileUpdateAction.RestorePlan.CanRestoreUserWorkspace);
        Assert.IsTrue(result.FileUpdateAction.RestorePlan.RequiresExactCurrentHash);
        Assert.IsTrue(result.FileUpdateAction.RestorePlan.RestrictedToTestOwnedFixture);
        Assert.IsNotNull(result.FileUpdateAction.Evidence);
        Assert.AreEqual("test-owned-file-update-verification", result.FileUpdateAction.Evidence.Kind);
        Assert.AreEqual(result.FileUpdateAction.UpdatedSha256, result.FileUpdateAction.Evidence.Hash);
        Assert.IsTrue(result.FixtureCleanup.Success);
        Assert.IsTrue(result.FixtureCleanup.RootRemoved);

        Assert.IsTrue(snapshot.LocalDevOnly);
        Assert.IsTrue(snapshot.ReadOnlySurface);
        Assert.IsTrue(snapshot.SecretsExcluded);
        Assert.AreEqual("Completed", snapshot.MissionStatus);
        Assert.AreEqual("Completed", snapshot.RegistryState);
        Assert.AreEqual("Approve", snapshot.ApprovalStatus);
        Assert.AreEqual("UpdatedAndVerified", snapshot.FileUpdateState);
        Assert.IsTrue(snapshot.FileUpdateVerified);
        Assert.IsTrue(snapshot.RollbackPlanCreated);
        Assert.IsTrue(snapshot.FixtureCleanupRemovedRollbackSnapshot);
        Assert.IsTrue(snapshot.TestOwnedFilesystemTouched);
        Assert.IsTrue(snapshot.TestOwnedFixtureCleaned);
        Assert.IsFalse(snapshot.UserWorkspaceFilesystemTouched);
        Assert.IsFalse(snapshot.NetworkUsed);
        Assert.IsFalse(snapshot.ProductAuthorityGranted);
        Assert.AreEqual(64, snapshot.OriginalSha256?.Length);
        Assert.AreEqual(64, snapshot.UpdatedSha256?.Length);
        Assert.AreNotEqual(snapshot.OriginalSha256, snapshot.UpdatedSha256);
        Assert.IsTrue(snapshot.EvidenceCount > 0);
        Assert.IsTrue(snapshot.TimelineCount > 0);
    }

    [TestMethod]
    public async Task MissionUsesOneApprovalAndDoesNotAddPerStepApprovalChurn()
    {
        var result = await new NodalOsTestOwnedFileUpdateMissionScenario().RunAsync();

        Assert.IsTrue(result.MissionAuthorizationReused);
        Assert.IsFalse(result.AdditionalStepApprovalRequested);
        Assert.AreEqual(1, result.Timeline.Count(item => item.Kind == NodalOsCoreEventKind.ApprovalRequired));
        Assert.AreEqual(1, result.Timeline.Count(item => item.Kind == NodalOsCoreEventKind.ApprovalGranted));
        Assert.AreEqual(
            result.MissionAuthorizationDecision.DecisionId,
            result.RegistryEntry.ApprovalRef);
    }

    [TestMethod]
    public async Task UpdateEvidencePrecedesMissionCompletionAndUsesCanonicalTimeline()
    {
        var result = await new NodalOsTestOwnedFileUpdateMissionScenario().RunAsync();
        var evidenceId = result.FileUpdateAction.Evidence!.EvidenceId;
        var evidenceIndexes = result.Timeline
            .Select((value, index) => (value, index))
            .Where(pair =>
                pair.value.Kind == NodalOsCoreEventKind.EvidenceAttached &&
                pair.value.EvidenceRefs.Any(reference => reference.EvidenceId == evidenceId))
            .Select(pair => pair.index)
            .ToArray();
        var runtimeCompletionIndex = result.Timeline
            .Select((value, index) => (value, index))
            .Where(pair =>
                pair.value.Kind == NodalOsCoreEventKind.ExecutionCompleted &&
                pair.value.EventId.StartsWith("mission-event-", StringComparison.Ordinal))
            .Select(pair => pair.index)
            .Single();

        Assert.IsTrue(evidenceIndexes.Length >= 2);
        Assert.IsTrue(evidenceIndexes.All(index => index < runtimeCompletionIndex));
        Assert.IsTrue(result.Timeline.Any(value => value.Kind == NodalOsCoreEventKind.PolicyGateEvaluated));
        Assert.IsTrue(result.Timeline.Any(value => value.Kind == NodalOsCoreEventKind.DryRunPlanCreated));
        Assert.IsTrue(result.Timeline.Any(value => value.Kind == NodalOsCoreEventKind.ExecutionCompleted));
        Assert.AreEqual(
            result.Timeline.Count,
            result.Timeline.Select(value => value.EventId).Distinct(StringComparer.Ordinal).Count());
    }

    [TestMethod]
    public async Task SnapshotAndSerializedResultDoNotExposeRawFixturePathOrAuthority()
    {
        var result = await new NodalOsTestOwnedFileUpdateMissionScenario().RunAsync();
        var snapshot = result.ToSnapshot();
        var json = JsonSerializer.Serialize(snapshot);

        Assert.IsFalse(json.Contains(Path.GetTempPath(), StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrWhiteSpace(Environment.UserName) && Environment.UserName.Length > 2)
            Assert.IsFalse(json.Contains(Environment.UserName, StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(json.Contains(".nodal-restore", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(json.Contains(".bak", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(snapshot.UserWorkspaceFilesystemTouched);
        Assert.IsFalse(snapshot.NetworkUsed);
        Assert.IsFalse(snapshot.ProductAuthorityGranted);
        Assert.IsFalse(result.MissionBinding.CanAuthorizeExecution);
        Assert.IsFalse(result.MissionBinding.RuntimeExecutionAllowed);
        Assert.IsFalse(result.Workspace.CanAuthorizeExecution);
        Assert.IsFalse(result.Workspace.RuntimeExecutionAllowed);
    }
}
