using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.Core.Recipes;

namespace OneBrain.Recipes.Tests;

[TestClass]
[TestCategory("RecipeProductSurfaceNavigationMessagingFinalPolish")]
public sealed class RecipeProductSurfaceNavigationMessagingFinalPolishTests
{
    private const string AllowedClaim = "NODAL OS has a fixture-safe Recipe Runtime product surface with read-only catalog, lab, templates, readiness explanations, operator previews and handoff/export preview summaries.";
    private const string ForbiddenClaim = "NODAL OS can execute/live automate these recipes.";

    [TestMethod]
    [TestCategory("RecipeProductSurfaceNavigationMessagingFinalPolish")]
    public void FinalNavigationMessagingLineSummaryIsReadOnlyAndAuditReady()
    {
        var surface = Surface();
        var composition = surface.FinalComposition;

        Assert.IsTrue(surface.ReadOnly);
        Assert.IsTrue(surface.PreviewSafe);
        Assert.IsTrue(surface.FixtureSafeOnly);
        Assert.AreEqual(RecipeProductSurfaceNavigationMessagingAuditReadinessStatus.ReadyForFinalAudit, composition.AuditReadinessStatus);
        Assert.AreEqual("COMPLETE_READ_ONLY_NAVIGATION_MESSAGING_AUDIT_READY", composition.FinalLineStatus);
        Assert.IsTrue(composition.ReadOnly);
        Assert.IsTrue(composition.PreviewSafe);
        Assert.IsTrue(composition.FixtureSafeOnly);
        Assert.IsFalse(surface.CanStartRecipeRun);
        Assert.IsFalse(surface.CanProcessWorkitem);
        Assert.IsFalse(surface.CanEnableLiveRuntime);
        Assert.IsFalse(surface.CanOpenConnector);
        Assert.IsFalse(surface.CanRequestSecrets);
        Assert.IsFalse(surface.CanCallNetwork);
        Assert.IsFalse(surface.CanCreateSchedulerWatcherHookOrListener);
        Assert.IsFalse(surface.CanCreateRecorderReplayOrCapture);
        Assert.IsFalse(surface.CanWriteExportFile);
        Assert.IsFalse(surface.CanApplyLocatorRepair);
        Assert.IsFalse(surface.LiveRuntimeEnabled);
    }

    [TestMethod]
    [TestCategory("RecipeProductSurfaceNavigationMessagingFinalPolish")]
    public void PhaseOneTaxonomyAndPhaseTwoDemoFlowAreRepresented()
    {
        var surface = Surface();

        Assert.AreEqual("NODAL_RECIPE_PRODUCT_SURFACE_NAVIGATION_MESSAGING_READ_ONLY", surface.Taxonomy.LineId);
        Assert.AreEqual("COMPLETE_READ_ONLY_PREVIEW_SAFE_FIXTURE_SAFE_PRODUCT_SURFACE_CLOSED", surface.Taxonomy.ClosedProductSurfaceStatus);
        Assert.AreEqual(Enum.GetValues<RecipeProductSurfaceNavigationEntryKind>().Length, surface.Taxonomy.NavigationLabels.Count);
        Assert.AreEqual(Enum.GetValues<RecipeProductSurfaceCapabilityBadgeKind>().Length, surface.Taxonomy.CapabilityBadges.Count);
        Assert.AreEqual(Enum.GetValues<RecipeProductSurfaceDisabledActionKind>().Length, surface.Taxonomy.DisabledActionMessages.Count);
        Assert.AreEqual(Enum.GetValues<RecipeProductSurfaceDemoFlowStepKind>().Length, surface.DemoFlow.Steps.Count);
        Assert.IsTrue(surface.DemoFlow.Microcopy.EmptyStates.Count >= 7);
    }

    [TestMethod]
    [TestCategory("RecipeProductSurfaceNavigationMessagingFinalPolish")]
    public void FinalCopyConsistencySetContainsRequiredSafeLanguage()
    {
        var copy = Surface().FinalCopyConsistencySet;

        CollectionAssert.IsSubsetOf(
            new[]
            {
                "read-only",
                "preview-safe",
                "fixture-safe",
                "demo-safe",
                "live runtime blocked",
                "automation not enabled",
                "connector execution disabled",
                "secrets by reference only",
                "export preview only",
                "no real file generated",
                "no workitems processed",
                "safe next action: review readiness and prepare requirements"
            },
            copy.ToArray());
    }

