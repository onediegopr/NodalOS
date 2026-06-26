namespace OneBrain.WindowsComputerUse;

public sealed record ComputerUsePerceptionFusionRequest(
    ComputerUseSnapshot Snapshot,
    RobustPerceptionBridgeResult VisualBridgeResult,
    Win32ContextCollectionResult? Win32Context = null,
    WindowsUiAutomationEventStreamState? UiaEvents = null);

public sealed record ComputerUsePerceptionFusionResult(
    ComputerUseCapabilityClassification CapabilityClassification,
    IReadOnlyList<ComputerUseBlockage> Blockages,
    IReadOnlyList<ComputerUseSensitiveSurface> SensitiveSurfaces,
    bool VisualFallbackRequired,
    bool SensitiveSurfaceDetected,
    bool BlockageDetected,
    string? LowConfidenceReason,
    ComputerUseHandoffReason HumanHandoffReason,
    IReadOnlyList<string> Reasons,
    ComputerUseSnapshot EnrichedSnapshot,
    double UiaRichnessScore,
    bool ActiveWindowMatched,
    bool ModalOrOverlayState,
    double EventDerivedConfidence,
    IReadOnlyList<string> EvidenceRefs,
    bool ActionAuthorityGranted);

public sealed record ComputerUsePerceptionFusionDecision(
    ComputerUsePerceptionFusionResult Fusion,
    ComputerUsePolicyDecision PolicyDecision);

