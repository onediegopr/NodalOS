using OneBrain.Core.Contracts;
using OneBrain.Core.Models;

namespace OneBrain.Core.Approval;

public sealed record ApprovalBindingResult(
    bool Success,
    FailureKind? FailureKind,
    string BlockReason,
    string MatchVerdict,
    string? ObservedIdentityDigest,
    ElementIdentity? ObservedIdentity,
    IReadOnlyList<string> Reasons);
