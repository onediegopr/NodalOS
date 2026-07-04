using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.Core.Approval;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("ApprovalHumanReview")]
[TestCategory("PhaseEApprovalHumanReview")]
public sealed class ProductLedgerRuntimeLocalOnlyInternalEnablementTests
{
    [TestMethod]
    public void RuntimeLocalOnlyFeatureFlag_FailsClosedByDefault()
    {
        var runtime = new ProductLedgerRuntimeLocalOnlyInternalEnablement();

        var missing = runtime.EvaluateFeatureFlag(null);
        var off = runtime.EvaluateFeatureFlag(ReadyFlagRequest() with { FeatureFlagValue = null });

        Assert.AreEqual(ProductLedgerRuntimeLocalOnlyFlagDecision.Rejected, missing.Decision);
        CollectionAssert.Contains(missing.Blockers.ToArray(), ProductLedgerRuntimeLocalOnlyBlocker.MissingRequest);
        AssertNoExternalOrProductRuntime(missing);

        Assert.AreEqual(ProductLedgerRuntimeLocalOnlyFlagDecision.BlockedOff, off.Decision);
        Assert.AreEqual(0, off.Blockers.Count);
        Assert.IsFalse(off.RuntimeLocalOnlyInternalEnabled);
        AssertNoExternalOrProductRuntime(off);
    }

    [TestMethod]
    public void RuntimeLocalOnlyFeatureFlag_ArmsOnlyInternalLocalDefaultOffSurface()
    {
        var result = new ProductLedgerRuntimeLocalOnlyInternalEnablement().EvaluateFeatureFlag(ReadyFlagRequest());

        Assert.AreEqual(ProductLedgerRuntimeLocalOnlyFlagDecision.ArmedLocalOnlyInternal, result.Decision);
        Assert.IsTrue(result.RuntimeLocalOnlyInternalEnabled);
        Assert.IsTrue(result.LocalRuntimeFlagDefaultOff);
        Assert.IsTrue(result.InternalServiceWiringAllowed);
        Assert.IsTrue(result.InternalCommandAdapterTestOnlyAllowed);
        Assert.IsTrue(result.InternalReadOnlyProductSurfaceAllowed);
        Assert.IsTrue(result.DiagnosticsReadinessSurfaceLocalOnlyAllowed);
        AssertNoExternalOrProductRuntime(result);
        StringAssert.Contains(result.StatusText, "LOCAL_ONLY_INTERNAL_ARMED");
        StringAssert.Contains(result.StatusText, "BOUNDED_WRITER_ONLY");
    }

