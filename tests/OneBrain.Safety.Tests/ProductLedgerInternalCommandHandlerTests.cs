using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.Core.Approval;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("ApprovalHumanReview")]
[TestCategory("PhaseEApprovalHumanReview")]
public sealed class ProductLedgerInternalCommandHandlerTests
{
    [TestMethod]
    public void InternalCommandHandler_FailsClosedOnUnknownCorruptOrRouterBlockedCommand()
    {
        var handler = new ProductLedgerInternalCommandHandler();
        var router = new ProductLedgerInternalCommandPreviewRouter();
        var missing = handler.Execute(null);
        var missingPreview = handler.Execute(ReadyRequest(null));
        var unknown = handler.Execute(ReadyRequest(router.Preview(RouterRequest((ProductLedgerInternalCommandKind)999) with { RawCommandName = "UnknownCommand" })));
        var corrupt = handler.Execute(ReadyRequest(router.Preview(RouterRequest(ProductLedgerInternalCommandKind.ViewDiagnostics) with { RawCommandName = " " })));
        var blocked = handler.Execute(ReadyRequest(router.Preview(RouterRequest(ProductLedgerInternalCommandKind.EnablePublicUi))));

        AssertRejected(missing, ProductLedgerInternalCommandBlocker.MissingRequest);
        AssertRejected(missingPreview, ProductLedgerInternalCommandBlocker.MissingRouterPreview);
        AssertRejected(unknown, ProductLedgerInternalCommandBlocker.RouterPreviewRejected);
        AssertRejected(corrupt, ProductLedgerInternalCommandBlocker.RouterPreviewRejected);
        AssertRejected(blocked, ProductLedgerInternalCommandBlocker.RouterPreviewRejected);
    }

    [TestMethod]
    public void InternalCommandHandler_RejectsExecutableHandlerCallbackAndProductiveCommandPreviews()
    {
        var preview = RouterPreview(ProductLedgerInternalCommandKind.ViewDiagnostics);
        var executable = handlerPreview(preview with { Preview = preview.Preview with { Executable = true, Disabled = false } });
        var handlerCallback = handlerPreview(preview with { Preview = preview.Preview with { HandlerId = "product-handler", CallbackName = "callback" } });
        var productiveCommand = handlerPreview(preview with { Preview = preview.Preview with { ProductiveCommandId = "product.execute" } });
        var invalidId = handlerPreview(preview with { Preview = preview.Preview with { CommandId = "product-ledger.execute" } });

        AssertRejected(executable, ProductLedgerInternalCommandBlocker.ExecutablePreviewRejected);
        AssertRejected(handlerCallback, ProductLedgerInternalCommandBlocker.HandlerOrCallbackRejected);
        AssertRejected(productiveCommand, ProductLedgerInternalCommandBlocker.ProductiveCommandIdRejected);
        AssertRejected(invalidId, ProductLedgerInternalCommandBlocker.InvalidPreviewCommandId);

        static ProductLedgerInternalCommandResult handlerPreview(ProductLedgerInternalCommandPreviewResult routerPreview) =>
            new ProductLedgerInternalCommandHandler().Execute(ReadyRequest(routerPreview));
    }