public sealed class ComputerUsePerceptionFusionClassifier
{
    public ComputerUsePerceptionFusionResult Fuse(ComputerUsePerceptionFusionRequest request)
    {
        var baseClassification = new ComputerUseCapabilityClassifier().Classify(request.Snapshot);
        var blockages = new List<ComputerUseBlockage>(new ComputerUseBlockageDetector().Detect(request.Snapshot));
        var sensitive = new List<ComputerUseSensitiveSurface>(new ComputerUseSensitiveSurfaceDetector().Detect(request.Snapshot));
        var reasons = new List<string>(baseClassification.Reasons);
        var visualSignals = request.VisualBridgeResult.Observations.SelectMany(o => o.Signals).ToArray();
        var visualRisks = visualSignals.SelectMany(s => s.SurfaceRisks).Where(r => r != VisualSurfaceRisk.None).Distinct().ToArray();
        var lowConfidenceSignal = visualSignals.FirstOrDefault(s => s.Confidence is VisualSignalConfidence.Missing or VisualSignalConfidence.Low);
        var eventState = request.UiaEvents ?? new DisabledWindowsUiAutomationEventStream().Read(new WindowsUiAutomationEventStreamOptions("disabled"));
        var win32 = request.Win32Context;
        var evidenceRefs = new List<string>();
        evidenceRefs.AddRange(request.VisualBridgeResult.Observations.SelectMany(o => o.Signals).SelectMany(s => s.TextObservations.Select(t => t.ObservationId)));
        evidenceRefs.AddRange(request.VisualBridgeResult.Observations.SelectMany(o => o.Signals).SelectMany(s => s.ElementObservations.Select(e => e.ObservationId)));
        evidenceRefs.AddRange(win32?.Windows.SelectMany(w => w.EvidenceRefs) ?? []);
        evidenceRefs.AddRange(eventState.Events.SelectMany(e => e.EvidenceRefs));

        if (!request.VisualBridgeResult.Available)
        {
            reasons.Add("Visual bridge unavailable; UIA fixture path remains usable when semantic metadata is sufficient.");
        }

        if (win32 is null || win32.Status == Win32ContextCollectionStatus.Disabled)
        {
            reasons.Add("Win32 context unavailable; fixture fusion falls back to UIA snapshot and visual bridge.");
        }
        else
        {
            ApplyWin32Context(win32, request.Snapshot, blockages, reasons);
        }

        if (eventState.Status == WindowsUiAutomationEventStreamStatus.Disabled)
        {
            reasons.Add("UIA event stream unavailable; no live event subscription was attempted.");
        }
        else
        {
            ApplyUiaEvents(eventState, blockages, sensitive, reasons);
        }

        if (request.VisualBridgeResult.RawScreenshotStored)
        {
            blockages.Add(Critical(ComputerUseBlockageKind.ScreenshotRisk, "Visual bridge attempted raw screenshot persistence."));
        }

        if (request.VisualBridgeResult.LiveProviderCalled)
        {
            blockages.Add(Critical(ComputerUseBlockageKind.AuditLogBypassRisk, "Visual bridge attempted to call a live OCR/vision provider; fixture-only enforcement violated."));
            reasons.Add("Live OCR/vision provider call detected; fixture-only enforcement violated.");
        }

        if (request.VisualBridgeResult.ActionAuthority || ComputerUseVisualSignalPolicy.HasActionAuthority(request.VisualBridgeResult.Observations))
        {
            blockages.Add(Critical(ComputerUseBlockageKind.AuditLogBypassRisk, "Visual/OCR signal attempted to claim action authority."));
        }

        foreach (var risk in visualRisks)
        {
            AddRisk(risk, blockages, sensitive, reasons);
        }

        if (lowConfidenceSignal is not null)
        {
            blockages.Add(new ComputerUseBlockage(
                ComputerUseBlockageKind.LowConfidenceLocator,
                ComputerUseBlockageSeverity.Warning,
                $"Visual signal '{lowConfidenceSignal.SignalId}' is low confidence and cannot target actions.",
                CanContinueAutomatically: false,
                RequiresHumanHandoff: true));
        }

        var visualFallbackRequired = baseClassification.RequiresVisualFallback ||
            request.Snapshot.Windows.SelectMany(w => ComputerUseBlockageDetector.Flatten(w.Elements)).Any(e => e.IsVisualOnly) ||
            visualSignals.Length > 0 && baseClassification.TechnologyKind is WindowTechnologyKind.UiaPoor or WindowTechnologyKind.VisualOnly or WindowTechnologyKind.Unknown;

        if (visualSignals.Length > 0)
        {
            reasons.Add("Visual/OCR observations are auxiliary evidence only.");
        }

        var sensitiveDetected = sensitive.Count > 0;
        var blockageDetected = blockages.Any(b => b.RequiresHumanHandoff);
        var bridgeRequestedHandoff = request.VisualBridgeResult.RequiresHumanHandoff;
        if (bridgeRequestedHandoff)
        {
            reasons.Add("Visual bridge explicitly requested human handoff; perception fusion honors the request.");
        }

        var eventDerivedConfidence = CalculateEventDerivedConfidence(eventState);
        var activeWindowMatched = ActiveWindowMatchesSnapshot(win32, request.Snapshot);
        var modalOrOverlay = (win32?.ActiveWindow?.Modal.IsModal ?? false) ||
            eventState.Events.Any(e => e.Kind is WindowsUiAutomationEventKind.ModalAppeared or WindowsUiAutomationEventKind.WindowOpened) ||
            blockages.Any(b => b.Kind == ComputerUseBlockageKind.HiddenWindowOrModal);
        var handoff = ResolveHandoff(blockages, sensitiveDetected, visualFallbackRequired, lowConfidenceSignal, bridgeRequestedHandoff);
        var uiaRichnessScore = CalculateUiaRichnessScore(request.Snapshot, eventState);
        var adjustedClassification = AdjustClassification(
            baseClassification,
            visualSignals.Length > 0,
            win32 is { Status: Win32ContextCollectionStatus.FixtureOnly },
            eventState.Status == WindowsUiAutomationEventStreamStatus.FixtureOnly,
            visualFallbackRequired,
            blockageDetected,
            reasons,
            eventDerivedConfidence,
            activeWindowMatched);

        return new ComputerUsePerceptionFusionResult(
            adjustedClassification,
            blockages,
            sensitive,
            visualFallbackRequired,
            sensitiveDetected,
            blockageDetected,
            lowConfidenceSignal is null ? null : $"Visual signal '{lowConfidenceSignal.SignalId}' has {lowConfidenceSignal.Confidence} confidence.",
            handoff,
            reasons.Distinct(StringComparer.OrdinalIgnoreCase).ToArray(),
            request.Snapshot,
            uiaRichnessScore,
            activeWindowMatched,
            modalOrOverlay,
            eventDerivedConfidence,
            evidenceRefs.Distinct(StringComparer.OrdinalIgnoreCase).ToArray(),
            ActionAuthorityGranted: false);
    }

