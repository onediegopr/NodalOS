using System.Text.Json;

namespace OneBrain.Core.Safety;

/// <summary>Pure builder: generates an approval manifest from a preflight result. No side effects.</summary>
public static class ApprovalManifestBuilder
{
    public static ApprovalManifest Build(ClickPreflightResult preflightResult, string mode = "commercialWeb")
    {
        // executionAllowedInThisHito: only for controlled/nonCommercialWeb with safe targets
        var allowExecution = (mode == "controlled" || mode == "nonCommercialWeb") &&
            preflightResult.Decision is "allowedForFuture" or "requiresApproval" or "requiresReview";

        var manifest = new Dictionary<string, object>
        {
            ["policyVersion"] = "approval-v2",
            ["actionType"] = "click",
            ["targetText"] = preflightResult.TargetText,
            ["siteMode"] = "commercialWeb",
            ["decision"] = preflightResult.Decision,
            ["riskCategory"] = preflightResult.RiskCategory,
            ["riskLevel"] = preflightResult.RiskLevel,
            ["allowed"] = preflightResult.Allowed,
            ["blocked"] = preflightResult.Blocked,
            ["requiresApproval"] = preflightResult.RequiresApproval,
            ["requiresReview"] = preflightResult.RequiresReview,
            ["reason"] = preflightResult.Reason,
            ["executionAllowedInThisHito"] = allowExecution,
        };

        if (!string.IsNullOrWhiteSpace(preflightResult.NearbyDangerousSignalsJson))
        {
            try
            {
                manifest["nearbyDangerousSignals"] = JsonSerializer.Deserialize<object>(preflightResult.NearbyDangerousSignalsJson);
            }
            catch { }
        }

        var manifestJson = JsonSerializer.Serialize(manifest);

        var human = $"[APPROVAL-V2] {preflightResult.TargetText}: {preflightResult.Decision.ToUpperInvariant()} " +
                    $"({preflightResult.RiskCategory}, {preflightResult.RiskLevel}). " +
                    $"Executable: {allowExecution}. {preflightResult.Reason}";

        return new ApprovalManifest
        {
            Required = preflightResult.RequiresApproval,
            Allowed = preflightResult.Allowed,
            Blocked = preflightResult.Blocked,
            Title = $"Click Preflight: {preflightResult.TargetText}",
            Summary = preflightResult.Summary,
            Reason = preflightResult.Reason,
            ManifestJson = manifestJson,
            HumanReadableText = human,
            PolicyVersion = "approval-v2",
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
                RiskCategory = doc.TryGetProperty("riskCategory", out var rc) ? rc.GetString() ?? "unknown" : "unknown",
                RiskLevel = doc.TryGetProperty("riskLevel", out var rl) ? rl.GetString() ?? "unknown" : "unknown",
                Allowed = doc.TryGetProperty("allowed", out var a) && a.GetBoolean(),
                Blocked = doc.TryGetProperty("blocked", out var b) && b.GetBoolean(),
                RequiresApproval = doc.TryGetProperty("requiresApproval", out var ra) && ra.GetBoolean(),
                Reason = doc.TryGetProperty("reason", out var rr) ? rr.GetString() ?? "" : "",
                NearbyDangerousSignalsJson = doc.TryGetProperty("nearbyDangerousSignalsJson", out var ns) ? ns.GetRawText() : null,
            };
        }
        catch { }

        if (pr == null) return new ApprovalManifest { Summary = "failed to parse preflight evidence" };

        return Build(pr, mode);
    }
}

public sealed class ApprovalManifest
{
    public bool Required { get; init; }
    public bool Allowed { get; init; }
    public bool Blocked { get; init; }
    public string Title { get; init; } = "";
    public string Summary { get; init; } = "";
    public string Reason { get; init; } = "";
    public string? ManifestJson { get; init; }
    public string HumanReadableText { get; init; } = "";
    public string PolicyVersion { get; init; } = "";
    public bool ExecutionAllowedInThisHito { get; init; }
}
