namespace OneBrain.Core.Approval;

public enum ProductLedgerLocalOnlyOperatorDiagnosticsDecision
{
    Rejected,
    RenderedReadOnly
}

public enum ProductLedgerLocalOnlyOperatorDiagnosticsSeverity
{
    Info,
    Warning,
    Blocker
}

public enum ProductLedgerLocalOnlyOperatorDiagnosticsBlocker
{
    MissingRequest,
    MissingExplicitOperatorReadOnlyLocalOnlyScope,
    MissingRuntimeFlag,
    UnsafeRuntimeFlag,
    MissingActivationResult,
    UnsafeActivationResult,
    MissingRuntimeDiagnostics,
    UnsafeRuntimeDiagnostics,
    MissingAuthorityEvidence,
    MissingRedactionBeforePersistenceEvidence,
    MissingRetentionEvidence,
    MissingReplayFailureEvidence,
    MissingRollbackEvidence,
    StaleEvidenceReferences,
    MalformedEvidenceReferences,
    PublicUiActionRequested,
    DestructiveUserFacingActionRequested,
    ProductCommandHandlerRequested,
    ProductiveServiceRegistrationRequested,
    ProviderCloudNetworkClaimed,
    DbMigrationClaimed,
    KmsWormExternalTrustClaimed,
    BrowserCdpWcuOcrRecipesLiveClaimed,
    ReleaseCommercialClaimed
}

public sealed record ProductLedgerLocalOnlyOperatorDiagnosticsRequest(
    bool ExplicitOperatorReadOnlyLocalOnlyScope,
    ProductLedgerRuntimeLocalOnlyFlagResult? RuntimeFlag,
    ProductLedgerPathLocalOnlyActivationResult? ActivationResult,
    ProductLedgerRuntimeLocalOnlyAdapterResult? RuntimeDiagnostics,
    bool HasAuthorityEvidence,
    bool HasRedactionBeforePersistenceEvidence,
    bool HasRetentionEvidence,
    bool HasReplayFailureEvidence,
    bool HasRollbackEvidence,
    bool EvidenceReferencesFresh,
    bool EvidenceReferencesWellFormed,
    bool RequestsPublicUiAction,
    bool RequestsDestructiveUserFacingAction,
    bool RequestsProductCommandHandler,
    bool RequestsProductiveServiceRegistration,
    bool ClaimsProviderCloudNetwork,
    bool ClaimsDbMigration,
    bool ClaimsKmsWormExternalTrust,
    bool ClaimsBrowserCdpWcuOcrRecipesLive,
    bool ClaimsReleaseCommercial);

public sealed record ProductLedgerLocalOnlyOperatorDiagnosticsSection(
    string Title,
    string Status,
    IReadOnlyList<string> Lines,
    ProductLedgerLocalOnlyOperatorDiagnosticsSeverity Severity);

public sealed record ProductLedgerLocalOnlyOperatorDiagnosticsActionPreview(
    string Label,
    string Reason,
    string Risk,
    IReadOnlyList<string> RequiredEvidence,
    bool Disabled,
    string? ProductiveCommandId,
    string? HandlerName,
    string? CallbackName);

public sealed record ProductLedgerLocalOnlyOperatorDiagnosticsResult(
    ProductLedgerLocalOnlyOperatorDiagnosticsDecision Decision,
    IReadOnlyList<ProductLedgerLocalOnlyOperatorDiagnosticsBlocker> Blockers,
    IReadOnlyList<ProductLedgerLocalOnlyOperatorDiagnosticsSection> Sections,
    IReadOnlyList<ProductLedgerLocalOnlyOperatorDiagnosticsActionPreview> ActionPreviews,
    IReadOnlyList<string> DisabledActions,
    string SafeNextStep,
    bool ReadOnly,
    bool LocalOnly,
    bool InternalOnly,
    bool FailClosed,
    bool PublicUiActionAvailable,
    bool DestructiveUserFacingActionAvailable,
    bool ProductCommandHandlerAvailable,
    bool ProductiveServiceRegistrationAvailable,
    bool ProviderCloudNetworkAvailable,
    bool DbMigrationAvailable,
    bool KmsWormExternalTrustAvailable,
    bool BrowserCdpWcuOcrRecipesLiveAvailable,
    bool ReleaseCommercialReady,
    string StatusText);

