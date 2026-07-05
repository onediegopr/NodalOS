namespace OneBrain.Core.Approval;

public enum ProductLedgerLocalApprovalPreviewDecision
{
    PreviewOnly,
    Blocked,
    NeedsHumanReviewPreview
}

public sealed record ProductLedgerApprovalPreview(
    string ApprovalId,
    string ApprovalRequiredLabel,
    string OperatorMessage,
    bool ReadOnly,
    bool PreviewOnly,
    bool HumanReviewRequired);

public sealed record ProductLedgerActionPreview(
    ProductLedgerInternalCommandKind CandidateActionKind,
    string CandidateActionDescription,
    string CommandId,
    bool Disabled,
    bool NoOp,
    bool Executable,
    bool AllowsProductCommandExecution);

public sealed record ProductLedgerPolicyGatePreview(
    ProductLedgerLocalApprovalPreviewDecision PolicyDecision,
    IReadOnlyList<string> BlockedReasons,
    bool AllowsExecution,
    bool AllowsWrite,
    bool AllowsExport,
    bool AllowsNetwork,
    bool AllowsDb,
    bool AllowsReleaseCommercial);

public sealed record ProductLedgerNoOpExecutionPreview(
    string ResultId,
    string NoOpResult,
    bool HandlerInvoked,
    bool CallbackInvoked,
    bool AppendInvoked,
    bool WriteInvoked,
    bool ExportInvoked,
    bool PilotRunInvoked);

public sealed record ProductLedgerPreviewEvidenceLink(
    string EvidenceId,
    string Source,
    string Status);

public sealed record ProductLedgerLocalApprovalPreviewLoop(
    string LoopId,
    string Scope,
    ProductLedgerApprovalPreview ApprovalPreview,
    ProductLedgerActionPreview ActionPreview,
    ProductLedgerPolicyGatePreview PolicyGatePreview,
    ProductLedgerNoOpExecutionPreview NoOpExecutionPreview,
    IReadOnlyList<ProductLedgerPreviewEvidenceLink> EvidenceRefs,
    string SafeNextStep,
    string OperatorMessage,
    IReadOnlyList<string> DomAnchors,
    bool IsLocalOnly,
    bool IsReadOnly,
    bool IsPreviewOnly,
    bool AllowsExecution,
    bool AllowsWrite,
    bool AllowsExport,
    bool AllowsNetwork,
    bool AllowsDb,
    bool AllowsReleaseCommercial);

public static class ProductLedgerLocalApprovalPreviewLoopFactory
{
    public const string LoopId = "product-ledger.local-approval-action-preview-loop.v1";
    public const string SafeNextStep = "LOCAL_ROUTE_LIVE_LEDGER_READ_MODEL_TEST_SAFE_OR_HTTP_IN_PROCESS_ROUTE_TEST_INFRA";

