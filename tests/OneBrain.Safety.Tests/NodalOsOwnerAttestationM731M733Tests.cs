using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("OwnerAttestation")]
[TestCategory("M731")]
[TestCategory("M732")]
[TestCategory("M733")]
public sealed class NodalOsOwnerAttestationM731M733Tests
{
    private const string M731ReportPath = "docs/reports/m731-owner-attestation-intake.md";
    private const string M732ReportPath = "docs/reports/m732-owner-risk-acceptance-register.md";
    private const string M733ReportPath = "docs/reports/m733-conditional-freeze-prep-decision.md";

    private const string OwnerAttestationIntakePath = "artifacts/agent-operations/m731/owner-attestation-intake.json";
    private const string ManualQaOwnerAttestationPath = "artifacts/agent-operations/m731/manual-qa-owner-attestation.json";
    private const string NonAuditableEvidencePath = "artifacts/agent-operations/m731/non-auditable-evidence-classification.json";
    private const string MissingEvidenceOwnerDecisionPath = "artifacts/agent-operations/m731/missing-evidence-owner-decision.json";
    private const string M731GoNoGoPath = "artifacts/agent-operations/m731/m731-go-no-go.json";

    private const string RiskAcceptancePath = "artifacts/agent-operations/m732/owner-risk-acceptance-register.json";
    private const string PublicReleaseRiskPath = "artifacts/agent-operations/m732/public-release-risk-boundary.json";
    private const string ChromeWebStoreRiskPath = "artifacts/agent-operations/m732/chrome-web-store-risk-boundary.json";
    private const string FreezeRiskPath = "artifacts/agent-operations/m732/freeze-risk-boundary.json";
    private const string NoEvidenceNoInventionPath = "artifacts/agent-operations/m732/no-evidence-no-invention-proof.json";
    private const string M732GoNoGoPath = "artifacts/agent-operations/m732/m732-go-no-go.json";

    private const string ConditionalFreezeDecisionPath = "artifacts/agent-operations/m733/conditional-freeze-prep-decision.json";
    private const string OwnerAttestedFreezeReadinessPath = "artifacts/agent-operations/m733/owner-attested-freeze-readiness.json";
    private const string PublicPackageFreezeConditionalPath = "artifacts/agent-operations/m733/public-package-freeze-conditional-go-no-go.json";
    private const string PublicReleaseProtectionPath = "artifacts/agent-operations/m733/public-release-protection-proof.json";
    private const string ChromeWebStoreProtectionPath = "artifacts/agent-operations/m733/chrome-web-store-protection-proof.json";
    private const string NextMilestonePath = "artifacts/agent-operations/m733/next-milestone-recommendation.json";
    private const string M733GoNoGoPath = "artifacts/agent-operations/m733/m733-go-no-go.json";

    private const string ConsolidatedPath = "artifacts/agent-operations/m731-m733/owner-attestation-risk-acceptance-go-no-go.json";

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
    public void M731ArtifactsExist()
    {
        foreach (var path in new[]
        {
            M731ReportPath, OwnerAttestationIntakePath, ManualQaOwnerAttestationPath,
            NonAuditableEvidencePath, MissingEvidenceOwnerDecisionPath, M731GoNoGoPath
        })
            Assert.IsTrue(File.Exists(FullPath(path)), path);
    }

    [TestMethod]
    public void M732ArtifactsExist()
    {
        foreach (var path in new[]
        {
            M732ReportPath, RiskAcceptancePath, PublicReleaseRiskPath,
            ChromeWebStoreRiskPath, FreezeRiskPath, NoEvidenceNoInventionPath, M732GoNoGoPath
        })
            Assert.IsTrue(File.Exists(FullPath(path)), path);
    }

    [TestMethod]
    public void M733ArtifactsExist()
    {
        foreach (var path in new[]
        {
            M733ReportPath, ConditionalFreezeDecisionPath, OwnerAttestedFreezeReadinessPath,
            PublicPackageFreezeConditionalPath, PublicReleaseProtectionPath,
            ChromeWebStoreProtectionPath, NextMilestonePath, M733GoNoGoPath, ConsolidatedPath
        })
            Assert.IsTrue(File.Exists(FullPath(path)), path);
    }

