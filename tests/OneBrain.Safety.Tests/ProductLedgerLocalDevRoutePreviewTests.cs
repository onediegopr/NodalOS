using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.Core.Approval;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("ApprovalHumanReview")]
[TestCategory("PhaseEApprovalHumanReview")]
public sealed class ProductLedgerLocalDevRoutePreviewTests
{
    [TestMethod]
    public void LocalDevRoutePreview_FailsClosedOutsideLocalDevAndOnUnsafeClaims()
    {
        var preview = new ProductLedgerLocalDevRoutePreview();
        var missing = preview.Render(null);
        var ready = ReadyRequest();
        var cases = new Dictionary<ProductLedgerLocalDevRoutePreviewRequest, ProductLedgerLocalDevRoutePreviewBlocker>
        {
            [ready with { ExplicitLocalDevInternalPreviewScope = false }] = ProductLedgerLocalDevRoutePreviewBlocker.MissingExplicitLocalDevInternalPreviewScope,
            [ready with { LocalMode = false }] = ProductLedgerLocalDevRoutePreviewBlocker.NonLocalMode,
            [ready with { DevMode = false }] = ProductLedgerLocalDevRoutePreviewBlocker.MissingDevMode,
            [ready with { ClaimsProductionMode = true }] = ProductLedgerLocalDevRoutePreviewBlocker.ProductionModeClaimed,
            [ready with { ClaimsReleaseMode = true }] = ProductLedgerLocalDevRoutePreviewBlocker.ReleaseModeClaimed,
            [ready with { ClaimsCommercialMode = true }] = ProductLedgerLocalDevRoutePreviewBlocker.CommercialModeClaimed,
            [ready with { ClaimsExternalHostOrPublicOrigin = true }] = ProductLedgerLocalDevRoutePreviewBlocker.ExternalHostOrPublicOriginClaimed,
            [ready with { ClaimsPublicDeploy = true }] = ProductLedgerLocalDevRoutePreviewBlocker.PublicDeployClaimed,
            [ready with { ClaimsTelemetryOrSync = true }] = ProductLedgerLocalDevRoutePreviewBlocker.TelemetryOrSyncClaimed,
            [ready with { ClaimsProviderCloudNetwork = true }] = ProductLedgerLocalDevRoutePreviewBlocker.ProviderCloudNetworkClaimed,
            [ready with { ClaimsDbMigration = true }] = ProductLedgerLocalDevRoutePreviewBlocker.DbMigrationClaimed,
            [ready with { ClaimsKmsWormExternalTrust = true }] = ProductLedgerLocalDevRoutePreviewBlocker.KmsWormExternalTrustClaimed,
            [ready with { ClaimsBrowserCdpWcuOcrRecipesLive = true }] = ProductLedgerLocalDevRoutePreviewBlocker.BrowserCdpWcuOcrRecipesLiveClaimed,
            [ready with { RequestsDestructiveAction = true }] = ProductLedgerLocalDevRoutePreviewBlocker.DestructiveActionRequested,
            [ready with { ClaimsUnboundedPhysicalExport = true }] = ProductLedgerLocalDevRoutePreviewBlocker.UnboundedPhysicalExportClaimed,
            [ready with { ClaimsExternalCloudExport = true }] = ProductLedgerLocalDevRoutePreviewBlocker.ExternalCloudExportClaimed,
            [ready with { ClaimsRawPayloadOrSecret = true }] = ProductLedgerLocalDevRoutePreviewBlocker.RawPayloadOrSecretClaimed,
            [ready with { RenderableSnapshotRequest = null }] = ProductLedgerLocalDevRoutePreviewBlocker.MissingRenderableSnapshotRequest,
            [ready with { RenderableSnapshotRequest = ReadyRenderableRequest() with { ClaimsEndpointRouteController = true } }] = ProductLedgerLocalDevRoutePreviewBlocker.RenderableSnapshotRejected
        };

        AssertRejected(missing, ProductLedgerLocalDevRoutePreviewBlocker.MissingRequest);
        foreach (var testCase in cases)
        {
            AssertRejected(preview.Render(testCase.Key), testCase.Value);
        }
    }

