using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.Core.Approval;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("ApprovalHumanReview")]
[TestCategory("PhaseEApprovalHumanReview")]
public sealed class DurableRuntimeEnablementSafetyScaffoldTests
{
    [TestMethod]
    public void RuntimeScaffold_FailsClosedByDefaultWithoutProductEnablement()
    {
        var scaffold = new DurableRuntimeEnablementSafetyScaffold();

        var nullResult = scaffold.Evaluate(null);
        var emptyResult = scaffold.Evaluate(new DurableRuntimeEnablementScaffoldRequest(
            ExplicitTestOnlyScope: false,
            ProductRuntimeEnablementRequested: true,
            ReleaseCommercialReadinessClaimed: true,
            ProductLedgerPath: null,
            RedactionProductWiring: null,
            RuntimeFeatureFlag: null,
            AuthorityWiring: null,
            ReplayFailureEvidence: null));

        Assert.AreEqual(DurableRuntimeEnablementScaffoldDecision.Rejected, nullResult.Decision);
        CollectionAssert.Contains(nullResult.Blockers.ToArray(), DurableRuntimeEnablementScaffoldBlocker.MissingRequest);
        AssertNoProductRuntime(nullResult);

        Assert.AreEqual(DurableRuntimeEnablementScaffoldDecision.Rejected, emptyResult.Decision);
        CollectionAssert.Contains(emptyResult.Blockers.ToArray(), DurableRuntimeEnablementScaffoldBlocker.MissingExplicitTestOnlyScope);
        CollectionAssert.Contains(emptyResult.Blockers.ToArray(), DurableRuntimeEnablementScaffoldBlocker.ProductRuntimeEnablementRequested);
        CollectionAssert.Contains(emptyResult.Blockers.ToArray(), DurableRuntimeEnablementScaffoldBlocker.ReleaseCommercialReadinessClaimed);
        CollectionAssert.Contains(emptyResult.Blockers.ToArray(), DurableRuntimeEnablementScaffoldBlocker.MissingProductLedgerPathReadiness);
        CollectionAssert.Contains(emptyResult.Blockers.ToArray(), DurableRuntimeEnablementScaffoldBlocker.MissingRedactionProductWiring);
        CollectionAssert.Contains(emptyResult.Blockers.ToArray(), DurableRuntimeEnablementScaffoldBlocker.MissingRuntimeFeatureFlagReadiness);
        CollectionAssert.Contains(emptyResult.Blockers.ToArray(), DurableRuntimeEnablementScaffoldBlocker.MissingAuthorityWiring);
        CollectionAssert.Contains(emptyResult.Blockers.ToArray(), DurableRuntimeEnablementScaffoldBlocker.MissingReplayFailureEvidence);
        AssertNoProductRuntime(emptyResult);
    }

    [TestMethod]
    public void RuntimeScaffold_BlocksMissingMajorDependencies()
    {
        using var temp = new TempDirectory();
        var scaffold = new DurableRuntimeEnablementSafetyScaffold();
        var ready = ReadyRequest(temp.Path);
        var cases = new Dictionary<DurableRuntimeEnablementScaffoldRequest, DurableRuntimeEnablementScaffoldBlocker>
        {
            [ready with { ProductLedgerPath = null }] = DurableRuntimeEnablementScaffoldBlocker.MissingProductLedgerPathReadiness,
            [ready with { RedactionProductWiring = null }] = DurableRuntimeEnablementScaffoldBlocker.MissingRedactionProductWiring,
            [ready with { RuntimeFeatureFlag = null }] = DurableRuntimeEnablementScaffoldBlocker.MissingRuntimeFeatureFlagReadiness,
            [ready with { AuthorityWiring = null }] = DurableRuntimeEnablementScaffoldBlocker.MissingAuthorityWiring,
            [ready with { ReplayFailureEvidence = null }] = DurableRuntimeEnablementScaffoldBlocker.MissingReplayFailureEvidence
        };

        foreach (var testCase in cases)
        {
            var result = scaffold.Evaluate(testCase.Key);

            Assert.AreEqual(DurableRuntimeEnablementScaffoldDecision.Rejected, result.Decision, testCase.Value.ToString());
            CollectionAssert.Contains(result.Blockers.ToArray(), testCase.Value, testCase.Value.ToString());
            AssertNoProductRuntime(result);
        }
    }

