namespace OneBrain.Core.Approval;

public enum ProductLedgerOperatorAcceptanceMatrixDecision
{
    Rejected,
    ReadyLocalOnly
}

public enum ProductLedgerOperatorAcceptanceMatrixBlocker
{
    MissingExplicitLocalOnlyAcceptanceScope,
    MissingScreenshotEvidence,
    MissingBoundedExportEvidence,
    MissingCommandRouterEvidence,
    MissingCommandHandlerEvidence,
    MissingRuntimeGateEvidence,
    MalformedMatrixRow,
    PublicDeployClaimed,
    ExternalNetworkProviderCloudClaimed,
    TelemetrySyncBillingClaimed,
    DbMigrationClaimed,
    KmsWormExternalTrustClaimed,
    BrowserCdpWcuOcrRecipesLiveClaimed,
    DestructiveActionClaimed,
    UnboundedExportWriteClaimed,
    ReleaseCommercialClaimed,
    CustodyComplianceClaimed
}

public sealed record ProductLedgerOperatorAcceptanceMatrixRequest(
    bool ExplicitLocalOnlyAcceptanceScope,
    bool HasScreenshotEvidence,
    bool HasBoundedExportEvidence,
    bool HasCommandRouterEvidence,
    bool HasCommandHandlerEvidence,
    bool HasRuntimeGateEvidence,
    bool ClaimsPublicDeploy,
    bool ClaimsExternalNetworkProviderCloud,
    bool ClaimsTelemetrySyncBilling,
    bool ClaimsDbMigration,
    bool ClaimsKmsWormExternalTrust,
    bool ClaimsBrowserCdpWcuOcrRecipesLive,
    bool ClaimsDestructiveAction,
    bool ClaimsUnboundedExportWrite,
    bool ClaimsReleaseCommercial,
    bool ClaimsCustodyComplianceGrade);

public sealed record ProductLedgerOperatorAcceptanceMatrixRow(
    string ActionId,
    string VisibleLabel,
    bool LocalOnly,
    bool NonDestructive,
    bool RequiresApproval,
    bool ExecutionAllowed,
    IReadOnlyList<string> BlockedCapabilities,
    IReadOnlyList<string> EvidenceRefs,
    string RiskLevel,
    string ExpectedOperatorMessage,
    string SafeNextStep);

public sealed record ProductLedgerOperatorAcceptanceMatrixResult(
    ProductLedgerOperatorAcceptanceMatrixDecision Decision,
    IReadOnlyList<ProductLedgerOperatorAcceptanceMatrixBlocker> Blockers,
    IReadOnlyList<ProductLedgerOperatorAcceptanceMatrixRow> Rows,
    bool LocalOnly,
    bool TestOnly,
    bool FixtureSafe,
    bool FailClosed,
    bool PublicInternetExposureAvailable,
    bool DestructiveActionAvailable,
    bool ExternalNetworkProviderCloudAvailable,
    bool TelemetrySyncBillingAvailable,
    bool DbMigrationAvailable,
    bool KmsWormExternalTrustAvailable,
    bool BrowserCdpWcuOcrRecipesLiveAvailable,
    bool UnboundedExportWriteAvailable,
    bool ReleaseCommercialReady,
    string StatusText);

public sealed class ProductLedgerOperatorAcceptanceLocalOnlyMatrix
{
    public const string ReadyStatus =
        "PRODUCT_LEDGER_OPERATOR_ACCEPTANCE_LOCAL_ONLY_MATRIX_READY LOCAL_ONLY TEST_ONLY FIXTURE_SAFE FAIL_CLOSED NO_PUBLIC_DEPLOY NO_EXTERNAL_NETWORK_PROVIDER_CLOUD NO_TELEMETRY_SYNC_BILLING NO_DB_MIGRATION NO_KMS_WORM_EXTERNAL_TRUST NO_LIVE_AUTOMATION NO_DESTRUCTIVE_ACTION NO_UNBOUNDED_EXPORT_WRITE NO_RELEASE_COMMERCIAL NO_COMPLIANCE_CUSTODY_CLAIM";

    public const string RejectedStatus =
        "PRODUCT_LEDGER_OPERATOR_ACCEPTANCE_LOCAL_ONLY_MATRIX_REJECTED FAIL_CLOSED NO_PUBLIC_DEPLOY NO_EXTERNAL_NETWORK_PROVIDER_CLOUD NO_TELEMETRY_SYNC_BILLING NO_DB_MIGRATION NO_KMS_WORM_EXTERNAL_TRUST NO_LIVE_AUTOMATION NO_DESTRUCTIVE_ACTION NO_UNBOUNDED_EXPORT_WRITE NO_RELEASE_COMMERCIAL";