    [TestMethod]
    public void RuntimeLocalOnlyFeatureFlag_BlocksPublicExternalDefaultOnAndReleaseClaims()
    {
        var runtime = new ProductLedgerRuntimeLocalOnlyInternalEnablement();
        var ready = ReadyFlagRequest();
        var cases = new Dictionary<ProductLedgerRuntimeLocalOnlyFeatureFlagRequest, ProductLedgerRuntimeLocalOnlyBlocker>
        {
            [ready with { ExplicitLocalOnlyRuntimeScope = false }] = ProductLedgerRuntimeLocalOnlyBlocker.MissingExplicitLocalOnlyRuntimeScope,
            [ready with { LocalRuntimeFlagDefaultOff = false }] = ProductLedgerRuntimeLocalOnlyBlocker.MissingLocalRuntimeFlagDefaultOff,
            [ready with { FeatureFlagValue = "enabled:product" }] = ProductLedgerRuntimeLocalOnlyBlocker.UnexpectedFeatureFlagValue,
            [ready with { RequestsRuntimeEnabledByDefault = true }] = ProductLedgerRuntimeLocalOnlyBlocker.RuntimeEnabledByDefault,
            [ready with { RequestsPublicUiAction = true }] = ProductLedgerRuntimeLocalOnlyBlocker.PublicUiActionRequested,
            [ready with { RequestsProductCommandHandler = true }] = ProductLedgerRuntimeLocalOnlyBlocker.ProductCommandHandlerRequested,
            [ready with { RequestsProductiveServiceRegistration = true }] = ProductLedgerRuntimeLocalOnlyBlocker.ProductiveServiceRegistrationRequested,
            [ready with { ClaimsProviderCloudNetwork = true }] = ProductLedgerRuntimeLocalOnlyBlocker.ProviderCloudNetworkClaimed,
            [ready with { ClaimsDbMigration = true }] = ProductLedgerRuntimeLocalOnlyBlocker.DbMigrationClaimed,
            [ready with { ClaimsKmsWormExternalTrust = true }] = ProductLedgerRuntimeLocalOnlyBlocker.KmsWormExternalTrustClaimed,
            [ready with { ClaimsBrowserCdpWcuOcrRecipesLive = true }] = ProductLedgerRuntimeLocalOnlyBlocker.BrowserCdpWcuOcrRecipesLiveClaimed,
            [ready with { ClaimsReleaseCommercial = true }] = ProductLedgerRuntimeLocalOnlyBlocker.ReleaseCommercialClaimed,
            [ready with { ClaimsDestructiveActionOutsideBoundedWriter = true }] = ProductLedgerRuntimeLocalOnlyBlocker.DestructiveActionOutsideBoundedWriterClaimed
        };

        foreach (var testCase in cases)
        {
            var result = runtime.EvaluateFeatureFlag(testCase.Key);

            Assert.AreEqual(ProductLedgerRuntimeLocalOnlyFlagDecision.Rejected, result.Decision, testCase.Value.ToString());
            CollectionAssert.Contains(result.Blockers.ToArray(), testCase.Value, testCase.Value.ToString());
            AssertNoExternalOrProductRuntime(result);
        }
    }

    [TestMethod]
    public void RuntimeLocalOnlyFeatureFlag_BlocksCasingCompositeAndRuntimeLikeFlagVariants()
    {
        var runtime = new ProductLedgerRuntimeLocalOnlyInternalEnablement();
        var ready = ReadyFlagRequest();
        var cases = new[]
        {
            "Enabled:local-only-internal",
            "ENABLED:LOCAL-ONLY-INTERNAL",
            "enabled:local-only-internal;product",
            "enabled:local-only-internal runtime",
            "enabled:local-only-internal\nrelease",
            "enabled:local-only-internal\tpublic-ui"
        };

        foreach (var flagValue in cases)
        {
            var result = runtime.EvaluateFeatureFlag(ready with { FeatureFlagValue = flagValue });

            Assert.AreEqual(ProductLedgerRuntimeLocalOnlyFlagDecision.Rejected, result.Decision, flagValue);
            CollectionAssert.Contains(result.Blockers.ToArray(), ProductLedgerRuntimeLocalOnlyBlocker.UnexpectedFeatureFlagValue, flagValue);
            AssertNoExternalOrProductRuntime(result);
        }

        var trimmed = runtime.EvaluateFeatureFlag(ready with { FeatureFlagValue = "  enabled:local-only-internal  " });
        Assert.AreEqual(ProductLedgerRuntimeLocalOnlyFlagDecision.ArmedLocalOnlyInternal, trimmed.Decision);
        Assert.AreEqual(ProductLedgerRuntimeLocalOnlyInternalEnablement.EnabledLocalOnlyInternalValue, trimmed.EffectiveFeatureFlagValue);
    }

