using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.BrowserExecutor.Cdp;
using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("EvidenceRefLedgerBridge")]
[TestCategory("EvidenceRef")]
[TestCategory("EvidenceLedger")]
[TestCategory("Redaction")]
[TestCategory("RunReport")]
[TestCategory("AgentProgressReporting")]
[TestCategory("VerificationBeforeDone")]
public sealed class NodalOsEvidenceRefLedgerBridgeM383M385Tests
{
    private static readonly DateTimeOffset FixedTimestamp = new(2026, 6, 18, 0, 0, 0, TimeSpan.Zero);

    private readonly NodalOsEvidenceRefBridge bridge = new();

    [TestMethod]
    public void BridgeFromEvidenceRef_PreservesEvidenceIdKindHashCreatedAt()
    {
        var evidence = Evidence();

        var result = bridge.BridgeFromEvidenceRef(
            evidence,
            NodalOsEvidenceBridgeSourceKind.AgentOperation,
            NodalOsEvidenceBridgeUseKind.AuditTrail);

        Assert.IsTrue(result.Accepted);
        Assert.AreEqual(evidence.EvidenceId, result.Evidence.EvidenceId);
        Assert.AreEqual(evidence.Kind, result.Evidence.Kind);
        Assert.AreEqual(evidence.Hash, result.Evidence.Hash);
        Assert.AreEqual(evidence.CreatedAt, result.Evidence.CreatedAt);
        Assert.AreEqual("audit:audit-ledger-test", result.Evidence.LedgerRef);
    }

    [TestMethod]
    public void BridgeFromEvidenceRef_DefaultsToNoAuthorityForAuxiliary()
    {
        var result = bridge.BridgeFromEvidenceRef(
            Evidence(),
            NodalOsEvidenceBridgeSourceKind.AgentTask,
            NodalOsEvidenceBridgeUseKind.Auxiliary);

        Assert.IsTrue(result.Accepted);
        Assert.AreEqual(NodalOsEvidenceBridgeAuthority.NoAuthority, result.Evidence.Authority);
    }

    [TestMethod]
    public void BridgeFromEvidenceRef_DiagnosticOnlyHasDiagnosticAuthority()
    {
        var result = bridge.BridgeFromEvidenceRef(
            Evidence(),
            NodalOsEvidenceBridgeSourceKind.BrowserRuntime,
            NodalOsEvidenceBridgeUseKind.DiagnosticOnly);

        Assert.IsTrue(result.Accepted);
        Assert.AreEqual(NodalOsEvidenceBridgeAuthority.DiagnosticOnly, result.Evidence.Authority);
    }

    [TestMethod]
    public void BridgeFromEvidenceRef_VerificationSupportDoesNotAuthorizeAction()
    {
        var result = bridge.BridgeFromEvidenceRef(
            Evidence(),
            NodalOsEvidenceBridgeSourceKind.VerificationGate,
            NodalOsEvidenceBridgeUseKind.VerificationSupport);

        Assert.IsTrue(result.Accepted);
        Assert.AreEqual(NodalOsEvidenceBridgeAuthority.SupportsVerificationOnly, result.Evidence.Authority);
        Assert.IsFalse(Enum.GetNames<NodalOsEvidenceBridgeAuthority>().Any(name =>
            name.Contains("Action", StringComparison.OrdinalIgnoreCase) ||
            name.Contains("Approve", StringComparison.OrdinalIgnoreCase) ||
            name.Contains("Click", StringComparison.OrdinalIgnoreCase) ||
            name.Contains("Submit", StringComparison.OrdinalIgnoreCase)));
    }

    [TestMethod]
    public void BridgeFromEvidenceRef_RejectsSensitiveWithoutRedaction()
    {
        var evidence = Evidence(@ref: "Authorization: Bearer fakeBearerToken1234567890");

        var result = bridge.BridgeFromEvidenceRef(
            evidence,
            NodalOsEvidenceBridgeSourceKind.RunReport,
            NodalOsEvidenceBridgeUseKind.AuditTrail);

        Assert.IsFalse(result.Accepted);
        Assert.AreEqual(NodalOsEvidenceRedactionState.RedactionRequired, result.Evidence.RedactionState);
        Assert.AreEqual("[REDACTED]", result.Evidence.Ref);
        StringAssert.Contains(string.Join(" ", result.Errors), "redaction");
    }

    [TestMethod]
    public void BridgeFromEvidenceRef_AcceptsRedactedSecret()
    {
        var evidence = Evidence(kind: "credential-evidence", @ref: "[REDACTED]");

        var result = bridge.BridgeFromEvidenceRef(
            evidence,
            NodalOsEvidenceBridgeSourceKind.Manual,
            NodalOsEvidenceBridgeUseKind.Auxiliary);

        Assert.IsTrue(result.Accepted);
        Assert.AreEqual(NodalOsEvidenceSensitivity.SecretRedacted, result.Evidence.Sensitivity);
        Assert.AreEqual(NodalOsEvidenceRedactionState.Redacted, result.Evidence.RedactionState);
    }

