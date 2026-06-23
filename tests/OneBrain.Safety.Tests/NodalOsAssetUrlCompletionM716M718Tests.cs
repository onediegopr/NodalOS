using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("NodalOsAssetUrlCompletionM716M718")]
public sealed class NodalOsAssetUrlCompletionM716M718Tests
{
    private const string AssetRetryPath = "artifacts/agent-operations/m716/asset-screenshot-completion-retry.json";
    private const string UrlRetryPath = "artifacts/agent-operations/m717/privacy-support-docs-url-closure-retry.json";
    private const string FinalPath = "artifacts/agent-operations/m718/final-readiness-reevaluation.json";
    private const string ConsolidatedPath = "artifacts/agent-operations/m716-m718/asset-url-completion-retry-go-no-go.json";

    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(Environment.CurrentDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "OneBrain.slnx")))
            dir = dir.Parent;
        return dir?.FullName ?? Environment.CurrentDirectory;
    }

    private static string FullPath(string relativePath) => Path.Combine(RepoRoot(), relativePath);

    private static JsonDocument ReadJson(string relativePath) => JsonDocument.Parse(File.ReadAllText(FullPath(relativePath)));

    [TestMethod]
    public void M716M718ArtifactsAndReportsExist()
    {
        foreach (var path in new[]
        {
            AssetRetryPath, UrlRetryPath, FinalPath, ConsolidatedPath,
            "artifacts/manual-qa/m716-m718/asset-url-completion-retry-index.json",
            "artifacts/manual-qa/m716-m718/operator-capture-instructions.md",
            "docs/reports/m716-asset-screenshot-completion-retry.md",
            "docs/reports/m717-privacy-support-docs-url-closure-retry.md",
            "docs/reports/m718-final-readiness-reevaluation.md"
        })
            Assert.IsTrue(File.Exists(FullPath(path)), path);
    }

    [TestMethod]
    public void ScreenshotsUrlsAndReleaseRemainBlocked()
    {
        using var asset = ReadJson(AssetRetryPath);
        using var url = ReadJson(UrlRetryPath);
        using var final = ReadJson(ConsolidatedPath);
        Assert.IsTrue(asset.RootElement.GetProperty("assetCapturePending").GetBoolean());
        Assert.IsFalse(asset.RootElement.GetProperty("requiredScreenshotsReady").GetBoolean());
        Assert.IsFalse(url.RootElement.GetProperty("privacyUrlReady").GetBoolean());
        Assert.IsFalse(url.RootElement.GetProperty("supportUrlReady").GetBoolean());
        Assert.IsTrue(url.RootElement.GetProperty("doNotInventUrls").GetBoolean());
        Assert.AreEqual("PACKAGE_FREEZE_FINAL_READINESS_CONDITIONAL_BLOCKERS_PENDING", final.RootElement.GetProperty("decision").GetString());
        Assert.IsFalse(final.RootElement.GetProperty("publicPackageFreezeReady").GetBoolean());
        Assert.IsFalse(final.RootElement.GetProperty("publicReleaseReady").GetBoolean());
        Assert.IsFalse(final.RootElement.GetProperty("chromeWebStoreReady").GetBoolean());
    }

    [TestMethod]
    public void ProtectedSurfacesRemainUnchangedAndDisabled()
    {
        foreach (var path in new[] { AssetRetryPath, UrlRetryPath, FinalPath, ConsolidatedPath })
        {
            using var doc = ReadJson(path);
            Assert.IsFalse(doc.RootElement.GetProperty("runtimeProductiveEnabled").GetBoolean(), path);
            Assert.IsFalse(doc.RootElement.GetProperty("providerCloudEnabled").GetBoolean(), path);
            Assert.IsFalse(doc.RootElement.GetProperty("filesystemEnabled").GetBoolean(), path);
            Assert.IsFalse(doc.RootElement.GetProperty("browserAutomationEnabled").GetBoolean(), path);
            Assert.IsFalse(doc.RootElement.GetProperty("capabilityUnlockEnabled").GetBoolean(), path);
            Assert.IsFalse(doc.RootElement.GetProperty("bridgeModified").GetBoolean(), path);
            Assert.IsFalse(doc.RootElement.GetProperty("cspModified").GetBoolean(), path);
            Assert.IsFalse(doc.RootElement.GetProperty("manifestModified").GetBoolean(), path);
            Assert.IsFalse(doc.RootElement.GetProperty("productFilesModified").GetBoolean(), path);
        }
    }
}
