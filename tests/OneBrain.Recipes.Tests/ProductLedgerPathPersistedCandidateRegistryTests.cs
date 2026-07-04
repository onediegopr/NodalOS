using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.Core.Approval;

namespace OneBrain.Recipes.Tests;

[TestClass]
[TestCategory("ApprovalHumanReview")]
[TestCategory("PhaseEApprovalHumanReview")]
public sealed class ProductLedgerPathPersistedCandidateRegistryTests
{
    [TestMethod]
    public void PersistedCandidateRegistry_ProducesLocalMemoryCandidateOnly()
    {
        var registry = new ProductLedgerPathPersistedCandidateRegistry();

        var result = registry.Persist(ReadyRequest());

        Assert.AreEqual(ProductLedgerPathPersistedCandidateDecision.PersistedCandidateNoWrite, result.Decision);
        Assert.IsNotNull(result.Candidate);
        Assert.AreEqual(1, registry.Snapshot().Count);
        Assert.IsFalse(result.ProductLedgerPathActive);
        Assert.IsFalse(result.ProductLedgerWriteAllowed);
        Assert.IsFalse(result.ProductRuntimeEnabled);
        Assert.IsFalse(result.ProductServiceRegistrationAllowed);
        Assert.IsFalse(result.ProductCommandHandlersAllowed);
        Assert.IsFalse(result.UiProductActionsAllowed);
        Assert.IsFalse(result.DbProviderCloudNetworkAllowed);
        Assert.IsFalse(result.KmsWormExternalTrustAllowed);
        Assert.IsFalse(result.LiveAutomationAllowed);
        Assert.IsFalse(result.ReleaseCommercialReady);
        StringAssert.Contains(result.StatusText, "PERSISTED_ACTIVE_PATH_CANDIDATE_LOCAL_ONLY_NO_WRITE");
        StringAssert.Contains(result.StatusText, "NO_ACTIVE_PRODUCT_LEDGER_PATH");
        StringAssert.Contains(result.StatusText, "NO_PRODUCT_LEDGER_WRITE");
    }

    [TestMethod]
    public void PersistedCandidateRegistry_RejectsUnsafeRecipeCorpusWithoutProductEffects()
    {
        var result = new ProductLedgerPathPersistedCandidateRegistry().Persist(
            ReadyRequest() with
            {
                EvidenceReferences = ["docs/qa/raw-payload-ledger-active-secret=abc"],
                RequestsProductLedgerPathActivation = true,
                RequestsWriterActivation = true,
                RequestsRuntimeEnablement = true,
                RequestsProductServiceRegistration = true,
                RequestsProductCommandHandler = true,
                RequestsUiProductAction = true,
                ClaimsProviderCloudNetwork = true,
                ClaimsWormKmsExternalTrust = true,
                ClaimsReleaseCommercialReadiness = true
            });

        Assert.AreEqual(ProductLedgerPathPersistedCandidateDecision.Blocked, result.Decision);
        CollectionAssert.Contains(result.Blockers.ToArray(), ProductLedgerPathPersistedCandidateBlocker.EvidenceReferenceContainsRawPayloadOrSecretMarker);
        CollectionAssert.Contains(result.Blockers.ToArray(), ProductLedgerPathPersistedCandidateBlocker.EvidenceReferenceContainsProductAuthorityClaim);
        CollectionAssert.Contains(result.Blockers.ToArray(), ProductLedgerPathPersistedCandidateBlocker.ProductLedgerPathActivationRequested);
        CollectionAssert.Contains(result.Blockers.ToArray(), ProductLedgerPathPersistedCandidateBlocker.ProductWriteRequested);
        CollectionAssert.Contains(result.Blockers.ToArray(), ProductLedgerPathPersistedCandidateBlocker.RuntimeEnablementRequested);
        CollectionAssert.Contains(result.Blockers.ToArray(), ProductLedgerPathPersistedCandidateBlocker.ProductServiceRegistrationRequested);
        CollectionAssert.Contains(result.Blockers.ToArray(), ProductLedgerPathPersistedCandidateBlocker.ProductCommandHandlerRequested);
        CollectionAssert.Contains(result.Blockers.ToArray(), ProductLedgerPathPersistedCandidateBlocker.UiProductActionRequested);
        CollectionAssert.Contains(result.Blockers.ToArray(), ProductLedgerPathPersistedCandidateBlocker.ProviderCloudNetworkClaimed);
        CollectionAssert.Contains(result.Blockers.ToArray(), ProductLedgerPathPersistedCandidateBlocker.WormKmsExternalTrustClaimed);
        CollectionAssert.Contains(result.Blockers.ToArray(), ProductLedgerPathPersistedCandidateBlocker.ReleaseCommercialReadinessClaimed);
        Assert.IsFalse(result.ProductLedgerPathActive);
        Assert.IsFalse(result.ProductLedgerWriteAllowed);
        Assert.IsFalse(result.ProductRuntimeEnabled);
        Assert.IsFalse(result.ReleaseCommercialReady);
    }