    [TestMethod]
    public void BridgeFromEvidenceRef_UsesCommonRedactionForSensitiveRef()
    {
        var evidence = Evidence(@ref: "access_token=fake-access-token-1234567890");

        var result = bridge.BridgeFromEvidenceRef(
            evidence,
            NodalOsEvidenceBridgeSourceKind.ProgressReport,
            NodalOsEvidenceBridgeUseKind.AuditTrail);

        Assert.AreEqual("[REDACTED]", result.Evidence.Ref);
        Assert.IsFalse(result.Accepted);
    }

    [TestMethod]
    public void BridgeFromEvidenceRef_DoesNotExposeRawSecretInErrors()
    {
        var evidence = Evidence(@ref: "Authorization: Bearer fakeBearerToken1234567890");

        var result = bridge.BridgeFromEvidenceRef(
            evidence,
            NodalOsEvidenceBridgeSourceKind.RunReport,
            NodalOsEvidenceBridgeUseKind.AuditTrail);
        var json = JsonSerializer.Serialize(result);

        Assert.IsFalse(json.Contains("fakeBearerToken1234567890", StringComparison.Ordinal));
    }

    [TestMethod]
    public void BridgeFromRunReport_PreservesRunEvidenceRefs()
    {
        var report = new NodalOsRunReportBuilder()
            .CreateSuccessfulRun("run-bridge-001", "Collect audit evidence");

        var results = bridge.BridgeFromRunReport(report);

        Assert.IsTrue(results.Count >= 2);
        Assert.IsTrue(results.All(result => result.Evidence.SourceKind == NodalOsEvidenceBridgeSourceKind.RunReport));
        Assert.IsTrue(results.Any(result => result.Evidence.EvidenceId == "evidence-success"));
        Assert.IsTrue(results.All(result => result.Accepted));
    }

    [TestMethod]
    public void BridgeFromProgressReport_PreservesProgressEvidenceRefs()
    {
        var evidence = Evidence(id: "evidence-progress-bridge");
        var report = new NodalOsAgentProgressReportBuilder()
            .CreateProgress("report-bridge-001", "mission-bridge", "task-bridge", "Progress captured", [evidence]);

        var results = bridge.BridgeFromProgressReport(report);

        Assert.AreEqual(1, results.Count);
        Assert.AreEqual("evidence-progress-bridge", results[0].Evidence.EvidenceId);
        Assert.AreEqual(NodalOsEvidenceBridgeSourceKind.ProgressReport, results[0].Evidence.SourceKind);
    }

    [TestMethod]
    public void BridgeFromVerificationResult_PreservesVerificationEvidenceRefs()
    {
        var evidence = Evidence(id: "evidence-verification-bridge");
        var verification = new NodalOsVerificationBeforeDoneResult
        {
            CanMarkDone = true,
            SubjectKind = NodalOsVerificationBeforeDoneSubjectKind.AgentTask,
            SubjectId = "task-bridge",
            EvidenceRefs = [evidence],
            VerificationLabels = ["unit verification"]
        };

        var results = bridge.BridgeFromVerificationResult(verification);

        Assert.AreEqual(1, results.Count);
        Assert.AreEqual("evidence-verification-bridge", results[0].Evidence.EvidenceId);
        Assert.AreEqual(NodalOsEvidenceBridgeSourceKind.VerificationGate, results[0].Evidence.SourceKind);
        Assert.AreEqual(NodalOsEvidenceBridgeUseKind.VerificationSupport, results[0].Evidence.UseKind);
        Assert.AreEqual(NodalOsEvidenceBridgeAuthority.SupportsVerificationOnly, results[0].Evidence.Authority);
    }

    [TestMethod]
    public void BridgeResult_SerializesToJson()
    {
        var result = bridge.BridgeFromEvidenceRef(
            Evidence(),
            NodalOsEvidenceBridgeSourceKind.AgentOperation,
            NodalOsEvidenceBridgeUseKind.AuditTrail);

        var json = JsonSerializer.Serialize(result);
        var roundTrip = JsonSerializer.Deserialize<NodalOsEvidenceBridgeResult>(json);

        Assert.IsNotNull(roundTrip);
        Assert.AreEqual(result.Accepted, roundTrip.Accepted);
        Assert.AreEqual(result.Evidence.EvidenceId, roundTrip.Evidence.EvidenceId);
    }

    [TestMethod]
    public void EvidenceBridgeRef_SerializesToJson()
    {
        var result = bridge.BridgeFromEvidenceRef(
            Evidence(),
            NodalOsEvidenceBridgeSourceKind.AgentOperation,
            NodalOsEvidenceBridgeUseKind.AuditTrail);

        var json = JsonSerializer.Serialize(result.Evidence);
        var roundTrip = JsonSerializer.Deserialize<NodalOsEvidenceBridgeRef>(json);

        Assert.IsNotNull(roundTrip);
        Assert.AreEqual(result.Evidence.EvidenceId, roundTrip.EvidenceId);
        Assert.AreEqual(result.Evidence.Authority, roundTrip.Authority);
    }

