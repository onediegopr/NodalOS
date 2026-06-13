using OneBrain.Core.Contracts;
using OneBrain.Core.Models;
using OneBrain.Core.Selectors;

namespace OneBrain.Core.Execution;

public sealed record PatternExecutionRequest(
    string ActionKind,
    string TargetRef,
    string ExpectedTargetName,
    string? ProcessName,
    string? WindowTitleContains,
    SelectorDefinition Selector,
    ElementIdentity ExpectedIdentity);

public sealed record PatternExecutionResult(
    bool Success,
    FailureKind? FailureKind,
    IReadOnlyList<string> Reasons,
    ElementIdentity? ObservedIdentity = null,
    bool WindowFound = false,
    bool TargetVisible = false,
    string TargetName = "",
    int ObservedActions = 0,
    IReadOnlyList<string>? Signals = null);

public interface IUiaPatternExecutor
{
    PatternExecutionResult Invoke(PatternExecutionRequest request);
}
