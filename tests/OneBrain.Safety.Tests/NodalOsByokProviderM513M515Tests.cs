using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.AgentOperations.Contracts;
using OneBrain.AgentOperations.Core;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("ByokProvider")]
[TestCategory("ProjectUnderstandingPolicy")]
[TestCategory("ContextIntakePreview")]
[TestCategory("UserContext")]
[TestCategory("WorkspaceReadinessContext")]
[TestCategory("WorkspaceMetadataHealth")]
[TestCategory("WorkspaceStorageMissionSwitcher")]
[TestCategory("WorkspaceLocalModel")]
[TestCategory("MissionControlVisualPolish")]
[TestCategory("MissionControlGuidance")]
[TestCategory("MissionControlInteractionNoOp")]
[TestCategory("MissionControlShellReadOnly")]
[TestCategory("AuditAPreUiBoundaryNaming")]
[TestCategory("ApprovalUxHandoffObservability")]
[TestCategory("ApprovalTimelineEvidence")]
[TestCategory("CoreRuntimeRegistryEventBusRedaction")]
[TestCategory("NewTopicsIntake")]
[TestCategory("NamingCleanup")]
public sealed class NodalOsByokProviderM513M515Tests
{
    private static readonly string[] ForbiddenNames = ["Nexa", "NEXA", "NODRIX", "HOTEP"];
    private static readonly string[] SensitiveMarkers = ["Bearer ", "Authorization:", "Cookie:", "password", "secret", "api_key", "access_token", "refresh_token", "id_token", "private key", "sk-", "connection string"];
    private readonly NodalOsByokProviderSettingsService service = new();
    private readonly NodalOsByokProviderJsonSerializer serializer = new();

    [TestMethod]
    public void ByokProviderSettings_CreateAllKeyStatusesAsReferenceOnly()
    {
        foreach (var status in Enum.GetValues<NodalOsByokProviderKeyStatus>())
        {
            var settings = service.CreateProviderSettings(NodalOsByokProviderKind.OpenAiFuture, status);

            Assert.AreEqual(status, settings.ProviderKeyStatus);
            Assert.IsTrue(settings.ReferenceOnly);
            Assert.IsFalse(settings.StoresRawCredential);
            Assert.IsFalse(settings.CallsProvider);
            Assert.IsFalse(settings.SendsNetworkRequest);
            Assert.IsFalse(settings.CreatesPrompt);
            Assert.IsFalse(settings.CreatesEmbeddings);
            Assert.IsFalse(settings.RoutesLlmTraffic);
            Assert.IsFalse(settings.CallsCloud);
            Assert.IsFalse(settings.CanAuthorizeExecution);
            AssertSafeOutput(serializer.SerializeProviderSettings(settings));
        }
    }

    [TestMethod]
    public void ByokProviderSettings_ModelProviderKindsCapabilitiesAndDisabledCapabilities()
    {
        var requiredKinds = new[]
        {
            NodalOsByokProviderKind.OpenAiFuture,
            NodalOsByokProviderKind.AnthropicFuture,
            NodalOsByokProviderKind.GeminiFuture,
            NodalOsByokProviderKind.LocalModelFuture,
            NodalOsByokProviderKind.OllamaFuture,
            NodalOsByokProviderKind.LmStudioFuture,
            NodalOsByokProviderKind.CustomOpenAiCompatibleFuture,
            NodalOsByokProviderKind.Unknown
        };

        foreach (var kind in requiredKinds)
        {
            var settings = service.CreateProviderSettings(kind);

            Assert.AreEqual(kind, settings.ProviderKind);
            CollectionAssert.Contains(settings.CapabilitiesDeclared.ToList(), NodalOsByokProviderCapability.ChatFuture);
            CollectionAssert.Contains(settings.CapabilitiesDeclared.ToList(), NodalOsByokProviderCapability.ReasoningFuture);
            CollectionAssert.Contains(settings.CapabilitiesDeclared.ToList(), NodalOsByokProviderCapability.ProjectUnderstandingFuture);
            CollectionAssert.Contains(settings.CapabilitiesDeclared.ToList(), NodalOsByokProviderCapability.AssignmentFuture);
            CollectionAssert.Contains(settings.CapabilitiesDeclared.ToList(), NodalOsByokProviderCapability.AdvisorFuture);
            Assert.IsTrue(settings.DisabledCapabilitiesRedacted.Count > 0);
        }
    }

