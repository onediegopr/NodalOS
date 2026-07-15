using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.AgentOperations.Core.Workspace;

namespace OneBrain.Runtime.Tests;

[TestClass]
[TestCategory("MvpVerticalSlice")]
[TestCategory("WorkspaceUnderstanding")]
public sealed class BoundedWorkspaceUnderstandingTests
{
    [TestMethod]
    public async Task ScanReadsOnlyBoundedFixtureAndReturnsRedactedEvidence()
    {
        var root = CreateRoot();
        try
        {
            Directory.CreateDirectory(Path.Combine(root, "src"));
            Directory.CreateDirectory(Path.Combine(root, "node_modules", "package"));
            await File.WriteAllTextAsync(Path.Combine(root, "README.md"), "# Fixture\nLocal workspace overview.");
            await File.WriteAllTextAsync(
                Path.Combine(root, "src", "Program.cs"),
                "var api_key = \"sk-example-secret-value-123456789\";\nConsole.WriteLine(\"fixture\");");
            await File.WriteAllTextAsync(Path.Combine(root, ".env"), "PASSWORD=must-not-appear");
            await File.WriteAllTextAsync(Path.Combine(root, "node_modules", "package", "index.js"), "ignored");
            var before = Snapshot(root);

            var result = await new BoundedWorkspaceUnderstandingService().ScanAsync(
                new BoundedWorkspaceScanRequest(root));

            Assert.AreEqual(BoundedWorkspaceScanDecision.Accepted, result.Decision);
            Assert.AreEqual(2, result.FilesRead);
            Assert.IsTrue(result.RealFilesystemRead);
            Assert.IsFalse(result.FilesystemMutationAllowed);
            Assert.IsFalse(result.NetworkUsed);
            Assert.IsFalse(result.ProductAuthorityGranted);
            Assert.IsTrue(result.SecretsExcluded);
            Assert.AreEqual(64, result.RootFingerprint.Length);
            Assert.AreEqual(64, result.EvidenceDigest.Length);
            Assert.IsTrue(result.Files.Any(value => value.RelativePathRedacted == "README.md"));
            var program = result.Files.Single(value => value.RelativePathRedacted == "src/Program.cs");
            Assert.IsTrue(program.SecretLikeContentRedacted);
            StringAssert.Contains(program.PreviewRedacted ?? string.Empty, "[REDACTED]");
            Assert.IsFalse((program.PreviewRedacted ?? string.Empty).Contains("sk-example", StringComparison.OrdinalIgnoreCase));
            var serialized = JsonSerializer.Serialize(result);
            Assert.IsFalse(serialized.Contains(root, StringComparison.OrdinalIgnoreCase));
            Assert.IsFalse(serialized.Contains("must-not-appear", StringComparison.OrdinalIgnoreCase));
            CollectionAssert.AreEqual(before, Snapshot(root));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [TestMethod]
    public async Task ScanStopsAtFileBudgetInDeterministicOrder()
    {
        var root = CreateRoot();
        try
        {
            await File.WriteAllTextAsync(Path.Combine(root, "b.cs"), "class B {}");
            await File.WriteAllTextAsync(Path.Combine(root, "a.cs"), "class A {}");
            var result = await new BoundedWorkspaceUnderstandingService().ScanAsync(
                new BoundedWorkspaceScanRequest(
                    root,
                    Limits: new BoundedWorkspaceScanLimits(
                        MaximumFiles: 1,
                        MaximumTotalBytes: 1024,
                        MaximumFileBytes: 1024,
                        MaximumDepth: 2,
                        MaximumPreviewCharacters: 100)));

            Assert.AreEqual(BoundedWorkspaceScanDecision.Accepted, result.Decision);
            Assert.IsTrue(result.Truncated);
            Assert.AreEqual(1, result.FilesRead);
            Assert.AreEqual("a.cs", result.Files.Single().RelativePathRedacted);
            Assert.IsTrue(result.Findings.Any(value => value.Contains("budget", StringComparison.OrdinalIgnoreCase)));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [TestMethod]
    public async Task MissingRootFailsClosedWithoutReadingFilesystem()
    {
        var missing = Path.Combine(Path.GetTempPath(), "nodal-missing-workspace-" + Guid.NewGuid().ToString("N"));
        var result = await new BoundedWorkspaceUnderstandingService().ScanAsync(
            new BoundedWorkspaceScanRequest(missing));

        Assert.AreEqual(BoundedWorkspaceScanDecision.InvalidRoot, result.Decision);
        Assert.IsFalse(result.RealFilesystemRead);
        Assert.IsFalse(result.FilesystemMutationAllowed);
        Assert.IsFalse(result.NetworkUsed);
        Assert.AreEqual(0, result.FilesRead);
    }

    [TestMethod]
    public async Task PreCancelledScanReturnsCancelledWithoutOpeningWorkspace()
    {
        var root = CreateRoot();
        try
        {
            await File.WriteAllTextAsync(Path.Combine(root, "README.md"), "fixture");
            using var cancellation = new CancellationTokenSource();
            cancellation.Cancel();

            var result = await new BoundedWorkspaceUnderstandingService().ScanAsync(
                new BoundedWorkspaceScanRequest(root),
                cancellation.Token);

            Assert.AreEqual(BoundedWorkspaceScanDecision.Cancelled, result.Decision);
            Assert.IsTrue(result.Cancelled);
            Assert.IsFalse(result.RealFilesystemRead);
            Assert.AreEqual(0, result.FilesRead);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [TestMethod]
    public async Task LargeFilesAreSampledWithinPerFileAndTotalBudgets()
    {
        var root = CreateRoot();
        try
        {
            await File.WriteAllTextAsync(Path.Combine(root, "large.md"), new string('x', 10_000));
            var result = await new BoundedWorkspaceUnderstandingService().ScanAsync(
                new BoundedWorkspaceScanRequest(
                    root,
                    Limits: new BoundedWorkspaceScanLimits(
                        MaximumFiles: 5,
                        MaximumTotalBytes: 128,
                        MaximumFileBytes: 128,
                        MaximumDepth: 2,
                        MaximumPreviewCharacters: 64)));

            var file = result.Files.Single();
            Assert.AreEqual(128, file.BytesRead);
            Assert.IsTrue(file.FileSampleTruncated);
            Assert.IsTrue((file.PreviewRedacted?.Length ?? 0) <= 64);
            Assert.AreEqual(128, result.TotalBytesRead);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private static string CreateRoot()
    {
        var root = Path.Combine(Path.GetTempPath(), "nodal-workspace-scan-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        return root;
    }

    private static string[] Snapshot(string root) =>
        Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories)
            .OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
            .Select(value => $"{Path.GetRelativePath(root, value)}|{Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(File.ReadAllBytes(value)))}")
            .ToArray();
}
