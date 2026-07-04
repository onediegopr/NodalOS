namespace OneBrain.Core.Approval;

public enum ProductLedgerRuntimeLocalOnlyFlagDecision
{
    Rejected,
    BlockedOff,
    ArmedLocalOnlyInternal
}

public enum ProductLedgerRuntimeLocalOnlyAdapterDecision
{
    Rejected,
    Blocked,
    DiagnosticsReadOnly,
    AppendedLocalOnly
}

public enum ProductLedgerRuntimeLocalOnlyCommandKind
{
    DiagnosticsReadOnly,
    AppendSafeHashOnly
}

public enum ProductLedgerRuntimeLocalOnlyBlocker
{
    MissingRequest,
    MissingExplicitLocalOnlyRuntimeScope,
    RuntimeEnabledByDefault,
    MissingLocalRuntimeFlagDefaultOff,
    UnexpectedFeatureFlagValue,
    PublicUiActionRequested,
    ProductCommandHandlerRequested,
    ProductiveServiceRegistrationRequested,
    ProviderCloudNetworkClaimed,
    DbMigrationClaimed,
    KmsWormExternalTrustClaimed,
    BrowserCdpWcuOcrRecipesLiveClaimed,
    ReleaseCommercialClaimed,
    DestructiveActionOutsideBoundedWriterClaimed,
    MissingArmedFeatureFlag,
    FailedArmedFeatureFlag,
    MissingExplicitTestOnlyCommandAdapter,
    MissingActivationResult,
    FailedActivationResult,
    UnsupportedCommandKind,
    UnsafeAppendRequest,
    ExistingLedgerInvalid
}

public sealed record ProductLedgerRuntimeLocalOnlyFeatureFlagRequest(
    bool ExplicitLocalOnlyRuntimeScope,
    bool LocalRuntimeFlagDefaultOff,
    string? FeatureFlagValue,
    bool RequestsRuntimeEnabledByDefault,
    bool RequestsPublicUiAction,
    bool RequestsProductCommandHandler,
    bool RequestsProductiveServiceRegistration,
    bool ClaimsProviderCloudNetwork,
    bool ClaimsDbMigration,
    bool ClaimsKmsWormExternalTrust,
    bool ClaimsBrowserCdpWcuOcrRecipesLive,
    bool ClaimsReleaseCommercial,
    bool ClaimsDestructiveActionOutsideBoundedWriter);

public sealed record ProductLedgerRuntimeLocalOnlyFlagResult(
    ProductLedgerRuntimeLocalOnlyFlagDecision Decision,
    IReadOnlyList<ProductLedgerRuntimeLocalOnlyBlocker> Blockers,
    string EffectiveFeatureFlagValue,
    bool RuntimeLocalOnlyInternalEnabled,
    bool LocalRuntimeFlagDefaultOff,
    bool InternalServiceWiringAllowed,
    bool InternalCommandAdapterTestOnlyAllowed,
    bool InternalReadOnlyProductSurfaceAllowed,
    bool DiagnosticsReadinessSurfaceLocalOnlyAllowed,
    bool ProductRuntimeEnabled,
    bool RuntimeEnabledByDefault,
    bool ProductiveServiceRegistrationAllowed,
    bool ProductCommandHandlersAllowed,
    bool PublicUiProductActionsAllowed,
    bool ProviderCloudNetworkAllowed,
    bool DbMigrationAllowed,
    bool KmsWormExternalTrustAllowed,
    bool BrowserCdpWcuOcrRecipesLiveAllowed,
    bool ReleaseCommercialReady,
    string StatusText);

public sealed record ProductLedgerRuntimeLocalOnlyAdapterRequest(
    ProductLedgerRuntimeLocalOnlyFlagResult? FeatureFlag,
    ProductLedgerPathLocalOnlyActivationResult? ActivationResult,
    ProductLedgerRuntimeLocalOnlyCommandKind CommandKind,
    bool ExplicitTestOnlyCommandAdapter,
    string? SafePayloadHash,
    IReadOnlyDictionary<string, string>? EvidenceMetadata,
    bool RequestsPublicUiAction,
    bool RequestsProductCommandHandler,
    bool RequestsProductiveServiceRegistration,
    bool ClaimsProviderCloudNetwork,
    bool ClaimsDbMigration,
    bool ClaimsKmsWormExternalTrust,
    bool ClaimsBrowserCdpWcuOcrRecipesLive,
    bool ClaimsReleaseCommercial,
    bool ClaimsDestructiveActionOutsideBoundedWriter);

