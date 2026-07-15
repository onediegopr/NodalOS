using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.AgentOperations.Contracts;
using OneBrain.AgentOperations.Core.Runtime;

namespace OneBrain.Runtime.Tests;

[TestClass]
[TestCategory("MvpVerticalSlice")]
public sealed class NodalOsTestOwnedFileCreateMissionScenarioTests
{
    [TestMethod]
    public async Task MissionApprovesOnceCreatesVerifiesCompletesAndCleansTheFixture()
    {
        var result = await new NodalOsTestOwnedFileCreateMissionScenario().RunAsync();
        var snapshot = result.ToSnapshot();

        Assert.AreEqual(MissionStatus.Completed, result.Mission.Status);
        Assert.AreEqual(NodalOsExecutionRegistryState.Completed, result.RegistryEntry.State);
        Assert.AreEqual(NodalOsApprovalDecisionKind.Approve, result.MissionAuthorizationDecision.DecisionKind);
        Assert.AreEqual(NodalOsTestOwnedFileCreateDecision.CreatedAndVerified, result.FileCreateAction.Decision);
        Assert.IsTrue(result.FileCreateAction.Verified);
        Assert.IsTrue(result.Mission.EvidenceRefs.Contains(
            result.FileCreateAction.Evidence!.EvidenceId,
            StringComparer.Ordinal));
        Assert.IsTrue(result.FixtureCleanup.Success);
        Assert.IsTrue(result.FixtureCleanup.RootRemoved);
        Assert.IsTrue(snapshot.TestOwnedFilesystemTouched);
        Assert.IsTrue(snapshot.TestOwnedFixtureCleaned);
        Assert.IsFalse(snapshot.UserWorkspaceFilesystemTouched);
        Assert.IsFalse(snapshot.NetworkUsed);
        Assert.IsFalse(snapshot.ProductAuthorityGranted);
        Assert.IsTrue(result.MissionAuthorizationReused);
        Assert.IsFalse(result.AdditionalStepApprovalRequested);
        Assert.AreEqual(1, result.Timeline.Count(item => item.Kind == NodalOsCoreEventKind.ApprovalRequired));
        Assert.AreEqual(1, result.Timeline.Count(item => item.Kind == NodalOsCoreEventKind.ApprovalGranted));
    }

    [TestMethod]
    public async Task FileEvidenceExistsBeforeMissionCompletion()
    {
        var result = await new NodalOsTestOwnedFileCreateMissionScenario().RunAsync();

        var approvalIndex = IndexOf(result, NodalOsCoreEventKind.ApprovalRequired);
        var createIndex = IndexOf(result, "Create-only test-owned file completed and verified");
        var evidenceIndex = IndexOf(result, "Verified file SHA-256 attached before mission completion");
        var missionCompletionIndex = IndexOf(result, "Mission completed after every required step was verified");

        Assert.IsTrue(approvalIndex < createIndex);
        Assert.IsTrue(createIndex < evidenceIndex);
        Assert.IsTrue(evidenceIndex < missionCompletionIndex);
    }

    [TestMethod]
    public async Task SnapshotAndEvidenceExposeNoRawTemporaryOrUserPaths()
    {
        var result = await new NodalOsTestOwnedFileCreateMissionScenario().RunAsync();
        var snapshot = result.ToSnapshot();
        var serialized = string.Join(
            "|",
            snapshot.WorkspaceId,
            snapshot.MissionId,
            snapshot.FileCreateState,
            snapshot.RelativePath,
            snapshot.ContentSha256,
            result.FileCreateAction.RootFingerprint,
            result.FileCreateAction.TargetFingerprint,
            result.FileCreateAction.SafeMessage,
            result.FileCreateAction.Evidence?.EvidenceId,
            result.FileCreateAction.Evidence?.LedgerRef,
            result.FileCreateAction.Evidence?.Provenance);

        Assert.IsFalse(serialized.Contains(Path.GetTempPath(), StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(serialized.Contains(Environment.UserName, StringComparison.OrdinalIgnoreCase));
        Assert.AreEqual("output/verified-handoff.md", snapshot.RelativePath);
        Assert.AreEqual(64, snapshot.ContentSha256?.Length);
    }

    private static int IndexOf(
        NodalOsTestOwnedFileCreateMissionResult result,
        NodalOsCoreEventKind kind) =>
        result.Timeline
            .Select((item, index) => new { item.Kind, Index = index })
            .Single(item => item.Kind == kind)
            .Index;

    private static int IndexOf(
        NodalOsTestOwnedFileCreateMissionResult result,
        string summary) =>
        result.Timeline
            .Select((item, index) => new { item.SummaryRedacted, Index = index })
            .Single(item => item.SummaryRedacted.Contains(summary, StringComparison.Ordinal))
            .Index;
}