public sealed class ProductLedgerLocalOnlyOperatorDiagnosticsPresenter
{
    public const string ReadyStatus =
        "PRODUCT_LEDGER_LOCAL_ONLY_OPERATOR_DIAGNOSTICS_READ_ONLY_SURFACE_READY INTERNAL_ONLY LOCAL_ONLY FAIL_CLOSED ACTIONS_DISABLED NO_PUBLIC_UI NO_COMMAND_HANDLERS NO_DB_MIGRATION NO_PROVIDER_CLOUD_NETWORK NO_KMS_WORM_EXTERNAL_TRUST NO_LIVE_AUTOMATION NO_RELEASE_COMMERCIAL";

    public const string RejectedStatus =
        "PRODUCT_LEDGER_LOCAL_ONLY_OPERATOR_DIAGNOSTICS_REJECTED FAIL_CLOSED ACTIONS_DISABLED NO_PUBLIC_UI NO_COMMAND_HANDLERS NO_DB_MIGRATION NO_PROVIDER_CLOUD_NETWORK NO_KMS_WORM_EXTERNAL_TRUST NO_LIVE_AUTOMATION NO_RELEASE_COMMERCIAL";

    public ProductLedgerLocalOnlyOperatorDiagnosticsResult Render(
        ProductLedgerLocalOnlyOperatorDiagnosticsRequest? request)
    {
        var blockers = new List<ProductLedgerLocalOnlyOperatorDiagnosticsBlocker>();
        if (request is null)
        {
            blockers.Add(ProductLedgerLocalOnlyOperatorDiagnosticsBlocker.MissingRequest);
            return Result(blockers, null);
        }

        AddRequestBlockers(request, blockers);
        AddRuntimeBlockers(request.RuntimeFlag, blockers);
        AddActivationBlockers(request.ActivationResult, blockers);
        AddDiagnosticsBlockers(request.RuntimeDiagnostics, blockers);
        return Result(blockers, request);
    }

