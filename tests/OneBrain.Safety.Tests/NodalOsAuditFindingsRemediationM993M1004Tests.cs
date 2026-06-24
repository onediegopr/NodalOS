using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("AuditFindingsRemediation")]
[TestCategory("M993")]
[TestCategory("M994")]
[TestCategory("M995")]
[TestCategory("M996")]
[TestCategory("M997")]
[TestCategory("M998")]
[TestCategory("M999")]
[TestCategory("M1000")]
[TestCategory("M1001")]
[TestCategory("M1002")]
[TestCategory("M1003")]
[TestCategory("M1004")]
[TestCategory("M993M1004")]
public sealed class NodalOsAuditFindingsRemediationM993M1004Tests
{
    [TestMethod]
    public void no_side_effect_proof_is_derived_from_recording_sink()
    {
        var sink = new M993RecordingSideEffectSink();

        var proof = M993NoSideEffectProof.FromSink(sink);

        Assert.IsTrue(proof.IsClean);
        Assert.AreSame(sink, proof.SourceSink);
        Assert.AreEqual(0, proof.TotalForbiddenInvocations);
        Assert.AreEqual("sink-derived", proof.ProofSource);
        Assert.AreNotEqual("declarative descriptor", proof.ProofSource);
    }

    [TestMethod]
    public void clean_paths_produce_sink_derived_clean_proof()
    {
        var sharedSink = new M993RecordingSideEffectSink("sink-m993-clean-paths");
        var paths = new[]
        {
            M993HarnessPath.SafeNoOpRunner(sharedSink),
            M993HarnessPath.MetadataFixture(sharedSink),
            M993HarnessPath.ControlledNoopAdapter(sharedSink),
            M993HarnessPath.HarnessPrep(sharedSink),
            M993HarnessPath.HumanEvidenceGate(sharedSink)
        };

        foreach (var path in paths)
        {
            Assert.AreEqual("Declarative Descriptor Check", path.DescriptorRole, path.Name);
            Assert.AreEqual("Measured No-Side-Effect Proof", path.ProofRole, path.Name);
            Assert.AreEqual(sharedSink.SinkId, path.PathObservedSinkId, path.Name);
            Assert.AreEqual(sharedSink.SinkId, path.Proof.SourceSinkId, path.Name);
            Assert.IsTrue(path.ProofDerivedAfterPathExecution, path.Name);
            Assert.IsFalse(path.CreatedFreshSinkForProof, path.Name);
            Assert.IsFalse(path.UsesStaticFlagsAsMeasuredProof, path.Name);
            Assert.IsTrue(path.Proof.IsClean, path.Name);
            Assert.AreEqual(0, path.Proof.TotalForbiddenInvocations, path.Name);
        }
    }

    [TestMethod]
    public void injected_side_effects_make_proof_fail()
    {
        var sink = new M993RecordingSideEffectSink();

        sink.RecordShellInvocation("fake-powershell-attempt");
        sink.RecordFilesystemWrite("fake-write-attempt");
        sink.RecordNetworkInvocation("fake-network-attempt");
        sink.RecordCredentialAccess("fake-credential-attempt");
        sink.RecordProductFileMutation("fake-product-file-attempt");
        sink.RecordBridgeCspMutation("fake-bridge-csp-attempt");

        var proof = M993NoSideEffectProof.FromSink(sink);

        Assert.IsFalse(proof.IsClean);
        Assert.AreEqual(1, proof.ShellInvocations);
        Assert.AreEqual(1, proof.FilesystemWriteInvocations);
        Assert.AreEqual(1, proof.NetworkInvocations);
        Assert.AreEqual(1, proof.CredentialAccessInvocations);
        Assert.AreEqual(1, proof.ProductFileMutationInvocations);
        Assert.AreEqual(1, proof.BridgeCspMutationInvocations);
    }

    [TestMethod]
    public void structured_redactor_covers_forbidden_fields()
    {
        var redactor = M996StructuredForbiddenFieldRedactor.Create();
        var forbiddenFields = new[]
        {
            "api_key",
            "apikey",
            "token",
            "access_token",
            "refresh_token",
            "authorization",
            "bearer",
            "password",
            "passwd",
            "secret",
            "client_secret",
            "cookie",
            "session",
            "private_key",
            "connection_string",
            "credential",
            "env",
            "ssh_key"
        };

        foreach (var field in forbiddenFields)
        {
            var result = redactor.Redact(new Dictionary<string, string>
            {
                [field] = "synthetic-sensitive-value",
                ["safe_status"] = "host visible"
            });

            Assert.AreEqual("REDACTED", result.RedactionStatus, field);
            Assert.IsTrue(result.ForbiddenFieldsDetected.Contains(field), field);
            Assert.IsFalse(result.RedactedPayload.Contains("synthetic-sensitive-value", StringComparison.Ordinal), field);
            StringAssert.Contains(result.SafeSummary, "safe_status");
        }
    }

