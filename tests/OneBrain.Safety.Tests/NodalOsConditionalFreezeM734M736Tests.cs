using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("ConditionalFreeze")]
[TestCategory("M734")]
[TestCategory("M735")]
[TestCategory("M736")]
public sealed class NodalOsConditionalFreezeM734M736Tests
{
    private const string M734ReportPath = "docs/reports/m734-conditional-freeze-candidate-prep.md";
    private const string M735ReportPath = "docs/reports/m735-owner-final-acceptance-gate.md";
    private const string M736ReportPath = "docs/reports/m736-release-store-protection-next-decision.md";

    private const string ConditionalFreezePrepPath = "artifacts/agent-operations/m734/conditional-freeze-candidate-prep.json";
    private const string FreezeCandidateScopePath = "artifacts/agent-operations/m734/freeze-candidate-scope.json";
    private const string FreezeCandidateContentsPath = "artifacts/agent-operations/m734/freeze-candidate-contents-manifest.json";
    private const string FreezeCandidateExclusionsPath = "artifacts/agent-operations/m734/freeze-candidate-exclusions.json";
    private const string OwnerAttestationBasisPath = "artifacts/agent-operations/m734/owner-attestation-basis.json";
    private const string NonAuditableBoundaryPath = "artifacts/agent-operations/m734/non-auditable-evidence-boundary.json";
    private const string M734GoNoGoPath = "artifacts/agent-operations/m734/m734-go-no-go.json";

    private const string OwnerFinalAcceptanceGatePath = "artifacts/agent-operations/m735/owner-final-acceptance-gate.json";
    private const string FinalAcceptanceFieldsPath = "artifacts/agent-operations/m735/final-acceptance-required-fields.json";
    private const string PublicReleaseOwnerBoundaryPath = "artifacts/agent-operations/m735/public-release-owner-acceptance-boundary.json";
    private const string ChromeWebStoreOwnerBoundaryPath = "artifacts/agent-operations/m735/chrome-web-store-owner-acceptance-boundary.json";
    private const string FreezeFinalOwnerBoundaryPath = "artifacts/agent-operations/m735/freeze-final-owner-acceptance-boundary.json";
    private const string M735GoNoGoPath = "artifacts/agent-operations/m735/m735-go-no-go.json";

    private const string ReleaseStoreProtectionPath = "artifacts/agent-operations/m736/release-store-protection-next-decision.json";
    private const string PublicReleaseNoGoPath = "artifacts/agent-operations/m736/public-release-no-go-proof.json";
    private const string ChromeWebStoreNoGoPath = "artifacts/agent-operations/m736/chrome-web-store-no-go-proof.json";
    private const string NoBlindFreezePath = "artifacts/agent-operations/m736/no-blind-freeze-proof.json";
    private const string NoEvidenceInventionPath = "artifacts/agent-operations/m736/no-evidence-invention-proof.json";
    private const string RuntimeDisabledPath = "artifacts/agent-operations/m736/runtime-provider-filesystem-browser-disabled-proof.json";
    private const string NextMilestonePath = "artifacts/agent-operations/m736/next-milestone-recommendation.json";
    private const string M736GoNoGoPath = "artifacts/agent-operations/m736/m736-go-no-go.json";

    private const string ConsolidatedPath = "artifacts/agent-operations/m734-m736/conditional-freeze-candidate-go-no-go.json";

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
    public void M734ArtifactsExist()
    {
        foreach (var path in new[]
        {
            M734ReportPath, ConditionalFreezePrepPath, FreezeCandidateScopePath,
            FreezeCandidateContentsPath, FreezeCandidateExclusionsPath,
            OwnerAttestationBasisPath, NonAuditableBoundaryPath, M734GoNoGoPath
        })
            Assert.IsTrue(File.Exists(FullPath(path)), path);
    }

    [TestMethod]
    public void M735ArtifactsExist()
    {
        foreach (var path in new[]
        {
            M735ReportPath, OwnerFinalAcceptanceGatePath, FinalAcceptanceFieldsPath,
            PublicReleaseOwnerBoundaryPath, ChromeWebStoreOwnerBoundaryPath,
            FreezeFinalOwnerBoundaryPath, M735GoNoGoPath
        })
            Assert.IsTrue(File.Exists(FullPath(path)), path);
    }

