using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("ManualP0Input")]
[TestCategory("M728")]
[TestCategory("M729")]
[TestCategory("M730")]
public sealed class NodalOsManualP0InputM728M730Tests
{
    private const string M728ReportPath = "docs/reports/m728-manual-p0-input-gate.md";
    private const string M729ReportPath = "docs/reports/m729-store-url-screenshot-evidence-collection.md";
    private const string M730ReportPath = "docs/reports/m730-freeze-unblock-decision.md";

    private const string ManualP0InputGatePath = "artifacts/agent-operations/m728/manual-p0-input-gate.json";
    private const string ManualInputReceivedStatusPath = "artifacts/agent-operations/m728/manual-input-received-status.json";
    private const string PrivacySupportInputStatusPath = "artifacts/agent-operations/m728/privacy-support-input-status.json";
    private const string ScreenshotInputStatusPath = "artifacts/agent-operations/m728/screenshot-input-status.json";
    private const string HumanChromeEvidenceInputStatusPath = "artifacts/agent-operations/m728/human-chrome-evidence-input-status.json";
    private const string P0InputGapRegisterPath = "artifacts/agent-operations/m728/p0-input-gap-register.json";
    private const string M728GoNoGoPath = "artifacts/agent-operations/m728/m728-go-no-go.json";

    private const string StoreCollectionPath = "artifacts/agent-operations/m729/store-url-screenshot-evidence-collection.json";
    private const string PrivacyUrlEvidencePath = "artifacts/agent-operations/m729/privacy-url-evidence.json";
    private const string SupportUrlEvidencePath = "artifacts/agent-operations/m729/support-url-evidence.json";
    private const string DocsUrlEvidencePath = "artifacts/agent-operations/m729/docs-url-evidence.json";
    private const string ScreenshotEvidenceRegisterPath = "artifacts/agent-operations/m729/screenshot-evidence-register.json";
    private const string HumanChromeEvidenceRegisterPath = "artifacts/agent-operations/m729/human-chrome-evidence-register.json";
    private const string EvidenceRedactionProofPath = "artifacts/agent-operations/m729/evidence-redaction-proof.json";
    private const string M729GoNoGoPath = "artifacts/agent-operations/m729/m729-go-no-go.json";

    private const string FreezeUnblockDecisionPath = "artifacts/agent-operations/m730/freeze-unblock-decision.json";
    private const string P0UnblockStatusPath = "artifacts/agent-operations/m730/p0-unblock-status.json";
    private const string FinalMustProvideChecklistPath = "artifacts/agent-operations/m730/final-manual-must-provide-checklist.json";
    private const string PublicPackageFreezePath = "artifacts/agent-operations/m730/public-package-freeze-go-no-go.json";
    private const string PublicReleaseNoGoPath = "artifacts/agent-operations/m730/public-release-no-go-proof.json";
    private const string ChromeWebStoreNoGoPath = "artifacts/agent-operations/m730/chrome-web-store-no-go-proof.json";
    private const string NextMilestonePath = "artifacts/agent-operations/m730/next-milestone-recommendation.json";
    private const string M730GoNoGoPath = "artifacts/agent-operations/m730/m730-go-no-go.json";