    [TestMethod]
    public void RuntimeScaffold_BlocksUnsafeProductLedgerPathClaims()
    {
        using var temp = new TempDirectory();
        var scaffold = new DurableRuntimeEnablementSafetyScaffold();
        var ready = ReadyRequest(temp.Path);
        var outside = System.IO.Path.Combine(Directory.GetCurrentDirectory(), $"durable-runtime-product-{Guid.NewGuid():N}");
        var cases = new Dictionary<DurableRuntimeProductLedgerPathReadiness, DurableRuntimeEnablementScaffoldBlocker>
        {
            [ready.ProductLedgerPath! with { ProposedLedgerPath = "" }] = DurableRuntimeEnablementScaffoldBlocker.EmptyProductLedgerPath,
            [ready.ProductLedgerPath! with { LocalBoundaryRoot = "" }] = DurableRuntimeEnablementScaffoldBlocker.MissingLedgerBoundaryRoot,
            [ready.ProductLedgerPath! with { ProposedLedgerPath = outside }] = DurableRuntimeEnablementScaffoldBlocker.LedgerPathOutsideLocalBoundary,
            [ready.ProductLedgerPath! with { LocalOnly = false }] = DurableRuntimeEnablementScaffoldBlocker.LedgerPathNotLocalOnly,
            [ready.ProductLedgerPath! with { ProposedLedgerPath = "https://example.invalid/ledger" }] = DurableRuntimeEnablementScaffoldBlocker.LedgerPathClaimsProviderCloudNetwork,
            [ready.ProductLedgerPath! with { ProposedLedgerPath = @"\\server\share\ledger" }] = DurableRuntimeEnablementScaffoldBlocker.LedgerPathClaimsProviderCloudNetwork,
            [ready.ProductLedgerPath! with { ClaimsProviderCloudNetwork = true }] = DurableRuntimeEnablementScaffoldBlocker.LedgerPathClaimsProviderCloudNetwork,
            [ready.ProductLedgerPath! with { HasRedactionPolicy = false }] = DurableRuntimeEnablementScaffoldBlocker.LedgerPathMissingRedactionPolicy,
            [ready.ProductLedgerPath! with { HasRetentionPolicy = false }] = DurableRuntimeEnablementScaffoldBlocker.LedgerPathMissingRetentionPolicy,
            [ready.ProductLedgerPath! with { HasFailureReplayEvidence = false }] = DurableRuntimeEnablementScaffoldBlocker.LedgerPathMissingFailureReplayEvidence,
            [ready.ProductLedgerPath! with { ClaimsWormKmsCloud = true }] = DurableRuntimeEnablementScaffoldBlocker.LedgerPathClaimsWormKmsCloud,
            [ready.ProductLedgerPath! with { HasNoSymlinkJunctionReparsePointEvidence = false }] = DurableRuntimeEnablementScaffoldBlocker.LedgerPathSymlinkJunctionReparsePointRiskUnresolved,
            [ready.ProductLedgerPath! with { HasCanonicalRealPathEvidence = false }] = DurableRuntimeEnablementScaffoldBlocker.LedgerPathCanonicalizationMismatchRiskUnresolved
        };

        foreach (var testCase in cases)
        {
            var result = scaffold.Evaluate(ready with { ProductLedgerPath = testCase.Key });

            Assert.AreEqual(DurableRuntimeEnablementScaffoldDecision.Rejected, result.Decision, testCase.Value.ToString());
            CollectionAssert.Contains(result.Blockers.ToArray(), testCase.Value, testCase.Value.ToString());
            AssertNoProductRuntime(result);
        }

        Assert.IsFalse(Directory.Exists(outside));
    }