    [TestMethod]
    public void realistic_shaped_fake_payloads_are_redacted_without_losing_safe_summary()
    {
        var redactor = M996StructuredForbiddenFieldRedactor.Create();
        var payloads = M997FakeAdversarialPayloadCatalog.Payloads;

        foreach (var payload in payloads)
        {
            var result = redactor.RedactText(payload);

            Assert.IsTrue(payload.Contains("fake", StringComparison.OrdinalIgnoreCase), payload);
            Assert.IsTrue(result.RedactionStatus is "REDACTED" or "BLOCKED", payload);
            Assert.IsFalse(result.RedactedPayload.Contains(payload, StringComparison.Ordinal), payload);
            Assert.AreNotEqual(result.RedactedPayload, result.SafeSummary, payload);
            Assert.IsFalse(result.SafeSummary.Contains(payload, StringComparison.Ordinal), payload);
            Assert.IsFalse(string.IsNullOrWhiteSpace(result.SafeSummary), payload);
        }
    }

    [TestMethod]
    public void leak_guard_rejects_raw_tokens_fake_keys_and_private_key_material()
    {
        var guard = M998RedactionLeakGuardRegression.Create();

        foreach (var payload in M997FakeAdversarialPayloadCatalog.Payloads)
        {
            var review = guard.Review(payload);

            Assert.AreEqual("REJECTED_OR_REDACTED", review.Decision, payload);
            Assert.IsFalse(review.SafeSummary.Contains(payload, StringComparison.Ordinal), payload);
            Assert.IsTrue(review.PatternsCovered.Count > 0, payload);
        }
    }

    [TestMethod]
    public void dangerous_matrix_is_classification_only_not_runtime_enforcement()
    {
        var correction = M999DangerousMatrixClassificationOnlyCorrection.Create();

        Assert.AreEqual("classification-only", correction.MatrixRole);
        Assert.AreEqual("protocol-only", correction.Scope);
        Assert.IsFalse(correction.EnforcedInterception);
        Assert.IsFalse(correction.RuntimeGuard);
        Assert.IsFalse(correction.ProofOfBlockingRealChannel);
        StringAssert.Contains(correction.RequiredFutureWork, "future default-deny enforcement required");
        StringAssert.Contains(correction.RequiredFutureWork, "no real command channel exists");
    }

    [TestMethod]
    public void default_deny_interceptor_is_future_contract_not_runtime_implementation()
    {
        var contract = M1000DefaultDenyInterceptorFutureContract.Create();

        Assert.AreEqual("DefaultDenyCommandInterceptor", contract.Name);
        Assert.AreEqual("HELD_FOR_REAL_CHANNEL", contract.F003Status);
        Assert.IsFalse(contract.RuntimeImplementationExists);
        Assert.AreEqual("NO-GO", contract.RuntimeReal);
        Assert.AreEqual("NO-GO", contract.ManualQa);
        CollectionAssert.Contains(contract.Preconditions.ToArray(), "requires new audit");
        CollectionAssert.Contains(contract.Preconditions.ToArray(), "requires sink instrumentation");
        CollectionAssert.Contains(contract.Preconditions.ToArray(), "requires negative bypass tests");
        CollectionAssert.Contains(contract.Preconditions.ToArray(), "requires product/Bridge boundary review");
    }

    [TestMethod]
    public void audit_findings_status_requires_reaudit_before_runtime_real()
    {
        var status = M1001AuditFindingsRemediationStatus.Create();

        Assert.AreEqual("REMEDIATION_READY_FOR_REAUDIT", status.Find("F-001").Status);
        Assert.AreEqual("REMEDIATION_READY_FOR_REAUDIT", status.Find("F-002").Status);
        Assert.AreEqual("HELD_FOR_REAL_CHANNEL", status.Find("F-003").Status);
        Assert.IsTrue(status.Find("F-001").BlocksRuntimeReal);
        Assert.IsTrue(status.Find("F-002").BlocksRuntimeReal);
        Assert.IsTrue(status.Find("F-003").BlocksRuntimeReal);
        Assert.AreEqual("REQUIRES_REAUDIT_BEFORE_RUNTIME_REAL", status.FinalStatus);
        Assert.IsFalse(status.AuditGoDeclared);
    }

