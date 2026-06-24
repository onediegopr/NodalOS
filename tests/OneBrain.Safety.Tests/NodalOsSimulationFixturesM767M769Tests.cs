using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("SimulationFixtures")]
[TestCategory("M767")]
[TestCategory("M768")]
[TestCategory("M769")]
public sealed class NodalOsSimulationFixturesM767M769Tests
{
    private static readonly string[] FixturePaths =
    [
        "artifacts/agent-operations/m767/fake-provider-fixture.json",
        "artifacts/agent-operations/m767/fake-local-model-fixture.json",
        "artifacts/agent-operations/m767/fake-filesystem-read-fixture.json",
        "artifacts/agent-operations/m767/fake-extension-bridge-fixture.json",
        "artifacts/agent-operations/m767/fake-websocket-bridge-fixture.json",
        "artifacts/agent-operations/m767/fake-evidence-ledger-fixture.json",
        "artifacts/agent-operations/m767/fake-policy-approval-fixture.json",
        "artifacts/agent-operations/m767/fake-redaction-payload-fixture.json"
    ];

    private static readonly string[] RequiredNegativeCases =
    [
        "attempt_live_provider_call",
        "attempt_provider_credential_use",
        "attempt_filesystem_write",
        "attempt_browser_action",
        "attempt_credential_captcha_2fa_bypass",
        "attempt_capability_unlock",
        "attempt_public_release",
        "attempt_chrome_web_store_submission",
        "attempt_signed_public_zip_creation",
        "attempt_product_file_modification",
        "attempt_bridge_csp_modification"
    ];

    private static readonly string[] ForbiddenFields =
    [
        "secrets",
        "credentials",
        "tokens",
        "cookies",
        "rawUserData",
        "rawLogs",
        "providerKeys",
        "privateKeys",
        "browserSessionData"
    ];

