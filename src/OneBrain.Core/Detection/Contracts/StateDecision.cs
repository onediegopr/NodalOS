namespace OneBrain.Core.Detection.Contracts;

/// <summary>Tipo de decisión de política (Capa 3). Separado de InteractionState (percepción).</summary>
public enum StateDecisionType
{
    Proceed,
    Wait,
    RequiresHuman,
    TriggerSelectorRecovery,
    Abort
}

/// <summary>Decisión de política de seguridad para un estado detectado.</summary>
public record StateDecision
{
    public StateDecisionType Type { get; init; }
    public string ReasonCode { get; init; } = string.Empty;
    public string? Description { get; init; }
    public TimeSpan? WaitDuration { get; init; }

    public static StateDecision Proceed(string? reasonCode = null) => new()
    {
        Type = StateDecisionType.Proceed,
        ReasonCode = reasonCode ?? "P-PROCEED"
    };

    public static StateDecision Wait(TimeSpan duration, string? reasonCode = null) => new()
    {
        Type = StateDecisionType.Wait,
        WaitDuration = duration,
        ReasonCode = reasonCode ?? "P-WAIT"
    };

    public static StateDecision RequiresHuman(string reasonCode) => new()
    {
        Type = StateDecisionType.RequiresHuman,
        ReasonCode = reasonCode
    };

    public static StateDecision TriggerSelectorRecovery(string? reasonCode = null) => new()
    {
        Type = StateDecisionType.TriggerSelectorRecovery,
        ReasonCode = reasonCode ?? "P-SELECTOR-RECOVERY"
    };

    public static StateDecision Abort(string reasonCode) => new()
    {
        Type = StateDecisionType.Abort,
        ReasonCode = reasonCode
    };
}