    public ProductLedgerOperatorAcceptanceMatrixResult Build(ProductLedgerOperatorAcceptanceMatrixRequest request)
    {
        var blockers = new List<ProductLedgerOperatorAcceptanceMatrixBlocker>();
        AddRequestBlockers(request, blockers);

        var rows = Rows();
        if (rows.Any(IsMalformed))
        {
            blockers.Add(ProductLedgerOperatorAcceptanceMatrixBlocker.MalformedMatrixRow);
        }

        var distinct = blockers.Distinct().OrderBy(blocker => blocker.ToString(), StringComparer.Ordinal).ToArray();
        var ready = distinct.Length == 0;
        return new ProductLedgerOperatorAcceptanceMatrixResult(
            Decision: ready
                ? ProductLedgerOperatorAcceptanceMatrixDecision.ReadyLocalOnly
                : ProductLedgerOperatorAcceptanceMatrixDecision.Rejected,
            Blockers: distinct,
            Rows: rows,
            LocalOnly: true,
            TestOnly: true,
            FixtureSafe: true,
            FailClosed: true,
            PublicInternetExposureAvailable: false,
            DestructiveActionAvailable: false,
            ExternalNetworkProviderCloudAvailable: false,
            TelemetrySyncBillingAvailable: false,
            DbMigrationAvailable: false,
            KmsWormExternalTrustAvailable: false,
            BrowserCdpWcuOcrRecipesLiveAvailable: false,
            UnboundedExportWriteAvailable: false,
            ReleaseCommercialReady: false,
            StatusText: ready ? ReadyStatus : RejectedStatus);
    }

    public static ProductLedgerOperatorAcceptanceMatrixRequest ReadyRequest() =>
        new(
            ExplicitLocalOnlyAcceptanceScope: true,
            HasScreenshotEvidence: true,
            HasBoundedExportEvidence: true,
            HasCommandRouterEvidence: true,
            HasCommandHandlerEvidence: true,
            HasRuntimeGateEvidence: true,
            ClaimsPublicDeploy: false,
            ClaimsExternalNetworkProviderCloud: false,
            ClaimsTelemetrySyncBilling: false,
            ClaimsDbMigration: false,
            ClaimsKmsWormExternalTrust: false,
            ClaimsBrowserCdpWcuOcrRecipesLive: false,
            ClaimsDestructiveAction: false,
            ClaimsUnboundedExportWrite: false,
            ClaimsReleaseCommercial: false,
            ClaimsCustodyComplianceGrade: false);

    private static void AddRequestBlockers(
        ProductLedgerOperatorAcceptanceMatrixRequest request,
        List<ProductLedgerOperatorAcceptanceMatrixBlocker> blockers)
    {
        if (!request.ExplicitLocalOnlyAcceptanceScope)
        {
            blockers.Add(ProductLedgerOperatorAcceptanceMatrixBlocker.MissingExplicitLocalOnlyAcceptanceScope);
        }

        if (!request.HasScreenshotEvidence)
        {
            blockers.Add(ProductLedgerOperatorAcceptanceMatrixBlocker.MissingScreenshotEvidence);
        }

        if (!request.HasBoundedExportEvidence)
        {
            blockers.Add(ProductLedgerOperatorAcceptanceMatrixBlocker.MissingBoundedExportEvidence);
        }

        if (!request.HasCommandRouterEvidence)
        {
            blockers.Add(ProductLedgerOperatorAcceptanceMatrixBlocker.MissingCommandRouterEvidence);
        }

        if (!request.HasCommandHandlerEvidence)
        {
            blockers.Add(ProductLedgerOperatorAcceptanceMatrixBlocker.MissingCommandHandlerEvidence);
        }

        if (!request.HasRuntimeGateEvidence)
        {
            blockers.Add(ProductLedgerOperatorAcceptanceMatrixBlocker.MissingRuntimeGateEvidence);
        }

        if (request.ClaimsPublicDeploy)
        {
            blockers.Add(ProductLedgerOperatorAcceptanceMatrixBlocker.PublicDeployClaimed);
        }

        if (request.ClaimsExternalNetworkProviderCloud)
        {
            blockers.Add(ProductLedgerOperatorAcceptanceMatrixBlocker.ExternalNetworkProviderCloudClaimed);
        }

        if (request.ClaimsTelemetrySyncBilling)
        {
            blockers.Add(ProductLedgerOperatorAcceptanceMatrixBlocker.TelemetrySyncBillingClaimed);
        }

        if (request.ClaimsDbMigration)
        {
            blockers.Add(ProductLedgerOperatorAcceptanceMatrixBlocker.DbMigrationClaimed);
        }

        if (request.ClaimsKmsWormExternalTrust)
        {
            blockers.Add(ProductLedgerOperatorAcceptanceMatrixBlocker.KmsWormExternalTrustClaimed);
        }

        if (request.ClaimsBrowserCdpWcuOcrRecipesLive)
        {
            blockers.Add(ProductLedgerOperatorAcceptanceMatrixBlocker.BrowserCdpWcuOcrRecipesLiveClaimed);
        }

        if (request.ClaimsDestructiveAction)
        {
            blockers.Add(ProductLedgerOperatorAcceptanceMatrixBlocker.DestructiveActionClaimed);
        }

        if (request.ClaimsUnboundedExportWrite)
        {
            blockers.Add(ProductLedgerOperatorAcceptanceMatrixBlocker.UnboundedExportWriteClaimed);
        }

        if (request.ClaimsReleaseCommercial)
        {
            blockers.Add(ProductLedgerOperatorAcceptanceMatrixBlocker.ReleaseCommercialClaimed);
        }

        if (request.ClaimsCustodyComplianceGrade)
        {
            blockers.Add(ProductLedgerOperatorAcceptanceMatrixBlocker.CustodyComplianceClaimed);
        }
    }

