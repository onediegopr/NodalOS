using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.Core.Recipes;

namespace OneBrain.Recipes.Tests;

[TestClass]
[TestCategory("Recipe")]
[TestCategory("RecipeLabReadOnlyUiMount")]
public sealed class RecipeLabReadOnlyUiMountTests
{
    [TestMethod]
    public void UiMountUsesReadOnlyProductSurfaceAndDeterministicFixture()
    {
        var mount = RecipeLabReadOnlyUiMount.CreateFixture();

        Assert.AreEqual(RecipeLabReadOnlyUiMount.MountId, mount.MountId);
        Assert.AreEqual("#recipeLabSurface", mount.Route);
        Assert.AreEqual("Recipe Lab", mount.NavigationLabel);
        Assert.AreEqual("recipe.catalog.surface.v1", mount.CatalogSurface.SurfaceId);
        Assert.AreEqual("recipe.lab.surface.v1", mount.LabSurface.SurfaceId);
        Assert.AreEqual("reliable-recipe-lab.audit-surface.m13", mount.AuditSurface.SurfaceId);
        Assert.AreEqual(RecipeLabReadOnlyUiMount.SelectedTemplateId, mount.TemplateDetailSurface.ViewModel.Header.TemplateId);
        Assert.IsTrue(mount.RouteVisible);
        Assert.IsTrue(mount.UsesReadOnlyProductSurface);
        Assert.IsTrue(mount.UsesDeterministicFixture);
        Assert.AreEqual(41, mount.CatalogSurface.ViewModel.TotalTemplates);
    }

    [TestMethod]
    public void UiMountExposesCatalogDetailReadinessOperatorAndHandoffSections()
    {
        var mount = RecipeLabReadOnlyUiMount.CreateFixture();
        var text = MountText(mount);

        foreach (var expected in new[]
        {
            "Recipe Catalog Summary",
            "Recipe Templates",
            "Recipe Detail Preview",
            "Readiness Matrix",
            "Blocked Reasons",
            "Required Human Actions",
            "Operator Preview",
            "Handoff Export Preview",
            "No-Runtime Notices",
            "No-Live Notices",
            "READ_ONLY",
            "FIXTURE_SAFE",
            "NO_RUNTIME",
            "NO_LIVE_AUTOMATION"
        })
        {
            StringAssert.Contains(text, expected);
        }
    }

    [TestMethod]
    public void UiMountKeepsRuntimeRecipeExecutionProviderPersistenceAndFileWritesDisabled()
    {
        var mount = RecipeLabReadOnlyUiMount.CreateFixture();

        Assert.IsTrue(mount.ReadOnly);
        Assert.IsTrue(mount.FixtureSafe);
        Assert.IsTrue(mount.PreviewSafe);
        Assert.IsFalse(mount.RuntimeEnabled);
        Assert.IsFalse(mount.RecipeExecutionEnabled);
        Assert.IsFalse(mount.BrowserCdpAutomationEnabled);
        Assert.IsFalse(mount.WcuLiveEnabled);
        Assert.IsFalse(mount.OcrLiveEnabled);
        Assert.IsFalse(mount.ProviderCloudEnabled);
        Assert.IsFalse(mount.DurablePersistenceEnabled);
        Assert.IsFalse(mount.FilesystemWritesEnabled);
        Assert.IsFalse(mount.HandoffExportWritesFile);
        Assert.IsFalse(mount.CatalogSurface.CanStartRecipeRun);
        Assert.IsFalse(mount.LabSurface.CanStartRecipeRun);
        Assert.IsFalse(mount.TemplateDetailSurface.CanStartRecipeRun);
        Assert.IsFalse(mount.OperatorSurface.CanStartRecipeRun);
        Assert.IsFalse(mount.OperatorSurface.HandoffExportPreview.WritesRealFile);
        Assert.IsFalse(mount.OperatorSurface.HandoffExportPreview.CallsNetwork);
    }

    [TestMethod]
    public void UiMountShowsBlockedReasonsHumanActionsAndSafeNextStep()
    {
        var mount = RecipeLabReadOnlyUiMount.CreateFixture();

        Assert.IsTrue(mount.TemplateDetailSurface.ViewModel.ReadinessExplanation.IsPreviewable);
        Assert.IsFalse(mount.TemplateDetailSurface.ViewModel.ReadinessExplanation.StartsRecipeRun);
        Assert.IsFalse(mount.OperatorSurface.OperatorPreview.LiveRuntimeEnabled);
        Assert.IsTrue(mount.OperatorSurface.OperatorPreview.ExpectedHumanInterventionPoints.Count > 0);
        Assert.IsFalse(string.IsNullOrWhiteSpace(mount.OperatorSurface.OperatorPreview.SafeNextAction.RedactedSummary));
        Assert.IsTrue(mount.LabSurface.ViewModel.BlockedRunModes.Count > 0);
        StringAssert.Contains(mount.LabSurface.ViewModel.SafetyBoundarySummary, "reference-only");
    }

    [TestMethod]
    public void UiMountDoesNotExposeForbiddenActionLabelsOrProductionClaims()
    {
        var text = MountText(RecipeLabReadOnlyUiMount.CreateFixture());
        var forbidden = new[]
        {
            "run " + "recipe",
            "execute " + "recipe",
            "start " + "automation",
            "launch " + "browser",
            "live " + "CDP",
            "write " + "files",
            "apply " + "patch",
            "production automation " + "ready",
            "can execute/live " + "automate"
        };

        foreach (var term in forbidden)
        {
            Assert.IsFalse(text.Contains(term, StringComparison.OrdinalIgnoreCase), term);
        }

        StringAssert.Contains(text, "No recipe execution.");
        StringAssert.Contains(text, "No browser/CDP automation.");
        StringAssert.Contains(text, "No filesystem writes.");
    }

    private static string MountText(RecipeLabReadOnlyUiMountViewModel mount)
    {
        var parts = new List<string>
        {
            mount.MountId,
            mount.Route,
            mount.NavigationLabel,
            mount.CatalogSurface.ViewModel.ProductSurfaceSummary,
            mount.LabSurface.ViewModel.DisplayName,
            mount.LabSurface.ViewModel.SafetyBoundarySummary,
            mount.LabSurface.ViewModel.EvidenceTimelineSummary,
            mount.TemplateDetailSurface.ViewModel.Header.DisplayName,
            mount.TemplateDetailSurface.ViewModel.OperatorVisibleSummary,
            mount.TemplateDetailSurface.ViewModel.ReadinessExplanation.OperatorVisibleSummary,
            mount.OperatorSurface.OperatorPreview.OperatorReviewSummary,
            mount.OperatorSurface.OperatorPreview.NotAutomatedSummary,
            mount.OperatorSurface.HandoffExportPreview.ProductSafeCopy,
            mount.AuditSurface.NoRuntimeNotice
        };
        parts.AddRange(mount.StatusBadges);
        parts.AddRange(mount.SafetyNotices);
        parts.AddRange(mount.VisibleSections);
        parts.AddRange(mount.AllowedUiActions);
        parts.AddRange(mount.ForbiddenUiActions);
        parts.AddRange(mount.CatalogSurface.SafetyCopy);
        parts.AddRange(mount.LabSurface.SafetyCopy);
        parts.AddRange(mount.TemplateDetailSurface.SafetyCopy);
        parts.AddRange(mount.OperatorSurface.SafetyCopy);
        return string.Join(" ", parts);
    }
}
