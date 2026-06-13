using OneBrain.Core.Selectors;

namespace OneBrain.Core.Approval;

public sealed record ApprovalBinding(
    string ApprovalDecisionId,
    string ApprovedIdentityDigest,
    SelectorDefinition Selector,
    string ActionKind,
    string Mode,
    string PolicyVersion,
    string EvidenceHash);
