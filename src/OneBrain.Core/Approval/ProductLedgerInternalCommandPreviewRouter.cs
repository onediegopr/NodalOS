namespace OneBrain.Core.Approval;

public enum ProductLedgerInternalCommandPreviewDecision
{
    Rejected,
    PreviewedNoOpReadOnly
}

public enum ProductLedgerInternalCommandKind
{
    ViewDiagnostics,
    ViewLedgerReadiness,
    ViewRuntimeGateStatus,
    ViewCheckpointHeadStatus,
    ViewEvidenceGates,
    ExportDisabledLocalReportPreview,
    RequestExternalAuditPreview,
    StaticScanPreview,
    PropertyCorpusHardeningPreview,
    EnablePublicUi,
    ExecuteAction,
    DestructiveWrite,
    RegisterCommandHandler,
    RegisterProductDI,
    ConnectProvider,
    EnableCloud,
    RunMigration,
    EnableKms,
    EnableWorm,
    EnableExternalTrust,
    RunBrowserCdp,
    RunWcu,
    RunOcr,
    RunRecipesLive,
    Release,
    CommercialLaunch,
    SyncExternal,
    TelemetryExternal,
    BillingLicensingCloud
}

public enum ProductLedgerInternalCommandPreviewBlocker
{
    MissingRequest,
    MissingExplicitInternalLocalOnlyNoOpReadOnlyScope,
    CorruptCommand,
    UnknownCommand,
    PublicUiActionRequested,
    DestructiveActionRequested,
    ProductCommandHandlerRequested,
    ProductiveServiceRegistrationRequested,
    ProviderCloudNetworkClaimed,
    DbMigrationClaimed,
    KmsWormExternalTrustClaimed,
    BrowserCdpWcuOcrRecipesLiveClaimed,
    ReleaseCommercialClaimed,
    ExternalTelemetryOrSyncClaimed,
    BillingLicensingCloudClaimed,
    WriterExecutionOutsideValidatedLocalOnlyPolicyClaimed
}

public sealed record ProductLedgerInternalCommandPreviewRequest(
    bool ExplicitInternalLocalOnlyNoOpReadOnlyScope,
    ProductLedgerInternalCommandKind? CommandKind,
    string? RawCommandName,
    ProductLedgerInternalOperatorUiPreviewViewModel? SourcePreview,
    bool RequestsPublicUiAction,
    bool RequestsDestructiveAction,
    bool RequestsProductCommandHandler,
    bool RequestsProductiveServiceRegistration,
    bool ClaimsProviderCloudNetwork,
    bool ClaimsDbMigration,
    bool ClaimsKmsWormExternalTrust,
    bool ClaimsBrowserCdpWcuOcrRecipesLive,
    bool ClaimsReleaseCommercial,
    bool ClaimsExternalTelemetryOrSync,
    bool ClaimsBillingLicensingCloud,
    bool ClaimsWriterExecutionOutsideValidatedLocalOnlyPolicy);

public sealed record ProductLedgerInternalCommandPreview(
    ProductLedgerInternalCommandKind CommandKind,
    string CommandId,
    string Label,
    string Eligibility,
    string BlockedReason,
    string RiskLevel,
    IReadOnlyList<string> RequiredEvidence,
    bool Disabled,
    bool Executable,
    string? ProductiveCommandId,
    string? HandlerId,
    string? CallbackName,
    string SafeNextStep);