    private const string FixturesSummaryPath = "artifacts/agent-operations/m767/simulation-harness-test-fixtures.json";
    private const string FixturePayloadPath = "artifacts/agent-operations/m767/fake-redaction-payload-fixture.json";
    private const string NoExecutionSuitePath = "artifacts/agent-operations/m768/no-execution-proof-suite.json";
    private const string RejectionCasesPath = "artifacts/agent-operations/m768/prohibited-action-rejection-cases.json";
    private const string RejectionAuditEventsPath = "artifacts/agent-operations/m768/rejection-audit-events.json";
    private const string NoExecutionFlagsByCasePath = "artifacts/agent-operations/m768/no-execution-proof-flags-by-case.json";
    private const string RedactionProofPath = "artifacts/agent-operations/m768/redaction-synthetic-payload-proof.json";
    private const string ForbiddenFieldsProofPath = "artifacts/agent-operations/m768/forbidden-fields-redaction-proof.json";
    private const string ClaudeRemediationPath = "artifacts/agent-operations/m769/claude-audit-remediation-status.json";
    private const string ProductBridgeCspPath = "artifacts/agent-operations/m769/product-bridge-csp-unchanged-proof.json";
    private const string NextMilestonePath = "artifacts/agent-operations/m769/next-milestone-recommendation.json";
    private const string ConsolidatedPath = "artifacts/agent-operations/m767-m769/simulation-fixtures-no-execution-go-no-go.json";

    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(Environment.CurrentDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "OneBrain.slnx")))
            dir = dir.Parent;

        return dir?.FullName ?? Environment.CurrentDirectory;
    }

    private static string FullPath(string relativePath) =>
        Path.Combine(RepoRoot(), relativePath);

    private static JsonDocument ReadJson(string relativePath) =>
        JsonDocument.Parse(File.ReadAllText(FullPath(relativePath)));

    [TestMethod]
    public void SimulationFixtureArtifactsExist()
    {
        foreach (var path in FixturePaths.Concat(new[]
        {
            FixturesSummaryPath,
            NoExecutionSuitePath,
            RejectionCasesPath,
            RejectionAuditEventsPath,
            NoExecutionFlagsByCasePath,
            RedactionProofPath,
            ForbiddenFieldsProofPath,
            ClaudeRemediationPath,
            ProductBridgeCspPath,
            NextMilestonePath,
            ConsolidatedPath
        }))
            Assert.IsTrue(File.Exists(FullPath(path)), path);
    }

    [TestMethod]
    public void EveryFixtureIsSimulatedFakeOnlyAndForbidsRealActions()
    {
        foreach (var path in FixturePaths)
        {
            using var doc = ReadJson(path);
            var root = doc.RootElement;
            Assert.AreEqual("SIMULATED_FAKE_ONLY", root.GetProperty("fixtureType").GetString(), path);
            Assert.IsFalse(root.GetProperty("realExecutionAllowed").GetBoolean(), path);
            Assert.IsFalse(root.GetProperty("liveCallAllowed").GetBoolean(), path);
            Assert.IsFalse(root.GetProperty("credentialUseAllowed").GetBoolean(), path);
            Assert.IsFalse(root.GetProperty("filesystemWriteAllowed").GetBoolean(), path);
            Assert.IsFalse(root.GetProperty("browserActionAllowed").GetBoolean(), path);
            Assert.IsFalse(root.GetProperty("capabilityUnlockAllowed").GetBoolean(), path);
            Assert.IsFalse(root.GetProperty("publicReleaseAllowed").GetBoolean(), path);
            Assert.IsFalse(root.GetProperty("storeSubmissionAllowed").GetBoolean(), path);
            Assert.IsFalse(root.GetProperty("productFilesModificationAllowed").GetBoolean(), path);
            Assert.IsFalse(root.GetProperty("bridgeCspModificationAllowed").GetBoolean(), path);
        }
    }

    [TestMethod]
    public void EveryNegativeCaseReturnsDeny()
    {
        using var doc = ReadJson(RejectionCasesPath);
        var cases = doc.RootElement.GetProperty("caseResults").EnumerateArray().ToArray();

        foreach (var requiredCase in RequiredNegativeCases)
        {
            var match = cases.Single(x => x.GetProperty("caseId").GetString() == requiredCase);
            Assert.AreEqual("DENY", match.GetProperty("decision").GetString(), requiredCase);
        }
    }

    [TestMethod]
    public void EveryNegativeCaseRequiresAuditEventAndEvidenceEnvelope()
    {
        using var doc = ReadJson(RejectionCasesPath);

        foreach (var item in doc.RootElement.GetProperty("caseResults").EnumerateArray())
        {
            var caseId = item.GetProperty("caseId").GetString() ?? string.Empty;
            Assert.IsTrue(item.GetProperty("auditEventRequired").GetBoolean(), caseId);
            Assert.IsTrue(item.GetProperty("evidenceEnvelopeRequired").GetBoolean(), caseId);
        }
    }

    [TestMethod]
    public void EveryNegativeCaseKeepsNoExecutionFlagsFalse()
    {
        using var doc = ReadJson(RejectionCasesPath);

        foreach (var item in doc.RootElement.GetProperty("caseResults").EnumerateArray())
        {
            var caseId = item.GetProperty("caseId").GetString() ?? string.Empty;
            Assert.IsFalse(item.GetProperty("actualExecutionPerformed").GetBoolean(), caseId);
            Assert.IsFalse(item.GetProperty("liveCallPerformed").GetBoolean(), caseId);
            Assert.IsFalse(item.GetProperty("filesystemWritePerformed").GetBoolean(), caseId);
            Assert.IsFalse(item.GetProperty("browserAutomationPerformed").GetBoolean(), caseId);
            Assert.IsFalse(item.GetProperty("capabilityUnlocked").GetBoolean(), caseId);
            Assert.IsFalse(item.GetProperty("publicReleasePerformed").GetBoolean(), caseId);
            Assert.IsFalse(item.GetProperty("storeSubmissionPerformed").GetBoolean(), caseId);
            Assert.IsFalse(item.GetProperty("signedPublicZipCreated").GetBoolean(), caseId);
            Assert.IsFalse(item.GetProperty("productFilesModified").GetBoolean(), caseId);
            Assert.IsFalse(item.GetProperty("bridgeCspModified").GetBoolean(), caseId);
        }
    }

    [TestMethod]
    public void NoExecutionFlagsByCaseMirrorRequiredFalseValues()
    {
        using var doc = ReadJson(NoExecutionFlagsByCasePath);
        var caseFlags = doc.RootElement.GetProperty("caseFlags").EnumerateArray().ToArray();

        foreach (var requiredCase in RequiredNegativeCases)
        {
            var item = caseFlags.Single(x => x.GetProperty("caseId").GetString() == requiredCase);
            Assert.IsFalse(item.GetProperty("actualExecutionPerformed").GetBoolean(), requiredCase);
            Assert.IsFalse(item.GetProperty("liveCallPerformed").GetBoolean(), requiredCase);
            Assert.IsFalse(item.GetProperty("filesystemWritePerformed").GetBoolean(), requiredCase);
            Assert.IsFalse(item.GetProperty("browserAutomationPerformed").GetBoolean(), requiredCase);
            Assert.IsFalse(item.GetProperty("capabilityUnlocked").GetBoolean(), requiredCase);
            Assert.IsFalse(item.GetProperty("publicReleasePerformed").GetBoolean(), requiredCase);
            Assert.IsFalse(item.GetProperty("storeSubmissionPerformed").GetBoolean(), requiredCase);
            Assert.IsFalse(item.GetProperty("signedPublicZipCreated").GetBoolean(), requiredCase);
            Assert.IsFalse(item.GetProperty("productFilesModified").GetBoolean(), requiredCase);
            Assert.IsFalse(item.GetProperty("bridgeCspModified").GetBoolean(), requiredCase);
        }
    }

    [TestMethod]
    public void RedactionSyntheticPayloadContainsAllForbiddenFieldsAsFakePlaceholders()
    {
        using var doc = ReadJson(FixturePayloadPath);
        var fields = doc.RootElement.GetProperty("forbiddenFields");

        foreach (var forbiddenField in ForbiddenFields)
        {
            var value = fields.GetProperty(forbiddenField).GetString() ?? string.Empty;
            StringAssert.StartsWith(value, "FAKE_");
        }
    }

    [TestMethod]
    public void RedactionProofMarksForbiddenFieldsExcludedFromOutput()
    {
        using var redaction = ReadJson(RedactionProofPath);
        using var forbidden = ReadJson(ForbiddenFieldsProofPath);
        var outputFlags = forbidden.RootElement.GetProperty("outputFlags");

        Assert.IsFalse(redaction.RootElement.GetProperty("secretsIncluded").GetBoolean());
        Assert.IsFalse(redaction.RootElement.GetProperty("credentialsIncluded").GetBoolean());
        Assert.IsFalse(redaction.RootElement.GetProperty("tokensIncluded").GetBoolean());
        Assert.IsFalse(redaction.RootElement.GetProperty("cookiesIncluded").GetBoolean());
        Assert.IsFalse(redaction.RootElement.GetProperty("rawUserDataIncluded").GetBoolean());
        Assert.IsFalse(redaction.RootElement.GetProperty("rawLogsIncluded").GetBoolean());
        Assert.IsFalse(redaction.RootElement.GetProperty("providerKeysIncluded").GetBoolean());
        Assert.IsFalse(redaction.RootElement.GetProperty("privateKeysIncluded").GetBoolean());
        Assert.IsFalse(redaction.RootElement.GetProperty("browserSessionDataIncluded").GetBoolean());

        foreach (var forbiddenField in ForbiddenFields)
            CollectionAssert.Contains(
                forbidden.RootElement.GetProperty("forbiddenFieldsCovered").EnumerateArray().Select(x => x.GetString()).ToArray(),
                forbiddenField);

        Assert.IsFalse(outputFlags.GetProperty("secretsIncluded").GetBoolean());
        Assert.IsFalse(outputFlags.GetProperty("credentialsIncluded").GetBoolean());
        Assert.IsFalse(outputFlags.GetProperty("tokensIncluded").GetBoolean());
        Assert.IsFalse(outputFlags.GetProperty("cookiesIncluded").GetBoolean());
        Assert.IsFalse(outputFlags.GetProperty("rawUserDataIncluded").GetBoolean());
        Assert.IsFalse(outputFlags.GetProperty("rawLogsIncluded").GetBoolean());
        Assert.IsFalse(outputFlags.GetProperty("providerKeysIncluded").GetBoolean());
        Assert.IsFalse(outputFlags.GetProperty("privateKeysIncluded").GetBoolean());
        Assert.IsFalse(outputFlags.GetProperty("browserSessionDataIncluded").GetBoolean());
    }

    [TestMethod]
    public void RuntimeReleaseStoreProductBridgeAndCspRemainBlocked()
    {
        using var consolidated = ReadJson(ConsolidatedPath);
        using var productBridge = ReadJson(ProductBridgeCspPath);

        Assert.AreEqual("DISABLED", consolidated.RootElement.GetProperty("runtimeProductiveExecution").GetString());
        Assert.AreEqual("DISABLED", consolidated.RootElement.GetProperty("providerCloudLiveCalls").GetString());
        Assert.AreEqual("DISABLED", consolidated.RootElement.GetProperty("filesystemBrowserCapabilityUnlock").GetString());
        Assert.AreEqual("NO-GO", consolidated.RootElement.GetProperty("publicRelease").GetString());
        Assert.AreEqual("NO-GO", consolidated.RootElement.GetProperty("chromeWebStore").GetString());
        Assert.IsFalse(productBridge.RootElement.GetProperty("productFilesModified").GetBoolean());
        Assert.IsFalse(productBridge.RootElement.GetProperty("bridgeModified").GetBoolean());
        Assert.IsFalse(productBridge.RootElement.GetProperty("cspModified").GetBoolean());
        Assert.IsFalse(productBridge.RootElement.GetProperty("bridgeCspModified").GetBoolean());
    }

    [TestMethod]
    public void ClaudeM1IsAddressedAtSimulationLevelOnly()
    {
        using var doc = ReadJson(ClaudeRemediationPath);

        Assert.AreEqual("M-1", doc.RootElement.GetProperty("findingId").GetString());
        Assert.AreEqual("ADDRESSED_FOR_SIMULATION_LEVEL", doc.RootElement.GetProperty("status").GetString());
        Assert.IsFalse(doc.RootElement.GetProperty("productiveUnlockAllowed").GetBoolean());
    }

    [TestMethod]
    public void NextMilestoneIsSimulatedOrchestratorContract()
    {
        using var doc = ReadJson(NextMilestonePath);

        Assert.AreEqual(
            "M770-M772 Simulated Dry-Run Orchestrator Contract + Ledger Event Projection",
            doc.RootElement.GetProperty("recommendedNextMilestone").GetString());
        Assert.AreEqual("simulation/contracts/tests only", doc.RootElement.GetProperty("scope").GetString());
        Assert.IsFalse(doc.RootElement.GetProperty("productiveUnlockAllowed").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("publicReleaseAllowed").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("storeSubmissionAllowed").GetBoolean());
    }

    [TestMethod]
    public void FinalDecisionIsSimulationFixturesNoExecutionProofReady()
    {
        using var doc = ReadJson(ConsolidatedPath);

        Assert.AreEqual("SIMULATION_HARNESS_FIXTURES_NO_EXECUTION_PROOF_READY", doc.RootElement.GetProperty("decision").GetString());
        Assert.AreEqual("ADDRESSED_FOR_SIMULATION_LEVEL", doc.RootElement.GetProperty("claudeAuditM1").GetString());
        Assert.AreEqual("OWNER_ATTESTATION_NON_AUDITABLE", doc.RootElement.GetProperty("evidenceClass").GetString());
        Assert.IsFalse(doc.RootElement.GetProperty("evidenceInvented").GetBoolean());
        Assert.AreEqual("PROHIBITED", doc.RootElement.GetProperty("productiveEnabled").GetString());
        Assert.IsFalse(doc.RootElement.GetProperty("actualExecutionPerformed").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("liveCallPerformed").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("filesystemWritePerformed").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("browserAutomationPerformed").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("capabilityUnlocked").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("publicReleasePerformed").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("storeSubmissionPerformed").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("signedPublicZipCreated").GetBoolean());
    }
}
