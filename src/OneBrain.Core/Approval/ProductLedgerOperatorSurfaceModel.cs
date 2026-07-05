namespace OneBrain.Core.Approval;

public enum ProductLedgerOperatorSurfaceReadModelMode
{
    FixtureSafeReadModel,
    ExistingLocalLedgerReadModel,
    TestSafeLiveLedgerReadModel
}

public sealed record ProductLedgerOperatorSurfaceStatus(
    string StatusId,
    string Label,
    string Value,
    string EvidenceRef,
    int ReadinessPercent);

public sealed record ProductLedgerOperatorSurfaceEvidenceRef(
    string EvidenceId,
    string Source,
    string Boundary);

public sealed record ProductLedgerOperatorSurfaceBlockedFrontier(
    string FrontierId,
    string Label,
    string Reason);

public sealed record ProductLedgerOperatorSurfaceActionPreview(
    string ActionId,
    string Label,
    bool Disabled,
    bool ReadOnly,
    bool LocalOnly,
    bool NonDestructive,
    string DisabledReason);

public sealed record ProductLedgerOperatorSurfaceModel(
    string SurfaceId,
    string GeneratedAtStrategy,
    string RoutePath,
    string Scope,
    ProductLedgerOperatorSurfaceReadModelMode ReadModelMode,
    string LedgerAuthority,
    string LedgerAuthorityBoundaryStatus,
    string LedgerVerificationStatus,
    string CheckpointStatus,
    string LedgerPathClassification,
    int LedgerEntryCount,
    string LedgerHeadSequence,
    string LedgerHeadHashPrefix,
    string LedgerHashPrefix,
    string RedactionRetentionGuardStatus,
    string ConcurrencyGuardStatus,
    string BoundedExportStatus,
    string OperatorAcceptanceStatus,
    string PublicLocalOnlyActionContractStatus,
    string VisualEvidenceStatus,
    string ScreenshotEvidenceStatus,
    IReadOnlyList<ProductLedgerOperatorSurfaceStatus> Statuses,
    IReadOnlyList<ProductLedgerOperatorSurfaceEvidenceRef> EvidenceRefs,
    IReadOnlyList<ProductLedgerOperatorSurfaceBlockedFrontier> BlockedFrontiers,
    IReadOnlyList<ProductLedgerOperatorSurfaceActionPreview> ActionPreviews,
    ProductLedgerLocalApprovalPreviewLoop ApprovalPreviewLoop,
    IReadOnlyList<string> SafeNextSteps,
    bool IsLocalOnly,
    bool IsDevelopmentOnly,
    bool IsReadOnly,
    bool AllowsProductCommandExecution,
    bool AllowsPublicInternetExposure,
    bool AllowsExternalNetwork,
    bool AllowsDbMigration,
    bool AllowsKmsWormExternalTrust,
    bool AllowsReleaseCommercial,
    bool AllowsBrowserCdpWcuOcrRecipesLive,
    bool AllowsDestructiveAction,
    bool AllowsUnboundedExport,
    bool AllowsExternalCloudExport,
    bool UsesFixtureReadModel,
    bool UsesTestSafeLiveLedgerReadModel);

public static class ProductLedgerOperatorSurfaceModelFactory
{
    public const string CanonicalSurfaceId = "product-ledger.local-dev.operator-surface.v1";
    public const string DeterministicGeneratedAtStrategy = "deterministic-local-dev-read-model";
    public const string Scope = "ProductLedgerLocalOnlyLineScoped";

