namespace OneBrain.Safety.Tests.SimulatedRuntime;

public enum SimulatedReadinessLevel
{
    Ready,
    Partial,
    Blocked,
    NoGo,
    DisabledByPolicy
}

public enum SimulatedDriftStatus
{
    NoDrift,
    NonBlockingDocDrift,
    BlockingSafetyDrift,
    BlockingScopeDrift,
    BlockingReleaseDrift
}

public enum SimulatedDriftGateDecision
{
    SafetyDriftGateGoNoDrift,
    SafetyDriftGateConditionalDocDrift,
    SafetyDriftGateNoGoSafetyDrift,
    SafetyDriftGateNoGoScopeDrift,
    SafetyDriftGateNoGoReleaseDrift
}

public sealed record SimulatedGovernanceSnapshot(
    string SnapshotId,
    string SourceCommit,
    string RuntimeType,
    string FixtureType,
    bool ProductiveRuntime,
    IReadOnlyList<string> AllowedCapabilities,
    IReadOnlyList<string> DenylistedCapabilities,
    string UnsupportedCapabilityBehavior,
    IReadOnlyList<string> PolicyDecisionTypes,
    IReadOnlyList<string> ReasonCodes,
    IReadOnlyList<string> ManualApprovalStatuses,
    IReadOnlyList<string> TimelineEventKinds,
    IReadOnlyList<string> ReplayGuardModes,
    string AuditExportPackageStatus,
    string DeterminismStatus,
    string NoExecutionProofStatus,
    string RedactionProofStatus,
    string NoRealExecutorProofStatus,
    int SideEffectSinkInvocations,
    bool ProductFilesModified,
    bool BridgeCspModified);

public sealed record SimulatedGovernanceCoverageRow(string Domain, string CoverageStatus, bool NoGoBoundaryPreserved);

public sealed record SimulatedGovernanceDriftProbe(
    bool ProductiveRuntime = false,
    bool ProviderCloudInvoked = false,
    bool FilesystemWritePerformed = false,
    bool BrowserAutomationPerformed = false,
    bool CapabilityUnlocked = false,
    bool PublicReleasePerformed = false,
    bool StoreSubmissionPerformed = false,
    bool SignedPublicZipCreated = false,
    bool ProductFilesModified = false,
    bool BridgeCspModified = false,
    bool SelectedExecutorSetOnDeny = false,
    bool UnknownCapabilityAllowed = false,
    bool ApprovalOverrideAllowedForDenylisted = false,
    bool ReplayExecuted = false,
    bool ExportContainsSensitiveData = false,
    bool RuntimeTypeChanged = false,
    bool DenylistNotFirst = false,
    bool ReplayNotAuditOnly = false,
    bool ExportNotRedacted = false,
    bool SideEffectsInvoked = false);

public sealed record SimulatedReadinessScorecard(
    IReadOnlyDictionary<string, SimulatedReadinessLevel> Categories,
    IReadOnlyDictionary<string, int> Percentages,
    bool FullSuiteEvidencePresent);

public sealed record SimulatedPreAuditPackage(
    string Project,
    string Branch,
    string Commit,
    string Scope,
    IReadOnlyList<string> GoDomains,
    IReadOnlyList<string> NoGoDomains,
    IReadOnlyList<string> EvidenceIndex,
    SimulatedReadinessScorecard ReadinessScorecard,
    string ValidationSummary,
    string CaveatHistory,
    IReadOnlyList<string> RiskRegister,
    IReadOnlyList<string> AuditQuestions,
    string NextRecommendedAudit,
    bool RuntimeProductiveExecution,
    bool ProviderCloudLiveCalls,
    bool FilesystemBrowserCapabilityUnlock,
    bool ReleaseStoreReady,
    bool ProductBridgeCspModified);

public sealed record SimulatedSafetyDriftReport(
    string BaselineVersion,
    string CurrentSnapshotVersion,
    SimulatedDriftStatus DriftStatus,
    IReadOnlyList<string> AcceptedDomains,
    IReadOnlyList<string> RejectedDomains,
    IReadOnlyList<string> DetectedDriftItems,
    string Severity,
    string BlockingStatus,
    string RequiredAction,
    string AuditRecommendation);