    private static ComputerUseCapabilityClassification AdjustClassification(
        ComputerUseCapabilityClassification baseClassification,
        bool hasVisualSignals,
        bool hasWin32Context,
        bool hasUiaEvents,
        bool visualFallbackRequired,
        bool blockageDetected,
        IReadOnlyList<string> reasons,
        double eventDerivedConfidence,
        bool activeWindowMatched)
    {
        var capabilities = baseClassification.Capabilities.ToList();
        if (hasVisualSignals)
        {
            capabilities.Add("visual.observation.redacted");
        }
        if (hasWin32Context)
        {
            capabilities.Add("win32.context.redacted");
        }
        if (hasUiaEvents)
        {
            capabilities.Add("uia.events.redacted");
        }

        var missing = baseClassification.MissingSignals.ToList();
        if (visualFallbackRequired && !hasVisualSignals)
        {
            missing.Add("visual.observation");
        }

        return new ComputerUseCapabilityClassification(
            baseClassification.TechnologyKind,
            AdjustConfidence(baseClassification, hasVisualSignals, hasWin32Context, hasUiaEvents, eventDerivedConfidence, activeWindowMatched),
            capabilities.Distinct(StringComparer.OrdinalIgnoreCase).ToArray(),
            missing.Distinct(StringComparer.OrdinalIgnoreCase).ToArray(),
            CanPlanFixtureOnly: baseClassification.CanPlanFixtureOnly && !blockageDetected && !visualFallbackRequired,
            RequiresVisualFallback: visualFallbackRequired,
            RequiresHumanHandoff: baseClassification.RequiresHumanHandoff || blockageDetected || visualFallbackRequired,
            reasons.Distinct(StringComparer.OrdinalIgnoreCase).ToArray());
    }

    private static void AddRisk(
        VisualSurfaceRisk risk,
        ICollection<ComputerUseBlockage> blockages,
        ICollection<ComputerUseSensitiveSurface> sensitive,
        ICollection<string> reasons)
    {
        switch (risk)
        {
            case VisualSurfaceRisk.SensitiveCredential:
            case VisualSurfaceRisk.OtpOrMfa:
                sensitive.Add(new ComputerUseSensitiveSurface($"visual-{risk}", risk.ToString(), "Sensitive visual/OCR observation detected.", true));
                blockages.Add(Critical(ComputerUseBlockageKind.CredentialField, $"Visual/OCR detected {risk}; handoff required."));
                break;
            case VisualSurfaceRisk.Payment:
            case VisualSurfaceRisk.Submission:
            case VisualSurfaceRisk.Destructive:
                blockages.Add(Critical(ComputerUseBlockageKind.DestructiveAction, $"Visual/OCR detected {risk}; action authority remains blocked."));
                break;
            case VisualSurfaceRisk.UacAdmin:
                blockages.Add(Critical(ComputerUseBlockageKind.UacAdmin, "Visual/OCR detected UAC/admin-like blocker."));
                break;
            case VisualSurfaceRisk.ModalOverlay:
                blockages.Add(new ComputerUseBlockage(ComputerUseBlockageKind.HiddenWindowOrModal, ComputerUseBlockageSeverity.Warning, "Visual/OCR detected modal or overlay blocker.", false, true));
                break;
            case VisualSurfaceRisk.EmptyOrBlocked:
                blockages.Add(new ComputerUseBlockage(ComputerUseBlockageKind.HiddenWindowOrModal, ComputerUseBlockageSeverity.Warning, "Visual/OCR detected empty or blocked state.", false, true));
                break;
            case VisualSurfaceRisk.Captcha:
                blockages.Add(Critical(ComputerUseBlockageKind.VisualOnlyTarget, "Visual/OCR detected captcha-like surface; handoff required."));
                break;
            case VisualSurfaceRisk.LowConfidence:
                blockages.Add(new ComputerUseBlockage(ComputerUseBlockageKind.LowConfidenceLocator, ComputerUseBlockageSeverity.Warning, "Visual/OCR confidence is too low for targeting.", false, true));
                break;
        }

        reasons.Add($"Visual risk detected: {risk}.");
    }

