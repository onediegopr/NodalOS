namespace OneBrain.WindowsComputerUse;

public sealed record ComputerUsePerceptionFusionRequest(
    ComputerUseSnapshot Snapshot,
    RobustPerceptionBridgeResult VisualBridgeResult);

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

        if (!request.VisualBridgeResult.Available)
        {
            reasons.Add("Visual bridge unavailable; UIA fixture path remains usable when semantic metadata is sufficient.");
        }

        if (request.VisualBridgeResult.RawScreenshotStored)
        {
            blockages.Add(Critical(ComputerUseBlockageKind.ScreenshotRisk, "Visual bridge attempted raw screenshot persistence."));
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
        var handoff = ResolveHandoff(blockages, sensitiveDetected, visualFallbackRequired, lowConfidenceSignal);
        var adjustedClassification = AdjustClassification(baseClassification, visualSignals.Length > 0, visualFallbackRequired, blockageDetected, reasons);

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
            ActionAuthorityGranted: false);
    }

    private static ComputerUseCapabilityClassification AdjustClassification(
        ComputerUseCapabilityClassification baseClassification,
        bool hasVisualSignals,
        bool visualFallbackRequired,
        bool blockageDetected,
        IReadOnlyList<string> reasons)
    {
        var capabilities = baseClassification.Capabilities.ToList();
        if (hasVisualSignals)
        {
            capabilities.Add("visual.observation.redacted");
        }

        var missing = baseClassification.MissingSignals.ToList();
        if (visualFallbackRequired && !hasVisualSignals)
        {
            missing.Add("visual.observation");
        }

        return new ComputerUseCapabilityClassification(
            baseClassification.TechnologyKind,
            hasVisualSignals && baseClassification.TechnologyKind == WindowTechnologyKind.UiaPoor
                ? Math.Min(0.59, baseClassification.Confidence + 0.06)
                : baseClassification.Confidence,
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
        VisualPerceptionSignal? lowConfidenceSignal)
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
        return ComputerUseHandoffReason.None;
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