    private static void AddRequestBlockers(
        ProductLedgerLocalOnlyOperatorDiagnosticsRequest request,
        List<ProductLedgerLocalOnlyOperatorDiagnosticsBlocker> blockers)
    {
        if (!request.ExplicitOperatorReadOnlyLocalOnlyScope)
        {
            blockers.Add(ProductLedgerLocalOnlyOperatorDiagnosticsBlocker.MissingExplicitOperatorReadOnlyLocalOnlyScope);
        }

        if (!request.HasAuthorityEvidence)
        {
            blockers.Add(ProductLedgerLocalOnlyOperatorDiagnosticsBlocker.MissingAuthorityEvidence);
        }

        if (!request.HasRedactionBeforePersistenceEvidence)
        {
            blockers.Add(ProductLedgerLocalOnlyOperatorDiagnosticsBlocker.MissingRedactionBeforePersistenceEvidence);
        }

        if (!request.HasRetentionEvidence)
        {
            blockers.Add(ProductLedgerLocalOnlyOperatorDiagnosticsBlocker.MissingRetentionEvidence);
        }

        if (!request.HasReplayFailureEvidence)
        {
            blockers.Add(ProductLedgerLocalOnlyOperatorDiagnosticsBlocker.MissingReplayFailureEvidence);
        }

        if (!request.HasRollbackEvidence)
        {
            blockers.Add(ProductLedgerLocalOnlyOperatorDiagnosticsBlocker.MissingRollbackEvidence);
        }

        if (!request.EvidenceReferencesFresh)
        {
            blockers.Add(ProductLedgerLocalOnlyOperatorDiagnosticsBlocker.StaleEvidenceReferences);
        }

        if (!request.EvidenceReferencesWellFormed)
        {
            blockers.Add(ProductLedgerLocalOnlyOperatorDiagnosticsBlocker.MalformedEvidenceReferences);
        }

        if (request.RequestsPublicUiAction)
        {
            blockers.Add(ProductLedgerLocalOnlyOperatorDiagnosticsBlocker.PublicUiActionRequested);
        }

        if (request.RequestsDestructiveUserFacingAction)
        {
            blockers.Add(ProductLedgerLocalOnlyOperatorDiagnosticsBlocker.DestructiveUserFacingActionRequested);
        }

        if (request.RequestsProductCommandHandler)
        {
            blockers.Add(ProductLedgerLocalOnlyOperatorDiagnosticsBlocker.ProductCommandHandlerRequested);
        }

        if (request.RequestsProductiveServiceRegistration)
        {
            blockers.Add(ProductLedgerLocalOnlyOperatorDiagnosticsBlocker.ProductiveServiceRegistrationRequested);
        }

        if (request.ClaimsProviderCloudNetwork)
        {
            blockers.Add(ProductLedgerLocalOnlyOperatorDiagnosticsBlocker.ProviderCloudNetworkClaimed);
        }

        if (request.ClaimsDbMigration)
        {
            blockers.Add(ProductLedgerLocalOnlyOperatorDiagnosticsBlocker.DbMigrationClaimed);
        }

        if (request.ClaimsKmsWormExternalTrust)
        {
            blockers.Add(ProductLedgerLocalOnlyOperatorDiagnosticsBlocker.KmsWormExternalTrustClaimed);
        }

        if (request.ClaimsBrowserCdpWcuOcrRecipesLive)
        {
            blockers.Add(ProductLedgerLocalOnlyOperatorDiagnosticsBlocker.BrowserCdpWcuOcrRecipesLiveClaimed);
        }

        if (request.ClaimsReleaseCommercial)
        {
            blockers.Add(ProductLedgerLocalOnlyOperatorDiagnosticsBlocker.ReleaseCommercialClaimed);
        }
    }

    private static void AddRuntimeBlockers(
        ProductLedgerRuntimeLocalOnlyFlagResult? runtime,
        List<ProductLedgerLocalOnlyOperatorDiagnosticsBlocker> blockers)
    {
        if (runtime is null)
        {
            blockers.Add(ProductLedgerLocalOnlyOperatorDiagnosticsBlocker.MissingRuntimeFlag);
            return;
        }

        var knownDecision = runtime.Decision is ProductLedgerRuntimeLocalOnlyFlagDecision.BlockedOff
            or ProductLedgerRuntimeLocalOnlyFlagDecision.ArmedLocalOnlyInternal;
        var knownValue = string.Equals(
                runtime.EffectiveFeatureFlagValue,
                ProductLedgerRuntimeLocalOnlyInternalEnablement.OffValue,
                StringComparison.Ordinal)
            || string.Equals(
                runtime.EffectiveFeatureFlagValue,
                ProductLedgerRuntimeLocalOnlyInternalEnablement.EnabledLocalOnlyInternalValue,
                StringComparison.Ordinal);
        if (!knownDecision
            || runtime.Blockers.Count > 0
            || !knownValue
            || !runtime.LocalRuntimeFlagDefaultOff
            || runtime.ProductRuntimeEnabled
            || runtime.RuntimeEnabledByDefault
            || runtime.ProductiveServiceRegistrationAllowed
            || runtime.ProductCommandHandlersAllowed
            || runtime.PublicUiProductActionsAllowed
            || runtime.ProviderCloudNetworkAllowed
            || runtime.DbMigrationAllowed
            || runtime.KmsWormExternalTrustAllowed
            || runtime.BrowserCdpWcuOcrRecipesLiveAllowed
            || runtime.ReleaseCommercialReady)
        {
            blockers.Add(ProductLedgerLocalOnlyOperatorDiagnosticsBlocker.UnsafeRuntimeFlag);
        }
    }

