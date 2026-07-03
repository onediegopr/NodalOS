namespace OneBrain.Core.Approval;

public enum CandidatePrepMapEntryKind
{
    FutureCandidateFile,
    FutureTestFile,
    FutureDocFile,
    ExistingReadOnlyPattern,
    ProhibitedForFirstImplementation,
    OutOfScope
}

public enum CandidatePrepRequirementStatus
{
    RequiredBeforeCode,
    BlockedPendingUserGo,
    BlockedPendingExternalAudit,
    FutureOnly
}

public sealed record CandidatePrepModuleMapEntry(
    string Path,
    CandidatePrepMapEntryKind Kind,
    string Purpose,
    string InScopeReason,
    IReadOnlyList<string> MustNotDo,
    IReadOnlyList<string> SideEffectsProhibited,
    IReadOnlyList<string> RequiredTests,
    string GateRequiredBeforeTouching);

public sealed record CandidatePrepGateRequirement(
    string GateId,
    string Title,
    CandidatePrepRequirementStatus Status,
    bool RequiredBeforeImplementation,
    bool SatisfiedNow,
    bool BlocksImplementation);

public sealed record CandidatePrepNegativeTestRequirement(
    string TestId,
    string Assertion,
    CandidatePrepRequirementStatus Status,
    bool RequiredBeforeImplementation,
    bool ImplementedNow);

public sealed record CandidatePrepPositiveTestPlan(
    string TestId,
    string Assertion,
    CandidatePrepRequirementStatus Status,
    bool RequiresRealIo);

public sealed record CandidatePrepFailClosedRule(
    string RuleId,
    string Trigger,
    string Result,
    bool RequiredBeforeImplementation);

public sealed record CandidatePrepNoSideEffectProofPlan(
    IReadOnlyList<string> RequiredCounters,
    IReadOnlyList<string> RequiredDryRunSignals,
    IReadOnlyList<string> ProhibitedSideEffects);

public sealed record CandidatePrepBlockedImplementationPrompt(
    string PromptName,
    string Header,
    IReadOnlyList<string> RequiredBeforeExecution,
    string Status);

public sealed record CandidatePrepPostImplementationAuditPrompt(
    string PromptName,
    IReadOnlyList<string> AuditScope,
    string RequiredBeforeEnablement);

public sealed record CandidatePrepCounts(
    int DurableAuditTrailRealEnabledCount,
    int AppendWriteEnabledCount,
    int RuntimeEnabledCount,
    int ExecutionEnabledCount,
    int MutationEnabledCount,
    int ExportEnabledCount,
    int ServiceRegistrationCount,
    int CommandHandlerCount,
    int ProductActionCount,
    int FilesystemOutputCount,
    int DbMigrationCount,
    int NetworkProviderCallCount,
    int ReleaseCommercialReadyCount)
{
    public bool AllZero =>
        DurableAuditTrailRealEnabledCount == 0
        && AppendWriteEnabledCount == 0
        && RuntimeEnabledCount == 0
        && ExecutionEnabledCount == 0
        && MutationEnabledCount == 0
        && ExportEnabledCount == 0
        && ServiceRegistrationCount == 0
        && CommandHandlerCount == 0
        && ProductActionCount == 0
        && FilesystemOutputCount == 0
        && DbMigrationCount == 0
        && NetworkProviderCallCount == 0
        && ReleaseCommercialReadyCount == 0;
}