    private const string ConsolidatedPath = "artifacts/agent-operations/m728-m730/manual-p0-input-freeze-go-no-go.json";

    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(Environment.CurrentDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "OneBrain.slnx")))
            dir = dir.Parent;
        return dir?.FullName ?? Environment.CurrentDirectory;
    }

    private static string FullPath(string relativePath) =>
        Path.Combine(RepoRoot(), relativePath);

    private static JsonDocument ReadJson(string relativePath) =>
        JsonDocument.Parse(File.ReadAllText(FullPath(relativePath)));

    [TestMethod]
    public void M728ArtifactsExist()
    {
        foreach (var path in new[]
        {
            M728ReportPath, ManualP0InputGatePath, ManualInputReceivedStatusPath,
            PrivacySupportInputStatusPath, ScreenshotInputStatusPath,
            HumanChromeEvidenceInputStatusPath, P0InputGapRegisterPath, M728GoNoGoPath
        })
            Assert.IsTrue(File.Exists(FullPath(path)), path);
    }

    [TestMethod]
    public void M729ArtifactsExist()
    {
        foreach (var path in new[]
        {
            M729ReportPath, StoreCollectionPath, PrivacyUrlEvidencePath,
            SupportUrlEvidencePath, DocsUrlEvidencePath, ScreenshotEvidenceRegisterPath,
            HumanChromeEvidenceRegisterPath, EvidenceRedactionProofPath, M729GoNoGoPath
        })
            Assert.IsTrue(File.Exists(FullPath(path)), path);
    }

    [TestMethod]
    public void M730ArtifactsExist()
    {
        foreach (var path in new[]
        {
            M730ReportPath, FreezeUnblockDecisionPath, P0UnblockStatusPath,
            FinalMustProvideChecklistPath, PublicPackageFreezePath, PublicReleaseNoGoPath,
            ChromeWebStoreNoGoPath, NextMilestonePath, M730GoNoGoPath, ConsolidatedPath
        })
            Assert.IsTrue(File.Exists(FullPath(path)), path);
    }

    [TestMethod]
    public void ConsolidatedDecisionIsAllowedAndBlocked()
    {
        using var doc = ReadJson(ConsolidatedPath);
        var decision = doc.RootElement.GetProperty("decision").GetString();
        CollectionAssert.Contains(new[]
        {
            "MANUAL_P0_INPUT_RECEIVED_FREEZE_READY",
            "MANUAL_P0_INPUT_REQUIRED_BLOCKED",
            "MANUAL_P0_INPUT_REMEDIATION_REQUIRED"
        }, decision);

        Assert.AreEqual("MANUAL_P0_INPUT_REQUIRED_BLOCKED", decision);
        Assert.AreEqual("NODAL OS", doc.RootElement.GetProperty("activeProject").GetString());
        Assert.IsTrue(doc.RootElement.GetProperty("nodrixOutOfScope").GetBoolean());
    }

    [TestMethod]
    public void MissingP0InputsKeepFreezeNoGo()
    {
        using var doc = ReadJson(ConsolidatedPath);
        Assert.IsFalse(doc.RootElement.GetProperty("manualInputReceived").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("privacyUrlReady").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("supportUrlReady").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("screenshotsReady").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("humanEvidenceReady").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("p0BlockersClosed").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("publicPackageFreezeReady").GetBoolean());
    }

    [TestMethod]
    public void MissingUrlsKeepFreezeAndStoreNoGo()
    {
        using var doc = ReadJson(ConsolidatedPath);
        using var privacy = ReadJson(PrivacyUrlEvidencePath);
        using var support = ReadJson(SupportUrlEvidencePath);
        Assert.IsFalse(doc.RootElement.GetProperty("publicPackageFreezeReady").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("chromeWebStoreReady").GetBoolean());
        Assert.IsFalse(privacy.RootElement.GetProperty("ready").GetBoolean());
        Assert.IsFalse(support.RootElement.GetProperty("ready").GetBoolean());
    }

    [TestMethod]
    public void MissingScreenshotsAndEvidenceKeepFreezeNoGo()
    {
        using var doc = ReadJson(ConsolidatedPath);
        using var screenshots = ReadJson(ScreenshotEvidenceRegisterPath);
        using var evidence = ReadJson(HumanChromeEvidenceRegisterPath);
        Assert.IsFalse(doc.RootElement.GetProperty("screenshotsReady").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("humanEvidenceReady").GetBoolean());
        Assert.IsFalse(screenshots.RootElement.GetProperty("screenshotsReady").GetBoolean());
        Assert.IsFalse(evidence.RootElement.GetProperty("humanEvidenceReady").GetBoolean());
    }

    [TestMethod]
    public void PublicReleaseAndCapabilitiesRemainNoGoAndDisabled()
    {
        foreach (var path in new[] { M728GoNoGoPath, M729GoNoGoPath, M730GoNoGoPath, ConsolidatedPath })
        {
            using var doc = ReadJson(path);
            Assert.IsFalse(doc.RootElement.GetProperty("publicReleaseReady").GetBoolean(), path);
            Assert.IsFalse(doc.RootElement.GetProperty("chromeWebStoreReady").GetBoolean(), path);
            Assert.IsFalse(doc.RootElement.GetProperty("runtimeProductiveEnabled").GetBoolean(), path);
            Assert.IsFalse(doc.RootElement.GetProperty("providerCloudEnabled").GetBoolean(), path);
            Assert.IsFalse(doc.RootElement.GetProperty("filesystemEnabled").GetBoolean(), path);
            Assert.IsFalse(doc.RootElement.GetProperty("browserAutomationEnabled").GetBoolean(), path);
            Assert.IsFalse(doc.RootElement.GetProperty("capabilityUnlockEnabled").GetBoolean(), path);
            Assert.IsFalse(doc.RootElement.GetProperty("bridgeModified").GetBoolean(), path);
            Assert.IsFalse(doc.RootElement.GetProperty("cspModified").GetBoolean(), path);
        }
    }
}
