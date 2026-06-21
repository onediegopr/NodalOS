using Microsoft.VisualStudio.TestTools.UnitTesting;
using TextStore = System.IO.File;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("DecisionAdvisorResearchOs")]
[TestCategory("NamingCleanup")]
public sealed class NodalOsDecisionAdvisorResearchOsM594M596Tests
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
    public void DecisionAdvisorResearchOsArtifacts_Exist()
    {
        AssertExists("artifacts", "agent-operations", "m596", "decision-room-research-os.json");
        AssertExists("artifacts", "agent-operations", "m596", "advisor-notes-visual-system.json");
        AssertExists("artifacts", "agent-operations", "m596", "static-decision-advisor-qa-pack.json");
        AssertExists("artifacts", "agent-operations", "m596", "decision-advisor-research-os-summary.json");
        AssertExists("artifacts", "agent-operations", "m596", "decision-advisor-research-os-preview.html");
        AssertExists("docs", "reports", "decision-advisor-research-os-m594-m596.md");
    }

    [TestMethod]
    public void DecisionRoom_IncludesRequiredDecisionsAndCardAnatomy()
    {
        var decisions = Decisions();

        AssertContains(decisions, "Enable productive consent storage?");
        AssertContains(decisions, "Enable real filesystem access?");
        AssertContains(decisions, "Allow File Read capability?");
        AssertContains(decisions, "Build LLM context from workspace?");
        AssertContains(decisions, "Promote synthetic baseline to real scan?");
        AssertContains(decisions, "Accept Visual Reengineering Foundation?");
        AssertContains(decisions, "Keep Advisor warning active?");
        AssertContains(decisions, "Recommendation");
        AssertContains(decisions, "Risk");
        AssertContains(decisions, "Evidence");
        AssertContains(decisions, "User Options");
    }

    [TestMethod]
    public void DecisionRoom_BlockedStatesIncludeRequiredAnatomyAndNoOpOptions()
    {
        var decisions = Decisions();

        AssertContains(decisions, "\"why\"");
        AssertContains(decisions, "\"missing\"");
        AssertContains(decisions, "\"evidenceRequired\"");
        AssertContains(decisions, "\"userActionNeeded\"");
        AssertContains(decisions, "\"intentionallyDisabled\"");
        AssertContains(decisions, "\"optionsAreNoOp\": true");
        AssertContains(decisions, "\"optionsArePreviewOnly\": true");
        AssertContains(decisions, "\"canAuthorizeDecision\": false");
        AssertContains(decisions, "\"canMutateApproval\": false");
        AssertSafeOutput(decisions);
    }

    [TestMethod]
    public void AdvisorNotes_IncludeRequiredAnatomyAndExamples()
    {
        var advisor = Advisor();

        AssertContains(advisor, "Concern");
        AssertContains(advisor, "Potential Impact");
        AssertContains(advisor, "Recommendation");
        AssertContains(advisor, "Evidence");
        AssertContains(advisor, "Severity");
        AssertContains(advisor, "Category");
        AssertContains(advisor, "Status");
        AssertContains(advisor, "Path jail is still design-only.");
        AssertContains(advisor, "Productive consent remains blocked.");
        AssertContains(advisor, "Evidence previews are not source-of-truth.");
        AssertContains(advisor, "Visual layer must not imply runtime authority.");
        AssertContains(advisor, "Cloud/provider activity remains disabled.");
        AssertContains(advisor, "Mission Control should not become a generic dashboard.");
        AssertContains(advisor, "Timeline must not become a simple checklist.");
        AssertContains(advisor, "Advisor must not become a chatbot.");
    }

    [TestMethod]
    public void AdvisorNotes_AreNotChatbotAndActionsAreNoOp()
    {
        var advisor = Advisor();

        AssertContains(advisor, "\"isChatbot\": false");
        AssertContains(advisor, "\"usesChatBubbles\": false");
        AssertContains(advisor, "\"canCallLlm\": false");
        AssertContains(advisor, "\"canCallProvider\": false");
        AssertContains(advisor, "\"canMutateState\": false");
        AssertContains(advisor, "\"canAuthorizeExecution\": false");
        AssertContains(advisor, "\"acknowledge\": \"preview only\"");
        AssertContains(advisor, "\"convertToDecision\": \"preview only\"");
        AssertSafeOutput(advisor);
    }

    [TestMethod]
    public void StaticDecisionAdvisorQa_ConfirmsStaticVisualBoundaries()
    {
        var qa = Read("artifacts", "agent-operations", "m596", "static-decision-advisor-qa-pack.json");
        var summary = Read("artifacts", "agent-operations", "m596", "decision-advisor-research-os-summary.json");
        var combined = qa + summary;

        AssertContains(qa, "decisions are not generic approval modals");
        AssertContains(qa, "advisor is not chatbot");
        AssertContains(qa, "no chat bubbles");
        AssertContains(qa, "\"confirmsNoRuntime\": true");
        AssertContains(qa, "\"confirmsNoFilesystem\": true");
        AssertContains(qa, "\"confirmsNoEvidenceVerificationReal\": true");
        AssertContains(qa, "\"confirmsNoLlmProviderCloud\": true");
        AssertContains(qa, "\"confirmsNoProductiveConsent\": true");
        AssertContains(qa, "\"confirmsNoCapabilityEnablement\": true");
        AssertContains(qa, "\"confirmsNoSourceOfTruthPromotion\": true");
        AssertContains(summary, "\"canBecomeProductiveSourceOfTruth\": false");
        AssertSafeOutput(combined);
    }

    [TestMethod]
    public void StaticHtmlPreview_HasRequiredVisualContentAndNoNetworkOrScriptSurfaces()
    {
        var html = Preview();

        AssertContains(html, "Decision Room");
        AssertContains(html, "Advisor Notes");
        AssertContains(html, "Enable productive consent storage?");
        AssertContains(html, "Enable real filesystem access?");
        AssertContains(html, "Visual layer must not imply runtime authority.");
        AssertContains(html, "Advisor must not become a chatbot.");
        AssertContains(html, "preview only");
        AssertDoesNotContain(html, "<script");
        AssertDoesNotContain(html, "https://");
        AssertDoesNotContain(html, "http://");
        AssertDoesNotContain(html, "c" + "dn");
        AssertDoesNotContain(html, "tele" + "metry");
        AssertSafeOutput(html);
    }

    [TestMethod]
    public void Boundary_NewDecisionAdvisorFiles_DoNotReferenceForbiddenRuntimeOrIoPrimitives()
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

    private static string Decisions() => Read("artifacts", "agent-operations", "m596", "decision-room-research-os.json");
    private static string Advisor() => Read("artifacts", "agent-operations", "m596", "advisor-notes-visual-system.json");
    private static string Preview() => Read("artifacts", "agent-operations", "m596", "decision-advisor-research-os-preview.html");
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
            Path.Combine("docs", "reports", "decision-advisor-research-os-m594-m596.md"),
            Path.Combine("artifacts", "agent-operations", "m596", "decision-room-research-os.json"),
            Path.Combine("artifacts", "agent-operations", "m596", "advisor-notes-visual-system.json"),
            Path.Combine("artifacts", "agent-operations", "m596", "static-decision-advisor-qa-pack.json"),
            Path.Combine("artifacts", "agent-operations", "m596", "decision-advisor-research-os-summary.json"),
            Path.Combine("artifacts", "agent-operations", "m596", "decision-advisor-research-os-preview.html")
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
