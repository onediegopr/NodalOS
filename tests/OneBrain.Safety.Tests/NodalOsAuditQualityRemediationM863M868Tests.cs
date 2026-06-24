using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.Safety.Tests.SimulatedRuntime;

namespace OneBrain.Safety.Tests;

// Audit quality remediation (Claude external audit findings F1 + F2).
// F1: no-execution proof must be measured from the real RecordingSideEffectSink,
//     not a hardcoded constant, and must FAIL when a side effect is recorded.
// F2: redaction must be adversarial — inject FAKE_* secret-like values and prove
//     the export does not contain them and the proof detects/redacts.
[TestClass]
[TestCategory("AuditQualityRemediation")]
[TestCategory("MeasuredNoExecution")]
[TestCategory("AdversarialRedaction")]
[TestCategory("M863M868")]
public sealed class NodalOsAuditQualityRemediationM863M868Tests
{
    // FAKE-only, explicitly non-real sensitive values.
    private const string FakeSecret = "FAKE_SECRET_NODAL_OS_TEST_TOKEN_123";
    private const string FakeProviderKey = "FAKE_PROVIDER_KEY_SHOULD_BE_REDACTED";
    private const string FakeCookie = "FAKE_COOKIE_SESSION_SHOULD_BE_REDACTED";
    private const string FakePrivateKey = "FAKE_PRIVATE_KEY_SHOULD_BE_REDACTED";
    private const string FakeBrowserSession = "FAKE_BROWSER_SESSION_DATA_SHOULD_BE_REDACTED";

    private static SimulatedDryRunOrchestrator Orchestrator(RecordingSideEffectSink sink) => new(sink);

    private static SimulatedRequest CleanRequest(string capability) =>
        new(SimulatedDryRunOrchestrator.RequiredMode, SimulatedDryRunOrchestrator.RequiredFixtureType, capability, IsProhibitedAction: false);

    // ===================== M863 — Measured no-execution proof =====================

    [TestMethod]
    public void no_execution_proof_reads_real_recording_sink_invocation_count()
    {
        var sink = new RecordingSideEffectSink();
        var result = Orchestrator(sink).Process(CleanRequest("local_provider_model"));
        Assert.AreEqual(sink.InvocationCount, result.Proof.SideEffectSinkInvocations);
        Assert.AreEqual(0, result.Proof.SideEffectSinkInvocations);
        Assert.IsTrue(result.Proof.IsClean);
    }

    [TestMethod]
    public void no_execution_proof_fails_when_sink_records_side_effect()
    {
        var sink = new RecordingSideEffectSink();
        Orchestrator(sink).Process(CleanRequest("local_provider_model"));
        // Adversarially record a side effect on the SAME sink, then re-derive.
        sink.InvokeRealExecutor();
        var measured = NoExecutionProof.FromSink(sink);
        Assert.AreEqual(1, measured.SideEffectSinkInvocations);
        Assert.IsTrue(measured.RealExecutorInvoked);
        Assert.IsTrue(measured.ActualExecutionPerformed);
        Assert.IsFalse(measured.IsClean, "proof must FAIL when a side effect is recorded");
    }

    [TestMethod]
    public void allowed_simulated_route_has_measured_sink_zero()
    {
        var sink = new RecordingSideEffectSink();
        var route = new SimulatedCapabilityRouter().Route("local_provider_model", sink);
        Assert.AreEqual(SimulatedDecision.AllowSimulatedDryRun, route.Decision);
        Assert.AreEqual(sink.InvocationCount, route.NoExecutionProof.SideEffectSinkInvocations);
        Assert.AreEqual(0, route.NoExecutionProof.SideEffectSinkInvocations);
    }

    [TestMethod]
    public void denied_route_has_measured_sink_zero()
    {
        var sink = new RecordingSideEffectSink();
        var route = new SimulatedCapabilityRouter().Route("provider_cloud_live_call", sink);
        Assert.AreEqual(SimulatedDecision.Deny, route.Decision);
        Assert.IsNull(route.SelectedExecutor);
        Assert.AreEqual(sink.InvocationCount, route.NoExecutionProof.SideEffectSinkInvocations);
        Assert.AreEqual(0, route.NoExecutionProof.SideEffectSinkInvocations);
    }