    [TestMethod]
    public void ByokProviderSettings_SanitizesAdversarialCredentialAndEndpointValues()
    {
        var unsafeValues = new[]
        {
            "Bearer abcdefghijklmnop",
            "Authorization: value",
            "Cookie: session=value",
            "password=value",
            "secret=value",
            "api_key=value",
            "access_token=value",
            "refresh_token=value",
            "id_token=value",
            "private key",
            "sk-test-value",
            "connection string"
        };

        foreach (var unsafeValue in unsafeValues)
        {
            var settings = service.CreateProviderSettings(
                NodalOsByokProviderKind.CustomOpenAiCompatibleFuture,
                credentialReferencePlaceholder: unsafeValue,
                endpointPolicyPlaceholder: unsafeValue);
            var json = serializer.SerializeProviderSettings(settings);

            AssertSafeOutput(json);
            StringAssert.Contains(json, "redacted-value");
        }
    }

    [TestMethod]
    public void SecretStoragePolicyAdr_ExistsAndDefinesRequiredPolicy()
    {
        var adr = System.IO.File.ReadAllText(PathFor("docs", "architecture", "nodal-os-secret-storage-policy-decision-record.md"));

        AssertContains(adr, "NODAL_OS_SECRET_STORAGE_POLICY_DEFINED");
        AssertContains(adr, "will not store raw secrets in JSON, logs, artifacts, reports");
        AssertContains(adr, "secure store or vault");
        AssertContains(adr, "OS keychain or credential manager");
        AssertContains(adr, "Cloud secret storage is prohibited by default");
        AssertContains(adr, "Provider test connection uses a credential reference");
        AssertContains(adr, "Errors must be redacted");
        AssertContains(adr, "No secure store implementation");
        AssertContains(adr, "No provider calls");
        AssertContains(adr, "No network or HTTP");
    }

    [TestMethod]
    public void ProviderTestConnection_CreateAllRequiredStatesAsDisabledMockOnly()
    {
        var settings = NodalOsByokProviderFixtures.ReferenceOnlySettings();

        foreach (var state in Enum.GetValues<NodalOsProviderTestConnectionState>())
        {
            var preview = service.CreateTestConnectionPreview(settings, state);

            Assert.AreEqual(state, preview.State);
            Assert.IsTrue(preview.ActionDisabled);
            Assert.IsTrue(preview.MockOnly);
            Assert.IsFalse(preview.PerformsNetworkRequest);
            Assert.IsFalse(preview.UsesProviderSdk);
            Assert.IsFalse(preview.ReadsEnvironmentVariables);
            Assert.IsFalse(preview.CreatesPrompt);
            Assert.IsFalse(preview.CallsLlmProvider);
            Assert.IsFalse(preview.StoresRawCredential);
            Assert.IsFalse(preview.CanAuthorizeExecution);
            AssertSafeOutput(serializer.SerializeTestConnectionPreview(preview));
        }
    }

    [TestMethod]
    public void ProviderTestConnection_RequestPreviewContainsOnlyRefsAndRedactedExplanation()
    {
        var preview = NodalOsByokProviderFixtures.MockOnlyTestConnection();

        Assert.IsFalse(string.IsNullOrWhiteSpace(preview.ProviderSettingsRef));
        AssertContains(preview.CredentialRefStatusRedacted, "credential-ref");
        Assert.IsTrue(preview.PreflightChecksRedacted.Count >= 5);
        AssertContains(preview.NetworkDisabledStatusRedacted, "disabled");
        AssertContains(preview.DryRunMockStatusRedacted, "Mock-only");
        AssertContains(preview.ErrorRedactionPolicyRedacted, "redacted");
        Assert.IsTrue(preview.EvidenceRefs.Count > 0);
        Assert.IsTrue(preview.TimelineRefs.Count > 0);
        Assert.IsTrue(preview.ObservabilityRefs.Count > 0);
        Assert.IsTrue(preview.GuardrailRefs.Count > 0);
    }

    [TestMethod]
    public void ProviderTestConnection_SanitizesAdversarialEndpointTargets()
    {
        var settings = NodalOsByokProviderFixtures.ReferenceOnlySettings();
        var unsafeValues = new[]
        {
            "Bearer abcdefghijklmnop",
            "Authorization: value",
            "Cookie: session=value",
            "password=value",
            "secret=value",
            "api_key=value",
            "access_token=value",
            "refresh_token=value",
            "id_token=value",
            "private key",
            "sk-test-value",
            "connection string"
        };

        foreach (var unsafeValue in unsafeValues)
        {
            var preview = service.CreateTestConnectionPreview(settings, endpointTarget: unsafeValue);

            AssertSafeOutput(serializer.SerializeTestConnectionPreview(preview));
            Assert.AreEqual("redacted-value", preview.EndpointTargetRedacted);
        }
    }

