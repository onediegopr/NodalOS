using System.Collections.Concurrent;
using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.BrowserExecutor.Cdp;

public sealed class BrowserCredentialBoundaryDetector
{
    public BrowserCredentialBoundaryDecision EvaluateObservation(BrowserObservation observation)
    {
        var signals = new List<BrowserCredentialSignal>();
        var text = observation.VisibleTextSummary;

        AddTextSignals(signals, observation.MainFrameId, text);

        foreach (var element in observation.ActionableElements)
        {
            var haystack = $"{element.Role} {element.TagName} {element.Text} {element.AccessibleName} {element.Label} {string.Join(" ", element.RiskHints)} {string.Join(" ", element.SelectorCandidates)}";
            AddElementSignals(signals, element.FrameId, element.ElementId, haystack);
        }

        foreach (var form in observation.Forms)
        {
            var haystack = $"{form.Label} {string.Join(" ", form.RiskHints)}";
            AddElementSignals(signals, form.FrameId, form.FormId, haystack);
        }

        return Decide(CreateBoundary(observation.RunId, observation.TargetContext, signals, "observation"));
    }

    public BrowserCredentialBoundaryDecision EvaluateAction(BrowserAction action, BrowserCredentialBoundary? currentBoundary = null)
    {
        var signals = currentBoundary?.Signals.ToList() ?? [];
        var haystack = $"{action.ActionType} {action.Target.CandidateId} {action.Target.Selector} {action.Target.Text} {action.Target.Url} {action.ExpectedOutcome.Description}";
        AddElementSignals(signals, action.FrameId, action.Target.CandidateId, haystack);

        if (action.Input is not null && BrowserCredentialRedactor.ContainsSecret($"{action.Input.Text} {action.Input.Value}"))
        {
            signals.Add(new BrowserCredentialSignal(
                BrowserCredentialSignalKind.TokenPromptDetected,
                BrowserCredentialRisk.Critical,
                action.FrameId,
                action.Target.CandidateId,
                "secret-like input value",
                "automation attempted to provide secret-like input"));
        }

        if (action.ActionType == BrowserActionType.Click &&
            ContainsAny(haystack, "submit", "ingresar", "login", "sign in", "pagar", "pay", "confirmar pago"))
        {
            signals.Add(new BrowserCredentialSignal(
                ContainsAny(haystack, "pagar", "pay", "payment") ? BrowserCredentialSignalKind.PaymentActionDetected : BrowserCredentialSignalKind.CredentialSubmitDetected,
                ContainsAny(haystack, "pagar", "pay", "payment") ? BrowserCredentialRisk.Critical : BrowserCredentialRisk.High,
                action.FrameId,
                action.Target.CandidateId,
                BrowserCredentialRedactor.Redact(haystack),
                "sensitive submit-like action"));
        }

        return Decide(CreateBoundary(action.RunId, action.TargetContext, signals, "action"));
    }

    public BrowserCredentialBoundaryDecision Decide(BrowserCredentialBoundary boundary)
    {
        if (boundary.Signals.Count == 0)
            return new BrowserCredentialBoundaryDecision(BrowserCredentialBoundaryDecisionKind.AllowReadOnly, BrowserCredentialRisk.None, null, "no credential boundary signals", boundary);

        if (boundary.Signals.Any(signal => signal.Kind == BrowserCredentialSignalKind.UnknownSensitivePrompt))
            return new BrowserCredentialBoundaryDecision(BrowserCredentialBoundaryDecisionKind.FailClosed, BrowserCredentialRisk.Unknown, BrowserHumanHandoffReason.UnknownSensitivePrompt, "unknown sensitive prompt detected; fail closed", boundary.Redact());

        var primarySignal = boundary.Signals
            .OrderByDescending(signal => signal.Risk)
            .ThenByDescending(signal => SignalPriority(signal.Kind))
            .First();
        var strongest = primarySignal.Risk;
        var reason = MapReason(primarySignal.Kind);
        var decision = strongest >= BrowserCredentialRisk.High
            ? BrowserCredentialBoundaryDecisionKind.RequiresHuman
            : BrowserCredentialBoundaryDecisionKind.RedactAndContinue;

        return new BrowserCredentialBoundaryDecision(decision, strongest, reason, $"credential boundary decision: {decision}", boundary.Redact());
    }

