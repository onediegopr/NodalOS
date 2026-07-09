using OneBrain.Core.Approval;

namespace OneBrain.Safety.Tests;

// D2 design/test-only mapper. It translates existing hard-block concepts into D1 candidates and is not runtime authority.
internal enum NodalOsCommonBoundarySourceConcept
{
    PublicProductHardBlock,
    ProductionRouteHardBlock,
    LatestPointerDenied,
    ReadPrecedenceDenied,
    ProductAuthorityDenied,
    CommandExecutionDenied,
    ReleaseCommercialDenied,
    ProductLedgerLocalDesignOnlySafetyBoundary,
    RunClaimCoherenceNoRuntimeOverclaim,
    StaticGuardCatalogHardBlock,
    UnsupportedSourceConcept,
    UnknownAmbiguousState
}

internal sealed record NodalOsCommonBoundaryMappingRequest(
    NodalOsCommonBoundarySourceConcept SourceConcept,
    bool ExistingHardBlockAuthoritative = true);

internal sealed record NodalOsCommonBoundaryMappingResult(
    NodalOsCommonBoundarySourceConcept SourceConcept,
    IReadOnlyList<NodalOsCommonBoundaryCapability> MappedCapabilities,
    NodalOsCommonSafetyEnvelope Envelope,
    bool ExistingHardBlockAuthoritative,
    bool UnsupportedSourceConcept,
    bool OverrideAllowed)
{
    public bool IsFailClosed =>
        UnsupportedSourceConcept
        || Envelope.Decision == NodalOsCommonSafetyDecision.Rejected
        || MappedCapabilities.All(Envelope.Claims.IsBlocked);
}

internal static class NodalOsCommonBoundaryMappingDesignOnlyAdapter
{
    public static NodalOsCommonBoundaryMappingResult Map(NodalOsCommonBoundaryMappingRequest? request)
    {
        if (request is null || !request.ExistingHardBlockAuthoritative)
        {
            return FailClosed(
                request?.SourceConcept ?? NodalOsCommonBoundarySourceConcept.UnknownAmbiguousState,
                Unsupported: true,
                ExistingHardBlockAuthoritative: request?.ExistingHardBlockAuthoritative ?? false);
        }

        return request.SourceConcept switch
        {
            NodalOsCommonBoundarySourceConcept.PublicProductHardBlock => Blocked(
                request,
                NodalOsCommonBoundaryCapability.PublicProductExposure),
            NodalOsCommonBoundarySourceConcept.ProductionRouteHardBlock => Blocked(
                request,
                NodalOsCommonBoundaryCapability.ProductionRoute),
            NodalOsCommonBoundarySourceConcept.LatestPointerDenied => Blocked(
                request,
                NodalOsCommonBoundaryCapability.LatestPointer),
            NodalOsCommonBoundarySourceConcept.ReadPrecedenceDenied => Blocked(
                request,
                NodalOsCommonBoundaryCapability.ReadPrecedence),
            NodalOsCommonBoundarySourceConcept.ProductAuthorityDenied => Blocked(
                request,
                NodalOsCommonBoundaryCapability.ProductAuthority),
            NodalOsCommonBoundarySourceConcept.CommandExecutionDenied => Blocked(
                request,
                NodalOsCommonBoundaryCapability.CommandExecution,
                NodalOsCommonBoundaryCapability.ShellSubprocess),
            NodalOsCommonBoundarySourceConcept.ReleaseCommercialDenied => Blocked(
                request,
                NodalOsCommonBoundaryCapability.ReleaseCommercialReadiness),
            NodalOsCommonBoundarySourceConcept.ProductLedgerLocalDesignOnlySafetyBoundary => Blocked(
                request,
                NodalOsCommonBoundaryCapability.PublicProductExposure,
                NodalOsCommonBoundaryCapability.ProductionRoute,
                NodalOsCommonBoundaryCapability.ProviderCloudNetwork,
                NodalOsCommonBoundaryCapability.DatabaseMigration,
                NodalOsCommonBoundaryCapability.KmsWormExternalTrust,
                NodalOsCommonBoundaryCapability.ReleaseCommercialReadiness),
            NodalOsCommonBoundarySourceConcept.RunClaimCoherenceNoRuntimeOverclaim => Blocked(
                request,
                NodalOsCommonBoundaryCapability.PilotRunCoupling),
            NodalOsCommonBoundarySourceConcept.StaticGuardCatalogHardBlock => Blocked(
                request,
                NodalOsCommonBoundaryCapability.PublicProductExposure,
                NodalOsCommonBoundaryCapability.ProductionRoute,
                NodalOsCommonBoundaryCapability.LatestPointer,
                NodalOsCommonBoundaryCapability.ReadPrecedence,
                NodalOsCommonBoundaryCapability.ProductAuthority,
                NodalOsCommonBoundaryCapability.CommandExecution,
                NodalOsCommonBoundaryCapability.ShellSubprocess,
                NodalOsCommonBoundaryCapability.ProviderCloudNetwork,
                NodalOsCommonBoundaryCapability.DatabaseMigration,
                NodalOsCommonBoundaryCapability.KmsWormExternalTrust,
                NodalOsCommonBoundaryCapability.ReleaseCommercialReadiness),
            _ => FailClosed(request.SourceConcept, Unsupported: true, ExistingHardBlockAuthoritative: true)
        };
    }

