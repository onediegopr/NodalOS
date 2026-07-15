using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.AgentOperations.Core.Runtime;
using OneBrain.AgentOperations.Core.Workspace;

namespace OneBrain.Runtime.Tests;

[TestClass]
[TestCategory("MvpVerticalSlice")]
[TestCategory("WorkspaceUnderstanding")]
public sealed class BoundedWorkspaceMissionIntegrationTests
{
    [TestMethod]
    public async Task BoundedWorkspaceCompletesMissionOnlyAfterVerifiedEvidence()
    {
        var root = CreateRoot();
        try
        {
            Directory.CreateDirectory(Path.Combine(root, "src"));
            await File.WriteAllTextAsync(Path.Combine(root, "README.md"), "# Fixture workspace");
            await File.WriteAllTextAsync(Path.Combine(root, "src", "Program.cs"), "Console.WriteLine(\"fixture\");");

            var result = await new NodalOsBoundedWorkspaceMissionScenario().RunAsync(root);

            Assert.AreEqual("GO_BOUNDED_WORKSPACE_UNDERSTANDING_VERIFIED", result.Decision);
            Assert.IsTrue(result.Completed);
            Assert.IsTrue(result.VerificationPassed);
            Assert.AreEqual(MissionStatus.Completed, result.Mission.Status);
            Assert.AreEqual(CompactMissionMemoryStatus.Done, result.ResumeCard.Status == MissionStatus.Completed
                ? CompactMissionMemoryStatus.Done
                : CompactMissionMemoryStatus.Active);
            Assert.IsTrue(result.Scan.RealFilesystemRead);
            Assert.IsFalse(result.FilesystemMutationAllowed);
            Assert.IsFalse(result.NetworkUsed);
            Assert.IsFalse(result.ProductAuthorityGranted);
            Assert.IsTrue(result.Timeline.Any(value => value.Kind == OneBrain.AgentOperations.Contracts.NodalOsCoreEventKind.EvidenceAttached));
            Assert.IsTrue(result.Timeline.Any(value => value.Kind == OneBrain.AgentOperations.Contracts.NodalOsCoreEventKind.ExecutionCompleted));
            Assert.IsTrue(result.Handoff.VerifiesEvidenceContent);
            Assert.IsFalse(result.Handoff.Executable);
            Assert.IsFalse(result.Handoff.IsAuthoritative);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [TestMethod]
    public async Task HandoffExposesDigestAndCountsWithoutRootOrSecrets()
    {
        var root = CreateRoot();
        try
        {
            await File.WriteAllTextAsync(
                Path.Combine(root, "config.json"),
                "{\"api_key\":\"sk-workspace-secret-value-123456789\",\"name\":\"fixture\"}");

            var result = await new NodalOsBoundedWorkspaceMissionScenario().RunAsync(root);
            var combined = string.Join(
                "|",
                result.Handoff.MarkdownFields(),
                result.HandoffRender.MarkdownRedacted,
                result.HandoffRender.HtmlRedacted);

            Assert.AreEqual(64, result.Scan.EvidenceDigest.Length);
            StringAssert.Contains(result.Handoff.EvidenceRefsOnlyRedacted, result.Scan.EvidenceDigest);
            Assert.IsFalse(combined.Contains(root, StringComparison.OrdinalIgnoreCase));
            Assert.IsFalse(combined.Contains("sk-workspace", StringComparison.OrdinalIgnoreCase));
            Assert.IsFalse(combined.Contains("secret-value", StringComparison.OrdinalIgnoreCase));
            Assert.IsFalse(result.HandoffRender.HtmlRedacted.Contains("<script", StringComparison.OrdinalIgnoreCase));
            Assert.IsFalse(result.HandoffRender.HtmlRedacted.Contains("<form", StringComparison.OrdinalIgnoreCase));
            Assert.IsFalse(result.HandoffRender.HtmlRedacted.Contains("http://", StringComparison.OrdinalIgnoreCase));
            Assert.IsFalse(result.HandoffRender.HtmlRedacted.Contains("https://", StringComparison.OrdinalIgnoreCase));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [TestMethod]
    public async Task MissingWorkspaceReturnsFailClosedMissionAndNonExecutableHandoff()
    {
        var missing = Path.Combine(Path.GetTempPath(), "nodal-missing-bounded-mission-" + Guid.NewGuid().ToString("N"));
        var result = await new NodalOsBoundedWorkspaceMissionScenario().RunAsync(missing);

        Assert.AreEqual("BLOCKED_BOUNDED_WORKSPACE_UNDERSTANDING_FAIL_CLOSED", result.Decision);
        Assert.IsFalse(result.Completed);
        Assert.IsFalse(result.VerificationPassed);
        Assert.AreEqual(BoundedWorkspaceScanDecision.InvalidRoot, result.Scan.Decision);
        Assert.IsFalse(result.Scan.RealFilesystemRead);
        Assert.IsFalse(result.Handoff.VerifiesEvidenceContent);
        Assert.IsFalse(result.Handoff.Executable);
        Assert.IsFalse(result.Handoff.RuntimeExecutionAllowed);
        Assert.IsTrue(result.Timeline.Any(value => value.Kind == OneBrain.AgentOperations.Contracts.NodalOsCoreEventKind.ExecutionFailed));
    }

    [TestMethod]
    public async Task TruncatedMissionCompletesButCannotExpandItsOwnBudget()
    {
        var root = CreateRoot();
        try
        {
            await File.WriteAllTextAsync(Path.Combine(root, "a.md"), "a");
            await File.WriteAllTextAsync(Path.Combine(root, "b.md"), "b");
            var result = await new NodalOsBoundedWorkspaceMissionScenario().RunAsync(
                root,
                new BoundedWorkspaceScanLimits(
                    MaximumFiles: 1,
                    MaximumTotalBytes: 1024,
                    MaximumFileBytes: 1024,
                    MaximumDepth: 2,
                    MaximumPreviewCharacters: 100));

            Assert.IsTrue(result.Completed);
            Assert.IsTrue(result.Scan.Truncated);
            Assert.AreEqual(1, result.Scan.FilesRead);
            Assert.IsTrue(result.Handoff.SelectedBlockersRedacted.Any(value => value.Contains("limits", StringComparison.OrdinalIgnoreCase)));
            Assert.IsTrue(result.Handoff.WhatNeedsUserDecisionRedacted.Contains("explicit", StringComparison.OrdinalIgnoreCase));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private static string CreateRoot()
    {
        var root = Path.Combine(Path.GetTempPath(), "nodal-bounded-mission-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        return root;
    }
}

internal static class BoundedWorkspaceHandoffTestExtensions
{
    public static string MarkdownFields(this OneBrain.AgentOperations.Contracts.NodalOsPlannerHandoffPack handoff) => string.Join(
        "|",
        handoff.WhatWasReviewedRedacted,
        handoff.WhatIsBlockedRedacted,
        handoff.WhatNeedsUserDecisionRedacted,
        handoff.EvidenceRefsOnlyRedacted,
        handoff.WhatIsNotVerifiedRedacted,
        handoff.WhatCannotExecuteRedacted,
        handoff.RecommendedNextSafeStepRedacted,
        string.Join("|", handoff.ContextRefsRedacted),
        string.Join("|", handoff.EvidenceRefs));
}
