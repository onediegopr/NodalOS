namespace OneBrain.Core.Approval;

public enum ProductLedgerLocalApprovalExecutionDecision
{
    Rejected,
    CompletedReadOnlyInMemory
}

public enum ProductLedgerLocalApprovalExecutionBlocker
{
    MissingRequest,
    MissingExplicitLocalOnlyInternalApprovalExecutionScope,
    MissingApproval,
    ApprovalExpiredOrNotYetValid,
    ActionMismatch,
    EvidenceMismatch,
    PolicyRecheckFailed,
    ReadModelNotVerified,
    CommandNotAllowedForApprovalExecution,
    RouterPreviewRejected,
    CommandHandlerRejected,
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
    PhysicalExportRequested,
    FileWriteRequested,
    AppendOutsideBoundedWriterClaimed,
    ArbitraryPathInputClaimed,
    RawPayloadOrSecretClaimed
}

public sealed record ProductLedgerLocalApprovalExecutionRequest(
    bool ExplicitLocalOnlyInternalApprovalExecutionScope,
    string? ApprovalId,
    DateTimeOffset? ApprovedAtUtc,
    DateTimeOffset? NowUtc,
    TimeSpan? MaxApprovalAge,
    ProductLedgerInternalCommandKind? CandidateActionKind,
    string? ApprovedActionName,
    string? ApprovedEvidenceHash,
    string? CurrentEvidenceHash,
    bool PolicyRecheckPassed,
    bool ReadModelVerified,
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
    bool RequestsPhysicalExport,
    bool RequestsFileWrite,
    bool ClaimsAppendOutsideBoundedWriter,
    bool ClaimsArbitraryPathInput,
    bool ClaimsRawPayloadOrSecret);

public sealed record ProductLedgerLocalApprovalExecutionResult(
    ProductLedgerLocalApprovalExecutionDecision Decision,
    IReadOnlyList<ProductLedgerLocalApprovalExecutionBlocker> Blockers,
    ProductLedgerInternalCommandKind CandidateActionKind,
    ProductLedgerInternalCommandResult? CommandResult,
    bool LocalOnly,
    bool InternalOnly,
    bool DefaultOff,
    bool FailClosed,
    bool ReadOnlyOrInMemory,
    bool PublicUiActionAvailable,
    bool ProductCommandHandlerAvailable,
    bool ProductiveServiceRegistrationAvailable,
    bool PhysicalExportCreated,
    bool FileWritePerformed,
    bool ProviderCloudNetworkAvailable,
    bool DbMigrationAvailable,
    bool KmsWormExternalTrustAvailable,
    bool BrowserCdpWcuOcrRecipesLiveAvailable,
    bool ReleaseCommercialReady,
    string StatusText);

public sealed class ProductLedgerLocalApprovalExecutionCandidate
{
    public const string ReadyStatus =
        "PRODUCT_LEDGER_LOCAL_APPROVAL_EXECUTION_CANDIDATE_READY LOCAL_ONLY INTERNAL_ONLY DEFAULT_OFF READ_ONLY_IN_MEMORY FAIL_CLOSED NO_PUBLIC_UI_ACTION NO_PRODUCT_COMMAND_HANDLER NO_WRITE_EXPORT NO_PROVIDER_CLOUD_NETWORK NO_DB_MIGRATION NO_KMS_WORM_EXTERNAL_TRUST NO_LIVE_AUTOMATION NO_RELEASE_COMMERCIAL";

    public const string RejectedStatus =
        "PRODUCT_LEDGER_LOCAL_APPROVAL_EXECUTION_CANDIDATE_REJECTED FAIL_CLOSED DEFAULT_OFF NO_PUBLIC_UI_ACTION NO_PRODUCT_COMMAND_HANDLER NO_WRITE_EXPORT NO_PROVIDER_CLOUD_NETWORK NO_DB_MIGRATION NO_KMS_WORM_EXTERNAL_TRUST NO_LIVE_AUTOMATION NO_RELEASE_COMMERCIAL";

    private static readonly ProductLedgerInternalCommandKind[] ApprovalExecutionAllowedCommands =
    [
        ProductLedgerInternalCommandKind.ViewDiagnostics,
        ProductLedgerInternalCommandKind.ViewLedgerReadiness,
        ProductLedgerInternalCommandKind.ViewRuntimeGateStatus,
        ProductLedgerInternalCommandKind.ViewCheckpointHeadStatus,
        ProductLedgerInternalCommandKind.ViewEvidenceGates,
        ProductLedgerInternalCommandKind.StaticScanPreview,
        ProductLedgerInternalCommandKind.RequestExternalAuditPreview
    ];

