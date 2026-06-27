namespace OneBrain.WindowsComputerUse;

public enum ComputerUseClaimStatus
{
    RequiredTrue,
    RequiredFalse,
    RequiredValue
}

public enum ComputerUseClaimDriftSeverity
{
    Info,
    Warning,
    Critical
}

public sealed record ComputerUseCanonicalClaim(
    string ClaimId,
    ComputerUseClaimStatus Status,
    string ExpectedValue,
    string SourceOfTruth,
    IReadOnlyList<string> ArtifactsThatMustMatch,
    IReadOnlyList<string> ForbiddenWording,
    string RegressionTest,
    string FailureBehavior);

public sealed record ComputerUseArtifactClaimSnapshot(
    string ArtifactId,
    string ArtifactKind,
    IReadOnlyDictionary<string, string> Claims,
    string TextRedacted,
    bool HistoricalOnly);

public sealed record ComputerUseClaimDriftFinding(
    string ArtifactId,
    string ClaimId,
    ComputerUseClaimDriftSeverity Severity,
    string Reason,
    bool BlocksLiveAdvance);

public sealed record ComputerUseClaimConsistencyResult(
    bool Consistent,
    IReadOnlyList<ComputerUseClaimDriftFinding> Findings,
    IReadOnlyList<string> BlockedClaims,
    IReadOnlyList<string> HistoricalOnlyClaims,
    IReadOnlyList<string> StaleArtifacts);

public static class ComputerUseClaimConsistencyCatalog
{
    public const string BlockedLivePrototypeStatus = "BLOCKED_PENDING_HUMAN_POLICY_DECISION_AND_EXTERNAL_GO";

    public static IReadOnlyList<ComputerUseCanonicalClaim> Claims { get; } =
    [
        Claim("contained_artifact", ComputerUseClaimStatus.RequiredTrue, "true", "WCU containment reports"),
        Claim("live_prototype_authorized", ComputerUseClaimStatus.RequiredFalse, "false", "WCU-037A external audit no-go reconciliation"),
        Claim("live_remains_blocked", ComputerUseClaimStatus.RequiredTrue, "true", "WCU-037A external audit no-go reconciliation"),
        Claim("current_code_defect_found", ComputerUseClaimStatus.RequiredFalse, "false", "WCU-037A external audit no-go reconciliation"),
        Claim("wcu_037_044_status", ComputerUseClaimStatus.RequiredValue, BlockedLivePrototypeStatus, "ComputerUseExternalAuditReconciliation.BlockedLivePrototypeStatus"),
        Claim("live_read_permitted", ComputerUseClaimStatus.RequiredFalse, "false", "ComputerUseReadOnlyLiveGateCatalog"),
        Claim("action_authority_granted", ComputerUseClaimStatus.RequiredFalse, "false", "WCU locator/evidence/handoff contracts"),
        Claim("product_automation_enabled", ComputerUseClaimStatus.RequiredFalse, "false", "WCU read-only live design gates"),
        Claim("browser_live_cdp_enabled", ComputerUseClaimStatus.RequiredFalse, "false", "WCU no-live containment policy"),
        Claim("safe_injection_live_enabled", ComputerUseClaimStatus.RequiredFalse, "false", "WCU no-live containment policy"),
        Claim("public_release_unlock", ComputerUseClaimStatus.RequiredFalse, "false", "WCU containment property catalog"),
        Claim("paid_beta_unlock", ComputerUseClaimStatus.RequiredFalse, "false", "WCU containment property catalog")
    ];

