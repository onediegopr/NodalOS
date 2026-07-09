namespace OneBrain.Core.Approval;

public enum ProductLedgerOperatorSurfaceReadModelMode
{
    FixtureSafeReadModel,
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
    ProductLedgerLocalApprovalExecutionResult ApprovalExecutionCandidatePreview,
    ProductLedgerLocalApprovalDecisionSnapshot ApprovalDecisionState,
    ProductLedgerLocalApprovedActionExecutionSnapshot ApprovedActionExecutionState,
    ProductLedgerLocalBoundedApprovedActionSnapshot BoundedApprovedActionState,
    ProductLedgerLocalApprovedHandoffReportDraftSnapshot HandoffReportDraftState,
    ProductLedgerLocalWorkspaceTestJailHandoffDraftSnapshot WorkspaceTestJailHandoffDraftState,
    ProductLedgerLocalUserWorkspaceAllowlistedHandoffDraftSnapshot UserWorkspaceAllowlistedHandoffDraftState,
    ProductLedgerLocalOperatorSurfaceLatestStateSnapshotResult LatestStateSnapshotState,
    ProductLedgerLocalOperatorSurfaceLatestStateManifestResult LatestStateManifestState,
    ProductLedgerLocalDurableLatestStateReaderCandidateResult DurableLatestStateReaderCandidateState,
    ProductLedgerLocalDurableLatestStateAuxiliaryEvidenceResult DurableLatestStateAuxiliaryEvidenceState,
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

internal static class ProductLedgerLocalDevSnapshotCollections
{
    public static IReadOnlyList<T> Seal<T>(params T[] items) =>
        Array.AsReadOnly(items);
}

internal static class ProductLedgerLocalDevActionProjection
{
    private const string CanonicalReadOnlyDisabledReason =
        "Canonical operator route is read-only and does not execute product commands.";

    public static IReadOnlyList<ProductLedgerOperatorSurfaceActionPreview> ToCanonicalPreviews(
        IReadOnlyList<ProductLedgerRenderableOperatorSurfaceActionModel> renderableActions) =>
        ProductLedgerLocalDevSnapshotCollections.Seal(renderableActions
            .Select(ToCanonicalPreview)
            .OrderBy(action => action.ActionId, StringComparer.Ordinal)
            .ToArray());

    private static ProductLedgerOperatorSurfaceActionPreview ToCanonicalPreview(
        ProductLedgerRenderableOperatorSurfaceActionModel action) =>
        new(
            ActionId: action.ActionId,
            Label: action.Label,
            Disabled: true,
            ReadOnly: true,
            LocalOnly: action.LocalOnly,
            NonDestructive: action.NonDestructive,
            DisabledReason: action.Disabled ? action.DisabledReason : CanonicalReadOnlyDisabledReason);
}

public static class ProductLedgerOperatorSurfaceModelFactory
{
    public const string CanonicalSurfaceId = "product-ledger.local-dev.operator-surface.v1";
    public const string DeterministicGeneratedAtStrategy = "deterministic-local-dev-read-model";
    public const string Scope = "ProductLedgerLocalOnlyLineScoped";

