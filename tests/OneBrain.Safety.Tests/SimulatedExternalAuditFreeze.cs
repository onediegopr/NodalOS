namespace OneBrain.Safety.Tests.SimulatedRuntime;

public enum SimulatedAuditDecision
{
    AuditGoContinueSimulatedRuntime,
    AuditConditionalGoDocDriftOnly,
    AuditConditionalGoFlakyExternalOnly,
    AuditNoGoSafetyDrift,
    AuditNoGoScopeDrift,
    AuditNoGoReleaseDrift,
    AuditNoGoProductiveRuntimePath,
    AuditNoGoProviderCloudPath,
    AuditNoGoFilesystemBrowserUnlockPath,
    AuditNotRunPrepOnly
}

public enum SimulatedFindingClass
{
    NoFinding,
    DocDrift,
    FlakyExternal,
    SafetyDrift,
    ScopeDrift,
    ReleaseDrift,
    ProductiveRuntimePath,
    ProviderCloudPath,
    FilesystemBrowserUnlockPath,
    RedactionGap,
    NoExecutionGap,
    GovernanceInconsistency,
    ValidationGap
}

public enum SimulatedFindingSeverity
{
    Info,
    Low,
    Medium,
    High,
    Critical
}

public enum SimulatedRemediationType
{
    DocFixOnly,
    TestFixRequired,
    SafetyFixRequired,
    ScopeFixRequired,
    ReleaseBoundaryFixRequired,
    ValidationRerunRequired,
    NoActionRequired
}

public enum SimulatedFreezeStatus
{
    FreezeCandidateReady,
    FreezeCandidateReadyWithCaveats,
    FreezeCandidateBlockedByAudit,
    FreezeCandidateBlockedByValidation,
    FreezeCandidateBlockedByScopeDrift,
    FreezeCandidateBlockedByReleaseDrift
}

public enum SimulatedFreezeLockStatus
{
    FreezeLockReady,
    FreezeLockReadyWithCaveats,
    FreezeLockBlocked
}

public enum SimulatedFreezeLockGateDecision
{
    SimulatedFoundationFreezeLockReady,
    SimulatedFoundationFreezeLockReadyWithCaveats,
    SimulatedFoundationFreezeLockBlockedByAudit,
    SimulatedFoundationFreezeLockBlockedByValidation,
    SimulatedFoundationFreezeLockBlockedByScopeDrift,
    SimulatedFoundationFreezeLockBlockedByReleaseDrift
}

public sealed record SimulatedExternalAuditInputPackage(
    string ExecutiveSummary,
    string Branch,
    string Commit,
    string Scope,
    IReadOnlyList<string> GoDomains,
    IReadOnlyList<string> NoGoDomains,
    IReadOnlyList<string> AllowedTouchedDirectories,
    IReadOnlyList<string> ProhibitedTouchedDirectories,
    SimulatedGovernanceSnapshot GovernanceSnapshot,
    SimulatedReadinessScorecard ReadinessScorecard,
    SimulatedDriftGateDecision SafetyDriftGateResult,
    SimulatedAuditHandoffPack AuditHandoffPack,
    IReadOnlyList<string> ValidationEvidence,
    string CaveatHistory,
    IReadOnlyList<string> RiskRegister,
    IReadOnlyList<string> AuditQuestions,
    IReadOnlyList<string> ExpectedAuditDecisionOptions,
    bool ProductiveRuntime,
    bool ProviderCloudLiveCalls,
    bool FilesystemBrowserCapabilityUnlock,
    bool ReleaseStoreReady,
    bool ProductBridgeCspModified);

public sealed record SimulatedExternalAuditPrompt(
    string TargetAuditor,
    IReadOnlyList<string> ReviewTargets,
    IReadOnlyList<string> DecisionOptions,
    bool CoversGoNoGoBoundaries,
    bool ProductiveRuntimeAllowed,
    bool ProviderCloudAllowed,
    bool FilesystemBrowserCapabilityUnlockAllowed,
    bool ReleaseStoreAllowed,
    bool ProductBridgeCspAllowed);

