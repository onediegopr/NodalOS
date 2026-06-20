using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.AgentOperations.Contracts;
using OneBrain.AgentOperations.Core;

namespace OneBrain.Safety.Tests;

[TestClass]
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
public sealed class NodalOsWorkspaceReadinessContextM501M503Tests
{
    private static readonly string[] ForbiddenNames = ["Nexa", "NEXA", "NODRIX", "HOTEP"];
    private static readonly string[] SensitiveMarkers = ["Bearer ", "Authorization:", "Cookie:", "password", "secret", "api_key", "access_token", "refresh_token", "id_token", "private key"];
    private readonly NodalOsWorkspaceReadinessService service = new();
    private readonly NodalOsWorkspaceReadinessValidator validator = new();
    private readonly NodalOsWorkspaceReadinessJsonSerializer serializer = new();

    [TestMethod]
    public void ReadinessGate_CreatesNotReady()
    {
        var result = service.EvaluateReadiness(null, null, null, null, null, null, null, null, NodalOsWorkspaceReadinessState.NotReady);

        Assert.AreEqual(NodalOsWorkspaceReadinessState.NotReady, result.Status);
        Assert.IsTrue(validator.ValidateReadiness(result).IsValid);
    }

    [TestMethod]
    public void ReadinessGate_CreatesReadyForReadOnlyPreview()
    {
        var workspace = NodalOsWorkspaceFixtures.ActiveReadOnlyWorkspace();
        var result = service.EvaluateReadiness(workspace, NodalOsWorkspaceFixtures.PathJailBinding(), null, null, null, null, null, null);

        Assert.AreEqual(NodalOsWorkspaceReadinessState.ReadyForReadOnlyPreview, result.Status);
        Assert.IsTrue(result.AllowedNextSafeCapabilitiesRedacted.Count > 0);
    }

    [TestMethod]
    public void ReadinessGate_CreatesReadyForMockMetadata()
    {
        var workspace = NodalOsWorkspaceFixtures.ActiveReadOnlyWorkspace();
        var result = service.EvaluateReadiness(
            workspace,
            NodalOsWorkspaceFixtures.PathJailBinding(),
            NodalOsWorkspaceFixtures.ImportWizard(),
            NodalOsWorkspaceMissionFixtures.StorageWithDraftAndActive().Summary(),
            new NodalOsWorkspaceMissionBindingService().CreateBinding(workspace),
            null,
            null,
            null);

        Assert.AreEqual(NodalOsWorkspaceReadinessState.ReadyForMockMetadata, result.Status);
    }

    [TestMethod]
    public void ReadinessGate_CreatesReadyForUserProvidedContextIntake()
    {
        var result = NodalOsWorkspaceReadinessFixtures.ReadyForUserProvidedContextIntake();

        Assert.AreEqual(NodalOsWorkspaceReadinessState.ReadyForUserProvidedContextIntake, result.Status);
        Assert.IsTrue(result.EvidenceRefs.Count > 0);
        Assert.IsTrue(result.TimelineRefs.Count > 0);
    }

    [TestMethod]
    public void ReadinessGate_CreatesBlockedStates()
    {
        var states = new[]
        {
            NodalOsWorkspaceReadinessState.BlockedByPathJail,
            NodalOsWorkspaceReadinessState.BlockedByMissingWorkspace,
            NodalOsWorkspaceReadinessState.BlockedByRuntimeGate,
            NodalOsWorkspaceReadinessState.BlockedByCloudQuarantine,
            NodalOsWorkspaceReadinessState.BlockedByLegacySensitiveSubsystem,
            NodalOsWorkspaceReadinessState.BlockedByRecipeRiskHardening,
            NodalOsWorkspaceReadinessState.Unknown
        };

        foreach (var state in states)
        {
            var result = service.EvaluateReadiness(null, null, null, null, null, null, null, null, state);

            Assert.AreEqual(state, result.Status);
            Assert.IsTrue(validator.ValidateReadiness(result).IsValid, state.ToString());
        }
    }

    [TestMethod]
    public void ReadinessGate_IsReadOnlyNoRuntimeNoProviderNoCloud()
    {
        var result = NodalOsWorkspaceReadinessFixtures.ReadyForUserProvidedContextIntake();

        Assert.IsTrue(result.ReadOnlyGate);
        Assert.IsFalse(result.FilesystemScanAllowed);
        Assert.IsFalse(result.LlmProviderCallsAllowed);
        Assert.IsFalse(result.CloudSyncAllowed);
        Assert.IsFalse(result.CanAuthorizeExecution);
        Assert.IsFalse(result.RuntimeExecutionAllowed);
        Assert.IsFalse(result.PositiveExecutionGateImplemented);
    }

