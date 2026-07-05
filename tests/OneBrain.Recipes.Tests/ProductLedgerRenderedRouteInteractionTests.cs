using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.Core.Approval;

namespace OneBrain.Recipes.Tests;

[TestClass]
[TestCategory("ApprovalHumanReview")]
[TestCategory("PhaseEApprovalHumanReview")]
public sealed class ProductLedgerRenderedRouteInteractionTests
{
    [TestMethod]
    public void RenderedRouteInteractionRecipe_ProducesCanonicalLocalOnlyReadOnlyDom()
    {
        var result = new ProductLedgerLocalDevRoutePreview().Render(
            ProductLedgerLocalDevRoutePreview.CreateDefaultLocalDevRequest());
        var html = result.HtmlSnapshot;

        Assert.AreEqual(ProductLedgerLocalDevRoutePreviewDecision.RenderedLocalDevInternalPreview, result.Decision);
        Assert.AreEqual("/internal/product-ledger/operator-surface", result.RouteTemplatePreview);
        Assert.IsTrue(result.CanonicalSurface.IsLocalOnly);
        Assert.IsTrue(result.CanonicalSurface.IsDevelopmentOnly);
        Assert.IsTrue(result.CanonicalSurface.IsReadOnly);
        Assert.IsFalse(result.CanonicalSurface.AllowsProductCommandExecution);
        StringAssert.Contains(html, "data-testid=\"product-ledger-surface-root\"");
        StringAssert.Contains(html, "data-testid=\"product-ledger-authority\"");
        StringAssert.Contains(html, "data-testid=\"product-ledger-blocked-frontiers\"");
        StringAssert.Contains(html, "data-testid=\"product-ledger-safe-next-steps\"");
        StringAssert.Contains(html, "data-testid=\"product-ledger-action-previews\"");
        StringAssert.Contains(html, "data-executable=\"false\"");
        StringAssert.Contains(html, "disabled aria-disabled=\"true\"");
        Assert.IsFalse(html.Contains("data-executable=\"true\"", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(html.Contains("<form", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(html.Contains("<script", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(html.Contains("href=\"http", StringComparison.OrdinalIgnoreCase));
    }
}
