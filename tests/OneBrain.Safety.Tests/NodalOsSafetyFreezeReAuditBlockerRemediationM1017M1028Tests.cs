using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("SafetyFreezeReAuditBlockerRemediation")]
[TestCategory("M1017")]
[TestCategory("M1018")]
[TestCategory("M1019")]
[TestCategory("M1020")]
[TestCategory("M1021")]
[TestCategory("M1022")]
[TestCategory("M1023")]
[TestCategory("M1024")]
[TestCategory("M1025")]
[TestCategory("M1026")]
[TestCategory("M1027")]
[TestCategory("M1028")]
[TestCategory("M1017M1028")]
public sealed class NodalOsSafetyFreezeReAuditBlockerRemediationM1017M1028Tests
{
    [TestMethod]
    public void shared_sink_identity_is_propagated_through_all_clean_paths()
    {
        var sharedSink = new M993RecordingSideEffectSink("sink-m1017-shared");
        var paths = ExecuteAllCleanPaths(sharedSink);

        foreach (var path in paths)
        {
            Assert.AreEqual("sink-m1017-shared", path.PathObservedSinkId, path.Name);
            Assert.AreEqual("sink-m1017-shared", path.Proof.SourceSinkId, path.Name);
            Assert.AreSame(sharedSink, path.Proof.SourceSink, path.Name);
            Assert.IsTrue(path.ProofDerivedAfterPathExecution, path.Name);
            Assert.IsFalse(path.CreatedFreshSinkForProof, path.Name);
            Assert.IsFalse(path.UsesStaticFlagsAsMeasuredProof, path.Name);
            Assert.IsTrue(path.Proof.IsClean, path.Name);
        }

        CollectionAssert.AreEquivalent(
            new[] { "safe no-op runner path", "metadata fixture path", "controlled no-op adapter path", "harness prep path", "human evidence gate path" },
            sharedSink.ObservedPaths.ToArray());
    }

    [TestMethod]
    public void safe_noop_and_metadata_negative_path_injections_fail_the_shared_sink_proof()
    {
        AssertDirtyAfterPath(M993HarnessPath.SafeNoOpRunner, static sink => sink.RecordShellInvocation("path-level-shell-injection"), "safe no-op runner path");
        AssertDirtyAfterPath(M993HarnessPath.MetadataFixture, static sink => sink.RecordFilesystemReadReal("path-level-read-real-injection"), "metadata fixture path");
        AssertDirtyAfterPath(M993HarnessPath.MetadataFixture, static sink => sink.RecordFilesystemWrite("path-level-write-injection"), "metadata fixture path");
        AssertDirtyAfterPath(M993HarnessPath.MetadataFixture, static sink => sink.RecordCredentialAccess("path-level-credential-injection"), "metadata fixture path");
    }

    [TestMethod]
    public void controlled_noop_adapter_negative_path_injections_fail_the_shared_sink_proof()
    {
        AssertDirtyAfterPath(M993HarnessPath.ControlledNoopAdapter, static sink => sink.RecordNetworkInvocation("adapter-network-injection"), "controlled no-op adapter path");
        AssertDirtyAfterPath(M993HarnessPath.ControlledNoopAdapter, static sink => sink.RecordProcessMutation("adapter-process-injection"), "controlled no-op adapter path");
        AssertDirtyAfterPath(M993HarnessPath.ControlledNoopAdapter, static sink => sink.RecordProductFileMutation("adapter-product-file-injection"), "controlled no-op adapter path");
        AssertDirtyAfterPath(M993HarnessPath.ControlledNoopAdapter, static sink => sink.RecordBridgeCspMutation("adapter-bridge-csp-injection"), "controlled no-op adapter path");
    }

    [TestMethod]
    public void harness_and_human_evidence_gate_negative_path_injections_fail_the_shared_sink_proof()
    {
        AssertDirtyAfterPath(M993HarnessPath.HarnessPrep, static sink => sink.RecordBrowserAutomation("harness-browser-automation-injection"), "harness prep path");
        AssertDirtyAfterPath(M993HarnessPath.HumanEvidenceGate, static sink => sink.RecordCredentialAccess("human-evidence-credential-injection"), "human evidence gate path");
        AssertDirtyAfterPath(M993HarnessPath.HumanEvidenceGate, static sink => sink.RecordBridgeCspMutation("human-evidence-bridge-csp-injection"), "human evidence gate path");
    }