    [TestMethod]
    public void InternalCommandHandler_BlocksForbiddenCommandsAndProductiveClaims()
    {
        var cases = new Dictionary<ProductLedgerInternalCommandKind, ProductLedgerInternalCommandBlocker>
        {
            [ProductLedgerInternalCommandKind.EnablePublicUi] = ProductLedgerInternalCommandBlocker.RouterPreviewRejected,
            [ProductLedgerInternalCommandKind.ExecuteAction] = ProductLedgerInternalCommandBlocker.RouterPreviewRejected,
            [ProductLedgerInternalCommandKind.DestructiveWrite] = ProductLedgerInternalCommandBlocker.RouterPreviewRejected,
            [ProductLedgerInternalCommandKind.RegisterCommandHandler] = ProductLedgerInternalCommandBlocker.RouterPreviewRejected,
            [ProductLedgerInternalCommandKind.RegisterProductDI] = ProductLedgerInternalCommandBlocker.RouterPreviewRejected,
            [ProductLedgerInternalCommandKind.ConnectProvider] = ProductLedgerInternalCommandBlocker.RouterPreviewRejected,
            [ProductLedgerInternalCommandKind.EnableCloud] = ProductLedgerInternalCommandBlocker.RouterPreviewRejected,
            [ProductLedgerInternalCommandKind.RunMigration] = ProductLedgerInternalCommandBlocker.RouterPreviewRejected,
            [ProductLedgerInternalCommandKind.EnableKms] = ProductLedgerInternalCommandBlocker.RouterPreviewRejected,
            [ProductLedgerInternalCommandKind.EnableWorm] = ProductLedgerInternalCommandBlocker.RouterPreviewRejected,
            [ProductLedgerInternalCommandKind.EnableExternalTrust] = ProductLedgerInternalCommandBlocker.RouterPreviewRejected,
            [ProductLedgerInternalCommandKind.RunBrowserCdp] = ProductLedgerInternalCommandBlocker.RouterPreviewRejected,
            [ProductLedgerInternalCommandKind.RunWcu] = ProductLedgerInternalCommandBlocker.RouterPreviewRejected,
            [ProductLedgerInternalCommandKind.RunOcr] = ProductLedgerInternalCommandBlocker.RouterPreviewRejected,
            [ProductLedgerInternalCommandKind.RunRecipesLive] = ProductLedgerInternalCommandBlocker.RouterPreviewRejected,
            [ProductLedgerInternalCommandKind.Release] = ProductLedgerInternalCommandBlocker.RouterPreviewRejected,
            [ProductLedgerInternalCommandKind.CommercialLaunch] = ProductLedgerInternalCommandBlocker.RouterPreviewRejected,
            [ProductLedgerInternalCommandKind.SyncExternal] = ProductLedgerInternalCommandBlocker.RouterPreviewRejected,
            [ProductLedgerInternalCommandKind.TelemetryExternal] = ProductLedgerInternalCommandBlocker.RouterPreviewRejected,
            [ProductLedgerInternalCommandKind.BillingLicensingCloud] = ProductLedgerInternalCommandBlocker.RouterPreviewRejected
        };

        foreach (var testCase in cases)
        {
            AssertRejected(new ProductLedgerInternalCommandHandler().Execute(ReadyRequest(RouterPreview(testCase.Key))), testCase.Value);
        }

        var ready = ReadyRequest(RouterPreview(ProductLedgerInternalCommandKind.ViewDiagnostics));
        var claimCases = new Dictionary<ProductLedgerInternalCommandRequest, ProductLedgerInternalCommandBlocker>
        {
            [ready with { ExplicitInternalLocalOnlyNonDestructiveScope = false }] = ProductLedgerInternalCommandBlocker.MissingExplicitInternalLocalOnlyNonDestructiveScope,
            [ready with { RequestsPublicUiAction = true }] = ProductLedgerInternalCommandBlocker.PublicUiActionRequested,
            [ready with { RequestsDestructiveAction = true }] = ProductLedgerInternalCommandBlocker.DestructiveActionRequested,
            [ready with { RequestsProductCommandHandler = true }] = ProductLedgerInternalCommandBlocker.ProductCommandHandlerRequested,
            [ready with { RequestsProductiveServiceRegistration = true }] = ProductLedgerInternalCommandBlocker.ProductiveServiceRegistrationRequested,
            [ready with { ClaimsProviderCloudNetwork = true }] = ProductLedgerInternalCommandBlocker.ProviderCloudNetworkClaimed,
            [ready with { ClaimsDbMigration = true }] = ProductLedgerInternalCommandBlocker.DbMigrationClaimed,
            [ready with { ClaimsKmsWormExternalTrust = true }] = ProductLedgerInternalCommandBlocker.KmsWormExternalTrustClaimed,
            [ready with { ClaimsBrowserCdpWcuOcrRecipesLive = true }] = ProductLedgerInternalCommandBlocker.BrowserCdpWcuOcrRecipesLiveClaimed,
            [ready with { ClaimsReleaseCommercial = true }] = ProductLedgerInternalCommandBlocker.ReleaseCommercialClaimed,
            [ready with { ClaimsExternalTelemetryOrSync = true }] = ProductLedgerInternalCommandBlocker.ExternalTelemetryOrSyncClaimed,
            [ready with { ClaimsBillingLicensingCloud = true }] = ProductLedgerInternalCommandBlocker.BillingLicensingCloudClaimed,
            [ready with { RequestsPhysicalExport = true }] = ProductLedgerInternalCommandBlocker.PhysicalExportRequested,
            [ready with { RequestsFileWrite = true }] = ProductLedgerInternalCommandBlocker.FileWriteRequested,
            [ready with { ClaimsAppendOutsideBoundedWriter = true }] = ProductLedgerInternalCommandBlocker.AppendOutsideBoundedWriterClaimed
        };

        foreach (var testCase in claimCases)
        {
            AssertRejected(new ProductLedgerInternalCommandHandler().Execute(testCase.Key), testCase.Value);
        }
    }