public sealed record SimulatedExternalAuditResultIntake(
    string AuditResultId,
    string AuditorType,
    string AuditorModel,
    string AuditInputPackageRef,
    string AuditPromptRef,
    SimulatedAuditDecision AuditDecision,
    IReadOnlyList<string> Findings,
    SimulatedFindingSeverity Severity,
    IReadOnlyList<string> BlockingFindings,
    IReadOnlyList<string> NonBlockingFindings,
    IReadOnlyList<string> DocDriftFindings,
    IReadOnlyList<string> FlakyExternalFindings,
    IReadOnlyList<string> SafetyDriftFindings,
    IReadOnlyList<string> ScopeDriftFindings,
    IReadOnlyList<string> ReleaseDriftFindings,
    IReadOnlyList<string> ProductiveRuntimeFindings,
    IReadOnlyList<string> ProviderCloudFindings,
    IReadOnlyList<string> FilesystemBrowserUnlockFindings,
    IReadOnlyList<string> RecommendedActions,
    string FinalRecommendation,
    bool FreezeCandidateAllowed,
    bool CaveatRequired);

public sealed record SimulatedFindingTriageRule(
    SimulatedFindingClass FindingClass,
    SimulatedFindingSeverity Severity,
    bool TargetedSecurityPass,
    bool BlocksFreeze,
    bool ConditionalAllowed);

public sealed record SimulatedRemediationPlan(
    string RemediationPlanId,
    string SourceAuditResultId,
    string FindingId,
    SimulatedFindingClass FindingClass,
    SimulatedFindingSeverity Severity,
    string BlockingStatus,
    SimulatedRemediationType RemediationType,
    IReadOnlyList<string> AllowedFileScope,
    IReadOnlyList<string> ProhibitedFileScope,
    IReadOnlyList<string> RequiredValidation,
    string Owner,
    string TargetMacroHito,
    bool CanProceedToFreeze,
    string Notes);

public sealed record SimulatedFindingsTriageReport(
    SimulatedAuditDecision AuditDecision,
    int TotalFindings,
    int BlockingFindings,
    int NonBlockingFindings,
    int ConditionalFindings,
    IReadOnlyList<SimulatedRemediationPlan> RemediationPlanSummary,
    bool FreezeCandidateEligibility,
    string GoNoGoRecommendation,
    bool ProductRuntimeAllowed,
    bool ReleaseStoreAllowed,
    bool ProductBridgeCspAllowed);

public sealed record SimulatedFoundationFreezeCandidate(
    string FreezeCandidateId,
    string SourceCommit,
    SimulatedAuditDecision SourceAuditDecision,
    SimulatedGovernanceSnapshot SourceGovernanceSnapshot,
    SimulatedReadinessScorecard SourceReadinessScorecard,
    SimulatedDriftGateDecision SourceSafetyDriftGate,
    string SourceValidationSummary,
    string Scope,
    SimulatedFreezeStatus FreezeStatus,
    bool FreezeAllowed,
    IReadOnlyList<string> FreezeCaveats,
    IReadOnlyList<string> BlockedDomains,
    IReadOnlyList<string> NoGoDomains,
    IReadOnlyList<string> AllowedFutureWork,
    IReadOnlyList<string> ProhibitedFutureWork,
    bool ProductiveRuntime,
    bool ProviderCloud,
    bool FilesystemWrite,
    bool BrowserAutomation,
    bool CapabilityUnlock,
    bool PublicRelease,
    bool ChromeWebStore,
    bool SignedPublicZip,
    bool ProductFilesModified,
    bool BridgeCspModified);

public sealed record SimulatedFreezeBoundaryMatrix(
    IReadOnlyList<string> GoWithinFreeze,
    IReadOnlyList<string> NoGoWithinFreeze);

