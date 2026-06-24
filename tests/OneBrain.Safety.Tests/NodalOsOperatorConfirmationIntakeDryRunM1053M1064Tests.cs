using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("OperatorConfirmationIntakeDryRun")]
[TestCategory("M1053")]
[TestCategory("M1054")]
[TestCategory("M1055")]
[TestCategory("M1056")]
[TestCategory("M1057")]
[TestCategory("M1058")]
[TestCategory("M1059")]
[TestCategory("M1060")]
[TestCategory("M1061")]
[TestCategory("M1062")]
[TestCategory("M1063")]
[TestCategory("M1064")]
[TestCategory("M1053M1064")]
public sealed class NodalOsOperatorConfirmationIntakeDryRunM1053M1064Tests
{
    private const string IntakePath = "artifacts/agent-operations/m1053/operator-confirmation-intake.json";
    private const string RoutePath = "artifacts/agent-operations/m1054/confirmation-route-enforcement.json";
    private const string AcknowledgementPath = "artifacts/agent-operations/m1055/operator-acknowledgement-dry-run-contract.json";
    private const string PacketPath = "artifacts/agent-operations/m1056/safe-evidence-capture-dry-run-packet.json";
    private const string CatalogPath = "artifacts/agent-operations/m1057/evidence-capture-item-catalog.json";
    private const string RedactionPath = "artifacts/agent-operations/m1058/redaction-precheck-dry-run.json";
    private const string AbortPath = "artifacts/agent-operations/m1059/abort-confirmation-dry-run.json";
    private const string NoGoPath = "artifacts/agent-operations/m1060/no-go-locks-reaffirmation.json";
    private const string CaveatPath = "artifacts/agent-operations/m1061/browser-runtime-smoke-caveat-continuity-update.json";
    private const string NextPath = "artifacts/agent-operations/m1062/next-path-recommendation.json";
    private const string FinalReportArtifactPath = "artifacts/agent-operations/m1063/final-report.json";
    private const string FinalArtifactPath = "artifacts/agent-operations/m1064/final-artifacts-validations.json";
    private const string GoNoGoPath = "artifacts/agent-operations/m1053-m1064/operator-confirmation-intake-dry-run-go-no-go.json";
    private const string ReportPath = "docs/reports/m1064-operator-confirmation-intake-safe-evidence-dry-run-packet.md";

    [TestMethod]
    public void missing_confirmation_keeps_protocol_hardening_only()
    {
        using var intake = ReadJson(IntakePath);

        Assert.AreEqual("OPERATOR_CONFIRMATION_GRANTED_FOR_SAFE_EVIDENCE_CAPTURE_ONLY", String(intake, "confirmation_phrase_required"));
        Assert.IsFalse(intake.RootElement.GetProperty("confirmation_phrase_detected").GetBoolean());
        Assert.AreEqual("OPERATOR_CONFIRMATION_PENDING", String(intake, "confirmation_status"));
        Assert.AreEqual("PROTOCOL_HARDENING_ONLY", String(intake, "route"));
        Assert.AreEqual("NO-GO", String(intake, "manual_qa_execution_status"));
        Assert.AreEqual("NOT_STARTED", String(intake, "safe_evidence_capture_status"));
    }

    [TestMethod]
    public void exact_confirmation_route_is_defined_but_not_selected()
    {
        using var route = ReadJson(RoutePath);

        Assert.AreEqual("PROTOCOL_HARDENING_ONLY", String(route, "route"));
        Assert.AreEqual("PREPARED_FOR_FUTURE_ONLY", String(route, "dry_run_packet_status"));
        Assert.AreEqual("NO-GO", String(route, "manual_qa_execution"));
        Assert.AreEqual("SAFE_EVIDENCE_CHECKLIST_PREP_ONLY", String(route.RootElement.GetProperty("routes"), "when_exact_confirmation"));
        CollectionAssert.Contains(Array(route, "never_allow"), "MANUAL_QA_PASSED");
        CollectionAssert.Contains(Array(route, "never_allow"), "RUNTIME_READY");
        CollectionAssert.Contains(Array(route, "never_allow"), "PC_COMMANDER_READY");
        CollectionAssert.Contains(Array(route, "never_allow"), "RELEASE_READY");
    }

    [TestMethod]
    public void ambiguous_confirmation_is_rejected()
    {
        using var route = ReadJson(RoutePath);

        CollectionAssert.Contains(Array(route, "ambiguous_inputs_rejected"), "operator approved");
        CollectionAssert.Contains(Array(route, "ambiguous_inputs_rejected"), "safe to proceed");
        CollectionAssert.Contains(Array(route, "ambiguous_inputs_rejected"), "manual QA ok");
        Assert.AreEqual("OPERATOR_CONFIRMATION_AMBIGUOUS_NO_GO", String(route, "ambiguous_status"));
    }

