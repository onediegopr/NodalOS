using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.Core.Approval;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("ApprovalHumanReview")]
[TestCategory("PhaseEApprovalHumanReview")]
public sealed class ProductLedgerRenderedRouteInteractionTests
{
    private static readonly string[] RequiredAnchors =
    [
        "product-ledger-surface-root",
        "product-ledger-authority",
        "product-ledger-verification-status",
        "product-ledger-checkpoint-status",
        "product-ledger-redaction-retention-status",
        "product-ledger-concurrency-status",
        "product-ledger-bounded-export-status",
        "product-ledger-operator-acceptance-status",
        "product-ledger-public-local-only-action-contract-status",
        "product-ledger-visual-evidence-status",
        "product-ledger-screenshot-evidence-status",
        "product-ledger-blocked-frontiers",
        "product-ledger-safe-next-steps"
    ];

    [TestMethod]
    public void RenderedRouteFallback_ExposesRequiredDomAnchorsWithNonEmptyLocalOnlyContent()
    {
        var result = Render();
        var html = result.HtmlSnapshot;

        Assert.AreEqual(ProductLedgerLocalDevRoutePreviewDecision.RenderedLocalDevInternalPreview, result.Decision);
        Assert.AreEqual("text/html; charset=utf-8", result.ContentType);
        Assert.AreEqual("/internal/product-ledger/operator-surface", result.RouteTemplatePreview);
        Assert.IsTrue(result.CanonicalSurface.UsesFixtureReadModel);
        Assert.IsTrue(result.CanonicalSurface.IsLocalOnly);
        Assert.IsTrue(result.CanonicalSurface.IsDevelopmentOnly);
        Assert.IsTrue(result.CanonicalSurface.IsReadOnly);
        Assert.IsFalse(result.CanonicalSurface.AllowsProductCommandExecution);

        foreach (var anchor in RequiredAnchors)
        {
            AssertAnchorHasContent(html, anchor);
        }

        StringAssert.Contains(html, "ProductLedgerLocalOnlyLineScoped");
        StringAssert.Contains(html, nameof(ProductLedgerPathLocalOnlyActiveWriter));
        StringAssert.Contains(html, "local-only");
        StringAssert.Contains(html, "development-only");
        StringAssert.Contains(html, "read-only");
        StringAssert.Contains(html, "no release/commercial");
        StringAssert.Contains(html, "not compliance-grade custody");
    }

    [TestMethod]
    public void RenderedRouteFallback_RendersBlockedFrontiersAndSafeNextSteps()
    {
        var result = Render();
        var html = result.HtmlSnapshot;
        var expectedFrontiers = new[]
        {
            "product-command-execution",
            "public-internet",
            "external-network-provider-cloud",
            "db-migration",
            "kms-worm-external-trust",
            "browser-cdp-wcu-ocr-recipes-live",
            "release-commercial",
            "destructive-action",
            "unbounded-export"
        };

        foreach (var frontier in expectedFrontiers)
        {
            AssertAnchorHasContent(html, $"surface-blocked-{frontier}");
        }

        StringAssert.Contains(html, "RENDERED_UI_INTERACTION_LOCAL_ONLY_TEST_PACK");
        StringAssert.Contains(html, "LOCAL_APPROVAL_TO_ACTION_READ_ONLY_PREVIEW_LOOP");
        StringAssert.Contains(html, "DELETION_LIFECYCLE_DESIGN_ONLY");
    }

    [TestMethod]
    public void RenderedRouteFallback_ActionPreviewsAreDisabledReadOnlyAndNoOp()
    {
        var result = Render();
        var html = result.HtmlSnapshot;
        var canonicalButtons = Regex.Matches(
            html,
            "<button[^>]+data-testid=\"product-ledger-action-preview-[^\"]+\"[^>]*>",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        Assert.IsTrue(canonicalButtons.Count > 0, "No canonical action preview buttons rendered.");
        foreach (Match button in canonicalButtons)
        {
            var value = button.Value;
            StringAssert.Contains(value, "disabled");
            StringAssert.Contains(value, "aria-disabled=\"true\"");
            StringAssert.Contains(value, "data-read-only=\"true\"");
            StringAssert.Contains(value, "data-executable=\"false\"");
            StringAssert.Contains(value, "data-handler-id=\"\"");
            StringAssert.Contains(value, "data-callback=\"\"");
            Assert.IsFalse(value.Contains("formaction=", StringComparison.OrdinalIgnoreCase));
            Assert.IsFalse(value.Contains("onclick=", StringComparison.OrdinalIgnoreCase));
        }

        Assert.IsFalse(html.Contains("data-executable=\"true\"", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(html.Contains("<form", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(html.Contains("formaction=", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(html.Contains("onclick=", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(html.Contains("<script", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(html.Contains("href=\"http", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(html.Contains("src=\"http", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public void RenderedRouteFallback_SourceDoesNotInvokeWriteExportRunOrNetworkBoundaries()
    {
        var source = string.Join(
            Environment.NewLine,
            File.ReadAllText(Path.Combine(RepoRoot(), "src", "OneBrain.Core", "Approval", "ProductLedgerLocalDevRoutePreview.cs")),
            File.ReadAllText(Path.Combine(RepoRoot(), "src", "OneBrain.Core", "Approval", "ProductLedgerOperatorSurfaceModel.cs")));
        var forbiddenFragments = new[]
        {
            "Process.Start",
            "HttpClient",
            "WebSocket",
            "DbContext",
            "MigrationBuilder",
            "File.AppendAllText",
            "File.WriteAllText",
            "File.WriteAllBytes",
            "File.Delete",
            "MapPost",
            ".Append(",
            ".Export(",
            "PilotRecipeExecutor",
            "PilotRecipeExecutionGate.Evaluate(",
            "NODAL_OS_ENABLE_PILOT_RECIPE_EXECUTION"
        };

        foreach (var fragment in forbiddenFragments)
        {
            Assert.IsFalse(source.Contains(fragment, StringComparison.OrdinalIgnoreCase), fragment);
        }
    }

    private static ProductLedgerLocalDevRoutePreviewResult Render() =>
        new ProductLedgerLocalDevRoutePreview().Render(
            ProductLedgerLocalDevRoutePreview.CreateDefaultLocalDevRequest());

    private static void AssertAnchorHasContent(string html, string testId)
    {
        var match = Regex.Match(
            html,
            $"data-testid=\"{Regex.Escape(testId)}\"[^>]*>(?<content>.*?)<",
            RegexOptions.Singleline | RegexOptions.CultureInvariant);
        Assert.IsTrue(match.Success, $"Missing anchor {testId}.");
        Assert.IsFalse(string.IsNullOrWhiteSpace(StripTags(match.Groups["content"].Value)), $"Empty anchor {testId}.");
    }

    private static string StripTags(string value) =>
        Regex.Replace(value, "<.*?>", string.Empty, RegexOptions.Singleline | RegexOptions.CultureInvariant).Trim();

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
                "ProductLedgerLocalDevRoutePreview.cs")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        Assert.Fail("repo root not found");
        return string.Empty;
    }
}