    private static ComputerUseHandoffReason ResolveHandoff(
        IReadOnlyList<ComputerUseBlockage> blockages,
        bool sensitiveDetected,
        bool visualFallbackRequired,
        VisualPerceptionSignal? lowConfidenceSignal,
        bool bridgeRequestedHandoff)
    {
        if (sensitiveDetected || blockages.Any(b => b.Kind == ComputerUseBlockageKind.CredentialField))
            return ComputerUseHandoffReason.SensitiveSurface;
        if (blockages.Any(b => b.Kind == ComputerUseBlockageKind.UacAdmin))
            return ComputerUseHandoffReason.UacAdmin;
        if (blockages.Any(b => b.Kind == ComputerUseBlockageKind.DestructiveAction))
            return ComputerUseHandoffReason.DestructiveAction;
        if (lowConfidenceSignal is not null || blockages.Any(b => b.Kind == ComputerUseBlockageKind.LowConfidenceLocator))
            return ComputerUseHandoffReason.LowConfidence;
        if (visualFallbackRequired)
            return ComputerUseHandoffReason.VisualOnlyTarget;
        if (blockages.Any(b => b.RequiresHumanHandoff))
            return ComputerUseHandoffReason.VerificationFailed;
        if (bridgeRequestedHandoff)
            return ComputerUseHandoffReason.VerificationFailed;
        return ComputerUseHandoffReason.None;
    }

    private static void ApplyWin32Context(
        Win32ContextCollectionResult result,
        ComputerUseSnapshot snapshot,
        ICollection<ComputerUseBlockage> blockages,
        ICollection<string> reasons)
    {
        if (result.WindowManipulationUsed || result.FocusStealingUsed || result.InputInjectionUsed || result.ClipboardUsed || result.ScreenshotCaptured || result.ActionAuthority)
        {
            blockages.Add(Critical(ComputerUseBlockageKind.AuditLogBypassRisk, "Win32 context attempted an action or unsafe capture."));
        }

        var active = result.ActiveWindow;
        if (active is null)
        {
            blockages.Add(new ComputerUseBlockage(ComputerUseBlockageKind.LowConfidenceLocator, ComputerUseBlockageSeverity.Warning, "Win32 active window context is missing.", false, true));
            return;
        }

        if (!active.Process.IsAllowlisted)
        {
            blockages.Add(Critical(ComputerUseBlockageKind.AppNotAllowlisted, "Win32 active process is not allowlisted."));
        }

        if (active.Modal.IsModal)
        {
            blockages.Add(new ComputerUseBlockage(ComputerUseBlockageKind.HiddenWindowOrModal, ComputerUseBlockageSeverity.Warning, "Win32 modal/top-level relationship detected.", false, true));
        }

        if (active.Dpi.MismatchDetected)
        {
            blockages.Add(new ComputerUseBlockage(ComputerUseBlockageKind.DpiMonitorMismatch, ComputerUseBlockageSeverity.Warning, "Win32 monitor/DPI mismatch detected.", false, true));
        }

        if (!ActiveWindowMatchesSnapshot(result, snapshot))
        {
            blockages.Add(new ComputerUseBlockage(ComputerUseBlockageKind.LowConfidenceLocator, ComputerUseBlockageSeverity.Warning, "Win32 active window does not match UIA fixture snapshot.", false, true));
        }

        reasons.Add("Win32 context contributes read-only active window metadata.");
    }

