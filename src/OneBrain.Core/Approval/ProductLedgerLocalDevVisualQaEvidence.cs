namespace OneBrain.Core.Approval;

public enum ProductLedgerLocalDevVisualQaEvidenceDecision
{
    Rejected,
    EvidenceReady
}

public enum ProductLedgerLocalDevVisualQaEvidenceBlocker
{
    MissingRequest,
    MissingExplicitLocalOnlyFixtureScope,
    MissingFixtureOnlyStaticHtmlEvidenceScope,
    RealScreenshotRequested,
    BrowserCdpProductiveClaimed,
    PublicDeployClaimed,
    ExternalNetworkClaimed,
    TelemetryOrSyncClaimed,
    ProviderCloudNetworkClaimed,
    DbMigrationClaimed,
    KmsWormExternalTrustClaimed,
    WcuOcrRecipesLiveClaimed,
    DestructiveActionClaimed,
    UnboundedPhysicalExportClaimed,
    ExternalCloudExportClaimed,
    ReleaseCommercialClaimed,
    RawPayloadOrSecretClaimed,
    MissingRoutePreviewRequest,
    RoutePreviewRejected,
    UnsafeRoutePreview,
    RequiredVisualSectionMissing,
    ForbiddenActiveClaimVisible
}

public sealed record ProductLedgerLocalDevVisualQaEvidenceRequest(
    bool ExplicitLocalOnlyFixtureScope,
    bool FixtureOnlyStaticHtmlEvidenceScope,
    bool RequestsRealScreenshot,
    bool ClaimsBrowserCdpProductive,
    bool ClaimsPublicDeploy,
    bool ClaimsExternalNetwork,
    bool ClaimsTelemetryOrSync,
    bool ClaimsProviderCloudNetwork,
    bool ClaimsDbMigration,
    bool ClaimsKmsWormExternalTrust,
    bool ClaimsWcuOcrRecipesLive,
    bool ClaimsDestructiveAction,
    bool ClaimsUnboundedPhysicalExport,
    bool ClaimsExternalCloudExport,
    bool ClaimsReleaseCommercial,
    bool ClaimsRawPayloadOrSecret,
    ProductLedgerLocalDevRoutePreviewRequest? RoutePreviewRequest);

public sealed record ProductLedgerLocalDevVisualQaEvidenceResult(
    ProductLedgerLocalDevVisualQaEvidenceDecision Decision,
    IReadOnlyList<ProductLedgerLocalDevVisualQaEvidenceBlocker> Blockers,
    ProductLedgerLocalDevRoutePreviewResult RoutePreview,
    string ScreenshotMode,
    string VisualArtifactHtml,
    IReadOnlyList<string> PositiveAssertions,
    IReadOnlyList<string> NegativeAssertions,
    bool LocalOnly,
    bool DevelopmentOnly,
    bool FixtureOnly,
    bool StaticHtmlOnly,
    bool RealScreenshotCaptured,
    bool BrowserCdpProductiveUsed,
    bool PublicDeployAvailable,
    bool ExternalNetworkAvailable,
    bool TelemetryOrSyncAvailable,
    bool ProviderCloudNetworkAvailable,
    bool DbMigrationAvailable,
    bool KmsWormExternalTrustAvailable,
    bool WcuOcrRecipesLiveAvailable,
    bool DestructiveActionAvailable,
    bool UnboundedPhysicalExportAvailable,
    bool ExternalCloudExportAvailable,
    bool ReleaseCommercialReady,
    string StatusText);

public sealed class ProductLedgerLocalDevVisualQaEvidence
{
    public const string ReadyStatus =
        "PRODUCT_LEDGER_LOCAL_DEV_VISUAL_QA_SCREENSHOT_EVIDENCE_READY LOCAL_ONLY DEVELOPMENT_ONLY FIXTURE_ONLY STATIC_HTML_SCREENSHOT_PLACEHOLDER NO_PUBLIC_DEPLOY NO_EXTERNAL_NETWORK NO_TELEMETRY NO_PROVIDER_CLOUD_NETWORK NO_DB_MIGRATION NO_KMS_WORM_EXTERNAL_TRUST NO_PRODUCTIVE_BROWSER_CDP NO_WCU_OCR_RECIPES_LIVE NO_DESTRUCTIVE_ACTION NO_UNBOUNDED_EXPORT NO_RELEASE_COMMERCIAL";

