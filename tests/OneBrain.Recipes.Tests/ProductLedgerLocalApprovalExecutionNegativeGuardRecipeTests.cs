using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.Core.Approval;

namespace OneBrain.Recipes.Tests;

[TestClass]
[TestCategory("ApprovalHumanReview")]
[TestCategory("PhaseEApprovalHumanReview")]
public sealed class ProductLedgerLocalApprovalExecutionNegativeGuardRecipeTests
{
    [TestMethod]
    public void LocalApprovalExecutionPreview_RemainsPreviewOnlyWithoutHandlerWriteExportOrPilotRun()
    {
        var result = new ProductLedgerLocalDevRoutePreview().Render(
            ProductLedgerLocalDevRoutePreview.CreateDefaultLocalDevRequest());
        var loop = result.CanonicalSurface.ApprovalPreviewLoop;

        Assert.AreEqual(ProductLedgerLocalDevRoutePreviewDecision.RenderedLocalDevInternalPreview, result.Decision);
        Assert.IsTrue(loop.IsLocalOnly);
        Assert.IsTrue(loop.IsReadOnly);
        Assert.IsTrue(loop.IsPreviewOnly);
        Assert.IsFalse(loop.AllowsExecution);
        Assert.IsFalse(loop.AllowsWrite);
        Assert.IsFalse(loop.AllowsExport);
        Assert.IsFalse(loop.NoOpExecutionPreview.HandlerInvoked);
        Assert.IsFalse(loop.NoOpExecutionPreview.AppendInvoked);
        Assert.IsFalse(loop.NoOpExecutionPreview.WriteInvoked);
        Assert.IsFalse(loop.NoOpExecutionPreview.ExportInvoked);
        Assert.IsFalse(loop.NoOpExecutionPreview.PilotRunInvoked);
    }

    [TestMethod]
    public void LocalApprovalExecutionCandidate_ReadOnlyCommandsStayInMemoryAndBoundedExportIsOutOfScope()
    {
        var allowed = new[]
        {
            ProductLedgerInternalCommandKind.ViewDiagnostics,
            ProductLedgerInternalCommandKind.ViewLedgerReadiness,
            ProductLedgerInternalCommandKind.ViewRuntimeGateStatus,
            ProductLedgerInternalCommandKind.ViewCheckpointHeadStatus,
            ProductLedgerInternalCommandKind.ViewEvidenceGates,
            ProductLedgerInternalCommandKind.StaticScanPreview,
            ProductLedgerInternalCommandKind.RequestExternalAuditPreview
        };

        CollectionAssert.DoesNotContain(allowed, ProductLedgerInternalCommandKind.LocalReportPhysicalExportBoundedInternal);

        foreach (var command in allowed)
        {
            var result = new ProductLedgerInternalCommandHandler().Execute(ReadyRequest(RouterPreview(command)));

            Assert.AreEqual(ProductLedgerInternalCommandDecision.CompletedReadOnlyInMemory, result.Decision, command.ToString());
            Assert.IsTrue(result.ExecutionPreview.InMemoryOnly);
            Assert.IsFalse(result.ExecutionPreview.PhysicalExportCreated);
            Assert.IsFalse(result.ExecutionPreview.FileWritePerformed);
            Assert.IsFalse(result.PublicUiActionAvailable);
            Assert.IsFalse(result.ProductCommandHandlerAvailable);
            Assert.IsFalse(result.ReleaseCommercialReady);
        }
    }

    private static ProductLedgerInternalCommandPreviewResult RouterPreview(ProductLedgerInternalCommandKind command) =>
        new ProductLedgerInternalCommandPreviewRouter().Preview(new ProductLedgerInternalCommandPreviewRequest(
            ExplicitInternalLocalOnlyNoOpReadOnlyScope: true,
            CommandKind: command,
            RawCommandName: command.ToString(),
            SourcePreview: null,
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
            ClaimsWriterExecutionOutsideValidatedLocalOnlyPolicy: false));

    private static ProductLedgerInternalCommandRequest ReadyRequest(ProductLedgerInternalCommandPreviewResult? preview) =>
        new(
            ExplicitInternalLocalOnlyNonDestructiveScope: true,
            RouterPreview: preview,
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
            ClaimsAppendOutsideBoundedWriter: false);
}

