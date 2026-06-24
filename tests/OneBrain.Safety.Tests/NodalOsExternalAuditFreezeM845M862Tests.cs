using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.Safety.Tests.SimulatedRuntime;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("ExternalAuditFreeze")]
[TestCategory("M845")]
[TestCategory("M846")]
[TestCategory("M847")]
[TestCategory("M848")]
[TestCategory("M849")]
[TestCategory("M850")]
[TestCategory("M851")]
[TestCategory("M852")]
[TestCategory("M853")]
[TestCategory("M854")]
[TestCategory("M855")]
[TestCategory("M856")]
[TestCategory("M857")]
[TestCategory("M858")]
[TestCategory("M859")]
[TestCategory("M860")]
[TestCategory("M861")]
[TestCategory("M862")]
public sealed class NodalOsExternalAuditFreezeM845M862Tests
{
    private const string InputPackagePath = "artifacts/agent-operations/m845/external-audit-input-package.json";
    private const string AuditPromptPath = "artifacts/agent-operations/m846/external-audit-prompt.json";
    private const string ResultIntakePath = "artifacts/agent-operations/m847/external-audit-result-intake-contract.json";
    private const string FreezeCandidatePath = "artifacts/agent-operations/m851/simulated-runtime-foundation-freeze-candidate-contract.json";
    private const string FreezeLockPath = "artifacts/agent-operations/m854/freeze-lock-guard-contract.json";
    private const string FinalPath = "artifacts/agent-operations/m845-m862/foundation-freeze-candidate-go-no-go.json";

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
    public void ExternalAuditInputPackageIncludesCommitScopeGovernanceAndNoGoBoundaries()
    {
        var package = SimulatedExternalAuditFreeze.CreateInputPackage();

        Assert.AreEqual(SimulatedExternalAuditFreeze.Branch, package.Branch);
        Assert.AreEqual(SimulatedExternalAuditFreeze.SourceCommit, package.Commit);
        StringAssert.Contains(package.Scope, "tests/safety");
        CollectionAssert.Contains(package.GoDomains.ToArray(), "governance snapshot");
        CollectionAssert.Contains(package.NoGoDomains.ToArray(), "runtime productive execution");
        CollectionAssert.Contains(package.AllowedTouchedDirectories.ToArray(), "tests/OneBrain.Safety.Tests");
        CollectionAssert.Contains(package.ProhibitedTouchedDirectories.ToArray(), "browser-extension");
        Assert.AreEqual("snapshot-m827-m844", package.GovernanceSnapshot.SnapshotId);
        Assert.AreEqual(SimulatedReadinessLevel.Ready, package.ReadinessScorecard.Categories["routingMatrixReadiness"]);
        Assert.AreEqual(SimulatedDriftGateDecision.SafetyDriftGateGoNoDrift, package.SafetyDriftGateResult);
        Assert.IsTrue(package.ValidationEvidence.Any(static x => x.Contains("PASS", StringComparison.Ordinal)));
        Assert.IsTrue(package.CaveatHistory.Contains("Gate 9", StringComparison.Ordinal));
        Assert.IsTrue(package.RiskRegister.Count >= 10);
        Assert.IsTrue(package.AuditQuestions.Count >= 15);
        CollectionAssert.Contains(package.ExpectedAuditDecisionOptions.ToArray(), "AUDIT_NO_GO_PRODUCTIVE_RUNTIME_PATH");
        Assert.IsFalse(package.ProductiveRuntime);
        Assert.IsFalse(package.ProviderCloudLiveCalls);
        Assert.IsFalse(package.FilesystemBrowserCapabilityUnlock);
        Assert.IsFalse(package.ReleaseStoreReady);
        Assert.IsFalse(package.ProductBridgeCspModified);
    }

