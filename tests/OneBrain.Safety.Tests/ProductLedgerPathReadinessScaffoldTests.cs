using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.Core.Approval;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("ApprovalHumanReview")]
[TestCategory("PhaseEApprovalHumanReview")]
public sealed class ProductLedgerPathReadinessScaffoldTests
{
    [TestMethod]
    public void ProductLedgerPathScaffold_FailsClosedByDefault()
    {
        var scaffold = new ProductLedgerPathReadinessScaffold();

        var nullResult = scaffold.Evaluate(null);
        var emptyResult = scaffold.Evaluate(new ProductLedgerPathReadinessRequest(
            ExplicitTestOnlyMode: false,
            NoProductWriteAssertion: false,
            NoRuntimeEnablementAssertion: false,
            NoReleaseCommercialAssertion: false,
            ClaimsExternalTrust: true,
            ClaimsWormKmsCloud: true,
            Canonicalization: null,
            ReparsePointRisk: null,
            Authority: null,
            HasRedactionPolicyEvidence: false,
            HasRetentionPolicyEvidence: false,
            HasReplayFailureEvidence: false,
            HasRollbackNonRollbackClassification: false));

        Assert.AreEqual(ProductLedgerPathReadinessDecision.Rejected, nullResult.Decision);
        CollectionAssert.Contains(nullResult.Blockers.ToArray(), ProductLedgerPathBlocker.MissingRequest);
        AssertNoProductEnablement(nullResult);

        Assert.AreEqual(ProductLedgerPathReadinessDecision.Rejected, emptyResult.Decision);
        CollectionAssert.Contains(emptyResult.Blockers.ToArray(), ProductLedgerPathBlocker.MissingExplicitTestOnlyMode);
        CollectionAssert.Contains(emptyResult.Blockers.ToArray(), ProductLedgerPathBlocker.ProductWriteRequested);
        CollectionAssert.Contains(emptyResult.Blockers.ToArray(), ProductLedgerPathBlocker.RuntimeEnablementRequested);
        CollectionAssert.Contains(emptyResult.Blockers.ToArray(), ProductLedgerPathBlocker.ReleaseCommercialReadinessClaimed);
        CollectionAssert.Contains(emptyResult.Blockers.ToArray(), ProductLedgerPathBlocker.ExternalTrustClaimed);
        CollectionAssert.Contains(emptyResult.Blockers.ToArray(), ProductLedgerPathBlocker.WormKmsCloudClaimed);
        AssertNoProductEnablement(emptyResult);
    }

    [TestMethod]
    public void ProductLedgerPathScaffold_BlocksMissingEvidenceDependencies()
    {
        var ready = ReadyRequest();
        var cases = new Dictionary<ProductLedgerPathReadinessRequest, ProductLedgerPathBlocker>
        {
            [ready with { ExplicitTestOnlyMode = false }] = ProductLedgerPathBlocker.MissingExplicitTestOnlyMode,
            [ready with { NoProductWriteAssertion = false }] = ProductLedgerPathBlocker.ProductWriteRequested,
            [ready with { NoRuntimeEnablementAssertion = false }] = ProductLedgerPathBlocker.RuntimeEnablementRequested,
            [ready with { NoReleaseCommercialAssertion = false }] = ProductLedgerPathBlocker.ReleaseCommercialReadinessClaimed,
            [ready with { Canonicalization = null }] = ProductLedgerPathBlocker.MissingCanonicalizationRiskPreview,
            [ready with { ReparsePointRisk = null }] = ProductLedgerPathBlocker.MissingReparsePointRiskPreview,
            [ready with { Authority = null }] = ProductLedgerPathBlocker.MissingAuthorityReadinessPreview,
            [ready with { HasRedactionPolicyEvidence = false }] = ProductLedgerPathBlocker.MissingRedactionPolicyEvidence,
            [ready with { HasRetentionPolicyEvidence = false }] = ProductLedgerPathBlocker.MissingRetentionPolicyEvidence,
            [ready with { HasReplayFailureEvidence = false }] = ProductLedgerPathBlocker.MissingReplayFailureEvidence,
            [ready with { HasRollbackNonRollbackClassification = false }] = ProductLedgerPathBlocker.MissingRollbackClassification
        };

        foreach (var testCase in cases)
        {
            var result = new ProductLedgerPathReadinessScaffold().Evaluate(testCase.Key);

            Assert.AreEqual(ProductLedgerPathReadinessDecision.Rejected, result.Decision, testCase.Value.ToString());
            CollectionAssert.Contains(result.Blockers.ToArray(), testCase.Value, testCase.Value.ToString());
            AssertNoProductEnablement(result);
        }
    }

