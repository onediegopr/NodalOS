using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("ApprovalHumanReview")]
[TestCategory("PhaseEApprovalHumanReview")]
public sealed class ProductLedgerLocalOnlyGlobalCoherenceAuditTests
{
    [TestMethod]
    public void GlobalCoherenceAudit_ClaimMatrixHasExpectedStatusDistribution()
    {
        using var document = LoadReport();
        var claims = document.RootElement.GetProperty("claims").EnumerateArray().ToArray();

        Assert.AreEqual(26, claims.Length);
        Assert.AreEqual(14, claims.Count(claim => Status(claim) == "SUPPORTED"));
        Assert.AreEqual(2, claims.Count(claim => Status(claim) == "LIMITED"));
        Assert.AreEqual(10, claims.Count(claim => Status(claim) == "BLOCKED"));
        Assert.AreEqual(0, claims.Count(claim => Status(claim) == "NOT_SUPPORTED"));

        AssertClaim(claims, "PL-CLAIM-012", "SUPPORTED");
        AssertClaim(claims, "PL-CLAIM-014", "LIMITED");
        AssertClaim(claims, "PL-CLAIM-016", "LIMITED");
        AssertClaim(claims, "PL-CLAIM-026", "BLOCKED");
    }

    [TestMethod]
    public void GlobalCoherenceAudit_CapabilityMatrixKeepsFrontiersBlocked()
    {
        using var document = LoadReport();
        var capabilities = document.RootElement.GetProperty("capabilities").EnumerateArray().ToArray();

        Assert.AreEqual(20, capabilities.Length);
        AssertCapability(capabilities, "PL-CAP-012", "TEST_ONLY", "92%");
        AssertCapability(capabilities, "PL-CAP-013", "IMPLEMENTED_LOCAL_ONLY", "84%");
        AssertCapability(capabilities, "PL-CAP-014", "BLOCKED", "0%");
        AssertCapability(capabilities, "PL-CAP-015", "BLOCKED", "0%");
        AssertCapability(capabilities, "PL-CAP-016", "BLOCKED", "0%");
        AssertCapability(capabilities, "PL-CAP-017", "BLOCKED", "0%");
        AssertCapability(capabilities, "PL-CAP-020", "NO_GO", "0%");
    }

    [TestMethod]
    public void GlobalCoherenceAudit_NoP0P1P2OrPositiveExternalBoundaryClaims()
    {
        using var document = LoadReport();
        var findings = document.RootElement.GetProperty("findings");

        Assert.AreEqual(0, findings.GetProperty("P0").GetArrayLength());
        Assert.AreEqual(0, findings.GetProperty("P1").GetArrayLength());
        Assert.AreEqual(0, findings.GetProperty("P2").GetArrayLength());

        var serialized = document.RootElement.GetRawText();
        var forbiddenPositiveClaims = new[]
        {
            "\"externalCloudReadiness\": \"1",
            "\"dbReadiness\": \"1",
            "\"kmsWormExternalTrust\": \"1",
            "\"browserCdpWcuOcrRecipesLive\": \"1",
            "\"releaseCommercial\": \"1",
            "\"status\": \"SUPPORTED\", \"text\": \"No public deploy",
            "\"status\": \"SUPPORTED\", \"text\": \"No external network"
        };

        foreach (var fragment in forbiddenPositiveClaims)
        {
            Assert.IsFalse(serialized.Contains(fragment, StringComparison.OrdinalIgnoreCase), fragment);
        }
    }

    [TestMethod]
    public void GlobalCoherenceAudit_ReportDeclaresExternalReviewPause()
    {
        using var document = LoadReport();

        Assert.AreEqual(
            "PAUSE_SAFE_LOCAL_ONLY_LINE_READY_FOR_EXTERNAL_REVIEW",
            document.RootElement.GetProperty("recommendedNextStep").GetString());
        StringAssert.Contains(
            document.RootElement.GetProperty("externalAuditPacket").GetProperty("reviewerPrompt").GetString()!,
            "Do not infer public deployment");
    }

    private static JsonDocument LoadReport() =>
        JsonDocument.Parse(File.ReadAllText(Path.Combine(
            RepoRoot(),
            "docs",
            "qa",
            "nodal-os-product-ledger-local-only-global-coherence-audit",
            "report.json")));

    private static void AssertClaim(JsonElement[] claims, string id, string expectedStatus)
    {
        var claim = claims.Single(claim => claim.GetProperty("id").GetString() == id);
        Assert.AreEqual(expectedStatus, Status(claim), id);
        Assert.IsFalse(string.IsNullOrWhiteSpace(claim.GetProperty("safeWording").GetString()), id);
    }

    private static void AssertCapability(
        JsonElement[] capabilities,
        string id,
        string expectedState,
        string expectedPercentage)
    {
        var capability = capabilities.Single(capability => capability.GetProperty("id").GetString() == id);
        Assert.AreEqual(expectedState, capability.GetProperty("state").GetString(), id);
        Assert.AreEqual(expectedPercentage, capability.GetProperty("currentPercentage").GetString(), id);
    }

    private static string? Status(JsonElement claim) => claim.GetProperty("status").GetString();

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