    public static ProductLedgerLocalApprovalPreviewLoop Build(
        string surfaceId,
        string routePath,
        IReadOnlyList<ProductLedgerOperatorSurfaceEvidenceRef> surfaceEvidenceRefs)
    {
        var routerPreview = new ProductLedgerInternalCommandPreviewRouter().Preview(
            new ProductLedgerInternalCommandPreviewRequest(
                ExplicitInternalLocalOnlyNoOpReadOnlyScope: true,
                CommandKind: ProductLedgerInternalCommandKind.ViewLedgerReadiness,
                RawCommandName: ProductLedgerInternalCommandKind.ViewLedgerReadiness.ToString(),
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
                ClaimsWriterExecutionOutsideValidatedLocalOnlyPolicy: false));

        var previewAllowed = routerPreview.Decision == ProductLedgerInternalCommandPreviewDecision.PreviewedNoOpReadOnly
            && routerPreview.Blockers.Count == 0
            && routerPreview.NoOp
            && routerPreview.ReadOnly
            && !routerPreview.Preview.Executable;

        var blockedReasons = BlockedReasons(routerPreview, previewAllowed);
        var decision = previewAllowed
            ? ProductLedgerLocalApprovalPreviewDecision.NeedsHumanReviewPreview
            : ProductLedgerLocalApprovalPreviewDecision.Blocked;

        return new ProductLedgerLocalApprovalPreviewLoop(
            LoopId: LoopId,
            Scope: ProductLedgerOperatorSurfaceModelFactory.Scope,
            ApprovalPreview: new ProductLedgerApprovalPreview(
                ApprovalId: "approval-preview.product-ledger.view-ledger-readiness",
                ApprovalRequiredLabel: "Human review preview required before any future execution authority.",
                OperatorMessage: "Read-only approval preview: this label does not approve real execution.",
                ReadOnly: true,
                PreviewOnly: true,
                HumanReviewRequired: true),
            ActionPreview: new ProductLedgerActionPreview(
                CandidateActionKind: routerPreview.Preview.CommandKind,
                CandidateActionDescription: "View Product Ledger readiness as a disabled no-op preview.",
                CommandId: routerPreview.Preview.CommandId,
                Disabled: true,
                NoOp: true,
                Executable: false,
                AllowsProductCommandExecution: false),
            PolicyGatePreview: new ProductLedgerPolicyGatePreview(
                PolicyDecision: decision,
                BlockedReasons: blockedReasons,
                AllowsExecution: false,
                AllowsWrite: false,
                AllowsExport: false,
                AllowsNetwork: false,
                AllowsDb: false,
                AllowsReleaseCommercial: false),
            NoOpExecutionPreview: new ProductLedgerNoOpExecutionPreview(
                ResultId: "noop-preview.product-ledger.view-ledger-readiness",
                NoOpResult: previewAllowed
                    ? "NO_OP_PREVIEW_ONLY_BLOCKED_BEFORE_HANDLER"
                    : "NO_OP_PREVIEW_REJECTED_FAIL_CLOSED",
                HandlerInvoked: false,
                CallbackInvoked: false,
                AppendInvoked: false,
                WriteInvoked: false,
                ExportInvoked: false,
                PilotRunInvoked: false),
            EvidenceRefs: EvidenceLinks(surfaceId, routePath, surfaceEvidenceRefs, routerPreview),
            SafeNextStep: SafeNextStep,
            OperatorMessage: "Approval-to-action loop is local-only/read-only/preview-only; no product command execution, no write/export and no release/commercial claim.",
            DomAnchors:
            [
                "product-ledger-approval-preview",
                "product-ledger-candidate-action-preview",
                "product-ledger-policy-gate-preview",
                "product-ledger-noop-execution-preview",
                "product-ledger-preview-evidence-refs",
                "product-ledger-approval-safe-next-step"
            ],
            IsLocalOnly: true,
            IsReadOnly: true,
            IsPreviewOnly: true,
            AllowsExecution: false,
            AllowsWrite: false,
            AllowsExport: false,
            AllowsNetwork: false,
            AllowsDb: false,
            AllowsReleaseCommercial: false);
    }

    private static IReadOnlyList<string> BlockedReasons(
        ProductLedgerInternalCommandPreviewResult routerPreview,
        bool previewAllowed)
    {
        var reasons = new List<string>
        {
            "preview-only: no product command execution",
            "read-only: no append/write/export",
            "local-only: no external network/provider/cloud",
            "no DB/migration",
            "no KMS/WORM/external trust",
            "no Browser/CDP/WCU/OCR/Recipes live",
            "no destructive action",
            "no release/commercial"
        };

        if (!previewAllowed)
        {
            reasons.AddRange(routerPreview.Blockers.Select(blocker => blocker.ToString()));
        }

        return reasons.Distinct().OrderBy(reason => reason, StringComparer.Ordinal).ToArray();
    }

    private static IReadOnlyList<ProductLedgerPreviewEvidenceLink> EvidenceLinks(
        string surfaceId,
        string routePath,
        IReadOnlyList<ProductLedgerOperatorSurfaceEvidenceRef> surfaceEvidenceRefs,
        ProductLedgerInternalCommandPreviewResult routerPreview) =>
    [
        new("operator-surface", surfaceId, "canonical local/dev route surface"),
        new("route-path", routePath, "development-only route path"),
        new("command-router-preview", routerPreview.Preview.CommandId, routerPreview.StatusText),
        .. surfaceEvidenceRefs.Select(evidence => new ProductLedgerPreviewEvidenceLink(
            EvidenceId: evidence.EvidenceId,
            Source: evidence.Source,
            Status: evidence.Boundary))
    ];
}
