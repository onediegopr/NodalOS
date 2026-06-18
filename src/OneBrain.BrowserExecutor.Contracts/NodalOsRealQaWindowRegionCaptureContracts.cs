namespace OneBrain.BrowserExecutor.Contracts;

// M310-M312 — Real QA window region capture gate.
// This explicitly distinguishes real QA-window capture from simulated-window abstractions.

public enum NodalOsRealQaWindowRegionCaptureMode
{
    RealQaWindowRegion,
    SimulatedWindowRegion,
    BlockedBeforeRealCapture
}

public enum NodalOsRealQaWindowRegionCaptureDecision
{
    AcceptedForRealQaWindowCapture,
    RejectedSimulatedWindow,
    RejectedUnknownWindow,
    RejectedProcessMismatch,
    RejectedFullScreen,
    RejectedInvalidBounds,
    RejectedRegionOutsideWindow,
    RejectedLivenessOrVisibility,
    RejectedSensitiveRegion,
    RejectedDocumentRegion,
    RejectedCredentialData,
    RejectedMissingExpectedText
}

public sealed record NodalOsRealQaWindowRegionCaptureProvenance(
    string FixtureId,
    string ExpectedText,
    NodalOsRealQaWindowRegionCaptureMode CaptureMode,
    string ExpectedWindowTitle,
    string ObservedWindowTitle,
    string ExpectedProcessOrSource,
    string ObservedProcessOrSource,
    string WindowHandleOrSourceId,
    NodalOsScreenRegionBounds WindowBounds,
    NodalOsScreenRegionBounds RegionBounds,
    bool WindowExists,
    bool WindowVisible,
    bool LivenessConfirmed,
    bool ContainsRealPersonData,
    bool ContainsCustomerData,
    bool ContainsFinancialData,
    bool ContainsDocumentData,
    bool ContainsCredentialOrPasswordData,
    bool ContainsFullScreen,
    bool Sensitive,
    string Reason);

public sealed record NodalOsRealQaWindowRegionCaptureResult(
    string FixtureId,
    NodalOsRealQaWindowRegionCaptureDecision Decision,
    bool AllowedForRealQaWindowCapture,
    bool RealQaWindowRegionUsed,
    bool SimulatedWindowRegionUsed,
    bool WindowLiveAndVisible,
    bool RegionInsideWindow,
    bool NoFullScreen,
    bool NoSensitive,
    bool NoDocumentData,
    bool NoCredentialData,
    string Reason);