    [TestMethod]
    public void operator_acknowledgement_contract_requires_no_go_and_caveat_acknowledgements()
    {
        using var doc = ReadJson(AcknowledgementPath);
        var required = Array(doc, "required_acknowledgements");

        CollectionAssert.Contains(required, "no manual QA execution");
        CollectionAssert.Contains(required, "no shell");
        CollectionAssert.Contains(required, "no filesystem write");
        CollectionAssert.Contains(required, "no browser automation productiva");
        CollectionAssert.Contains(required, "no provider/cloud");
        CollectionAssert.Contains(required, "no credentials capture");
        CollectionAssert.Contains(required, "no product file changes");
        CollectionAssert.Contains(required, "no Bridge/CSP changes");
        CollectionAssert.Contains(required, "BrowserRuntimeSmoke caveat visible");
        CollectionAssert.Contains(required, "no full-suite clean claim");
        CollectionAssert.Contains(required, "abort matrix active");
    }

    [TestMethod]
    public void dry_run_packet_has_no_execution_and_no_manual_qa_passed()
    {
        using var doc = ReadJson(PacketPath);

        Assert.AreEqual("PREPARED_FOR_FUTURE_ONLY", String(doc, "dry_run_packet_status"));
        Assert.AreEqual("PROTOCOL_HARDENING_ONLY", String(doc, "route"));
        Assert.IsFalse(doc.RootElement.GetProperty("execution_started").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("manual_qa_passed").GetBoolean());
        CollectionAssert.Contains(Array(doc, "planned_evidence_items"), "branch/head/worktree summary");
        CollectionAssert.Contains(Array(doc, "forbidden_actions"), "runtime real");
        CollectionAssert.Contains(Array(doc, "forbidden_actions"), "Bridge/CSP modification");
    }

    [TestMethod]
    public void evidence_catalog_forbids_secrets_raw_logs_provider_and_browser_productive_outputs()
    {
        using var doc = ReadJson(CatalogPath);
        var forbidden = Array(doc, "forbidden_items");

        CollectionAssert.Contains(Array(doc, "allowed_future_items"), "BrowserRuntimeSmoke caveat summary");
        CollectionAssert.Contains(Array(doc, "allowed_future_items"), "trace/evidence refs summary");
        CollectionAssert.Contains(forbidden, "raw logs extensive");
        CollectionAssert.Contains(forbidden, "secret values");
        CollectionAssert.Contains(forbidden, "credentials");
        CollectionAssert.Contains(forbidden, "provider/cloud outputs");
        CollectionAssert.Contains(forbidden, "browser automation productive outputs");
    }

    [TestMethod]
    public void redaction_precheck_rejects_fake_secret_leaks_and_raw_logs()
    {
        using var doc = ReadJson(RedactionPath);

        CollectionAssert.Contains(Array(doc, "classification_options"), "SAFE_METADATA_ONLY");
        CollectionAssert.Contains(Array(doc, "classification_options"), "REJECT_SECRET_LEAK");
        CollectionAssert.Contains(Array(doc, "classification_options"), "REJECT_RAW_LOG");
        CollectionAssert.Contains(Array(doc, "rejected_examples"), "fake-token-shaped-value");
        CollectionAssert.Contains(Array(doc, "rejected_examples"), "large raw log dump");
        Assert.IsFalse(doc.RootElement.GetProperty("captures_real_evidence").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("stores_secrets").GetBoolean());
    }

    [TestMethod]
    public void abort_confirmation_detects_missing_confirmation_and_scope_drift()
    {
        using var doc = ReadJson(AbortPath);
        var triggers = Array(doc, "abort_triggers");

        CollectionAssert.Contains(triggers, "missing explicit confirmation for capture route");
        CollectionAssert.Contains(triggers, "wrong branch");
        CollectionAssert.Contains(triggers, "wrong worktree");
        CollectionAssert.Contains(triggers, "dirty worktree");
        CollectionAssert.Contains(triggers, "product files changed");
        CollectionAssert.Contains(triggers, "Bridge/CSP changed");
        CollectionAssert.Contains(triggers, "BrowserRuntimeSmoke caveat hidden");
        CollectionAssert.Contains(triggers, "runtime real claim");
        CollectionAssert.Contains(triggers, "release/store claim");
    }

    [TestMethod]
    public void no_go_locks_preserve_runtime_pc_commander_provider_release_boundaries()
    {
        using var doc = ReadJson(NoGoPath);

        Assert.AreEqual("NO-GO", String(doc, "manual_qa_execution"));
        Assert.AreEqual("NOT_READY_EVIDENCE_PENDING", String(doc, "manual_qa_trigger"));
        Assert.AreEqual("NO-GO", String(doc, "runtime_real"));
        Assert.AreEqual("NO-GO", String(doc, "pc_commander_real"));
        Assert.AreEqual("NO-GO", String(doc, "provider_cloud"));
        Assert.AreEqual("NO-GO", String(doc, "filesystem_browser_capability_unlock"));
        Assert.AreEqual("NO-GO", String(doc, "release_store"));
        Assert.IsFalse(doc.RootElement.GetProperty("product_files_modified").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("bridge_csp_modified").GetBoolean());
    }