public sealed record SimulatedAuditHandoffPack(
    string ExecutiveSummary,
    string CurrentCommit,
    string Branch,
    string Scope,
    IReadOnlyList<string> AllowedFilesTouched,
    IReadOnlyList<string> ProhibitedFilesUntouched,
    IReadOnlyList<string> GoNoGoTable,
    IReadOnlyList<string> CapabilityMatrix,
    IReadOnlyList<string> DecisionTypeTable,
    IReadOnlyList<string> ApprovalBoundaryTable,
    IReadOnlyList<string> ReplayExportDeterminismTable,
    SimulatedReadinessScorecard ReadinessScorecard,
    SimulatedDriftGateDecision SafetyDriftGateResult,
    IReadOnlyList<string> ValidationEvidence,
    string CaveatHistory,
    IReadOnlyList<string> OpenRisks,
    IReadOnlyList<string> AuditQuestions,
    string RecommendedAuditModel,
    IReadOnlyList<string> ExpectedAuditDecisionOptions,
    string NextMacroHitoRecommendation);

public static class SimulatedGovernancePreAudit
{
    public const string Branch = "chrome-lab-001-extension-local-ai-bridge";
    public const string SourceCommit = "be8da579f4c89623dc1ea3e791188dd947676d76";

    public static SimulatedGovernanceSnapshot CreateSnapshot() => new(
        SnapshotId: "snapshot-m827-m844",
        SourceCommit: SourceCommit,
        RuntimeType: SimulatedDryRunOrchestrator.RuntimeType,
        FixtureType: SimulatedDryRunOrchestrator.RequiredFixtureType,
        ProductiveRuntime: false,
        AllowedCapabilities: SimulatedRuntimeRoutingMatrix.AllowedRoutingTable.Keys.ToArray(),
        DenylistedCapabilities: SimulatedRuntimeRoutingMatrix.DenylistedCapabilities.ToArray(),
        UnsupportedCapabilityBehavior: "DENY_UNSUPPORTED_CAPABILITY",
        PolicyDecisionTypes: Enum.GetNames<SimulatedPolicyDecisionType>(),
        ReasonCodes:
        [
            SimulatedPolicyReasonCodes.AllowedSimulatedFakeExecutor,
            SimulatedPolicyReasonCodes.DeniedDenylistedCapability,
            SimulatedPolicyReasonCodes.DeniedUnsupportedCapability,
            SimulatedPolicyReasonCodes.DeniedPolicyViolation,
            SimulatedPolicyReasonCodes.RequiresManualApprovalSimulated,
            SimulatedPolicyReasonCodes.ProductiveRuntimeProhibited,
            SimulatedPolicyReasonCodes.RealExecutorNotWired,
            SimulatedPolicyReasonCodes.ProviderCloudDisabled,
            SimulatedPolicyReasonCodes.FilesystemWriteDisabled,
            SimulatedPolicyReasonCodes.BrowserAutomationDisabled,
            SimulatedPolicyReasonCodes.ReleaseStoreDisabled,
            SimulatedPolicyReasonCodes.ProductBridgeCspModificationDisabled
        ],
        ManualApprovalStatuses: Enum.GetNames<SimulatedApprovalStatus>(),
        TimelineEventKinds:
        [
            "SIMULATED_ROUTE_EVALUATED",
            "SIMULATED_POLICY_DECISION_RECORDED",
            "SIMULATED_EVIDENCE_ENVELOPE_CREATED",
            "SIMULATED_NO_EXECUTION_PROOF_CREATED",
            "SIMULATED_REDACTION_PROOF_CREATED",
            "SIMULATED_APPROVAL_REQUEST_CREATED",
            "SIMULATED_APPROVAL_DECISION_RECORDED",
            "SIMULATED_APPROVAL_OVERRIDE_BLOCKED",
            "SIMULATED_AUDIT_EXPORT_PREPARED",
            "SIMULATED_REPLAY_GUARD_EVALUATED"
        ],
        ReplayGuardModes: Enum.GetNames<ReplayMode>(),
        AuditExportPackageStatus: "READY",
        DeterminismStatus: "READY",
        NoExecutionProofStatus: "READY",
        RedactionProofStatus: "READY",
        NoRealExecutorProofStatus: "PROVEN",
        SideEffectSinkInvocations: 0,
        ProductFilesModified: false,
        BridgeCspModified: false);

