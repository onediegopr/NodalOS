using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("OwnerFinalFreeze")]
[TestCategory("M737")]
[TestCategory("M738")]
[TestCategory("M739")]
public sealed class NodalOsOwnerFinalFreezeM737M739Tests
{
    private const string M737ReportPath = "docs/reports/m737-owner-final-freeze-acceptance.md";
    private const string M738ReportPath = "docs/reports/m738-freeze-candidate-lock.md";
    private const string M739ReportPath = "docs/reports/m739-release-still-protected-decision.md";

    private const string OwnerAcceptancePath = "artifacts/agent-operations/m737/owner-final-freeze-acceptance.json";
    private const string PhraseCheckPath = "artifacts/agent-operations/m737/final-freeze-acceptance-phrase-check.json";
    private const string NonAuditablePath = "artifacts/agent-operations/m737/owner-attestation-non-auditable-confirmation.json";
    private const string EvidenceNotProvidedPath = "artifacts/agent-operations/m737/evidence-not-provided-confirmation.json";
    private const string NoEvidenceInventionPath = "artifacts/agent-operations/m737/no-evidence-invention-proof.json";
    private const string M737GoNoGoPath = "artifacts/agent-operations/m737/m737-go-no-go.json";

    private const string FreezeLockPath = "artifacts/agent-operations/m738/freeze-candidate-lock.json";
    private const string FreezeScopePath = "artifacts/agent-operations/m738/freeze-lock-scope.json";
    private const string FreezeContentsPath = "artifacts/agent-operations/m738/freeze-lock-contents-manifest.json";
    private const string FreezeExclusionsPath = "artifacts/agent-operations/m738/freeze-lock-exclusions.json";
    private const string NonPublicablePath = "artifacts/agent-operations/m738/non-publicable-freeze-lock-proof.json";
    private const string M738GoNoGoPath = "artifacts/agent-operations/m738/m738-go-no-go.json";

    private const string ReleaseProtectedPath = "artifacts/agent-operations/m739/release-still-protected-decision.json";
    private const string PublicReleaseNoGoPath = "artifacts/agent-operations/m739/public-release-no-go-proof.json";
    private const string ChromeWebStoreNoGoPath = "artifacts/agent-operations/m739/chrome-web-store-no-go-proof.json";
    private const string PublicReleaseAcceptancePath = "artifacts/agent-operations/m739/public-release-owner-acceptance-required.json";
    private const string StoreAcceptancePath = "artifacts/agent-operations/m739/store-submission-owner-acceptance-required.json";
    private const string RuntimeDisabledPath = "artifacts/agent-operations/m739/runtime-provider-filesystem-browser-disabled-proof.json";
    private const string NextMilestonePath = "artifacts/agent-operations/m739/next-milestone-recommendation.json";
    private const string M739GoNoGoPath = "artifacts/agent-operations/m739/m739-go-no-go.json";

    private const string ConsolidatedPath = "artifacts/agent-operations/m737-m739/owner-final-freeze-lock-go-no-go.json";

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
    public void M737ArtifactsExist()
    {
        foreach (var path in new[]
        {
            M737ReportPath, OwnerAcceptancePath, PhraseCheckPath, NonAuditablePath,
            EvidenceNotProvidedPath, NoEvidenceInventionPath, M737GoNoGoPath
        })
            Assert.IsTrue(File.Exists(FullPath(path)), path);
    }

    [TestMethod]
    public void M738ArtifactsExist()
    {
        foreach (var path in new[]
        {
            M738ReportPath, FreezeLockPath, FreezeScopePath, FreezeContentsPath,
            FreezeExclusionsPath, NonPublicablePath, M738GoNoGoPath
        })
            Assert.IsTrue(File.Exists(FullPath(path)), path);
    }

    [TestMethod]
    public void M739ArtifactsExist()
    {
        foreach (var path in new[]
        {
            M739ReportPath, ReleaseProtectedPath, PublicReleaseNoGoPath,
            ChromeWebStoreNoGoPath, PublicReleaseAcceptancePath, StoreAcceptancePath,
            RuntimeDisabledPath, NextMilestonePath, M739GoNoGoPath, ConsolidatedPath
        })
            Assert.IsTrue(File.Exists(FullPath(path)), path);
    }