    private static BrowserCredentialBoundary CreateBoundary(string runId, BrowserTargetContext target, IReadOnlyList<BrowserCredentialSignal> signals, string source) =>
        new(
            BoundaryId: $"boundary-{Guid.NewGuid():N}",
            RunId: runId,
            TargetContext: target,
            DetectedAtUtc: DateTimeOffset.UtcNow,
            Signals: signals.Select(signal => signal.Redact()).ToList(),
            RedactionApplied: true,
            EvidenceRefs: signals.Count == 0 ? [] : [$"credential-boundary:{source}:{target.TargetId}:{target.FrameId}"],
            ProofRefs: signals.Count == 0 ? [] : [$"proof:credential-boundary:{source}:{target.TargetId}:{target.FrameId}"]);

    private static void AddTextSignals(List<BrowserCredentialSignal> signals, string frameId, string text)
    {
        AddElementSignals(signals, frameId, "visible-text", text);
        if (ContainsAny(text, "clave fiscal"))
            Add(signals, BrowserCredentialSignalKind.ClaveFiscalDetected, BrowserCredentialRisk.Critical, frameId, "visible-text", text, "clave fiscal prompt");
        if (ContainsAny(text, "captcha", "no soy un robot", "robot check"))
            Add(signals, BrowserCredentialSignalKind.CaptchaDetected, BrowserCredentialRisk.Critical, frameId, "visible-text", text, "captcha prompt");
        if (ContainsAny(text, "2fa", "two factor", "segundo factor", "verificacion en dos pasos", "verificación en dos pasos"))
            Add(signals, BrowserCredentialSignalKind.TwoFactorPromptDetected, BrowserCredentialRisk.Critical, frameId, "visible-text", text, "two factor prompt");
        if (ContainsAny(text, "unknown sensitive prompt", "sensitive prompt"))
            Add(signals, BrowserCredentialSignalKind.UnknownSensitivePrompt, BrowserCredentialRisk.Unknown, frameId, "visible-text", text, "unknown sensitive prompt");
    }

    private static void AddElementSignals(List<BrowserCredentialSignal> signals, string frameId, string elementId, string haystack)
    {
        if (ContainsAny(haystack, "type=password", "password", "contraseña", "passwd"))
            Add(signals, BrowserCredentialSignalKind.PasswordFieldDetected, BrowserCredentialRisk.Critical, frameId, elementId, haystack, "password field detected");
        if (ContainsAny(haystack, "otp", "one-time", "one time", "codigo", "código", "verification code"))
            Add(signals, BrowserCredentialSignalKind.OtpFieldDetected, BrowserCredentialRisk.Critical, frameId, elementId, haystack, "otp field detected");
        if (ContainsAny(haystack, "token", "access token", "bearer"))
            Add(signals, BrowserCredentialSignalKind.TokenPromptDetected, BrowserCredentialRisk.Critical, frameId, elementId, haystack, "token prompt detected");
        if (ContainsAny(haystack, "login", "sign in", "ingresar", "usuario"))
            Add(signals, BrowserCredentialSignalKind.LoginFormDetected, BrowserCredentialRisk.High, frameId, elementId, haystack, "login form detected");
        if (ContainsAny(haystack, "captcha", "recaptcha", "hcaptcha"))
            Add(signals, BrowserCredentialSignalKind.CaptchaDetected, BrowserCredentialRisk.Critical, frameId, elementId, haystack, "captcha detected");
        if (ContainsAny(haystack, "pagar", "payment", "pay now", "card number", "tarjeta"))
            Add(signals, BrowserCredentialSignalKind.PaymentActionDetected, BrowserCredentialRisk.Critical, frameId, elementId, haystack, "payment action detected");
        if (ContainsAny(haystack, "mis datos", "account", "cuenta", "private data"))
            Add(signals, BrowserCredentialSignalKind.SensitiveAccountAreaDetected, BrowserCredentialRisk.High, frameId, elementId, haystack, "sensitive account area detected");
    }

    private static void Add(List<BrowserCredentialSignal> signals, BrowserCredentialSignalKind kind, BrowserCredentialRisk risk, string frameId, string elementId, string evidence, string reason)
    {
        if (signals.Any(signal => signal.Kind == kind && signal.ElementId == elementId))
            return;

        signals.Add(new BrowserCredentialSignal(kind, risk, frameId, elementId, BrowserCredentialRedactor.Redact(evidence), reason));
    }

