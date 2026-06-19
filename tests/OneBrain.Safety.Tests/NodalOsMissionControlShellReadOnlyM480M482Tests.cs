using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.AgentOperations.Contracts;
using OneBrain.AgentOperations.Core;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("MissionControlShellReadOnly")]
[TestCategory("AuditAPreUiBoundaryNaming")]
[TestCategory("ApprovalUxHandoffObservability")]
[TestCategory("ApprovalTimelineEvidence")]
[TestCategory("CoreRuntimeRegistryEventBusRedaction")]
[TestCategory("NewTopicsIntake")]
[TestCategory("NamingCleanup")]
public sealed class NodalOsMissionControlShellReadOnlyM480M482Tests
{
    private static readonly string[] ForbiddenOperationalNames = ["Nexa", "NEXA", "NODRIX", "HOTEP"];
    private static readonly string[] ForbiddenSecretMarkers =
    [
        "Bearer ",
        "access_token",
        "refresh_token",
        "id_token",
        "password=",
        "api_key",
        "Cookie:",
        "Set-Cookie:",
        "Authorization:",
        "raw body",
        "data:image/"
    ];

    private readonly NodalOsMissionControlShellValidator validator = new();
    private readonly NodalOsMissionControlShellJsonSerializer serializer = new();

    [TestMethod]
    public void UiBoundary_NewAgentOperationsMissionControlFiles_DoNotReferenceBrowserExecutorCdp()
    {
        var text = MissionControlSourceText();

        AssertDoesNotContain(text, "OneBrain.BrowserExecutor.Cdp");
        AssertDoesNotContain(text, "using OneBrain.BrowserExecutor.Cdp");
        AssertDoesNotContain(text, "namespace OneBrain.BrowserExecutor.Cdp");
    }

    [TestMethod]
    public void UiBoundary_NewAgentOperationsMissionControlFiles_DoNotContainRuntimePrimitives()
    {
        var text = MissionControlSourceText();

        AssertDoesNotContain(text, "HttpClient");
        AssertDoesNotContain(text, "ClientWebSocket");
        AssertDoesNotContain(text, "Process.Start");
        AssertDoesNotContain(text, "System.Diagnostics.Process");
        AssertDoesNotContain(text, "BackgroundService");
        AssertDoesNotContain(text, "Task.Run");
        AssertDoesNotContain(text, "new Timer(");
        AssertDoesNotContain(text, "System.Threading.Timer");
        AssertDoesNotContain(text, "new Thread(");
    }

    [TestMethod]
    public void UiBoundary_NoUiProjectReferencesBrowserExecutorCdp()
    {
        foreach (var project in UiProjectFiles())
            AssertDoesNotContain(File.ReadAllText(project), "OneBrain.BrowserExecutor.Cdp");
    }

    [TestMethod]
    public void MissionControlShell_ExistsAndValidatesReadOnly()
    {
        var shell = Shell();

        var result = validator.ValidateShell(shell);

        Assert.IsTrue(result.IsValid, string.Join(" | ", result.Errors));
        Assert.IsTrue(shell.ReadOnlyUi);
        Assert.IsFalse(shell.RuntimeExecutionAllowed);
        Assert.IsTrue(shell.RuntimeExecutionDeferred);
        Assert.IsFalse(shell.CanAuthorizeExecution);
    }

    [TestMethod]
    public void MissionControlShell_UsesNodalOsOperationalName()
    {
        var shell = Shell();
        var json = serializer.SerializeShell(shell);

        Assert.AreEqual("NODAL OS", shell.ProjectOperationalName);
        AssertContains(json, "NODAL OS");
        AssertNoForbiddenOperationalNames(json);
    }

    [TestMethod]
    public void MissionControlShell_ExposesMissionStatusProgressAndGuardrails()
    {
        var shell = Shell();

        AssertContains(shell.TopBar.MissionTitleRedacted, "Mission Control");
        AssertContains(shell.TopBar.OverallStatusRedacted, "Read-only");
        Assert.IsTrue(shell.TopBar.ProgressPercent is >= 0 and <= 100);
        AssertContains(shell.GuardrailsSummaryRedacted, "Read-only preview");
        AssertContains(shell.GuardrailsSummaryRedacted, "No runtime execution");
        AssertContains(shell.GuardrailsSummaryRedacted, "No browser automation");
        AssertContains(shell.GuardrailsSummaryRedacted, "No cloud sync");
        AssertContains(shell.GuardrailsSummaryRedacted, "No LLM provider calls");
    }