    public static ProductLedgerOperatorSurfaceModel Build(
        ProductLedgerRenderableOperatorSurfaceResult renderable,
        ProductLedgerOperatorSurfaceReadModelSource? readModelSource = null)
    {
        var readModel = new ProductLedgerOperatorSurfaceReadModelProvider().Read(readModelSource);
        var actions = renderable.Model.Actions
            .Select(action => new ProductLedgerOperatorSurfaceActionPreview(
                ActionId: action.ActionId,
                Label: action.Label,
                Disabled: true,
                ReadOnly: true,
                LocalOnly: action.LocalOnly,
                NonDestructive: action.NonDestructive,
                DisabledReason: action.Disabled
                    ? action.DisabledReason
                    : "Canonical operator route is read-only and does not execute product commands."))
            .OrderBy(action => action.ActionId, StringComparer.Ordinal)
            .ToArray();

        var statuses = Statuses(readModel);
        var evidenceRefs = EvidenceRefs();
        var approvalPreviewLoop = ProductLedgerLocalApprovalPreviewLoopFactory.Build(
            CanonicalSurfaceId,
            ProductLedgerLocalDevRoutePreview.RouteTemplatePreview,
            evidenceRefs);
        return new ProductLedgerOperatorSurfaceModel(
            SurfaceId: CanonicalSurfaceId,
            GeneratedAtStrategy: DeterministicGeneratedAtStrategy,
            RoutePath: ProductLedgerLocalDevRoutePreview.RouteTemplatePreview,
            Scope: Scope,
            ReadModelMode: readModel.Mode,
            LedgerAuthority: readModel.LedgerAuthority,
            LedgerAuthorityBoundaryStatus: readModel.LedgerAuthorityBoundaryStatus,
            LedgerVerificationStatus: readModel.LedgerVerificationStatus,
            CheckpointStatus: readModel.CheckpointStatus,
            LedgerPathClassification: readModel.LedgerPathClassification,
            LedgerEntryCount: readModel.EntryCount,
            LedgerHeadSequence: readModel.HeadSequence,
            LedgerHeadHashPrefix: readModel.HeadHashPrefix,
            LedgerHashPrefix: readModel.LedgerHashPrefix,
            RedactionRetentionGuardStatus: readModel.RedactionRetentionGuardStatus,
            ConcurrencyGuardStatus: readModel.ConcurrencyGuardStatus,
            BoundedExportStatus: readModel.BoundedExportStatus,
            OperatorAcceptanceStatus: readModel.OperatorAcceptanceStatus,
            PublicLocalOnlyActionContractStatus: readModel.PublicLocalOnlyActionContractStatus,
            VisualEvidenceStatus: readModel.VisualEvidenceStatus,
            ScreenshotEvidenceStatus: readModel.ScreenshotEvidenceStatus,
            Statuses: statuses,
            EvidenceRefs: evidenceRefs,
            BlockedFrontiers: BlockedFrontiers(),
            ActionPreviews: actions,
            ApprovalPreviewLoop: approvalPreviewLoop,
            SafeNextSteps:
            [
                "RENDERED_UI_INTERACTION_LOCAL_ONLY_TEST_PACK",
                "LOCAL_APPROVAL_TO_ACTION_READ_ONLY_PREVIEW_LOOP",
                "DELETION_LIFECYCLE_DESIGN_ONLY"
            ],
            IsLocalOnly: true,
            IsDevelopmentOnly: true,
            IsReadOnly: true,
            AllowsProductCommandExecution: false,
            AllowsPublicInternetExposure: false,
            AllowsExternalNetwork: false,
            AllowsDbMigration: false,
            AllowsKmsWormExternalTrust: false,
            AllowsReleaseCommercial: false,
            AllowsBrowserCdpWcuOcrRecipesLive: false,
            AllowsDestructiveAction: false,
            AllowsUnboundedExport: false,
            AllowsExternalCloudExport: false,
            UsesFixtureReadModel: readModel.UsesFixtureReadModel,
            UsesTestSafeLiveLedgerReadModel: readModel.UsesTestSafeLiveLedger);
    }

