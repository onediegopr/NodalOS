using System.Text;

namespace OneBrain.Core.Approval;

public enum ProductLedgerLocalDevRoutePreviewDecision
{
    Rejected,
    RenderedLocalDevInternalPreview
}

public enum ProductLedgerLocalDevRoutePreviewBlocker
{
    MissingRequest,
    MissingExplicitLocalDevInternalPreviewScope,
    NonLocalMode,
    MissingDevMode,
    ProductionModeClaimed,
    ReleaseModeClaimed,
    CommercialModeClaimed,
    ExternalHostOrPublicOriginClaimed,
    PublicDeployClaimed,
    TelemetryOrSyncClaimed,
    ProviderCloudNetworkClaimed,
    DbMigrationClaimed,
    KmsWormExternalTrustClaimed,
    BrowserCdpWcuOcrRecipesLiveClaimed,
    DestructiveActionRequested,
    UnboundedPhysicalExportClaimed,
    ExternalCloudExportClaimed,
    RawPayloadOrSecretClaimed,
    MissingRenderableSnapshotRequest,
    RenderableSnapshotRejected,
    UnsafeRenderableSnapshot
}

public sealed record ProductLedgerLocalDevRoutePreviewRequest(
    bool ExplicitLocalDevInternalPreviewScope,
    bool LocalMode,
    bool DevMode,
    bool ClaimsProductionMode,
    bool ClaimsReleaseMode,
    bool ClaimsCommercialMode,
    bool ClaimsExternalHostOrPublicOrigin,
    bool ClaimsPublicDeploy,
    bool ClaimsTelemetryOrSync,
    bool ClaimsProviderCloudNetwork,
    bool ClaimsDbMigration,
    bool ClaimsKmsWormExternalTrust,
    bool ClaimsBrowserCdpWcuOcrRecipesLive,
    bool RequestsDestructiveAction,
    bool ClaimsUnboundedPhysicalExport,
    bool ClaimsExternalCloudExport,
    bool ClaimsRawPayloadOrSecret,
    ProductLedgerRenderableOperatorSurfaceRequest? RenderableSnapshotRequest);

public sealed record ProductLedgerLocalDevRoutePreviewResult(
    ProductLedgerLocalDevRoutePreviewDecision Decision,
    IReadOnlyList<ProductLedgerLocalDevRoutePreviewBlocker> Blockers,
    ProductLedgerRenderableOperatorSurfaceResult RenderableSnapshot,
    ProductLedgerOperatorSurfaceModel CanonicalSurface,
    string RouteTemplatePreview,
    string ContentType,
    string HtmlSnapshot,
    IReadOnlyList<string> Notices,
    bool LocalOnly,
    bool DevOnly,
    bool InternalOnly,
    bool ReadOnly,
    bool NonDestructive,
    bool FailClosed,
    bool PublicDeployAvailable,
    bool ExternalNetworkAvailable,
    bool TelemetryOrSyncAvailable,
    bool ProviderCloudNetworkAvailable,
    bool DbMigrationAvailable,
    bool KmsWormExternalTrustAvailable,
    bool BrowserCdpWcuOcrRecipesLiveAvailable,
    bool DestructiveActionAvailable,
    bool UnboundedPhysicalExportAvailable,
    bool ExternalCloudExportAvailable,
    bool ReleaseCommercialReady,
    string StatusText);

public sealed class ProductLedgerLocalDevRoutePreview
{
    public const string LegacyRouteTemplatePreview =
        "/__internal/local-dev/product-ledger/operator-snapshot";

    public const string RouteTemplatePreview =
        "/internal/product-ledger/operator-surface";

    public const string ReadyStatus =
        "PRODUCT_LEDGER_LOCAL_DEV_ROUTE_INTERNAL_ENDPOINT_PREVIEW_READY LOCAL_DEV_INTERNAL_ONLY READ_ONLY NON_DESTRUCTIVE FAIL_CLOSED NOT_PUBLICLY_DEPLOYED NO_EXTERNAL_NETWORK NO_TELEMETRY NO_PROVIDER_CLOUD_NETWORK NO_DB_MIGRATION NO_KMS_WORM_EXTERNAL_TRUST NO_LIVE_AUTOMATION NO_RELEASE_COMMERCIAL";

