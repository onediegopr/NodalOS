using System.Text.Json;
using System.Text.Json.Serialization;
using OneBrain.AgentOperations.Contracts;

namespace OneBrain.AgentOperations.Core;

public sealed class NodalOsNextPhaseAdrService
{
    private static readonly DateTimeOffset FixtureTime = new(2026, 6, 20, 0, 0, 0, TimeSpan.Zero);

    public NodalOsNextPhaseAdr CreateAdr()
    {
        return new()
        {
            AdrId = "adr-governed-project-understanding-preconditions-m536",
            TitleRedacted = "ADR — Governed Project Understanding Preconditions Before Real Scan / LLM Context",
            ContextRedacted =
                "NODAL OS has completed the Assignment/Planner Preview governance phase (M519-M533). " +
                "The project is now preparing for the next governed phase. " +
                "Project Understanding (real filesystem scan, indexing, embeddings, LLM context build) " +
                "is the natural next capability but requires extensive preconditions to maintain " +
                "the local-first, no-execution, no-cloud-by-default, policy-first invariants. " +
                "This ADR defines the decision not to move directly to real Project Understanding " +
                "and establishes the governance gates required before any real capability can be enabled.",
            DecisionSummaryRedacted =
                "NODAL OS will NOT advance directly to real Project Understanding. " +
                "Instead, a Governed Project Understanding Preconditions phase (M534-M536) " +
                "defines all required gates explicitly. Real scan, file read, indexing, embeddings, " +
                "LLM context build, and cloud sync remain blocked until future governed milestones " +
                "implement each capability with its associated policy, consent, audit, and evidence plan.",
            ConsequencesRedacted =
                "Positive: All capabilities remain blocked by structural preconditions, not just documentation. " +
                "Assignment/Planner outputs can only feed Project Understanding as governance context refs, never as execution authority. " +
                "Each future real capability requires its own milestone, audit, and consent gate. " +
                "Negative: Project Understanding real capability is deferred; timeline impact depends on future milestones.",
            Decisions =
            [
                NodalOsNextPhaseAdrDecision.NoDirectMoveToRealProjectUnderstanding,
                NodalOsNextPhaseAdrDecision.RealScanBlockedUntilFutureMilestone,
                NodalOsNextPhaseAdrDecision.FilesystemScanBlockedUntilGatesDefined,
                NodalOsNextPhaseAdrDecision.LlmContextBuildBlockedUntilFutureMilestone,
                NodalOsNextPhaseAdrDecision.EmbeddingsBlockedUntilFutureMilestone,
                NodalOsNextPhaseAdrDecision.IndexingBlockedUntilFutureMilestone,
                NodalOsNextPhaseAdrDecision.CloudSyncBlockedUntilFutureMilestone,
                NodalOsNextPhaseAdrDecision.AssignmentOutputsAreRefsAndGovernanceContextOnly,
                NodalOsNextPhaseAdrDecision.MockHistoryIsNotSourceOfTruth,
                NodalOsNextPhaseAdrDecision.ByokAndProviderPolicyRequiredBeforeLlm,
                NodalOsNextPhaseAdrDecision.PathJailAndConsentRequiredBeforeFilesystem,
                NodalOsNextPhaseAdrDecision.PositiveExecutionGateRequiredBeforeRuntime,
                NodalOsNextPhaseAdrDecision.SeparateAuditRequiredBeforeRuntime
            ],
            AcceptedAlternativesRedacted =
            [
                "Define preconditions fully before any real capability (accepted — this ADR).",
                "Maintain Assignment/Planner outputs as governance context only (accepted).",
                "Require a separate dedicated audit before any real scan or runtime (accepted)."
            ],
            RejectedAlternativesRedacted =
            [
                "Proceed directly to real filesystem scan without preconditions (rejected — violates policy-first invariant).",
                "Use Assignment/Planner mock history as LLM prompt source (rejected — mock is not authoritative).",
                "Reuse governance closeout as runtime permission (rejected — closeout is not execution authorization).",
                "Enable cloud sync by default for Project Understanding (rejected — no-cloud-by-default invariant).",
                "Build LLM context without BYOK and provider policy (rejected — BYOK required before provider calls)."
            ],
            Guardrails =
            [
                new()
                {
                    GuardrailId = "guardrail-no-real-scan-until-preconditions-met",
                    DescriptionRedacted = "Real filesystem scan is blocked until path jail, consent, scope preview, secret detection, exclusion policy, cancellation semantics, and audit are all implemented and validated.",
                    EnforcedStructurally = true,
                    RequiresSeparateMilestone = true
                },
                new()
                {
                    GuardrailId = "guardrail-no-llm-until-byok-policy",
                    DescriptionRedacted = "LLM context build, prompt generation, and provider calls are blocked until BYOK configuration, provider policy, prompt governance, and budget enforcement are implemented.",
                    EnforcedStructurally = true,
                    RequiresSeparateMilestone = true
                },
                new()
                {
                    GuardrailId = "guardrail-no-filesystem-mutation",
                    DescriptionRedacted = "Any scan or read operation must be structurally read-only. No file writes, no git commits, no state changes to the scanned workspace.",
                    EnforcedStructurally = true,
                    RequiresSeparateMilestone = false
                },
                new()
                {
                    GuardrailId = "guardrail-no-cloud-by-default",
                    DescriptionRedacted = "No cloud upload by default. Cloud sync requires a separate explicit policy, consent, and milestone.",
                    EnforcedStructurally = true,
                    RequiresSeparateMilestone = true
                },
                new()
                {
                    GuardrailId = "guardrail-assignment-outputs-are-refs-only",
                    DescriptionRedacted = "All Assignment/Planner M519-M533 outputs can only be used as governance context refs. They cannot serve as execution authority, LLM prompt source, or filesystem authority.",
                    EnforcedStructurally = true,
                    RequiresSeparateMilestone = false
                },
                new()
                {
                    GuardrailId = "guardrail-separate-audit-before-runtime",
                    DescriptionRedacted = "A dedicated architectural and security audit of any real scan, LLM, or runtime implementation must be completed before enabling the capability.",
                    EnforcedStructurally = false,
                    RequiresSeparateMilestone = true
                }
            ],
            RequiredNextMilestones =
            [
                new()
                {
                    MilestoneRef = "M537+ Path Jail Implementation",
                    PurposeRedacted = "Implement real path jail validation with defined boundaries, symlink protection, and escape prevention.",
                    BlocksRealScan = true,
                    BlocksLlmContext = false,
                    BlocksRuntime = false
                },
                new()
                {
                    MilestoneRef = "M537+ Consent UI and Scope Preview",
                    PurposeRedacted = "Implement explicit user consent mechanism and scan scope preview (file count, size estimate, excluded patterns).",
                    BlocksRealScan = true,
                    BlocksLlmContext = false,
                    BlocksRuntime = false
                },
                new()
                {
                    MilestoneRef = "M537+ Secret Detection Implementation",
                    PurposeRedacted = "Implement real secret and credential detection before any path, filename, or content is included in context.",
                    BlocksRealScan = true,
                    BlocksLlmContext = true,
                    BlocksRuntime = false
                },
                new()
                {
                    MilestoneRef = "M537+ BYOK and Provider Policy",
                    PurposeRedacted = "Implement BYOK configuration, provider policy, prompt governance, and budget enforcement before any LLM context build or provider call.",
                    BlocksRealScan = false,
                    BlocksLlmContext = true,
                    BlocksRuntime = false
                },
                new()
                {
                    MilestoneRef = "M537+ Real Scan Implementation Audit",
                    PurposeRedacted = "Dedicated architectural and security audit of real scan implementation before enabling it.",
                    BlocksRealScan = true,
                    BlocksLlmContext = true,
                    BlocksRuntime = true
                },
                new()
                {
                    MilestoneRef = "M537+ Positive Execution Gate",
                    PurposeRedacted = "Define and implement the positive execution gate (policy + approval + verification) required before any runtime execution.",
                    BlocksRealScan = false,
                    BlocksLlmContext = false,
                    BlocksRuntime = true
                }
            ],
            ExplicitNonGoalsRedacted =
            [
                "This ADR does not implement real filesystem scan.",
                "This ADR does not implement real directory listing.",
                "This ADR does not implement file read or file hashing.",
                "This ADR does not implement git commands.",
                "This ADR does not implement embeddings.",
                "This ADR does not implement index creation.",
                "This ADR does not implement LLM context building.",
                "This ADR does not implement prompt generation.",
                "This ADR does not implement provider calls.",
                "This ADR does not implement BYOK.",
                "This ADR does not implement cloud sync.",
                "This ADR does not implement runtime execution.",
                "This ADR does not implement a real planner.",
                "This ADR does not promote Assignment/Planner mock outputs to operational authority."
            ],
            EvidenceRefs =
            [
                "evidence-adr-m536-ref-only",
                "evidence-m531-m533-governance-closeout-ref",
                "evidence-m534-preconditions-ref-only"
            ],
            TimelineRefs =
            [
                "timeline-adr-created-m536",
                "timeline-next-phase-decision-recorded"
            ],
            RealProjectUnderstandingAllowed = false,
            RealScanAllowed = false,
            FilesystemReadAllowed = false,
            FilesystemHashAllowed = false,
            GitCommandsAllowed = false,
            EmbeddingsAllowed = false,
            IndexingAllowed = false,
            LlmContextBuildAllowed = false,
            PromptGenerationAllowed = false,
            LlmProviderCallAllowed = false,
            CloudSyncAllowed = false,
            CreatedAt = FixtureTime
        };
    }
}

public sealed class NodalOsNextPhaseAdrJsonSerializer
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
        WriteIndented = true
    };

    public string Serialize(NodalOsNextPhaseAdr adr) =>
        JsonSerializer.Serialize(adr, Options);
}

public static class NodalOsNextPhaseAdrFixtures
{
    public static NodalOsNextPhaseAdr Adr() =>
        new NodalOsNextPhaseAdrService().CreateAdr();
}
