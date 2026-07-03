namespace OneBrain.Core.Approval;

public enum ImplementationPlanningGateStatus
{
    DesignOnly,
    BlockedNoGo
}

public enum ImplementationCapabilityCandidateDecision
{
    Blocked,
    FutureCandidateBlockedByAudit,
    ApprovedForImplementation
}

public enum ImplementationRiskLevel
{
    Low,
    Medium,
    High,
    Critical
}

public sealed record ImplementationCapabilityCandidate(
    string CandidateId,
    string CandidateName,
    string BusinessProductValue,
    ImplementationRiskLevel TechnicalRisk,
    ImplementationRiskLevel SafetyRisk,
    ImplementationRiskLevel DataRisk,
    IReadOnlyList<string> RequiredPreconditions,
    IReadOnlyList<string> RequiredNegativeTests,
    IReadOnlyList<string> RequiredExternalAudits,
    IReadOnlyList<string> RequiredRollbackNoSideEffectProof,
    string RequiredIsolatedScope,
    string WhyNotNow,
    int RecommendedOrder,
    ImplementationCapabilityCandidateDecision Decision)
{
    public bool ApprovedForImplementation => Decision == ImplementationCapabilityCandidateDecision.ApprovedForImplementation;

    public bool IsStillBlocked =>
        Decision is ImplementationCapabilityCandidateDecision.Blocked
        or ImplementationCapabilityCandidateDecision.FutureCandidateBlockedByAudit;
}

public sealed record ImplementationGateRequirement(
    string GateId,
    string Title,
    bool RequiredBeforeImplementation,
    bool SatisfiedNow,
    bool BlocksImplementation,
    string EvidenceRequired);

public sealed record ImplementationNegativeTestRequirement(
    string TestId,
    string Capability,
    bool RequiredBeforeImplementation,
    bool ImplementedNow,
    string Assertion);

public sealed record ImplementationNoGoCapabilityStatus(
    bool CanOpenRuntimeNow,
    bool CanExecuteNow,
    bool CanMutateNow,
    bool CanExportNow,
    bool CanRunRedactionRuntimeNow,
    bool CanRunSecretPiiScanNow,
    bool CanRunRetentionDeletionNow,
    bool CanRegisterServiceNow,
    bool CanCreateCommandHandlerNow,
    bool CanUseProductIoNow,
    bool CanUseProviderNetworkNow,
    string ReleaseCommercialReadiness)
{
    public bool AllNoGo =>
        !CanOpenRuntimeNow
        && !CanExecuteNow
        && !CanMutateNow
        && !CanExportNow
        && !CanRunRedactionRuntimeNow
        && !CanRunSecretPiiScanNow
        && !CanRunRetentionDeletionNow
        && !CanRegisterServiceNow
        && !CanCreateCommandHandlerNow
        && !CanUseProductIoNow
        && !CanUseProviderNetworkNow
        && ReleaseCommercialReadiness == "NO-GO";
}

public sealed record ImplementationPlanningGateCounts(
    int RuntimeEnabledCount,
    int ExecutionEnabledCount,
    int MutationEnabledCount,
    int ExportEnabledCount,
    int RedactionRuntimeEnabledCount,
    int RetentionDeletionEnabledCount,
    int ServiceRegistrationCount,
    int CommandHandlerCount,
    int ProductActionCount,
    int FilesystemOutputCount,
    int NetworkProviderCallCount)
{
    public bool AllZero =>
        RuntimeEnabledCount == 0
        && ExecutionEnabledCount == 0
        && MutationEnabledCount == 0
        && ExportEnabledCount == 0
        && RedactionRuntimeEnabledCount == 0
        && RetentionDeletionEnabledCount == 0
        && ServiceRegistrationCount == 0
        && CommandHandlerCount == 0
        && ProductActionCount == 0
        && FilesystemOutputCount == 0
        && NetworkProviderCallCount == 0;
}

