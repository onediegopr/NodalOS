using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("ReleaseStoreGate")]
[TestCategory("M740")]
[TestCategory("M741")]
[TestCategory("M742")]
public sealed class NodalOsReleaseStoreGateM740M742Tests
{
    private const string M740ReportPath = "docs/reports/m740-public-release-owner-acceptance-gate.md";
    private const string M741ReportPath = "docs/reports/m741-chrome-web-store-submission-preflight-boundary.md";
    private const string M742ReportPath = "docs/reports/m742-release-store-preflight-next-decision.md";

    private const string PublicReleaseGatePath = "artifacts/agent-operations/m740/public-release-owner-acceptance-gate.json";
    private const string PublicReleasePhrasePath = "artifacts/agent-operations/m740/public-release-acceptance-phrase-check.json";
    private const string PublicReleaseRiskPath = "artifacts/agent-operations/m740/public-release-risk-boundary.json";
    private const string PublicReleaseNoGoUntilPath = "artifacts/agent-operations/m740/public-release-no-go-until-owner-acceptance.json";
    private const string NoPublicReleasePath = "artifacts/agent-operations/m740/no-public-release-proof.json";
    private const string M740GoNoGoPath = "artifacts/agent-operations/m740/m740-go-no-go.json";

    private const string StoreBoundaryPath = "artifacts/agent-operations/m741/chrome-web-store-submission-preflight-boundary.json";
    private const string StorePhrasePath = "artifacts/agent-operations/m741/store-submission-acceptance-phrase-check.json";
    private const string StoreRiskPath = "artifacts/agent-operations/m741/store-submission-risk-boundary.json";
    private const string StoreNoGoUntilPath = "artifacts/agent-operations/m741/store-submission-no-go-until-owner-acceptance.json";
    private const string StoreAssetsPath = "artifacts/agent-operations/m741/store-required-assets-boundary.json";
    private const string NoStoreSubmissionPath = "artifacts/agent-operations/m741/no-store-submission-proof.json";
    private const string M741GoNoGoPath = "artifacts/agent-operations/m741/m741-go-no-go.json";

    private const string NextDecisionPath = "artifacts/agent-operations/m742/release-store-preflight-next-decision.json";
    private const string AcceptanceSummaryPath = "artifacts/agent-operations/m742/release-store-acceptance-summary.json";
    private const string NoBlindPublicationPath = "artifacts/agent-operations/m742/no-blind-publication-proof.json";
    private const string NoSignedZipPath = "artifacts/agent-operations/m742/no-signed-public-zip-proof.json";
    private const string RuntimeDisabledPath = "artifacts/agent-operations/m742/runtime-provider-filesystem-browser-disabled-proof.json";
    private const string ProductBridgeCspPath = "artifacts/agent-operations/m742/product-bridge-csp-unchanged-proof.json";
    private const string NextMilestonePath = "artifacts/agent-operations/m742/next-milestone-recommendation.json";
    private const string M742GoNoGoPath = "artifacts/agent-operations/m742/m742-go-no-go.json";

    private const string ConsolidatedPath = "artifacts/agent-operations/m740-m742/public-release-store-preflight-go-no-go.json";

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
    public void M740ArtifactsExist()
    {
        foreach (var path in new[]
        {
            M740ReportPath, PublicReleaseGatePath, PublicReleasePhrasePath,
            PublicReleaseRiskPath, PublicReleaseNoGoUntilPath, NoPublicReleasePath, M740GoNoGoPath
        })
            Assert.IsTrue(File.Exists(FullPath(path)), path);
    }

    [TestMethod]
    public void M741ArtifactsExist()
    {
        foreach (var path in new[]
        {
            M741ReportPath, StoreBoundaryPath, StorePhrasePath, StoreRiskPath,
            StoreNoGoUntilPath, StoreAssetsPath, NoStoreSubmissionPath, M741GoNoGoPath
        })
            Assert.IsTrue(File.Exists(FullPath(path)), path);
    }

