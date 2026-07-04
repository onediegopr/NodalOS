using System.Text.RegularExpressions;

namespace OneBrain.Core.Approval;

public enum ProductLedgerPathActivePolicyDecision
{
    Rejected,
    Blocked,
    CandidateAcceptedNoWrite
}

public enum ProductLedgerPathActivePolicyBlocker
{
    MissingRequest,
    MissingCanonicalizationResult,
    FailedCanonicalizationResult,
    MissingCanonicalAllowedBoundaryEvidence,
    UnresolvedReparseSymlinkJunctionRisk,
    TocTouBlockerPresent,
    MissingRedactionPolicyEvidence,
    MissingRetentionPolicyEvidence,
    MissingReplayFailureEvidence,
    MissingRollbackNonRollbackClassification,
    MissingAuthorityEvidence,
    HumanGoTreatedAsProductAuthority,
    ProductWriteRequested,
    RuntimeEnablementRequested,
    ProductLedgerPathActivationRequested,
    ProductServiceRegistrationRequested,
    ProductCommandHandlerRequested,
    UiProductActionRequested,
    ReleaseCommercialReadinessClaimed,
    ProviderCloudNetworkClaimed,
    WormKmsExternalTrustClaimed,
    LocalTempClaimedAsProductLedgerPath,
    EvidenceReferenceMissing,
    EvidenceReferenceMalformed,
    EvidenceReferenceDuplicate,
    EvidenceReferenceStale,
    EvidenceReferenceInconsistent,
    EvidenceReferenceContainsRawPayloadOrSecretMarker,
    EvidenceReferenceContainsProductAuthorityClaim
}

public sealed record ProductLedgerPathActivePolicyRequest(
    ProductLedgerPathCanonicalizationResult? CanonicalizationResult,
    bool HasCanonicalAllowedBoundaryEvidence,
    bool HasNoUnresolvedReparseSymlinkJunctionRiskEvidence,
    bool HasTocTouMitigationEvidence,
    bool HasRedactionPolicyEvidence,
    bool HasRetentionPolicyEvidence,
    bool HasReplayFailureEvidence,
    bool HasRollbackNonRollbackClassification,
    bool HasAuthorityEvidence,
    bool AuthorityEvidenceIsNonProduct,
    bool TreatsHumanGoAsProductAuthority,
    IReadOnlyList<string>? EvidenceReferences,
    bool EvidenceReferencesAreStale,
    bool EvidenceReferencesAreInconsistent,
    bool ClaimsLocalTempAsProductLedgerPath,
    bool NoProductWriteAssertion,
    bool NoRuntimeEnablementAssertion,
    bool NoReleaseCommercialAssertion,
    bool NoProviderCloudNetworkAssertion,
    bool NoWormKmsExternalTrustAssertion,
    bool RequestsProductLedgerPathActivation,
    bool RequestsWriterActivation,
    bool RequestsRuntimeEnablement,
    bool RequestsProductServiceRegistration,
    bool RequestsProductCommandHandler,
    bool RequestsUiProductAction,
    bool ClaimsReleaseCommercialReadiness,
    bool ClaimsProviderCloudNetwork,
    bool ClaimsWormKmsExternalTrust);

