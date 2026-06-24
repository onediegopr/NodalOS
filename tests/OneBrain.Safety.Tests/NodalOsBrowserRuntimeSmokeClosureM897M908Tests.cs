using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("BrowserRuntimeSmokeClosure")]
[TestCategory("M897")]
[TestCategory("M898")]
[TestCategory("M899")]
[TestCategory("M900")]
[TestCategory("M901")]
[TestCategory("M902")]
[TestCategory("M903")]
[TestCategory("M904")]
[TestCategory("M905")]
[TestCategory("M906")]
[TestCategory("M907")]
[TestCategory("M908")]
[TestCategory("M897M908")]
public sealed class NodalOsBrowserRuntimeSmokeClosureM897M908Tests
{
    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(Environment.CurrentDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "OneBrain.slnx")))
            dir = dir.Parent;

        return dir?.FullName ?? Environment.CurrentDirectory;
    }

    private static string ReadAll(string relativePath) =>
        File.ReadAllText(Path.Combine(RepoRoot(), relativePath));

    private static JsonDocument ReadJson(string relativePath) =>
        JsonDocument.Parse(ReadAll(relativePath));

    [TestMethod]
    public void cleanup_root_cause_report_created_and_classifies_external_test_infra()
    {
        using var doc = ReadJson("artifacts/agent-operations/m897/cleanup-root-cause-evidence.json");
        var root = doc.RootElement;

        Assert.AreEqual("BROWSER_RUNTIME_SMOKE_CLEANUP_ROOT_CAUSE", root.GetProperty("evidenceType").GetString());
        Assert.AreEqual("%TEMP%/onebrain-cdp-*", root.GetProperty("observedPattern").GetString());
        Assert.AreEqual("EXTERNAL_TEMP_CDP_PROFILE_CLEANUP_RACE_OR_IO_LOCK", root.GetProperty("rootCauseClassification").GetString());
        Assert.IsTrue(root.GetProperty("diagnosedFactors").EnumerateArray().Any(x => x.GetString() == "temp directory residual pattern"));
        Assert.IsTrue(root.GetProperty("externalVsTestInfra").GetString()!.Contains("external cleanup timing", StringComparison.Ordinal));
        Assert.IsFalse(root.GetProperty("productRuntimeUnlock").GetBoolean());
        Assert.IsFalse(root.GetProperty("bridgeCspModification").GetBoolean());
    }

    [TestMethod]
    public void cleanup_stabilization_preserves_visibility_and_does_not_disable_smoke()
    {
        using var doc = ReadJson("artifacts/agent-operations/m898/cleanup-stabilization-attempt.json");
        var root = doc.RootElement;

        Assert.AreEqual("TEST_ONLY_READY_WITH_VISIBLE_QUARANTINE", root.GetProperty("status").GetString());
        Assert.IsTrue(root.GetProperty("cleanupEventualWait").GetBoolean());
        Assert.IsTrue(root.GetProperty("disposeFinallyPreserved").GetBoolean());
        Assert.IsTrue(root.GetProperty("testVisibilityPreserved").GetBoolean());
        Assert.IsFalse(root.GetProperty("smokeDisabled").GetBoolean());
        Assert.IsFalse(root.GetProperty("failureHidden").GetBoolean());
        Assert.IsFalse(root.GetProperty("productTouched").GetBoolean());
        Assert.IsFalse(root.GetProperty("bridgeCspTouched").GetBoolean());
    }

    [TestMethod]
    public void cleanup_quarantine_policy_keeps_caveat_visible_and_rejects_hidden_pass_claim()
    {
        using var doc = ReadJson("artifacts/agent-operations/m899/cleanup-quarantine-policy.json");
        var root = doc.RootElement;

        Assert.IsTrue(root.GetProperty("externalCleanupMayBeQuarantined").GetBoolean());
        Assert.IsTrue(root.GetProperty("quarantineVisible").GetBoolean());
        Assert.IsFalse(root.GetProperty("quarantineEqualsCleanPass").GetBoolean());
        Assert.IsFalse(root.GetProperty("twentyOfTwentyCleanClaimAllowedWhenSkippedOrInconclusive").GetBoolean());
        Assert.IsTrue(root.GetProperty("foundationMayProceedWhenSafetyPasses").GetBoolean());
        Assert.AreEqual("95%", root.GetProperty("fullSuiteConfidenceWithCaveat").GetString());
        Assert.IsFalse(root.GetProperty("hiddenFailureAllowed").GetBoolean());
        Assert.IsFalse(root.GetProperty("runtimeUnlock").GetBoolean());
    }

    [TestMethod]
    public void caveat_ledger_records_historical_and_current_external_smoke_caveats()
    {
        using var doc = ReadJson("artifacts/agent-operations/m900/freeze-baseline-caveat-ledger.json");
        var root = doc.RootElement;
        var caveats = root.GetProperty("caveats").EnumerateArray().ToArray();

        Assert.IsTrue(caveats.Any(x => x.GetProperty("id").GetString() == "GATE1_CDP_READINESS_TRANSIENT"));
        Assert.IsTrue(caveats.Any(x => x.GetProperty("id").GetString() == "GATE9_WEBSOCKET_IDEMPOTENCY_TRANSIENT"));
        Assert.IsTrue(caveats.Any(x => x.GetProperty("id").GetString() == "BROWSER_RUNTIME_SMOKE_CLEANUP_EXTERNAL_QUARANTINED"));
        Assert.IsTrue(caveats.Any(x => x.GetProperty("id").GetString() == "TEMP_IO_DISK_FULL_TRANSIENT"));
        Assert.AreEqual("TEST_ONLY_READY", root.GetProperty("simulatedFoundationSafety").GetString());
        Assert.AreEqual("DISABLED", root.GetProperty("runtimeProductiveExecution").GetString());
        Assert.AreEqual("NO-GO", root.GetProperty("releaseStore").GetString());
    }

    [TestMethod]
    public void cleanliness_matrix_marks_current_external_caveat_and_rejects_false_full_clean()
    {
        using var doc = ReadJson("artifacts/agent-operations/m901/freeze-baseline-cleanliness-matrix.json");
        var root = doc.RootElement;

        Assert.AreEqual("CONDITIONAL_EXTERNAL_SMOKE", root.GetProperty("currentStatus").GetString());
        Assert.IsTrue(root.GetProperty("falseFullCleanClaimRejected").GetBoolean());
        Assert.AreEqual("NO_GO_SAFETY", root.GetProperty("safetyDriftResult").GetString());
        Assert.AreEqual("NO_GO_SCOPE", root.GetProperty("scopeDriftResult").GetString());
        Assert.AreEqual("NO_GO_RELEASE", root.GetProperty("releaseDriftResult").GetString());
        Assert.IsFalse(root.GetProperty("productFilesModified").GetBoolean());
        Assert.IsFalse(root.GetProperty("bridgeCspModified").GetBoolean());
    }

    [TestMethod]
    public void freeze_baseline_closure_allows_m909_only_with_visible_external_caveat()
    {
        using var doc = ReadJson("artifacts/agent-operations/m902/freeze-baseline-hardening-closure.json");
        var root = doc.RootElement;

        Assert.AreEqual("TEST_ONLY_READY_WITH_EXTERNAL_SMOKE_CAVEAT", root.GetProperty("status").GetString());
        Assert.AreEqual("OPEN_BROWSER_RUNTIME_SMOKE_CLEANUP_EXTERNAL_QUARANTINED_VISIBLE", root.GetProperty("caveatStatus").GetString());
        Assert.AreEqual("PASS_WITH_VISIBLE_INCONCLUSIVE_CLEANUP_CAVEAT", root.GetProperty("browserRuntimeSmokeStatus").GetString());
        Assert.AreEqual("TEST_ONLY_READY", root.GetProperty("simulatedFoundationStatus").GetString());
        Assert.IsTrue(root.GetProperty("m909M920CanStart").GetBoolean());
        Assert.AreEqual("READY_WITH_EXTERNAL_SMOKE_CAVEAT", root.GetProperty("m909M920StartCondition").GetString());
        Assert.IsFalse(root.GetProperty("productFilesModified").GetBoolean());
        Assert.IsFalse(root.GetProperty("bridgeCspModified").GetBoolean());
    }

    [TestMethod]
    public void browser_claim_intake_parking_marks_feature_not_implemented_and_out_of_scope_unlocks()
    {
        using var doc = ReadJson("artifacts/agent-operations/m903/browser-claim-feature-intake-parking.json");
        var root = doc.RootElement;

        Assert.IsFalse(root.GetProperty("implementedInM897M908").GetBoolean());
        Assert.AreEqual("permissive observational routing-oriented", root.GetProperty("scope").GetString());
        Assert.IsFalse(root.GetProperty("authorizationOriented").GetBoolean());
        Assert.IsFalse(root.GetProperty("perActionApproval").GetBoolean());
        Assert.IsFalse(root.GetProperty("blockingUx").GetBoolean());
        Assert.IsTrue(root.GetProperty("outOfScopeFeatures").EnumerateArray().Any(x => x.GetString() == "Browser Injection Shield"));
        Assert.IsFalse(root.GetProperty("providerCloudUnlock").GetBoolean());
        Assert.IsFalse(root.GetProperty("browserAutomationUnlock").GetBoolean());
        Assert.IsTrue(root.GetProperty("productBridgeCspRequiresSeparateGate").GetBoolean());
    }

    [TestMethod]
    public void m909_scope_boundary_draft_is_permissive_and_has_no_unlocks()
    {
        using var doc = ReadJson("artifacts/agent-operations/m904/m909-m920-scope-boundary-draft.json");
        var root = doc.RootElement;

        Assert.IsTrue(root.GetProperty("scopeItems").EnumerateArray().Any(x => x.GetString() == "Browser Capability Map permissive"));
        Assert.IsTrue(root.GetProperty("permissive").GetBoolean());
        Assert.IsFalse(root.GetProperty("approvalPrompts").GetBoolean());
        Assert.IsFalse(root.GetProperty("blockingFlow").GetBoolean());
        Assert.IsFalse(root.GetProperty("unlocks").GetBoolean());
        Assert.IsFalse(root.GetProperty("implementedNow").GetBoolean());
    }

    [TestMethod]
    public void m909_readiness_gate_ready_with_external_smoke_caveat_and_blocks_drift_options()
    {
        using var doc = ReadJson("artifacts/agent-operations/m905/m909-m920-readiness-gate.json");
        var root = doc.RootElement;
        var options = root.GetProperty("decisionOptions").EnumerateArray().Select(x => x.GetString()).ToArray();

        Assert.AreEqual("M909_READY_WITH_EXTERNAL_SMOKE_CAVEAT", root.GetProperty("decision").GetString());
        CollectionAssert.Contains(options, "M909_BLOCKED_BY_SAFETY");
        CollectionAssert.Contains(options, "M909_BLOCKED_BY_SCOPE");
        CollectionAssert.Contains(options, "M909_BLOCKED_BY_PRODUCT_BRIDGE_CSP");
        CollectionAssert.Contains(options, "M909_BLOCKED_BY_RELEASE_DRIFT");
        Assert.IsTrue(root.GetProperty("productBridgeCspUnchanged").GetBoolean());
        Assert.IsFalse(root.GetProperty("runtimeUnlock").GetBoolean());
        Assert.AreEqual("NO-GO", root.GetProperty("releaseStore").GetString());
        Assert.IsTrue(root.GetProperty("featureParkedAndScoped").GetBoolean());
    }

    [TestMethod]
    public void final_go_no_go_preserves_no_go_boundaries()
    {
        using var doc = ReadJson("artifacts/agent-operations/m897-m908/browser-runtime-smoke-freeze-baseline-go-no-go.json");
        var root = doc.RootElement;

        Assert.AreEqual("VISIBLE_EXTERNAL_QUARANTINE", root.GetProperty("browserRuntimeSmokeStabilization").GetString());
        Assert.AreEqual("TEST_ONLY_READY_WITH_EXTERNAL_SMOKE_CAVEAT", root.GetProperty("freezeBaselineHardeningClosure").GetString());
        Assert.AreEqual("TEST_ONLY_READY", root.GetProperty("m909M920IntakeParking").GetString());
        Assert.AreEqual("M909_READY_WITH_EXTERNAL_SMOKE_CAVEAT", root.GetProperty("m909M920Readiness").GetString());
        Assert.AreEqual("0%", root.GetProperty("productiveRuntimeUnlock").GetString());
        Assert.AreEqual("0%", root.GetProperty("providerCloudLiveCalls").GetString());
        Assert.AreEqual("0% / NO-GO", root.GetProperty("publicRelease").GetString());
        Assert.IsFalse(root.GetProperty("productFilesModified").GetBoolean());
        Assert.IsFalse(root.GetProperty("bridgeCspModified").GetBoolean());
    }
}
