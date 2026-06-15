namespace OneBrain.Core.Detection.Audit;

using System.Security.Cryptography;
using System.Text;

/// <summary>
/// Utilidad para construir cadena HMAC de eventos de auditoría.
/// Cada evento encadena el hash del anterior.
/// </summary>
public static class AuditHashChain
{
    public static string ComputeEventHash(string previousHash, object evt, string secret)
    {
        var content = $"{previousHash}|{evt}";
        var key = Encoding.UTF8.GetBytes(secret);
        var data = Encoding.UTF8.GetBytes(content);
        var hmac = HMACSHA256.HashData(key, data);
        return Convert.ToHexStringLower(hmac);
    }

    public static StateDecisionEvent Link(StateDecisionEvent current, string previousHash, string secret)
    {
        var eventHash = ComputeEventHash(previousHash, current, secret);
        return current with { PreviousEventHash = previousHash, EventHash = eventHash };
    }
}
