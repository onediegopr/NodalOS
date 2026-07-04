using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.Core.Approval;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("ApprovalHumanReview")]
[TestCategory("PhaseEApprovalHumanReview")]
public sealed class ProductLedgerOperatorAcceptanceLocalOnlyMatrixTests
{
    [TestMethod]
    public void OperatorAcceptanceMatrix_HasRequiredFifteenLocalOnlyRows()
    {
        var result = new ProductLedgerOperatorAcceptanceLocalOnlyMatrix().Build(
            ProductLedgerOperatorAcceptanceLocalOnlyMatrix.ReadyRequest());

        Assert.AreEqual(ProductLedgerOperatorAcceptanceMatrixDecision.ReadyLocalOnly, result.Decision);
        Assert.AreEqual(15, result.Rows.Count);
        AssertSafeResult(result);

        var required = new[]
        {
            "inspect-ledger-local-evidence",
            "inspect-screenshot-evidence",
            "inspect-bounded-export-evidence",
            "inspect-command-router-preview",
            "inspect-command-handler-result",
            "inspect-runtime-gate-status",
            "inspect-public-local-actions",
            "blocked-external-cloud-live-release-reasons",
            "cannot-trigger-destructive-action",
            "cannot-trigger-external-cloud-provider-network",
            "cannot-trigger-db-migration",
            "cannot-trigger-telemetry-sync-billing",
            "cannot-trigger-browser-cdp-live",
            "cannot-claim-release-commercial",
            "cannot-claim-kms-worm-external-trust"
        };

        foreach (var id in required)
        {
            var row = result.Rows.Single(row => row.ActionId == id);
            Assert.IsTrue(row.LocalOnly, id);
            Assert.IsTrue(row.NonDestructive, id);
            Assert.IsFalse(row.ExecutionAllowed, id);
            Assert.IsFalse(string.IsNullOrWhiteSpace(row.VisibleLabel), id);
            Assert.IsFalse(string.IsNullOrWhiteSpace(row.ExpectedOperatorMessage), id);
            Assert.IsFalse(string.IsNullOrWhiteSpace(row.SafeNextStep), id);
            Assert.IsTrue(row.EvidenceRefs.Count > 0, id);
        }
    }

    [TestMethod]
    public void OperatorAcceptanceMatrix_LinksScreenshotBoundedExportRouterHandlerAndRuntimeEvidence()
    {
        var rows = new ProductLedgerOperatorAcceptanceLocalOnlyMatrix()
            .Build(ProductLedgerOperatorAcceptanceLocalOnlyMatrix.ReadyRequest())
            .Rows;

        AssertEvidence(rows, "inspect-screenshot-evidence", "browser-local-only-screenshot");
        AssertEvidence(rows, "inspect-bounded-export-evidence", "bounded-local-report-export");
        AssertEvidence(rows, "inspect-command-router-preview", "internal-command-router-noop-read-only");
        AssertEvidence(rows, "inspect-command-handler-result", "internal-command-handler-non-destructive");
        AssertEvidence(rows, "inspect-runtime-gate-status", "runtime-local-only-gate");
        AssertEvidence(rows, "inspect-public-local-actions", "public-local-only-actions");
    }

