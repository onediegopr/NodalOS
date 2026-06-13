using OneBrain.Core.Selectors;

namespace OneBrain.Core.Approval;

public sealed record ApprovalIdentityMetadata(
    string IdentitySchemaVersion,
    string? ApprovedIdentityDigest,
    SelectorDefinition? ApprovedSelector,
    IdentityStrength IdentityStrength,
    string? IdentitySource,
    bool? ShadowAgreesWithLegacy,
    string? IdentityBindingHash);
