using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.Core.Approval;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("ApprovalHumanReview")]
[TestCategory("PhaseEApprovalHumanReview")]
public sealed class ProductLedgerPathLocalTempWriterTestOnlyTests
{
    [TestMethod]
    public void LocalTempWriter_FailsClosedByDefault()
    {
        var result = new ProductLedgerPathLocalTempWriterTestOnly().Append(null);

        Assert.AreEqual(ProductLedgerPathLocalTempWriterDecision.Rejected, result.Decision);
        CollectionAssert.Contains(result.Blockers.ToArray(), ProductLedgerPathLocalTempWriterBlocker.MissingRequest);
        AssertNoProductEnablement(result);
    }

    [TestMethod]
    public void LocalTempWriter_BlocksMissingOrFailedScaffold()
    {
        var ready = ReadyRequest(NewTempRoot());
        var cases = new Dictionary<ProductLedgerPathLocalTempWriterRequest, ProductLedgerPathLocalTempWriterBlocker>
        {
            [ready with { WriterScaffoldResult = null }] = ProductLedgerPathLocalTempWriterBlocker.MissingWriterScaffoldResult,
            [ready with { WriterScaffoldResult = new ProductLedgerPathWriterScaffoldDisabled().Evaluate(null) }] = ProductLedgerPathLocalTempWriterBlocker.FailedWriterScaffoldResult
        };

        foreach (var testCase in cases)
        {
            var result = new ProductLedgerPathLocalTempWriterTestOnly().Append(testCase.Key);

            Assert.AreEqual(ProductLedgerPathLocalTempWriterDecision.Blocked, result.Decision, testCase.Value.ToString());
            CollectionAssert.Contains(result.Blockers.ToArray(), testCase.Value, testCase.Value.ToString());
            AssertNoProductEnablement(result);
        }
    }

    [TestMethod]
    public void LocalTempWriter_BlocksNonTempRootsAndUnsafeEvidence()
    {
        var ready = ReadyRequest(NewTempRoot());
        var cases = new Dictionary<ProductLedgerPathLocalTempWriterRequest, ProductLedgerPathLocalTempWriterBlocker>
        {
            [ready with { CandidateId = "" }] = ProductLedgerPathLocalTempWriterBlocker.MissingCandidateId,
            [ready with { CandidateId = "../bad" }] = ProductLedgerPathLocalTempWriterBlocker.InvalidCandidateId,
            [ready with { LocalTempRoot = "" }] = ProductLedgerPathLocalTempWriterBlocker.MissingLocalTempRoot,
            [ready with { LocalTempRoot = RepoRoot() }] = ProductLedgerPathLocalTempWriterBlocker.LocalTempRootOutsideSystemTemp,
            [ready with { ExplicitLocalTempTestOnlyMode = false }] = ProductLedgerPathLocalTempWriterBlocker.MissingExplicitLocalTempTestOnlyMode,
            [ready with { SafePayloadHash = "" }] = ProductLedgerPathLocalTempWriterBlocker.MissingSafePayloadHash,
            [ready with { SafePayloadHash = "raw-secret" }] = ProductLedgerPathLocalTempWriterBlocker.UnsafePayloadHash,
            [ready with { SafePayloadHash = new string('G', 64) }] = ProductLedgerPathLocalTempWriterBlocker.UnsafePayloadHash,
            [ready with { EvidenceMetadata = new Dictionary<string, string> { ["secret"] = "redacted" } }] = ProductLedgerPathLocalTempWriterBlocker.UnsafeEvidenceMetadata,
            [ready with { EvidenceMetadata = new Dictionary<string, string> { ["ref"] = "..\\secret" } }] = ProductLedgerPathLocalTempWriterBlocker.UnsafeEvidenceMetadata,
            [ready with { EvidenceMetadata = new Dictionary<string, string> { ["payload"] = "bearer redacted" } }] = ProductLedgerPathLocalTempWriterBlocker.UnsafeEvidenceMetadata,
            [ready with { EvidenceMetadata = new Dictionary<string, string> { ["client"] = "client_secret_redacted" } }] = ProductLedgerPathLocalTempWriterBlocker.UnsafeEvidenceMetadata,
            [ready with { EvidenceMetadata = new Dictionary<string, string> { ["path"] = "C:\\Users\\synthetic" } }] = ProductLedgerPathLocalTempWriterBlocker.UnsafeEvidenceMetadata,
            [ready with { EvidenceMetadata = new Dictionary<string, string> { ["ref"] = new string('x', 129) } }] = ProductLedgerPathLocalTempWriterBlocker.UnsafeEvidenceMetadata
        };

        foreach (var testCase in cases)
        {
            var result = new ProductLedgerPathLocalTempWriterTestOnly().Append(testCase.Key);

            Assert.AreEqual(ProductLedgerPathLocalTempWriterDecision.Blocked, result.Decision, testCase.Value.ToString());
            CollectionAssert.Contains(result.Blockers.ToArray(), testCase.Value, testCase.Value.ToString());
            AssertNoProductEnablement(result);
        }
    }