    public const string RejectedStatus =
        "PRODUCT_LEDGER_LOCAL_DEV_VISUAL_QA_SCREENSHOT_EVIDENCE_REJECTED FAIL_CLOSED NO_PUBLIC_DEPLOY NO_EXTERNAL_NETWORK NO_TELEMETRY NO_PROVIDER_CLOUD_NETWORK NO_DB_MIGRATION NO_KMS_WORM_EXTERNAL_TRUST NO_PRODUCTIVE_BROWSER_CDP NO_WCU_OCR_RECIPES_LIVE NO_DESTRUCTIVE_ACTION NO_UNBOUNDED_EXPORT NO_RELEASE_COMMERCIAL";

    private static readonly string[] RequiredPositiveFragments =
    [
        "Product Ledger Operator Surface Snapshot",
        "data-testid=\"runtime-gate\"",
        "data-testid=\"writer\"",
        "data-testid=\"bounded-export\"",
        "data-testid=\"evidence-gates\"",
        "data-testid=\"disabled-dangerous-actions\"",
        "data-testid=\"safe-next-step\"",
        "local-dev/internal-only",
        "not publicly deployed",
        "no telemetry",
        "no release/commercial",
        "no external trust",
        "no WORM/KMS/cloud",
        "not compliance-grade custody"
    ];

    private static readonly string[] ForbiddenActiveFragments =
    [
        "data-executable=\"true\"",
        "data-public-deploy=\"true\"",
        "data-external-network=\"true\"",
        "data-telemetry=\"true\"",
        "<script",
        "src=\"http",
        "href=\"http",
        "onclick=",
        "formaction=",
        "release ready",
        "commercial ready",
        "WORM/KMS enabled",
        "external trust enabled",
        "cloud/provider connected",
        "DB enabled",
        "telemetry enabled",
        "live Browser/CDP/WCU/OCR/Recipes",
        "destructive action enabled",
        "unbounded export/write enabled"
    ];

    public static ProductLedgerLocalDevVisualQaEvidenceRequest CreateDefaultFixtureRequest() =>
        new(
            ExplicitLocalOnlyFixtureScope: true,
            FixtureOnlyStaticHtmlEvidenceScope: true,
            RequestsRealScreenshot: false,
            ClaimsBrowserCdpProductive: false,
            ClaimsPublicDeploy: false,
            ClaimsExternalNetwork: false,
            ClaimsTelemetryOrSync: false,
            ClaimsProviderCloudNetwork: false,
            ClaimsDbMigration: false,
            ClaimsKmsWormExternalTrust: false,
            ClaimsWcuOcrRecipesLive: false,
            ClaimsDestructiveAction: false,
            ClaimsUnboundedPhysicalExport: false,
            ClaimsExternalCloudExport: false,
            ClaimsReleaseCommercial: false,
            ClaimsRawPayloadOrSecret: false,
            RoutePreviewRequest: ProductLedgerLocalDevRoutePreview.CreateDefaultLocalDevRequest());

    public ProductLedgerLocalDevVisualQaEvidenceResult Evaluate(ProductLedgerLocalDevVisualQaEvidenceRequest? request)
    {
        var blockers = new List<ProductLedgerLocalDevVisualQaEvidenceBlocker>();
        if (request is null)
        {
            blockers.Add(ProductLedgerLocalDevVisualQaEvidenceBlocker.MissingRequest);
            return Result(blockers, null);
        }

        AddRequestBlockers(request, blockers);

        ProductLedgerLocalDevRoutePreviewResult? routePreview = null;
        if (request.RoutePreviewRequest is null)
        {
            blockers.Add(ProductLedgerLocalDevVisualQaEvidenceBlocker.MissingRoutePreviewRequest);
        }
        else
        {
            routePreview = new ProductLedgerLocalDevRoutePreview().Render(request.RoutePreviewRequest);
            AddRoutePreviewBlockers(routePreview, blockers);
            AddVisualBlockers(routePreview.HtmlSnapshot, blockers);
        }

        return Result(blockers, routePreview);
    }