public sealed record SimulatedFreezeLockEvidence(
    string FreezeLockId,
    string FreezeCandidateId,
    SimulatedFreezeLockStatus LockStatus,
    string LockedScope,
    IReadOnlyList<string> BlockedInterpretations,
    IReadOnlyList<string> ValidationRefs,
    IReadOnlyList<string> AuditRefs,
    IReadOnlyList<string> GoNoGoRefs,
    string NoExecutionProofRef,
    string RedactionProofRef,
    string SafetyDriftGateRef,
    string LogicalTimestamp,
    bool ProductFilesModified,
    bool BridgeCspModified,
    bool ProductiveRuntime,
    bool ReleaseStore);

public sealed record SimulatedPostAuditContinuationPlan(
    IReadOnlyList<string> AllowedImmediateWork,
    IReadOnlyList<string> ConditionalWork,
    IReadOnlyList<string> BlockedWork,
    IReadOnlyList<string> FutureAuditRequiredWork,
    IReadOnlyList<string> OwnerManualApprovalRequiredWork);

public sealed record SimulatedPostAuditRiskRegister(IReadOnlyList<string> Risks);

public static class SimulatedExternalAuditFreeze
{
    public const string SourceCommit = "34625bcb5488cd910f8fc8ce547d98cef8983d26";
    public const string Branch = "chrome-lab-001-extension-local-ai-bridge";
    public const string Scope = "tests/safety + docs/reports + artifacts/agent-operations only";

    public static SimulatedExternalAuditInputPackage CreateInputPackage() => new(
        ExecutiveSummary: "NODAL OS simulated runtime foundation is fake-only, in-memory, test-only and ready for external audit input.",
        Branch: Branch,
        Commit: SourceCommit,
        Scope: Scope,
        GoDomains:
        [
            "simulated runtime fake-only",
            "in-memory evidence ledger",
            "routing matrix",
            "denylist-first enforcement",
            "manual approval simulated boundary",
            "replay audit-only guard",
            "audit export readiness",
            "governance snapshot",
            "readiness scorecard",
            "safety drift guard"
        ],
        NoGoDomains: NoGoDomains(),
        AllowedTouchedDirectories: ["tests/OneBrain.Safety.Tests", "docs/reports", "artifacts/agent-operations"],
        ProhibitedTouchedDirectories: ["src", "browser-extension", "release/store packaging", "provider/cloud production paths"],
        GovernanceSnapshot: SimulatedGovernancePreAudit.CreateSnapshot(),
        ReadinessScorecard: SimulatedGovernancePreAudit.CreateReadinessScorecard(),
        SafetyDriftGateResult: SimulatedDriftGateDecision.SafetyDriftGateGoNoDrift,
        AuditHandoffPack: SimulatedGovernancePreAudit.CreateAuditHandoffPack(),
        ValidationEvidence: ValidationEvidence(),
        CaveatHistory: "BrowserRuntimeSmoke Gate 9 historical flake closed in M827-M844; no reproduction in that block.",
        RiskRegister: CreateRiskRegister().Risks,
        AuditQuestions: SimulatedGovernancePreAudit.CreateAuditQuestions(),
        ExpectedAuditDecisionOptions: CreateAuditDecisionOptions(),
        ProductiveRuntime: false,
        ProviderCloudLiveCalls: false,
        FilesystemBrowserCapabilityUnlock: false,
        ReleaseStoreReady: false,
        ProductBridgeCspModified: false);

    public static SimulatedExternalAuditPrompt CreateAuditPrompt() => new(
        TargetAuditor: "Claude if available, otherwise GPT-5.5 Thinking XHigh",
        ReviewTargets:
        [
            "simulated runtime fake-only boundary",
            "no real executor proof",
            "no-execution proof",
            "redaction proof",
            "routing matrix",
            "denylist-first enforcement",
            "unsupported capability guard",
            "policy decision normalization",
            "manual approval boundary",
            "approval override prevention",
            "replay audit-only guard",
            "tamper/idempotency guard",
            "audit export redaction",
            "cross-run determinism",
            "governance snapshot",
            "readiness scorecard",
            "safety drift guard",
            "docs/artifacts/tests consistency",
            "validation evidence",
            "release/store/productive NO-GO boundaries"
        ],
        DecisionOptions: CreateAuditDecisionOptions(),
        CoversGoNoGoBoundaries: true,
        ProductiveRuntimeAllowed: false,
        ProviderCloudAllowed: false,
        FilesystemBrowserCapabilityUnlockAllowed: false,
        ReleaseStoreAllowed: false,
        ProductBridgeCspAllowed: false);

