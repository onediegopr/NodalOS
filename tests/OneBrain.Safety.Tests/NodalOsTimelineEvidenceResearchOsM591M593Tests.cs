using Microsoft.VisualStudio.TestTools.UnitTesting;
using TextStore = System.IO.File;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("TimelineEvidenceResearchOs")]
[TestCategory("NamingCleanup")]
public sealed class NodalOsTimelineEvidenceResearchOsM591M593Tests
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
    public void TimelineEvidenceResearchOsArtifacts_Exist()
    {
        AssertExists("artifacts", "agent-operations", "m593", "timeline-research-journal.json");
        AssertExists("artifacts", "agent-operations", "m593", "evidence-archive-visual-system.json");
        AssertExists("artifacts", "agent-operations", "m593", "static-traceability-qa-pack.json");
        AssertExists("artifacts", "agent-operations", "m593", "timeline-evidence-research-os-summary.json");
        AssertExists("artifacts", "agent-operations", "m593", "timeline-evidence-research-os-preview.html");
        AssertExists("docs", "reports", "timeline-evidence-research-os-m591-m593.md");
    }

    [TestMethod]
    public void TimelineResearchJournal_IncludesRequiredEntries()
    {
        var timeline = Timeline();

        AssertContains(timeline, "Consent Governance Closeout");
        AssertContains(timeline, "Consent Storage Boundary Ready");
        AssertContains(timeline, "Visual Reengineering Foundation");
        AssertContains(timeline, "Visual Foundation Phase 1");
        AssertContains(timeline, "Mission Control Research OS Layout");
        AssertContains(timeline, "Real Access Still Blocked");
        AssertContains(timeline, "Productive Consent Still Blocked");
        AssertContains(timeline, "File Read Capability Still Disabled");
        AssertContains(timeline, "\"status\": \"blocked\"");
        AssertContains(timeline, "\"status\": \"intentionally disabled\"");
    }

    [TestMethod]
    public void TimelineEntries_IncludeTraceRefsAndBlockedAnatomy()
    {
        var timeline = Timeline();

        AssertContains(timeline, "decisionRef");
        AssertContains(timeline, "evidenceRefs");
        AssertContains(timeline, "commitRef");
        AssertContains(timeline, "testSummary");
        AssertContains(timeline, "\"why\"");
        AssertContains(timeline, "\"missing\"");
        AssertContains(timeline, "\"evidenceRequired\"");
        AssertContains(timeline, "\"userActionNeeded\"");
        AssertContains(timeline, "\"intentionallyDisabled\"");
        AssertSafeOutput(timeline);
    }

    [TestMethod]
    public void EvidenceArchive_IncludesRequiredArtifactsAndCardAnatomy()
    {
        var archive = Archive();

        AssertContains(archive, "Productive Consent Storage Implementation ADR");
        AssertContains(archive, "Consent Governance Closeout");
        AssertContains(archive, "Consent Storage Boundary Test Pack");
        AssertContains(archive, "Visual Reengineering Foundation");
        AssertContains(archive, "Research OS Design System");
        AssertContains(archive, "Mission Control Research OS Layout");
        AssertContains(archive, "Real Scan Readiness ADR");
        AssertContains(archive, "Operational Access Audit ADR");
        AssertContains(archive, "Type");
        AssertContains(archive, "Status");
        AssertContains(archive, "Linked Mission");
        AssertContains(archive, "Commit");
        AssertContains(archive, "Tests");
        AssertContains(archive, "Traceability");
    }

    [TestMethod]
    public void EvidenceArchive_IncludesGovernanceAndAuditStates()
    {
        var archive = Archive();

        AssertContains(archive, "Governance Baselines");
        AssertContains(archive, "ADRs");
        AssertContains(archive, "Test Matrices");
        AssertContains(archive, "Reports");
        AssertContains(archive, "Visual System Artifacts");
        AssertContains(archive, "Safety Gates");
        AssertContains(archive, "Blocker Closeouts");
        AssertContains(archive, "Approved");
        AssertContains(archive, "Audit Required");
        AssertContains(archive, "Blocked");
    }

    [TestMethod]
    public void TraceabilityQa_ConfirmsStaticVisualBoundaries()
    {
        var qa = Read("artifacts", "agent-operations", "m593", "static-traceability-qa-pack.json");
        var summary = Read("artifacts", "agent-operations", "m593", "timeline-evidence-research-os-summary.json");
        var combined = qa + summary;

        AssertContains(qa, "timeline is not a simple checklist");
        AssertContains(qa, "evidence is not generic attachment list");
        AssertContains(qa, "\"confirmsNoRuntime\": true");
        AssertContains(qa, "\"confirmsNoFilesystem\": true");
        AssertContains(qa, "\"confirmsNoEvidenceVerificationReal\": true");
        AssertContains(qa, "\"confirmsNoLlmProviderCloud\": true");
        AssertContains(qa, "\"confirmsNoProductiveConsent\": true");
        AssertContains(qa, "\"confirmsNoCapabilityEnablement\": true");
        AssertContains(summary, "\"canBecomeProductiveSourceOfTruth\": false");
        AssertContains(summary, "\"canVerifyEvidenceProductively\": false");
        AssertContains(summary, "\"canConnectRuntime\": false");
        AssertSafeOutput(combined);
    }

    [TestMethod]
    public void StaticHtmlPreview_HasRequiredVisualContentAndNoNetworkOrScriptSurfaces()
    {
        var html = Preview();

        AssertContains(html, "Timeline Research Journal");
        AssertContains(html, "Evidence Archive");
        AssertContains(html, "Mission Traceability");
        AssertContains(html, "Timeline is a research journal");
        AssertContains(html, "Evidence is a research archive");
        AssertContains(html, "Real Access Still Blocked");
        AssertContains(html, "Productive Consent Still Blocked");
        AssertDoesNotContain(html, "<script");
        AssertDoesNotContain(html, "https://");
        AssertDoesNotContain(html, "http://");
        AssertDoesNotContain(html, "c" + "dn");
        AssertDoesNotContain(html, "tele" + "metry");
        AssertSafeOutput(html);
    }

    [TestMethod]
    public void Boundary_NewTimelineEvidenceFiles_DoNotReferenceForbiddenRuntimeOrIoPrimitives()
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

    private static string Timeline() => Read("artifacts", "agent-operations", "m593", "timeline-research-journal.json");
    private static string Archive() => Read("artifacts", "agent-operations", "m593", "evidence-archive-visual-system.json");
    private static string Preview() => Read("artifacts", "agent-operations", "m593", "timeline-evidence-research-os-preview.html");
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
            Path.Combine("docs", "reports", "timeline-evidence-research-os-m591-m593.md"),
            Path.Combine("artifacts", "agent-operations", "m593", "timeline-research-journal.json"),
            Path.Combine("artifacts", "agent-operations", "m593", "evidence-archive-visual-system.json"),
            Path.Combine("artifacts", "agent-operations", "m593", "static-traceability-qa-pack.json"),
            Path.Combine("artifacts", "agent-operations", "m593", "timeline-evidence-research-os-summary.json"),
            Path.Combine("artifacts", "agent-operations", "m593", "timeline-evidence-research-os-preview.html")
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
