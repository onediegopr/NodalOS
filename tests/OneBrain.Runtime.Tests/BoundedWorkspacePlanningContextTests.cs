using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.AgentOperations.Core.Workspace;

namespace OneBrain.Runtime.Tests;

[TestClass]
[TestCategory("MvpVerticalSlice")]
[TestCategory("WorkspaceUnderstanding")]
public sealed class BoundedWorkspacePlanningContextTests
{
    [TestMethod]
    public async Task VerifiedScanProjectsIntoNonExecutableReviewedPlanWithoutRawRootOrSecrets()
    {
        var root = CreateRoot();
        try
        {
            Directory.CreateDirectory(Path.Combine(root, "src"));
            await File.WriteAllTextAsync(Path.Combine(root, "README.md"), "# Fixture workspace\nReviewable context.");
            await File.WriteAllTextAsync(
                Path.Combine(root, "src", "Program.cs"),
                "var password = \"secret-password-value\";\nConsole.WriteLine(\"fixture\");");
            var scan = await new BoundedWorkspaceUnderstandingService().ScanAsync(
                new BoundedWorkspaceScanRequest(root));

            var result = new BoundedWorkspacePlanningContextService().Build(
                scan,
                workspaceId: "workspace-fixture",
                missionId: "mission-fixture");

            Assert.IsTrue(result.Accepted);
            Assert.AreEqual("GO_BOUNDED_WORKSPACE_CONTEXT_READY_FOR_REVIEWED_PLAN", result.Decision);
            Assert.IsTrue(result.RequiresHumanReview);
            Assert.IsFalse(result.CanExecute);
            Assert.IsFalse(result.CallsModelProvider);
            Assert.IsFalse(result.CreatesPrompt);
            Assert.IsFalse(result.FilesystemMutationAllowed);
            Assert.IsFalse(result.NetworkUsed);
            Assert.IsFalse(result.ProductAuthorityGranted);

            var context = result.Context ?? throw new AssertFailedException("Context was not created.");
            var graph = result.TaskGraph ?? throw new AssertFailedException("TaskGraph was not created.");
            var plan = result.MissionPlan ?? throw new AssertFailedException("MissionPlan was not created.");
            Assert.AreEqual(scan.EvidenceDigest, context.EvidenceDigest);
            Assert.AreEqual(scan.RootFingerprint, context.RootFingerprint);
            Assert.AreEqual(scan.FilesRead, context.FilesRead);
            Assert.IsTrue(context.Redacted);
            Assert.IsTrue(context.ReadOnly);
            Assert.IsFalse(context.Authoritative);
            Assert.AreEqual(1, graph.Tasks.Count);
            Assert.IsTrue(graph.DraftOnly);
            Assert.IsFalse(graph.Executable);
            Assert.IsFalse(graph.CallsLlmProvider);
            Assert.IsFalse(graph.CallsRuntime);
            Assert.IsFalse(graph.TouchesFilesystem);
            Assert.IsFalse(graph.CanAuthorizeExecution);
            Assert.AreEqual(1, plan.Steps.Count);
            Assert.IsFalse(plan.Steps.Single().ApprovalRequired);
            CollectionAssert.Contains(plan.Steps.Single().AllowedCapabilities.ToArray(), "filesystem.read");

            var serialized = JsonSerializer.Serialize(result);
            Assert.IsFalse(serialized.Contains(root, StringComparison.OrdinalIgnoreCase));
            Assert.IsFalse(serialized.Contains("secret-password-value", StringComparison.OrdinalIgnoreCase));
            Assert.IsFalse(context.SafeFileRefs.Any(Path.IsPathRooted));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [TestMethod]
    public async Task TruncatedScanCreatesExplicitReviewBlockerWithoutExpandingBudget()
    {
        var root = CreateRoot();
        try
        {
            await File.WriteAllTextAsync(Path.Combine(root, "a.md"), "a");
            await File.WriteAllTextAsync(Path.Combine(root, "b.md"), "b");
            var scan = await new BoundedWorkspaceUnderstandingService().ScanAsync(
                new BoundedWorkspaceScanRequest(
                    root,
                    Limits: new BoundedWorkspaceScanLimits(
                        MaximumFiles: 1,
                        MaximumTotalBytes: 1024,
                        MaximumFileBytes: 1024,
                        MaximumDepth: 2,
                        MaximumPreviewCharacters: 100)));

            var result = new BoundedWorkspacePlanningContextService().Build(
                scan,
                "workspace-truncated",
                "mission-truncated");

            Assert.IsTrue(result.Accepted);
            var context = result.Context ?? throw new AssertFailedException("Context was not created.");
            var graph = result.TaskGraph ?? throw new AssertFailedException("TaskGraph was not created.");
            Assert.IsTrue(context.ScanTruncated);
            Assert.AreEqual(1, context.FilesRead);
            Assert.AreEqual(1, result.Blockers.Count);
            Assert.IsTrue(result.Blockers.Single().Contains("operator-authorized", StringComparison.OrdinalIgnoreCase));
            Assert.IsFalse(result.CanExecute);
            Assert.IsFalse(graph.Executable);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [TestMethod]
    public async Task InvalidRootCannotCreatePlanningContext()
    {
        var missing = Path.Combine(Path.GetTempPath(), "nodal-missing-plan-context-" + Guid.NewGuid().ToString("N"));
        var scan = await new BoundedWorkspaceUnderstandingService().ScanAsync(
            new BoundedWorkspaceScanRequest(missing));

        var result = new BoundedWorkspacePlanningContextService().Build(
            scan,
            "workspace-missing",
            "mission-missing");

        Assert.IsFalse(result.Accepted);
        Assert.AreEqual("BLOCKED_BOUNDED_WORKSPACE_CONTEXT_FAIL_CLOSED", result.Decision);
        Assert.IsNull(result.Context);
        Assert.IsNull(result.TaskGraph);
        Assert.IsNull(result.MissionPlan);
        Assert.IsTrue(result.Blockers.Count > 0);
        Assert.IsFalse(result.CanExecute);
        Assert.IsFalse(result.ProductAuthorityGranted);
    }

    [TestMethod]
    public async Task MutatingOrNetworkTaintedEvidenceIsRejectedFailClosed()
    {
        var root = CreateRoot();
        try
        {
            await File.WriteAllTextAsync(Path.Combine(root, "README.md"), "fixture");
            var scan = await new BoundedWorkspaceUnderstandingService().ScanAsync(
                new BoundedWorkspaceScanRequest(root));
            var tainted = scan with
            {
                FilesystemMutationAllowed = true,
                NetworkUsed = true,
                ProductAuthorityGranted = true
            };

            var result = new BoundedWorkspacePlanningContextService().Build(
                tainted,
                "workspace-tainted",
                "mission-tainted");

            Assert.IsFalse(result.Accepted);
            Assert.IsNull(result.Context);
            Assert.IsTrue(result.Blockers.Any(value => value.Contains("mutation", StringComparison.OrdinalIgnoreCase)));
            Assert.IsTrue(result.Blockers.Any(value => value.Contains("network", StringComparison.OrdinalIgnoreCase)));
            Assert.IsTrue(result.Blockers.Any(value => value.Contains("authority", StringComparison.OrdinalIgnoreCase)));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private static string CreateRoot()
    {
        var root = Path.Combine(Path.GetTempPath(), "nodal-plan-context-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        return root;
    }
}
