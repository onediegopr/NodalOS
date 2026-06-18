using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.BrowserExecutor.Cdp;

// M304-M306 — fail-closed provenance gate for bounded internal screen-region OCR fixtures.
public sealed class NodalOsInternalControlledScreenRegionProvenanceEvaluator
{
    public NodalOsInternalControlledScreenRegionFixtureProvenanceResult Evaluate(
        NodalOsInternalControlledScreenRegionFixtureProvenance provenance)
    {
        if (string.IsNullOrWhiteSpace(provenance.FixtureId) ||
            string.IsNullOrWhiteSpace(provenance.WindowTitleOrSource) ||
            string.IsNullOrWhiteSpace(provenance.ProcessOrSource) ||
            provenance.SourceCategory == NodalOsInternalControlledScreenRegionSourceCategory.RejectedUnknownWindow)
        {
            return Reject(provenance, NodalOsInternalControlledScreenRegionFixtureDecision.RejectedUnknownWindow, "unknown window or incomplete screen-region provenance");
        }

        if (provenance.ContainsFullScreen ||
            provenance.SourceCategory == NodalOsInternalControlledScreenRegionSourceCategory.RejectedFullScreen)
        {
            return Reject(provenance, NodalOsInternalControlledScreenRegionFixtureDecision.RejectedFullScreen, "full-screen capture is blocked");
        }

        if (provenance.RegionBounds.Width <= 0 ||
            provenance.RegionBounds.Height <= 0 ||
            provenance.WindowBounds.Width <= 0 ||
            provenance.WindowBounds.Height <= 0 ||
            provenance.SourceCategory == NodalOsInternalControlledScreenRegionSourceCategory.RejectedUnboundedRegion)
        {
            return Reject(provenance, NodalOsInternalControlledScreenRegionFixtureDecision.RejectedUnboundedRegion, "screen-region bounds are empty or invalid");
        }

        if (provenance.RegionBounds.X < 0 ||
            provenance.RegionBounds.Y < 0 ||
            provenance.RegionBounds.X + provenance.RegionBounds.Width > provenance.WindowBounds.Width ||
            provenance.RegionBounds.Y + provenance.RegionBounds.Height > provenance.WindowBounds.Height)
        {
            return Reject(provenance, NodalOsInternalControlledScreenRegionFixtureDecision.RejectedRegionOutsideWindow, "screen-region bounds are outside the QA window");
        }

        if (provenance.RegionBounds.Width == provenance.WindowBounds.Width &&
            provenance.RegionBounds.Height == provenance.WindowBounds.Height &&
            provenance.BoundsSource != NodalOsInternalControlledScreenRegionBoundsSource.GeneratedQaFixture)
        {
            return Reject(provenance, NodalOsInternalControlledScreenRegionFixtureDecision.RejectedRegionTooLarge, "region covers the full window without generated QA fixture evidence");
        }

        if (provenance.Sensitive ||
            provenance.SourceCategory == NodalOsInternalControlledScreenRegionSourceCategory.RejectedSensitiveWindow)
        {
            return Reject(provenance, NodalOsInternalControlledScreenRegionFixtureDecision.RejectedSensitiveWindow, "sensitive screen-region rejected");
        }

        if (provenance.ContainsDocumentData ||
            provenance.SourceCategory == NodalOsInternalControlledScreenRegionSourceCategory.RejectedDocumentRegion)
        {
            return Reject(provenance, NodalOsInternalControlledScreenRegionFixtureDecision.RejectedDocumentRegion, "document regions are blocked in M304-M306");
        }

        if (provenance.ContainsCredentialOrPasswordData)
            return Reject(provenance, NodalOsInternalControlledScreenRegionFixtureDecision.RejectedCredentialData, "credential/password regions are blocked");

        if (provenance.ContainsCustomerData)
            return Reject(provenance, NodalOsInternalControlledScreenRegionFixtureDecision.RejectedCustomerData, "customer data regions are blocked");

        if (provenance.ContainsFinancialData)
            return Reject(provenance, NodalOsInternalControlledScreenRegionFixtureDecision.RejectedFinancialData, "financial data regions are blocked");

        if (provenance.ContainsRealPersonData)
            return Reject(provenance, NodalOsInternalControlledScreenRegionFixtureDecision.RejectedPersonData, "real person data regions are blocked");

        if (string.IsNullOrWhiteSpace(provenance.ExpectedText))
            return Reject(provenance, NodalOsInternalControlledScreenRegionFixtureDecision.RejectedMissingExpectedText, "expected text is required for this controlled gate");

        if (!provenance.CreatedByInternalQa ||
            provenance.SourceCategory is not (NodalOsInternalControlledScreenRegionSourceCategory.InternalControlledScreenRegion
                or NodalOsInternalControlledScreenRegionSourceCategory.InternalQaWindowRegion))
        {
            return Reject(provenance, NodalOsInternalControlledScreenRegionFixtureDecision.RejectedUnknownWindow, "region is not approved internal QA provenance");
        }

        return new NodalOsInternalControlledScreenRegionFixtureProvenanceResult(
            provenance.FixtureId,
            NodalOsInternalControlledScreenRegionFixtureDecision.AcceptedForOcrPipeline,
            AllowedForOcrPipeline: true,
            NoSensitive: true,
            NoFullScreen: true,
            NoDocumentData: true,
            NoCredentialData: true,
            NoCustomerData: true,
            NoFinancialData: true,
            NoPersonData: true,
            BoundedRegion: true,
            RegionInsideWindow: true,
            "screen-region provenance accepted for internal controlled OCR pipeline");
    }

    private static NodalOsInternalControlledScreenRegionFixtureProvenanceResult Reject(
        NodalOsInternalControlledScreenRegionFixtureProvenance provenance,
        NodalOsInternalControlledScreenRegionFixtureDecision decision,
        string reason) =>
        new(
            provenance.FixtureId,
            decision,
            AllowedForOcrPipeline: false,
            NoSensitive: !provenance.Sensitive,
            NoFullScreen: !provenance.ContainsFullScreen,
            NoDocumentData: !provenance.ContainsDocumentData,
            NoCredentialData: !provenance.ContainsCredentialOrPasswordData,
            NoCustomerData: !provenance.ContainsCustomerData,
            NoFinancialData: !provenance.ContainsFinancialData,
            NoPersonData: !provenance.ContainsRealPersonData,
            BoundedRegion: provenance.RegionBounds.Width > 0 && provenance.RegionBounds.Height > 0,
            RegionInsideWindow: provenance.RegionBounds.X >= 0 &&
                provenance.RegionBounds.Y >= 0 &&
                provenance.WindowBounds.Width > 0 &&
                provenance.WindowBounds.Height > 0 &&
                provenance.RegionBounds.X + provenance.RegionBounds.Width <= provenance.WindowBounds.Width &&
                provenance.RegionBounds.Y + provenance.RegionBounds.Height <= provenance.WindowBounds.Height,
            BrowserCredentialRedactor.Redact(reason));
}