    [TestMethod]
    [TestCategory("RecipeProductSurfaceNavigationMessagingFinalPolish")]
    public void AuditReadinessMatrixCoversAllFinalAuditAreas()
    {
        var matrix = Surface().FinalComposition.AuditMatrix;

        CollectionAssert.AreEquivalent(
            Enum.GetValues<RecipeProductSurfaceNavigationMessagingAuditArea>(),
            matrix.Select(item => item.Area).ToArray());

        foreach (var item in matrix)
        {
            Assert.AreEqual(RecipeProductSurfaceNavigationMessagingAuditReadinessStatus.ReadyForFinalAudit, item.Status, item.Area.ToString());
            Assert.IsFalse(item.BlocksFinalAudit, item.Area.ToString());
            Assert.IsFalse(item.GrantsRuntimeCapability, item.Area.ToString());
            Assert.IsFalse(item.GrantsLiveRuntime, item.Area.ToString());
            Assert.IsFalse(string.IsNullOrWhiteSpace(item.RedactedSummary), item.Area.ToString());
            Assert.IsFalse(string.IsNullOrWhiteSpace(item.EvidenceSummary), item.Area.ToString());
        }
    }

    [TestMethod]
    [TestCategory("RecipeProductSurfaceNavigationMessagingFinalPolish")]
    public void DisabledActionMessagingRemainsExplicitAndSafe()
    {
        foreach (var action in Surface().Taxonomy.DisabledActionMessages)
        {
            Assert.IsFalse(action.Available, action.Label);
            Assert.IsFalse(action.CanInvoke, action.Label);
            Assert.IsFalse(action.GrantsLiveRuntime, action.Label);
            Assert.IsFalse(action.CallsConnectorOrNetwork, action.Label);
            Assert.IsFalse(action.ReadsSecrets, action.Label);
            Assert.IsFalse(action.WritesExternalSystem, action.Label);
            Assert.IsFalse(action.WritesFile, action.Label);
            StringAssert.Contains(action.BlockedReason, "not enabled", action.Label);
            Assert.IsFalse(string.IsNullOrWhiteSpace(action.SafeNextAction), action.Label);
        }
    }

    [TestMethod]
    [TestCategory("RecipeProductSurfaceNavigationMessagingFinalPolish")]
    public void AllowedClaimIsPresentAndForbiddenClaimIsNotProductFacing()
    {
        var surface = Surface();
        var copy = ProductFacingCopy(surface).ToArray();

        Assert.AreEqual(AllowedClaim, surface.FinalComposition.AllowedFinalClaim);
        Assert.AreEqual(ForbiddenClaim, surface.FinalComposition.ForbiddenFinalClaim);
        CollectionAssert.Contains(copy, AllowedClaim);
        CollectionAssert.DoesNotContain(copy, ForbiddenClaim);
        Assert.IsFalse(copy.Any(text => text.Contains("live automate", StringComparison.OrdinalIgnoreCase)));
    }

    [TestMethod]
    [TestCategory("RecipeProductSurfaceNavigationMessagingFinalPolish")]
    public void ProductFacingCopyAvoidsForbiddenLiveActionClaims()
    {
        var hits = RecipeProductSurfaceCopyPolicy.FindForbiddenCopy(ProductFacingCopy(Surface()));

        Assert.AreEqual(0, hits.Count, string.Join(Environment.NewLine, hits));
    }

    [TestMethod]
    [TestCategory("RecipeProductSurfaceNavigationMessagingFinalPolish")]
    public void LiveActionTermsAppearOnlyInBlockedDisabledOrNegatedContexts()
    {
        var riskyTerms = new[]
        {
            "execution",
            "live runtime",
            "live automation",
            "automation",
            "connector",
            "API",
            "vault",
            "secret",
            "scheduler",
            "watcher",
            "hook",
            "listener",
            "recording",
            "playback",
            "capture",
            "workitem",
            "export",
            "mutation",
            "browser",
            "desktop"
        };

        foreach (var text in AllCopy(Surface()))
        {
            if (!riskyTerms.Any(term => text.Contains(term, StringComparison.OrdinalIgnoreCase)))
                continue;

            Assert.IsTrue(IsBlockedOrNegated(text), $"Term must be safely negated or blocked: {text}");
        }
    }