    private static void AddRequestBlockers(
        ProductLedgerLocalDevVisualQaEvidenceRequest request,
        List<ProductLedgerLocalDevVisualQaEvidenceBlocker> blockers)
    {
        if (!request.ExplicitLocalOnlyFixtureScope)
        {
            blockers.Add(ProductLedgerLocalDevVisualQaEvidenceBlocker.MissingExplicitLocalOnlyFixtureScope);
        }

        if (!request.FixtureOnlyStaticHtmlEvidenceScope)
        {
            blockers.Add(ProductLedgerLocalDevVisualQaEvidenceBlocker.MissingFixtureOnlyStaticHtmlEvidenceScope);
        }

        if (request.RequestsRealScreenshot)
        {
            blockers.Add(ProductLedgerLocalDevVisualQaEvidenceBlocker.RealScreenshotRequested);
        }

        if (request.ClaimsBrowserCdpProductive)
        {
            blockers.Add(ProductLedgerLocalDevVisualQaEvidenceBlocker.BrowserCdpProductiveClaimed);
        }

        if (request.ClaimsPublicDeploy)
        {
            blockers.Add(ProductLedgerLocalDevVisualQaEvidenceBlocker.PublicDeployClaimed);
        }

        if (request.ClaimsExternalNetwork)
        {
            blockers.Add(ProductLedgerLocalDevVisualQaEvidenceBlocker.ExternalNetworkClaimed);
        }

        if (request.ClaimsTelemetryOrSync)
        {
            blockers.Add(ProductLedgerLocalDevVisualQaEvidenceBlocker.TelemetryOrSyncClaimed);
        }

        if (request.ClaimsProviderCloudNetwork)
        {
            blockers.Add(ProductLedgerLocalDevVisualQaEvidenceBlocker.ProviderCloudNetworkClaimed);
        }

        if (request.ClaimsDbMigration)
        {
            blockers.Add(ProductLedgerLocalDevVisualQaEvidenceBlocker.DbMigrationClaimed);
        }

        if (request.ClaimsKmsWormExternalTrust)
        {
            blockers.Add(ProductLedgerLocalDevVisualQaEvidenceBlocker.KmsWormExternalTrustClaimed);
        }

        if (request.ClaimsWcuOcrRecipesLive)
        {
            blockers.Add(ProductLedgerLocalDevVisualQaEvidenceBlocker.WcuOcrRecipesLiveClaimed);
        }

        if (request.ClaimsDestructiveAction)
        {
            blockers.Add(ProductLedgerLocalDevVisualQaEvidenceBlocker.DestructiveActionClaimed);
        }

        if (request.ClaimsUnboundedPhysicalExport)
        {
            blockers.Add(ProductLedgerLocalDevVisualQaEvidenceBlocker.UnboundedPhysicalExportClaimed);
        }

        if (request.ClaimsExternalCloudExport)
        {
            blockers.Add(ProductLedgerLocalDevVisualQaEvidenceBlocker.ExternalCloudExportClaimed);
        }

        if (request.ClaimsReleaseCommercial)
        {
            blockers.Add(ProductLedgerLocalDevVisualQaEvidenceBlocker.ReleaseCommercialClaimed);
        }

        if (request.ClaimsRawPayloadOrSecret)
        {
            blockers.Add(ProductLedgerLocalDevVisualQaEvidenceBlocker.RawPayloadOrSecretClaimed);
        }
    }

    private static void AddRoutePreviewBlockers(
        ProductLedgerLocalDevRoutePreviewResult routePreview,
        List<ProductLedgerLocalDevVisualQaEvidenceBlocker> blockers)
    {
        if (routePreview.Decision != ProductLedgerLocalDevRoutePreviewDecision.RenderedLocalDevInternalPreview
            || routePreview.Blockers.Count > 0)
        {
            blockers.Add(ProductLedgerLocalDevVisualQaEvidenceBlocker.RoutePreviewRejected);
        }

        if (!routePreview.LocalOnly
            || !routePreview.DevOnly
            || !routePreview.InternalOnly
            || !routePreview.ReadOnly
            || !routePreview.NonDestructive
            || !routePreview.FailClosed
            || routePreview.PublicDeployAvailable
            || routePreview.ExternalNetworkAvailable
            || routePreview.TelemetryOrSyncAvailable
            || routePreview.ProviderCloudNetworkAvailable
            || routePreview.DbMigrationAvailable
            || routePreview.KmsWormExternalTrustAvailable
            || routePreview.BrowserCdpWcuOcrRecipesLiveAvailable
            || routePreview.DestructiveActionAvailable
            || routePreview.UnboundedPhysicalExportAvailable
            || routePreview.ExternalCloudExportAvailable
            || routePreview.ReleaseCommercialReady)
        {
            blockers.Add(ProductLedgerLocalDevVisualQaEvidenceBlocker.UnsafeRoutePreview);
        }
    }