    private static bool IsMalformed(ProductLedgerOperatorAcceptanceMatrixRow row) =>
        string.IsNullOrWhiteSpace(row.ActionId)
        || string.IsNullOrWhiteSpace(row.VisibleLabel)
        || string.IsNullOrWhiteSpace(row.RiskLevel)
        || string.IsNullOrWhiteSpace(row.ExpectedOperatorMessage)
        || string.IsNullOrWhiteSpace(row.SafeNextStep)
        || !row.LocalOnly
        || !row.NonDestructive
        || row.ExecutionAllowed
        || row.EvidenceRefs.Count == 0;

    private static IReadOnlyList<ProductLedgerOperatorAcceptanceMatrixRow> Rows() =>
    [
        Inspect("inspect-ledger-local-evidence", "Inspect Product Ledger local-only evidence", ["product-ledger-local-only-writer", "operator-diagnostics"]),
        Inspect("inspect-screenshot-evidence", "Inspect screenshot evidence metadata", ["browser-local-only-screenshot", "screenshot-sha256"]),
        Inspect("inspect-bounded-export-evidence", "Inspect bounded local report export evidence", ["bounded-local-report-export", "post-write-hash"]),
        Inspect("inspect-command-router-preview", "Inspect internal command router preview", ["internal-command-router-noop-read-only"]),
        Inspect("inspect-command-handler-result", "Inspect command handler non-destructive result", ["internal-command-handler-non-destructive"]),
        Inspect("inspect-runtime-gate-status", "Inspect runtime local-only gate status", ["runtime-local-only-gate"]),
        Inspect("inspect-public-local-actions", "Inspect public local-only action availability", ["public-local-only-actions", "disabled-action-model"]),
        Blocked("blocked-external-cloud-live-release-reasons", "See blocked reasons for external/cloud/live/release", ["provider/cloud/network", "live automation", "release/commercial"], ["public-surface-readiness-packet"]),
        Blocked("cannot-trigger-destructive-action", "Cannot trigger destructive action", ["destructive action"], ["disabled-dangerous-actions"]),
        Blocked("cannot-trigger-external-cloud-provider-network", "Cannot trigger external/cloud/provider/network", ["external network", "provider/cloud"], ["static-scan-no-external"]),
        Blocked("cannot-trigger-db-migration", "Cannot trigger DB/migration", ["DB/migration"], ["static-scan-no-db"]),
        Blocked("cannot-trigger-telemetry-sync-billing", "Cannot trigger telemetry/sync/billing", ["telemetry/sync", "billing cloud"], ["static-scan-no-telemetry"]),
        Blocked("cannot-trigger-browser-cdp-live", "Cannot trigger Browser/CDP live automation", ["Browser/CDP live", "WCU/OCR/Recipes live"], ["browser-local-only-screenshot-boundary"]),
        Blocked("cannot-claim-release-commercial", "Cannot claim release/commercial readiness", ["release/commercial"], ["release-commercial-no-go"]),
        Blocked("cannot-claim-kms-worm-external-trust", "Cannot claim KMS/WORM/external trust", ["KMS/WORM", "external trust"], ["not-compliance-grade-custody"])
    ];

    private static ProductLedgerOperatorAcceptanceMatrixRow Inspect(
        string actionId,
        string label,
        IReadOnlyList<string> evidenceRefs) =>
        new(
            ActionId: actionId,
            VisibleLabel: label,
            LocalOnly: true,
            NonDestructive: true,
            RequiresApproval: false,
            ExecutionAllowed: false,
            BlockedCapabilities: ["product runtime execution"],
            EvidenceRefs: evidenceRefs,
            RiskLevel: "low-local-read-only",
            ExpectedOperatorMessage: "Visible for local-only inspection; no execution authority is granted.",
            SafeNextStep: "read-only audit or static scan hardening");

    private static ProductLedgerOperatorAcceptanceMatrixRow Blocked(
        string actionId,
        string label,
        IReadOnlyList<string> blockedCapabilities,
        IReadOnlyList<string> evidenceRefs) =>
        new(
            ActionId: actionId,
            VisibleLabel: label,
            LocalOnly: true,
            NonDestructive: true,
            RequiresApproval: true,
            ExecutionAllowed: false,
            BlockedCapabilities: blockedCapabilities,
            EvidenceRefs: evidenceRefs,
            RiskLevel: "blocked-frontier",
            ExpectedOperatorMessage: "Blocked in local-only acceptance; requires a separate explicit GO before any real frontier.",
            SafeNextStep: "keep blocked and run external audit read-only");
}
