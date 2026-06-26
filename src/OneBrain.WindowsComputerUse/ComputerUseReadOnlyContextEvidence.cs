using System.Text.Json;

namespace OneBrain.WindowsComputerUse;

public sealed record ComputerUseReadOnlyContextEvidencePack(
    string EvidenceId,
    ComputerUseEvidenceKind EvidenceKind,
    string SourceRef,
    IReadOnlyList<string> EvidenceRefs,
    string SummaryRedacted,
    IReadOnlyList<string> SensitiveFieldsRedacted,
    bool RawScreenshotStored,
    bool ClipboardCaptured,
    bool ActionAuthority,
    bool EventTriggeredExecution,
    bool Redacted,
    DateTimeOffset CreatedAtUtc);

public sealed class ComputerUseReadOnlyContextEvidenceBuilder
{
    private readonly ComputerUseEvidenceRedactor _redactor = new();

    public ComputerUseReadOnlyContextEvidencePack BuildWin32ContextEvidence(Win32ContextCollectionResult result)
    {
        var active = result.ActiveWindow;
        var summary = active is null
            ? $"win32 status={result.Status}"
            : $"win32 hwnd={active.Identity.HwndOpaque}; title={active.Identity.TitleRedacted}; process={active.Process.ProcessName}; path={active.Process.ProcessPathRedacted}; placement={active.Placement}; modal={active.Modal.IsModal}; dpi={active.Dpi.DpiScale}";
        var redaction = _redactor.Redact(summary);
        return Pack(
            ComputerUseEvidenceKind.Win32ContextObservation,
            active?.Identity.HwndOpaque ?? "win32:none",
            active?.EvidenceRefs ?? [],
            redaction);
    }

    public ComputerUseReadOnlyContextEvidencePack BuildUiaEventEvidence(WindowsUiAutomationEventStreamState state)
    {
        var redactedEvents = state.Events.Select(WindowsUiAutomationEventRedactor.Redact).ToArray();
        var summary = string.Join("; ", redactedEvents.Select(e =>
            $"{e.Event.Kind}:{e.Event.Payload.NameRedacted}:{e.Event.Payload.PropertyName}:{e.Event.Payload.ValueRedacted}"));
        var redaction = _redactor.Redact(summary);
        var kind = redactedEvents.Any(e => e.Event.Kind is WindowsUiAutomationEventKind.ModalAppeared or WindowsUiAutomationEventKind.AppBecameUnresponsive or WindowsUiAutomationEventKind.BlockedStateDetected)
            ? ComputerUseEvidenceKind.EventDerivedBlockage
            : redactedEvents.Any(e => e.Event.Kind == WindowsUiAutomationEventKind.FocusChanged)
                ? ComputerUseEvidenceKind.ActiveWindowChanged
                : ComputerUseEvidenceKind.UiaEventObservation;

        return Pack(
            kind,
            state.Status.ToString(),
            redactedEvents.SelectMany(e => e.Event.EvidenceRefs).Distinct(StringComparer.OrdinalIgnoreCase).ToArray(),
            redaction,
            redactedEvents.SelectMany(e => e.SensitiveFieldsRedacted).Distinct(StringComparer.OrdinalIgnoreCase).ToArray());
    }

    public string Serialize(ComputerUseReadOnlyContextEvidencePack pack) =>
        JsonSerializer.Serialize(pack, new JsonSerializerOptions { WriteIndented = true });

    private static ComputerUseReadOnlyContextEvidencePack Pack(
        ComputerUseEvidenceKind kind,
        string sourceRef,
        IReadOnlyList<string> evidenceRefs,
        ComputerUseRedactionResult redaction,
        IReadOnlyList<string>? extraFields = null) =>
        new(
            EvidenceId: $"wcu-context-evidence-{Guid.NewGuid():N}",
            kind,
            sourceRef,
            evidenceRefs,
            redaction.Value,
            redaction.SensitiveFieldsRedacted.Concat(extraFields ?? []).Distinct(StringComparer.OrdinalIgnoreCase).ToArray(),
            RawScreenshotStored: false,
            ClipboardCaptured: false,
            ActionAuthority: false,
            EventTriggeredExecution: false,
            Redacted: true,
            CreatedAtUtc: DateTimeOffset.UnixEpoch);
}
