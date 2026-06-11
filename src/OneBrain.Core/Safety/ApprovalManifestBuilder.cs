using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace OneBrain.Core.Safety;

/// <summary>Pure builder: generates an approval manifest from a preflight result. No side effects.</summary>
public static class ApprovalManifestBuilder
{
    public const string PolicyVersion = "approval-v2";

    public static ApprovalManifest Build(ClickPreflightResult preflightResult, string mode = "commercialWeb")
    {
        var normalizedMode = NormalizeMode(mode);
        var allowExecution = IsExecutionAllowed(preflightResult, normalizedMode);
        var evidenceHash = ComputeEvidenceHash(
            preflightResult.TargetText,
            normalizedMode,
            preflightResult.Decision,
            preflightResult.RiskCategory,
            preflightResult.RiskLevel,
            PolicyVersion);

        var manifest = new Dictionary<string, object>
        {
            ["policyVersion"] = PolicyVersion,
            ["actionType"] = "click",
            ["targetText"] = preflightResult.TargetText,
            ["mode"] = normalizedMode,
            ["siteMode"] = normalizedMode,
            ["decision"] = preflightResult.Decision,
            ["riskCategory"] = preflightResult.RiskCategory,
            ["riskLevel"] = preflightResult.RiskLevel,
            ["allowed"] = preflightResult.Allowed,
            ["blocked"] = preflightResult.Blocked,
            ["requiresApproval"] = preflightResult.RequiresApproval,
            ["requiresReview"] = preflightResult.RequiresReview,
            ["reason"] = preflightResult.Reason,
            ["executionAllowedInThisHito"] = allowExecution,
            ["evidenceHash"] = evidenceHash
        };

        if (!string.IsNullOrWhiteSpace(preflightResult.NearbyDangerousSignalsJson))
        {
            try
            {
                manifest["nearbyDangerousSignals"] = JsonSerializer.Deserialize<JsonElement>(preflightResult.NearbyDangerousSignalsJson);
            }
            catch { }
        }

        var manifestJson = JsonSerializer.Serialize(manifest);

        var human = $"[APPROVAL-V2] {preflightResult.TargetText}: {preflightResult.Decision.ToUpperInvariant()} " +
                    $"({preflightResult.RiskCategory}, {preflightResult.RiskLevel}, mode={normalizedMode}). " +
                    $"Executable: {allowExecution}. {preflightResult.Reason}";

        return new ApprovalManifest
        {
            Required = preflightResult.RequiresApproval,
            Allowed = preflightResult.Allowed,
            Blocked = preflightResult.Blocked,
            TargetText = preflightResult.TargetText,
            Mode = normalizedMode,
            Decision = preflightResult.Decision,
            RiskCategory = preflightResult.RiskCategory,
            RiskLevel = preflightResult.RiskLevel,
            EvidenceHash = evidenceHash,
            Title = $"Click Preflight: {preflightResult.TargetText}",
            Summary = preflightResult.Summary,
            Reason = preflightResult.Reason,
            ManifestJson = manifestJson,
            HumanReadableText = human,
            PolicyVersion = ApprovalManifestBuilder.PolicyVersion,
            ExecutionAllowedInThisHito = allowExecution
        };
    }

    public static ApprovalManifest BuildFromEvidence(string? evidenceJson, string mode = "commercialWeb")
    {
        if (string.IsNullOrWhiteSpace(evidenceJson))
            return new ApprovalManifest { Summary = "no preflight evidence" };

        ClickPreflightResult? pr = null;
        try
        {
            var doc = JsonSerializer.Deserialize<JsonElement>(evidenceJson);
            pr = new ClickPreflightResult
            {
                TargetText = doc.TryGetProperty("targetText", out var tt) ? tt.GetString() ?? "" : "",
                Decision = doc.TryGetProperty("decision", out var d) ? d.GetString() ?? "unknown" : "unknown",
                RiskCategory = ReadString(doc, "riskCategory", "category", "unknown"),
                RiskLevel = doc.TryGetProperty("riskLevel", out var rl) ? rl.GetString() ?? "unknown" : "unknown",
                Allowed = doc.TryGetProperty("allowed", out var a) && a.GetBoolean(),
                Blocked = doc.TryGetProperty("blocked", out var b) && b.GetBoolean(),
                RequiresApproval = doc.TryGetProperty("requiresApproval", out var ra) && ra.GetBoolean(),
                RequiresReview = doc.TryGetProperty("requiresReview", out var rrw) && rrw.GetBoolean(),
                Reason = doc.TryGetProperty("reason", out var rr) ? rr.GetString() ?? "" : "",
                NearbyDangerousSignalsJson = doc.TryGetProperty("nearbyDangerousSignalsJson", out var ns) ? ns.GetRawText() : null,
            };
        }
        catch { }

        if (pr == null) return new ApprovalManifest { Summary = "failed to parse preflight evidence" };

        return Build(pr, mode);
    }

    public static string ComputeEvidenceHash(
        string targetText,
        string mode,
        string decision,
        string riskCategory,
        string riskLevel,
        string policyVersion)
    {
        var canonical = string.Join("|",
            targetText.Trim(),
            NormalizeMode(mode),
            decision.Trim(),
            riskCategory.Trim(),
            riskLevel.Trim(),
            policyVersion.Trim());
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(canonical));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private static bool IsExecutionAllowed(ClickPreflightResult preflightResult, string mode)
    {
        if (mode is not ("controlled" or "nonCommercialWeb"))
            return false;
        if (preflightResult.Blocked || preflightResult.RequiresReview)
            return false;
        return preflightResult.Decision == "allowedForFuture" ||
               (preflightResult.Decision == "requiresApproval" &&
                preflightResult.RiskCategory == "navigation-candidate");
    }

    private static string NormalizeMode(string mode)
    {
        return string.IsNullOrWhiteSpace(mode) ? "commercialWeb" : mode.Trim();
    }

    private static string ReadString(JsonElement doc, string primary, string fallback, string defaultValue)
    {
        if (doc.TryGetProperty(primary, out var p))
            return p.GetString() ?? defaultValue;
        if (doc.TryGetProperty(fallback, out var f))
            return f.GetString() ?? defaultValue;
        return defaultValue;
    }
}

public sealed class ApprovalManifest
{
    public bool Required { get; init; }
    public bool Allowed { get; init; }
    public bool Blocked { get; init; }
    public string TargetText { get; init; } = "";
    public string Mode { get; init; } = "";
    public string Decision { get; init; } = "";
    public string RiskCategory { get; init; } = "";
    public string RiskLevel { get; init; } = "";
    public string EvidenceHash { get; init; } = "";
    public string Title { get; init; } = "";
    public string Summary { get; init; } = "";
    public string Reason { get; init; } = "";
    public string? ManifestJson { get; init; }
    public string HumanReadableText { get; init; } = "";
    public string PolicyVersion { get; init; } = "";
    public bool ExecutionAllowedInThisHito { get; init; }
}
