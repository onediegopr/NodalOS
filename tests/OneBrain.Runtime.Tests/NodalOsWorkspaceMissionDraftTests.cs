using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.AgentOperations.Core.Workspace;
using OneBrain.Core.Models;

namespace OneBrain.Runtime.Tests;

[TestClass]
[TestCategory("WorkspaceMissionDraft")]
[TestCategory("MvpVerticalSlice")]
public sealed class NodalOsWorkspaceMissionDraftTests
{
    [TestMethod]
    public async Task CreateAsync_PersistsReviewedCreateCandidateAndRehydratesWithoutWorkspaceMutation()
    {
        RequireWindows();
        using var fixture = WorkspaceFixture.Create();
        var before = fixture.FileHashes();
        var selection = fixture.CreateSelectionService();
        var selected = await selection.SelectAsync(
            fixture.WorkspaceRoot,
            "Mission Draft Workspace",
            TestContext.CancellationTokenSource.Token);
        Assert.IsTrue(selected.Accepted, string.Join(" | ", selected.ReviewBlockers));

        var service = fixture.CreateMissionService(selection);
        var draft = await service.CreateAsync(
            "Prepare a deterministic handoff for the current product milestone and its next verified steps.",
            TestContext.CancellationTokenSource.Token);

        Assert.IsTrue(draft.Accepted, string.Join(" | ", draft.ReviewBlockers));
        Assert.AreEqual(NodalOsWorkspaceMissionDraftState.ReadyForReview, draft.State);
        Assert.AreEqual("GO_REAL_WORKSPACE_MISSION_DRAFT_READY", draft.Decision);
        Assert.IsTrue(draft.Persisted);
        Assert.IsFalse(draft.Rehydrated);
        Assert.IsTrue(draft.AppConfigurationMutated);
        Assert.IsTrue(draft.RealFilesystemRead);
        Assert.IsFalse(draft.WorkspaceFilesystemMutated);
        Assert.IsFalse(draft.NetworkUsed);
        Assert.IsTrue(draft.SecretsExcluded);
        Assert.IsFalse(draft.ProductAuthorityGranted);
        Assert.IsNotNull(draft.Binding);
        Assert.IsNotNull(draft.Plan);
        Assert.IsNotNull(draft.Candidate);
        Assert.AreEqual(6, draft.Plan.Steps.Count);
        Assert.AreEqual(MissionStepStatus.Verified, draft.Plan.Steps[0].Status);
        Assert.AreEqual(MissionStepStatus.InProgress, draft.Plan.Steps[1].Status);
        Assert.AreEqual(NodalOsReviewedWorkspaceActionKind.CreateTextFile, draft.Candidate.Kind);
        Assert.AreEqual(NodalOsReviewedWorkspaceActionState.ReadyForReview, draft.Candidate.State);
        Assert.AreEqual(NodalOsWorkspaceMissionDraftService.RelativeTargetPath, draft.Candidate.RelativeTargetPath);
        Assert.IsFalse(draft.Candidate.TargetExists);
        Assert.IsTrue(draft.Candidate.ApprovalRequired);
        Assert.IsTrue(draft.Candidate.Reversible);
        Assert.IsFalse(draft.Candidate.ExecutionEnabled);
        Assert.AreEqual(64, draft.Candidate.ProposedSha256.Length);
        Assert.IsFalse(draft.Binding.CanAuthorizeExecution);
        Assert.IsFalse(draft.Binding.RuntimeExecutionAllowed);
        Assert.IsFalse(draft.Binding.TouchesFilesystem);
        Assert.IsFalse(draft.Binding.MutatesExecutionRegistryRuntime);
        Assert.IsFalse(File.Exists(fixture.TargetPath));
        Assert.IsTrue(File.Exists(fixture.MissionMetadataPath));

        var metadata = await File.ReadAllTextAsync(
            fixture.MissionMetadataPath,
            TestContext.CancellationTokenSource.Token);
        Assert.IsFalse(metadata.Contains(fixture.WorkspaceRoot, StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(metadata.Contains(fixture.SensitiveFixtureValue, StringComparison.Ordinal));
        StringAssert.Contains(metadata, "NODAL_HANDOFF.md");
        StringAssert.Contains(metadata, "ReadyForReview");

        var rehydrated = await fixture.CreateMissionService(fixture.CreateSelectionService())
            .GetCurrentAsync(TestContext.CancellationTokenSource.Token);

        Assert.IsTrue(rehydrated.Accepted, string.Join(" | ", rehydrated.ReviewBlockers));
        Assert.AreEqual("GO_REAL_WORKSPACE_MISSION_DRAFT_REHYDRATED", rehydrated.Decision);
        Assert.IsTrue(rehydrated.Persisted);
        Assert.IsTrue(rehydrated.Rehydrated);
        Assert.IsFalse(rehydrated.AppConfigurationMutated);
        Assert.AreEqual(draft.MissionId, rehydrated.MissionId);
        Assert.AreEqual(draft.WorkspaceFingerprint, rehydrated.WorkspaceFingerprint);
        Assert.AreEqual(draft.Candidate.ProposedSha256, rehydrated.Candidate?.ProposedSha256);
        Assert.IsFalse(rehydrated.Candidate?.ExecutionEnabled ?? true);
        Assert.IsFalse(File.Exists(fixture.TargetPath));
        AssertWorkspaceUnchanged(before, fixture.FileHashes());

        var serialized = JsonSerializer.Serialize(rehydrated);
        Assert.IsFalse(serialized.Contains(fixture.WorkspaceRoot, StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(serialized.Contains(fixture.SensitiveFixtureValue, StringComparison.Ordinal));
    }

    [TestMethod]
    public async Task ExistingTarget_ProducesExactHashCandidateAndFailsClosedWhenPreconditionChanges()
    {
        RequireWindows();
        using var fixture = WorkspaceFixture.Create(includeExistingTarget: true);
        var selection = fixture.CreateSelectionService();
        var selected = await selection.SelectAsync(
            fixture.WorkspaceRoot,
            cancellationToken: TestContext.CancellationTokenSource.Token);
        Assert.IsTrue(selected.Accepted, string.Join(" | ", selected.ReviewBlockers));
        var service = fixture.CreateMissionService(selection);

        var draft = await service.CreateAsync(
            "Refresh the project handoff while preserving a reversible exact-hash update boundary.",
            TestContext.CancellationTokenSource.Token);

        Assert.IsTrue(draft.Accepted, string.Join(" | ", draft.ReviewBlockers));
        Assert.IsNotNull(draft.Candidate);
        Assert.AreEqual(NodalOsReviewedWorkspaceActionKind.ExactHashUpdate, draft.Candidate.Kind);
        Assert.IsTrue(draft.Candidate.TargetExists);
        Assert.AreEqual(fixture.TargetHash(), draft.Candidate.ExistingSha256);
        Assert.IsFalse(draft.Candidate.ExecutionEnabled);

        await File.AppendAllTextAsync(
            fixture.TargetPath,
            Environment.NewLine + "external user change",
            TestContext.CancellationTokenSource.Token);
        var changedHash = fixture.TargetHash();
        Assert.AreNotEqual(draft.Candidate.ExistingSha256, changedHash);

        var stale = await service.GetCurrentAsync(TestContext.CancellationTokenSource.Token);

        Assert.IsFalse(stale.Accepted);
        Assert.AreEqual(NodalOsWorkspaceMissionDraftState.CandidateStale, stale.State);
        Assert.AreEqual("BLOCKED_REAL_WORKSPACE_MISSION_ACTION_PRECONDITION_CHANGED", stale.Decision);
        Assert.IsTrue(stale.Persisted);
        Assert.IsNotNull(stale.Candidate);
        Assert.AreEqual(NodalOsReviewedWorkspaceActionState.StalePrecondition, stale.Candidate.State);
        Assert.IsFalse(stale.Candidate.ExecutionEnabled);
        Assert.IsFalse(stale.WorkspaceFilesystemMutated);
        Assert.IsFalse(stale.ProductAuthorityGranted);
        Assert.IsTrue(stale.ReviewBlockers.Any(value => value.Contains("changed after review", StringComparison.OrdinalIgnoreCase)));
    }

    [TestMethod]
    public async Task CreateAsync_RejectsPathAndCredentialLikeGoalsWithoutPersistingMission()
    {
        RequireWindows();
        using var fixture = WorkspaceFixture.Create();
        var selection = fixture.CreateSelectionService();
        var selected = await selection.SelectAsync(
            fixture.WorkspaceRoot,
            cancellationToken: TestContext.CancellationTokenSource.Token);
        Assert.IsTrue(selected.Accepted);
        var service = fixture.CreateMissionService(selection);

        var pathGoal = @"Prepare a handoff for C:\Users\Example\private-workspace without exposing it.";
        var pathRejected = await service.CreateAsync(
            pathGoal,
            TestContext.CancellationTokenSource.Token);

        Assert.IsFalse(pathRejected.Accepted);
        Assert.AreEqual(NodalOsWorkspaceMissionDraftState.GoalRejected, pathRejected.State);
        Assert.IsFalse(File.Exists(fixture.MissionMetadataPath));

        var credentialMarker = string.Concat("api", "_key", "=", fixture.SensitiveFixtureValue);
        var credentialRejected = await service.CreateAsync(
            "Prepare the handoff using " + credentialMarker + " only as unsafe fixture input.",
            TestContext.CancellationTokenSource.Token);

        Assert.IsFalse(credentialRejected.Accepted);
        Assert.AreEqual(NodalOsWorkspaceMissionDraftState.GoalRejected, credentialRejected.State);
        Assert.IsFalse(File.Exists(fixture.MissionMetadataPath));
        Assert.IsFalse(credentialRejected.ReviewBlockers.Any(value => value.Contains(fixture.SensitiveFixtureValue, StringComparison.Ordinal)));
    }

    [TestMethod]
    public async Task ClearAsync_RemovesOnlyMissionConfigurationAndLeavesWorkspaceSelectionIntact()
    {
        RequireWindows();
        using var fixture = WorkspaceFixture.Create();
        var before = fixture.FileHashes();
        var selection = fixture.CreateSelectionService();
        var selected = await selection.SelectAsync(
            fixture.WorkspaceRoot,
            cancellationToken: TestContext.CancellationTokenSource.Token);
        Assert.IsTrue(selected.Accepted);
        var service = fixture.CreateMissionService(selection);
        var draft = await service.CreateAsync(
            "Prepare a clear project handoff and preserve the selected workspace for the next mission.",
            TestContext.CancellationTokenSource.Token);
        Assert.IsTrue(draft.Accepted);

        var cleared = await service.ClearAsync(TestContext.CancellationTokenSource.Token);

        Assert.AreEqual(NodalOsWorkspaceMissionDraftState.NotConfigured, cleared.State);
        Assert.IsTrue(cleared.AppConfigurationMutated);
        Assert.IsFalse(File.Exists(fixture.MissionMetadataPath));
        Assert.IsTrue(File.Exists(fixture.SelectionMetadataPath));
        var currentWorkspace = await fixture.CreateSelectionService()
            .GetCurrentAsync(TestContext.CancellationTokenSource.Token);
        Assert.IsTrue(currentWorkspace.Accepted, string.Join(" | ", currentWorkspace.ReviewBlockers));
        AssertWorkspaceUnchanged(before, fixture.FileHashes());
    }

    public TestContext TestContext { get; set; } = null!;

    private static void RequireWindows()
    {
        if (!OperatingSystem.IsWindows())
            Assert.Inconclusive("Protected real workspace mission drafts depend on the Windows DPAPI workspace root reference.");
    }

    private static void AssertWorkspaceUnchanged(
        IReadOnlyDictionary<string, string> before,
        IReadOnlyDictionary<string, string> after)
    {
        CollectionAssert.AreEquivalent(before.Keys.ToArray(), after.Keys.ToArray());
        foreach (var pair in before)
            Assert.AreEqual(pair.Value, after[pair.Key], pair.Key);
    }

    private sealed class WorkspaceFixture : IDisposable
    {
        private WorkspaceFixture(string root, bool includeExistingTarget)
        {
            Root = root;
            WorkspaceRoot = Path.Combine(root, "workspace");
            SelectionMetadataPath = Path.Combine(root, "config", "selection.v1.json");
            MissionMetadataPath = Path.Combine(root, "config", "mission.v1.json");
            SecretRoot = Path.Combine(root, "secrets");
            TargetPath = Path.Combine(WorkspaceRoot, NodalOsWorkspaceMissionDraftService.RelativeTargetPath);
            SensitiveFixtureValue = "mission-draft-sensitive-fixture-value";

            Directory.CreateDirectory(Path.Combine(WorkspaceRoot, "src"));
            File.WriteAllText(Path.Combine(WorkspaceRoot, "README.md"), "# Mission draft workspace fixture");
            File.WriteAllText(Path.Combine(WorkspaceRoot, "src", "Program.cs"), "Console.WriteLine(\"fixture\");");
            if (includeExistingTarget)
                File.WriteAllText(TargetPath, "# Existing handoff" + Environment.NewLine + "Initial content");
        }

        public string Root { get; }
        public string WorkspaceRoot { get; }
        public string SelectionMetadataPath { get; }
        public string MissionMetadataPath { get; }
        public string SecretRoot { get; }
        public string TargetPath { get; }
        public string SensitiveFixtureValue { get; }

        public static WorkspaceFixture Create(bool includeExistingTarget = false) => new(
            Path.Combine(Path.GetTempPath(), "nodal-os-workspace-mission-draft-tests", Guid.NewGuid().ToString("N")),
            includeExistingTarget);

        public NodalOsWorkspaceSelectionService CreateSelectionService() => new(
            SelectionMetadataPath,
            new WindowsDpapiSecretReferenceStore(SecretRoot));

        public NodalOsWorkspaceMissionDraftService CreateMissionService(
            NodalOsWorkspaceSelectionService selection) => new(
            MissionMetadataPath,
            selection,
            new WindowsDpapiSecretReferenceStore(SecretRoot));

        public Dictionary<string, string> FileHashes() =>
            Directory.GetFiles(WorkspaceRoot, "*", SearchOption.AllDirectories)
                .OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(
                    value => Path.GetRelativePath(WorkspaceRoot, value),
                    value => Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(value))).ToLowerInvariant(),
                    StringComparer.OrdinalIgnoreCase);

        public string TargetHash() =>
            Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(TargetPath))).ToLowerInvariant();

        public void Dispose()
        {
            try
            {
                if (!Directory.Exists(Root))
                    return;
                foreach (var path in Directory.GetFiles(Root, "*", SearchOption.AllDirectories))
                    File.SetAttributes(path, FileAttributes.Normal);
                Directory.Delete(Root, recursive: true);
            }
            catch
            {
            }
        }
    }
}