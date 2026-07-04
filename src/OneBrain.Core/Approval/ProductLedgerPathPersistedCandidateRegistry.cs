using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace OneBrain.Core.Approval;

public enum ProductLedgerPathPersistedCandidateDecision
{
    Rejected,
    Blocked,
    PersistedCandidateNoWrite
}

public enum ProductLedgerPathPersistedCandidateBlocker
{
    MissingRequest,
    MissingCandidateId,
    InvalidCandidateId,
    DuplicateCandidateId,
    MissingActivePolicyResult,
    FailedActivePolicyResult,
    MissingCanonicalizationResult,
    FailedCanonicalizationResult,
    MissingEvidenceReference,
    MalformedEvidenceReference,
    EvidenceReferenceContainsRawPayloadOrSecretMarker,
    EvidenceReferenceContainsProductAuthorityClaim,
    ProductLedgerPathActivationRequested,
    ProductWriteRequested,
    RuntimeEnablementRequested,
    ProductServiceRegistrationRequested,
    ProductCommandHandlerRequested,
    UiProductActionRequested,
    ProviderCloudNetworkClaimed,
    WormKmsExternalTrustClaimed,
    ReleaseCommercialReadinessClaimed,
    LocalTempClaimedAsProductLedgerPath
}

public sealed record ProductLedgerPathPersistedCandidateRequest(
    string? CandidateId,
    ProductLedgerPathActivePolicyResult? ActivePolicyResult,
    ProductLedgerPathCanonicalizationResult? CanonicalizationResult,
    IReadOnlyList<string>? EvidenceReferences,
    bool ClaimsLocalTempAsProductLedgerPath,
    bool RequestsProductLedgerPathActivation,
    bool RequestsWriterActivation,
    bool RequestsRuntimeEnablement,
    bool RequestsProductServiceRegistration,
    bool RequestsProductCommandHandler,
    bool RequestsUiProductAction,
    bool ClaimsProviderCloudNetwork,
    bool ClaimsWormKmsExternalTrust,
    bool ClaimsReleaseCommercialReadiness);

public sealed record ProductLedgerPathPersistedCandidateRecord(
    string CandidateId,
    string CandidateFingerprint,
    string CanonicalCandidatePath,
    string CanonicalAllowedRootPath,
    IReadOnlyList<string> EvidenceReferences,
    string StatusText);