    [TestMethod]
    public void Boundary_NewByokProviderFiles_DoNotReferenceForbiddenRuntimeOrProviderPrimitives()
    {
        var source = NewSource();

        AssertDoesNotContain(source, "OneBrain.BrowserExecutor.Cdp");
        AssertDoesNotContain(source, "HttpClient");
        AssertDoesNotContain(source, "ClientWebSocket");
        AssertDoesNotContain(source, "Process.Start");
        AssertDoesNotContain(source, "System.Diagnostics.Process");
        AssertDoesNotContain(source, "BackgroundService");
        AssertDoesNotContain(source, "Task.Run");
        AssertDoesNotContain(source, "IHostedService");
        AssertDoesNotContain(source, "QueueClient");
        AssertDoesNotContain(source, "RecorderRuntime");
        AssertDoesNotContain(source, "ReplayRuntime");
        AssertDoesNotContain(source, "DslParserRuntime");
        AssertDoesNotContain(source, "ProviderClient");
        AssertDoesNotContain(source, "ProviderSdkClient");
        AssertDoesNotContain(source, "CloudSync");
        AssertDoesNotContain(source, "Environment.GetEnvironmentVariable");
        AssertDoesNotContain(source, "File.Write");
        AssertDoesNotContain(source, "File.Read");
        AssertDoesNotContain(source, "Directory.");
        AssertDoesNotContain(source, "git ");
        AssertDoesNotContain(source, "embeddings client");
    }

    [TestMethod]
    public void ExistingSafetyContinuity_ProjectUnderstandingPolicyAndMissionControlRemainNoAuthority()
    {
        var governance = NodalOsProjectUnderstandingPolicyFixtures.FutureLlmGovernance();
        var contextSet = NodalOsContextIntakePreviewFixtures.SafeContextIntakeSet();
        var shell = NodalOsMissionControlShellFixtures.ShellPreview();

        Assert.IsFalse(governance.CreatesPrompt);
        Assert.IsFalse(governance.CallsLlmProvider);
        Assert.IsFalse(governance.CanAuthorizeExecution);
        Assert.IsFalse(contextSet.Report.CanAuthorizeExecution);
        Assert.IsFalse(contextSet.Report.CallsLlmProvider);
        Assert.IsFalse(contextSet.Report.CreatesPrompt);
        Assert.IsFalse(shell.RuntimeExecutionAllowed);
        Assert.IsTrue(shell.ReadOnlyUi);
    }

    [TestMethod]
    public void ArtifactMarksByokProviderPolicyReady()
    {
        var artifact = System.IO.File.ReadAllText(PathFor("artifacts", "agent-operations", "m515", "byok-provider-settings-summary.json"));

        AssertContains(artifact, "\"byokProviderSettingsContract\": true");
        AssertContains(artifact, "\"secretStoragePolicyAdr\": true");
        AssertContains(artifact, "\"providerTestConnectionUxContract\": true");
        AssertContains(artifact, "\"byokRealIntroduced\": false");
        AssertContains(artifact, "\"providerCallsIntroduced\": false");
        AssertContains(artifact, "\"networkIntroduced\": false");
        AssertContains(artifact, "\"rawSecretsPersisted\": false");
    }

    private static void AssertSafeOutput(string text)
    {
        foreach (var marker in SensitiveMarkers)
            AssertDoesNotContain(text, marker);
        foreach (var name in ForbiddenNames)
            AssertDoesNotContain(text, name);
    }

    private static string NewSource() =>
        System.IO.File.ReadAllText(PathFor("src", "OneBrain.AgentOperations.Contracts", "NodalOsByokProviderContracts.cs")) +
        System.IO.File.ReadAllText(PathFor("src", "OneBrain.AgentOperations.Core", "NodalOsByokProviderServices.cs"));

    private static string PathFor(params string[] parts) =>
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", Path.Combine(parts));

    private static void AssertContains(string text, string expected) =>
        StringAssert.Contains(text, expected);

    private static void AssertDoesNotContain(string text, string unexpected) =>
        Assert.IsFalse(text.Contains(unexpected, StringComparison.Ordinal), $"Unexpected marker found: {unexpected}");
}
