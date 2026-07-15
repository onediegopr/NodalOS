using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.AgentOperations.Core.Runtime;

namespace OneBrain.Runtime.Tests;

[TestClass]
[TestCategory("MvpVerticalSlice")]
public sealed class NodalOsTestOwnedFileCreateActionTests
{
    [TestMethod]
    public void CreateOnlyActionWritesAtomicallyVerifiesAndCleansItsOwnedRoot()
    {
        var action = new NodalOsTestOwnedFileCreateAction();
        var root = UniqueRoot();
        var request = Request(root, "output/verified-handoff.md", "# Verified fixture\n");
        NodalOsTestOwnedFileCreateResult? result = null;

        try
        {
            result = action.Execute(request);
            var target = Path.Combine(root, "output", "verified-handoff.md");

            Assert.AreEqual(NodalOsTestOwnedFileCreateDecision.CreatedAndVerified, result.Decision);
            Assert.IsTrue(result.Success);
            Assert.IsTrue(result.Created);
            Assert.IsTrue(result.Verified);
            Assert.IsFalse(result.OverwriteAttempted);
            Assert.IsFalse(result.NetworkUsed);
            Assert.IsFalse(result.ExternalProcessUsed);
            Assert.AreEqual("output/verified-handoff.md", result.RelativePath);
            Assert.AreEqual(64, result.ContentSha256?.Length);
            Assert.IsNotNull(result.Evidence);
            Assert.AreEqual(result.ContentSha256, result.Evidence.Hash);
            Assert.IsTrue(File.Exists(target));
            Assert.AreEqual(request.Content, File.ReadAllText(target));

            var cleanup = action.CleanupOwnedRoot(root, result.RootFingerprint);
            Assert.IsTrue(cleanup.Success);
            Assert.IsTrue(cleanup.RootRemoved);
            Assert.IsFalse(Directory.Exists(root));
        }
        finally
        {
            if (Directory.Exists(root) && result is not null)
                action.CleanupOwnedRoot(root, result.RootFingerprint);
        }
    }

    [TestMethod]
    public void CreateOnlyActionRejectsTraversalAbsoluteMixedAndUnsupportedTargets()
    {
        var action = new NodalOsTestOwnedFileCreateAction();
        var root = UniqueRoot();
        var invalidPaths = new[]
        {
            "output/../escape.md",
            "../escape.md",
            "/output/escape.md",
            @"output\escape.md",
            "nested/output/escape.md",
            "output/escape.exe"
        };

        foreach (var invalidPath in invalidPaths)
        {
            var result = action.Execute(Request(root, invalidPath, "fixture"));
            Assert.AreEqual(NodalOsTestOwnedFileCreateDecision.InvalidRelativePath, result.Decision, invalidPath);
            Assert.IsFalse(result.Success, invalidPath);
        }

        Assert.IsFalse(Directory.Exists(root));
    }

    [TestMethod]
    public void CreateOnlyActionRejectsOverwriteAndPreservesExistingContent()
    {
        var action = new NodalOsTestOwnedFileCreateAction();
        var root = UniqueRoot();
        var output = Path.Combine(root, "output");
        var target = Path.Combine(output, "verified-handoff.md");
        Directory.CreateDirectory(output);
        File.WriteAllText(target, "existing content");
        var fingerprint = string.Empty;

        try
        {
            var result = action.Execute(Request(root, "output/verified-handoff.md", "replacement"));
            fingerprint = result.RootFingerprint;

            Assert.AreEqual(NodalOsTestOwnedFileCreateDecision.TargetAlreadyExists, result.Decision);
            Assert.IsFalse(result.Success);
            Assert.IsFalse(result.OverwriteAttempted);
            Assert.AreEqual("existing content", File.ReadAllText(target));
        }
        finally
        {
            if (Directory.Exists(root) && fingerprint.Length > 0)
                action.CleanupOwnedRoot(root, fingerprint);
        }
    }