    [TestMethod]
    public void ProductLedgerPathScaffold_BlocksCanonicalizationAndPathCorpusRisks()
    {
        var ready = ReadyRequest();
        var cases = new Dictionary<CanonicalizationRiskPreview, ProductLedgerPathBlocker>
        {
            [ready.Canonicalization! with { CandidatePath = "" }] = ProductLedgerPathBlocker.EmptyPath,
            [ready.Canonicalization! with { CandidatePath = "relative\\ledger" }] = ProductLedgerPathBlocker.RelativePathWithoutExplicitHandling,
            [ready.Canonicalization! with { CandidatePath = @"C:\safe\..\escape" }] = ProductLedgerPathBlocker.PathTraversalRisk,
            [ready.Canonicalization! with { CandidatePath = @"C:\safe/mixed" }] = ProductLedgerPathBlocker.MixedSeparatorRisk,
            [ready.Canonicalization! with { CandidatePath = @"\\server\share\ledger" }] = ProductLedgerPathBlocker.UncNetworkPathRisk,
            [ready.Canonicalization! with { CandidatePath = @"C:\safe\CON" }] = ProductLedgerPathBlocker.WindowsReservedDeviceNameRisk,
            [ready.Canonicalization! with { CandidatePath = @"%TEMP%\ledger" }] = ProductLedgerPathBlocker.EnvironmentVariableExpansionRisk,
            [ready.Canonicalization! with { CandidatePath = "C:ledger" }] = ProductLedgerPathBlocker.DriveRelativePathRisk,
            [ready.Canonicalization! with { CandidatePath = @"\\?\C:\safe\ledger" }] = ProductLedgerPathBlocker.LongPathPrefixAmbiguity,
            [ready.Canonicalization! with { CandidatePath = @"C:\safe\ledger. " }] = ProductLedgerPathBlocker.TrailingDotOrSpaceRisk,
            [ready.Canonicalization! with { CandidatePath = @"C:\safe\ledger:stream" }] = ProductLedgerPathBlocker.AlternateDataStreamRisk,
            [ready.Canonicalization! with { HasCanonicalPathEvidence = false }] = ProductLedgerPathBlocker.CanonicalPathEvidenceMissing,
            [ready.Canonicalization! with { HasJailBoundaryEvidence = false }] = ProductLedgerPathBlocker.JailBoundaryEvidenceMissing,
            [ready.Canonicalization! with { CanonicalPathInsideJail = false }] = ProductLedgerPathBlocker.PathAppearsInsideButCanonicalOutside,
            [ready.Canonicalization! with { HasTocTouMitigationEvidence = false }] = ProductLedgerPathBlocker.TocTouMitigationMissing,
            [ready.Canonicalization! with { CasingNormalizationMismatch = true }] = ProductLedgerPathBlocker.CasingNormalizationMismatch,
            [ready.Canonicalization! with { UnicodeNormalizationMismatch = true }] = ProductLedgerPathBlocker.UnicodeNormalizationMismatch,
            [ready.Canonicalization! with { ClaimsLocalTempAsProductLedgerPath = true }] = ProductLedgerPathBlocker.LocalTempClaimedAsProductLedgerPath,
            [ready.Canonicalization! with { ClaimsProductLedgerReadyWithoutProductPolicy = true }] = ProductLedgerPathBlocker.ProductLedgerReadyClaimWithoutProductPolicy
        };

        foreach (var testCase in cases)
        {
            var result = new ProductLedgerPathReadinessScaffold().Evaluate(ready with { Canonicalization = testCase.Key });

            Assert.AreEqual(ProductLedgerPathReadinessDecision.Rejected, result.Decision, testCase.Value.ToString());
            CollectionAssert.Contains(result.Blockers.ToArray(), testCase.Value, testCase.Value.ToString());
            AssertNoProductEnablement(result);
        }
    }

