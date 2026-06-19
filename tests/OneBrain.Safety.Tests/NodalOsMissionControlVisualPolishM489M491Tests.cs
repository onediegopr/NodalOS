using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.AgentOperations.Contracts;
using OneBrain.AgentOperations.Core;

namespace OneBrain.Safety.Tests;

[TestClass]
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
public sealed class NodalOsMissionControlVisualPolishM489M491Tests
{
    private static readonly string[] ForbiddenOperationalNames = ["Nexa", "NEXA", "NODRIX", "HOTEP"];
    private static readonly string[] SecretMarkers =
    [
        "Bearer ",
        "Authorization:",
        "Cookie:",
        "Set-Cookie:",
        "password",
        "api_key",
        "access_token",
        "refresh_token",
        "id_token",
        "private key",
        "data:image/"
    ];

    private readonly NodalOsMissionControlVisualService service = new();
    private readonly NodalOsMissionControlVisualValidator validator = new();

    [TestMethod]
    public void RendererContainsNodalOsOperationalName()
    {
        var html = RenderHtml();

        AssertContains(html, "NODAL OS");
        AssertDoesNotContainForbiddenNames(html);
    }

    [TestMethod]
    public void RendererContainsReadOnlyBadge()
    {
        var html = RenderHtml();

        AssertContains(html, "Read-only preview");
    }

    [TestMethod]
    public void RendererContainsNoRuntimeBadge()
    {
        var html = RenderHtml();

        AssertContains(html, "No runtime execution");
    }

    [TestMethod]
    public void RendererContainsTimelineVertical()
    {
        var html = RenderHtml();

        AssertContains(html, "timeline-vertical");
        AssertContains(html, "Execution request registered");
    }

    [TestMethod]
    public void RendererContainsApprovalDisplayDisabledNoAuthority()
    {
        var html = RenderHtml();

        AssertContains(html, "Approval Display");
        AssertContains(html, "Disabled");
        AssertContains(html, "non-authoritative");
        AssertContains(html, "data-can-authorize-execution=\"false\"");
    }

    [TestMethod]
    public void RendererContainsEvidenceRefs()
    {
        var html = RenderHtml();

        AssertContains(html, "Evidence refs");
        AssertContains(html, "inline=false");
        AssertContains(html, "ref-only");
    }

    [TestMethod]
    public void RendererContainsObservabilityLogPreview()
    {
        var html = RenderHtml();

        AssertContains(html, "Observability / LOG Preview");
        AssertContains(html, "Redacted technical report surface");
    }

    [TestMethod]
    public void RendererContainsGuardrailExplainers()
    {
        var html = RenderHtml();

        AssertContains(html, "Guardrail explainers");
        AssertContains(html, "Positive execution gate");
        AssertContains(html, "Read-only mode");
    }

    [TestMethod]
    public void RendererDoesNotContainSecretsOrRawPayloads()
    {
        var html = RenderHtml();

        AssertDoesNotContainSecrets(html);
        AssertDoesNotContain(html, "raw payload");
        AssertDoesNotContain(html, "network raw");
        AssertDoesNotContain(html, "DOM raw");
        AssertDoesNotContain(html, "screenshot inline");
    }

    [TestMethod]
    public void LayoutSpecDefinesCompactStandardWideAndControlRoom()
    {
        var spec = service.CreateResponsiveDesktopLayoutSpec();

        Assert.IsTrue(spec.Breakpoints.Any(bp => bp.BreakpointKind == NodalOsMissionControlDesktopBreakpointKind.CompactDesktop));
        Assert.IsTrue(spec.Breakpoints.Any(bp => bp.BreakpointKind == NodalOsMissionControlDesktopBreakpointKind.StandardDesktop));
        Assert.IsTrue(spec.Breakpoints.Any(bp => bp.BreakpointKind == NodalOsMissionControlDesktopBreakpointKind.WideDesktop));
        Assert.IsTrue(spec.Breakpoints.Any(bp => bp.BreakpointKind == NodalOsMissionControlDesktopBreakpointKind.UltrawideControlRoom));
    }