    [TestMethod]
    public void OperatorAcceptanceMatrix_FailsClosedWhenEvidenceIsMissingOrOverclaimed()
    {
        var ready = ProductLedgerOperatorAcceptanceLocalOnlyMatrix.ReadyRequest();
        var cases = new Dictionary<ProductLedgerOperatorAcceptanceMatrixRequest, ProductLedgerOperatorAcceptanceMatrixBlocker>
        {
            [ready with { ExplicitLocalOnlyAcceptanceScope = false }] = ProductLedgerOperatorAcceptanceMatrixBlocker.MissingExplicitLocalOnlyAcceptanceScope,
            [ready with { HasScreenshotEvidence = false }] = ProductLedgerOperatorAcceptanceMatrixBlocker.MissingScreenshotEvidence,
            [ready with { HasBoundedExportEvidence = false }] = ProductLedgerOperatorAcceptanceMatrixBlocker.MissingBoundedExportEvidence,
            [ready with { HasCommandRouterEvidence = false }] = ProductLedgerOperatorAcceptanceMatrixBlocker.MissingCommandRouterEvidence,
            [ready with { HasCommandHandlerEvidence = false }] = ProductLedgerOperatorAcceptanceMatrixBlocker.MissingCommandHandlerEvidence,
            [ready with { HasRuntimeGateEvidence = false }] = ProductLedgerOperatorAcceptanceMatrixBlocker.MissingRuntimeGateEvidence,
            [ready with { ClaimsPublicDeploy = true }] = ProductLedgerOperatorAcceptanceMatrixBlocker.PublicDeployClaimed,
            [ready with { ClaimsExternalNetworkProviderCloud = true }] = ProductLedgerOperatorAcceptanceMatrixBlocker.ExternalNetworkProviderCloudClaimed,
            [ready with { ClaimsTelemetrySyncBilling = true }] = ProductLedgerOperatorAcceptanceMatrixBlocker.TelemetrySyncBillingClaimed,
            [ready with { ClaimsDbMigration = true }] = ProductLedgerOperatorAcceptanceMatrixBlocker.DbMigrationClaimed,
            [ready with { ClaimsKmsWormExternalTrust = true }] = ProductLedgerOperatorAcceptanceMatrixBlocker.KmsWormExternalTrustClaimed,
            [ready with { ClaimsBrowserCdpWcuOcrRecipesLive = true }] = ProductLedgerOperatorAcceptanceMatrixBlocker.BrowserCdpWcuOcrRecipesLiveClaimed,
            [ready with { ClaimsDestructiveAction = true }] = ProductLedgerOperatorAcceptanceMatrixBlocker.DestructiveActionClaimed,
            [ready with { ClaimsUnboundedExportWrite = true }] = ProductLedgerOperatorAcceptanceMatrixBlocker.UnboundedExportWriteClaimed,
            [ready with { ClaimsReleaseCommercial = true }] = ProductLedgerOperatorAcceptanceMatrixBlocker.ReleaseCommercialClaimed,
            [ready with { ClaimsCustodyComplianceGrade = true }] = ProductLedgerOperatorAcceptanceMatrixBlocker.CustodyComplianceClaimed
        };

        foreach (var testCase in cases)
        {
            var result = new ProductLedgerOperatorAcceptanceLocalOnlyMatrix().Build(testCase.Key);

            Assert.AreEqual(ProductLedgerOperatorAcceptanceMatrixDecision.Rejected, result.Decision, testCase.Value.ToString());
            CollectionAssert.Contains(result.Blockers.ToArray(), testCase.Value, testCase.Value.ToString());
            AssertSafeResult(result);
        }
    }

    [TestMethod]
    public void OperatorAcceptanceMatrix_SourceHasNoRuntimeRegistrationOrExternalSurface()
    {
        var source = File.ReadAllText(Path.Combine(
            RepoRoot(),
            "src",
            "OneBrain.Core",
            "Approval",
            "ProductLedgerOperatorAcceptanceLocalOnlyMatrix.cs"));
        var forbiddenFragments = new[]
        {
            "IService" + "Collection",
            "Add" + "Singleton",
            "Add" + "Scoped",
            "IHosted" + "Service",
            "Map" + "Get",
            "Map" + "Post",
            "Http" + "Client",
            "Db" + "Context",
            "Migration" + "Builder",
            "File.Write" + "AllText",
            "BrowserCdpWcuOcrRecipesLiveAvailable:" + " true",
            "ReleaseCommercialReady:" + " true",
            "PublicInternetExposureAvailable:" + " true"
        };

        foreach (var fragment in forbiddenFragments)
        {
            Assert.IsFalse(source.Contains(fragment, StringComparison.OrdinalIgnoreCase), fragment);
        }
    }

    private static void AssertEvidence(
        IReadOnlyList<ProductLedgerOperatorAcceptanceMatrixRow> rows,
        string actionId,
        string evidenceRef)
    {
        var row = rows.Single(row => row.ActionId == actionId);
        CollectionAssert.Contains(row.EvidenceRefs.ToArray(), evidenceRef, actionId);
    }

    private static void AssertSafeResult(ProductLedgerOperatorAcceptanceMatrixResult result)
    {
        Assert.IsTrue(result.LocalOnly);
        Assert.IsTrue(result.TestOnly);
        Assert.IsTrue(result.FixtureSafe);
        Assert.IsTrue(result.FailClosed);
        Assert.IsFalse(result.PublicInternetExposureAvailable);
        Assert.IsFalse(result.DestructiveActionAvailable);
        Assert.IsFalse(result.ExternalNetworkProviderCloudAvailable);
        Assert.IsFalse(result.TelemetrySyncBillingAvailable);
        Assert.IsFalse(result.DbMigrationAvailable);
        Assert.IsFalse(result.KmsWormExternalTrustAvailable);
        Assert.IsFalse(result.BrowserCdpWcuOcrRecipesLiveAvailable);
        Assert.IsFalse(result.UnboundedExportWriteAvailable);
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
                "ProductLedgerOperatorAcceptanceLocalOnlyMatrix.cs")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        Assert.Fail("repo root not found");
        return string.Empty;
    }
}