    [TestMethod]
    public void ExternalAuditPromptCoversExpectedReviewTargetsAndDecisionOptions()
    {
        var prompt = SimulatedExternalAuditFreeze.CreateAuditPrompt();

        StringAssert.Contains(prompt.TargetAuditor, "Claude");
        StringAssert.Contains(prompt.TargetAuditor, "GPT-5.5 Thinking XHigh");
        foreach (var target in new[]
        {
            "simulated runtime fake-only boundary",
            "no real executor proof",
            "no-execution proof",
            "redaction proof",
            "routing matrix",
            "denylist-first enforcement",
            "unsupported capability guard",
            "manual approval boundary",
            "replay audit-only guard",
            "audit export redaction",
            "release/store/productive NO-GO boundaries"
        })
        {
            CollectionAssert.Contains(prompt.ReviewTargets.ToArray(), target);
        }

        CollectionAssert.Contains(prompt.DecisionOptions.ToArray(), "AUDIT_GO_CONTINUE_SIMULATED_RUNTIME");
        CollectionAssert.Contains(prompt.DecisionOptions.ToArray(), "AUDIT_NO_GO_FILESYSTEM_BROWSER_UNLOCK_PATH");
        Assert.IsTrue(prompt.CoversGoNoGoBoundaries);
        Assert.IsFalse(prompt.ProductiveRuntimeAllowed);
        Assert.IsFalse(prompt.ProviderCloudAllowed);
        Assert.IsFalse(prompt.FilesystemBrowserCapabilityUnlockAllowed);
        Assert.IsFalse(prompt.ReleaseStoreAllowed);
        Assert.IsFalse(prompt.ProductBridgeCspAllowed);
    }

    [TestMethod]
    public void ExternalAuditResultIntakeAllowsOnlyGoOrConditionalFreezeCandidate()
    {
        Assert.IsTrue(SimulatedExternalAuditFreeze.IntakeAuditResult(SimulatedAuditDecision.AuditGoContinueSimulatedRuntime).FreezeCandidateAllowed);
        Assert.IsTrue(SimulatedExternalAuditFreeze.IntakeAuditResult(SimulatedAuditDecision.AuditConditionalGoDocDriftOnly).FreezeCandidateAllowed);
        Assert.IsTrue(SimulatedExternalAuditFreeze.IntakeAuditResult(SimulatedAuditDecision.AuditConditionalGoFlakyExternalOnly).FreezeCandidateAllowed);
        Assert.IsTrue(SimulatedExternalAuditFreeze.IntakeAuditResult(SimulatedAuditDecision.AuditConditionalGoDocDriftOnly).CaveatRequired);

        foreach (var decision in new[]
        {
            SimulatedAuditDecision.AuditNoGoSafetyDrift,
            SimulatedAuditDecision.AuditNoGoScopeDrift,
            SimulatedAuditDecision.AuditNoGoReleaseDrift,
            SimulatedAuditDecision.AuditNoGoProductiveRuntimePath,
            SimulatedAuditDecision.AuditNoGoProviderCloudPath,
            SimulatedAuditDecision.AuditNoGoFilesystemBrowserUnlockPath,
            SimulatedAuditDecision.AuditNotRunPrepOnly
        })
        {
            var result = SimulatedExternalAuditFreeze.IntakeAuditResult(decision);
            Assert.IsFalse(result.FreezeCandidateAllowed, decision.ToString());
            Assert.IsTrue(result.BlockingFindings.Count > 0, decision.ToString());
        }
    }

    [TestMethod]
    public void FindingsTriageMatrixAppliesBlockingRules()
    {
        Assert.IsFalse(SimulatedExternalAuditFreeze.Triage(SimulatedFindingClass.DocDrift, SimulatedFindingSeverity.Low).BlocksFreeze);
        Assert.IsTrue(SimulatedExternalAuditFreeze.Triage(SimulatedFindingClass.DocDrift, SimulatedFindingSeverity.Medium).ConditionalAllowed);
        Assert.IsTrue(SimulatedExternalAuditFreeze.Triage(SimulatedFindingClass.FlakyExternal, SimulatedFindingSeverity.Low, targetedSecurityPass: true).ConditionalAllowed);
        Assert.IsTrue(SimulatedExternalAuditFreeze.Triage(SimulatedFindingClass.SafetyDrift, SimulatedFindingSeverity.High).BlocksFreeze);
        Assert.IsTrue(SimulatedExternalAuditFreeze.Triage(SimulatedFindingClass.ScopeDrift, SimulatedFindingSeverity.Low).BlocksFreeze);
        Assert.IsTrue(SimulatedExternalAuditFreeze.Triage(SimulatedFindingClass.ReleaseDrift, SimulatedFindingSeverity.Low).BlocksFreeze);
        Assert.IsTrue(SimulatedExternalAuditFreeze.Triage(SimulatedFindingClass.ProductiveRuntimePath, SimulatedFindingSeverity.Low).BlocksFreeze);
        Assert.IsTrue(SimulatedExternalAuditFreeze.Triage(SimulatedFindingClass.ProviderCloudPath, SimulatedFindingSeverity.Low).BlocksFreeze);
        Assert.IsTrue(SimulatedExternalAuditFreeze.Triage(SimulatedFindingClass.FilesystemBrowserUnlockPath, SimulatedFindingSeverity.Low).BlocksFreeze);
        Assert.IsTrue(SimulatedExternalAuditFreeze.Triage(SimulatedFindingClass.RedactionGap, SimulatedFindingSeverity.High).BlocksFreeze);
        Assert.IsTrue(SimulatedExternalAuditFreeze.Triage(SimulatedFindingClass.NoExecutionGap, SimulatedFindingSeverity.Low).BlocksFreeze);
        Assert.IsTrue(SimulatedExternalAuditFreeze.Triage(SimulatedFindingClass.ValidationGap, SimulatedFindingSeverity.Low).BlocksFreeze);
    }