public sealed record ProductLedgerPathPersistedCandidateResult(
    ProductLedgerPathPersistedCandidateDecision Decision,
    IReadOnlyList<ProductLedgerPathPersistedCandidateBlocker> Blockers,
    ProductLedgerPathPersistedCandidateRecord? Candidate,
    bool CandidatePersistedNoWrite,
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

public sealed class ProductLedgerPathPersistedCandidateRegistry
{
    public const string PersistedCandidateNoWriteStatus =
        "PERSISTED_ACTIVE_PATH_CANDIDATE_LOCAL_ONLY_NO_WRITE CANDIDATE_ONLY NO_ACTIVE_PRODUCT_LEDGER_PATH NO_PRODUCT_LEDGER_WRITE NO_PRODUCT_RUNTIME_ENABLEMENT NO_RELEASE_COMMERCIAL NO_EXTERNAL_TRUST NO_WORM_KMS_CLOUD";

    private static readonly Regex CandidateIdPattern = new(
        @"^[a-z0-9][a-z0-9._-]{2,63}$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

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

    private readonly Dictionary<string, ProductLedgerPathPersistedCandidateRecord> candidates = new(StringComparer.OrdinalIgnoreCase);

    public ProductLedgerPathPersistedCandidateResult Persist(ProductLedgerPathPersistedCandidateRequest? request)
    {
        var blockers = new List<ProductLedgerPathPersistedCandidateBlocker>();
        if (request is null)
        {
            blockers.Add(ProductLedgerPathPersistedCandidateBlocker.MissingRequest);
            return Result(blockers, null);
        }

        AddCandidateIdBlockers(request, blockers);
        AddPolicyBlockers(request, blockers);
        AddCanonicalizationBlockers(request, blockers);
        AddEvidenceReferenceBlockers(request, blockers);
        AddNoEnableBlockers(request, blockers);

        if (blockers.Count > 0)
        {
            return Result(blockers, null);
        }

        var canonicalization = request.CanonicalizationResult!;
        var record = new ProductLedgerPathPersistedCandidateRecord(
            CandidateId: request.CandidateId!,
            CandidateFingerprint: Fingerprint(canonicalization.CanonicalCandidatePath!, canonicalization.CanonicalAllowedRootPath!),
            CanonicalCandidatePath: canonicalization.CanonicalCandidatePath!,
            CanonicalAllowedRootPath: canonicalization.CanonicalAllowedRootPath!,
            EvidenceReferences: request.EvidenceReferences!.ToArray(),
            StatusText: PersistedCandidateNoWriteStatus);

        candidates.Add(record.CandidateId, record);
        return Result([], record);
    }

    public ProductLedgerPathPersistedCandidateRecord? FindCandidate(string candidateId) =>
        candidates.TryGetValue(candidateId, out var record) ? record : null;

    public IReadOnlyList<ProductLedgerPathPersistedCandidateRecord> Snapshot() =>
        candidates.Values.OrderBy(candidate => candidate.CandidateId, StringComparer.OrdinalIgnoreCase).ToArray();

    private void AddCandidateIdBlockers(
        ProductLedgerPathPersistedCandidateRequest request,
        List<ProductLedgerPathPersistedCandidateBlocker> blockers)
    {
        if (string.IsNullOrWhiteSpace(request.CandidateId))
        {
            blockers.Add(ProductLedgerPathPersistedCandidateBlocker.MissingCandidateId);
            return;
        }

        if (!CandidateIdPattern.IsMatch(request.CandidateId))
        {
            blockers.Add(ProductLedgerPathPersistedCandidateBlocker.InvalidCandidateId);
        }

        if (candidates.ContainsKey(request.CandidateId))
        {
            blockers.Add(ProductLedgerPathPersistedCandidateBlocker.DuplicateCandidateId);
        }
    }

    private static void AddPolicyBlockers(
        ProductLedgerPathPersistedCandidateRequest request,
        List<ProductLedgerPathPersistedCandidateBlocker> blockers)
    {
        var policy = request.ActivePolicyResult;
        if (policy is null)
        {
            blockers.Add(ProductLedgerPathPersistedCandidateBlocker.MissingActivePolicyResult);
            return;
        }

        if (policy.Decision != ProductLedgerPathActivePolicyDecision.CandidateAcceptedNoWrite
            || !policy.CandidateAcceptedNoWrite
            || policy.Blockers.Count > 0
            || policy.ProductLedgerPathActive
            || policy.ProductLedgerWriteAllowed
            || policy.ProductRuntimeEnabled
            || policy.ProductServiceRegistrationAllowed
            || policy.ProductCommandHandlersAllowed
            || policy.UiProductActionsAllowed
            || policy.DbProviderCloudNetworkAllowed
            || policy.KmsWormExternalTrustAllowed
            || policy.LiveAutomationAllowed
            || policy.ReleaseCommercialReady)
        {
            blockers.Add(ProductLedgerPathPersistedCandidateBlocker.FailedActivePolicyResult);
        }
    }

    private static void AddCanonicalizationBlockers(
        ProductLedgerPathPersistedCandidateRequest request,
        List<ProductLedgerPathPersistedCandidateBlocker> blockers)
    {
        var canonicalization = request.CanonicalizationResult;
        if (canonicalization is null)
        {
            blockers.Add(ProductLedgerPathPersistedCandidateBlocker.MissingCanonicalizationResult);
            return;
        }

        if (canonicalization.Decision != ProductLedgerPathCanonicalizationDecision.CandidateReadinessAllowed
            || !canonicalization.CandidateReadinessAllowed
            || canonicalization.Blockers.Count > 0
            || canonicalization.ProductLedgerPathActive
            || canonicalization.ProductLedgerWriteAllowed
            || canonicalization.ProductRuntimeEnabled
            || canonicalization.ProductServiceRegistrationAllowed
            || canonicalization.ProductCommandHandlersAllowed
            || canonicalization.UiProductActionsAllowed
            || canonicalization.DbProviderCloudNetworkAllowed
            || canonicalization.KmsWormExternalTrustAllowed
            || canonicalization.LiveAutomationAllowed
            || canonicalization.ReleaseCommercialReady
            || string.IsNullOrWhiteSpace(canonicalization.CanonicalCandidatePath)
            || string.IsNullOrWhiteSpace(canonicalization.CanonicalAllowedRootPath))
        {
            blockers.Add(ProductLedgerPathPersistedCandidateBlocker.FailedCanonicalizationResult);
        }
    }

    private static void AddEvidenceReferenceBlockers(
        ProductLedgerPathPersistedCandidateRequest request,
        List<ProductLedgerPathPersistedCandidateBlocker> blockers)
    {
        var evidenceReferences = request.EvidenceReferences;
        if (evidenceReferences is null
            || evidenceReferences.Count == 0
            || evidenceReferences.Any(string.IsNullOrWhiteSpace))
        {
            blockers.Add(ProductLedgerPathPersistedCandidateBlocker.MissingEvidenceReference);
            return;
        }

        foreach (var evidenceReference in evidenceReferences)
        {
            if (!EvidenceReferencePattern.IsMatch(evidenceReference)
                || evidenceReference.Contains("..", StringComparison.Ordinal)
                || evidenceReference.Contains('\\', StringComparison.Ordinal))
            {
                blockers.Add(ProductLedgerPathPersistedCandidateBlocker.MalformedEvidenceReference);
            }

            if (RawPayloadOrSecretMarkers.Any(marker => evidenceReference.Contains(marker, StringComparison.OrdinalIgnoreCase)))
            {
                blockers.Add(ProductLedgerPathPersistedCandidateBlocker.EvidenceReferenceContainsRawPayloadOrSecretMarker);
            }

            if (ProductAuthorityMarkers.Any(marker => evidenceReference.Contains(marker, StringComparison.OrdinalIgnoreCase)))
            {
                blockers.Add(ProductLedgerPathPersistedCandidateBlocker.EvidenceReferenceContainsProductAuthorityClaim);
            }
        }
    }

    private static void AddNoEnableBlockers(
        ProductLedgerPathPersistedCandidateRequest request,
        List<ProductLedgerPathPersistedCandidateBlocker> blockers)
    {
        if (request.ClaimsLocalTempAsProductLedgerPath)
        {
            blockers.Add(ProductLedgerPathPersistedCandidateBlocker.LocalTempClaimedAsProductLedgerPath);
        }

        if (request.RequestsProductLedgerPathActivation)
        {
            blockers.Add(ProductLedgerPathPersistedCandidateBlocker.ProductLedgerPathActivationRequested);
        }

        if (request.RequestsWriterActivation)
        {
            blockers.Add(ProductLedgerPathPersistedCandidateBlocker.ProductWriteRequested);
        }

        if (request.RequestsRuntimeEnablement)
        {
            blockers.Add(ProductLedgerPathPersistedCandidateBlocker.RuntimeEnablementRequested);
        }

        if (request.RequestsProductServiceRegistration)
        {
            blockers.Add(ProductLedgerPathPersistedCandidateBlocker.ProductServiceRegistrationRequested);
        }

        if (request.RequestsProductCommandHandler)
        {
            blockers.Add(ProductLedgerPathPersistedCandidateBlocker.ProductCommandHandlerRequested);
        }

        if (request.RequestsUiProductAction)
        {
            blockers.Add(ProductLedgerPathPersistedCandidateBlocker.UiProductActionRequested);
        }

        if (request.ClaimsProviderCloudNetwork)
        {
            blockers.Add(ProductLedgerPathPersistedCandidateBlocker.ProviderCloudNetworkClaimed);
        }

        if (request.ClaimsWormKmsExternalTrust)
        {
            blockers.Add(ProductLedgerPathPersistedCandidateBlocker.WormKmsExternalTrustClaimed);
        }

        if (request.ClaimsReleaseCommercialReadiness)
        {
            blockers.Add(ProductLedgerPathPersistedCandidateBlocker.ReleaseCommercialReadinessClaimed);
        }
    }

    private static ProductLedgerPathPersistedCandidateResult Result(
        IReadOnlyList<ProductLedgerPathPersistedCandidateBlocker> blockers,
        ProductLedgerPathPersistedCandidateRecord? record)
    {
        var distinct = blockers.Distinct().OrderBy(blocker => blocker.ToString(), StringComparer.Ordinal).ToArray();
        var accepted = distinct.Length == 0 && record is not null;
        return new ProductLedgerPathPersistedCandidateResult(
            Decision: accepted
                ? ProductLedgerPathPersistedCandidateDecision.PersistedCandidateNoWrite
                : (distinct.Contains(ProductLedgerPathPersistedCandidateBlocker.MissingRequest)
                    ? ProductLedgerPathPersistedCandidateDecision.Rejected
                    : ProductLedgerPathPersistedCandidateDecision.Blocked),
            Blockers: distinct,
            Candidate: record,
            CandidatePersistedNoWrite: accepted,
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
            StatusText: PersistedCandidateNoWriteStatus);
    }

    private static string Fingerprint(string canonicalCandidatePath, string canonicalAllowedRootPath)
    {
        var material = $"{canonicalAllowedRootPath}\n{canonicalCandidatePath}";
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(material));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
