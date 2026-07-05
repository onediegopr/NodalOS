namespace OneBrain.Core.Approval;

public enum ProductLedgerOperatorSurfaceReadModelMode
{
    FixtureSafeReadModel,
    ExistingLocalLedgerReadModel
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
    bool UsesFixtureReadModel);

public static class ProductLedgerOperatorSurfaceModelFactory
{
    public const string CanonicalSurfaceId = "product-ledger.local-dev.operator-surface.v1";
    public const string DeterministicGeneratedAtStrategy = "deterministic-local-dev-read-model";
    public const string Scope = "ProductLedgerLocalOnlyLineScoped";

    public static ProductLedgerOperatorSurfaceModel Build(
        ProductLedgerRenderableOperatorSurfaceResult renderable)
    {
        var authority = ProductLedgerLocalLedgerTaxonomy.ForComponent(nameof(ProductLedgerPathLocalOnlyActiveWriter));
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

        var statuses = Statuses(authority.BoundaryStatus);
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
            ReadModelMode: ProductLedgerOperatorSurfaceReadModelMode.FixtureSafeReadModel,
            LedgerAuthority: nameof(ProductLedgerPathLocalOnlyActiveWriter),
            LedgerAuthorityBoundaryStatus: authority.BoundaryStatus,
            LedgerVerificationStatus: "FIXTURE_SAFE_READ_MODEL_VERIFIED_NO_LEDGER_FILE_READ",
            CheckpointStatus: "FIXTURE_SAFE_CHECKPOINT_HEAD_VISIBLE",
            RedactionRetentionGuardStatus: "REDACTION_RETENTION_GUARDS_REQUIRED_AND_VISIBLE",
            ConcurrencyGuardStatus: "ACTIVE_WRITER_CONCURRENCY_LOCK_REQUIRED_AND_VISIBLE",
            BoundedExportStatus: "BOUNDED_LOCAL_EXPORT_STATUS_VISIBLE_NO_EXPORT_CALL",
            OperatorAcceptanceStatus: "OPERATOR_ACCEPTANCE_MATRIX_LINKABLE",
            PublicLocalOnlyActionContractStatus: "PUBLIC_LOCAL_ONLY_ACTION_CONTRACT_LINKABLE_ACTIONS_DISABLED",
            VisualEvidenceStatus: "LOCAL_DEV_VISUAL_QA_EVIDENCE_LINKABLE_STATIC_HTML_ONLY",
            ScreenshotEvidenceStatus: "SCREENSHOT_EVIDENCE_FIXTURE_LINKABLE_NO_LIVE_BROWSER",
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
            UsesFixtureReadModel: true);
    }

    private static IReadOnlyList<ProductLedgerOperatorSurfaceStatus> Statuses(string authorityBoundaryStatus) =>
    [
        new("ledger-authority", "Ledger authority", authorityBoundaryStatus, nameof(ProductLedgerLocalLedgerTaxonomy), 96),
        new("ledger-verification", "Ledger verification", "FIXTURE_SAFE_READ_MODEL_VERIFIED_NO_LEDGER_FILE_READ", nameof(ProductLedgerPathLocalOnlyActiveWriter), 72),
        new("checkpoint", "Checkpoint/head", "FIXTURE_SAFE_CHECKPOINT_HEAD_VISIBLE", nameof(ProductLedgerPathLocalOnlyActiveWriter), 72),
        new("redaction-retention", "Redaction/retention", "REDACTION_RETENTION_GUARDS_REQUIRED_AND_VISIBLE", "redaction-retention-behavioral-gates", 84),
        new("concurrency", "Concurrency", "ACTIVE_WRITER_CONCURRENCY_LOCK_REQUIRED_AND_VISIBLE", "writer-concurrency-lock", 88),
        new("bounded-export", "Bounded export", "BOUNDED_LOCAL_EXPORT_STATUS_VISIBLE_NO_EXPORT_CALL", nameof(ProductLedgerLocalReportExportService), 70),
        new("operator-acceptance", "Operator acceptance", "OPERATOR_ACCEPTANCE_MATRIX_LINKABLE", nameof(ProductLedgerOperatorAcceptanceLocalOnlyMatrix), 76),
        new("public-action-contract", "Public local-only action contract", "PUBLIC_LOCAL_ONLY_ACTION_CONTRACT_LINKABLE_ACTIONS_DISABLED", nameof(ProductLedgerPublicUiActionSurface), 70),
        new("visual-evidence", "Visual evidence", "LOCAL_DEV_VISUAL_QA_EVIDENCE_LINKABLE_STATIC_HTML_ONLY", nameof(ProductLedgerLocalDevVisualQaEvidence), 64),
        new("screenshot-evidence", "Screenshot evidence", "SCREENSHOT_EVIDENCE_FIXTURE_LINKABLE_NO_LIVE_BROWSER", "ProductLedgerBrowserLocalOnlyScreenshotEvidence", 58)
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
