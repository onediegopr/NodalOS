using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.AgentOperations.Core;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("AuditAPreUiBoundaryNaming")]
[TestCategory("ApprovalUxHandoffObservability")]
[TestCategory("ApprovalTimelineEvidence")]
[TestCategory("CoreRuntimeRegistryEventBusRedaction")]
[TestCategory("NewTopicsIntake")]
[TestCategory("NamingCleanup")]
public sealed class NodalOsAuditAPreUiBoundaryNamingM477M479Tests
{
    private static readonly string[] ForbiddenOperationalNames = ["Nexa", "NEXA", "NODRIX", "HOTEP"];

    [TestMethod]
    public void AgentOperationsContracts_DoesNotReferenceBrowserExecutorCdpProject()
    {
        var csproj = File.ReadAllText(PathFor("src", "OneBrain.AgentOperations.Contracts", "OneBrain.AgentOperations.Contracts.csproj"));

        AssertDoesNotContain(csproj, "OneBrain.BrowserExecutor.Cdp");
    }

    [TestMethod]
    public void AgentOperationsCore_DoesNotReferenceBrowserExecutorCdpProject()
    {
        var csproj = File.ReadAllText(PathFor("src", "OneBrain.AgentOperations.Core", "OneBrain.AgentOperations.Core.csproj"));

        AssertDoesNotContain(csproj, "OneBrain.BrowserExecutor.Cdp");
    }

    [TestMethod]
    public void AgentOperationsSources_DoNotUseBrowserExecutorCdpNamespace()
    {
        var sources = AgentOperationsSourceText();

        AssertDoesNotContain(sources, "using OneBrain.BrowserExecutor.Cdp");
        AssertDoesNotContain(sources, "namespace OneBrain.BrowserExecutor.Cdp");
        AssertDoesNotContain(sources, "OneBrain.BrowserExecutor.Cdp.");
    }

    [TestMethod]
    public void FutureUiProjects_DoNotReferenceBrowserExecutorCdp()
    {
        foreach (var project in FutureUiProjectFiles())
        {
            var text = File.ReadAllText(project);
            AssertDoesNotContain(text, "OneBrain.BrowserExecutor.Cdp");
        }
    }

    [TestMethod]
    public void AgentOperationsSources_DoNotContainForbiddenRuntimePrimitives()
    {
        var sources = AgentOperationsSourceText();

        AssertDoesNotContain(sources, "HttpClient");
        AssertDoesNotContain(sources, "ClientWebSocket");
        AssertDoesNotContain(sources, "Process.Start");
        AssertDoesNotContain(sources, "System.Diagnostics.Process");
        AssertDoesNotContain(sources, "BackgroundService");
        AssertDoesNotContain(sources, "Task.Run");
        AssertDoesNotContain(sources, "new Timer(");
        AssertDoesNotContain(sources, "System.Threading.Timer");
        AssertDoesNotContain(sources, "new Thread(");
        AssertDoesNotContain(sources, ".Start(); // runtime worker");
    }

    [TestMethod]
    public void BrowserExecutorCdp_CanExistButRemainsOutsideNewCore()
    {
        Assert.IsTrue(File.Exists(PathFor("src", "OneBrain.BrowserExecutor.Cdp", "OneBrain.BrowserExecutor.Cdp.csproj")));
        AssertDoesNotContain(AgentOperationsProjectReferences(), "OneBrain.BrowserExecutor.Cdp");
    }

    [TestMethod]
    public void SafetyCurrentState_IsDocumentedAsByDisconnection()
    {
        var report = File.ReadAllText(PathFor("docs", "reports", "audit-a-pre-ui-boundary-naming-m477-m479.md"));

        AssertContains(report, "by disconnection");
        AssertContains(report, "future UI cannot accidentally");
    }

    [TestMethod]
    public void EvidenceModelConsolidationAdrExistsAndDeclaresCanonicalNodalOsModel()
    {
        var adr = EvidenceAdr();

        AssertContains(adr, "NODAL_OS_EVIDENCE_MODEL_CONSOLIDATION_DECIDED");
        AssertContains(adr, "NodalOsEvidenceBridgeRef");
        AssertContains(adr, "canonical NODAL OS evidence reference model");
    }

    [TestMethod]
    public void EvidenceModelAdrMarksNexaEvidenceRefLegacyCompatibilityOnly()
    {
        var adr = EvidenceAdr();

        AssertContains(adr, "`NexaEvidenceRef` is legacy/compatibility only");
        AssertContains(adr, "not an operational NODAL OS evidence model");
    }

    [TestMethod]
    public void EvidenceModelAdrForbidsFutureUiBindingToNexaEvidenceRef()
    {
        var adr = EvidenceAdr();

        AssertContains(adr, "Future UI must not bind `NexaEvidenceRef` directly");
        AssertContains(adr, "Exports, handoff data packs and observability reports must serialize only NODAL OS evidence references");
    }

