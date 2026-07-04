namespace OneBrain.Core.Approval;

public enum ProductLedgerInternalOperatorUiPreviewDecision
{
    Rejected,
    RenderedPreview
}

public enum ProductLedgerInternalOperatorUiPreviewSeverity
{
    Info,
    Warning,
    Blocker
}

public enum ProductLedgerInternalOperatorUiPreviewBlocker
{
    MissingRequest,
    MissingExplicitInternalLocalOnlyReadOnlyPreviewScope,
    MissingDiagnosticsSurface,
    UnsafeDiagnosticsSurface,
    MissingRequiredDiagnosticsSection,
    UnsafeCommandPreviewRouter,
    PublicUiActionRequested,
    DestructiveUserFacingActionRequested,
    ProductCommandHandlerRequested,
    ProductiveServiceRegistrationRequested,
    ProviderCloudNetworkClaimed,
    DbMigrationClaimed,
    KmsWormExternalTrustClaimed,
    BrowserCdpWcuOcrRecipesLiveClaimed,
    ReleaseCommercialClaimed,
    ExternalTelemetryOrSyncClaimed,
    BillingLicensingCloudClaimed
}

public sealed record ProductLedgerInternalOperatorUiPreviewRequest(
    bool ExplicitInternalLocalOnlyReadOnlyPreviewScope,
    ProductLedgerLocalOnlyOperatorDiagnosticsResult? Diagnostics,
    bool RequestsPublicUiAction,
    bool RequestsDestructiveUserFacingAction,
    bool RequestsProductCommandHandler,
    bool RequestsProductiveServiceRegistration,
    bool ClaimsProviderCloudNetwork,
    bool ClaimsDbMigration,
    bool ClaimsKmsWormExternalTrust,
    bool ClaimsBrowserCdpWcuOcrRecipesLive,
    bool ClaimsReleaseCommercial,
    bool ClaimsExternalTelemetryOrSync,
    bool ClaimsBillingLicensingCloud,
    IReadOnlyList<ProductLedgerInternalCommandPreviewResult>? CommandPreviews = null);

public sealed record ProductLedgerInternalOperatorUiPreviewHeader(
    string Title,
    string Status,
    int ReadinessPercentage,
    IReadOnlyList<string> Notices);

public sealed record ProductLedgerInternalOperatorUiPreviewSection(
    string SectionId,
    string Title,
    string Status,
    IReadOnlyList<string> Lines,
    ProductLedgerInternalOperatorUiPreviewSeverity Severity);

public sealed record ProductLedgerInternalOperatorUiPreviewActionPreview(
    string ActionId,
    string Label,
    string RiskLabel,
    string BlockedReason,
    IReadOnlyList<string> RequiredEvidence,
    bool Disabled,
    string? ProductiveCommandId,
    string? HandlerId,
    string? CallbackName);

public sealed record ProductLedgerInternalOperatorUiPreviewViewModel(
    string ViewModelId,
    ProductLedgerInternalOperatorUiPreviewHeader Header,
    IReadOnlyList<ProductLedgerInternalOperatorUiPreviewSection> Sections,
    IReadOnlyList<ProductLedgerInternalOperatorUiPreviewActionPreview> ActionPreviews,
    IReadOnlyList<string> Blockers,
    IReadOnlyList<string> Warnings,
    string SafeNextStep,
    bool LocalOnly,
    bool InternalOnly,
    bool ReadOnly,
    bool FailClosed,
    bool PublicUiActionAvailable,
    bool DestructiveUserFacingActionAvailable,
    bool ProductCommandHandlerAvailable,
    bool ProductiveServiceRegistrationAvailable,
    bool ProviderCloudNetworkAvailable,
    bool DbMigrationAvailable,
    bool KmsWormExternalTrustAvailable,
    bool BrowserCdpWcuOcrRecipesLiveAvailable,
    bool ExternalTelemetryOrSyncAvailable,
    bool BillingLicensingCloudAvailable,
    bool ReleaseCommercialReady,
    string StatusText);

public sealed record ProductLedgerInternalOperatorUiPreviewResult(
    ProductLedgerInternalOperatorUiPreviewDecision Decision,
    IReadOnlyList<ProductLedgerInternalOperatorUiPreviewBlocker> Blockers,
    ProductLedgerInternalOperatorUiPreviewViewModel ViewModel);

