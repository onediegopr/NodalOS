using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.Core.Approval;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("ApprovalHumanReview")]
[TestCategory("PhaseEApprovalHumanReview")]
public sealed class ProductLedgerPathWriterScaffoldDisabledTests
{
    [TestMethod]
    public void WriterScaffold_FailsClosedByDefault()
    {
        var result = new ProductLedgerPathWriterScaffoldDisabled().Evaluate(null);

        Assert.AreEqual(ProductLedgerPathWriterScaffoldDecision.Rejected, result.Decision);
        CollectionAssert.Contains(result.Blockers.ToArray(), ProductLedgerPathWriterScaffoldBlocker.MissingRequest);
        AssertNoProductEnablement(result);
    }

    [TestMethod]
    public void WriterScaffold_BlocksMissingOrFailedCandidate()
    {
        var ready = ReadyRequest();
        var cases = new Dictionary<ProductLedgerPathWriterScaffoldRequest, ProductLedgerPathWriterScaffoldBlocker>
        {
            [ready with { PersistedCandidateResult = null }] = ProductLedgerPathWriterScaffoldBlocker.MissingPersistedCandidateResult,
            [ready with { PersistedCandidateResult = new ProductLedgerPathPersistedCandidateRegistry().Persist(null) }] = ProductLedgerPathWriterScaffoldBlocker.FailedPersistedCandidateResult
        };

        foreach (var testCase in cases)
        {
            var result = new ProductLedgerPathWriterScaffoldDisabled().Evaluate(testCase.Key);

            Assert.AreEqual(ProductLedgerPathWriterScaffoldDecision.Blocked, result.Decision, testCase.Value.ToString());
            CollectionAssert.Contains(result.Blockers.ToArray(), testCase.Value, testCase.Value.ToString());
            AssertNoProductEnablement(result);
        }
    }

    [TestMethod]
    public void WriterScaffold_BlocksMissingModeEvidenceAndAssertions()
    {
        var ready = ReadyRequest();
        var cases = new Dictionary<ProductLedgerPathWriterScaffoldRequest, ProductLedgerPathWriterScaffoldBlocker>
        {
            [ready with { ExplicitDisabledTestOnlyMode = false }] = ProductLedgerPathWriterScaffoldBlocker.MissingExplicitDisabledTestOnlyMode,
            [ready with { NoProductWriterAssertion = false }] = ProductLedgerPathWriterScaffoldBlocker.MissingNoProductWriterAssertion,
            [ready with { HasRedactionBeforePersistenceEvidence = false }] = ProductLedgerPathWriterScaffoldBlocker.MissingRedactionBeforePersistenceEvidence,
            [ready with { HasFailureReplayRollbackEvidence = false }] = ProductLedgerPathWriterScaffoldBlocker.MissingFailureReplayRollbackEvidence
        };

        foreach (var testCase in cases)
        {
            var result = new ProductLedgerPathWriterScaffoldDisabled().Evaluate(testCase.Key);

            Assert.AreEqual(ProductLedgerPathWriterScaffoldDecision.Blocked, result.Decision, testCase.Value.ToString());
            CollectionAssert.Contains(result.Blockers.ToArray(), testCase.Value, testCase.Value.ToString());
            AssertNoProductEnablement(result);
        }
    }

