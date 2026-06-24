using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("FocusedReAudit")]
[TestCategory("M869")]
[TestCategory("M870")]
[TestCategory("M871")]
[TestCategory("M872")]
[TestCategory("M869M872")]
public sealed class NodalOsFocusedReAuditM869M872Tests
{
    private const string PackagePath = "artifacts/agent-operations/m869/focused-f1-f2-re-audit-package.json";
    private const string PromptPath = "artifacts/agent-operations/m870/focused-re-audit-prompt.json";
    private const string MatrixPath = "artifacts/agent-operations/m871/re-audit-result-intake-eligibility-matrix.json";
    private const string ResultPath = "artifacts/agent-operations/m871/focused-re-audit-result.json";
    private const string SummaryPath = "artifacts/agent-operations/m872/re-audit-summary-next-decision.json";
    private const string GoNoGoPath = "artifacts/agent-operations/m869-m872/focused-re-audit-freeze-eligibility-go-no-go.json";

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
    public void ReAuditPackageExistsAndReferencesAuditedCommitAndDiffScope()
    {
        var content = ReadAll(PackagePath);

        StringAssert.Contains(content, "\"packageType\": \"FOCUSED_F1_F2_REAUDIT_PACKAGE\"");
        StringAssert.Contains(content, "b7457b86d4d5704c7567fc67035c8117fc54e5c0");
        StringAssert.Contains(content, "fe0f63b80c0e83b8a514ff30b1754d467ebf10de");
        StringAssert.Contains(content, "tests/safety, docs/reports, artifacts/agent-operations only");
        StringAssert.Contains(content, "SimulatedDryRunOrchestrator.cs");
        StringAssert.Contains(content, "SimulatedRedactor.cs");
    }

    [TestMethod]
    public void ReAuditPackageDocumentsF1MeasuredSinkRemediation()
    {
        var content = ReadAll(PackagePath);

        StringAssert.Contains(content, "NoExecutionProof.SideEffectSinkInvocations derives from RecordingSideEffectSink.InvocationCount");
        StringAssert.Contains(content, "NoExecutionProof.FromSink");
        StringAssert.Contains(content, "Tests inject side-effect records");
        StringAssert.Contains(content, "Does NoExecutionProof now derive from a real RecordingSideEffectSink?");
    }

    [TestMethod]
    public void ReAuditPackageDocumentsF2AdversarialRedactionRemediation()
    {
        var content = ReadAll(PackagePath);

        foreach (var fakeValue in new[]
        {
            "FAKE_SECRET_NODAL_OS_TEST_TOKEN_123",
            "FAKE_PROVIDER_KEY_SHOULD_BE_REDACTED",
            "FAKE_COOKIE_SESSION_SHOULD_BE_REDACTED",
            "FAKE_PRIVATE_KEY_SHOULD_BE_REDACTED",
            "FAKE_BROWSER_SESSION_DATA_SHOULD_BE_REDACTED"
        })
        {
            StringAssert.Contains(content, fakeValue);
        }

        StringAssert.Contains(content, "redaction-enabled and redaction-disabled paths");
        StringAssert.Contains(content, "Does final export remove raw values?");
    }

    [TestMethod]
    public void ReAuditPromptIncludesAllowedDecisionOptionsAndScopeChecks()
    {
        var content = ReadAll(PromptPath);

        foreach (var decision in ReAuditDecisions.All)
            StringAssert.Contains(content, decision);

        StringAssert.Contains(content, "confirm F1 is measured and not hardcoded");
        StringAssert.Contains(content, "confirm F2 uses fake secret-like payloads");
        StringAssert.Contains(content, "prohibited paths were not touched");
        StringAssert.Contains(content, "freeze lock remains NO-GO until audited decision");
    }

    [TestMethod]
    public void ReAuditResultIntakeMatrixBlocksNoGoAndMissingResult()
    {
        var content = ReadAll(MatrixPath);

        StringAssert.Contains(content, "\"decision\": \"REAUDIT_GO_F1_F2_REMEDIATED\"");
        StringAssert.Contains(content, "\"freezeLockEligible\": true");
        foreach (var blocked in ReAuditDecisions.Blocked)
            StringAssert.Contains(content, blocked);

        StringAssert.Contains(content, "\"decision\": \"NO_REAUDIT_RESULT\"");
        StringAssert.Contains(content, "\"freezeLockEligible\": false");
    }

    [TestMethod]
    public void FocusedReAuditResultRegistersGoButDoesNotActivateFreezeLock()
    {
        var content = ReadAll(ResultPath);

        StringAssert.Contains(content, "\"decision\": \"REAUDIT_GO_F1_F2_REMEDIATED\"");
        StringAssert.Contains(content, "\"f1Status\": \"REMEDIATED_MEASURED_SINK_PROOF\"");
        StringAssert.Contains(content, "\"f2Status\": \"REMEDIATED_ADVERSARIAL_REDACTION_PROOF\"");
        StringAssert.Contains(content, "\"freezeLockEligible\": true");
        StringAssert.Contains(content, "\"freezeLockActivated\": false");
        StringAssert.Contains(content, "Proceed to M873-M884");
    }