public sealed class ProductLedgerInternalOperatorUiPresenter
{
    public const string ReadyStatus =
        "PRODUCT_LEDGER_INTERNAL_OPERATOR_UI_READ_ONLY_PREVIEW_READY INTERNAL_ONLY LOCAL_ONLY READ_ONLY FAIL_CLOSED ACTIONS_DISABLED NO_PUBLIC_UI_ACTION NO_DESTRUCTIVE_ACTION NO_COMMAND_HANDLERS NO_PROVIDER_CLOUD_NETWORK NO_DB_MIGRATION NO_KMS_WORM_EXTERNAL_TRUST NO_LIVE_AUTOMATION NO_RELEASE_COMMERCIAL";

    public const string RejectedStatus =
        "PRODUCT_LEDGER_INTERNAL_OPERATOR_UI_READ_ONLY_PREVIEW_REJECTED FAIL_CLOSED ACTIONS_DISABLED NO_PUBLIC_UI_ACTION NO_DESTRUCTIVE_ACTION NO_COMMAND_HANDLERS NO_PROVIDER_CLOUD_NETWORK NO_DB_MIGRATION NO_KMS_WORM_EXTERNAL_TRUST NO_LIVE_AUTOMATION NO_RELEASE_COMMERCIAL";

    private static readonly string[] RequiredDiagnosticsSections =
    [
        "Runtime Local-Only Gate",
        "Product Ledger Path Policy",
        "Bounded Writer Status",
        "Checkpoint / Head Status",
        "Evidence Gates",
        "Disabled Actions",
        "Safe Next Step"
    ];

    public ProductLedgerInternalOperatorUiPreviewResult Render(ProductLedgerInternalOperatorUiPreviewRequest? request)
    {
        var blockers = new List<ProductLedgerInternalOperatorUiPreviewBlocker>();
        if (request is null)
        {
            blockers.Add(ProductLedgerInternalOperatorUiPreviewBlocker.MissingRequest);
            return Result(blockers, null);
        }

        AddRequestBlockers(request, blockers);
        AddDiagnosticsBlockers(request.Diagnostics, blockers);
        AddCommandPreviewBlockers(request.CommandPreviews, blockers);
        return Result(blockers, request);
    }

    private static void AddRequestBlockers(
        ProductLedgerInternalOperatorUiPreviewRequest request,
        List<ProductLedgerInternalOperatorUiPreviewBlocker> blockers)
    {
        if (!request.ExplicitInternalLocalOnlyReadOnlyPreviewScope)
        {
            blockers.Add(ProductLedgerInternalOperatorUiPreviewBlocker.MissingExplicitInternalLocalOnlyReadOnlyPreviewScope);
        }

        if (request.RequestsPublicUiAction)
        {
            blockers.Add(ProductLedgerInternalOperatorUiPreviewBlocker.PublicUiActionRequested);
        }

        if (request.RequestsDestructiveUserFacingAction)
        {
            blockers.Add(ProductLedgerInternalOperatorUiPreviewBlocker.DestructiveUserFacingActionRequested);
        }

        if (request.RequestsProductCommandHandler)
        {
            blockers.Add(ProductLedgerInternalOperatorUiPreviewBlocker.ProductCommandHandlerRequested);
        }

        if (request.RequestsProductiveServiceRegistration)
        {
            blockers.Add(ProductLedgerInternalOperatorUiPreviewBlocker.ProductiveServiceRegistrationRequested);
        }

        if (request.ClaimsProviderCloudNetwork)
        {
            blockers.Add(ProductLedgerInternalOperatorUiPreviewBlocker.ProviderCloudNetworkClaimed);
        }

        if (request.ClaimsDbMigration)
        {
            blockers.Add(ProductLedgerInternalOperatorUiPreviewBlocker.DbMigrationClaimed);
        }

        if (request.ClaimsKmsWormExternalTrust)
        {
            blockers.Add(ProductLedgerInternalOperatorUiPreviewBlocker.KmsWormExternalTrustClaimed);
        }

        if (request.ClaimsBrowserCdpWcuOcrRecipesLive)
        {
            blockers.Add(ProductLedgerInternalOperatorUiPreviewBlocker.BrowserCdpWcuOcrRecipesLiveClaimed);
        }

        if (request.ClaimsReleaseCommercial)
        {
            blockers.Add(ProductLedgerInternalOperatorUiPreviewBlocker.ReleaseCommercialClaimed);
        }

        if (request.ClaimsExternalTelemetryOrSync)
        {
            blockers.Add(ProductLedgerInternalOperatorUiPreviewBlocker.ExternalTelemetryOrSyncClaimed);
        }

        if (request.ClaimsBillingLicensingCloud)
        {
            blockers.Add(ProductLedgerInternalOperatorUiPreviewBlocker.BillingLicensingCloudClaimed);
        }
    }

