using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace OneBrain.Core.Approval;

public enum RedactionBeforePersistenceDecision
{
    Allowed,
    Rejected
}

public enum RedactionBeforePersistenceReason
{
    MissingPolicy,
    UnknownPolicyVersion,
    MissingCandidate,
    MalformedCandidate,
    MalformedMetadata,
    MalformedEvidenceReference,
    RawPayloadRejected,
    SecretLikeContentRejected,
    PiiLikeContentRejected,
    PathLikeContentRejected
}

public sealed record RedactionBeforePersistencePolicy(
    string PolicyId,
    string PolicyVersion,
    bool AllowRawPayload = false)
{
    public const string TestOnlyPolicyId = "redaction-before-persistence.test-only";
    public const string TestOnlyPolicyVersion = "v1";

    public static RedactionBeforePersistencePolicy TestOnly =>
        new(TestOnlyPolicyId, TestOnlyPolicyVersion);
}

public sealed record RedactionBeforePersistenceEvidence(
    string PolicyId,
    string PolicyVersion,
    string ClassifierVersion,
    string RedactorVersion,
    string CandidateHash,
    RedactionBeforePersistenceDecision Decision,
    IReadOnlyList<string> ClassificationCategories,
    IReadOnlyList<string> OmittedFieldKeys,
    IReadOnlyList<RedactionBeforePersistenceReason> ReasonCodes,
    bool CompletedBeforePersistence,
    bool ContainsRawValues);

public sealed record RedactionBeforePersistenceResult(
    RedactionBeforePersistenceDecision Decision,
    IReadOnlyList<RedactionBeforePersistenceReason> Reasons,
    DurableAuditTrailAppendOnlyMinimalRequest? SafeRequest,
    RedactionBeforePersistenceEvidence Evidence,
    bool Succeeded);

public sealed class RedactionBeforePersistenceService
{
    private const string ClassifierVersion = "redaction-before-persistence-classifier.test-only.v1";
    private const string RedactorVersion = "redaction-before-persistence-redactor.test-only.v1";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private static readonly Regex JwtLikePattern = new(
        @"\b[A-Za-z0-9_-]{10,}\.[A-Za-z0-9_-]{10,}\.[A-Za-z0-9_-]{10,}\b",
        RegexOptions.Compiled);
    private static readonly Regex OpenAiKeyLikePattern = new(
        @"\bsk-(proj-)?[A-Za-z0-9_-]{8,}\b",
        RegexOptions.Compiled);
    private static readonly Regex SecretAssignmentLikePattern = new(
        @"\b(password|token|secret|api[\s_-]?key)\s*[:=]",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex EmailLikePattern = new(
        @"\b[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}\b",
        RegexOptions.Compiled);
    private static readonly Regex WindowsAbsolutePathPattern = new(
        @"\b[A-Za-z]:\\[^:*?""<>|\r\n]+",
        RegexOptions.Compiled);

    public RedactionBeforePersistenceResult Evaluate(
        RedactionBeforePersistencePolicy policy,
        DurableAuditTrailAppendOnlyMinimalRequest request)
    {
        var reasons = new List<RedactionBeforePersistenceReason>();
        var policyId = policy?.PolicyId ?? string.Empty;
        var policyVersion = policy?.PolicyVersion ?? string.Empty;
        var candidateHash = ComputeCandidateHash(request);

        if (policy is null || string.IsNullOrWhiteSpace(policy.PolicyId))
        {
            reasons.Add(RedactionBeforePersistenceReason.MissingPolicy);
        }
        else if (!string.Equals(policy.PolicyId, RedactionBeforePersistencePolicy.TestOnlyPolicyId, StringComparison.Ordinal)
            || !string.Equals(policy.PolicyVersion, RedactionBeforePersistencePolicy.TestOnlyPolicyVersion, StringComparison.Ordinal))
        {
            reasons.Add(RedactionBeforePersistenceReason.UnknownPolicyVersion);
        }

        if (request is null)
        {
            reasons.Add(RedactionBeforePersistenceReason.MissingCandidate);
            return Rejected(policyId, policyVersion, candidateHash, reasons);
        }

        AddMalformedCandidateReasons(request, reasons);
        AddSensitiveContentReasons(policy, request, reasons);

        if (reasons.Count > 0)
        {
            return Rejected(policyId, policyVersion, candidateHash, reasons);
        }

        var safeRequest = request with
        {
            RawPayload = null,
            EvidenceReferences = request.EvidenceReferences.ToArray(),
            Metadata = request.Metadata!
                .OrderBy(pair => pair.Key, StringComparer.Ordinal)
                .ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal)
        };

        return new RedactionBeforePersistenceResult(
            Decision: RedactionBeforePersistenceDecision.Allowed,
            Reasons: [],
            SafeRequest: safeRequest,
            Evidence: Evidence(policyId, policyVersion, candidateHash, RedactionBeforePersistenceDecision.Allowed, [], []),
            Succeeded: true);
    }

    public static string ComputeCandidateHash(DurableAuditTrailAppendOnlyMinimalRequest? request)
    {
        if (request is null)
        {
            return HashText("missing-candidate");
        }

        var material = new
        {
            request.EventKind,
            request.ActorReference,
            request.ApprovalReference,
            EvidenceReferences = request.EvidenceReferences?.ToArray(),
            Metadata = request.Metadata?
                .OrderBy(pair => pair.Key, StringComparer.Ordinal)
                .Select(pair => new[] { pair.Key, pair.Value })
                .ToArray(),
            request.RawPayload
        };

        return HashText(JsonSerializer.Serialize(material, JsonOptions));
    }

