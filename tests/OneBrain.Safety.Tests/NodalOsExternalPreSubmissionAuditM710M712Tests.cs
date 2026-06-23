using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("ExternalPreSubmissionAudit")]
[TestCategory("M710")]
[TestCategory("M711")]
[TestCategory("M712")]
public sealed class NodalOsExternalPreSubmissionAuditM710M712Tests
{
    private const string M710ReportPath = "docs/reports/m710-external-pre-submission-audit.md";
    private const string M711ReportPath = "docs/reports/m711-freeze-candidate-review.md";
    private const string M712ReportPath = "docs/reports/m712-release-blocker-decision-next-milestone.md";

    private const string ExternalAuditPath = "artifacts/agent-operations/m710/external-pre-submission-audit.json";
    private const string AuditScopeReviewPath = "artifacts/agent-operations/m710/audit-scope-review.json";
    private const string ManifestAuditPath = "artifacts/agent-operations/m710/manifest-public-variant-audit.json";
    private const string PermissionDisclosureAuditPath = "artifacts/agent-operations/m710/permission-disclosure-audit.json";
    private const string StoreListingAuditPath = "artifacts/agent-operations/m710/store-listing-audit.json";
    private const string EvidenceAssetsAuditPath = "artifacts/agent-operations/m710/evidence-assets-audit.json";
    private const string M710GoNoGoPath = "artifacts/agent-operations/m710/m710-go-no-go.json";

    private const string FreezeCandidateReviewPath = "artifacts/agent-operations/m711/freeze-candidate-review.json";
    private const string PackageFreezeReadinessReviewPath = "artifacts/agent-operations/m711/package-freeze-readiness-review.json";
    private const string PackageExclusionReviewPath = "artifacts/agent-operations/m711/package-exclusion-review.json";
    private const string ChecksumPlanReviewPath = "artifacts/agent-operations/m711/checksum-plan-review.json";
    private const string EvidenceReadinessReviewPath = "artifacts/agent-operations/m711/evidence-readiness-review.json";
    private const string StoreReadinessReviewPath = "artifacts/agent-operations/m711/store-readiness-review.json";
    private const string M711GoNoGoPath = "artifacts/agent-operations/m711/m711-go-no-go.json";

    private const string ReleaseBlockerDecisionPath = "artifacts/agent-operations/m712/release-blocker-decision.json";
    private const string PublicReleaseNoGoPath = "artifacts/agent-operations/m712/public-release-no-go-proof.json";
    private const string ChromeWebStoreNoGoPath = "artifacts/agent-operations/m712/chrome-web-store-no-go-proof.json";
    private const string BlockerPriorityMatrixPath = "artifacts/agent-operations/m712/blocker-priority-matrix.json";
    private const string NextMilestonePath = "artifacts/agent-operations/m712/next-milestone-recommendation.json";
    private const string M712GoNoGoPath = "artifacts/agent-operations/m712/m712-go-no-go.json";

    private const string ConsolidatedPath = "artifacts/agent-operations/m710-m712/external-pre-submission-audit-go-no-go.json";

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
    public void M710ArtifactsExist()
    {
        foreach (var path in new[]
        {
            M710ReportPath, ExternalAuditPath, AuditScopeReviewPath, ManifestAuditPath,
            PermissionDisclosureAuditPath, StoreListingAuditPath, EvidenceAssetsAuditPath, M710GoNoGoPath
        })
            Assert.IsTrue(File.Exists(FullPath(path)), path);
    }

    [TestMethod]
    public void M711ArtifactsExist()
    {
        foreach (var path in new[]
        {
            M711ReportPath, FreezeCandidateReviewPath, PackageFreezeReadinessReviewPath,
            PackageExclusionReviewPath, ChecksumPlanReviewPath, EvidenceReadinessReviewPath,
            StoreReadinessReviewPath, M711GoNoGoPath
        })
            Assert.IsTrue(File.Exists(FullPath(path)), path);
    }

    [TestMethod]
    public void M712ArtifactsExist()
    {
        foreach (var path in new[]
        {
            M712ReportPath, ReleaseBlockerDecisionPath, PublicReleaseNoGoPath,
            ChromeWebStoreNoGoPath, BlockerPriorityMatrixPath, NextMilestonePath,
            M712GoNoGoPath, ConsolidatedPath
        })
            Assert.IsTrue(File.Exists(FullPath(path)), path);
    }

    [TestMethod]
    public void ConsolidatedDecisionIsConditionalGo()
    {
        using var doc = ReadJson(ConsolidatedPath);
        var decision = doc.RootElement.GetProperty("decision").GetString();
        CollectionAssert.Contains(new[]
        {
            "EXTERNAL_PRE_SUBMISSION_AUDIT_CONDITIONAL_GO",
            "EXTERNAL_PRE_SUBMISSION_AUDIT_BLOCKED_ASSETS_URLS_REQUIRED",
            "EXTERNAL_PRE_SUBMISSION_AUDIT_BLOCKED_REMEDIATION_REQUIRED"
        }, decision);

        Assert.AreEqual("EXTERNAL_PRE_SUBMISSION_AUDIT_CONDITIONAL_GO", decision);
        Assert.IsTrue(doc.RootElement.GetProperty("externalAuditCompleted").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("criticalTechnicalBlockersFound").GetBoolean());
        Assert.IsTrue(doc.RootElement.GetProperty("manifestPublicVariantAcceptable").GetBoolean());
        Assert.IsTrue(doc.RootElement.GetProperty("permissionDisclosureAcceptable").GetBoolean());
        Assert.IsTrue(doc.RootElement.GetProperty("storeListingDraftAcceptable").GetBoolean());
        Assert.IsTrue(doc.RootElement.GetProperty("packageFreezePrepAcceptable").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("publicPackageFreezeReady").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("publicReleaseReady").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("chromeWebStoreReady").GetBoolean());
    }

    [TestMethod]
    public void CapabilitiesRemainDisabled()
    {
        foreach (var path in new[] { M710GoNoGoPath, M711GoNoGoPath, M712GoNoGoPath, ConsolidatedPath })
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
    public void PendingAssetsAndUrlsKeepStoreNoGo()
    {
        using var doc = ReadJson(ConsolidatedPath);
        Assert.IsTrue(doc.RootElement.GetProperty("assetsStillPending").GetBoolean());
        Assert.IsTrue(doc.RootElement.GetProperty("privacySupportUrlsStillPending").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("chromeWebStoreReady").GetBoolean());
    }

    [TestMethod]
    public void BlockerPriorityMatrixContainsRequiredPriorities()
    {
        using var doc = ReadJson(BlockerPriorityMatrixPath);
        var serialized = doc.RootElement.ToString();
        StringAssert.Contains(serialized, "privacy URL missing");
        StringAssert.Contains(serialized, "support URL missing");
        StringAssert.Contains(serialized, "screenshots/assets missing");
        StringAssert.Contains(serialized, "permission warning capture unknown");
        StringAssert.Contains(serialized, "docs URL pending");
    }

    [TestMethod]
    public void PublicReleaseRemainsNoGo()
    {
        using var release = ReadJson(PublicReleaseNoGoPath);
        using var store = ReadJson(ChromeWebStoreNoGoPath);
        Assert.IsFalse(release.RootElement.GetProperty("publicReleaseReady").GetBoolean());
        Assert.IsFalse(store.RootElement.GetProperty("chromeWebStoreReady").GetBoolean());
    }
}
