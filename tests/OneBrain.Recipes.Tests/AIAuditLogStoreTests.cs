using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.Core.AI;
using OneBrain.Core.History;

namespace OneBrain.Recipes.Tests;

[TestClass]
public sealed class AIAuditLogStoreTests
{
    [TestMethod]
    public void Write_And_Read_AI_Audit_Log_Under_Artifacts()
    {
        var temp = CreateTempDir();
        var record = CreateRecord();

        var result = AIAuditLogStore.Write(temp, record);
        var records = AIAuditLogStore.ReadAll(temp);

        Assert.IsTrue(result.Success, result.Error);
        Assert.IsTrue(File.Exists(result.Path));
        StringAssert.Contains(result.RelativePath.Replace('\\', '/'), "artifacts/ai-audit/");
        StringAssert.Contains(File.ReadAllText(result.Path), AIAuditLogStore.SchemaVersion);
        Assert.AreEqual(1, records.Count);
        Assert.AreEqual("OB_AI_CRITICAL_REASONER", records[0].RecommendedProfileId);
    }

    [TestMethod]
    public void FromRoutingResult_Records_Profile_Risk_Budget_And_Status()
    {
        var profiles = AIModelConfiguration.LoadOfficialProfiles(new Dictionary<string, string?>
        {
            ["OB_AI_CHEAP_INTENT_PROVIDER"] = "mock",
            ["OB_AI_CHEAP_INTENT_MODEL"] = "mock-cheap"
        });
        var result = new AIModelRouter().Route(
            new AIModelRoutingRequest("mostrame la demo", AIModelCapabilities.Intent, AIRiskLevels.Low, false, false, false, 0.01m, 1, "local", "pilot"),
            new AIModelRoutingPolicy(profiles, []));

        var audit = AIAuditLogStore.FromRoutingResult("audit-1", result, requiresHumanApproval: false);

        Assert.AreEqual("OB_AI_CHEAP_INTENT", audit.RecommendedProfileId);
        Assert.AreEqual("low", audit.RiskLevel);
        Assert.AreEqual(AIBudgetDecisions.Allowed, audit.BudgetDecision);
        Assert.AreEqual(AIAuditResultStatuses.Routed, audit.ResultStatus);
    }

    [TestMethod]
    public void Budget_Blocked_Decision_Is_Auditable()
    {
        var audit = CreateRecord() with
        {
            BudgetDecision = AIBudgetDecisions.Blocked,
            ResultStatus = AIAuditResultStatuses.FailedClosed,
            Reason = "budget exceeded"
        };

        var result = AIAuditLogStore.Write(CreateTempDir(), audit);

        Assert.IsTrue(result.Success, result.Error);
        var json = File.ReadAllText(result.Path);
        StringAssert.Contains(json, "budget exceeded");
        StringAssert.Contains(json, AIBudgetDecisions.Blocked);
    }

    [TestMethod]
    public void Writer_Fails_Closed_On_Secret_Like_Content()
    {
        var record = CreateRecord() with
        {
            Error = "OpenAI key " + "sk-" + "test-secret-123456789 leaked"
        };

        var result = AIAuditLogStore.Write(CreateTempDir(), record);

        Assert.IsFalse(result.Success);
        StringAssert.Contains(result.Error, "secret-like");
    }

    private static AIAuditRecord CreateRecord()
    {
        return new AIAuditRecord(
            AiAuditId: "audit-123",
            TimestampUtc: "2026-06-12T11:00:00Z",
            RecommendedProfileId: "OB_AI_CRITICAL_REASONER",
            UsedProfileId: null,
            Provider: "mock",
            Model: "mock-critical",
            TaskType: "critical_reasoning",
            RiskLevel: "high",
            RequiresVision: false,
            RequiresHumanApproval: true,
            FallbackUsed: false,
            FallbackFrom: null,
            FallbackTo: null,
            BudgetDecision: AIBudgetDecisions.Blocked,
            EstimatedCostUsd: 0.20m,
            ActualCostUsd: null,
            TokensIn: null,
            TokensOut: null,
            ResultStatus: AIAuditResultStatuses.FailedClosed,
            Reason: "send action requires approval",
            Error: "");
    }

    private static string CreateTempDir()
    {
        var path = Path.Combine(Path.GetTempPath(), "onebrain-ai-audit-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }
}
