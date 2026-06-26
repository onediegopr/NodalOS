namespace OneBrain.WindowsComputerUse;

public enum ComputerUseLocatorHandoffReason
{
    None,
    AmbiguousTarget,
    StaleElement,
    SensitiveSurface,
    BlockageDetected,
    VisualOnlyTarget,
    LowConfidence,
    UacAdmin,
    ActionAuthorityDenied
}

public sealed record ComputerUseLocatorFusionInput(
    ComputerUseSnapshot Snapshot,
    string Objective,
    Win32ContextCollectionResult? Win32Context = null,
    WindowsUiAutomationEventStreamState? UiaEvents = null,
    RobustPerceptionBridgeResult? VisualBridgeResult = null,
    IReadOnlyList<ComputerUseSensitiveSurface>? SensitiveSurfaces = null,
    IReadOnlyList<ComputerUseBlockage>? Blockages = null);

public sealed record ComputerUseSelectorEvidence(
    string EvidenceId,
    ComputerUseEvidenceKind EvidenceKind,
    string SourceSignal,
    string DetailRedacted,
    double Weight,
    bool Redacted,
    bool ActionAuthority);

public sealed record ComputerUseSelectorConfidenceBreakdown(
    double AutomationIdScore,
    double RuntimeIdScore,
    double AncestryScore,
    double ControlTypeClassNameScore,
    double NameMatchScore,
    double Win32AnchorScore,
    double UiaEventContinuityScore,
    double VisualHintScore,
    double BoundsFallbackScore,
    double AmbiguityPenalty,
    double StalePenalty,
    double SensitivePenalty,
    double BlockagePenalty,
    double FinalConfidence);

public sealed record ComputerUseSelectorCandidate(
    string CandidateId,
    string SelectorKind,
    UiElementIdentity? Identity,
    UiElementBounds? Bounds,
    string LabelRedacted,
    ComputerUseSelectorConfidenceBreakdown ConfidenceBreakdown,
    IReadOnlyList<ComputerUseSelectorEvidence> Evidence,
    bool RequiresVisualFallback,
    bool RequiresHumanHandoff,
    bool SensitiveSurface,
    bool Stale,
    bool Ambiguous,
    bool ActionAuthority);

public sealed record ComputerUseLocatorAmbiguity(
    bool IsAmbiguous,
    double Score,
    IReadOnlyList<string> CompetingCandidateIds,
    string ReasonRedacted,
    bool Redacted);

public sealed record ComputerUseStaleElementRisk(
    bool IsStale,
    double Score,
    IReadOnlyList<string> Reasons,
    bool RequiresHumanHandoff);

public sealed record ComputerUseVisualHintMatch(
    string ObservationId,
    string TextRedacted,
    string RoleHint,
    VisualSignalConfidence Confidence,
    double MatchScore,
    bool EvidenceOnly,
    bool ActionAuthority);

public sealed record ComputerUseEventContinuitySignal(
    string SignalId,
    IReadOnlyList<string> EventIds,
    double ContinuityScore,
    bool ModalOrBlockerObserved,
    bool StaleOrUnresponsiveObserved,
    bool ActionAuthority);

public sealed record ComputerUseWin32AnchorSignal(
    string SignalId,
    bool ActiveWindowMatched,
    string ProcessNameRedacted,
    string WindowTitleRedacted,
    double ConfidenceDelta,
    bool RequiresHumanHandoff,
    bool ActionAuthority);

public sealed record ComputerUseLocatorFusionResult(
    IReadOnlyList<ComputerUseSelectorCandidate> LocatorCandidates,
    ComputerUseSelectorCandidate? BestCandidate,
    ComputerUseLocatorAmbiguity Ambiguity,
    ComputerUseStaleElementRisk StaleElementRisk,
    IReadOnlyList<ComputerUseVisualHintMatch> VisualHintMatches,
    IReadOnlyList<ComputerUseEventContinuitySignal> EventContinuitySignals,
    ComputerUseWin32AnchorSignal Win32Anchor,
    bool VisualFallbackRequired,
    bool RequiresHumanHandoff,
    IReadOnlyList<ComputerUseLocatorHandoffReason> HandoffReasons,
    IReadOnlyList<ComputerUseBlockage> Blockages,
    IReadOnlyList<ComputerUseSensitiveSurface> SensitiveSurfaces,
    IReadOnlyList<string> EvidenceRefs,
    bool ActionAuthorityGranted,
    bool AllowedToExecuteLive,
    bool LiveProviderCalled,
    bool RealPcRead,
    bool RawScreenshotStored,
    bool RawTextPresent);

