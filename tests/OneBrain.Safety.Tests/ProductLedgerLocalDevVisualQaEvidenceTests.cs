using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.Core.Approval;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("ApprovalHumanReview")]
[TestCategory("PhaseEApprovalHumanReview")]
public sealed class ProductLedgerLocalDevVisualQaEvidenceTests
{
    [TestMethod]
    public void VisualQaEvidence_FailsClosedByDefaultAndRejectsUnsafeScopes()
    {
        var service = new ProductLedgerLocalDevVisualQaEvidence();
        var missing = service.Evaluate(null);
        var ready = ProductLedgerLocalDevVisualQaEvidence.CreateDefaultFixtureRequest();
        var cases = new Dictionary<ProductLedgerLocalDevVisualQaEvidenceRequest, ProductLedgerLocalDevVisualQaEvidenceBlocker>
        {
            [ready with { ExplicitLocalOnlyFixtureScope = false }] = ProductLedgerLocalDevVisualQaEvidenceBlocker.MissingExplicitLocalOnlyFixtureScope,
            [ready with { FixtureOnlyStaticHtmlEvidenceScope = false }] = ProductLedgerLocalDevVisualQaEvidenceBlocker.MissingFixtureOnlyStaticHtmlEvidenceScope,
            [ready with { RequestsRealScreenshot = true }] = ProductLedgerLocalDevVisualQaEvidenceBlocker.RealScreenshotRequested,
            [ready with { ClaimsBrowserCdpProductive = true }] = ProductLedgerLocalDevVisualQaEvidenceBlocker.BrowserCdpProductiveClaimed,
            [ready with { ClaimsPublicDeploy = true }] = ProductLedgerLocalDevVisualQaEvidenceBlocker.PublicDeployClaimed,
            [ready with { ClaimsExternalNetwork = true }] = ProductLedgerLocalDevVisualQaEvidenceBlocker.ExternalNetworkClaimed,
            [ready with { ClaimsTelemetryOrSync = true }] = ProductLedgerLocalDevVisualQaEvidenceBlocker.TelemetryOrSyncClaimed,
            [ready with { ClaimsProviderCloudNetwork = true }] = ProductLedgerLocalDevVisualQaEvidenceBlocker.ProviderCloudNetworkClaimed,
            [ready with { ClaimsDbMigration = true }] = ProductLedgerLocalDevVisualQaEvidenceBlocker.DbMigrationClaimed,
            [ready with { ClaimsKmsWormExternalTrust = true }] = ProductLedgerLocalDevVisualQaEvidenceBlocker.KmsWormExternalTrustClaimed,
            [ready with { ClaimsWcuOcrRecipesLive = true }] = ProductLedgerLocalDevVisualQaEvidenceBlocker.WcuOcrRecipesLiveClaimed,
            [ready with { ClaimsDestructiveAction = true }] = ProductLedgerLocalDevVisualQaEvidenceBlocker.DestructiveActionClaimed,
            [ready with { ClaimsUnboundedPhysicalExport = true }] = ProductLedgerLocalDevVisualQaEvidenceBlocker.UnboundedPhysicalExportClaimed,
            [ready with { ClaimsExternalCloudExport = true }] = ProductLedgerLocalDevVisualQaEvidenceBlocker.ExternalCloudExportClaimed,
            [ready with { ClaimsReleaseCommercial = true }] = ProductLedgerLocalDevVisualQaEvidenceBlocker.ReleaseCommercialClaimed,
            [ready with { ClaimsRawPayloadOrSecret = true }] = ProductLedgerLocalDevVisualQaEvidenceBlocker.RawPayloadOrSecretClaimed,
            [ready with { RoutePreviewRequest = null }] = ProductLedgerLocalDevVisualQaEvidenceBlocker.MissingRoutePreviewRequest,
            [ready with { RoutePreviewRequest = ProductLedgerLocalDevRoutePreview.CreateDefaultLocalDevRequest() with { ClaimsPublicDeploy = true } }] = ProductLedgerLocalDevVisualQaEvidenceBlocker.RoutePreviewRejected
        };

        AssertRejected(missing, ProductLedgerLocalDevVisualQaEvidenceBlocker.MissingRequest);
        foreach (var testCase in cases)
        {
            AssertRejected(service.Evaluate(testCase.Key), testCase.Value);
        }
    }