    [TestMethod]
    public void RuntimeLocalOnlyInternalAdapter_BlocksMissingFlagActivationScopeAndExternalClaims()
    {
        using var fixture = LedgerFixture.Create();
        var runtime = new ProductLedgerRuntimeLocalOnlyInternalEnablement();
        var flag = runtime.EvaluateFeatureFlag(ReadyFlagRequest());
        var activation = Activate(fixture);
        var ready = ReadyAdapterRequest(flag, activation);
        var cases = new Dictionary<ProductLedgerRuntimeLocalOnlyAdapterRequest, ProductLedgerRuntimeLocalOnlyBlocker>
        {
            [ready with { FeatureFlag = null }] = ProductLedgerRuntimeLocalOnlyBlocker.MissingArmedFeatureFlag,
            [ready with { FeatureFlag = runtime.EvaluateFeatureFlag(ReadyFlagRequest() with { FeatureFlagValue = ProductLedgerRuntimeLocalOnlyInternalEnablement.OffValue }) }] = ProductLedgerRuntimeLocalOnlyBlocker.FailedArmedFeatureFlag,
            [ready with { FeatureFlag = flag with { InternalReadOnlyProductSurfaceAllowed = false } }] = ProductLedgerRuntimeLocalOnlyBlocker.FailedArmedFeatureFlag,
            [ready with { FeatureFlag = flag with { DiagnosticsReadinessSurfaceLocalOnlyAllowed = false } }] = ProductLedgerRuntimeLocalOnlyBlocker.FailedArmedFeatureFlag,
            [ready with { ActivationResult = null }] = ProductLedgerRuntimeLocalOnlyBlocker.MissingActivationResult,
            [ready with { ActivationResult = activation with { ProductLedgerWriteAllowed = false } }] = ProductLedgerRuntimeLocalOnlyBlocker.FailedActivationResult,
            [ready with { ExplicitTestOnlyCommandAdapter = false }] = ProductLedgerRuntimeLocalOnlyBlocker.MissingExplicitTestOnlyCommandAdapter,
            [ready with { CommandKind = (ProductLedgerRuntimeLocalOnlyCommandKind)999 }] = ProductLedgerRuntimeLocalOnlyBlocker.UnsupportedCommandKind,
            [ready with { RequestsPublicUiAction = true }] = ProductLedgerRuntimeLocalOnlyBlocker.PublicUiActionRequested,
            [ready with { RequestsProductCommandHandler = true }] = ProductLedgerRuntimeLocalOnlyBlocker.ProductCommandHandlerRequested,
            [ready with { RequestsProductiveServiceRegistration = true }] = ProductLedgerRuntimeLocalOnlyBlocker.ProductiveServiceRegistrationRequested,
            [ready with { ClaimsProviderCloudNetwork = true }] = ProductLedgerRuntimeLocalOnlyBlocker.ProviderCloudNetworkClaimed,
            [ready with { ClaimsDbMigration = true }] = ProductLedgerRuntimeLocalOnlyBlocker.DbMigrationClaimed,
            [ready with { ClaimsKmsWormExternalTrust = true }] = ProductLedgerRuntimeLocalOnlyBlocker.KmsWormExternalTrustClaimed,
            [ready with { ClaimsBrowserCdpWcuOcrRecipesLive = true }] = ProductLedgerRuntimeLocalOnlyBlocker.BrowserCdpWcuOcrRecipesLiveClaimed,
            [ready with { ClaimsReleaseCommercial = true }] = ProductLedgerRuntimeLocalOnlyBlocker.ReleaseCommercialClaimed,
            [ready with { ClaimsDestructiveActionOutsideBoundedWriter = true }] = ProductLedgerRuntimeLocalOnlyBlocker.DestructiveActionOutsideBoundedWriterClaimed
        };

        foreach (var testCase in cases)
        {
            var result = runtime.ExecuteInternal(testCase.Key);

            Assert.AreEqual(ProductLedgerRuntimeLocalOnlyAdapterDecision.Blocked, result.Decision, testCase.Value.ToString());
            CollectionAssert.Contains(result.Blockers.ToArray(), testCase.Value, testCase.Value.ToString());
            AssertNoExternalOrProductRuntime(result);
        }
    }

