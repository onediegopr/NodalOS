namespace OneBrain.Core.Approval;

public enum FirstCapabilityCandidateDecision
{
    SelectedScopeProposalReadOnly,
    FutureCandidateBlocked,
    NotFirstCandidate,
    NoGoHighRisk,
    DesignOnlyOnly
}

public enum FirstCapabilityRiskLevel
{
    Low,
    Medium,
    High,
    Critical
}

public sealed record FirstCapabilityCandidateAssessment(
    string CandidateId,
    string Name,
    string ProductValue,
    FirstCapabilityRiskLevel SafetyRisk,
    FirstCapabilityRiskLevel DataRisk,
    FirstCapabilityRiskLevel RuntimeRisk,
    FirstCapabilityRiskLevel IoRisk,
    string Testability,
    string IsolationFeasibility,
    string RollbackNoSideEffectFeasibility,
    string ExternalAuditComplexity,
    IReadOnlyList<string> Dependencies,
    IReadOnlyList<string> RequiredGates,
    IReadOnlyList<string> RequiredNegativeTests,
    string WhyNotNow,
    bool SuitableAsFirstCandidate,
    FirstCapabilityCandidateDecision Decision)
{
    public bool IsSelectedScopeProposal => Decision == FirstCapabilityCandidateDecision.SelectedScopeProposalReadOnly;

    public bool KeepsImplementationBlocked => Decision != FirstCapabilityCandidateDecision.SelectedScopeProposalReadOnly || !SuitableAsFirstCandidate
        ? Decision is FirstCapabilityCandidateDecision.FutureCandidateBlocked
            or FirstCapabilityCandidateDecision.NotFirstCandidate
            or FirstCapabilityCandidateDecision.NoGoHighRisk
            or FirstCapabilityCandidateDecision.DesignOnlyOnly
        : true;
}

public sealed record FirstCapabilityScopeBoundary(
    string FutureHitoName,
    string Objective,
    string ProblemSolved,
    string WhyFirstCandidate,
    string WhySaferThanAlternatives,
    IReadOnlyList<string> InScopeFutureOnly,
    IReadOnlyList<string> OutOfScope,
    IReadOnlyList<string> CandidateFutureFiles,
    IReadOnlyList<string> PositiveTestsAllowedFuture,
    IReadOnlyList<string> EvidenceNoSideEffectProofRequired,
    IReadOnlyList<string> RollbackFailClosedPlan,
    string PreImplementationExternalAudit,
    string PostImplementationExternalAudit,
    string FutureCloseCriteria,
    string FutureNoGoCriteria,
    string WordingPolicy);

public sealed record FirstCapabilityRequiredGate(
    string GateId,
    string Title,
    bool RequiredBeforeImplementation,
    bool SatisfiedNow,
    bool BlocksImplementation);

public sealed record FirstCapabilityNegativeTestRequirement(
    string TestId,
    string Assertion,
    bool RequiredBeforeImplementation,
    bool ImplementedNow);

public sealed record FirstCapabilityExternalAuditRequirement(
    string AuditId,
    string AuditName,
    bool RequiredBeforeImplementation,
    bool RequiredAfterImplementationBeforeEnablement,
    bool SatisfiedNow,
    string PromptName);

public sealed record FirstCapabilityNoGoStatus(
    bool ImplementationPromptExecutableNow,
    bool SafeToImplementNow,
    bool RuntimeReady,
    bool ReleaseCommercialReady,
    string MaxDecisionAllowed)
{
    public bool BlocksImplementation =>
        !ImplementationPromptExecutableNow
        && !SafeToImplementNow
        && !RuntimeReady
        && !ReleaseCommercialReady
        && MaxDecisionAllowed == "SAFE_TO_PREPARE_EXTERNAL_AUDIT_FOR_SELECTED_CANDIDATE";
}