    private static void AddActivationBlockers(
        ProductLedgerPathLocalOnlyActivationResult? activation,
        List<ProductLedgerLocalOnlyOperatorDiagnosticsBlocker> blockers)
    {
        if (activation is null)
        {
            blockers.Add(ProductLedgerLocalOnlyOperatorDiagnosticsBlocker.MissingActivationResult);
            return;
        }

        if (activation.Decision != ProductLedgerPathLocalOnlyActivationDecision.ActivatedLocalOnly
            || activation.Blockers.Count > 0
            || string.IsNullOrWhiteSpace(activation.CandidateId)
            || string.IsNullOrWhiteSpace(activation.ActiveLedgerRootPath)
            || string.IsNullOrWhiteSpace(activation.ActiveLedgerFilePath)
            || string.IsNullOrWhiteSpace(activation.ActiveCheckpointFilePath)
            || !activation.LocalRuntimeFlagDefaultOff
            || !activation.ProductLedgerPathActive
            || !activation.ProductLedgerWriteAllowed
            || activation.ProductRuntimeEnabled
            || activation.ProductServiceRegistrationAllowed
            || activation.ProductCommandHandlersAllowed
            || activation.UiProductActionsAllowed
            || activation.DbProviderCloudNetworkAllowed
            || activation.KmsWormExternalTrustAllowed
            || activation.LiveAutomationAllowed
            || activation.ReleaseCommercialReady)
        {
            blockers.Add(ProductLedgerLocalOnlyOperatorDiagnosticsBlocker.UnsafeActivationResult);
        }
    }

    private static void AddDiagnosticsBlockers(
        ProductLedgerRuntimeLocalOnlyAdapterResult? diagnostics,
        List<ProductLedgerLocalOnlyOperatorDiagnosticsBlocker> blockers)
    {
        if (diagnostics is null)
        {
            blockers.Add(ProductLedgerLocalOnlyOperatorDiagnosticsBlocker.MissingRuntimeDiagnostics);
            return;
        }

        if (diagnostics.Decision != ProductLedgerRuntimeLocalOnlyAdapterDecision.DiagnosticsReadOnly
            || diagnostics.Blockers.Count > 0
            || diagnostics.VerifiedEntryCount < 0
            || diagnostics.AppendResult is not null
            || diagnostics.ProductRuntimeEnabled
            || diagnostics.RuntimeEnabledByDefault
            || diagnostics.ProductiveServiceRegistrationAllowed
            || diagnostics.ProductCommandHandlersAllowed
            || diagnostics.PublicUiProductActionsAllowed
            || diagnostics.ProviderCloudNetworkAllowed
            || diagnostics.DbMigrationAllowed
            || diagnostics.KmsWormExternalTrustAllowed
            || diagnostics.BrowserCdpWcuOcrRecipesLiveAllowed
            || diagnostics.ReleaseCommercialReady)
        {
            blockers.Add(ProductLedgerLocalOnlyOperatorDiagnosticsBlocker.UnsafeRuntimeDiagnostics);
        }
    }

    private static ProductLedgerLocalOnlyOperatorDiagnosticsResult Result(
        IReadOnlyList<ProductLedgerLocalOnlyOperatorDiagnosticsBlocker> blockers,
        ProductLedgerLocalOnlyOperatorDiagnosticsRequest? request)
    {
        var distinct = blockers.Distinct().OrderBy(blocker => blocker.ToString(), StringComparer.Ordinal).ToArray();
        var rendered = distinct.Length == 0 && request is not null;
        var disabledActions = DisabledActions();
        var sections = rendered ? ReadySections(request!) : BlockedSections(distinct);
        return new ProductLedgerLocalOnlyOperatorDiagnosticsResult(
            Decision: rendered
                ? ProductLedgerLocalOnlyOperatorDiagnosticsDecision.RenderedReadOnly
                : ProductLedgerLocalOnlyOperatorDiagnosticsDecision.Rejected,
            Blockers: distinct,
            Sections: sections,
            ActionPreviews: ActionPreviews(),
            DisabledActions: disabledActions,
            SafeNextStep: rendered
                ? "LOCAL_DEV_RUNTIME_PRODUCT_READINESS_ACCEPTANCE_THEN_OPERATOR_FRONTIER_DECISION"
                : "FIX_BLOCKERS_BEFORE_OPERATOR_DIAGNOSTICS_SURFACE",
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
            StatusText: rendered ? ReadyStatus : RejectedStatus);
    }