    private static ProductLedgerPathPersistedCandidateRequest ReadyRequest()
    {
        var canonicalization = ReadyCanonicalization();
        var policy = new ProductLedgerPathActivePolicy().Evaluate(ReadyPolicyRequest(canonicalization));
        return new ProductLedgerPathPersistedCandidateRequest(
            CandidateId: "ledger-candidate-001",
            ActivePolicyResult: policy,
            CanonicalizationResult: canonicalization,
            EvidenceReferences: ["docs/qa/product-ledger-path-persisted-candidate-local-only-no-write/report.md"],
            ClaimsLocalTempAsProductLedgerPath: false,
            RequestsProductLedgerPathActivation: false,
            RequestsWriterActivation: false,
            RequestsRuntimeEnablement: false,
            RequestsProductServiceRegistration: false,
            RequestsProductCommandHandler: false,
            RequestsUiProductAction: false,
            ClaimsProviderCloudNetwork: false,
            ClaimsWormKmsExternalTrust: false,
            ClaimsReleaseCommercialReadiness: false);
    }

    private static ProductLedgerPathCanonicalizationResult ReadyCanonicalization() =>
        new ProductLedgerPathCanonicalizationValidator().Validate(
            new ProductLedgerPathCanonicalizationRequest(
                CandidatePath: Path.Combine(RepoRoot(), "src", "OneBrain.Core"),
                AllowedRootPath: RepoRoot(),
                ExplicitLocalOnlyMode: true,
                NoProductLedgerWriteAssertion: true,
                NoRuntimeEnablementAssertion: true,
                NoReleaseCommercialAssertion: true,
                ClaimsProductLedgerActive: false,
                ClaimsProductReady: false,
                ClaimsExternalTrust: false,
                ClaimsWormKmsCloud: false,
                ClaimsLocalTempAsProductLedgerPath: false,
                HasResolvedReparsePointEvidence: true,
                HasTocTouMitigationEvidence: true,
                HardlinkOrMountAliasRiskUnresolved: false));

    private static ProductLedgerPathActivePolicyRequest ReadyPolicyRequest(ProductLedgerPathCanonicalizationResult canonicalization) =>
        new(
            CanonicalizationResult: canonicalization,
            HasCanonicalAllowedBoundaryEvidence: true,
            HasNoUnresolvedReparseSymlinkJunctionRiskEvidence: true,
            HasTocTouMitigationEvidence: true,
            HasRedactionPolicyEvidence: true,
            HasRetentionPolicyEvidence: true,
            HasReplayFailureEvidence: true,
            HasRollbackNonRollbackClassification: true,
            HasAuthorityEvidence: true,
            AuthorityEvidenceIsNonProduct: true,
            TreatsHumanGoAsProductAuthority: false,
            EvidenceReferences: ["docs/qa/product-ledger-path-active-policy-local-only-no-write/report.md"],
            EvidenceReferencesAreStale: false,
            EvidenceReferencesAreInconsistent: false,
            ClaimsLocalTempAsProductLedgerPath: false,
            NoProductWriteAssertion: true,
            NoRuntimeEnablementAssertion: true,
            NoReleaseCommercialAssertion: true,
            NoProviderCloudNetworkAssertion: true,
            NoWormKmsExternalTrustAssertion: true,
            RequestsProductLedgerPathActivation: false,
            RequestsWriterActivation: false,
            RequestsRuntimeEnablement: false,
            RequestsProductServiceRegistration: false,
            RequestsProductCommandHandler: false,
            RequestsUiProductAction: false,
            ClaimsReleaseCommercialReadiness: false,
            ClaimsProviderCloudNetwork: false,
            ClaimsWormKmsExternalTrust: false);

    private static string RepoRoot()
    {
        var directory = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(
                directory.FullName,
                "src",
                "OneBrain.Core",
                "Approval",
                "ProductLedgerPathReadinessScaffold.cs")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        Assert.Fail("repo root not found");
        return string.Empty;
    }
}