    private static void AddDiagnosticsBlockers(
        ProductLedgerLocalOnlyOperatorDiagnosticsResult? diagnostics,
        List<ProductLedgerInternalOperatorUiPreviewBlocker> blockers)
    {
        if (diagnostics is null)
        {
            blockers.Add(ProductLedgerInternalOperatorUiPreviewBlocker.MissingDiagnosticsSurface);
            return;
        }

        if (diagnostics.Decision != ProductLedgerLocalOnlyOperatorDiagnosticsDecision.RenderedReadOnly
            || diagnostics.Blockers.Count > 0
            || !diagnostics.ReadOnly
            || !diagnostics.LocalOnly
            || !diagnostics.InternalOnly
            || !diagnostics.FailClosed
            || diagnostics.PublicUiActionAvailable
            || diagnostics.DestructiveUserFacingActionAvailable
            || diagnostics.ProductCommandHandlerAvailable
            || diagnostics.ProductiveServiceRegistrationAvailable
            || diagnostics.ProviderCloudNetworkAvailable
            || diagnostics.DbMigrationAvailable
            || diagnostics.KmsWormExternalTrustAvailable
            || diagnostics.BrowserCdpWcuOcrRecipesLiveAvailable
            || diagnostics.ReleaseCommercialReady
            || diagnostics.ActionPreviews.Any(action => !action.Disabled
                || action.ProductiveCommandId is not null
                || action.HandlerName is not null
                || action.CallbackName is not null))
        {
            blockers.Add(ProductLedgerInternalOperatorUiPreviewBlocker.UnsafeDiagnosticsSurface);
        }

        var sectionTitles = diagnostics.Sections.Select(section => section.Title).ToHashSet(StringComparer.Ordinal);
        if (RequiredDiagnosticsSections.Any(section => !sectionTitles.Contains(section)))
        {
            blockers.Add(ProductLedgerInternalOperatorUiPreviewBlocker.MissingRequiredDiagnosticsSection);
        }
    }

    private static void AddCommandPreviewBlockers(
        IReadOnlyList<ProductLedgerInternalCommandPreviewResult>? previews,
        List<ProductLedgerInternalOperatorUiPreviewBlocker> blockers)
    {
        if (previews is null)
        {
            return;
        }

        if (previews.Count == 0
            || previews.Any(result => result.Decision != ProductLedgerInternalCommandPreviewDecision.PreviewedNoOpReadOnly
                || result.Blockers.Count > 0
                || !result.LocalOnly
                || !result.InternalOnly
                || !result.NoOp
                || !result.ReadOnly
                || !result.NonDestructive
                || !result.FailClosed
                || result.PublicUiActionAvailable
                || result.DestructiveActionAvailable
                || result.ProductCommandHandlerAvailable
                || result.ProductiveServiceRegistrationAvailable
                || result.ProviderCloudNetworkAvailable
                || result.DbMigrationAvailable
                || result.KmsWormExternalTrustAvailable
                || result.BrowserCdpWcuOcrRecipesLiveAvailable
                || result.ExternalTelemetryOrSyncAvailable
                || result.BillingLicensingCloudAvailable
                || result.ReleaseCommercialReady
                || !result.Preview.Disabled
                || result.Preview.Executable
                || result.Preview.ProductiveCommandId is not null
                || result.Preview.HandlerId is not null
                || result.Preview.CallbackName is not null))
        {
            blockers.Add(ProductLedgerInternalOperatorUiPreviewBlocker.UnsafeCommandPreviewRouter);
        }
    }

