using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.BrowserExecutor.Cdp;

// M301-M303 — fail-closed provenance gate for internal controlled real image OCR fixtures.
public sealed class NodalOsInternalControlledRealImageFixtureProvenanceEvaluator
{
    public NodalOsInternalControlledRealImageFixtureProvenanceResult Evaluate(
        NodalOsInternalControlledRealImageFixtureProvenance provenance)
    {
        if (string.IsNullOrWhiteSpace(provenance.FixtureId) ||
            string.IsNullOrWhiteSpace(provenance.FileName) ||
            provenance.SourceCategory == NodalOsInternalControlledRealImageSourceCategory.RejectedUnknownProvenance)
        {
            return Reject(provenance, NodalOsInternalControlledRealImageFixtureDecision.RejectedUnknownProvenance, "unknown or incomplete fixture provenance");
        }

        if (provenance.Sensitive ||
            provenance.SourceCategory == NodalOsInternalControlledRealImageSourceCategory.RejectedSensitive)
        {
            return Reject(provenance, NodalOsInternalControlledRealImageFixtureDecision.RejectedSensitive, "sensitive fixture rejected");
        }

        if (provenance.ContainsScreenCapture)
            return Reject(provenance, NodalOsInternalControlledRealImageFixtureDecision.RejectedScreenCapture, "screen capture fixtures are blocked in M301-M303");

        if (provenance.ContainsDocumentData)
            return Reject(provenance, NodalOsInternalControlledRealImageFixtureDecision.RejectedDocumentFixture, "document fixtures are blocked in M301-M303");

        if (provenance.ContainsCustomerData)
            return Reject(provenance, NodalOsInternalControlledRealImageFixtureDecision.RejectedCustomerData, "customer data fixtures are blocked");

        if (provenance.ContainsFinancialData)
            return Reject(provenance, NodalOsInternalControlledRealImageFixtureDecision.RejectedFinancialData, "financial data fixtures are blocked");

        if (provenance.ContainsRealPersonData)
            return Reject(provenance, NodalOsInternalControlledRealImageFixtureDecision.RejectedPersonData, "real person data fixtures are blocked");

        if (!provenance.CreatedByInternalQa ||
            provenance.SourceCategory is not (NodalOsInternalControlledRealImageSourceCategory.InternalControlledRealImage
                or NodalOsInternalControlledRealImageSourceCategory.InternalNonSensitiveFixture
                or NodalOsInternalControlledRealImageSourceCategory.SyntheticGenerated))
        {
            return Reject(provenance, NodalOsInternalControlledRealImageFixtureDecision.RejectedUnknownProvenance, "fixture is not approved internal QA provenance");
        }

        return new NodalOsInternalControlledRealImageFixtureProvenanceResult(
            provenance.FixtureId,
            NodalOsInternalControlledRealImageFixtureDecision.AcceptedForOcrPipeline,
            AllowedForOcrPipeline: true,
            NoSensitive: true,
            NoScreenCapture: true,
            NoDocumentData: true,
            NoCustomerData: true,
            NoFinancialData: true,
            NoPersonData: true,
            "fixture provenance accepted for internal controlled OCR pipeline");
    }

    private static NodalOsInternalControlledRealImageFixtureProvenanceResult Reject(
        NodalOsInternalControlledRealImageFixtureProvenance provenance,
        NodalOsInternalControlledRealImageFixtureDecision decision,
        string reason) =>
        new(
            provenance.FixtureId,
            decision,
            AllowedForOcrPipeline: false,
            NoSensitive: !provenance.Sensitive,
            NoScreenCapture: !provenance.ContainsScreenCapture,
            NoDocumentData: !provenance.ContainsDocumentData,
            NoCustomerData: !provenance.ContainsCustomerData,
            NoFinancialData: !provenance.ContainsFinancialData,
            NoPersonData: !provenance.ContainsRealPersonData,
            BrowserCredentialRedactor.Redact(reason));
}
