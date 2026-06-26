namespace OneBrain.WindowsComputerUse;

public enum VisualSignalSource
{
    Unknown,
    UiaSemanticTree,
    Win32WindowContext,
    UiaEvent,
    ExistingRobustPerception,
    ExistingOcrObservation,
    MistralOcrProviderRouterCandidate,
    Fixture,
    HumanHandoff
}

public enum VisualSignalConfidence
{
    Missing,
    Low,
    Medium,
    High,
    VerifiedFixture
}

public enum VisualSurfaceRisk
{
    None,
    SensitiveCredential,
    OtpOrMfa,
    Payment,
    Submission,
    Destructive,
    UacAdmin,
    ModalOverlay,
    EmptyOrBlocked,
    Captcha,
    LowConfidence,
    Unknown
}

public sealed record VisualTextObservation(
    string ObservationId,
    string TextRedacted,
    VisualSignalSource Source,
    VisualSignalConfidence Confidence,
    UiElementBounds? Bounds,
    IReadOnlyList<VisualSurfaceRisk> SurfaceRisks,
    bool RawTextPresent = false,
    bool ActionAuthority = false);

public sealed record VisualElementObservation(
    string ObservationId,
    string LabelRedacted,
    string RoleHint,
    VisualSignalSource Source,
    VisualSignalConfidence Confidence,
    UiElementBounds? Bounds,
    IReadOnlyList<VisualSurfaceRisk> SurfaceRisks,
    bool CandidateOnly = true,
    bool ActionAuthority = false);

public sealed record VisualPerceptionSignal(
    string SignalId,
    VisualSignalSource Source,
    VisualSignalConfidence Confidence,
    IReadOnlyList<VisualTextObservation> TextObservations,
    IReadOnlyList<VisualElementObservation> ElementObservations,
    IReadOnlyList<VisualSurfaceRisk> SurfaceRisks,
    bool EvidenceOnly,
    bool Redacted,
    bool RawScreenshotStored,
    bool ActionAuthority);

public sealed record RedactedVisualObservation(
    string ObservationId,
    string SourceHash,
    IReadOnlyList<VisualPerceptionSignal> Signals,
    IReadOnlyList<string> SensitiveFieldsRedacted,
    bool RawScreenshotStored,
    bool RawTextPresent,
    bool Redacted,
    bool ActionAuthority);

public sealed record RobustPerceptionBridgeResult(
    bool Available,
    string ProviderId,
    string Mode,
    IReadOnlyList<RedactedVisualObservation> Observations,
    bool RequiresHumanHandoff,
    bool RawScreenshotStored,
    bool LiveProviderCalled,
    bool ActionAuthority,
    IReadOnlyList<string> Reasons);

public interface IComputerUseVisualPerceptionBridge
{
    RobustPerceptionBridgeResult Observe(ComputerUseSnapshot snapshot);
}

public sealed class ComputerUseVisualPerceptionBridgeDisabled : IComputerUseVisualPerceptionBridge
{
    public RobustPerceptionBridgeResult Observe(ComputerUseSnapshot snapshot) =>
        new(
            Available: false,
            ProviderId: "wcu.visual.bridge.disabled",
            Mode: "DesignOnlyDisabled",
            Observations: [],
            RequiresHumanHandoff: false,
            RawScreenshotStored: false,
            LiveProviderCalled: false,
            ActionAuthority: false,
            Reasons: ["Existing OCR/Robust Perception bridge is disabled in fixture-safe WCU tests."]);
}

public sealed class ComputerUseFixtureVisualPerceptionBridge : IComputerUseVisualPerceptionBridge
{
    private readonly IReadOnlyList<RedactedVisualObservation> _observations;

    public ComputerUseFixtureVisualPerceptionBridge(IReadOnlyList<RedactedVisualObservation> observations)
    {
        _observations = observations;
    }

    public RobustPerceptionBridgeResult Observe(ComputerUseSnapshot snapshot) =>
        new(
            Available: _observations.Count > 0,
            ProviderId: "existing.robust-perception.fixture",
            Mode: "FixtureOnly",
            Observations: _observations,
            RequiresHumanHandoff: _observations.SelectMany(o => o.Signals).Any(ComputerUseVisualSignalPolicy.RequiresHandoff),
            RawScreenshotStored: false,
            LiveProviderCalled: false,
            ActionAuthority: false,
            Reasons: ["Fixture visual observations wrap existing OCR/Robust Perception outputs; no provider is executed."]);
}