    [TestMethod]
    public void MissionControlShell_NavigationContainsExpectedSections()
    {
        var shell = Shell();
        var panels = shell.Navigation.Select(item => item.PanelKind).ToArray();

        CollectionAssert.Contains(panels, NodalOsMissionControlPanelKind.MissionControl);
        CollectionAssert.Contains(panels, NodalOsMissionControlPanelKind.Timeline);
        CollectionAssert.Contains(panels, NodalOsMissionControlPanelKind.Approvals);
        CollectionAssert.Contains(panels, NodalOsMissionControlPanelKind.Evidence);
        CollectionAssert.Contains(panels, NodalOsMissionControlPanelKind.LogsObservability);
        CollectionAssert.Contains(panels, NodalOsMissionControlPanelKind.SettingsDisabled);
    }

    [TestMethod]
    public void MissionControlShell_RenderedHtmlShowsReadOnlyNoRuntimeNoCloudNoLlm()
    {
        var html = new NodalOsMissionControlShellService().RenderReadOnlyHtml(Shell());

        AssertContains(html, "Read-only preview");
        AssertContains(html, "No runtime execution");
        AssertContains(html, "No browser automation");
        AssertContains(html, "No cloud sync");
        AssertContains(html, "No LLM provider calls");
        AssertContains(html, "data-can-authorize-execution=\"false\"");
    }

    [TestMethod]
    public void ApprovalDisplay_IncludesRiskActionResourcesAndEvidenceRefs()
    {
        var card = Shell().ApprovalDisplay.Cards.Single();

        Assert.AreEqual(NodalOsApprovalSeverity.High, card.Severity);
        Assert.AreEqual(NodalOsApprovalActionKind.SubmitFuture, card.RequestedAction);
        Assert.IsTrue(card.AffectedResourcesRedacted.Count > 0);
        Assert.IsTrue(card.EvidenceRefs.Count > 0);
        Assert.IsFalse(string.IsNullOrWhiteSpace(card.PolicyGateReasonRedacted));
        Assert.IsFalse(string.IsNullOrWhiteSpace(card.ExpectedEvidenceRedacted));
    }

    [TestMethod]
    public void ApprovalDisplay_RepresentsAllRequiredOptionsAsDisabled()
    {
        var options = Shell().ApprovalDisplay.ActionOptions;

        AssertOption(options, NodalOsApprovalUserOptionKind.Approve);
        AssertOption(options, NodalOsApprovalUserOptionKind.Reject);
        AssertOption(options, NodalOsApprovalUserOptionKind.RequestChanges);
        AssertOption(options, NodalOsApprovalUserOptionKind.RequestExplanation);
        AssertOption(options, NodalOsApprovalUserOptionKind.Defer);
        AssertOption(options, NodalOsApprovalUserOptionKind.CopyTechnicalLog);
        Assert.IsTrue(options.All(option => option.Disabled));
        Assert.IsTrue(options.All(option => !option.CanAuthorizeExecution));
        Assert.IsTrue(options.All(option => option.DisabledReasonRedacted.Contains("Disabled in read-only preview", StringComparison.Ordinal)));
    }

    [TestMethod]
    public void ApprovalDisplay_CannotAuthorizeExecution()
    {
        var approvalDisplay = Shell().ApprovalDisplay;

        Assert.IsTrue(approvalDisplay.ReadOnlyUi);
        Assert.IsFalse(approvalDisplay.CanAuthorizeExecution);
        Assert.IsTrue(approvalDisplay.Cards.All(card => !card.CanAuthorizeExecution));
        Assert.IsTrue(approvalDisplay.Cards.All(card => !card.RuntimeExecutionAllowed));
    }