    [TestMethod]
    public void ExecutionAuthorizationGateAdrExistsAndBlocksRuntimeWithoutPositiveGate()
    {
        var adr = ExecutionGateAdr();

        AssertContains(adr, "NODAL_OS_EXECUTION_AUTHORIZATION_GATE_REQUIRED_BEFORE_RUNTIME");
        AssertContains(adr, "No real execution is allowed until a positive execution authorization gate exists");
        AssertContains(adr, "`CanAuthorizeExecution=false` remains mandatory");
    }

    [TestMethod]
    public void ExecutionAuthorizationGateAdrForbidsUiAndAgentOperationsDirectRuntimeCalls()
    {
        var adr = ExecutionGateAdr();

        AssertContains(adr, "UI cannot call runtime directly");
        AssertContains(adr, "AgentOperations cannot call `BrowserExecutor.Cdp` directly");
    }

    [TestMethod]
    public void ExecutionAuthorizationGateAdrRequiresPolicyApprovalVerificationEvidenceRollbackRedactionJail()
    {
        var adr = ExecutionGateAdr();

        AssertContains(adr, "Execution Registry state");
        AssertContains(adr, "Policy Gate decision");
        AssertContains(adr, "Approval Decision");
        AssertContains(adr, "Evidence requirements");
        AssertContains(adr, "Verification plan");
        AssertContains(adr, "rollback/restore plan");
        AssertContains(adr, "redaction check");
        AssertContains(adr, "jail/path boundary");
        AssertContains(adr, "risk classifier hardening");
    }

    [TestMethod]
    public void ApprovalUxPreviewSerializedOutput_UsesOnlyNodalOsOperationalName()
    {
        var json = new NodalOsApprovalUxHandoffObservabilityJsonSerializer()
            .SerializeApprovalUxPreview(NodalOsApprovalUxHandoffObservabilityFixtures.ApprovalUxPreview());

        AssertContains(json, "NODAL OS");
        AssertNoForbiddenOperationalNames(json);
    }

    [TestMethod]
    public void HandoffDataPackSerializedOutput_UsesOnlyNodalOsOperationalName()
    {
        var json = new NodalOsApprovalUxHandoffObservabilityJsonSerializer()
            .SerializeHandoffDataPack(NodalOsApprovalUxHandoffObservabilityFixtures.HandoffDataPack());

        AssertContains(json, "NODAL OS");
        AssertNoForbiddenOperationalNames(json);
    }

    [TestMethod]
    public void RuntimeObservabilityReportSerializedOutput_UsesOnlyNodalOsOperationalName()
    {
        var json = new NodalOsApprovalUxHandoffObservabilityJsonSerializer()
            .SerializeRuntimeObservabilityReport(NodalOsApprovalUxHandoffObservabilityFixtures.RuntimeObservabilityReport());

        AssertContains(json, "NODAL OS");
        AssertNoForbiddenOperationalNames(json);
    }

    [TestMethod]
    public void TimelineAndEvidenceSerializedOutput_DoNotUseForbiddenOperationalNames()
    {
        var timeline = new NodalOsApprovalTimelineEvidenceJsonSerializer()
            .SerializeTimelineEntry(NodalOsApprovalTimelineEvidenceFixtures.TimelineEntry());
        var evidence = new NodalOsApprovalTimelineEvidenceJsonSerializer()
            .SerializeEvidenceAttachment(NodalOsApprovalTimelineEvidenceFixtures.EvidenceAttachment());

        AssertNoForbiddenOperationalNames(timeline);
        AssertNoForbiddenOperationalNames(evidence);
    }

    [TestMethod]
    public void HistoricalDocsMayMentionLegacyNamesOnlyAsExternalOrLegacy()
    {
        var plan = File.ReadAllText(PathFor("docs", "backlog", "nodal-os-legacy-nexa-subsystem-quarantine-plan.md"));
        var intake = File.ReadAllText(PathFor("docs", "reports", "new-topics-intake-m465.md"));

        AssertContains(plan, "legacy `Nexa*`");
        AssertContains(plan, "not the operational product identity");
        AssertContains(intake, "planning inputs only");
        AssertContains(intake, "external/historical input wording");
    }

    [TestMethod]
    public void LegacyNexaQuarantinePlanExistsAndMentionsSensitiveDomains()
    {
        var plan = QuarantinePlan();

        AssertContains(plan, "billing");
        AssertContains(plan, "email");
        AssertContains(plan, "credentials");
        AssertContains(plan, "admin");
        AssertContains(plan, "configuration profiles");
    }

    [TestMethod]
    public void LegacyNexaQuarantinePlanBlocksCloudLicensingByok()
    {
        var plan = QuarantinePlan();

        AssertContains(plan, "Cloud, licensing and BYOK are blocked");
        AssertContains(plan, "cloud/licensing/BYOK");
    }

    [TestMethod]
    public void LegacyNexaQuarantinePlanIncludesSafeOptionsAndRejectsBroadRenameWithoutTests()
    {
        var plan = QuarantinePlan();

        AssertContains(plan, "Delete if proven unreferenced");
        AssertContains(plan, "Move to archive excluded from build");
        AssertContains(plan, "Mark obsolete and isolate");
        AssertContains(plan, "scoped migration");
        AssertContains(plan, "Do not perform broad rename without tests");
    }

