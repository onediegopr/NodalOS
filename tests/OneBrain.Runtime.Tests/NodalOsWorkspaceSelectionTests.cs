using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.AgentOperations.Core.Workspace;
using OneBrain.Core.Models;

namespace OneBrain.Runtime.Tests;

[TestClass]
[TestCategory("WorkspaceSelection")]
[TestCategory("MvpVerticalSlice")]
public sealed class NodalOsWorkspaceSelectionTests
{
    [TestMethod]
    public async Task SelectAsync_ProtectsRootPersistsMetadataAndRehydratesWithoutWorkspaceMutation()
    {
        RequireWindows();
        using var fixture = WorkspaceFixture.Create();
        var before = fixture.FileHashes();
        var service = fixture.CreateService();

        var selected = await service.SelectAsync(
            fixture.WorkspaceRoot,
            "Product Alpha",
            TestContext.CancellationTokenSource.Token);

        Assert.IsTrue(selected.Accepted, string.Join(" | ", selected.ReviewBlockers));
        Assert.AreEqual(NodalOsWorkspaceSelectionState.Ready, selected.State);
        Assert.AreEqual("GO_REAL_LOCAL_WORKSPACE_SELECTED_AND_PERSISTED", selected.Decision);
        Assert.IsTrue(selected.Persisted);
        Assert.IsFalse(selected.Rehydrated);
        Assert.IsTrue(selected.RealFilesystemRead);
        Assert.IsTrue(selected.FilesRead >= 2);
        Assert.IsTrue(selected.PlanSteps.Count >= 1);
        Assert.IsTrue(selected.AppConfigurationMutated);
        Assert.IsFalse(selected.WorkspaceFilesystemMutated);
        Assert.IsFalse(selected.NetworkUsed);
        Assert.IsTrue(selected.SecretsExcluded);
        Assert.IsFalse(selected.ProductAuthorityGranted);
        Assert.IsNotNull(selected.Workspace);
        Assert.AreEqual("Product Alpha", selected.DisplayNameRedacted);
        Assert.IsFalse(Path.IsPathRooted(selected.RootPathHintRedacted));
        Assert.AreEqual(64, selected.RootPathFingerprint?.Length);
        Assert.AreEqual(64, selected.EvidenceDigest.Length);
        Assert.IsTrue(File.Exists(fixture.MetadataPath));

        var metadata = await File.ReadAllTextAsync(
            fixture.MetadataPath,
            TestContext.CancellationTokenSource.Token);
        Assert.IsFalse(metadata.Contains(fixture.WorkspaceRoot, StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(metadata.Contains(fixture.FakeSecret, StringComparison.Ordinal));
        Assert.IsFalse(metadata.Contains("Program.cs contents", StringComparison.Ordinal));
        StringAssert.Contains(metadata, "windows-dpapi");
        StringAssert.Contains(metadata, "local-workspace://");

        var protectedFiles = Directory.GetFiles(fixture.SecretRoot, "*.bin");
        Assert.AreEqual(1, protectedFiles.Length);
        var protectedBytes = await File.ReadAllBytesAsync(
            protectedFiles[0],
            TestContext.CancellationTokenSource.Token);
        Assert.AreEqual(-1, IndexOf(protectedBytes, Encoding.UTF8.GetBytes(fixture.WorkspaceRoot)));

        var rehydrated = await fixture.CreateService().GetCurrentAsync(
            TestContext.CancellationTokenSource.Token);

        Assert.IsTrue(rehydrated.Accepted, string.Join(" | ", rehydrated.ReviewBlockers));
        Assert.AreEqual("GO_REAL_LOCAL_WORKSPACE_REHYDRATED", rehydrated.Decision);
        Assert.IsTrue(rehydrated.Persisted);
        Assert.IsTrue(rehydrated.Rehydrated);
        Assert.IsFalse(rehydrated.AppConfigurationMutated);
        Assert.AreEqual(selected.WorkspaceId, rehydrated.WorkspaceId);
        Assert.AreEqual(selected.RootPathFingerprint, rehydrated.RootPathFingerprint);
        CollectionAssert.AreEquivalent(before.Keys.ToArray(), fixture.FileHashes().Keys.ToArray());
        foreach (var pair in before)
            Assert.AreEqual(pair.Value, fixture.FileHashes()[pair.Key], pair.Key);

        var serialized = JsonSerializer.Serialize(rehydrated);
        Assert.IsFalse(serialized.Contains(fixture.WorkspaceRoot, StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(serialized.Contains(fixture.FakeSecret, StringComparison.Ordinal));
    }

    [TestMethod]
    public async Task SelectAsync_InvalidRootFailsClosedWithoutPersistence()
    {
        RequireWindows();
        using var fixture = WorkspaceFixture.Create(createWorkspace: false);
        var service = fixture.CreateService();

        var result = await service.SelectAsync(
            fixture.WorkspaceRoot,
            cancellationToken: TestContext.CancellationTokenSource.Token);

        Assert.IsFalse(result.Accepted);
        Assert.AreEqual(NodalOsWorkspaceSelectionState.InvalidRoot, result.State);
        Assert.IsFalse(result.Persisted);
        Assert.IsFalse(result.WorkspaceFilesystemMutated);
        Assert.IsFalse(result.AppConfigurationMutated);
        Assert.IsFalse(File.Exists(fixture.MetadataPath));
        Assert.IsFalse(Directory.Exists(fixture.SecretRoot));
    }

    [TestMethod]
    public async Task GetCurrentAsync_MissingSelectedRootFailsClosedWithoutLeakingPath()
    {
        RequireWindows();
        using var fixture = WorkspaceFixture.Create();
        var service = fixture.CreateService();
        var selected = await service.SelectAsync(
            fixture.WorkspaceRoot,
            cancellationToken: TestContext.CancellationTokenSource.Token);
        Assert.IsTrue(selected.Accepted);

        fixture.DeleteWorkspaceOnly();
        var result = await fixture.CreateService().GetCurrentAsync(
            TestContext.CancellationTokenSource.Token);

        Assert.IsFalse(result.Accepted);
        Assert.AreEqual(NodalOsWorkspaceSelectionState.InvalidRoot, result.State);
        Assert.IsTrue(result.Persisted);
        Assert.IsFalse(result.WorkspaceFilesystemMutated);
        var serialized = JsonSerializer.Serialize(result);
        Assert.IsFalse(serialized.Contains(fixture.WorkspaceRoot, StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(result.ReviewBlockers.Any(value => value.Contains(fixture.WorkspaceRoot, StringComparison.OrdinalIgnoreCase)));
    }

    [TestMethod]
    public async Task ClearAsync_RemovesMetadataAndProtectedRootReference()
    {
        RequireWindows();
        using var fixture = WorkspaceFixture.Create();
        var service = fixture.CreateService();
        var selected = await service.SelectAsync(
            fixture.WorkspaceRoot,
            cancellationToken: TestContext.CancellationTokenSource.Token);
        Assert.IsTrue(selected.Accepted);

        var cleared = await service.ClearAsync(TestContext.CancellationTokenSource.Token);

        Assert.AreEqual(NodalOsWorkspaceSelectionState.NotConfigured, cleared.State);
        Assert.IsTrue(cleared.AppConfigurationMutated);
        Assert.IsFalse(File.Exists(fixture.MetadataPath));
        Assert.AreEqual(0, Directory.Exists(fixture.SecretRoot)
            ? Directory.GetFiles(fixture.SecretRoot, "*.bin").Length
            : 0);
    }

    public TestContext TestContext { get; set; } = null!;

    private static void RequireWindows()
    {
        if (!OperatingSystem.IsWindows())
            Assert.Inconclusive("Protected workspace selection uses Windows DPAPI.");
    }

    private static int IndexOf(byte[] source, byte[] value)
    {
        if (value.Length == 0 || value.Length > source.Length)
            return -1;
        for (var i = 0; i <= source.Length - value.Length; i++)
        {
            if (source.AsSpan(i, value.Length).SequenceEqual(value))
                return i;
        }
        return -1;
    }

    private sealed class WorkspaceFixture : IDisposable
    {
        private WorkspaceFixture(string root, bool createWorkspace)
        {
            Root = root;
            WorkspaceRoot = Path.Combine(root, "workspace");
            MetadataPath = Path.Combine(root, "config", "selection.v1.json");
            SecretRoot = Path.Combine(root, "secrets");
            FakeSecret = string.Concat(
                "s",
                "k-",
                "workspace-selection-fixture-",
                "value-123456789");

            if (!createWorkspace)
                return;
            Directory.CreateDirectory(Path.Combine(WorkspaceRoot, "src"));
            File.WriteAllText(Path.Combine(WorkspaceRoot, "README.md"), "# Workspace selection fixture");
            File.WriteAllText(
                Path.Combine(WorkspaceRoot, "src", "Program.cs"),
                $"var api_key = \"{FakeSecret}\";{Environment.NewLine}Console.WriteLine(\"fixture\");");
        }

        public string Root { get; }
        public string WorkspaceRoot { get; }
        public string MetadataPath { get; }
        public string SecretRoot { get; }
        public string FakeSecret { get; }

        public static WorkspaceFixture Create(bool createWorkspace = true) => new(
            Path.Combine(Path.GetTempPath(), "nodal-os-workspace-selection-tests", Guid.NewGuid().ToString("N")),
            createWorkspace);

        public NodalOsWorkspaceSelectionService CreateService() => new(
            MetadataPath,
            new WindowsDpapiSecretReferenceStore(SecretRoot));

        public Dictionary<string, string> FileHashes()
        {
            if (!Directory.Exists(WorkspaceRoot))
                return new Dictionary<string, string>();
            return Directory.GetFiles(WorkspaceRoot, "*", SearchOption.AllDirectories)
                .OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(
                    value => Path.GetRelativePath(WorkspaceRoot, value),
                    value => Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(value))).ToLowerInvariant(),
                    StringComparer.OrdinalIgnoreCase);
        }

        public void DeleteWorkspaceOnly()
        {
            if (Directory.Exists(WorkspaceRoot))
                Directory.Delete(WorkspaceRoot, recursive: true);
        }

        public void Dispose()
        {
            try
            {
                if (Directory.Exists(Root))
                {
                    foreach (var path in Directory.GetFiles(Root, "*", SearchOption.AllDirectories))
                        File.SetAttributes(path, FileAttributes.Normal);
                    Directory.Delete(Root, recursive: true);
                }
            }
            catch
            {
            }
        }
    }
}