public sealed record FirstCapabilityScopeProposalCounts(
    int RuntimeEnabledCount,
    int ExecutionEnabledCount,
    int MutationEnabledCount,
    int ExportEnabledCount,
    int BrowserCdpLiveEnabledCount,
    int WcuOcrLiveEnabledCount,
    int RecipesExecutionEnabledCount,
    int ServiceRegistrationCount,
    int CommandHandlerCount,
    int ProductActionCount,
    int FilesystemOutputCount,
    int NetworkProviderCallCount,
    int ReleaseCommercialReadyCount)
{
    public bool AllZero =>
        RuntimeEnabledCount == 0
        && ExecutionEnabledCount == 0
        && MutationEnabledCount == 0
        && ExportEnabledCount == 0
        && BrowserCdpLiveEnabledCount == 0
        && WcuOcrLiveEnabledCount == 0
        && RecipesExecutionEnabledCount == 0
        && ServiceRegistrationCount == 0
        && CommandHandlerCount == 0
        && ProductActionCount == 0
        && FilesystemOutputCount == 0
        && NetworkProviderCallCount == 0
        && ReleaseCommercialReadyCount == 0;
}

public sealed record FirstRealCapabilityCandidateScopeProposalReadOnlyPacket(
    string PacketId,
    string GeneratedAtUtc,
    string CanonicalState,
    string Decision,
    IReadOnlyList<FirstCapabilityCandidateAssessment> CandidateMatrix,
    string SelectedCandidateId,
    FirstCapabilityScopeBoundary SelectedCandidateScope,
    IReadOnlyList<FirstCapabilityRequiredGate> RequiredGates,
    IReadOnlyList<FirstCapabilityNegativeTestRequirement> RequiredNegativeTests,
    IReadOnlyList<FirstCapabilityExternalAuditRequirement> ExternalAuditRequirements,
    FirstCapabilityNoGoStatus NoGoStatus,
    FirstCapabilityScopeProposalCounts Counts,
    IReadOnlyList<string> BlockedCapabilities,
    IReadOnlyList<string> EvidenceLinks,
    string FutureImplementationPromptStatus,
    string ExternalAuditPromptName,
    string BlockedFutureImplementationPromptName,
    string HumanOperatorRecommendation)
{
    public FirstCapabilityCandidateAssessment SelectedCandidate =>
        CandidateMatrix.Single(candidate => candidate.CandidateId == SelectedCandidateId);

    public bool ExactlyOneSelected =>
        CandidateMatrix.Count(candidate => candidate.Decision == FirstCapabilityCandidateDecision.SelectedScopeProposalReadOnly) == 1;

    public bool PassesReadOnlySafetyProof =>
        CanonicalState == "PAUSED_READ_ONLY_NO_RUNTIME_NO_EXECUTION_NO_MUTATION_NO_PHYSICAL_EXPORT_NO_REDACTION_RUNTIME"
        && Decision == "SAFE_TO_PREPARE_EXTERNAL_AUDIT_FOR_SELECTED_CANDIDATE"
        && ExactlyOneSelected
        && SelectedCandidate.IsSelectedScopeProposal
        && SelectedCandidate.SuitableAsFirstCandidate
        && CandidateMatrix.All(candidate => candidate.KeepsImplementationBlocked)
        && RequiredGates.Count >= 17
        && RequiredGates.All(gate => gate.RequiredBeforeImplementation && !gate.SatisfiedNow && gate.BlocksImplementation)
        && RequiredNegativeTests.Count >= 20
        && RequiredNegativeTests.All(test => test.RequiredBeforeImplementation && !test.ImplementedNow)
        && ExternalAuditRequirements.Count >= 2
        && ExternalAuditRequirements.All(audit => audit.RequiredBeforeImplementation && audit.RequiredAfterImplementationBeforeEnablement && !audit.SatisfiedNow)
        && NoGoStatus.BlocksImplementation
        && Counts.AllZero
        && FutureImplementationPromptStatus == "BLOCKED_NOT_EXECUTABLE"
        && EvidenceLinks.All(link => link.StartsWith("docs/", StringComparison.Ordinal));
}

