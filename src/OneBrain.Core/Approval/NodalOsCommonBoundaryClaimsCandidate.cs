namespace OneBrain.Core.Approval;

/// <summary>
/// Parallel-only source candidate for common NODAL OS boundary claims.
/// This type is descriptive and non-authoritative: it is not a service,
/// not runtime-wired, and cannot override existing hard-block decisions.
/// </summary>
public sealed record NodalOsCommonBoundaryClaimsCandidate(
    IReadOnlyDictionary<NodalOsCommonBoundaryClaimsCandidate.Claim, NodalOsCommonBoundaryClaimsCandidate.ClaimState> Claims,
    bool ParallelOnly,
    bool NonAuthoritative,
    bool ExistingHardBlockAuthorityReplaced,
    bool RuntimeWired,
    bool ServiceRegistered,
    bool RouteRegistered,
    bool CommandHandlerRegistered,
    bool RuntimeProductEnablementAllowed,
    bool ProductAuthorityAllowed,
    bool CiEnforcementClaimed,
    bool ReleaseCommercialReady)
{
    public enum Claim
    {
        PublicProductBlocked,
        ProductionRouteBlocked,
        LatestPointerDisabled,
        ReadPrecedenceDisabled,
        ProductAuthorityBlocked,
        CommandExecutionDenied,
        ShellSubprocessDenied,
        ProviderCloudNetworkNotClaimed,
        DatabaseMigrationNotClaimed,
        ExternalTrustNotClaimed,
        ReleaseCommercialNoGo,
        RuntimeProductEnablementNoGo,
        CiEnforcementNotClaimed
    }

    public enum ClaimState
    {
        Blocked,
        Disabled,
        Denied,
        NotClaimed,
        NoGo
    }

    public static NodalOsCommonBoundaryClaimsCandidate DefaultBlocked() =>
        new(
            Claims: new System.Collections.ObjectModel.ReadOnlyDictionary<Claim, ClaimState>(
                new Dictionary<Claim, ClaimState>
                {
                    [Claim.PublicProductBlocked] = ClaimState.Blocked,
                    [Claim.ProductionRouteBlocked] = ClaimState.Blocked,
                    [Claim.LatestPointerDisabled] = ClaimState.Disabled,
                    [Claim.ReadPrecedenceDisabled] = ClaimState.Disabled,
                    [Claim.ProductAuthorityBlocked] = ClaimState.Blocked,
                    [Claim.CommandExecutionDenied] = ClaimState.Denied,
                    [Claim.ShellSubprocessDenied] = ClaimState.Denied,
                    [Claim.ProviderCloudNetworkNotClaimed] = ClaimState.NotClaimed,
                    [Claim.DatabaseMigrationNotClaimed] = ClaimState.NotClaimed,
                    [Claim.ExternalTrustNotClaimed] = ClaimState.NotClaimed,
                    [Claim.ReleaseCommercialNoGo] = ClaimState.NoGo,
                    [Claim.RuntimeProductEnablementNoGo] = ClaimState.NoGo,
                    [Claim.CiEnforcementNotClaimed] = ClaimState.NotClaimed
                }),
            ParallelOnly: true,
            NonAuthoritative: true,
            ExistingHardBlockAuthorityReplaced: false,
            RuntimeWired: false,
            ServiceRegistered: false,
            RouteRegistered: false,
            CommandHandlerRegistered: false,
            RuntimeProductEnablementAllowed: false,
            ProductAuthorityAllowed: false,
            CiEnforcementClaimed: false,
            ReleaseCommercialReady: false);

    public ClaimState StateFor(Claim claim) =>
        IsSupported(claim) && Claims.TryGetValue(claim, out var state)
            ? state
            : ClaimState.Denied;

    public bool IsSupported(Claim claim) =>
        Enum.IsDefined(claim);

    public bool IsFailClosed(Claim claim) =>
        !IsSupported(claim)
        || StateFor(claim) is ClaimState.Blocked
            or ClaimState.Disabled
            or ClaimState.Denied
            or ClaimState.NotClaimed
            or ClaimState.NoGo;

    public bool CanOverrideExistingHardBlock(Claim claim) =>
        false;

    public bool AllowsRuntimeProductOrAuthority() =>
        RuntimeWired
        || ServiceRegistered
        || RouteRegistered
        || CommandHandlerRegistered
        || RuntimeProductEnablementAllowed
        || ProductAuthorityAllowed
        || CiEnforcementClaimed
        || ReleaseCommercialReady
        || !ParallelOnly
        || !NonAuthoritative
        || ExistingHardBlockAuthorityReplaced;
}
