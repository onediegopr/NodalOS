using OneBrain.Core.Contracts;
using OneBrain.Core.Models;
using OneBrain.Core.Selectors;

namespace OneBrain.Core.Execution;

public sealed record PatternReadRequest(
    string ActionKind,
    string TargetRef,
    string ExpectedTargetName,
    string? ProcessName,
    string? WindowTitleContains,
    SelectorDefinition Selector,
    ElementIdentity ExpectedIdentity,
    IntPtr? RootHwnd = null,
    string ReadMode = "");

public sealed record PatternReadResult(
    bool Success,
    FailureKind? FailureKind,
    IReadOnlyList<string> Reasons,
    string Value = "",
    string PatternUsed = "",
    ElementIdentity? ObservedIdentity = null,
    bool WindowFound = false,
    bool TargetVisible = false,
    bool MutationObserved = false,
    IReadOnlyList<string>? Signals = null,
    bool InvokeTimeIdentityChecked = false,
    string InvokeTimeIdentityVerdict = "",
    string InvokeTimeIdentityReason = "",
    string ExpectedIdentityDigest = "",
    string ObservedIdentityDigest = "");

public interface IUiaReadExecutor
{
    PatternReadResult Read(PatternReadRequest request);
}
