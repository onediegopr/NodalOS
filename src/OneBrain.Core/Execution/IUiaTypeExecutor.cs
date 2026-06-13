using OneBrain.Core.Contracts;
using OneBrain.Core.Models;
using OneBrain.Core.Selectors;

namespace OneBrain.Core.Execution;

public sealed record TypeExecutionRequest(
    string ActionKind,
    string TargetRef,
    string ExpectedTargetName,
    string? ProcessName,
    string? WindowTitleContains,
    SelectorDefinition Selector,
    ElementIdentity ExpectedIdentity,
    string ApprovedText,
    string ApprovedTextDigest,
    IntPtr? RootHwnd = null,
    TimeSpan? Timeout = null);

public sealed record TypeExecutionResult(
    bool Success,
    FailureKind? FailureKind,
    IReadOnlyList<string> Reasons,
    string ValueBefore = "",
    string ValueAfter = "",
    string ApprovedTextDigest = "",
    string PatternUsed = "",
    ElementIdentity? ObservedIdentity = null,
    string IdentityVerdict = "",
    bool InvokeTimeIdentityChecked = false,
    string InvokeTimeIdentityVerdict = "",
    string InvokeTimeIdentityReason = "",
    string ExpectedIdentityDigest = "",
    string ObservedIdentityDigest = "",
    bool MutationObserved = false,
    bool SurfaceAllowed = false,
    string SurfaceReason = "",
    bool OwnershipChecked = false,
    bool OwnershipAllowed = false,
    bool WindowFound = false,
    bool TargetVisible = false,
    IReadOnlyList<string>? Signals = null);

public interface IUiaTypeExecutor
{
    TypeExecutionResult Type(TypeExecutionRequest request);
}