    [TestMethod]
    public void ReadinessGate_SerializationIsSafe()
    {
        var json = serializer.SerializeReadiness(NodalOsWorkspaceReadinessFixtures.ReadyForUserProvidedContextIntake());

        AssertSafeSerialized(json);
    }

    [TestMethod]
    public void Intake_CreatesUserProvidedSummary()
    {
        var intake = service.CreateUserProvidedSummaryIntake(NodalOsWorkspaceFixtures.ActiveReadOnlyWorkspace());
        var result = validator.ValidateIntake(intake);

        Assert.IsTrue(result.IsValid, string.Join(" | ", result.Errors));
        Assert.AreEqual(NodalOsProjectUnderstandingIntakeSource.UserProvidedSummary, intake.Source);
        Assert.AreEqual(NodalOsProjectUnderstandingIntakeItemType.ProjectSummary, intake.Items[0].ItemType);
    }

    [TestMethod]
    public void Intake_CreatesTechStackHint()
    {
        var intake = service.CreateTechStackHintIntake(NodalOsWorkspaceFixtures.ActiveReadOnlyWorkspace());

        Assert.AreEqual(NodalOsProjectUnderstandingIntakeSource.UserProvidedTechStack, intake.Source);
        Assert.AreEqual(NodalOsProjectUnderstandingIntakeItemType.TechStackHint, intake.Items[0].ItemType);
    }

    [TestMethod]
    public void Intake_CreatesFolderStructureHintWithoutRealValidation()
    {
        var intake = service.CreateFolderStructureHintIntake(NodalOsWorkspaceFixtures.ActiveReadOnlyWorkspace());

        Assert.AreEqual(NodalOsProjectUnderstandingIntakeItemType.FolderStructureHint, intake.Items[0].ItemType);
        Assert.IsFalse(intake.ValidatesRealStructure);
        Assert.IsFalse(intake.Items[0].ValidatesRealExistence);
    }

    [TestMethod]
    public void Intake_CreatesFutureRealScanPlaceholderDisabled()
    {
        var intake = service.CreateFutureRealScanPlaceholder(NodalOsWorkspaceFixtures.ActiveReadOnlyWorkspace());

        Assert.AreEqual(NodalOsProjectUnderstandingIntakeSource.FutureRealScanPlaceholder, intake.Source);
        Assert.AreEqual(NodalOsProjectSummaryConfidence.Unknown, intake.DeclaredConfidence);
        Assert.IsFalse(intake.ReadsFiles);
    }

    [TestMethod]
    public void Intake_DeclaresProvenanceConfidenceFreshnessAndUsage()
    {
        var intake = NodalOsWorkspaceReadinessFixtures.UserProvidedSummaryIntake();

        Assert.IsFalse(string.IsNullOrWhiteSpace(intake.DeclaredProvenanceRedacted));
        Assert.IsFalse(string.IsNullOrWhiteSpace(intake.DeclaredFreshnessRedacted));
        Assert.AreEqual(NodalOsProjectSummaryConfidence.UserProvided, intake.DeclaredConfidence);
        Assert.IsTrue(intake.AllowedUsageRedacted.Count > 0);
        Assert.IsTrue(intake.DisallowedUsageRedacted.Count > 0);
    }

    [TestMethod]
    public void Intake_DeclaresNoFilesReadAndNoRealUnderstanding()
    {
        var intake = NodalOsWorkspaceReadinessFixtures.UserProvidedSummaryIntake();
        var text = string.Join(" ", intake.ContextDisclosureRedacted, intake.NoContentAccessDisclosureRedacted, intake.StructureNotVerifiedDisclosureRedacted, intake.NoRealUnderstandingDisclosureRedacted);

        AssertContains(text, "Contexto provisto");
        AssertContains(text, "No se leyo ningun archivo");
        AssertContains(text, "No se verifico estructura real");
        AssertContains(text, "project understanding real");
    }

    [TestMethod]
    public void Intake_DoesNotUseProviderGitVectorsOrProductiveChanges()
    {
        var intake = NodalOsWorkspaceReadinessFixtures.UserProvidedSummaryIntake();

        Assert.IsFalse(intake.ReadsFiles);
        Assert.IsFalse(intake.ValidatesRealStructure);
        Assert.IsFalse(intake.UsesGit);
        Assert.IsFalse(intake.CreatesVectorIndex);
        Assert.IsFalse(intake.CallsLlmProvider);
        Assert.IsFalse(intake.CreatesRealProjectUnderstanding);
        Assert.IsFalse(intake.CanAuthorizeExecution);
        Assert.IsFalse(intake.ChangesWorkspaceProductively);
    }