public static class FirstRealCapabilityCandidateScopeProposalReadOnlyPresenter
{
    public static FirstRealCapabilityCandidateScopeProposalReadOnlyPacket CreateFixture() =>
        new(
            PacketId: "nodal-os.first-real-capability.scope-proposal.read-only.fixture.v1",
            GeneratedAtUtc: "2026-07-03T00:00:00Z",
            CanonicalState: "PAUSED_READ_ONLY_NO_RUNTIME_NO_EXECUTION_NO_MUTATION_NO_PHYSICAL_EXPORT_NO_REDACTION_RUNTIME",
            Decision: "SAFE_TO_PREPARE_EXTERNAL_AUDIT_FOR_SELECTED_CANDIDATE",
            CandidateMatrix: Candidates(),
            SelectedCandidateId: "DURABLE_AUDIT_TRAIL_APPEND_ONLY_MINIMAL_SCOPE_PROPOSAL_READ_ONLY",
            SelectedCandidateScope: SelectedScope(),
            RequiredGates: Gates(),
            RequiredNegativeTests: NegativeTests(),
            ExternalAuditRequirements: ExternalAudits(),
            NoGoStatus: new FirstCapabilityNoGoStatus(
                ImplementationPromptExecutableNow: false,
                SafeToImplementNow: false,
                RuntimeReady: false,
                ReleaseCommercialReady: false,
                MaxDecisionAllowed: "SAFE_TO_PREPARE_EXTERNAL_AUDIT_FOR_SELECTED_CANDIDATE"),
            Counts: Counts(),
            BlockedCapabilities: BlockedCapabilities(),
            EvidenceLinks: EvidenceLinks(),
            FutureImplementationPromptStatus: "BLOCKED_NOT_EXECUTABLE",
            ExternalAuditPromptName: "NODAL_OS_SELECTED_CAPABILITY_SCOPE_EXTERNAL_AUDIT_READ_ONLY",
            BlockedFutureImplementationPromptName: "BLOCKED_DO_NOT_EXECUTE_NODAL_OS_SELECTED_FIRST_CAPABILITY_IMPLEMENTATION_CANDIDATE",
            HumanOperatorRecommendation: "Prepare a read-only external audit for the selected durable audit trail scope. Do not implement runtime, execution, mutation, storage, export, service registration or command handlers.");