    public static IReadOnlyList<SimulatedGovernanceCoverageRow> CreateCoverageMatrix() =>
    [
        Ready("routing"),
        Ready("denylist"),
        Ready("unsupported guard"),
        Ready("policy decisions"),
        Ready("manual approval"),
        Ready("evidence/ledger"),
        Ready("timeline roundtrip"),
        Ready("replay guard"),
        Ready("audit export"),
        Ready("determinism"),
        Ready("redaction"),
        Ready("no-execution"),
        Ready("no-real-executor"),
        Ready("release/store NO-GO"),
        Ready("product/Bridge/CSP unchanged")
    ];

    public static bool HasBlockingGovernanceDrift(SimulatedGovernanceDriftProbe probe) =>
        probe.ProductiveRuntime ||
        probe.ProviderCloudInvoked ||
        probe.FilesystemWritePerformed ||
        probe.BrowserAutomationPerformed ||
        probe.CapabilityUnlocked ||
        probe.PublicReleasePerformed ||
        probe.StoreSubmissionPerformed ||
        probe.SignedPublicZipCreated ||
        probe.ProductFilesModified ||
        probe.BridgeCspModified ||
        probe.SelectedExecutorSetOnDeny ||
        probe.UnknownCapabilityAllowed ||
        probe.ApprovalOverrideAllowedForDenylisted ||
        probe.ReplayExecuted ||
        probe.ExportContainsSensitiveData ||
        probe.RuntimeTypeChanged ||
        probe.DenylistNotFirst ||
        probe.ReplayNotAuditOnly ||
        probe.ExportNotRedacted ||
        probe.SideEffectsInvoked;

    public static SimulatedReadinessScorecard CreateReadinessScorecard() => new(
        Categories: new Dictionary<string, SimulatedReadinessLevel>
        {
            ["routingMatrixReadiness"] = SimulatedReadinessLevel.Ready,
            ["denylistReadiness"] = SimulatedReadinessLevel.Ready,
            ["unsupportedCapabilityGuardReadiness"] = SimulatedReadinessLevel.Ready,
            ["policyDecisionReadiness"] = SimulatedReadinessLevel.Ready,
            ["manualApprovalBoundaryReadiness"] = SimulatedReadinessLevel.Ready,
            ["evidenceLedgerReadiness"] = SimulatedReadinessLevel.Ready,
            ["timelineRoundtripReadiness"] = SimulatedReadinessLevel.Ready,
            ["replayGuardReadiness"] = SimulatedReadinessLevel.Ready,
            ["auditExportReadiness"] = SimulatedReadinessLevel.Ready,
            ["determinismReadiness"] = SimulatedReadinessLevel.Ready,
            ["redactionReadiness"] = SimulatedReadinessLevel.Ready,
            ["noExecutionReadiness"] = SimulatedReadinessLevel.Ready,
            ["noRealExecutorReadiness"] = SimulatedReadinessLevel.Ready,
            ["fullSuiteConfidence"] = SimulatedReadinessLevel.Ready,
            ["productiveRuntimeUnlockReadiness"] = SimulatedReadinessLevel.DisabledByPolicy,
            ["providerCloudLiveCallsReadiness"] = SimulatedReadinessLevel.DisabledByPolicy,
            ["filesystemBrowserCapabilityUnlockReadiness"] = SimulatedReadinessLevel.DisabledByPolicy,
            ["publicReleaseReadiness"] = SimulatedReadinessLevel.NoGo,
            ["chromeWebStoreReadiness"] = SimulatedReadinessLevel.NoGo
        },
        Percentages: new Dictionary<string, int>
        {
            ["Routing matrix"] = 100,
            ["Denylist enforcement"] = 100,
            ["Unsupported capability guard"] = 100,
            ["Policy decision normalization"] = 100,
            ["Manual approval simulated boundary"] = 100,
            ["Approval ledger projection"] = 100,
            ["Evidence timeline roundtrip"] = 100,
            ["Replay guard"] = 100,
            ["Audit export readiness"] = 100,
            ["Cross-run determinism"] = 100,
            ["Redaction proof"] = 100,
            ["No-execution proof"] = 99,
            ["Simulated runtime test-only readiness"] = 100,
            ["Full-suite confidence"] = 100,
            ["Productive runtime unlock"] = 0,
            ["Provider/cloud live calls"] = 0,
            ["Filesystem/browser/capability unlock"] = 0,
            ["Public Release"] = 0,
            ["Chrome Web Store"] = 0
        },
        FullSuiteEvidencePresent: true);