    [TestMethod]
    public void M742ArtifactsExist()
    {
        foreach (var path in new[]
        {
            M742ReportPath, NextDecisionPath, AcceptanceSummaryPath, NoBlindPublicationPath,
            NoSignedZipPath, RuntimeDisabledPath, ProductBridgeCspPath, NextMilestonePath,
            M742GoNoGoPath, ConsolidatedPath
        })
            Assert.IsTrue(File.Exists(FullPath(path)), path);
    }

    [TestMethod]
    public void PublicReleaseRequiresSeparateOwnerAcceptancePhrase()
    {
        using var doc = ReadJson(PublicReleasePhrasePath);
        Assert.AreEqual("ACEPTO PUBLIC RELEASE BAJO MI RESPONSABILIDAD", doc.RootElement.GetProperty("requiredPhrase").GetString());
        Assert.IsFalse(doc.RootElement.GetProperty("phrasePresentAsOwnerAcceptance").GetBoolean());
        Assert.IsTrue(doc.RootElement.GetProperty("phrasePresentOnlyAsFutureRequirement").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("publicReleaseOwnerAcceptanceReceived").GetBoolean());
    }

    [TestMethod]
    public void ChromeWebStoreRequiresSeparateOwnerAcceptancePhrase()
    {
        using var doc = ReadJson(StorePhrasePath);
        Assert.AreEqual("ACEPTO CHROME WEB STORE SUBMISSION BAJO MI RESPONSABILIDAD", doc.RootElement.GetProperty("requiredPhrase").GetString());
        Assert.IsFalse(doc.RootElement.GetProperty("phrasePresentAsOwnerAcceptance").GetBoolean());
        Assert.IsTrue(doc.RootElement.GetProperty("phrasePresentOnlyAsFutureRequirement").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("chromeWebStoreOwnerAcceptanceReceived").GetBoolean());
    }

    [TestMethod]
    public void ReleaseAndStoreRemainNoGoWithoutAcceptance()
    {
        using var doc = ReadJson(ConsolidatedPath);
        Assert.AreEqual("PUBLIC_RELEASE_OWNER_ACCEPTANCE_REQUIRED_STORE_OWNER_ACCEPTANCE_REQUIRED", doc.RootElement.GetProperty("decision").GetString());
        Assert.AreEqual("NO-GO", doc.RootElement.GetProperty("publicRelease").GetString());
        Assert.AreEqual("NO-GO", doc.RootElement.GetProperty("chromeWebStore").GetString());
    }

    [TestMethod]
    public void ThisBlockPerformsNoReleaseStoreSubmissionOrSignedZip()
    {
        using var doc = ReadJson(ConsolidatedPath);
        Assert.IsFalse(doc.RootElement.GetProperty("signedPublicZipCreated").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("actualPublicReleasePerformed").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("actualChromeWebStoreSubmissionPerformed").GetBoolean());
        Assert.IsTrue(doc.RootElement.GetProperty("noBlindPublication").GetBoolean());
    }

    [TestMethod]
    public void EvidenceAndRuntimeBoundariesRemainProtected()
    {
        using var doc = ReadJson(ConsolidatedPath);
        Assert.AreEqual("OWNER_ATTESTATION_NON_AUDITABLE", doc.RootElement.GetProperty("evidenceClass").GetString());
        Assert.IsFalse(doc.RootElement.GetProperty("evidenceInvented").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("runtimeProductiveEnabled").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("providerCloudEnabled").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("filesystemEnabled").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("browserAutomationEnabled").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("capabilityUnlockEnabled").GetBoolean());
    }

    [TestMethod]
    public void ProductBridgeCspRemainUnchanged()
    {
        using var doc = ReadJson(ProductBridgeCspPath);
        Assert.IsFalse(doc.RootElement.GetProperty("productFilesModified").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("bridgeModified").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("cspModified").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("manifestsModified").GetBoolean());
    }

    [TestMethod]
    public void NextMilestoneReflectsMissingAcceptancePhrases()
    {
        using var doc = ReadJson(NextMilestonePath);
        Assert.AreEqual(
            "M743-M745 Release Acceptance Required + Store Assets Boundary Stabilization",
            doc.RootElement.GetProperty("recommendedNextMilestone").GetString());
    }
}