    private static IReadOnlyList<FirstCapabilityCandidateAssessment> Candidates() =>
    [
        Candidate(
            id: "DURABLE_AUDIT_TRAIL_APPEND_ONLY_MINIMAL_SCOPE_PROPOSAL_READ_ONLY",
            name: "Durable audit trail append-only minimal scope proposal",
            value: "Creates accountability foundation for later approvals, mutation and export without selecting browser, provider or recipe surfaces.",
            safetyRisk: FirstCapabilityRiskLevel.Medium,
            dataRisk: FirstCapabilityRiskLevel.Medium,
            runtimeRisk: FirstCapabilityRiskLevel.Low,
            ioRisk: FirstCapabilityRiskLevel.Medium,
            testability: "High; append eligibility and blocked-write behavior can be tested deterministically before any storage implementation.",
            isolation: "High; can be scoped to a narrow audit boundary with no service registration or command handler.",
            rollback: "High; future implementation can fail closed before append and keep no-side-effect proof for denied paths.",
            audit: "Medium; requires storage boundary, redaction status and event schema review.",
            whyNotNow: "This block only proposes scope; durable audit trail implementation remains 0% until external audit and explicit user GO.",
            suitable: true,
            decision: FirstCapabilityCandidateDecision.SelectedScopeProposalReadOnly),
        Candidate(
            id: "APPROVAL_EXECUTION_MINIMAL_BRIDGE_SCOPE_PROPOSAL_READ_ONLY",
            name: "Approval execution minimal bridge",
            value: "Would eventually connect approvals to actions.",
            safetyRisk: FirstCapabilityRiskLevel.Critical,
            dataRisk: FirstCapabilityRiskLevel.High,
            runtimeRisk: FirstCapabilityRiskLevel.Critical,
            ioRisk: FirstCapabilityRiskLevel.High,
            testability: "Medium; requires many negative execution-path tests.",
            isolation: "Medium; close to product action boundaries.",
            rollback: "Medium; rollback depends on action semantics.",
            audit: "High; requires execution, mutation, writer and policy review.",
            whyNotNow: "Too close to execution and product actions for the first future capability.",
            suitable: false,
            decision: FirstCapabilityCandidateDecision.NotFirstCandidate),
        Candidate(
            id: "PHYSICAL_EXPORT_CONTROLLED_SCOPE_PROPOSAL_READ_ONLY",
            name: "Physical export controlled minimal path",
            value: "Would eventually provide portable handoff output.",
            safetyRisk: FirstCapabilityRiskLevel.Critical,
            dataRisk: FirstCapabilityRiskLevel.Critical,
            runtimeRisk: FirstCapabilityRiskLevel.High,
            ioRisk: FirstCapabilityRiskLevel.Critical,
            testability: "Medium; requires renderer, destination and privacy tests.",
            isolation: "Medium; file output and clipboard/download boundaries are sensitive.",
            rollback: "Medium; physical outputs are hard to unwind.",
            audit: "High; requires redaction, retention/deletion and destination policy review.",
            whyNotNow: "Physical export depends on redaction and destination gates.",
            suitable: false,
            decision: FirstCapabilityCandidateDecision.NotFirstCandidate),
        Candidate(
            id: "REDACTION_RUNTIME_MINIMAL_SCOPE_PROPOSAL_READ_ONLY",
            name: "Redaction runtime minimal safe path",
            value: "Would eventually reduce sensitive payload exposure.",
            safetyRisk: FirstCapabilityRiskLevel.High,
            dataRisk: FirstCapabilityRiskLevel.Critical,
            runtimeRisk: FirstCapabilityRiskLevel.High,
            ioRisk: FirstCapabilityRiskLevel.Medium,
            testability: "Medium; requires secret/PII policy and adversarial datasets.",
            isolation: "Medium; data handling scope is sensitive.",
            rollback: "Medium; missed redaction failures are high impact.",
            audit: "High; requires privacy-specific external audit.",
            whyNotNow: "Redaction runtime needs scanner policy, data rules and external review.",
            suitable: false,
            decision: FirstCapabilityCandidateDecision.NotFirstCandidate),
        Candidate(
            id: "RETENTION_DELETION_RUNTIME_SCOPE_PROPOSAL_READ_ONLY",
            name: "Retention/deletion runtime",
            value: "Would eventually enforce lifecycle policy.",
            safetyRisk: FirstCapabilityRiskLevel.High,
            dataRisk: FirstCapabilityRiskLevel.Critical,
            runtimeRisk: FirstCapabilityRiskLevel.High,
            ioRisk: FirstCapabilityRiskLevel.High,
            testability: "Low until storage, legal hold and tombstone rules are concrete.",
            isolation: "Medium; lifecycle operations are irreversible if real.",
            rollback: "Low; deletion must be designed as fail-closed with legal hold.",
            audit: "High; requires legal hold and lifecycle audit.",
            whyNotNow: "Deletion and retention should not be first because they alter lifecycle state.",
            suitable: false,
            decision: FirstCapabilityCandidateDecision.NoGoHighRisk),
        Candidate(
            id: "BROWSER_CDP_SAFE_RUNTIME_SCOPE_PROPOSAL_READ_ONLY",
            name: "Browser/CDP safe runtime",
            value: "Would eventually support browser-bound workflows.",
            safetyRisk: FirstCapabilityRiskLevel.Critical,
            dataRisk: FirstCapabilityRiskLevel.Critical,
            runtimeRisk: FirstCapabilityRiskLevel.Critical,
            ioRisk: FirstCapabilityRiskLevel.High,
            testability: "Low for first opening because live browser state creates broad side effects.",
            isolation: "Low until a dedicated browser sandbox is designed.",
            rollback: "Low; navigation/session effects are hard to guarantee.",
            audit: "Critical; requires dedicated Browser/CDP audit.",
            whyNotNow: "Browser/CDP remains blocked by specific hardening requirements.",
            suitable: false,
            decision: FirstCapabilityCandidateDecision.NoGoHighRisk),
        Candidate(
            id: "WCU_OCR_SAFE_RUNTIME_SCOPE_PROPOSAL_READ_ONLY",
            name: "WCU/OCR safe runtime",
            value: "Would eventually support desktop evidence capture.",
            safetyRisk: FirstCapabilityRiskLevel.Critical,
            dataRisk: FirstCapabilityRiskLevel.Critical,
            runtimeRisk: FirstCapabilityRiskLevel.Critical,
            ioRisk: FirstCapabilityRiskLevel.High,
            testability: "Low for first opening because desktop state, OCR and real data are broad.",
            isolation: "Low until computer-use and OCR boundaries are audited.",
            rollback: "Low; screenshots/OCR text can expose real data.",
            audit: "Critical; requires WCU/OCR-specific audit.",
            whyNotNow: "WCU/OCR remains blocked by specific hardening requirements.",
            suitable: false,
            decision: FirstCapabilityCandidateDecision.NoGoHighRisk),
        Candidate(
            id: "RECIPES_EXECUTION_SAFE_RUNTIME_SCOPE_PROPOSAL_READ_ONLY",
            name: "Recipes execution safe runtime",
            value: "Would eventually coordinate approved workflows.",
            safetyRisk: FirstCapabilityRiskLevel.Critical,
            dataRisk: FirstCapabilityRiskLevel.High,
            runtimeRisk: FirstCapabilityRiskLevel.Critical,
            ioRisk: FirstCapabilityRiskLevel.High,
            testability: "Low until scheduler, trigger, retry and action runner boundaries are audited.",
            isolation: "Low because recipes compose multiple side-effect surfaces.",
            rollback: "Low; composed actions complicate rollback.",
            audit: "Critical; requires dedicated recipes execution audit.",
            whyNotNow: "Recipes execution is downstream of runtime and action boundaries.",
            suitable: false,
            decision: FirstCapabilityCandidateDecision.NoGoHighRisk),
        Candidate(
            id: "MUTATION_STORE_MINIMAL_SCOPE_PROPOSAL_READ_ONLY",
            name: "Mutation store minimal path",
            value: "Would eventually capture state transition intent.",
            safetyRisk: FirstCapabilityRiskLevel.High,
            dataRisk: FirstCapabilityRiskLevel.High,
            runtimeRisk: FirstCapabilityRiskLevel.Medium,
            ioRisk: FirstCapabilityRiskLevel.High,
            testability: "Medium; requires state transition and conflict tests.",
            isolation: "Medium; close to approval mutation.",
            rollback: "Medium; mutation records can mislead if not paired with audit trail.",
            audit: "High; should follow durable audit trail scope.",
            whyNotNow: "Mutation store should not precede audit trail accountability.",
            suitable: false,
            decision: FirstCapabilityCandidateDecision.FutureCandidateBlocked)
    ];

