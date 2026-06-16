using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.BrowserExecutor.Cdp;
using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.Safety.Tests;

[TestClass]
public sealed class NexaCanonicalWorkspaceGuardM74Tests
{
    private const string CanonicalPath = @"C:\Users\diego\OneDrive\PERSONAL\ONE Brain\Codigo-m12-audit";
    private const string LegacyPath = @"C:\Users\diego\OneDrive\PERSONAL\ONE Brain\Codigo";
    private const string RemoteBranch = "origin/chrome-lab-001-extension-local-ai-bridge";
    private const string CanonicalHead = "45ec42a9372b0bdb3f54f4ae159f506548cb37aa";

    [TestMethod]
    public void CanonicalWorkspaceGuardAllowsCanonicalEnvironment()
    {
        var result = Evaluate(Snapshot(CanonicalPath, RemoteBranch, dirty: false, detached: false));

        Assert.AreEqual(NexaCanonicalWorkspaceGuardDecisionKind.Allowed, result.Decision);
        Assert.IsFalse(result.ModifiedWorkspace);
        Assert.IsTrue(result.MatchesRemoteHead);
    }

    [TestMethod]
    public void CanonicalWorkspaceGuardBlocksDirtyEnvironment()
    {
        var result = Evaluate(Snapshot(CanonicalPath, RemoteBranch, dirty: true, detached: false, status: [" M src/file.cs"]));

        Assert.AreEqual(NexaCanonicalWorkspaceGuardDecisionKind.Blocked, result.Decision);
        CollectionAssert.Contains(result.BlockingReasons.ToList(), "workspace has uncommitted changes");
        Assert.IsFalse(result.ModifiedWorkspace);
    }

    [TestMethod]
    public void CanonicalWorkspaceGuardBlocksWrongBranch()
    {
        var result = Evaluate(Snapshot(CanonicalPath, "master", dirty: false, detached: false));

        Assert.AreEqual(NexaCanonicalWorkspaceGuardDecisionKind.Blocked, result.Decision);
        CollectionAssert.Contains(result.BlockingReasons.ToList(), "current branch is not the expected canonical branch");
    }

    [TestMethod]
    public void CanonicalWorkspaceGuardBlocksLegacyCodigoPath()
    {
        var result = Evaluate(Snapshot(LegacyPath, "master", dirty: true, detached: false));

        Assert.AreEqual(NexaCanonicalWorkspaceGuardDecisionKind.Blocked, result.Decision);
        Assert.IsTrue(result.IsLegacyPath);
        Assert.IsTrue(result.OperatorMessage.Contains("legacy", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public void CanonicalWorkspaceGuardAllowsDetachedHeadWhenItMatchesCanonicalRemote()
    {
        var result = Evaluate(Snapshot(CanonicalPath, null, dirty: false, detached: true));

        Assert.AreEqual(NexaCanonicalWorkspaceGuardDecisionKind.Allowed, result.Decision);
        Assert.IsTrue(result.DetachedHeadAccepted);
    }

    [TestMethod]
    public void CanonicalWorkspaceGuardProvidesClearOperatorBlockMessage()
    {
        var result = Evaluate(Snapshot(LegacyPath, "master", dirty: true, detached: false, head: "f60a4c8"));

        Assert.AreEqual(NexaCanonicalWorkspaceGuardDecisionKind.Blocked, result.Decision);
        Assert.IsTrue(result.OperatorMessage.Contains("Canonical workspace guard blocked", StringComparison.Ordinal));
        Assert.IsTrue(result.OperatorMessage.Contains("Codigo-m12-audit", StringComparison.Ordinal));
    }

    private static NexaCanonicalWorkspaceGuardResult Evaluate(NexaCanonicalWorkspaceSnapshot snapshot) =>
        new NexaCanonicalWorkspaceGuardService().Evaluate(
            new NexaCanonicalWorkspaceGuardConfig(CanonicalPath, RemoteBranch, CanonicalHead, [LegacyPath]),
            snapshot);

    private static NexaCanonicalWorkspaceSnapshot Snapshot(string path, string? branch, bool dirty, bool detached, string? head = null, IReadOnlyList<string>? status = null) =>
        new(path, branch, head ?? CanonicalHead, CanonicalHead, dirty, detached, status ?? []);
}