    [TestMethod]
    public void VisualQaEvidence_RendersExpectedSectionsAndLocalOnlyNotices()
    {
        var result = new ProductLedgerLocalDevVisualQaEvidence().Evaluate(
            ProductLedgerLocalDevVisualQaEvidence.CreateDefaultFixtureRequest());

        Assert.AreEqual(ProductLedgerLocalDevVisualQaEvidenceDecision.EvidenceReady, result.Decision);
        Assert.AreEqual("STATIC_HTML_FIXTURE_NO_BROWSER_CDP", result.ScreenshotMode);
        AssertNoForbiddenSurface(result);

        var html = result.VisualArtifactHtml;
        StringAssert.Contains(html, "data-testid=\"product-ledger-visual-qa-evidence\"");
        StringAssert.Contains(html, "Product Ledger Operator Surface Snapshot");
        StringAssert.Contains(html, "data-testid=\"runtime-gate\"");
        StringAssert.Contains(html, "data-testid=\"writer\"");
        StringAssert.Contains(html, "data-testid=\"bounded-export\"");
        StringAssert.Contains(html, "data-testid=\"evidence-gates\"");
        StringAssert.Contains(html, "data-testid=\"disabled-dangerous-actions\"");
        StringAssert.Contains(html, "data-testid=\"safe-next-step\"");
        StringAssert.Contains(html, "local-dev/internal-only");
        StringAssert.Contains(html, "not publicly deployed");
        StringAssert.Contains(html, "no telemetry");
        StringAssert.Contains(html, "no release/commercial");
        StringAssert.Contains(html, "no external trust");
        StringAssert.Contains(html, "no WORM/KMS/cloud");
        StringAssert.Contains(html, "not compliance-grade custody");
    }

    [TestMethod]
    public void VisualQaEvidence_KeepsDangerousActionsDisabledAndNoActiveClaimsVisible()
    {
        var result = new ProductLedgerLocalDevVisualQaEvidence().Evaluate(
            ProductLedgerLocalDevVisualQaEvidence.CreateDefaultFixtureRequest());
        var html = result.VisualArtifactHtml;

        Assert.AreEqual(ProductLedgerLocalDevVisualQaEvidenceDecision.EvidenceReady, result.Decision);
        StringAssert.Contains(html, "data-testid=\"action-destructive-write\"");
        StringAssert.Contains(html, "data-testid=\"action-unbounded-export\"");
        StringAssert.Contains(html, "data-testid=\"action-external-export\"");
        StringAssert.Contains(html, "disabled aria-disabled=\"true\"");
        StringAssert.Contains(html, "data-executable=\"false\"");
        StringAssert.Contains(html, "data-handler-id=\"\"");
        StringAssert.Contains(html, "data-callback=\"\"");

        foreach (var fragment in result.NegativeAssertions)
        {
            Assert.IsFalse(html.Contains(fragment, StringComparison.OrdinalIgnoreCase), fragment);
        }
    }

    [TestMethod]
    public void VisualQaEvidence_SourceHasNoBrowserCdpNetworkTelemetryDbKmsWriteOrRelease()
    {
        var source = File.ReadAllText(Path.Combine(
            RepoRoot(),
            "src",
            "OneBrain.Core",
            "Approval",
            "ProductLedgerLocalDevVisualQaEvidence.cs"));
        var forbiddenFragments = new[]
        {
            "IService" + "Collection",
            "Add" + "Singleton",
            "Add" + "Scoped",
            "Add" + "Transient",
            "IHosted" + "Service",
            "Map" + "Get",
            "Map" + "Post",
            "Controller" + "Base",
            "Http" + "Client",
            "Web" + "Socket",
            "Play" + "wright",
            "Chrome" + "DevTools",
            "Db" + "Context",
            "Migration" + "Builder",
            "Kms" + "Client",
            "Worm" + "Store",
            "File.Write" + "AllText",
            "File.Append" + "AllText",
            "Directory.Create" + "Directory",
            "RealScreenshotCaptured:" + " true",
            "BrowserCdpProductiveUsed:" + " true",
            "PublicDeployAvailable:" + " true",
            "ExternalNetworkAvailable:" + " true",
            "TelemetryOrSyncAvailable:" + " true",
            "ProviderCloudNetworkAvailable:" + " true",
            "DbMigrationAvailable:" + " true",
            "KmsWormExternalTrustAvailable:" + " true",
            "WcuOcrRecipesLiveAvailable:" + " true",
            "DestructiveActionAvailable:" + " true",
            "UnboundedPhysicalExportAvailable:" + " true",
            "ExternalCloudExportAvailable:" + " true",
            "ReleaseCommercialReady:" + " true"
        };

        foreach (var fragment in forbiddenFragments)
        {
            Assert.IsFalse(source.Contains(fragment, StringComparison.OrdinalIgnoreCase), fragment);
        }

        StringAssert.Contains(source, "STATIC_HTML_FIXTURE_NO_BROWSER_CDP");
        StringAssert.Contains(source, "NO_PUBLIC_DEPLOY");
        StringAssert.Contains(source, "NO_EXTERNAL_NETWORK");
    }

    private static void AssertRejected(
        ProductLedgerLocalDevVisualQaEvidenceResult result,
        ProductLedgerLocalDevVisualQaEvidenceBlocker blocker)
    {
        Assert.AreEqual(ProductLedgerLocalDevVisualQaEvidenceDecision.Rejected, result.Decision, blocker.ToString());
        CollectionAssert.Contains(result.Blockers.ToArray(), blocker, blocker.ToString());
        Assert.AreEqual(string.Empty, result.VisualArtifactHtml);
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

    private static string RepoRoot()
    {
        var directory = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(
                directory.FullName,
                "src",
                "OneBrain.Core",
                "Approval",
                "ProductLedgerLocalDevVisualQaEvidence.cs")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        Assert.Fail("repo root not found");
        return string.Empty;
    }
}