    [TestMethod]
    public void RemediationPlanPreservesScopeAndBlocksUnsafeFixes()
    {
        Assert.IsTrue(SimulatedExternalAuditFreeze.CreateRemediationPlan(SimulatedFindingClass.DocDrift, SimulatedRemediationType.DocFixOnly).CanProceedToFreeze);

        foreach (var type in new[]
        {
            SimulatedRemediationType.TestFixRequired,
            SimulatedRemediationType.SafetyFixRequired,
            SimulatedRemediationType.ScopeFixRequired,
            SimulatedRemediationType.ReleaseBoundaryFixRequired,
            SimulatedRemediationType.ValidationRerunRequired
        })
        {
            var plan = SimulatedExternalAuditFreeze.CreateRemediationPlan(SimulatedFindingClass.SafetyDrift, type);
            Assert.IsFalse(plan.CanProceedToFreeze, type.ToString());
            CollectionAssert.Contains(plan.AllowedFileScope.ToArray(), "tests/OneBrain.Safety.Tests");
            CollectionAssert.Contains(plan.ProhibitedFileScope.ToArray(), "Bridge/CSP");
        }
    }

    [TestMethod]
    public void FindingsTriageReportRestatesEligibilityAndNoProductBoundaries()
    {
        var report = SimulatedExternalAuditFreeze.CreateFindingsTriageReport(SimulatedAuditDecision.AuditNotRunPrepOnly);

        Assert.AreEqual(SimulatedAuditDecision.AuditNotRunPrepOnly, report.AuditDecision);
        Assert.AreEqual(1, report.BlockingFindings);
        Assert.IsFalse(report.FreezeCandidateEligibility);
        Assert.IsTrue(report.RemediationPlanSummary.Count > 0);
        Assert.IsFalse(report.ProductRuntimeAllowed);
        Assert.IsFalse(report.ReleaseStoreAllowed);
        Assert.IsFalse(report.ProductBridgeCspAllowed);
    }

