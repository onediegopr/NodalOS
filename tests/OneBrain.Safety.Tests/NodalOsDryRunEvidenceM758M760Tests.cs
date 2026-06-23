using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("DryRunEvidence")]
[TestCategory("M758")]
[TestCategory("M759")]
[TestCategory("M760")]
public sealed class NodalOsDryRunEvidenceM758M760Tests
{
    private const string EnvelopePath = "artifacts/agent-operations/m758/dry-run-evidence-envelope.json";
    private const string SchemaPath = "artifacts/agent-operations/m758/evidence-envelope-schema.json";
    private const string RequiredFieldsPath = "artifacts/agent-operations/m758/evidence-required-fields.json";
    private const string RedactionRulesPath = "artifacts/agent-operations/m758/evidence-redaction-rules.json";
    private const string ForbiddenFieldsPath = "artifacts/agent-operations/m758/evidence-forbidden-fields.json";
    private const string EvidenceByCapabilityPath = "artifacts/agent-operations/m758/evidence-by-capability.json";
    private const string PromotionCriteriaPath = "artifacts/agent-operations/m759/runtime-promotion-criteria.json";
    private const string PromotionByCapabilityPath = "artifacts/agent-operations/m759/promotion-criteria-by-capability.json";
    private const string PromotionBlockersPath = "artifacts/agent-operations/m759/promotion-blockers.json";
    private const string PromotionApprovalsPath = "artifacts/agent-operations/m759/promotion-required-approvals.json";
    private const string PromotionEvidencePath = "artifacts/agent-operations/m759/promotion-required-evidence.json";
    private const string ProductiveProhibitionPath = "artifacts/agent-operations/m759/productive-enable-prohibition.json";
    private const string NextDecisionPath = "artifacts/agent-operations/m760/dry-run-promotion-next-decision.json";
    private const string NoRuntimeUnlockPath = "artifacts/agent-operations/m760/no-productive-runtime-unlock-proof.json";
    private const string NoProviderCloudPath = "artifacts/agent-operations/m760/no-provider-cloud-live-call-proof.json";
    private const string NoFilesystemBrowserPath = "artifacts/agent-operations/m760/no-filesystem-browser-capability-unlock-proof.json";
    private const string ProductBridgeCspPath = "artifacts/agent-operations/m760/product-bridge-csp-unchanged-proof.json";
    private const string NoReleaseStorePath = "artifacts/agent-operations/m760/no-public-release-store-proof.json";
    private const string NextMilestonePath = "artifacts/agent-operations/m760/next-milestone-recommendation.json";
    private const string ConsolidatedPath = "artifacts/agent-operations/m758-m760/dry-run-evidence-promotion-go-no-go.json";

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
    public void DryRunEvidenceArtifactsExist()
    {
        foreach (var path in new[]
        {
            EnvelopePath, SchemaPath, RequiredFieldsPath, RedactionRulesPath, ForbiddenFieldsPath,
            EvidenceByCapabilityPath, PromotionCriteriaPath, PromotionByCapabilityPath,
            PromotionBlockersPath, PromotionApprovalsPath, PromotionEvidencePath, ProductiveProhibitionPath,
            NextDecisionPath, NoRuntimeUnlockPath, NoProviderCloudPath, NoFilesystemBrowserPath,
            ProductBridgeCspPath, NoReleaseStorePath, NextMilestonePath, ConsolidatedPath
        })
            Assert.IsTrue(File.Exists(FullPath(path)), path);
    }

    [TestMethod]
    public void EvidenceEnvelopeSchemaAndRequiredFieldsExist()
    {
        using var schema = ReadJson(SchemaPath);
        using var fields = ReadJson(RequiredFieldsPath);
        Assert.AreEqual("READY", schema.RootElement.GetProperty("schemaStatus").GetString());
        Assert.IsTrue(schema.RootElement.GetProperty("requiredProperties").EnumerateArray().Any(p => p.GetString() == "dryRunId"));
        Assert.IsTrue(fields.RootElement.GetProperty("requiredFields").EnumerateArray().Any(p => p.GetString() == "policyGateDecision"));
        Assert.IsTrue(fields.RootElement.GetProperty("requiredFields").EnumerateArray().Any(p => p.GetString() == "actualExecutionPerformed"));
    }

    [TestMethod]
    public void EvidenceForbiddenFieldsExcludeSensitiveValues()
    {
        using var doc = ReadJson(ForbiddenFieldsPath);
        var fields = doc.RootElement.GetProperty("forbiddenFields").EnumerateArray().Select(p => p.GetString()).ToArray();
        CollectionAssert.Contains(fields, "secrets");
        CollectionAssert.Contains(fields, "credentials");
        CollectionAssert.Contains(fields, "tokens");
        CollectionAssert.Contains(fields, "rawUserData");
        Assert.IsFalse(doc.RootElement.GetProperty("secretsIncluded").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("credentialsIncluded").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("tokensIncluded").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("rawUserDataIncluded").GetBoolean());
    }