    private static void AddMalformedCandidateReasons(
        DurableAuditTrailAppendOnlyMinimalRequest request,
        List<RedactionBeforePersistenceReason> reasons)
    {
        if (string.IsNullOrWhiteSpace(request.EventKind)
            || string.IsNullOrWhiteSpace(request.ActorReference)
            || string.IsNullOrWhiteSpace(request.ApprovalReference))
        {
            reasons.Add(RedactionBeforePersistenceReason.MalformedCandidate);
        }

        if (request.EvidenceReferences is null
            || request.EvidenceReferences.Count == 0
            || request.EvidenceReferences.Any(IsMalformedReference))
        {
            reasons.Add(RedactionBeforePersistenceReason.MalformedEvidenceReference);
        }

        if (request.Metadata is null
            || request.Metadata.Any(pair => string.IsNullOrWhiteSpace(pair.Key) || pair.Value is null))
        {
            reasons.Add(RedactionBeforePersistenceReason.MalformedMetadata);
        }
    }

    private static void AddSensitiveContentReasons(
        RedactionBeforePersistencePolicy? policy,
        DurableAuditTrailAppendOnlyMinimalRequest request,
        List<RedactionBeforePersistenceReason> reasons)
    {
        var values = new[]
            {
                request.EventKind,
                request.ActorReference,
                request.ApprovalReference,
                request.RawPayload
            }
            .Concat(request.EvidenceReferences ?? [])
            .Concat(request.Metadata?.Keys ?? [])
            .Concat(request.Metadata?.Values ?? [])
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .ToArray();

        if ((policy?.AllowRawPayload != true) && !string.IsNullOrWhiteSpace(request.RawPayload))
        {
            reasons.Add(RedactionBeforePersistenceReason.RawPayloadRejected);
        }

        if (values.Any(ContainsSecretLikeContent))
        {
            reasons.Add(RedactionBeforePersistenceReason.SecretLikeContentRejected);
        }

        if (values.Any(value => EmailLikePattern.IsMatch(value!)))
        {
            reasons.Add(RedactionBeforePersistenceReason.PiiLikeContentRejected);
        }

        if (values.Any(ContainsPathLikeContent))
        {
            reasons.Add(RedactionBeforePersistenceReason.PathLikeContentRejected);
        }
    }

    private static bool IsMalformedReference(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return true;
        }

        return Uri.TryCreate(value, UriKind.Absolute, out _);
    }

    private static RedactionBeforePersistenceResult Rejected(
        string policyId,
        string policyVersion,
        string candidateHash,
        IReadOnlyList<RedactionBeforePersistenceReason> reasons) =>
        new(
            Decision: RedactionBeforePersistenceDecision.Rejected,
            Reasons: reasons.Distinct().ToArray(),
            SafeRequest: null,
            Evidence: Evidence(policyId, policyVersion, candidateHash, RedactionBeforePersistenceDecision.Rejected, reasons.Distinct().ToArray(), ["candidate"]),
            Succeeded: false);

    private static RedactionBeforePersistenceEvidence Evidence(
        string policyId,
        string policyVersion,
        string candidateHash,
        RedactionBeforePersistenceDecision decision,
        IReadOnlyList<RedactionBeforePersistenceReason> reasons,
        IReadOnlyList<string> omittedFieldKeys) =>
        new(
            PolicyId: policyId,
            PolicyVersion: policyVersion,
            ClassifierVersion: ClassifierVersion,
            RedactorVersion: RedactorVersion,
            CandidateHash: candidateHash,
            Decision: decision,
            ClassificationCategories: reasons.Select(reason => reason.ToString()).OrderBy(reason => reason, StringComparer.Ordinal).ToArray(),
            OmittedFieldKeys: omittedFieldKeys,
            ReasonCodes: reasons,
            CompletedBeforePersistence: true,
            ContainsRawValues: false);

    private static bool ContainsPathLikeContent(string? value) =>
        !string.IsNullOrWhiteSpace(value)
        && (WindowsAbsolutePathPattern.IsMatch(value)
            || value.TrimStart().StartsWith(@"\\", StringComparison.Ordinal));

    private static bool ContainsSecretLikeContent(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return false;
        }

        var lowered = value.ToLowerInvariant();
        return SecretAssignmentLikePattern.IsMatch(value)
            || lowered.Contains("password=", StringComparison.Ordinal)
            || lowered.Contains("token=", StringComparison.Ordinal)
            || lowered.Contains("secret=", StringComparison.Ordinal)
            || lowered.Contains("api_key", StringComparison.Ordinal)
            || lowered.Contains("apikey", StringComparison.Ordinal)
            || lowered.Contains("api-key", StringComparison.Ordinal)
            || lowered.Contains("bearer ", StringComparison.Ordinal)
            || lowered.Contains("ghp_", StringComparison.Ordinal)
            || lowered.Contains("github_pat_", StringComparison.Ordinal)
            || lowered.Contains("user id=", StringComparison.Ordinal)
            || lowered.Contains("accountkey=", StringComparison.Ordinal)
            || lowered.Contains("sharedaccesskey=", StringComparison.Ordinal)
            || lowered.Contains("defaultendpointsprotocol=", StringComparison.Ordinal)
            || lowered.Contains("authorization:", StringComparison.Ordinal)
            || lowered.Contains("cookie:", StringComparison.Ordinal)
            || lowered.Contains("begin private key", StringComparison.Ordinal)
            || lowered.Contains("begin rsa private key", StringComparison.Ordinal)
            || lowered.Contains("begin openssh private key", StringComparison.Ordinal)
            || JwtLikePattern.IsMatch(value)
            || OpenAiKeyLikePattern.IsMatch(value);
    }

    private static string HashText(string material)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(material));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
