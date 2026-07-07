namespace OneBrain.Safety.Tests;

// D1 design/test-only candidate. This is not production authority and must not be wired into runtime.
internal enum NodalOsCommonBoundaryCapability
{
    PublicProductExposure,
    ProductionRoute,
    LatestPointer,
    ReadPrecedence,
    ProductAuthority,
    CommandExecution,
    ShellSubprocess,
    ProviderCloudNetwork,
    DatabaseMigration,
    KmsWormExternalTrust,
    ReleaseCommercialReadiness,
    PilotRunCoupling,
    CiEnforcement
}

internal enum NodalOsCommonCapabilityState
{
    Blocked,
    AuthorizedByFutureGo
}

internal enum NodalOsCommonEvidenceBoundary
{
    DesignOnly,
    TestOnly,
    LocalInternalOnly
}

internal enum NodalOsCommonSafetyDecision
{
    Rejected,
    DescriptiveOnlyNoRuntimeAuthority
}

internal enum NodalOsCommonWriterMode
{
    ReadOnly,
    CreateOnly,
    AppendOnly,
    VersionedCreateOnly,
    Disabled
}

internal enum NodalOsCommonEvidenceRole
{
    Snapshot,
    Manifest,
    ReaderCandidate,
    Auxiliary,
    HandoffDraft,
    WorkspaceDraft,
    LedgerEntry,
    Checkpoint
}

internal sealed record NodalOsCommonBoundaryClaim(
    NodalOsCommonBoundaryCapability Capability,
    NodalOsCommonCapabilityState State,
    string Reason);

internal sealed record NodalOsCommonBoundaryClaims(
    IReadOnlyList<NodalOsCommonBoundaryClaim> Claims,
    NodalOsCommonEvidenceBoundary EvidenceBoundary,
    bool RuntimeActivationAllowed,
    bool ProductAuthorityAllowed,
    bool CiEnforcementAllowed)
{
    public static NodalOsCommonBoundaryClaims CurrentLocalDesignOnlyNoGo() =>
        new(
            Claims:
            [
                Blocked(NodalOsCommonBoundaryCapability.PublicProductExposure, "public/product remains blocked"),
                Blocked(NodalOsCommonBoundaryCapability.ProductionRoute, "Production route remains blocked"),
                Blocked(NodalOsCommonBoundaryCapability.LatestPointer, "latest pointer remains blocked"),
                Blocked(NodalOsCommonBoundaryCapability.ReadPrecedence, "read precedence remains blocked"),
                Blocked(NodalOsCommonBoundaryCapability.ProductAuthority, "product authority remains blocked"),
                Blocked(NodalOsCommonBoundaryCapability.CommandExecution, "command execution remains blocked"),
                Blocked(NodalOsCommonBoundaryCapability.ShellSubprocess, "shell/subprocess remains blocked"),
                Blocked(NodalOsCommonBoundaryCapability.ProviderCloudNetwork, "provider/cloud/network remains blocked"),
                Blocked(NodalOsCommonBoundaryCapability.DatabaseMigration, "DB/migration remains blocked"),
                Blocked(NodalOsCommonBoundaryCapability.KmsWormExternalTrust, "KMS/WORM/external trust remains blocked"),
                Blocked(NodalOsCommonBoundaryCapability.ReleaseCommercialReadiness, "release/commercial remains NO-GO"),
                Blocked(NodalOsCommonBoundaryCapability.PilotRunCoupling, "Product Ledger remains decoupled from Pilot /run"),
                Blocked(NodalOsCommonBoundaryCapability.CiEnforcement, "Tier 1 remains manual/discovery-only")
            ],
            EvidenceBoundary: NodalOsCommonEvidenceBoundary.DesignOnly,
            RuntimeActivationAllowed: false,
            ProductAuthorityAllowed: false,
            CiEnforcementAllowed: false);

    public bool IsBlocked(NodalOsCommonBoundaryCapability capability) =>
        Claims.Any(claim => claim.Capability == capability && claim.State == NodalOsCommonCapabilityState.Blocked);

    public bool AllowsAnyRuntimeOrProductAuthority() =>
        RuntimeActivationAllowed
        || ProductAuthorityAllowed
        || Claims.Any(claim => claim.State == NodalOsCommonCapabilityState.AuthorizedByFutureGo);

    private static NodalOsCommonBoundaryClaim Blocked(
        NodalOsCommonBoundaryCapability capability,
        string reason) =>
        new(capability, NodalOsCommonCapabilityState.Blocked, reason);
}

internal sealed record NodalOsCommonSafetyEnvelope(
    NodalOsCommonSafetyDecision Decision,
    NodalOsCommonBoundaryClaims Claims,
    NodalOsCommonWriterMode WriterMode,
    NodalOsCommonEvidenceRole EvidenceRole,
    IReadOnlyList<string> EvidenceRefs,
    bool RuntimeWired,
    bool ServiceRegistered,
    bool RouteRegistered,
    bool CommandHandlerRegistered)
{
    public static NodalOsCommonSafetyEnvelope D1DescriptiveOnly(
        NodalOsCommonWriterMode writerMode,
        NodalOsCommonEvidenceRole evidenceRole) =>
        new(
            Decision: NodalOsCommonSafetyDecision.DescriptiveOnlyNoRuntimeAuthority,
            Claims: NodalOsCommonBoundaryClaims.CurrentLocalDesignOnlyNoGo(),
            WriterMode: writerMode,
            EvidenceRole: evidenceRole,
            EvidenceRefs:
            [
                "docs/architecture/nodal-os-model-contract-merge-design.md",
                "NODAL_OS_BLOCK_D1_COMMON_CONTRACTS_PARALLEL_DESIGN_TEST_ONLY"
            ],
            RuntimeWired: false,
            ServiceRegistered: false,
            RouteRegistered: false,
            CommandHandlerRegistered: false);

    public bool HasRuntimeOrProductSurface() =>
        RuntimeWired || ServiceRegistered || RouteRegistered || CommandHandlerRegistered || Claims.AllowsAnyRuntimeOrProductAuthority();
}
