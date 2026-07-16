using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.AgentOperations.Core.Workspace;
using OneBrain.Core.Models;

namespace OneBrain.Runtime.Tests;

[TestClass]
[TestCategory("WorkspaceHandoffExecution")]
[TestCategory("MvpVerticalSlice")]
public sealed class NodalOsWorkspaceHandoffExecutionTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    [TestMethod]
    public async Task CreateCandidate_ApprovesOnceExecutesVerifiesRehydratesAndRollsBack()
    {
        RequireWindows();
        using var fixture = WorkspaceFixture.Create();
        var baseline = fixture.FileHashes();
        var services = fixture.CreateServices();
        await PrepareMissionAsync(services);

        var ready = await services.Execution.GetCurrentAsync(TestContext.CancellationTokenSource.Token);
        Assert.AreEqual(NodalOsWorkspaceHandoffExecutionState.ReadyForApproval, ready.State);
        Assert.IsTrue(ready.Accepted);
        Assert.IsFalse(ready.Executed);
        Assert.IsFalse(ready.ProductAuthorityGranted);

        var completed = await services.Execution.ApproveAndExecuteAsync(
            cancellationToken: TestContext.CancellationTokenSource.Token);

        Assert.IsTrue(completed.Accepted, string.Join(" | ", completed.Blockers));
        Assert.AreEqual("GO_WORKSPACE_HANDOFF_EXECUTED_AND_VERIFIED", completed.Decision);
        Assert.AreEqual(NodalOsWorkspaceHandoffExecutionState.Completed, completed.State);
        Assert.IsTrue(completed.Persisted);
        Assert.IsTrue(completed.Executed);
        Assert.IsTrue(completed.Verified);
        Assert.IsTrue(completed.RollbackAvailable);
        Assert.IsTrue(completed.WorkspaceFilesystemMutated);
        Assert.IsFalse(completed.NetworkUsed);
        Assert.IsFalse(completed.ExternalProcessUsed);
        Assert.IsFalse(completed.ProductAuthorityGranted);
        Assert.AreEqual("CreateTextFile", completed.ActionKind);
        Assert.AreEqual(NodalOsWorkspaceMissionDraftService.RelativeTargetPath, completed.RelativeTargetPath);
        Assert.AreEqual(64, completed.ResultSha256?.Length);
        Assert.IsTrue(File.Exists(fixture.TargetPath));
        Assert.AreEqual(completed.ResultSha256, fixture.TargetHash());
        Assert.IsTrue(completed.EvidenceRefs.Any(value => value.Contains("workspace-handoff-create", StringComparison.Ordinal)));
        Assert.IsTrue(completed.Timeline.Any(value => value.Kind == OneBrain.AgentOperations.Contracts.NodalOsCoreEventKind.ApprovalGranted));
        Assert.IsTrue(completed.Timeline.Any(value => value.Kind == OneBrain.AgentOperations.Contracts.NodalOsCoreEventKind.ExecutionCompleted));
        Assert.IsTrue(completed.Timeline.Any(value => value.Kind == OneBrain.AgentOperations.Contracts.NodalOsCoreEventKind.EvidenceAttached));

        var document = await fixture.ReadExecutionDocumentAsync(TestContext.CancellationTokenSource.Token);
        Assert.IsNotNull(document);
        Assert.IsTrue(document.Approval.OneShot);
        Assert.IsFalse(document.Approval.DecisionIsExecutionAuthority);
        Assert.AreEqual(NodalOsWorkspaceHandoffExecutionService.CapabilityId, document.Approval.CapabilityId);
        Assert.AreEqual(document.MissionId, document.Approval.MissionId);
        Assert.AreEqual(document.WorkspaceFingerprint, document.Approval.WorkspaceFingerprint);
        Assert.AreEqual(document.Candidate.ActionId, document.Approval.ActionId);
        Assert.AreEqual(document.Candidate.ProposedSha256, document.Approval.ProposedSha256);
        Assert.IsFalse(document.Approval.ApprovalDecision.RuntimeExecutionAllowed);
        Assert.IsFalse(document.Approval.ApprovalDecision.CanAuthorizeExecution);

        var rehydrated = await fixture.CreateServices().Execution.GetCurrentAsync(TestContext.CancellationTokenSource.Token);
        Assert.IsTrue(rehydrated.Accepted, string.Join(" | ", rehydrated.Blockers));
        Assert.AreEqual("GO_WORKSPACE_HANDOFF_EXECUTION_REHYDRATED", rehydrated.Decision);
        Assert.IsTrue(rehydrated.Rehydrated);
        Assert.IsTrue(rehydrated.Verified);
        Assert.AreEqual(completed.OperationId, rehydrated.OperationId);
        Assert.AreEqual(completed.ResultSha256, rehydrated.ResultSha256);

        var rolledBack = await fixture.CreateServices().Execution.RollbackAsync(TestContext.CancellationTokenSource.Token);

        Assert.IsTrue(rolledBack.Accepted, string.Join(" | ", rolledBack.Blockers));
        Assert.AreEqual("GO_WORKSPACE_HANDOFF_ROLLED_BACK_AND_VERIFIED", rolledBack.Decision);
        Assert.AreEqual(NodalOsWorkspaceHandoffExecutionState.RolledBack, rolledBack.State);
        Assert.IsTrue(rolledBack.RolledBack);
        Assert.IsFalse(rolledBack.RollbackAvailable);
        Assert.IsFalse(File.Exists(fixture.TargetPath));
        AssertWorkspaceUnchanged(baseline, fixture.FileHashes());

        var restored = await fixture.CreateServices().Execution.GetCurrentAsync(TestContext.CancellationTokenSource.Token);
        Assert.IsTrue(restored.Accepted, string.Join(" | ", restored.Blockers));
        Assert.AreEqual("GO_WORKSPACE_HANDOFF_ROLLBACK_REHYDRATED", restored.Decision);
        Assert.IsTrue(restored.Rehydrated);
        Assert.IsTrue(restored.RolledBack);
    }

    [TestMethod]
    public async Task ExactHashUpdate_UsesAppLocalSnapshotAndRestoresOriginalBytes()
    {
        RequireWindows();
        using var fixture = WorkspaceFixture.Create(includeExistingTarget: true);
        var originalBytes = await File.ReadAllBytesAsync(fixture.TargetPath, TestContext.CancellationTokenSource.Token);
        var originalHash = fixture.TargetHash();
        var services = fixture.CreateServices();
        await PrepareMissionAsync(services);
        var draft = await services.Mission.GetCurrentAsync(TestContext.CancellationTokenSource.Token);
        Assert.AreEqual(NodalOsReviewedWorkspaceActionKind.ExactHashUpdate, draft.Candidate?.Kind);
        Assert.AreEqual(originalHash, draft.Candidate?.ExistingSha256);

        var completed = await services.Execution.ApproveAndExecuteAsync(
            cancellationToken: TestContext.CancellationTokenSource.Token);

        Assert.IsTrue(completed.Accepted, string.Join(" | ", completed.Blockers));
        Assert.AreEqual("ExactHashUpdate", completed.ActionKind);
        Assert.AreEqual(originalHash, completed.OriginalSha256);
        Assert.AreEqual(completed.ResultSha256, fixture.TargetHash());
        Assert.AreNotEqual(originalHash, completed.ResultSha256);
        Assert.IsTrue(completed.RollbackAvailable);
        Assert.AreEqual(1, Directory.GetFiles(fixture.RestoreRoot, "*.bak", SearchOption.TopDirectoryOnly).Length);

        var rolledBack = await services.Execution.RollbackAsync(TestContext.CancellationTokenSource.Token);

        Assert.IsTrue(rolledBack.Accepted, string.Join(" | ", rolledBack.Blockers));
        CollectionAssert.AreEqual(originalBytes, await File.ReadAllBytesAsync(fixture.TargetPath, TestContext.CancellationTokenSource.Token));
        Assert.AreEqual(originalHash, fixture.TargetHash());
        Assert.AreEqual(0, Directory.Exists(fixture.RestoreRoot)
            ? Directory.GetFiles(fixture.RestoreRoot, "*.bak", SearchOption.TopDirectoryOnly).Length
            : 0);
    }

    [TestMethod]
    public async Task StalePrecondition_BlocksBeforeApprovalAndDoesNotWriteExecutionMetadata()
    {
        RequireWindows();
        using var fixture = WorkspaceFixture.Create();
        var services = fixture.CreateServices();
        await PrepareMissionAsync(services);
        await File.WriteAllTextAsync(
            fixture.TargetPath,
            "external file created after review",
            TestContext.CancellationTokenSource.Token);
        var externalHash = fixture.TargetHash();

        var result = await services.Execution.ApproveAndExecuteAsync(
            cancellationToken: TestContext.CancellationTokenSource.Token);

        Assert.IsFalse(result.Accepted);
        Assert.AreEqual(NodalOsWorkspaceHandoffExecutionState.CandidateStale, result.State);
        Assert.AreEqual("BLOCKED_WORKSPACE_HANDOFF_EXECUTION_PRECONDITION_CHANGED", result.Decision);
        Assert.IsFalse(result.Executed);
        Assert.IsFalse(result.WorkspaceFilesystemMutated);
        Assert.IsFalse(result.ProductAuthorityGranted);
        Assert.IsFalse(File.Exists(fixture.ExecutionMetadataPath));
        Assert.AreEqual(externalHash, fixture.TargetHash());
    }

    [TestMethod]
    public async Task ResultChangedAfterExecution_DisablesGuardedRollback()
    {
        RequireWindows();
        using var fixture = WorkspaceFixture.Create();
        var services = fixture.CreateServices();
        await PrepareMissionAsync(services);
        var completed = await services.Execution.ApproveAndExecuteAsync(
            cancellationToken: TestContext.CancellationTokenSource.Token);
        Assert.IsTrue(completed.Accepted);
        await File.AppendAllTextAsync(
            fixture.TargetPath,
            Environment.NewLine + "external change after verified execution",
            TestContext.CancellationTokenSource.Token);
        var changedHash = fixture.TargetHash();

        var changed = await fixture.CreateServices().Execution.GetCurrentAsync(TestContext.CancellationTokenSource.Token);

        Assert.IsFalse(changed.Accepted);
        Assert.AreEqual(NodalOsWorkspaceHandoffExecutionState.ResultChanged, changed.State);
        Assert.IsFalse(changed.RollbackAvailable);
        Assert.IsFalse(changed.ProductAuthorityGranted);

        var rollback = await fixture.CreateServices().Execution.RollbackAsync(TestContext.CancellationTokenSource.Token);
        Assert.IsFalse(rollback.Accepted);
        Assert.AreEqual("BLOCKED_WORKSPACE_HANDOFF_ROLLBACK_FAILED_CLOSED", rollback.Decision);
        Assert.AreEqual(changedHash, fixture.TargetHash());
        Assert.IsTrue(File.Exists(fixture.TargetPath));
    }

    [TestMethod]
    public async Task PersistedExecutionMetadata_ExcludesAbsoluteRootAndSensitiveWorkspaceContent()
    {
        RequireWindows();
        using var fixture = WorkspaceFixture.Create();
        var services = fixture.CreateServices();
        await PrepareMissionAsync(services);
        var completed = await services.Execution.ApproveAndExecuteAsync(
            cancellationToken: TestContext.CancellationTokenSource.Token);
        Assert.IsTrue(completed.Accepted, string.Join(" | ", completed.Blockers));

        var metadata = await File.ReadAllTextAsync(
            fixture.ExecutionMetadataPath,
            TestContext.CancellationTokenSource.Token);
        var serializedSnapshot = JsonSerializer.Serialize(completed, JsonOptions);

        Assert.IsFalse(metadata.Contains(fixture.WorkspaceRoot, StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(metadata.Contains(fixture.SensitiveFixtureValue, StringComparison.Ordinal));
        Assert.IsFalse(serializedSnapshot.Contains(fixture.WorkspaceRoot, StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(serializedSnapshot.Contains(fixture.SensitiveFixtureValue, StringComparison.Ordinal));
        StringAssert.Contains(metadata, "filesystem.write.safe");
        StringAssert.Contains(metadata, "NODAL_HANDOFF.md");
        Assert.IsFalse(completed.NetworkUsed);
        Assert.IsFalse(completed.ExternalProcessUsed);
        Assert.IsTrue(completed.SecretsExcluded);
        Assert.IsFalse(completed.ProductAuthorityGranted);

        var clearBlocked = await services.Execution.ClearAsync(TestContext.CancellationTokenSource.Token);
        Assert.IsFalse(clearBlocked.Accepted);
        Assert.AreEqual("BLOCKED_WORKSPACE_HANDOFF_EXECUTION_CLEAR_REQUIRES_ROLLBACK", clearBlocked.Decision);
        Assert.IsTrue(File.Exists(fixture.ExecutionMetadataPath));

        var rollback = await services.Execution.RollbackAsync(TestContext.CancellationTokenSource.Token);
        Assert.IsTrue(rollback.Accepted);
        var cleared = await services.Execution.ClearAsync(TestContext.CancellationTokenSource.Token);
        Assert.IsTrue(cleared.Accepted);
        Assert.IsFalse(File.Exists(fixture.ExecutionMetadataPath));
        Assert.IsTrue(File.Exists(fixture.SelectionMetadataPath));
        Assert.IsTrue(File.Exists(fixture.MissionMetadataPath));
    }

    public TestContext TestContext { get; set; } = null!;

    private async Task PrepareMissionAsync(ServiceSet services)
    {
        var selected = await services.Selection.SelectAsync(
            services.Fixture.WorkspaceRoot,
            "Controlled Handoff Workspace",
            TestContext.CancellationTokenSource.Token);
        Assert.IsTrue(selected.Accepted, string.Join(" | ", selected.ReviewBlockers));
        var draft = await services.Mission.CreateAsync(
            "Prepare a deterministic project handoff, execute only the reviewed relative target, and retain exact verification evidence.",
            TestContext.CancellationTokenSource.Token);
        Assert.IsTrue(draft.Accepted, string.Join(" | ", draft.ReviewBlockers));
        Assert.IsNotNull(draft.Candidate);
        Assert.IsFalse(draft.Candidate.ExecutionEnabled);
    }

    private static void RequireWindows()
    {
        if (!OperatingSystem.IsWindows())
            Assert.Inconclusive("Protected workspace execution uses the Windows DPAPI root reference.");
    }

    private static void AssertWorkspaceUnchanged(
        IReadOnlyDictionary<string, string> before,
        IReadOnlyDictionary<string, string> after)
    {
        CollectionAssert.AreEquivalent(before.Keys.ToArray(), after.Keys.ToArray());
        foreach (var pair in before)
            Assert.AreEqual(pair.Value, after[pair.Key], pair.Key);
    }

    private sealed record ServiceSet(
        WorkspaceFixture Fixture,
        NodalOsWorkspaceSelectionService Selection,
        NodalOsWorkspaceMissionDraftService Mission,
        NodalOsWorkspaceHandoffExecutionService Execution);

    private sealed class WorkspaceFixture : IDisposable
    {
        private WorkspaceFixture(string root, bool includeExistingTarget)
        {
            Root = root;
            WorkspaceRoot = Path.Combine(root, "workspace");
            SelectionMetadataPath = Path.Combine(root, "config", "selection.v1.json");
            MissionMetadataPath = Path.Combine(root, "config", "mission.v1.json");
            ExecutionMetadataPath = Path.Combine(root, "config", "execution.v1.json");
            RestoreRoot = Path.Combine(root, "restore");
            SecretRoot = Path.Combine(root, "secrets");
            TargetPath = Path.Combine(WorkspaceRoot, NodalOsWorkspaceMissionDraftService.RelativeTargetPath);
            SensitiveFixtureValue = "controlled-handoff-sensitive-fixture-value";

            Directory.CreateDirectory(Path.Combine(WorkspaceRoot, "src"));
            File.WriteAllText(Path.Combine(WorkspaceRoot, "README.md"), "# Controlled handoff workspace fixture");
            var sensitiveName = string.Concat("api", "_key");
            File.WriteAllText(
                Path.Combine(WorkspaceRoot, "src", "Program.cs"),
                $"var {sensitiveName} = \"{SensitiveFixtureValue}\";{Environment.NewLine}Console.WriteLine(\"fixture\");");
            if (includeExistingTarget)
                File.WriteAllText(TargetPath, "# Existing handoff" + Environment.NewLine + "Original exact bytes");
        }

        public string Root { get; }
        public string WorkspaceRoot { get; }
        public string SelectionMetadataPath { get; }
        public string MissionMetadataPath { get; }
        public string ExecutionMetadataPath { get; }
        public string RestoreRoot { get; }
        public string SecretRoot { get; }
        public string TargetPath { get; }
        public string SensitiveFixtureValue { get; }

        public static WorkspaceFixture Create(bool includeExistingTarget = false) => new(
            Path.Combine(Path.GetTempPath(), "nodal-os-workspace-handoff-execution-tests", Guid.NewGuid().ToString("N")),
            includeExistingTarget);

        public ServiceSet CreateServices()
        {
            var store = new WindowsDpapiSecretReferenceStore(SecretRoot);
            var selection = new NodalOsWorkspaceSelectionService(SelectionMetadataPath, store);
            var mission = new NodalOsWorkspaceMissionDraftService(MissionMetadataPath, selection, store);
            var execution = new NodalOsWorkspaceHandoffExecutionService(
                ExecutionMetadataPath,
                RestoreRoot,
                selection,
                mission,
                store);
            return new ServiceSet(this, selection, mission, execution);
        }

        public async Task<NodalOsPersistedWorkspaceHandoffExecution?> ReadExecutionDocumentAsync(
            CancellationToken cancellationToken)
        {
            var json = await File.ReadAllTextAsync(ExecutionMetadataPath, cancellationToken);
            return JsonSerializer.Deserialize<NodalOsPersistedWorkspaceHandoffExecution>(json, JsonOptions);
        }

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