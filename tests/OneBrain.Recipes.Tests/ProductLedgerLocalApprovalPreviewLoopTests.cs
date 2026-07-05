using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.Core.Approval;

namespace OneBrain.Recipes.Tests;

[TestClass]
[TestCategory("ApprovalHumanReview")]
[TestCategory("PhaseEApprovalHumanReview")]
public sealed class ProductLedgerLocalApprovalPreviewLoopTests
{
    [TestMethod]
    public void LocalApprovalPreviewLoopRecipe_RendersPreviewOnlyApprovalToActionFlow()
    {
        var result = new ProductLedgerLocalDevRoutePreview().Render(
            ProductLedgerLocalDevRoutePreview.CreateDefaultLocalDevRequest());
        var loop = result.CanonicalSurface.ApprovalPreviewLoop;
        var html = result.HtmlSnapshot;

        Assert.AreEqual(ProductLedgerLocalDevRoutePreviewDecision.RenderedLocalDevInternalPreview, result.Decision);
        Assert.AreEqual(ProductLedgerLocalApprovalPreviewLoopFactory.LoopId, loop.LoopId);
        Assert.IsTrue(loop.IsLocalOnly);
        Assert.IsTrue(loop.IsReadOnly);
        Assert.IsTrue(loop.IsPreviewOnly);
        Assert.IsFalse(loop.AllowsExecution);
        Assert.IsFalse(loop.AllowsWrite);
        Assert.IsFalse(loop.AllowsExport);
        Assert.IsFalse(loop.AllowsNetwork);
        Assert.IsFalse(loop.AllowsDb);
        Assert.IsFalse(loop.AllowsReleaseCommercial);
        StringAssert.Contains(html, "data-testid=\"product-ledger-approval-preview\"");
        StringAssert.Contains(html, "data-testid=\"product-ledger-candidate-action-preview\"");
        StringAssert.Contains(html, "data-testid=\"product-ledger-policy-gate-preview\"");
        StringAssert.Contains(html, "data-testid=\"product-ledger-noop-execution-preview\"");
        StringAssert.Contains(html, "data-testid=\"product-ledger-preview-evidence-refs\"");
        StringAssert.Contains(html, "data-testid=\"product-ledger-approval-safe-next-step\"");
        StringAssert.Contains(html, "no product command execution");
        StringAssert.Contains(html, "no write/export");
        Assert.IsFalse(html.Contains("data-executable=\"true\"", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(loop.NoOpExecutionPreview.HandlerInvoked);
        Assert.IsFalse(loop.NoOpExecutionPreview.PilotRunInvoked);
    }
}
