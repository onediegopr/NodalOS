using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.Core.Approval;

namespace OneBrain.Recipes.Tests;

[TestClass]
[TestCategory("ApprovalHumanReview")]
[TestCategory("PhaseEApprovalHumanReview")]
public sealed class ProductLedgerOperatorAcceptanceLocalOnlyMatrixTests
{
    [TestMethod]
    public void OperatorAcceptanceMatrix_RecipeBuildsAcceptancePacket()
    {
        var result = new ProductLedgerOperatorAcceptanceLocalOnlyMatrix().Build(
            ProductLedgerOperatorAcceptanceLocalOnlyMatrix.ReadyRequest());

        Assert.AreEqual(ProductLedgerOperatorAcceptanceMatrixDecision.ReadyLocalOnly, result.Decision);
        Assert.AreEqual(15, result.Rows.Count);
        Assert.IsTrue(result.Rows.Any(row => row.ActionId == "inspect-screenshot-evidence"));
        Assert.IsTrue(result.Rows.Any(row => row.ActionId == "inspect-bounded-export-evidence"));
        Assert.IsTrue(result.Rows.Any(row => row.ActionId == "cannot-trigger-destructive-action"));
        Assert.IsFalse(result.Rows.Any(row => row.ExecutionAllowed));
        AssertSafe(result);
    }

    [TestMethod]
    public void OperatorAcceptanceMatrix_RecipeRejectsReleaseCloudAndCustodyOverclaims()
    {
        var request = ProductLedgerOperatorAcceptanceLocalOnlyMatrix.ReadyRequest() with
        {
            ClaimsExternalNetworkProviderCloud = true,
            ClaimsReleaseCommercial = true,
            ClaimsCustodyComplianceGrade = true
        };

        var result = new ProductLedgerOperatorAcceptanceLocalOnlyMatrix().Build(request);

        Assert.AreEqual(ProductLedgerOperatorAcceptanceMatrixDecision.Rejected, result.Decision);
        CollectionAssert.Contains(result.Blockers.ToArray(), ProductLedgerOperatorAcceptanceMatrixBlocker.ExternalNetworkProviderCloudClaimed);
        CollectionAssert.Contains(result.Blockers.ToArray(), ProductLedgerOperatorAcceptanceMatrixBlocker.ReleaseCommercialClaimed);
        CollectionAssert.Contains(result.Blockers.ToArray(), ProductLedgerOperatorAcceptanceMatrixBlocker.CustodyComplianceClaimed);
        AssertSafe(result);
    }

    private static void AssertSafe(ProductLedgerOperatorAcceptanceMatrixResult result)
    {
        Assert.IsTrue(result.LocalOnly);
        Assert.IsTrue(result.TestOnly);
        Assert.IsTrue(result.FixtureSafe);
        Assert.IsFalse(result.DestructiveActionAvailable);
        Assert.IsFalse(result.ExternalNetworkProviderCloudAvailable);
        Assert.IsFalse(result.BrowserCdpWcuOcrRecipesLiveAvailable);
        Assert.IsFalse(result.ReleaseCommercialReady);
    }
}