    [TestMethod]
    public void TimelineView_IsReadOnlyOrderedAndLinked()
    {
        var timeline = Shell().Timeline;

        Assert.IsTrue(timeline.ReadOnlyUi);
        Assert.IsTrue(timeline.Entries.Count > 0);
        Assert.IsTrue(timeline.Entries.SequenceEqual(timeline.Entries.OrderBy(entry => entry.CreatedAt)));
        Assert.IsTrue(timeline.Entries.Any(entry => !string.IsNullOrWhiteSpace(entry.ExecutionRegistryEntryId)));
        Assert.IsTrue(timeline.Entries.Any(entry => entry.EvidenceRefs.Count > 0));
    }

    [TestMethod]
    public void EvidenceView_IsRefOnlyAndDoesNotExposeRawPayload()
    {
        var evidence = Shell().Evidence;

        Assert.IsTrue(evidence.ReadOnlyUi);
        Assert.IsTrue(evidence.RefOnly);
        Assert.IsTrue(evidence.EvidenceRefs.Count > 0);
        Assert.IsTrue(evidence.EvidenceRefs.All(item => item.RefOnly));
        Assert.IsTrue(evidence.EvidenceRefs.All(item => !item.RawPayloadInline));
        Assert.IsTrue(evidence.EvidenceRefs.All(item => !item.RefRedacted.StartsWith("data:image/", StringComparison.OrdinalIgnoreCase)));
    }

    [TestMethod]
    public void ObservabilityView_IsReadOnlyAndCopyReportDisabled()
    {
        var observability = Shell().Observability;

        Assert.IsTrue(observability.ReadOnlyUi);
        Assert.IsFalse(observability.CopyReportAvailable);
        AssertContains(observability.CopyReportDisabledReasonRedacted, "Disabled in read-only preview");
        Assert.IsTrue(observability.LinesRedacted.Count > 0);
    }

    [TestMethod]
    public void RenderedHtmlAndSerializedShell_DoNotContainSecretsOrForbiddenNames()
    {
        var shell = Shell() with
        {
            Workspace = Shell().Workspace with { SummaryRedacted = "Authorization: Bearer abcdefghijklmnopqrstuvwxyz" },
            Observability = Shell().Observability with { LinesRedacted = ["password=super-secret", "api_key=raw-fixture-key"] }
        };
        var json = serializer.SerializeShell(shell);
        var html = new NodalOsMissionControlShellService().RenderReadOnlyHtml(shell);

        AssertContains(json, "[REDACTED]");
        AssertContains(html, "[REDACTED]");
        AssertNoSecrets(json);
        AssertNoSecrets(html);
        AssertNoForbiddenOperationalNames(json);
        AssertNoForbiddenOperationalNames(html);
    }

    [TestMethod]
    public void EvidenceView_RejectsUnsafeRawEvidenceMarkers()
    {
        var shell = Shell();
        var invalid = shell with
        {
            Evidence = shell.Evidence with
            {
                EvidenceRefs =
                [
                    shell.Evidence.EvidenceRefs[0] with
                    {
                        RefRedacted = "data:image/png;base64,abcdef",
                        RawPayloadInline = true
                    }
                ]
            }
        };

        Assert.IsFalse(validator.ValidateShell(invalid).IsValid);
    }

    [TestMethod]
    public void Guardrails_NoCloudLlmBrowserAutomationSchedulerWorkerQueueRecorderReplayDslParserExecution()
    {
        var shell = Shell();
        var source = MissionControlSourceText();
        var json = serializer.SerializeShell(shell);

        Assert.IsFalse(shell.CloudSyncAllowed);
        Assert.IsFalse(shell.LlmProviderCallsAllowed);
        Assert.IsFalse(shell.BrowserAutomationAllowed);
        Assert.IsFalse(shell.RuntimeExecutionAllowed);
        AssertDoesNotContain(source, "BrowserExecutor.Cdp");
        AssertDoesNotContain(source, "Scheduler");
        AssertDoesNotContain(source, "BackgroundService");
        AssertDoesNotContain(source, "Recorder");
        AssertDoesNotContain(source, "Replay");
        AssertDoesNotContain(source, "Queue");
        AssertDoesNotContain(source, "DslParser");
        AssertDoesNotContain(json, "\"runtimeExecutionAllowed\": true");
    }