    [TestMethod]
    public void InternalCommandHandler_AllowsOnlyReadOnlyInMemoryCommands()
    {
        var allowed = new[]
        {
            ProductLedgerInternalCommandKind.ViewDiagnostics,
            ProductLedgerInternalCommandKind.ViewLedgerReadiness,
            ProductLedgerInternalCommandKind.ViewRuntimeGateStatus,
            ProductLedgerInternalCommandKind.ViewCheckpointHeadStatus,
            ProductLedgerInternalCommandKind.ViewEvidenceGates,
            ProductLedgerInternalCommandKind.StaticScanPreview,
            ProductLedgerInternalCommandKind.RequestExternalAuditPreview,
            ProductLedgerInternalCommandKind.LocalReportPreviewInMemory
        };

        foreach (var command in allowed)
        {
            var result = new ProductLedgerInternalCommandHandler().Execute(ReadyRequest(RouterPreview(command)));

            Assert.AreEqual(ProductLedgerInternalCommandDecision.CompletedReadOnlyInMemory, result.Decision, command.ToString());
            Assert.AreEqual(command, result.ExecutionPreview.CommandKind);
            Assert.IsTrue(result.ExecutionPreview.InMemoryOnly);
            Assert.IsFalse(result.ExecutionPreview.PhysicalExportCreated);
            Assert.IsFalse(result.ExecutionPreview.FileWritePerformed);
            Assert.IsFalse(result.ExecutionPreview.ExecutableCallbackInvoked);
            AssertNoProductExternalWrite(result);
            StringAssert.Contains(result.StatusText, "READ_ONLY_OR_IN_MEMORY");
        }
    }

    [TestMethod]
    public void InternalCommandHandler_LocalReportPreviewIsInMemoryOnlyWithoutFileCreation()
    {
        var result = new ProductLedgerInternalCommandHandler().Execute(
            ReadyRequest(RouterPreview(ProductLedgerInternalCommandKind.LocalReportPreviewInMemory)));

        Assert.AreEqual(ProductLedgerInternalCommandDecision.CompletedReadOnlyInMemory, result.Decision);
        Assert.AreEqual("LOCAL_REPORT_PREVIEW_IN_MEMORY_NO_FILE", result.ExecutionPreview.ResultKind);
        Assert.IsTrue(result.ExecutionPreview.Lines.Any(line => line.Contains("No physical export", StringComparison.Ordinal)));
        Assert.IsFalse(result.PhysicalExportCreated);
        Assert.IsFalse(result.FileWritePerformed);
        AssertNoProductExternalWrite(result);
    }

