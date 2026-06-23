using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("PublicPackageFreezePrep")]
[TestCategory("M704")]
[TestCategory("M705")]
[TestCategory("M706")]
public sealed class NodalOsPublicPackageFreezePrepM704M706Tests
{
    private const string M704ReportPath = "docs/reports/m704-evidence-screenshot-asset-completion.md";
    private const string M705ReportPath = "docs/reports/m705-public-package-candidate-freeze-prep.md";
    private const string M706ReportPath = "docs/reports/m706-final-pre-submission-gate.md";

    private const string EvidenceCompletionPath = "artifacts/agent-operations/m704/evidence-screenshot-asset-completion.json";
    private const string ManualQaStatusPath = "artifacts/agent-operations/m704/manual-qa-evidence-status.json";
    private const string ScreenshotStatusPath = "artifacts/agent-operations/m704/screenshot-capture-status.json";
    private const string RuntimeDevtoolsStatusPath = "artifacts/agent-operations/m704/runtime-devtools-capture-status.json";
    private const string PermissionWarningStatusPath = "artifacts/agent-operations/m704/permission-warning-capture-status.json";
    private const string RedactionFinalCheckPath = "artifacts/agent-operations/m704/evidence-redaction-final-check.json";
    private const string M704GoNoGoPath = "artifacts/agent-operations/m704/m704-go-no-go.json";

    private const string FreezePrepPath = "artifacts/agent-operations/m705/public-package-candidate-freeze-prep.json";
    private const string StagingFileAuditPath = "artifacts/agent-operations/m705/public-staging-final-file-audit.json";
    private const string StagingManifestAuditPath = "artifacts/agent-operations/m705/public-staging-manifest-final-audit.json";
    private const string ExclusionAuditPath = "artifacts/agent-operations/m705/public-package-exclusion-audit.json";
    private const string ChecksumPlanPath = "artifacts/agent-operations/m705/package-freeze-checksum-plan.json";
    private const string FreezeRiskRegisterPath = "artifacts/agent-operations/m705/package-freeze-risk-register.json";
    private const string M705GoNoGoPath = "artifacts/agent-operations/m705/m705-go-no-go.json";

    private const string FinalGatePath = "artifacts/agent-operations/m706/final-pre-submission-gate.json";
    private const string StoreAssetsGapPath = "artifacts/agent-operations/m706/store-assets-final-gap-register.json";
    private const string PrivacySupportGapPath = "artifacts/agent-operations/m706/privacy-support-final-gap-register.json";
    private const string ExternalAuditPackPath = "artifacts/agent-operations/m706/pre-submission-external-audit-pack.json";
    private const string FreezeGoNoGoPath = "artifacts/agent-operations/m706/public-package-freeze-go-no-go.json";
    private const string PublicReleaseNoGoPath = "artifacts/agent-operations/m706/public-release-no-go-proof.json";
    private const string ChromeWebStoreNoGoPath = "artifacts/agent-operations/m706/chrome-web-store-no-go-proof.json";
    private const string NextMilestonePath = "artifacts/agent-operations/m706/next-milestone-recommendation.json";
    private const string M706GoNoGoPath = "artifacts/agent-operations/m706/m706-go-no-go.json";

    private const string ConsolidatedPath = "artifacts/agent-operations/m704-m706/public-package-freeze-prep-go-no-go.json";
    private const string StagingManifestPath = "artifacts/manual-qa/public-variant-staging/manifest.json";

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
    public void M704ArtifactsExist()
    {
        foreach (var path in new[]
        {
            M704ReportPath, EvidenceCompletionPath, ManualQaStatusPath, ScreenshotStatusPath,
            RuntimeDevtoolsStatusPath, PermissionWarningStatusPath, RedactionFinalCheckPath, M704GoNoGoPath
        })
            Assert.IsTrue(File.Exists(FullPath(path)), path);
    }

    [TestMethod]
    public void M705ArtifactsExist()
    {
        foreach (var path in new[]
        {
            M705ReportPath, FreezePrepPath, StagingFileAuditPath, StagingManifestAuditPath,
            ExclusionAuditPath, ChecksumPlanPath, FreezeRiskRegisterPath, M705GoNoGoPath
        })
            Assert.IsTrue(File.Exists(FullPath(path)), path);
    }