    [TestMethod]
    public void unsupported_route_has_measured_sink_zero()
    {
        var sink = new RecordingSideEffectSink();
        var route = new SimulatedCapabilityRouter().Route("unknown_future_capability", sink);
        Assert.AreEqual(SimulatedDecision.Deny, route.Decision);
        Assert.AreEqual(SimulatedPolicyDecisionType.DenyUnsupportedCapability, route.PolicyDecisionType);
        Assert.IsNull(route.SelectedExecutor);
        Assert.AreEqual(0, route.NoExecutionProof.SideEffectSinkInvocations);
    }

    [TestMethod]
    public void policy_violation_route_has_measured_sink_zero()
    {
        var sink = new RecordingSideEffectSink();
        var route = new SimulatedCapabilityRouter().Route(SimulatedRuntimeRoutingMatrix.PolicyViolationCapability, sink);
        Assert.AreEqual(SimulatedDecision.Deny, route.Decision);
        Assert.AreEqual(SimulatedPolicyDecisionType.DenyPolicyViolation, route.PolicyDecisionType);
        Assert.AreEqual(0, route.NoExecutionProof.SideEffectSinkInvocations);
    }

    [TestMethod]
    public void approval_granted_fake_only_has_measured_sink_zero()
    {
        var sink = new RecordingSideEffectSink();
        var outcome = new SimulatedManualApprovalBoundary().Decide("local_provider_model", SimulatedApprovalStatus.ApprovalGrantedSimulated, sink);
        Assert.AreEqual(SimulatedApprovalCapabilityClass.Allowed, outcome.CapabilityClass);
        Assert.AreEqual(sink.InvocationCount, outcome.NoExecutionProof.SideEffectSinkInvocations);
        Assert.IsTrue(outcome.NoExecutionProof.IsClean);
        Assert.IsFalse(outcome.ProductiveUnlockAllowed);
    }

    [TestMethod]
    public void replay_audit_only_has_measured_sink_zero()
    {
        var route = new SimulatedCapabilityRouter().Route("ledger_append");
        var summary = new SimulatedTimelineRoundtrip().Roundtrip(route);
        var sink = new RecordingSideEffectSink();
        var replay = new SimulatedReplayGuard().Evaluate(summary, sink);

        Assert.AreEqual(sink.InvocationCount, replay.SideEffectSinkInvocations);
        Assert.AreEqual(sink.InvocationCount, replay.NoExecutionProof.SideEffectSinkInvocations);
        Assert.AreEqual(0, replay.NoExecutionProof.SideEffectSinkInvocations);
        Assert.IsTrue(replay.NoExecutionProof.IsClean);
    }

    [TestMethod]
    public void export_readiness_has_measured_sink_zero()
    {
        var sink = new RecordingSideEffectSink();
        var export = new SimulatedRedactingExporter().ExportWithRawPayload("ctx-export", new RawAuditPayload("summary"), sink);

        Assert.AreEqual(sink.InvocationCount, export.NoExecutionProof.SideEffectSinkInvocations);
        Assert.AreEqual(0, export.NoExecutionProof.SideEffectSinkInvocations);
        Assert.IsTrue(export.NoExecutionProof.IsClean);
    }

    [TestMethod]
    public void approval_denied_has_measured_sink_zero()
    {
        var outcome = new SimulatedManualApprovalBoundary().Decide("local_provider_model", SimulatedApprovalStatus.ApprovalDeniedSimulated);
        Assert.IsNull(outcome.SelectedExecutor);
        Assert.IsFalse(outcome.CanExecute);
        Assert.AreEqual(0, outcome.NoExecutionProof.SideEffectSinkInvocations);
    }

    [TestMethod]
    public void approval_granted_cannot_override_denylist_and_has_measured_sink_zero()
    {
        var outcome = new SimulatedManualApprovalBoundary().Decide("provider_cloud_live_call", SimulatedApprovalStatus.ApprovalGrantedSimulated);
        Assert.AreEqual(SimulatedApprovalCapabilityClass.Denylisted, outcome.CapabilityClass);
        Assert.IsNull(outcome.SelectedExecutor);
        Assert.IsFalse(outcome.CanExecute);
        Assert.AreEqual(0, outcome.NoExecutionProof.SideEffectSinkInvocations);
    }