    [TestMethod]
    public void RuntimeLocalOnlyInternalAdapter_ReadsDiagnosticsAndAppendsOnlyThroughBoundedWriter()
    {
        using var fixture = LedgerFixture.Create();
        var runtime = new ProductLedgerRuntimeLocalOnlyInternalEnablement();
        var flag = runtime.EvaluateFeatureFlag(ReadyFlagRequest());
        var activation = Activate(fixture);

        var diagnostics = runtime.ExecuteInternal(ReadyAdapterRequest(flag, activation));
        var append = runtime.ExecuteInternal(
            ReadyAdapterRequest(flag, activation) with
            {
                CommandKind = ProductLedgerRuntimeLocalOnlyCommandKind.AppendSafeHashOnly,
                SafePayloadHash = new string('a', 64)
            });
        var afterAppendDiagnostics = runtime.ExecuteInternal(ReadyAdapterRequest(flag, activation));

        Assert.AreEqual(ProductLedgerRuntimeLocalOnlyAdapterDecision.DiagnosticsReadOnly, diagnostics.Decision);
        Assert.AreEqual(0, diagnostics.VerifiedEntryCount);
        Assert.AreEqual(ProductLedgerRuntimeLocalOnlyAdapterDecision.AppendedLocalOnly, append.Decision);
        Assert.AreEqual(1, append.VerifiedEntryCount);
        Assert.IsNotNull(append.AppendResult);
        Assert.AreEqual(ProductLedgerPathLocalOnlyWriterDecision.AppendedLocalOnly, append.AppendResult!.Decision);
        Assert.AreEqual(ProductLedgerRuntimeLocalOnlyAdapterDecision.DiagnosticsReadOnly, afterAppendDiagnostics.Decision);
        Assert.AreEqual(1, afterAppendDiagnostics.VerifiedEntryCount);
        Assert.AreEqual(append.HeadEntryHash, afterAppendDiagnostics.HeadEntryHash);
        Assert.IsTrue(File.Exists(activation.ActiveLedgerFilePath));
        AssertNoExternalOrProductRuntime(append);
    }

    [TestMethod]
    public void RuntimeLocalOnlyInternalAdapter_FailsClosedWhenBoundedWriterRejectsAppend()
    {
        using var fixture = LedgerFixture.Create();
        var runtime = new ProductLedgerRuntimeLocalOnlyInternalEnablement();
        var flag = runtime.EvaluateFeatureFlag(ReadyFlagRequest());
        var activation = Activate(fixture);
        var result = runtime.ExecuteInternal(
            ReadyAdapterRequest(flag, activation) with
            {
                CommandKind = ProductLedgerRuntimeLocalOnlyCommandKind.AppendSafeHashOnly,
                SafePayloadHash = "raw-secret",
                EvidenceMetadata = new Dictionary<string, string> { ["token"] = "redacted" }
            });

        Assert.AreEqual(ProductLedgerRuntimeLocalOnlyAdapterDecision.Blocked, result.Decision);
        CollectionAssert.Contains(result.Blockers.ToArray(), ProductLedgerRuntimeLocalOnlyBlocker.UnsafeAppendRequest);
        AssertNoExternalOrProductRuntime(result);
    }

    [TestMethod]
    public void RuntimeLocalOnlyInternalAdapter_FailsClosedOnInvalidExistingLedger()
    {
        using var fixture = LedgerFixture.Create();
        var runtime = new ProductLedgerRuntimeLocalOnlyInternalEnablement();
        var flag = runtime.EvaluateFeatureFlag(ReadyFlagRequest());
        var activation = Activate(fixture);
        File.WriteAllText(activation.ActiveLedgerFilePath!, "{ invalid json");

        var result = runtime.ExecuteInternal(ReadyAdapterRequest(flag, activation));

        Assert.AreEqual(ProductLedgerRuntimeLocalOnlyAdapterDecision.Blocked, result.Decision);
        CollectionAssert.Contains(result.Blockers.ToArray(), ProductLedgerRuntimeLocalOnlyBlocker.ExistingLedgerInvalid);
        AssertNoExternalOrProductRuntime(result);
    }