    public static ProductLedgerOperatorSurfaceModel Build(
        ProductLedgerRenderableOperatorSurfaceResult renderable,
        ProductLedgerOperatorSurfaceReadModelSource? readModelSource = null,
        ProductLedgerLocalApprovalDecisionSnapshot? approvalDecisionState = null,
        ProductLedgerLocalApprovedActionExecutionSnapshot? approvedActionExecutionState = null,
        ProductLedgerLocalBoundedApprovedActionSnapshot? boundedApprovedActionState = null,
        ProductLedgerLocalApprovedHandoffReportDraftSnapshot? handoffReportDraftState = null,
        ProductLedgerLocalWorkspaceTestJailHandoffDraftSnapshot? workspaceTestJailHandoffDraftState = null,
        ProductLedgerLocalUserWorkspaceAllowlistedHandoffDraftSnapshot? userWorkspaceAllowlistedHandoffDraftState = null,
        ProductLedgerLocalOperatorSurfaceLatestStateSnapshotResult? latestStateSnapshotState = null,
        ProductLedgerLocalOperatorSurfaceLatestStateManifestResult? latestStateManifestState = null,
        ProductLedgerLocalDurableLatestStateReaderCandidateResult? durableLatestStateReaderCandidateState = null,
        ProductLedgerLocalDurableLatestStateAuxiliaryEvidenceResult? durableLatestStateAuxiliaryEvidenceState = null)
    {
        var readModel = new ProductLedgerOperatorSurfaceReadModelProvider().Read(readModelSource);
        var actions = ProductLedgerLocalDevActionProjection.ToCanonicalPreviews(renderable.Model.Actions);

        var statuses = Statuses(readModel);
        var evidenceRefs = EvidenceRefs();
        var approvalPreviewLoop = ProductLedgerLocalApprovalPreviewLoopFactory.Build(
            CanonicalSurfaceId,
            ProductLedgerLocalDevRoutePreview.RouteTemplatePreview,
            evidenceRefs);
        var approvalExecutionCandidatePreview = new ProductLedgerLocalApprovalExecutionCandidate().Execute(
            CreateApprovalExecutionCandidatePreviewRequest());
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
            ApprovalExecutionCandidatePreview: approvalExecutionCandidatePreview,
            ApprovalDecisionState: approvalDecisionState ?? ProductLedgerLocalApprovalDecisionSnapshot.PendingPreviewOnly,
            ApprovedActionExecutionState: approvedActionExecutionState ?? ProductLedgerLocalApprovedActionExecutionSnapshot.Pending,
            BoundedApprovedActionState: boundedApprovedActionState ?? ProductLedgerLocalBoundedApprovedActionSnapshot.Pending,
            HandoffReportDraftState: handoffReportDraftState ?? ProductLedgerLocalApprovedHandoffReportDraftSnapshot.Pending,
            WorkspaceTestJailHandoffDraftState: workspaceTestJailHandoffDraftState ?? ProductLedgerLocalWorkspaceTestJailHandoffDraftSnapshot.Pending,
            UserWorkspaceAllowlistedHandoffDraftState: userWorkspaceAllowlistedHandoffDraftState ?? ProductLedgerLocalUserWorkspaceAllowlistedHandoffDraftSnapshot.Pending,
            LatestStateSnapshotState: latestStateSnapshotState ?? ProductLedgerLocalOperatorSurfaceLatestStateSnapshotResult.Pending,
            LatestStateManifestState: latestStateManifestState ?? ProductLedgerLocalOperatorSurfaceLatestStateManifestResult.Pending,
            DurableLatestStateReaderCandidateState: durableLatestStateReaderCandidateState ?? ProductLedgerLocalDurableLatestStateReaderCandidateResult.Pending,
            DurableLatestStateAuxiliaryEvidenceState: durableLatestStateAuxiliaryEvidenceState ?? ProductLedgerLocalDurableLatestStateAuxiliaryEvidenceResult.Pending,
            SafeNextSteps: ProductLedgerLocalDevSnapshotCollections.Seal(
                "LOCAL_DEV_PRODUCT_SURFACE_ROUTE_RESPONSE_VERIFICATION",
                "LOCAL_DEV_OPERATOR_PREVIEW_DISABLED_ACTION_REVIEW",
                "LOCAL_DEV_READINESS_BLOCKER_COPY_REVIEW",
                "PRODUCT_LEDGER_LOCAL_DEV_PRODUCT_SURFACE_ADVANCEMENT_FOLLOW_UP_OR_CLOSE"
            ),
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
        ProductLedgerLocalDevSnapshotCollections.Seal<ProductLedgerOperatorSurfaceStatus>(
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
        new("local-dev-product-surface-prep", "Local/dev product surface prep", "VISIBLE_ROUTE_RESPONSE_WITH_BLOCKED_ACTIONS", nameof(ProductLedgerLocalDevRoutePreview), 78),
        new("visual-evidence", "Visual evidence", readModel.VisualEvidenceStatus, nameof(ProductLedgerLocalDevVisualQaEvidence), 64),
        new("screenshot-evidence", "Screenshot evidence", readModel.ScreenshotEvidenceStatus, "ProductLedgerBrowserLocalOnlyScreenshotEvidence", 58)
    );

    private static IReadOnlyList<ProductLedgerOperatorSurfaceEvidenceRef> EvidenceRefs() =>
        ProductLedgerLocalDevSnapshotCollections.Seal<ProductLedgerOperatorSurfaceEvidenceRef>(
        new("authority-taxonomy", nameof(ProductLedgerLocalLedgerTaxonomy), "local-only canonical authority"),
        new("active-writer", nameof(ProductLedgerPathLocalOnlyActiveWriter), "bounded local-only writer authority"),
        new("route-preview", nameof(ProductLedgerLocalDevRoutePreview), "development-only route adapter"),
        new("operator-acceptance", nameof(ProductLedgerOperatorAcceptanceLocalOnlyMatrix), "fixture-safe acceptance matrix"),
        new("visual-qa", nameof(ProductLedgerLocalDevVisualQaEvidence), "static HTML visual evidence")
    );

    private static ProductLedgerLocalApprovalExecutionRequest CreateApprovalExecutionCandidatePreviewRequest()
    {
        var now = new DateTimeOffset(2026, 7, 5, 12, 0, 0, TimeSpan.Zero);
        return new ProductLedgerLocalApprovalExecutionRequest(
            ExplicitLocalOnlyInternalApprovalExecutionScope: true,
            ApprovalId: "approval-route-preview.product-ledger.view-ledger-readiness",
            ApprovedAtUtc: now.AddMinutes(-1),
            NowUtc: now,
            MaxApprovalAge: TimeSpan.FromMinutes(5),
            CandidateActionKind: ProductLedgerInternalCommandKind.ViewLedgerReadiness,
            ApprovedActionName: ProductLedgerInternalCommandKind.ViewLedgerReadiness.ToString(),
            ApprovedEvidenceHash: "route-preview-evidence-hash",
            CurrentEvidenceHash: "route-preview-evidence-hash",
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
    }

    private static IReadOnlyList<ProductLedgerOperatorSurfaceBlockedFrontier> BlockedFrontiers() =>
        ProductLedgerLocalDevSnapshotCollections.Seal<ProductLedgerOperatorSurfaceBlockedFrontier>(
        new("public-ui-action", "Public UI action", "No public operator action is exposed by this route."),
        new("product-command-execution", "Product command execution", "Route renders read-only state and never invokes product commands."),
        new("public-internet", "Public internet exposure", "Pilot maps this route only in Development mode."),
        new("external-network-provider-cloud", "External network/provider/cloud", "No provider, cloud, sync, telemetry or billing surface is enabled."),
        new("db-migration", "DB/migration", "No database or migration authority is introduced."),
        new("kms-worm-external-trust", "KMS/WORM/external trust", "No external custody, key or WORM claim is made."),
        new("browser-cdp-wcu-ocr-recipes-live", "Browser/CDP/WCU/OCR/Recipes live", "No live automation authority is exposed."),
        new("release-commercial", "Release/commercial", "No release, commercial or compliance custody readiness is claimed."),
        new("latest-pointer-read-precedence", "Latest pointer/read precedence", "Local/dev latest-state evidence remains auxiliary and does not become product authority."),
        new("destructive-action", "Destructive action", "All destructive and unsafe write actions remain disabled."),
        new("unbounded-export", "Unbounded export/write", "Only bounded export status is visible; the route does not call an exporter.")
    );

}
