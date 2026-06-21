using Microsoft.VisualStudio.TestTools.UnitTesting;
using TextStore = System.IO.File;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("ConsentRuntimeModelsResearchOs")]
[TestCategory("NamingCleanup")]
public sealed class NodalOsConsentRuntimeModelsResearchOsM597M599Tests
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
    public void ConsentRuntimeModelsResearchOsArtifacts_Exist()
    {
        AssertExists("artifacts", "agent-operations", "m599", "consent-governance-console.json");
        AssertExists("artifacts", "agent-operations", "m599", "runtime-local-first-safety.json");
        AssertExists("artifacts", "agent-operations", "m599", "models-policy-visual-system.json");
        AssertExists("artifacts", "agent-operations", "m599", "static-consent-runtime-models-qa-pack.json");
        AssertExists("artifacts", "agent-operations", "m599", "consent-runtime-models-research-os-summary.json");
        AssertExists("artifacts", "agent-operations", "m599", "consent-runtime-models-research-os-preview.html");
        AssertExists("docs", "reports", "consent-runtime-models-research-os-m597-m599.md");
    }

    [TestMethod]
    public void ConsentGovernanceConsole_IncludesRequiredCapabilitiesAndPolicies()
    {
        var consent = Consent();

        AssertContains(consent, "File Read");
        AssertContains(consent, "\"Status\": \"Blocked\"");
        AssertContains(consent, "Directory Listing");
        AssertContains(consent, "File Hash");
        AssertContains(consent, "Indexing");
        AssertContains(consent, "Embeddings");
        AssertContains(consent, "LLM Context Build");
        AssertContains(consent, "Cloud Sync");
        AssertContains(consent, "Provider Call");
        AssertContains(consent, "Runtime Execution");
        AssertContains(consent, "no productive consent accidental");
        AssertContains(consent, "File Read consent does not imply indexing, Embeddings or LLM context");
        AssertContains(consent, "\"canEnableCapability\": false");
        AssertContains(consent, "\"canPersistConsent\": false");
        AssertSafeOutput(consent);
    }

    [TestMethod]
    public void RuntimeLocalFirstSafety_IncludesRequiredBlockedStatesAndAnatomy()
    {
        var runtime = Runtime();

        AssertContains(runtime, "\"Runtime Status\": \"Local-only\"");
        AssertContains(runtime, "\"Network\": \"Disabled\"");
        AssertContains(runtime, "\"File Access\": \"Blocked\"");
        AssertContains(runtime, "\"Indexing\": \"Disabled\"");
        AssertContains(runtime, "\"Embeddings\": \"Disabled\"");
        AssertContains(runtime, "\"Provider Calls\": \"Disabled\"");
        AssertContains(runtime, "File Read blocked");
        AssertContains(runtime, "File Hash blocked");
        AssertContains(runtime, "LLM context blocked");
        AssertContains(runtime, "\"why\"");
        AssertContains(runtime, "\"missing\"");
        AssertContains(runtime, "\"evidenceRequired\"");
        AssertContains(runtime, "\"userActionNeeded\"");
        AssertContains(runtime, "\"intentionallyDisabled\"");
        AssertContains(runtime, "\"canStartRuntime\": false");
        AssertContains(runtime, "\"canExecuteTask\": false");
        AssertSafeOutput(runtime);
    }

    [TestMethod]
    public void ModelsPolicyVisualSystem_IncludesRequiredPolicyWarnings()
    {
        var models = Models();

        AssertContains(models, "Primary Model");
        AssertContains(models, "OpenAI GPT-5.5");
        AssertContains(models, "Fallback");
        AssertContains(models, "GPT-5.5 Mini / cheaper model");
        AssertContains(models, "Provider Calls");
        AssertContains(models, "Disabled");
        AssertContains(models, "No workspace context sent");
        AssertContains(models, "BYOK not implemented");
        AssertContains(models, "Managed AI not enabled");
        AssertContains(models, "model configured does not mean provider activity allowed");
        AssertContains(models, "\"canCallProvider\": false");
        AssertContains(models, "\"canUseCloud\": false");
        AssertContains(models, "\"canBuildWorkspaceContext\": false");
        AssertSafeOutput(models);
    }

    [TestMethod]
    public void StaticConsentRuntimeModelsQa_ConfirmsGovernanceBoundaries()
    {
        var qa = Read("artifacts", "agent-operations", "m599", "static-consent-runtime-models-qa-pack.json");
        var summary = Read("artifacts", "agent-operations", "m599", "consent-runtime-models-research-os-summary.json");
        var combined = qa + summary;

        AssertContains(qa, "Consent looks like Governance Console, not settings");
        AssertContains(qa, "Runtime communicates local-first safety");
        AssertContains(qa, "Models communicate policy, not just selection");
        AssertContains(qa, "\"confirmsNoRuntime\": true");
        AssertContains(qa, "\"confirmsNoFilesystem\": true");
        AssertContains(qa, "\"confirmsNoEvidenceVerificationReal\": true");
        AssertContains(qa, "\"confirmsNoLlmProviderCloud\": true");
        AssertContains(qa, "\"confirmsNoProductiveConsent\": true");
        AssertContains(qa, "\"confirmsNoSourceOfTruthPromotion\": true");
        AssertContains(summary, "\"canBecomeProductiveSourceOfTruth\": false");
        AssertContains(summary, "\"canCallProvider\": false");
        AssertSafeOutput(combined);
    }

    [TestMethod]
    public void StaticHtmlPreview_HasRequiredVisualContentAndNoNetworkOrScriptSurfaces()
    {
        var html = Preview();

        AssertContains(html, "Consent Governance Console");
        AssertContains(html, "Runtime Local-First Safety");
        AssertContains(html, "Models Policy");
        AssertContains(html, "File Read blocked");
        AssertContains(html, "Provider Calls Disabled");
        AssertContains(html, "No workspace context sent");
        AssertContains(html, "No source-of-truth promotion");
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
    public void Boundary_NewConsentRuntimeModelsFiles_DoNotReferenceForbiddenRuntimeOrIoPrimitives()
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

    private static string Consent() => Read("artifacts", "agent-operations", "m599", "consent-governance-console.json");
    private static string Runtime() => Read("artifacts", "agent-operations", "m599", "runtime-local-first-safety.json");
    private static string Models() => Read("artifacts", "agent-operations", "m599", "models-policy-visual-system.json");
    private static string Preview() => Read("artifacts", "agent-operations", "m599", "consent-runtime-models-research-os-preview.html");
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
            Path.Combine("docs", "reports", "consent-runtime-models-research-os-m597-m599.md"),
            Path.Combine("artifacts", "agent-operations", "m599", "consent-governance-console.json"),
            Path.Combine("artifacts", "agent-operations", "m599", "runtime-local-first-safety.json"),
            Path.Combine("artifacts", "agent-operations", "m599", "models-policy-visual-system.json"),
            Path.Combine("artifacts", "agent-operations", "m599", "static-consent-runtime-models-qa-pack.json"),
            Path.Combine("artifacts", "agent-operations", "m599", "consent-runtime-models-research-os-summary.json"),
            Path.Combine("artifacts", "agent-operations", "m599", "consent-runtime-models-research-os-preview.html")
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