    public static IReadOnlyList<string> CreateAuditDecisionOptions() =>
    [
        "AUDIT_GO_CONTINUE_SIMULATED_RUNTIME",
        "AUDIT_CONDITIONAL_GO_DOC_DRIFT_ONLY",
        "AUDIT_CONDITIONAL_GO_FLAKY_EXTERNAL_ONLY",
        "AUDIT_NO_GO_SAFETY_DRIFT",
        "AUDIT_NO_GO_SCOPE_DRIFT",
        "AUDIT_NO_GO_RELEASE_DRIFT",
        "AUDIT_NO_GO_PRODUCTIVE_RUNTIME_PATH",
        "AUDIT_NO_GO_PROVIDER_CLOUD_PATH",
        "AUDIT_NO_GO_FILESYSTEM_BROWSER_UNLOCK_PATH"
    ];

    public static SimulatedExternalAuditResultIntake IntakeAuditResult(SimulatedAuditDecision decision) => new(
        AuditResultId: $"audit-result-{decision.ToString().ToLowerInvariant()}",
        AuditorType: decision == SimulatedAuditDecision.AuditNotRunPrepOnly ? "NOT_RUN" : "EXTERNAL",
        AuditorModel: decision == SimulatedAuditDecision.AuditNotRunPrepOnly ? "AUDIT_EXECUTION_PENDING" : "Claude_or_GPT_5_5_XHigh",
        AuditInputPackageRef: "artifacts/agent-operations/m845/external-audit-input-package.json",
        AuditPromptRef: "artifacts/agent-operations/m846/external-audit-prompt.json",
        AuditDecision: decision,
        Findings: decision == SimulatedAuditDecision.AuditGoContinueSimulatedRuntime ? [] : [decision.ToString()],
        Severity: IsNoGo(decision) ? SimulatedFindingSeverity.Critical : SimulatedFindingSeverity.Info,
        BlockingFindings: IsNoGo(decision) || decision == SimulatedAuditDecision.AuditNotRunPrepOnly ? [decision.ToString()] : [],
        NonBlockingFindings: IsConditional(decision) ? [decision.ToString()] : [],
        DocDriftFindings: decision == SimulatedAuditDecision.AuditConditionalGoDocDriftOnly ? ["DOC_DRIFT"] : [],
        FlakyExternalFindings: decision == SimulatedAuditDecision.AuditConditionalGoFlakyExternalOnly ? ["FLAKY_EXTERNAL"] : [],
        SafetyDriftFindings: decision == SimulatedAuditDecision.AuditNoGoSafetyDrift ? ["SAFETY_DRIFT"] : [],
        ScopeDriftFindings: decision == SimulatedAuditDecision.AuditNoGoScopeDrift ? ["SCOPE_DRIFT"] : [],
        ReleaseDriftFindings: decision == SimulatedAuditDecision.AuditNoGoReleaseDrift ? ["RELEASE_DRIFT"] : [],
        ProductiveRuntimeFindings: decision == SimulatedAuditDecision.AuditNoGoProductiveRuntimePath ? ["PRODUCTIVE_RUNTIME_PATH"] : [],
        ProviderCloudFindings: decision == SimulatedAuditDecision.AuditNoGoProviderCloudPath ? ["PROVIDER_CLOUD_PATH"] : [],
        FilesystemBrowserUnlockFindings: decision == SimulatedAuditDecision.AuditNoGoFilesystemBrowserUnlockPath ? ["FILESYSTEM_BROWSER_UNLOCK_PATH"] : [],
        RecommendedActions: decision == SimulatedAuditDecision.AuditNotRunPrepOnly ? ["execute external audit before freeze lock"] : ["triage audit findings before continuation"],
        FinalRecommendation: ResolveFinalRecommendation(decision),
        FreezeCandidateAllowed: AllowsFreezeCandidate(decision),
        CaveatRequired: IsConditional(decision));