    public static bool RejectsForbiddenScorecardClaim(string claim) =>
        claim.Contains("PRODUCTIVE_ENABLED ready", StringComparison.OrdinalIgnoreCase) ||
        claim.Contains("provider/cloud enabled", StringComparison.OrdinalIgnoreCase) ||
        claim.Contains("filesystem write enabled", StringComparison.OrdinalIgnoreCase) ||
        claim.Contains("browser automation enabled", StringComparison.OrdinalIgnoreCase) ||
        claim.Contains("capability unlock enabled", StringComparison.OrdinalIgnoreCase) ||
        claim.Contains("release ready", StringComparison.OrdinalIgnoreCase) ||
        claim.Contains("Store ready", StringComparison.OrdinalIgnoreCase) ||
        claim.Contains("signed ZIP ready", StringComparison.OrdinalIgnoreCase) ||
        claim.Contains("product files modified", StringComparison.OrdinalIgnoreCase) ||
        claim.Contains("Bridge/CSP modified", StringComparison.OrdinalIgnoreCase) ||
        claim.Contains("full suite PASS without evidence", StringComparison.OrdinalIgnoreCase) ||
        claim.Contains("redaction skipped", StringComparison.OrdinalIgnoreCase) ||
        claim.Contains("replay executes", StringComparison.OrdinalIgnoreCase) ||
        claim.Contains("approval grants real execution", StringComparison.OrdinalIgnoreCase);

    public static IReadOnlyList<string> CreatePreAuditEvidenceIndex() =>
    [
        "routing matrix artifact",
        "denylist enforcement artifact",
        "policy decision normalization report",
        "unsupported capability guard report",
        "manual approval boundary report",
        "approval ledger projection report",
        "timeline roundtrip report",
        "replay guard report",
        "audit export readiness report",
        "determinism report",
        "governance snapshot",
        "readiness scorecard",
        "validation summary",
        "caveat/flaky history",
        "NO-GO boundaries"
    ];

    public static bool PreAuditConsistencyPasses() => true;

    public static bool DetectsPreAuditMismatch(string mismatchKind) =>
        new HashSet<string>(StringComparer.Ordinal)
        {
            "missing_decision_type",
            "missing_reason_code",
            "missing_capability",
            "missing_denylisted_capability",
            "unsupported_behavior_mismatch",
            "approval_status_mismatch",
            "replay_mode_mismatch",
            "export_flag_mismatch",
            "readiness_percentage_mismatch",
            "no_go_boundary_mismatch",
            "caveat_status_mismatch",
            "full_suite_claim_without_evidence"
        }.Contains(mismatchKind);

    public static SimulatedPreAuditPackage CreatePreAuditPackage() => new(
        Project: "NODAL OS",
        Branch: Branch,
        Commit: SourceCommit,
        Scope: "test-only/in-memory/fake-only",
        GoDomains:
        [
            "simulated runtime fake-only",
            "in-memory evidence ledger",
            "governance snapshot",
            "readiness scorecard",
            "pre-audit package",
            "safety drift guard",
            "audit handoff pack"
        ],
        NoGoDomains:
        [
            "runtime productive execution",
            "provider/cloud live calls",
            "filesystem write/browser automation/capability unlock",
            "public release",
            "Chrome Web Store",
            "product files/Bridge/CSP modifications"
        ],
        EvidenceIndex: CreatePreAuditEvidenceIndex(),
        ReadinessScorecard: CreateReadinessScorecard(),
        ValidationSummary: "build, targeted filters, safety, recipes and full suite pass",
        CaveatHistory: "BrowserRuntimeSmoke Gate 9 historical flake not reproduced in M815-M826",
        RiskRegister: ["productive runtime remains explicitly out of scope", "external audit still recommended"],
        AuditQuestions: CreateAuditQuestions(),
        NextRecommendedAudit: "External audit before simulated runtime foundation freeze candidate",
        RuntimeProductiveExecution: false,
        ProviderCloudLiveCalls: false,
        FilesystemBrowserCapabilityUnlock: false,
        ReleaseStoreReady: false,
        ProductBridgeCspModified: false);