    [TestMethod]
    public void RuntimeLocalOnlyInternalEnablement_SourceHasNoPublicRuntimeRegistrationNetworkDbKmsUiOrLiveAutomation()
    {
        var source = File.ReadAllText(Path.Combine(
            RepoRoot(),
            "src",
            "OneBrain.Core",
            "Approval",
            "ProductLedgerRuntimeLocalOnlyInternalEnablement.cs"));
        var forbiddenFragments = new[]
        {
            "IService" + "Collection",
            "Add" + "Singleton",
            "Add" + "Scoped",
            "Add" + "Transient",
            "IHosted" + "Service",
            "ICommand" + "Handler",
            "Handle" + "Async(",
            "Control" + "ler",
            "Map" + "Post",
            "Map" + "Get",
            "Http" + "Client",
            "Web" + "Socket",
            "Db" + "Context",
            "Migration" + "Builder",
            "Kms" + "Client",
            "Worm" + "Store",
            "ProductRuntimeEnabled:" + " true",
            "RuntimeEnabledByDefault:" + " true",
            "ProductiveServiceRegistrationAllowed:" + " true",
            "ProductCommandHandlersAllowed:" + " true",
            "PublicUiProductActionsAllowed:" + " true",
            "ProviderCloudNetworkAllowed:" + " true",
            "DbMigrationAllowed:" + " true",
            "KmsWormExternalTrustAllowed:" + " true",
            "BrowserCdpWcuOcrRecipesLiveAllowed:" + " true",
            "ReleaseCommercialReady:" + " true"
        };

        foreach (var fragment in forbiddenFragments)
        {
            Assert.IsFalse(source.Contains(fragment, StringComparison.Ordinal), fragment);
        }

        StringAssert.Contains(source, ProductLedgerRuntimeLocalOnlyInternalEnablement.EnabledLocalOnlyInternalValue);
        StringAssert.Contains(source, "writer.Append");
        StringAssert.Contains(source, "writer.ReadVerified");
    }

    private static ProductLedgerRuntimeLocalOnlyFeatureFlagRequest ReadyFlagRequest() =>
        new(
            ExplicitLocalOnlyRuntimeScope: true,
            LocalRuntimeFlagDefaultOff: true,
            FeatureFlagValue: ProductLedgerRuntimeLocalOnlyInternalEnablement.EnabledLocalOnlyInternalValue,
            RequestsRuntimeEnabledByDefault: false,
            RequestsPublicUiAction: false,
            RequestsProductCommandHandler: false,
            RequestsProductiveServiceRegistration: false,
            ClaimsProviderCloudNetwork: false,
            ClaimsDbMigration: false,
            ClaimsKmsWormExternalTrust: false,
            ClaimsBrowserCdpWcuOcrRecipesLive: false,
            ClaimsReleaseCommercial: false,
            ClaimsDestructiveActionOutsideBoundedWriter: false);

    private static ProductLedgerRuntimeLocalOnlyAdapterRequest ReadyAdapterRequest(
        ProductLedgerRuntimeLocalOnlyFlagResult flag,
        ProductLedgerPathLocalOnlyActivationResult activation) =>
        new(
            FeatureFlag: flag,
            ActivationResult: activation,
            CommandKind: ProductLedgerRuntimeLocalOnlyCommandKind.DiagnosticsReadOnly,
            ExplicitTestOnlyCommandAdapter: true,
            SafePayloadHash: null,
            EvidenceMetadata: new Dictionary<string, string>
            {
                ["authority"] = "local-only-policy-bound",
                ["redaction"] = "redacted-before-persistence",
                ["failure"] = "replay-rollback-evidence"
            },
            RequestsPublicUiAction: false,
            RequestsProductCommandHandler: false,
            RequestsProductiveServiceRegistration: false,
            ClaimsProviderCloudNetwork: false,
            ClaimsDbMigration: false,
            ClaimsKmsWormExternalTrust: false,
            ClaimsBrowserCdpWcuOcrRecipesLive: false,
            ClaimsReleaseCommercial: false,
            ClaimsDestructiveActionOutsideBoundedWriter: false);