    public static bool AllowsFreezeCandidate(SimulatedAuditDecision decision) =>
        decision is SimulatedAuditDecision.AuditGoContinueSimulatedRuntime
            or SimulatedAuditDecision.AuditConditionalGoDocDriftOnly
            or SimulatedAuditDecision.AuditConditionalGoFlakyExternalOnly;

    public static SimulatedFindingTriageRule Triage(SimulatedFindingClass findingClass, SimulatedFindingSeverity severity, bool targetedSecurityPass = true)
    {
        var blocks = findingClass switch
        {
            SimulatedFindingClass.DocDrift when severity is SimulatedFindingSeverity.Low or SimulatedFindingSeverity.Medium => false,
            SimulatedFindingClass.FlakyExternal when targetedSecurityPass => false,
            SimulatedFindingClass.SafetyDrift when severity is SimulatedFindingSeverity.High or SimulatedFindingSeverity.Critical => true,
            SimulatedFindingClass.ScopeDrift => true,
            SimulatedFindingClass.ReleaseDrift => true,
            SimulatedFindingClass.ProductiveRuntimePath => true,
            SimulatedFindingClass.ProviderCloudPath => true,
            SimulatedFindingClass.FilesystemBrowserUnlockPath => true,
            SimulatedFindingClass.RedactionGap when severity is SimulatedFindingSeverity.High or SimulatedFindingSeverity.Critical => true,
            SimulatedFindingClass.NoExecutionGap => true,
            SimulatedFindingClass.ValidationGap => true,
            _ => false
        };

        var conditional = !blocks && findingClass is SimulatedFindingClass.DocDrift or SimulatedFindingClass.FlakyExternal;
        return new SimulatedFindingTriageRule(findingClass, severity, targetedSecurityPass, blocks, conditional);
    }

    public static SimulatedRemediationPlan CreateRemediationPlan(SimulatedFindingClass findingClass, SimulatedRemediationType remediationType) => new(
        RemediationPlanId: $"remediation-{findingClass.ToString().ToLowerInvariant()}",
        SourceAuditResultId: "audit-result-pending",
        FindingId: $"finding-{findingClass.ToString().ToLowerInvariant()}",
        FindingClass: findingClass,
        Severity: findingClass == SimulatedFindingClass.DocDrift ? SimulatedFindingSeverity.Low : SimulatedFindingSeverity.High,
        BlockingStatus: RemediationBlocksFreeze(remediationType) ? "BLOCKING" : "CONDITIONAL_OR_NONE",
        RemediationType: remediationType,
        AllowedFileScope: ["tests/OneBrain.Safety.Tests", "docs/reports", "artifacts/agent-operations"],
        ProhibitedFileScope: ["src", "browser-extension", "product files", "Bridge/CSP"],
        RequiredValidation: ["build", "targeted filters", "full safety suite", "recipes suite", "full suite"],
        Owner: "NODAL OS owner",
        TargetMacroHito: "M863-M874",
        CanProceedToFreeze: !RemediationBlocksFreeze(remediationType),
        Notes: "No product files or Bridge/CSP changes are allowed.");

    public static SimulatedFindingsTriageReport CreateFindingsTriageReport(SimulatedAuditDecision decision) => new(
        AuditDecision: decision,
        TotalFindings: decision == SimulatedAuditDecision.AuditGoContinueSimulatedRuntime ? 0 : 1,
        BlockingFindings: AllowsFreezeCandidate(decision) ? 0 : 1,
        NonBlockingFindings: IsConditional(decision) ? 1 : 0,
        ConditionalFindings: IsConditional(decision) ? 1 : 0,
        RemediationPlanSummary: [CreateRemediationPlan(SimulatedFindingClass.ValidationGap, SimulatedRemediationType.ValidationRerunRequired)],
        FreezeCandidateEligibility: AllowsFreezeCandidate(decision),
        GoNoGoRecommendation: AllowsFreezeCandidate(decision) ? "GO_OR_CONDITIONAL_GO" : "NO_GO_OR_PREP_ONLY",
        ProductRuntimeAllowed: false,
        ReleaseStoreAllowed: false,
        ProductBridgeCspAllowed: false);