    [TestMethod]
    public void FreezeCandidateContractAllowsOnlyAuditedGoAndNeverImpliesReleaseOrRuntime()
    {
        Assert.AreEqual(SimulatedFreezeStatus.FreezeCandidateReady, SimulatedExternalAuditFreeze.CreateFreezeCandidate(SimulatedAuditDecision.AuditGoContinueSimulatedRuntime).FreezeStatus);
        Assert.AreEqual(SimulatedFreezeStatus.FreezeCandidateReadyWithCaveats, SimulatedExternalAuditFreeze.CreateFreezeCandidate(SimulatedAuditDecision.AuditConditionalGoDocDriftOnly).FreezeStatus);
        Assert.AreEqual(SimulatedFreezeStatus.FreezeCandidateReadyWithCaveats, SimulatedExternalAuditFreeze.CreateFreezeCandidate(SimulatedAuditDecision.AuditConditionalGoFlakyExternalOnly).FreezeStatus);
        Assert.AreEqual(SimulatedFreezeStatus.FreezeCandidateBlockedByValidation, SimulatedExternalAuditFreeze.CreateFreezeCandidate(SimulatedAuditDecision.AuditGoContinueSimulatedRuntime, validationPass: false).FreezeStatus);
        Assert.AreEqual(SimulatedFreezeStatus.FreezeCandidateBlockedByScopeDrift, SimulatedExternalAuditFreeze.CreateFreezeCandidate(SimulatedAuditDecision.AuditNoGoScopeDrift).FreezeStatus);
        Assert.AreEqual(SimulatedFreezeStatus.FreezeCandidateBlockedByReleaseDrift, SimulatedExternalAuditFreeze.CreateFreezeCandidate(SimulatedAuditDecision.AuditNoGoReleaseDrift).FreezeStatus);
        Assert.IsFalse(SimulatedExternalAuditFreeze.CreateFreezeCandidate(SimulatedAuditDecision.AuditNoGoSafetyDrift).FreezeAllowed);

        var prepOnly = SimulatedExternalAuditFreeze.CreateFreezeCandidate(SimulatedAuditDecision.AuditNotRunPrepOnly);
        Assert.AreEqual("SIMULATED_RUNTIME_FOUNDATION_TEST_ONLY", prepOnly.Scope);
        Assert.IsFalse(prepOnly.FreezeAllowed);
        Assert.IsFalse(prepOnly.ProductiveRuntime);
        Assert.IsFalse(prepOnly.ProviderCloud);
        Assert.IsFalse(prepOnly.FilesystemWrite);
        Assert.IsFalse(prepOnly.BrowserAutomation);
        Assert.IsFalse(prepOnly.CapabilityUnlock);
        Assert.IsFalse(prepOnly.PublicRelease);
        Assert.IsFalse(prepOnly.ChromeWebStore);
        Assert.IsFalse(prepOnly.SignedPublicZip);
        Assert.IsFalse(prepOnly.ProductFilesModified);
        Assert.IsFalse(prepOnly.BridgeCspModified);
    }

    [TestMethod]
    public void FreezeBoundaryMatrixListsGoDomainsAndBlocksNoGoDomains()
    {
        var matrix = SimulatedExternalAuditFreeze.CreateFreezeBoundaryMatrix();

        CollectionAssert.Contains(matrix.GoWithinFreeze.ToArray(), "simulated runtime contracts");
        CollectionAssert.Contains(matrix.GoWithinFreeze.ToArray(), "fake-only executors");
        CollectionAssert.Contains(matrix.GoWithinFreeze.ToArray(), "in-memory evidence ledger");
        CollectionAssert.Contains(matrix.GoWithinFreeze.ToArray(), "timeline/replay/export/determinism");
        CollectionAssert.Contains(matrix.NoGoWithinFreeze.ToArray(), "productive runtime");
        CollectionAssert.Contains(matrix.NoGoWithinFreeze.ToArray(), "provider/cloud");
        CollectionAssert.Contains(matrix.NoGoWithinFreeze.ToArray(), "filesystem write real");
        CollectionAssert.Contains(matrix.NoGoWithinFreeze.ToArray(), "browser automation real");
        CollectionAssert.Contains(matrix.NoGoWithinFreeze.ToArray(), "public release");
        CollectionAssert.Contains(matrix.NoGoWithinFreeze.ToArray(), "Chrome Web Store");
        CollectionAssert.Contains(matrix.NoGoWithinFreeze.ToArray(), "signed public ZIP");
        CollectionAssert.Contains(matrix.NoGoWithinFreeze.ToArray(), "product file changes");
        CollectionAssert.Contains(matrix.NoGoWithinFreeze.ToArray(), "Bridge/CSP changes");
    }

    [TestMethod]
    public void FreezeLockGuardRejectsForbiddenInterpretations()
    {
        foreach (var claim in new[]
        {
            "freeze means productive enabled",
            "freeze means provider cloud enabled",
            "freeze means filesystem write enabled",
            "freeze means browser automation enabled",
            "freeze means capability unlock enabled",
            "freeze means public release ready",
            "freeze means Store ready",
            "freeze means signed ZIP ready",
            "freeze means product files can be modified",
            "freeze means Bridge/CSP can be modified",
            "freeze means audit findings can be ignored",
            "freeze means no future tests required"
        })
        {
            Assert.IsTrue(SimulatedExternalAuditFreeze.RejectsForbiddenFreezeClaim(claim), claim);
        }
    }