    private static FirstCapabilityCandidateAssessment Candidate(
        string id,
        string name,
        string value,
        FirstCapabilityRiskLevel safetyRisk,
        FirstCapabilityRiskLevel dataRisk,
        FirstCapabilityRiskLevel runtimeRisk,
        FirstCapabilityRiskLevel ioRisk,
        string testability,
        string isolation,
        string rollback,
        string audit,
        string whyNotNow,
        bool suitable,
        FirstCapabilityCandidateDecision decision) =>
        new(
            CandidateId: id,
            Name: name,
            ProductValue: value,
            SafetyRisk: safetyRisk,
            DataRisk: dataRisk,
            RuntimeRisk: runtimeRisk,
            IoRisk: ioRisk,
            Testability: testability,
            IsolationFeasibility: isolation,
            RollbackNoSideEffectFeasibility: rollback,
            ExternalAuditComplexity: audit,
            Dependencies:
            [
                "explicit user GO",
                "external audit",
                "negative tests before implementation",
                "scope isolation",
                "release/commercial NO-GO"
            ],
            RequiredGates:
            [
                "repo guard clean",
                "external audit before implementation",
                "final audit before enablement",
                "overclaim scan"
            ],
            RequiredNegativeTests:
            [
                "no implementation approval in scope proposal",
                "no runtime/live",
                "no service registration",
                "no command handler",
                "no product action"
            ],
            WhyNotNow: whyNotNow,
            SuitableAsFirstCandidate: suitable,
            Decision: decision);