    public static SimulatedFoundationFreezeCandidate CreateFreezeCandidate(SimulatedAuditDecision decision, bool validationPass = true)
    {
        var status = ResolveFreezeStatus(decision, validationPass);
        return new SimulatedFoundationFreezeCandidate(
            FreezeCandidateId: "freeze-candidate-m851",
            SourceCommit: SourceCommit,
            SourceAuditDecision: decision,
            SourceGovernanceSnapshot: SimulatedGovernancePreAudit.CreateSnapshot(),
            SourceReadinessScorecard: SimulatedGovernancePreAudit.CreateReadinessScorecard(),
            SourceSafetyDriftGate: SimulatedDriftGateDecision.SafetyDriftGateGoNoDrift,
            SourceValidationSummary: validationPass ? "validation pass" : "validation gap",
            Scope: "SIMULATED_RUNTIME_FOUNDATION_TEST_ONLY",
            FreezeStatus: status,
            FreezeAllowed: AllowsFreezeCandidate(decision) && validationPass,
            FreezeCaveats: IsConditional(decision) ? [decision.ToString()] : [],
            BlockedDomains: AllowsFreezeCandidate(decision) && validationPass ? [] : [decision.ToString()],
            NoGoDomains: NoGoDomains(),
            AllowedFutureWork: ["docs cleanup", "artifact consolidation", "safety tests", "fake-only in-memory matrix consolidation"],
            ProhibitedFutureWork: BlockedWork(),
            ProductiveRuntime: false,
            ProviderCloud: false,
            FilesystemWrite: false,
            BrowserAutomation: false,
            CapabilityUnlock: false,
            PublicRelease: false,
            ChromeWebStore: false,
            SignedPublicZip: false,
            ProductFilesModified: false,
            BridgeCspModified: false);
    }

    public static SimulatedFreezeBoundaryMatrix CreateFreezeBoundaryMatrix() => new(
        GoWithinFreeze:
        [
            "simulated runtime contracts",
            "fake-only executors",
            "in-memory evidence ledger",
            "no-execution proof",
            "redaction proof",
            "governance snapshot",
            "readiness scorecard",
            "audit handoff",
            "safety drift guard",
            "timeline/replay/export/determinism"
        ],
        NoGoWithinFreeze: BlockedWork());

    public static bool RejectsForbiddenFreezeClaim(string claim) =>
        claim.Contains("productive enabled", StringComparison.OrdinalIgnoreCase) ||
        claim.Contains("provider cloud enabled", StringComparison.OrdinalIgnoreCase) ||
        claim.Contains("filesystem write enabled", StringComparison.OrdinalIgnoreCase) ||
        claim.Contains("browser automation enabled", StringComparison.OrdinalIgnoreCase) ||
        claim.Contains("capability unlock enabled", StringComparison.OrdinalIgnoreCase) ||
        claim.Contains("public release ready", StringComparison.OrdinalIgnoreCase) ||
        claim.Contains("Store ready", StringComparison.OrdinalIgnoreCase) ||
        claim.Contains("signed ZIP ready", StringComparison.OrdinalIgnoreCase) ||
        claim.Contains("product files can be modified", StringComparison.OrdinalIgnoreCase) ||
        claim.Contains("Bridge/CSP can be modified", StringComparison.OrdinalIgnoreCase) ||
        claim.Contains("audit findings can be ignored", StringComparison.OrdinalIgnoreCase) ||
        claim.Contains("no future tests required", StringComparison.OrdinalIgnoreCase);

