using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.Core.Recipes;

namespace OneBrain.Recipes.Tests;

[TestClass]
[TestCategory("RecipeProductSurfaceNavigationMessaging")]
public sealed class RecipeProductSurfaceNavigationMessagingTests
{
    private const string AllowedClaim = "NODAL OS has a fixture-safe Recipe Runtime product surface with read-only catalog, lab, templates, readiness explanations, operator previews and handoff/export preview summaries.";
    private const string ForbiddenClaim = "NODAL OS can execute/live automate these recipes.";

    [TestMethod]
    [TestCategory("RecipeProductSurfaceNavigationMessaging")]
    public void NavigationMessagingTaxonomyIsReadOnlyAndDoesNotExposeRuntimeFlags()
    {
        var taxonomy = Taxonomy();

        Assert.IsTrue(taxonomy.ReadOnly);
        Assert.IsTrue(taxonomy.PreviewSafe);
        Assert.IsTrue(taxonomy.FixtureSafeOnly);
        Assert.IsFalse(taxonomy.CanStartRecipeRun);
        Assert.IsFalse(taxonomy.CanProcessWorkitem);
        Assert.IsFalse(taxonomy.CanEnableLiveRuntime);
        Assert.IsFalse(taxonomy.CanOpenConnector);
        Assert.IsFalse(taxonomy.CanRequestSecrets);
        Assert.IsFalse(taxonomy.CanCallNetwork);
        Assert.IsFalse(taxonomy.CanCreateSchedulerWatcherHookOrListener);
        Assert.IsFalse(taxonomy.CanCreateRecorderReplayOrCapture);
        Assert.IsFalse(taxonomy.CanWriteExportFile);
        Assert.IsFalse(taxonomy.CanApplyLocatorRepair);
        Assert.IsFalse(taxonomy.LiveRuntimeEnabled);
        Assert.AreEqual("COMPLETE_READ_ONLY_PREVIEW_SAFE_FIXTURE_SAFE_PRODUCT_SURFACE_CLOSED", taxonomy.ClosedProductSurfaceStatus);
        Assert.AreEqual("df92f6fb4c86f246e1d956ede9fd4876e1d0080d", taxonomy.FinalCloseCommit);
    }

    [TestMethod]
    [TestCategory("RecipeProductSurfaceNavigationMessaging")]
    public void NavigationLabelsCoverClosedSurfaceWithoutLiveExecutionLanguage()
    {
        var labels = Taxonomy().NavigationLabels;

        CollectionAssert.AreEquivalent(
            Enum.GetValues<RecipeProductSurfaceNavigationEntryKind>(),
            labels.Select(label => label.Kind).ToArray());

        CollectionAssert.AreEquivalent(
            new[]
            {
                "Recipe Catalog",
                "Recipe Lab",
                "Template Detail",
                "Readiness Explanation",
                "Operator Preview",
                "Handoff/Export Preview",
                "Safe Demo"
            },
            labels.Select(label => label.Label).ToArray());

        foreach (var label in labels)
        {
            Assert.IsTrue(label.ReadOnly, label.Label);
            Assert.IsTrue(label.PreviewSafe, label.Label);
            Assert.IsFalse(label.StartsRecipeRun, label.Label);
            Assert.IsFalse(label.OpensConnector, label.Label);
            Assert.IsFalse(label.RequestsSecrets, label.Label);
            Assert.IsFalse(label.EnablesAutomation, label.Label);
            Assert.IsFalse(label.CreatesCaptureReplay, label.Label);
            Assert.IsFalse(label.LiveRuntimeEnabled, label.Label);
        }

        var hits = RecipeProductSurfaceCopyPolicy.FindForbiddenCopy(labels.SelectMany(LabelCopy));
        Assert.AreEqual(0, hits.Count, string.Join(Environment.NewLine, hits));
    }

    [TestMethod]
    [TestCategory("RecipeProductSurfaceNavigationMessaging")]
    public void CapabilityBadgesAreSafeAndDoNotGrantCapabilities()
    {
        var badges = Taxonomy().CapabilityBadges;

        CollectionAssert.AreEquivalent(
            Enum.GetValues<RecipeProductSurfaceCapabilityBadgeKind>(),
            badges.Select(badge => badge.Kind).ToArray());

        CollectionAssert.IsSubsetOf(
            new[]
            {
                "Read-only",
                "Preview-safe",
                "Fixture-safe",
                "Demo-safe",
                "Live runtime blocked",
                "Connector execution disabled",
                "Secrets by reference only",
                "Export preview only",
                "Human approval path required",
                "Not automated"
            },
            badges.Select(badge => badge.Label).ToArray());

        foreach (var badge in badges)
        {
            Assert.IsFalse(badge.GrantsCapability, badge.Label);
            Assert.IsFalse(badge.GrantsLiveRuntime, badge.Label);
            Assert.IsFalse(badge.GrantsConnectorExecution, badge.Label);
            Assert.IsFalse(badge.GrantsSecretAccess, badge.Label);
            Assert.IsFalse(badge.GrantsExternalMutation, badge.Label);
        }
    }