    [TestMethod]
    public void LocalTempWriter_BlocksProductEnablementRequests()
    {
        var ready = ReadyRequest(NewTempRoot());
        var cases = new Dictionary<ProductLedgerPathLocalTempWriterRequest, ProductLedgerPathLocalTempWriterBlocker>
        {
            [ready with { ClaimsLocalTempAsProductLedgerPath = true }] = ProductLedgerPathLocalTempWriterBlocker.LocalTempClaimedAsProductLedgerPath,
            [ready with { RequestsWriterActivation = true }] = ProductLedgerPathLocalTempWriterBlocker.ProductWriteRequested,
            [ready with { RequestsRuntimeEnablement = true }] = ProductLedgerPathLocalTempWriterBlocker.RuntimeEnablementRequested,
            [ready with { RequestsProductLedgerPathActivation = true }] = ProductLedgerPathLocalTempWriterBlocker.ProductLedgerPathActivationRequested,
            [ready with { RequestsProductServiceRegistration = true }] = ProductLedgerPathLocalTempWriterBlocker.ProductServiceRegistrationRequested,
            [ready with { RequestsProductCommandHandler = true }] = ProductLedgerPathLocalTempWriterBlocker.ProductCommandHandlerRequested,
            [ready with { RequestsUiProductAction = true }] = ProductLedgerPathLocalTempWriterBlocker.UiProductActionRequested,
            [ready with { ClaimsProviderCloudNetwork = true }] = ProductLedgerPathLocalTempWriterBlocker.ProviderCloudNetworkClaimed,
            [ready with { ClaimsWormKmsExternalTrust = true }] = ProductLedgerPathLocalTempWriterBlocker.WormKmsExternalTrustClaimed,
            [ready with { ClaimsReleaseCommercialReadiness = true }] = ProductLedgerPathLocalTempWriterBlocker.ReleaseCommercialReadinessClaimed
        };

        foreach (var testCase in cases)
        {
            var result = new ProductLedgerPathLocalTempWriterTestOnly().Append(testCase.Key);

            Assert.AreEqual(ProductLedgerPathLocalTempWriterDecision.Blocked, result.Decision, testCase.Value.ToString());
            CollectionAssert.Contains(result.Blockers.ToArray(), testCase.Value, testCase.Value.ToString());
            AssertNoProductEnablement(result);
        }
    }

    [TestMethod]
    public void LocalTempWriter_WritesOnlyLocalTempTestOnlyLedger()
    {
        var root = NewTempRoot();
        var writer = new ProductLedgerPathLocalTempWriterTestOnly();
        var first = writer.Append(ReadyRequest(root));
        var second = writer.Append(ReadyRequest(root) with { SafePayloadHash = new string('b', 64) });

        Assert.AreEqual(ProductLedgerPathLocalTempWriterDecision.WrittenLocalTempTestOnly, first.Decision);
        Assert.AreEqual(ProductLedgerPathLocalTempWriterDecision.WrittenLocalTempTestOnly, second.Decision);
        Assert.IsTrue(first.LocalTempTestOnlyWriteCompleted);
        Assert.IsTrue(second.LocalTempTestOnlyWriteCompleted);
        Assert.IsNotNull(first.LocalTempLedgerPath);
        Assert.IsTrue(first.LocalTempLedgerPath.StartsWith(Path.GetTempPath(), StringComparison.OrdinalIgnoreCase));
        Assert.AreEqual(first.LocalTempLedgerPath, second.LocalTempLedgerPath);
        Assert.AreEqual(1, first.Entry!.Sequence);
        Assert.AreEqual(2, second.Entry!.Sequence);
        Assert.AreEqual(first.Entry.EntryHash, second.Entry.PreviousEntryHash);
        StringAssert.Contains(first.StatusText, "LOCAL_TEMP_WRITER_TEST_ONLY");
        StringAssert.Contains(first.StatusText, "NOT_PRODUCT_LEDGER_PATH");
        AssertNoProductEnablement(first);
        AssertNoProductEnablement(second);

        var entries = writer.ReadLocalTempEntries(root);
        Assert.AreEqual(2, entries.Count);
        Assert.AreEqual(new string('a', 64), entries[0].SafePayloadHash);
        Assert.AreEqual(new string('b', 64), entries[1].SafePayloadHash);
    }

    [TestMethod]
    public void LocalTempWriter_FailsClosedOnExistingLedgerTamper()
    {
        var root = NewTempRoot();
        var writer = new ProductLedgerPathLocalTempWriterTestOnly();
        var first = writer.Append(ReadyRequest(root));
        Assert.AreEqual(ProductLedgerPathLocalTempWriterDecision.WrittenLocalTempTestOnly, first.Decision);

        File.AppendAllText(first.LocalTempLedgerPath!, "{}" + Environment.NewLine);
        var second = writer.Append(ReadyRequest(root) with { SafePayloadHash = new string('c', 64) });

        Assert.AreEqual(ProductLedgerPathLocalTempWriterDecision.Blocked, second.Decision);
        CollectionAssert.Contains(second.Blockers.ToArray(), ProductLedgerPathLocalTempWriterBlocker.ExistingLocalTempLedgerInvalid);
        AssertNoProductEnablement(second);
    }