    [TestMethod]
    public void SummaryKeepsNoGoBoundariesAndRecommendsFreezeLockMacroHito()
    {
        var content = ReadAll(SummaryPath);

        StringAssert.Contains(content, "\"finalDecision\": \"REAUDIT_READY_FOR_FREEZE_LOCK\"");
        StringAssert.Contains(content, "\"freezeLockEligibility\": \"ELIGIBLE_NOT_ACTIVATED\"");
        StringAssert.Contains(content, "\"runtimeProductiveExecution\": \"DISABLED\"");
        StringAssert.Contains(content, "\"providerCloudLiveCalls\": \"DISABLED\"");
        StringAssert.Contains(content, "\"filesystemBrowserCapabilityUnlock\": \"DISABLED\"");
        StringAssert.Contains(content, "\"publicRelease\": \"NO-GO\"");
        StringAssert.Contains(content, "\"chromeWebStore\": \"NO-GO\"");
        StringAssert.Contains(content, "M873-M884");
    }

    [TestMethod]
    public void GoNoGoRecordsEligibilityWithoutForbiddenUnlocks()
    {
        var content = ReadAll(GoNoGoPath);

        StringAssert.Contains(content, "\"decision\": \"REAUDIT_READY_FOR_FREEZE_LOCK\"");
        StringAssert.Contains(content, "\"freezeLockEligible\": true");
        StringAssert.Contains(content, "\"freezeLockActivated\": false");
        StringAssert.Contains(content, "\"runtimeProductiveExecution\": \"DISABLED\"");
        StringAssert.Contains(content, "\"providerCloudLiveCalls\": \"DISABLED\"");
        StringAssert.Contains(content, "\"publicRelease\": \"NO-GO\"");
        StringAssert.Contains(content, "\"signedPublicZipCreated\": false");
        StringAssert.Contains(content, "\"productFilesModified\": false");
        StringAssert.Contains(content, "\"bridgeCspModified\": false");
    }

    [TestMethod]
    public void NoProductiveOrReleaseReadyClaimAppearsInFocusedReAuditArtifacts()
    {
        foreach (var path in new[] { PackagePath, PromptPath, MatrixPath, ResultPath, SummaryPath, GoNoGoPath })
        {
            var content = ReadAll(path);
            Assert.IsFalse(content.Contains("\"decision\": \"PRODUCTIVE_ENABLED\"", StringComparison.Ordinal), path);
            Assert.IsFalse(content.Contains("\"decision\": \"PUBLIC_RELEASE_READY\"", StringComparison.Ordinal), path);
            Assert.IsFalse(content.Contains("\"decision\": \"CHROME_WEB_STORE_READY\"", StringComparison.Ordinal), path);
            Assert.IsFalse(content.Contains("\"decision\": \"LIVE_CALL_ENABLED\"", StringComparison.Ordinal), path);
            Assert.IsFalse(content.Contains("\"decision\": \"FILESYSTEM_WRITE_ENABLED\"", StringComparison.Ordinal), path);
            Assert.IsFalse(content.Contains("\"decision\": \"BROWSER_AUTOMATION_ENABLED\"", StringComparison.Ordinal), path);
            Assert.IsFalse(content.Contains("\"decision\": \"CAPABILITY_UNLOCKED\"", StringComparison.Ordinal), path);
            Assert.IsFalse(content.Contains("\"decision\": \"SIGNED_PUBLIC_ZIP_CREATED\"", StringComparison.Ordinal), path);
        }
    }

    [TestMethod]
    public void ProductFilesAndBridgeCspRemainOutOfScope()
    {
        foreach (var path in new[] { PackagePath, MatrixPath, ResultPath, SummaryPath, GoNoGoPath })
        {
            var content = ReadAll(path);
            StringAssert.Contains(content, "\"productFilesModified\": false");
            StringAssert.Contains(content, "\"bridgeCspModified\": false");
        }
    }

    private static class ReAuditDecisions
    {
        public static readonly string[] All =
        [
            "REAUDIT_GO_F1_F2_REMEDIATED",
            "REAUDIT_CONDITIONAL_GO_MINOR_QUALITY_NOTES",
            "REAUDIT_NO_GO_F1_STILL_TAUTOLOGICAL",
            "REAUDIT_NO_GO_F2_STILL_THEATER",
            "REAUDIT_NO_GO_SCOPE_OR_SAFETY_DRIFT",
            "REAUDIT_NO_GO_TEST_QUALITY_INSUFFICIENT"
        ];

        public static readonly string[] Blocked =
        [
            "REAUDIT_NO_GO_F1_STILL_TAUTOLOGICAL",
            "REAUDIT_NO_GO_F2_STILL_THEATER",
            "REAUDIT_NO_GO_SCOPE_OR_SAFETY_DRIFT",
            "REAUDIT_NO_GO_TEST_QUALITY_INSUFFICIENT"
        ];
    }
}
