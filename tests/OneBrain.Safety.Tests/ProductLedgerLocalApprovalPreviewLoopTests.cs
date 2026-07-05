using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.Core.Approval;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("ApprovalHumanReview")]
[TestCategory("PhaseEApprovalHumanReview")]
public sealed class ProductLedgerLocalApprovalPreviewLoopTests
{
    private static readonly string[] RequiredAnchors =
    [
        "product-ledger-approval-preview",
        "product-ledger-candidate-action-preview",
        "product-ledger-policy-gate-preview",
        "product-ledger-noop-execution-preview",
        "product-ledger-preview-evidence-refs",
        "product-ledger-approval-safe-next-step"
    ];

    [TestMethod]
    public void LocalApprovalPreviewLoop_ModelIsLocalOnlyReadOnlyPreviewOnly()
    {
        var loop = Render().CanonicalSurface.ApprovalPreviewLoop;

        Assert.AreEqual(ProductLedgerLocalApprovalPreviewLoopFactory.LoopId, loop.LoopId);
        Assert.AreEqual(ProductLedgerOperatorSurfaceModelFactory.Scope, loop.Scope);
        Assert.IsTrue(loop.IsLocalOnly);
        Assert.IsTrue(loop.IsReadOnly);
        Assert.IsTrue(loop.IsPreviewOnly);
        Assert.IsFalse(loop.AllowsExecution);
        Assert.IsFalse(loop.AllowsWrite);
        Assert.IsFalse(loop.AllowsExport);
        Assert.IsFalse(loop.AllowsNetwork);
        Assert.IsFalse(loop.AllowsDb);
        Assert.IsFalse(loop.AllowsReleaseCommercial);
        Assert.IsTrue(loop.ApprovalPreview.HumanReviewRequired);
        Assert.IsTrue(loop.ApprovalPreview.ReadOnly);
        Assert.IsTrue(loop.ApprovalPreview.PreviewOnly);
        Assert.AreEqual(ProductLedgerInternalCommandKind.ViewLedgerReadiness, loop.ActionPreview.CandidateActionKind);
        Assert.IsTrue(loop.ActionPreview.Disabled);
        Assert.IsTrue(loop.ActionPreview.NoOp);
        Assert.IsFalse(loop.ActionPreview.Executable);
        Assert.IsFalse(loop.ActionPreview.AllowsProductCommandExecution);
    }

    [TestMethod]
    public void LocalApprovalPreviewLoop_RendersRequiredDomAnchorsWithPreviewOnlyWarnings()
    {
        var result = Render();
        var html = result.HtmlSnapshot;

        foreach (var anchor in RequiredAnchors)
        {
            AssertAnchorHasContent(html, anchor);
        }

        StringAssert.Contains(html, "read-only");
        StringAssert.Contains(html, "preview-only");
        StringAssert.Contains(html, "no product command execution");
        StringAssert.Contains(html, "no write/export");
        StringAssert.Contains(html, "no release/commercial");
        StringAssert.Contains(html, "data-executable=\"false\"");
        StringAssert.Contains(html, "disabled aria-disabled=\"true\"");
        Assert.IsFalse(html.Contains("data-executable=\"true\"", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(html.Contains("<form", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(html.Contains("<script", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(html.Contains("formaction=", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(html.Contains("onclick=", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public void LocalApprovalPreviewLoop_PolicyAndNoOpResultBlockExecutionWriteExportAndPilotRun()
    {
        var loop = Render().CanonicalSurface.ApprovalPreviewLoop;
        var gate = loop.PolicyGatePreview;
        var noop = loop.NoOpExecutionPreview;

        Assert.AreEqual(ProductLedgerLocalApprovalPreviewDecision.NeedsHumanReviewPreview, gate.PolicyDecision);
        Assert.IsFalse(gate.AllowsExecution);
        Assert.IsFalse(gate.AllowsWrite);
        Assert.IsFalse(gate.AllowsExport);
        Assert.IsFalse(gate.AllowsNetwork);
        Assert.IsFalse(gate.AllowsDb);
        Assert.IsFalse(gate.AllowsReleaseCommercial);
        Assert.IsFalse(noop.HandlerInvoked);
        Assert.IsFalse(noop.CallbackInvoked);
        Assert.IsFalse(noop.AppendInvoked);
        Assert.IsFalse(noop.WriteInvoked);
        Assert.IsFalse(noop.ExportInvoked);
        Assert.IsFalse(noop.PilotRunInvoked);
        StringAssert.Contains(noop.NoOpResult, "NO_OP_PREVIEW_ONLY");
    }

    [TestMethod]
    public void LocalApprovalPreviewLoop_BlockedReasonsIncludeAllExternalExecutionFrontiers()
    {
        var reasons = Render().CanonicalSurface.ApprovalPreviewLoop.PolicyGatePreview.BlockedReasons;

        CollectionAssert.Contains(reasons.ToArray(), "preview-only: no product command execution");
        CollectionAssert.Contains(reasons.ToArray(), "read-only: no append/write/export");
        CollectionAssert.Contains(reasons.ToArray(), "local-only: no external network/provider/cloud");
        CollectionAssert.Contains(reasons.ToArray(), "no DB/migration");
        CollectionAssert.Contains(reasons.ToArray(), "no KMS/WORM/external trust");
        CollectionAssert.Contains(reasons.ToArray(), "no Browser/CDP/WCU/OCR/Recipes live");
        CollectionAssert.Contains(reasons.ToArray(), "no destructive action");
        CollectionAssert.Contains(reasons.ToArray(), "no release/commercial");
    }

    [TestMethod]
    public void LocalApprovalPreviewLoop_EvidenceRefsLinkToExistingProductLedgerReadiness()
    {
        var refs = Render().CanonicalSurface.ApprovalPreviewLoop.EvidenceRefs;
        var ids = refs.Select(evidence => evidence.EvidenceId).ToArray();

        CollectionAssert.Contains(ids, "operator-surface");
        CollectionAssert.Contains(ids, "route-path");
        CollectionAssert.Contains(ids, "command-router-preview");
        CollectionAssert.Contains(ids, "active-writer");
        CollectionAssert.Contains(ids, "operator-acceptance");
        CollectionAssert.Contains(ids, "visual-qa");
        Assert.IsTrue(refs.All(evidence => !string.IsNullOrWhiteSpace(evidence.Source)));
        Assert.IsTrue(refs.All(evidence => !string.IsNullOrWhiteSpace(evidence.Status)));
    }

    [TestMethod]
    public void LocalApprovalPreviewLoop_SourceHasNoExecutionWriteExportNetworkDbOrPilotRun()
    {
        var source = string.Join(
            Environment.NewLine,
            File.ReadAllText(Path.Combine(RepoRoot(), "src", "OneBrain.Core", "Approval", "ProductLedgerLocalApprovalPreviewLoop.cs")),
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
            "ProductLedgerInternalCommandHandler",
            "ProductLedgerLocalReportExportService().Export",
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
            $"<(?<tag>[a-z0-9]+)[^>]*data-testid=\"{Regex.Escape(testId)}\"[^>]*>(?<content>.*?)</\\k<tag>>",
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
                "ProductLedgerLocalApprovalPreviewLoop.cs")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        Assert.Fail("repo root not found");
        return string.Empty;
    }
}