    [TestMethod]
    public void Intake_SerializationIsSafe()
    {
        var json = serializer.SerializeIntake(NodalOsWorkspaceReadinessFixtures.UserProvidedSummaryIntake());

        AssertSafeSerialized(json);
    }

    [TestMethod]
    public void ContextBoundary_AllowsPublicSafeForDisplay()
    {
        var boundary = service.ClassifyContext("workspace-local-active", NodalOsContextSensitivityLevel.PublicSafe, NodalOsSafeContextUsageTarget.Display);

        Assert.IsTrue(boundary.SafeForDisplay);
        Assert.IsFalse(boundary.CallsLlmProvider);
        Assert.IsTrue(validator.ValidateContextBoundary(boundary).IsValid);
    }

    [TestMethod]
    public void ContextBoundary_AllowsUserProvidedSafeForDisplayAndExport()
    {
        var display = service.ClassifyContext("workspace-local-active", NodalOsContextSensitivityLevel.UserProvidedSafe, NodalOsSafeContextUsageTarget.Display);
        var export = service.ClassifyContext("workspace-local-active", NodalOsContextSensitivityLevel.UserProvidedSafe, NodalOsSafeContextUsageTarget.Export);

        Assert.IsTrue(display.SafeForDisplay);
        Assert.IsTrue(export.SafeForExport);
    }

    [TestMethod]
    public void ContextBoundary_AllowsEvidenceRefOnlyAsRefOnly()
    {
        var boundary = service.ClassifyContext("workspace-local-active", NodalOsContextSensitivityLevel.EvidenceRefOnly, NodalOsSafeContextUsageTarget.FutureEvidenceReport);

        Assert.IsTrue(boundary.EvidenceRefOnly);
        Assert.IsTrue(boundary.SafeForDisplay);
        Assert.IsTrue(boundary.SafeForExport);
    }

    [TestMethod]
    public void ContextBoundary_AllowsRedactedOnly()
    {
        var boundary = service.ClassifyContext("workspace-local-active", NodalOsContextSensitivityLevel.RedactedOnly, NodalOsSafeContextUsageTarget.Export);

        Assert.IsTrue(boundary.SafeForDisplay);
        Assert.IsTrue(boundary.SafeForExport);
        AssertContains(boundary.RedactionStatusRedacted, "Redacted");
    }

    [TestMethod]
    public void ContextBoundary_BlocksSensitiveRawAndUnknown()
    {
        var blocked = new[]
        {
            NodalOsContextSensitivityLevel.SensitiveBlocked,
            NodalOsContextSensitivityLevel.SecretBlocked,
            NodalOsContextSensitivityLevel.RawPayloadBlocked,
            NodalOsContextSensitivityLevel.UnknownRequiresReview
        };

        foreach (var sensitivity in blocked)
        {
            var boundary = service.ClassifyContext("workspace-local-active", sensitivity, NodalOsSafeContextUsageTarget.Display);

            Assert.IsFalse(boundary.SafeForDisplay, sensitivity.ToString());
            Assert.IsFalse(boundary.SafeForExport, sensitivity.ToString());
            Assert.IsTrue(boundary.DeniedContextRefs.Count > 0, sensitivity.ToString());
        }
    }

    [TestMethod]
    public void ContextBoundary_FutureLlmUsageRequiresPolicyAndByok()
    {
        var boundary = service.ClassifyContext("workspace-local-active", NodalOsContextSensitivityLevel.UserProvidedSafe, NodalOsSafeContextUsageTarget.FutureLlmPrompt);

        Assert.IsTrue(boundary.FutureLlmPolicyRequired);
        Assert.IsTrue(boundary.ByokRequiredForFutureLlm);
        Assert.IsFalse(boundary.CallsLlmProvider);
    }

    [TestMethod]
    public void ContextBoundary_CannotAuthorizeExecutionOrBypassApproval()
    {
        var boundary = NodalOsWorkspaceReadinessFixtures.DisplaySafeContext();

        Assert.IsFalse(boundary.CanAuthorizeExecution);
        Assert.IsFalse(boundary.CanBypassApproval);
        Assert.IsTrue(boundary.RawPathRedactedOrFingerprinted);
        Assert.IsFalse(boundary.ScansWorkspace);
        Assert.IsFalse(boundary.MutatesWorkspace);
        Assert.IsFalse(boundary.CreatesVectorIndex);
        Assert.IsFalse(boundary.CreatesPrompt);
    }

