using OneBrain.Core.Confidence;
using OneBrain.Core.Recording;

namespace OneBrain.Core.Approval;

public static class BusinessFlowDemoFixture
{
    public const string CandidateFlowId = "candidate-whatsapp-browser-demo-v0";
    public const string MessagePreview =
        "Hola, te comparto un presupuesto demo basado en evidencia local. Precio pendiente de verificacion humana antes de enviar.";

    public static ApprovalRequest CreateSendMessageApproval()
    {
        return ApprovalPolicy.CreateRequest(
            source: "pilot_business_flow_fixture",
            candidateFlowId: CandidateFlowId,
            actionKind: ApprovalActionKinds.Send,
            riskLevel: ApprovalRiskLevels.Critical,
            title: "Approval required: send demo WhatsApp/browser message",
            description: "The first business flow prepares a message preview and stops before sending.",
            preview: MessagePreview,
            notes:
            [
                "fixture/mock only",
                "no WhatsApp send action is executed",
                "no executable recipe is generated"
            ],
            createdAtUtc: new DateTimeOffset(2026, 06, 12, 10, 30, 0, TimeSpan.Zero));
    }

    public static ApprovalDecision CreateRejectedDecision(string reason = "No enviar todavia; requiere revision humana.")
    {
        return ApprovalPolicy.Decide(
            CreateSendMessageApproval(),
            ApprovalDecisionKinds.Rejected,
            reason,
            decidedBy: "pilot-demo",
            decidedAtUtc: new DateTimeOffset(2026, 06, 12, 10, 31, 0, TimeSpan.Zero));
    }

    public static RecipeConfidenceProfile CreateConfidenceProfile()
    {
        return RecipeConfidenceScorer.Score(new RecipeConfidenceInput(
            RecipeId: "",
            CandidateFlowId: CandidateFlowId,
            Status: RecipeConfidenceStatuses.Candidate,
            RiskLevel: RecipeConfidenceRiskLevels.Critical,
            Runs: 0,
            Successes: 0,
            Failures: 0,
            LastError: null,
            LastVerifiedAt: "2026-06-12T10:32:00Z",
            ApprovalRequiredUntil: null,
            Notes:
            [
                "first business flow is fixture/mock only",
                "send message requires approval and no executor exists in this hito"
            ]));
    }
}
