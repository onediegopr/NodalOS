using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.Core.Approval;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("ApprovalHumanReview")]
[TestCategory("PhaseEApprovalHumanReview")]
public sealed class ProductLedgerRenderableOperatorSurfaceTests
{
    [TestMethod]
    public void RenderableOperatorSurface_FailsClosedByDefaultAndRejectsExternalClaims()
    {
        var renderer = new ProductLedgerRenderableOperatorSurfaceRenderer();
        var missing = renderer.Render(null);
        var ready = ReadyRequest();
        var cases = new Dictionary<ProductLedgerRenderableOperatorSurfaceRequest, ProductLedgerRenderableOperatorSurfaceBlocker>
        {
            [ready with { ExplicitLocalOnlySnapshotScope = false }] = ProductLedgerRenderableOperatorSurfaceBlocker.MissingExplicitLocalOnlySnapshotScope,
            [ready with { PublicActionSurface = null }] = ProductLedgerRenderableOperatorSurfaceBlocker.MissingPublicActionSurface,
            [ready with { PublicActionSurface = UnsafeActionSurface() }] = ProductLedgerRenderableOperatorSurfaceBlocker.UnsafePublicActionSurface,
            [ready with { ClaimsEndpointRouteController = true }] = ProductLedgerRenderableOperatorSurfaceBlocker.EndpointRouteControllerClaimed,
            [ready with { ClaimsExternalScript = true }] = ProductLedgerRenderableOperatorSurfaceBlocker.ExternalScriptClaimed,
            [ready with { ClaimsTelemetryOrSync = true }] = ProductLedgerRenderableOperatorSurfaceBlocker.TelemetryOrSyncClaimed,
            [ready with { ClaimsProviderCloudNetwork = true }] = ProductLedgerRenderableOperatorSurfaceBlocker.ProviderCloudNetworkClaimed,
            [ready with { ClaimsDbMigration = true }] = ProductLedgerRenderableOperatorSurfaceBlocker.DbMigrationClaimed,
            [ready with { ClaimsKmsWormExternalTrust = true }] = ProductLedgerRenderableOperatorSurfaceBlocker.KmsWormExternalTrustClaimed,
            [ready with { ClaimsBrowserCdpWcuOcrRecipesLive = true }] = ProductLedgerRenderableOperatorSurfaceBlocker.BrowserCdpWcuOcrRecipesLiveClaimed,
            [ready with { ClaimsReleaseCommercial = true }] = ProductLedgerRenderableOperatorSurfaceBlocker.ReleaseCommercialClaimed,
            [ready with { ClaimsRawPayloadOrSecret = true }] = ProductLedgerRenderableOperatorSurfaceBlocker.RawPayloadOrSecretClaimed
        };

        AssertRejected(missing, ProductLedgerRenderableOperatorSurfaceBlocker.MissingRequest);
        foreach (var testCase in cases)
        {
            AssertRejected(renderer.Render(testCase.Key), testCase.Value);
        }
    }

    [TestMethod]
    public void RenderableOperatorSurface_RendersDeterministicSnapshotWithRequiredSections()
    {
        var renderer = new ProductLedgerRenderableOperatorSurfaceRenderer();
        var first = renderer.Render(ReadyRequest());
        var second = renderer.Render(ReadyRequest());

        Assert.AreEqual(ProductLedgerRenderableOperatorSurfaceDecision.RenderedSnapshot, first.Decision);
        Assert.AreEqual(first.HtmlSnapshot, second.HtmlSnapshot);
        Assert.AreEqual(100, first.Model.RenderableOperatorSnapshotPercent);
        Assert.AreEqual(100, first.Model.DomContractPercent);
        Assert.AreEqual(0, first.Model.ExternalCloudReadinessPercent);
        Assert.AreEqual(0, first.Model.KmsWormExternalTrustPercent);
        Assert.AreEqual(0, first.Model.ReleaseCommercialReadinessPercent);
        AssertNoExternalRelease(first);

        var html = first.HtmlSnapshot;
        StringAssert.Contains(html, "data-testid=\"header-local-only\"");
        StringAssert.Contains(html, "data-testid=\"runtime-gate\"");
        StringAssert.Contains(html, "data-testid=\"writer\"");
        StringAssert.Contains(html, "data-testid=\"bounded-export\"");
        StringAssert.Contains(html, "data-testid=\"evidence-gates\"");
        StringAssert.Contains(html, "data-testid=\"disabled-dangerous-actions\"");
        StringAssert.Contains(html, "data-testid=\"safe-next-step\"");
        StringAssert.Contains(html, "renderable snapshot fixture");
        StringAssert.Contains(html, "not deployed");
        StringAssert.Contains(html, "no public route");
        StringAssert.Contains(html, "no telemetry");
        StringAssert.Contains(html, "not compliance-grade custody");
    }