    [TestMethod]
    public void LocalDevRoutePreview_RendersSnapshotOnlyHtmlWithLocalDevNotices()
    {
        var result = new ProductLedgerLocalDevRoutePreview().Render(ReadyRequest());
        var defaultResult = new ProductLedgerLocalDevRoutePreview().Render(
            ProductLedgerLocalDevRoutePreview.CreateDefaultLocalDevRequest());

        Assert.AreEqual(ProductLedgerLocalDevRoutePreviewDecision.RenderedLocalDevInternalPreview, result.Decision);
        Assert.AreEqual(ProductLedgerLocalDevRoutePreviewDecision.RenderedLocalDevInternalPreview, defaultResult.Decision);
        Assert.AreEqual("text/html; charset=utf-8", result.ContentType);
        Assert.AreEqual(ProductLedgerLocalDevRoutePreview.RouteTemplatePreview, result.RouteTemplatePreview);
        AssertCanonicalSurface(result);
        AssertNoForbiddenSurface(result);

        var html = result.HtmlSnapshot;
        StringAssert.Contains(html, "data-testid=\"local-dev-route-preview\"");
        StringAssert.Contains(html, "data-testid=\"canonical-surface-model\"");
        StringAssert.Contains(html, "data-route-path=\"/internal/product-ledger/operator-surface\"");
        StringAssert.Contains(html, "data-read-model-mode=\"FixtureSafeReadModel\"");
        StringAssert.Contains(html, "local-dev/internal-only");
        StringAssert.Contains(html, "not publicly deployed");
        StringAssert.Contains(html, "no telemetry");
        StringAssert.Contains(html, "no external network");
        StringAssert.Contains(html, "no release/commercial");
        StringAssert.Contains(html, "no external trust");
        StringAssert.Contains(html, "no WORM/KMS/cloud");
        StringAssert.Contains(html, "not compliance-grade custody");
        StringAssert.Contains(html, "data-testid=\"runtime-gate\"");
        StringAssert.Contains(html, "data-testid=\"writer\"");
        StringAssert.Contains(html, "data-testid=\"bounded-export\"");
        StringAssert.Contains(html, "data-testid=\"surface-ledger-authority\"");
        StringAssert.Contains(html, nameof(ProductLedgerPathLocalOnlyActiveWriter));
        StringAssert.Contains(html, "data-testid=\"surface-ledger-verification\"");
        StringAssert.Contains(html, "data-testid=\"surface-checkpoint\"");
        StringAssert.Contains(html, "data-testid=\"surface-redaction-retention\"");
        StringAssert.Contains(html, "data-testid=\"surface-concurrency\"");
        StringAssert.Contains(html, "data-testid=\"surface-bounded-export\"");
        StringAssert.Contains(html, "data-testid=\"surface-operator-acceptance\"");
        StringAssert.Contains(html, "data-testid=\"surface-public-action-contract\"");
        StringAssert.Contains(html, "data-testid=\"surface-visual-evidence\"");
        StringAssert.Contains(html, "data-testid=\"surface-screenshot-evidence\"");
        StringAssert.Contains(html, "data-testid=\"surface-blocked-frontiers\"");
        StringAssert.Contains(html, "data-testid=\"surface-blocked-product-command-execution\"");
        StringAssert.Contains(html, "data-testid=\"surface-blocked-public-internet\"");
        StringAssert.Contains(html, "data-testid=\"surface-blocked-release-commercial\"");
        StringAssert.Contains(html, "data-testid=\"disabled-dangerous-actions\"");
        StringAssert.Contains(html, "data-testid=\"action-destructive-write\"");
        StringAssert.Contains(html, "disabled aria-disabled=\"true\"");
        StringAssert.Contains(html, "data-executable=\"false\"");
        StringAssert.Contains(html, "data-handler-id=\"\"");
        StringAssert.Contains(html, "data-callback=\"\"");
    }