    [TestMethod]
    public void ProductLedgerPathScaffold_BlocksReparseSymlinkJunctionAndAliasRisks()
    {
        var ready = ReadyRequest();
        var cases = new Dictionary<ReparsePointRiskPreview, ProductLedgerPathBlocker>
        {
            [ready.ReparsePointRisk! with { HasSymlinkJunctionReparseEvidence = false }] = ProductLedgerPathBlocker.SymlinkRiskUnresolved,
            [ready.ReparsePointRisk! with { SymlinkRiskUnresolved = true }] = ProductLedgerPathBlocker.SymlinkRiskUnresolved,
            [ready.ReparsePointRisk! with { JunctionRiskUnresolved = true }] = ProductLedgerPathBlocker.JunctionRiskUnresolved,
            [ready.ReparsePointRisk! with { ReparsePointRiskUnresolved = true }] = ProductLedgerPathBlocker.ReparsePointRiskUnresolved,
            [ready.ReparsePointRisk! with { HardlinkOrMountAliasRiskUnresolved = true }] = ProductLedgerPathBlocker.HardlinkOrMountAliasRiskUnresolved
        };

        foreach (var testCase in cases)
        {
            var result = new ProductLedgerPathReadinessScaffold().Evaluate(ready with { ReparsePointRisk = testCase.Key });

            Assert.AreEqual(ProductLedgerPathReadinessDecision.Rejected, result.Decision, testCase.Value.ToString());
            CollectionAssert.Contains(result.Blockers.ToArray(), testCase.Value, testCase.Value.ToString());
            AssertNoProductEnablement(result);
        }
    }

    [TestMethod]
    public void ProductLedgerPathScaffold_BlocksAuthorityRisks()
    {
        var ready = ReadyRequest();
        var cases = new Dictionary<AuthorityReadinessPreview, ProductLedgerPathBlocker>
        {
            [ready.Authority! with { HasHumanApprovalEvidence = false }] = ProductLedgerPathBlocker.MissingHumanApprovalEvidence,
            [ready.Authority! with { TreatsHumanGoAsProductAuthority = true }] = ProductLedgerPathBlocker.HumanGoTreatedAsProductAuthority,
            [ready.Authority! with { OperatorIdentityEvidence = "" }] = ProductLedgerPathBlocker.MissingOperatorIdentityEvidence,
            [ready.Authority! with { LocalOperatorSessionEvidence = "" }] = ProductLedgerPathBlocker.MissingLocalOperatorSessionEvidence,
            [ready.Authority! with { ApprovalIsStale = true }] = ProductLedgerPathBlocker.StaleApprovalEvidence,
            [ready.Authority! with { ApprovalForDifferentScope = true }] = ProductLedgerPathBlocker.ApprovalForDifferentScope,
            [ready.Authority! with { ApprovalForDifferentLedgerPath = true }] = ProductLedgerPathBlocker.ApprovalForDifferentLedgerPath,
            [ready.Authority! with { ApprovalForDifferentRuntimeFlag = true }] = ProductLedgerPathBlocker.ApprovalForDifferentRuntimeFlag,
            [ready.Authority! with { ApprovalReplayOrTamperRisk = true }] = ProductLedgerPathBlocker.ApprovalReplayOrTamperRisk,
            [ready.Authority! with { ApprovalAfterRiskChanges = true }] = ProductLedgerPathBlocker.ApprovalAfterRiskChanges,
            [ready.Authority! with { EvidenceReferences = [] }] = ProductLedgerPathBlocker.ApprovalMissingEvidenceRefs,
            [ready.Authority! with { ApprovalAttemptsProviderCloudKmsWormExternalTrust = true }] = ProductLedgerPathBlocker.ApprovalAttemptsProviderCloudKmsWormExternalTrust,
            [ready.Authority! with { ApprovalAttemptsLiveAutomation = true }] = ProductLedgerPathBlocker.ApprovalAttemptsLiveAutomation,
            [ready.Authority! with { ApprovalAttemptsReleaseCommercial = true }] = ProductLedgerPathBlocker.ApprovalAttemptsReleaseCommercial
        };

        foreach (var testCase in cases)
        {
            var result = new ProductLedgerPathReadinessScaffold().Evaluate(ready with { Authority = testCase.Key });

            Assert.AreEqual(ProductLedgerPathReadinessDecision.Rejected, result.Decision, testCase.Value.ToString());
            CollectionAssert.Contains(result.Blockers.ToArray(), testCase.Value, testCase.Value.ToString());
            AssertNoProductEnablement(result);
        }
    }