public sealed record ImplementationPlanningGateDesignOnly(
    string PacketId,
    string GeneratedAtUtc,
    ImplementationPlanningGateStatus Status,
    string Mode,
    string CanonicalState,
    string CurrentDecision,
    IReadOnlyList<ImplementationCapabilityCandidate> CandidateMatrix,
    string RecommendedFutureCandidateId,
    string RecommendedFutureCandidateStatus,
    IReadOnlyList<ImplementationGateRequirement> GateMatrix,
    IReadOnlyList<ImplementationNegativeTestRequirement> NegativeTestRequirements,
    ImplementationNoGoCapabilityStatus NoGoCapabilityStatus,
    ImplementationPlanningGateCounts Counts,
    IReadOnlyList<string> Blockers,
    IReadOnlyList<string> EvidenceLinks,
    IReadOnlyList<string> Warnings,
    string HumanOperatorRecommendation)
{
    public ImplementationCapabilityCandidate RecommendedFutureCandidate =>
        CandidateMatrix.Single(candidate => candidate.CandidateId == RecommendedFutureCandidateId);

    public bool NoCandidateApprovedForImplementation =>
        CandidateMatrix.All(candidate => !candidate.ApprovedForImplementation);

    public bool PassesDesignOnlySafetyProof =>
        Status == ImplementationPlanningGateStatus.DesignOnly
        && Mode.Contains("DESIGN_ONLY", StringComparison.Ordinal)
        && CanonicalState == "PAUSED_READ_ONLY_NO_RUNTIME_NO_EXECUTION_NO_MUTATION_NO_PHYSICAL_EXPORT_NO_REDACTION_RUNTIME"
        && NoCandidateApprovedForImplementation
        && CandidateMatrix.All(candidate => candidate.IsStillBlocked)
        && RecommendedFutureCandidate.Decision == ImplementationCapabilityCandidateDecision.FutureCandidateBlockedByAudit
        && RecommendedFutureCandidateStatus == "FUTURE_CANDIDATE_BLOCKED_BY_AUDIT"
        && GateMatrix.Count >= 16
        && GateMatrix.All(gate => gate.RequiredBeforeImplementation && !gate.SatisfiedNow && gate.BlocksImplementation)
        && NegativeTestRequirements.All(test => test.RequiredBeforeImplementation && !test.ImplementedNow)
        && NoGoCapabilityStatus.AllNoGo
        && Counts.AllZero
        && EvidenceLinks.All(link => link.StartsWith("docs/", StringComparison.Ordinal));
}

public static class ImplementationPlanningGateDesignOnlyPresenter
{
    public static ImplementationPlanningGateDesignOnly CreateFixture() =>
        new(
            PacketId: "nodal-os.implementation-planning-gate.design-only.fixture.v1",
            GeneratedAtUtc: "2026-07-03T00:00:00Z",
            Status: ImplementationPlanningGateStatus.DesignOnly,
            Mode: "DESIGN_ONLY_PLANNING_GATE_NO_RUNTIME_NO_EXECUTION_NO_MUTATION_NO_EXPORT_NO_REDACTION_RUNTIME",
            CanonicalState: "PAUSED_READ_ONLY_NO_RUNTIME_NO_EXECUTION_NO_MUTATION_NO_PHYSICAL_EXPORT_NO_REDACTION_RUNTIME",
            CurrentDecision: "GO_NODAL_OS_READ_ONLY_REENTRY_PRODUCT_SURFACE_AND_DECISION_PACKET_READY",
            CandidateMatrix: Candidates(),
            RecommendedFutureCandidateId: "DURABLE_AUDIT_TRAIL_APPEND_ONLY_IMPLEMENTATION_PLANNING_DESIGN_ONLY",
            RecommendedFutureCandidateStatus: "FUTURE_CANDIDATE_BLOCKED_BY_AUDIT",
            GateMatrix: Gates(),
            NegativeTestRequirements: NegativeTests(),
            NoGoCapabilityStatus: NoGoStatus(),
            Counts: Counts(),
            Blockers: Blockers(),
            EvidenceLinks: EvidenceLinks(),
            Warnings: Warnings(),
            HumanOperatorRecommendation: "Use this packet only to plan an externally audited future capability. Do not implement runtime/live, execution, mutation, physical export, redaction runtime, retention/deletion runtime, service registration, command handlers or release/commercial readiness.");

