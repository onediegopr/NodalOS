using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.BrowserExecutor.Cdp;

// M310-M312 — real QA-window capture provenance gate.
public sealed class NodalOsRealQaWindowRegionCaptureProvenanceEvaluator
{
    public NodalOsRealQaWindowRegionCaptureResult Evaluate(NodalOsRealQaWindowRegionCaptureProvenance provenance)
    {
        if (provenance.CaptureMode != NodalOsRealQaWindowRegionCaptureMode.RealQaWindowRegion)
            return Reject(provenance, NodalOsRealQaWindowRegionCaptureDecision.RejectedSimulatedWindow, "simulated or blocked capture is not accepted as real QA window capture");

        if (string.IsNullOrWhiteSpace(provenance.ExpectedWindowTitle) ||
            string.IsNullOrWhiteSpace(provenance.ObservedWindowTitle) ||
            !string.Equals(provenance.ExpectedWindowTitle, provenance.ObservedWindowTitle, StringComparison.Ordinal))
        {
            return Reject(provenance, NodalOsRealQaWindowRegionCaptureDecision.RejectedUnknownWindow, "QA window title could not be verified");
        }

        if (string.IsNullOrWhiteSpace(provenance.ExpectedProcessOrSource) ||
            string.IsNullOrWhiteSpace(provenance.ObservedProcessOrSource) ||
            !string.Equals(provenance.ExpectedProcessOrSource, provenance.ObservedProcessOrSource, StringComparison.Ordinal))
        {
            return Reject(provenance, NodalOsRealQaWindowRegionCaptureDecision.RejectedProcessMismatch, "QA process/source could not be verified");
        }

        if (provenance.ContainsFullScreen)
            return Reject(provenance, NodalOsRealQaWindowRegionCaptureDecision.RejectedFullScreen, "full-screen capture is blocked");

        if (provenance.WindowBounds.Width <= 0 ||
            provenance.WindowBounds.Height <= 0 ||
            provenance.RegionBounds.Width <= 0 ||
            provenance.RegionBounds.Height <= 0)
        {
            return Reject(provenance, NodalOsRealQaWindowRegionCaptureDecision.RejectedInvalidBounds, "window or region bounds are invalid");
        }

        if (provenance.RegionBounds.X < 0 ||
            provenance.RegionBounds.Y < 0 ||
            provenance.RegionBounds.X + provenance.RegionBounds.Width > provenance.WindowBounds.Width ||
            provenance.RegionBounds.Y + provenance.RegionBounds.Height > provenance.WindowBounds.Height)
        {
            return Reject(provenance, NodalOsRealQaWindowRegionCaptureDecision.RejectedRegionOutsideWindow, "region is outside QA window bounds");
        }

        if (!provenance.WindowExists ||
            !provenance.WindowVisible ||
            !provenance.LivenessConfirmed ||
            string.IsNullOrWhiteSpace(provenance.WindowHandleOrSourceId))
        {
            return Reject(provenance, NodalOsRealQaWindowRegionCaptureDecision.RejectedLivenessOrVisibility, "QA window liveness or visibility is not confirmed");
        }

        if (provenance.Sensitive ||
            provenance.ContainsCustomerData ||
            provenance.ContainsFinancialData ||
            provenance.ContainsRealPersonData)
        {
            return Reject(provenance, NodalOsRealQaWindowRegionCaptureDecision.RejectedSensitiveRegion, "sensitive QA window region rejected");
        }

        if (provenance.ContainsDocumentData)
            return Reject(provenance, NodalOsRealQaWindowRegionCaptureDecision.RejectedDocumentRegion, "document regions are blocked in M310-M312");

        if (provenance.ContainsCredentialOrPasswordData)
            return Reject(provenance, NodalOsRealQaWindowRegionCaptureDecision.RejectedCredentialData, "credential/password regions are blocked");

        if (string.IsNullOrWhiteSpace(provenance.ExpectedText))
            return Reject(provenance, NodalOsRealQaWindowRegionCaptureDecision.RejectedMissingExpectedText, "expected text is required for this controlled gate");

        return new NodalOsRealQaWindowRegionCaptureResult(
            provenance.FixtureId,
            NodalOsRealQaWindowRegionCaptureDecision.AcceptedForRealQaWindowCapture,
            AllowedForRealQaWindowCapture: true,
            RealQaWindowRegionUsed: true,
            SimulatedWindowRegionUsed: false,
            WindowLiveAndVisible: true,
            RegionInsideWindow: true,
            NoFullScreen: true,
            NoSensitive: true,
            NoDocumentData: true,
            NoCredentialData: true,
            "real QA window region provenance accepted");
    }

    private static NodalOsRealQaWindowRegionCaptureResult Reject(
        NodalOsRealQaWindowRegionCaptureProvenance provenance,
        NodalOsRealQaWindowRegionCaptureDecision decision,
        string reason) =>
        new(
            provenance.FixtureId,
            decision,
            AllowedForRealQaWindowCapture: false,
            RealQaWindowRegionUsed: false,
            SimulatedWindowRegionUsed: provenance.CaptureMode == NodalOsRealQaWindowRegionCaptureMode.SimulatedWindowRegion,
            WindowLiveAndVisible: provenance.WindowExists && provenance.WindowVisible && provenance.LivenessConfirmed,
            RegionInsideWindow: provenance.WindowBounds.Width > 0 &&
                provenance.WindowBounds.Height > 0 &&
                provenance.RegionBounds.Width > 0 &&
                provenance.RegionBounds.Height > 0 &&
                provenance.RegionBounds.X >= 0 &&
                provenance.RegionBounds.Y >= 0 &&
                provenance.RegionBounds.X + provenance.RegionBounds.Width <= provenance.WindowBounds.Width &&
                provenance.RegionBounds.Y + provenance.RegionBounds.Height <= provenance.WindowBounds.Height,
            NoFullScreen: !provenance.ContainsFullScreen,
            NoSensitive: !provenance.Sensitive &&
                !provenance.ContainsCustomerData &&
                !provenance.ContainsFinancialData &&
                !provenance.ContainsRealPersonData,
            NoDocumentData: !provenance.ContainsDocumentData,
            NoCredentialData: !provenance.ContainsCredentialOrPasswordData,
            BrowserCredentialRedactor.Redact(reason));
}