public sealed record ProductLedgerRuntimeLocalOnlyAdapterResult(
    ProductLedgerRuntimeLocalOnlyAdapterDecision Decision,
    IReadOnlyList<ProductLedgerRuntimeLocalOnlyBlocker> Blockers,
    int VerifiedEntryCount,
    string? HeadEntryHash,
    ProductLedgerPathLocalOnlyAppendResult? AppendResult,
    bool RuntimeLocalOnlyInternalEnabled,
    bool ProductRuntimeEnabled,
    bool RuntimeEnabledByDefault,
    bool ProductiveServiceRegistrationAllowed,
    bool ProductCommandHandlersAllowed,
    bool PublicUiProductActionsAllowed,
    bool ProviderCloudNetworkAllowed,
    bool DbMigrationAllowed,
    bool KmsWormExternalTrustAllowed,
    bool BrowserCdpWcuOcrRecipesLiveAllowed,
    bool ReleaseCommercialReady,
    string StatusText);

public sealed class ProductLedgerRuntimeLocalOnlyInternalEnablement
{
    public const string EnabledLocalOnlyInternalValue = "enabled:local-only-internal";
    public const string OffValue = "off";
    public const string DefaultOffStatus =
        "PRODUCT_LEDGER_RUNTIME_LOCAL_ONLY_INTERNAL_DEFAULT_OFF FAIL_CLOSED NO_PUBLIC_UI NO_PROVIDER_CLOUD_NETWORK NO_DB_MIGRATION NO_KMS_WORM_EXTERNAL_TRUST NO_LIVE_AUTOMATION NO_RELEASE_COMMERCIAL";
    public const string ArmedLocalOnlyInternalStatus =
        "PRODUCT_LEDGER_RUNTIME_LOCAL_ONLY_INTERNAL_ARMED DEFAULT_OFF_CONFIGURED INTERNAL_ONLY FAIL_CLOSED BOUNDED_WRITER_ONLY NO_PUBLIC_UI NO_PROVIDER_CLOUD_NETWORK NO_DB_MIGRATION NO_KMS_WORM_EXTERNAL_TRUST NO_LIVE_AUTOMATION NO_RELEASE_COMMERCIAL";

    private readonly ProductLedgerPathLocalOnlyActiveWriter writer;

    public ProductLedgerRuntimeLocalOnlyInternalEnablement()
        : this(new ProductLedgerPathLocalOnlyActiveWriter())
    {
    }

    internal ProductLedgerRuntimeLocalOnlyInternalEnablement(ProductLedgerPathLocalOnlyActiveWriter writer)
    {
        this.writer = writer;
    }

    public ProductLedgerRuntimeLocalOnlyFlagResult EvaluateFeatureFlag(ProductLedgerRuntimeLocalOnlyFeatureFlagRequest? request)
    {
        var blockers = new List<ProductLedgerRuntimeLocalOnlyBlocker>();
        if (request is null)
        {
            blockers.Add(ProductLedgerRuntimeLocalOnlyBlocker.MissingRequest);
            return FlagResult(blockers, OffValue);
        }

        AddScopeAndBoundaryBlockers(request, blockers);
        var effective = string.IsNullOrWhiteSpace(request.FeatureFlagValue)
            ? OffValue
            : request.FeatureFlagValue.Trim();
        if (!string.Equals(effective, OffValue, StringComparison.Ordinal)
            && !string.Equals(effective, EnabledLocalOnlyInternalValue, StringComparison.Ordinal))
        {
            blockers.Add(ProductLedgerRuntimeLocalOnlyBlocker.UnexpectedFeatureFlagValue);
        }

        return FlagResult(blockers, effective);
    }