    public ProductLedgerLocalApprovalExecutionResult Execute(ProductLedgerLocalApprovalExecutionRequest? request)
    {
        var blockers = new List<ProductLedgerLocalApprovalExecutionBlocker>();
        if (request is null)
        {
            blockers.Add(ProductLedgerLocalApprovalExecutionBlocker.MissingRequest);
            return Result(blockers, ProductLedgerInternalCommandKind.ViewDiagnostics, null);
        }

        AddRequestBlockers(request, blockers);
        var command = ResolveCommand(request, blockers);
        ProductLedgerInternalCommandResult? commandResult = null;

        if (blockers.Count == 0)
        {
            var routerPreview = new ProductLedgerInternalCommandPreviewRouter().Preview(RouterRequest(command));
            if (routerPreview.Decision != ProductLedgerInternalCommandPreviewDecision.PreviewedNoOpReadOnly
                || routerPreview.Blockers.Count > 0
                || routerPreview.Preview.Executable
                || !routerPreview.ReadOnly
                || !routerPreview.NonDestructive)
            {
                blockers.Add(ProductLedgerLocalApprovalExecutionBlocker.RouterPreviewRejected);
            }
            else
            {
                commandResult = new ProductLedgerInternalCommandHandler().Execute(CommandRequest(routerPreview));
                if (commandResult.Decision != ProductLedgerInternalCommandDecision.CompletedReadOnlyInMemory
                    || commandResult.Blockers.Count > 0
                    || commandResult.PhysicalExportCreated
                    || commandResult.FileWritePerformed
                    || !commandResult.ReadOnlyOrInMemory)
                {
                    blockers.Add(ProductLedgerLocalApprovalExecutionBlocker.CommandHandlerRejected);
                }
            }
        }

        return Result(blockers, command, commandResult);
    }

    public static IReadOnlyList<ProductLedgerInternalCommandKind> AllowedCommands() =>
        ApprovalExecutionAllowedCommands.ToArray();

    private static void AddRequestBlockers(
        ProductLedgerLocalApprovalExecutionRequest request,
        List<ProductLedgerLocalApprovalExecutionBlocker> blockers)
    {
        if (!request.ExplicitLocalOnlyInternalApprovalExecutionScope)
        {
            blockers.Add(ProductLedgerLocalApprovalExecutionBlocker.MissingExplicitLocalOnlyInternalApprovalExecutionScope);
        }

        if (string.IsNullOrWhiteSpace(request.ApprovalId)
            || string.IsNullOrWhiteSpace(request.ApprovedActionName)
            || string.IsNullOrWhiteSpace(request.ApprovedEvidenceHash)
            || string.IsNullOrWhiteSpace(request.CurrentEvidenceHash)
            || request.ApprovedAtUtc is null
            || request.NowUtc is null)
        {
            blockers.Add(ProductLedgerLocalApprovalExecutionBlocker.MissingApproval);
        }
        else
        {
            var maxAge = request.MaxApprovalAge ?? TimeSpan.FromMinutes(5);
            var age = request.NowUtc.Value - request.ApprovedAtUtc.Value;
            if (age < TimeSpan.Zero || age > maxAge)
            {
                blockers.Add(ProductLedgerLocalApprovalExecutionBlocker.ApprovalExpiredOrNotYetValid);
            }

            if (!string.Equals(request.ApprovedEvidenceHash.Trim(), request.CurrentEvidenceHash.Trim(), StringComparison.Ordinal))
            {
                blockers.Add(ProductLedgerLocalApprovalExecutionBlocker.EvidenceMismatch);
            }
        }

        if (request.CandidateActionKind is null
            || string.IsNullOrWhiteSpace(request.ApprovedActionName)
            || !string.Equals(request.ApprovedActionName.Trim(), request.CandidateActionKind.Value.ToString(), StringComparison.Ordinal))
        {
            blockers.Add(ProductLedgerLocalApprovalExecutionBlocker.ActionMismatch);
        }

        if (!request.PolicyRecheckPassed)
        {
            blockers.Add(ProductLedgerLocalApprovalExecutionBlocker.PolicyRecheckFailed);
        }

        if (!request.ReadModelVerified)
        {
            blockers.Add(ProductLedgerLocalApprovalExecutionBlocker.ReadModelNotVerified);
        }

        AddAuthorityBlockers(request, blockers);
    }