    [TestMethod]
    public void ArtifactMarksAuditAFixesAndNoRuntimeIntroduced()
    {
        var artifact = Artifact();

        AssertContains(artifact, "\"decision\": \"AUDIT_A_PRE_UI_BOUNDARY_NAMING_HARDENING_READY\"");
        AssertContains(artifact, "\"dependencyDirectionGuard\": true");
        AssertContains(artifact, "\"browserRuntimeDisconnectionGuard\": true");
        AssertContains(artifact, "\"evidenceModelConsolidationAdr\": true");
        AssertContains(artifact, "\"executionAuthorizationGateAdr\": true");
        AssertContains(artifact, "\"namingSerializationGuard\": true");
        AssertContains(artifact, "\"legacyNexaSubsystemQuarantinePlan\": true");
        AssertContains(artifact, "\"forbiddenRuntimeIntroduced\": false");
        AssertContains(artifact, "\"uiIntroduced\": false");
        AssertContains(artifact, "\"cloudIntroduced\": false");
        AssertContains(artifact, "\"llmProviderCallsIntroduced\": false");
        AssertContains(artifact, "\"browserAutomationIntroduced\": false");
        AssertContains(artifact, "\"schedulerOrWorkerIntroduced\": false");
        AssertContains(artifact, "\"shellOrSubprocessIntroduced\": false");
    }

    [TestMethod]
    public void RoadmapsReferenceM477M479AndNextMissionControlReadOnly()
    {
        var vnext = File.ReadAllText(PathFor("docs", "roadmap", "nodal-os-roadmap-vnext.md"));
        var unified = File.ReadAllText(PathFor("docs", "roadmap", "nodal-os-unified-roadmap-post-pause.md"));

        AssertContains(vnext, "M477-M479 AUDIT-A Pre-UI Boundary & Naming Hardening");
        AssertContains(vnext, "M480-M482");
        AssertContains(unified, "M477-M479");
        AssertContains(unified, "Mission Control Shell V1 Read-Only");
    }

    private static string EvidenceAdr() =>
        File.ReadAllText(PathFor("docs", "architecture", "nodal-os-evidence-model-consolidation-decision-record.md"));

    private static string ExecutionGateAdr() =>
        File.ReadAllText(PathFor("docs", "architecture", "nodal-os-execution-authorization-gate-decision-record.md"));

    private static string QuarantinePlan() =>
        File.ReadAllText(PathFor("docs", "backlog", "nodal-os-legacy-nexa-subsystem-quarantine-plan.md"));

    private static string Artifact() =>
        File.ReadAllText(PathFor("artifacts", "agent-operations", "m479", "audit-a-pre-ui-boundary-naming-summary.json"));

    private static string AgentOperationsSourceText() =>
        string.Join(Environment.NewLine, AgentOperationsSourceFiles().Select(File.ReadAllText));

    private static string AgentOperationsProjectReferences() =>
        string.Join(Environment.NewLine, Directory.GetFiles(PathFor("src"), "OneBrain.AgentOperations.*.csproj", SearchOption.AllDirectories)
            .Select(File.ReadAllText));

    private static IEnumerable<string> AgentOperationsSourceFiles() =>
        Directory.GetFiles(PathFor("src"), "*.cs", SearchOption.AllDirectories)
            .Where(path => path.Contains($"{Path.DirectorySeparatorChar}OneBrain.AgentOperations.", StringComparison.Ordinal))
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase));

    private static IEnumerable<string> FutureUiProjectFiles() =>
        Directory.GetFiles(PathFor("src"), "*.csproj", SearchOption.AllDirectories)
            .Where(path =>
            {
                var name = Path.GetFileNameWithoutExtension(path);
                return name.Contains("Ui", StringComparison.OrdinalIgnoreCase) ||
                       name.Contains("MissionControl", StringComparison.OrdinalIgnoreCase) ||
                       name.Contains("Frontend", StringComparison.OrdinalIgnoreCase) ||
                       name.Contains("Web", StringComparison.OrdinalIgnoreCase);
            });

    private static void AssertNoForbiddenOperationalNames(string text)
    {
        foreach (var forbidden in ForbiddenOperationalNames)
            AssertDoesNotContain(text, forbidden);
    }

    private static void AssertContains(string text, string expected) =>
        Assert.IsTrue(text.Contains(expected, StringComparison.Ordinal), expected);

    private static void AssertDoesNotContain(string text, string unexpected) =>
        Assert.IsFalse(text.Contains(unexpected, StringComparison.Ordinal), unexpected);

    private static string PathFor(params string[] parts) =>
        Path.Combine(new[] { RepoRoot() }.Concat(parts).ToArray());

    private static string RepoRoot()
    {
        var current = AppContext.BaseDirectory;
        while (!string.IsNullOrWhiteSpace(current))
        {
            if (File.Exists(Path.Combine(current, "OneBrain.slnx")))
                return current;

            current = Directory.GetParent(current)?.FullName;
        }

        throw new DirectoryNotFoundException("Could not locate repository root.");
    }
}