    [TestMethod]
    public void FreezeLockEvidenceLinksRequiredRefsAndPreservesNoGoFlags()
    {
        var evidence = SimulatedExternalAuditFreeze.CreateFreezeLockEvidence(SimulatedAuditDecision.AuditNotRunPrepOnly);

        Assert.AreEqual("freeze-lock-m855", evidence.FreezeLockId);
        Assert.AreEqual("freeze-candidate-m851", evidence.FreezeCandidateId);
        Assert.AreEqual(SimulatedFreezeLockStatus.FreezeLockBlocked, evidence.LockStatus);
        Assert.IsTrue(evidence.BlockedInterpretations.Count > 0);
        Assert.IsTrue(evidence.ValidationRefs.Count > 0);
        Assert.IsTrue(evidence.AuditRefs.Count > 0);
        Assert.IsTrue(evidence.GoNoGoRefs.Count > 0);
        Assert.AreEqual("no-execution-proof", evidence.NoExecutionProofRef);
        Assert.AreEqual("redaction-proof", evidence.RedactionProofRef);
        Assert.AreEqual("safety-drift-gate-go-no-drift", evidence.SafetyDriftGateRef);
        Assert.IsFalse(evidence.ProductFilesModified);
        Assert.IsFalse(evidence.BridgeCspModified);
        Assert.IsFalse(evidence.ProductiveRuntime);
        Assert.IsFalse(evidence.ReleaseStore);
    }

    [TestMethod]
    public void FreezeLockGateDecisionFollowsAuditAndValidation()
    {
        Assert.AreEqual(SimulatedFreezeLockGateDecision.SimulatedFoundationFreezeLockReady, SimulatedExternalAuditFreeze.DecideFreezeLockGate(SimulatedAuditDecision.AuditGoContinueSimulatedRuntime));
        Assert.AreEqual(SimulatedFreezeLockGateDecision.SimulatedFoundationFreezeLockReadyWithCaveats, SimulatedExternalAuditFreeze.DecideFreezeLockGate(SimulatedAuditDecision.AuditConditionalGoDocDriftOnly));
        Assert.AreEqual(SimulatedFreezeLockGateDecision.SimulatedFoundationFreezeLockBlockedByAudit, SimulatedExternalAuditFreeze.DecideFreezeLockGate(SimulatedAuditDecision.AuditNoGoSafetyDrift));
        Assert.AreEqual(SimulatedFreezeLockGateDecision.SimulatedFoundationFreezeLockBlockedByValidation, SimulatedExternalAuditFreeze.DecideFreezeLockGate(SimulatedAuditDecision.AuditGoContinueSimulatedRuntime, validationPass: false));
        Assert.AreEqual(SimulatedFreezeLockGateDecision.SimulatedFoundationFreezeLockBlockedByScopeDrift, SimulatedExternalAuditFreeze.DecideFreezeLockGate(SimulatedAuditDecision.AuditNoGoScopeDrift));
        Assert.AreEqual(SimulatedFreezeLockGateDecision.SimulatedFoundationFreezeLockBlockedByReleaseDrift, SimulatedExternalAuditFreeze.DecideFreezeLockGate(SimulatedAuditDecision.AuditNoGoReleaseDrift));
        Assert.AreEqual(SimulatedFreezeLockGateDecision.SimulatedFoundationFreezeLockBlockedByAudit, SimulatedExternalAuditFreeze.DecideFreezeLockGate(SimulatedAuditDecision.AuditNotRunPrepOnly));
    }

    [TestMethod]
    public void PostAuditContinuationPlanSeparatesAllowedConditionalAndBlockedWork()
    {
        var plan = SimulatedExternalAuditFreeze.CreateContinuationPlan();

        CollectionAssert.Contains(plan.AllowedImmediateWork.ToArray(), "docs cleanup");
        CollectionAssert.Contains(plan.AllowedImmediateWork.ToArray(), "fake-only in-memory matrix consolidation");
        CollectionAssert.Contains(plan.ConditionalWork.ToArray(), "future dry-run real planning");
        CollectionAssert.Contains(plan.BlockedWork.ToArray(), "productive runtime");
        CollectionAssert.Contains(plan.BlockedWork.ToArray(), "provider/cloud");
        CollectionAssert.Contains(plan.BlockedWork.ToArray(), "filesystem write real");
        CollectionAssert.Contains(plan.BlockedWork.ToArray(), "public release");
        CollectionAssert.Contains(plan.BlockedWork.ToArray(), "Chrome Web Store");
        CollectionAssert.Contains(plan.BlockedWork.ToArray(), "Bridge/CSP changes");
        Assert.IsTrue(plan.FutureAuditRequiredWork.Count > 0);
        Assert.IsTrue(plan.OwnerManualApprovalRequiredWork.Count > 0);
    }