    [TestMethod]
    public void ContextBoundary_SerializationIsSafe()
    {
        var json = serializer.SerializeContextBoundary(NodalOsWorkspaceReadinessFixtures.DisplaySafeContext());

        AssertSafeSerialized(json);
    }

    [TestMethod]
    public void Boundary_NewWorkspaceReadinessFiles_DoNotReferenceRuntimePrimitives()
    {
        var source = NewWorkspaceReadinessSource();

        AssertDoesNotContain(source, "OneBrain.BrowserExecutor.Cdp");
        AssertDoesNotContain(source, "HttpClient");
        AssertDoesNotContain(source, "ClientWebSocket");
        AssertDoesNotContain(source, "Process.Start");
        AssertDoesNotContain(source, "System.Diagnostics.Process");
        AssertDoesNotContain(source, "BackgroundService");
        AssertDoesNotContain(source, "Task.Run");
        AssertDoesNotContain(source, "ExecuteAsync");
        AssertDoesNotContain(source, "OpenAI");
        AssertDoesNotContain(source, "TelemetryClient");
        AssertDoesNotContain(source, "AnalyticsClient");
        AssertDoesNotContain(source, "File.Write");
        AssertDoesNotContain(source, "File.Read");
        AssertDoesNotContain(source, "Directory.");
        AssertDoesNotContain(source, "Process ");
    }

    [TestMethod]
    public void ExistingSafetyContinuity_PreviousWorkspaceAndUiLayersRemainSafe()
    {
        var readiness = NodalOsWorkspaceReadinessFixtures.ReadyForUserProvidedContextIntake();
        var metadata = NodalOsWorkspaceMetadataFixtures.MockIndex();
        var storage = NodalOsWorkspaceMissionFixtures.StorageWithDraftAndActive();
        var shell = NodalOsMissionControlShellFixtures.ShellPreview();
        var approval = NodalOsApprovalUxHandoffObservabilityFixtures.ApprovalUxPreview();

        Assert.IsFalse(readiness.RuntimeExecutionAllowed);
        Assert.IsTrue(metadata.MockOnly);
        Assert.AreEqual(2, storage.ListWorkspaces().Count);
        Assert.IsFalse(shell.CanAuthorizeExecution);
        Assert.IsFalse(approval.CanAuthorizeExecution);
    }

    [TestMethod]
    public void ArtifactMarksWorkspaceReadinessContextBoundary()
    {
        var artifact = File.ReadAllText(PathFor("artifacts", "agent-operations", "m503", "workspace-readiness-context-summary.json"));

        AssertContains(artifact, "\"workspaceReadinessGate\": true");
        AssertContains(artifact, "\"projectUnderstandingIntakeContract\": true");
        AssertContains(artifact, "\"safeContextBoundary\": true");
        AssertContains(artifact, "\"projectUnderstandingRealIntroduced\": false");
        AssertContains(artifact, "\"llmProviderCallsIntroduced\": false");
    }

    private static void AssertSafeSerialized(string text)
    {
        foreach (var marker in SensitiveMarkers)
            AssertDoesNotContain(text, marker);
        foreach (var name in ForbiddenNames)
            AssertDoesNotContain(text, name);
    }

    private static string NewWorkspaceReadinessSource() =>
        string.Join(Environment.NewLine,
            new[]
            {
                PathFor("src", "OneBrain.AgentOperations.Contracts", "NodalOsWorkspaceReadinessContracts.cs"),
                PathFor("src", "OneBrain.AgentOperations.Core", "NodalOsWorkspaceReadinessServices.cs")
            }.Select(File.ReadAllText));

    private static void AssertContains(string text, string expected) =>
        Assert.IsTrue(text.Contains(expected, StringComparison.OrdinalIgnoreCase), expected);

    private static void AssertDoesNotContain(string text, string unexpected) =>
        Assert.IsFalse(text.Contains(unexpected, StringComparison.Ordinal), unexpected);

    private static string PathFor(params string[] parts)
    {
        var root = AppContext.BaseDirectory;
        for (var i = 0; i < 10; i++)
        {
            var candidate = Path.Combine(new[] { root }.Concat(parts).ToArray());
            if (File.Exists(candidate) || Directory.Exists(candidate))
                return candidate;
            var parent = Directory.GetParent(root);
            if (parent is null)
                break;
            root = parent.FullName;
        }

        return Path.Combine(new[] { AppContext.BaseDirectory }.Concat(parts).ToArray());
    }
}
