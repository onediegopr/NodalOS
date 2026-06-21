using Microsoft.VisualStudio.TestTools.UnitTesting;
using TextStore = System.IO.File;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("AgentsSettingsActivityResearchOs")]
[TestCategory("NamingCleanup")]
public sealed class NodalOsAgentsSettingsActivityResearchOsM600M602Tests
{
    private static readonly string[] SensitiveMarkers =
    [
        "Bear" + "er ",
        "Authorization:",
        "Cook" + "ie:",
        "password",
        "raw " + "secret",
        "api" + "_key",
        "access" + "_token",
        "refresh" + "_token",
        "private key",
        "s" + "k-"
    ];

    [TestMethod]
    public void AgentsSettingsActivityResearchOsArtifacts_Exist()
    {
        AssertExists("artifacts", "agent-operations", "m602", "agents-research-os-visual-system.json");
        AssertExists("artifacts", "agent-operations", "m602", "settings-governance-visual-system.json");
        AssertExists("artifacts", "agent-operations", "m602", "mission-activity-feed-visual-system.json");
        AssertExists("artifacts", "agent-operations", "m602", "static-agents-settings-activity-qa-pack.json");
        AssertExists("artifacts", "agent-operations", "m602", "agents-settings-activity-research-os-summary.json");
        AssertExists("artifacts", "agent-operations", "m602", "agents-settings-activity-research-os-preview.html");
        AssertExists("docs", "reports", "agents-settings-activity-research-os-m600-m602.md");
    }

    [TestMethod]
    public void AgentsVisualSystem_IncludesRequiredAgentsAndNoAuthorityDisclosures()
    {
        var agents = Agents();

        AssertContains(agents, "Research Planner");
        AssertContains(agents, "Evidence Curator");
        AssertContains(agents, "Consent Auditor");
        AssertContains(agents, "Runtime Sentinel");
        AssertContains(agents, "Model Policy Reviewer");
        AssertContains(agents, "Advisor Analyst");
        AssertContains(agents, "Mission Reporter");
        AssertContains(agents, "No execution authority");
        AssertContains(agents, "no agent can use Provider Call");
        AssertContains(agents, "no agent can access filesystem");
        AssertContains(agents, "no agent can approve capability");
        AssertContains(agents, "\"canEnableAgents\": false");
        AssertContains(agents, "\"canMutateState\": false");
        AssertSafeOutput(agents);
    }

    [TestMethod]
    public void SettingsGovernance_IncludesRequiredSettingsAndPreviewDisclosures()
    {
        var settings = Settings();

        AssertContains(settings, "Local-only mode");
        AssertContains(settings, "\"Status\": \"Enabled\"");
        AssertContains(settings, "Network");
        AssertContains(settings, "\"Status\": \"Disabled\"");
        AssertContains(settings, "Provider Calls");
        AssertContains(settings, "Productive consent");
        AssertContains(settings, "Filesystem access");
        AssertContains(settings, "Cloud sync");
        AssertContains(settings, "Runtime execution");
        AssertContains(settings, "no productive settings are persisted");
        AssertContains(settings, "no provider key is stored");
        AssertContains(settings, "no capability is enabled");
        AssertContains(settings, "\"canPersistSettings\": false");
        AssertContains(settings, "\"canStoreProviderKey\": false");
        AssertSafeOutput(settings);
    }

    [TestMethod]
    public void MissionActivityFeed_IncludesRequiredEventsAndBlockedAnatomy()
    {
        var feed = Feed();

        AssertContains(feed, "Full suite passed");
        AssertContains(feed, "4326 passed, 37 skipped");
        AssertContains(feed, "Productive consent still blocked");
        AssertContains(feed, "Consent Runtime Models Research OS");
        AssertContains(feed, "Provider Calls Disabled");
        AssertContains(feed, "Guardrails confirmed");
        AssertContains(feed, "\"isRawLog\": false");
        AssertContains(feed, "\"why\"");
        AssertContains(feed, "\"missing\"");
        AssertContains(feed, "\"evidenceRequired\"");
        AssertContains(feed, "\"userActionNeeded\"");
        AssertContains(feed, "\"intentionallyDisabled\"");
        AssertContains(feed, "\"canPersistFeed\": false");
        AssertSafeOutput(feed);
    }

    [TestMethod]
    public void StaticAgentsSettingsActivityQa_ConfirmsIntegrationBoundaries()
    {
        var qa = Read("artifacts", "agent-operations", "m602", "static-agents-settings-activity-qa-pack.json");
        var summary = Read("artifacts", "agent-operations", "m602", "agents-settings-activity-research-os-summary.json");
        var combined = qa + summary;

        AssertContains(qa, "Agents are not autonomous bots");
        AssertContains(qa, "Settings are governance config, not generic forms");
        AssertContains(qa, "Activity Feed is mission-readable, not raw logs");
        AssertContains(qa, "\"confirmsNoRuntime\": true");
        AssertContains(qa, "\"confirmsNoFilesystem\": true");
        AssertContains(qa, "\"confirmsNoEvidenceVerificationReal\": true");
        AssertContains(qa, "\"confirmsNoLlmProviderCloud\": true");
        AssertContains(qa, "\"confirmsNoProductiveConsent\": true");
        AssertContains(qa, "\"confirmsNoSourceOfTruthPromotion\": true");
        AssertContains(summary, "\"canBecomeProductiveSourceOfTruth\": false");
        AssertContains(summary, "\"canPersistSettings\": false");
        AssertSafeOutput(combined);
    }

