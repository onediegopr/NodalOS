namespace OneBrain.Core.Detection.Evidence;

using OneBrain.Core.Detection.Contracts;

/// <summary>
/// Redactor de evidencia. Captura snapshot del DOM y redacta PII/valores sensibles.
/// NUNCA incluye valores de input, tokens, ni PII.
/// </summary>
public interface IEvidenceRedactor
{
    /// <summary>Captura snapshot DOM redactado. Retorna hash del snapshot.</summary>
    Task<string> CaptureAndRedactAsync(TargetContext ctx, CancellationToken ct = default);
}

/// <summary>Stub: retorna un hash determinista basado en la URL para tests.</summary>
public class DeterministicEvidenceRedactor : IEvidenceRedactor
{
    public Task<string> CaptureAndRedactAsync(TargetContext ctx, CancellationToken ct = default)
    {
        var content = ctx.Url ?? ctx.TargetSelector ?? "empty";
        var hash = Convert.ToHexStringLower(
            System.Security.Cryptography.SHA256.HashData(
                System.Text.Encoding.UTF8.GetBytes(content)));
        return Task.FromResult(hash);
    }
}

/// <summary>Stub: retorna siempre un valor fijo para tests que requieren FailureKind.</summary>
public class FixedEvidenceRedactor : IEvidenceRedactor
{
    private readonly string _hash;
    public FixedEvidenceRedactor(string hash) => _hash = hash;
    public Task<string> CaptureAndRedactAsync(TargetContext ctx, CancellationToken ct = default)
        => Task.FromResult(_hash);
}