public sealed class ComputerUseLocatorFusionEngine
{
    private readonly ComputerUseEvidenceRedactor _redactor = new();

    public ComputerUseLocatorFusionResult Fuse(ComputerUseLocatorFusionInput input)
    {
        var blockages = (input.Blockages ?? new ComputerUseBlockageDetector().Detect(input.Snapshot))
            .Concat(BuildVisualRiskBlockages(input.VisualBridgeResult))
            .Concat(BuildEventDerivedBlockages(input.UiaEvents))
            .Concat(BuildHostileSourceBlockages(input))
            .Distinct()
            .ToArray();
        var sensitive = (input.SensitiveSurfaces ?? new ComputerUseSensitiveSurfaceDetector().Detect(input.Snapshot))
            .Concat(BuildVisualRiskSensitiveSurfaces(input.VisualBridgeResult))
            .Distinct()
            .ToArray();
        var visualHints = BuildVisualHintMatches(input.VisualBridgeResult, input.Objective);
        var win32Anchor = BuildWin32Anchor(input.Win32Context, input.Snapshot);
        var eventContinuity = BuildEventContinuity(input.UiaEvents);
        var staleRisk = BuildStaleRisk(input.Win32Context, input.Snapshot, input.UiaEvents, blockages);
        var elements = input.Snapshot.Windows
            .SelectMany(window => ComputerUseBlockageDetector.Flatten(window.Elements).Select(element => (window, element)))
            .ToArray();

        var candidates = elements
            .Select(pair => BuildCandidate(pair.window, pair.element, input.Objective, win32Anchor, eventContinuity, visualHints, staleRisk, blockages, sensitive))
            .Concat(BuildVisualOnlyCandidates(visualHints))
            .ToArray();
        var ambiguity = BuildAmbiguity(candidates);
        var ranked = candidates
            .Select(candidate => ApplyAmbiguity(candidate, ambiguity))
            .OrderByDescending(candidate => candidate.ConfidenceBreakdown.FinalConfidence)
            .ThenBy(candidate => candidate.LabelRedacted, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var top = ranked.FirstOrDefault();
        var visualFallbackRequired = ranked.Any(c => c.RequiresVisualFallback) ||
            visualHints.Any() && !ranked.Any(c => c.Identity is not null && c.ConfidenceBreakdown.FinalConfidence >= 0.6);
        var handoffReasons = ResolveHandoffReasons(top, ambiguity, staleRisk, visualFallbackRequired, blockages, sensitive).ToArray();
        var requiresHandoff = handoffReasons.Length > 0;
        var refs = ranked.Select(c => $"locator:{c.CandidateId}:redacted")
            .Concat(ranked.SelectMany(c => c.Evidence.Select(e => e.EvidenceId)))
            .Concat(input.Win32Context?.Windows.SelectMany(w => w.EvidenceRefs) ?? [])
            .Concat(input.UiaEvents?.Events.SelectMany(e => e.EvidenceRefs) ?? [])
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var rawTextPresent = ranked.Any(c => c.SensitiveSurface) || visualHints.Any(h => _redactor.Redact(h.TextRedacted).SensitiveFieldsRedacted.Count > 0);

        return new ComputerUseLocatorFusionResult(
            ranked,
            top,
            ambiguity,
            staleRisk,
            visualHints,
            eventContinuity,
            win32Anchor,
            visualFallbackRequired,
            requiresHandoff,
            handoffReasons,
            blockages,
            sensitive,
            refs,
            ActionAuthorityGranted: false,
            AllowedToExecuteLive: false,
            LiveProviderCalled: false,
            RealPcRead: false,
            RawScreenshotStored: false,
            RawTextPresent: rawTextPresent);
    }

    private ComputerUseSelectorCandidate BuildCandidate(
        WindowContext window,
        UiElementNode element,
        string objective,
        ComputerUseWin32AnchorSignal win32Anchor,
        IReadOnlyList<ComputerUseEventContinuitySignal> eventContinuity,
        IReadOnlyList<ComputerUseVisualHintMatch> visualHints,
        ComputerUseStaleElementRisk staleRisk,
        IReadOnlyList<ComputerUseBlockage> blockages,
        IReadOnlyList<ComputerUseSensitiveSurface> sensitive)
    {
        var nameMatch = MatchesObjective(objective, element.Identity.Name) ? 0.16 : 0;
        var automationId = string.IsNullOrWhiteSpace(element.Identity.AutomationId) ? 0 : 0.32;
        var runtimeId = string.IsNullOrWhiteSpace(element.Identity.RuntimeId) ? 0 : 0.2;
        var ancestry = element.Identity.Ancestry.Count > 0 ? 0.08 : 0;
        var controlType = !string.IsNullOrWhiteSpace(element.Identity.ControlType) || !string.IsNullOrWhiteSpace(element.Identity.ClassName) ? 0.12 : 0;
        var win32 = win32Anchor.ActiveWindowMatched ? win32Anchor.ConfidenceDelta : 0;
        var continuity = eventContinuity.Any(s => s.ContinuityScore > 0 && !s.ModalOrBlockerObserved && !s.StaleOrUnresponsiveObserved) ? 0.05 : 0;
        var visualHint = visualHints.Any(h => MatchesObjective(h.TextRedacted, element.Identity.Name)) ? 0.04 : 0;
        var bounds = element.Bounds.IsEmpty ? 0 : 0.03;
        var sensitiveTarget = IsSensitiveTarget(element, sensitive);
        var sensitivePenalty = sensitiveTarget ? 0.45 : 0;
        var stalePenalty = staleRisk.IsStale ? Math.Min(0.35, staleRisk.Score * 0.35) : 0;
        var blockagePenalty = blockages.Any(b => b.RequiresHumanHandoff) ? 0.22 : 0;
        var score = 0.12 + automationId + runtimeId + ancestry + controlType + nameMatch + win32 + continuity + visualHint + bounds - sensitivePenalty - stalePenalty - blockagePenalty;
        if (element.IsVisualOnly)
        {
            score = Math.Min(score, 0.45);
        }

        var selectorKind = ResolveSelectorKind(element, window, win32Anchor);
        var label = _redactor.Redact($"{element.Identity.Name} {element.Identity.AutomationId}").Value.Trim();
        var evidence = BuildSelectorEvidence(element, window, selectorKind, win32Anchor, visualHint, continuity);
        var breakdown = new ComputerUseSelectorConfidenceBreakdown(
            automationId,
            runtimeId,
            ancestry,
            controlType,
            nameMatch,
            win32,
            continuity,
            visualHint,
            bounds,
            AmbiguityPenalty: 0,
            stalePenalty,
            sensitivePenalty,
            blockagePenalty,
            Math.Clamp(score, 0, 0.99));

        var safeIdentity = element.Identity with
        {
            AutomationId = _redactor.Redact(element.Identity.AutomationId).Value,
            RuntimeId = _redactor.Redact(element.Identity.RuntimeId).Value,
            Name = _redactor.Redact(element.Identity.Name).Value,
            ClassName = _redactor.Redact(element.Identity.ClassName).Value,
            ProcessName = _redactor.Redact(element.Identity.ProcessName).Value,
            Ancestry = element.Identity.Ancestry.Select(item => _redactor.Redact(item).Value).ToArray()
        };

        return new ComputerUseSelectorCandidate(
            CandidateId(element),
            selectorKind,
            safeIdentity,
            element.Bounds,
            label,
            breakdown,
            evidence,
            RequiresVisualFallback: element.IsVisualOnly || selectorKind == "BoundingBoxFallbackMetadata",
            RequiresHumanHandoff: breakdown.FinalConfidence < 0.6 || element.IsVisualOnly || sensitiveTarget || staleRisk.IsStale || blockages.Any(b => b.RequiresHumanHandoff),
            SensitiveSurface: sensitiveTarget,
            Stale: staleRisk.IsStale,
            Ambiguous: false,
            ActionAuthority: false);
    }

    private IEnumerable<ComputerUseSelectorCandidate> BuildVisualOnlyCandidates(IReadOnlyList<ComputerUseVisualHintMatch> visualHints)
    {
        foreach (var hint in visualHints)
        {
            var score = Math.Min(0.42, hint.MatchScore);
            var breakdown = new ComputerUseSelectorConfidenceBreakdown(0, 0, 0, 0, 0, 0, 0, score, 0, 0, 0, 0, 0, score);
            yield return new ComputerUseSelectorCandidate(
                $"visual-{hint.ObservationId}",
                "VisualHintOnly",
                Identity: null,
                Bounds: null,
                hint.TextRedacted,
                breakdown,
                [new ComputerUseSelectorEvidence($"visual:{hint.ObservationId}:redacted", ComputerUseEvidenceKind.SelectorConfidenceBreakdown, "ocr.visual.hint", hint.TextRedacted, score, Redacted: true, ActionAuthority: false)],
                RequiresVisualFallback: true,
                RequiresHumanHandoff: true,
                SensitiveSurface: false,
                Stale: false,
                Ambiguous: false,
                ActionAuthority: false);
        }
    }

    private static ComputerUseSelectorCandidate ApplyAmbiguity(ComputerUseSelectorCandidate candidate, ComputerUseLocatorAmbiguity ambiguity)
    {
        if (!ambiguity.CompetingCandidateIds.Contains(candidate.CandidateId, StringComparer.OrdinalIgnoreCase))
        {
            return candidate;
        }

        var breakdown = candidate.ConfidenceBreakdown with
        {
            AmbiguityPenalty = ambiguity.Score,
            FinalConfidence = Math.Clamp(candidate.ConfidenceBreakdown.FinalConfidence - ambiguity.Score, 0, 0.99)
        };

        return candidate with
        {
            ConfidenceBreakdown = breakdown,
            RequiresHumanHandoff = true,
            Ambiguous = true
        };
    }

    private ComputerUseLocatorAmbiguity BuildAmbiguity(IReadOnlyList<ComputerUseSelectorCandidate> candidates)
    {
        var duplicate = candidates
            .Where(c => c.Identity is not null)
            .GroupBy(c => $"{c.Identity!.ProcessName}|{c.Identity.ControlType}|{c.Identity.Name}", StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(g => g.Count() > 1);
        if (duplicate is null)
        {
            return new ComputerUseLocatorAmbiguity(false, 0, [], "No ambiguous locator candidates detected.", Redacted: true);
        }

        var ids = duplicate.Select(c => c.CandidateId).ToArray();
        var label = _redactor.Redact(duplicate.First().LabelRedacted).Value;
        return new ComputerUseLocatorAmbiguity(true, 0.28, ids, $"Duplicate selector label '{label}' requires handoff.", Redacted: true);
    }

    private static ComputerUseStaleElementRisk BuildStaleRisk(
        Win32ContextCollectionResult? win32,
        ComputerUseSnapshot snapshot,
        WindowsUiAutomationEventStreamState? events,
        IReadOnlyList<ComputerUseBlockage> blockages)
    {
        var reasons = new List<string>();
        var score = 0.0;
        if (win32?.Status == Win32ContextCollectionStatus.FixtureOnly &&
            win32.ActiveWindow is not null &&
            !ActiveWindowMatchesSnapshot(win32, snapshot))
        {
            reasons.Add("Win32 active window does not match snapshot.");
            score += 0.4;
        }

        if (win32?.ActiveWindow?.Modal.IsModal == true || blockages.Any(b => b.Kind == ComputerUseBlockageKind.HiddenWindowOrModal))
        {
            reasons.Add("Modal or top-level blocker can stale existing locators.");
            score += 0.35;
        }

        if (events?.Events.Any(e => e.Kind is WindowsUiAutomationEventKind.AppBecameUnresponsive or WindowsUiAutomationEventKind.BlockedStateDetected) == true)
        {
            reasons.Add("UIA event stream reports stale or unresponsive state.");
            score += 0.4;
        }

        if (events?.Events.Any(e => e.Kind is WindowsUiAutomationEventKind.ModalAppeared or WindowsUiAutomationEventKind.WindowOpened) == true)
        {
            reasons.Add("UIA event stream reports modal/window change after snapshot.");
            score += 0.4;
        }

        score = Math.Clamp(score, 0, 1);
        return new ComputerUseStaleElementRisk(score >= 0.4, score, reasons.Distinct(StringComparer.OrdinalIgnoreCase).ToArray(), score >= 0.4);
    }

    private static ComputerUseWin32AnchorSignal BuildWin32Anchor(Win32ContextCollectionResult? win32, ComputerUseSnapshot snapshot)
    {
        var active = win32?.ActiveWindow;
        if (active is null || win32?.Status != Win32ContextCollectionStatus.FixtureOnly)
        {
            return new ComputerUseWin32AnchorSignal("win32-anchor:none", false, "", "", 0, true, ActionAuthority: false);
        }

        var matched = ActiveWindowMatchesSnapshot(win32, snapshot);
        return new ComputerUseWin32AnchorSignal(
            $"win32-anchor:{active.Identity.HwndOpaque}",
            matched,
            active.Process.ProcessName,
            active.Identity.TitleRedacted,
            matched ? 0.08 : -0.2,
            RequiresHumanHandoff: !matched || !active.Process.IsAllowlisted || active.Modal.IsModal || active.Dpi.MismatchDetected,
            ActionAuthority: false);
    }

    private static IReadOnlyList<ComputerUseEventContinuitySignal> BuildEventContinuity(WindowsUiAutomationEventStreamState? events)
    {
        if (events is null || events.Status != WindowsUiAutomationEventStreamStatus.FixtureOnly)
        {
            return [];
        }

        var modal = events.Events.Any(e => e.Kind is WindowsUiAutomationEventKind.ModalAppeared or WindowsUiAutomationEventKind.WindowOpened);
        var stale = events.Events.Any(e => e.Kind is WindowsUiAutomationEventKind.AppBecameUnresponsive or WindowsUiAutomationEventKind.BlockedStateDetected);
        var continuity = events.Events.Any(e => e.Kind is WindowsUiAutomationEventKind.FocusChanged or WindowsUiAutomationEventKind.PropertyChanged or WindowsUiAutomationEventKind.StructureChanged) ? 0.05 : 0;
        return
        [
            new ComputerUseEventContinuitySignal(
                $"uia-events:{events.Events.Count}:redacted",
                events.Events.Select(e => e.EventId).ToArray(),
                continuity,
                modal,
                stale,
                ActionAuthority: false)
        ];
    }

    private static IReadOnlyList<ComputerUseBlockage> BuildEventDerivedBlockages(WindowsUiAutomationEventStreamState? events)
    {
        if (events is null || events.Status != WindowsUiAutomationEventStreamStatus.FixtureOnly)
        {
            return [];
        }

        var blockages = new List<ComputerUseBlockage>();
        AddIf(blockages, events.Events.Any(e => e.Kind is WindowsUiAutomationEventKind.ModalAppeared or WindowsUiAutomationEventKind.WindowOpened), ComputerUseBlockageKind.HiddenWindowOrModal, "UIA event stream indicates modal/window change; locator fusion requires handoff.");
        AddIf(blockages, events.Events.Any(e => e.Kind is WindowsUiAutomationEventKind.AppBecameUnresponsive or WindowsUiAutomationEventKind.BlockedStateDetected), ComputerUseBlockageKind.HiddenWindowOrModal, "UIA event stream indicates stale or blocked state; locator fusion requires handoff.");
        AddIf(blockages, events.Events.Any(e => e.Kind is WindowsUiAutomationEventKind.SensitiveValueChanged or WindowsUiAutomationEventKind.TextValueChanged), ComputerUseBlockageKind.CredentialField, "UIA event stream contains sensitive text/value metadata; locator fusion requires handoff.");
        return blockages;
    }

    private static IReadOnlyList<ComputerUseBlockage> BuildHostileSourceBlockages(ComputerUseLocatorFusionInput input)
    {
        var blockages = new List<ComputerUseBlockage>();
        var bridge = input.VisualBridgeResult;
        if (bridge is not null)
        {
            AddIf(blockages, bridge.ActionAuthority || ComputerUseVisualSignalPolicy.HasActionAuthority(bridge.Observations), ComputerUseBlockageKind.AuditLogBypassRisk, "Visual/OCR bridge attempted to claim action authority; locator fusion blocked.");
            AddIf(blockages, bridge.LiveProviderCalled, ComputerUseBlockageKind.AuditLogBypassRisk, "Visual/OCR bridge called a live provider; locator fusion blocked.");
            AddIf(blockages, bridge.RawScreenshotStored, ComputerUseBlockageKind.ScreenshotRisk, "Visual/OCR bridge stored a raw screenshot; locator fusion blocked.");
            AddIf(blockages, bridge.RequiresHumanHandoff, ComputerUseBlockageKind.AuditLogBypassRisk, "Visual/OCR bridge explicitly requested human handoff; locator fusion honors the request.");
        }

        if (input.Win32Context is not null)
        {
            var win32 = input.Win32Context;
            AddIf(blockages, win32.ReadRealPc, ComputerUseBlockageKind.AuditLogBypassRisk, "Win32 context read a real PC; locator fusion blocked.");
            AddIf(blockages, win32.ActionAuthority, ComputerUseBlockageKind.AuditLogBypassRisk, "Win32 context claimed action authority; locator fusion blocked.");
            AddIf(blockages, win32.WindowManipulationUsed || win32.FocusStealingUsed || win32.InputInjectionUsed, ComputerUseBlockageKind.AuditLogBypassRisk, "Win32 context attempted window manipulation, focus stealing, or input injection; locator fusion blocked.");
            AddIf(blockages, win32.ClipboardUsed, ComputerUseBlockageKind.AuditLogBypassRisk, "Win32 context accessed clipboard; locator fusion blocked.");
            AddIf(blockages, win32.ScreenshotCaptured, ComputerUseBlockageKind.ScreenshotRisk, "Win32 context captured a screenshot; locator fusion blocked.");
        }

        if (input.UiaEvents is not null)
        {
            var events = input.UiaEvents;
            AddIf(blockages, events.LiveSubscribed, ComputerUseBlockageKind.AuditLogBypassRisk, "UIA event stream performed a live subscription; locator fusion blocked.");
            AddIf(blockages, events.ActionCallbackRegistered, ComputerUseBlockageKind.AuditLogBypassRisk, "UIA event stream registered an action callback; locator fusion blocked.");
            AddIf(blockages, events.ActionAuthority, ComputerUseBlockageKind.AuditLogBypassRisk, "UIA event stream claimed action authority; locator fusion blocked.");
            AddIf(blockages, events.Events.Any(e => e.CanTriggerExecution || e.ActionAuthority), ComputerUseBlockageKind.AuditLogBypassRisk, "UIA event payload claims execution authority; locator fusion blocked.");
        }

        return blockages;
    }

    private static IReadOnlyList<ComputerUseBlockage> BuildVisualRiskBlockages(RobustPerceptionBridgeResult? bridge)
    {
        if (bridge is null)
        {
            return [];
        }

        var risks = bridge.Observations.SelectMany(o => o.Signals).SelectMany(s => s.SurfaceRisks).Distinct().ToArray();
        var blockages = new List<ComputerUseBlockage>();
        AddIf(blockages, risks.Any(r => r is VisualSurfaceRisk.Payment or VisualSurfaceRisk.Submission or VisualSurfaceRisk.Destructive), ComputerUseBlockageKind.DestructiveAction, "Visual/OCR risk indicates payment, submission, or destructive action; locator fusion requires handoff.");
        AddIf(blockages, risks.Any(r => r == VisualSurfaceRisk.UacAdmin), ComputerUseBlockageKind.UacAdmin, "Visual/OCR risk indicates UAC/admin blocker; locator fusion requires handoff.");
        AddIf(blockages, risks.Any(r => r is VisualSurfaceRisk.ModalOverlay or VisualSurfaceRisk.EmptyOrBlocked), ComputerUseBlockageKind.HiddenWindowOrModal, "Visual/OCR risk indicates modal, overlay, empty, or blocked state.");
        AddIf(blockages, risks.Any(r => r is VisualSurfaceRisk.LowConfidence or VisualSurfaceRisk.Captcha), ComputerUseBlockageKind.LowConfidenceLocator, "Visual/OCR risk is low confidence or captcha-like; locator fusion requires handoff.");
        return blockages;
    }

    private static IReadOnlyList<ComputerUseSensitiveSurface> BuildVisualRiskSensitiveSurfaces(RobustPerceptionBridgeResult? bridge)
    {
        if (bridge is null)
        {
            return [];
        }

        var risks = bridge.Observations.SelectMany(o => o.Signals).SelectMany(s => s.SurfaceRisks).Distinct().ToArray();
        if (!risks.Any(r => r is VisualSurfaceRisk.SensitiveCredential or VisualSurfaceRisk.OtpOrMfa or VisualSurfaceRisk.Payment))
        {
            return [];
        }

        return
        [
            new ComputerUseSensitiveSurface("visual-risk", "visual-risk-redacted", "Visual/OCR risk indicates sensitive surface; handoff required.", true)
        ];
    }

    private static void AddIf(ICollection<ComputerUseBlockage> blockages, bool condition, ComputerUseBlockageKind kind, string reason)
    {
        if (condition)
        {
            blockages.Add(new ComputerUseBlockage(kind, ComputerUseBlockageSeverity.Critical, reason, CanContinueAutomatically: false, RequiresHumanHandoff: true));
        }
    }

    private IReadOnlyList<ComputerUseVisualHintMatch> BuildVisualHintMatches(RobustPerceptionBridgeResult? bridge, string objective)
    {
        if (bridge is null || !bridge.Available)
        {
            return [];
        }

        var hints = new List<ComputerUseVisualHintMatch>();
        foreach (var signal in bridge.Observations.SelectMany(o => o.Signals))
        {
            foreach (var text in signal.TextObservations)
            {
                hints.Add(new ComputerUseVisualHintMatch(text.ObservationId, text.TextRedacted, "text", text.Confidence, ScoreVisualHint(objective, text.TextRedacted, text.Confidence), EvidenceOnly: true, ActionAuthority: false));
            }

            foreach (var element in signal.ElementObservations)
            {
                hints.Add(new ComputerUseVisualHintMatch(element.ObservationId, element.LabelRedacted, element.RoleHint, element.Confidence, ScoreVisualHint(objective, element.LabelRedacted, element.Confidence), EvidenceOnly: true, ActionAuthority: false));
            }
        }

        return hints;
    }

    private static IReadOnlyList<ComputerUseSelectorEvidence> BuildSelectorEvidence(
        UiElementNode element,
        WindowContext window,
        string selectorKind,
        ComputerUseWin32AnchorSignal win32Anchor,
        double visualHint,
        double continuity)
    {
        var redactor = new ComputerUseEvidenceRedactor();
        var detail = redactor.Redact($"{selectorKind}; name={element.Identity.Name}; automationId={element.Identity.AutomationId}; process={window.ProcessName}; win32={win32Anchor.ActiveWindowMatched}").Value;
        var evidence = new List<ComputerUseSelectorEvidence>
        {
            new($"selector:{CandidateId(element)}:redacted", ComputerUseEvidenceKind.SelectorConfidenceBreakdown, "uia.identity", detail, 1, Redacted: true, ActionAuthority: false)
        };
        if (win32Anchor.ActiveWindowMatched)
        {
            evidence.Add(new($"selector:{CandidateId(element)}:win32-anchor", ComputerUseEvidenceKind.Win32ContextObservation, "win32.anchor", win32Anchor.WindowTitleRedacted, win32Anchor.ConfidenceDelta, Redacted: true, ActionAuthority: false));
        }
        if (continuity > 0)
        {
            evidence.Add(new($"selector:{CandidateId(element)}:uia-event-continuity", ComputerUseEvidenceKind.UiaEventObservation, "uia.event.continuity", "Read-only UIA event continuity.", continuity, Redacted: true, ActionAuthority: false));
        }
        if (visualHint > 0)
        {
            evidence.Add(new($"selector:{CandidateId(element)}:visual-hint", ComputerUseEvidenceKind.SelectorConfidenceBreakdown, "ocr.visual.hint", "Redacted visual/OCR text hint.", visualHint, Redacted: true, ActionAuthority: false));
        }

        return evidence;
    }

    private static IEnumerable<ComputerUseLocatorHandoffReason> ResolveHandoffReasons(
        ComputerUseSelectorCandidate? top,
        ComputerUseLocatorAmbiguity ambiguity,
        ComputerUseStaleElementRisk stale,
        bool visualFallbackRequired,
        IReadOnlyList<ComputerUseBlockage> blockages,
        IReadOnlyList<ComputerUseSensitiveSurface> sensitive)
    {
        if (ambiguity.IsAmbiguous)
            yield return ComputerUseLocatorHandoffReason.AmbiguousTarget;
        if (stale.IsStale)
            yield return ComputerUseLocatorHandoffReason.StaleElement;
        if (sensitive.Count > 0 || top?.SensitiveSurface == true)
            yield return ComputerUseLocatorHandoffReason.SensitiveSurface;
        if (visualFallbackRequired || top?.RequiresVisualFallback == true)
            yield return ComputerUseLocatorHandoffReason.VisualOnlyTarget;
        if (top is null || top.ConfidenceBreakdown.FinalConfidence < 0.6)
            yield return ComputerUseLocatorHandoffReason.LowConfidence;
        if (blockages.Any(b => b.Kind == ComputerUseBlockageKind.UacAdmin))
            yield return ComputerUseLocatorHandoffReason.UacAdmin;
        if (blockages.Any(b => b.RequiresHumanHandoff))
            yield return ComputerUseLocatorHandoffReason.BlockageDetected;
    }

    private static bool IsSensitiveTarget(UiElementNode element, IReadOnlyList<ComputerUseSensitiveSurface> sensitive)
    {
        if (element.IsPasswordField || element.IsCredentialField)
        {
            return true;
        }

        var id = string.IsNullOrWhiteSpace(element.Identity.AutomationId) ? element.Identity.RuntimeId : element.Identity.AutomationId;
        return sensitive.Any(s => string.Equals(s.SurfaceId, id, StringComparison.OrdinalIgnoreCase));
    }

    private static string ResolveSelectorKind(UiElementNode element, WindowContext window, ComputerUseWin32AnchorSignal win32Anchor)
    {
        if (!string.IsNullOrWhiteSpace(element.Identity.AutomationId) && win32Anchor.ActiveWindowMatched)
            return "AutomationId+Process+WindowContext";
        if (!string.IsNullOrWhiteSpace(element.Identity.RuntimeId) && element.Identity.Ancestry.Count > 0)
            return "RuntimeId+Ancestry";
        if (!string.IsNullOrWhiteSpace(element.Identity.ControlType) && !string.IsNullOrWhiteSpace(element.Identity.ClassName) && !string.IsNullOrWhiteSpace(element.Identity.Name))
            return "ControlType+ClassName+Name";
        if (win32Anchor.ActiveWindowMatched && !string.IsNullOrWhiteSpace(window.ProcessName))
            return "Win32WindowProcessAnchor";
        return element.Bounds.IsEmpty ? "VisualHintOnly" : "BoundingBoxFallbackMetadata";
    }

    private static double ScoreVisualHint(string objective, string label, VisualSignalConfidence confidence)
    {
        var score = MatchesObjective(objective, label) ? 0.42 : 0.18;
        score += confidence switch
        {
            VisualSignalConfidence.VerifiedFixture => 0.08,
            VisualSignalConfidence.High => 0.04,
            VisualSignalConfidence.Medium => 0.02,
            VisualSignalConfidence.Low => -0.16,
            _ => -0.22
        };
        return Math.Clamp(score, 0, 0.5);
    }

    private static bool MatchesObjective(string objective, string value) =>
        !string.IsNullOrWhiteSpace(value) &&
        (objective.Contains(value, StringComparison.OrdinalIgnoreCase) ||
         value.Contains(objective, StringComparison.OrdinalIgnoreCase));

    private static bool ActiveWindowMatchesSnapshot(Win32ContextCollectionResult? result, ComputerUseSnapshot snapshot)
    {
        var active = result?.ActiveWindow;
        if (active is null)
        {
            return false;
        }

        return snapshot.Windows.Any(window =>
            string.Equals(window.ProcessName, active.Process.ProcessName, StringComparison.OrdinalIgnoreCase) ||
            active.Identity.TitleRedacted.Contains(window.Title, StringComparison.OrdinalIgnoreCase) ||
            window.Title.Contains(active.Identity.TitleRedacted, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(window.ClassName, active.Identity.ClassName, StringComparison.OrdinalIgnoreCase));
    }

    private static string CandidateId(UiElementNode element)
    {
        var id = !string.IsNullOrWhiteSpace(element.Identity.AutomationId)
            ? element.Identity.AutomationId
            : !string.IsNullOrWhiteSpace(element.Identity.RuntimeId)
                ? element.Identity.RuntimeId
                : $"{element.Identity.ProcessName}-{element.Identity.ControlType}-{element.Identity.Name}-{element.Bounds.X}-{element.Bounds.Y}";
        return id.Replace(' ', '-').ToLowerInvariant();
    }
}