    private static IReadOnlyList<ImplementationCapabilityCandidate> Candidates() =>
    [
        Candidate(
            id: "DURABLE_AUDIT_TRAIL_APPEND_ONLY_IMPLEMENTATION_PLANNING_DESIGN_ONLY",
            name: "Durable audit trail append-only minimal path",
            value: "Highest traceability value and a prerequisite for later mutation/export accountability.",
            technicalRisk: ImplementationRiskLevel.Medium,
            safetyRisk: ImplementationRiskLevel.Medium,
            dataRisk: ImplementationRiskLevel.Medium,
            scope: "isolated append-only audit component with no service registration, no command handler and no product IO until separately approved",
            whyNotNow: "Requires external audit, storage boundary, redaction status model, negative tests and fail-closed rollback plan before any append implementation.",
            order: 1,
            decision: ImplementationCapabilityCandidateDecision.FutureCandidateBlockedByAudit),
        Candidate(
            id: "APPROVAL_EXECUTION_MINIMAL_IMPLEMENTATION_PLANNING_DESIGN_ONLY",
            name: "Approval execution minimal path",
            value: "Would connect approval decisions to real action only after policy and audit gates exist.",
            technicalRisk: ImplementationRiskLevel.High,
            safetyRisk: ImplementationRiskLevel.Critical,
            dataRisk: ImplementationRiskLevel.High,
            scope: "single approval execution boundary with no writer invocation or product action until approved",
            whyNotNow: "Execution remains too close to product actions and requires audit trail, rollback proof and external audit.",
            order: 2,
            decision: ImplementationCapabilityCandidateDecision.Blocked),
        Candidate(
            id: "MUTATION_STORE_MINIMAL_IMPLEMENTATION_PLANNING_DESIGN_ONLY",
            name: "Mutation store minimal path",
            value: "Would prepare state transition accountability after execution gates mature.",
            technicalRisk: ImplementationRiskLevel.High,
            safetyRisk: ImplementationRiskLevel.High,
            dataRisk: ImplementationRiskLevel.High,
            scope: "isolated mutation store with no state mutation path exposed",
            whyNotNow: "Mutation storage cannot precede external audit and durable audit trail implementation gates.",
            order: 3,
            decision: ImplementationCapabilityCandidateDecision.Blocked),
        Candidate(
            id: "REDACTION_RUNTIME_MINIMAL_IMPLEMENTATION_PLANNING_DESIGN_ONLY",
            name: "Redaction runtime minimal path",
            value: "Would reduce sensitive payload exposure before export, storage or audit persistence.",
            technicalRisk: ImplementationRiskLevel.High,
            safetyRisk: ImplementationRiskLevel.High,
            dataRisk: ImplementationRiskLevel.Critical,
            scope: "isolated deterministic redaction boundary with fixture-only inputs before real data approval",
            whyNotNow: "Requires secret/PII policy, scanner tests, external audit and data handling gate before runtime.",
            order: 4,
            decision: ImplementationCapabilityCandidateDecision.Blocked),
        Candidate(
            id: "PHYSICAL_EXPORT_CONTROLLED_IMPLEMENTATION_PLANNING_DESIGN_ONLY",
            name: "Physical export controlled minimal path",
            value: "Would make decision packet output portable only after redaction and audit gates exist.",
            technicalRisk: ImplementationRiskLevel.High,
            safetyRisk: ImplementationRiskLevel.Critical,
            dataRisk: ImplementationRiskLevel.Critical,
            scope: "single format renderer sandbox with no file output, clipboard or download until approved",
            whyNotNow: "Physical export requires redaction runtime, retention/deletion policy, destination policy and external audit.",
            order: 5,
            decision: ImplementationCapabilityCandidateDecision.Blocked),
        Candidate(
            id: "RETENTION_DELETION_RUNTIME_MINIMAL_IMPLEMENTATION_PLANNING_DESIGN_ONLY",
            name: "Retention/deletion runtime minimal path",
            value: "Would enforce data lifecycle only after storage and legal hold gates exist.",
            technicalRisk: ImplementationRiskLevel.High,
            safetyRisk: ImplementationRiskLevel.High,
            dataRisk: ImplementationRiskLevel.Critical,
            scope: "policy-only lifecycle boundary with no delete/tombstone/write path until approved",
            whyNotNow: "Requires legal hold, tombstone, retention store and external audit before implementation.",
            order: 6,
            decision: ImplementationCapabilityCandidateDecision.Blocked),
        Candidate(
            id: "RECIPES_EXECUTION_SAFE_RUNTIME_PLANNING_DESIGN_ONLY",
            name: "Recipes execution safe runtime path",
            value: "Would eventually coordinate approved workflows after execution and audit controls mature.",
            technicalRisk: ImplementationRiskLevel.Critical,
            safetyRisk: ImplementationRiskLevel.Critical,
            dataRisk: ImplementationRiskLevel.High,
            scope: "no recipe execution; planning only for a future sandboxed runner",
            whyNotNow: "Recipe execution is downstream of runtime, provider, audit, rollback and command handler gates.",
            order: 7,
            decision: ImplementationCapabilityCandidateDecision.Blocked),
        Candidate(
            id: "WCU_OCR_SAFE_RUNTIME_PLANNING_DESIGN_ONLY",
            name: "WCU/OCR safe runtime path",
            value: "Would ingest visual evidence only after runtime and data policy gates mature.",
            technicalRisk: ImplementationRiskLevel.Critical,
            safetyRisk: ImplementationRiskLevel.High,
            dataRisk: ImplementationRiskLevel.Critical,
            scope: "no live capture or OCR; planning only for a future isolated evidence boundary",
            whyNotNow: "WCU/OCR needs real data controls, redaction, audit and external review before any live path.",
            order: 8,
            decision: ImplementationCapabilityCandidateDecision.Blocked),
        Candidate(
            id: "BROWSER_CDP_SAFE_RUNTIME_PLANNING_DESIGN_ONLY",
            name: "Browser/CDP safe runtime path",
            value: "Would support browser-bound workflows only after all runtime and user-data gates exist.",
            technicalRisk: ImplementationRiskLevel.Critical,
            safetyRisk: ImplementationRiskLevel.Critical,
            dataRisk: ImplementationRiskLevel.Critical,
            scope: "no browser/CDP live access; planning only for a future isolated runtime boundary",
            whyNotNow: "Browser/CDP live is too broad for first opening and requires separate external audit.",
            order: 9,
            decision: ImplementationCapabilityCandidateDecision.Blocked)
    ];

