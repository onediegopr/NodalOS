using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.Core.Approval;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("ApprovalHumanReview")]
[TestCategory("PhaseEApprovalHumanReview")]
public sealed class ProductLedgerPathPersistedCandidateRegistryTests
{
    [TestMethod]
    public void PersistedCandidateRegistry_FailsClosedByDefault()
    {
        var result = new ProductLedgerPathPersistedCandidateRegistry().Persist(null);

        Assert.AreEqual(ProductLedgerPathPersistedCandidateDecision.Rejected, result.Decision);
        CollectionAssert.Contains(result.Blockers.ToArray(), ProductLedgerPathPersistedCandidateBlocker.MissingRequest);
        AssertNoProductEnablement(result);
    }

    [TestMethod]
    public void PersistedCandidateRegistry_BlocksMissingOrFailedPolicyAndCanonicalization()
    {
        var ready = ReadyRequest();
        var cases = new Dictionary<ProductLedgerPathPersistedCandidateRequest, ProductLedgerPathPersistedCandidateBlocker>
        {
            [ready with { ActivePolicyResult = null }] = ProductLedgerPathPersistedCandidateBlocker.MissingActivePolicyResult,
            [ready with { ActivePolicyResult = new ProductLedgerPathActivePolicy().Evaluate(null) }] = ProductLedgerPathPersistedCandidateBlocker.FailedActivePolicyResult,
            [ready with { CanonicalizationResult = null }] = ProductLedgerPathPersistedCandidateBlocker.MissingCanonicalizationResult,
            [ready with { CanonicalizationResult = new ProductLedgerPathCanonicalizationValidator().Validate(null) }] = ProductLedgerPathPersistedCandidateBlocker.FailedCanonicalizationResult
        };

        foreach (var testCase in cases)
        {
            var result = new ProductLedgerPathPersistedCandidateRegistry().Persist(testCase.Key);

            Assert.AreEqual(ProductLedgerPathPersistedCandidateDecision.Blocked, result.Decision, testCase.Value.ToString());
            CollectionAssert.Contains(result.Blockers.ToArray(), testCase.Value, testCase.Value.ToString());
            AssertNoProductEnablement(result);
        }
    }

    [TestMethod]
    public void PersistedCandidateRegistry_BlocksCandidateIdAndEvidenceReferenceRisks()
    {
        var ready = ReadyRequest();
        var cases = new Dictionary<ProductLedgerPathPersistedCandidateRequest, ProductLedgerPathPersistedCandidateBlocker>
        {
            [ready with { CandidateId = null }] = ProductLedgerPathPersistedCandidateBlocker.MissingCandidateId,
            [ready with { CandidateId = "../escape" }] = ProductLedgerPathPersistedCandidateBlocker.InvalidCandidateId,
            [ready with { EvidenceReferences = [] }] = ProductLedgerPathPersistedCandidateBlocker.MissingEvidenceReference,
            [ready with { EvidenceReferences = ["../escape"] }] = ProductLedgerPathPersistedCandidateBlocker.MalformedEvidenceReference,
            [ready with { EvidenceReferences = ["docs/qa/raw-payload-secret=abc"] }] = ProductLedgerPathPersistedCandidateBlocker.EvidenceReferenceContainsRawPayloadOrSecretMarker,
            [ready with { EvidenceReferences = ["docs/qa/ledger-active-writer-ready"] }] = ProductLedgerPathPersistedCandidateBlocker.EvidenceReferenceContainsProductAuthorityClaim
        };

        foreach (var testCase in cases)
        {
            var result = new ProductLedgerPathPersistedCandidateRegistry().Persist(testCase.Key);

            Assert.AreEqual(ProductLedgerPathPersistedCandidateDecision.Blocked, result.Decision, testCase.Value.ToString());
            CollectionAssert.Contains(result.Blockers.ToArray(), testCase.Value, testCase.Value.ToString());
            AssertNoProductEnablement(result);
        }
    }