    [TestMethod]
    public void InternalCommandHandler_SourceHasNoPublicCommandExposurePhysicalWriteNetworkDbKmsLiveOrRelease()
    {
        var source = File.ReadAllText(Path.Combine(
            RepoRoot(),
            "src",
            "OneBrain.Core",
            "Approval",
            "ProductLedgerInternalCommandHandler.cs"));
        var forbiddenFragments = new[]
        {
            "IService" + "Collection",
            "Add" + "Singleton",
            "Add" + "Scoped",
            "Add" + "Transient",
            "IHosted" + "Service",
            "ICommand" + "Handler",
            "Handle" + "Async(",
            "Control" + "ler",
            "Map" + "Post",
            "Map" + "Get",
            "Http" + "Client",
            "Web" + "Socket",
            "Db" + "Context",
            "Migration" + "Builder",
            "Kms" + "Client",
            "Worm" + "Store",
            "File.Write" + "AllText",
            "File.Append" + "AllText",
            "Directory.Create" + "Directory",
            "PublicCommandExposureAvailable:" + " true",
            "PublicUiActionAvailable:" + " true",
            "DestructiveActionAvailable:" + " true",
            "ProductCommandHandlerAvailable:" + " true",
            "ProductiveServiceRegistrationAvailable:" + " true",
            "ProviderCloudNetworkAvailable:" + " true",
            "DbMigrationAvailable:" + " true",
            "KmsWormExternalTrustAvailable:" + " true",
            "BrowserCdpWcuOcrRecipesLiveAvailable:" + " true",
            "ExternalTelemetryOrSyncAvailable:" + " true",
            "BillingLicensingCloudAvailable:" + " true",
            "ReleaseCommercialReady:" + " true",
            "PhysicalExportCreated:" + " true",
            "FileWritePerformed:" + " true",
            "product" + "-ready",
            "public" + "-ready"
        };

        foreach (var fragment in forbiddenFragments)
        {
            Assert.IsFalse(source.Contains(fragment, StringComparison.OrdinalIgnoreCase), fragment);
        }

        StringAssert.Contains(source, "INTERNAL_LOCAL_ONLY");
        StringAssert.Contains(source, "NON_DESTRUCTIVE");
    }

    private static ProductLedgerInternalCommandPreviewResult RouterPreview(ProductLedgerInternalCommandKind command) =>
        new ProductLedgerInternalCommandPreviewRouter().Preview(RouterRequest(command));

    private static ProductLedgerInternalCommandPreviewRequest RouterRequest(ProductLedgerInternalCommandKind command) =>
        new(
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
            ClaimsWriterExecutionOutsideValidatedLocalOnlyPolicy: false);

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

    private static void AssertRejected(ProductLedgerInternalCommandResult result, ProductLedgerInternalCommandBlocker blocker)
    {
        Assert.AreEqual(ProductLedgerInternalCommandDecision.Rejected, result.Decision, blocker.ToString());
        CollectionAssert.Contains(result.Blockers.ToArray(), blocker, blocker.ToString());
        Assert.IsTrue(result.ExecutionPreview.InMemoryOnly);
        Assert.IsFalse(result.ExecutionPreview.PhysicalExportCreated);
        Assert.IsFalse(result.ExecutionPreview.FileWritePerformed);
        Assert.IsFalse(result.ExecutionPreview.ExecutableCallbackInvoked);
        AssertNoProductExternalWrite(result);
    }

    private static void AssertNoProductExternalWrite(ProductLedgerInternalCommandResult result)
    {
        Assert.IsTrue(result.InternalOnly);
        Assert.IsTrue(result.LocalOnly);
        Assert.IsTrue(result.NonDestructive);
        Assert.IsTrue(result.ReadOnlyOrInMemory);
        Assert.IsTrue(result.FailClosed);
        Assert.IsFalse(result.PublicCommandExposureAvailable);
        Assert.IsFalse(result.PublicUiActionAvailable);
        Assert.IsFalse(result.DestructiveActionAvailable);
        Assert.IsFalse(result.ProductCommandHandlerAvailable);
        Assert.IsFalse(result.ProductiveServiceRegistrationAvailable);
        Assert.IsFalse(result.ProviderCloudNetworkAvailable);
        Assert.IsFalse(result.DbMigrationAvailable);
        Assert.IsFalse(result.KmsWormExternalTrustAvailable);
        Assert.IsFalse(result.BrowserCdpWcuOcrRecipesLiveAvailable);
        Assert.IsFalse(result.ExternalTelemetryOrSyncAvailable);
        Assert.IsFalse(result.BillingLicensingCloudAvailable);
        Assert.IsFalse(result.ReleaseCommercialReady);
        Assert.IsFalse(result.PhysicalExportCreated);
        Assert.IsFalse(result.FileWritePerformed);
    }

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
                "ProductLedgerInternalCommandHandler.cs")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        Assert.Fail("repo root not found");
        return string.Empty;
    }
}