    private static ImplementationCapabilityCandidate Candidate(
        string id,
        string name,
        string value,
        ImplementationRiskLevel technicalRisk,
        ImplementationRiskLevel safetyRisk,
        ImplementationRiskLevel dataRisk,
        string scope,
        string whyNotNow,
        int order,
        ImplementationCapabilityCandidateDecision decision) =>
        new(
            CandidateId: id,
            CandidateName: name,
            BusinessProductValue: value,
            TechnicalRisk: technicalRisk,
            SafetyRisk: safetyRisk,
            DataRisk: dataRisk,
            RequiredPreconditions:
            [
                "explicit user GO for this exact real capability",
                "repo guard clean and origin synchronized",
                "scope isolation approved before code",
                "external audit completed before implementation",
                "negative tests defined before implementation"
            ],
            RequiredNegativeTests:
            [
                "fails closed without explicit gate",
                "does not register services or command handlers",
                "does not perform product IO without approval",
                "does not expose secrets, PII or raw payloads",
                "keeps release/commercial NO-GO"
            ],
            RequiredExternalAudits:
            [
                "pre-implementation external audit",
                "post-implementation external audit before enablement"
            ],
            RequiredRollbackNoSideEffectProof:
            [
                "rollback path documented before code",
                "no-side-effect regression proof required",
                "audit/evidence failure mode must fail closed"
            ],
            RequiredIsolatedScope: scope,
            WhyNotNow: whyNotNow,
            RecommendedOrder: order,
            Decision: decision);

    private static IReadOnlyList<ImplementationGateRequirement> Gates() =>
    [
        Gate("explicit-user-go", "User explicit GO for the exact real capability.", "signed human decision and scope statement"),
        Gate("repo-guard-clean", "Repo guard clean before implementation.", "repo, branch, HEAD, worktree and origin sync proof"),
        Gate("scope-isolation", "Scope isolation before code.", "approved file/module boundary and forbidden-scope list"),
        Gate("external-audit-before-implementation", "External audit before implementation.", "read-only audit report with GO"),
        Gate("negative-tests-before-code", "Negative tests defined before implementation.", "failing-first or reviewed negative test plan"),
        Gate("no-secrets-pii-exposure", "No secrets or PII exposure.", "redaction/privacy threat model and fixtures"),
        Gate("no-broad-filesystem-access", "No broad filesystem access.", "explicit IO allow/deny model"),
        Gate("no-service-registration-until-approved", "No service registration until separately approved.", "registration deny assertion"),
        Gate("no-command-handler-until-approved", "No command handler until separately approved.", "handler deny assertion"),
        Gate("no-product-io-until-approved", "No product IO until separately approved.", "product IO deny assertion"),
        Gate("rollback-no-side-effect-plan", "Rollback and no-side-effect plan.", "rollback plan and zero-side-effect proof"),
        Gate("evidence-audit-trail-plan", "Evidence and audit trail plan.", "evidence refs, audit refs and fail-closed policy"),
        Gate("failure-mode-fail-closed", "Failure mode must fail closed.", "failure matrix and tests"),
        Gate("overclaim-scan", "Overclaim scan before closing implementation.", "scan log without TRUE_RISK"),
        Gate("final-external-audit-before-enablement", "Final external audit before enablement.", "post-implementation audit GO"),
        Gate("release-commercial-no-go", "Release/commercial remains NO-GO.", "explicit NO-GO decision")
    ];

