namespace OneBrain.WindowsComputerUse;

public sealed record ComputerUseContainmentProperty(
    string Id,
    string Description,
    string SourceOfTruth,
    string ExpectedScan,
    string EvidenceAllowed,
    string EvidenceProhibited,
    string FailureMode,
    string Blocks);

public static class ComputerUseContainmentPropertyCatalog
{
    public static IReadOnlyList<ComputerUseContainmentProperty> Properties { get; } =
    [
        Property("NO_DESKTOP_LIVE_AUTOMATION", "No desktop live automation is implemented or authorized.", "External audit reconciliation and read-only gates.", "No live desktop provider entry points.", "Fixture metadata and disabled-provider status.", "Real PC reads or live desktop automation.", "Any live desktop automation path appears.", "Live prototype, product automation, and release unlock."),
        Property("NO_REAL_MOUSE_KEYBOARD_CONTROL", "No real mouse or keyboard control is allowed.", "Disabled collectors and no-action scans.", "No SendInput, mouse_event, keybd_event, SendKeys, cursor APIs.", "Boolean false action flags.", "Real input execution or evidence.", "Input API usage appears or flags become true.", "Any live or controlled action path."),
        Property("NO_REAL_SCREENSHOT_CAPTURE", "No real/raw screenshot capture or persistence is allowed.", "Snapshot/evidence flags and visual bridge contracts.", "No CopyFromScreen or raw screenshot persistence.", "Hashes, metadata, and redacted text only.", "Raw screenshot bytes, base64 screenshot payloads.", "Evidence contains screenshot bytes or screenshot flags true.", "Live visual capture and evidence export."),
        Property("NO_P_INVOKE_OR_DLLIMPORT", "WCU containment must not introduce P/Invoke or DllImport.", "No-live/no-action scan.", "No DllImport or LibraryImport in WCU code.", "Managed fixture contracts.", "Native Windows API bindings.", "Native binding appears in WCU containment code.", "Live Win32 adapters and action APIs."),
        Property("NO_FLAUI_OR_LIVE_UIA", "No FlaUI dependency or live UIA action/subscription is allowed.", "Disabled UIA collectors and event stream.", "No FlaUI, live UIA subscription, or AddAutomationEventHandler usage.", "Fixture UIA snapshots and fixture events.", "Live UIA actions, events, or dependencies.", "Live UIA dependency or subscription appears.", "Live read-only prototype and UIA actions."),
        Property("NO_CLIPBOARD_REAL", "No real clipboard read/write is allowed.", "Snapshot/evidence flags and collectors.", "No Clipboard API use.", "Clipboard-present false metadata.", "Clipboard content or real clipboard evidence.", "Clipboard API usage or clipboard flags true.", "Evidence export and any live action path."),
        Property("NO_BROWSER_LIVE_AUTOMATION", "No browser live automation is in WCU containment scope.", "External no-go reconciliation.", "No browser live bridge or browser automation path in WCU.", "Static docs stating browser live is blocked.", "Browser live sessions.", "Browser live integration appears.", "Browser live/CDP work."),
        Property("NO_CDP_LIVE_EXECUTION", "No live CDP execution is allowed.", "External no-go reconciliation.", "No live CDP execution in WCU containment.", "Fixture reports only.", "CDP sessions or execution evidence.", "CDP execution path appears.", "Browser live/CDP unlock."),
        Property("NO_SAFE_INJECTION_LIVE", "No Safe Injection live path is allowed.", "External no-go reconciliation.", "No Safe Injection live path in WCU.", "Blocked status only.", "Safe Injection live execution.", "Safe Injection path appears.", "Live bridge execution."),
        Property("NO_LOCATOR_ACTION_AUTHORITY", "Locator confidence cannot authorize actions.", "Locator fusion contracts.", "ActionAuthorityGranted and AllowedToExecuteLive remain false.", "Confidence breakdown and redacted selector evidence.", "Action authority derived from locator confidence.", "High-confidence selector grants authority.", "Controlled actions and product automation."),
        Property("NO_EVENT_TRIGGERED_EXECUTION", "UIA events cannot trigger execution.", "UIA event stream contracts.", "CanTriggerExecution and ActionAuthority remain false.", "Redacted event metadata.", "Event callbacks, actions, or triggered execution.", "Event stream claims execution capability.", "Live UIA event automation."),
        Property("NO_OCR_ACTION_AUTHORITY", "OCR/visual signals cannot authorize actions.", "Visual bridge contracts.", "ActionAuthority false and LiveProviderCalled false in fixtures.", "Redacted visual observations.", "OCR-only action authorization.", "Visual bridge grants authority.", "OCR-driven actions."),
        Property("NO_WIN32_ACTION_AUTHORITY", "Win32 context cannot authorize actions.", "Win32 read-only context contracts.", "ActionAuthority and manipulation flags remain false.", "Redacted window/process metadata.", "Win32 action authority or manipulation.", "Win32 context grants authority.", "Window manipulation and controlled actions."),
        Property("NO_EVIDENCE_ACTION_AUTHORITY", "Evidence cannot unlock action authority.", "Unified evidence contracts.", "ActionAuthorityGranted false.", "Tamper-guarded redacted evidence metadata.", "Evidence-based action approval.", "Evidence pack grants authority.", "Action execution and release unlock."),
        Property("NO_PROVIDER_NETWORK_LIVE", "No provider network live calls are allowed in containment tests.", "Fixture visual bridge and external reconciliation.", "LiveProviderCalled false and no network provider path.", "Provider unavailable/disabled status.", "Network calls to OCR or providers.", "Live provider call appears.", "Provider-backed live perception."),
        Property("NO_PUBLIC_RELEASE_UNLOCK", "Containment does not unlock public release.", "External no-go reconciliation.", "Reports keep release unlock at 0%.", "Blocked release status.", "Public release approval claim.", "Docs claim public release is unlocked.", "Release/public launch."),
        Property("NO_PAID_BETA_UNLOCK", "Containment does not unlock paid beta.", "External no-go reconciliation.", "Reports keep paid beta unlock at 0%.", "Blocked beta status.", "Paid beta approval claim.", "Docs claim paid beta is unlocked.", "Paid beta launch.")
    ];

    private static ComputerUseContainmentProperty Property(
        string id,
        string description,
        string sourceOfTruth,
        string expectedScan,
        string evidenceAllowed,
        string evidenceProhibited,
        string failureMode,
        string blocks) =>
        new(id, description, sourceOfTruth, expectedScan, evidenceAllowed, evidenceProhibited, failureMode, blocks);
}