    [TestMethod]
    public void no_execution_proof_detects_injected_provider_invocation()
    {
        var sink = new RecordingSideEffectSink();
        new SimulatedCapabilityRouter().Route("local_provider_model", sink);
        sink.InvokeProviderClient();
        var measured = NoExecutionProof.FromSink(sink);
        Assert.IsTrue(measured.LiveCallPerformed);
        Assert.IsFalse(measured.IsClean);
    }

    [TestMethod]
    public void no_execution_proof_detects_injected_filesystem_write()
    {
        var sink = new RecordingSideEffectSink();
        sink.InvokeFilesystemWriter();
        var measured = NoExecutionProof.FromSink(sink);
        Assert.IsTrue(measured.FilesystemWritePerformed);
        Assert.IsFalse(measured.IsClean);
    }

    [TestMethod]
    public void no_execution_proof_detects_injected_browser_automation()
    {
        var sink = new RecordingSideEffectSink();
        sink.InvokeBrowserAutomation();
        var measured = NoExecutionProof.FromSink(sink);
        Assert.IsTrue(measured.BrowserAutomationPerformed);
        Assert.IsFalse(measured.IsClean);
    }

    [TestMethod]
    public void no_execution_proof_detects_injected_capability_unlock()
    {
        var sink = new RecordingSideEffectSink();
        sink.InvokeCapabilityUnlock();
        var measured = NoExecutionProof.FromSink(sink);
        Assert.IsTrue(measured.CapabilityUnlocked);
        Assert.IsFalse(measured.IsClean);
    }

    [TestMethod]
    public void no_execution_proof_detects_injected_release_store()
    {
        var releaseSink = new RecordingSideEffectSink();
        releaseSink.InvokePublicRelease();
        Assert.IsFalse(NoExecutionProof.FromSink(releaseSink).IsClean);

        var storeSink = new RecordingSideEffectSink();
        storeSink.InvokeStoreSubmission();
        Assert.IsFalse(NoExecutionProof.FromSink(storeSink).IsClean);

        var zipSink = new RecordingSideEffectSink();
        zipSink.CreateSignedZip();
        Assert.IsFalse(NoExecutionProof.FromSink(zipSink).IsClean);
    }

    [TestMethod]
    public void no_execution_proof_detects_injected_product_bridge_csp_modification()
    {
        var sink = new RecordingSideEffectSink();
        Assert.IsFalse(NoExecutionProof.FromSink(sink, productFilesModified: true).IsClean);
        Assert.IsFalse(NoExecutionProof.FromSink(sink, bridgeCspModified: true).IsClean);
    }

    // ===================== M864 — Replace tautological CleanProof assertions =====================

    [TestMethod]
    public void assert_clean_route_uses_real_sink_state()
    {
        var sink = new RecordingSideEffectSink();
        var route = new SimulatedCapabilityRouter().Route("ledger_append", sink);
        // The route proof equals what the real sink measures — not a constant.
        Assert.AreEqual(sink.InvocationCount, route.NoExecutionProof.SideEffectSinkInvocations);
        Assert.AreEqual(sink.RealExecutorInvoked, route.NoExecutionProof.RealExecutorInvoked);
        Assert.AreEqual(sink.ProviderClientInvoked, route.NoExecutionProof.ProviderClientInvoked);
    }

    [TestMethod]
    public void assert_clean_route_fails_on_injected_side_effect()
    {
        var sink = new RecordingSideEffectSink();
        var route = new SimulatedCapabilityRouter().Route("filesystem_read_metadata", sink);
        Assert.IsTrue(route.NoExecutionProof.IsClean);
        // Inject into the same sink the route used; a re-derived proof must fail.
        sink.InvokeProviderClient();
        Assert.IsFalse(NoExecutionProof.FromSink(sink).IsClean);
    }

    [TestMethod]
    public void routing_tests_use_measured_no_execution()
    {
        foreach (var cap in new[] { "local_provider_model", "filesystem_read_metadata", "ledger_append", "provider_cloud_live_call", "unknown_future_capability" })
        {
            var sink = new RecordingSideEffectSink();
            var route = new SimulatedCapabilityRouter().Route(cap, sink);
            Assert.AreEqual(sink.InvocationCount, route.NoExecutionProof.SideEffectSinkInvocations, cap);
        }
    }