    [TestMethod]
    public void generic_secret_shaped_fake_values_are_redacted_without_exact_catalog_dependency()
    {
        var redactor = M996StructuredForbiddenFieldRedactor.Create();

        foreach (var payload in M997FakeAdversarialPayloadCatalog.GenericPayloadsNotInOriginalCatalog)
        {
            var result = redactor.RedactText(payload);

            Assert.AreEqual("REDACTED", result.RedactionStatus, payload);
            Assert.IsFalse(result.RedactedPayload.Contains(payload, StringComparison.Ordinal), payload);
            Assert.IsFalse(result.SafeSummary.Contains(payload, StringComparison.Ordinal), payload);
            Assert.IsTrue(result.PatternsDetected.Count > 0, payload);
            Assert.IsFalse(M997FakeAdversarialPayloadCatalog.Payloads.Contains(payload), payload);
        }
    }

    [TestMethod]
    public void structured_json_text_and_header_key_matching_redacts_forbidden_fields()
    {
        var redactor = M996StructuredForbiddenFieldRedactor.Create();
        var payloads = new[]
        {
            "{\"outer\":{\"api_key\":\"sk-altopenai9876543210\"}}",
            "access_token=token-alt-987654",
            "password: yaml-alt-password",
            "Authorization: Bearer altfake.header.payload",
            "x-api-key: alt-api-key-987654",
            "mixed text client_secret=client-secret-alt-987654 end"
        };

        foreach (var payload in payloads)
        {
            var result = redactor.RedactText(payload);

            Assert.AreEqual("REDACTED", result.RedactionStatus, payload);
            Assert.IsFalse(result.RedactedPayload.Contains("altopenai9876543210", StringComparison.Ordinal), payload);
            Assert.IsFalse(result.RedactedPayload.Contains("token-alt-987654", StringComparison.Ordinal), payload);
            Assert.IsFalse(result.RedactedPayload.Contains("yaml-alt-password", StringComparison.Ordinal), payload);
            Assert.IsFalse(result.RedactedPayload.Contains("altfake.header.payload", StringComparison.Ordinal), payload);
            Assert.IsFalse(result.RedactedPayload.Contains("alt-api-key-987654", StringComparison.Ordinal), payload);
            Assert.IsFalse(result.RedactedPayload.Contains("client-secret-alt-987654", StringComparison.Ordinal), payload);
            CollectionAssert.Contains(result.PatternsDetected.ToArray(), "forbidden_key_value");
        }
    }

    [TestMethod]
    public void safe_summary_is_metadata_only_and_not_the_redacted_payload()
    {
        var redactor = M996StructuredForbiddenFieldRedactor.Create();
        var payload = "Authorization: Bearer altfake.header.payload";
        var result = redactor.RedactText(payload);

        Assert.AreNotEqual(result.RedactedPayload, result.SafeSummary);
        Assert.IsFalse(result.SafeSummary.Contains(payload, StringComparison.Ordinal));
        Assert.IsFalse(result.SafeSummary.Contains("altfake.header.payload", StringComparison.Ordinal));
        Assert.IsFalse(result.SafeSummary.Contains("[REDACTED", StringComparison.Ordinal));
        StringAssert.Contains(result.SafeSummary, "status=REDACTED");
        StringAssert.Contains(result.SafeSummary, "findings=");
        StringAssert.Contains(result.SafeSummary, "categories=");
        StringAssert.Contains(result.SafeSummary, "payload_length_bucket=");
        Assert.IsFalse(M996StructuredForbiddenFieldRedactor.SafeSummaryContainsPayloadContent(result, payload));
    }

    [TestMethod]
    public void f003_remains_classification_only_and_held_for_real_channel()
    {
        var correction = M999DangerousMatrixClassificationOnlyCorrection.Create();
        var contract = M1000DefaultDenyInterceptorFutureContract.Create();

        Assert.AreEqual("classification-only", correction.MatrixRole);
        Assert.IsFalse(correction.EnforcedInterception);
        Assert.IsFalse(correction.RuntimeGuard);
        Assert.IsFalse(correction.ProofOfBlockingRealChannel);
        Assert.AreEqual("HELD_FOR_REAL_CHANNEL", contract.F003Status);
        Assert.IsFalse(contract.RuntimeImplementationExists);
        Assert.AreEqual("NO-GO", contract.RuntimeReal);
        Assert.AreEqual("NO-GO", contract.ManualQa);
    }

