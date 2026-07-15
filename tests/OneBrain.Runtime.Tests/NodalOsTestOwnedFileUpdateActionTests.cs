using System.Text;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.AgentOperations.Core.Runtime;

namespace OneBrain.Runtime.Tests;

[TestClass]
[TestCategory("AiriSelectiveRuntime")]
[TestCategory("MvpVerticalSlice")]
[TestCategory("ControlledFileOperation")]
public sealed class NodalOsTestOwnedFileUpdateActionTests
{
    [TestMethod]
    public void ExactHashUpdateCreatesSnapshotReplacesAtomicallyVerifiesAndRollsBack()
    {
        var fixture = CreateFixture("# Original\n");
        try
        {
            var action = new NodalOsTestOwnedFileUpdateAction();
            var update = action.Execute(new NodalOsTestOwnedFileUpdateRequest(
                OperationId: "update-happy-path",
                ApprovalDecisionId: "approval-update-happy-path",
                TestOwnedRootPath: fixture.Root,
                RelativePath: fixture.RelativePath,
                ExpectedCurrentSha256: fixture.OriginalSha256,
                ReplacementContent: "# Updated\nEvidence-first.\n"));

            Assert.IsTrue(update.Success);
            Assert.AreEqual(NodalOsTestOwnedFileUpdateDecision.UpdatedAndVerified, update.Decision);
            Assert.IsTrue(update.ExistingTargetRequired);
            Assert.IsTrue(update.PreconditionMatched);
            Assert.IsTrue(update.SnapshotCreated);
            Assert.IsTrue(update.AtomicReplaceUsed);
            Assert.IsTrue(update.Verified);
            Assert.IsTrue(update.RollbackAvailable);
            Assert.IsFalse(update.RollbackPerformedAfterFailure);
            Assert.IsFalse(update.NetworkUsed);
            Assert.IsFalse(update.ExternalProcessUsed);
            Assert.AreEqual(fixture.RelativePath, update.RelativePath);
            Assert.AreEqual(fixture.OriginalSha256, update.OriginalSha256);
            Assert.AreEqual(Sha256("# Updated\nEvidence-first.\n"), update.UpdatedSha256);
            Assert.AreEqual("# Updated\nEvidence-first.\n", File.ReadAllText(fixture.Target));
            Assert.IsNotNull(update.RestorePlan);
            Assert.IsNotNull(update.Evidence);
            Assert.AreEqual("test-owned-file-update-verification", update.Evidence.Kind);
            Assert.AreEqual(update.UpdatedSha256, update.Evidence.Hash);
            Assert.AreEqual(64, update.RootFingerprint.Length);
            Assert.AreEqual(64, update.TargetFingerprint.Length);
            Assert.IsFalse(update.RestorePlan.CanRestoreUserWorkspace);
            Assert.IsTrue(update.RestorePlan.RequiresExactCurrentHash);
            Assert.IsTrue(update.RestorePlan.RestrictedToTestOwnedFixture);

            var rollback = action.Rollback(fixture.Root, update.RestorePlan);

            Assert.IsTrue(rollback.Success);
            Assert.AreEqual(NodalOsTestOwnedFileRollbackDecision.RestoredAndVerified, rollback.Decision);
            Assert.IsTrue(rollback.Restored);
            Assert.IsTrue(rollback.Verified);
            Assert.IsTrue(rollback.SnapshotRemoved);
            Assert.AreEqual(fixture.OriginalSha256, rollback.RestoredSha256);
            Assert.AreEqual("# Original\n", File.ReadAllText(fixture.Target));
            Assert.IsNotNull(rollback.Evidence);
            Assert.AreEqual("test-owned-file-rollback-verification", rollback.Evidence.Kind);
            Assert.AreEqual(fixture.OriginalSha256, rollback.Evidence.Hash);
        }
        finally
        {
            Cleanup(fixture);
        }
    }