    [TestMethod]
    public void approval_tests_use_measured_no_execution()
    {
        foreach (var status in new[] { SimulatedApprovalStatus.ApprovalGrantedSimulated, SimulatedApprovalStatus.ApprovalDeniedSimulated, SimulatedApprovalStatus.ApprovalExpiredSimulated, SimulatedApprovalStatus.ApprovalInvalidSimulated })
        {
            var outcome = new SimulatedManualApprovalBoundary().Decide("local_provider_model", status);
            Assert.AreEqual(0, outcome.NoExecutionProof.SideEffectSinkInvocations, status.ToString());
            Assert.IsTrue(outcome.NoExecutionProof.IsClean, status.ToString());
        }
    }

    [TestMethod]
    public void replay_tests_use_measured_no_execution()
    {
        var (_, _, proofRunOne) = RunMeasuredReplay();
        var (_, _, proofRunTwo) = RunMeasuredReplay();
        Assert.AreEqual(0, proofRunOne.SideEffectSinkInvocations);
        Assert.AreEqual(0, proofRunTwo.SideEffectSinkInvocations);
        Assert.IsTrue(proofRunOne.IsClean);
        Assert.IsTrue(proofRunTwo.IsClean);
    }

    [TestMethod]
    public void export_tests_use_measured_no_execution()
    {
        var export = new SimulatedRedactingExporter().ExportWithRawPayload("ctx-export", new RawAuditPayload("summary"));
        Assert.AreEqual(0, export.NoExecutionProof.SideEffectSinkInvocations);
        Assert.IsTrue(export.NoExecutionProof.IsClean);
    }

    [TestMethod]
    public void governance_tests_use_measured_no_execution()
    {
        // Governance aggregates routing/approval results whose proofs are now measured.
        var router = new SimulatedCapabilityRouter();
        foreach (var entry in SimulatedRuntimeRoutingMatrix.Entries)
        {
            var sink = new RecordingSideEffectSink();
            var route = router.Route(entry.CapabilityName, sink);
            Assert.AreEqual(sink.InvocationCount, route.NoExecutionProof.SideEffectSinkInvocations, entry.CapabilityName);
            Assert.AreEqual(0, route.NoExecutionProof.SideEffectSinkInvocations, entry.CapabilityName);
        }
    }

    [TestMethod]
    public void no_existing_safety_coverage_removed()
    {
        // Denylist-first, default-deny, and approval-no-override still hold post-remediation.
        var router = new SimulatedCapabilityRouter();
        foreach (var denied in SimulatedRuntimeRoutingMatrix.DenylistedCapabilities)
        {
            var route = router.Route(denied);
            Assert.AreEqual(SimulatedDecision.Deny, route.Decision, denied);
            Assert.IsNull(route.SelectedExecutor, denied);
        }
        Assert.AreEqual(SimulatedDecision.Deny, router.Route("totally_unknown").Decision);
    }

    // ===================== M865-M866 — Real adversarial redaction =====================

    private static RawAuditPayload DirtyPayload() => new(
        "audit export summary",
        Secret: FakeSecret,
        ProviderKey: FakeProviderKey,
        Cookie: FakeCookie,
        PrivateKey: FakePrivateKey,
        BrowserSessionData: FakeBrowserSession);

    [TestMethod]
    public void audit_export_injected_fake_secret_is_removed()
    {
        var export = new SimulatedRedactingExporter().ExportWithRawPayload("ctx", DirtyPayload());
        Assert.IsFalse(export.SerializedExport.Contains(FakeSecret, StringComparison.Ordinal));
    }

    [TestMethod]
    public void audit_export_injected_fake_provider_key_is_removed()
    {
        var export = new SimulatedRedactingExporter().ExportWithRawPayload("ctx", DirtyPayload());
        Assert.IsFalse(export.SerializedExport.Contains(FakeProviderKey, StringComparison.Ordinal));
    }

    [TestMethod]
    public void audit_export_injected_fake_cookie_is_removed()
    {
        var export = new SimulatedRedactingExporter().ExportWithRawPayload("ctx", DirtyPayload());
        Assert.IsFalse(export.SerializedExport.Contains(FakeCookie, StringComparison.Ordinal));
    }

