using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.BrowserExecutor.Cdp;

public sealed class BrowserHumanHandoffCompanionAdapter
{
    public const string RuntimeKind = "core-governed-browser-runtime";
    public const string CompanionSource = "chrome-companion";
    public const string CoreSource = "core-browser-executor";

    public BrowserHumanHandoffPresentation CreatePresentation(BrowserHumanHandoffRequest request, BrowserHumanHandoffDisplayState? state = null)
    {
        var displayState = state ?? MapDisplayState(request.Status);
        return new BrowserHumanHandoffPresentation(
            HandoffId: request.RequestId,
            RunId: request.Context.RunId,
            ActionId: request.Context.ActionId,
            CorrelationId: request.Context.CorrelationId,
            Reason: request.Reason,
            Status: request.Status,
            DisplayState: displayState,
            SafeTitle: BrowserCredentialRedactor.Redact(request.Context.RedactedTitle),
            SafeUrl: RedactUrl(request.Context.RedactedUrl),
            Instruction: InstructionFor(request.Reason),
            ExpectedUserAction: ExpectedActionFor(request.Reason),
            AllowedOptions: AllowedOptions(displayState),
            BlockedOptions: BlockedOptionsFor(request.Reason),
            ExpiresAtUtc: request.ExpiresAtUtc,
            RedactionApplied: true,
            EvidenceRefs: request.EvidenceRefs,
            ProofRefs: request.ProofRefs,
            RuntimeKind: RuntimeKind,
            Source: CompanionSource,
            Authoritative: false);
    }

    public BrowserHumanHandoffProtocolEnvelope Created(BrowserHumanHandoffRequest request) =>
        Envelope("handoff.created", Event(BrowserHumanHandoffUiEventKind.HandoffCreated, request, false, null, request.EvidenceRefs, request.ProofRefs, "handoff created"), CreatePresentation(request));

    public BrowserHumanHandoffProtocolEnvelope UserCompleted(BrowserHumanHandoffRequest request) =>
        Envelope("handoff.userCompleted", Event(BrowserHumanHandoffUiEventKind.HandoffUserCompleted, request, false, null, [], [], "user completed signal accepted; verification pending"), CreatePresentation(request, BrowserHumanHandoffDisplayState.UserCompletedPendingVerification));

    public BrowserHumanHandoffProtocolEnvelope Cancelled(BrowserHumanHandoffRequest request) =>
        Envelope("handoff.cancelled", Event(BrowserHumanHandoffUiEventKind.HandoffCancelled, request, false, null, [], [], "user cancelled handoff"), CreatePresentation(request with { Status = BrowserHumanHandoffStatus.UserCancelled }, BrowserHumanHandoffDisplayState.Cancelled));

    public BrowserHumanHandoffProtocolEnvelope Expired(BrowserHumanHandoffRequest request) =>
        Envelope("handoff.expired", Event(BrowserHumanHandoffUiEventKind.HandoffExpired, request, false, null, [], [], "handoff expired"), CreatePresentation(request with { Status = BrowserHumanHandoffStatus.Expired }, BrowserHumanHandoffDisplayState.Expired));

    public BrowserHumanHandoffProtocolEnvelope ResumeRequested(BrowserHumanHandoffRequest request) =>
        Envelope("handoff.resumeRequested", Event(BrowserHumanHandoffUiEventKind.HandoffResumeRequested, request, false, null, [], [], "resume requested; core must re-observe and verify"), CreatePresentation(request, BrowserHumanHandoffDisplayState.UserCompletedPendingVerification));

    public BrowserHumanHandoffProtocolEnvelope ResumeResult(BrowserHumanHandoffRequest request, BrowserHumanHandoffResumeDecision decision, BrowserVerification? verification)
    {
        var verified = decision.Success && verification?.Status == BrowserVerificationStatus.Verified && verification.HasSemanticProof;
        var kind = verified ? BrowserHumanHandoffUiEventKind.HandoffResumeVerified : BrowserHumanHandoffUiEventKind.HandoffResumeRejected;
        var state = verified ? BrowserHumanHandoffDisplayState.Resumed : BrowserHumanHandoffDisplayState.Failed;
        var status = verified ? BrowserHumanHandoffStatus.Resumed : decision.Status;
        var presentation = CreatePresentation(request with { Status = status }, state);
        var eventEnvelope = Event(kind, request, verified, verification?.Status, decision.EvidenceRefs, decision.ProofRefs, decision.Reason);
        return Envelope(verified ? "handoff.resumeVerified" : "handoff.resumeRejected", eventEnvelope, presentation);
    }

    public BrowserHumanHandoffProtocolEnvelope Disconnected(BrowserHumanHandoffRequest request) =>
        Envelope("handoff.updated", Event(BrowserHumanHandoffUiEventKind.HandoffDisconnected, request, false, null, [], [], "companion disconnected; not done"), CreatePresentation(request, BrowserHumanHandoffDisplayState.Disconnected));

    private static BrowserHumanHandoffProtocolEnvelope Envelope(string messageType, BrowserHumanHandoffUiEvent uiEvent, BrowserHumanHandoffPresentation presentation) =>
        new(messageType, uiEvent, presentation, DateTimeOffset.UtcNow);

    private static BrowserHumanHandoffUiEvent Event(
        BrowserHumanHandoffUiEventKind kind,
        BrowserHumanHandoffRequest request,
        bool authoritative,
        BrowserVerificationStatus? verificationStatus,
        IReadOnlyList<string> evidenceRefs,
        IReadOnlyList<string> proofRefs,
        string diagnostics) =>
        new(
            Kind: kind,
            HandoffId: request.RequestId,
            RunId: request.Context.RunId,
            ActionId: request.Context.ActionId,
            CorrelationId: request.Context.CorrelationId,
            RuntimeKind: RuntimeKind,
            Source: authoritative ? CoreSource : CompanionSource,
            Authoritative: authoritative,
            VerificationStatus: verificationStatus,
            EvidenceRefs: evidenceRefs,
            ProofRefs: proofRefs,
            Redacted: true,
            Diagnostics: BrowserCredentialRedactor.Redact(diagnostics));

