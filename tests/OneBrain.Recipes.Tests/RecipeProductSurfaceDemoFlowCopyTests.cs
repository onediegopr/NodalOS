using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.Core.Recipes;

namespace OneBrain.Recipes.Tests;

[TestClass]
[TestCategory("RecipeProductSurfaceDemoFlowCopy")]
public sealed class RecipeProductSurfaceDemoFlowCopyTests
{
    private const string AllowedClaim = "NODAL OS has a fixture-safe Recipe Runtime product surface with read-only catalog, lab, templates, readiness explanations, operator previews and handoff/export preview summaries.";
    private const string ForbiddenClaim = "NODAL OS can execute/live automate these recipes.";

    [TestMethod]
    [TestCategory("RecipeProductSurfaceDemoFlowCopy")]
    public void DemoFlowSurfaceIsReadOnlyAndDoesNotExposeRuntimeFlags()
    {
        var surface = Surface();

        Assert.IsTrue(surface.ReadOnly);
        Assert.IsTrue(surface.PreviewSafe);
        Assert.IsTrue(surface.FixtureSafeOnly);
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
    [TestCategory("RecipeProductSurfaceDemoFlowCopy")]
    public void DemoFlowContainsRequiredReadOnlyJourneySteps()
    {
        var steps = Surface().Steps;

        CollectionAssert.AreEquivalent(
            Enum.GetValues<RecipeProductSurfaceDemoFlowStepKind>(),
            steps.Select(step => step.Kind).ToArray());

        CollectionAssert.AreEqual(
            new[]
            {
                "Browse Recipe Catalog",
                "Review Recipe Lab",
                "Open Template Detail",
                "Read Readiness Explanation",
                "Review Operator Preview",
                "Review Handoff/Export Preview",
                "Understand blocked live runtime",
                "Understand safe next action"
            },
            steps.Select(step => step.Title).ToArray());
    }

    [TestMethod]
    [TestCategory("RecipeProductSurfaceDemoFlowCopy")]
    public void EveryStepHasSafetyBadgesSafeNextActionAndDisabledActionCopy()
    {
        foreach (var step in Surface().Steps)
        {
            Assert.IsTrue(step.ReadOnly, step.StepId);
            Assert.IsTrue(step.PreviewSafe, step.StepId);
            Assert.IsTrue(step.SafetyBadges.Count > 0, step.StepId);
            Assert.IsFalse(string.IsNullOrWhiteSpace(step.SafeNextAction), step.StepId);
            Assert.IsFalse(string.IsNullOrWhiteSpace(step.BlockedActionNote), step.StepId);
            Assert.IsTrue(step.UnavailableActionLabels.Count > 0, step.StepId);
            Assert.IsFalse(string.IsNullOrWhiteSpace(step.ClaimGuardrailReminder), step.StepId);
            Assert.IsFalse(step.StartsRecipeRun, step.StepId);
            Assert.IsFalse(step.ProcessesWorkitem, step.StepId);
            Assert.IsFalse(step.OpensConnector, step.StepId);
            Assert.IsFalse(step.RequestsSecrets, step.StepId);
            Assert.IsFalse(step.CallsNetwork, step.StepId);
            Assert.IsFalse(step.WritesFile, step.StepId);
            Assert.IsFalse(step.EnablesBrowserAutomation, step.StepId);
            Assert.IsFalse(step.EnablesDesktopAutomation, step.StepId);
            Assert.IsFalse(step.CreatesRecorderReplayOrCapture, step.StepId);
            Assert.IsFalse(step.EnablesExternalMutation, step.StepId);
            Assert.IsFalse(step.LiveRuntimeEnabled, step.StepId);
        }
    }

    [TestMethod]
    [TestCategory("RecipeProductSurfaceDemoFlowCopy")]
    public void MicrocopyCoversEmptyStatesDisabledControlsAndBoundaryMessages()
    {
        var copy = Surface().Microcopy;

        StringAssert.Contains(copy.Intro, "Preview-only demo flow");
        StringAssert.Contains(copy.BlockedLiveRuntimeCopy, "No live runtime available");
        StringAssert.Contains(copy.ExportPreviewOnlyCopy, "Export preview only");
        StringAssert.Contains(copy.NoCredentialsReadCopy, "No credentials are read");
        StringAssert.Contains(copy.NoConnectorApiCallsCopy, "No connector/API calls are made");
        StringAssert.Contains(copy.NoBrowserDesktopAutomationCopy, "No browser or desktop automation is performed");
        StringAssert.Contains(copy.NoRecordingPlaybackCaptureCopy, "No recording, playback, or real capture is performed");
        StringAssert.Contains(copy.NoAutomaticWorkitemProcessingCopy, "No automatic workitem processing is performed");
        Assert.AreEqual(8, copy.StepTransitions.Count);

        CollectionAssert.IsSubsetOf(
            new[]
            {
                "No live runtime available",
                "No connector connected",
                "No credentials requested",
                "No export file generated",
                "No workitems processed",
                "No browser or desktop automation performed",
                "Preview data only"
            },
            copy.EmptyStates.Select(state => state.Label).ToArray());

        foreach (var state in copy.EmptyStates)
        {
            Assert.IsTrue(state.ReadOnly, state.StateId);
            Assert.IsTrue(state.PreviewOnly, state.StateId);
            Assert.IsFalse(state.StartsRecipeRun, state.StateId);
            Assert.IsFalse(state.CallsNetwork, state.StateId);
            Assert.IsFalse(state.ReadsSecrets, state.StateId);
            Assert.IsFalse(state.WritesFile, state.StateId);
        }
    }

    [TestMethod]
    [TestCategory("RecipeProductSurfaceDemoFlowCopy")]
    public void ProductFacingCopyPreservesAllowedClaimAndExcludesForbiddenClaim()
    {
        var surface = Surface();
        var productCopy = ProductFacingCopy(surface).ToArray();

        Assert.AreEqual(AllowedClaim, surface.AllowedFinalClaim);
        Assert.AreEqual(ForbiddenClaim, surface.ForbiddenFinalClaim);
        CollectionAssert.Contains(productCopy, AllowedClaim);
        CollectionAssert.DoesNotContain(productCopy, ForbiddenClaim);
        Assert.IsFalse(productCopy.Any(text => text.Contains("live automate", StringComparison.OrdinalIgnoreCase)));
    }

    [TestMethod]
    [TestCategory("RecipeProductSurfaceDemoFlowCopy")]
    public void ProductFacingCopyAvoidsForbiddenLiveActionClaims()
    {
        var surface = Surface();
        var hits = RecipeProductSurfaceCopyPolicy.FindForbiddenCopy(ProductFacingCopy(surface));

        Assert.AreEqual(0, hits.Count, string.Join(Environment.NewLine, hits));
    }

    [TestMethod]
    [TestCategory("RecipeProductSurfaceDemoFlowCopy")]
    public void LiveActionTermsAppearOnlyInBlockedDisabledOrNegatedContexts()
    {
        var riskyTerms = new[]
        {
            "execution",
            "live runtime",
            "live automation",
            "connector/API",
            "vault",
            "secret",
            "browser automation",
            "desktop automation",
            "recording",
            "playback",
            "capture",
            "workitem",
            "fiscal",
            "payment",
            "marketplace",
            "message",
            "delete",
            "write",
            "export"
        };

        foreach (var text in AllCopy(Surface()))
        {
            if (!riskyTerms.Any(term => text.Contains(term, StringComparison.OrdinalIgnoreCase)))
                continue;

            Assert.IsTrue(IsApprovedSafeRiskCopy(text), $"Risk term must be inside an approved safe copy entry: {text}");
        }
    }

    [TestMethod]
    [TestCategory("RecipeProductSurfaceDemoFlowCopy")]
    public void DemoFlowReusesPhaseOneTaxonomyAndDisabledActions()
    {
        var surface = Surface();

        Assert.AreEqual("NODAL_RECIPE_PRODUCT_SURFACE_NAVIGATION_MESSAGING_READ_ONLY", surface.Taxonomy.LineId);
        Assert.AreEqual("COMPLETE_READ_ONLY_PREVIEW_SAFE_FIXTURE_SAFE_PRODUCT_SURFACE_CLOSED", surface.Taxonomy.ClosedProductSurfaceStatus);
        Assert.AreEqual(Enum.GetValues<RecipeProductSurfaceNavigationEntryKind>().Length, surface.Taxonomy.NavigationLabels.Count);
        Assert.AreEqual(Enum.GetValues<RecipeProductSurfaceDisabledActionKind>().Length, surface.Taxonomy.DisabledActionMessages.Count);
        Assert.IsTrue(surface.Steps.Any(step => step.UnavailableActionLabels.Count == surface.Taxonomy.DisabledActionMessages.Count));
    }

    private static RecipeProductSurfaceDemoFlowCopySurface Surface() =>
        RecipeProductSurfaceFactory.CreateDemoFlowCopySurface();

    private static IEnumerable<string> ProductFacingCopy(RecipeProductSurfaceDemoFlowCopySurface surface)
    {
        yield return surface.AllowedFinalClaim;
        yield return surface.Microcopy.Intro;
        yield return surface.Microcopy.BlockedLiveRuntimeCopy;
        yield return surface.Microcopy.ExportPreviewOnlyCopy;
        yield return surface.Microcopy.NoCredentialsReadCopy;
        yield return surface.Microcopy.NoConnectorApiCallsCopy;
        yield return surface.Microcopy.NoBrowserDesktopAutomationCopy;
        yield return surface.Microcopy.NoRecordingPlaybackCaptureCopy;
        yield return surface.Microcopy.NoAutomaticWorkitemProcessingCopy;
        yield return surface.Microcopy.FinalSummary;

        foreach (var transition in surface.Microcopy.StepTransitions)
            yield return transition;

        foreach (var state in surface.Microcopy.EmptyStates)
        {
            yield return state.Label;
            yield return state.RedactedSummary;
            yield return state.SafeNextAction;
        }

        foreach (var disabled in surface.Microcopy.DisabledControlCopy)
            yield return disabled;

        foreach (var step in surface.Steps)
        {
            yield return step.Title;
            yield return step.Subtitle;
            yield return step.OperatorDescription;
            yield return step.BlockedActionNote;
            yield return step.SafeNextAction;
            yield return step.ClaimGuardrailReminder;
            foreach (var label in step.UnavailableActionLabels)
                yield return label;
        }
    }

    private static IEnumerable<string> AllCopy(RecipeProductSurfaceDemoFlowCopySurface surface) =>
        ProductFacingCopy(surface)
            .Concat(surface.Taxonomy.CapabilityBadges.SelectMany(b => new[] { b.Label, b.RedactedSummary }))
            .Concat(surface.Taxonomy.DisabledActionMessages.SelectMany(a => new[] { a.Label, a.BlockedReason, a.SafeNextAction }));

    private static bool IsApprovedSafeRiskCopy(string text)
    {
        var approvedExactCopy = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Connector execution disabled",
            "Secrets by reference only",
            "Export preview only",
            "Review Handoff/Export Preview",
            "Live runtime blocked",
            "Not automated",
            "Recipe execution blocked",
            "Workitem processing blocked",
            "Connector/API blocked",
            "Vault/secrets blocked",
            "Browser automation blocked",
            "Desktop automation blocked",
            "Recording/playback/capture-draft blocked",
            "Export file generation blocked",
            "Fiscal/payment/marketplace/message/delete/write blocked",
            "Review connector eligibility and tool trust refs only.",
            "Review required secret aliases or refs by reference only.",
            "Review preview-only capture draft summaries.",
            "Review handoff/export preview metadata next.",
            "Review or copy the safe handoff summary text.",
            "Request human review path and keep the item blocked for live action.",
            "Secret values are never requested or shown.",
            "Browser, desktop, connector, vault, recorder, and external mutation paths are blocked.",
            "No live runtime available",
            "No connector connected",
            "No credentials requested",
            "No export file generated",
            "No workitems processed",
            "No browser or desktop automation performed",
            "Preview data only"
        };

        if (approvedExactCopy.Contains(text))
            return true;

        var approvedPhrasePatterns = new[]
        {
            "is not enabled",
            "are not enabled",
            "not available",
            "does not write",
            "remain unavailable",
            "remains unavailable",
            "remain not enabled",
            "remains disabled",
            "remains blocked",
            "remain blocked",
            "stay blocked",
            "stays blocked",
            "blocked live",
            "blocked runtime",
            "disabled control",
            "blocked reasons",
            "unavailable action",
            "not automated",
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
            "read-only",
            "preview-only",
            "preview-safe",
            "fixture-safe",
            "by reference only",
            "metadata only",
            "aliases only",
            "cannot",
            "without live behavior"
        };

        return approvedPhrasePatterns.Any(pattern => text.Contains(pattern, StringComparison.OrdinalIgnoreCase));
    }
}