    [TestMethod]
    public void audit_export_injected_fake_private_key_is_removed()
    {
        var export = new SimulatedRedactingExporter().ExportWithRawPayload("ctx", DirtyPayload());
        Assert.IsFalse(export.SerializedExport.Contains(FakePrivateKey, StringComparison.Ordinal));
    }

    [TestMethod]
    public void audit_export_injected_fake_browser_session_data_is_removed()
    {
        var export = new SimulatedRedactingExporter().ExportWithRawPayload("ctx", DirtyPayload());
        Assert.IsFalse(export.SerializedExport.Contains(FakeBrowserSession, StringComparison.Ordinal));
    }

    [TestMethod]
    public void redaction_proof_marks_secret_detected()
    {
        var result = new SimulatedRedactor().Redact(DirtyPayload());
        Assert.IsTrue(result.RawContainedSensitive);
        Assert.IsTrue(result.SecretDetected);
    }

    [TestMethod]
    public void redaction_proof_marks_secret_redacted()
    {
        var result = new SimulatedRedactor().Redact(DirtyPayload());
        Assert.IsTrue(result.Redacted);
        Assert.IsTrue(result.RedactedPayload.Contains(SimulatedRedactor.Marker, StringComparison.Ordinal));
    }

    [TestMethod]
    public void redaction_proof_does_not_claim_clean_without_scanning_payload()
    {
        var result = new SimulatedRedactor().Redact(DirtyPayload());
        // Output contains no forbidden values, but detection reflects the scanned input.
        Assert.IsTrue(result.SecretDetected);
        Assert.IsFalse(result.OutputContainsAnySensitiveValue(DirtyPayload().SensitiveValues));
        // The RedactionProof *Included flags reflect actual output (all false = clean output).
        var p = result.RedactionProof;
        Assert.IsFalse(p.SecretsIncluded || p.ProviderKeysIncluded || p.CookiesIncluded || p.PrivateKeysIncluded || p.BrowserSessionDataIncluded);
    }

    [TestMethod]
    public void exported_payload_contains_redaction_marker()
    {
        var export = new SimulatedRedactingExporter().ExportWithRawPayload("ctx", DirtyPayload());
        Assert.IsTrue(export.SerializedExport.Contains(SimulatedRedactor.Marker, StringComparison.Ordinal));
    }

    [TestMethod]
    public void exported_payload_contains_no_forbidden_raw_values()
    {
        var export = new SimulatedRedactingExporter().ExportWithRawPayload("ctx", DirtyPayload());
        foreach (var value in DirtyPayload().SensitiveValues)
            Assert.IsFalse(export.SerializedExport.Contains(value, StringComparison.Ordinal), value);
    }

    [TestMethod]
    public void redaction_proof_clean_input_reports_no_secret_detected()
    {
        var result = new SimulatedRedactor().Redact(new RawAuditPayload("only safe summary, nothing forbidden"));
        Assert.IsFalse(result.SecretDetected);
        Assert.IsFalse(result.RawContainedSensitive);
        Assert.AreEqual("only safe summary, nothing forbidden", result.RedactedPayload);
    }

    [TestMethod]
    public void redaction_proof_dirty_input_reports_secret_detected()
    {
        Assert.IsTrue(new SimulatedRedactor().Redact(DirtyPayload()).SecretDetected);
    }

    [TestMethod]
    public void redaction_proof_dirty_input_reports_secret_redacted()
    {
        Assert.IsTrue(new SimulatedRedactor().Redact(DirtyPayload()).Redacted);
    }

    [TestMethod]
    public void redaction_test_fails_if_secret_remains_in_export()
    {
        // Safe mode: secret is absent (this assertion would FAIL if redaction were a no-op).
        var safe = new SimulatedRedactingExporter().ExportWithRawPayload("ctx", DirtyPayload(), redactionEnabled: true);
        Assert.IsFalse(safe.SerializedExport.Contains(FakeSecret, StringComparison.Ordinal));
        // Proof of non-vacuity: with redaction DISABLED the same secret DOES remain.
        var unsafeExport = new SimulatedRedactingExporter().ExportWithRawPayload("ctx", DirtyPayload(), redactionEnabled: false);
        Assert.IsTrue(unsafeExport.SerializedExport.Contains(FakeSecret, StringComparison.Ordinal));
    }