    public static ComputerUseClaimConsistencyResult Evaluate(IReadOnlyList<ComputerUseArtifactClaimSnapshot> artifacts)
    {
        var findings = new List<ComputerUseClaimDriftFinding>();
        var historical = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
        var stale = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var artifact in artifacts)
        {
            if (artifact.HistoricalOnly)
                historical.Add(artifact.ArtifactId);

            foreach (var claim in Claims)
            {
                if (!artifact.Claims.TryGetValue(claim.ClaimId, out var value))
                {
                    if (!artifact.HistoricalOnly)
                    {
                        stale.Add(artifact.ArtifactId);
                        findings.Add(new ComputerUseClaimDriftFinding(
                            artifact.ArtifactId,
                            claim.ClaimId,
                            ComputerUseClaimDriftSeverity.Warning,
                            "Current artifact omits canonical claim.",
                            BlocksLiveAdvance: true));
                    }

                    continue;
                }

                if (!Matches(claim, value))
                {
                    findings.Add(new ComputerUseClaimDriftFinding(
                        artifact.ArtifactId,
                        claim.ClaimId,
                        ComputerUseClaimDriftSeverity.Critical,
                        $"Claim value '{value}' does not match expected '{claim.ExpectedValue}'.",
                        BlocksLiveAdvance: true));
                }
            }

            foreach (var claim in Claims)
            {
                foreach (var wording in claim.ForbiddenWording)
                {
                    if (artifact.TextRedacted.Contains(wording, StringComparison.OrdinalIgnoreCase))
                    {
                        findings.Add(new ComputerUseClaimDriftFinding(
                            artifact.ArtifactId,
                            claim.ClaimId,
                            ComputerUseClaimDriftSeverity.Critical,
                            $"Forbidden wording present: {wording}.",
                            BlocksLiveAdvance: true));
                    }
                }
            }
        }

        return new ComputerUseClaimConsistencyResult(
            Consistent: findings.Count == 0,
            Findings: findings,
            BlockedClaims: Claims.Select(c => c.ClaimId).ToArray(),
            HistoricalOnlyClaims: historical.ToArray(),
            StaleArtifacts: stale.ToArray());
    }

    private static ComputerUseCanonicalClaim Claim(
        string id,
        ComputerUseClaimStatus status,
        string expected,
        string sourceOfTruth) =>
        new(
            id,
            status,
            expected,
            sourceOfTruth,
            ArtifactsThatMustMatch:
            [
                "latest report.json",
                "latest report.md",
                "latest handoff",
                "latest next prompt",
                "claim consistency matrix",
                "external audit reconciliation report"
            ],
            ForbiddenWording: ForbiddenWordingFor(id),
            RegressionTest: "WindowsComputerUseClaimConsistencyDrift",
            FailureBehavior: "Fail closed: mark drift critical, keep live prototype blocked, and do not grant action authority.");

    private static IReadOnlyList<string> ForbiddenWordingFor(string id) =>
        id switch
        {
            "live_prototype_authorized" => ["\"live_prototype_authorized\": true", "live prototype authorized: yes", "containment pass = live go"],
            "live_read_permitted" => ["\"LiveReadPermitted\": true", "\"live_read_permitted\": true", "safe to start live implementation"],
            "action_authority_granted" => ["\"ActionAuthorityGranted\": true", "\"action_authority_granted\": true", "high confidence = authorization", "evidence = authorization"],
            "product_automation_enabled" => ["\"ProductAutomationEnabled\": true", "\"product_automation_enabled\": true", "product automation ready", "desktop automation enabled"],
            "browser_live_cdp_enabled" => ["browser live enabled", "CDP live enabled", "\"browser_live_cdp_enabled\": true"],
            "safe_injection_live_enabled" => ["Safe Injection live enabled", "\"safe_injection_live_enabled\": true"],
            "public_release_unlock" => ["public release unlocked", "\"public_release_unlock\": true"],
            "paid_beta_unlock" => ["paid beta unlocked", "\"paid_beta_unlock\": true"],
            _ => []
        };

    private static bool Matches(ComputerUseCanonicalClaim claim, string value) =>
        claim.Status switch
        {
            ComputerUseClaimStatus.RequiredTrue => string.Equals(value, "true", StringComparison.OrdinalIgnoreCase),
            ComputerUseClaimStatus.RequiredFalse => string.Equals(value, "false", StringComparison.OrdinalIgnoreCase),
            ComputerUseClaimStatus.RequiredValue => string.Equals(value, claim.ExpectedValue, StringComparison.Ordinal),
            _ => false
        };
}