    public static SimulatedFreezeLockEvidence CreateFreezeLockEvidence(SimulatedAuditDecision decision) => new(
        FreezeLockId: "freeze-lock-m855",
        FreezeCandidateId: "freeze-candidate-m851",
        LockStatus: AllowsFreezeCandidate(decision)
            ? (IsConditional(decision) ? SimulatedFreezeLockStatus.FreezeLockReadyWithCaveats : SimulatedFreezeLockStatus.FreezeLockReady)
            : SimulatedFreezeLockStatus.FreezeLockBlocked,
        LockedScope: "SIMULATED_RUNTIME_FOUNDATION_TEST_ONLY",
        BlockedInterpretations:
        [
            "freeze does not mean productive enabled",
            "freeze does not mean provider/cloud enabled",
            "freeze does not mean release/store ready",
            "freeze does not allow product files or Bridge/CSP modifications"
        ],
        ValidationRefs: ValidationEvidence(),
        AuditRefs: ["external-audit-input-package", "external-audit-prompt", "external-audit-result-intake-contract"],
        GoNoGoRefs: ["governance-pre-audit-go-no-go", "foundation-freeze-candidate-go-no-go"],
        NoExecutionProofRef: "no-execution-proof",
        RedactionProofRef: "redaction-proof",
        SafetyDriftGateRef: "safety-drift-gate-go-no-drift",
        LogicalTimestamp: "logical-m855",
        ProductFilesModified: false,
        BridgeCspModified: false,
        ProductiveRuntime: false,
        ReleaseStore: false);

    public static SimulatedFreezeLockGateDecision DecideFreezeLockGate(SimulatedAuditDecision decision, bool validationPass = true) =>
        !validationPass ? SimulatedFreezeLockGateDecision.SimulatedFoundationFreezeLockBlockedByValidation :
        decision switch
        {
            SimulatedAuditDecision.AuditGoContinueSimulatedRuntime => SimulatedFreezeLockGateDecision.SimulatedFoundationFreezeLockReady,
            SimulatedAuditDecision.AuditConditionalGoDocDriftOnly or SimulatedAuditDecision.AuditConditionalGoFlakyExternalOnly => SimulatedFreezeLockGateDecision.SimulatedFoundationFreezeLockReadyWithCaveats,
            SimulatedAuditDecision.AuditNoGoScopeDrift => SimulatedFreezeLockGateDecision.SimulatedFoundationFreezeLockBlockedByScopeDrift,
            SimulatedAuditDecision.AuditNoGoReleaseDrift => SimulatedFreezeLockGateDecision.SimulatedFoundationFreezeLockBlockedByReleaseDrift,
            _ => SimulatedFreezeLockGateDecision.SimulatedFoundationFreezeLockBlockedByAudit
        };

    public static SimulatedPostAuditContinuationPlan CreateContinuationPlan() => new(
        AllowedImmediateWork:
        [
            "docs cleanup",
            "artifact consolidation",
            "safety tests",
            "governance scorecards",
            "internal simulated runtime refinements",
            "no-execution proof hardening",
            "redaction proof hardening",
            "fake-only in-memory matrix consolidation"
        ],
        ConditionalWork:
        [
            "future dry-run real planning",
            "future local-only provider planning",
            "future filesystem read-only planning",
            "future UI documentation views planning"
        ],
        BlockedWork: BlockedWork(),
        FutureAuditRequiredWork: ["any movement toward real dry-run", "any local provider runtime re-entry"],
        OwnerManualApprovalRequiredWork: ["future runtime re-entry criteria", "any product file authorization"]);

    public static string RecommendNextMacroHito(SimulatedFreezeLockGateDecision gate) =>
        gate switch
        {
            SimulatedFreezeLockGateDecision.SimulatedFoundationFreezeLockReady =>
                "M863-M874 — Simulated Runtime Foundation Freeze Lock Documentation + Future Runtime Re-Entry Criteria",
            SimulatedFreezeLockGateDecision.SimulatedFoundationFreezeLockReadyWithCaveats =>
                "M863-M874 — Caveat Remediation + Freeze Lock Documentation + Future Runtime Re-Entry Criteria",
            _ => "M863-M874 — Audit Remediation + Safety Drift Repair + Revalidation Gate"
        };