    private static void AddAuthorityBlockers(
        ProductLedgerLocalApprovalExecutionRequest request,
        List<ProductLedgerLocalApprovalExecutionBlocker> blockers)
    {
        if (request.RequestsPublicUiAction)
        {
            blockers.Add(ProductLedgerLocalApprovalExecutionBlocker.PublicUiActionRequested);
        }

        if (request.RequestsDestructiveAction)
        {
            blockers.Add(ProductLedgerLocalApprovalExecutionBlocker.DestructiveActionRequested);
        }

        if (request.RequestsProductCommandHandler)
        {
            blockers.Add(ProductLedgerLocalApprovalExecutionBlocker.ProductCommandHandlerRequested);
        }

        if (request.RequestsProductiveServiceRegistration)
        {
            blockers.Add(ProductLedgerLocalApprovalExecutionBlocker.ProductiveServiceRegistrationRequested);
        }

        if (request.ClaimsProviderCloudNetwork)
        {
            blockers.Add(ProductLedgerLocalApprovalExecutionBlocker.ProviderCloudNetworkClaimed);
        }

        if (request.ClaimsDbMigration)
        {
            blockers.Add(ProductLedgerLocalApprovalExecutionBlocker.DbMigrationClaimed);
        }

        if (request.ClaimsKmsWormExternalTrust)
        {
            blockers.Add(ProductLedgerLocalApprovalExecutionBlocker.KmsWormExternalTrustClaimed);
        }

        if (request.ClaimsBrowserCdpWcuOcrRecipesLive)
        {
            blockers.Add(ProductLedgerLocalApprovalExecutionBlocker.BrowserCdpWcuOcrRecipesLiveClaimed);
        }

        if (request.ClaimsReleaseCommercial)
        {
            blockers.Add(ProductLedgerLocalApprovalExecutionBlocker.ReleaseCommercialClaimed);
        }

        if (request.ClaimsExternalTelemetryOrSync)
        {
            blockers.Add(ProductLedgerLocalApprovalExecutionBlocker.ExternalTelemetryOrSyncClaimed);
        }

        if (request.ClaimsBillingLicensingCloud)
        {
            blockers.Add(ProductLedgerLocalApprovalExecutionBlocker.BillingLicensingCloudClaimed);
        }

        if (request.RequestsPhysicalExport)
        {
            blockers.Add(ProductLedgerLocalApprovalExecutionBlocker.PhysicalExportRequested);
        }

        if (request.RequestsFileWrite)
        {
            blockers.Add(ProductLedgerLocalApprovalExecutionBlocker.FileWriteRequested);
        }

        if (request.ClaimsAppendOutsideBoundedWriter)
        {
            blockers.Add(ProductLedgerLocalApprovalExecutionBlocker.AppendOutsideBoundedWriterClaimed);
        }

        if (request.ClaimsArbitraryPathInput)
        {
            blockers.Add(ProductLedgerLocalApprovalExecutionBlocker.ArbitraryPathInputClaimed);
        }

        if (request.ClaimsRawPayloadOrSecret)
        {
            blockers.Add(ProductLedgerLocalApprovalExecutionBlocker.RawPayloadOrSecretClaimed);
        }
    }

    private static ProductLedgerInternalCommandKind ResolveCommand(
        ProductLedgerLocalApprovalExecutionRequest request,
        List<ProductLedgerLocalApprovalExecutionBlocker> blockers)
    {
        var command = request.CandidateActionKind ?? ProductLedgerInternalCommandKind.ViewDiagnostics;
        if (!ApprovalExecutionAllowedCommands.Contains(command))
        {
            blockers.Add(ProductLedgerLocalApprovalExecutionBlocker.CommandNotAllowedForApprovalExecution);
        }

        return command;
    }

    private static ProductLedgerInternalCommandPreviewRequest RouterRequest(ProductLedgerInternalCommandKind command) =>
        new(
            ExplicitInternalLocalOnlyNoOpReadOnlyScope: true,
            CommandKind: command,
            RawCommandName: command.ToString(),
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
            ClaimsWriterExecutionOutsideValidatedLocalOnlyPolicy: false);

    private static ProductLedgerInternalCommandRequest CommandRequest(ProductLedgerInternalCommandPreviewResult routerPreview) =>
        new(
            ExplicitInternalLocalOnlyNonDestructiveScope: true,
            RouterPreview: routerPreview,
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
            ClaimsAppendOutsideBoundedWriter: false);

    private static ProductLedgerLocalApprovalExecutionResult Result(
        IReadOnlyList<ProductLedgerLocalApprovalExecutionBlocker> blockers,
        ProductLedgerInternalCommandKind command,
        ProductLedgerInternalCommandResult? commandResult)
    {
        var completed = blockers.Count == 0
            && commandResult is not null
            && commandResult.Decision == ProductLedgerInternalCommandDecision.CompletedReadOnlyInMemory;

        return new ProductLedgerLocalApprovalExecutionResult(
            Decision: completed
                ? ProductLedgerLocalApprovalExecutionDecision.CompletedReadOnlyInMemory
                : ProductLedgerLocalApprovalExecutionDecision.Rejected,
            Blockers: blockers.Distinct().OrderBy(blocker => blocker.ToString(), StringComparer.Ordinal).ToArray(),
            CandidateActionKind: command,
            CommandResult: commandResult,
            LocalOnly: true,
            InternalOnly: true,
            DefaultOff: true,
            FailClosed: true,
            ReadOnlyOrInMemory: completed,
            PublicUiActionAvailable: false,
            ProductCommandHandlerAvailable: false,
            ProductiveServiceRegistrationAvailable: false,
            PhysicalExportCreated: commandResult?.PhysicalExportCreated == true,
            FileWritePerformed: commandResult?.FileWritePerformed == true,
            ProviderCloudNetworkAvailable: false,
            DbMigrationAvailable: false,
            KmsWormExternalTrustAvailable: false,
            BrowserCdpWcuOcrRecipesLiveAvailable: false,
            ReleaseCommercialReady: false,
            StatusText: completed ? ReadyStatus : RejectedStatus);
    }
}

