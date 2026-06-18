namespace OneBrain.BrowserExecutor.Contracts;

// M304-M306 — Internal controlled screen-region fixture provenance.
// This gate allows only bounded, non-sensitive QA regions before local OCR probes.

public enum NodalOsInternalControlledScreenRegionSourceCategory
{
    InternalControlledScreenRegion,
    InternalQaWindowRegion,
    RejectedFullScreen,
    RejectedUnknownWindow,
    RejectedSensitiveWindow,
    RejectedDocumentRegion,
    RejectedUnboundedRegion
}

public enum NodalOsInternalControlledScreenRegionBoundsSource
{
    Explicit,
    WindowRelative,
    ControlRelative,
    GeneratedQaFixture
}

public enum NodalOsInternalControlledScreenRegionFixtureDecision
{
    AcceptedForOcrPipeline,
    RejectedFullScreen,
    RejectedUnknownWindow,
    RejectedSensitiveWindow,
    RejectedDocumentRegion,
    RejectedUnboundedRegion,
    RejectedCredentialData,
    RejectedCustomerData,
    RejectedFinancialData,
    RejectedPersonData,
    RejectedMissingExpectedText
}

public sealed record NodalOsScreenRegionBounds(int X, int Y, int Width, int Height);

public sealed record NodalOsInternalControlledScreenRegionFixtureProvenance(
    string FixtureId,
    NodalOsInternalControlledScreenRegionSourceCategory SourceCategory,
    bool CreatedByInternalQa,
    string WindowTitleOrSource,
    string ProcessOrSource,
    NodalOsScreenRegionBounds RegionBounds,
    NodalOsInternalControlledScreenRegionBoundsSource BoundsSource,
    bool ContainsRealPersonData,
    bool ContainsCustomerData,
    bool ContainsFinancialData,
    bool ContainsDocumentData,
    bool ContainsCredentialOrPasswordData,
    bool ContainsFullScreen,
    bool Sensitive,
    string ExpectedText,
    string Reason);

public sealed record NodalOsInternalControlledScreenRegionFixtureProvenanceResult(
    string FixtureId,
    NodalOsInternalControlledScreenRegionFixtureDecision Decision,
    bool AllowedForOcrPipeline,
    bool NoSensitive,
    bool NoFullScreen,
    bool NoDocumentData,
    bool NoCredentialData,
    bool NoCustomerData,
    bool NoFinancialData,
    bool NoPersonData,
    bool BoundedRegion,
    string Reason);
