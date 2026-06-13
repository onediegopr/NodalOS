using System.Security.Cryptography;
using System.Text;

namespace OneBrain.Core.Execution;

public sealed record OwnershipSnapshot(
    ulong LastInputTick,
    long ForegroundHandle,
    string ForegroundTitle,
    DateTimeOffset CapturedAtUtc)
{
    public string Hash
    {
        get
        {
            var canonical = $"{LastInputTick}|{ForegroundHandle}|{ForegroundTitle}|{CapturedAtUtc.UtcTicks}";
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(canonical));
            return Convert.ToHexString(bytes).ToLowerInvariant();
        }
    }
}
