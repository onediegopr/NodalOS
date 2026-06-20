using System.Text.Json;
using System.Text.Json.Serialization;
using OneBrain.AgentOperations.Contracts;

namespace OneBrain.AgentOperations.Core;

public sealed class NodalOsDryRunEvidencePlanService
{
    public NodalOsDryRunEvidencePlan CreatePlan(
        NodalOsScanDryRunRequest request,
        IReadOnlyList<NodalOsScanConsentReviewCard> cards)
    {
        var cardRefs = cards.Select(c => c.CardId).ToArray();
        return new()
        {
            EvidencePlanId = "dry-run-evidence-plan-m545",
            DryRunContractRef = request.DryRunId,
            ConsentReviewCardRefs = cardRefs,
            PlannedEvidenceItems = Enum.GetValues<NodalOsDryRunPlannedEvidenceKind>()
                .Select((kind, index) => Evidence(kind, index + 1, request.DryRunId))
                .ToArray(),
            PlannedTimelineEvents = Enum.GetValues<NodalOsDryRunTimelineEventType>()
                .Select((type, index) => Timeline(type, index + 1, request.DryRunId))
                .ToArray(),
            PlannedAuditRefs =
            [
                "real-scan-audit-gate-m539",
                "dry-run-review-audit-ref-m545"
            ],
            PlannedRedactionRefs =
            [
                "redaction-policy-ref-m545",
                "safe-output-policy-ref-m545"
            ],
            IsPlanOnly = true,
            EmitsRealEvidence = false,
            VerifiesRealContent = false,
            UsesRealFilesystem = false,
            BuildsLlmContext = false
        };
    }

    public NodalOsDryRunEvidencePlanReadiness Evaluate(NodalOsDryRunEvidencePlan plan) =>
        new()
        {
            ReadinessId = "dry-run-evidence-plan-readiness-m545",
            EvidencePlanRef = plan.EvidencePlanId,
            ReadyForRealDryRunEvidence = false,
            ReadyForRealScan = false,
            ReadyForRealEvidenceVerification = false,
            MissingRequirementsRedacted =
            [
                "Future scan evidence implementation audit.",
                "Future timeline emission audit.",
                "Future content verification policy."
            ],
            BlockersRedacted =
            [
                "Evidence plan is preview-only.",
                "No real evidence is emitted.",
                "No real content is verified."
            ],
            GuardrailRefs =
            [
                "guardrail-evidence-plan-only",
                "guardrail-no-real-evidence-emission",
                "guardrail-no-content-verification"
            ]
        };

    private static NodalOsDryRunPlannedEvidenceItem Evidence(
        NodalOsDryRunPlannedEvidenceKind kind,
        int index,
        string sourceRef) =>
        new()
        {
            PlannedEvidenceId = $"planned-evidence-{index:000}",
            Kind = kind,
            SourceRef = sourceRef,
            DescriptionRedacted = $"{kind} would be captured as a future redacted evidence ref.",
            RedactionRequirementRedacted = "Redaction required before any future evidence emission.",
            UserFacingExplanationRedacted = "This item is a plan only and contains refs, not real content.",
            CanContainRawContent = false,
            CanContainRawSecret = false,
            CanVerifyFilesystemContent = false
        };

    private static NodalOsDryRunPlannedTimelineEvent Timeline(
        NodalOsDryRunTimelineEventType type,
        int index,
        string sourceRef) =>
        new()
        {
            PlannedEventId = $"planned-timeline-event-{index:000}",
            EventType = type,
            DisplayTitleRedacted = $"{type}",
            DescriptionRedacted = $"{type} would be displayed as a future timeline preview.",
            SeverityRedacted = "Info",
            SourceRefs = [sourceRef],
            IsPreviewOnly = true,
            Emitted = false
        };
}

public sealed class NodalOsDryRunEvidencePlanJsonSerializer
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
        WriteIndented = true
    };

    public string SerializePlan(NodalOsDryRunEvidencePlan plan) =>
        JsonSerializer.Serialize(plan, Options);

    public string SerializeReadiness(NodalOsDryRunEvidencePlanReadiness readiness) =>
        JsonSerializer.Serialize(readiness, Options);
}

public static class NodalOsDryRunEvidencePlanFixtures
{
    public static NodalOsDryRunEvidencePlan Plan() =>
        new NodalOsDryRunEvidencePlanService().CreatePlan(
            NodalOsScanDryRunFixtures.Request(),
            NodalOsScanConsentReviewCardsFixtures.Cards());
}