    private static ProductLedgerInternalOperatorUiPreviewResult Result(
        IReadOnlyList<ProductLedgerInternalOperatorUiPreviewBlocker> blockers,
        ProductLedgerInternalOperatorUiPreviewRequest? request)
    {
        var distinct = blockers.Distinct().OrderBy(blocker => blocker.ToString(), StringComparer.Ordinal).ToArray();
        var rendered = distinct.Length == 0 && request?.Diagnostics is not null;
        var viewModel = rendered ? ReadyViewModel(request!.Diagnostics!, request.CommandPreviews) : BlockedViewModel(distinct);

        return new ProductLedgerInternalOperatorUiPreviewResult(
            Decision: rendered
                ? ProductLedgerInternalOperatorUiPreviewDecision.RenderedPreview
                : ProductLedgerInternalOperatorUiPreviewDecision.Rejected,
            Blockers: distinct,
            ViewModel: viewModel);
    }

    private static ProductLedgerInternalOperatorUiPreviewViewModel ReadyViewModel(
        ProductLedgerLocalOnlyOperatorDiagnosticsResult diagnostics,
        IReadOnlyList<ProductLedgerInternalCommandPreviewResult>? commandPreviews)
    {
        var sections = RequiredDiagnosticsSections
            .Select(title => FromDiagnosticsSection(diagnostics.Sections.Single(section => section.Title == title)))
            .ToArray();
        return ViewModel(
            rendered: true,
            headerStatus: "LOCAL_ONLY_INTERNAL_READ_ONLY_PREVIEW",
            readinessPercentage: 82,
            sections: sections,
            actionPreviews: commandPreviews is null ? ActionPreviews() : FromCommandPreviews(commandPreviews),
            blockers: [],
            warnings:
            [
                "Preview is internal-only and local-only.",
                "Checkpoint/head evidence is same-boundary local trust only.",
                "Action previews are disabled labels without execution authority."
            ]);
    }

    private static ProductLedgerInternalOperatorUiPreviewViewModel BlockedViewModel(
        IReadOnlyList<ProductLedgerInternalOperatorUiPreviewBlocker> blockers) =>
        ViewModel(
            rendered: false,
            headerStatus: "FAIL_CLOSED",
            readinessPercentage: 0,
        sections:
            [
                new(
                    "header",
                    "Header",
                    "FAIL_CLOSED",
                    ["Product Ledger Local-Only", "internal-only", "read-only", "no release/commercial"],
                    ProductLedgerInternalOperatorUiPreviewSeverity.Blocker),
                new(
                    "blockers",
                    "Blockers / Warnings",
                    "BLOCKED",
                    blockers.Select(blocker => blocker.ToString()).ToArray(),
                    ProductLedgerInternalOperatorUiPreviewSeverity.Blocker),
                new(
                    "disabled-actions",
                    "Disabled Actions",
                    "ALL_ACTIONS_DISABLED",
                    DisabledActionLabels(),
                    ProductLedgerInternalOperatorUiPreviewSeverity.Blocker),
                new(
                    "safe-next-step",
                    "Safe Next Step",
                    "FIX_BLOCKERS_THEN_READ_ONLY_AUDIT",
                    SafeNextStepLabels(),
                    ProductLedgerInternalOperatorUiPreviewSeverity.Blocker)
            ],
            actionPreviews: ActionPreviews(),
            blockers: blockers.Select(blocker => blocker.ToString()).ToArray(),
            warnings: ["Fail-closed preview hides execution authority."]);

    private static ProductLedgerInternalOperatorUiPreviewSection FromDiagnosticsSection(
        ProductLedgerLocalOnlyOperatorDiagnosticsSection section) =>
        new(
            SectionId(section.Title),
            section.Title,
            section.Status,
            section.Lines,
            section.Severity switch
            {
                ProductLedgerLocalOnlyOperatorDiagnosticsSeverity.Blocker => ProductLedgerInternalOperatorUiPreviewSeverity.Blocker,
                ProductLedgerLocalOnlyOperatorDiagnosticsSeverity.Warning => ProductLedgerInternalOperatorUiPreviewSeverity.Warning,
                _ => ProductLedgerInternalOperatorUiPreviewSeverity.Info
            });

