using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.Core.Approval;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("ApprovalHumanReview")]
[TestCategory("PhaseEApprovalHumanReview")]
public sealed class ProductLedgerLocalApprovalExecutionOperatorAcceptanceTests
{
    [TestMethod]
    public void OperatorAcceptance_LocalApprovalExecutionRouteShowsSafeEvidenceStory()
    {
        var result = new ProductLedgerLocalDevRoutePreview().Render(
            ProductLedgerLocalDevRoutePreview.CreateDefaultLocalDevRequest());
        var html = result.HtmlSnapshot;
        var candidate = result.CanonicalSurface.ApprovalExecutionCandidatePreview;

        Assert.AreEqual(ProductLedgerLocalDevRoutePreviewDecision.RenderedLocalDevInternalPreview, result.Decision);
        Assert.AreEqual(ProductLedgerLocalApprovalExecutionDecision.CompletedReadOnlyInMemory, candidate.Decision);
        Assert.AreEqual(ProductLedgerInternalCommandKind.ViewLedgerReadiness, candidate.CandidateActionKind);
        StringAssert.Contains(html, "product-ledger-approval-preview");
        StringAssert.Contains(html, "product-ledger-approval-execution-candidate-preview");
        StringAssert.Contains(html, "PRODUCT_LEDGER_LOCAL_APPROVAL_EXECUTION_CANDIDATE_READY");
        StringAssert.Contains(html, "read-only in-memory default-off");
        StringAssert.Contains(html, "no public UI");
        StringAssert.Contains(html, "no write/export");
        StringAssert.Contains(html, "no release/commercial");
    }

    [TestMethod]
    public void OperatorAcceptance_LocalApprovalExecutionRouteHasNoExecutableAffordance()
    {
        var html = new ProductLedgerLocalDevRoutePreview()
            .Render(ProductLedgerLocalDevRoutePreview.CreateDefaultLocalDevRequest())
            .HtmlSnapshot;

        StringAssert.Contains(html, "data-executable=\"false\"");
        StringAssert.Contains(html, "disabled aria-disabled=\"true\"");
        Assert.IsFalse(html.Contains("data-executable=\"true\"", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(html.Contains("<form", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(html.Contains("<script", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(html.Contains("onclick=", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(html.Contains("formaction=", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public void OperatorAcceptance_LocalApprovalExecutionBlocksUnsafeOperatorExpectations()
    {
        var now = new DateTimeOffset(2026, 7, 5, 12, 0, 0, TimeSpan.Zero);
        var export = Execute(Request(ProductLedgerInternalCommandKind.LocalReportPhysicalExportBoundedInternal, now));
        var publicUi = Execute(Request(ProductLedgerInternalCommandKind.ViewLedgerReadiness, now) with
        {
            RequestsPublicUiAction = true
        });
        var path = Execute(Request(ProductLedgerInternalCommandKind.ViewLedgerReadiness, now) with
        {
            ClaimsArbitraryPathInput = true
        });

        AssertRejected(export, ProductLedgerLocalApprovalExecutionBlocker.CommandNotAllowedForApprovalExecution);
        AssertRejected(publicUi, ProductLedgerLocalApprovalExecutionBlocker.PublicUiActionRequested);
        AssertRejected(path, ProductLedgerLocalApprovalExecutionBlocker.ArbitraryPathInputClaimed);
    }

    private static ProductLedgerLocalApprovalExecutionResult Execute(ProductLedgerLocalApprovalExecutionRequest request) =>
        new ProductLedgerLocalApprovalExecutionCandidate().Execute(request);

    private static ProductLedgerLocalApprovalExecutionRequest Request(
        ProductLedgerInternalCommandKind command,
        DateTimeOffset now) =>
        new(
            ExplicitLocalOnlyInternalApprovalExecutionScope: true,
            ApprovalId: "operator-acceptance-local-approval",
            ApprovedAtUtc: now.AddMinutes(-1),
            NowUtc: now,
            MaxApprovalAge: TimeSpan.FromMinutes(5),
            CandidateActionKind: command,
            ApprovedActionName: command.ToString(),
            ApprovedEvidenceHash: "operator-acceptance-evidence",
            CurrentEvidenceHash: "operator-acceptance-evidence",
            PolicyRecheckPassed: true,
            ReadModelVerified: true,
            RequestsPublicUiAction: false,
            RequestsDestructiveAction: false,
            RequestsProductCommandHandler: false,
            RequestsProductiveServiceRegistration: false,
            ClaimsProviderCloudNetwork: false,
            ClaimsDbMigration: false,
            ClaimsKmsWormExternalTrust: false,
            ClaimsBrowserCdpWcuOcrRecipesLive: false,
            ClaimsReleaseCommercial: false,
            ClaimsExternalTelemetryOrSync: false,
            ClaimsBillingLicensingCloud: false,
            RequestsPhysicalExport: false,
            RequestsFileWrite: false,
            ClaimsAppendOutsideBoundedWriter: false,
            ClaimsArbitraryPathInput: false,
            ClaimsRawPayloadOrSecret: false);

    private static void AssertRejected(
        ProductLedgerLocalApprovalExecutionResult result,
        ProductLedgerLocalApprovalExecutionBlocker blocker)
    {
        Assert.AreEqual(ProductLedgerLocalApprovalExecutionDecision.Rejected, result.Decision, blocker.ToString());
        CollectionAssert.Contains(result.Blockers.ToArray(), blocker, blocker.ToString());
        Assert.IsFalse(result.PhysicalExportCreated);
        Assert.IsFalse(result.FileWritePerformed);
        Assert.IsFalse(result.PublicUiActionAvailable);
        Assert.IsFalse(result.ProductCommandHandlerAvailable);
        Assert.IsFalse(result.ReleaseCommercialReady);
    }
}