    [TestMethod]
    public void LocalDevRoutePreview_PilotRouteMappingIsDevelopmentOnlyAndReadOnly()
    {
        var program = File.ReadAllText(Path.Combine(RepoRoot(), "src", "OneBrain.Pilot", "Program.cs"));
        var mapper = File.ReadAllText(Path.Combine(RepoRoot(), "src", "OneBrain.Pilot", "ProductLedgerLocalDevRouteEndpointMapper.cs"));
        var guardIndex = mapper.IndexOf("environment.IsDevelopment()", StringComparison.Ordinal);
        var routeIndex = mapper.IndexOf("ProductLedgerLocalDevRoutePreview.RouteTemplatePreview", StringComparison.Ordinal);

        Assert.IsTrue(guardIndex >= 0, "Development guard missing.");
        Assert.IsTrue(routeIndex > guardIndex, "Route preview must be registered only after the development guard.");
        StringAssert.Contains(program, "app.MapProductLedgerLocalDevRoutePreview(app.Environment)");
        Assert.IsFalse(mapper.Contains("Map" + "Post(ProductLedgerLocalDevRoutePreview.RouteTemplatePreview", StringComparison.Ordinal));
        Assert.IsFalse(mapper.Contains("Results.Json(new ProductLedgerLocalDevRoutePreview", StringComparison.Ordinal));
        StringAssert.Contains(mapper, "Results.Content(result.HtmlSnapshot, result.ContentType)");
        StringAssert.Contains(mapper, "Results.NotFound()");
    }

