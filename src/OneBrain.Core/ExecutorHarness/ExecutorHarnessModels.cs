using OneBrain.Core.Approval;
using OneBrain.Core.History;

namespace OneBrain.Core.ExecutorHarness;

public static class ExecutorHarnessStatuses
{
    public const string Succeeded = "succeeded";
    public const string Blocked = "blocked";
    public const string Failed = "failed";
}

public sealed record ExecutorHarnessTarget(
    string HarnessId,
    string Title,
    string Description,
    string AppProfileId,
    string WindowTitleContains,
    string TargetRef,
    string ExpectedTargetName,
    string ActionKind,
    bool ControlledSurface,
    bool IsBenign,
    bool HasSafeExecutor,
    IReadOnlyList<string> Notes);

public sealed record ExecutorHarnessClickCommand(
    string HarnessId,
    string WindowTitleContains,
    string TargetRef,
    string ExpectedTargetName,
    string ActionKind);

public sealed record ExecutorHarnessWindowConstraints(
    string TitleContains,
    bool LocalPilotOnly,
    bool ExternalNavigationBlocked);

public sealed record ExecutorHarnessTargetConstraints(
    string TargetRef,
    string ExpectedTargetName,
    bool AllowOnlyExactBenignHarnessTarget,
    bool UserConfigurableTargetAllowed);

public sealed record ExecutorHarnessApprovalState(
    string ApprovalRequestId,
    string? ApprovalDecisionId,
    bool RequiresApproval,
    bool Approved,
    bool ExecutionAllowed,
    bool FailClosed);

public sealed record ExecutorHarnessPreActionState(
    bool DryRunOnly,
    bool ExecutorWillRun,
    IReadOnlyList<string> Checks);

public sealed record ExecutorHarnessPostActionExpectation(
    bool WindowMustRemainVisible,
    bool TargetMustRemainVisible,
    string ExpectedTargetName,
    int ExpectedClickCount,
    IReadOnlyList<string> RequiredSignals);

public sealed record ExecutorHarnessInteractionContract(
    string ContractId,
    string CreatedAtUtc,
    string HarnessId,
    string AppProfileId,
    ExecutorHarnessWindowConstraints WindowConstraints,
    ExecutorHarnessTargetConstraints TargetConstraints,
    ExecutorHarnessTargetResolution ResolvedTarget,
    string ActionKind,
    ExecutorHarnessApprovalState ApprovalState,
    ExecutorHarnessSafetyMatrixEvaluation SafetyMatrix,
    ExecutorHarnessPreActionState PreActionState,
    ExecutorHarnessPostActionExpectation PostActionExpectation,
    string LogicalEvidencePath);

public sealed record ExecutorHarnessDryRunExplanation(
    ExecutorHarnessInteractionContract Contract,
    bool WouldExecute,
    string Status,
    string Summary,
    string Element,
    string SelectionReason,
    IReadOnlyList<string> SafetyRules,
    IReadOnlyList<string> BlockingConditions,
    IReadOnlyList<string> Notes);

public sealed record ExecutorHarnessTargetResolution(
    bool Success,
    string Status,
    string Message,
    string HarnessId,
    string AppProfileId,
    string WindowTitleContains,
    string TargetRef,
    string ExpectedTargetName,
    bool ControlledSurface,
    bool LocalOnly,
    IReadOnlyList<string> Signals);

public sealed record ExecutorHarnessPostActionState(
    bool WindowFound,
    bool TargetVisible,
    string TargetName,
    int ObservedClicks,
    bool ClickCountVerified,
    IReadOnlyList<string> Signals);

public sealed record ExecutorHarnessExecutorResult(
    bool Success,
    string Message,
    bool TargetFound,
    int Clicks,
    IReadOnlyList<string> Signals,
    ExecutorHarnessTargetResolution? TargetResolution = null,
    ExecutorHarnessPostActionState? PostActionState = null);

public interface IExecutorHarnessClickExecutor
{
    ExecutorHarnessExecutorResult Click(ExecutorHarnessClickCommand command);
}

public sealed record ExecutorHarnessPostActionVerification(
    bool Success,
    string Status,
    string Message,
    bool TargetFound,
    bool ClickObserved,
    IReadOnlyList<string> Signals);

public sealed record ExecutorHarnessRunResult(
    bool Success,
    string Status,
    string Message,
    ExecutorHarnessTarget Target,
    ApprovalRequest ApprovalRequest,
    ApprovalDecision? ApprovalDecision,
    ExecutorHarnessPostActionVerification Verification,
    RunHistoryRecord RunHistory,
    IReadOnlyList<string> Evidence,
    IReadOnlyList<string> ArtifactPaths,
    ExecutorHarnessSafetyMatrixEvaluation? SafetyMatrix = null,
    ExecutorHarnessTargetResolution? TargetResolution = null);

public sealed record ExecutorHarnessEvidenceRecord(
    string EvidenceId,
    string CreatedAtUtc,
    string HarnessId,
    string Status,
    string Message,
    string ApprovalRequestId,
    string? ApprovalDecisionId,
    ExecutorHarnessPostActionVerification Verification,
    RunSafetyCounters SafetyCounters,
    IReadOnlyList<string> Notes,
    ExecutorHarnessInteractionContract? InteractionContract = null);

public sealed record ExecutorHarnessArtifactWriteResult
{
    public bool Success { get; init; }
    public string Path { get; init; } = "";
    public string RelativePath { get; init; } = "";
    public string Error { get; init; } = "";
}

public sealed record ExecutorHarnessSafetyMatrixEvaluation(
    bool Allowed,
    string Status,
    IReadOnlyList<string> Passed,
    IReadOnlyList<string> Blocked,
    IReadOnlyList<string> RequiresApproval,
    IReadOnlyList<string> Notes);

public sealed record ExecutorHarnessEvidenceReplay(
    bool Success,
    string Status,
    string Message,
    string RelativePath,
    ExecutorHarnessEvidenceRecord? Evidence,
    IReadOnlyList<string> Notes);