public sealed record ProductLedgerInternalCommandPreviewResult(
    ProductLedgerInternalCommandPreviewDecision Decision,
    IReadOnlyList<ProductLedgerInternalCommandPreviewBlocker> Blockers,
    ProductLedgerInternalCommandPreview Preview,
    bool LocalOnly,
    bool InternalOnly,
    bool NoOp,
    bool ReadOnly,
    bool NonDestructive,
    bool FailClosed,
    bool PublicUiActionAvailable,
    bool DestructiveActionAvailable,
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

public sealed class ProductLedgerInternalCommandPreviewRouter
{
    public const string ReadyStatus =
        "PRODUCT_LEDGER_INTERNAL_COMMAND_ROUTER_NOOP_READ_ONLY_READY INTERNAL_ONLY LOCAL_ONLY NO_OP READ_ONLY PREVIEW_ONLY ACTIONS_DISABLED NO_PRODUCT_COMMAND_HANDLER NO_PUBLIC_UI_ACTION NO_DESTRUCTIVE_ACTION NO_PROVIDER_CLOUD_NETWORK NO_DB_MIGRATION NO_KMS_WORM_EXTERNAL_TRUST NO_LIVE_AUTOMATION NO_RELEASE_COMMERCIAL";

    public const string RejectedStatus =
        "PRODUCT_LEDGER_INTERNAL_COMMAND_ROUTER_NOOP_READ_ONLY_REJECTED FAIL_CLOSED ACTIONS_DISABLED NO_PRODUCT_COMMAND_HANDLER NO_PUBLIC_UI_ACTION NO_DESTRUCTIVE_ACTION NO_PROVIDER_CLOUD_NETWORK NO_DB_MIGRATION NO_KMS_WORM_EXTERNAL_TRUST NO_LIVE_AUTOMATION NO_RELEASE_COMMERCIAL";

    private static readonly ProductLedgerInternalCommandKind[] AllowedCommands =
    [
        ProductLedgerInternalCommandKind.ViewDiagnostics,
        ProductLedgerInternalCommandKind.ViewLedgerReadiness,
        ProductLedgerInternalCommandKind.ViewRuntimeGateStatus,
        ProductLedgerInternalCommandKind.ViewCheckpointHeadStatus,
        ProductLedgerInternalCommandKind.ViewEvidenceGates,
        ProductLedgerInternalCommandKind.ExportDisabledLocalReportPreview,
        ProductLedgerInternalCommandKind.RequestExternalAuditPreview,
        ProductLedgerInternalCommandKind.StaticScanPreview,
        ProductLedgerInternalCommandKind.PropertyCorpusHardeningPreview
    ];

    public ProductLedgerInternalCommandPreviewResult Preview(ProductLedgerInternalCommandPreviewRequest? request)
    {
        var blockers = new List<ProductLedgerInternalCommandPreviewBlocker>();
        if (request is null)
        {
            blockers.Add(ProductLedgerInternalCommandPreviewBlocker.MissingRequest);
            return Result(blockers, ProductLedgerInternalCommandKind.ViewDiagnostics);
        }

        AddRequestBlockers(request, blockers);
        var command = ResolveCommand(request, blockers);
        return Result(blockers, command);
    }

    public IReadOnlyList<ProductLedgerInternalCommandPreviewResult> PreviewAllowedCommands(
        ProductLedgerInternalOperatorUiPreviewViewModel sourcePreview) =>
        AllowedCommands
            .Select(command => Preview(new ProductLedgerInternalCommandPreviewRequest(
                ExplicitInternalLocalOnlyNoOpReadOnlyScope: true,
                CommandKind: command,
                RawCommandName: command.ToString(),
                SourcePreview: sourcePreview,
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
                ClaimsWriterExecutionOutsideValidatedLocalOnlyPolicy: false)))
            .ToArray();

    private static ProductLedgerInternalCommandKind ResolveCommand(
        ProductLedgerInternalCommandPreviewRequest request,
        List<ProductLedgerInternalCommandPreviewBlocker> blockers)
    {
        if (request.CommandKind is null || string.IsNullOrWhiteSpace(request.RawCommandName))
        {
            blockers.Add(ProductLedgerInternalCommandPreviewBlocker.CorruptCommand);
            return ProductLedgerInternalCommandKind.ViewDiagnostics;
        }

        if (!Enum.IsDefined(request.CommandKind.Value)
            || !string.Equals(request.RawCommandName.Trim(), request.CommandKind.Value.ToString(), StringComparison.Ordinal))
        {
            blockers.Add(ProductLedgerInternalCommandPreviewBlocker.UnknownCommand);
            return request.CommandKind.Value;
        }

        if (!AllowedCommands.Contains(request.CommandKind.Value))
        {
            AddBlockedCommandBlocker(request.CommandKind.Value, blockers);
        }

        return request.CommandKind.Value;
    }

    private static void AddRequestBlockers(
        ProductLedgerInternalCommandPreviewRequest request,
        List<ProductLedgerInternalCommandPreviewBlocker> blockers)
    {
        if (!request.ExplicitInternalLocalOnlyNoOpReadOnlyScope)
        {
            blockers.Add(ProductLedgerInternalCommandPreviewBlocker.MissingExplicitInternalLocalOnlyNoOpReadOnlyScope);
        }

        if (request.SourcePreview is not null && (!request.SourcePreview.LocalOnly
            || !request.SourcePreview.InternalOnly
            || !request.SourcePreview.ReadOnly
            || !request.SourcePreview.FailClosed
            || request.SourcePreview.PublicUiActionAvailable
            || request.SourcePreview.DestructiveUserFacingActionAvailable
            || request.SourcePreview.ProductCommandHandlerAvailable
            || request.SourcePreview.ProductiveServiceRegistrationAvailable
            || request.SourcePreview.ProviderCloudNetworkAvailable
            || request.SourcePreview.DbMigrationAvailable
            || request.SourcePreview.KmsWormExternalTrustAvailable
            || request.SourcePreview.BrowserCdpWcuOcrRecipesLiveAvailable
            || request.SourcePreview.ReleaseCommercialReady))
        {
            blockers.Add(ProductLedgerInternalCommandPreviewBlocker.PublicUiActionRequested);
        }

        if (request.RequestsPublicUiAction)
        {
            blockers.Add(ProductLedgerInternalCommandPreviewBlocker.PublicUiActionRequested);
        }

        if (request.RequestsDestructiveAction)
        {
            blockers.Add(ProductLedgerInternalCommandPreviewBlocker.DestructiveActionRequested);
        }

        if (request.RequestsProductCommandHandler)
        {
            blockers.Add(ProductLedgerInternalCommandPreviewBlocker.ProductCommandHandlerRequested);
        }

        if (request.RequestsProductiveServiceRegistration)
        {
            blockers.Add(ProductLedgerInternalCommandPreviewBlocker.ProductiveServiceRegistrationRequested);
        }

        if (request.ClaimsProviderCloudNetwork)
        {
            blockers.Add(ProductLedgerInternalCommandPreviewBlocker.ProviderCloudNetworkClaimed);
        }

        if (request.ClaimsDbMigration)
        {
            blockers.Add(ProductLedgerInternalCommandPreviewBlocker.DbMigrationClaimed);
        }

        if (request.ClaimsKmsWormExternalTrust)
        {
            blockers.Add(ProductLedgerInternalCommandPreviewBlocker.KmsWormExternalTrustClaimed);
        }

        if (request.ClaimsBrowserCdpWcuOcrRecipesLive)
        {
            blockers.Add(ProductLedgerInternalCommandPreviewBlocker.BrowserCdpWcuOcrRecipesLiveClaimed);
        }

        if (request.ClaimsReleaseCommercial)
        {
            blockers.Add(ProductLedgerInternalCommandPreviewBlocker.ReleaseCommercialClaimed);
        }

        if (request.ClaimsExternalTelemetryOrSync)
        {
            blockers.Add(ProductLedgerInternalCommandPreviewBlocker.ExternalTelemetryOrSyncClaimed);
        }

        if (request.ClaimsBillingLicensingCloud)
        {
            blockers.Add(ProductLedgerInternalCommandPreviewBlocker.BillingLicensingCloudClaimed);
        }

        if (request.ClaimsWriterExecutionOutsideValidatedLocalOnlyPolicy)
        {
            blockers.Add(ProductLedgerInternalCommandPreviewBlocker.WriterExecutionOutsideValidatedLocalOnlyPolicyClaimed);
        }
    }

    private static void AddBlockedCommandBlocker(
        ProductLedgerInternalCommandKind command,
        List<ProductLedgerInternalCommandPreviewBlocker> blockers)
    {
        switch (command)
        {
            case ProductLedgerInternalCommandKind.EnablePublicUi:
                blockers.Add(ProductLedgerInternalCommandPreviewBlocker.PublicUiActionRequested);
                break;
            case ProductLedgerInternalCommandKind.ExecuteAction:
            case ProductLedgerInternalCommandKind.DestructiveWrite:
                blockers.Add(ProductLedgerInternalCommandPreviewBlocker.DestructiveActionRequested);
                break;
            case ProductLedgerInternalCommandKind.RegisterCommandHandler:
                blockers.Add(ProductLedgerInternalCommandPreviewBlocker.ProductCommandHandlerRequested);
                break;
            case ProductLedgerInternalCommandKind.RegisterProductDI:
                blockers.Add(ProductLedgerInternalCommandPreviewBlocker.ProductiveServiceRegistrationRequested);
                break;
            case ProductLedgerInternalCommandKind.ConnectProvider:
            case ProductLedgerInternalCommandKind.EnableCloud:
                blockers.Add(ProductLedgerInternalCommandPreviewBlocker.ProviderCloudNetworkClaimed);
                break;
            case ProductLedgerInternalCommandKind.RunMigration:
                blockers.Add(ProductLedgerInternalCommandPreviewBlocker.DbMigrationClaimed);
                break;
            case ProductLedgerInternalCommandKind.EnableKms:
            case ProductLedgerInternalCommandKind.EnableWorm:
            case ProductLedgerInternalCommandKind.EnableExternalTrust:
                blockers.Add(ProductLedgerInternalCommandPreviewBlocker.KmsWormExternalTrustClaimed);
                break;
            case ProductLedgerInternalCommandKind.RunBrowserCdp:
            case ProductLedgerInternalCommandKind.RunWcu:
            case ProductLedgerInternalCommandKind.RunOcr:
            case ProductLedgerInternalCommandKind.RunRecipesLive:
                blockers.Add(ProductLedgerInternalCommandPreviewBlocker.BrowserCdpWcuOcrRecipesLiveClaimed);
                break;
            case ProductLedgerInternalCommandKind.Release:
            case ProductLedgerInternalCommandKind.CommercialLaunch:
                blockers.Add(ProductLedgerInternalCommandPreviewBlocker.ReleaseCommercialClaimed);
                break;
            case ProductLedgerInternalCommandKind.SyncExternal:
            case ProductLedgerInternalCommandKind.TelemetryExternal:
                blockers.Add(ProductLedgerInternalCommandPreviewBlocker.ExternalTelemetryOrSyncClaimed);
                break;
            case ProductLedgerInternalCommandKind.BillingLicensingCloud:
                blockers.Add(ProductLedgerInternalCommandPreviewBlocker.BillingLicensingCloudClaimed);
                break;
        }
    }

    private static ProductLedgerInternalCommandPreviewResult Result(
        IReadOnlyList<ProductLedgerInternalCommandPreviewBlocker> blockers,
        ProductLedgerInternalCommandKind command)
    {
        var distinct = blockers.Distinct().OrderBy(blocker => blocker.ToString(), StringComparer.Ordinal).ToArray();
        var previewed = distinct.Length == 0 && AllowedCommands.Contains(command);
        return new ProductLedgerInternalCommandPreviewResult(
            Decision: previewed
                ? ProductLedgerInternalCommandPreviewDecision.PreviewedNoOpReadOnly
                : ProductLedgerInternalCommandPreviewDecision.Rejected,
            Blockers: distinct,
            Preview: CreatePreview(command, previewed),
            LocalOnly: true,
            InternalOnly: true,
            NoOp: true,
            ReadOnly: true,
            NonDestructive: true,
            FailClosed: true,
            PublicUiActionAvailable: false,
            DestructiveActionAvailable: false,
            ProductCommandHandlerAvailable: false,
            ProductiveServiceRegistrationAvailable: false,
            ProviderCloudNetworkAvailable: false,
            DbMigrationAvailable: false,
            KmsWormExternalTrustAvailable: false,
            BrowserCdpWcuOcrRecipesLiveAvailable: false,
            ExternalTelemetryOrSyncAvailable: false,
            BillingLicensingCloudAvailable: false,
            ReleaseCommercialReady: false,
            StatusText: previewed ? ReadyStatus : RejectedStatus);
    }

    private static ProductLedgerInternalCommandPreview CreatePreview(
        ProductLedgerInternalCommandKind command,
        bool eligible) =>
        new(
            CommandKind: command,
            CommandId: $"preview-only.product-ledger.{ToKebab(command.ToString())}",
            Label: Label(command),
            Eligibility: eligible ? "eligible-as-disabled-preview-only" : "blocked-fail-closed",
            BlockedReason: eligible
                ? "Preview is intentionally disabled and no-op/read-only."
                : "Command is blocked before any handler, callback or execution authority exists.",
            RiskLevel: eligible ? "low-preview-only" : "blocked",
            RequiredEvidence: RequiredEvidence(command),
            Disabled: true,
            Executable: false,
            ProductiveCommandId: null,
            HandlerId: null,
            CallbackName: null,
            SafeNextStep: eligible
                ? "READ_ONLY_AUDIT_OR_STATIC_SCAN_HARDENING"
                : "FIX_BLOCKERS_BEFORE_COMMAND_PREVIEW");

    private static IReadOnlyList<string> RequiredEvidence(ProductLedgerInternalCommandKind command) =>
        command switch
        {
            ProductLedgerInternalCommandKind.ViewDiagnostics => ["operator diagnostics surface", "read-only audit"],
            ProductLedgerInternalCommandKind.ViewLedgerReadiness => ["active path policy", "bounded writer tests"],
            ProductLedgerInternalCommandKind.ViewRuntimeGateStatus => ["runtime local-only gate", "default-off evidence"],
            ProductLedgerInternalCommandKind.ViewCheckpointHeadStatus => ["checkpoint/head evidence", "same-boundary limitation notice"],
            ProductLedgerInternalCommandKind.ViewEvidenceGates => ["redaction", "retention", "authority", "failure/replay", "rollback/non-rollback"],
            ProductLedgerInternalCommandKind.ExportDisabledLocalReportPreview => ["read-only report preview policy", "no physical export"],
            ProductLedgerInternalCommandKind.RequestExternalAuditPreview => ["external audit read-only packet"],
            ProductLedgerInternalCommandKind.StaticScanPreview => ["static no-public-command scan"],
            ProductLedgerInternalCommandKind.PropertyCorpusHardeningPreview => ["property/corpus hardening plan"],
            _ => ["new explicit GO", "boundary audit", "safety tests"]
        };

    private static string Label(ProductLedgerInternalCommandKind command) =>
        command switch
        {
            ProductLedgerInternalCommandKind.ViewDiagnostics => "View diagnostics",
            ProductLedgerInternalCommandKind.ViewLedgerReadiness => "View ledger readiness",
            ProductLedgerInternalCommandKind.ViewRuntimeGateStatus => "View runtime gate status",
            ProductLedgerInternalCommandKind.ViewCheckpointHeadStatus => "View checkpoint/head status",
            ProductLedgerInternalCommandKind.ViewEvidenceGates => "View evidence gates",
            ProductLedgerInternalCommandKind.ExportDisabledLocalReportPreview => "Export disabled local report preview",
            ProductLedgerInternalCommandKind.RequestExternalAuditPreview => "Request external audit preview",
            ProductLedgerInternalCommandKind.StaticScanPreview => "Static scan preview",
            ProductLedgerInternalCommandKind.PropertyCorpusHardeningPreview => "Property/corpus hardening preview",
            _ => $"{command} blocked"
        };

    private static string ToKebab(string value) =>
        string.Concat(value.Select((ch, index) =>
            index > 0 && char.IsUpper(ch) ? "-" + char.ToLowerInvariant(ch) : char.ToLowerInvariant(ch).ToString()));
}