public sealed record ProductLedgerPathActivePolicyResult(
    ProductLedgerPathActivePolicyDecision Decision,
    IReadOnlyList<ProductLedgerPathActivePolicyBlocker> Blockers,
    bool CandidateAcceptedNoWrite,
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

public sealed class ProductLedgerPathActivePolicy
{
    public const string CandidateAcceptedNoWriteStatus =
        "CANDIDATE_ACCEPTED_NO_WRITE POLICY_ACCEPTED_CANDIDATE_ONLY NO_ACTIVE_PRODUCT_LEDGER_PATH NO_PRODUCT_LEDGER_WRITE NO_PRODUCT_RUNTIME_ENABLEMENT NO_RELEASE_COMMERCIAL NO_EXTERNAL_TRUST NO_WORM_KMS_CLOUD";

    private static readonly Regex EvidenceReferencePattern = new(
        @"^[a-z0-9][a-z0-9._/\-:]*$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly string[] RawPayloadOrSecretMarkers =
    [
        "raw-payload",
        "raw_payload",
        "secret=",
        "password=",
        "api_key",
        "apikey",
        "token=",
        "bearer "
    ];

    private static readonly string[] ProductAuthorityMarkers =
    [
        "ledger-active",
        "writer-active",
        "writer-ready",
        "runtime-enabled",
        "runtime-ready",
        "product-ready",
        "release-ready",
        "commercial-ready",
        "product-authority"
    ];

    public ProductLedgerPathActivePolicyResult Evaluate(ProductLedgerPathActivePolicyRequest? request)
    {
        var blockers = new List<ProductLedgerPathActivePolicyBlocker>();
        if (request is null)
        {
            blockers.Add(ProductLedgerPathActivePolicyBlocker.MissingRequest);
            return Result(blockers);
        }

        AddCanonicalizationBlockers(request, blockers);
        AddEvidenceBlockers(request, blockers);
        AddAuthorityBlockers(request, blockers);
        AddNoEnableBlockers(request, blockers);

        return Result(blockers);
    }

    private static void AddCanonicalizationBlockers(
        ProductLedgerPathActivePolicyRequest request,
        List<ProductLedgerPathActivePolicyBlocker> blockers)
    {
        var canonicalization = request.CanonicalizationResult;
        if (canonicalization is null)
        {
            blockers.Add(ProductLedgerPathActivePolicyBlocker.MissingCanonicalizationResult);
            return;
        }

        if (canonicalization.Decision != ProductLedgerPathCanonicalizationDecision.CandidateReadinessAllowed
            || !canonicalization.CandidateReadinessAllowed
            || canonicalization.Blockers.Count > 0
            || canonicalization.ProductLedgerPathActive
            || canonicalization.ProductLedgerWriteAllowed
            || canonicalization.ProductRuntimeEnabled
            || canonicalization.ReleaseCommercialReady
            || string.IsNullOrWhiteSpace(canonicalization.CanonicalAllowedRootPath)
            || string.IsNullOrWhiteSpace(canonicalization.CanonicalCandidatePath))
        {
            blockers.Add(ProductLedgerPathActivePolicyBlocker.FailedCanonicalizationResult);
        }

        if (!request.HasCanonicalAllowedBoundaryEvidence)
        {
            blockers.Add(ProductLedgerPathActivePolicyBlocker.MissingCanonicalAllowedBoundaryEvidence);
        }

        if (!request.HasNoUnresolvedReparseSymlinkJunctionRiskEvidence)
        {
            blockers.Add(ProductLedgerPathActivePolicyBlocker.UnresolvedReparseSymlinkJunctionRisk);
        }

        if (!request.HasTocTouMitigationEvidence)
        {
            blockers.Add(ProductLedgerPathActivePolicyBlocker.TocTouBlockerPresent);
        }

        if (request.ClaimsLocalTempAsProductLedgerPath)
        {
            blockers.Add(ProductLedgerPathActivePolicyBlocker.LocalTempClaimedAsProductLedgerPath);
        }
    }

    private static void AddEvidenceBlockers(
        ProductLedgerPathActivePolicyRequest request,
        List<ProductLedgerPathActivePolicyBlocker> blockers)
    {
        if (!request.HasRedactionPolicyEvidence)
        {
            blockers.Add(ProductLedgerPathActivePolicyBlocker.MissingRedactionPolicyEvidence);
        }

        if (!request.HasRetentionPolicyEvidence)
        {
            blockers.Add(ProductLedgerPathActivePolicyBlocker.MissingRetentionPolicyEvidence);
        }

        if (!request.HasReplayFailureEvidence)
        {
            blockers.Add(ProductLedgerPathActivePolicyBlocker.MissingReplayFailureEvidence);
        }

        if (!request.HasRollbackNonRollbackClassification)
        {
            blockers.Add(ProductLedgerPathActivePolicyBlocker.MissingRollbackNonRollbackClassification);
        }

        AddEvidenceReferenceBlockers(request, blockers);
    }

    private static void AddAuthorityBlockers(
        ProductLedgerPathActivePolicyRequest request,
        List<ProductLedgerPathActivePolicyBlocker> blockers)
    {
        if (!request.HasAuthorityEvidence || !request.AuthorityEvidenceIsNonProduct)
        {
            blockers.Add(ProductLedgerPathActivePolicyBlocker.MissingAuthorityEvidence);
        }

        if (request.TreatsHumanGoAsProductAuthority)
        {
            blockers.Add(ProductLedgerPathActivePolicyBlocker.HumanGoTreatedAsProductAuthority);
        }
    }

    private static void AddNoEnableBlockers(
        ProductLedgerPathActivePolicyRequest request,
        List<ProductLedgerPathActivePolicyBlocker> blockers)
    {
        if (!request.NoProductWriteAssertion || request.RequestsWriterActivation)
        {
            blockers.Add(ProductLedgerPathActivePolicyBlocker.ProductWriteRequested);
        }

        if (!request.NoRuntimeEnablementAssertion || request.RequestsRuntimeEnablement)
        {
            blockers.Add(ProductLedgerPathActivePolicyBlocker.RuntimeEnablementRequested);
        }

        if (request.RequestsProductLedgerPathActivation)
        {
            blockers.Add(ProductLedgerPathActivePolicyBlocker.ProductLedgerPathActivationRequested);
        }

        if (request.RequestsProductServiceRegistration)
        {
            blockers.Add(ProductLedgerPathActivePolicyBlocker.ProductServiceRegistrationRequested);
        }

        if (request.RequestsProductCommandHandler)
        {
            blockers.Add(ProductLedgerPathActivePolicyBlocker.ProductCommandHandlerRequested);
        }

        if (request.RequestsUiProductAction)
        {
            blockers.Add(ProductLedgerPathActivePolicyBlocker.UiProductActionRequested);
        }

        if (!request.NoReleaseCommercialAssertion || request.ClaimsReleaseCommercialReadiness)
        {
            blockers.Add(ProductLedgerPathActivePolicyBlocker.ReleaseCommercialReadinessClaimed);
        }

        if (!request.NoProviderCloudNetworkAssertion || request.ClaimsProviderCloudNetwork)
        {
            blockers.Add(ProductLedgerPathActivePolicyBlocker.ProviderCloudNetworkClaimed);
        }

        if (!request.NoWormKmsExternalTrustAssertion || request.ClaimsWormKmsExternalTrust)
        {
            blockers.Add(ProductLedgerPathActivePolicyBlocker.WormKmsExternalTrustClaimed);
        }
    }

    private static void AddEvidenceReferenceBlockers(
        ProductLedgerPathActivePolicyRequest request,
        List<ProductLedgerPathActivePolicyBlocker> blockers)
    {
        var evidenceReferences = request.EvidenceReferences;
        if (evidenceReferences is null
            || evidenceReferences.Count == 0
            || evidenceReferences.Any(string.IsNullOrWhiteSpace))
        {
            blockers.Add(ProductLedgerPathActivePolicyBlocker.EvidenceReferenceMissing);
            return;
        }

        if (evidenceReferences.Distinct(StringComparer.OrdinalIgnoreCase).Count() != evidenceReferences.Count)
        {
            blockers.Add(ProductLedgerPathActivePolicyBlocker.EvidenceReferenceDuplicate);
        }

        if (request.EvidenceReferencesAreStale)
        {
            blockers.Add(ProductLedgerPathActivePolicyBlocker.EvidenceReferenceStale);
        }

        if (request.EvidenceReferencesAreInconsistent)
        {
            blockers.Add(ProductLedgerPathActivePolicyBlocker.EvidenceReferenceInconsistent);
        }

        foreach (var evidenceReference in evidenceReferences)
        {
            if (!EvidenceReferencePattern.IsMatch(evidenceReference)
                || evidenceReference.Contains("..", StringComparison.Ordinal)
                || evidenceReference.Contains('\\', StringComparison.Ordinal))
            {
                blockers.Add(ProductLedgerPathActivePolicyBlocker.EvidenceReferenceMalformed);
            }

            if (RawPayloadOrSecretMarkers.Any(marker => evidenceReference.Contains(marker, StringComparison.OrdinalIgnoreCase)))
            {
                blockers.Add(ProductLedgerPathActivePolicyBlocker.EvidenceReferenceContainsRawPayloadOrSecretMarker);
            }

            if (ProductAuthorityMarkers.Any(marker => evidenceReference.Contains(marker, StringComparison.OrdinalIgnoreCase)))
            {
                blockers.Add(ProductLedgerPathActivePolicyBlocker.EvidenceReferenceContainsProductAuthorityClaim);
            }
        }
    }

    private static ProductLedgerPathActivePolicyResult Result(
        IReadOnlyList<ProductLedgerPathActivePolicyBlocker> blockers)
    {
        var distinct = blockers.Distinct().OrderBy(blocker => blocker.ToString(), StringComparer.Ordinal).ToArray();
        var accepted = distinct.Length == 0;
        return new ProductLedgerPathActivePolicyResult(
            Decision: accepted
                ? ProductLedgerPathActivePolicyDecision.CandidateAcceptedNoWrite
                : (distinct.Contains(ProductLedgerPathActivePolicyBlocker.MissingRequest)
                    ? ProductLedgerPathActivePolicyDecision.Rejected
                    : ProductLedgerPathActivePolicyDecision.Blocked),
            Blockers: distinct,
            CandidateAcceptedNoWrite: accepted,
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
            StatusText: CandidateAcceptedNoWriteStatus);
    }
}