    public ProductLedgerRuntimeLocalOnlyAdapterResult ExecuteInternal(ProductLedgerRuntimeLocalOnlyAdapterRequest? request)
    {
        var blockers = new List<ProductLedgerRuntimeLocalOnlyBlocker>();
        if (request is null)
        {
            blockers.Add(ProductLedgerRuntimeLocalOnlyBlocker.MissingRequest);
            return AdapterResult(blockers, null, [], null);
        }

        AddFeatureFlagBlockers(request.FeatureFlag, blockers);
        AddActivationBlockers(request.ActivationResult, blockers);
        AddAdapterBoundaryBlockers(request, blockers);

        IReadOnlyList<ProductLedgerPathLocalOnlyEntry> entries = [];
        ProductLedgerPathLocalOnlyAppendResult? append = null;
        if (blockers.Count == 0)
        {
            try
            {
                if (request.CommandKind == ProductLedgerRuntimeLocalOnlyCommandKind.AppendSafeHashOnly)
                {
                    append = writer.Append(new ProductLedgerPathLocalOnlyAppendRequest(
                        ActivationResult: request.ActivationResult,
                        SafePayloadHash: request.SafePayloadHash,
                        EvidenceMetadata: request.EvidenceMetadata,
                        RuntimeFlagStillDefaultOff: true,
                        RequestsRuntimeEnablement: false,
                        RequestsProductServiceRegistration: false,
                        RequestsProductCommandHandler: false,
                        RequestsUiProductAction: false,
                        ClaimsProviderCloudNetwork: false,
                        ClaimsWormKmsExternalTrust: false,
                        ClaimsDbMigration: false,
                        ClaimsBrowserCdpWcuOcrRecipesLive: false,
                        ClaimsReleaseCommercialReadiness: false));

                    if (append.Decision != ProductLedgerPathLocalOnlyWriterDecision.AppendedLocalOnly
                        || append.Blockers.Count > 0)
                    {
                        blockers.Add(ProductLedgerRuntimeLocalOnlyBlocker.UnsafeAppendRequest);
                    }
                }

                if (blockers.Count == 0)
                {
                    entries = writer.ReadVerified(request.ActivationResult!);
                }
            }
            catch (InvalidDataException)
            {
                blockers.Add(ProductLedgerRuntimeLocalOnlyBlocker.ExistingLedgerInvalid);
            }
        }

        return AdapterResult(blockers, request, entries, append);
    }

    private static void AddScopeAndBoundaryBlockers(
        ProductLedgerRuntimeLocalOnlyFeatureFlagRequest request,
        List<ProductLedgerRuntimeLocalOnlyBlocker> blockers)
    {
        if (!request.ExplicitLocalOnlyRuntimeScope)
        {
            blockers.Add(ProductLedgerRuntimeLocalOnlyBlocker.MissingExplicitLocalOnlyRuntimeScope);
        }

        if (!request.LocalRuntimeFlagDefaultOff)
        {
            blockers.Add(ProductLedgerRuntimeLocalOnlyBlocker.MissingLocalRuntimeFlagDefaultOff);
        }

        if (request.RequestsRuntimeEnabledByDefault)
        {
            blockers.Add(ProductLedgerRuntimeLocalOnlyBlocker.RuntimeEnabledByDefault);
        }

        AddNoExternalProductSurfaceBlockers(
            request.RequestsPublicUiAction,
            request.RequestsProductCommandHandler,
            request.RequestsProductiveServiceRegistration,
            request.ClaimsProviderCloudNetwork,
            request.ClaimsDbMigration,
            request.ClaimsKmsWormExternalTrust,
            request.ClaimsBrowserCdpWcuOcrRecipesLive,
            request.ClaimsReleaseCommercial,
            request.ClaimsDestructiveActionOutsideBoundedWriter,
            blockers);
    }

    private static void AddFeatureFlagBlockers(
        ProductLedgerRuntimeLocalOnlyFlagResult? featureFlag,
        List<ProductLedgerRuntimeLocalOnlyBlocker> blockers)
    {
        if (featureFlag is null)
        {
            blockers.Add(ProductLedgerRuntimeLocalOnlyBlocker.MissingArmedFeatureFlag);
            return;
        }

        if (featureFlag.Decision != ProductLedgerRuntimeLocalOnlyFlagDecision.ArmedLocalOnlyInternal
            || featureFlag.Blockers.Count > 0
            || !featureFlag.RuntimeLocalOnlyInternalEnabled
            || !featureFlag.LocalRuntimeFlagDefaultOff
            || !featureFlag.InternalServiceWiringAllowed
            || !featureFlag.InternalCommandAdapterTestOnlyAllowed
            || !featureFlag.InternalReadOnlyProductSurfaceAllowed
            || !featureFlag.DiagnosticsReadinessSurfaceLocalOnlyAllowed
            || featureFlag.ProductRuntimeEnabled
            || featureFlag.RuntimeEnabledByDefault
            || featureFlag.ProductiveServiceRegistrationAllowed
            || featureFlag.ProductCommandHandlersAllowed
            || featureFlag.PublicUiProductActionsAllowed
            || featureFlag.ProviderCloudNetworkAllowed
            || featureFlag.DbMigrationAllowed
            || featureFlag.KmsWormExternalTrustAllowed
            || featureFlag.BrowserCdpWcuOcrRecipesLiveAllowed
            || featureFlag.ReleaseCommercialReady)
        {
            blockers.Add(ProductLedgerRuntimeLocalOnlyBlocker.FailedArmedFeatureFlag);
        }
    }