    [TestMethod]
    [TestCategory("RecipeProductSurfaceNavigationMessagingFinalPolish")]
    public void NoConnectorApiVaultSchedulerRecorderExportWriteOrProtectedScopeCapabilitiesAreExposed()
    {
        var surface = Surface();
        var composition = surface.FinalComposition;

        Assert.IsFalse(composition.CanOpenConnector);
        Assert.IsFalse(composition.CanRequestSecrets);
        Assert.IsFalse(composition.CanCallNetwork);
        Assert.IsFalse(composition.CanCreateSchedulerWatcherHookOrListener);
        Assert.IsFalse(composition.CanCreateRecorderReplayOrCapture);
        Assert.IsFalse(composition.CanWriteExportFile);
        Assert.IsFalse(composition.CanApplyLocatorRepair);
        Assert.IsFalse(composition.LiveRuntimeEnabled);

        var productFacing = ProductFacingCopy(surface).ToArray();
        var enabledProtectedTerms = new[]
        {
            "CDP enabled",
            "Playwright enabled",
            "Selenium enabled",
            "Puppeteer enabled",
            "browser runtime enabled",
            "desktop runtime enabled",
            "connector enabled",
            "vault enabled",
            "real export enabled"
        };

        foreach (var marker in enabledProtectedTerms)
            Assert.IsFalse(productFacing.Any(text => text.Contains(marker, StringComparison.OrdinalIgnoreCase)), marker);
    }

    private static RecipeProductSurfaceNavigationMessagingFinalPolishSurface Surface() =>
        RecipeProductSurfaceFactory.CreateNavigationMessagingFinalPolishSurface();

    private static IEnumerable<string> ProductFacingCopy(RecipeProductSurfaceNavigationMessagingFinalPolishSurface surface)
    {
        yield return surface.FinalComposition.AllowedFinalClaim;
        yield return surface.FinalComposition.NavigationTaxonomyReadiness;
        yield return surface.FinalComposition.CapabilityBadgeReadiness;
        yield return surface.FinalComposition.DisabledActionMessagingReadiness;
        yield return surface.FinalComposition.DemoFlowCopyReadiness;
        yield return surface.FinalComposition.EmptyStateReadiness;
        yield return surface.FinalComposition.ProductClaimGuardrailReadiness;
        yield return surface.FinalComposition.AuditReadinessSummary;

        foreach (var item in surface.FinalComposition.AuditMatrix)
        {
            yield return item.RedactedSummary;
            yield return item.EvidenceSummary;
        }

        foreach (var copy in surface.FinalCopyConsistencySet)
            yield return copy;
    }

    private static IEnumerable<string> AllCopy(RecipeProductSurfaceNavigationMessagingFinalPolishSurface surface) =>
        ProductFacingCopy(surface)
            .Concat(surface.Taxonomy.NavigationLabels.SelectMany(label => new[] { label.Label, label.OperatorSummary, label.RouteHint }))
            .Concat(surface.Taxonomy.CapabilityBadges.SelectMany(badge => new[] { badge.Label, badge.RedactedSummary }))
            .Concat(surface.Taxonomy.DisabledActionMessages.SelectMany(action => new[] { action.Label, action.BlockedReason, action.SafeNextAction }))
            .Concat(surface.DemoFlow.Steps.SelectMany(step => new[] { step.Title, step.Subtitle, step.OperatorDescription, step.BlockedActionNote, step.SafeNextAction, step.ClaimGuardrailReminder }))
            .Concat(surface.DemoFlow.Microcopy.EmptyStates.SelectMany(state => new[] { state.Label, state.RedactedSummary, state.SafeNextAction }))
            .Concat(surface.DemoFlow.Microcopy.DisabledControlCopy)
            .Concat(surface.DemoFlow.Microcopy.StepTransitions);

    private static bool IsBlockedOrNegated(string text)
    {
        var safeMarkers = new[]
        {
            "blocked",
            "not enabled",
            "disabled",
            "not available",
            "unavailable",
            "never",
            "no live",
            "no real",
            "no export",
            "no step",
            "no connector",
            "no credentials",
            "no browser",
            "no recording",
            "no automatic",
            "no workitems",
            "no protected",
            "read-only",
            "preview",
            "fixture-safe",
            "by reference only",
            "metadata only",
            "outside",
            "remain",
            "remains",
            "grant no",
            "false",
            "excluded",
            "cannot",
            "not runtime-ready",
            "only",
            "without live behavior"
        };

        return safeMarkers.Any(marker => text.Contains(marker, StringComparison.OrdinalIgnoreCase));
    }
}