    [TestMethod]
    public void StalePreconditionRejectsUpdateAndPreservesExistingContent()
    {
        var fixture = CreateFixture("original");
        try
        {
            var action = new NodalOsTestOwnedFileUpdateAction();
            var result = action.Execute(new NodalOsTestOwnedFileUpdateRequest(
                "update-stale",
                "approval-update-stale",
                fixture.Root,
                fixture.RelativePath,
                new string('a', 64),
                "replacement"));

            Assert.IsFalse(result.Success);
            Assert.AreEqual(NodalOsTestOwnedFileUpdateDecision.PreconditionMismatch, result.Decision);
            Assert.IsFalse(result.PreconditionMatched);
            Assert.IsFalse(result.SnapshotCreated);
            Assert.IsFalse(result.AtomicReplaceUsed);
            Assert.IsFalse(result.RollbackAvailable);
            Assert.AreEqual(fixture.OriginalSha256, result.OriginalSha256);
            Assert.AreEqual("original", File.ReadAllText(fixture.Target));
            Assert.IsFalse(Directory.Exists(Path.Combine(fixture.Root, ".nodal-restore")));
        }
        finally
        {
            Cleanup(fixture);
        }
    }

    [TestMethod]
    public void MissingNoChangeTraversalAndOutsideRootFailClosed()
    {
        var fixture = CreateFixture("same");
        var outside = Path.Combine(Path.GetTempPath(), "nodal-update-outside-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(outside);
        try
        {
            var action = new NodalOsTestOwnedFileUpdateAction();
            var missing = action.Execute(new NodalOsTestOwnedFileUpdateRequest(
                "update-missing",
                "approval-update-missing",
                fixture.Root,
                "output/missing.md",
                fixture.OriginalSha256,
                "replacement"));
            var noChange = action.Execute(new NodalOsTestOwnedFileUpdateRequest(
                "update-no-change",
                "approval-update-no-change",
                fixture.Root,
                fixture.RelativePath,
                fixture.OriginalSha256,
                "same"));
            var traversal = action.Execute(new NodalOsTestOwnedFileUpdateRequest(
                "update-traversal",
                "approval-update-traversal",
                fixture.Root,
                "output/../escape.md",
                fixture.OriginalSha256,
                "replacement"));
            var absolute = action.Execute(new NodalOsTestOwnedFileUpdateRequest(
                "update-absolute",
                "approval-update-absolute",
                outside,
                "output/file.md",
                fixture.OriginalSha256,
                "replacement"));

            Assert.AreEqual(NodalOsTestOwnedFileUpdateDecision.TargetMissing, missing.Decision);
            Assert.AreEqual(NodalOsTestOwnedFileUpdateDecision.NoChangeRequested, noChange.Decision);
            Assert.AreEqual(NodalOsTestOwnedFileUpdateDecision.InvalidRelativePath, traversal.Decision);
            Assert.AreEqual(NodalOsTestOwnedFileUpdateDecision.RootOutsideFixtureBoundary, absolute.Decision);
            Assert.IsFalse(missing.Success);
            Assert.IsFalse(noChange.Success);
            Assert.IsFalse(traversal.Success);
            Assert.IsFalse(absolute.Success);
            Assert.AreEqual("same", File.ReadAllText(fixture.Target));
        }
        finally
        {
            Cleanup(fixture);
            Directory.Delete(outside, recursive: true);
        }
    }

    [TestMethod]
    public void OversizedExistingOrReplacementContentIsRejectedBeforeReplacement()
    {
        var fixture = CreateFixture("original");
        try
        {
            var action = new NodalOsTestOwnedFileUpdateAction();
            var oversizedReplacement = action.Execute(new NodalOsTestOwnedFileUpdateRequest(
                "update-large-replacement",
                "approval-large-replacement",
                fixture.Root,
                fixture.RelativePath,
                fixture.OriginalSha256,
                new string('x', NodalOsTestOwnedFileUpdateAction.MaximumContentBytes + 1)));
            Assert.AreEqual(NodalOsTestOwnedFileUpdateDecision.InvalidRequest, oversizedReplacement.Decision);
            Assert.AreEqual("original", File.ReadAllText(fixture.Target));

            File.WriteAllText(fixture.Target, new string('y', NodalOsTestOwnedFileUpdateAction.MaximumContentBytes + 1));
            var oversizedTarget = action.Execute(new NodalOsTestOwnedFileUpdateRequest(
                "update-large-target",
                "approval-large-target",
                fixture.Root,
                fixture.RelativePath,
                Sha256(File.ReadAllText(fixture.Target)),
                "replacement"));
            Assert.AreEqual(NodalOsTestOwnedFileUpdateDecision.TargetTooLarge, oversizedTarget.Decision);
            Assert.IsFalse(oversizedTarget.Success);
        }
        finally
        {
            Cleanup(fixture);
        }
    }

    [TestMethod]
    public void RollbackRejectsTargetChangedAfterVerifiedUpdate()
    {
        var fixture = CreateFixture("original");
        try
        {
            var action = new NodalOsTestOwnedFileUpdateAction();
            var update = action.Execute(new NodalOsTestOwnedFileUpdateRequest(
                "update-rollback-stale",
                "approval-rollback-stale",
                fixture.Root,
                fixture.RelativePath,
                fixture.OriginalSha256,
                "updated"));
            Assert.IsTrue(update.Success);
            Assert.IsNotNull(update.RestorePlan);

            File.WriteAllText(fixture.Target, "unexpected third-party change");
            var rollback = action.Rollback(fixture.Root, update.RestorePlan);

            Assert.IsFalse(rollback.Success);
            Assert.AreEqual(NodalOsTestOwnedFileRollbackDecision.CurrentHashMismatch, rollback.Decision);
            Assert.AreEqual("unexpected third-party change", File.ReadAllText(fixture.Target));
        }
        finally
        {
            Cleanup(fixture);
        }
    }

    [TestMethod]
    public void ResultPlanAndEvidenceNeverExposeRawPathsOrUserName()
    {
        var fixture = CreateFixture("original");
        try
        {
            var action = new NodalOsTestOwnedFileUpdateAction();
            var result = action.Execute(new NodalOsTestOwnedFileUpdateRequest(
                "update-redaction",
                "approval-update-redaction",
                fixture.Root,
                fixture.RelativePath,
                fixture.OriginalSha256,
                "updated"));
            Assert.IsTrue(result.Success);

            var json = JsonSerializer.Serialize(result);
            var userName = Environment.UserName;
            Assert.IsFalse(json.Contains(fixture.Root, StringComparison.OrdinalIgnoreCase));
            Assert.IsFalse(json.Contains(fixture.Target, StringComparison.OrdinalIgnoreCase));
            if (!string.IsNullOrWhiteSpace(userName) && userName.Length > 2)
                Assert.IsFalse(json.Contains(userName, StringComparison.OrdinalIgnoreCase));
            Assert.IsFalse(json.Contains(".nodal-restore", StringComparison.OrdinalIgnoreCase));
            Assert.IsFalse(json.Contains(".bak", StringComparison.OrdinalIgnoreCase));
            Assert.AreEqual(64, result.RestorePlan?.RootFingerprint.Length);
            Assert.AreEqual(64, result.RestorePlan?.TargetFingerprint.Length);
        }
        finally
        {
            Cleanup(fixture);
        }
    }

    private static Fixture CreateFixture(string originalContent)
    {
        var create = new NodalOsTestOwnedFileCreateAction();
        var root = Path.Combine(
            NodalOsTestOwnedFileCreateAction.AllowedBaseRoot,
            "run-" + Guid.NewGuid().ToString("N"));
        const string relativePath = "output/update-target.md";
        var created = create.Execute(new NodalOsTestOwnedFileCreateRequest(
            OperationId: "setup-" + Guid.NewGuid().ToString("N"),
            ApprovalDecisionId: "approval-setup",
            TestOwnedRootPath: root,
            RelativePath: relativePath,
            Content: originalContent));
        Assert.IsTrue(created.Success, created.SafeMessage);
        Assert.IsNotNull(created.ContentSha256);
        return new Fixture(
            root,
            Path.Combine(root, relativePath.Replace('/', Path.DirectorySeparatorChar)),
            relativePath,
            created.ContentSha256,
            created.RootFingerprint);
    }

    private static void Cleanup(Fixture fixture)
    {
        var cleanup = new NodalOsTestOwnedFileCreateAction()
            .CleanupOwnedRoot(fixture.Root, fixture.RootFingerprint);
        Assert.IsTrue(cleanup.Success, cleanup.SafeMessage);
    }

    private static string Sha256(string value) =>
        Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(Encoding.UTF8.GetBytes(value)))
            .ToLowerInvariant();

    private sealed record Fixture(
        string Root,
        string Target,
        string RelativePath,
        string OriginalSha256,
        string RootFingerprint);
}
