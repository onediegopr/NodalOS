using System.Security.Cryptography;
using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.BrowserExecutor.Cdp;

// M197 — PaddleOCR ONNX model integrity checker.
// Verifies file existence, size, and checksum without executing inference.
public sealed class NodalOsPaddleOcrOnnxModelIntegrityCheckerService
{
    public NodalOsPaddleOcrOnnxModelIntegrityCheck Check(string modelId, string filePath, string expectedAlgorithm, string expectedChecksum, long expectedSizeBytes)
    {
        if (!File.Exists(filePath))
        {
            return IntegrityResult(modelId, expectedAlgorithm, expectedChecksum, null, expectedSizeBytes, 0, false, "file not found");
        }

        var actualSize = new FileInfo(filePath).Length;
        if (actualSize != expectedSizeBytes)
        {
            return IntegrityResult(modelId, expectedAlgorithm, expectedChecksum, null, expectedSizeBytes, actualSize, false, "size mismatch");
        }

        if (string.IsNullOrWhiteSpace(expectedChecksum))
        {
            return IntegrityResult(modelId, expectedAlgorithm, expectedChecksum, null, expectedSizeBytes, actualSize, false, "expected checksum missing");
        }

        try
        {
            using var stream = File.OpenRead(filePath);
            var actualChecksum = expectedAlgorithm.ToUpperInvariant() switch
            {
                "SHA-256" => Convert.ToHexStringLower(SHA256.HashData(stream)),
                "SHA-512" => Convert.ToHexStringLower(SHA512.HashData(stream)),
                _ => throw new NotSupportedException($"hash algorithm {expectedAlgorithm} not supported")
            };

            var match = actualChecksum.Equals(expectedChecksum, StringComparison.OrdinalIgnoreCase);
            return IntegrityResult(modelId, expectedAlgorithm, expectedChecksum, actualChecksum, expectedSizeBytes, actualSize, match, match ? "checksum matches" : "checksum mismatch");
        }
        catch (Exception ex)
        {
            return IntegrityResult(modelId, expectedAlgorithm, expectedChecksum, null, expectedSizeBytes, actualSize, false, $"hash failed: {ex.Message}");
        }
    }

    private static NodalOsPaddleOcrOnnxModelIntegrityCheck IntegrityResult(
        string modelId,
        string algorithm,
        string expectedChecksum,
        string? actualChecksum,
        long expectedSizeBytes,
        long actualSizeBytes,
        bool match,
        string reason)
    {
        return new NodalOsPaddleOcrOnnxModelIntegrityCheck(
            $"integrity-{Guid.NewGuid():N}",
            modelId,
            algorithm,
            BrowserCredentialRedactor.Redact(expectedChecksum),
            actualChecksum is null ? null : BrowserCredentialRedactor.Redact(actualChecksum),
            expectedSizeBytes,
            actualSizeBytes,
            match,
            BrowserCredentialRedactor.Redact(reason),
            NoAuthority: true,
            Redacted: true);
    }
}