    [TestMethod]
    public void CreateOnlyActionRejectsRootsOutsideTheDedicatedFixtureBoundary()
    {
        var action = new NodalOsTestOwnedFileCreateAction();
        var outsideRoot = Path.Combine(Path.GetTempPath(), "outside-nodal-fixture-" + Guid.NewGuid().ToString("N"));

        var result = action.Execute(Request(outsideRoot, "output/report.md", "fixture"));

        Assert.AreEqual(NodalOsTestOwnedFileCreateDecision.RootOutsideFixtureBoundary, result.Decision);
        Assert.IsFalse(result.Success);
        Assert.IsFalse(Directory.Exists(outsideRoot));
    }

    [TestMethod]
    public void CreateOnlyEvidenceAndResultNeverExposeRawPathsOrUserName()
    {
        var action = new NodalOsTestOwnedFileCreateAction();
        var root = UniqueRoot();
        NodalOsTestOwnedFileCreateResult? result = null;

        try
        {
            result = action.Execute(Request(root, "output/report.txt", "verified fixture report"));
            Assert.IsTrue(result.Success);

            var serialized = string.Join(
                "|",
                result.RootFingerprint,
                result.TargetFingerprint,
                result.RelativePath,
                result.ContentSha256,
                result.SafeMessage,
                result.Evidence?.EvidenceId,
                result.Evidence?.Kind,
                result.Evidence?.Ref,
                result.Evidence?.LedgerRef,
                result.Evidence?.Provenance);

            Assert.IsFalse(serialized.Contains(root, StringComparison.OrdinalIgnoreCase));
            Assert.IsFalse(serialized.Contains(Path.GetTempPath(), StringComparison.OrdinalIgnoreCase));
            Assert.IsFalse(serialized.Contains(Environment.UserName, StringComparison.OrdinalIgnoreCase));
        }
        finally
        {
            if (Directory.Exists(root) && result is not null)
                action.CleanupOwnedRoot(root, result.RootFingerprint);
        }
    }

    [TestMethod]
    public void CreateOnlyActionRejectsReparseRootWhenThePlatformCanCreateOne()
    {
        var action = new NodalOsTestOwnedFileCreateAction();
        Directory.CreateDirectory(NodalOsTestOwnedFileCreateAction.AllowedBaseRoot);
        var targetRoot = Path.Combine(Path.GetTempPath(), "nodal-reparse-target-" + Guid.NewGuid().ToString("N"));
        var linkRoot = UniqueRoot();
        Directory.CreateDirectory(targetRoot);

        try
        {
            try
            {
                Directory.CreateSymbolicLink(linkRoot, targetRoot);
            }
            catch (Exception exception) when (exception is UnauthorizedAccessException or PlatformNotSupportedException or IOException)
            {
                Assert.Inconclusive($"Symbolic link creation is unavailable on this runner: {exception.GetType().Name}");
                return;
            }

            var result = action.Execute(Request(linkRoot, "output/report.md", "fixture"));

            Assert.AreEqual(NodalOsTestOwnedFileCreateDecision.ReparsePointRejected, result.Decision);
            Assert.IsFalse(result.Success);
            Assert.IsFalse(File.Exists(Path.Combine(targetRoot, "output", "report.md")));
        }
        finally
        {
            if (Directory.Exists(linkRoot))
                Directory.Delete(linkRoot);
            if (Directory.Exists(targetRoot))
                Directory.Delete(targetRoot, recursive: true);
        }
    }

    private static string UniqueRoot() =>
        Path.Combine(NodalOsTestOwnedFileCreateAction.AllowedBaseRoot, "run-" + Guid.NewGuid().ToString("N"));

    private static NodalOsTestOwnedFileCreateRequest Request(string root, string relativePath, string content) =>
        new(
            OperationId: "op-" + Guid.NewGuid().ToString("N"),
            ApprovalDecisionId: "approval-" + Guid.NewGuid().ToString("N"),
            TestOwnedRootPath: root,
            RelativePath: relativePath,
            Content: content);
}
