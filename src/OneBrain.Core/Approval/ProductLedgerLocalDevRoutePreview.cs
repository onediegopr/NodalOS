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
        => Render(request, ProductLedgerOperatorSurfaceReadModelSource.FixtureSafe);

    public ProductLedgerLocalDevRoutePreviewResult Render(
        ProductLedgerLocalDevRoutePreviewRequest? request,
        ProductLedgerOperatorSurfaceReadModelSource? readModelSource)
        => Render(request, readModelSource, null);

    public ProductLedgerLocalDevRoutePreviewResult Render(
        ProductLedgerLocalDevRoutePreviewRequest? request,
        ProductLedgerOperatorSurfaceReadModelSource? readModelSource,
        ProductLedgerLocalApprovalDecisionSnapshot? approvalDecisionState)
        => Render(request, readModelSource, approvalDecisionState, null);

    public ProductLedgerLocalDevRoutePreviewResult Render(
        ProductLedgerLocalDevRoutePreviewRequest? request,
        ProductLedgerOperatorSurfaceReadModelSource? readModelSource,
        ProductLedgerLocalApprovalDecisionSnapshot? approvalDecisionState,
        ProductLedgerLocalApprovedActionExecutionSnapshot? approvedActionExecutionState)
        => Render(request, readModelSource, approvalDecisionState, approvedActionExecutionState, null);

    public ProductLedgerLocalDevRoutePreviewResult Render(
        ProductLedgerLocalDevRoutePreviewRequest? request,
        ProductLedgerOperatorSurfaceReadModelSource? readModelSource,
        ProductLedgerLocalApprovalDecisionSnapshot? approvalDecisionState,
        ProductLedgerLocalApprovedActionExecutionSnapshot? approvedActionExecutionState,
        ProductLedgerLocalBoundedApprovedActionSnapshot? boundedApprovedActionState)
        => Render(request, readModelSource, approvalDecisionState, approvedActionExecutionState, boundedApprovedActionState, null);

    public ProductLedgerLocalDevRoutePreviewResult Render(
        ProductLedgerLocalDevRoutePreviewRequest? request,
        ProductLedgerOperatorSurfaceReadModelSource? readModelSource,
        ProductLedgerLocalApprovalDecisionSnapshot? approvalDecisionState,
        ProductLedgerLocalApprovedActionExecutionSnapshot? approvedActionExecutionState,
        ProductLedgerLocalBoundedApprovedActionSnapshot? boundedApprovedActionState,
        ProductLedgerLocalApprovedHandoffReportDraftSnapshot? handoffReportDraftState)
        => Render(request, readModelSource, approvalDecisionState, approvedActionExecutionState, boundedApprovedActionState, handoffReportDraftState, null);

    public ProductLedgerLocalDevRoutePreviewResult Render(
        ProductLedgerLocalDevRoutePreviewRequest? request,
        ProductLedgerOperatorSurfaceReadModelSource? readModelSource,
        ProductLedgerLocalApprovalDecisionSnapshot? approvalDecisionState,
        ProductLedgerLocalApprovedActionExecutionSnapshot? approvedActionExecutionState,
        ProductLedgerLocalBoundedApprovedActionSnapshot? boundedApprovedActionState,
        ProductLedgerLocalApprovedHandoffReportDraftSnapshot? handoffReportDraftState,
        ProductLedgerLocalWorkspaceTestJailHandoffDraftSnapshot? workspaceTestJailHandoffDraftState,
        ProductLedgerLocalUserWorkspaceAllowlistedHandoffDraftSnapshot? userWorkspaceAllowlistedHandoffDraftState = null)
        => Render(request, readModelSource, approvalDecisionState, approvedActionExecutionState, boundedApprovedActionState, handoffReportDraftState, workspaceTestJailHandoffDraftState, userWorkspaceAllowlistedHandoffDraftState, null);

    public ProductLedgerLocalDevRoutePreviewResult Render(
        ProductLedgerLocalDevRoutePreviewRequest? request,
        ProductLedgerOperatorSurfaceReadModelSource? readModelSource,
        ProductLedgerLocalApprovalDecisionSnapshot? approvalDecisionState,
        ProductLedgerLocalApprovedActionExecutionSnapshot? approvedActionExecutionState,
        ProductLedgerLocalBoundedApprovedActionSnapshot? boundedApprovedActionState,
        ProductLedgerLocalApprovedHandoffReportDraftSnapshot? handoffReportDraftState,
        ProductLedgerLocalWorkspaceTestJailHandoffDraftSnapshot? workspaceTestJailHandoffDraftState,
        ProductLedgerLocalUserWorkspaceAllowlistedHandoffDraftSnapshot? userWorkspaceAllowlistedHandoffDraftState,
        ProductLedgerLocalOperatorSurfaceLatestStateSnapshotResult? latestStateSnapshotState)
    {
        var blockers = new List<ProductLedgerLocalDevRoutePreviewBlocker>();
        if (request is null)
        {
            blockers.Add(ProductLedgerLocalDevRoutePreviewBlocker.MissingRequest);
            return Result(blockers, null, readModelSource, approvalDecisionState, approvedActionExecutionState, boundedApprovedActionState, handoffReportDraftState, workspaceTestJailHandoffDraftState, userWorkspaceAllowlistedHandoffDraftState, latestStateSnapshotState);
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

        return Result(blockers, renderable, readModelSource, approvalDecisionState, approvedActionExecutionState, boundedApprovedActionState, handoffReportDraftState, workspaceTestJailHandoffDraftState, userWorkspaceAllowlistedHandoffDraftState, latestStateSnapshotState);
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
        ProductLedgerRenderableOperatorSurfaceResult? renderable,
        ProductLedgerOperatorSurfaceReadModelSource? readModelSource,
        ProductLedgerLocalApprovalDecisionSnapshot? approvalDecisionState,
        ProductLedgerLocalApprovedActionExecutionSnapshot? approvedActionExecutionState,
        ProductLedgerLocalBoundedApprovedActionSnapshot? boundedApprovedActionState,
        ProductLedgerLocalApprovedHandoffReportDraftSnapshot? handoffReportDraftState,
        ProductLedgerLocalWorkspaceTestJailHandoffDraftSnapshot? workspaceTestJailHandoffDraftState,
        ProductLedgerLocalUserWorkspaceAllowlistedHandoffDraftSnapshot? userWorkspaceAllowlistedHandoffDraftState,
        ProductLedgerLocalOperatorSurfaceLatestStateSnapshotResult? latestStateSnapshotState)
    {
        var distinct = blockers.Distinct().OrderBy(blocker => blocker.ToString(), StringComparer.Ordinal).ToArray();
        var rendered = distinct.Length == 0 && renderable is not null;
        var safeRenderable = renderable ?? new ProductLedgerRenderableOperatorSurfaceRenderer().Render(null);
        var canonicalSurface = ProductLedgerOperatorSurfaceModelFactory.Build(
            safeRenderable,
            readModelSource,
            approvalDecisionState,
            approvedActionExecutionState,
            boundedApprovedActionState,
            handoffReportDraftState,
            workspaceTestJailHandoffDraftState,
            userWorkspaceAllowlistedHandoffDraftState,
            latestStateSnapshotState);
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
        html.AppendLine($"    <div data-testid=\"product-ledger-surface-root\">{Encode(model.SurfaceId)} / {Encode(model.Scope)} / local-only development-only read-only</div>");
        html.AppendLine($"    <p data-testid=\"surface-ledger-authority\">{Encode(model.LedgerAuthority)} / {Encode(model.LedgerAuthorityBoundaryStatus)}</p>");
        html.AppendLine($"    <p data-testid=\"product-ledger-authority\">{Encode(model.LedgerAuthority)} / {Encode(model.LedgerAuthorityBoundaryStatus)}</p>");
        html.AppendLine($"    <p data-testid=\"product-ledger-read-model-source\">{Encode(model.ReadModelMode.ToString())} / {Encode(model.LedgerPathClassification)}</p>");
        html.AppendLine($"    <p data-testid=\"surface-ledger-verification\">{Encode(model.LedgerVerificationStatus)}</p>");
        html.AppendLine($"    <p data-testid=\"product-ledger-verification-status\">{Encode(model.LedgerVerificationStatus)}</p>");
        html.AppendLine($"    <p data-testid=\"surface-checkpoint\">{Encode(model.CheckpointStatus)}</p>");
        html.AppendLine($"    <p data-testid=\"product-ledger-checkpoint-status\">{Encode(model.CheckpointStatus)}</p>");
        html.AppendLine($"    <p data-testid=\"product-ledger-entry-count\">entry_count={model.LedgerEntryCount}</p>");
        html.AppendLine($"    <p data-testid=\"product-ledger-head-sequence\">head_sequence={Encode(model.LedgerHeadSequence)}</p>");
        html.AppendLine($"    <p data-testid=\"product-ledger-head-hash-prefix\">head_hash_prefix={Encode(model.LedgerHeadHashPrefix)}</p>");
        html.AppendLine($"    <p data-testid=\"product-ledger-ledger-hash-prefix\">ledger_hash_prefix={Encode(model.LedgerHashPrefix)}</p>");
        html.AppendLine($"    <p data-testid=\"surface-redaction-retention\">{Encode(model.RedactionRetentionGuardStatus)}</p>");
        html.AppendLine($"    <p data-testid=\"product-ledger-redaction-retention-status\">{Encode(model.RedactionRetentionGuardStatus)}</p>");
        html.AppendLine($"    <p data-testid=\"surface-concurrency\">{Encode(model.ConcurrencyGuardStatus)}</p>");
        html.AppendLine($"    <p data-testid=\"product-ledger-concurrency-status\">{Encode(model.ConcurrencyGuardStatus)}</p>");
        html.AppendLine($"    <p data-testid=\"surface-bounded-export\">{Encode(model.BoundedExportStatus)}</p>");
        html.AppendLine($"    <p data-testid=\"product-ledger-bounded-export-status\">{Encode(model.BoundedExportStatus)}</p>");
        html.AppendLine($"    <p data-testid=\"surface-operator-acceptance\">{Encode(model.OperatorAcceptanceStatus)}</p>");
        html.AppendLine($"    <p data-testid=\"product-ledger-operator-acceptance-status\">{Encode(model.OperatorAcceptanceStatus)}</p>");
        html.AppendLine($"    <p data-testid=\"surface-public-action-contract\">{Encode(model.PublicLocalOnlyActionContractStatus)}</p>");
        html.AppendLine($"    <p data-testid=\"product-ledger-public-local-only-action-contract-status\">{Encode(model.PublicLocalOnlyActionContractStatus)}</p>");
        html.AppendLine($"    <p data-testid=\"surface-visual-evidence\">{Encode(model.VisualEvidenceStatus)}</p>");
        html.AppendLine($"    <p data-testid=\"product-ledger-visual-evidence-status\">{Encode(model.VisualEvidenceStatus)}</p>");
        html.AppendLine($"    <p data-testid=\"surface-screenshot-evidence\">{Encode(model.ScreenshotEvidenceStatus)}</p>");
        html.AppendLine($"    <p data-testid=\"product-ledger-screenshot-evidence-status\">{Encode(model.ScreenshotEvidenceStatus)}</p>");
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
        html.AppendLine($"    <p data-testid=\"product-ledger-blocked-frontiers\">{model.BlockedFrontiers.Count} blocked frontiers visible</p>");
        html.AppendLine("    <div data-testid=\"surface-blocked-frontiers\">");
        foreach (var frontier in model.BlockedFrontiers.OrderBy(frontier => frontier.FrontierId, StringComparer.Ordinal))
        {
            html.AppendLine($"      <p data-testid=\"surface-blocked-{Encode(frontier.FrontierId)}\">{Encode(frontier.Label)}: {Encode(frontier.Reason)}</p>");
        }

        html.AppendLine("    </div>");
        html.AppendLine("    <div data-testid=\"product-ledger-action-previews\">");
        foreach (var action in model.ActionPreviews.OrderBy(action => action.ActionId, StringComparer.Ordinal))
        {
            html.AppendLine($"      <button type=\"button\" data-testid=\"product-ledger-action-preview-{Encode(action.ActionId)}\" data-action-id=\"{Encode(action.ActionId)}\" data-read-only=\"{Lower(action.ReadOnly)}\" data-local-only=\"{Lower(action.LocalOnly)}\" data-non-destructive=\"{Lower(action.NonDestructive)}\" data-executable=\"false\" data-handler-id=\"\" data-callback=\"\" disabled aria-disabled=\"true\">{Encode(action.Label)}</button>");
            html.AppendLine($"      <p data-testid=\"product-ledger-action-preview-{Encode(action.ActionId)}-blocked-reason\">{Encode(action.DisabledReason)}</p>");
        }

        html.AppendLine("    </div>");
        html.AppendLine(ToApprovalPreviewLoopHtml(model.ApprovalPreviewLoop));
        html.AppendLine(ToApprovalExecutionCandidatePreviewHtml(model.ApprovalExecutionCandidatePreview));
        html.AppendLine(ToApprovalDecisionStateHtml(model.ApprovalDecisionState));
        html.AppendLine(ToApprovedActionExecutionStateHtml(model.ApprovedActionExecutionState));
        html.AppendLine(ToBoundedApprovedActionStateHtml(model.BoundedApprovedActionState));
        html.AppendLine(ToApprovedHandoffReportDraftStateHtml(model.HandoffReportDraftState));
        html.AppendLine(ToWorkspaceTestJailHandoffDraftStateHtml(model.WorkspaceTestJailHandoffDraftState));
        html.AppendLine(ToUserWorkspaceAllowlistedHandoffDraftStateHtml(model.UserWorkspaceAllowlistedHandoffDraftState));
        html.AppendLine(ToLatestStateSnapshotStateHtml(model.LatestStateSnapshotState));
        html.AppendLine($"    <p data-testid=\"product-ledger-safe-next-steps\">{Encode(string.Join("; ", model.SafeNextSteps))}</p>");
        html.AppendLine("    <div data-testid=\"surface-safe-next-steps\">");
        foreach (var step in model.SafeNextSteps)
        {
            html.AppendLine($"      <p>{Encode(step)}</p>");
        }

        html.AppendLine("    </div>");
        html.AppendLine("  </section>");
        return html.ToString();
    }

    private static string ToApprovedActionExecutionStateHtml(ProductLedgerLocalApprovedActionExecutionSnapshot state)
    {
        var html = new StringBuilder();
        html.AppendLine($"    <section data-testid=\"product-ledger-approved-action-execution-state\" data-state=\"{Encode(state.State.ToString())}\" data-decision=\"{Encode(state.Decision.ToString())}\" data-local-only=\"{Lower(state.LocalOnly)}\" data-internal-only=\"{Lower(state.InternalOnly)}\" data-development-only=\"{Lower(state.DevelopmentOnly)}\" data-default-off=\"{Lower(state.DefaultOff)}\" data-fail-closed=\"{Lower(state.FailClosed)}\" data-no-op-only=\"{Lower(state.NoOpOnly)}\" data-bounded-action-executed=\"{Lower(state.BoundedActionExecuted)}\" data-product-command-executed=\"{Lower(state.ProductCommandExecuted)}\" data-public-ui-action=\"{Lower(state.PublicUiActionAvailable)}\" data-product-command-handler=\"{Lower(state.ProductCommandHandlerAvailable)}\" data-productive-service-registration=\"{Lower(state.ProductiveServiceRegistrationAvailable)}\" data-physical-export-created=\"{Lower(state.PhysicalExportCreated)}\" data-file-write-outside-execution-store=\"{Lower(state.FileWriteOutsideExecutionStorePerformed)}\" data-provider-cloud-network=\"{Lower(state.ProviderCloudNetworkAvailable)}\" data-db-migration=\"{Lower(state.DbMigrationAvailable)}\" data-kms-worm-external-trust=\"{Lower(state.KmsWormExternalTrustAvailable)}\" data-live-automation=\"{Lower(state.BrowserCdpWcuOcrRecipesLiveAvailable)}\" data-pilot-run=\"{Lower(state.PilotRunAvailable)}\" data-release-commercial=\"{Lower(state.ReleaseCommercialReady)}\">");
        html.AppendLine("      <h2>Approved action execution state</h2>");
        html.AppendLine($"      <p data-testid=\"product-ledger-approved-action-execution-status\">{Encode(state.StatusText)}</p>");
        html.AppendLine($"      <p data-testid=\"product-ledger-approved-action-execution-result-kind\">{Encode(state.State.ToString())}</p>");
        html.AppendLine($"      <p data-testid=\"product-ledger-approved-action-execution-ids\">execution={Encode(state.ExecutionId)} / approval={Encode(state.ApprovalId)} / candidate={Encode(state.CandidateActionKind)}</p>");
        html.AppendLine($"      <p data-testid=\"product-ledger-approved-action-execution-hashes\">candidate_hash={Encode(state.CandidateEvidenceHashPrefix)} / result_hash={Encode(state.ExecutionResultHashPrefix)}</p>");
        html.AppendLine("      <p data-testid=\"product-ledger-approved-action-execution-protection\">local/internal/development-only no-op execution only no shell no subprocess no Pilot run no product command execution no public UI no write/export no release/commercial no compliance custody</p>");
        html.AppendLine("      <div data-testid=\"product-ledger-approved-action-execution-evidence-refs\">");
        foreach (var evidence in state.EvidenceReferences.OrderBy(evidence => evidence, StringComparer.Ordinal))
        {
            html.AppendLine($"        <p>{Encode(evidence)}</p>");
        }

        html.AppendLine("      </div>");
        html.AppendLine("      <div data-testid=\"product-ledger-approved-action-execution-blockers\">");
        if (state.Blockers.Count == 0)
        {
            html.AppendLine("        <p>none</p>");
        }
        else
        {
            foreach (var blocker in state.Blockers.OrderBy(blocker => blocker.ToString(), StringComparer.Ordinal))
            {
                html.AppendLine($"        <p>{Encode(blocker.ToString())}</p>");
            }
        }

        html.AppendLine("      </div>");
        html.AppendLine("    </section>");
        return html.ToString();
    }

    private static string ToBoundedApprovedActionStateHtml(ProductLedgerLocalBoundedApprovedActionSnapshot state)
    {
        var html = new StringBuilder();
        html.AppendLine($"    <section data-testid=\"product-ledger-bounded-approved-action-state\" data-state=\"{Encode(state.State.ToString())}\" data-decision=\"{Encode(state.Decision.ToString())}\" data-local-only=\"{Lower(state.LocalOnly)}\" data-internal-only=\"{Lower(state.InternalOnly)}\" data-development-only=\"{Lower(state.DevelopmentOnly)}\" data-default-off=\"{Lower(state.DefaultOff)}\" data-fail-closed=\"{Lower(state.FailClosed)}\" data-non-destructive=\"{Lower(state.NonDestructive)}\" data-completion-marker=\"{Lower(state.BoundedInternalCompletionMarker)}\" data-touches-user-files=\"{Lower(state.TouchesUserFiles)}\" data-shell-subprocess-allowed=\"{Lower(state.ShellOrSubprocessAllowed)}\" data-command-execution-allowed=\"{Lower(state.ArbitraryCommandExecutionAllowed)}\" data-product-command-executed=\"{Lower(state.ProductCommandExecuted)}\" data-public-ui-action=\"{Lower(state.PublicUiActionAvailable)}\" data-product-command-handler=\"{Lower(state.ProductCommandHandlerAvailable)}\" data-productive-service-registration=\"{Lower(state.ProductiveServiceRegistrationAvailable)}\" data-physical-export-created=\"{Lower(state.PhysicalExportCreated)}\" data-file-write-outside-execution-store=\"{Lower(state.FileWriteOutsideExecutionStorePerformed)}\" data-provider-cloud-network=\"{Lower(state.ProviderCloudNetworkAvailable)}\" data-db-migration=\"{Lower(state.DbMigrationAvailable)}\" data-kms-worm-external-trust=\"{Lower(state.KmsWormExternalTrustAvailable)}\" data-live-automation=\"{Lower(state.BrowserCdpWcuOcrRecipesLiveAvailable)}\" data-pilot-run=\"{Lower(state.PilotRunAvailable)}\" data-release-commercial=\"{Lower(state.ReleaseCommercialReady)}\">");
        html.AppendLine("      <h2>Bounded approved action state</h2>");
        html.AppendLine($"      <p data-testid=\"product-ledger-bounded-approved-action-status\">{Encode(state.StatusText)}</p>");
        html.AppendLine($"      <p data-testid=\"product-ledger-bounded-approved-action-result-kind\">{Encode(state.State.ToString())}</p>");
        html.AppendLine($"      <p data-testid=\"product-ledger-bounded-approved-action-ids\">execution={Encode(state.ExecutionId)} / action={Encode(state.ActionId)} / approval={Encode(state.ApprovalId)} / noop={Encode(state.NoOpExecutionId)} / candidate={Encode(state.CandidateActionKind)}</p>");
        html.AppendLine($"      <p data-testid=\"product-ledger-bounded-approved-action-kind\">{Encode(state.ActionKind)}</p>");
        html.AppendLine($"      <p data-testid=\"product-ledger-bounded-approved-action-hashes\">candidate_hash={Encode(state.CandidateEvidenceHashPrefix)} / result_hash={Encode(state.ResultHashPrefix)}</p>");
        html.AppendLine("      <p data-testid=\"product-ledger-bounded-approved-action-protection\">local/internal/development-only bounded internal completion marker non-destructive no user file write no shell no subprocess no command execution no Pilot run no product command execution no public UI no export no network no DB no WORM/KMS no release/commercial no compliance custody no business signoff</p>");
        html.AppendLine("      <div data-testid=\"product-ledger-bounded-approved-action-evidence-refs\">");
        foreach (var evidence in state.EvidenceReferences.OrderBy(evidence => evidence, StringComparer.Ordinal))
        {
            html.AppendLine($"        <p>{Encode(evidence)}</p>");
        }

        html.AppendLine("      </div>");
        html.AppendLine("      <div data-testid=\"product-ledger-bounded-approved-action-blockers\">");
        if (state.Blockers.Count == 0)
        {
            html.AppendLine("        <p>none</p>");
        }
        else
        {
            foreach (var blocker in state.Blockers.OrderBy(blocker => blocker.ToString(), StringComparer.Ordinal))
            {
                html.AppendLine($"        <p>{Encode(blocker.ToString())}</p>");
            }
        }

        html.AppendLine("      </div>");
        html.AppendLine("    </section>");
        return html.ToString();
    }

    private static string ToApprovedHandoffReportDraftStateHtml(ProductLedgerLocalApprovedHandoffReportDraftSnapshot state)
    {
        var html = new StringBuilder();
        html.AppendLine($"    <section data-testid=\"product-ledger-approved-handoff-report-draft-state\" data-state=\"{Encode(state.State.ToString())}\" data-decision=\"{Encode(state.Decision.ToString())}\" data-local-only=\"{Lower(state.LocalOnly)}\" data-internal-only=\"{Lower(state.InternalOnly)}\" data-development-only=\"{Lower(state.DevelopmentOnly)}\" data-create-only=\"{Lower(state.CreateOnly)}\" data-overwrite-allowed=\"{Lower(state.OverwriteAllowed)}\" data-user-file-write=\"{Lower(state.UserFileWrite)}\" data-shell-allowed=\"{Lower(state.ShellAllowed)}\" data-network-allowed=\"{Lower(state.NetworkAllowed)}\" data-production-allowed=\"{Lower(state.ProductionAllowed)}\" data-public-product-allowed=\"{Lower(state.PublicProductAllowed)}\" data-redaction-applied=\"{Lower(state.RedactionApplied)}\" data-product-command-executed=\"{Lower(state.ProductCommandExecuted)}\" data-public-ui-action=\"{Lower(state.PublicUiActionAvailable)}\" data-product-command-handler=\"{Lower(state.ProductCommandHandlerAvailable)}\" data-provider-cloud-network=\"{Lower(state.ProviderCloudNetworkAvailable)}\" data-db-migration=\"{Lower(state.DbMigrationAvailable)}\" data-kms-worm-external-trust=\"{Lower(state.KmsWormExternalTrustAvailable)}\" data-live-automation=\"{Lower(state.BrowserCdpWcuOcrRecipesLiveAvailable)}\" data-pilot-run=\"{Lower(state.PilotRunAvailable)}\" data-release-commercial=\"{Lower(state.ReleaseCommercialReady)}\">");
        html.AppendLine("      <h2>Approved handoff report draft state</h2>");
        html.AppendLine($"      <p data-testid=\"product-ledger-approved-handoff-report-draft-status\">{Encode(state.StatusText)}</p>");
        html.AppendLine($"      <p data-testid=\"product-ledger-approved-handoff-report-draft-result-kind\">{Encode(state.State.ToString())}</p>");
        html.AppendLine($"      <p data-testid=\"product-ledger-approved-handoff-report-draft-ids\">action={Encode(state.ActionId)} / candidate={Encode(state.CandidateId)} / approval={Encode(state.ApprovalId)} / noop={Encode(state.NoOpExecutionId)} / bounded={Encode(state.BoundedExecutionId)}</p>");
        html.AppendLine($"      <p data-testid=\"product-ledger-approved-handoff-report-draft-path\">{Encode(state.DraftRelativePath)}</p>");
        html.AppendLine($"      <p data-testid=\"product-ledger-approved-handoff-report-draft-boundary\">{Encode(state.OutputBoundary)} create-only no-overwrite</p>");
        html.AppendLine($"      <p data-testid=\"product-ledger-approved-handoff-report-draft-hash\">content_hash={Encode(state.ContentHashPrefix)}</p>");
        html.AppendLine("      <p data-testid=\"product-ledger-approved-handoff-report-draft-protection\">local/internal/development-only create-only no overwrite no arbitrary path no user workspace write no shell no subprocess no command execution no Pilot run no public/product path no Production route no network no DB no WORM/KMS no compliance custody no release/commercial no business signoff</p>");
        html.AppendLine("      <div data-testid=\"product-ledger-approved-handoff-report-draft-evidence-refs\">");
        foreach (var evidence in state.EvidenceRefs.OrderBy(evidence => evidence, StringComparer.Ordinal))
        {
            html.AppendLine($"        <p>{Encode(evidence)}</p>");
        }

        html.AppendLine("      </div>");
        html.AppendLine("      <div data-testid=\"product-ledger-approved-handoff-report-draft-blockers\">");
        if (state.Blockers.Count == 0)
        {
            html.AppendLine("        <p>none</p>");
        }
        else
        {
            foreach (var blocker in state.Blockers.OrderBy(blocker => blocker.ToString(), StringComparer.Ordinal))
            {
                html.AppendLine($"        <p>{Encode(blocker.ToString())}</p>");
            }
        }

        html.AppendLine("      </div>");
        html.AppendLine("    </section>");
        return html.ToString();
    }

    private static string ToWorkspaceTestJailHandoffDraftStateHtml(ProductLedgerLocalWorkspaceTestJailHandoffDraftSnapshot state)
    {
        var html = new StringBuilder();
        html.AppendLine($"    <section data-testid=\"product-ledger-workspace-test-jail-handoff-draft-state\" data-state=\"{Encode(state.State.ToString())}\" data-decision=\"{Encode(state.Decision.ToString())}\" data-local-only=\"{Lower(state.LocalOnly)}\" data-internal-only=\"{Lower(state.InternalOnly)}\" data-development-only=\"{Lower(state.DevelopmentOnly)}\" data-workspace-test-jail-only=\"{Lower(state.WorkspaceTestJailOnly)}\" data-create-only=\"{Lower(state.CreateOnly)}\" data-overwrite-allowed=\"{Lower(state.OverwriteAllowed)}\" data-user-selected-path-allowed=\"{Lower(state.UserSelectedPathAllowed)}\" data-payload-controlled-root-allowed=\"{Lower(state.PayloadControlledRootAllowed)}\" data-canonicalization-passed=\"{Lower(state.CanonicalizationPassed)}\" data-reparse-validation-passed=\"{Lower(state.ReparseValidationPassed)}\" data-shell-allowed=\"{Lower(state.ShellAllowed)}\" data-network-allowed=\"{Lower(state.NetworkAllowed)}\" data-production-allowed=\"{Lower(state.ProductionAllowed)}\" data-public-product-allowed=\"{Lower(state.PublicProductAllowed)}\" data-redaction-applied=\"{Lower(state.RedactionApplied)}\" data-product-command-executed=\"{Lower(state.ProductCommandExecuted)}\" data-public-ui-action=\"{Lower(state.PublicUiActionAvailable)}\" data-product-command-handler=\"{Lower(state.ProductCommandHandlerAvailable)}\" data-provider-cloud-network=\"{Lower(state.ProviderCloudNetworkAvailable)}\" data-db-migration=\"{Lower(state.DbMigrationAvailable)}\" data-kms-worm-external-trust=\"{Lower(state.KmsWormExternalTrustAvailable)}\" data-live-automation=\"{Lower(state.BrowserCdpWcuOcrRecipesLiveAvailable)}\" data-pilot-run=\"{Lower(state.PilotRunAvailable)}\" data-release-commercial=\"{Lower(state.ReleaseCommercialReady)}\">");
        html.AppendLine("      <h2>Workspace test-jail handoff draft state</h2>");
        html.AppendLine($"      <p data-testid=\"product-ledger-workspace-test-jail-handoff-draft-status\">{Encode(state.StatusText)}</p>");
        html.AppendLine($"      <p data-testid=\"product-ledger-workspace-test-jail-handoff-draft-result-kind\">{Encode(state.State.ToString())}</p>");
        html.AppendLine($"      <p data-testid=\"product-ledger-workspace-test-jail-handoff-draft-ids\">action={Encode(state.ActionId)} / candidate={Encode(state.CandidateId)} / approval={Encode(state.ApprovalId)} / noop={Encode(state.NoOpExecutionId)} / bounded={Encode(state.BoundedExecutionId)} / predecessor={Encode(state.PredecessorDraftId)}</p>");
        html.AppendLine($"      <p data-testid=\"product-ledger-workspace-test-jail-handoff-draft-path\">{Encode(state.DraftRelativePath)}</p>");
        html.AppendLine($"      <p data-testid=\"product-ledger-workspace-test-jail-handoff-draft-boundary\">{Encode(state.WorkspaceTestJailBoundary)} workspace test-jail only create-only no-overwrite</p>");
        html.AppendLine($"      <p data-testid=\"product-ledger-workspace-test-jail-handoff-draft-hashes\">content_hash={Encode(state.ContentHashPrefix)} / jail_hash={Encode(state.CanonicalJailRootHash)} / final_path_hash={Encode(state.CanonicalFinalPathHash)}</p>");
        html.AppendLine("      <p data-testid=\"product-ledger-workspace-test-jail-handoff-draft-protection\">local/internal/development-only workspace test-jail only create-only no overwrite no arbitrary path no user-selected path no payload-controlled root no shell no subprocess no command execution no Pilot run no public/product path no Production route no network no DB no WORM/KMS no compliance custody no release/commercial no business signoff</p>");
        html.AppendLine("      <div data-testid=\"product-ledger-workspace-test-jail-handoff-draft-evidence-refs\">");
        foreach (var evidence in state.EvidenceRefs.OrderBy(evidence => evidence, StringComparer.Ordinal))
        {
            html.AppendLine($"        <p>{Encode(evidence)}</p>");
        }

        html.AppendLine("      </div>");
        html.AppendLine("      <div data-testid=\"product-ledger-workspace-test-jail-handoff-draft-blockers\">");
        if (state.Blockers.Count == 0)
        {
            html.AppendLine("        <p>none</p>");
        }
        else
        {
            foreach (var blocker in state.Blockers.OrderBy(blocker => blocker.ToString(), StringComparer.Ordinal))
            {
                html.AppendLine($"        <p>{Encode(blocker.ToString())}</p>");
            }
        }

        html.AppendLine("      </div>");
        html.AppendLine("    </section>");
        return html.ToString();
    }

    private static string ToUserWorkspaceAllowlistedHandoffDraftStateHtml(ProductLedgerLocalUserWorkspaceAllowlistedHandoffDraftSnapshot state)
    {
        var html = new StringBuilder();
        html.AppendLine($"    <section data-testid=\"product-ledger-user-workspace-allowlisted-handoff-draft-state\" data-state=\"{Encode(state.State.ToString())}\" data-decision=\"{Encode(state.Decision.ToString())}\" data-local-only=\"{Lower(state.LocalOnly)}\" data-internal-only=\"{Lower(state.InternalOnly)}\" data-development-only=\"{Lower(state.DevelopmentOnly)}\" data-user-workspace-allowlisted-boundary-only=\"{Lower(state.UserWorkspaceAllowlistedBoundaryOnly)}\" data-create-only=\"{Lower(state.CreateOnly)}\" data-overwrite-allowed=\"{Lower(state.OverwriteAllowed)}\" data-user-selected-path-allowed=\"{Lower(state.UserSelectedPathAllowed)}\" data-payload-controlled-root-allowed=\"{Lower(state.PayloadControlledRootAllowed)}\" data-canonicalization-passed=\"{Lower(state.CanonicalizationPassed)}\" data-reparse-validation-passed=\"{Lower(state.ReparseValidationPassed)}\" data-shell-allowed=\"{Lower(state.ShellAllowed)}\" data-network-allowed=\"{Lower(state.NetworkAllowed)}\" data-production-allowed=\"{Lower(state.ProductionAllowed)}\" data-public-product-allowed=\"{Lower(state.PublicProductAllowed)}\" data-redaction-applied=\"{Lower(state.RedactionApplied)}\" data-product-command-executed=\"{Lower(state.ProductCommandExecuted)}\" data-public-ui-action=\"{Lower(state.PublicUiActionAvailable)}\" data-product-command-handler=\"{Lower(state.ProductCommandHandlerAvailable)}\" data-provider-cloud-network=\"{Lower(state.ProviderCloudNetworkAvailable)}\" data-db-migration=\"{Lower(state.DbMigrationAvailable)}\" data-kms-worm-external-trust=\"{Lower(state.KmsWormExternalTrustAvailable)}\" data-live-automation=\"{Lower(state.BrowserCdpWcuOcrRecipesLiveAvailable)}\" data-pilot-run=\"{Lower(state.PilotRunAvailable)}\" data-release-commercial=\"{Lower(state.ReleaseCommercialReady)}\">");
        html.AppendLine("      <h2>User workspace allowlisted handoff draft state</h2>");
        html.AppendLine($"      <p data-testid=\"product-ledger-user-workspace-allowlisted-handoff-draft-status\">{Encode(state.StatusText)}</p>");
        html.AppendLine($"      <p data-testid=\"product-ledger-user-workspace-allowlisted-handoff-draft-result-kind\">{Encode(state.State.ToString())}</p>");
        html.AppendLine($"      <p data-testid=\"product-ledger-user-workspace-allowlisted-handoff-draft-ids\">action={Encode(state.ActionId)} / candidate={Encode(state.CandidateId)} / approval={Encode(state.ApprovalId)} / noop={Encode(state.NoOpExecutionId)} / bounded={Encode(state.BoundedExecutionId)} / local_draft={Encode(state.LocalApprovedHandoffDraftId)} / workspace_test_jail={Encode(state.WorkspaceTestJailHandoffDraftId)}</p>");
        html.AppendLine($"      <p data-testid=\"product-ledger-user-workspace-allowlisted-handoff-draft-path\">{Encode(state.DraftRelativePath)}</p>");
        html.AppendLine($"      <p data-testid=\"product-ledger-user-workspace-allowlisted-handoff-draft-boundary\">{Encode(state.AllowedBoundary)} {Encode(state.WorkspaceClassification)} create-only no-overwrite</p>");
        html.AppendLine($"      <p data-testid=\"product-ledger-user-workspace-allowlisted-handoff-draft-hashes\">content_hash={Encode(state.ContentHashPrefix)} / workspace_hash={Encode(state.CanonicalWorkspaceRootHash)} / final_path_hash={Encode(state.CanonicalFinalPathHash)}</p>");
        html.AppendLine("      <p data-testid=\"product-ledger-user-workspace-allowlisted-handoff-draft-protection\">local/internal/development-only user workspace allowlisted boundary only create-only no overwrite no arbitrary path no user-selected path no payload-controlled root no shell no subprocess no command execution no Pilot run no public/product path no Production route no network no DB no WORM/KMS no compliance custody no release/commercial no business signoff</p>");
        html.AppendLine("      <div data-testid=\"product-ledger-user-workspace-allowlisted-handoff-draft-evidence-refs\">");
        foreach (var evidence in state.EvidenceRefs.OrderBy(evidence => evidence, StringComparer.Ordinal))
        {
            html.AppendLine($"        <p>{Encode(evidence)}</p>");
        }

        html.AppendLine("      </div>");
        html.AppendLine("      <div data-testid=\"product-ledger-user-workspace-allowlisted-handoff-draft-blockers\">");
        if (state.Blockers.Count == 0)
        {
            html.AppendLine("        <p>none</p>");
        }
        else
        {
            foreach (var blocker in state.Blockers.OrderBy(blocker => blocker.ToString(), StringComparer.Ordinal))
            {
                html.AppendLine($"        <p>{Encode(blocker.ToString())}</p>");
            }
        }

        html.AppendLine("      </div>");
        html.AppendLine("    </section>");
        return html.ToString();
    }

    private static string ToLatestStateSnapshotStateHtml(ProductLedgerLocalOperatorSurfaceLatestStateSnapshotResult state)
    {
        var html = new StringBuilder();
        html.AppendLine($"    <section data-testid=\"product-ledger-latest-state-snapshot-state\" data-state=\"{Encode(state.State.ToString())}\" data-decision=\"{Encode(state.Decision.ToString())}\" data-local-only=\"{Lower(state.LocalOnly)}\" data-internal-only=\"{Lower(state.InternalOnly)}\" data-development-only=\"{Lower(state.DevelopmentOnly)}\" data-historical-evidence-only=\"{Lower(state.HistoricalEvidenceOnly)}\" data-authority-live-product=\"{Lower(state.AuthorityLiveProduct)}\" data-create-only=\"{Lower(state.CreateOnly)}\" data-versioned=\"{Lower(state.Versioned)}\" data-json-only=\"{Lower(state.JsonOnly)}\" data-overwrite-allowed=\"{Lower(state.OverwriteAllowed)}\" data-latest-pointer-overwrite-allowed=\"{Lower(state.LatestPointerOverwriteAllowed)}\" data-user-selected-path-allowed=\"{Lower(state.UserSelectedPathAllowed)}\" data-payload-controlled-root-allowed=\"{Lower(state.PayloadControlledRootAllowed)}\" data-canonicalization-passed=\"{Lower(state.CanonicalizationPassed)}\" data-reparse-validation-passed=\"{Lower(state.ReparseValidationPassed)}\" data-production-allowed=\"{Lower(state.ProductionAllowed)}\" data-public-product-allowed=\"{Lower(state.PublicProductAllowed)}\" data-shell-allowed=\"{Lower(state.ShellAllowed)}\" data-command-execution-allowed=\"{Lower(state.CommandExecutionAllowed)}\" data-provider-cloud-network=\"{Lower(state.ProviderCloudNetworkAvailable)}\" data-db-migration=\"{Lower(state.DbMigrationAvailable)}\" data-kms-worm-external-trust=\"{Lower(state.KmsWormExternalTrustAvailable)}\" data-live-automation=\"{Lower(state.BrowserCdpWcuOcrRecipesLiveAvailable)}\" data-pilot-run=\"{Lower(state.PilotRunAvailable)}\" data-release-commercial=\"{Lower(state.ReleaseCommercialReady)}\">");
        html.AppendLine("      <h2>Latest-state snapshot state</h2>");
        html.AppendLine($"      <p data-testid=\"product-ledger-latest-state-snapshot-status\">{Encode(state.StatusText)}</p>");
        html.AppendLine($"      <p data-testid=\"product-ledger-latest-state-snapshot-result-kind\">{Encode(state.State.ToString())}</p>");
        html.AppendLine($"      <p data-testid=\"product-ledger-latest-state-snapshot-ids\">snapshot={Encode(state.SnapshotId)} / action={Encode(state.ActionId)}</p>");
        html.AppendLine($"      <p data-testid=\"product-ledger-latest-state-snapshot-path\">{Encode(state.SnapshotRelativePath)}</p>");
        html.AppendLine($"      <p data-testid=\"product-ledger-latest-state-snapshot-boundary\">{Encode(state.AllowedBoundary)} {Encode(state.Classification)} json-only immutable versioned create-only no-overwrite no latest pointer overwrite</p>");
        html.AppendLine($"      <p data-testid=\"product-ledger-latest-state-snapshot-hashes\">surface_hash={Encode(state.OperatorSurfaceModelHashPrefix)} / snapshot_hash={Encode(state.SnapshotContentHashPrefix)} / checkpoint_hash={Encode(state.CheckpointHashPrefix)}</p>");
        html.AppendLine($"      <p data-testid=\"product-ledger-latest-state-snapshot-stale-classification\">{Encode(state.StaleStateClassification)}</p>");
        html.AppendLine("      <p data-testid=\"product-ledger-latest-state-snapshot-protection\">local/internal/development-only historical evidence only not authority live/product no public/product path no Production route no broader workspace action no edit update delete no user-selected path no shell no subprocess no command execution no Pilot run no live Browser CDP WCU OCR Recipes no network no DB no WORM/KMS no compliance custody no release/commercial no business signoff</p>");
        html.AppendLine("      <div data-testid=\"product-ledger-latest-state-snapshot-evidence-refs\">");
        foreach (var evidence in state.EvidenceRefs.OrderBy(evidence => evidence, StringComparer.Ordinal))
        {
            html.AppendLine($"        <p>{Encode(evidence)}</p>");
        }

        html.AppendLine("      </div>");
        html.AppendLine("      <div data-testid=\"product-ledger-latest-state-snapshot-negative-flags\">");
        foreach (var flag in state.NegativeFlags.OrderBy(flag => flag, StringComparer.Ordinal))
        {
            html.AppendLine($"        <p>{Encode(flag)}</p>");
        }

        html.AppendLine("      </div>");
        html.AppendLine("      <div data-testid=\"product-ledger-latest-state-snapshot-blockers\">");
        if (state.Blockers.Count == 0)
        {
            html.AppendLine("        <p>none</p>");
        }
        else
        {
            foreach (var blocker in state.Blockers.OrderBy(blocker => blocker.ToString(), StringComparer.Ordinal))
            {
                html.AppendLine($"        <p>{Encode(blocker.ToString())}</p>");
            }
        }

        html.AppendLine("      </div>");
        html.AppendLine("    </section>");
        return html.ToString();
    }

    private static string ToApprovalDecisionStateHtml(ProductLedgerLocalApprovalDecisionSnapshot state)
    {
        var html = new StringBuilder();
        html.AppendLine($"    <section data-testid=\"product-ledger-approval-decision-state\" data-state=\"{Encode(state.State.ToString())}\" data-decision=\"{Encode(state.Decision.ToString())}\" data-local-only=\"{Lower(state.LocalOnly)}\" data-internal-only=\"{Lower(state.InternalOnly)}\" data-default-off=\"{Lower(state.DefaultOff)}\" data-fail-closed=\"{Lower(state.FailClosed)}\" data-product-command-executed=\"{Lower(state.ProductCommandExecuted)}\" data-public-ui-action=\"{Lower(state.PublicUiActionAvailable)}\" data-product-command-handler=\"{Lower(state.ProductCommandHandlerAvailable)}\" data-productive-service-registration=\"{Lower(state.ProductiveServiceRegistrationAvailable)}\" data-physical-export-created=\"{Lower(state.PhysicalExportCreated)}\" data-file-write-outside-state-store=\"{Lower(state.FileWriteOutsideApprovalStateStorePerformed)}\" data-provider-cloud-network=\"{Lower(state.ProviderCloudNetworkAvailable)}\" data-db-migration=\"{Lower(state.DbMigrationAvailable)}\" data-kms-worm-external-trust=\"{Lower(state.KmsWormExternalTrustAvailable)}\" data-live-automation=\"{Lower(state.BrowserCdpWcuOcrRecipesLiveAvailable)}\" data-pilot-run=\"{Lower(state.PilotRunAvailable)}\" data-release-commercial=\"{Lower(state.ReleaseCommercialReady)}\">");
        html.AppendLine("      <h2>Approval decision state</h2>");
        html.AppendLine($"      <p data-testid=\"product-ledger-approval-decision-status\">{Encode(state.StatusText)}</p>");
        html.AppendLine($"      <p data-testid=\"product-ledger-approval-decision-kind\">{Encode(state.OperatorDecision)}</p>");
        html.AppendLine($"      <p data-testid=\"product-ledger-approval-decision-approval-id\">{Encode(state.ApprovalId)}</p>");
        html.AppendLine($"      <p data-testid=\"product-ledger-approval-decision-candidate\">{Encode(state.CandidateActionKind)} / evidence={Encode(state.CandidateEvidenceHashPrefix)} / decision_hash={Encode(state.DecisionHashPrefix)}</p>");
        html.AppendLine($"      <p data-testid=\"product-ledger-approval-decision-note\">{Encode(state.RedactedOperatorNote)}</p>");
        html.AppendLine("      <p data-testid=\"product-ledger-approval-decision-execution-disabled\">no product command execution no public UI action no product command handler no write/export no release/commercial</p>");
        html.AppendLine("      <div data-testid=\"product-ledger-approval-decision-evidence-refs\">");
        foreach (var evidence in state.EvidenceReferences.OrderBy(evidence => evidence, StringComparer.Ordinal))
        {
            html.AppendLine($"        <p>{Encode(evidence)}</p>");
        }

        html.AppendLine("      </div>");
        html.AppendLine("      <div data-testid=\"product-ledger-approval-decision-blockers\">");
        if (state.Blockers.Count == 0)
        {
            html.AppendLine("        <p>none</p>");
        }
        else
        {
            foreach (var blocker in state.Blockers.OrderBy(blocker => blocker.ToString(), StringComparer.Ordinal))
            {
                html.AppendLine($"        <p>{Encode(blocker.ToString())}</p>");
            }
        }

        html.AppendLine("      </div>");
        html.AppendLine("    </section>");
        return html.ToString();
    }

    private static string ToApprovalPreviewLoopHtml(ProductLedgerLocalApprovalPreviewLoop loop)
    {
        var html = new StringBuilder();
        html.AppendLine($"    <section data-testid=\"product-ledger-approval-preview\" data-loop-id=\"{Encode(loop.LoopId)}\" data-local-only=\"{Lower(loop.IsLocalOnly)}\" data-read-only=\"{Lower(loop.IsReadOnly)}\" data-preview-only=\"{Lower(loop.IsPreviewOnly)}\" data-allows-execution=\"{Lower(loop.AllowsExecution)}\" data-allows-write=\"{Lower(loop.AllowsWrite)}\" data-allows-export=\"{Lower(loop.AllowsExport)}\" data-allows-network=\"{Lower(loop.AllowsNetwork)}\" data-allows-db=\"{Lower(loop.AllowsDb)}\" data-release-commercial=\"{Lower(loop.AllowsReleaseCommercial)}\">");
        html.AppendLine("      <h2>Approval/action preview loop</h2>");
        html.AppendLine($"      <p>{Encode(loop.ApprovalPreview.ApprovalRequiredLabel)} read-only preview-only no product command execution no write/export no release/commercial</p>");
        html.AppendLine($"      <p>{Encode(loop.OperatorMessage)}</p>");
        html.AppendLine($"      <button type=\"button\" data-testid=\"product-ledger-approval-preview-control\" data-executable=\"false\" data-handler-id=\"\" data-callback=\"\" disabled aria-disabled=\"true\">{Encode(loop.ApprovalPreview.ApprovalId)}</button>");
        html.AppendLine("    </section>");
        html.AppendLine($"    <section data-testid=\"product-ledger-candidate-action-preview\" data-command-kind=\"{Encode(loop.ActionPreview.CandidateActionKind.ToString())}\" data-command-id=\"{Encode(loop.ActionPreview.CommandId)}\" data-disabled=\"{Lower(loop.ActionPreview.Disabled)}\" data-no-op=\"{Lower(loop.ActionPreview.NoOp)}\" data-executable=\"{Lower(loop.ActionPreview.Executable)}\" data-product-command-execution=\"{Lower(loop.ActionPreview.AllowsProductCommandExecution)}\">");
        html.AppendLine($"      <p>{Encode(loop.ActionPreview.CandidateActionDescription)} read-only preview-only no product command execution no write/export no release/commercial</p>");
        html.AppendLine("    </section>");
        html.AppendLine($"    <section data-testid=\"product-ledger-policy-gate-preview\" data-policy-decision=\"{Encode(loop.PolicyGatePreview.PolicyDecision.ToString())}\" data-allows-execution=\"{Lower(loop.PolicyGatePreview.AllowsExecution)}\" data-allows-write=\"{Lower(loop.PolicyGatePreview.AllowsWrite)}\" data-allows-export=\"{Lower(loop.PolicyGatePreview.AllowsExport)}\" data-allows-network=\"{Lower(loop.PolicyGatePreview.AllowsNetwork)}\" data-allows-db=\"{Lower(loop.PolicyGatePreview.AllowsDb)}\" data-release-commercial=\"{Lower(loop.PolicyGatePreview.AllowsReleaseCommercial)}\">");
        foreach (var reason in loop.PolicyGatePreview.BlockedReasons)
        {
            html.AppendLine($"      <p>{Encode(reason)}</p>");
        }

        html.AppendLine("    </section>");
        html.AppendLine($"    <section data-testid=\"product-ledger-noop-execution-preview\" data-result-id=\"{Encode(loop.NoOpExecutionPreview.ResultId)}\" data-handler-invoked=\"{Lower(loop.NoOpExecutionPreview.HandlerInvoked)}\" data-callback-invoked=\"{Lower(loop.NoOpExecutionPreview.CallbackInvoked)}\" data-append-invoked=\"{Lower(loop.NoOpExecutionPreview.AppendInvoked)}\" data-write-invoked=\"{Lower(loop.NoOpExecutionPreview.WriteInvoked)}\" data-export-invoked=\"{Lower(loop.NoOpExecutionPreview.ExportInvoked)}\" data-pilot-run-invoked=\"{Lower(loop.NoOpExecutionPreview.PilotRunInvoked)}\">");
        html.AppendLine($"      <p>{Encode(loop.NoOpExecutionPreview.NoOpResult)} read-only preview-only no product command execution no write/export no release/commercial</p>");
        html.AppendLine("    </section>");
        html.AppendLine("    <section data-testid=\"product-ledger-preview-evidence-refs\">");
        foreach (var evidence in loop.EvidenceRefs.OrderBy(evidence => evidence.EvidenceId, StringComparer.Ordinal))
        {
            html.AppendLine($"      <p data-testid=\"product-ledger-preview-evidence-{Encode(evidence.EvidenceId)}\">{Encode(evidence.Source)} / {Encode(evidence.Status)}</p>");
        }

        html.AppendLine("    </section>");
        html.AppendLine($"    <p data-testid=\"product-ledger-approval-safe-next-step\">{Encode(loop.SafeNextStep)} read-only preview-only no product command execution no write/export no release/commercial</p>");
        return html.ToString();
    }

    private static string ToApprovalExecutionCandidatePreviewHtml(ProductLedgerLocalApprovalExecutionResult result)
    {
        var html = new StringBuilder();
        html.AppendLine($"    <section data-testid=\"product-ledger-approval-execution-candidate-preview\" data-decision=\"{Encode(result.Decision.ToString())}\" data-command-kind=\"{Encode(result.CandidateActionKind.ToString())}\" data-local-only=\"{Lower(result.LocalOnly)}\" data-internal-only=\"{Lower(result.InternalOnly)}\" data-default-off=\"{Lower(result.DefaultOff)}\" data-fail-closed=\"{Lower(result.FailClosed)}\" data-read-only-in-memory=\"{Lower(result.ReadOnlyOrInMemory)}\" data-public-ui-action=\"{Lower(result.PublicUiActionAvailable)}\" data-product-command-handler=\"{Lower(result.ProductCommandHandlerAvailable)}\" data-productive-service-registration=\"{Lower(result.ProductiveServiceRegistrationAvailable)}\" data-physical-export-created=\"{Lower(result.PhysicalExportCreated)}\" data-file-write-performed=\"{Lower(result.FileWritePerformed)}\" data-provider-cloud-network=\"{Lower(result.ProviderCloudNetworkAvailable)}\" data-db-migration=\"{Lower(result.DbMigrationAvailable)}\" data-kms-worm-external-trust=\"{Lower(result.KmsWormExternalTrustAvailable)}\" data-live-automation=\"{Lower(result.BrowserCdpWcuOcrRecipesLiveAvailable)}\" data-release-commercial=\"{Lower(result.ReleaseCommercialReady)}\">");
        html.AppendLine("      <h2>Approval execution candidate preview</h2>");
        html.AppendLine($"      <p data-testid=\"product-ledger-approval-execution-candidate-status\">{Encode(result.StatusText)} read-only in-memory default-off no public UI no write/export no release/commercial</p>");
        html.AppendLine($"      <p data-testid=\"product-ledger-approval-execution-candidate-result-kind\">{Encode(result.CommandResult?.ExecutionPreview.ResultKind ?? "REJECTED_NO_EXECUTION")}</p>");
        html.AppendLine("      <button type=\"button\" data-testid=\"product-ledger-approval-execution-candidate-control\" data-executable=\"false\" data-handler-id=\"\" data-callback=\"\" disabled aria-disabled=\"true\">approval execution candidate evidence</button>");
        html.AppendLine("    </section>");
        html.AppendLine("    <section data-testid=\"product-ledger-approval-execution-candidate-blockers\">");
        if (result.Blockers.Count == 0)
        {
            html.AppendLine("      <p>none</p>");
        }
        else
        {
            foreach (var blocker in result.Blockers.OrderBy(blocker => blocker.ToString(), StringComparer.Ordinal))
            {
                html.AppendLine($"      <p>{Encode(blocker.ToString())}</p>");
            }
        }

        html.AppendLine("    </section>");
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