    [TestMethod]
    public void NextMacroHitoRecommendationFollowsFreezeLockOutcome()
    {
        StringAssert.Contains(
            SimulatedExternalAuditFreeze.RecommendNextMacroHito(SimulatedFreezeLockGateDecision.SimulatedFoundationFreezeLockReady),
            "Simulated Runtime Foundation Freeze Lock Documentation");
        StringAssert.Contains(
            SimulatedExternalAuditFreeze.RecommendNextMacroHito(SimulatedFreezeLockGateDecision.SimulatedFoundationFreezeLockReadyWithCaveats),
            "Caveat Remediation");
        StringAssert.Contains(
            SimulatedExternalAuditFreeze.RecommendNextMacroHito(SimulatedFreezeLockGateDecision.SimulatedFoundationFreezeLockBlockedByAudit),
            "Audit Remediation");
    }

    [TestMethod]
    public void PostAuditRiskRegisterCoversKnownRisks()
    {
        var risks = SimulatedExternalAuditFreeze.CreateRiskRegister().Risks;

        foreach (var expected in new[]
        {
            "freeze misinterpreted as release",
            "freeze misinterpreted as productive unlock",
            "future drift from simulated to real execution",
            "provider/cloud accidentally wired",
            "filesystem/browser unlock accidentally wired",
            "product/Bridge/CSP touched without gate",
            "redaction regression",
            "no-execution proof regression",
            "full suite flake recurrence",
            "docs/artifacts/tests contradiction"
        })
        {
            CollectionAssert.Contains(risks.ToArray(), expected);
        }
    }

    [TestMethod]
    public void ArtifactsExistAndFinalDecisionIsPrepReadyAuditPending()
    {
        foreach (var path in new[]
        {
            InputPackagePath,
            AuditPromptPath,
            ResultIntakePath,
            FreezeCandidatePath,
            FreezeLockPath,
            FinalPath
        })
        {
            Assert.IsTrue(File.Exists(FullPath(path)), path);
        }

        var final = ReadAll(FinalPath);
        StringAssert.Contains(final, "SIMULATED_RUNTIME_FOUNDATION_FREEZE_CANDIDATE_PREP_READY_AUDIT_EXECUTION_PENDING");
        StringAssert.Contains(final, "\"externalAuditExecuted\": false");
        StringAssert.Contains(final, "\"freezeLockGateDecision\": \"SIMULATED_FOUNDATION_FREEZE_LOCK_BLOCKED_BY_AUDIT\"");
    }

    [TestMethod]
    public void ProductRuntimeProviderCloudReleaseStoreBridgeAndCspRemainBlocked()
    {
        var final = ReadAll(FinalPath);

        StringAssert.Contains(final, "\"runtimeProductiveExecution\": \"DISABLED\"");
        StringAssert.Contains(final, "\"providerCloudLiveCalls\": \"DISABLED\"");
        StringAssert.Contains(final, "\"filesystemBrowserCapabilityUnlock\": \"DISABLED\"");
        StringAssert.Contains(final, "\"publicRelease\": \"NO-GO\"");
        StringAssert.Contains(final, "\"chromeWebStore\": \"NO-GO\"");
        StringAssert.Contains(final, "\"signedPublicZipCreated\": false");
        StringAssert.Contains(final, "\"productFilesModified\": false");
        StringAssert.Contains(final, "\"bridgeCspModified\": false");
    }

    [TestMethod]
    public void NextMacroHitoPreservesAuditPendingPath()
    {
        var recommendation = ReadAll("artifacts/agent-operations/m858/next-macro-hito-recommendation.json");

        StringAssert.Contains(recommendation, "M863-M874");
        StringAssert.Contains(recommendation, "External Audit Result Application + Freeze Candidate Lock + Future Runtime Re-Entry Criteria");
        StringAssert.Contains(recommendation, "\"productiveUnlockAllowed\": false");
    }
}
