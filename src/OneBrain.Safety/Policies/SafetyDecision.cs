namespace OneBrain.Safety.Policies;

public sealed record SafetyDecision(
    bool Allowed,
    string Reason);