    [TestMethod]
    public void browser_runtime_smoke_caveat_blocks_hundred_percent_and_release_claims()
    {
        using var doc = ReadJson(CaveatPath);

        Assert.IsTrue(doc.RootElement.GetProperty("browser_runtime_smoke_caveat_visible").GetBoolean());
        Assert.AreEqual("29 passed / 1 skipped-inconclusive / 0 failed", String(doc, "latest_observed_status"));
        Assert.AreEqual("95%", String(doc, "full_suite_confidence"));
        Assert.IsFalse(doc.RootElement.GetProperty("hundred_percent_confidence_claim_allowed").GetBoolean());
        Assert.IsTrue(doc.RootElement.GetProperty("blocks_release_store_public_claims").GetBoolean());
    }

    [TestMethod]
    public void next_path_recommendation_depends_on_confirmation_status()
    {
        using var doc = ReadJson(NextPath);

        StringAssert.Contains(String(doc, "if_operator_confirmation_pending"), "Operator Confirmation Pending Protocol Hardening");
        StringAssert.Contains(String(doc, "if_operator_confirmation_granted"), "Safe Evidence Capture Operator Review Packet");
        StringAssert.Contains(String(doc, "if_browser_runtime_smoke_deteriorates"), "BrowserRuntimeSmoke Caveat Isolation");
        Assert.AreEqual("OPERATOR_CONFIRMATION_PENDING", String(doc, "current_confirmation_status"));
    }

    [TestMethod]
    public void final_artifacts_exist_and_preserve_no_go_statuses()
    {
        foreach (var path in AllPaths())
        {
            Assert.IsTrue(File.Exists(FullPath(path)), path);
        }

        using var final = ReadJson(FinalArtifactPath);
        Assert.AreEqual("OPERATOR_CONFIRMATION_INTAKE_PROTOCOL_HARDENING_READY_WITH_EXTERNAL_SMOKE_CAVEAT", String(final, "decision"));
        Assert.AreEqual("PROTOCOL_HARDENING_ONLY", String(final, "route_taken"));
        Assert.AreEqual("OPERATOR_CONFIRMATION_PENDING", String(final, "confirmation_status"));
        Assert.AreEqual("NOT_STARTED", String(final, "safe_evidence_capture_status"));
        Assert.AreEqual("NO-GO", String(final, "manual_qa_execution"));
        Assert.AreEqual("NO-GO", String(final, "runtime_real"));
        Assert.AreEqual("NO-GO", String(final, "pc_commander_real"));
        Assert.IsFalse(final.RootElement.GetProperty("product_files_modified").GetBoolean());
        Assert.IsFalse(final.RootElement.GetProperty("bridge_csp_modified").GetBoolean());
    }

    [TestMethod]
    public void artifacts_contain_no_secret_leaks_or_forbidden_clean_claims()
    {
        foreach (var file in AllPaths())
        {
            var content = File.ReadAllText(FullPath(file));
            foreach (var forbidden in new[] { "sk-", "AKIA", "BEGIN PRIVATE KEY", "Authorization: Bearer", "full suite clean", "100% confidence" })
            {
                Assert.IsFalse(content.Contains(forbidden, StringComparison.Ordinal), $"{file} contains forbidden wording {forbidden}");
            }
        }
    }

    private static string[] AllPaths() =>
    [
        IntakePath, RoutePath, AcknowledgementPath, PacketPath, CatalogPath, RedactionPath, AbortPath,
        NoGoPath, CaveatPath, NextPath, FinalReportArtifactPath, FinalArtifactPath, GoNoGoPath, ReportPath
    ];

    private static string FullPath(string relativePath) => Path.Combine(RepoRoot(), relativePath);

    private static JsonDocument ReadJson(string relativePath) => JsonDocument.Parse(File.ReadAllText(FullPath(relativePath)));

    private static string String(JsonDocument doc, string property) => doc.RootElement.GetProperty(property).GetString() ?? string.Empty;

    private static string String(JsonElement element, string property) => element.GetProperty(property).GetString() ?? string.Empty;

    private static string[] Array(JsonDocument doc, string property) => Array(doc.RootElement, property);

    private static string[] Array(JsonElement element, string property) => element.GetProperty(property).EnumerateArray().Select(item => item.GetString() ?? string.Empty).ToArray();

    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(Environment.CurrentDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "OneBrain.slnx")))
        {
            dir = dir.Parent;
        }

        return dir?.FullName ?? Environment.CurrentDirectory;
    }
}