    private static void AddActivationBlockers(
        ProductLedgerPathLocalOnlyActivationResult? activation,
        List<ProductLedgerRuntimeLocalOnlyBlocker> blockers)
    {
        if (activation is null)
        {
            blockers.Add(ProductLedgerRuntimeLocalOnlyBlocker.MissingActivationResult);
            return;
        }

        if (activation.Decision != ProductLedgerPathLocalOnlyActivationDecision.ActivatedLocalOnly
            || activation.Blockers.Count > 0
            || string.IsNullOrWhiteSpace(activation.ActiveLedgerRootPath)
            || string.IsNullOrWhiteSpace(activation.ActiveLedgerFilePath)
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
            blockers.Add(ProductLedgerRuntimeLocalOnlyBlocker.FailedActivationResult);
        }
    }

    private static void AddAdapterBoundaryBlockers(
        ProductLedgerRuntimeLocalOnlyAdapterRequest request,
        List<ProductLedgerRuntimeLocalOnlyBlocker> blockers)
    {
        if (!request.ExplicitTestOnlyCommandAdapter)
        {
            blockers.Add(ProductLedgerRuntimeLocalOnlyBlocker.MissingExplicitTestOnlyCommandAdapter);
        }

        if (!Enum.IsDefined(request.CommandKind))
        {
            blockers.Add(ProductLedgerRuntimeLocalOnlyBlocker.UnsupportedCommandKind);
        }

        AddNoExternalProductSurfaceBlockers(
            request.RequestsPublicUiAction,
            request.RequestsProductCommandHandler,
            request.RequestsProductiveServiceRegistration,
            request.ClaimsProviderCloudNetwork,
            request.ClaimsDbMigration,
            request.ClaimsKmsWormExternalTrust,
            request.ClaimsBrowserCdpWcuOcrRecipesLive,
            request.ClaimsReleaseCommercial,
            request.ClaimsDestructiveActionOutsideBoundedWriter,
            blockers);
    }

    private static void AddNoExternalProductSurfaceBlockers(
        bool requestsPublicUiAction,
        bool requestsProductCommandHandler,
        bool requestsProductiveServiceRegistration,
        bool claimsProviderCloudNetwork,
        bool claimsDbMigration,
        bool claimsKmsWormExternalTrust,
        bool claimsBrowserCdpWcuOcrRecipesLive,
        bool claimsReleaseCommercial,
        bool claimsDestructiveActionOutsideBoundedWriter,
        List<ProductLedgerRuntimeLocalOnlyBlocker> blockers)
    {
        if (requestsPublicUiAction)
        {
            blockers.Add(ProductLedgerRuntimeLocalOnlyBlocker.PublicUiActionRequested);
        }

        if (requestsProductCommandHandler)
        {
            blockers.Add(ProductLedgerRuntimeLocalOnlyBlocker.ProductCommandHandlerRequested);
        }

        if (requestsProductiveServiceRegistration)
        {
            blockers.Add(ProductLedgerRuntimeLocalOnlyBlocker.ProductiveServiceRegistrationRequested);
        }

        if (claimsProviderCloudNetwork)
        {
            blockers.Add(ProductLedgerRuntimeLocalOnlyBlocker.ProviderCloudNetworkClaimed);
        }

        if (claimsDbMigration)
        {
            blockers.Add(ProductLedgerRuntimeLocalOnlyBlocker.DbMigrationClaimed);
        }

        if (claimsKmsWormExternalTrust)
        {
            blockers.Add(ProductLedgerRuntimeLocalOnlyBlocker.KmsWormExternalTrustClaimed);
        }

        if (claimsBrowserCdpWcuOcrRecipesLive)
        {
            blockers.Add(ProductLedgerRuntimeLocalOnlyBlocker.BrowserCdpWcuOcrRecipesLiveClaimed);
        }

        if (claimsReleaseCommercial)
        {
            blockers.Add(ProductLedgerRuntimeLocalOnlyBlocker.ReleaseCommercialClaimed);
        }

        if (claimsDestructiveActionOutsideBoundedWriter)
        {
            blockers.Add(ProductLedgerRuntimeLocalOnlyBlocker.DestructiveActionOutsideBoundedWriterClaimed);
        }
    }