    [TestMethod]
    public void injected_dangerous_export_flag_includes_actual_dangerous_payload()
    {
        // The unsafe path actually carries the dangerous value (real injection, not a boolean flip).
        var unsafeRedaction = new SimulatedRedactor().Redact(DirtyPayload(), redactionEnabled: false);
        Assert.IsTrue(unsafeRedaction.RedactedPayload.Contains(FakeSecret, StringComparison.Ordinal));
        Assert.IsTrue(unsafeRedaction.RedactionProof.SecretsIncluded);
    }

    [TestMethod]
    public void dangerous_payload_is_sanitized_or_rejected()
    {
        var sanitized = new SimulatedRedactor().Redact(DirtyPayload(), redactionEnabled: true);
        Assert.IsFalse(sanitized.OutputContainsAnySensitiveValue(DirtyPayload().SensitiveValues));
        Assert.IsTrue(sanitized.Redacted);
    }

    [TestMethod]
    public void redaction_proof_links_to_evidence_envelope()
    {
        var export = new SimulatedRedactingExporter().ExportWithRawPayload("link-ctx", DirtyPayload());
        Assert.IsFalse(string.IsNullOrWhiteSpace(export.EvidenceEnvelopeRef));
        StringAssert.Contains(export.RedactionProofRef, "link-ctx");
    }

    [TestMethod]
    public void redaction_proof_links_to_audit_export_package()
    {
        var export = new SimulatedRedactingExporter().ExportWithRawPayload("link-ctx", DirtyPayload());
        Assert.AreEqual(export.ExportId, export.AuditExportPackageRef);
    }

    // ===================== M867 — Replay measured idempotency (F4) =====================

    private static (string decision, string? executor, NoExecutionProof proof) RunMeasuredReplay()
    {
        // Audit-only replay modelled as a deterministic re-route of the same capability.
        var sink = new RecordingSideEffectSink();
        var route = new SimulatedCapabilityRouter().Route("ledger_append", sink);
        return (route.Decision.ToString(), route.SelectedExecutor, NoExecutionProof.FromSink(sink));
    }

    [TestMethod]
    public void duplicate_replay_two_runs_keep_sink_zero()
    {
        var sinkOne = new RecordingSideEffectSink();
        var sinkTwo = new RecordingSideEffectSink();
        var router = new SimulatedCapabilityRouter();
        router.Route("ledger_append", sinkOne);
        router.Route("ledger_append", sinkTwo);
        Assert.AreEqual(0, sinkOne.InvocationCount);
        Assert.AreEqual(0, sinkTwo.InvocationCount);
    }

    [TestMethod]
    public void duplicate_replay_two_runs_are_idempotent_audit_only()
    {
        var first = RunMeasuredReplay();
        var second = RunMeasuredReplay();
        Assert.AreEqual(first.decision, second.decision);
        Assert.AreEqual(first.executor, second.executor);
        Assert.AreEqual(first.proof.SideEffectSinkInvocations, second.proof.SideEffectSinkInvocations);
    }

    [TestMethod]
    public void replay_tampered_run_keeps_sink_zero()
    {
        // A tampered (denylisted) replay target is still denied with no executor and sink zero.
        var sink = new RecordingSideEffectSink();
        var route = new SimulatedCapabilityRouter().Route("product_file_modification", sink);
        Assert.AreEqual(SimulatedDecision.Deny, route.Decision);
        Assert.IsNull(route.SelectedExecutor);
        Assert.AreEqual(0, sink.InvocationCount);
    }

    [TestMethod]
    public void replay_never_selects_executor_after_two_runs()
    {
        var router = new SimulatedCapabilityRouter();
        var a = router.Route("provider_cloud_live_call", new RecordingSideEffectSink());
        var b = router.Route("provider_cloud_live_call", new RecordingSideEffectSink());
        Assert.IsNull(a.SelectedExecutor);
        Assert.IsNull(b.SelectedExecutor);
    }

    [TestMethod]
    public void replay_no_execution_proof_uses_measured_sink()
    {
        var (_, _, proof) = RunMeasuredReplay();
        Assert.AreEqual(0, proof.SideEffectSinkInvocations);
        Assert.IsTrue(proof.IsClean);
    }
}
