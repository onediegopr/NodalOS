namespace OneBrain.BrowserExecutor.Contracts;

// M301-M303 — Internal controlled real image fixture provenance.
// These contracts gate non-sensitive QA images before any local OCR pipeline probe.

public enum NodalOsInternalControlledRealImageSourceCategory
{
    SyntheticGenerated,
    InternalControlledRealImage,
    InternalNonSensitiveFixture,
    RejectedSensitive,
    RejectedUnknownProvenance
}

public enum NodalOsInternalControlledRealImageFixtureDecision
{
    AcceptedForOcrPipeline,
    RejectedSensitive,
    RejectedUnknownProvenance,
    RejectedScreenCapture,
    RejectedDocumentFixture,
    RejectedCustomerData,
    RejectedFinancialData,
    RejectedPersonData
}

public sealed record NodalOsInternalControlledRealImageFixtureProvenance(
    string FixtureId,
    string FileName,
    NodalOsInternalControlledRealImageSourceCategory SourceCategory,
    bool CreatedByInternalQa,
    bool ContainsRealPersonData,
    bool ContainsCustomerData,
    bool ContainsFinancialData,
    bool ContainsDocumentData,
    bool ContainsScreenCapture,
    bool Sensitive,
    string ExpectedText,
    string Reason);

public sealed record NodalOsInternalControlledRealImageFixtureProvenanceResult(
    string FixtureId,
    NodalOsInternalControlledRealImageFixtureDecision Decision,
    bool AllowedForOcrPipeline,
    bool NoSensitive,
    bool NoScreenCapture,
    bool NoDocumentData,
    bool NoCustomerData,
    bool NoFinancialData,
    bool NoPersonData,
    string Reason);