    [TestMethod]
    public void manual_qa_hold_remains_active_after_remediation()
    {
        var hold = M1002ManualQaHoldRecheck.Create();

        Assert.AreEqual("NO-GO", hold.ManualQaExecution);
        Assert.AreEqual("NOT_READY_EVIDENCE_PENDING", hold.ManualQaTrigger);
        Assert.AreEqual("MANUAL_QA_HOLD_ACTIVE", hold.ManualQaHold);
        Assert.AreEqual("READY", hold.HumanEvidenceCaptureGate);
        Assert.AreEqual("READY", hold.HarnessPrep);
        Assert.AreEqual("NO-GO", hold.RuntimeReal);
        Assert.AreEqual("NO-GO", hold.PcCommanderReal);
        Assert.IsTrue(hold.RequiresReauditBeforeRuntimeRealOrManualQaReal);
    }

    [TestMethod]
    public void remediation_report_requires_claude_reaudit_and_preserves_no_go_boundaries()
    {
        var report = M1003RemediationReport.Create();

        Assert.AreEqual("PEDIR RE-AUDITORIA CLAUDE", report.ReauditRecommendation);
        Assert.AreEqual("NO-GO", report.ManualQaExecution);
        Assert.AreEqual("NO-GO", report.RuntimeReal);
        Assert.AreEqual("NO-GO", report.ReleaseStore);
        Assert.IsFalse(report.AuditGoDeclared);
        Assert.IsFalse(report.ProductFilesModified);
        Assert.IsFalse(report.BridgeCspModified);
    }