    [TestMethod]
    public void safety_freeze_and_followup_artifacts_preserve_no_go_boundaries()
    {
        var root = FindRepositoryRoot();
        var required = new[]
        {
            "artifacts/agent-operations/m1017/shared-sink-path-contract.json",
            "artifacts/agent-operations/m1021/f001-path-connected-measured-proof-remediation.json",
            "artifacts/agent-operations/m1024/f002-generic-redaction-safe-summary-remediation.json",
            "artifacts/agent-operations/m1025/f003-held-for-real-channel-reaffirmation.json",
            "artifacts/agent-operations/m1026/safety-freeze-status-update.json",
            "artifacts/agent-operations/m1027/re-audit-followup-package-update.json",
            "artifacts/agent-operations/m1028/safety-freeze-re-audit-blocker-remediation-final-report.json",
            "artifacts/agent-operations/m1017-m1028/safety-freeze-re-audit-blocker-remediation-go-no-go.json",
            "docs/reports/m1028-safety-freeze-re-audit-blocker-remediation.md"
        };

        foreach (var relativePath in required)
        {
            Assert.IsTrue(File.Exists(Path.Combine(root, relativePath)), relativePath);
        }

        var freeze = File.ReadAllText(Path.Combine(root, "artifacts/agent-operations/m1026/safety-freeze-status-update.json"));
        StringAssert.Contains(freeze, "\"SafetyFreeze\": \"ACTIVE\"");
        StringAssert.Contains(freeze, "\"ManualQaExecution\": \"NO-GO\"");
        StringAssert.Contains(freeze, "\"RuntimeReal\": \"NO-GO\"");
        StringAssert.Contains(freeze, "\"PcCommanderReal\": \"NO-GO\"");
        StringAssert.Contains(freeze, "\"BridgeCsp\": \"unchanged\"");
    }

    [TestMethod]
    public void artifacts_do_not_contain_generic_fake_secret_leaks_and_followup_prompt_is_present()
    {
        var root = FindRepositoryRoot();
        var artifactFiles = Directory.GetFiles(Path.Combine(root, "artifacts/agent-operations"), "*.json", SearchOption.AllDirectories)
            .Where(path => path.Contains("m1017", StringComparison.OrdinalIgnoreCase) ||
                           path.Contains("m1018", StringComparison.OrdinalIgnoreCase) ||
                           path.Contains("m1019", StringComparison.OrdinalIgnoreCase) ||
                           path.Contains("m1020", StringComparison.OrdinalIgnoreCase) ||
                           path.Contains("m1021", StringComparison.OrdinalIgnoreCase) ||
                           path.Contains("m1022", StringComparison.OrdinalIgnoreCase) ||
                           path.Contains("m1023", StringComparison.OrdinalIgnoreCase) ||
                           path.Contains("m1024", StringComparison.OrdinalIgnoreCase) ||
                           path.Contains("m1025", StringComparison.OrdinalIgnoreCase) ||
                           path.Contains("m1026", StringComparison.OrdinalIgnoreCase) ||
                           path.Contains("m1027", StringComparison.OrdinalIgnoreCase) ||
                           path.Contains("m1028", StringComparison.OrdinalIgnoreCase))
            .ToArray();

        foreach (var file in artifactFiles)
        {
            var content = File.ReadAllText(file);
            foreach (var forbidden in M997FakeAdversarialPayloadCatalog.GenericPayloadsNotInOriginalCatalog)
            {
                Assert.IsFalse(content.Contains(forbidden, StringComparison.Ordinal), file);
            }
        }

        var followup = File.ReadAllText(Path.Combine(root, "artifacts/agent-operations/m1027/re-audit-followup-package-update.json"));
        StringAssert.Contains(followup, "PEDIR RE-AUDITORIA GPT-5.5 XHIGH O CLAUDE");
    }

    private static IReadOnlyList<M993HarnessPath> ExecuteAllCleanPaths(M993RecordingSideEffectSink sink) =>
        new[]
        {
            M993HarnessPath.SafeNoOpRunner(sink),
            M993HarnessPath.MetadataFixture(sink),
            M993HarnessPath.ControlledNoopAdapter(sink),
            M993HarnessPath.HarnessPrep(sink),
            M993HarnessPath.HumanEvidenceGate(sink)
        };

    private static void AssertDirtyAfterPath(
        Func<M993RecordingSideEffectSink, M993HarnessPath> executePath,
        Action<M993RecordingSideEffectSink> inject,
        string expectedPath)
    {
        var sink = new M993RecordingSideEffectSink($"sink-{Guid.NewGuid():N}");
        var path = executePath(sink);

        Assert.AreEqual(expectedPath, path.Name);
        Assert.AreEqual(sink.SinkId, path.Proof.SourceSinkId);

        inject(sink);
        var dirtyProof = M993NoSideEffectProof.FromSink(sink);

        Assert.AreEqual(sink.SinkId, dirtyProof.SourceSinkId);
        Assert.IsFalse(dirtyProof.IsClean);
        Assert.IsTrue(dirtyProof.TotalForbiddenInvocations > 0);
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "OneBrain.slnx")))
        {
            directory = directory.Parent;
        }

        Assert.IsNotNull(directory, "Repository root with OneBrain.slnx was not found.");
        return directory.FullName;
    }
}