    private static void AddVisualBlockers(
        string html,
        List<ProductLedgerLocalDevVisualQaEvidenceBlocker> blockers)
    {
        if (RequiredPositiveFragments.Any(fragment => !html.Contains(fragment, StringComparison.OrdinalIgnoreCase)))
        {
            blockers.Add(ProductLedgerLocalDevVisualQaEvidenceBlocker.RequiredVisualSectionMissing);
        }

        if (ForbiddenActiveFragments.Any(fragment => html.Contains(fragment, StringComparison.OrdinalIgnoreCase)))
        {
            blockers.Add(ProductLedgerLocalDevVisualQaEvidenceBlocker.ForbiddenActiveClaimVisible);
        }
    }

    private static ProductLedgerLocalDevVisualQaEvidenceResult Result(
        IReadOnlyList<ProductLedgerLocalDevVisualQaEvidenceBlocker> blockers,
        ProductLedgerLocalDevRoutePreviewResult? routePreview)
    {
        var distinct = blockers.Distinct().OrderBy(blocker => blocker.ToString(), StringComparer.Ordinal).ToArray();
        var ready = distinct.Length == 0 && routePreview is not null;
        var safeRoutePreview = routePreview ?? new ProductLedgerLocalDevRoutePreview().Render(null);
        return new ProductLedgerLocalDevVisualQaEvidenceResult(
            Decision: ready
                ? ProductLedgerLocalDevVisualQaEvidenceDecision.EvidenceReady
                : ProductLedgerLocalDevVisualQaEvidenceDecision.Rejected,
            Blockers: distinct,
            RoutePreview: safeRoutePreview,
            ScreenshotMode: "STATIC_HTML_FIXTURE_NO_BROWSER_CDP",
            VisualArtifactHtml: ready ? BuildVisualArtifact(safeRoutePreview) : string.Empty,
            PositiveAssertions: RequiredPositiveFragments,
            NegativeAssertions: ForbiddenActiveFragments,
            LocalOnly: true,
            DevelopmentOnly: true,
            FixtureOnly: true,
            StaticHtmlOnly: true,
            RealScreenshotCaptured: false,
            BrowserCdpProductiveUsed: false,
            PublicDeployAvailable: false,
            ExternalNetworkAvailable: false,
            TelemetryOrSyncAvailable: false,
            ProviderCloudNetworkAvailable: false,
            DbMigrationAvailable: false,
            KmsWormExternalTrustAvailable: false,
            WcuOcrRecipesLiveAvailable: false,
            DestructiveActionAvailable: false,
            UnboundedPhysicalExportAvailable: false,
            ExternalCloudExportAvailable: false,
            ReleaseCommercialReady: false,
            StatusText: ready ? ReadyStatus : RejectedStatus);
    }

    private static string BuildVisualArtifact(ProductLedgerLocalDevRoutePreviewResult routePreview) =>
        "<!doctype html>\n" +
        "<html lang=\"en\" data-testid=\"product-ledger-visual-qa-evidence\" data-local-only=\"true\" data-development-only=\"true\" data-fixture-only=\"true\">\n" +
        "<head><meta charset=\"utf-8\"><title>Product Ledger Local Dev Visual QA Evidence</title></head>\n" +
        "<body>\n" +
        "  <section data-testid=\"visual-qa-summary\">\n" +
        "    <h1>Product Ledger Local Dev Visual QA Evidence</h1>\n" +
        "    <p>local-only</p><p>Development-only or fixture-only</p><p>not deployed</p><p>no telemetry</p><p>no external network</p><p>no release/commercial</p><p>no external trust</p><p>no WORM/KMS/cloud</p><p>not compliance-grade custody</p>\n" +
        "    <p data-testid=\"screenshot-mode\">STATIC_HTML_FIXTURE_NO_BROWSER_CDP</p>\n" +
        "  </section>\n" +
        routePreview.HtmlSnapshot +
        "\n</body>\n</html>\n";
}