    [TestMethod]
    [TestCategory("RecipeProductSurfaceNavigationMessaging")]
    public void DisabledActionsAreExplicitBlockedAndHaveSafeNextActions()
    {
        var disabled = Taxonomy().DisabledActionMessages;

        CollectionAssert.AreEquivalent(
            Enum.GetValues<RecipeProductSurfaceDisabledActionKind>(),
            disabled.Select(action => action.Kind).ToArray());

        foreach (var action in disabled)
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
    [TestCategory("RecipeProductSurfaceNavigationMessaging")]
    public void AllowedClaimIsPreservedAndForbiddenClaimIsNotProductFacing()
    {
        var taxonomy = Taxonomy();

        Assert.AreEqual(AllowedClaim, taxonomy.AllowedFinalClaim);
        Assert.AreEqual(ForbiddenClaim, taxonomy.ForbiddenFinalClaim);

        var productFacingCopy = ProductFacingCopy(taxonomy).ToArray();
        CollectionAssert.Contains(productFacingCopy, AllowedClaim);
        CollectionAssert.DoesNotContain(productFacingCopy, ForbiddenClaim);

        foreach (var copy in productFacingCopy)
            Assert.IsFalse(copy.Contains("live automate", StringComparison.OrdinalIgnoreCase), copy);
    }

    [TestMethod]
    [TestCategory("RecipeProductSurfaceNavigationMessaging")]
    public void LiveActionTermsOnlyAppearInBlockedOrNegatedCopy()
    {
        var taxonomy = Taxonomy();
        var copy = ProductFacingCopy(taxonomy)
            .Concat(taxonomy.DisabledActionMessages.SelectMany(ActionCopy))
            .Concat(taxonomy.CapabilityBadges.SelectMany(BadgeCopy))
            .ToArray();

        var riskyTerms = new[]
        {
            "execution",
            "connector/API",
            "vault",
            "secret reading",
            "browser automation",
            "desktop automation",
            "recorder",
            "replay",
            "capture",
            "fiscal",
            "payment",
            "marketplace",
            "message",
            "delete",
            "write"
        };

        foreach (var text in copy)
        {
            if (!riskyTerms.Any(term => text.Contains(term, StringComparison.OrdinalIgnoreCase)))
                continue;

            Assert.IsTrue(
                IsBlockedOrNegated(text),
                $"Live/action term must be blocked or negated: {text}");
        }
    }

    [TestMethod]
    [TestCategory("RecipeProductSurfaceNavigationMessaging")]
    public void ProductCopyPolicyCatchesForbiddenNavigationWords()
    {
        var forbidden = RecipeProductSurfaceCopyPolicy.FindForbiddenCopy(
            ["Run recipe", "Execute", "Automation ready", "Connect", "Capture now", "Replay", "Use credentials"]);

        Assert.IsTrue(forbidden.Count >= 6, string.Join(Environment.NewLine, forbidden));

        var taxonomy = Taxonomy();
        var hits = RecipeProductSurfaceCopyPolicy.FindForbiddenCopy(ProductFacingCopy(taxonomy));
        Assert.AreEqual(0, hits.Count, string.Join(Environment.NewLine, hits));
    }

    [TestMethod]
    [TestCategory("RecipeProductSurfaceNavigationMessaging")]
    public void NoRuntimeDependencyOrProviderAssumptionIsExposed()
    {
        var taxonomy = Taxonomy();
        var allCopy = ProductFacingCopy(taxonomy)
            .Concat(taxonomy.DisabledActionMessages.SelectMany(ActionCopy))
            .Concat(taxonomy.CapabilityBadges.SelectMany(BadgeCopy))
            .ToArray();

        var forbiddenRuntimeMarkers = new[]
        {
            "connection string",
            "access token",
            "refresh token",
            "client secret",
            "api key",
            "OAuth flow",
            "SQL query",
            "REST request builder",
            "ToolJet",
            "AGPL"
        };

        foreach (var marker in forbiddenRuntimeMarkers)
            Assert.IsFalse(allCopy.Any(text => text.Contains(marker, StringComparison.OrdinalIgnoreCase)), marker);
    }

    private static RecipeProductSurfaceNavigationMessagingTaxonomy Taxonomy() =>
        RecipeProductSurfaceFactory.CreateNavigationMessagingTaxonomy();

    private static IEnumerable<string> ProductFacingCopy(RecipeProductSurfaceNavigationMessagingTaxonomy taxonomy)
    {
        yield return taxonomy.AllowedFinalClaim;
        yield return taxonomy.NoLiveNoAutomationCopyPolicy;
        foreach (var label in taxonomy.NavigationLabels)
        {
            foreach (var text in LabelCopy(label))
                yield return text;
        }
    }

    private static IEnumerable<string> LabelCopy(RecipeProductSurfaceNavigationLabel label)
    {
        yield return label.Label;
        yield return label.OperatorSummary;
        yield return label.RouteHint;
        yield return label.PrimaryBadge.ToString();
    }

    private static IEnumerable<string> BadgeCopy(RecipeProductSurfaceCapabilityStatusBadge badge)
    {
        yield return badge.Label;
        yield return badge.RedactedSummary;
    }

    private static IEnumerable<string> ActionCopy(RecipeProductSurfaceDisabledActionMessage action)
    {
        yield return action.Label;
        yield return action.BlockedReason;
        yield return action.SafeNextAction;
    }

    private static bool IsBlockedOrNegated(string text)
    {
        var safeMarkers = new[]
        {
            "blocked",
            "not enabled",
            "disabled",
            "not available",
            "never requested",
            "never shown",
            "preview",
            "by reference only",
            "no real file",
            "no live"
        };

        return safeMarkers.Any(marker => text.Contains(marker, StringComparison.OrdinalIgnoreCase));
    }
}