public sealed record SelectedCapabilityImplementationCandidatePrepReadOnlyPacket(
    string PacketId,
    string GeneratedAtUtc,
    string CanonicalState,
    string SelectedCapability,
    string CandidateStatus,
    string MaximumDecisionAllowed,
    IReadOnlyList<CandidatePrepModuleMapEntry> ModuleFileCandidateMap,
    IReadOnlyList<CandidatePrepGateRequirement> RequiredGates,
    IReadOnlyList<CandidatePrepNegativeTestRequirement> RequiredNegativeTests,
    IReadOnlyList<CandidatePrepPositiveTestPlan> FuturePositiveTests,
    IReadOnlyList<CandidatePrepFailClosedRule> FailClosedPlan,
    CandidatePrepNoSideEffectProofPlan NoSideEffectProofPlan,
    CandidatePrepBlockedImplementationPrompt BlockedFutureImplementationPrompt,
    CandidatePrepPostImplementationAuditPrompt PostImplementationAuditPrompt,
    CandidatePrepCounts Counts,
    IReadOnlyList<string> EvidenceLinks,
    string HumanOperatorRecommendation)
{
    public bool PassesPrepOnlySafetyProof =>
        CanonicalState == "PAUSED_READ_ONLY_NO_RUNTIME_NO_EXECUTION_NO_MUTATION_NO_PHYSICAL_EXPORT_NO_REDACTION_RUNTIME"
        && SelectedCapability == "DURABLE_AUDIT_TRAIL_APPEND_ONLY_MINIMAL"
        && CandidateStatus == "BLOCKED_PENDING_USER_GO_FOR_IMPLEMENTATION"
        && MaximumDecisionAllowed == "IMPLEMENTATION_CANDIDATE_PREPARED_BUT_BLOCKED_PENDING_USER_GO"
        && ModuleFileCandidateMap.Any(entry => entry.Kind == CandidatePrepMapEntryKind.FutureCandidateFile)
        && ModuleFileCandidateMap.Any(entry => entry.Kind == CandidatePrepMapEntryKind.FutureTestFile)
        && ModuleFileCandidateMap.Any(entry => entry.Kind == CandidatePrepMapEntryKind.ProhibitedForFirstImplementation)
        && RequiredGates.Count >= 12
        && RequiredGates.All(gate => gate.RequiredBeforeImplementation && !gate.SatisfiedNow && gate.BlocksImplementation)
        && RequiredNegativeTests.Count >= 25
        && RequiredNegativeTests.All(test => test.RequiredBeforeImplementation && !test.ImplementedNow)
        && FuturePositiveTests.All(test => test.Status is CandidatePrepRequirementStatus.BlockedPendingUserGo or CandidatePrepRequirementStatus.FutureOnly)
        && FailClosedPlan.Count >= 8
        && Counts.AllZero
        && BlockedFutureImplementationPrompt.Status == "BLOCKED_NOT_EXECUTABLE"
        && PostImplementationAuditPrompt.RequiredBeforeEnablement == "REQUIRED_BEFORE_ANY_ENABLEMENT"
        && EvidenceLinks.All(link => link.StartsWith("docs/", StringComparison.Ordinal));
}

public static class SelectedCapabilityImplementationCandidatePrepReadOnlyPresenter
{
    public static SelectedCapabilityImplementationCandidatePrepReadOnlyPacket CreateFixture() =>
        new(
            PacketId: "nodal-os.selected-capability.implementation-candidate-prep.read-only.fixture.v1",
            GeneratedAtUtc: "2026-07-03T00:00:00Z",
            CanonicalState: "PAUSED_READ_ONLY_NO_RUNTIME_NO_EXECUTION_NO_MUTATION_NO_PHYSICAL_EXPORT_NO_REDACTION_RUNTIME",
            SelectedCapability: "DURABLE_AUDIT_TRAIL_APPEND_ONLY_MINIMAL",
            CandidateStatus: "BLOCKED_PENDING_USER_GO_FOR_IMPLEMENTATION",
            MaximumDecisionAllowed: "IMPLEMENTATION_CANDIDATE_PREPARED_BUT_BLOCKED_PENDING_USER_GO",
            ModuleFileCandidateMap: ModuleFileCandidateMap(),
            RequiredGates: Gates(),
            RequiredNegativeTests: NegativeTests(),
            FuturePositiveTests: FuturePositiveTests(),
            FailClosedPlan: FailClosedPlan(),
            NoSideEffectProofPlan: NoSideEffectProof(),
            BlockedFutureImplementationPrompt: BlockedPrompt(),
            PostImplementationAuditPrompt: PostImplementationAuditPrompt(),
            Counts: Counts(),
            EvidenceLinks: EvidenceLinks(),
            HumanOperatorRecommendation: "Pause for explicit user GO before implementation. This prep package does not implement durable audit trail real, append/write, storage, runtime, services, handlers or product actions.");