    private static ProductLedgerPathLocalOnlyActivationResult Activate(LedgerFixture fixture)
    {
        var activation = new ProductLedgerPathLocalOnlyActiveWriter().Activate(
            new ProductLedgerPathLocalOnlyActivationRequest(
                PersistedCandidateResult: ReadyPersistedCandidate(fixture),
                ExplicitLocalOnlyActivationMode: true,
                HasAuthorityEvidence: true,
                HasRedactionBeforePersistenceEvidence: true,
                HasFailureReplayRollbackEvidence: true,
                HasRetentionEvidence: true,
                LocalRuntimeFlagDefaultOff: true,
                RequestsRuntimeEnablement: false,
                RequestsProductServiceRegistration: false,
                RequestsProductCommandHandler: false,
                RequestsUiProductAction: false,
                ClaimsProviderCloudNetwork: false,
                ClaimsWormKmsExternalTrust: false,
                ClaimsDbMigration: false,
                ClaimsBrowserCdpWcuOcrRecipesLive: false,
                ClaimsReleaseCommercialReadiness: false,
                ClaimsLocalTempAsProductLedgerPath: false));
        Assert.AreEqual(ProductLedgerPathLocalOnlyActivationDecision.ActivatedLocalOnly, activation.Decision);
        return activation;
    }

    private static ProductLedgerPathPersistedCandidateResult ReadyPersistedCandidate(LedgerFixture fixture)
    {
        var canonicalization = new ProductLedgerPathCanonicalizationValidator().Validate(
            new ProductLedgerPathCanonicalizationRequest(
                CandidatePath: fixture.LedgerRoot,
                AllowedRootPath: fixture.AllowedRoot,
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
                CandidateId: "ledger-candidate-runtime-001",
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

    private static void AssertNoExternalOrProductRuntime(ProductLedgerRuntimeLocalOnlyFlagResult result)
    {
        Assert.IsFalse(result.ProductRuntimeEnabled);
        Assert.IsFalse(result.RuntimeEnabledByDefault);
        Assert.IsFalse(result.ProductiveServiceRegistrationAllowed);
        Assert.IsFalse(result.ProductCommandHandlersAllowed);
        Assert.IsFalse(result.PublicUiProductActionsAllowed);
        Assert.IsFalse(result.ProviderCloudNetworkAllowed);
        Assert.IsFalse(result.DbMigrationAllowed);
        Assert.IsFalse(result.KmsWormExternalTrustAllowed);
        Assert.IsFalse(result.BrowserCdpWcuOcrRecipesLiveAllowed);
        Assert.IsFalse(result.ReleaseCommercialReady);
    }

    private static void AssertNoExternalOrProductRuntime(ProductLedgerRuntimeLocalOnlyAdapterResult result)
    {
        Assert.IsFalse(result.ProductRuntimeEnabled);
        Assert.IsFalse(result.RuntimeEnabledByDefault);
        Assert.IsFalse(result.ProductiveServiceRegistrationAllowed);
        Assert.IsFalse(result.ProductCommandHandlersAllowed);
        Assert.IsFalse(result.PublicUiProductActionsAllowed);
        Assert.IsFalse(result.ProviderCloudNetworkAllowed);
        Assert.IsFalse(result.DbMigrationAllowed);
        Assert.IsFalse(result.KmsWormExternalTrustAllowed);
        Assert.IsFalse(result.BrowserCdpWcuOcrRecipesLiveAllowed);
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
                "ProductLedgerRuntimeLocalOnlyInternalEnablement.cs")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        Assert.Fail("repo root not found");
        return string.Empty;
    }

    private sealed class LedgerFixture : IDisposable
    {
        private LedgerFixture(string allowedRoot, string ledgerRoot)
        {
            AllowedRoot = allowedRoot;
            LedgerRoot = ledgerRoot;
        }

        public string AllowedRoot { get; }

        public string LedgerRoot { get; }

        public static LedgerFixture Create()
        {
            var allowedRoot = Path.Combine(RepoRoot(), ".tmp-product-ledger-runtime-local-only-tests", Guid.NewGuid().ToString("N"));
            var ledgerRoot = Path.Combine(allowedRoot, Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(ledgerRoot);
            return new LedgerFixture(allowedRoot, ledgerRoot);
        }

        public void Dispose()
        {
            if (AllowedRoot.StartsWith(RepoRoot(), StringComparison.OrdinalIgnoreCase) && Directory.Exists(AllowedRoot))
            {
                Directory.Delete(AllowedRoot, recursive: true);
            }
        }
    }
}
