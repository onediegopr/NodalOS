using System.Security.Cryptography;
using System.Text;

namespace OneBrain.Core.Approval;

public static class ProductLedgerLocalAppendOnlyHashing
{
    public static string ComputeLedgerHash(IEnumerable<(int Sequence, string EntryHash)> entries)
    {
        var material = string.Join("\n", entries.Select(entry => $"{entry.Sequence}:{entry.EntryHash}"));
        return Sha256(material);
    }

    public static string ComputeEntryHash(
        int sequence,
        string candidateId,
        string safePayloadHash,
        IReadOnlyDictionary<string, string> metadata,
        string previousHash)
    {
        var metadataText = string.Join(
            "\n",
            metadata.OrderBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase)
                .Select(pair => $"{pair.Key}={pair.Value}"));
        return Sha256($"{sequence}\n{candidateId}\n{safePayloadHash.ToLowerInvariant()}\n{metadataText}\n{previousHash}");
    }

    private static string Sha256(string value)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
