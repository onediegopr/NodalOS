using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.Core.AI;

namespace OneBrain.Recipes.Tests;

[TestClass]
public sealed class AIModelRouterTests
{
    [TestMethod]
    public void Router_Selects_Cheap_Intent_For_Simple_Low_Risk_Intent()
    {
        var result = Route(new AIModelRoutingRequest("mostrame la demo", AIModelCapabilities.Intent, AIRiskLevels.Low, false, false, false, 0.01m, 1, "local", "pilot"));

        Assert.IsTrue(result.Decision.Success, result.Decision.Reason);
        Assert.AreEqual(AIProfileIds.CheapIntent, result.Decision.SelectedProfileId);
        Assert.IsFalse(result.Decision.WouldCallProvider);
    }

    [TestMethod]
    public void Router_Selects_Standard_Task_For_Normal_Operational_Task()
    {
        var result = Route(new AIModelRoutingRequest("resumi este reporte", AIModelCapabilities.StandardTask, AIRiskLevels.Medium, false, false, false, 0.02m, 1, "local", "pilot"));

        Assert.IsTrue(result.Decision.Success, result.Decision.Reason);
        Assert.AreEqual(AIProfileIds.StandardTask, result.Decision.SelectedProfileId);
    }

    [TestMethod]
    public void Router_Selects_Critical_Reasoner_For_Sensitive_Action_Text()
    {
        var result = Route(new AIModelRoutingRequest("send message to customer", AIModelCapabilities.StandardTask, AIRiskLevels.Low, false, false, false, 0.02m, 1, "local", "pilot"));

        Assert.IsTrue(result.Decision.Success, result.Decision.Reason);
        Assert.AreEqual(AIProfileIds.CriticalReasoner, result.Decision.SelectedProfileId);
    }

    [TestMethod]
    public void Router_Selects_Vision_Verifier_For_Vision_Request()
    {
        var result = Route(new AIModelRoutingRequest("verifica pantalla", AIModelCapabilities.VisionVerification, AIRiskLevels.Low, true, false, false, 0.02m, 1, "local", "pilot"));

        Assert.IsTrue(result.Decision.Success, result.Decision.Reason);
        Assert.AreEqual(AIProfileIds.VisionVerifier, result.Decision.SelectedProfileId);
    }

    [TestMethod]
    public void Router_Escalates_Vision_High_Risk_To_Critical_Reasoner()
    {
        var result = Route(new AIModelRoutingRequest("verifica pantalla antes de enviar", AIModelCapabilities.VisionVerification, AIRiskLevels.High, true, false, false, 0.02m, 1, "local", "pilot"));

        Assert.IsTrue(result.Decision.Success, result.Decision.Reason);
        Assert.AreEqual(AIProfileIds.CriticalReasoner, result.Decision.SelectedProfileId);
        StringAssert.Contains(string.Join(" ", result.Decision.Notes), "escalated");
    }

    [TestMethod]
    public void Disabled_Profile_Uses_Allowed_Fallback()
    {
        var profiles = ConfiguredProfiles()
            .Select(profile => profile.ProfileId == AIProfileIds.VisionVerifier ? profile with { Enabled = false } : profile)
            .ToList();

        var result = new AIModelRouter().Route(
            new AIModelRoutingRequest("verifica pantalla", AIModelCapabilities.VisionVerification, AIRiskLevels.Low, true, false, false, 0.02m, 1, "local", "pilot"),
            new AIModelRoutingPolicy(profiles, []));

        Assert.IsTrue(result.Decision.Success, result.Decision.Reason);
        Assert.AreEqual(AIProfileIds.CriticalReasoner, result.Decision.SelectedProfileId);
    }

    [TestMethod]
    public void Critical_Reasoner_Disabled_With_Null_Fallback_Fails_Closed()
    {
        var profiles = ConfiguredProfiles()
            .Select(profile => profile.ProfileId == AIProfileIds.CriticalReasoner ? profile with { Enabled = false, FallbackProfileId = null } : profile)
            .ToList();

        var result = new AIModelRouter().Route(
            new AIModelRoutingRequest("pay invoice", AIModelCapabilities.StandardTask, AIRiskLevels.High, false, false, true, 0.02m, 1, "local", "pilot"),
            new AIModelRoutingPolicy(profiles, []));

        Assert.IsFalse(result.Decision.Success);
        Assert.IsTrue(result.Decision.FailClosed);
        Assert.AreEqual("profile_disabled", result.Decision.Status);
    }

    [TestMethod]
    public void Budget_Exceeded_Blocks_Routing()
    {
        var profiles = ConfiguredProfiles()
            .Select(profile => profile.ProfileId == AIProfileIds.CheapIntent ? profile with { DailyBudgetUsd = 0.05m } : profile)
            .ToList();
        var usage = new[] { new AIModelUsageSnapshot(AIProfileIds.CheapIntent, 0.05m, 0.05m, 10) };

        var result = new AIModelRouter().Route(
            new AIModelRoutingRequest("mostrame la demo", AIModelCapabilities.Intent, AIRiskLevels.Low, false, false, false, 0.01m, 1, "local", "pilot"),
            new AIModelRoutingPolicy(profiles, usage));

        Assert.IsFalse(result.Decision.Success);
        Assert.AreEqual("budget_blocked", result.Decision.Status);
        Assert.IsTrue(result.Decision.FailClosed);
    }

    [TestMethod]
    public void Missing_Required_Config_Blocks_Routing()
    {
        var profiles = AIModelConfiguration.LoadOfficialProfiles(new Dictionary<string, string?>());

        var result = new AIModelRouter().Route(
            new AIModelRoutingRequest("mostrame la demo", AIModelCapabilities.Intent, AIRiskLevels.Low, false, false, false, 0.01m, 1, "local", "pilot"),
            new AIModelRoutingPolicy(profiles, []));

        Assert.IsFalse(result.Decision.Success);
        Assert.IsTrue(result.Decision.FailClosed);
        StringAssert.Contains(result.Decision.Reason, "missing model");
    }

    private static AIModelRouterResult Route(AIModelRoutingRequest request)
    {
        return new AIModelRouter().Route(request, new AIModelRoutingPolicy(ConfiguredProfiles(), []));
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