    [TestMethod]
    public void OwnerAttestationIsAcceptedAsNonAuditable()
    {
        using var doc = ReadJson(OwnerAttestationIntakePath);
        Assert.IsTrue(doc.RootElement.GetProperty("ownerAttestationReceived").GetBoolean());
        Assert.AreEqual("OWNER_ATTESTATION_NON_AUDITABLE", doc.RootElement.GetProperty("evidenceClass").GetString());
        Assert.IsFalse(doc.RootElement.GetProperty("screenshotsProvided").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("logsProvided").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("evidenceInvented").GetBoolean());
        Assert.IsTrue(doc.RootElement.GetProperty("continueRequestedByOwner").GetBoolean());
    }

    [TestMethod]
    public void NoEvidenceIsInvented()
    {
        using var doc = ReadJson(NoEvidenceNoInventionPath);
        Assert.IsFalse(doc.RootElement.GetProperty("evidenceInvented").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("screenshotsInvented").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("runtimeDevtoolsPassInvented").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("permissionWarningEvidenceInvented").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("logsInvented").GetBoolean());
    }

    [TestMethod]
    public void ReleaseAndStoreRemainProtected()
    {
        using var release = ReadJson(PublicReleaseProtectionPath);
        using var store = ReadJson(ChromeWebStoreProtectionPath);
        Assert.IsFalse(release.RootElement.GetProperty("publicReleaseReady").GetBoolean());
        Assert.IsTrue(release.RootElement.GetProperty("publicReleaseProtected").GetBoolean());
        Assert.IsFalse(store.RootElement.GetProperty("chromeWebStoreReady").GetBoolean());
        Assert.IsTrue(store.RootElement.GetProperty("chromeWebStoreProtected").GetBoolean());
    }

    [TestMethod]
    public void CapabilitiesProductFilesBridgeAndCspRemainProtected()
    {
        foreach (var path in new[] { M731GoNoGoPath, M732GoNoGoPath, M733GoNoGoPath, ConsolidatedPath })
        {
            using var doc = ReadJson(path);
            Assert.IsFalse(doc.RootElement.GetProperty("runtimeProductiveEnabled").GetBoolean(), path);
            Assert.IsFalse(doc.RootElement.GetProperty("providerCloudEnabled").GetBoolean(), path);
            Assert.IsFalse(doc.RootElement.GetProperty("filesystemEnabled").GetBoolean(), path);
            Assert.IsFalse(doc.RootElement.GetProperty("browserAutomationEnabled").GetBoolean(), path);
            Assert.IsFalse(doc.RootElement.GetProperty("capabilityUnlockEnabled").GetBoolean(), path);
            Assert.IsFalse(doc.RootElement.GetProperty("productFilesModified").GetBoolean(), path);
            Assert.IsFalse(doc.RootElement.GetProperty("bridgeModified").GetBoolean(), path);
            Assert.IsFalse(doc.RootElement.GetProperty("cspModified").GetBoolean(), path);
        }
    }

    [TestMethod]
    public void FinalDecisionAllowsConditionalFreezePrepOnly()
    {
        using var doc = ReadJson(ConsolidatedPath);
        Assert.AreEqual("OWNER_ATTESTATION_ACCEPTED_CONDITIONAL_FREEZE_PREP_READY", doc.RootElement.GetProperty("decision").GetString());
        Assert.AreEqual("GO", doc.RootElement.GetProperty("conditionalPackageFreezePrep").GetString());
        Assert.AreEqual("CONDITIONAL", doc.RootElement.GetProperty("publicPackageFreezeFinal").GetString());
        Assert.IsFalse(doc.RootElement.GetProperty("publicPackageFreezeFinalReady").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("publicReleaseReady").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("chromeWebStoreReady").GetBoolean());
    }
}