    [TestMethod]
    public void RuntimeScaffold_BlocksPathCorpusAndReparseThreatModelRisks()
    {
        using var temp = new TempDirectory();
        var scaffold = new DurableRuntimeEnablementSafetyScaffold();
        var ready = ReadyRequest(temp.Path);
        var cases = new Dictionary<string, DurableRuntimeEnablementScaffoldBlocker>
        {
            [System.IO.Path.Combine(temp.Path, "..", "escape")] = DurableRuntimeEnablementScaffoldBlocker.LedgerPathTraversalRejected,
            ["%TEMP%\\durable-runtime"] = DurableRuntimeEnablementScaffoldBlocker.LedgerPathEnvironmentVariableRejected,
            ["${TEMP}/durable-runtime"] = DurableRuntimeEnablementScaffoldBlocker.LedgerPathEnvironmentVariableRejected,
            [System.IO.Path.Combine(temp.Path, "CON")] = DurableRuntimeEnablementScaffoldBlocker.LedgerPathReservedDeviceNameRejected,
            [temp.Path + "\\mixed/separators"] = DurableRuntimeEnablementScaffoldBlocker.LedgerPathMixedSeparatorRejected
        };

        foreach (var testCase in cases)
        {
            var result = scaffold.Evaluate(
                ready with
                {
                    ProductLedgerPath = ready.ProductLedgerPath! with { ProposedLedgerPath = testCase.Key }
                });

            Assert.AreEqual(DurableRuntimeEnablementScaffoldDecision.Rejected, result.Decision, testCase.Value.ToString());
            CollectionAssert.Contains(result.Blockers.ToArray(), testCase.Value, testCase.Value.ToString());
            AssertNoProductRuntime(result);
        }
    }

    [TestMethod]
    public void RuntimeScaffold_BlocksInvalidRedactionProductWiring()
    {
        using var temp = new TempDirectory();
        var scaffold = new DurableRuntimeEnablementSafetyScaffold();
        var ready = ReadyRequest(temp.Path);
        var request = Request();
        var safeRedaction = new RedactionBeforePersistenceService().Evaluate(RedactionBeforePersistencePolicy.TestOnly, request);
        var rejectedRedaction = new RedactionBeforePersistenceService().Evaluate(
            RedactionBeforePersistencePolicy.TestOnly,
            request with { Metadata = new Dictionary<string, string> { ["note"] = "Authorization: Bearer secret-token" } });
        var cases = new Dictionary<DurableRuntimeRedactionProductWiringReadiness, DurableRuntimeEnablementScaffoldBlocker>
        {
            [ready.RedactionProductWiring! with { RedactionResult = null }] = DurableRuntimeEnablementScaffoldBlocker.RedactionResultMissing,
            [ready.RedactionProductWiring! with { RedactionResult = rejectedRedaction }] = DurableRuntimeEnablementScaffoldBlocker.RedactionResultRejected,
            [ready.RedactionProductWiring! with { EvidencePresent = false }] = DurableRuntimeEnablementScaffoldBlocker.RedactionEvidenceMissing,
            [ready.RedactionProductWiring! with { ExplicitPolicyId = "" }] = DurableRuntimeEnablementScaffoldBlocker.RedactionEvidencePolicyMissing,
            [ready.RedactionProductWiring! with { ExpectedCandidateHash = "wrong-hash" }] = DurableRuntimeEnablementScaffoldBlocker.RedactionEvidenceHashMismatch,
            [ready.RedactionProductWiring! with { RedactionResult = safeRedaction with { Evidence = safeRedaction.Evidence with { ContainsRawValues = true } } }] = DurableRuntimeEnablementScaffoldBlocker.RedactionEvidenceContainsRawValues,
            [ready.RedactionProductWiring! with { RedactionResult = safeRedaction with { Evidence = safeRedaction.Evidence with { CompletedBeforePersistence = false } } }] = DurableRuntimeEnablementScaffoldBlocker.RedactionEvidenceMissingBeforePersistence,
            [ready.RedactionProductWiring! with { CandidateAppendRequest = request with { Metadata = new Dictionary<string, string> { ["note"] = "token=raw-secret" } } }] = DurableRuntimeEnablementScaffoldBlocker.RedactionEvidenceSecretMarkerRejected
        };

        foreach (var testCase in cases)
        {
            var result = scaffold.Evaluate(ready with { RedactionProductWiring = testCase.Key });

            Assert.AreEqual(DurableRuntimeEnablementScaffoldDecision.Rejected, result.Decision, testCase.Value.ToString());
            CollectionAssert.Contains(result.Blockers.ToArray(), testCase.Value, testCase.Value.ToString());
            AssertNoProductRuntime(result);
        }
    }