    private static IReadOnlyList<CandidatePrepModuleMapEntry> ModuleFileCandidateMap() =>
    [
        Map(
            "src/OneBrain.Core/Approval/DurableAuditTrailAppendOnlyCandidate.cs",
            CandidatePrepMapEntryKind.FutureCandidateFile,
            "Future minimal candidate boundary for durable audit trail append-only logic.",
            "Only after explicit user GO, external audit GO and scope lock.",
            ["must not append now", "must not persist now", "must not register services", "must not expose command handlers"],
            ["runtime/live", "filesystem product IO outside isolated future scope", "DB/migration", "network/provider"],
            ["no append without user GO", "fail closed on missing audit", "no service registration", "no command handlers"],
            "USER_GO_AND_EXTERNAL_AUDIT_REQUIRED"),
        Map(
            "tests/OneBrain.Safety.Tests/DurableAuditTrailAppendOnlyCandidateSafetyTests.cs",
            CandidatePrepMapEntryKind.FutureTestFile,
            "Future negative tests that must be written before any implementation code.",
            "Safety tests define blocked behavior before a real candidate can exist.",
            ["must not instantiate a live store", "must not write files", "must not use DB"],
            ["append/write", "runtime invocation", "service registration"],
            ["missing user GO blocks", "missing external audit blocks", "scope mismatch blocks"],
            "NEGATIVE_TESTS_FIRST"),
        Map(
            "tests/OneBrain.Recipes.Tests/DurableAuditTrailAppendOnlyCandidateTests.cs",
            CandidatePrepMapEntryKind.FutureTestFile,
            "Future recipe-facing negative tests for candidate boundaries.",
            "Recipes must remain unable to execute, schedule or trigger audit writes.",
            ["must not execute recipes", "must not schedule background work", "must not call action runners"],
            ["recipes execution real", "scheduler", "trigger", "retry loop"],
            ["no recipe execution", "no scheduler", "no product action"],
            "NEGATIVE_TESTS_FIRST"),
        Map(
            "docs/adr/selected-capability-implementation-candidate-prep-read-only.md",
            CandidatePrepMapEntryKind.FutureDocFile,
            "Document the implementation candidate prep status and blocked future implementation prompt.",
            "Docs can describe gates without opening capability.",
            ["must not claim implementation readiness", "must not claim runtime readiness"],
            ["release/commercial claim", "runtime enablement claim"],
            ["overclaim scan", "blocked prompt wording"],
            "DOCS_ONLY_ALLOWED_NOW"),
        Map(
            "src/OneBrain.Core/Approval/FirstRealCapabilityCandidateScopeProposalReadOnly.cs",
            CandidatePrepMapEntryKind.ExistingReadOnlyPattern,
            "Existing selected scope proposal and safety pattern.",
            "This prep packet follows the same deterministic in-memory fixture style.",
            ["must not change selected candidate to enabled", "must not relax negative tests"],
            ["capability activation", "service registration", "command handler"],
            ["selected candidate remains blocked", "counts remain 0"],
            "READ_ONLY_PATTERN_REFERENCE"),
        Map(
            "src/**/ServiceCollection*.cs",
            CandidatePrepMapEntryKind.ProhibitedForFirstImplementation,
            "Service registration remains out of scope for the first implementation candidate.",
            "Registration would activate product/runtime integration.",
            ["must not add AddSingleton/AddScoped/AddTransient", "must not wire runtime"],
            ["service registration", "runtime/live", "product actions"],
            ["service registration count remains 0"],
            "EXPLICIT_USER_GO_AND_SEPARATE_AUDIT_REQUIRED"),
        Map(
            "src/**/CommandHandler*.cs",
            CandidatePrepMapEntryKind.ProhibitedForFirstImplementation,
            "Command handlers remain out of scope for the first implementation candidate.",
            "Handlers could expose product actions before enablement review.",
            ["must not create command handlers", "must not map commands", "must not expose Execute/Run path"],
            ["command handler", "execution", "mutation"],
            ["command handler count remains 0"],
            "EXPLICIT_USER_GO_AND_SEPARATE_AUDIT_REQUIRED"),
        Map(
            "src/**/Migrations/**",
            CandidatePrepMapEntryKind.ProhibitedForFirstImplementation,
            "DB/migration remains out of scope for first candidate.",
            "Storage backend selection needs separate audit.",
            ["must not create migrations", "must not use DbContext", "must not open database"],
            ["DB/migration", "event persistence", "storage"],
            ["DB migration count remains 0"],
            "SEPARATE_STORAGE_AUDIT_REQUIRED")
    ];

    private static CandidatePrepModuleMapEntry Map(
        string path,
        CandidatePrepMapEntryKind kind,
        string purpose,
        string inScopeReason,
        IReadOnlyList<string> mustNotDo,
        IReadOnlyList<string> sideEffectsProhibited,
        IReadOnlyList<string> requiredTests,
        string gate) =>
        new(path, kind, purpose, inScopeReason, mustNotDo, sideEffectsProhibited, requiredTests, gate);

    private static IReadOnlyList<CandidatePrepGateRequirement> Gates() =>
    [
        Gate("user-go", "Explicit user GO for durable audit trail implementation candidate"),
        Gate("repo-guard", "Clean repo guard before implementation"),
        Gate("scope-lock", "Locked selected capability scope"),
        Gate("external-audit-go", "Selected capability external audit GO recorded"),
        Gate("negative-tests-first", "Negative tests written or updated before real code"),
        Gate("no-unresolved-p0-p2", "No unresolved P0/P1/P2"),
        Gate("isolated-boundary", "Isolated audit trail boundary"),
        Gate("no-broad-io", "No broad filesystem or DB access"),
        Gate("no-service-registration", "No service registration unless separately approved"),
        Gate("no-command-handler", "No command handler unless separately approved"),
        Gate("fail-closed", "Fail-closed behavior defined and tested"),
        Gate("post-implementation-audit", "Post-implementation external audit before enablement"),
        Gate("release-no-go", "Release/commercial remains NO-GO")
    ];