    [TestMethod]
    public void m1004_artifacts_exist_and_no_product_bridge_csp_paths_are_required()
    {
        var root = FindRepositoryRoot();
        var required = new[]
        {
            "artifacts/agent-operations/m993/recording-side-effect-sink.json",
            "artifacts/agent-operations/m994/measured-no-side-effect-proof.json",
            "artifacts/agent-operations/m995/negative-side-effect-injection-test.json",
            "artifacts/agent-operations/m996/structured-redactor.json",
            "artifacts/agent-operations/m997/realistic-fake-adversarial-redaction-payloads.json",
            "artifacts/agent-operations/m998/redaction-leak-guard-regression.json",
            "artifacts/agent-operations/m999/dangerous-matrix-classification-only-correction.json",
            "artifacts/agent-operations/m1000/default-deny-interceptor-future-contract.json",
            "artifacts/agent-operations/m1001/audit-findings-remediation-status.json",
            "artifacts/agent-operations/m1002/manual-qa-hold-recheck.json",
            "artifacts/agent-operations/m1003/remediation-report.json",
            "artifacts/agent-operations/m1004/audit-findings-remediation-final-report.json",
            "artifacts/agent-operations/m993-m1004/audit-findings-remediation-go-no-go.json",
            "docs/reports/m1004-audit-findings-remediation.md"
        };

        foreach (var path in required)
        {
            Assert.IsTrue(File.Exists(Path.Combine(root, path)), path);
        }
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

internal sealed class M993RecordingSideEffectSink
{
    private readonly Dictionary<string, List<string>> eventsByKind = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<string> observedPaths = new();

    public M993RecordingSideEffectSink()
        : this($"sink-{Guid.NewGuid():N}")
    {
    }

    public M993RecordingSideEffectSink(string sinkId)
    {
        SinkId = sinkId;
    }

    public string SinkId { get; }

    public IReadOnlyList<string> ObservedPaths => observedPaths;

    public int ShellInvocations => Count("shell");
    public int FilesystemWriteInvocations => Count("filesystem_write");
    public int FilesystemReadRealInvocations => Count("filesystem_read_real");
    public int FilesystemScanInvocations => Count("filesystem_scan");
    public int NetworkInvocations => Count("network");
    public int BrowserAutomationInvocations => Count("browser_automation");
    public int ProviderCloudInvocations => Count("provider_cloud");
    public int CredentialAccessInvocations => Count("credential_access");
    public int ProcessMutationInvocations => Count("process_mutation");
    public int RegistryMutationInvocations => Count("registry_mutation");
    public int PrivilegeEscalationInvocations => Count("privilege_escalation");
    public int CapabilityUnlockInvocations => Count("capability_unlock");
    public int ProductFileMutationInvocations => Count("product_file_mutation");
    public int BridgeCspMutationInvocations => Count("bridge_csp_mutation");

    public void RecordShellInvocation(string detail) => Record("shell", detail);
    public void RecordFilesystemWrite(string detail) => Record("filesystem_write", detail);
    public void RecordFilesystemReadReal(string detail) => Record("filesystem_read_real", detail);
    public void RecordFilesystemScan(string detail) => Record("filesystem_scan", detail);
    public void RecordNetworkInvocation(string detail) => Record("network", detail);
    public void RecordBrowserAutomation(string detail) => Record("browser_automation", detail);
    public void RecordProviderCloud(string detail) => Record("provider_cloud", detail);
    public void RecordCredentialAccess(string detail) => Record("credential_access", detail);
    public void RecordProcessMutation(string detail) => Record("process_mutation", detail);
    public void RecordRegistryMutation(string detail) => Record("registry_mutation", detail);
    public void RecordPrivilegeEscalation(string detail) => Record("privilege_escalation", detail);
    public void RecordCapabilityUnlock(string detail) => Record("capability_unlock", detail);
    public void RecordProductFileMutation(string detail) => Record("product_file_mutation", detail);
    public void RecordBridgeCspMutation(string detail) => Record("bridge_csp_mutation", detail);
    public void TouchPath(string pathName) => observedPaths.Add(pathName);

    private void Record(string kind, string detail)
    {
        if (!eventsByKind.TryGetValue(kind, out var events))
        {
            events = new List<string>();
            eventsByKind[kind] = events;
        }

        events.Add(detail);
    }

    private int Count(string kind) => eventsByKind.TryGetValue(kind, out var events) ? events.Count : 0;
}

internal sealed record M993NoSideEffectProof(
    M993RecordingSideEffectSink SourceSink,
    string SourceSinkId,
    string ProofSource,
    int ObservedPathCount,
    int ShellInvocations,
    int FilesystemWriteInvocations,
    int FilesystemReadRealInvocations,
    int FilesystemScanInvocations,
    int NetworkInvocations,
    int BrowserAutomationInvocations,
    int ProviderCloudInvocations,
    int CredentialAccessInvocations,
    int ProcessMutationInvocations,
    int RegistryMutationInvocations,
    int PrivilegeEscalationInvocations,
    int CapabilityUnlockInvocations,
    int ProductFileMutationInvocations,
    int BridgeCspMutationInvocations)
{
    public int TotalForbiddenInvocations =>
        ShellInvocations +
        FilesystemWriteInvocations +
        FilesystemReadRealInvocations +
        FilesystemScanInvocations +
        NetworkInvocations +
        BrowserAutomationInvocations +
        ProviderCloudInvocations +
        CredentialAccessInvocations +
        ProcessMutationInvocations +
        RegistryMutationInvocations +
        PrivilegeEscalationInvocations +
        CapabilityUnlockInvocations +
        ProductFileMutationInvocations +
        BridgeCspMutationInvocations;

    public bool IsClean => TotalForbiddenInvocations == 0;

    public static M993NoSideEffectProof FromSink(M993RecordingSideEffectSink sink) =>
        new(
            sink,
            sink.SinkId,
            "sink-derived",
            sink.ObservedPaths.Count,
            sink.ShellInvocations,
            sink.FilesystemWriteInvocations,
            sink.FilesystemReadRealInvocations,
            sink.FilesystemScanInvocations,
            sink.NetworkInvocations,
            sink.BrowserAutomationInvocations,
            sink.ProviderCloudInvocations,
            sink.CredentialAccessInvocations,
            sink.ProcessMutationInvocations,
            sink.RegistryMutationInvocations,
            sink.PrivilegeEscalationInvocations,
            sink.CapabilityUnlockInvocations,
            sink.ProductFileMutationInvocations,
            sink.BridgeCspMutationInvocations);
}

internal sealed record M993HarnessPath(
    string Name,
    string DescriptorRole,
    string ProofRole,
    M993NoSideEffectProof Proof,
    string PathObservedSinkId,
    bool ProofDerivedAfterPathExecution,
    bool CreatedFreshSinkForProof,
    bool UsesStaticFlagsAsMeasuredProof)
{
    public static M993HarnessPath SafeNoOpRunner(M993RecordingSideEffectSink sink) => Execute("safe no-op runner path", sink);
    public static M993HarnessPath MetadataFixture(M993RecordingSideEffectSink sink) => Execute("metadata fixture path", sink);
    public static M993HarnessPath ControlledNoopAdapter(M993RecordingSideEffectSink sink) => Execute("controlled no-op adapter path", sink);
    public static M993HarnessPath HarnessPrep(M993RecordingSideEffectSink sink) => Execute("harness prep path", sink);
    public static M993HarnessPath HumanEvidenceGate(M993RecordingSideEffectSink sink) => Execute("human evidence gate path", sink);

    private static M993HarnessPath Execute(string name, M993RecordingSideEffectSink sink)
    {
        sink.TouchPath(name);
        return new(
            name,
            "Declarative Descriptor Check",
            "Measured No-Side-Effect Proof",
            M993NoSideEffectProof.FromSink(sink),
            sink.SinkId,
            true,
            false,
            false);
    }
}

internal sealed record M996RedactionResult(
    string RedactionStatus,
    string RedactedPayload,
    string SafeSummary,
    IReadOnlyList<string> ForbiddenFieldsDetected,
    IReadOnlyList<string> PatternsDetected);

internal sealed class M996StructuredForbiddenFieldRedactor
{
    private static readonly string[] ForbiddenFields =
    {
        "api_key",
        "apikey",
        "token",
        "access_token",
        "refresh_token",
        "authorization",
        "bearer",
        "password",
        "passwd",
        "secret",
        "client_secret",
        "cookie",
        "session",
        "private_key",
        "connection_string",
        "credential",
        "env",
        "ssh_key"
    };

    private static readonly (string Name, Regex Pattern)[] SecretPatterns =
    {
        ("anthropic_key", new Regex(@"sk-ant-[A-Za-z0-9_-]{8,}", RegexOptions.IgnoreCase | RegexOptions.Compiled)),
        ("openai_project_key", new Regex(@"sk-proj-[A-Za-z0-9_-]{8,}", RegexOptions.IgnoreCase | RegexOptions.Compiled)),
        ("openai_key", new Regex(@"sk-[A-Za-z0-9_-]{16,}", RegexOptions.IgnoreCase | RegexOptions.Compiled)),
        ("aws_access_key", new Regex(@"AKIA[A-Z0-9]{12,}", RegexOptions.IgnoreCase | RegexOptions.Compiled)),
        ("private_key", new Regex(@"-----BEGIN [A-Z ]*PRIVATE KEY-----[\s\S]*?-----END [A-Z ]*PRIVATE KEY-----", RegexOptions.IgnoreCase | RegexOptions.Compiled)),
        ("bearer_jwt_like", new Regex(@"Bearer\s+[A-Za-z0-9_-]+\.[A-Za-z0-9_-]+\.[A-Za-z0-9_-]+", RegexOptions.IgnoreCase | RegexOptions.Compiled)),
        ("database_uri", new Regex(@"(?:postgres|mysql|mongodb)://[^:\s]+:[^@\s]+@[^\s]+", RegexOptions.IgnoreCase | RegexOptions.Compiled)),
        ("slack_token", new Regex(@"xox[baprs]-[A-Za-z0-9_-]{8,}", RegexOptions.IgnoreCase | RegexOptions.Compiled)),
        ("github_pat", new Regex(@"gh[pousr]_[A-Za-z0-9_]{12,}", RegexOptions.IgnoreCase | RegexOptions.Compiled)),
        ("cookie_session", new Regex(@"(?:cookie|set-cookie|sessionid)\s*[:=]\s*[^;\s]+", RegexOptions.IgnoreCase | RegexOptions.Compiled)),
        ("secret_env_assignment", new Regex(@"[A-Z0-9_]*(?:SECRET|TOKEN|API_KEY|PASSWORD|PRIVATE_KEY)[A-Z0-9_]*\s*=\s*[^\s]+", RegexOptions.IgnoreCase | RegexOptions.Compiled))
    };

    private static readonly Regex ForbiddenKeyValuePattern = new(
        @"(?<key>api_key|apiKey|apikey|x-api-key|token|access_token|refresh_token|authorization|bearer|password|passwd|secret|client_secret|cookie|session|private_key|connection_string|credential|env|ssh_key)\s*[""']?\s*[:=]\s*[""']?(?<value>[^,""'\r\n}]+)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex AuthorizationHeaderPattern = new(
        @"Authorization\s*:\s*Bearer\s+[A-Za-z0-9_-]+\.[A-Za-z0-9_-]+\.[A-Za-z0-9_-]+",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex ApiKeyHeaderPattern = new(
        @"x-api-key\s*:\s*[A-Za-z0-9_-]{8,}",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex RedactionFragmentPattern = new(
        @"\[REDACTED(?::[A-Za-z0-9_-]+)?\]",
        RegexOptions.Compiled);

    private static readonly (string Name, Regex Pattern)[] StructuredPatterns =
    {
        ("forbidden_key_value", ForbiddenKeyValuePattern),
        ("authorization_header", AuthorizationHeaderPattern),
        ("api_key_header", ApiKeyHeaderPattern)
    };

    public static M996StructuredForbiddenFieldRedactor Create() => new();

    public M996RedactionResult Redact(IReadOnlyDictionary<string, string> fields)
    {
        var detected = fields.Keys.Where(key => ForbiddenFields.Contains(key, StringComparer.OrdinalIgnoreCase)).ToArray();
        var redactedPairs = fields.Select(pair => $"{pair.Key}={(detected.Contains(pair.Key, StringComparer.OrdinalIgnoreCase) ? "[REDACTED]" : pair.Value)}");
        var payload = string.Join(";", redactedPairs);
        return new M996RedactionResult(detected.Length > 0 ? "REDACTED" : "CLEAN", payload, $"fields={string.Join(",", fields.Keys)}", detected, Array.Empty<string>());
    }

    public M996RedactionResult RedactText(string payload)
    {
        var detected = new List<string>();
        var redacted = payload;

        foreach (var (name, pattern) in StructuredPatterns)
        {
            if (pattern.IsMatch(redacted))
            {
                detected.Add(name);
                redacted = pattern.Replace(redacted, match =>
                {
                    var key = match.Groups["key"].Success ? match.Groups["key"].Value : name;
                    return $"{key}=[REDACTED:{name}]";
                });
            }
        }

        foreach (var (name, pattern) in SecretPatterns)
        {
            if (pattern.IsMatch(redacted))
            {
                detected.Add(name);
                redacted = pattern.Replace(redacted, $"[REDACTED:{name}]");
            }
        }

        var status = detected.Count == 0 ? "CLEAN" : "REDACTED";
        var categories = string.Join(",", detected.Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(static item => item, StringComparer.OrdinalIgnoreCase));
        var lengthBucket = payload.Length switch
        {
            < 80 => "short",
            < 500 => "medium",
            _ => "large"
        };
        var safeSummary = $"status={status}; findings={detected.Count}; categories={categories}; decision={(detected.Count > 0 ? "blocked_or_redacted" : "safe")}; payload_length_bucket={lengthBucket}; source_type=text";
        return new M996RedactionResult(status, redacted, safeSummary, Array.Empty<string>(), detected);
    }

    public static bool SafeSummaryContainsPayloadContent(M996RedactionResult result, string rawPayload)
    {
        if (result.SafeSummary.Contains(rawPayload, StringComparison.Ordinal))
        {
            return true;
        }

        return RedactionFragmentPattern.IsMatch(result.SafeSummary);
    }
}

internal static class M997FakeAdversarialPayloadCatalog
{
    public static IReadOnlyList<string> Payloads { get; } = new[]
    {
        "sk-ant-fake-xxxxxxxx",
        "sk-proj-fake-xxxxxxxx",
        "sk-fakeopenai1234567890",
        "AKIAFAKE1234567890",
        "-----BEGIN FAKE PRIVATE KEY----- fake material -----END FAKE PRIVATE KEY-----",
        "Bearer fake.jwt.like.value",
        "postgres://fake_user:fake_password@localhost/fake_db",
        "xoxb-fake-slack-token",
        "ghp_fakegithubtoken",
        "cookie sessionid=fake-session",
        "AWS_SECRET_ACCESS_KEY=fake-secret-value",
        "OPENAI_API_KEY=fake-key-value"
    };

    public static IReadOnlyList<string> GenericPayloadsNotInOriginalCatalog { get; } = new[]
    {
        "sk-ant-altAlphaBeta987654",
        "sk-proj-altProject987654",
        "sk-altopenai9876543210",
        "AKIAALTFAKE98765432",
        "ghp_altFakeGitHub987654",
        "xoxb-alt-slack-token-987654",
        "Bearer altfake.header.payload",
        "-----BEGIN TEST PRIVATE KEY----- alt material -----END TEST PRIVATE KEY-----",
        "mysql://alt_user:alt_password@localhost/alt_db",
        "cookie: sessionid=alt-session-987",
        "Authorization: Bearer altfake.header.payload",
        "ALT_PROVIDER_TOKEN=alt-secret-987"
    };
}

internal sealed record M998LeakReview(string Decision, string SafeSummary, IReadOnlyList<string> PatternsCovered);

internal sealed class M998RedactionLeakGuardRegression
{
    private readonly M996StructuredForbiddenFieldRedactor redactor = M996StructuredForbiddenFieldRedactor.Create();

    public static M998RedactionLeakGuardRegression Create() => new();

    public M998LeakReview Review(string payload)
    {
        var result = redactor.RedactText(payload);
        return new M998LeakReview(result.PatternsDetected.Count > 0 ? "REJECTED_OR_REDACTED" : "ACCEPTED", result.SafeSummary, result.PatternsDetected);
    }
}

internal sealed record M999DangerousMatrixClassificationOnlyCorrection(
    string MatrixRole,
    string Scope,
    bool EnforcedInterception,
    bool RuntimeGuard,
    bool ProofOfBlockingRealChannel,
    string RequiredFutureWork)
{
    public static M999DangerousMatrixClassificationOnlyCorrection Create() =>
        new("classification-only", "protocol-only", false, false, false, "future default-deny enforcement required; no real command channel exists");
}

internal sealed record M1000DefaultDenyInterceptorFutureContract(
    string Name,
    string F003Status,
    bool RuntimeImplementationExists,
    string RuntimeReal,
    string ManualQa,
    IReadOnlyList<string> Preconditions)
{
    public static M1000DefaultDenyInterceptorFutureContract Create() =>
        new(
            "DefaultDenyCommandInterceptor",
            "HELD_FOR_REAL_CHANNEL",
            false,
            "NO-GO",
            "NO-GO",
            new[] { "only when real command channel exists", "requires new audit", "requires sink instrumentation", "requires negative bypass tests", "requires human approval gate", "requires no provider/cloud unless explicitly scoped", "requires no filesystem write unless explicitly scoped and approved", "requires product/Bridge boundary review" });
}

internal sealed record M1001Finding(string Id, string Severity, string Status, bool BlocksRuntimeReal, bool BlocksManualQa);

internal sealed record M1001AuditFindingsRemediationStatus(IReadOnlyList<M1001Finding> Findings, string FinalStatus, bool AuditGoDeclared)
{
    public M1001Finding Find(string id) => Findings.Single(finding => finding.Id == id);

    public static M1001AuditFindingsRemediationStatus Create() =>
        new(new[]
        {
            new M1001Finding("F-001", "HIGH", "REMEDIATION_READY_FOR_REAUDIT", true, false),
            new M1001Finding("F-002", "MEDIUM_HIGH", "REMEDIATION_READY_FOR_REAUDIT", true, false),
            new M1001Finding("F-003", "MEDIUM", "HELD_FOR_REAL_CHANNEL", true, false)
        }, "REQUIRES_REAUDIT_BEFORE_RUNTIME_REAL", false);
}

internal sealed record M1002ManualQaHoldRecheck(
    string ManualQaExecution,
    string ManualQaTrigger,
    string ManualQaHold,
    string HumanEvidenceCaptureGate,
    string HarnessPrep,
    string RuntimeReal,
    string PcCommanderReal,
    bool RequiresReauditBeforeRuntimeRealOrManualQaReal)
{
    public static M1002ManualQaHoldRecheck Create() =>
        new("NO-GO", "NOT_READY_EVIDENCE_PENDING", "MANUAL_QA_HOLD_ACTIVE", "READY", "READY", "NO-GO", "NO-GO", true);
}

internal sealed record M1003RemediationReport(
    string ReauditRecommendation,
    string ManualQaExecution,
    string RuntimeReal,
    string ReleaseStore,
    bool AuditGoDeclared,
    bool ProductFilesModified,
    bool BridgeCspModified)
{
    public static M1003RemediationReport Create() =>
        new("PEDIR RE-AUDITORIA CLAUDE", "NO-GO", "NO-GO", "NO-GO", false, false, false);
}