    private static ProductLedgerRuntimeLocalOnlyFlagResult FlagResult(
        IReadOnlyList<ProductLedgerRuntimeLocalOnlyBlocker> blockers,
        string effectiveValue)
    {
        var distinct = blockers.Distinct().OrderBy(blocker => blocker.ToString(), StringComparer.Ordinal).ToArray();
        var armed = distinct.Length == 0 && string.Equals(effectiveValue, EnabledLocalOnlyInternalValue, StringComparison.Ordinal);
        var blockedOff = distinct.Length == 0 && string.Equals(effectiveValue, OffValue, StringComparison.Ordinal);
        return new ProductLedgerRuntimeLocalOnlyFlagResult(
            Decision: armed
                ? ProductLedgerRuntimeLocalOnlyFlagDecision.ArmedLocalOnlyInternal
                : (blockedOff
                    ? ProductLedgerRuntimeLocalOnlyFlagDecision.BlockedOff
                    : ProductLedgerRuntimeLocalOnlyFlagDecision.Rejected),
            Blockers: distinct,
            EffectiveFeatureFlagValue: effectiveValue,
            RuntimeLocalOnlyInternalEnabled: armed,
            LocalRuntimeFlagDefaultOff: armed || blockedOff,
            InternalServiceWiringAllowed: armed,
            InternalCommandAdapterTestOnlyAllowed: armed,
            InternalReadOnlyProductSurfaceAllowed: armed,
            DiagnosticsReadinessSurfaceLocalOnlyAllowed: armed,
            ProductRuntimeEnabled: false,
            RuntimeEnabledByDefault: false,
            ProductiveServiceRegistrationAllowed: false,
            ProductCommandHandlersAllowed: false,
            PublicUiProductActionsAllowed: false,
            ProviderCloudNetworkAllowed: false,
            DbMigrationAllowed: false,
            KmsWormExternalTrustAllowed: false,
            BrowserCdpWcuOcrRecipesLiveAllowed: false,
            ReleaseCommercialReady: false,
            StatusText: armed ? ArmedLocalOnlyInternalStatus : DefaultOffStatus);
    }

    private static ProductLedgerRuntimeLocalOnlyAdapterResult AdapterResult(
        IReadOnlyList<ProductLedgerRuntimeLocalOnlyBlocker> blockers,
        ProductLedgerRuntimeLocalOnlyAdapterRequest? request,
        IReadOnlyList<ProductLedgerPathLocalOnlyEntry> entries,
        ProductLedgerPathLocalOnlyAppendResult? append)
    {
        var distinct = blockers.Distinct().OrderBy(blocker => blocker.ToString(), StringComparer.Ordinal).ToArray();
        var success = distinct.Length == 0;
        var head = entries.Count == 0 ? null : entries[^1].EntryHash;
        return new ProductLedgerRuntimeLocalOnlyAdapterResult(
            Decision: success
                ? (request!.CommandKind == ProductLedgerRuntimeLocalOnlyCommandKind.AppendSafeHashOnly
                    ? ProductLedgerRuntimeLocalOnlyAdapterDecision.AppendedLocalOnly
                    : ProductLedgerRuntimeLocalOnlyAdapterDecision.DiagnosticsReadOnly)
                : (distinct.Contains(ProductLedgerRuntimeLocalOnlyBlocker.MissingRequest)
                    ? ProductLedgerRuntimeLocalOnlyAdapterDecision.Rejected
                    : ProductLedgerRuntimeLocalOnlyAdapterDecision.Blocked),
            Blockers: distinct,
            VerifiedEntryCount: success ? entries.Count : 0,
            HeadEntryHash: success ? head : null,
            AppendResult: success ? append : null,
            RuntimeLocalOnlyInternalEnabled: success,
            ProductRuntimeEnabled: false,
            RuntimeEnabledByDefault: false,
            ProductiveServiceRegistrationAllowed: false,
            ProductCommandHandlersAllowed: false,
            PublicUiProductActionsAllowed: false,
            ProviderCloudNetworkAllowed: false,
            DbMigrationAllowed: false,
            KmsWormExternalTrustAllowed: false,
            BrowserCdpWcuOcrRecipesLiveAllowed: false,
            ReleaseCommercialReady: false,
            StatusText: success ? ArmedLocalOnlyInternalStatus : DefaultOffStatus);
    }
}