    public const string RejectedStatus =
        "PRODUCT_LEDGER_LOCAL_DEV_ROUTE_INTERNAL_ENDPOINT_PREVIEW_REJECTED FAIL_CLOSED NOT_PUBLICLY_DEPLOYED NO_EXTERNAL_NETWORK NO_TELEMETRY NO_PROVIDER_CLOUD_NETWORK NO_DB_MIGRATION NO_KMS_WORM_EXTERNAL_TRUST NO_LIVE_AUTOMATION NO_RELEASE_COMMERCIAL";

    public static ProductLedgerLocalDevRoutePreviewRequest CreateDefaultLocalDevRequest() =>
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
            RenderableSnapshotRequest: CreateDefaultRenderableSnapshotRequest());

    public ProductLedgerLocalDevRoutePreviewResult Render(ProductLedgerLocalDevRoutePreviewRequest? request)
    {
        var blockers = new List<ProductLedgerLocalDevRoutePreviewBlocker>();
        if (request is null)
        {
            blockers.Add(ProductLedgerLocalDevRoutePreviewBlocker.MissingRequest);
            return Result(blockers, null);
        }

        AddGuardBlockers(request, blockers);

        ProductLedgerRenderableOperatorSurfaceResult? renderable = null;
        if (request.RenderableSnapshotRequest is null)
        {
            blockers.Add(ProductLedgerLocalDevRoutePreviewBlocker.MissingRenderableSnapshotRequest);
        }
        else
        {
            renderable = new ProductLedgerRenderableOperatorSurfaceRenderer().Render(request.RenderableSnapshotRequest);
            AddRenderableBlockers(renderable, blockers);
        }

        return Result(blockers, renderable);
    }

    private static void AddGuardBlockers(
        ProductLedgerLocalDevRoutePreviewRequest request,
        List<ProductLedgerLocalDevRoutePreviewBlocker> blockers)
    {
        if (!request.ExplicitLocalDevInternalPreviewScope)
        {
            blockers.Add(ProductLedgerLocalDevRoutePreviewBlocker.MissingExplicitLocalDevInternalPreviewScope);
        }

        if (!request.LocalMode)
        {
            blockers.Add(ProductLedgerLocalDevRoutePreviewBlocker.NonLocalMode);
        }

        if (!request.DevMode)
        {
            blockers.Add(ProductLedgerLocalDevRoutePreviewBlocker.MissingDevMode);
        }

        if (request.ClaimsProductionMode)
        {
            blockers.Add(ProductLedgerLocalDevRoutePreviewBlocker.ProductionModeClaimed);
        }

        if (request.ClaimsReleaseMode)
        {
            blockers.Add(ProductLedgerLocalDevRoutePreviewBlocker.ReleaseModeClaimed);
        }

        if (request.ClaimsCommercialMode)
        {
            blockers.Add(ProductLedgerLocalDevRoutePreviewBlocker.CommercialModeClaimed);
        }

        if (request.ClaimsExternalHostOrPublicOrigin)
        {
            blockers.Add(ProductLedgerLocalDevRoutePreviewBlocker.ExternalHostOrPublicOriginClaimed);
        }

        if (request.ClaimsPublicDeploy)
        {
            blockers.Add(ProductLedgerLocalDevRoutePreviewBlocker.PublicDeployClaimed);
        }

        if (request.ClaimsTelemetryOrSync)
        {
            blockers.Add(ProductLedgerLocalDevRoutePreviewBlocker.TelemetryOrSyncClaimed);
        }

        if (request.ClaimsProviderCloudNetwork)
        {
            blockers.Add(ProductLedgerLocalDevRoutePreviewBlocker.ProviderCloudNetworkClaimed);
        }

        if (request.ClaimsDbMigration)
        {
            blockers.Add(ProductLedgerLocalDevRoutePreviewBlocker.DbMigrationClaimed);
        }

        if (request.ClaimsKmsWormExternalTrust)
        {
            blockers.Add(ProductLedgerLocalDevRoutePreviewBlocker.KmsWormExternalTrustClaimed);
        }

        if (request.ClaimsBrowserCdpWcuOcrRecipesLive)
        {
            blockers.Add(ProductLedgerLocalDevRoutePreviewBlocker.BrowserCdpWcuOcrRecipesLiveClaimed);
        }

        if (request.RequestsDestructiveAction)
        {
            blockers.Add(ProductLedgerLocalDevRoutePreviewBlocker.DestructiveActionRequested);
        }

        if (request.ClaimsUnboundedPhysicalExport)
        {
            blockers.Add(ProductLedgerLocalDevRoutePreviewBlocker.UnboundedPhysicalExportClaimed);
        }

        if (request.ClaimsExternalCloudExport)
        {
            blockers.Add(ProductLedgerLocalDevRoutePreviewBlocker.ExternalCloudExportClaimed);
        }

        if (request.ClaimsRawPayloadOrSecret)
        {
            blockers.Add(ProductLedgerLocalDevRoutePreviewBlocker.RawPayloadOrSecretClaimed);
        }
    }

    private static void AddRenderableBlockers(
        ProductLedgerRenderableOperatorSurfaceResult renderable,
        List<ProductLedgerLocalDevRoutePreviewBlocker> blockers)
    {
        if (renderable.Decision != ProductLedgerRenderableOperatorSurfaceDecision.RenderedSnapshot
            || renderable.Blockers.Count > 0)
        {
            blockers.Add(ProductLedgerLocalDevRoutePreviewBlocker.RenderableSnapshotRejected);
        }

        if (!renderable.Model.LocalOnly
            || !renderable.Model.InternalOnly
            || !renderable.Model.SnapshotOnly
            || !renderable.Model.Deterministic
            || !renderable.Model.FailClosed
            || renderable.Model.EndpointRouteControllerAvailable
            || renderable.Model.ExternalScriptAvailable
            || renderable.Model.TelemetryOrSyncAvailable
            || renderable.Model.ProviderCloudNetworkAvailable
            || renderable.Model.DbMigrationAvailable
            || renderable.Model.KmsWormExternalTrustAvailable
            || renderable.Model.BrowserCdpWcuOcrRecipesLiveAvailable
            || renderable.Model.ReleaseCommercialReady
            || renderable.Model.RawPayloadOrSecretAvailable)
        {
            blockers.Add(ProductLedgerLocalDevRoutePreviewBlocker.UnsafeRenderableSnapshot);
        }
    }

    private static ProductLedgerLocalDevRoutePreviewResult Result(
        IReadOnlyList<ProductLedgerLocalDevRoutePreviewBlocker> blockers,
        ProductLedgerRenderableOperatorSurfaceResult? renderable)
    {
        var distinct = blockers.Distinct().OrderBy(blocker => blocker.ToString(), StringComparer.Ordinal).ToArray();
        var rendered = distinct.Length == 0 && renderable is not null;
        var safeRenderable = renderable ?? new ProductLedgerRenderableOperatorSurfaceRenderer().Render(null);
        var canonicalSurface = ProductLedgerOperatorSurfaceModelFactory.Build(safeRenderable);
        var html = rendered ? AddLocalDevShell(safeRenderable.HtmlSnapshot, canonicalSurface) : string.Empty;
        return new ProductLedgerLocalDevRoutePreviewResult(
            Decision: rendered
                ? ProductLedgerLocalDevRoutePreviewDecision.RenderedLocalDevInternalPreview
                : ProductLedgerLocalDevRoutePreviewDecision.Rejected,
            Blockers: distinct,
            RenderableSnapshot: safeRenderable,
            CanonicalSurface: canonicalSurface,
            RouteTemplatePreview: RouteTemplatePreview,
            ContentType: "text/html; charset=utf-8",
            HtmlSnapshot: html,
            Notices: Notices(),
            LocalOnly: true,
            DevOnly: true,
            InternalOnly: true,
            ReadOnly: true,
            NonDestructive: true,
            FailClosed: true,
            PublicDeployAvailable: false,
            ExternalNetworkAvailable: false,
            TelemetryOrSyncAvailable: false,
            ProviderCloudNetworkAvailable: false,
            DbMigrationAvailable: false,
            KmsWormExternalTrustAvailable: false,
            BrowserCdpWcuOcrRecipesLiveAvailable: false,
            DestructiveActionAvailable: false,
            UnboundedPhysicalExportAvailable: false,
            ExternalCloudExportAvailable: false,
            ReleaseCommercialReady: false,
            StatusText: rendered ? ReadyStatus : RejectedStatus);
    }

    private static IReadOnlyList<string> Notices() =>
    [
        "local-dev/internal-only",
        "not publicly deployed",
        "read-only",
        "non-destructive",
        "no telemetry",
        "no external network",
        "no release/commercial",
        "no external trust",
        "no WORM/KMS/cloud",
        "not compliance-grade custody"
    ];

    private static string AddLocalDevShell(
        string htmlSnapshot,
        ProductLedgerOperatorSurfaceModel canonicalSurface)
    {
        const string body = "<body>";
        var banner =
            "  <section data-testid=\"local-dev-route-preview\" data-local-dev=\"true\" data-internal-only=\"true\" data-read-only=\"true\" data-public-deploy=\"false\" data-external-network=\"false\" data-telemetry=\"false\">\n" +
            $"    <p data-testid=\"local-dev-route-template\">{RouteTemplatePreview}</p>\n" +
            $"    <p data-testid=\"local-dev-legacy-route-template\">{LegacyRouteTemplatePreview}</p>\n" +
            "    <p>local-dev/internal-only</p>\n" +
            "    <p>not publicly deployed</p>\n" +
            "    <p>no telemetry</p>\n" +
            "    <p>no external network</p>\n" +
            "    <p>no release/commercial</p>\n" +
            "    <p>no external trust</p>\n" +
            "    <p>no WORM/KMS/cloud</p>\n" +
            "    <p>not compliance-grade custody</p>\n" +
            "  </section>";

        return htmlSnapshot.Replace(
            body,
            body + "\n" + banner + "\n" + ToCanonicalSurfaceHtml(canonicalSurface),
            StringComparison.Ordinal);
    }

    private static string ToCanonicalSurfaceHtml(ProductLedgerOperatorSurfaceModel model)
    {
        var html = new StringBuilder();
        html.AppendLine(
            $"  <section data-testid=\"canonical-surface-model\" data-surface-id=\"{Encode(model.SurfaceId)}\" data-route-path=\"{Encode(model.RoutePath)}\" data-scope=\"{Encode(model.Scope)}\" data-read-model-mode=\"{Encode(model.ReadModelMode.ToString())}\" data-local-only=\"{Lower(model.IsLocalOnly)}\" data-development-only=\"{Lower(model.IsDevelopmentOnly)}\" data-read-only=\"{Lower(model.IsReadOnly)}\" data-product-command-execution=\"{Lower(model.AllowsProductCommandExecution)}\" data-public-internet=\"{Lower(model.AllowsPublicInternetExposure)}\" data-external-network=\"{Lower(model.AllowsExternalNetwork)}\" data-db-migration=\"{Lower(model.AllowsDbMigration)}\" data-kms-worm-external-trust=\"{Lower(model.AllowsKmsWormExternalTrust)}\" data-release-commercial=\"{Lower(model.AllowsReleaseCommercial)}\">");
        html.AppendLine("    <h2>Canonical Product Ledger operator surface</h2>");
        html.AppendLine($"    <p data-testid=\"surface-ledger-authority\">{Encode(model.LedgerAuthority)} / {Encode(model.LedgerAuthorityBoundaryStatus)}</p>");
        html.AppendLine($"    <p data-testid=\"surface-ledger-verification\">{Encode(model.LedgerVerificationStatus)}</p>");
        html.AppendLine($"    <p data-testid=\"surface-checkpoint\">{Encode(model.CheckpointStatus)}</p>");
        html.AppendLine($"    <p data-testid=\"surface-redaction-retention\">{Encode(model.RedactionRetentionGuardStatus)}</p>");
        html.AppendLine($"    <p data-testid=\"surface-concurrency\">{Encode(model.ConcurrencyGuardStatus)}</p>");
        html.AppendLine($"    <p data-testid=\"surface-bounded-export\">{Encode(model.BoundedExportStatus)}</p>");
        html.AppendLine($"    <p data-testid=\"surface-operator-acceptance\">{Encode(model.OperatorAcceptanceStatus)}</p>");
        html.AppendLine($"    <p data-testid=\"surface-public-action-contract\">{Encode(model.PublicLocalOnlyActionContractStatus)}</p>");
        html.AppendLine($"    <p data-testid=\"surface-visual-evidence\">{Encode(model.VisualEvidenceStatus)}</p>");
        html.AppendLine($"    <p data-testid=\"surface-screenshot-evidence\">{Encode(model.ScreenshotEvidenceStatus)}</p>");
        html.AppendLine("    <div data-testid=\"surface-statuses\">");
        foreach (var status in model.Statuses.OrderBy(status => status.StatusId, StringComparer.Ordinal))
        {
            html.AppendLine($"      <p data-testid=\"surface-status-{Encode(status.StatusId)}\">{Encode(status.Label)}: {Encode(status.Value)}; evidence={Encode(status.EvidenceRef)}; readiness={status.ReadinessPercent}%</p>");
        }

        html.AppendLine("    </div>");
        html.AppendLine("    <div data-testid=\"surface-evidence-refs\">");
        foreach (var evidenceRef in model.EvidenceRefs.OrderBy(evidenceRef => evidenceRef.EvidenceId, StringComparer.Ordinal))
        {
            html.AppendLine($"      <p data-testid=\"surface-evidence-{Encode(evidenceRef.EvidenceId)}\">{Encode(evidenceRef.Source)} / {Encode(evidenceRef.Boundary)}</p>");
        }

        html.AppendLine("    </div>");
        html.AppendLine("    <div data-testid=\"surface-blocked-frontiers\">");
        foreach (var frontier in model.BlockedFrontiers.OrderBy(frontier => frontier.FrontierId, StringComparer.Ordinal))
        {
            html.AppendLine($"      <p data-testid=\"surface-blocked-{Encode(frontier.FrontierId)}\">{Encode(frontier.Label)}: {Encode(frontier.Reason)}</p>");
        }

        html.AppendLine("    </div>");
        html.AppendLine("    <div data-testid=\"surface-safe-next-steps\">");
        foreach (var step in model.SafeNextSteps)
        {
            html.AppendLine($"      <p>{Encode(step)}</p>");
        }

        html.AppendLine("    </div>");
        html.AppendLine("  </section>");
        return html.ToString();
    }

    private static string Encode(string value) =>
        value
            .Replace("&", "&amp;", StringComparison.Ordinal)
            .Replace("<", "&lt;", StringComparison.Ordinal)
            .Replace(">", "&gt;", StringComparison.Ordinal)
            .Replace("\"", "&quot;", StringComparison.Ordinal)
            .Replace("'", "&#39;", StringComparison.Ordinal);

    private static string Lower(bool value) => value ? "true" : "false";

    private static ProductLedgerRenderableOperatorSurfaceRequest CreateDefaultRenderableSnapshotRequest() =>
        new(
            ExplicitLocalOnlySnapshotScope: true,
            PublicActionSurface: new ProductLedgerPublicUiActionSurface().Execute(CreateDefaultActionRequest()),
            ClaimsEndpointRouteController: false,
            ClaimsExternalScript: false,
            ClaimsTelemetryOrSync: false,
            ClaimsProviderCloudNetwork: false,
            ClaimsDbMigration: false,
            ClaimsKmsWormExternalTrust: false,
            ClaimsBrowserCdpWcuOcrRecipesLive: false,
            ClaimsReleaseCommercial: false,
            ClaimsRawPayloadOrSecret: false);

    private static ProductLedgerPublicUiActionRequest CreateDefaultActionRequest() =>
        new(
            ExplicitPublicLocalOnlyNonDestructiveScope: true,
            PublicReadOnlyDisabledPreview: CreateDefaultPublicPreview(),
            ActionKind: ProductLedgerPublicUiActionKind.ViewDiagnostics,
            RawActionName: ProductLedgerPublicUiActionKind.ViewDiagnostics.ToString(),
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

    private static ProductLedgerPublicUiReadOnlyDisabledPreviewResult CreateDefaultPublicPreview() =>
        new ProductLedgerPublicUiReadOnlyDisabledPreviewPresenter().Render(
            new ProductLedgerPublicUiReadOnlyDisabledPreviewRequest(
                ExplicitReadOnlyDisabledMockScope: true,
                InternalPreview: CreateDefaultInternalPreview(),
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

    private static ProductLedgerInternalOperatorUiPreviewResult CreateDefaultInternalPreview() =>
        new ProductLedgerInternalOperatorUiPresenter().Render(
            new ProductLedgerInternalOperatorUiPreviewRequest(
                ExplicitInternalLocalOnlyReadOnlyPreviewScope: true,
                Diagnostics: CreateDefaultDiagnostics(),
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

    private static ProductLedgerLocalOnlyOperatorDiagnosticsResult CreateDefaultDiagnostics() =>
        new(
            Decision: ProductLedgerLocalOnlyOperatorDiagnosticsDecision.RenderedReadOnly,
            Blockers: [],
            Sections:
            [
                Section("Runtime Local-Only Gate", "ENABLED_LOCAL_ONLY_INTERNAL", ["feature_flag=enabled:local-only-internal"]),
                Section("Product Ledger Path Policy", "ACTIVE_LOCAL_ONLY_POLICY_BOUND", ["candidate_id=ledger-local-dev-route-default"]),
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
}