    [TestMethod]
    public void WriterScaffold_BlocksProductEnablementRequests()
    {
        var ready = ReadyRequest();
        var cases = new Dictionary<ProductLedgerPathWriterScaffoldRequest, ProductLedgerPathWriterScaffoldBlocker>
        {
            [ready with { NoProductWriteAssertion = false }] = ProductLedgerPathWriterScaffoldBlocker.ProductWriteRequested,
            [ready with { RequestsWriterActivation = true }] = ProductLedgerPathWriterScaffoldBlocker.ProductWriteRequested,
            [ready with { NoRuntimeEnablementAssertion = false }] = ProductLedgerPathWriterScaffoldBlocker.RuntimeEnablementRequested,
            [ready with { RequestsRuntimeEnablement = true }] = ProductLedgerPathWriterScaffoldBlocker.RuntimeEnablementRequested,
            [ready with { NoProductLedgerPathActivationAssertion = false }] = ProductLedgerPathWriterScaffoldBlocker.ProductLedgerPathActivationRequested,
            [ready with { RequestsProductLedgerPathActivation = true }] = ProductLedgerPathWriterScaffoldBlocker.ProductLedgerPathActivationRequested,
            [ready with { NoProductServiceRegistrationAssertion = false }] = ProductLedgerPathWriterScaffoldBlocker.ProductServiceRegistrationRequested,
            [ready with { RequestsProductServiceRegistration = true }] = ProductLedgerPathWriterScaffoldBlocker.ProductServiceRegistrationRequested,
            [ready with { NoProductCommandHandlerAssertion = false }] = ProductLedgerPathWriterScaffoldBlocker.ProductCommandHandlerRequested,
            [ready with { RequestsProductCommandHandler = true }] = ProductLedgerPathWriterScaffoldBlocker.ProductCommandHandlerRequested,
            [ready with { NoUiProductActionAssertion = false }] = ProductLedgerPathWriterScaffoldBlocker.UiProductActionRequested,
            [ready with { RequestsUiProductAction = true }] = ProductLedgerPathWriterScaffoldBlocker.UiProductActionRequested,
            [ready with { ClaimsProviderCloudNetwork = true }] = ProductLedgerPathWriterScaffoldBlocker.ProviderCloudNetworkClaimed,
            [ready with { ClaimsWormKmsExternalTrust = true }] = ProductLedgerPathWriterScaffoldBlocker.WormKmsExternalTrustClaimed,
            [ready with { ClaimsReleaseCommercialReadiness = true }] = ProductLedgerPathWriterScaffoldBlocker.ReleaseCommercialReadinessClaimed
        };

        foreach (var testCase in cases)
        {
            var result = new ProductLedgerPathWriterScaffoldDisabled().Evaluate(testCase.Key);

            Assert.AreEqual(ProductLedgerPathWriterScaffoldDecision.Blocked, result.Decision, testCase.Value.ToString());
            CollectionAssert.Contains(result.Blockers.ToArray(), testCase.Value, testCase.Value.ToString());
            AssertNoProductEnablement(result);
        }
    }

    [TestMethod]
    public void WriterScaffold_AllowsOnlyDisabledTestOnlyNoWriteScaffold()
    {
        var result = new ProductLedgerPathWriterScaffoldDisabled().Evaluate(ReadyRequest());

        Assert.AreEqual(ProductLedgerPathWriterScaffoldDecision.DisabledWriterScaffoldReady, result.Decision);
        Assert.IsTrue(result.DisabledWriterScaffoldReady);
        Assert.AreEqual(0, result.Blockers.Count);
        StringAssert.Contains(result.StatusText, "DISABLED_WRITER_SCAFFOLD_TEST_ONLY");
        StringAssert.Contains(result.StatusText, "NO_ACTIVE_PRODUCT_LEDGER_PATH");
        StringAssert.Contains(result.StatusText, "NO_PRODUCT_LEDGER_WRITE");
        StringAssert.Contains(result.StatusText, "NO_PRODUCT_RUNTIME_ENABLEMENT");
        AssertNoProductEnablement(result);
    }

    private static ProductLedgerPathWriterScaffoldRequest ReadyRequest() =>
        new(
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
            ClaimsReleaseCommercialReadiness: false);

    private static ProductLedgerPathPersistedCandidateResult ReadyPersistedCandidate()
    {
        var canonicalization = new ProductLedgerPathCanonicalizationValidator().Validate(
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

    private static void AssertNoProductEnablement(ProductLedgerPathWriterScaffoldResult result)
    {
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
                "ProductLedgerPathWriterScaffoldDisabled.cs")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        Assert.Fail("repo root not found");
        return string.Empty;
    }
}