    [TestMethod]
    public void RuntimeScaffold_BlocksMalformedDuplicateStaleAndInconsistentEvidenceReferences()
    {
        using var temp = new TempDirectory();
        var scaffold = new DurableRuntimeEnablementSafetyScaffold();
        var ready = ReadyRequest(temp.Path);
        var cases = new Dictionary<IReadOnlyList<string>, DurableRuntimeEnablementScaffoldBlocker>
        {
            [[]] = DurableRuntimeEnablementScaffoldBlocker.RedactionEvidenceReferenceMalformed,
            [[""]] = DurableRuntimeEnablementScaffoldBlocker.RedactionEvidenceReferenceMalformed,
            [["https://example.invalid/evidence"]] = DurableRuntimeEnablementScaffoldBlocker.RedactionEvidenceReferenceMalformed,
            [["docs/qa/durable-runtime-readiness/report.md", "DOCS/QA/DURABLE-RUNTIME-READINESS/REPORT.MD"]] = DurableRuntimeEnablementScaffoldBlocker.RedactionEvidenceReferenceDuplicate,
            [["docs/qa/stale-durable-runtime-readiness/report.md"]] = DurableRuntimeEnablementScaffoldBlocker.RedactionEvidenceReferenceStale,
            [["docs/qa/inconsistent-durable-runtime-readiness/report.md"]] = DurableRuntimeEnablementScaffoldBlocker.RedactionEvidenceReferenceInconsistent
        };

        foreach (var testCase in cases)
        {
            var request = Request() with { EvidenceReferences = testCase.Key };
            var redaction = new RedactionBeforePersistenceService().Evaluate(RedactionBeforePersistencePolicy.TestOnly, request);
            var result = scaffold.Evaluate(
                ready with
                {
                    RedactionProductWiring = ready.RedactionProductWiring! with
                    {
                        CandidateAppendRequest = request,
                        RedactionResult = redaction,
                        ExpectedCandidateHash = RedactionBeforePersistenceService.ComputeCandidateHash(request)
                    }
                });

            Assert.AreEqual(DurableRuntimeEnablementScaffoldDecision.Rejected, result.Decision, testCase.Value.ToString());
            CollectionAssert.Contains(result.Blockers.ToArray(), testCase.Value, testCase.Value.ToString());
            AssertNoProductRuntime(result);
        }
    }

