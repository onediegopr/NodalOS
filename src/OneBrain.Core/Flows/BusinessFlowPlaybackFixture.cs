using OneBrain.Core.Approval;
using OneBrain.Core.Recording;

namespace OneBrain.Core.Flows;

public static class BusinessFlowPlaybackFixture
{
    public const string CandidateFlowId = "candidate-whatsapp-browser-demo-v0";

    public static CandidateFlowPromotionRequest CreatePromotionRequest()
    {
        var timeline = RecordingDemoFixture.CreateTimeline();
        return new CandidateFlowPromotionRequest(
            CandidateFlowId: CandidateFlowId,
            Title: "Presupuesto demo supervisado",
            Description: "Prepara un mensaje demo con evidencia local y frena antes de enviar.",
            Timeline: timeline,
            LinterPassed: true,
            VariablesResolvedOrDeclared: true,
            RiskPolicyConsistent: true,
            ApprovalPolicyPresent: true,
            HasBlockedActions: false,
            RequestedBy: "pilot-demo",
            CreatedAtUtc: "2026-06-12T11:00:00Z",
            Variables:
            [
                "cliente_demo",
                "mensaje_preview",
                "ruta_reporte_html"
            ],
            Notes:
            [
                "fixture/demo controlado",
                "no WhatsApp real",
                "no browser live required",
                "send message step requires approval and remains blocked without safe executor"
            ]);
    }

    public static PromotedCandidateFlow CreatePromotedFlow()
    {
        var result = CandidateFlowPromotionService.Promote(CreatePromotionRequest());
        return result.Flow ?? throw new InvalidOperationException("demo promotion should succeed");
    }

    public static SupervisedPlaybackSession CreatePlaybackSession()
    {
        return SupervisedPlaybackService.Start(CreatePromotedFlow(), new DateTimeOffset(2026, 06, 12, 11, 01, 00, TimeSpan.Zero));
    }

    public static ApprovalDecision CreateSendApprovalDecision()
    {
        var request = BusinessFlowDemoFixture.CreateSendMessageApproval();
        return ApprovalPolicy.Decide(
            request,
            ApprovalDecisionKinds.Approved,
            "Aprobado solo como decision auditada; no ejecutar envio real.",
            decidedBy: "pilot-demo",
            decidedAtUtc: new DateTimeOffset(2026, 06, 12, 11, 02, 00, TimeSpan.Zero));
    }
}