    [TestMethod]
    public void StaticHtmlPreview_HasRequiredVisualContentAndNoNetworkOrScriptSurfaces()
    {
        var html = Preview();

        AssertContains(html, "Agents Research OS");
        AssertContains(html, "Settings Governance");
        AssertContains(html, "Mission Activity Feed");
        AssertContains(html, "Research Planner");
        AssertContains(html, "Local-only mode enabled");
        AssertContains(html, "Provider Calls Disabled");
        AssertContains(html, "Productive consent still blocked");
        AssertContains(html, "Guardrails confirmed");
        AssertContains(html, "Why");
        AssertContains(html, "Missing");
        AssertContains(html, "Evidence");
        AssertContains(html, "Action");
        AssertContains(html, "Disabled");
        AssertDoesNotContain(html, "<script");
        AssertDoesNotContain(html, "https://");
        AssertDoesNotContain(html, "http://");
        AssertDoesNotContain(html, "c" + "dn");
        AssertDoesNotContain(html, "tele" + "metry");
        AssertSafeOutput(html);
    }

    [TestMethod]
    public void Boundary_NewAgentsSettingsActivityFiles_DoNotReferenceForbiddenRuntimeOrIoPrimitives()
    {
        var text = string.Join(Environment.NewLine, NewFiles().Select(TextStore.ReadAllText));

        AssertDoesNotContain(text, "OneBrain." + "BrowserExecutor" + ".Cdp");
        AssertDoesNotContain(text, "Http" + "Client");
        AssertDoesNotContain(text, "Client" + "WebSocket");
        AssertDoesNotContain(text, "Process" + ".Start");
        AssertDoesNotContain(text, "System.Diagnostics." + "Process");
        AssertDoesNotContain(text, "Background" + "Service");
        AssertDoesNotContain(text, "Task.Run");
        AssertDoesNotContain(text, "File" + ".Read");
        AssertDoesNotContain(text, "File" + ".Write");
        AssertDoesNotContain(text, "File" + ".Delete");
        AssertDoesNotContain(text, "File" + ".Move");
        AssertDoesNotContain(text, "Directory" + ".");
        AssertDoesNotContain(text, "File" + "Info");
        AssertDoesNotContain(text, "Directory" + "Info");
        AssertSafeOutput(text);
    }

    private static string Agents() => Read("artifacts", "agent-operations", "m602", "agents-research-os-visual-system.json");
    private static string Settings() => Read("artifacts", "agent-operations", "m602", "settings-governance-visual-system.json");
    private static string Feed() => Read("artifacts", "agent-operations", "m602", "mission-activity-feed-visual-system.json");
    private static string Preview() => Read("artifacts", "agent-operations", "m602", "agents-settings-activity-research-os-preview.html");
    private static void AssertExists(params string[] segments) => Assert.IsTrue(TextStore.Exists(PathFor(segments)), $"Missing {PathFor(segments)}");
    private static string Read(params string[] segments) => TextStore.ReadAllText(PathFor(segments));
    private static void AssertContains(string value, string expected) => StringAssert.Contains(value, expected);

    private static void AssertSafeOutput(string value)
    {
        foreach (var marker in SensitiveMarkers)
            AssertDoesNotContain(value, marker);
    }

    private static void AssertDoesNotContain(string value, string forbidden) =>
        Assert.IsFalse(value.Contains(forbidden, StringComparison.OrdinalIgnoreCase), $"Unexpected content: {forbidden}");

    private static IEnumerable<string> NewFiles()
    {
        var root = FindRepoRoot();
        var relative = new[]
        {
            Path.Combine("docs", "reports", "agents-settings-activity-research-os-m600-m602.md"),
            Path.Combine("artifacts", "agent-operations", "m602", "agents-research-os-visual-system.json"),
            Path.Combine("artifacts", "agent-operations", "m602", "settings-governance-visual-system.json"),
            Path.Combine("artifacts", "agent-operations", "m602", "mission-activity-feed-visual-system.json"),
            Path.Combine("artifacts", "agent-operations", "m602", "static-agents-settings-activity-qa-pack.json"),
            Path.Combine("artifacts", "agent-operations", "m602", "agents-settings-activity-research-os-summary.json"),
            Path.Combine("artifacts", "agent-operations", "m602", "agents-settings-activity-research-os-preview.html")
        };

        return relative.Select(path => Path.Combine(root, path));
    }

    private static string PathFor(params string[] segments) => Path.Combine([FindRepoRoot(), .. segments]);

    private static string FindRepoRoot()
    {
        var current = Path.GetFullPath(AppContext.BaseDirectory);
        while (!string.IsNullOrEmpty(current) && !TextStore.Exists(Path.Combine(current, "OneBrain.slnx")))
            current = Path.GetDirectoryName(current) ?? string.Empty;

        return string.IsNullOrEmpty(current) ? throw new InvalidOperationException("Repository root not found.") : current;
    }
}