public static class ComputerUseVisualObservationFixtures
{
    public static RedactedVisualObservation FromText(
        string id,
        string text,
        VisualSignalConfidence confidence = VisualSignalConfidence.High,
        VisualSignalSource source = VisualSignalSource.ExistingOcrObservation,
        UiElementBounds? bounds = null)
    {
        var redactor = new ComputerUseEvidenceRedactor();
        var redacted = redactor.Redact(text);
        var risks = ComputerUseVisualSignalPolicy.ClassifyRisks(redacted.Value);
        var signal = new VisualPerceptionSignal(
            $"signal-{id}",
            source,
            confidence,
            [
                new VisualTextObservation(
                    $"text-{id}",
                    redacted.Value,
                    source,
                    confidence,
                    bounds,
                    risks,
                    RawTextPresent: false,
                    ActionAuthority: false)
            ],
            [],
            risks,
            EvidenceOnly: true,
            Redacted: true,
            RawScreenshotStored: false,
            ActionAuthority: false);

        return new RedactedVisualObservation(
            id,
            $"fixture-sha256-{id}",
            [signal],
            redacted.SensitiveFieldsRedacted,
            RawScreenshotStored: false,
            RawTextPresent: false,
            Redacted: true,
            ActionAuthority: false);
    }

    public static RedactedVisualObservation FromElement(
        string id,
        string label,
        string roleHint,
        VisualSignalConfidence confidence,
        VisualSignalSource source = VisualSignalSource.ExistingRobustPerception,
        UiElementBounds? bounds = null)
    {
        var redactor = new ComputerUseEvidenceRedactor();
        var redacted = redactor.Redact(label);
        var risks = ComputerUseVisualSignalPolicy.ClassifyRisks(redacted.Value);
        if (confidence is VisualSignalConfidence.Low or VisualSignalConfidence.Missing)
        {
            risks = risks.Append(VisualSurfaceRisk.LowConfidence).Distinct().ToArray();
        }

        var signal = new VisualPerceptionSignal(
            $"signal-{id}",
            source,
            confidence,
            [],
            [
                new VisualElementObservation(
                    $"element-{id}",
                    redacted.Value,
                    roleHint,
                    source,
                    confidence,
                    bounds,
                    risks,
                    CandidateOnly: true,
                    ActionAuthority: false)
            ],
            risks,
            EvidenceOnly: true,
            Redacted: true,
            RawScreenshotStored: false,
            ActionAuthority: false);

        return new RedactedVisualObservation(
            id,
            $"fixture-sha256-{id}",
            [signal],
            redacted.SensitiveFieldsRedacted,
            RawScreenshotStored: false,
            RawTextPresent: false,
            Redacted: true,
            ActionAuthority: false);
    }
}

public static class ComputerUseVisualSignalPolicy
{
    public static IReadOnlyList<VisualSurfaceRisk> ClassifyRisks(string redactedText)
    {
        var value = redactedText.ToLowerInvariant();
        var risks = new List<VisualSurfaceRisk>();

        AddIf(risks, VisualSurfaceRisk.SensitiveCredential, value.Contains("password") || value.Contains("credential") || value.Contains("api key") || value.Contains("token") || value.Contains("[redacted]"));
        AddIf(risks, VisualSurfaceRisk.OtpOrMfa, value.Contains("otp") || value.Contains("2fa") || value.Contains("mfa"));
        AddIf(risks, VisualSurfaceRisk.Payment, value.Contains("pay") || value.Contains("payment") || value.Contains("card") || value.Contains("cvv"));
        AddIf(risks, VisualSurfaceRisk.Submission, value.Contains("submit") || value.Contains("sign in") || value.Contains("login"));
        AddIf(risks, VisualSurfaceRisk.Destructive, value.Contains("delete") || value.Contains("remove") || value.Contains("overwrite") || value.Contains("format"));
        AddIf(risks, VisualSurfaceRisk.UacAdmin, value.Contains("user account control") || value.Contains("administrator") || value.Contains("admin permission"));
        AddIf(risks, VisualSurfaceRisk.ModalOverlay, value.Contains("modal") || value.Contains("dialog") || value.Contains("confirm"));
        AddIf(risks, VisualSurfaceRisk.EmptyOrBlocked, value.Contains("empty") || value.Contains("blocked") || value.Contains("loading") || value.Contains("unavailable"));
        AddIf(risks, VisualSurfaceRisk.Captcha, value.Contains("captcha"));

        return risks.Count == 0 ? [VisualSurfaceRisk.None] : risks.Distinct().ToArray();
    }

    public static bool RequiresHandoff(VisualPerceptionSignal signal) =>
        !signal.EvidenceOnly ||
        signal.ActionAuthority ||
        signal.RawScreenshotStored ||
        signal.Confidence is VisualSignalConfidence.Missing or VisualSignalConfidence.Low ||
        signal.SurfaceRisks.Any(risk => risk is not VisualSurfaceRisk.None);

    public static bool HasActionAuthority(IReadOnlyList<RedactedVisualObservation> observations) =>
        observations.Any(o => o.ActionAuthority || o.Signals.Any(s => s.ActionAuthority));

    private static void AddIf(ICollection<VisualSurfaceRisk> risks, VisualSurfaceRisk risk, bool condition)
    {
        if (condition)
        {
            risks.Add(risk);
        }
    }
}