    private static NodalOsCommonBoundaryMappingResult Blocked(
        NodalOsCommonBoundaryMappingRequest request,
        params NodalOsCommonBoundaryCapability[] capabilities) =>
        new(
            SourceConcept: request.SourceConcept,
            MappedCapabilities: capabilities,
            Envelope: NodalOsCommonSafetyEnvelope.D1DescriptiveOnly(
                NodalOsCommonWriterMode.Disabled,
                NodalOsCommonEvidenceRole.Auxiliary),
            ExistingHardBlockAuthoritative: true,
            UnsupportedSourceConcept: false,
            OverrideAllowed: false);

    private static NodalOsCommonBoundaryMappingResult FailClosed(
        NodalOsCommonBoundarySourceConcept sourceConcept,
        bool Unsupported,
        bool ExistingHardBlockAuthoritative) =>
        new(
            SourceConcept: sourceConcept,
            MappedCapabilities: Enum.GetValues<NodalOsCommonBoundaryCapability>(),
            Envelope: new NodalOsCommonSafetyEnvelope(
                Decision: NodalOsCommonSafetyDecision.Rejected,
                Claims: NodalOsCommonBoundaryClaims.CurrentLocalDesignOnlyNoGo(),
                WriterMode: NodalOsCommonWriterMode.Disabled,
                EvidenceRole: NodalOsCommonEvidenceRole.Auxiliary,
                EvidenceRefs:
                [
                    "NODAL_OS_BLOCK_D2_MAPPING_ADAPTERS_EQUIVALENCE_EXPANSION_TEST_ONLY",
                    "unsupported-or-ambiguous-common-contract-mapping"
                ],
                RuntimeWired: false,
                ServiceRegistered: false,
                RouteRegistered: false,
                CommandHandlerRegistered: false),
            ExistingHardBlockAuthoritative: ExistingHardBlockAuthoritative,
            UnsupportedSourceConcept: Unsupported,
            OverrideAllowed: false);

    public static NodalOsCommonBoundaryClaimsCandidate.Claim ToCandidateClaim(
        NodalOsCommonBoundaryCapability capability) =>
        capability switch
        {
            NodalOsCommonBoundaryCapability.PublicProductExposure => NodalOsCommonBoundaryClaimsCandidate.Claim.PublicProductBlocked,
            NodalOsCommonBoundaryCapability.ProductionRoute => NodalOsCommonBoundaryClaimsCandidate.Claim.ProductionRouteBlocked,
            NodalOsCommonBoundaryCapability.LatestPointer => NodalOsCommonBoundaryClaimsCandidate.Claim.LatestPointerDisabled,
            NodalOsCommonBoundaryCapability.ReadPrecedence => NodalOsCommonBoundaryClaimsCandidate.Claim.ReadPrecedenceDisabled,
            NodalOsCommonBoundaryCapability.ProductAuthority => NodalOsCommonBoundaryClaimsCandidate.Claim.ProductAuthorityBlocked,
            NodalOsCommonBoundaryCapability.CommandExecution => NodalOsCommonBoundaryClaimsCandidate.Claim.CommandExecutionDenied,
            NodalOsCommonBoundaryCapability.ShellSubprocess => NodalOsCommonBoundaryClaimsCandidate.Claim.ShellSubprocessDenied,
            NodalOsCommonBoundaryCapability.ProviderCloudNetwork => NodalOsCommonBoundaryClaimsCandidate.Claim.ProviderCloudNetworkNotClaimed,
            NodalOsCommonBoundaryCapability.DatabaseMigration => NodalOsCommonBoundaryClaimsCandidate.Claim.DatabaseMigrationNotClaimed,
            NodalOsCommonBoundaryCapability.KmsWormExternalTrust => NodalOsCommonBoundaryClaimsCandidate.Claim.ExternalTrustNotClaimed,
            NodalOsCommonBoundaryCapability.ReleaseCommercialReadiness => NodalOsCommonBoundaryClaimsCandidate.Claim.ReleaseCommercialNoGo,
            NodalOsCommonBoundaryCapability.PilotRunCoupling => NodalOsCommonBoundaryClaimsCandidate.Claim.RuntimeProductEnablementNoGo,
            NodalOsCommonBoundaryCapability.CiEnforcement => NodalOsCommonBoundaryClaimsCandidate.Claim.CiEnforcementNotClaimed,
            _ => (NodalOsCommonBoundaryClaimsCandidate.Claim)999
        };
}
