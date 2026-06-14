using System.Security.Cryptography;
using System.Text;
using OneBrain.Core.Contracts;

namespace OneBrain.Core.Approval;

public sealed record ApprovedInputBinding(
    string BindingVersion,
    string ActionKind,
    string ApprovalRef,
    string IdentityBindingHash,
    string ApprovedValueDigest,
    string ApprovedValueDigestAlgorithm,
    string ApprovedValueCanonicalization,
    string ApprovedInputBindingHash);

public sealed record ApprovedInputBindingValidationResult(
    bool Success,
    FailureKind? FailureKind,
    string Reason,
    string ExpectedHash,
    string ActualHash);

public static class ApprovedInputBindingHashBuilder
{
    public const string BindingVersion = "approved-input-v1";
    public const string DigestAlgorithm = "SHA256";
    public const string Canonicalization = "utf8-raw-v1";

    public static ApprovedInputBinding Build(
        string actionKind,
        string approvalRef,
        string identityBindingHash,
        string approvedValueDigest)
    {
        Require(actionKind, nameof(actionKind));
        Require(approvalRef, nameof(approvalRef));
        Require(identityBindingHash, nameof(identityBindingHash));
        Require(approvedValueDigest, nameof(approvedValueDigest));

        var normalizedActionKind = actionKind.Trim().ToLowerInvariant();
        var normalizedApprovalRef = approvalRef.Trim();
        var normalizedIdentityBindingHash = identityBindingHash.Trim().ToLowerInvariant();
        var normalizedApprovedValueDigest = approvedValueDigest.Trim().ToLowerInvariant();
        var bindingHash = ComputeHash(
            normalizedActionKind,
            normalizedApprovalRef,
            normalizedIdentityBindingHash,
            normalizedApprovedValueDigest);

        return new ApprovedInputBinding(
            BindingVersion,
            normalizedActionKind,
            normalizedApprovalRef,
            normalizedIdentityBindingHash,
            normalizedApprovedValueDigest,
            DigestAlgorithm,
            Canonicalization,
            bindingHash);
    }

    public static string ComputeHash(
        string actionKind,
        string approvalRef,
        string identityBindingHash,
        string approvedValueDigest)
    {
        Require(actionKind, nameof(actionKind));
        Require(approvalRef, nameof(approvalRef));
        Require(identityBindingHash, nameof(identityBindingHash));
        Require(approvedValueDigest, nameof(approvedValueDigest));

        var canonical = string.Join("|",
            "onebrain.approved-input.v1",
            actionKind.Trim().ToLowerInvariant(),
            approvalRef.Trim(),
            identityBindingHash.Trim().ToLowerInvariant(),
            approvedValueDigest.Trim().ToLowerInvariant(),
            DigestAlgorithm,
            Canonicalization);

        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(canonical));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private static void Require(string value, string name)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException($"{name} is required", name);
    }
}

public static class ApprovedInputBindingValidator
{
    public static ApprovedInputBindingValidationResult Validate(
        ApprovedInputBinding? binding,
        string actionKind,
        string approvalRef,
        string identityBindingHash,
        string approvedValueDigest)
    {
        if (binding == null)
            return Denied("ApprovedInputBindingRequired");

        if (string.IsNullOrWhiteSpace(binding.ApprovedValueDigest) ||
            string.IsNullOrWhiteSpace(binding.ApprovedInputBindingHash) ||
            string.IsNullOrWhiteSpace(binding.IdentityBindingHash) ||
            string.IsNullOrWhiteSpace(binding.ApprovalRef))
        {
            return Denied("ApprovedInputBindingRequired");
        }

        if (!string.Equals(binding.BindingVersion, ApprovedInputBindingHashBuilder.BindingVersion, StringComparison.Ordinal))
            return Denied("ApprovedInputBindingVersionMismatch");

        if (!string.Equals(binding.ApprovedValueDigestAlgorithm, ApprovedInputBindingHashBuilder.DigestAlgorithm, StringComparison.Ordinal))
            return Denied("ApprovedInputDigestAlgorithmMismatch");

        if (!string.Equals(binding.ApprovedValueCanonicalization, ApprovedInputBindingHashBuilder.Canonicalization, StringComparison.Ordinal))
            return Denied("ApprovedInputCanonicalizationMismatch");

        if (!string.Equals(binding.ActionKind, actionKind.Trim().ToLowerInvariant(), StringComparison.Ordinal))
            return Denied("ApprovedInputActionKindMismatch");

        if (!string.Equals(binding.ApprovalRef, approvalRef.Trim(), StringComparison.Ordinal))
            return Denied("ApprovedInputApprovalRefMismatch");

        if (!string.Equals(binding.IdentityBindingHash, identityBindingHash.Trim().ToLowerInvariant(), StringComparison.Ordinal))
            return Denied("ApprovedInputIdentityBindingMismatch");

        if (!string.Equals(binding.ApprovedValueDigest, approvedValueDigest.Trim().ToLowerInvariant(), StringComparison.Ordinal))
            return Denied("ApprovedInputDigestMismatch");

        var expectedHash = ApprovedInputBindingHashBuilder.ComputeHash(
            actionKind,
            approvalRef,
            identityBindingHash,
            approvedValueDigest);

        if (!string.Equals(binding.ApprovedInputBindingHash, expectedHash, StringComparison.Ordinal))
            return new ApprovedInputBindingValidationResult(
                false,
                FailureKind.PolicyDenied,
                "ApprovedInputBindingHashMismatch",
                expectedHash,
                binding.ApprovedInputBindingHash);

        return new ApprovedInputBindingValidationResult(
            true,
            null,
            "ApprovedInputBindingValid",
            expectedHash,
            binding.ApprovedInputBindingHash);
    }

    private static ApprovedInputBindingValidationResult Denied(string reason) =>
        new(false, FailureKind.PolicyDenied, reason, "", "");
}