    [TestMethod]
    public void PersistedCandidateRegistry_BlocksProductEnablementRequests()
    {
        var ready = ReadyRequest();
        var cases = new Dictionary<ProductLedgerPathPersistedCandidateRequest, ProductLedgerPathPersistedCandidateBlocker>
        {
            [ready with { ClaimsLocalTempAsProductLedgerPath = true }] = ProductLedgerPathPersistedCandidateBlocker.LocalTempClaimedAsProductLedgerPath,
            [ready with { RequestsProductLedgerPathActivation = true }] = ProductLedgerPathPersistedCandidateBlocker.ProductLedgerPathActivationRequested,
            [ready with { RequestsWriterActivation = true }] = ProductLedgerPathPersistedCandidateBlocker.ProductWriteRequested,
            [ready with { RequestsRuntimeEnablement = true }] = ProductLedgerPathPersistedCandidateBlocker.RuntimeEnablementRequested,
            [ready with { RequestsProductServiceRegistration = true }] = ProductLedgerPathPersistedCandidateBlocker.ProductServiceRegistrationRequested,
            [ready with { RequestsProductCommandHandler = true }] = ProductLedgerPathPersistedCandidateBlocker.ProductCommandHandlerRequested,
            [ready with { RequestsUiProductAction = true }] = ProductLedgerPathPersistedCandidateBlocker.UiProductActionRequested,
            [ready with { ClaimsProviderCloudNetwork = true }] = ProductLedgerPathPersistedCandidateBlocker.ProviderCloudNetworkClaimed,
            [ready with { ClaimsWormKmsExternalTrust = true }] = ProductLedgerPathPersistedCandidateBlocker.WormKmsExternalTrustClaimed,
            [ready with { ClaimsReleaseCommercialReadiness = true }] = ProductLedgerPathPersistedCandidateBlocker.ReleaseCommercialReadinessClaimed
        };

        foreach (var testCase in cases)
        {
            var result = new ProductLedgerPathPersistedCandidateRegistry().Persist(testCase.Key);

            Assert.AreEqual(ProductLedgerPathPersistedCandidateDecision.Blocked, result.Decision, testCase.Value.ToString());
            CollectionAssert.Contains(result.Blockers.ToArray(), testCase.Value, testCase.Value.ToString());
            AssertNoProductEnablement(result);
        }
    }

    [TestMethod]
    public void PersistedCandidateRegistry_PersistsCandidateOnlyInLocalMemoryNoWrite()
    {
        var registry = new ProductLedgerPathPersistedCandidateRegistry();

        var result = registry.Persist(ReadyRequest());

        Assert.AreEqual(ProductLedgerPathPersistedCandidateDecision.PersistedCandidateNoWrite, result.Decision);
        Assert.IsTrue(result.CandidatePersistedNoWrite);
        Assert.IsNotNull(result.Candidate);
        Assert.AreEqual("ledger-candidate-001", result.Candidate.CandidateId);
        Assert.AreEqual(64, result.Candidate.CandidateFingerprint.Length);
        Assert.AreEqual(1, registry.Snapshot().Count);
        Assert.IsNotNull(registry.FindCandidate("ledger-candidate-001"));
        StringAssert.Contains(result.StatusText, "PERSISTED_ACTIVE_PATH_CANDIDATE_LOCAL_ONLY_NO_WRITE");
        StringAssert.Contains(result.StatusText, "NO_ACTIVE_PRODUCT_LEDGER_PATH");
        StringAssert.Contains(result.StatusText, "NO_PRODUCT_LEDGER_WRITE");
        StringAssert.Contains(result.StatusText, "NO_PRODUCT_RUNTIME_ENABLEMENT");
        AssertNoProductEnablement(result);

        var duplicate = registry.Persist(ReadyRequest());
        CollectionAssert.Contains(duplicate.Blockers.ToArray(), ProductLedgerPathPersistedCandidateBlocker.DuplicateCandidateId);
        AssertNoProductEnablement(duplicate);
    }

    [TestMethod]
    public void PersistedCandidateRegistry_SourceContainsNoFilesystemWriterRegistrationHandlersOrProviders()
    {
        var source = File.ReadAllText(Path.Combine(
            RepoRoot(),
            "src",
            "OneBrain.Core",
            "Approval",
            "ProductLedgerPathPersistedCandidateRegistry.cs"));
        var forbiddenFragments = new[]
        {
            "Add" + "Singleton",
            "Add" + "Scoped",
            "Add" + "Transient",
            "IHosted" + "Service",
            "Map" + "Post",
            "Map" + "Get",
            "Add" + "CommandHandler",
            "ICommand" + "Handler",
            "Run" + "ProductAction",
            "Product" + "ActionButton",
            "Http" + "Client",
            "Web" + "Socket",
            "Db" + "Context",
            "Migration" + "Builder",
            "Save" + "Changes",
            "File." + "AppendAllText",
            "File." + "WriteAllText",
            "Directory." + "CreateDirectory",
            "File." + "Create",
            "File" + "Stream",
            "Release" + "Ready = true",
            "Commercial" + "Ready = true"
        };

        foreach (var fragment in forbiddenFragments)
        {
            Assert.IsFalse(source.Contains(fragment, StringComparison.Ordinal), fragment);
        }
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

    private static void AssertNoProductEnablement(ProductLedgerPathPersistedCandidateResult result)
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
                "ProductLedgerPathPersistedCandidateRegistry.cs")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        Assert.Fail("repo root not found");
        return string.Empty;
    }
}