    private static FirstCapabilityScopeBoundary SelectedScope() =>
        new(
            FutureHitoName: "NODAL_OS_DURABLE_AUDIT_TRAIL_APPEND_ONLY_IMPLEMENTATION_CANDIDATE_BLOCKED_PENDING_EXTERNAL_AUDIT_AND_USER_GO",
            Objective: "Define a future isolated append-only audit trail candidate that can record approved audit events after external audit and explicit user GO.",
            ProblemSolved: "Later real capabilities need accountable evidence before mutation, execution or export can be trusted.",
            WhyFirstCandidate: "It is narrower than execution, export, Browser/CDP, WCU/OCR, recipes, redaction and retention/deletion, and can be tested as fail-closed before storage is opened.",
            WhySaferThanAlternatives: "It avoids live browser/desktop/provider surfaces and does not require immediate product actions.",
            InScopeFutureOnly:
            [
                "append-only audit event contract",
                "approved event schema",
                "fail-closed append eligibility",
                "redaction status reference requirement",
                "deterministic denied-append tests"
            ],
            OutOfScope:
            [
                "runtime/live",
                "approval execution",
                "approval mutation",
                "physical export",
                "redaction runtime",
                "retention/deletion runtime",
                "broad filesystem IO",
                "DB/migration",
                "service registration",
                "command handler",
                "release/commercial readiness"
            ],
            CandidateFutureFiles:
            [
                "src/OneBrain.Core/Approval/<future-durable-audit-trail-implementation>.cs",
                "tests/OneBrain.Safety.Tests/<future-durable-audit-trail-negative-tests>.cs",
                "tests/OneBrain.Recipes.Tests/<future-durable-audit-trail-fixture-tests>.cs",
                "docs/qa/<future-durable-audit-trail-implementation-report>/report.md"
            ],
            PositiveTestsAllowedFuture:
            [
                "accepts deterministic fixture event only after explicit approved gate",
                "returns blocked result for missing audit eligibility",
                "returns blocked result for missing redaction status"
            ],
            EvidenceNoSideEffectProofRequired:
            [
                "denied append does not persist",
                "missing gate fails closed",
                "unexpected target fails closed",
                "all non-selected capabilities remain 0"
            ],
            RollbackFailClosedPlan:
            [
                "fail before append when eligibility is missing",
                "treat storage boundary errors as denied",
                "disable enablement until post-implementation audit",
                "keep release/commercial NO-GO"
            ],
            PreImplementationExternalAudit: "NODAL_OS_SELECTED_CAPABILITY_SCOPE_EXTERNAL_AUDIT_READ_ONLY",
            PostImplementationExternalAudit: "required before any enablement",
            FutureCloseCriteria: "external audit GO, explicit user GO, negative tests first, isolated scope, no unresolved P0/P1/P2",
            FutureNoGoCriteria: "any service registration, command handler, runtime/live, broad IO, DB/migration, export, mutation, execution, redaction runtime or release/commercial claim outside approved scope",
            WordingPolicy: "Use scope proposal, blocked candidate, external audit required and implementation readiness 0%; implementation remains blocked.");

    private static IReadOnlyList<FirstCapabilityRequiredGate> Gates() =>
    [
        Gate("explicit-user-go", "User explicit GO for selected candidate implementation."),
        Gate("repo-guard-clean", "Repo guard clean."),
        Gate("branch-confirmation", "Dedicated branch or explicit branch confirmation."),
        Gate("external-audit-before-implementation", "External audit before implementation."),
        Gate("negative-tests-before-implementation", "Negative tests written or updated before implementation."),
        Gate("scope-isolation", "Scope isolation."),
        Gate("overclaim-scan-before-after", "Overclaim scan before and after."),
        Gate("no-service-registration-unless-scoped", "No service registration unless explicitly scoped."),
        Gate("no-command-handler-unless-scoped", "No command handler unless explicitly scoped."),
        Gate("no-product-ui-action-unless-scoped", "No product UI action unless explicitly scoped."),
        Gate("no-broad-io", "No broad IO."),
        Gate("no-secrets-pii-real-data", "No secrets, PII or real data."),
        Gate("fail-closed", "Fail-closed behavior."),
        Gate("rollback-no-side-effect-proof", "Rollback and no-side-effect proof."),
        Gate("qa-report", "QA report MD/JSON."),
        Gate("final-audit-before-enablement", "Final audit before enablement."),
        Gate("release-commercial-no-go", "Release/commercial remains NO-GO.")
    ];

    private static FirstCapabilityRequiredGate Gate(string id, string title) =>
        new(id, title, RequiredBeforeImplementation: true, SatisfiedNow: false, BlocksImplementation: true);

