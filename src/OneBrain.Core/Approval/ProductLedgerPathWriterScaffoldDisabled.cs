namespace OneBrain.Core.Approval;

public enum ProductLedgerPathWriterScaffoldDecision
{
    Rejected,
    Blocked,
    DisabledWriterScaffoldReady
}

public enum ProductLedgerPathWriterScaffoldBlocker
{
    MissingRequest,
    MissingPersistedCandidateResult,
    FailedPersistedCandidateResult,
    MissingExplicitDisabledTestOnlyMode,
    ProductWriteRequested,
    RuntimeEnablementRequested,
    ProductLedgerPathActivationRequested,
    ProductServiceRegistrationRequested,
    ProductCommandHandlerRequested,
    UiProductActionRequested,
    ProviderCloudNetworkClaimed,
    WormKmsExternalTrustClaimed,
    ReleaseCommercialReadinessClaimed,
    MissingRedactionBeforePersistenceEvidence,
    MissingFailureReplayRollbackEvidence,
    MissingNoProductWriterAssertion
}

public sealed record ProductLedgerPathWriterScaffoldRequest(
    ProductLedgerPathPersistedCandidateResult? PersistedCandidateResult,
    bool ExplicitDisabledTestOnlyMode,
    bool NoProductWriterAssertion,
    bool NoProductWriteAssertion,
    bool NoRuntimeEnablementAssertion,
    bool NoProductLedgerPathActivationAssertion,
    bool NoProductServiceRegistrationAssertion,
    bool NoProductCommandHandlerAssertion,
    bool NoUiProductActionAssertion,
    bool NoProviderCloudNetworkAssertion,
    bool NoWormKmsExternalTrustAssertion,
    bool NoReleaseCommercialAssertion,
    bool HasRedactionBeforePersistenceEvidence,
    bool HasFailureReplayRollbackEvidence,
    bool RequestsWriterActivation,
    bool RequestsRuntimeEnablement,
    bool RequestsProductLedgerPathActivation,
    bool RequestsProductServiceRegistration,
    bool RequestsProductCommandHandler,
    bool RequestsUiProductAction,
    bool ClaimsProviderCloudNetwork,
    bool ClaimsWormKmsExternalTrust,
    bool ClaimsReleaseCommercialReadiness);

public sealed record ProductLedgerPathWriterScaffoldResult(
    ProductLedgerPathWriterScaffoldDecision Decision,
    IReadOnlyList<ProductLedgerPathWriterScaffoldBlocker> Blockers,
    bool DisabledWriterScaffoldReady,
    bool ProductLedgerPathActive,
    bool ProductLedgerWriteAllowed,
    bool ProductRuntimeEnabled,
    bool ProductServiceRegistrationAllowed,
    bool ProductCommandHandlersAllowed,
    bool UiProductActionsAllowed,
    bool DbProviderCloudNetworkAllowed,
    bool KmsWormExternalTrustAllowed,
    bool LiveAutomationAllowed,
    bool ReleaseCommercialReady,
    string StatusText);

public sealed class ProductLedgerPathWriterScaffoldDisabled
{
    public const string DisabledWriterScaffoldStatus =
        "DISABLED_WRITER_SCAFFOLD_TEST_ONLY NO_ACTIVE_PRODUCT_LEDGER_PATH NO_PRODUCT_LEDGER_WRITE NO_PRODUCT_RUNTIME_ENABLEMENT NO_PRODUCT_SERVICE_REGISTRATION NO_COMMAND_HANDLERS NO_UI_PRODUCT_ACTIONS NO_RELEASE_COMMERCIAL NO_EXTERNAL_TRUST NO_WORM_KMS_CLOUD";

    public ProductLedgerPathWriterScaffoldResult Evaluate(ProductLedgerPathWriterScaffoldRequest? request)
    {
        var blockers = new List<ProductLedgerPathWriterScaffoldBlocker>();
        if (request is null)
        {
            blockers.Add(ProductLedgerPathWriterScaffoldBlocker.MissingRequest);
            return Result(blockers);
        }

        AddCandidateBlockers(request, blockers);
        AddModeAndEvidenceBlockers(request, blockers);
        AddNoEnableBlockers(request, blockers);

        return Result(blockers);
    }

    private static void AddCandidateBlockers(
        ProductLedgerPathWriterScaffoldRequest request,
        List<ProductLedgerPathWriterScaffoldBlocker> blockers)
    {
        var persistedCandidate = request.PersistedCandidateResult;
        if (persistedCandidate is null)
        {
            blockers.Add(ProductLedgerPathWriterScaffoldBlocker.MissingPersistedCandidateResult);
            return;
        }

        if (persistedCandidate.Decision != ProductLedgerPathPersistedCandidateDecision.PersistedCandidateNoWrite
            || !persistedCandidate.CandidatePersistedNoWrite
            || persistedCandidate.Blockers.Count > 0
            || persistedCandidate.Candidate is null
            || persistedCandidate.ProductLedgerPathActive
            || persistedCandidate.ProductLedgerWriteAllowed
            || persistedCandidate.ProductRuntimeEnabled
            || persistedCandidate.ProductServiceRegistrationAllowed
            || persistedCandidate.ProductCommandHandlersAllowed
            || persistedCandidate.UiProductActionsAllowed
            || persistedCandidate.DbProviderCloudNetworkAllowed
            || persistedCandidate.KmsWormExternalTrustAllowed
            || persistedCandidate.LiveAutomationAllowed
            || persistedCandidate.ReleaseCommercialReady)
        {
            blockers.Add(ProductLedgerPathWriterScaffoldBlocker.FailedPersistedCandidateResult);
        }
    }