    private static CandidatePrepGateRequirement Gate(string id, string title) =>
        new(
            GateId: id,
            Title: title,
            Status: CandidatePrepRequirementStatus.RequiredBeforeCode,
            RequiredBeforeImplementation: true,
            SatisfiedNow: false,
            BlocksImplementation: true);

    private static IReadOnlyList<CandidatePrepNegativeTestRequirement> NegativeTests() =>
    [
        Negative("no-append-without-user-go", "candidate cannot append without explicit user GO"),
        Negative("no-append-without-external-audit", "candidate cannot append without pre-implementation external audit GO"),
        Negative("no-append-without-scope-lock", "candidate cannot append without scope lock"),
        Negative("no-write-outside-isolated-future-audit-path", "candidate cannot write outside isolated future audit path"),
        Negative("no-service-registration", "candidate cannot register services"),
        Negative("no-command-handlers", "candidate cannot expose command handlers"),
        Negative("no-product-actions", "candidate cannot expose product actions"),
        Negative("no-runtime-live", "candidate cannot run as runtime/live"),
        Negative("no-domain-state-mutation", "candidate cannot mutate domain state"),
        Negative("no-physical-export", "candidate cannot export physical files"),
        Negative("no-redaction-runtime", "candidate cannot redact runtime content"),
        Negative("no-retention-deletion-runtime", "candidate cannot retain/delete records"),
        Negative("no-provider-cloud-network", "candidate cannot call provider/cloud/network"),
        Negative("no-browser-cdp", "candidate cannot use browser/CDP"),
        Negative("no-wcu-ocr", "candidate cannot use WCU/OCR"),
        Negative("no-recipes-execution", "candidate cannot execute recipes"),
        Negative("fail-closed-missing-gate", "candidate fails closed when a gate is missing"),
        Negative("fail-closed-missing-audit", "candidate fails closed when audit is missing"),
        Negative("fail-closed-missing-user-go", "candidate fails closed when user GO is missing"),
        Negative("fail-closed-unexpected-target", "candidate fails closed on unexpected target/path"),
        Negative("status-blocked-until-explicit-go", "candidate reports implementation status as blocked until explicit GO"),
        Negative("release-commercial-no-go", "candidate does not change release/commercial status"),
        Negative("runtime-readiness-remains-zero", "candidate does not increase runtime/live readiness above 0 until implemented and audited"),
        Negative("service-registration-count-zero", "candidate does not create service registration count > 0"),
        Negative("command-handler-count-zero", "candidate does not create command handler count > 0"),
        Negative("no-db-migration", "candidate cannot create DB/migration"),
        Negative("no-append-only-store-now", "candidate cannot create append-only store in prep")
    ];

    private static CandidatePrepNegativeTestRequirement Negative(string id, string assertion) =>
        new(
            TestId: id,
            Assertion: assertion,
            Status: CandidatePrepRequirementStatus.RequiredBeforeCode,
            RequiredBeforeImplementation: true,
            ImplementedNow: false);

    private static IReadOnlyList<CandidatePrepPositiveTestPlan> FuturePositiveTests() =>
    [
        Positive("deterministic-in-memory-fixture", "can create deterministic in-memory append candidate fixture", requiresIo: false),
        Positive("validate-request-shape-without-writing", "can validate append request shape without writing", requiresIo: false),
        Positive("reject-invalid-target", "can reject invalid target", requiresIo: false),
        Positive("no-side-effect-preview", "can produce no-side-effect preview", requiresIo: false),
        Positive("audit-event-envelope-preview", "can produce audit event envelope preview", requiresIo: false),
        Positive("blocked-status-before-enablement", "can produce blocked implementation status before enablement", requiresIo: false),
        Positive("test-only-fixture-result", "can record test-only fixture result without product IO", requiresIo: false)
    ];

    private static CandidatePrepPositiveTestPlan Positive(string id, string assertion, bool requiresIo) =>
        new(
            TestId: id,
            Assertion: assertion,
            Status: CandidatePrepRequirementStatus.BlockedPendingUserGo,
            RequiresRealIo: requiresIo);