    [TestMethod]
    public void OwnerFinalFreezeAcceptancePhraseIsRequiredAndPresent()
    {
        using var doc = ReadJson(PhraseCheckPath);
        Assert.AreEqual("ACEPTO FREEZE FINAL CON OWNER ATTESTATION NO AUDITABLE", doc.RootElement.GetProperty("requiredPhrase").GetString());
        Assert.AreEqual("OWNER_FINAL_FREEZE_ACCEPTANCE_REQUIRED", doc.RootElement.GetProperty("missingPhraseDecision").GetString());
        Assert.IsTrue(doc.RootElement.GetProperty("phrasePresentInM737M739Input").GetBoolean());
        Assert.IsTrue(doc.RootElement.GetProperty("ownerFinalFreezeAcceptanceReceived").GetBoolean());
    }

    [TestMethod]
    public void FreezeCandidateLockIsAllowedOnlyAfterOwnerAcceptance()
    {
        using var doc = ReadJson(ConsolidatedPath);
        Assert.AreEqual("OWNER_FINAL_FREEZE_ACCEPTANCE_RECEIVED_FREEZE_CANDIDATE_LOCK_READY", doc.RootElement.GetProperty("decision").GetString());
        Assert.IsTrue(doc.RootElement.GetProperty("ownerFinalFreezeAcceptanceReceived").GetBoolean());
        Assert.AreEqual("GO", doc.RootElement.GetProperty("freezeCandidateLock").GetString());
        Assert.AreEqual("OWNER-ACCEPTED CONDITIONAL LOCK", doc.RootElement.GetProperty("publicPackageFreezeFinal").GetString());
    }

    [TestMethod]
    public void EvidenceClassRemainsNonAuditableAndNotInvented()
    {
        using var doc = ReadJson(ConsolidatedPath);
        Assert.AreEqual("OWNER_ATTESTATION_NON_AUDITABLE", doc.RootElement.GetProperty("evidenceClass").GetString());
        Assert.IsFalse(doc.RootElement.GetProperty("screenshotsProvided").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("logsProvided").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("runtimeDevtoolsEvidenceProvided").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("permissionWarningsEvidenceProvided").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("evidenceInvented").GetBoolean());
    }

    [TestMethod]
    public void PublicReleaseAndChromeWebStoreRequireSeparateAcceptance()
    {
        using var release = ReadJson(PublicReleaseAcceptancePath);
        using var store = ReadJson(StoreAcceptancePath);
        Assert.IsTrue(release.RootElement.GetProperty("publicReleaseOwnerAcceptanceRequired").GetBoolean());
        Assert.IsFalse(release.RootElement.GetProperty("acceptanceReceived").GetBoolean());
        Assert.IsFalse(release.RootElement.GetProperty("publicReleaseReady").GetBoolean());
        Assert.IsTrue(store.RootElement.GetProperty("storeSubmissionOwnerAcceptanceRequired").GetBoolean());
        Assert.IsFalse(store.RootElement.GetProperty("acceptanceReceived").GetBoolean());
        Assert.IsFalse(store.RootElement.GetProperty("chromeWebStoreReady").GetBoolean());
    }

    [TestMethod]
    public void ReleaseStoreRuntimeProductAndBridgeBoundariesRemainProtected()
    {
        using var doc = ReadJson(ConsolidatedPath);
        Assert.IsFalse(doc.RootElement.GetProperty("publicReleaseReady").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("chromeWebStoreReady").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("runtimeProductiveEnabled").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("providerCloudEnabled").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("filesystemEnabled").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("browserAutomationEnabled").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("capabilityUnlockEnabled").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("productFilesModified").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("bridgeModified").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("cspModified").GetBoolean());
        Assert.IsTrue(doc.RootElement.GetProperty("noBlindRelease").GetBoolean());
        Assert.IsTrue(doc.RootElement.GetProperty("noStoreSubmission").GetBoolean());
    }

    [TestMethod]
    public void NextMilestoneIsPublicReleaseOwnerAcceptanceBoundary()
    {
        using var doc = ReadJson(NextMilestonePath);
        Assert.AreEqual(
            "M740-M742 Public Release Owner Acceptance Gate + Store Submission Preflight Boundary",
            doc.RootElement.GetProperty("recommendedNextMilestone").GetString());
    }
}