    public static SimulatedPostAuditRiskRegister CreateRiskRegister() => new(
    [
        "freeze misinterpreted as release",
        "freeze misinterpreted as productive unlock",
        "future drift from simulated to real execution",
        "provider/cloud accidentally wired",
        "filesystem/browser unlock accidentally wired",
        "product/Bridge/CSP touched without gate",
        "redaction regression",
        "no-execution proof regression",
        "full suite flake recurrence",
        "docs/artifacts/tests contradiction"
    ]);

    private static SimulatedFreezeStatus ResolveFreezeStatus(SimulatedAuditDecision decision, bool validationPass)
    {
        if (!validationPass)
            return SimulatedFreezeStatus.FreezeCandidateBlockedByValidation;

        return decision switch
        {
            SimulatedAuditDecision.AuditGoContinueSimulatedRuntime => SimulatedFreezeStatus.FreezeCandidateReady,
            SimulatedAuditDecision.AuditConditionalGoDocDriftOnly or SimulatedAuditDecision.AuditConditionalGoFlakyExternalOnly => SimulatedFreezeStatus.FreezeCandidateReadyWithCaveats,
            SimulatedAuditDecision.AuditNoGoScopeDrift => SimulatedFreezeStatus.FreezeCandidateBlockedByScopeDrift,
            SimulatedAuditDecision.AuditNoGoReleaseDrift => SimulatedFreezeStatus.FreezeCandidateBlockedByReleaseDrift,
            _ => SimulatedFreezeStatus.FreezeCandidateBlockedByAudit
        };
    }

    private static bool IsConditional(SimulatedAuditDecision decision) =>
        decision is SimulatedAuditDecision.AuditConditionalGoDocDriftOnly or SimulatedAuditDecision.AuditConditionalGoFlakyExternalOnly;

    private static bool IsNoGo(SimulatedAuditDecision decision) =>
        decision.ToString().StartsWith("AuditNoGo", StringComparison.Ordinal);

    private static bool RemediationBlocksFreeze(SimulatedRemediationType type) =>
        type is SimulatedRemediationType.TestFixRequired
            or SimulatedRemediationType.SafetyFixRequired
            or SimulatedRemediationType.ScopeFixRequired
            or SimulatedRemediationType.ReleaseBoundaryFixRequired
            or SimulatedRemediationType.ValidationRerunRequired;

    private static string ResolveFinalRecommendation(SimulatedAuditDecision decision) =>
        decision switch
        {
            SimulatedAuditDecision.AuditGoContinueSimulatedRuntime => "continue to simulated foundation freeze candidate",
            SimulatedAuditDecision.AuditConditionalGoDocDriftOnly => "continue with doc drift caveat",
            SimulatedAuditDecision.AuditConditionalGoFlakyExternalOnly => "continue with flaky external caveat",
            SimulatedAuditDecision.AuditNotRunPrepOnly => "audit execution pending before freeze lock",
            _ => "do not continue to freeze candidate"
        };

    private static IReadOnlyList<string> ValidationEvidence() =>
    [
        "build PASS",
        "M797-M799 filter PASS",
        "M800-M802 filter PASS",
        "M803-M805 filter PASS",
        "M806-M814 filter PASS",
        "M815-M826 filter PASS",
        "M827-M844 filter PASS",
        "BrowserRuntimeSmoke isolated PASS",
        "full safety suite PASS",
        "Recipes PASS",
        "full suite PASS"
    ];

    private static IReadOnlyList<string> NoGoDomains() =>
    [
        "runtime productive execution",
        "provider/cloud live calls",
        "filesystem write/browser automation/capability unlock",
        "PRODUCTIVE_ENABLED",
        "public release",
        "Chrome Web Store",
        "signed public ZIP",
        "product files modification",
        "Bridge/CSP modification"
    ];

    private static IReadOnlyList<string> BlockedWork() =>
    [
        "productive runtime",
        "provider/cloud",
        "filesystem write real",
        "browser automation real",
        "capability unlock",
        "public release",
        "Chrome Web Store",
        "signed public ZIP",
        "product file changes",
        "Bridge/CSP changes"
    ];
}