    private static IReadOnlyList<FirstCapabilityNegativeTestRequirement> NegativeTests() =>
    [
        Negative("candidate-disabled-without-go", "selected candidate remains disabled until explicit user GO"),
        Negative("no-unrelated-runtime", "no unrelated runtime is enabled"),
        Negative("no-command-handlers", "command handler count remains 0"),
        Negative("no-service-registration", "service registration count remains 0"),
        Negative("no-product-actions", "product action count remains 0"),
        Negative("no-filesystem-output-outside-scope", "filesystem output count remains 0 in this scope proposal"),
        Negative("no-network-provider-cloud", "network/provider/cloud call count remains 0"),
        Negative("no-browser-cdp", "Browser/CDP live count remains 0"),
        Negative("no-wcu-ocr", "WCU/OCR live count remains 0"),
        Negative("no-recipes-execution", "recipes execution count remains 0"),
        Negative("no-physical-export", "physical export count remains 0"),
        Negative("no-redaction-runtime", "redaction runtime remains 0"),
        Negative("no-retention-deletion", "retention/deletion runtime remains 0"),
        Negative("no-release-commercial", "release/commercial remains NO-GO"),
        Negative("fail-closed-missing-approval", "future implementation must fail closed on missing approval or gate"),
        Negative("fail-closed-scope-mismatch", "future implementation must fail closed on scope mismatch"),
        Negative("fail-closed-unexpected-target", "future implementation must fail closed on unexpected target"),
        Negative("no-overclaim-wording", "scope proposal must not claim implementation readiness"),
        Negative("no-implementation-approval", "scope proposal does not approve implementation"),
        Negative("no-append-without-gate", "durable audit trail future append is denied without explicit approved gate"),
        Negative("no-arbitrary-event-writes", "future audit trail must reject arbitrary event writes"),
        Negative("no-mutation-store", "future audit trail scope does not open mutation store"),
        Negative("no-lifecycle-delete-retain", "future audit trail scope does not open deletion or retention runtime"),
        Negative("deterministic-fixture-only", "only deterministic fixture planning is allowed until implementation GO")
    ];

    private static FirstCapabilityNegativeTestRequirement Negative(string id, string assertion) =>
        new(id, assertion, RequiredBeforeImplementation: true, ImplementedNow: false);

    private static IReadOnlyList<FirstCapabilityExternalAuditRequirement> ExternalAudits() =>
    [
        new(
            AuditId: "selected-scope-pre-implementation",
            AuditName: "Selected capability scope external audit",
            RequiredBeforeImplementation: true,
            RequiredAfterImplementationBeforeEnablement: true,
            SatisfiedNow: false,
            PromptName: "NODAL_OS_SELECTED_CAPABILITY_SCOPE_EXTERNAL_AUDIT_READ_ONLY"),
        new(
            AuditId: "post-implementation-before-enablement",
            AuditName: "Post-implementation external audit before enablement",
            RequiredBeforeImplementation: true,
            RequiredAfterImplementationBeforeEnablement: true,
            SatisfiedNow: false,
            PromptName: "NODAL_OS_SELECTED_CAPABILITY_POST_IMPLEMENTATION_AUDIT_BEFORE_ENABLEMENT")
    ];

    private static FirstCapabilityScopeProposalCounts Counts() =>
        new(
            RuntimeEnabledCount: 0,
            ExecutionEnabledCount: 0,
            MutationEnabledCount: 0,
            ExportEnabledCount: 0,
            BrowserCdpLiveEnabledCount: 0,
            WcuOcrLiveEnabledCount: 0,
            RecipesExecutionEnabledCount: 0,
            ServiceRegistrationCount: 0,
            CommandHandlerCount: 0,
            ProductActionCount: 0,
            FilesystemOutputCount: 0,
            NetworkProviderCallCount: 0,
            ReleaseCommercialReadyCount: 0);

    private static IReadOnlyList<string> BlockedCapabilities() =>
    [
        "runtime/live",
        "execution real",
        "mutation real",
        "physical export real",
        "redaction runtime real",
        "secret/PII scan real",
        "retention/deletion runtime real",
        "service registration",
        "command handlers",
        "product actions",
        "filesystem product IO",
        "DB/migration",
        "provider/cloud/network",
        "browser/CDP live",
        "WCU/OCR live",
        "recipes execution real",
        "release/commercial readiness"
    ];

    private static IReadOnlyList<string> EvidenceLinks() =>
    [
        "docs/qa/nodal-os-implementation-planning-gate-hardening-design-only/report.md",
        "docs/qa/nodal-os-external-audit-pre-runtime-gate/report.md",
        "docs/adr/implementation-planning-gate-design-only.md",
        "docs/adr/first-real-capability-candidate-scope-proposal-read-only.md",
        "docs/decision-log.md"
    ];
}