    private static void ApplyUiaEvents(
        WindowsUiAutomationEventStreamState state,
        ICollection<ComputerUseBlockage> blockages,
        ICollection<ComputerUseSensitiveSurface> sensitive,
        ICollection<string> reasons)
    {
        if (state.LiveSubscribed || state.ActionCallbackRegistered || state.ActionAuthority || state.Events.Any(e => e.CanTriggerExecution || e.ActionAuthority))
        {
            blockages.Add(Critical(ComputerUseBlockageKind.AuditLogBypassRisk, "UIA event stream attempted live subscription, callback execution, or action authority."));
        }

        foreach (var ev in state.Events)
        {
            if ((ev.Kind is WindowsUiAutomationEventKind.SensitiveValueChanged or WindowsUiAutomationEventKind.TextValueChanged) &&
                (ev.Payload.SensitiveFieldsRedacted.Count > 0 || ContainsSensitiveEventHint(ev)))
            {
                sensitive.Add(new ComputerUseSensitiveSurface($"uia-event-{ev.EventId}", "uia-event-sensitive-payload", "Sensitive UIA event payload detected.", true));
                blockages.Add(Critical(ComputerUseBlockageKind.CredentialField, "UIA event payload indicates sensitive value/text change."));
            }

            if (ev.Kind is WindowsUiAutomationEventKind.ModalAppeared or WindowsUiAutomationEventKind.WindowOpened)
            {
                var combined = $"{ev.Payload.NameRedacted} {ev.Payload.ValueRedacted}";
                if (combined.Contains("User Account Control", StringComparison.OrdinalIgnoreCase) ||
                    combined.Contains("administrator", StringComparison.OrdinalIgnoreCase))
                {
                    blockages.Add(Critical(ComputerUseBlockageKind.UacAdmin, "UIA event indicates UAC/admin-like blocker."));
                }
                else if (combined.Contains("modal", StringComparison.OrdinalIgnoreCase) ||
                         combined.Contains("confirm", StringComparison.OrdinalIgnoreCase) ||
                         combined.Contains("overwrite", StringComparison.OrdinalIgnoreCase))
                {
                    blockages.Add(new ComputerUseBlockage(ComputerUseBlockageKind.HiddenWindowOrModal, ComputerUseBlockageSeverity.Warning, "UIA event indicates modal/overlay blocker.", false, true));
                }
            }

            if (ev.Kind is WindowsUiAutomationEventKind.AppBecameUnresponsive or WindowsUiAutomationEventKind.BlockedStateDetected)
            {
                blockages.Add(new ComputerUseBlockage(ComputerUseBlockageKind.HiddenWindowOrModal, ComputerUseBlockageSeverity.Warning, "UIA event indicates blocked or stale surface.", false, true));
            }
        }

        reasons.Add("UIA events contribute read-only event evidence only.");
    }

    private static bool ContainsSensitiveEventHint(WindowsUiAutomationEvent ev)
    {
        var value = $"{ev.Payload.NameRedacted} {ev.Payload.PropertyName} {ev.Payload.ValueRedacted}".ToLowerInvariant();
        return value.Contains("password") || value.Contains("credential") || value.Contains("token") || value.Contains("otp") || value.Contains("[redacted]");
    }

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

    private static double CalculateUiaRichnessScore(ComputerUseSnapshot snapshot, WindowsUiAutomationEventStreamState events)
    {
        var elements = snapshot.Windows.SelectMany(w => ComputerUseBlockageDetector.Flatten(w.Elements)).ToArray();
        if (elements.Length == 0)
        {
            return 0;
        }

        var richElements = elements.Count(e =>
            !string.IsNullOrWhiteSpace(e.Identity.AutomationId) ||
            !string.IsNullOrWhiteSpace(e.Identity.RuntimeId) ||
            e.Patterns.SupportsInvoke ||
            e.Patterns.SupportsValue ||
            e.Patterns.SupportsText);
        var score = richElements / (double)elements.Length;
        if (events.Events.Any(e => e.Kind is WindowsUiAutomationEventKind.FocusChanged or WindowsUiAutomationEventKind.StructureChanged or WindowsUiAutomationEventKind.PropertyChanged))
        {
            score += 0.05;
        }

        return Math.Clamp(score, 0, 1);
    }