    [TestMethod]
    public void RuntimeScaffold_BlocksRuntimeFeatureFlagReadinessGaps()
    {
        using var temp = new TempDirectory();
        var scaffold = new DurableRuntimeEnablementSafetyScaffold();
        var ready = ReadyRequest(temp.Path);
        var cases = new Dictionary<DurableRuntimeFeatureFlagProductReadiness, DurableRuntimeEnablementScaffoldBlocker>
        {
            [ready.RuntimeFeatureFlag! with { BlockedByDefault = false }] = DurableRuntimeEnablementScaffoldBlocker.RuntimeFeatureFlagNotBlockedByDefault,
            [ready.RuntimeFeatureFlag! with { ProductLedgerPathApproved = false }] = DurableRuntimeEnablementScaffoldBlocker.RuntimeFeatureFlagMissingLedgerDependency,
            [ready.RuntimeFeatureFlag! with { RedactionProductWiringApproved = false }] = DurableRuntimeEnablementScaffoldBlocker.RuntimeFeatureFlagMissingRedactionDependency,
            [ready.RuntimeFeatureFlag! with { AuthorityWiringApproved = false }] = DurableRuntimeEnablementScaffoldBlocker.RuntimeFeatureFlagMissingAuthorityDependency,
            [ready.RuntimeFeatureFlag! with { ReplayFailureEvidenceApproved = false }] = DurableRuntimeEnablementScaffoldBlocker.RuntimeFeatureFlagMissingReplayFailureDependency,
            [ready.RuntimeFeatureFlag! with { NoExternalTrustOverclaim = false }] = DurableRuntimeEnablementScaffoldBlocker.RuntimeFeatureFlagExternalTrustOverclaim,
            [ready.RuntimeFeatureFlag! with { HumanGoEvidencePresent = false }] = DurableRuntimeEnablementScaffoldBlocker.RuntimeFeatureFlagMissingHumanGoEvidence
        };

        foreach (var testCase in cases)
        {
            var result = scaffold.Evaluate(ready with { RuntimeFeatureFlag = testCase.Key });

            Assert.AreEqual(DurableRuntimeEnablementScaffoldDecision.Rejected, result.Decision, testCase.Value.ToString());
            CollectionAssert.Contains(result.Blockers.ToArray(), testCase.Value, testCase.Value.ToString());
            AssertNoProductRuntime(result);
        }
    }

    [TestMethod]
    public void RuntimeScaffold_BlocksAuthorityWiringScopeAndLiveClaims()
    {
        using var temp = new TempDirectory();
        var scaffold = new DurableRuntimeEnablementSafetyScaffold();
        var ready = ReadyRequest(temp.Path);
        var cases = new Dictionary<DurableRuntimeAuthorityWiringReadiness, DurableRuntimeEnablementScaffoldBlocker>
        {
            [ready.AuthorityWiring! with { HumanApprovalPresent = false }] = DurableRuntimeEnablementScaffoldBlocker.AuthorityMissingHumanApproval,
            [ready.AuthorityWiring! with { LocalTestOperatorIdentity = "" }] = DurableRuntimeEnablementScaffoldBlocker.AuthorityMissingLocalTestOperatorIdentity,
            [ready.AuthorityWiring! with { Reason = "" }] = DurableRuntimeEnablementScaffoldBlocker.AuthorityMissingReason,
            [ready.AuthorityWiring! with { EvidenceReferences = [] }] = DurableRuntimeEnablementScaffoldBlocker.AuthorityMissingEvidence,
            [ready.AuthorityWiring! with { Scope = "durable-runtime-product" }] = DurableRuntimeEnablementScaffoldBlocker.AuthorityScopeExceeded,
            [ready.AuthorityWiring! with { AttemptsLiveAutomationAuthority = true }] = DurableRuntimeEnablementScaffoldBlocker.AuthorityAttemptsLiveAutomation,
            [ready.AuthorityWiring! with { AttemptsProviderCloudKmsWorm = true }] = DurableRuntimeEnablementScaffoldBlocker.AuthorityAttemptsProviderCloudKmsWorm,
            [ready.AuthorityWiring! with { ClaimsRealHumanAuthorization = true }] = DurableRuntimeEnablementScaffoldBlocker.AuthorityClaimsRealHumanAuthorization,
            [ready.AuthorityWiring! with { ClaimsProductionOperatorApproval = true }] = DurableRuntimeEnablementScaffoldBlocker.AuthorityClaimsProductionOperatorApproval,
            [ready.AuthorityWiring! with { ClaimsProductAuthority = true }] = DurableRuntimeEnablementScaffoldBlocker.AuthorityClaimsProductAuthority,
            [ready.AuthorityWiring! with { ClaimsReleaseApproval = true }] = DurableRuntimeEnablementScaffoldBlocker.AuthorityClaimsReleaseApproval,
            [ready.AuthorityWiring! with { Reason = "real human authorization granted" }] = DurableRuntimeEnablementScaffoldBlocker.AuthorityClaimsRealHumanAuthorization,
            [ready.AuthorityWiring! with { Reason = "runtime approval real" }] = DurableRuntimeEnablementScaffoldBlocker.AuthorityClaimsProductAuthority,
            [ready.AuthorityWiring! with { Reason = "release-ready approval" }] = DurableRuntimeEnablementScaffoldBlocker.AuthorityClaimsReleaseApproval,
            [ready.AuthorityWiring! with { Reason = "Browser/CDP live execution allowed" }] = DurableRuntimeEnablementScaffoldBlocker.AuthorityAttemptsLiveAutomation
        };

        foreach (var testCase in cases)
        {
            var result = scaffold.Evaluate(ready with { AuthorityWiring = testCase.Key });

            Assert.AreEqual(DurableRuntimeEnablementScaffoldDecision.Rejected, result.Decision, testCase.Value.ToString());
            CollectionAssert.Contains(result.Blockers.ToArray(), testCase.Value, testCase.Value.ToString());
            AssertNoProductRuntime(result);
        }
    }