    [TestMethod]
    public void M706ArtifactsExist()
    {
        foreach (var path in new[]
        {
            M706ReportPath, FinalGatePath, StoreAssetsGapPath, PrivacySupportGapPath,
            ExternalAuditPackPath, FreezeGoNoGoPath, PublicReleaseNoGoPath,
            ChromeWebStoreNoGoPath, NextMilestonePath, M706GoNoGoPath, ConsolidatedPath
        })
            Assert.IsTrue(File.Exists(FullPath(path)), path);
    }

    [TestMethod]
    public void ConsolidatedDecisionIsConditionalAssetsPending()
    {
        using var doc = ReadJson(ConsolidatedPath);
        var decision = doc.RootElement.GetProperty("decision").GetString();
        CollectionAssert.Contains(new[]
        {
            "PUBLIC_PACKAGE_FREEZE_PREP_READY",
            "PUBLIC_PACKAGE_FREEZE_PREP_CONDITIONAL_ASSETS_PENDING",
            "PUBLIC_PACKAGE_FREEZE_PREP_BLOCKED_REMEDIATION_REQUIRED"
        }, decision);

        Assert.AreEqual("PUBLIC_PACKAGE_FREEZE_PREP_CONDITIONAL_ASSETS_PENDING", decision);
        Assert.IsTrue(doc.RootElement.GetProperty("internalCandidateReady").GetBoolean());
        Assert.IsTrue(doc.RootElement.GetProperty("publicBuildCandidateReady").GetBoolean());
        Assert.IsTrue(doc.RootElement.GetProperty("publicStagingReady").GetBoolean());
        Assert.IsTrue(doc.RootElement.GetProperty("assetCapturePending").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("publicPackageFreezeReady").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("publicReleaseReady").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("chromeWebStoreReady").GetBoolean());
    }

    [TestMethod]
    public void CapabilitiesRemainDisabled()
    {
        foreach (var path in new[] { M704GoNoGoPath, M705GoNoGoPath, M706GoNoGoPath, ConsolidatedPath })
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

    [TestMethod]
    public void StagingManifestRemainsValidAndNarrowed()
    {
        using var doc = ReadJson(StagingManifestPath);
        var serialized = doc.RootElement.ToString();
        Assert.IsFalse(serialized.Contains("http://*/*", StringComparison.Ordinal));
        Assert.IsFalse(serialized.Contains("https://*/*", StringComparison.Ordinal));
        Assert.IsFalse(serialized.Contains("\"content_scripts\"", StringComparison.Ordinal));
    }

    [TestMethod]
    public void PackageExclusionAuditExcludesSensitiveInputs()
    {
        using var doc = ReadJson(ExclusionAuditPath);
        Assert.IsTrue(doc.RootElement.GetProperty("excludesTokens").GetBoolean());
        Assert.IsTrue(doc.RootElement.GetProperty("excludesConfig").GetBoolean());
        Assert.IsTrue(doc.RootElement.GetProperty("excludesLogs").GetBoolean());
        Assert.IsTrue(doc.RootElement.GetProperty("excludesTemp").GetBoolean());
        Assert.IsTrue(doc.RootElement.GetProperty("excludesUserData").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("secretsIncluded").GetBoolean());

        var serialized = doc.RootElement.ToString();
        StringAssert.Contains(serialized, "config/chrome-lab.local.json");
        StringAssert.Contains(serialized, "tokens");
        StringAssert.Contains(serialized, "logs");
        StringAssert.Contains(serialized, "temporary profiles");
        StringAssert.Contains(serialized, "user data");
    }

    [TestMethod]
    public void FinalGateKeepsReleaseAndStoreNoGo()
    {
        using var release = ReadJson(PublicReleaseNoGoPath);
        using var store = ReadJson(ChromeWebStoreNoGoPath);
        Assert.IsFalse(release.RootElement.GetProperty("publicReleaseReady").GetBoolean());
        Assert.IsFalse(store.RootElement.GetProperty("chromeWebStoreReady").GetBoolean());
    }

    [TestMethod]
    public void ExternalAuditPackIsReadyButRequiresAssetCompletion()
    {
        using var doc = ReadJson(ExternalAuditPackPath);
        Assert.IsTrue(doc.RootElement.GetProperty("externalPreSubmissionAuditPackReady").GetBoolean());
        Assert.IsTrue(doc.RootElement.GetProperty("requiresHumanAssetCompletionBeforeFreeze").GetBoolean());
    }
}