    [TestMethod]
    public void LocalDevRoutePreview_OutputHasNoScriptsExecutableHandlersPublicDeployOrExternalSurfaces()
    {
        var result = new ProductLedgerLocalDevRoutePreview().Render(ReadyRequest());
        var html = result.HtmlSnapshot;

        Assert.AreEqual(ProductLedgerLocalDevRoutePreviewDecision.RenderedLocalDevInternalPreview, result.Decision);
        Assert.IsFalse(html.Contains("<script", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(html.Contains("src=\"http", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(html.Contains("href=\"http", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(html.Contains("onclick=", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(html.Contains("formaction=", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(html.Contains("data-executable=\"true\"", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(html.Contains("handler-id=\"product", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(html.Contains("callback=\"product", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(html.Contains("data-public-deploy=\"true\"", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(html.Contains("data-external-network=\"true\"", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(html.Contains("data-telemetry=\"true\"", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(html.Contains("provider/cloud/network enabled", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(html.Contains("DB migration enabled", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(html.Contains("release ready", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(html.Contains("commercial ready", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public void LocalDevRoutePreview_SourceHasNoActualEndpointRegistrationNetworkDbKmsLiveOrRelease()
    {
        var source = File.ReadAllText(Path.Combine(
            RepoRoot(),
            "src",
            "OneBrain.Core",
            "Approval",
            "ProductLedgerLocalDevRoutePreview.cs"));
        var forbiddenFragments = new[]
        {
            "IEndpoint" + "RouteBuilder",
            "Route" + "HandlerBuilder",
            "IService" + "Collection",
            "Add" + "Singleton",
            "Add" + "Scoped",
            "Add" + "Transient",
            "IHosted" + "Service",
            "Map" + "Get",
            "Map" + "Post",
            "Controller" + "Base",
            "MapController" + "Route",
            "[" + "Route",
            "Http" + "Client",
            "Web" + "Socket",
            "Db" + "Context",
            "Migration" + "Builder",
            "Kms" + "Client",
            "Worm" + "Store",
            "File.Write" + "AllText",
            "File.Append" + "AllText",
            "Directory.Create" + "Directory",
            "PublicDeployAvailable:" + " true",
            "ExternalNetworkAvailable:" + " true",
            "TelemetryOrSyncAvailable:" + " true",
            "ProviderCloudNetworkAvailable:" + " true",
            "DbMigrationAvailable:" + " true",
            "KmsWormExternalTrustAvailable:" + " true",
            "BrowserCdpWcuOcrRecipesLiveAvailable:" + " true",
            "DestructiveActionAvailable:" + " true",
            "UnboundedPhysicalExportAvailable:" + " true",
            "ExternalCloudExportAvailable:" + " true",
            "ReleaseCommercialReady:" + " true"
        };

        foreach (var fragment in forbiddenFragments)
        {
            Assert.IsFalse(source.Contains(fragment, StringComparison.OrdinalIgnoreCase), fragment);
        }

        StringAssert.Contains(source, "LOCAL_DEV_INTERNAL_ONLY");
        StringAssert.Contains(source, "NOT_PUBLICLY_DEPLOYED");
        StringAssert.Contains(source, "NO_EXTERNAL_NETWORK");
        StringAssert.Contains(source, "NO_TELEMETRY");
    }

    [TestMethod]
    public void LocalDevRoutePreview_CanonicalSurfaceIsSingleRouteReadOnlyModel()
    {
        var result = new ProductLedgerLocalDevRoutePreview().Render(ReadyRequest());
        var model = result.CanonicalSurface;

        Assert.AreEqual(ProductLedgerLocalDevRoutePreviewDecision.RenderedLocalDevInternalPreview, result.Decision);
        Assert.AreEqual(ProductLedgerOperatorSurfaceModelFactory.CanonicalSurfaceId, model.SurfaceId);
        Assert.AreEqual(ProductLedgerLocalDevRoutePreview.RouteTemplatePreview, model.RoutePath);
        Assert.AreEqual(ProductLedgerOperatorSurfaceModelFactory.Scope, model.Scope);
        Assert.AreEqual(ProductLedgerOperatorSurfaceReadModelMode.FixtureSafeReadModel, model.ReadModelMode);
        Assert.AreEqual(nameof(ProductLedgerPathLocalOnlyActiveWriter), model.LedgerAuthority);
        Assert.AreEqual("ACTIVE_PRODUCT_LEDGER_PATH_LOCAL_ONLY", model.LedgerAuthorityBoundaryStatus);
        Assert.IsTrue(model.UsesFixtureReadModel);
        Assert.IsTrue(model.IsLocalOnly);
        Assert.IsTrue(model.IsDevelopmentOnly);
        Assert.IsTrue(model.IsReadOnly);
        Assert.IsFalse(model.AllowsProductCommandExecution);
        Assert.IsFalse(model.AllowsPublicInternetExposure);
        Assert.IsFalse(model.AllowsExternalNetwork);
        Assert.IsFalse(model.AllowsDbMigration);
        Assert.IsFalse(model.AllowsKmsWormExternalTrust);
        Assert.IsFalse(model.AllowsReleaseCommercial);
        Assert.IsFalse(model.AllowsBrowserCdpWcuOcrRecipesLive);
        Assert.IsFalse(model.AllowsDestructiveAction);
        Assert.IsFalse(model.AllowsUnboundedExport);
        Assert.IsFalse(model.AllowsExternalCloudExport);
        Assert.IsTrue(model.Statuses.Any(status => status.StatusId == "ledger-verification"));
        Assert.IsTrue(model.Statuses.Any(status => status.StatusId == "checkpoint"));
        Assert.IsTrue(model.Statuses.Any(status => status.StatusId == "redaction-retention"));
        Assert.IsTrue(model.Statuses.Any(status => status.StatusId == "concurrency"));
        Assert.IsTrue(model.Statuses.Any(status => status.StatusId == "bounded-export"));
        Assert.IsTrue(model.Statuses.Any(status => status.StatusId == "operator-acceptance"));
        Assert.IsTrue(model.Statuses.Any(status => status.StatusId == "public-action-contract"));
        Assert.IsTrue(model.BlockedFrontiers.Any(frontier => frontier.FrontierId == "product-command-execution"));
        Assert.IsTrue(model.BlockedFrontiers.Any(frontier => frontier.FrontierId == "public-internet"));
        Assert.IsTrue(model.BlockedFrontiers.Any(frontier => frontier.FrontierId == "release-commercial"));
        Assert.IsTrue(model.ActionPreviews.Count > 0);
        Assert.IsTrue(model.ActionPreviews.All(action => action.Disabled && action.ReadOnly));
    }

    private static ProductLedgerLocalDevRoutePreviewRequest ReadyRequest() =>
        new(
            ExplicitLocalDevInternalPreviewScope: true,
            LocalMode: true,
            DevMode: true,
            ClaimsProductionMode: false,
            ClaimsReleaseMode: false,
            ClaimsCommercialMode: false,
            ClaimsExternalHostOrPublicOrigin: false,
            ClaimsPublicDeploy: false,
            ClaimsTelemetryOrSync: false,
            ClaimsProviderCloudNetwork: false,
            ClaimsDbMigration: false,
            ClaimsKmsWormExternalTrust: false,
            ClaimsBrowserCdpWcuOcrRecipesLive: false,
            RequestsDestructiveAction: false,
            ClaimsUnboundedPhysicalExport: false,
            ClaimsExternalCloudExport: false,
            ClaimsRawPayloadOrSecret: false,
            RenderableSnapshotRequest: ReadyRenderableRequest());

    private static ProductLedgerRenderableOperatorSurfaceRequest ReadyRenderableRequest() =>
        new(
            ExplicitLocalOnlySnapshotScope: true,
            PublicActionSurface: ReadyActionSurface(),
            ClaimsEndpointRouteController: false,
            ClaimsExternalScript: false,
            ClaimsTelemetryOrSync: false,
            ClaimsProviderCloudNetwork: false,
            ClaimsDbMigration: false,
            ClaimsKmsWormExternalTrust: false,
            ClaimsBrowserCdpWcuOcrRecipesLive: false,
            ClaimsReleaseCommercial: false,
            ClaimsRawPayloadOrSecret: false);

    private static ProductLedgerPublicUiActionResult ReadyActionSurface() =>
        new ProductLedgerPublicUiActionSurface().Execute(ReadyActionRequest(ProductLedgerPublicUiActionKind.ViewDiagnostics));

    private static ProductLedgerPublicUiActionRequest ReadyActionRequest(ProductLedgerPublicUiActionKind action) =>
        new(
            ExplicitPublicLocalOnlyNonDestructiveScope: true,
            PublicReadOnlyDisabledPreview: ReadyPublicPreview(),
            ActionKind: action,
            RawActionName: action.ToString(),
            LocalReportExportRequest: null,
            ClaimsRawPayloadOrSecret: false,
            RequestsDestructiveAction: false,
            RequestsGenericExecuteAction: false,
            RequestsProductiveServiceRegistration: false,
            ClaimsProviderCloudNetwork: false,
            ClaimsDbMigration: false,
            ClaimsKmsWormExternalTrust: false,
            ClaimsBrowserCdpWcuOcrRecipesLive: false,
            ClaimsReleaseCommercial: false,
            ClaimsExternalTelemetryOrSync: false,
            ClaimsBillingLicensingCloud: false,
            ClaimsUnboundedPhysicalExport: false,
            ClaimsExternalCloudExport: false,
            RequestsDeleteOrUnsafeOverwrite: false);

    private static ProductLedgerPublicUiReadOnlyDisabledPreviewResult ReadyPublicPreview() =>
        new ProductLedgerPublicUiReadOnlyDisabledPreviewPresenter().Render(
            new ProductLedgerPublicUiReadOnlyDisabledPreviewRequest(
                ExplicitReadOnlyDisabledMockScope: true,
                InternalPreview: ReadyInternalPreview(),
                HasPublicSurfaceReadinessPacket: true,
                ReadinessPacketFreshAndConsistent: true,
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
                ClaimsExternalCloudExport: false,
                ClaimsUnboundedPhysicalExport: false,
                ClaimsRawPayloadOrSecret: false));

    private static ProductLedgerInternalOperatorUiPreviewResult ReadyInternalPreview() =>
        new ProductLedgerInternalOperatorUiPresenter().Render(
            new ProductLedgerInternalOperatorUiPreviewRequest(
                ExplicitInternalLocalOnlyReadOnlyPreviewScope: true,
                Diagnostics: ReadyDiagnostics(),
                RequestsPublicUiAction: false,
                RequestsDestructiveUserFacingAction: false,
                RequestsProductCommandHandler: false,
                RequestsProductiveServiceRegistration: false,
                ClaimsProviderCloudNetwork: false,
                ClaimsDbMigration: false,
                ClaimsKmsWormExternalTrust: false,
                ClaimsBrowserCdpWcuOcrRecipesLive: false,
                ClaimsReleaseCommercial: false,
                ClaimsExternalTelemetryOrSync: false,
                ClaimsBillingLicensingCloud: false));

    private static ProductLedgerLocalOnlyOperatorDiagnosticsResult ReadyDiagnostics() =>
        new(
            Decision: ProductLedgerLocalOnlyOperatorDiagnosticsDecision.RenderedReadOnly,
            Blockers: [],
            Sections:
            [
                Section("Runtime Local-Only Gate", "ENABLED_LOCAL_ONLY_INTERNAL", ["feature_flag=enabled:local-only-internal"]),
                Section("Product Ledger Path Policy", "ACTIVE_LOCAL_ONLY_POLICY_BOUND", ["candidate_id=ledger-local-dev-route-001"]),
                Section("Bounded Writer Status", "WRITER_BOUNDED_LOCAL_ONLY_SURFACE_READ_ONLY", ["operator_surface_write_allowed=false"]),
                Section("Checkpoint / Head Status", "VERIFIED_HEAD_PRESENT", ["same_boundary_trust=true"]),
                Section("Evidence Gates", "EVIDENCE_REFERENCES_FRESH_AND_WELL_FORMED", ["redaction_before_persistence=True", "authority=True"]),
                Section("Disabled Actions", "ALL_ACTIONS_DISABLED", ["destructive action", "unbounded export", "external/cloud export"]),
                Section("Safe Next Step", "LOCAL_DEV_ROUTE_EXTERNAL_AUDIT_READ_ONLY", ["snapshot fixture"])
            ],
            ActionPreviews:
            [
                new("View local-only diagnostics snapshot", "read-only preview only", "operator visibility without execution authority", ["runtime gate"], Disabled: true, ProductiveCommandId: null, HandlerName: null, CallbackName: null)
            ],
            DisabledActions: ["destructive user-facing action", "provider/cloud/network", "release/commercial"],
            SafeNextStep: "LOCAL_DEV_ROUTE_INTERNAL_ENDPOINT_PREVIEW",
            ReadOnly: true,
            LocalOnly: true,
            InternalOnly: true,
            FailClosed: true,
            PublicUiActionAvailable: false,
            DestructiveUserFacingActionAvailable: false,
            ProductCommandHandlerAvailable: false,
            ProductiveServiceRegistrationAvailable: false,
            ProviderCloudNetworkAvailable: false,
            DbMigrationAvailable: false,
            KmsWormExternalTrustAvailable: false,
            BrowserCdpWcuOcrRecipesLiveAvailable: false,
            ReleaseCommercialReady: false,
            StatusText: ProductLedgerLocalOnlyOperatorDiagnosticsPresenter.ReadyStatus);

    private static ProductLedgerLocalOnlyOperatorDiagnosticsSection Section(
        string title,
        string status,
        IReadOnlyList<string> lines) =>
        new(title, status, lines, ProductLedgerLocalOnlyOperatorDiagnosticsSeverity.Info);

    private static void AssertRejected(
        ProductLedgerLocalDevRoutePreviewResult result,
        ProductLedgerLocalDevRoutePreviewBlocker blocker)
    {
        Assert.AreEqual(ProductLedgerLocalDevRoutePreviewDecision.Rejected, result.Decision, blocker.ToString());
        CollectionAssert.Contains(result.Blockers.ToArray(), blocker, blocker.ToString());
        Assert.AreEqual(string.Empty, result.HtmlSnapshot);
        AssertNoForbiddenSurface(result);
    }

    private static void AssertNoForbiddenSurface(ProductLedgerLocalDevRoutePreviewResult result)
    {
        Assert.IsTrue(result.LocalOnly);
        Assert.IsTrue(result.DevOnly);
        Assert.IsTrue(result.InternalOnly);
        Assert.IsTrue(result.ReadOnly);
        Assert.IsTrue(result.NonDestructive);
        Assert.IsTrue(result.FailClosed);
        Assert.IsFalse(result.PublicDeployAvailable);
        Assert.IsFalse(result.ExternalNetworkAvailable);
        Assert.IsFalse(result.TelemetryOrSyncAvailable);
        Assert.IsFalse(result.ProviderCloudNetworkAvailable);
        Assert.IsFalse(result.DbMigrationAvailable);
        Assert.IsFalse(result.KmsWormExternalTrustAvailable);
        Assert.IsFalse(result.BrowserCdpWcuOcrRecipesLiveAvailable);
        Assert.IsFalse(result.DestructiveActionAvailable);
        Assert.IsFalse(result.UnboundedPhysicalExportAvailable);
        Assert.IsFalse(result.ExternalCloudExportAvailable);
        Assert.IsFalse(result.ReleaseCommercialReady);
    }

    private static void AssertCanonicalSurface(ProductLedgerLocalDevRoutePreviewResult result)
    {
        Assert.IsNotNull(result.CanonicalSurface);
        Assert.AreEqual(ProductLedgerOperatorSurfaceModelFactory.CanonicalSurfaceId, result.CanonicalSurface.SurfaceId);
        Assert.AreEqual(ProductLedgerLocalDevRoutePreview.RouteTemplatePreview, result.CanonicalSurface.RoutePath);
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