    [TestMethod]
    public void ArtifactMarksMissionControlReadOnlyAndNoRuntime()
    {
        var artifact = File.ReadAllText(ArtifactPath());

        AssertContains(artifact, "\"decision\": \"MISSION_CONTROL_SHELL_READONLY_READY\"");
        AssertContains(artifact, "\"projectOperationalName\": \"NODAL OS\"");
        AssertContains(artifact, "\"missionControlShellReadOnly\": true");
        AssertContains(artifact, "\"approvalDisplayReadOnly\": true");
        AssertContains(artifact, "\"timelineEvidenceViewsReadOnly\": true");
        AssertContains(artifact, "\"observabilityLogPreviewReadOnly\": true");
        AssertContains(artifact, "\"readOnlyUi\": true");
        AssertContains(artifact, "\"canAuthorizeExecution\": false");
        AssertContains(artifact, "\"runtimeExecutionAllowed\": false");
        AssertContains(artifact, "\"browserExecutorCdpReferenced\": false");
        AssertContains(artifact, "\"forbiddenRuntimeIntroduced\": false");
    }

    [TestMethod]
    public void ReportAndRoadmapsReferenceM480M482AndNextNoOpStateMilestone()
    {
        var report = File.ReadAllText(PathFor("docs", "reports", "mission-control-shell-readonly-m480-m482.md"));
        var vnext = File.ReadAllText(PathFor("docs", "roadmap", "nodal-os-roadmap-vnext.md"));
        var unified = File.ReadAllText(PathFor("docs", "roadmap", "nodal-os-unified-roadmap-post-pause.md"));

        AssertContains(report, "M480-M482");
        AssertContains(report, "read-only");
        AssertContains(report, "No runtime execution");
        AssertContains(vnext, "M480-M482 Mission Control Shell V1 Read-Only");
        AssertContains(vnext, "M483-M485");
        AssertContains(unified, "M480-M482");
        AssertContains(unified, "M483-M485");
    }

    private static NodalOsMissionControlShellPreview Shell() =>
        NodalOsMissionControlShellFixtures.ShellPreview();

    private static void AssertOption(IReadOnlyList<NodalOsApprovalDisplayActionOption> options, NodalOsApprovalUserOptionKind expected) =>
        Assert.IsTrue(options.Any(option => option.OptionKind == expected), expected.ToString());

    private static string MissionControlSourceText() =>
        string.Join(Environment.NewLine,
            new[]
            {
                PathFor("src", "OneBrain.AgentOperations.Contracts", "NodalOsMissionControlShellContracts.cs"),
                PathFor("src", "OneBrain.AgentOperations.Core", "NodalOsMissionControlShellServices.cs")
            }.Select(File.ReadAllText));

    private static IEnumerable<string> UiProjectFiles() =>
        Directory.GetFiles(RepoRoot(), "*.csproj", SearchOption.AllDirectories)
            .Where(path =>
            {
                var normalized = path.Replace($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", "/", StringComparison.OrdinalIgnoreCase);
                if (normalized.Contains("/obj/", StringComparison.OrdinalIgnoreCase))
                    return false;

                var name = Path.GetFileNameWithoutExtension(path);
                return name.Contains("Ui", StringComparison.OrdinalIgnoreCase) ||
                       name.Contains("MissionControl", StringComparison.OrdinalIgnoreCase) ||
                       name.Contains("Frontend", StringComparison.OrdinalIgnoreCase) ||
                       name.Contains("Web", StringComparison.OrdinalIgnoreCase);
            });

    private static string ArtifactPath() =>
        PathFor("artifacts", "agent-operations", "m482", "mission-control-shell-readonly-summary.json");

    private static void AssertNoSecrets(string text)
    {
        foreach (var marker in ForbiddenSecretMarkers)
            AssertDoesNotContain(text, marker);
    }

    private static void AssertNoForbiddenOperationalNames(string text)
    {
        foreach (var forbidden in ForbiddenOperationalNames)
            AssertDoesNotContain(text, forbidden);
    }

    private static void AssertContains(IReadOnlyList<string> values, string expected) =>
        Assert.IsTrue(values.Any(value => value.Contains(expected, StringComparison.Ordinal)), expected);

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