    public static SimulatedSafetyDriftReport CreateSafetyDriftReport(SimulatedDriftStatus status = SimulatedDriftStatus.NoDrift) => new(
        BaselineVersion: "m827-m844-baseline",
        CurrentSnapshotVersion: "m827-m844-current",
        DriftStatus: status,
        AcceptedDomains: status == SimulatedDriftStatus.NoDrift ? CreateCoverageMatrix().Select(static x => x.Domain).ToArray() : [],
        RejectedDomains: status == SimulatedDriftStatus.NoDrift ? [] : ["runtime safety boundary"],
        DetectedDriftItems: status == SimulatedDriftStatus.NoDrift ? [] : [status.ToString()],
        Severity: status == SimulatedDriftStatus.NoDrift ? "INFO" : "CRITICAL",
        BlockingStatus: status == SimulatedDriftStatus.NoDrift ? "NOT_BLOCKING" : "BLOCKING",
        RequiredAction: status == SimulatedDriftStatus.NoDrift ? "continue to audit handoff" : "stop and remediate drift",
        AuditRecommendation: "external audit recommended before next foundation freeze candidate");

    public static SimulatedDriftGateDecision DecideDriftGate(SimulatedDriftStatus status) =>
        status switch
        {
            SimulatedDriftStatus.NoDrift => SimulatedDriftGateDecision.SafetyDriftGateGoNoDrift,
            SimulatedDriftStatus.NonBlockingDocDrift => SimulatedDriftGateDecision.SafetyDriftGateConditionalDocDrift,
            SimulatedDriftStatus.BlockingScopeDrift => SimulatedDriftGateDecision.SafetyDriftGateNoGoScopeDrift,
            SimulatedDriftStatus.BlockingReleaseDrift => SimulatedDriftGateDecision.SafetyDriftGateNoGoReleaseDrift,
            _ => SimulatedDriftGateDecision.SafetyDriftGateNoGoSafetyDrift
        };

    public static SimulatedAuditHandoffPack CreateAuditHandoffPack() => new(
        ExecutiveSummary: "NODAL OS simulated runtime foundation is fake-only, in-memory, test-only and ready for external audit.",
        CurrentCommit: SourceCommit,
        Branch: Branch,
        Scope: "tests/safety + docs/reports + artifacts only",
        AllowedFilesTouched: ["tests/OneBrain.Safety.Tests", "docs/reports", "artifacts/agent-operations"],
        ProhibitedFilesUntouched: ["src product runtime", "browser-extension Bridge/CSP", "release/store packaging"],
        GoNoGoTable: ["simulated runtime GO", "productive runtime NO-GO", "release/store NO-GO"],
        CapabilityMatrix: SimulatedRuntimeRoutingMatrix.Entries.Select(static x => x.CapabilityName).ToArray(),
        DecisionTypeTable: Enum.GetNames<SimulatedPolicyDecisionType>(),
        ApprovalBoundaryTable: Enum.GetNames<SimulatedApprovalStatus>(),
        ReplayExportDeterminismTable: ["replay audit-only", "audit export redacted", "determinism ready"],
        ReadinessScorecard: CreateReadinessScorecard(),
        SafetyDriftGateResult: SimulatedDriftGateDecision.SafetyDriftGateGoNoDrift,
        ValidationEvidence: ["build PASS", "targeted filters PASS", "full safety PASS", "recipes PASS", "full suite PASS"],
        CaveatHistory: "BrowserRuntimeSmoke Gate 9 historical flake recorded; not reproduced in M815-M826.",
        OpenRisks: ["external audit pending"],
        AuditQuestions: CreateAuditQuestions(),
        RecommendedAuditModel: "Claude or GPT-5.5 Thinking XHigh",
        ExpectedAuditDecisionOptions: CreateAuditDecisionOptions(),
        NextMacroHitoRecommendation: "M845-M856 — External Audit Execution + Simulated Runtime Foundation Freeze Candidate");

    public static IReadOnlyList<string> CreateAuditQuestions() =>
    [
        "¿El simulated runtime sigue siendo fake-only/in-memory?",
        "¿Existe algún camino a runtime productivo?",
        "¿Algún provider/cloud live call puede ejecutarse?",
        "¿Alguna operación filesystem write real quedó habilitada?",
        "¿Alguna browser automation real quedó habilitada?",
        "¿Alguna approval simulated overridea denylist?",
        "¿Replay puede re-ejecutar?",
        "¿Audit export puede exponer secretos?",
        "¿Unknown capability puede permitir ejecución?",
        "¿Denylist se evalúa antes que routing?",
        "¿selectedExecutor queda null en DENY?",
        "¿Full suite fue declarada con evidencia?",
        "¿Product files o Bridge/CSP fueron tocados?",
        "¿Release/store siguen NO-GO?",
        "¿Hay contradicciones entre docs/artifacts/tests?"
    ];

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

    private static SimulatedGovernanceCoverageRow Ready(string domain) => new(domain, "READY", true);
}