    private static BrowserHumanHandoffReason MapReason(BrowserCredentialSignalKind kind) => kind switch
    {
        BrowserCredentialSignalKind.PasswordFieldDetected => BrowserHumanHandoffReason.PasswordRequired,
        BrowserCredentialSignalKind.OtpFieldDetected or BrowserCredentialSignalKind.TwoFactorPromptDetected => BrowserHumanHandoffReason.TwoFactorRequired,
        BrowserCredentialSignalKind.CaptchaDetected => BrowserHumanHandoffReason.CaptchaRequired,
        BrowserCredentialSignalKind.ClaveFiscalDetected => BrowserHumanHandoffReason.ClaveFiscalRequired,
        BrowserCredentialSignalKind.PaymentActionDetected => BrowserHumanHandoffReason.FinancialActionRequired,
        BrowserCredentialSignalKind.CredentialSubmitDetected => BrowserHumanHandoffReason.SensitiveSubmitRequired,
        BrowserCredentialSignalKind.UnknownSensitivePrompt => BrowserHumanHandoffReason.UnknownSensitivePrompt,
        _ => BrowserHumanHandoffReason.LoginRequired
    };

    private static int SignalPriority(BrowserCredentialSignalKind kind) => kind switch
    {
        BrowserCredentialSignalKind.PaymentActionDetected => 100,
        BrowserCredentialSignalKind.CredentialSubmitDetected => 90,
        BrowserCredentialSignalKind.PasswordFieldDetected => 80,
        BrowserCredentialSignalKind.OtpFieldDetected or BrowserCredentialSignalKind.TwoFactorPromptDetected => 70,
        BrowserCredentialSignalKind.CaptchaDetected => 60,
        BrowserCredentialSignalKind.ClaveFiscalDetected => 50,
        BrowserCredentialSignalKind.TokenPromptDetected => 40,
        BrowserCredentialSignalKind.LoginFormDetected => 20,
        _ => 0
    };

    private static bool ContainsAny(string value, params string[] needles) =>
        needles.Any(needle => value.Contains(needle, StringComparison.OrdinalIgnoreCase));
}

public sealed class BrowserHumanHandoffCoordinator
{
    private readonly ConcurrentDictionary<string, bool> _completedHandoffs = new(StringComparer.Ordinal);

    public BrowserHumanHandoffRequest CreateApprovalRequest(
        BrowserAction action,
        string correlationId,
        string profileId,
        string sessionId,
        string message,
        TimeSpan? expiresAfter = null) =>
        CreateRequestCore(
            BrowserHumanHandoffReason.UserConfirmationRequired,
            action,
            correlationId,
            profileId,
            sessionId,
            message,
            expiresAfter);

    public BrowserHumanHandoffRequest CreateRequest(
        BrowserCredentialBoundaryDecision decision,
        BrowserAction action,
        string correlationId,
        string profileId,
        string sessionId,
        TimeSpan? expiresAfter = null)
    {
        var reason = decision.HandoffReason ?? BrowserHumanHandoffReason.AutomationBlocked;
        return CreateRequestCore(reason, action, correlationId, profileId, sessionId, decision.Message, expiresAfter);
    }

    private BrowserHumanHandoffRequest CreateRequestCore(
        BrowserHumanHandoffReason reason,
        BrowserAction action,
        string correlationId,
        string profileId,
        string sessionId,
        string message,
        TimeSpan? expiresAfter)
    {
        var now = DateTimeOffset.UtcNow;
        var expires = now + (expiresAfter ?? TimeSpan.FromMinutes(10));
        var requestId = $"handoff-{Guid.NewGuid():N}";

        return new BrowserHumanHandoffRequest(
            RequestId: requestId,
            Status: BrowserHumanHandoffStatus.Created,
            Reason: reason,
            Context: new BrowserHumanHandoffContext(
                RunId: action.RunId,
                ActionId: action.ActionId,
                CorrelationId: correlationId,
                ProfileId: profileId,
                SessionId: sessionId,
                TargetContext: action.TargetContext,
                FrameId: action.FrameId,
                RedactedUrl: BrowserCredentialRedactor.Redact(action.TargetContext.Url.ToString()),
                RedactedTitle: BrowserCredentialRedactor.Redact(action.TargetContext.Title),
                RedactionApplied: true),
            ResumeToken: new BrowserHumanHandoffResumeToken($"resume-{Guid.NewGuid():N}", requestId, correlationId, now, expires),
            Instruction: new BrowserHumanHandoffInstruction(
                Message: string.IsNullOrWhiteSpace(message) ? InstructionFor(reason) : BrowserCredentialRedactor.Redact(message),
                AllowedUserActions: AllowedActionsFor(reason),
                ResumeConditions: ["user completed the sensitive step", "browser target remains alive", "post-handoff observation verifies expected non-secret state"]),
            CreatedAtUtc: now,
            ExpiresAtUtc: expires,
            RedactionApplied: true,
            EvidenceRefs: [$"handoff:{requestId}:created"],
            ProofRefs: [$"proof:handoff:{requestId}:created"]);
    }

