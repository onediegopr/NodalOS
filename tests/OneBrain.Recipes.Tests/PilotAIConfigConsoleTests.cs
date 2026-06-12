using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.Core.AI;
using OneBrain.Pilot;

namespace OneBrain.Recipes.Tests;

[TestClass]
public sealed class PilotAIConfigConsoleTests
{
    [TestMethod]
    public void Home_Render_Includes_AI_Config_Link()
    {
        var html = PilotHomePageRenderer.Render();

        StringAssert.Contains(html, "AI model router");
        StringAssert.Contains(html, "/ai/config");
    }

    [TestMethod]
    public void AI_Config_Console_Shows_Four_Official_Profiles()
    {
        var html = PilotHomePageRenderer.RenderAIConfigConsole(ConfiguredProfiles());

        StringAssert.Contains(html, "Cheap Intent Engine");
        StringAssert.Contains(html, "Standard Task Engine");
        StringAssert.Contains(html, "Critical Reasoner");
        StringAssert.Contains(html, "Vision Verifier");
        StringAssert.Contains(html, "OB_AI_CHEAP_INTENT");
        StringAssert.Contains(html, "OB_AI_STANDARD_TASK");
        StringAssert.Contains(html, "OB_AI_CRITICAL_REASONER");
        StringAssert.Contains(html, "OB_AI_VISION_VERIFIER");
    }

    [TestMethod]
    public void AI_Config_Console_Masks_Keys_And_Does_Not_Show_Full_Secret()
    {
        var profiles = AIModelConfiguration.LoadOfficialProfiles(new Dictionary<string, string?>
        {
            ["OB_AI_CHEAP_INTENT_PROVIDER"] = "openai",
            ["OB_AI_CHEAP_INTENT_MODEL"] = "configured-model",
            ["OB_AI_CHEAP_INTENT_API_KEY"] = "sk-test-secret-A91F"
        });

        var html = PilotHomePageRenderer.RenderAIConfigConsole(profiles);

        StringAssert.Contains(html, "sk-...A91F");
        Assert.IsFalse(html.Contains("sk-test-secret-A91F", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(html.Contains("test-secret", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public void AI_Config_Console_Shows_Dry_Run_Routing_Result()
    {
        var profiles = ConfiguredProfiles();
        var result = new AIModelRouter().Route(
            new AIModelRoutingRequest("mostrame la demo", AIModelCapabilities.Intent, AIRiskLevels.Low, false, false, false, 0.01m, 1, "local", "pilot"),
            new AIModelRoutingPolicy(profiles, []));

        var html = PilotHomePageRenderer.RenderAIConfigConsole(profiles, result);

        StringAssert.Contains(html, "Run dry-run routing test");
        StringAssert.Contains(html, "selectedProfile=OB_AI_CHEAP_INTENT");
        StringAssert.Contains(html, "wouldCallProvider=False");
    }

    private static IReadOnlyList<AIModelProfile> ConfiguredProfiles()
    {
        return AIModelConfiguration.LoadOfficialProfiles(new Dictionary<string, string?>
        {
            ["OB_AI_CHEAP_INTENT_PROVIDER"] = "mock",
            ["OB_AI_CHEAP_INTENT_MODEL"] = "mock-cheap",
            ["OB_AI_STANDARD_TASK_PROVIDER"] = "mock",
            ["OB_AI_STANDARD_TASK_MODEL"] = "mock-standard",
            ["OB_AI_CRITICAL_REASONER_PROVIDER"] = "mock",
            ["OB_AI_CRITICAL_REASONER_MODEL"] = "mock-critical",
            ["OB_AI_VISION_VERIFIER_PROVIDER"] = "mock",
            ["OB_AI_VISION_VERIFIER_MODEL"] = "mock-vision"
        });
    }
}