    private static ImplementationGateRequirement Gate(string id, string title, string evidenceRequired) =>
        new(
            GateId: id,
            Title: title,
            RequiredBeforeImplementation: true,
            SatisfiedNow: false,
            BlocksImplementation: true,
            EvidenceRequired: evidenceRequired);

    private static IReadOnlyList<ImplementationNegativeTestRequirement> NegativeTests() =>
    [
        Negative("runtime-live", "runtime/live", "runtime remains disabled without explicit implementation GO"),
        Negative("execution", "approval execution", "execution count remains 0 and no product action is exposed"),
        Negative("mutation", "approval mutation", "state mutation count remains 0 and no mutation store is used"),
        Negative("physical-export", "physical export", "export count and filesystem output count remain 0"),
        Negative("redaction-runtime", "redaction runtime", "redaction runtime count remains 0 and no scan is performed"),
        Negative("retention-deletion", "retention/deletion runtime", "retention/deletion counts remain 0 and no tombstone is written"),
        Negative("service-registration", "service registration", "service registration count remains 0"),
        Negative("command-handler", "command handler", "command handler count remains 0"),
        Negative("provider-network", "provider/network", "provider network call count remains 0"),
        Negative("release-commercial", "release/commercial", "release/commercial readiness remains NO-GO")
    ];

    private static ImplementationNegativeTestRequirement Negative(string id, string capability, string assertion) =>
        new(
            TestId: id,
            Capability: capability,
            RequiredBeforeImplementation: true,
            ImplementedNow: false,
            Assertion: assertion);

    private static ImplementationNoGoCapabilityStatus NoGoStatus() =>
        new(
            CanOpenRuntimeNow: false,
            CanExecuteNow: false,
            CanMutateNow: false,
            CanExportNow: false,
            CanRunRedactionRuntimeNow: false,
            CanRunSecretPiiScanNow: false,
            CanRunRetentionDeletionNow: false,
            CanRegisterServiceNow: false,
            CanCreateCommandHandlerNow: false,
            CanUseProductIoNow: false,
            CanUseProviderNetworkNow: false,
            ReleaseCommercialReadiness: "NO-GO");

    private static ImplementationPlanningGateCounts Counts() =>
        new(
            RuntimeEnabledCount: 0,
            ExecutionEnabledCount: 0,
            MutationEnabledCount: 0,
            ExportEnabledCount: 0,
            RedactionRuntimeEnabledCount: 0,
            RetentionDeletionEnabledCount: 0,
            ServiceRegistrationCount: 0,
            CommandHandlerCount: 0,
            ProductActionCount: 0,
            FilesystemOutputCount: 0,
            NetworkProviderCallCount: 0);

    private static IReadOnlyList<string> Blockers() =>
    [
        "No candidate receives implementation approval in this block.",
        "The recommended future candidate remains blocked by pre-implementation external audit.",
        "Runtime/live, execution, mutation, physical export, redaction runtime and retention/deletion runtime remain 0%.",
        "Service registration, command handlers, product IO, provider/network calls and release/commercial readiness remain NO-GO."
    ];

    private static IReadOnlyList<string> EvidenceLinks() =>
    [
        "docs/qa/nodal-os-read-only-reentry-decision-packet/report.md",
        "docs/qa/nodal-os-canonical-status-docs-hardening/report.md",
        "docs/handoff/nodal-os-final-privacy-export-controlled-execution-closeout-handoff.md",
        "docs/qa/nodal-os-final-privacy-export-controlled-execution-closeout/report.md",
        "docs/decision-log.md"
    ];

    private static IReadOnlyList<string> Warnings() =>
    [
        "This planning gate is design-only and does not approve implementation.",
        "The recommended future candidate is blocked by external audit and explicit user GO.",
        "Release/commercial readiness remains NO-GO.",
        "No runtime/live, execution, mutation, export, redaction, retention/deletion, service registration or command handler path is opened."
    ];
}