    private static IReadOnlyList<ProductLedgerLocalOnlyOperatorDiagnosticsSection> ReadySections(
        ProductLedgerLocalOnlyOperatorDiagnosticsRequest request)
    {
        var runtime = request.RuntimeFlag!;
        var activation = request.ActivationResult!;
        var diagnostics = request.RuntimeDiagnostics!;
        return
        [
            new(
                "Runtime Local-Only Gate",
                runtime.Decision == ProductLedgerRuntimeLocalOnlyFlagDecision.ArmedLocalOnlyInternal
                    ? "ENABLED_LOCAL_ONLY_INTERNAL"
                    : "DEFAULT_OFF",
                [
                    $"feature_flag={runtime.EffectiveFeatureFlagValue}",
                    $"default_off={runtime.LocalRuntimeFlagDefaultOff}",
                    $"internal_read_only_surface={runtime.InternalReadOnlyProductSurfaceAllowed}",
                    "no_public_ui=true"
                ],
                ProductLedgerLocalOnlyOperatorDiagnosticsSeverity.Info),
            new(
                "Product Ledger Path Policy",
                "ACTIVE_LOCAL_ONLY_POLICY_BOUND",
                [
                    $"candidate_id={activation.CandidateId}",
                    $"ledger_root={activation.ActiveLedgerRootPath}",
                    "product_runtime_enabled=false",
                    "release_commercial_ready=false"
                ],
                ProductLedgerLocalOnlyOperatorDiagnosticsSeverity.Info),
            new(
                "Bounded Writer Status",
                "WRITER_BOUNDED_LOCAL_ONLY_SURFACE_READ_ONLY",
                [
                    $"writer_policy_allows_bounded_local_only={activation.ProductLedgerWriteAllowed}",
                    "operator_surface_write_allowed=false",
                    "destructive_user_facing_action_available=false"
                ],
                ProductLedgerLocalOnlyOperatorDiagnosticsSeverity.Info),
            new(
                "Checkpoint / Head Status",
                diagnostics.VerifiedEntryCount == 0 ? "EMPTY_VERIFIED_LEDGER" : "VERIFIED_HEAD_PRESENT",
                [
                    $"verified_entry_count={diagnostics.VerifiedEntryCount}",
                    $"head_entry_hash={diagnostics.HeadEntryHash ?? "none"}",
                    $"checkpoint_path={activation.ActiveCheckpointFilePath}"
                ],
                ProductLedgerLocalOnlyOperatorDiagnosticsSeverity.Info),
            new(
                "Evidence Gates",
                "EVIDENCE_REFERENCES_FRESH_AND_WELL_FORMED",
                [
                    $"authority={request.HasAuthorityEvidence}",
                    $"redaction_before_persistence={request.HasRedactionBeforePersistenceEvidence}",
                    $"retention={request.HasRetentionEvidence}",
                    $"replay_failure={request.HasReplayFailureEvidence}",
                    $"rollback_non_rollback={request.HasRollbackEvidence}",
                    "no_external_trust=true",
                    "no_worm_kms_cloud=true"
                ],
                ProductLedgerLocalOnlyOperatorDiagnosticsSeverity.Info),
            new(
                "Runtime/Product Local-Dev Readiness",
                "LOCAL_DEV_RUNTIME_PRODUCT_READINESS_SLICE_VISIBLE",
                [
                    "runtime_product_local_dev_readiness=36",
                    "runtime_product_production_readiness=0",
                    "product_surface_local_dev_readiness=86",
                    $"runtime_local_only_internal_enabled={runtime.RuntimeLocalOnlyInternalEnabled}",
                    $"diagnostics_readiness_surface_local_only={runtime.DiagnosticsReadinessSurfaceLocalOnlyAllowed}",
                    "production_runtime_enabled=false",
                    "public_product_surface_enabled=false",
                    "latest_pointer_authority=false",
                    "read_precedence_authority=false",
                    "product_authority=false",
                    "release_commercial_ready=false"
                ],
                ProductLedgerLocalOnlyOperatorDiagnosticsSeverity.Info),
            new(
                "Disabled Actions",
                "ALL_ACTIONS_DISABLED",
                DisabledActions(),
                ProductLedgerLocalOnlyOperatorDiagnosticsSeverity.Warning),
            new(
                "Safe Next Step",
                "LOCAL_DEV_RUNTIME_PRODUCT_READINESS_NEXT_OPERATOR_FRONTIER",
                [
                    "LOCAL_DEV_RUNTIME_PRODUCT_READINESS_ACCEPTANCE_THEN_OPERATOR_FRONTIER_DECISION",
                    "NO_RELEASE_COMMERCIAL",
                    "NO_PUBLIC_DESTRUCTIVE_ACTION",
                    "NO_PRODUCTION_RUNTIME"
                ],
                ProductLedgerLocalOnlyOperatorDiagnosticsSeverity.Info)
        ];
    }