    [TestMethod]
    public void UnknownSourceKind_ProducesWarningOrRejection()
    {
        var result = bridge.BridgeFromEvidenceRef(
            Evidence(),
            NodalOsEvidenceBridgeSourceKind.Unknown,
            NodalOsEvidenceBridgeUseKind.DiagnosticOnly);

        Assert.IsTrue(result.Accepted);
        StringAssert.Contains(string.Join(" ", result.Warnings), "Unknown");
    }

    [TestMethod]
    public void LedgerRefOptionalInDesignPhase()
    {
        var result = bridge.BridgeFromEvidenceRef(
            Evidence(@ref: "artifact://run-report.json"),
            NodalOsEvidenceBridgeSourceKind.RunReport,
            NodalOsEvidenceBridgeUseKind.AuditTrail);

        Assert.IsTrue(result.Accepted);
        Assert.IsNull(result.Evidence.LedgerRef);
    }

    [TestMethod]
    public void EvidenceCannotAuthorizeAction_ModelDoesNotExposeActionApproval()
    {
        var propertyNames = typeof(NodalOsEvidenceBridgeRef)
            .GetProperties()
            .Select(property => property.Name)
            .ToArray();

        Assert.IsFalse(propertyNames.Any(name =>
            name.Contains("Approve", StringComparison.OrdinalIgnoreCase) ||
            name.Contains("Authorize", StringComparison.OrdinalIgnoreCase) ||
            name.Contains("ActionAllowed", StringComparison.OrdinalIgnoreCase) ||
            name.Contains("CanClick", StringComparison.OrdinalIgnoreCase) ||
            name.Contains("CanSubmit", StringComparison.OrdinalIgnoreCase)));
    }

    [TestMethod]
    public void DiagnosticRejected_CanBeCreatedForRejectedEvidence()
    {
        var result = bridge.CreateDiagnosticRejected(
            Evidence(),
            "Rejected because secret fakeBearerToken1234567890 appeared in source diagnostics");
        var json = JsonSerializer.Serialize(result);

        Assert.IsFalse(result.Accepted);
        Assert.AreEqual(NodalOsEvidenceBridgeUseKind.DiagnosticOnly, result.Evidence.UseKind);
        Assert.AreEqual(NodalOsEvidenceBridgeAuthority.DiagnosticOnly, result.Evidence.Authority);
        Assert.IsFalse(json.Contains("fakeBearerToken1234567890", StringComparison.Ordinal));
    }

    [TestMethod]
    public void RedactionRequiredState_BlocksAcceptedWhenConfigured()
    {
        var result = bridge.BridgeFromEvidenceRef(
            Evidence(@ref: "password=fake-password-value"),
            NodalOsEvidenceBridgeSourceKind.Manual,
            NodalOsEvidenceBridgeUseKind.Auxiliary,
            new NodalOsEvidenceBridgeOptions
            {
                RequireRedactionForPotentiallySensitive = true,
                RejectSensitiveWithoutRedaction = true
            });

        Assert.IsFalse(result.Accepted);
        Assert.AreEqual(NodalOsEvidenceRedactionState.RedactionRequired, result.Evidence.RedactionState);
    }

    [TestMethod]
    public void NonSensitiveEvidence_AcceptsWithoutRedaction()
    {
        var result = bridge.BridgeFromEvidenceRef(
            Evidence(@ref: "artifact://safe-report.json"),
            NodalOsEvidenceBridgeSourceKind.AgentOperation,
            NodalOsEvidenceBridgeUseKind.Auxiliary);

        Assert.IsTrue(result.Accepted);
        Assert.AreEqual(NodalOsEvidenceSensitivity.NonSensitive, result.Evidence.Sensitivity);
        Assert.AreEqual(NodalOsEvidenceRedactionState.NotRequired, result.Evidence.RedactionState);
    }

    [TestMethod]
    public void NoUiOrRuntimeActionsIntroduced()
    {
        var artifactPath = Path.Combine(
            FindRepoRoot(),
            "artifacts",
            "core",
            "m385",
            "evidenceref-ledger-bridge-summary.json");

        Assert.IsTrue(File.Exists(artifactPath));
        var json = File.ReadAllText(artifactPath);

        StringAssert.Contains(json, "\"noRuntimeBehaviorChange\": true");
        StringAssert.Contains(json, "\"noUiImplemented\": true");
        StringAssert.Contains(json, "\"noRecipeExecutionImplemented\": true");
        StringAssert.Contains(json, "\"noOrchestrationApiImplemented\": true");
        StringAssert.Contains(json, "\"noPersistenceImplemented\": true");
    }

    private static NexaEvidenceRef Evidence(
        string id = "evidence-bridge-001",
        string kind = "run-report",
        string? @ref = "audit:audit-ledger-test",
        string? hash = "sha256-test") =>
        new()
        {
            EvidenceId = id,
            Kind = kind,
            Ref = @ref,
            Hash = hash,
            CreatedAt = FixedTimestamp
        };

    private static string FindRepoRoot()
    {
        var directory = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "OneBrain.slnx")))
                return directory.FullName;

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate repository root.");
    }
}
