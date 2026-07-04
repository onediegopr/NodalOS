using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.Core.Approval;

namespace OneBrain.Recipes.Tests;

[TestClass]
[TestCategory("ApprovalHumanReview")]
[TestCategory("PhaseEApprovalHumanReview")]
public sealed class ProductLedgerPathLocalTempWriterTestOnlyTests
{
    [TestMethod]
    public void LocalTempWriter_WritesTestOnlyLedgerOutsideProductLedgerPath()
    {
        var root = NewTempRoot();
        var result = new ProductLedgerPathLocalTempWriterTestOnly().Append(ReadyRequest(root));

        Assert.AreEqual(ProductLedgerPathLocalTempWriterDecision.WrittenLocalTempTestOnly, result.Decision);
        Assert.IsTrue(result.LocalTempTestOnlyWriteCompleted);
        Assert.IsNotNull(result.LocalTempLedgerPath);
        Assert.IsTrue(result.LocalTempLedgerPath.StartsWith(Path.GetTempPath(), StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(result.ProductLedgerPathActive);
        Assert.IsFalse(result.ProductLedgerWriteAllowed);
        Assert.IsFalse(result.ProductRuntimeEnabled);
        StringAssert.Contains(result.StatusText, "LOCAL_TEMP_WRITER_TEST_ONLY");
        StringAssert.Contains(result.StatusText, "NOT_PRODUCT_LEDGER_PATH");
    }

    [TestMethod]
    public void LocalTempWriter_RejectsUnsafeRecipeCorpus()
    {
        var root = NewTempRoot();
        var result = new ProductLedgerPathLocalTempWriterTestOnly().Append(
            ReadyRequest(root) with
            {
                ClaimsLocalTempAsProductLedgerPath = true,
                RequestsWriterActivation = true,
                RequestsRuntimeEnablement = true,
                RequestsProductLedgerPathActivation = true,
                RequestsProductServiceRegistration = true,
                RequestsProductCommandHandler = true,
                RequestsUiProductAction = true,
                ClaimsProviderCloudNetwork = true,
                ClaimsWormKmsExternalTrust = true,
                ClaimsReleaseCommercialReadiness = true
            });

        Assert.AreEqual(ProductLedgerPathLocalTempWriterDecision.Blocked, result.Decision);
        CollectionAssert.Contains(result.Blockers.ToArray(), ProductLedgerPathLocalTempWriterBlocker.LocalTempClaimedAsProductLedgerPath);
        CollectionAssert.Contains(result.Blockers.ToArray(), ProductLedgerPathLocalTempWriterBlocker.ProductWriteRequested);
        CollectionAssert.Contains(result.Blockers.ToArray(), ProductLedgerPathLocalTempWriterBlocker.RuntimeEnablementRequested);
        CollectionAssert.Contains(result.Blockers.ToArray(), ProductLedgerPathLocalTempWriterBlocker.ProductLedgerPathActivationRequested);
        CollectionAssert.Contains(result.Blockers.ToArray(), ProductLedgerPathLocalTempWriterBlocker.ProductServiceRegistrationRequested);
        CollectionAssert.Contains(result.Blockers.ToArray(), ProductLedgerPathLocalTempWriterBlocker.ProductCommandHandlerRequested);
        CollectionAssert.Contains(result.Blockers.ToArray(), ProductLedgerPathLocalTempWriterBlocker.UiProductActionRequested);
        CollectionAssert.Contains(result.Blockers.ToArray(), ProductLedgerPathLocalTempWriterBlocker.ProviderCloudNetworkClaimed);
        CollectionAssert.Contains(result.Blockers.ToArray(), ProductLedgerPathLocalTempWriterBlocker.WormKmsExternalTrustClaimed);
        CollectionAssert.Contains(result.Blockers.ToArray(), ProductLedgerPathLocalTempWriterBlocker.ReleaseCommercialReadinessClaimed);
        Assert.IsFalse(result.ProductLedgerPathActive);
        Assert.IsFalse(result.ProductLedgerWriteAllowed);
        Assert.IsFalse(result.ProductRuntimeEnabled);
    }

    private static ProductLedgerPathLocalTempWriterRequest ReadyRequest(string root) =>
        new(
            WriterScaffoldResult: ReadyScaffold(),
            CandidateId: "ledger-candidate-001",
            LocalTempRoot: root,
            SafePayloadHash: new string('d', 64),
            EvidenceMetadata: new Dictionary<string, string>
            {
                ["redaction"] = "redacted-before-persistence",
                ["failure"] = "replay-rollback-evidence"
            },
            ExplicitLocalTempTestOnlyMode: true,
            ClaimsLocalTempAsProductLedgerPath: false,
            HasRedactionBeforePersistenceEvidence: true,
            HasFailureReplayRollbackEvidence: true,
            RequestsWriterActivation: false,
            RequestsRuntimeEnablement: false,
            RequestsProductLedgerPathActivation: false,
            RequestsProductServiceRegistration: false,
            RequestsProductCommandHandler: false,
            RequestsUiProductAction: false,
            ClaimsProviderCloudNetwork: false,
            ClaimsWormKmsExternalTrust: false,
            ClaimsReleaseCommercialReadiness: false);

    private static ProductLedgerPathWriterScaffoldResult ReadyScaffold() =>
        new ProductLedgerPathWriterScaffoldDisabled().Evaluate(new ProductLedgerPathWriterScaffoldRequest(
            PersistedCandidateResult: ReadyPersistedCandidate(),
            ExplicitDisabledTestOnlyMode: true,
            NoProductWriterAssertion: true,
            NoProductWriteAssertion: true,
            NoRuntimeEnablementAssertion: true,
            NoProductLedgerPathActivationAssertion: true,
            NoProductServiceRegistrationAssertion: true,
            NoProductCommandHandlerAssertion: true,
            NoUiProductActionAssertion: true,
            NoProviderCloudNetworkAssertion: true,
            NoWormKmsExternalTrustAssertion: true,
            NoReleaseCommercialAssertion: true,
            HasRedactionBeforePersistenceEvidence: true,
            HasFailureReplayRollbackEvidence: true,
            RequestsWriterActivation: false,
            RequestsRuntimeEnablement: false,
            RequestsProductLedgerPathActivation: false,
            RequestsProductServiceRegistration: false,
            RequestsProductCommandHandler: false,
            RequestsUiProductAction: false,
            ClaimsProviderCloudNetwork: false,
            ClaimsWormKmsExternalTrust: false,
            ClaimsReleaseCommercialReadiness: false));

    private static ProductLedgerPathPersistedCandidateResult ReadyPersistedCandidate()
    {
        var root = RepoRoot();
        var canonicalization = new ProductLedgerPathCanonicalizationValidator().Validate(
            new ProductLedgerPathCanonicalizationRequest(
                CandidatePath: Path.Combine(root, "src", "OneBrain.Core"),
                AllowedRootPath: root,
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
        var policy = new ProductLedgerPathActivePolicy().Evaluate(
            new ProductLedgerPathActivePolicyRequest(
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
                ClaimsWormKmsExternalTrust: false));
        return new ProductLedgerPathPersistedCandidateRegistry().Persist(
            new ProductLedgerPathPersistedCandidateRequest(
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
                ClaimsReleaseCommercialReadiness: false));
    }

    private static string NewTempRoot()
    {
        var root = Path.Combine(Path.GetTempPath(), "nodal-os-product-ledger-path-local-temp-writer-recipes-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        return root;
    }

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
                "ProductLedgerPathLocalTempWriterTestOnly.cs")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        Assert.Fail("repo root not found");
        return string.Empty;
    }
}
