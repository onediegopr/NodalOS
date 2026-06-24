using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("FreezeLock")]
[TestCategory("M873")]
[TestCategory("M874")]
[TestCategory("M875")]
[TestCategory("M876")]
[TestCategory("M877")]
[TestCategory("M878")]
[TestCategory("M879")]
[TestCategory("M880")]
[TestCategory("M881")]
[TestCategory("M882")]
[TestCategory("M883")]
[TestCategory("M884")]
[TestCategory("M873M884")]
public sealed class NodalOsFreezeLockM873M884Tests
{
    private const string EligibilityPath = "artifacts/agent-operations/m873/freeze-lock-eligibility-verification.json";
    private const string LockPath = "artifacts/agent-operations/m874/simulated-foundation-freeze-lock-contract.json";
    private const string NegativeGuardPath = "artifacts/agent-operations/m875/freeze-lock-negative-claim-guard.json";
    private const string BaselinePath = "artifacts/agent-operations/m876/frozen-baseline-index.json";
    private const string ChangeControlPath = "artifacts/agent-operations/m877/frozen-baseline-change-control.json";
    private const string DeferredPath = "artifacts/agent-operations/m878/deferred-findings-register.json";
    private const string ReEntryCriteriaPath = "artifacts/agent-operations/m879/future-runtime-re-entry-criteria.json";
    private const string ReEntryMatrixPath = "artifacts/agent-operations/m880/re-entry-gate-decision-matrix.json";
    private const string ReEntryRiskPath = "artifacts/agent-operations/m881/re-entry-risk-register.json";
    private const string GoNoGoPath = "artifacts/agent-operations/m873-m884/simulated-runtime-foundation-freeze-lock-go-no-go.json";

    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(Environment.CurrentDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "OneBrain.slnx")))
            dir = dir.Parent;

        return dir?.FullName ?? Environment.CurrentDirectory;
    }

    private static string ReadAll(string relativePath) =>
        File.ReadAllText(Path.Combine(RepoRoot(), relativePath));

    [TestMethod]
    public void freeze_lock_eligibility_created()
    {
        var content = ReadAll(EligibilityPath);

        StringAssert.Contains(content, "\"verificationType\": \"FREEZE_LOCK_ELIGIBILITY_VERIFICATION\"");
        StringAssert.Contains(content, "\"eligibilityDecision\": \"ELIGIBLE_FOR_SIMULATED_FOUNDATION_FREEZE_LOCK\"");
    }

    [TestMethod]
    public void freeze_lock_eligibility_requires_reaudit_ready_f1_f2_and_measured_proofs()
    {
        var content = ReadAll(EligibilityPath);

        StringAssert.Contains(content, "\"reAuditDecision\": \"REAUDIT_READY_FOR_FREEZE_LOCK\"");
        StringAssert.Contains(content, "\"f1Status\": \"REMEDIATED\"");
        StringAssert.Contains(content, "\"f2Status\": \"REMEDIATED\"");
        StringAssert.Contains(content, "\"measuredNoExecutionProof\": true");
        StringAssert.Contains(content, "\"realAdversarialRedaction\": true");
        StringAssert.Contains(content, "\"fullSuitePassEvidence\": true");
    }

    [TestMethod]
    public void freeze_lock_eligibility_rejects_drift_product_bridge_runtime_provider_and_release_store()
    {
        var content = ReadAll(EligibilityPath);

        foreach (var expected in new[]
        {
            "\"noSafetyDrift\": true",
            "\"noScopeDrift\": true",
            "\"productFilesModified\": false",
            "\"bridgeCspModified\": false",
            "\"runtimeProductive\": false",
            "\"providerCloud\": false",
            "\"filesystemWrite\": false",
            "\"browserAutomation\": false",
            "\"capabilityUnlock\": false",
            "\"publicRelease\": false",
            "\"chromeWebStore\": false",
            "\"signedPublicZip\": false"
        })
        {
            StringAssert.Contains(content, expected);
        }
    }

    [TestMethod]
    public void simulated_foundation_freeze_lock_created_and_marks_test_only_internal_baseline()
    {
        var content = ReadAll(LockPath);

        StringAssert.Contains(content, "\"lockStatus\": \"SIMULATED_FOUNDATION_FREEZE_LOCK_READY\"");
        StringAssert.Contains(content, "\"lockedScope\": \"SIMULATED_RUNTIME_FOUNDATION_TEST_ONLY\"");
        StringAssert.Contains(content, "\"lockMode\": \"INTERNAL_TEST_ONLY_BASELINE\"");
        StringAssert.Contains(content, "\"reAuditDecision\": \"REAUDIT_READY_FOR_FREEZE_LOCK\"");
    }

    [TestMethod]
    public void freeze_lock_links_measured_no_execution_redaction_validation_and_allowed_no_go_domains()
    {
        var content = ReadAll(LockPath);

        StringAssert.Contains(content, "NodalOsAuditQualityRemediationM863M868Tests.cs");
        StringAssert.Contains(content, "SimulatedDryRunOrchestrator.cs");
        StringAssert.Contains(content, "SimulatedRedactor.cs");
        StringAssert.Contains(content, "M863-M868 PASS 48");
        StringAssert.Contains(content, "M869-M872 PASS 10");
        StringAssert.Contains(content, "tests/safety only");
        StringAssert.Contains(content, "runtime productive execution");
        StringAssert.Contains(content, "Chrome Web Store");
    }

    [TestMethod]
    public void freeze_lock_marks_productive_provider_filesystem_browser_release_and_product_bridge_false()
    {
        var content = ReadAll(LockPath);

        foreach (var expected in NoGoFalseFields)
            StringAssert.Contains(content, expected);
    }

    [TestMethod]
    public void freeze_lock_negative_claim_guard_rejects_false_freeze_interpretations()
    {
        var content = ReadAll(NegativeGuardPath);

        foreach (var rejected in new[]
        {
            "freeze lock means public release ready",
            "freeze lock means Chrome Web Store ready",
            "freeze lock means signed ZIP ready",
            "freeze lock means runtime productive enabled",
            "freeze lock means provider/cloud enabled",
            "freeze lock means filesystem write enabled",
            "freeze lock means browser automation enabled",
            "freeze lock means capability unlock enabled",
            "freeze lock means product files may be modified",
            "freeze lock means Bridge/CSP may be modified",
            "freeze lock means future audits no longer required",
            "freeze lock means findings F3/F5/F6/F7/F9 are fixed",
            "freeze lock means real runtime re-entry approved"
        })
        {
            StringAssert.Contains(content, rejected);
        }
    }

    [TestMethod]
    public void frozen_baseline_index_links_source_commit_freeze_lock_evidence_validation_and_no_go_boundaries()
    {
        var content = ReadAll(BaselinePath);

        StringAssert.Contains(content, "9eee2919206ed6c6f41da54725af5ef6a7f63cd5");
        StringAssert.Contains(content, "freeze-lock-m873-m884-simulated-foundation");
        StringAssert.Contains(content, "m827");
        StringAssert.Contains(content, "m830");
        StringAssert.Contains(content, "m863-m868");
        StringAssert.Contains(content, "m869-m872");
        StringAssert.Contains(content, "m815");
        StringAssert.Contains(content, "m806");
        StringAssert.Contains(content, "m797");
        StringAssert.Contains(content, "deferred-findings-register.json");
        StringAssert.Contains(content, "Bridge/CSP modification");
    }

    [TestMethod]
    public void change_control_allows_docs_tests_artifacts_quality_and_blocks_unsafe_boundaries()
    {
        var content = ReadAll(ChangeControlPath);

        foreach (var changeClass in new[]
        {
            "DOC_ONLY_ALLOWED",
            "TEST_ONLY_ALLOWED",
            "ARTIFACT_CONSOLIDATION_ALLOWED",
            "QUALITY_HARDENING_ALLOWED",
            "REQUIRES_REAUDIT",
            "REQUIRES_NEW_SAFETY_GATE",
            "BLOCKED_PRODUCTIVE_RUNTIME",
            "BLOCKED_PROVIDER_CLOUD",
            "BLOCKED_FILESYSTEM_BROWSER_UNLOCK",
            "BLOCKED_RELEASE_STORE",
            "BLOCKED_PRODUCT_BRIDGE_CSP"
        })
        {
            StringAssert.Contains(content, changeClass);
        }
    }

    [TestMethod]
    public void deferred_findings_register_contains_f3_f5_f6_f7_f9_as_non_blocking_no_runtime_unlock()
    {
        var content = ReadAll(DeferredPath);

        foreach (var id in new[] { "\"id\": \"F3\"", "\"id\": \"F5\"", "\"id\": \"F6\"", "\"id\": \"F7\"", "\"id\": \"F9\"" })
            StringAssert.Contains(content, id);

        StringAssert.Contains(content, "\"blockingStatus\": false");
        StringAssert.Contains(content, "\"freezeImpact\": \"none\"");
        StringAssert.Contains(content, "\"runtimeUnlocked\": false");
        StringAssert.Contains(content, "\"releaseStoreStatusChanged\": false");
    }

    [TestMethod]
    public void runtime_reentry_criteria_allow_simulated_planning_and_require_gates_for_real_requests()
    {
        var content = ReadAll(ReEntryCriteriaPath);

        foreach (var reentryClass in new[]
        {
            "SIMULATED_ONLY_CONTINUATION",
            "LOCAL_DRY_RUN_PLANNING",
            "LOCAL_PROVIDER_PLANNING",
            "FILESYSTEM_READ_ONLY_PLANNING",
            "UI_READ_ONLY_SURFACING_PLANNING",
            "PRODUCTIVE_RUNTIME_REQUEST",
            "PROVIDER_CLOUD_REQUEST",
            "FILESYSTEM_WRITE_REQUEST",
            "BROWSER_AUTOMATION_REQUEST",
            "CAPABILITY_UNLOCK_REQUEST",
            "RELEASE_STORE_REQUEST",
            "PRODUCT_BRIDGE_CSP_REQUEST"
        })
        {
            StringAssert.Contains(content, reentryClass);
        }

        StringAssert.Contains(content, "requires new safety gate");
        StringAssert.Contains(content, "requires provider audit");
        StringAssert.Contains(content, "requires path jail");
        StringAssert.Contains(content, "requires separate browser safety gate");
        StringAssert.Contains(content, "requires explicit manual owner gate");
        StringAssert.Contains(content, "requires separate product gate");
    }

    [TestMethod]
    public void reentry_gate_matrix_maps_allowed_conditional_gated_owner_audit_and_blocked_decisions()
    {
        var content = ReadAll(ReEntryMatrixPath);

        foreach (var decision in new[]
        {
            "REENTRY_ALLOWED_SIMULATED_ONLY",
            "REENTRY_CONDITIONAL_PLANNING_ONLY",
            "REENTRY_REQUIRES_NEW_SAFETY_GATE",
            "REENTRY_REQUIRES_OWNER_APPROVAL",
            "REENTRY_REQUIRES_EXTERNAL_AUDIT",
            "REENTRY_BLOCKED_NO_GO"
        })
        {
            StringAssert.Contains(content, decision);
        }

        StringAssert.Contains(content, "\"class\": \"RELEASE_STORE_REQUEST\", \"decision\": \"REENTRY_BLOCKED_NO_GO\"");
        StringAssert.Contains(content, "\"class\": \"PRODUCT_BRIDGE_CSP_REQUEST\", \"decision\": \"REENTRY_BLOCKED_NO_GO\"");
    }

    [TestMethod]
    public void reentry_risk_register_covers_expected_transition_risks()
    {
        var content = ReadAll(ReEntryRiskPath);

        foreach (var risk in new[]
        {
            "simulated freeze misread as runtime approval",
            "planning-only work becomes implementation",
            "provider/cloud accidentally wired",
            "secrets leak through provider setup",
            "filesystem read-only drifts to write",
            "browser automation bypasses HIL",
            "capability unlock bypasses owner gate",
            "product files touched without product gate",
            "Bridge/CSP changed without CSP audit",
            "release/store accidentally reopened",
            "tests assume no-execution but runtime executes",
            "redaction regression under real provider"
        })
        {
            StringAssert.Contains(content, risk);
        }
    }

    [TestMethod]
    public void go_no_go_records_freeze_lock_ready_without_productive_release_or_product_bridge_claims()
    {
        var content = ReadAll(GoNoGoPath);

        StringAssert.Contains(content, "\"decision\": \"SIMULATED_RUNTIME_FOUNDATION_FREEZE_LOCK_READY\"");
        StringAssert.Contains(content, "\"simulatedRuntimeFoundation\": \"LOCKED_TEST_ONLY_BASELINE\"");
        StringAssert.Contains(content, "\"productiveRuntimeUnlock\": \"0%\"");
        StringAssert.Contains(content, "\"providerCloudLiveCalls\": \"0%\"");
        StringAssert.Contains(content, "\"filesystemBrowserCapabilityUnlock\": \"0%\"");
        StringAssert.Contains(content, "\"publicRelease\": \"0% / NO-GO\"");
        StringAssert.Contains(content, "\"chromeWebStore\": \"0% / NO-GO\"");
        StringAssert.Contains(content, "\"signedPublicZipCreated\": false");
        StringAssert.Contains(content, "\"productFilesModified\": false");
        StringAssert.Contains(content, "\"bridgeCspModified\": false");
    }

    private static readonly string[] NoGoFalseFields =
    [
        "\"productiveRuntime\": false",
        "\"providerCloud\": false",
        "\"filesystemWrite\": false",
        "\"browserAutomation\": false",
        "\"capabilityUnlock\": false",
        "\"publicRelease\": false",
        "\"chromeWebStore\": false",
        "\"signedPublicZip\": false",
        "\"productFilesModified\": false",
        "\"bridgeCspModified\": false"
    ];
}
