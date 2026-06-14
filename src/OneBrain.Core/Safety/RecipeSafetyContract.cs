using OneBrain.Core.Approval;
using OneBrain.Core.Contracts;
using OneBrain.Core.Models;
using OneBrain.Core.Selectors;

namespace OneBrain.Core.Execution;

public sealed record ExecutionWindowConstraints(
    bool LocalPilotOnly,
    bool ExternalNavigationBlocked);

public sealed record RecipeSafetyContract(
    int SchemaVersion,
    string ContractId,
    string ActionKind,
    ElementIdentity? ExpectedIdentity,
    SelectorDefinition? Selector,
    ExecutionWindowConstraints? WindowConstraints,
    bool Reversible,
    int MaxActions,
    ActionCeiling ActionCeiling,
    Provenance Provenance,
    TrustLevel TrustLevel,
    ApprovalBinding? ApprovalRef,
    string ApprovedValueDigest = "",
    string ApprovedInputBindingHash = "",
    string ApprovedInputBindingVersion = "",
    string ApprovedInputDigestAlgorithm = "");
