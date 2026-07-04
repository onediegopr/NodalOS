using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.Core.Approval;

namespace OneBrain.Recipes.Tests;

[TestClass]
[TestCategory("ApprovalHumanReview")]
[TestCategory("PhaseEApprovalHumanReview")]
public sealed class ProductLedgerInternalCommandHandlerTests
{
    [TestMethod]
    public void InternalCommandHandler_CompletesDiagnosticsAndReportPreviewInMemoryRecipe()
    {
        var handler = new ProductLedgerInternalCommandHandler();
        var diagnostics = handler.Execute(ReadyRequest(RouterPreview(ProductLedgerInternalCommandKind.ViewDiagnostics)));
        var readiness = handler.Execute(ReadyRequest(RouterPreview(ProductLedgerInternalCommandKind.ViewLedgerReadiness)));
        var report = handler.Execute(ReadyRequest(RouterPreview(ProductLedgerInternalCommandKind.LocalReportPreviewInMemory)));

        Assert.AreEqual(ProductLedgerInternalCommandDecision.CompletedReadOnlyInMemory, diagnostics.Decision);
        Assert.AreEqual(ProductLedgerInternalCommandDecision.CompletedReadOnlyInMemory, readiness.Decision);
        Assert.AreEqual(ProductLedgerInternalCommandDecision.CompletedReadOnlyInMemory, report.Decision);
        Assert.AreEqual("DIAGNOSTICS_READ_ONLY_IN_MEMORY", diagnostics.ExecutionPreview.ResultKind);
        Assert.AreEqual("LEDGER_READINESS_READ_ONLY_IN_MEMORY", readiness.ExecutionPreview.ResultKind);
        Assert.AreEqual("LOCAL_REPORT_PREVIEW_IN_MEMORY_NO_FILE", report.ExecutionPreview.ResultKind);
        AssertNoProductExternalWrite(diagnostics);
        AssertNoProductExternalWrite(readiness);
        AssertNoProductExternalWrite(report);
    }

    [TestMethod]
    public void InternalCommandHandler_BlocksPublicDestructiveExternalAndPhysicalWriteRecipe()
    {
        var result = new ProductLedgerInternalCommandHandler().Execute(
            ReadyRequest(RouterPreview(ProductLedgerInternalCommandKind.ViewDiagnostics)) with
            {
                RequestsPublicUiAction = true,
                RequestsDestructiveAction = true,
                RequestsProductCommandHandler = true,
                ClaimsProviderCloudNetwork = true,
                ClaimsDbMigration = true,
                ClaimsKmsWormExternalTrust = true,
                ClaimsBrowserCdpWcuOcrRecipesLive = true,
                ClaimsReleaseCommercial = true,
                RequestsPhysicalExport = true,
                RequestsFileWrite = true
            });

        Assert.AreEqual(ProductLedgerInternalCommandDecision.Rejected, result.Decision);
        CollectionAssert.Contains(result.Blockers.ToArray(), ProductLedgerInternalCommandBlocker.PublicUiActionRequested);
        CollectionAssert.Contains(result.Blockers.ToArray(), ProductLedgerInternalCommandBlocker.DestructiveActionRequested);
        CollectionAssert.Contains(result.Blockers.ToArray(), ProductLedgerInternalCommandBlocker.ProductCommandHandlerRequested);
        CollectionAssert.Contains(result.Blockers.ToArray(), ProductLedgerInternalCommandBlocker.ProviderCloudNetworkClaimed);
        CollectionAssert.Contains(result.Blockers.ToArray(), ProductLedgerInternalCommandBlocker.DbMigrationClaimed);
        CollectionAssert.Contains(result.Blockers.ToArray(), ProductLedgerInternalCommandBlocker.KmsWormExternalTrustClaimed);
        CollectionAssert.Contains(result.Blockers.ToArray(), ProductLedgerInternalCommandBlocker.BrowserCdpWcuOcrRecipesLiveClaimed);
        CollectionAssert.Contains(result.Blockers.ToArray(), ProductLedgerInternalCommandBlocker.ReleaseCommercialClaimed);
        CollectionAssert.Contains(result.Blockers.ToArray(), ProductLedgerInternalCommandBlocker.PhysicalExportRequested);
        CollectionAssert.Contains(result.Blockers.ToArray(), ProductLedgerInternalCommandBlocker.FileWriteRequested);
        AssertNoProductExternalWrite(result);
    }

    private static ProductLedgerInternalCommandPreviewResult RouterPreview(ProductLedgerInternalCommandKind command) =>
        new ProductLedgerInternalCommandPreviewRouter().Preview(
            new ProductLedgerInternalCommandPreviewRequest(
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

    private static void AssertNoProductExternalWrite(ProductLedgerInternalCommandResult result)
    {
        Assert.IsTrue(result.InternalOnly);
        Assert.IsTrue(result.LocalOnly);
        Assert.IsTrue(result.NonDestructive);
        Assert.IsTrue(result.ReadOnlyOrInMemory);
        Assert.IsFalse(result.PublicCommandExposureAvailable);
        Assert.IsFalse(result.PublicUiActionAvailable);
        Assert.IsFalse(result.DestructiveActionAvailable);
        Assert.IsFalse(result.ProductCommandHandlerAvailable);
        Assert.IsFalse(result.ProductiveServiceRegistrationAvailable);
        Assert.IsFalse(result.ProviderCloudNetworkAvailable);
        Assert.IsFalse(result.DbMigrationAvailable);
        Assert.IsFalse(result.KmsWormExternalTrustAvailable);
        Assert.IsFalse(result.BrowserCdpWcuOcrRecipesLiveAvailable);
        Assert.IsFalse(result.ReleaseCommercialReady);
        Assert.IsFalse(result.PhysicalExportCreated);
        Assert.IsFalse(result.FileWritePerformed);
        Assert.IsFalse(result.ExecutionPreview.PhysicalExportCreated);
        Assert.IsFalse(result.ExecutionPreview.FileWritePerformed);
        Assert.IsFalse(result.ExecutionPreview.ExecutableCallbackInvoked);
    }
}