    private static ProductLedgerInternalOperatorUiPreviewViewModel ViewModel(
        bool rendered,
        string headerStatus,
        int readinessPercentage,
        IReadOnlyList<ProductLedgerInternalOperatorUiPreviewSection> sections,
        IReadOnlyList<ProductLedgerInternalOperatorUiPreviewActionPreview> actionPreviews,
        IReadOnlyList<string> blockers,
        IReadOnlyList<string> warnings) =>
        new(
            ViewModelId: "product-ledger.internal-operator-ui.read-only-preview.v1",
            Header: new ProductLedgerInternalOperatorUiPreviewHeader(
                Title: "Product Ledger Local-Only",
                Status: headerStatus,
                ReadinessPercentage: readinessPercentage,
                Notices:
                [
                    "local-only",
                    "internal-only",
                    "read-only preview",
                    "no public UI action",
                    "no destructive action",
                    "no provider/cloud/KMS/WORM/external trust",
                    "no release/commercial"
                ]),
            Sections: sections,
            ActionPreviews: actionPreviews,
            Blockers: blockers,
            Warnings: warnings,
            SafeNextStep: rendered
                ? "READ_ONLY_AUDIT_OR_PROPERTY_CORPUS_OR_STATIC_SCAN_HARDENING"
                : "FIX_BLOCKERS_BEFORE_INTERNAL_OPERATOR_UI_PREVIEW",
            LocalOnly: true,
            InternalOnly: true,
            ReadOnly: true,
            FailClosed: true,
            PublicUiActionAvailable: false,
            DestructiveUserFacingActionAvailable: false,
            ProductCommandHandlerAvailable: false,
            ProductiveServiceRegistrationAvailable: false,
            ProviderCloudNetworkAvailable: false,
            DbMigrationAvailable: false,
            KmsWormExternalTrustAvailable: false,
            BrowserCdpWcuOcrRecipesLiveAvailable: false,
            ExternalTelemetryOrSyncAvailable: false,
            BillingLicensingCloudAvailable: false,
            ReleaseCommercialReady: false,
            StatusText: rendered ? ReadyStatus : RejectedStatus);

    private static IReadOnlyList<ProductLedgerInternalOperatorUiPreviewActionPreview> ActionPreviews() =>
        DisabledActionLabels()
            .Select(label => new ProductLedgerInternalOperatorUiPreviewActionPreview(
                ActionId: SectionId(label),
                Label: label,
                RiskLabel: "disabled-preview-only",
                BlockedReason: "Internal operator UI preview is read-only and exposes no executable handler.",
                RequiredEvidence: ["explicit human GO", "boundary audit", "safety tests"],
                Disabled: true,
                ProductiveCommandId: null,
                HandlerId: null,
                CallbackName: null))
            .ToArray();

    private static IReadOnlyList<ProductLedgerInternalOperatorUiPreviewActionPreview> FromCommandPreviews(
        IReadOnlyList<ProductLedgerInternalCommandPreviewResult> commandPreviews) =>
        commandPreviews
            .Select(result => new ProductLedgerInternalOperatorUiPreviewActionPreview(
                ActionId: result.Preview.CommandId,
                Label: result.Preview.Label,
                RiskLabel: result.Preview.RiskLevel,
                BlockedReason: result.Preview.BlockedReason,
                RequiredEvidence: result.Preview.RequiredEvidence,
                Disabled: true,
                ProductiveCommandId: null,
                HandlerId: null,
                CallbackName: null))
            .ToArray();

    private static IReadOnlyList<string> DisabledActionLabels() =>
    [
        "enable public UI",
        "run destructive action",
        "register command handler",
        "connect provider/cloud",
        "create DB migration",
        "enable KMS/WORM",
        "enable Browser/CDP/WCU/OCR/Recipes live",
        "release/commercial"
    ];

    private static IReadOnlyList<string> SafeNextStepLabels() =>
    [
        "read-only audit",
        "property/corpus hardening",
        "static scan hardening",
        "operator docs",
        "manual external review packet"
    ];

    private static string SectionId(string value) =>
        string.Join(
            "-",
            value.ToLowerInvariant()
                .Replace("/", " ", StringComparison.Ordinal)
                .Replace(":", " ", StringComparison.Ordinal)
                .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
}