    private static IReadOnlyList<CandidatePrepFailClosedRule> FailClosedPlan() =>
    [
        FailClosed("missing-user-go", "missing user GO", "blocked"),
        FailClosed("missing-external-audit", "missing external audit", "blocked"),
        FailClosed("missing-scope-lock", "missing scope lock", "blocked"),
        FailClosed("unexpected-path", "unexpected path", "blocked"),
        FailClosed("service-registration-attempted", "service registration attempted", "blocked"),
        FailClosed("command-handler-attempted", "command handler attempted", "blocked"),
        FailClosed("provider-network-call-attempted", "provider/network call attempted", "blocked"),
        FailClosed("product-io-outside-scope", "product IO outside scope attempted", "blocked"),
        FailClosed("release-commercial-claim", "release/commercial claim attempted", "blocked")
    ];

    private static CandidatePrepFailClosedRule FailClosed(string id, string trigger, string result) =>
        new(id, trigger, result, RequiredBeforeImplementation: true);

    private static CandidatePrepNoSideEffectProofPlan NoSideEffectProof() =>
        new(
            RequiredCounters:
            [
                "DurableAuditTrailRealEnabledCount",
                "AppendWriteEnabledCount",
                "RuntimeEnabledCount",
                "ExecutionEnabledCount",
                "MutationEnabledCount",
                "ExportEnabledCount",
                "ServiceRegistrationCount",
                "CommandHandlerCount",
                "ProductActionCount",
                "FilesystemOutputCount",
                "DbMigrationCount",
                "NetworkProviderCallCount",
                "ReleaseCommercialReadyCount"
            ],
            RequiredDryRunSignals:
            [
                "candidate status remains BLOCKED_PENDING_USER_GO_FOR_IMPLEMENTATION",
                "future positive tests remain blocked until explicit GO",
                "future implementation prompt remains BLOCKED_NOT_EXECUTABLE"
            ],
            ProhibitedSideEffects:
            [
                "service registration",
                "command handlers",
                "product actions",
                "filesystem product IO",
                "DB/migration",
                "network/provider",
                "runtime invocation",
                "browser/CDP/WCU/OCR/recipes"
            ]);

    private static CandidatePrepBlockedImplementationPrompt BlockedPrompt() =>
        new(
            PromptName: "NODAL_OS_DURABLE_AUDIT_TRAIL_APPEND_ONLY_MINIMAL_IMPLEMENTATION_CANDIDATE_BLOCKED",
            Header: "BLOCKED - DO NOT EXECUTE WITHOUT USER EXPLICIT GO",
            RequiredBeforeExecution:
            [
                "User explicit GO",
                "Repo guard clean",
                "Scope locked",
                "External audit GO already recorded",
                "Negative tests written or updated first",
                "No unresolved P0/P1/P2",
                "No stash touched",
                "Implementation remains minimal and isolated"
            ],
            Status: "BLOCKED_NOT_EXECUTABLE");

    private static CandidatePrepPostImplementationAuditPrompt PostImplementationAuditPrompt() =>
        new(
            PromptName: "NODAL_OS_DURABLE_AUDIT_TRAIL_POST_IMPLEMENTATION_EXTERNAL_AUDIT_READ_ONLY",
            AuditScope:
            [
                "no broad runtime",
                "no unintended side effects",
                "no service registration unless explicitly scoped",
                "no command handlers unless explicitly scoped",
                "no product UI enablement",
                "no release/commercial readiness",
                "tests pass",
                "no overclaim",
                "capability remains disabled unless enablement gate later approved"
            ],
            RequiredBeforeEnablement: "REQUIRED_BEFORE_ANY_ENABLEMENT");

    private static CandidatePrepCounts Counts() =>
        new(
            DurableAuditTrailRealEnabledCount: 0,
            AppendWriteEnabledCount: 0,
            RuntimeEnabledCount: 0,
            ExecutionEnabledCount: 0,
            MutationEnabledCount: 0,
            ExportEnabledCount: 0,
            ServiceRegistrationCount: 0,
            CommandHandlerCount: 0,
            ProductActionCount: 0,
            FilesystemOutputCount: 0,
            DbMigrationCount: 0,
            NetworkProviderCallCount: 0,
            ReleaseCommercialReadyCount: 0);

    private static IReadOnlyList<string> EvidenceLinks() =>
    [
        "docs/qa/nodal-os-selected-capability-scope-external-audit-read-only/report.md",
        "docs/qa/nodal-os-first-real-capability-candidate-scope-proposal-read-only/report.md",
        "docs/adr/first-real-capability-candidate-scope-proposal-read-only.md",
        "docs/decision-log.md"
    ];
}
