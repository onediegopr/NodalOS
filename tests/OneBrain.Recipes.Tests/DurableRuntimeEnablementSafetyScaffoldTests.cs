using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.Core.Approval;

namespace OneBrain.Recipes.Tests;

[TestClass]
[TestCategory("ApprovalHumanReview")]
[TestCategory("PhaseEApprovalHumanReview")]
public sealed class DurableRuntimeEnablementSafetyScaffoldTests
{
    [TestMethod]
    public void RuntimeScaffold_ProducesTestOnlyLocalReadinessPreviewStatus()
    {
        using var temp = new TempDirectory();
        var request = Request();
        var redaction = new RedactionBeforePersistenceService().Evaluate(RedactionBeforePersistencePolicy.TestOnly, request);
        var result = new DurableRuntimeEnablementSafetyScaffold().Evaluate(
            new DurableRuntimeEnablementScaffoldRequest(
                ExplicitTestOnlyScope: true,
                ProductRuntimeEnablementRequested: false,
                ReleaseCommercialReadinessClaimed: false,
                ProductLedgerPath: new DurableRuntimeProductLedgerPathReadiness(
                    ProposedLedgerPath: System.IO.Path.Combine(temp.Path, "durable-runtime-readiness-preview"),
                    LocalBoundaryRoot: temp.Path,
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
                    ExpectedCandidateHash: RedactionBeforePersistenceService.ComputeCandidateHash(request),
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
                    Reason: "recipe preview only",
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
                    ClaimsDurableProductRecovery: false)));

        Assert.AreEqual(DurableRuntimeEnablementScaffoldDecision.ReadinessPreviewAllowed, result.Decision);
        Assert.IsTrue(result.ReadinessPreviewAllowed);
        Assert.AreEqual(0, result.Blockers.Count);
        Assert.AreEqual(DurableRuntimeEnablementSafetyScaffold.NoProductRuntimeEnablementStatus, result.StatusText);
        Assert.IsFalse(result.ProductRuntimeEnabled);
        Assert.IsFalse(result.ProductLedgerPathActive);
        Assert.IsFalse(result.ProductServiceRegistrationAllowed);
        Assert.IsFalse(result.ProductCommandHandlersAllowed);
        Assert.IsFalse(result.UiProductActionsAllowed);
        Assert.IsFalse(result.ProviderCloudNetworkAllowed);
        Assert.IsFalse(result.KmsWormCloudAllowed);
        Assert.IsFalse(result.LiveAutomationAllowed);
        Assert.IsFalse(result.ReleaseCommercialReady);
        Assert.IsFalse(File.Exists(System.IO.Path.Combine(temp.Path, "durable-audit-trail.append-only.jsonl")));
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