    [TestMethod]
    public void RuntimeScaffold_BlocksReplayFailureEvidenceGaps()
    {
        using var temp = new TempDirectory();
        var scaffold = new DurableRuntimeEnablementSafetyScaffold();
        var ready = ReadyRequest(temp.Path);
        var cases = new Dictionary<DurableRuntimeReplayFailureEvidenceReadiness, DurableRuntimeEnablementScaffoldBlocker>
        {
            [ready.ReplayFailureEvidence! with { HasReplayEvidence = false }] = DurableRuntimeEnablementScaffoldBlocker.ReplayEvidenceMissing,
            [ready.ReplayFailureEvidence! with { HasFailureEvidence = false }] = DurableRuntimeEnablementScaffoldBlocker.FailureEvidenceMissing,
            [ready.ReplayFailureEvidence! with { EvidenceReferences = [] }] = DurableRuntimeEnablementScaffoldBlocker.ReplayFailureEvidenceReferenceMissing,
            [ready.ReplayFailureEvidence! with { EvidenceReferences = ["https://example.invalid/evidence"] }] = DurableRuntimeEnablementScaffoldBlocker.ReplayFailureEvidenceReferenceMalformed,
            [ready.ReplayFailureEvidence! with { EvidenceReferences = ["docs/qa/durable-runtime-readiness/report.md", "DOCS/QA/DURABLE-RUNTIME-READINESS/REPORT.MD"] }] = DurableRuntimeEnablementScaffoldBlocker.ReplayFailureEvidenceReferenceDuplicate,
            [ready.ReplayFailureEvidence! with { HasReadModelSnapshot = false }] = DurableRuntimeEnablementScaffoldBlocker.ReadModelSnapshotMissing,
            [ready.ReplayFailureEvidence! with { HasReplayReadModelConsistencyCheck = false }] = DurableRuntimeEnablementScaffoldBlocker.ReplayReadModelConsistencyMissing,
            [ready.ReplayFailureEvidence! with { HasFailureModeCatalog = false }] = DurableRuntimeEnablementScaffoldBlocker.FailureModeCatalogMissing,
            [ready.ReplayFailureEvidence! with { HasRollbackAndNonRollbackClassification = false }] = DurableRuntimeEnablementScaffoldBlocker.RollbackNonRollbackClassificationMissing,
            [ready.ReplayFailureEvidence! with { ClaimsLiveReplayExecution = true }] = DurableRuntimeEnablementScaffoldBlocker.ReplayEvidenceClaimsLiveExecution,
            [ready.ReplayFailureEvidence! with { EvidenceReferences = ["docs/qa/replay-live-execution/report.md"] }] = DurableRuntimeEnablementScaffoldBlocker.ReplayEvidenceClaimsLiveExecution,
            [ready.ReplayFailureEvidence! with { ContainsRawPayloadEvidence = true }] = DurableRuntimeEnablementScaffoldBlocker.ReplayEvidenceContainsRawPayload,
            [ready.ReplayFailureEvidence! with { AcknowledgesTailDeletionLimitation = false }] = DurableRuntimeEnablementScaffoldBlocker.TailDeletionLimitationNotAcknowledged,
            [ready.ReplayFailureEvidence! with { AcknowledgesCheckpointLimitation = false }] = DurableRuntimeEnablementScaffoldBlocker.CheckpointLimitationNotAcknowledged,
            [ready.ReplayFailureEvidence! with { HasNoWormKmsCloudDisclaimer = false }] = DurableRuntimeEnablementScaffoldBlocker.NoWormKmsCloudDisclaimerMissing,
            [ready.ReplayFailureEvidence! with { ClaimsDurableProductRecovery = true }] = DurableRuntimeEnablementScaffoldBlocker.DurableProductRecoveryClaimed
        };

        foreach (var testCase in cases)
        {
            var result = scaffold.Evaluate(ready with { ReplayFailureEvidence = testCase.Key });

            Assert.AreEqual(DurableRuntimeEnablementScaffoldDecision.Rejected, result.Decision, testCase.Value.ToString());
            CollectionAssert.Contains(result.Blockers.ToArray(), testCase.Value, testCase.Value.ToString());
            AssertNoProductRuntime(result);
        }
    }