    public BrowserHumanHandoffResumeDecision TryResume(
        BrowserHumanHandoffRequest request,
        BrowserHumanHandoffStatus userStatus,
        BrowserTargetContext observedTarget,
        BrowserSessionDescriptor session,
        BrowserVerification? postHandoffVerification,
        DateTimeOffset now)
    {
        if (_completedHandoffs.TryGetValue(request.RequestId, out _))
            return Decision(false, true, BrowserHumanHandoffStatus.Resumed, "handoff already completed (idempotent)");

        if (request.ResumeToken.IsExpired(now) || userStatus == BrowserHumanHandoffStatus.Expired)
            return Decision(false, false, BrowserHumanHandoffStatus.Expired, "handoff expired");
        if (userStatus == BrowserHumanHandoffStatus.UserCancelled)
            return Decision(false, false, BrowserHumanHandoffStatus.UserCancelled, "user cancelled handoff");
        if (!session.IsAlive(now))
            return Decision(false, false, BrowserHumanHandoffStatus.Failed, "browser session is not alive");
        if (request.Context.TargetContext.IsStaleComparedTo(observedTarget))
            return Decision(false, false, BrowserHumanHandoffStatus.Failed, "target context is stale after handoff");
        if (userStatus != BrowserHumanHandoffStatus.UserCompleted)
            return Decision(false, false, BrowserHumanHandoffStatus.WaitingForUser, "waiting for user completion");
        if (postHandoffVerification is null || postHandoffVerification.Status != BrowserVerificationStatus.Verified || !postHandoffVerification.HasSemanticProof)
            return Decision(false, false, BrowserHumanHandoffStatus.UserCompleted, "user completed is not success without re-observation and verification");

        _completedHandoffs.TryAdd(request.RequestId, true);

        return new BrowserHumanHandoffResumeDecision(
            CanResume: true,
            Success: true,
            Status: BrowserHumanHandoffStatus.Resumed,
            Reason: "handoff completed and verified",
            EvidenceRefs: [$"handoff:{request.RequestId}:resumed", .. postHandoffVerification.EvidenceRefs],
            ProofRefs: [$"proof:handoff:{request.RequestId}:resumed", .. postHandoffVerification.ProofReferences]);
    }

    private static BrowserHumanHandoffResumeDecision Decision(bool canResume, bool success, BrowserHumanHandoffStatus status, string reason) =>
        new(canResume, success, status, reason, [], []);

    private static string InstructionFor(BrowserHumanHandoffReason reason) => reason switch
    {
        BrowserHumanHandoffReason.PasswordRequired => "Ingrese la contraseña manualmente. ONE BRAIN no captura ni escribe contraseñas.",
        BrowserHumanHandoffReason.TwoFactorRequired => "Complete el segundo factor manualmente. ONE BRAIN no captura códigos.",
        BrowserHumanHandoffReason.CaptchaRequired => "Resuelva el CAPTCHA manualmente. ONE BRAIN no intenta resolverlo.",
        BrowserHumanHandoffReason.ClaveFiscalRequired => "Complete la clave fiscal manualmente. ONE BRAIN no captura claves fiscales.",
        BrowserHumanHandoffReason.FinancialActionRequired => "Revise la acción financiera manualmente. La automatización queda detenida.",
        _ => "Complete la intervención sensible manualmente. ONE BRAIN retomará sólo después de re-observar y verificar."
    };

    private static IReadOnlyList<string> AllowedActionsFor(BrowserHumanHandoffReason reason) => reason switch
    {
        BrowserHumanHandoffReason.CaptchaRequired => ["solve captcha manually", "cancel"],
        BrowserHumanHandoffReason.TwoFactorRequired => ["enter 2FA manually", "cancel"],
        BrowserHumanHandoffReason.PasswordRequired or BrowserHumanHandoffReason.LoginRequired => ["enter credentials manually", "cancel"],
        _ => ["complete requested sensitive step manually", "cancel"]
    };
}