    [TestMethod]
    public void LayoutSpecKeepsSafetyBadgesAlwaysVisible()
    {
        var spec = service.CreateResponsiveDesktopLayoutSpec();

        Assert.IsTrue(spec.ReadOnlyBadgeAlwaysVisible);
        Assert.IsTrue(spec.NoRuntimeBadgeAlwaysVisible);
        Assert.IsTrue(spec.DisabledControlsRemainVisible);
    }

    [TestMethod]
    public void LayoutSpecDefinesPanelBehaviorAndTimelineDensity()
    {
        var spec = service.CreateResponsiveDesktopLayoutSpec();

        Assert.IsTrue(spec.Breakpoints.All(bp => bp.SidebarBehavior != NodalOsMissionControlPanelBehavior.HiddenDisabled));
        Assert.IsTrue(spec.Breakpoints.All(bp => bp.RightPanelBehavior is NodalOsMissionControlPanelBehavior.Expanded or NodalOsMissionControlPanelBehavior.Collapsed));
        Assert.IsTrue(spec.Breakpoints.All(bp => bp.BottomLogPanelBehavior is NodalOsMissionControlPanelBehavior.Expanded or NodalOsMissionControlPanelBehavior.Collapsed));
        Assert.IsTrue(spec.Breakpoints.Any(bp => bp.DensityMode == NodalOsMissionControlDensityMode.DenseLogHeavy));
    }

    [TestMethod]
    public void LayoutSpecDoesNotExecuteOrMutate()
    {
        var spec = service.CreateResponsiveDesktopLayoutSpec();
        var result = validator.ValidateLayoutSpec(spec);

        Assert.IsTrue(result.IsValid, string.Join(" | ", result.Errors));
        Assert.IsFalse(spec.CanExecuteOrMutateState);
        Assert.IsFalse(spec.CallsBrowserRuntime);
        Assert.IsFalse(spec.ModalTrapAllowed);
        Assert.IsFalse(spec.UsesExternalCssFramework);
    }

    [TestMethod]
    public void AcceptanceDocExistsAndDeclaresMissionControlDirection()
    {
        var doc = File.ReadAllText(AcceptanceDocPath());

        AssertContains(doc, "dark-first");
        AssertContains(doc, "Mission Control");
        AssertContains(doc, "classic ERP/dashboard");
        AssertContains(doc, "RPA/workflow designer");
        AssertContains(doc, "timeline central");
    }

    [TestMethod]
    public void AcceptanceDocRequiresDisabledApprovalEvidenceLogGuardrailsAndNaming()
    {
        var doc = File.ReadAllText(AcceptanceDocPath());

        AssertContains(doc, "approval disabled/no-authority");
        AssertContains(doc, "evidence ref-only");
        AssertContains(doc, "observability/log");
        AssertContains(doc, "guardrail explainers");
        AssertContains(doc, "no-runtime/no-cloud/no-LLM");
        AssertContains(doc, "NODAL OS");
    }

    [TestMethod]
    public void StaticPreviewArtifactExistsAndIsSafe()
    {
        var html = File.ReadAllText(StaticPreviewPath());

        AssertContains(html, "NODAL OS");
        AssertContains(html, "Read-only preview");
        AssertContains(html, "No runtime execution");
        AssertDoesNotContainForbiddenNames(html);
        AssertDoesNotContainSecrets(html);
    }

    [TestMethod]
    public void Boundary_NewVisualFiles_DoNotReferenceBrowserExecutorOrRuntimePrimitives()
    {
        var source = NewVisualSourceText();

        AssertDoesNotContain(source, "OneBrain.BrowserExecutor.Cdp");
        AssertDoesNotContain(source, "HttpClient");
        AssertDoesNotContain(source, "ClientWebSocket");
        AssertDoesNotContain(source, "Process.Start");
        AssertDoesNotContain(source, "System.Diagnostics.Process");
        AssertDoesNotContain(source, "BackgroundService");
        AssertDoesNotContain(source, "Task.Run");
        AssertDoesNotContain(source, "new Timer(");
        AssertDoesNotContain(source, "new Thread(");
    }

