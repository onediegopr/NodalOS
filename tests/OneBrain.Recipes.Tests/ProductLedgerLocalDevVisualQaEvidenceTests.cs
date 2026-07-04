using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.Core.Approval;

namespace OneBrain.Recipes.Tests;

[TestClass]
[TestCategory("ApprovalHumanReview")]
[TestCategory("PhaseEApprovalHumanReview")]
public sealed class ProductLedgerLocalDevVisualQaEvidenceTests
{
    [TestMethod]
    public void VisualQaEvidence_RendersFixtureRecipe()
    {
        var result = new ProductLedgerLocalDevVisualQaEvidence().Evaluate(
            ProductLedgerLocalDevVisualQaEvidence.CreateDefaultFixtureRequest());

        Assert.AreEqual(ProductLedgerLocalDevVisualQaEvidenceDecision.EvidenceReady, result.Decision);
        Assert.AreEqual("STATIC_HTML_FIXTURE_NO_BROWSER_CDP", result.ScreenshotMode);
        StringAssert.Contains(result.VisualArtifactHtml, "data-testid=\"product-ledger-visual-qa-evidence\"");
        StringAssert.Contains(result.VisualArtifactHtml, "Product Ledger Operator Surface Snapshot");
        StringAssert.Contains(result.VisualArtifactHtml, "local-dev/internal-only");
        StringAssert.Contains(result.VisualArtifactHtml, "not publicly deployed");
        StringAssert.Contains(result.VisualArtifactHtml, "no telemetry");
        StringAssert.Contains(result.VisualArtifactHtml, "no external network");
        StringAssert.Contains(result.VisualArtifactHtml, "disabled aria-disabled=\"true\"");
        AssertNoForbiddenSurface(result);
    }

    [TestMethod]
    public void VisualQaEvidence_BlocksRealScreenshotAndProductiveBrowserRecipe()
    {
        var result = new ProductLedgerLocalDevVisualQaEvidence().Evaluate(
            ProductLedgerLocalDevVisualQaEvidence.CreateDefaultFixtureRequest() with
            {
                RequestsRealScreenshot = true,
                ClaimsBrowserCdpProductive = true,
                ClaimsReleaseCommercial = true
            });

        Assert.AreEqual(ProductLedgerLocalDevVisualQaEvidenceDecision.Rejected, result.Decision);
        CollectionAssert.Contains(result.Blockers.ToArray(), ProductLedgerLocalDevVisualQaEvidenceBlocker.RealScreenshotRequested);
        CollectionAssert.Contains(result.Blockers.ToArray(), ProductLedgerLocalDevVisualQaEvidenceBlocker.BrowserCdpProductiveClaimed);
        CollectionAssert.Contains(result.Blockers.ToArray(), ProductLedgerLocalDevVisualQaEvidenceBlocker.ReleaseCommercialClaimed);
        AssertNoForbiddenSurface(result);
    }

    private static void AssertNoForbiddenSurface(ProductLedgerLocalDevVisualQaEvidenceResult result)
    {
        Assert.IsTrue(result.LocalOnly);
        Assert.IsTrue(result.DevelopmentOnly);
        Assert.IsTrue(result.FixtureOnly);
        Assert.IsTrue(result.StaticHtmlOnly);
        Assert.IsFalse(result.RealScreenshotCaptured);
        Assert.IsFalse(result.BrowserCdpProductiveUsed);
        Assert.IsFalse(result.PublicDeployAvailable);
        Assert.IsFalse(result.ExternalNetworkAvailable);
        Assert.IsFalse(result.TelemetryOrSyncAvailable);
        Assert.IsFalse(result.ProviderCloudNetworkAvailable);
        Assert.IsFalse(result.DbMigrationAvailable);
        Assert.IsFalse(result.KmsWormExternalTrustAvailable);
        Assert.IsFalse(result.WcuOcrRecipesLiveAvailable);
        Assert.IsFalse(result.DestructiveActionAvailable);
        Assert.IsFalse(result.UnboundedPhysicalExportAvailable);
        Assert.IsFalse(result.ExternalCloudExportAvailable);
        Assert.IsFalse(result.ReleaseCommercialReady);
    }
}
