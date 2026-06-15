namespace OneBrain.Core.Detection.Scoring;

using System.Security.Cryptography;
using System.Text;
using OneBrain.Core.Detection.Contracts;

/// <summary>
/// Motor de scoring heurístico. Aplica reglas explícitas sobre StructuralFeatures.
/// Misma entrada + misma config = mismos scores.
/// </summary>
public sealed class StateScoringEngine : IStateScoringEngine
{
    private readonly IReadOnlyList<IScoringRule> _rules;
    private readonly string _configHash;

    public string ConfigurationHash => _configHash;

    public StateScoringEngine(IEnumerable<IScoringRule> rules)
    {
        _rules = rules.ToList().AsReadOnly();
        _configHash = ComputeConfigHash(_rules);
    }

    public StateDetectionResult Score(StructuralFeatures structural, NetworkFeatures? network = null)
    {
        var vector = new StateVector();

        foreach (var rule in _rules)
        {
            var contribution = rule.Evaluate(structural);
            vector = Accumulate(vector, rule, contribution);
        }

        if (network?.BlockedStatusCode is not null)
        {
            vector = vector with { AntiBotScore = Math.Max(vector.AntiBotScore, 0.90) };
        }

        var (detected, confidence) = DetermineDominantState(vector);

        return new StateDetectionResult
        {
            DetectedState = detected,
            ConfidenceScore = confidence,
            Vector = vector,
            ScoringConfigHash = _configHash,
            DetectedAt = DateTimeOffset.UtcNow
        };
    }

    private static (InteractionState, double) DetermineDominantState(StateVector v)
    {
        var scores = new (InteractionState, double)[]
        {
            (InteractionState.CaptchaChallenge, v.CaptchaScore),
            (InteractionState.TwoFactorRequired, v.TwoFactorScore),
            (InteractionState.AntiBotBlock, v.AntiBotScore),
            (InteractionState.Loading, v.LoadingScore),
            (InteractionState.LayoutChanged, v.LayoutChangedScore),
            (InteractionState.ModalOverlay, v.ModalScore),
            (InteractionState.HoneypotDetected, v.HoneypotScore),
            (InteractionState.TimeoutOrHang, v.TimeoutScore)
        };

        var max = scores.MaxBy(s => s.Item2);
        return max;
    }

    private static StateVector Accumulate(StateVector v, IScoringRule rule, double contribution)
    {
        var weighted = Math.Min(1.0, contribution * rule.Weight);
        return rule.TargetState switch
        {
            InteractionState.CaptchaChallenge => v with { CaptchaScore = Math.Max(v.CaptchaScore, weighted) },
            InteractionState.TwoFactorRequired => v with { TwoFactorScore = Math.Max(v.TwoFactorScore, weighted) },
            InteractionState.AntiBotBlock => v with { AntiBotScore = Math.Max(v.AntiBotScore, weighted) },
            InteractionState.Loading => v with { LoadingScore = Math.Max(v.LoadingScore, weighted) },
            InteractionState.LayoutChanged => v with { LayoutChangedScore = Math.Max(v.LayoutChangedScore, weighted) },
            InteractionState.ModalOverlay => v with { ModalScore = Math.Max(v.ModalScore, weighted) },
            InteractionState.HoneypotDetected => v with { HoneypotScore = Math.Max(v.HoneypotScore, weighted) },
            InteractionState.TimeoutOrHang => v with { TimeoutScore = Math.Max(v.TimeoutScore, weighted) },
            _ => v
        };
    }

    private static string ComputeConfigHash(IReadOnlyList<IScoringRule> rules)
    {
        var content = string.Join("|", rules.Select(r => $"{r.RuleId}:{r.Weight}"));
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(content));
        return Convert.ToHexStringLower(hash);
    }
}