    [TestMethod]
    public void ProductLedgerPathScaffold_AllowsOnlyDisabledTestOnlyReadinessPreview()
    {
        var result = new ProductLedgerPathReadinessScaffold().Evaluate(ReadyRequest());

        Assert.AreEqual(ProductLedgerPathReadinessDecision.ReadinessPreviewAllowed, result.Decision);
        Assert.IsTrue(result.ReadinessPreviewAllowed);
        Assert.AreEqual(0, result.Blockers.Count);
        AssertNoProductEnablement(result);
        StringAssert.Contains(result.StatusText, "READINESS_PREVIEW_ONLY");
        StringAssert.Contains(result.StatusText, "DISABLED_TEST_ONLY");
        StringAssert.Contains(result.StatusText, "NO_PRODUCT_LEDGER_WRITE");
        StringAssert.Contains(result.StatusText, "NO_PRODUCT_RUNTIME_ENABLEMENT");
        StringAssert.Contains(result.StatusText, "NO_RELEASE_COMMERCIAL");
        StringAssert.Contains(result.StatusText, "NO_EXTERNAL_TRUST");
        StringAssert.Contains(result.StatusText, "NO_WORM_KMS_CLOUD");
    }

    [TestMethod]
    public void ProductLedgerPathScaffold_SourceContainsNoProductRegistrationHandlersWritersOrProviders()
    {
        var sourcePath = System.IO.Path.Combine(
            FindRepoRoot(),
            "src",
            "OneBrain.Core",
            "Approval",
            "ProductLedgerPathReadinessScaffold.cs");
        var source = File.ReadAllText(sourcePath);
        var forbiddenFragments = new[]
        {
            "AddSingleton",
            "AddScoped",
            "AddTransient",
            "IHostedService",
            "MapPost",
            "MapGet",
            "AddCommandHandler",
            "ICommandHandler",
            "RunProductAction",
            "ProductActionButton",
            "HttpClient",
            "WebSocket",
            "DbContext",
            "MigrationBuilder",
            "SaveChanges",
            "File.AppendAllText",
            "File.WriteAllText",
            "Directory.CreateDirectory",
            "ReleaseReady = true",
            "CommercialReady = true"
        };

        foreach (var fragment in forbiddenFragments)
        {
            Assert.IsFalse(source.Contains(fragment, StringComparison.Ordinal), fragment);
        }
    }

    private static ProductLedgerPathReadinessRequest ReadyRequest() =>
        new(
            ExplicitTestOnlyMode: true,
            NoProductWriteAssertion: true,
            NoRuntimeEnablementAssertion: true,
            NoReleaseCommercialAssertion: true,
            ClaimsExternalTrust: false,
            ClaimsWormKmsCloud: false,
            Canonicalization: new CanonicalizationRiskPreview(
                CandidatePath: @"C:\NodalOS\disabled-ledger-preview",
                RelativePathExplicitlyHandled: false,
                HasCanonicalPathEvidence: true,
                HasJailBoundaryEvidence: true,
                CanonicalPathInsideJail: true,
                HasTocTouMitigationEvidence: true,
                CasingNormalizationMismatch: false,
                UnicodeNormalizationMismatch: false,
                ClaimsLocalTempAsProductLedgerPath: false,
                ClaimsProductLedgerReadyWithoutProductPolicy: false),
            ReparsePointRisk: new ReparsePointRiskPreview(
                HasSymlinkJunctionReparseEvidence: true,
                SymlinkRiskUnresolved: false,
                JunctionRiskUnresolved: false,
                ReparsePointRiskUnresolved: false,
                HardlinkOrMountAliasRiskUnresolved: false),
            Authority: new AuthorityReadinessPreview(
                HasHumanApprovalEvidence: true,
                TreatsHumanGoAsProductAuthority: false,
                OperatorIdentityEvidence: "operator:test-only-fixture",
                LocalOperatorSessionEvidence: "session:test-only-fixture",
                EvidenceReferences: ["docs/qa/product-ledger-path-readiness/report.md"],
                ApprovalIsStale: false,
                ApprovalForDifferentScope: false,
                ApprovalForDifferentLedgerPath: false,
                ApprovalForDifferentRuntimeFlag: false,
                ApprovalReplayOrTamperRisk: false,
                ApprovalAfterRiskChanges: false,
                ApprovalAttemptsProviderCloudKmsWormExternalTrust: false,
                ApprovalAttemptsLiveAutomation: false,
                ApprovalAttemptsReleaseCommercial: false),
            HasRedactionPolicyEvidence: true,
            HasRetentionPolicyEvidence: true,
            HasReplayFailureEvidence: true,
            HasRollbackNonRollbackClassification: true);

    private static void AssertNoProductEnablement(ProductLedgerPathReadinessResult result)
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

    private static string FindRepoRoot()
    {
        var directory = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (directory is not null)
        {
            if (File.Exists(System.IO.Path.Combine(
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