    private static IReadOnlyList<ProductLedgerOperatorSurfaceStatus> Statuses(
        ProductLedgerOperatorSurfaceReadModelSnapshot readModel) =>
    [
        new("ledger-authority", "Ledger authority", readModel.LedgerAuthorityBoundaryStatus, nameof(ProductLedgerLocalLedgerTaxonomy), 96),
        new("ledger-verification", "Ledger verification", readModel.LedgerVerificationStatus, nameof(ProductLedgerPathLocalOnlyActiveWriter), readModel.UsesTestSafeLiveLedger ? 88 : 72),
        new("checkpoint", "Checkpoint/head", readModel.CheckpointStatus, nameof(ProductLedgerPathLocalOnlyActiveWriter), readModel.UsesTestSafeLiveLedger ? 88 : 72),
        new("ledger-entry-count", "Ledger entry count", readModel.EntryCount.ToString(System.Globalization.CultureInfo.InvariantCulture), readModel.SourceId, readModel.UsesTestSafeLiveLedger ? 86 : 60),
        new("ledger-head-prefix", "Ledger head hash prefix", readModel.HeadHashPrefix, readModel.SourceId, readModel.UsesTestSafeLiveLedger ? 86 : 60),
        new("ledger-hash-prefix", "Ledger hash prefix", readModel.LedgerHashPrefix, readModel.SourceId, readModel.UsesTestSafeLiveLedger ? 86 : 60),
        new("redaction-retention", "Redaction/retention", readModel.RedactionRetentionGuardStatus, "redaction-retention-behavioral-gates", 84),
        new("concurrency", "Concurrency", readModel.ConcurrencyGuardStatus, "writer-concurrency-lock", 88),
        new("bounded-export", "Bounded export", readModel.BoundedExportStatus, nameof(ProductLedgerLocalReportExportService), 70),
        new("operator-acceptance", "Operator acceptance", readModel.OperatorAcceptanceStatus, nameof(ProductLedgerOperatorAcceptanceLocalOnlyMatrix), 76),
        new("public-action-contract", "Public local-only action contract", readModel.PublicLocalOnlyActionContractStatus, nameof(ProductLedgerPublicUiActionSurface), 70),
        new("visual-evidence", "Visual evidence", readModel.VisualEvidenceStatus, nameof(ProductLedgerLocalDevVisualQaEvidence), 64),
        new("screenshot-evidence", "Screenshot evidence", readModel.ScreenshotEvidenceStatus, "ProductLedgerBrowserLocalOnlyScreenshotEvidence", 58)
    ];

    private static IReadOnlyList<ProductLedgerOperatorSurfaceEvidenceRef> EvidenceRefs() =>
    [
        new("authority-taxonomy", nameof(ProductLedgerLocalLedgerTaxonomy), "local-only canonical authority"),
        new("active-writer", nameof(ProductLedgerPathLocalOnlyActiveWriter), "bounded local-only writer authority"),
        new("route-preview", nameof(ProductLedgerLocalDevRoutePreview), "development-only route adapter"),
        new("operator-acceptance", nameof(ProductLedgerOperatorAcceptanceLocalOnlyMatrix), "fixture-safe acceptance matrix"),
        new("visual-qa", nameof(ProductLedgerLocalDevVisualQaEvidence), "static HTML visual evidence")
    ];

    private static IReadOnlyList<ProductLedgerOperatorSurfaceBlockedFrontier> BlockedFrontiers() =>
    [
        new("public-ui-action", "Public UI action", "No public operator action is exposed by this route."),
        new("product-command-execution", "Product command execution", "Route renders read-only state and never invokes product commands."),
        new("public-internet", "Public internet exposure", "Pilot maps this route only in Development mode."),
        new("external-network-provider-cloud", "External network/provider/cloud", "No provider, cloud, sync, telemetry or billing surface is enabled."),
        new("db-migration", "DB/migration", "No database or migration authority is introduced."),
        new("kms-worm-external-trust", "KMS/WORM/external trust", "No external custody, key or WORM claim is made."),
        new("browser-cdp-wcu-ocr-recipes-live", "Browser/CDP/WCU/OCR/Recipes live", "No live automation authority is exposed."),
        new("release-commercial", "Release/commercial", "No release, commercial or compliance custody readiness is claimed."),
        new("destructive-action", "Destructive action", "All destructive and unsafe write actions remain disabled."),
        new("unbounded-export", "Unbounded export/write", "Only bounded export status is visible; the route does not call an exporter.")
    ];
}
