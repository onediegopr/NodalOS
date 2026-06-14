using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using OneBrain.Core.Approval;
using OneBrain.Core.Identity;
using OneBrain.Core.Models;
using OneBrain.Core.Selectors;

namespace OneBrain.Core.Safety;

/// <summary>Pure builder: generates an approval manifest from a preflight result. No side effects.</summary>
public static class ApprovalManifestBuilder
{
    public const string PolicyVersion = "approval-v2";
    public const string IdentitySchemaVersion = "approval-v3";

    public static ApprovalManifest Build(ClickPreflightResult preflightResult, string mode = "commercialWeb")
    {
        return Build(preflightResult, mode, identityInput: null);
    }

    public static ApprovalManifest Build(
        ClickPreflightResult preflightResult,
        string mode,
        ApprovedIdentityInput? identityInput)
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

        var identityMetadata = BuildIdentityMetadata(identityInput, normalizedMode);

        if (!string.IsNullOrWhiteSpace(preflightResult.NearbyDangerousSignalsJson))
        {
            try
            {
                manifest["nearbyDangerousSignals"] = JsonSerializer.Deserialize<JsonElement>(preflightResult.NearbyDangerousSignalsJson);
            }
            catch { }
        }

        if (identityMetadata != null)
        {
            manifest["identitySchemaVersion"] = identityMetadata.IdentitySchemaVersion;
            manifest["identityStrength"] = identityMetadata.IdentityStrength.ToString();

            if (!string.IsNullOrWhiteSpace(identityMetadata.ApprovedIdentityDigest))
                manifest["approvedIdentityDigest"] = identityMetadata.ApprovedIdentityDigest;

            if (identityMetadata.ApprovedSelector != null)
                manifest["approvedSelector"] = JsonSerializer.SerializeToElement(identityMetadata.ApprovedSelector);

            if (!string.IsNullOrWhiteSpace(identityMetadata.IdentitySource))
                manifest["identitySource"] = identityMetadata.IdentitySource;

            if (identityMetadata.ShadowAgreesWithLegacy.HasValue)
                manifest["shadowAgreesWithLegacy"] = identityMetadata.ShadowAgreesWithLegacy.Value;

            if (!string.IsNullOrWhiteSpace(identityMetadata.IdentityBindingHash))
                manifest["identityBindingHash"] = identityMetadata.IdentityBindingHash;
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
            ExecutionAllowedInThisHito = allowExecution,
            IdentitySchemaVersion = identityMetadata?.IdentitySchemaVersion,
            ApprovedIdentityDigest = identityMetadata?.ApprovedIdentityDigest,
            ApprovedSelector = identityMetadata?.ApprovedSelector,
            IdentityStrength = identityMetadata?.IdentityStrength ?? IdentityStrength.None,
            IdentitySource = identityMetadata?.IdentitySource,
            ShadowAgreesWithLegacy = identityMetadata?.ShadowAgreesWithLegacy,
            IdentityBindingHash = identityMetadata?.IdentityBindingHash
        };
    }

    public static ApprovalManifest BuildFromEvidence(string? evidenceJson, string mode = "commercialWeb")
    {
        return BuildFromEvidence(evidenceJson, mode, identityInput: null);
    }

    public static ApprovalManifest BuildFromEvidence(
        string? evidenceJson,
        string mode,
        ApprovedIdentityInput? identityInput)
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

        return Build(pr, mode, identityInput);
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

    public static string ComputeIdentityBindingHash(
        string approvedIdentityDigest,
        SelectorDefinition approvedSelector,
        string actionKind,
        string mode,
        string? identitySource,
        IdentityStrength identityStrength)
    {
        ArgumentNullException.ThrowIfNull(approvedSelector);

        var canonical = string.Join("|",
            approvedIdentityDigest.Trim(),
            CanonicalizeSelector(approvedSelector),
            actionKind.Trim(),
            NormalizeMode(mode),
            (identitySource ?? "").Trim(),
            identityStrength.ToString());

        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(canonical));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    public static ApprovalBinding? TryBuildApprovalBinding(ApprovalManifest manifest)
    {
        ArgumentNullException.ThrowIfNull(manifest);

        if (string.IsNullOrWhiteSpace(manifest.ApprovedIdentityDigest) ||
            manifest.ApprovedSelector == null ||
            string.IsNullOrWhiteSpace(manifest.IdentityBindingHash) ||
            string.IsNullOrWhiteSpace(manifest.EvidenceHash) ||
            string.IsNullOrWhiteSpace(manifest.PolicyVersion))
        {
            return null;
        }

        return new ApprovalBinding(
            ApprovalDecisionId: manifest.IdentityBindingHash,
            ApprovedIdentityDigest: manifest.ApprovedIdentityDigest,
            Selector: manifest.ApprovedSelector,
            ActionKind: "click",
            Mode: manifest.Mode,
            PolicyVersion: manifest.PolicyVersion,
            EvidenceHash: manifest.EvidenceHash);
    }

    public static ApprovalManifest AttachApprovedInputBinding(
        ApprovalManifest manifest,
        ApprovedInputBinding approvedInputBinding)
    {
        ArgumentNullException.ThrowIfNull(manifest);
        ArgumentNullException.ThrowIfNull(approvedInputBinding);

        var manifestNode = string.IsNullOrWhiteSpace(manifest.ManifestJson)
            ? new JsonObject()
            : JsonNode.Parse(manifest.ManifestJson)?.AsObject() ?? new JsonObject();

        manifestNode["approvedInputBindingVersion"] = approvedInputBinding.BindingVersion;
        manifestNode["approvedInputActionKind"] = approvedInputBinding.ActionKind;
        manifestNode["approvedValueDigest"] = approvedInputBinding.ApprovedValueDigest;
        manifestNode["approvedValueDigestAlgorithm"] = approvedInputBinding.ApprovedValueDigestAlgorithm;
        manifestNode["approvedValueCanonicalization"] = approvedInputBinding.ApprovedValueCanonicalization;
        manifestNode["approvedInputBindingHash"] = approvedInputBinding.ApprovedInputBindingHash;

        manifestNode["approvedInput"] = new JsonObject
        {
            ["bindingVersion"] = approvedInputBinding.BindingVersion,
            ["actionKind"] = approvedInputBinding.ActionKind,
            ["approvalRef"] = approvedInputBinding.ApprovalRef,
            ["identityBindingHash"] = approvedInputBinding.IdentityBindingHash,
            ["approvedValueDigest"] = approvedInputBinding.ApprovedValueDigest,
            ["approvedValueDigestAlgorithm"] = approvedInputBinding.ApprovedValueDigestAlgorithm,
            ["approvedValueCanonicalization"] = approvedInputBinding.ApprovedValueCanonicalization,
            ["approvedInputBindingHash"] = approvedInputBinding.ApprovedInputBindingHash
        };

        return manifest with
        {
            ManifestJson = manifestNode.ToJsonString(),
            ApprovedInputBinding = approvedInputBinding,
            ApprovedValueDigest = approvedInputBinding.ApprovedValueDigest,
            ApprovedInputBindingHash = approvedInputBinding.ApprovedInputBindingHash,
            ApprovedInputBindingVersion = approvedInputBinding.BindingVersion,
            ApprovedInputDigestAlgorithm = approvedInputBinding.ApprovedValueDigestAlgorithm,
            ApprovedInputCanonicalization = approvedInputBinding.ApprovedValueCanonicalization
        };
    }

    private static ApprovalIdentityMetadata? BuildIdentityMetadata(
        ApprovedIdentityInput? identityInput,
        string normalizedMode)
    {
        if (identityInput == null)
            return null;

        var identity = identityInput.Identity;
        var strength = ClassifyIdentityStrength(identity);
        var digest = strength == IdentityStrength.None || identity == null
            ? null
            : EmptyToNull(ElementFingerprintBuilder.Build(identity));

        var selector = strength == IdentityStrength.None || identity == null
            ? null
            : TryBuildApprovedSelector(identity);

        var source = EmptyToNull(identityInput.Source);
        var shadowAgrees = identityInput.Parity?.AgreesWithLegacy;
        var bindingHash = !string.IsNullOrWhiteSpace(digest) && selector != null
            ? ComputeIdentityBindingHash(digest, selector, "click", normalizedMode, source, strength)
            : null;

        return new ApprovalIdentityMetadata(
            IdentitySchemaVersion,
            digest,
            selector,
            strength,
            source,
            shadowAgrees,
            bindingHash);
    }

    private static IdentityStrength ClassifyIdentityStrength(ElementIdentity? identity)
    {
        if (identity == null)
            return IdentityStrength.None;

        if (identity.IsStrong)
            return IdentityStrength.Strong;

        return HasIdentityCriteria(identity)
            ? IdentityStrength.Weak
            : IdentityStrength.None;
    }

    private static bool HasIdentityCriteria(ElementIdentity identity)
    {
        return !string.IsNullOrWhiteSpace(identity.AutomationId) ||
               !string.IsNullOrWhiteSpace(identity.Name) ||
               !string.IsNullOrWhiteSpace(identity.HelpText) ||
               !string.IsNullOrWhiteSpace(identity.LegacyName) ||
               !string.IsNullOrWhiteSpace(identity.LegacyValue) ||
               !string.IsNullOrWhiteSpace(identity.LabeledByName) ||
               !string.IsNullOrWhiteSpace(identity.EffectiveControlType) ||
               !string.IsNullOrWhiteSpace(identity.ClassName) ||
               !string.IsNullOrWhiteSpace(identity.AncestorPath) ||
               !string.IsNullOrWhiteSpace(identity.BoundsHint) ||
               !string.IsNullOrWhiteSpace(identity.ParentFingerprint) ||
               identity.SiblingIndex.HasValue;
    }

    private static SelectorDefinition? TryBuildApprovedSelector(ElementIdentity identity)
    {
        var selector = SelectorEngine.GenerateSelector(identity);

        if (string.IsNullOrWhiteSpace(selector.Role) &&
            string.IsNullOrWhiteSpace(selector.Name) &&
            string.IsNullOrWhiteSpace(selector.AutomationId) &&
            string.IsNullOrWhiteSpace(selector.HelpText) &&
            string.IsNullOrWhiteSpace(selector.LegacyName) &&
            string.IsNullOrWhiteSpace(selector.ClassName) &&
            string.IsNullOrWhiteSpace(selector.AncestorPath))
        {
            return null;
        }

        return selector;
    }

    private static string CanonicalizeSelector(SelectorDefinition selector)
    {
        return string.Join("|",
            selector.SchemaVersion.ToString(),
            selector.Provenance.ToString(),
            selector.Role ?? "",
            selector.Name ?? "",
            selector.AutomationId ?? "",
            selector.HelpText ?? "",
            selector.LegacyName ?? "",
            selector.ClassName ?? "",
            selector.AncestorPath ?? "",
            selector.AppProfileAlias ?? "");
    }

    private static string? EmptyToNull(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value;

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

public sealed record ApprovalManifest
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
    public string? IdentitySchemaVersion { get; init; }
    public string? ApprovedIdentityDigest { get; init; }
    public SelectorDefinition? ApprovedSelector { get; init; }
    public IdentityStrength IdentityStrength { get; init; }
    public string? IdentitySource { get; init; }
    public bool? ShadowAgreesWithLegacy { get; init; }
    public string? IdentityBindingHash { get; init; }
    public ApprovedInputBinding? ApprovedInputBinding { get; init; }
    public string? ApprovedValueDigest { get; init; }
    public string? ApprovedInputBindingHash { get; init; }
    public string? ApprovedInputBindingVersion { get; init; }
    public string? ApprovedInputDigestAlgorithm { get; init; }
    public string? ApprovedInputCanonicalization { get; init; }
}
