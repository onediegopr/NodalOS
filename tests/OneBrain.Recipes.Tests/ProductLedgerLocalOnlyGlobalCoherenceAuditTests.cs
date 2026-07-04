using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace OneBrain.Recipes.Tests;

[TestClass]
[TestCategory("ApprovalHumanReview")]
[TestCategory("PhaseEApprovalHumanReview")]
public sealed class ProductLedgerLocalOnlyGlobalCoherenceAuditTests
{
    [TestMethod]
    public void GlobalCoherenceAudit_RecipePacketIsReadyForExternalReview()
    {
        using var document = LoadReport();

        Assert.AreEqual(
            "GO_WITH_FINDINGS_PRODUCT_LEDGER_LOCAL_ONLY_GLOBAL_COHERENCE_AUDIT_READY",
            document.RootElement.GetProperty("decision").GetString());
        Assert.AreEqual(26, document.RootElement.GetProperty("claims").GetArrayLength());
        Assert.AreEqual(20, document.RootElement.GetProperty("capabilities").GetArrayLength());
        Assert.AreEqual(
            "PAUSE_SAFE_LOCAL_ONLY_LINE_READY_FOR_EXTERNAL_REVIEW",
            document.RootElement.GetProperty("recommendedNextStep").GetString());
    }

    [TestMethod]
    public void GlobalCoherenceAudit_RecipePacketKeepsExternalAndReleaseReadinessAtZero()
    {
        using var document = LoadReport();
        var readiness = document.RootElement.GetProperty("readinessBeforeAfter");

        Assert.AreEqual("0% -> 0%", readiness.GetProperty("externalCloudReadiness").GetString());
        Assert.AreEqual("0% -> 0%", readiness.GetProperty("dbReadiness").GetString());
        Assert.AreEqual("0% -> 0%", readiness.GetProperty("kmsWormExternalTrust").GetString());
        Assert.AreEqual("0% -> 0%", readiness.GetProperty("browserCdpWcuOcrRecipesLive").GetString());
        Assert.AreEqual("0% -> 0%", readiness.GetProperty("releaseCommercial").GetString());
    }

    private static JsonDocument LoadReport() =>
        JsonDocument.Parse(File.ReadAllText(Path.Combine(
            RepoRoot(),
            "docs",
            "qa",
            "nodal-os-product-ledger-local-only-global-coherence-audit",
            "report.json")));

    private static string RepoRoot()
    {
        var directory = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "OneBrain.slnx")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        Assert.Fail("repo root not found");
        return string.Empty;
    }
}