    [TestMethod]
    public void RenderableOperatorSurface_DomContractHasNoExecutableDangerousButtonsHandlersRoutesOrScripts()
    {
        var result = new ProductLedgerRenderableOperatorSurfaceRenderer().Render(ReadyRequest());
        var html = result.HtmlSnapshot;

        Assert.AreEqual(ProductLedgerRenderableOperatorSurfaceDecision.RenderedSnapshot, result.Decision);
        Assert.IsFalse(html.Contains("<script", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(html.Contains("src=\"http", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(html.Contains("onclick=", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(html.Contains("formaction=", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(html.Contains("href=\"http", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(html.Contains("Map" + "Post", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(html.Contains("Map" + "Get", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(html.Contains("Controller" + "Base", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(html.Contains("MapController" + "Route", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(html.Contains("[" + "Route", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(html.Contains("Http" + "Client", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(html.Contains("Web" + "Socket", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(html.Contains("Db" + "Context", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(html.Contains("Migration" + "Builder", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(html.Contains("Kms" + "Client", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(html.Contains("Worm" + "Store", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(html.Contains("raw payload", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(html.Contains("secret", StringComparison.OrdinalIgnoreCase));

        foreach (var action in new[]
        {
            "execute-action",
            "destructive-write",
            "connect-provider",
            "run-migration",
            "enable-kms",
            "run-browser-cdp",
            "release",
            "unbounded-export",
            "external-export"
        })
        {
            StringAssert.Contains(html, $"data-testid=\"action-{action}\"");
            StringAssert.Contains(html, $"data-testid=\"action-{action}\" data-action-id=\"{action}\"");
            StringAssert.Contains(html, "data-risk=\"blocked dangerous action\"");
            StringAssert.Contains(html, "data-executable=\"false\"");
            StringAssert.Contains(html, "data-handler-id=\"\"");
            StringAssert.Contains(html, "data-callback=\"\"");
        }
    }

    [TestMethod]
    public void RenderableOperatorSurface_SourceHasNoRouteTelemetryNetworkDbKmsLiveOrRelease()
    {
        var source = File.ReadAllText(Path.Combine(
            RepoRoot(),
            "src",
            "OneBrain.Core",
            "Approval",
            "ProductLedgerRenderableOperatorSurface.cs"));
        var forbiddenFragments = new[]
        {
            "IService" + "Collection",
            "Add" + "Singleton",
            "Add" + "Scoped",
            "Add" + "Transient",
            "IHosted" + "Service",
            "Map" + "Post",
            "Map" + "Get",
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
            "EndpointRouteControllerAvailable:" + " true",
            "ExternalScriptAvailable:" + " true",
            "TelemetryOrSyncAvailable:" + " true",
            "ProviderCloudNetworkAvailable:" + " true",
            "DbMigrationAvailable:" + " true",
            "KmsWormExternalTrustAvailable:" + " true",
            "BrowserCdpWcuOcrRecipesLiveAvailable:" + " true",
            "ReleaseCommercialReady:" + " true"
        };

        foreach (var fragment in forbiddenFragments)
        {
            Assert.IsFalse(source.Contains(fragment, StringComparison.OrdinalIgnoreCase), fragment);
        }

        StringAssert.Contains(source, "SNAPSHOT_ONLY");
        StringAssert.Contains(source, "NO_PUBLIC_ROUTE");
    }

    private static ProductLedgerRenderableOperatorSurfaceRequest ReadyRequest() =>
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

    private static ProductLedgerPublicUiActionResult UnsafeActionSurface() =>
        ReadyActionSurface() with { ReleaseCommercialReady = true };

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
                Section("Product Ledger Path Policy", "ACTIVE_LOCAL_ONLY_POLICY_BOUND", ["candidate_id=ledger-renderable-snapshot-001"]),
                Section("Bounded Writer Status", "WRITER_BOUNDED_LOCAL_ONLY_SURFACE_READ_ONLY", ["operator_surface_write_allowed=false"]),
                Section("Checkpoint / Head Status", "VERIFIED_HEAD_PRESENT", ["same_boundary_trust=true"]),
                Section("Evidence Gates", "EVIDENCE_REFERENCES_FRESH_AND_WELL_FORMED", ["redaction_before_persistence=True", "authority=True"]),
                Section("Disabled Actions", "ALL_ACTIONS_DISABLED", ["destructive action", "unbounded export", "external/cloud export"]),
                Section("Safe Next Step", "DOM_CONTRACT_HARDENING_OR_READ_ONLY_AUDIT_ONLY", ["snapshot fixture"])
            ],
            ActionPreviews:
            [
                new("View local-only diagnostics snapshot", "read-only preview only", "operator visibility without execution authority", ["runtime gate"], Disabled: true, ProductiveCommandId: null, HandlerName: null, CallbackName: null)
            ],
            DisabledActions: ["destructive user-facing action", "provider/cloud/network", "release/commercial"],
            SafeNextStep: "RENDERABLE_OPERATOR_SURFACE_SNAPSHOT",
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
        ProductLedgerRenderableOperatorSurfaceResult result,
        ProductLedgerRenderableOperatorSurfaceBlocker blocker)
    {
        Assert.AreEqual(ProductLedgerRenderableOperatorSurfaceDecision.Rejected, result.Decision, blocker.ToString());
        CollectionAssert.Contains(result.Blockers.ToArray(), blocker, blocker.ToString());
        AssertNoExternalRelease(result);
    }

    private static void AssertNoExternalRelease(ProductLedgerRenderableOperatorSurfaceResult result)
    {
        Assert.IsTrue(result.Model.LocalOnly);
        Assert.IsTrue(result.Model.InternalOnly);
        Assert.IsTrue(result.Model.SnapshotOnly);
        Assert.IsTrue(result.Model.Deterministic);
        Assert.IsTrue(result.Model.FailClosed);
        Assert.IsFalse(result.Model.EndpointRouteControllerAvailable);
        Assert.IsFalse(result.Model.ExternalScriptAvailable);
        Assert.IsFalse(result.Model.TelemetryOrSyncAvailable);
        Assert.IsFalse(result.Model.ProviderCloudNetworkAvailable);
        Assert.IsFalse(result.Model.DbMigrationAvailable);
        Assert.IsFalse(result.Model.KmsWormExternalTrustAvailable);
        Assert.IsFalse(result.Model.BrowserCdpWcuOcrRecipesLiveAvailable);
        Assert.IsFalse(result.Model.ReleaseCommercialReady);
        Assert.IsFalse(result.Model.RawPayloadOrSecretAvailable);
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
                "ProductLedgerRenderableOperatorSurface.cs")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        Assert.Fail("repo root not found");
        return string.Empty;
    }
}