    [TestMethod]
    public void Boundary_NoExecutionCloudLlmTelemetryOrPersistenceWiringIntroduced()
    {
        var source = NewVisualSourceText();

        AssertDoesNotContain(source, "ExecuteAsync");
        AssertDoesNotContain(source, "RunAsync");
        AssertDoesNotContain(source, "SendAsync");
        AssertDoesNotContain(source, "OpenAI");
        AssertDoesNotContain(source, "ProviderCall");
        AssertDoesNotContain(source, "CloudSync");
        AssertDoesNotContain(source, "TelemetryClient");
        AssertDoesNotContain(source, "AnalyticsClient");
        AssertDoesNotContain(source, "File.Write");
        AssertDoesNotContain(source, "Directory.CreateDirectory");
    }

    [TestMethod]
    public void ExistingSafety_GuidanceInteractionAndShellRemainNoOp()
    {
        var shell = NodalOsMissionControlShellFixtures.ShellPreview();
        var intent = NodalOsMissionControlInteractionFixtures.SelectTimelineIntent();
        var emptyState = new NodalOsMissionControlGuidanceService().CreateDefaultEmptyStates().First();

        Assert.IsTrue(shell.ReadOnlyUi);
        Assert.IsFalse(shell.CanAuthorizeExecution);
        Assert.IsTrue(intent.IsNoOp);
        Assert.IsFalse(intent.CanAuthorizeExecution);
        Assert.IsTrue(emptyState.IsReadOnly);
        Assert.IsFalse(emptyState.CanExecuteAction);
    }

    [TestMethod]
    public void ArtifactMarksStaticUxAcceptanceAndNoRuntime()
    {
        var artifact = File.ReadAllText(ArtifactPath());

        AssertContains(artifact, "\"missionControlVisualPolish\": true");
        AssertContains(artifact, "\"responsiveDesktopLayoutContract\": true");
        AssertContains(artifact, "\"staticUxAcceptancePack\": true");
        AssertContains(artifact, "\"visualPreviewOnly\": true");
        AssertContains(artifact, "\"runtimeExecutionAllowed\": false");
        AssertContains(artifact, "\"telemetryOrAnalyticsIntroduced\": false");
    }

    private string RenderHtml() =>
        service.RenderStaticUxPreview(
            NodalOsMissionControlShellFixtures.ShellPreview(),
            NodalOsMissionControlGuidanceFixtures.EmptyStates(),
            NodalOsMissionControlGuidanceFixtures.OnboardingSteps(),
            NodalOsMissionControlGuidanceFixtures.GuardrailExplainers(),
            service.CreateResponsiveDesktopLayoutSpec());

    private static string NewVisualSourceText() =>
        string.Join(Environment.NewLine,
            new[]
            {
                PathFor("src", "OneBrain.AgentOperations.Contracts", "NodalOsMissionControlVisualContracts.cs"),
                PathFor("src", "OneBrain.AgentOperations.Core", "NodalOsMissionControlVisualServices.cs")
            }.Select(File.ReadAllText));

    private static string ArtifactPath() =>
        PathFor("artifacts", "agent-operations", "m491", "mission-control-static-ux-acceptance-summary.json");

    private static string StaticPreviewPath() =>
        PathFor("artifacts", "agent-operations", "m491", "mission-control-static-ux-preview.html");

    private static string AcceptanceDocPath() =>
        PathFor("docs", "reports", "mission-control-static-ux-acceptance-m489-m491.md");

    private static void AssertDoesNotContainSecrets(string text)
    {
        foreach (var marker in SecretMarkers)
            AssertDoesNotContain(text, marker);
    }

    private static void AssertDoesNotContainForbiddenNames(string text)
    {
        foreach (var forbidden in ForbiddenOperationalNames)
            AssertDoesNotContain(text, forbidden);
    }

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
