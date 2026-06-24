using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.Safety.Tests.SimulatedRuntime;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("GovernancePreAudit")]
[TestCategory("M827")]
[TestCategory("M828")]
[TestCategory("M829")]
[TestCategory("M830")]
[TestCategory("M831")]
[TestCategory("M832")]
[TestCategory("M833")]
[TestCategory("M834")]
[TestCategory("M835")]
[TestCategory("M836")]
[TestCategory("M837")]
[TestCategory("M838")]
[TestCategory("M839")]
[TestCategory("M840")]
[TestCategory("M841")]
[TestCategory("M842")]
[TestCategory("M843")]
[TestCategory("M844")]
public sealed class NodalOsGovernancePreAuditM827M844Tests
{
    private const string GovernanceSnapshotPath = "artifacts/agent-operations/m827/governance-snapshot-contract.json";
    private const string CoverageMatrixPath = "artifacts/agent-operations/m828/governance-snapshot-coverage-matrix.json";
    private const string ReadinessScorecardPath = "artifacts/agent-operations/m830/simulated-runtime-readiness-scorecard.json";
    private const string PreAuditIndexPath = "artifacts/agent-operations/m833/pre-audit-evidence-index.json";
    private const string DriftGuardPath = "artifacts/agent-operations/m836/safety-drift-guard-contract.json";
    private const string HandoffPackPath = "artifacts/agent-operations/m839/audit-handoff-pack-contract.json";
    private const string AuditQuestionsPath = "artifacts/agent-operations/m840/audit-question-set.json";
    private const string DecisionOptionsPath = "artifacts/agent-operations/m841/audit-decision-options.json";
    private const string FinalPath = "artifacts/agent-operations/m827-m844/governance-pre-audit-go-no-go.json";
    private const string ProductBridgeCspPath = "artifacts/agent-operations/m843/product-bridge-csp-unchanged-proof.json";
    private const string NextMilestonePath = "artifacts/agent-operations/m844/next-macro-milestone-recommendation.json";

    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(Environment.CurrentDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "OneBrain.slnx")))
            dir = dir.Parent;

        return dir?.FullName ?? Environment.CurrentDirectory;
    }

    private static string FullPath(string relativePath) => Path.Combine(RepoRoot(), relativePath);
    private static string ReadAll(string relativePath) => File.ReadAllText(FullPath(relativePath));

    [TestMethod]
    public void GovernanceSnapshotCreatedAndRecordsCoreRuntimeState()
    {
        var snapshot = SimulatedGovernancePreAudit.CreateSnapshot();

        Assert.AreEqual("snapshot-m827-m844", snapshot.SnapshotId);
        Assert.AreEqual(SimulatedGovernancePreAudit.SourceCommit, snapshot.SourceCommit);
        Assert.AreEqual(SimulatedDryRunOrchestrator.RuntimeType, snapshot.RuntimeType);
        Assert.AreEqual(SimulatedDryRunOrchestrator.RequiredFixtureType, snapshot.FixtureType);
        Assert.IsFalse(snapshot.ProductiveRuntime);
        CollectionAssert.AreEquivalent(SimulatedRuntimeRoutingMatrix.AllowedRoutingTable.Keys.ToArray(), snapshot.AllowedCapabilities.ToArray());
        CollectionAssert.AreEquivalent(SimulatedRuntimeRoutingMatrix.DenylistedCapabilities.ToArray(), snapshot.DenylistedCapabilities.ToArray());
        Assert.AreEqual("DENY_UNSUPPORTED_CAPABILITY", snapshot.UnsupportedCapabilityBehavior);
        CollectionAssert.Contains(snapshot.PolicyDecisionTypes.ToArray(), nameof(SimulatedPolicyDecisionType.DenyUnsupportedCapability));
        CollectionAssert.Contains(snapshot.ReasonCodes.ToArray(), SimulatedPolicyReasonCodes.DeniedDenylistedCapability);
        CollectionAssert.Contains(snapshot.ManualApprovalStatuses.ToArray(), nameof(SimulatedApprovalStatus.ApprovalGrantedSimulated));
        CollectionAssert.Contains(snapshot.TimelineEventKinds.ToArray(), "SIMULATED_REPLAY_GUARD_EVALUATED");
        CollectionAssert.Contains(snapshot.ReplayGuardModes.ToArray(), nameof(ReplayMode.AuditOnlyInMemory));
        Assert.AreEqual("READY", snapshot.AuditExportPackageStatus);
        Assert.AreEqual("READY", snapshot.DeterminismStatus);
        Assert.AreEqual("READY", snapshot.NoExecutionProofStatus);
        Assert.AreEqual("READY", snapshot.RedactionProofStatus);
        Assert.AreEqual("PROVEN", snapshot.NoRealExecutorProofStatus);
        Assert.AreEqual(0, snapshot.SideEffectSinkInvocations);
        Assert.IsFalse(snapshot.ProductFilesModified);
        Assert.IsFalse(snapshot.BridgeCspModified);
    }

    [TestMethod]
    public void GovernanceCoverageMatrixContainsAllRuntimeDomainsReady()
    {
        var matrix = SimulatedGovernancePreAudit.CreateCoverageMatrix();
        var domains = matrix.Select(static x => x.Domain).ToArray();

        foreach (var expected in new[]
        {
            "routing",
            "denylist",
            "unsupported guard",
            "policy decisions",
            "manual approval",
            "evidence/ledger",
            "timeline roundtrip",
            "replay guard",
            "audit export",
            "determinism",
            "redaction",
            "no-execution",
            "no-real-executor",
            "release/store NO-GO",
            "product/Bridge/CSP unchanged"
        })
        {
            CollectionAssert.Contains(domains, expected);
        }

        Assert.IsTrue(matrix.All(static x => x.CoverageStatus == "READY"));
        Assert.IsTrue(matrix.All(static x => x.NoGoBoundaryPreserved));
    }

    [TestMethod]
    public void GovernanceNegativeDriftChecksRejectDangerousDrift()
    {
        Assert.IsFalse(SimulatedGovernancePreAudit.HasBlockingGovernanceDrift(new SimulatedGovernanceDriftProbe()));

        foreach (var probe in new[]
        {
            new SimulatedGovernanceDriftProbe(ProductiveRuntime: true),
            new SimulatedGovernanceDriftProbe(ProviderCloudInvoked: true),
            new SimulatedGovernanceDriftProbe(FilesystemWritePerformed: true),
            new SimulatedGovernanceDriftProbe(BrowserAutomationPerformed: true),
            new SimulatedGovernanceDriftProbe(CapabilityUnlocked: true),
            new SimulatedGovernanceDriftProbe(PublicReleasePerformed: true),
            new SimulatedGovernanceDriftProbe(StoreSubmissionPerformed: true),
            new SimulatedGovernanceDriftProbe(SignedPublicZipCreated: true),
            new SimulatedGovernanceDriftProbe(ProductFilesModified: true),
            new SimulatedGovernanceDriftProbe(BridgeCspModified: true),
            new SimulatedGovernanceDriftProbe(SelectedExecutorSetOnDeny: true),
            new SimulatedGovernanceDriftProbe(UnknownCapabilityAllowed: true),
            new SimulatedGovernanceDriftProbe(ApprovalOverrideAllowedForDenylisted: true),
            new SimulatedGovernanceDriftProbe(ReplayExecuted: true),
            new SimulatedGovernanceDriftProbe(ExportContainsSensitiveData: true)
        })
        {
            Assert.IsTrue(SimulatedGovernancePreAudit.HasBlockingGovernanceDrift(probe), probe.ToString());
        }
    }

    [TestMethod]
    public void ReadinessScorecardMarksSimulatedDomainsReadyAndNoGoDomainsDisabled()
    {
        var scorecard = SimulatedGovernancePreAudit.CreateReadinessScorecard();

        foreach (var key in new[]
        {
            "routingMatrixReadiness",
            "denylistReadiness",
            "unsupportedCapabilityGuardReadiness",
            "policyDecisionReadiness",
            "manualApprovalBoundaryReadiness",
            "evidenceLedgerReadiness",
            "timelineRoundtripReadiness",
            "replayGuardReadiness",
            "auditExportReadiness",
            "determinismReadiness",
            "redactionReadiness",
            "noExecutionReadiness",
            "noRealExecutorReadiness"
        })
        {
            Assert.AreEqual(SimulatedReadinessLevel.Ready, scorecard.Categories[key], key);
        }

        Assert.AreEqual(SimulatedReadinessLevel.DisabledByPolicy, scorecard.Categories["productiveRuntimeUnlockReadiness"]);
        Assert.AreEqual(SimulatedReadinessLevel.DisabledByPolicy, scorecard.Categories["providerCloudLiveCallsReadiness"]);
        Assert.AreEqual(SimulatedReadinessLevel.DisabledByPolicy, scorecard.Categories["filesystemBrowserCapabilityUnlockReadiness"]);
        Assert.AreEqual(SimulatedReadinessLevel.NoGo, scorecard.Categories["publicReleaseReadiness"]);
        Assert.AreEqual(SimulatedReadinessLevel.NoGo, scorecard.Categories["chromeWebStoreReadiness"]);
        Assert.IsTrue(scorecard.FullSuiteEvidencePresent);
    }

    [TestMethod]
    public void ReadinessPercentagesAreCanonicalAndNeverClaimProductiveUnlock()
    {
        var percentages = SimulatedGovernancePreAudit.CreateReadinessScorecard().Percentages;

        Assert.AreEqual(100, percentages["Routing matrix"]);
        Assert.AreEqual(100, percentages["Denylist enforcement"]);
        Assert.AreEqual(100, percentages["Unsupported capability guard"]);
        Assert.AreEqual(100, percentages["Policy decision normalization"]);
        Assert.AreEqual(100, percentages["Manual approval simulated boundary"]);
        Assert.AreEqual(100, percentages["Replay guard"]);
        Assert.IsTrue(percentages["No-execution proof"] is 99 or 100);
        Assert.AreEqual(0, percentages["Productive runtime unlock"]);
        Assert.AreEqual(0, percentages["Provider/cloud live calls"]);
        Assert.AreEqual(0, percentages["Filesystem/browser/capability unlock"]);
        Assert.AreEqual(0, percentages["Public Release"]);
        Assert.AreEqual(0, percentages["Chrome Web Store"]);
    }

    [TestMethod]
    public void ScorecardNegativeClaimsGuardRejectsFalseClaims()
    {
        foreach (var claim in new[]
        {
            "PRODUCTIVE_ENABLED ready",
            "provider/cloud enabled",
            "filesystem write enabled",
            "browser automation enabled",
            "capability unlock enabled",
            "release ready",
            "Store ready",
            "signed ZIP ready",
            "product files modified",
            "Bridge/CSP modified",
            "full suite PASS without evidence",
            "redaction skipped",
            "replay executes",
            "approval grants real execution"
        })
        {
            Assert.IsTrue(SimulatedGovernancePreAudit.RejectsForbiddenScorecardClaim(claim), claim);
        }
    }

    [TestMethod]
    public void PreAuditIndexAndConsistencyCoverExpectedEvidence()
    {
        var index = SimulatedGovernancePreAudit.CreatePreAuditEvidenceIndex();

        foreach (var expected in new[]
        {
            "routing matrix artifact",
            "denylist enforcement artifact",
            "policy decision normalization report",
            "unsupported capability guard report",
            "manual approval boundary report",
            "timeline roundtrip report",
            "replay guard report",
            "audit export readiness report",
            "determinism report",
            "NO-GO boundaries",
            "validation summary"
        })
        {
            CollectionAssert.Contains(index.ToArray(), expected);
        }

        Assert.IsTrue(SimulatedGovernancePreAudit.PreAuditConsistencyPasses());
        foreach (var mismatch in new[]
        {
            "missing_decision_type",
            "missing_reason_code",
            "missing_capability",
            "missing_denylisted_capability",
            "unsupported_behavior_mismatch",
            "approval_status_mismatch",
            "replay_mode_mismatch",
            "export_flag_mismatch",
            "readiness_percentage_mismatch",
            "no_go_boundary_mismatch",
            "full_suite_claim_without_evidence"
        })
        {
            Assert.IsTrue(SimulatedGovernancePreAudit.DetectsPreAuditMismatch(mismatch), mismatch);
        }
    }

    [TestMethod]
    public void PreAuditPackageSummaryRestatesGoNoGoAndValidation()
    {
        var package = SimulatedGovernancePreAudit.CreatePreAuditPackage();

        Assert.AreEqual("NODAL OS", package.Project);
        Assert.AreEqual(SimulatedGovernancePreAudit.Branch, package.Branch);
        Assert.AreEqual("test-only/in-memory/fake-only", package.Scope);
        CollectionAssert.Contains(package.GoDomains.ToArray(), "governance snapshot");
        CollectionAssert.Contains(package.NoGoDomains.ToArray(), "runtime productive execution");
        Assert.IsTrue(package.EvidenceIndex.Count > 0);
        Assert.AreEqual(SimulatedReadinessLevel.Ready, package.ReadinessScorecard.Categories["routingMatrixReadiness"]);
        StringAssert.Contains(package.ValidationSummary, "full suite pass");
        Assert.IsTrue(package.CaveatHistory.Contains("BrowserRuntimeSmoke", StringComparison.Ordinal));
        Assert.IsTrue(package.RiskRegister.Count > 0);
        Assert.IsTrue(package.AuditQuestions.Count > 0);
        Assert.IsFalse(package.RuntimeProductiveExecution);
        Assert.IsFalse(package.ProviderCloudLiveCalls);
        Assert.IsFalse(package.FilesystemBrowserCapabilityUnlock);
        Assert.IsFalse(package.ReleaseStoreReady);
        Assert.IsFalse(package.ProductBridgeCspModified);
    }

    [TestMethod]
    public void SafetyDriftGuardAndGateDecisionsRejectBlockingDrift()
    {
        Assert.AreEqual(SimulatedDriftGateDecision.SafetyDriftGateGoNoDrift, SimulatedGovernancePreAudit.DecideDriftGate(SimulatedDriftStatus.NoDrift));
        Assert.AreEqual(SimulatedDriftGateDecision.SafetyDriftGateConditionalDocDrift, SimulatedGovernancePreAudit.DecideDriftGate(SimulatedDriftStatus.NonBlockingDocDrift));
        Assert.AreEqual(SimulatedDriftGateDecision.SafetyDriftGateNoGoSafetyDrift, SimulatedGovernancePreAudit.DecideDriftGate(SimulatedDriftStatus.BlockingSafetyDrift));
        Assert.AreEqual(SimulatedDriftGateDecision.SafetyDriftGateNoGoScopeDrift, SimulatedGovernancePreAudit.DecideDriftGate(SimulatedDriftStatus.BlockingScopeDrift));
        Assert.AreEqual(SimulatedDriftGateDecision.SafetyDriftGateNoGoReleaseDrift, SimulatedGovernancePreAudit.DecideDriftGate(SimulatedDriftStatus.BlockingReleaseDrift));

        foreach (var probe in new[]
        {
            new SimulatedGovernanceDriftProbe(RuntimeTypeChanged: true),
            new SimulatedGovernanceDriftProbe(DenylistNotFirst: true),
            new SimulatedGovernanceDriftProbe(ReplayNotAuditOnly: true),
            new SimulatedGovernanceDriftProbe(ExportNotRedacted: true),
            new SimulatedGovernanceDriftProbe(SideEffectsInvoked: true)
        })
        {
            Assert.IsTrue(SimulatedGovernancePreAudit.HasBlockingGovernanceDrift(probe), probe.ToString());
        }
    }

    [TestMethod]
    public void SafetyDriftReportIncludesRequiredActionAndAuditRecommendation()
    {
        var clean = SimulatedGovernancePreAudit.CreateSafetyDriftReport();
        Assert.AreEqual(SimulatedDriftStatus.NoDrift, clean.DriftStatus);
        Assert.AreEqual("NOT_BLOCKING", clean.BlockingStatus);
        Assert.IsTrue(clean.AcceptedDomains.Count > 0);
        Assert.IsTrue(clean.RequiredAction.Length > 0);
        Assert.IsTrue(clean.AuditRecommendation.Contains("external audit", StringComparison.Ordinal));

        foreach (var status in new[]
        {
            SimulatedDriftStatus.BlockingSafetyDrift,
            SimulatedDriftStatus.BlockingScopeDrift,
            SimulatedDriftStatus.BlockingReleaseDrift
        })
        {
            var report = SimulatedGovernancePreAudit.CreateSafetyDriftReport(status);
            Assert.AreEqual("BLOCKING", report.BlockingStatus);
            Assert.IsTrue(report.DetectedDriftItems.Count > 0);
        }
    }

    [TestMethod]
    public void AuditHandoffPackContainsRequiredExternalAuditSections()
    {
        var pack = SimulatedGovernancePreAudit.CreateAuditHandoffPack();

        Assert.IsTrue(pack.ExecutiveSummary.Contains("fake-only", StringComparison.Ordinal));
        Assert.AreEqual(SimulatedGovernancePreAudit.SourceCommit, pack.CurrentCommit);
        Assert.AreEqual(SimulatedGovernancePreAudit.Branch, pack.Branch);
        CollectionAssert.Contains(pack.AllowedFilesTouched.ToArray(), "tests/OneBrain.Safety.Tests");
        CollectionAssert.Contains(pack.ProhibitedFilesUntouched.ToArray(), "browser-extension Bridge/CSP");
        Assert.IsTrue(pack.GoNoGoTable.Count > 0);
        Assert.IsTrue(pack.CapabilityMatrix.Count > 0);
        CollectionAssert.Contains(pack.DecisionTypeTable.ToArray(), nameof(SimulatedPolicyDecisionType.AllowSimulatedDryRun));
        CollectionAssert.Contains(pack.ApprovalBoundaryTable.ToArray(), nameof(SimulatedApprovalStatus.ApprovalGrantedSimulated));
        CollectionAssert.Contains(pack.ReplayExportDeterminismTable.ToArray(), "replay audit-only");
        Assert.AreEqual(SimulatedDriftGateDecision.SafetyDriftGateGoNoDrift, pack.SafetyDriftGateResult);
        Assert.IsTrue(pack.ValidationEvidence.Any(static x => x.Contains("PASS", StringComparison.Ordinal)));
        Assert.IsTrue(pack.CaveatHistory.Contains("BrowserRuntimeSmoke", StringComparison.Ordinal));
        Assert.IsTrue(pack.OpenRisks.Count > 0);
        Assert.IsTrue(pack.AuditQuestions.Count >= 15);
        Assert.IsTrue(pack.RecommendedAuditModel.Contains("XHigh", StringComparison.Ordinal));
        CollectionAssert.Contains(pack.ExpectedAuditDecisionOptions.ToArray(), "AUDIT_GO_CONTINUE_SIMULATED_RUNTIME");
    }

    [TestMethod]
    public void AuditQuestionSetCoversCriticalBoundaries()
    {
        var questions = SimulatedGovernancePreAudit.CreateAuditQuestions();

        foreach (var expected in new[]
        {
            "fake-only/in-memory",
            "runtime productivo",
            "provider/cloud",
            "filesystem write",
            "browser automation",
            "overridea denylist",
            "Replay puede re-ejecutar",
            "exponer secretos",
            "Unknown capability",
            "Denylist se evalúa antes",
            "selectedExecutor queda null",
            "Full suite",
            "Product files o Bridge/CSP",
            "Release/store",
            "docs/artifacts/tests"
        })
        {
            Assert.IsTrue(questions.Any(x => x.Contains(expected, StringComparison.OrdinalIgnoreCase)), expected);
        }
    }

    [TestMethod]
    public void AuditDecisionOptionsIncludeOnlyAllowedAuditOutcomes()
    {
        var options = SimulatedGovernancePreAudit.CreateAuditDecisionOptions();

        foreach (var expected in new[]
        {
            "AUDIT_GO_CONTINUE_SIMULATED_RUNTIME",
            "AUDIT_CONDITIONAL_GO_DOC_DRIFT_ONLY",
            "AUDIT_CONDITIONAL_GO_FLAKY_EXTERNAL_ONLY",
            "AUDIT_NO_GO_SAFETY_DRIFT",
            "AUDIT_NO_GO_SCOPE_DRIFT",
            "AUDIT_NO_GO_RELEASE_DRIFT",
            "AUDIT_NO_GO_PRODUCTIVE_RUNTIME_PATH",
            "AUDIT_NO_GO_PROVIDER_CLOUD_PATH",
            "AUDIT_NO_GO_FILESYSTEM_BROWSER_UNLOCK_PATH"
        })
        {
            CollectionAssert.Contains(options.ToArray(), expected);
        }

        Assert.IsFalse(options.Any(static x => x.Contains("PRODUCTIVE_ENABLE_GO", StringComparison.Ordinal)));
    }

    [TestMethod]
    public void ArtifactsExistAndFinalDecisionIsReady()
    {
        foreach (var path in new[]
        {
            GovernanceSnapshotPath,
            CoverageMatrixPath,
            ReadinessScorecardPath,
            PreAuditIndexPath,
            DriftGuardPath,
            HandoffPackPath,
            AuditQuestionsPath,
            DecisionOptionsPath,
            FinalPath
        })
        {
            Assert.IsTrue(File.Exists(FullPath(path)), path);
        }

        var final = ReadAll(FinalPath);
        StringAssert.Contains(final, "SIMULATED_RUNTIME_GOVERNANCE_SNAPSHOT_PRE_AUDIT_READY");
        StringAssert.Contains(final, "\"runtimeProductiveExecution\": \"DISABLED\"");
        StringAssert.Contains(final, "\"productiveEnabled\": \"PROHIBITED\"");
    }

    [TestMethod]
    public void ProductRuntimeReleaseStoreBridgeAndCspRemainBlocked()
    {
        var final = ReadAll(FinalPath);
        var productBridge = ReadAll(ProductBridgeCspPath);

        StringAssert.Contains(final, "\"providerCloudLiveCalls\": \"DISABLED\"");
        StringAssert.Contains(final, "\"filesystemBrowserCapabilityUnlock\": \"DISABLED\"");
        StringAssert.Contains(final, "\"publicRelease\": \"NO-GO\"");
        StringAssert.Contains(final, "\"chromeWebStore\": \"NO-GO\"");
        StringAssert.Contains(productBridge, "\"productFilesModified\": false");
        StringAssert.Contains(productBridge, "\"bridgeCspModified\": false");
    }

    [TestMethod]
    public void NextMilestoneRecommendsExternalAuditFoundationFreezeCandidate()
    {
        var content = ReadAll(NextMilestonePath);

        StringAssert.Contains(content, "M845-M856");
        StringAssert.Contains(content, "External Audit Execution + Simulated Runtime Foundation Freeze Candidate");
        StringAssert.Contains(content, "\"productiveUnlockAllowed\": false");
    }
}