    [TestMethod]
    public void RuntimeScaffold_AllowsOnlyTestOnlyLocalReadinessPreviewNeverProductEnablement()
    {
        using var temp = new TempDirectory();
        var result = new DurableRuntimeEnablementSafetyScaffold().Evaluate(ReadyRequest(temp.Path));

        Assert.AreEqual(DurableRuntimeEnablementScaffoldDecision.ReadinessPreviewAllowed, result.Decision);
        Assert.IsTrue(result.ReadinessPreviewAllowed);
        Assert.AreEqual(0, result.Blockers.Count);
        AssertNoProductRuntime(result);
        Assert.AreEqual(DurableRuntimeEnablementSafetyScaffold.NoProductRuntimeEnablementStatus, result.StatusText);
    }

    [TestMethod]
    public void RuntimeScaffold_SourceContainsNoProductRegistrationHandlersOrExternalProviders()
    {
        var sourcePath = System.IO.Path.Combine(
            FindRepoRoot(),
            "src",
            "OneBrain.Core",
            "Approval",
            "DurableRuntimeEnablementSafetyScaffold.cs");
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
            "ReleaseReady = true",
            "CommercialReady = true",
            "ReleaseCommercialReady: true"
        };

        foreach (var fragment in forbiddenFragments)
        {
            Assert.IsFalse(source.Contains(fragment, StringComparison.Ordinal), fragment);
        }
    }

    private static DurableRuntimeEnablementScaffoldRequest ReadyRequest(string boundaryRoot)
    {
        var request = Request();
        var redaction = new RedactionBeforePersistenceService().Evaluate(RedactionBeforePersistencePolicy.TestOnly, request);
        var hash = RedactionBeforePersistenceService.ComputeCandidateHash(request);
        return new DurableRuntimeEnablementScaffoldRequest(
            ExplicitTestOnlyScope: true,
            ProductRuntimeEnablementRequested: false,
            ReleaseCommercialReadinessClaimed: false,
            ProductLedgerPath: new DurableRuntimeProductLedgerPathReadiness(
                ProposedLedgerPath: System.IO.Path.Combine(boundaryRoot, "durable-runtime-readiness-preview"),
                LocalBoundaryRoot: boundaryRoot,
                LocalOnly: true,
                HasRedactionPolicy: true,
                HasRetentionPolicy: true,
                HasFailureReplayEvidence: true,
                ClaimsProviderCloudNetwork: false,
                ClaimsWormKmsCloud: false,
                HasNoSymlinkJunctionReparsePointEvidence: true,
                HasCanonicalRealPathEvidence: true),
            RedactionProductWiring: new DurableRuntimeRedactionProductWiringReadiness(
                CandidateAppendRequest: request,
                RedactionResult: redaction,
                ExpectedCandidateHash: hash,
                ExplicitPolicyId: RedactionBeforePersistencePolicy.TestOnlyPolicyId,
                EvidencePresent: true),
            RuntimeFeatureFlag: new DurableRuntimeFeatureFlagProductReadiness(
                BlockedByDefault: true,
                ProductLedgerPathApproved: true,
                RedactionProductWiringApproved: true,
                AuthorityWiringApproved: true,
                ReplayFailureEvidenceApproved: true,
                NoExternalTrustOverclaim: true,
                HumanGoEvidencePresent: true),
            AuthorityWiring: new DurableRuntimeAuthorityWiringReadiness(
                HumanApprovalPresent: true,
                LocalTestOperatorIdentity: "local-test-operator:fixture",
                Reason: "readiness preview only",
                EvidenceReferences: ["docs/qa/durable-runtime-readiness/report.md"],
                Scope: DurableRuntimeEnablementSafetyScaffold.RequiredScope,
                AttemptsLiveAutomationAuthority: false,
                AttemptsProviderCloudKmsWorm: false,
                ClaimsRealHumanAuthorization: false,
                ClaimsProductionOperatorApproval: false,
                ClaimsProductAuthority: false,
                ClaimsReleaseApproval: false),
            ReplayFailureEvidence: new DurableRuntimeReplayFailureEvidenceReadiness(
                HasReplayEvidence: true,
                HasFailureEvidence: true,
                EvidenceReferences: ["docs/qa/durable-runtime-readiness/report.md"],
                HasReadModelSnapshot: true,
                HasReplayReadModelConsistencyCheck: true,
                HasFailureModeCatalog: true,
                HasRollbackAndNonRollbackClassification: true,
                ClaimsLiveReplayExecution: false,
                ContainsRawPayloadEvidence: false,
                AcknowledgesTailDeletionLimitation: true,
                AcknowledgesCheckpointLimitation: true,
                HasNoWormKmsCloudDisclaimer: true,
                ClaimsDurableProductRecovery: false));
    }

    private static DurableAuditTrailAppendOnlyMinimalRequest Request() =>
        new(
            EventKind: DurableAuditTrailAppendOnlyMinimal.SupportedEventKind,
            ActorReference: "human-operator:fixture",
            ApprovalReference: "approval-001",
            EvidenceReferences:
            [
                "docs/qa/durable-runtime-readiness/report.md"
            ],
            Metadata: new Dictionary<string, string>
            {
                ["decision"] = "test-only-readiness-preview"
            });

    private static void AssertNoProductRuntime(DurableRuntimeEnablementScaffoldResult result)
    {
        Assert.IsFalse(result.ProductRuntimeEnabled);
        Assert.IsFalse(result.ProductLedgerPathActive);
        Assert.IsFalse(result.ProductServiceRegistrationAllowed);
        Assert.IsFalse(result.ProductCommandHandlersAllowed);
        Assert.IsFalse(result.UiProductActionsAllowed);
        Assert.IsFalse(result.ProviderCloudNetworkAllowed);
        Assert.IsFalse(result.KmsWormCloudAllowed);
        Assert.IsFalse(result.LiveAutomationAllowed);
        Assert.IsFalse(result.ReleaseCommercialReady);
        Assert.AreEqual(DurableRuntimeEnablementSafetyScaffold.NoProductRuntimeEnablementStatus, result.StatusText);
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
                "DurableRuntimeEnablementSafetyScaffold.cs")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        Assert.Fail("repo root not found");
        return string.Empty;
    }

    private sealed class TempDirectory : IDisposable
    {
        public TempDirectory()
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"nodal-durable-runtime-scaffold-{Guid.NewGuid():N}");
            Directory.CreateDirectory(Path);
        }

        public string Path { get; }

        public void Dispose()
        {
            if (Directory.Exists(Path))
            {
                Directory.Delete(Path, recursive: true);
            }
        }
    }
}