    [TestMethod]
    public void EnvelopeProvesNoActualExecutionOrPublication()
    {
        using var doc = ReadJson(EnvelopePath);
        Assert.IsFalse(doc.RootElement.GetProperty("productiveExecutionAllowed").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("actualExecutionPerformed").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("liveCallPerformed").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("filesystemWritePerformed").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("browserAutomationPerformed").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("publicReleasePerformed").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("storeSubmissionPerformed").GetBoolean());
    }

    [TestMethod]
    public void RuntimePromotionCriteriaAndBlockersExist()
    {
        using var criteria = ReadJson(PromotionCriteriaPath);
        using var blockers = ReadJson(PromotionBlockersPath);
        Assert.AreEqual("READY", criteria.RootElement.GetProperty("runtimePromotionCriteria").GetString());
        Assert.IsTrue(criteria.RootElement.GetProperty("futureOwnerApprovedRuntimeUnlockGateRequired").GetBoolean());
        Assert.IsFalse(criteria.RootElement.GetProperty("productiveEnableAllowedInThisMilestone").GetBoolean());
        Assert.IsTrue(blockers.RootElement.GetProperty("blockers").EnumerateArray().Any(p => p.GetString() == "productive execution requested"));
        Assert.IsTrue(blockers.RootElement.GetProperty("blockers").EnumerateArray().Any(p => p.GetString() == "Bridge/CSP modified"));
    }

    [TestMethod]
    public void ProductiveEnabledIsProhibited()
    {
        using var doc = ReadJson(ProductiveProhibitionPath);
        Assert.IsFalse(doc.RootElement.GetProperty("productiveEnabledAllowed").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("productiveEnabledStateReturned").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("liveCallEnabledStateReturned").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("filesystemUnlockedStateReturned").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("browserAutomationEnabledStateReturned").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("publicReleaseReadyStateReturned").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("chromeWebStoreReadyStateReturned").GetBoolean());
    }

    [TestMethod]
    public void RuntimeProviderFilesystemBrowserAndCapabilityRemainDisabled()
    {
        using var doc = ReadJson(ConsolidatedPath);
        Assert.AreEqual("DISABLED", doc.RootElement.GetProperty("runtimeProductiveExecution").GetString());
        Assert.AreEqual("DISABLED", doc.RootElement.GetProperty("providerCloudLiveCalls").GetString());
        Assert.AreEqual("DISABLED", doc.RootElement.GetProperty("filesystemUnlock").GetString());
        Assert.AreEqual("DISABLED", doc.RootElement.GetProperty("browserAutomationUnlock").GetString());
        Assert.AreEqual("DISABLED", doc.RootElement.GetProperty("capabilityUnlock").GetString());
    }

    [TestMethod]
    public void ProductFilesBridgeAndCspAreUnchanged()
    {
        using var doc = ReadJson(ProductBridgeCspPath);
        Assert.IsFalse(doc.RootElement.GetProperty("productFilesModified").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("bridgeModified").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("cspModified").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("manifestsModified").GetBoolean());
    }

    [TestMethod]
    public void NextMilestoneIsDryRunEligibilityClassification()
    {
        using var doc = ReadJson(NextMilestonePath);
        Assert.AreEqual("M761-M763 Dry-Run Eligibility Classification + Future Runtime Gate", doc.RootElement.GetProperty("recommendedNextMilestone").GetString());
        Assert.AreEqual("classification/contracts/tests only", doc.RootElement.GetProperty("scope").GetString());
        Assert.IsFalse(doc.RootElement.GetProperty("productiveUnlockAllowed").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("publicReleaseAllowed").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("storeSubmissionAllowed").GetBoolean());
    }

    [TestMethod]
    public void FinalDecisionIsDryRunEvidencePromotionReady()
    {
        using var doc = ReadJson(ConsolidatedPath);
        Assert.AreEqual("DRY_RUN_EVIDENCE_ENVELOPE_RUNTIME_PROMOTION_CRITERIA_READY", doc.RootElement.GetProperty("decision").GetString());
        Assert.AreEqual("OWNER_ATTESTATION_NON_AUDITABLE", doc.RootElement.GetProperty("evidenceClass").GetString());
        Assert.IsFalse(doc.RootElement.GetProperty("evidenceInvented").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("productiveExecutionAllowed").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("actualExecutionPerformed").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("prohibitedProductiveStateReturned").GetBoolean());
    }
}