    private static IReadOnlyList<ProductLedgerLocalOnlyOperatorDiagnosticsSection> BlockedSections(
        IReadOnlyList<ProductLedgerLocalOnlyOperatorDiagnosticsBlocker> blockers) =>
        [
            new(
                "Runtime Local-Only Gate",
                "FAIL_CLOSED",
                blockers.Select(blocker => blocker.ToString()).ToArray(),
                ProductLedgerLocalOnlyOperatorDiagnosticsSeverity.Blocker),
            new(
                "Disabled Actions",
                "ALL_ACTIONS_DISABLED",
                DisabledActions(),
                ProductLedgerLocalOnlyOperatorDiagnosticsSeverity.Blocker),
            new(
                "Safe Next Step",
                "FIX_BLOCKERS_BEFORE_OPERATOR_DIAGNOSTICS_SURFACE",
                ["NO_WRITE", "NO_PUBLIC_UI", "NO_RELEASE_COMMERCIAL"],
                ProductLedgerLocalOnlyOperatorDiagnosticsSeverity.Blocker)
        ];

    private static IReadOnlyList<ProductLedgerLocalOnlyOperatorDiagnosticsActionPreview> ActionPreviews() =>
        [
            new(
                "View local-only diagnostics snapshot",
                "read-only preview only",
                "operator visibility without execution authority",
                ["runtime gate", "active path policy", "checkpoint head", "evidence gates"],
                Disabled: true,
                ProductiveCommandId: null,
                HandlerName: null,
                CallbackName: null),
            new(
                "Append bounded local-only ledger entry",
                "disabled on the operator surface",
                "writes stay outside this read-only presenter",
                ["redaction-before-persistence", "safe payload hash", "authority evidence"],
                Disabled: true,
                ProductiveCommandId: null,
                HandlerName: null,
                CallbackName: null),
            new(
                "Enable public UI product action",
                "prohibited by boundary",
                "would require a new human decision",
                ["public UI GO", "release gate"],
                Disabled: true,
                ProductiveCommandId: null,
                HandlerName: null,
                CallbackName: null),
            new(
                "Promote release or commercial readiness",
                "prohibited by boundary",
                "release/commercial remains NO-GO",
                ["release authority", "commercial readiness packet"],
                Disabled: true,
                ProductiveCommandId: null,
                HandlerName: null,
                CallbackName: null)
        ];

    private static IReadOnlyList<string> DisabledActions() =>
        [
            "public UI action",
            "destructive user-facing action",
            "product command handler",
            "productive service registration",
            "provider/cloud/network access",
            "database migration",
            "KMS/WORM/external trust",
            "Browser/CDP/WCU/OCR/Recipes live automation",
            "release/commercial readiness"
        ];
}
