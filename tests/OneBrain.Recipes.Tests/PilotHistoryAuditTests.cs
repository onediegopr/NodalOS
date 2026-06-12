using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.Core.History;
using OneBrain.Pilot;

namespace OneBrain.Recipes.Tests;

[TestClass]
public sealed class PilotHistoryAuditTests
{
    [TestMethod]
    public void Home_Render_Includes_Run_History_And_AI_Audit_Links()
    {
        var html = PilotHomePageRenderer.Render();

        StringAssert.Contains(html, "Historial");
        StringAssert.Contains(html, "/runs");
        StringAssert.Contains(html, "Decisiones de IA");
        StringAssert.Contains(html, "/ai/audit");
    }

    [TestMethod]
    public void Run_History_Render_Shows_Status_Safety_Artifacts_Approval_And_Confidence()
    {
        var html = PilotHomePageRenderer.RenderRunHistory(HistoryDemoFixture.CreateRunHistory());

        StringAssert.Contains(html, "Historial");
        StringAssert.Contains(html, "evidencia generada local solamente");
        StringAssert.Contains(html, "sin secretos guardados");
        StringAssert.Contains(html, "demo-product-evidence-html-report");
        StringAssert.Contains(html, "clicks=0; cookies=0; login=0; cart=0; purchase=0; payment=0");
        StringAssert.Contains(html, "approval-send-message-demo");
        StringAssert.Contains(html, "confidence-whatsapp-browser-demo-flow");
        StringAssert.Contains(html, "ai-audit-critical-send-preview");
        StringAssert.Contains(html, "artifacts/product-evidence-demo-html-reports");
    }

    [TestMethod]
    public void AI_Audit_Render_Shows_Profile_Risk_Budget_Fallback_And_No_Secrets_Claim()
    {
        var html = PilotHomePageRenderer.RenderAIAuditLog(HistoryDemoFixture.CreateAIAudit());

        StringAssert.Contains(html, "Decisiones de IA");
        StringAssert.Contains(html, "sin llamada a proveedor");
        StringAssert.Contains(html, "sin API keys");
        StringAssert.Contains(html, "OB_AI_CHEAP_INTENT");
        StringAssert.Contains(html, "OB_AI_CRITICAL_REASONER");
        StringAssert.Contains(html, "high");
        StringAssert.Contains(html, AIBudgetDecisions.Blocked);
        StringAssert.Contains(html, "Aprobacion humana");
    }
}