    [TestMethod]
    public void LocalTempWriter_FailsClosedOnTailDeletionWhenCheckpointRemains()
    {
        var root = NewTempRoot();
        var writer = new ProductLedgerPathLocalTempWriterTestOnly();
        var first = writer.Append(ReadyRequest(root));
        var second = writer.Append(ReadyRequest(root) with { SafePayloadHash = new string('b', 64) });
        Assert.AreEqual(ProductLedgerPathLocalTempWriterDecision.WrittenLocalTempTestOnly, first.Decision);
        Assert.AreEqual(ProductLedgerPathLocalTempWriterDecision.WrittenLocalTempTestOnly, second.Decision);

        var firstLine = File.ReadLines(second.LocalTempLedgerPath!).First();
        File.WriteAllText(second.LocalTempLedgerPath!, firstLine + Environment.NewLine);
        var third = writer.Append(ReadyRequest(root) with { SafePayloadHash = new string('c', 64) });

        Assert.AreEqual(ProductLedgerPathLocalTempWriterDecision.Blocked, third.Decision);
        CollectionAssert.Contains(third.Blockers.ToArray(), ProductLedgerPathLocalTempWriterBlocker.ExistingLocalTempLedgerInvalid);
        AssertNoProductEnablement(third);
    }

    [TestMethod]
    public void LocalTempWriter_FailsClosedWhenCheckpointMissingAfterWrite()
    {
        var root = NewTempRoot();
        var writer = new ProductLedgerPathLocalTempWriterTestOnly();
        var first = writer.Append(ReadyRequest(root));
        Assert.AreEqual(ProductLedgerPathLocalTempWriterDecision.WrittenLocalTempTestOnly, first.Decision);

        File.Delete(Path.Combine(root, "product-ledger-path-local-temp-writer-test-only.head.json"));
        var second = writer.Append(ReadyRequest(root) with { SafePayloadHash = new string('c', 64) });

        Assert.AreEqual(ProductLedgerPathLocalTempWriterDecision.Blocked, second.Decision);
        CollectionAssert.Contains(second.Blockers.ToArray(), ProductLedgerPathLocalTempWriterBlocker.ExistingLocalTempLedgerInvalid);
        AssertNoProductEnablement(second);
    }

    [TestMethod]
    public void LocalTempWriter_SourceHasNoProductWiringOrExternalTrust()
    {
        var sourcePaths = Directory.GetFiles(
            Path.Combine(RepoRoot(), "src", "OneBrain.Core", "Approval"),
            "ProductLedgerPath*.cs",
            SearchOption.TopDirectoryOnly);
        var sources = sourcePaths.ToDictionary(path => path, File.ReadAllText, StringComparer.OrdinalIgnoreCase);
        var writerSource = File.ReadAllText(Path.Combine(
            RepoRoot(),
            "src",
            "OneBrain.Core",
            "Approval",
            "ProductLedgerPathLocalTempWriterTestOnly.cs"));
        var forbiddenFragments = new[]
        {
            "IService" + "Collection",
            "Add" + "Singleton",
            "Add" + "Scoped",
            "Add" + "Transient",
            "ICommand" + "Handler",
            "Handle" + "Async(",
            "Control" + "ler",
            "Db" + "Context",
            "Http" + "Client",
            "Kms" + "Client",
            "Worm" + "Store",
            "ProductLedgerPathActive:" + " true",
            "ProductLedgerWriteAllowed:" + " true",
            "ProductRuntimeEnabled:" + " true"
        };

        foreach (var source in sources)
        {
            foreach (var fragment in forbiddenFragments)
            {
                Assert.IsFalse(source.Value.Contains(fragment, StringComparison.Ordinal), $"{source.Key}: {fragment}");
            }
        }

        StringAssert.Contains(writerSource, "Path.GetTempPath()");
        StringAssert.Contains(writerSource, "LOCAL_TEMP_WRITER_TEST_ONLY");
        StringAssert.Contains(writerSource, "NOT_PRODUCT_LEDGER_PATH");
        StringAssert.Contains(writerSource, "ProductLedgerPathActive: false");
        StringAssert.Contains(writerSource, "ProductLedgerWriteAllowed: false");
    }

    private static ProductLedgerPathLocalTempWriterRequest ReadyRequest(string root) =>
        new(
            WriterScaffoldResult: ReadyScaffold(),
            CandidateId: "ledger-candidate-001",
            LocalTempRoot: root,
            SafePayloadHash: new string('a', 64),
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

    private static void AssertNoProductEnablement(ProductLedgerPathLocalTempWriterResult result)
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

    private static string NewTempRoot()
    {
        var root = Path.Combine(Path.GetTempPath(), "nodal-os-product-ledger-path-local-temp-writer-tests", Guid.NewGuid().ToString("N"));
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
