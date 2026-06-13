using System.Security.Cryptography;
using System.Text;
using OneBrain.Core.Models;

namespace OneBrain.Core.Identity;

public static class ElementFingerprintBuilder
{
    public static string Build(ElementIdentity identity)
    {
        ArgumentNullException.ThrowIfNull(identity);

        var canonical = string.Join("|",
        [
            Normalize(identity.RuntimeId),
            Normalize(identity.AutomationId),
            Normalize(identity.EffectiveControlType),
            Normalize(identity.Name),
            Normalize(identity.ClassName),
            Normalize(identity.AncestorPath),
            identity.SiblingIndex?.ToString() ?? "",
            Normalize(identity.ParentFingerprint),
            identity.Provenance.ToString()
        ]);

        if (canonical.Length == 0)
            return "";

        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(canonical));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    internal static string Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "";

        return string.Join(" ", value
            .Trim()
            .Split([' ', '\t', '\r', '\n'], StringSplitOptions.RemoveEmptyEntries))
            .ToLowerInvariant();
    }
}