    private static BrowserHumanHandoffDisplayState MapDisplayState(BrowserHumanHandoffStatus status) => status switch
    {
        BrowserHumanHandoffStatus.Created or BrowserHumanHandoffStatus.WaitingForUser => BrowserHumanHandoffDisplayState.WaitingForUser,
        BrowserHumanHandoffStatus.UserCompleted => BrowserHumanHandoffDisplayState.UserCompletedPendingVerification,
        BrowserHumanHandoffStatus.UserCancelled => BrowserHumanHandoffDisplayState.Cancelled,
        BrowserHumanHandoffStatus.Expired => BrowserHumanHandoffDisplayState.Expired,
        BrowserHumanHandoffStatus.Failed => BrowserHumanHandoffDisplayState.Failed,
        BrowserHumanHandoffStatus.Resumed => BrowserHumanHandoffDisplayState.Resumed,
        _ => BrowserHumanHandoffDisplayState.Blocked
    };

    private static IReadOnlyList<BrowserHumanHandoffUserOption> AllowedOptions(BrowserHumanHandoffDisplayState state) => state switch
    {
        BrowserHumanHandoffDisplayState.WaitingForUser => [BrowserHumanHandoffUserOption.ContinueAfterUserAction, BrowserHumanHandoffUserOption.Cancel, BrowserHumanHandoffUserOption.CopyDiagnosticLog],
        BrowserHumanHandoffDisplayState.UserCompletedPendingVerification => [BrowserHumanHandoffUserOption.Cancel, BrowserHumanHandoffUserOption.CopyDiagnosticLog],
        BrowserHumanHandoffDisplayState.Cancelled or BrowserHumanHandoffDisplayState.Expired or BrowserHumanHandoffDisplayState.Failed or BrowserHumanHandoffDisplayState.Resumed or BrowserHumanHandoffDisplayState.Disconnected => [BrowserHumanHandoffUserOption.CopyDiagnosticLog],
        _ => [BrowserHumanHandoffUserOption.CopyDiagnosticLog]
    };

    private static IReadOnlyList<string> BlockedOptionsFor(BrowserHumanHandoffReason reason) => reason switch
    {
        BrowserHumanHandoffReason.CaptchaRequired => ["solve captcha automatically", "mark success without verification"],
        BrowserHumanHandoffReason.TwoFactorRequired => ["capture OTP", "mark success without verification"],
        BrowserHumanHandoffReason.PasswordRequired or BrowserHumanHandoffReason.LoginRequired => ["type password automatically", "store credentials", "mark success without verification"],
        BrowserHumanHandoffReason.FinancialActionRequired => ["confirm payment automatically", "mark success without verification"],
        _ => ["mark success without verification"]
    };

    private static string InstructionFor(BrowserHumanHandoffReason reason) => reason switch
    {
        BrowserHumanHandoffReason.PasswordRequired or BrowserHumanHandoffReason.LoginRequired =>
            "Se detectó un paso sensible: contraseña/login. NEXA se detuvo para que lo completes manualmente. Cuando termines, presioná \"Ya lo hice, continuar\". NEXA volverá a observar la página y sólo continuará si puede verificar el estado.",
        BrowserHumanHandoffReason.CaptchaRequired =>
            "Se detectó CAPTCHA o verificación anti-bot. NEXA no intentará resolverlo automáticamente. Resolvelo manualmente en el navegador y luego presioná \"Ya lo hice, continuar\".",
        BrowserHumanHandoffReason.TwoFactorRequired =>
            "Se detectó un paso de doble factor. Completá el código o aprobación desde tu dispositivo. Luego presioná \"Ya lo hice, continuar\".",
        BrowserHumanHandoffReason.ClaveFiscalRequired =>
            "Se detectó clave fiscal. NEXA se detuvo; completá el paso manualmente. NEXA no captura ni guarda claves fiscales.",
        BrowserHumanHandoffReason.FinancialActionRequired =>
            "Se detectó una acción financiera. NEXA se detuvo para revisión humana y no continuará sin verificación posterior.",
        _ =>
            "NEXA necesita intervención humana. Completá el paso manualmente y luego solicitá continuar; NEXA verificará antes de avanzar."
    };

    private static string ExpectedActionFor(BrowserHumanHandoffReason reason) => reason switch
    {
        BrowserHumanHandoffReason.PasswordRequired or BrowserHumanHandoffReason.LoginRequired => "Completar credenciales manualmente sin compartirlas con NEXA.",
        BrowserHumanHandoffReason.CaptchaRequired => "Resolver el CAPTCHA manualmente.",
        BrowserHumanHandoffReason.TwoFactorRequired => "Completar el segundo factor manualmente.",
        BrowserHumanHandoffReason.ClaveFiscalRequired => "Completar la clave fiscal manualmente.",
        BrowserHumanHandoffReason.FinancialActionRequired => "Revisar manualmente la acción financiera.",
        _ => "Completar el paso sensible manualmente."
    };

    private static string RedactUrl(string value)
    {
        var redacted = BrowserCredentialRedactor.Redact(value);
        if (!Uri.TryCreate(redacted, UriKind.Absolute, out var uri) || string.IsNullOrEmpty(uri.Query))
            return redacted;

        var builder = new UriBuilder(uri) { Query = "" };
        return builder.Uri.ToString();
    }
}