    private static double CalculateEventDerivedConfidence(WindowsUiAutomationEventStreamState state)
    {
        if (state.Status != WindowsUiAutomationEventStreamStatus.FixtureOnly || state.Events.Count == 0)
        {
            return 0;
        }

        var baseScore = state.Events.All(e => e.EvidenceOnly && !e.CanTriggerExecution && !e.ActionAuthority) ? 0.72 : 0.2;
        if (state.Events.Any(e => e.Kind is WindowsUiAutomationEventKind.AppBecameUnresponsive or WindowsUiAutomationEventKind.BlockedStateDetected or WindowsUiAutomationEventKind.ModalAppeared))
        {
            return Math.Min(baseScore, 0.55);
        }

        return baseScore;
    }

    private static double AdjustConfidence(
        ComputerUseCapabilityClassification baseClassification,
        bool hasVisualSignals,
        bool hasWin32Context,
        bool hasUiaEvents,
        double eventDerivedConfidence,
        bool activeWindowMatched)
    {
        var confidence = baseClassification.Confidence;
        if (hasVisualSignals && baseClassification.TechnologyKind == WindowTechnologyKind.UiaPoor)
        {
            confidence = Math.Min(0.59, confidence + 0.06);
        }
        if (hasWin32Context && activeWindowMatched)
        {
            confidence += 0.04;
        }
        if (hasUiaEvents && eventDerivedConfidence > 0)
        {
            confidence += 0.03;
        }

        return Math.Clamp(confidence, 0, 0.99);
    }

    private static ComputerUseBlockage Critical(ComputerUseBlockageKind kind, string reason) =>
        new(kind, ComputerUseBlockageSeverity.Critical, reason, false, true);
}

public sealed class ComputerUsePerceptionFusionPlanner
{
    private readonly ComputerUsePerceptionFusionClassifier _fusionClassifier = new();
    private readonly ComputerUseSafeActionPlanner _safeActionPlanner = new();

    public ComputerUsePerceptionFusionDecision Plan(
        ComputerUsePerceptionFusionRequest request,
        string objective,
        ComputerUseActionKind requestedAction)
    {
        var fusion = _fusionClassifier.Fuse(request);
        var baseDecision = _safeActionPlanner.Plan(request.Snapshot, objective, requestedAction);
        var requiresHandoff = fusion.HumanHandoffReason != ComputerUseHandoffReason.None ||
            fusion.VisualFallbackRequired ||
            fusion.BlockageDetected ||
            fusion.SensitiveSurfaceDetected ||
            fusion.LowConfidenceReason is not null;

        if (!requiresHandoff)
        {
            return new ComputerUsePerceptionFusionDecision(fusion, baseDecision);
        }

        var handoffReasons = baseDecision.Candidates.SelectMany(c => c.HandoffReasons)
            .Append(fusion.HumanHandoffReason)
            .Where(r => r != ComputerUseHandoffReason.None)
            .Distinct()
            .ToArray();
        var candidate = new ComputerUseActionCandidate(
            ComputerUseActionKind.HumanHandoff,
            Target: null,
            objective,
            Confidence: 0,
            DryRunOnly: true,
            RequiresExplicitFutureApproval: true,
            RequiresHumanHandoff: true,
            HandoffReasons: handoffReasons.Length == 0 ? [ComputerUseHandoffReason.VerificationFailed] : handoffReasons,
            Reason: "Perception fusion requires human handoff; visual/OCR observations do not authorize actions.");

        var decision = new ComputerUsePolicyDecision(
            AllowedToPlan: false,
            AllowedToExecuteLive: false,
            FixtureOnly: true,
            Candidates: [candidate],
            Blockages: baseDecision.Blockages.Concat(fusion.Blockages).Distinct().ToArray(),
            SensitiveSurfaces: baseDecision.SensitiveSurfaces.Concat(fusion.SensitiveSurfaces).Distinct().ToArray(),
            Reasons: baseDecision.Reasons.Concat(fusion.Reasons).Append("Perception is not authorization.").Distinct(StringComparer.OrdinalIgnoreCase).ToArray());

        return new ComputerUsePerceptionFusionDecision(fusion, decision);
    }
}