    private static void AddModeAndEvidenceBlockers(
        ProductLedgerPathWriterScaffoldRequest request,
        List<ProductLedgerPathWriterScaffoldBlocker> blockers)
    {
        if (!request.ExplicitDisabledTestOnlyMode)
        {
            blockers.Add(ProductLedgerPathWriterScaffoldBlocker.MissingExplicitDisabledTestOnlyMode);
        }

        if (!request.NoProductWriterAssertion)
        {
            blockers.Add(ProductLedgerPathWriterScaffoldBlocker.MissingNoProductWriterAssertion);
        }

        if (!request.HasRedactionBeforePersistenceEvidence)
        {
            blockers.Add(ProductLedgerPathWriterScaffoldBlocker.MissingRedactionBeforePersistenceEvidence);
        }

        if (!request.HasFailureReplayRollbackEvidence)
        {
            blockers.Add(ProductLedgerPathWriterScaffoldBlocker.MissingFailureReplayRollbackEvidence);
        }
    }

    private static void AddNoEnableBlockers(
        ProductLedgerPathWriterScaffoldRequest request,
        List<ProductLedgerPathWriterScaffoldBlocker> blockers)
    {
        if (!request.NoProductWriteAssertion || request.RequestsWriterActivation)
        {
            blockers.Add(ProductLedgerPathWriterScaffoldBlocker.ProductWriteRequested);
        }

        if (!request.NoRuntimeEnablementAssertion || request.RequestsRuntimeEnablement)
        {
            blockers.Add(ProductLedgerPathWriterScaffoldBlocker.RuntimeEnablementRequested);
        }

        if (!request.NoProductLedgerPathActivationAssertion || request.RequestsProductLedgerPathActivation)
        {
            blockers.Add(ProductLedgerPathWriterScaffoldBlocker.ProductLedgerPathActivationRequested);
        }

        if (!request.NoProductServiceRegistrationAssertion || request.RequestsProductServiceRegistration)
        {
            blockers.Add(ProductLedgerPathWriterScaffoldBlocker.ProductServiceRegistrationRequested);
        }

        if (!request.NoProductCommandHandlerAssertion || request.RequestsProductCommandHandler)
        {
            blockers.Add(ProductLedgerPathWriterScaffoldBlocker.ProductCommandHandlerRequested);
        }

        if (!request.NoUiProductActionAssertion || request.RequestsUiProductAction)
        {
            blockers.Add(ProductLedgerPathWriterScaffoldBlocker.UiProductActionRequested);
        }

        if (!request.NoProviderCloudNetworkAssertion || request.ClaimsProviderCloudNetwork)
        {
            blockers.Add(ProductLedgerPathWriterScaffoldBlocker.ProviderCloudNetworkClaimed);
        }

        if (!request.NoWormKmsExternalTrustAssertion || request.ClaimsWormKmsExternalTrust)
        {
            blockers.Add(ProductLedgerPathWriterScaffoldBlocker.WormKmsExternalTrustClaimed);
        }

        if (!request.NoReleaseCommercialAssertion || request.ClaimsReleaseCommercialReadiness)
        {
            blockers.Add(ProductLedgerPathWriterScaffoldBlocker.ReleaseCommercialReadinessClaimed);
        }
    }

    private static ProductLedgerPathWriterScaffoldResult Result(IReadOnlyList<ProductLedgerPathWriterScaffoldBlocker> blockers)
    {
        var distinct = blockers.Distinct().OrderBy(blocker => blocker.ToString(), StringComparer.Ordinal).ToArray();
        var ready = distinct.Length == 0;
        return new ProductLedgerPathWriterScaffoldResult(
            Decision: ready
                ? ProductLedgerPathWriterScaffoldDecision.DisabledWriterScaffoldReady
                : (distinct.Contains(ProductLedgerPathWriterScaffoldBlocker.MissingRequest)
                    ? ProductLedgerPathWriterScaffoldDecision.Rejected
                    : ProductLedgerPathWriterScaffoldDecision.Blocked),
            Blockers: distinct,
            DisabledWriterScaffoldReady: ready,
            ProductLedgerPathActive: false,
            ProductLedgerWriteAllowed: false,
            ProductRuntimeEnabled: false,
            ProductServiceRegistrationAllowed: false,
            ProductCommandHandlersAllowed: false,
            UiProductActionsAllowed: false,
            DbProviderCloudNetworkAllowed: false,
            KmsWormExternalTrustAllowed: false,
            LiveAutomationAllowed: false,
            ReleaseCommercialReady: false,
            StatusText: DisabledWriterScaffoldStatus);
    }
}