    [TestMethod]
    public void M736ArtifactsExist()
    {
        foreach (var path in new[]
        {
            M736ReportPath, ReleaseStoreProtectionPath, PublicReleaseNoGoPath,
            ChromeWebStoreNoGoPath, NoBlindFreezePath, NoEvidenceInventionPath,
            RuntimeDisabledPath, NextMilestonePath, M736GoNoGoPath, ConsolidatedPath
        })
            Assert.IsTrue(File.Exists(FullPath(path)), path);
    }

    [TestMethod]
    public void OwnerAttestationAndEvidenceClassRemainNonAuditable()
    {
        using var doc = ReadJson(ConsolidatedPath);
        Assert.IsTrue(doc.RootElement.GetProperty("ownerAttestationReceived").GetBoolean());
        Assert.AreEqual("OWNER_ATTESTATION_NON_AUDITABLE", doc.RootElement.GetProperty("evidenceClass").GetString());
        Assert.IsFalse(doc.RootElement.GetProperty("screenshotsProvided").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("logsProvided").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("runtimeDevtoolsEvidenceProvided").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("permissionWarningsEvidenceProvided").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("evidenceInvented").GetBoolean());
    }

    [TestMethod]
    public void ConditionalFreezePrepIsAllowedButFinalFreezeIsPendingOwnerAcceptance()
    {
        using var doc = ReadJson(ConsolidatedPath);
        Assert.AreEqual("CONDITIONAL_FREEZE_CANDIDATE_PREP_READY_OWNER_FINAL_ACCEPTANCE_PENDING", doc.RootElement.GetProperty("decision").GetString());
        Assert.AreEqual("GO", doc.RootElement.GetProperty("conditionalFreezeCandidatePrep").GetString());
        Assert.IsTrue(doc.RootElement.GetProperty("conditionalFreezeCandidatePrepReady").GetBoolean());
        Assert.AreEqual("CONDITIONAL", doc.RootElement.GetProperty("publicPackageFreezeFinal").GetString());
        Assert.IsFalse(doc.RootElement.GetProperty("publicPackageFreezeFinalReady").GetBoolean());
    }

    [TestMethod]
    public void PublicReleaseAndChromeWebStoreRemainNoGo()
    {
        using var release = ReadJson(PublicReleaseNoGoPath);
        using var store = ReadJson(ChromeWebStoreNoGoPath);
        Assert.IsFalse(release.RootElement.GetProperty("publicReleaseReady").GetBoolean());
        Assert.AreEqual("NO-GO", release.RootElement.GetProperty("publicRelease").GetString());
        Assert.IsFalse(store.RootElement.GetProperty("chromeWebStoreReady").GetBoolean());
        Assert.AreEqual("NO-GO", store.RootElement.GetProperty("chromeWebStore").GetString());
    }

    [TestMethod]
    public void RuntimeProviderFilesystemBrowserCapabilityRemainDisabled()
    {
        using var doc = ReadJson(RuntimeDisabledPath);
        Assert.IsFalse(doc.RootElement.GetProperty("runtimeProductiveEnabled").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("providerCloudEnabled").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("filesystemEnabled").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("browserAutomationEnabled").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("capabilityUnlockEnabled").GetBoolean());
    }

    [TestMethod]
    public void ProductFilesBridgeCspAndBlindReleaseRemainProtected()
    {
        using var doc = ReadJson(ConsolidatedPath);
        using var noBlind = ReadJson(NoBlindFreezePath);
        Assert.IsFalse(doc.RootElement.GetProperty("productFilesModified").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("bridgeModified").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("cspModified").GetBoolean());
        Assert.IsTrue(doc.RootElement.GetProperty("noBlindRelease").GetBoolean());
        Assert.IsTrue(noBlind.RootElement.GetProperty("noBlindFreeze").GetBoolean());
    }

    [TestMethod]
    public void NextMilestoneIsOwnerFinalFreezeAcceptanceGate()
    {
        using var doc = ReadJson(NextMilestonePath);
        Assert.AreEqual(
            "M737-M739 Owner Final Freeze Acceptance + Freeze Candidate Lock + Release Still Protected",
            doc.RootElement.GetProperty("recommendedNextMilestone").GetString());
    }
}
