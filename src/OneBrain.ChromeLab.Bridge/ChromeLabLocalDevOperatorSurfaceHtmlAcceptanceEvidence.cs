namespace OneBrain.ChromeLab.Bridge;

public enum ChromeLabLocalDevOperatorSurfaceHtmlAcceptanceDecision
{
    Rejected,
    Accepted
}

public sealed record ChromeLabLocalDevOperatorSurfaceHtmlAcceptancePacket(
    string EvidenceId,
    ChromeLabLocalDevOperatorSurfaceHtmlAcceptanceDecision Decision,
    IReadOnlyList<string> Findings,
    int StatusCode,
    string ContentType,
    int ReadinessPercentage,
    IReadOnlyList<string> SectionIds,
    string? BlockedFrontier,
    string? RequiredOperatorSignal,
    bool LocalDevOnly,
    bool ReadOnly,
    bool FailClosed,
    bool ScriptsAbsent,
    bool FormsAbsent,
    bool ExternalResourcesAbsent,
    bool DisabledActionVisible,
    bool AcceptanceEvidenceVisible,
    string SafeNextStep);

public sealed class ChromeLabLocalDevOperatorSurfaceHtmlAcceptanceEvidence
{
    public const string EvidenceId = "chromelab.local-dev.operator-surface.html.acceptance.v1";

    private static readonly string[] RequiredSectionIds =
    [
        "status",
        "limits",
        "blockers",
        "operator-signal",
        "safe-next-step"
    ];

    public ChromeLabLocalDevOperatorSurfaceHtmlAcceptancePacket Evaluate(
        ChromeLabLocalDevOperatorSurfaceHtmlResult? result)
    {
        if (result is null)
            return MissingResult();

        var findings = new List<string>();
        var html = result.Html ?? string.Empty;
        var sectionIds = RequiredSectionIds
            .Where(id => html.Contains($"data-section-id=\"{id}\"", StringComparison.Ordinal))
            .ToArray();
        var blockedFrontier = Contains(html, "CHROMELAB_LIVE_BROWSER_EXECUTION_AUTHORITY")
            ? "CHROMELAB_LIVE_BROWSER_EXECUTION_AUTHORITY"
            : null;
        var requiredOperatorSignal = Contains(html, "explicit-chromelab-local-dev-frontier")
            ? "explicit-chromelab-local-dev-frontier"
            : null;
        var readinessVisible = Contains(html, "Readiness: <strong>27%</strong>");
        var disabledActionVisible =
            Contains(html, "aria-disabled=\"true\"") &&
            Contains(html, "Action unavailable in this read-only surface.");
        var acceptanceVisible = Contains(html, ChromeLabLocalDevOperatorSurfaceAcceptanceEvidence.EvidenceId);

        if (result.Decision != ChromeLabLocalDevOperatorSurfaceHtmlDecision.Rendered)
            findings.Add("html-view-not-rendered");
        if (result.StatusCode != 200)
            findings.Add("html-status-not-ok");
        if (!string.Equals(result.ContentType, ChromeLabLocalDevOperatorSurfaceHtmlRenderer.ContentType, StringComparison.Ordinal))
            findings.Add("html-content-type-mismatch");
        if (!result.LocalDevOnly || !result.ReadOnly || !result.FailClosed)
            findings.Add("html-boundary-mismatch");
        if (!result.ScriptsAbsent)
            findings.Add("scripts-must-stay-absent");
        if (!result.FormsAbsent)
            findings.Add("forms-must-stay-absent");
        if (!result.ExternalResourcesAbsent)
            findings.Add("external-resources-must-stay-absent");
        if (!readinessVisible)
            findings.Add("readiness-not-visible");
        if (sectionIds.Length != RequiredSectionIds.Length)
            findings.Add("required-section-missing");
        if (blockedFrontier is null)
            findings.Add("blocked-frontier-not-visible");
        if (requiredOperatorSignal is null)
            findings.Add("operator-signal-not-visible");
        if (!disabledActionVisible)
            findings.Add("disabled-action-not-visible");
        if (!acceptanceVisible)
            findings.Add("surface-acceptance-evidence-not-visible");
        if (!string.Equals(
                result.SafeNextStep,
                "CHROMELAB_LOCAL_DEV_OPERATOR_HTML_VIEW_ACCEPTANCE_OR_CLOSE",
                StringComparison.Ordinal))
            findings.Add("safe-next-step-mismatch");

        var accepted = findings.Count == 0;
        return new ChromeLabLocalDevOperatorSurfaceHtmlAcceptancePacket(
            EvidenceId: EvidenceId,
            Decision: accepted
                ? ChromeLabLocalDevOperatorSurfaceHtmlAcceptanceDecision.Accepted
                : ChromeLabLocalDevOperatorSurfaceHtmlAcceptanceDecision.Rejected,
            Findings: findings.OrderBy(value => value, StringComparer.Ordinal).ToArray(),
            StatusCode: result.StatusCode,
            ContentType: result.ContentType,
            ReadinessPercentage: readinessVisible ? 27 : 0,
            SectionIds: sectionIds,
            BlockedFrontier: blockedFrontier,
            RequiredOperatorSignal: requiredOperatorSignal,
            LocalDevOnly: result.LocalDevOnly,
            ReadOnly: result.ReadOnly,
            FailClosed: result.FailClosed,
            ScriptsAbsent: result.ScriptsAbsent,
            FormsAbsent: result.FormsAbsent,
            ExternalResourcesAbsent: result.ExternalResourcesAbsent,
            DisabledActionVisible: disabledActionVisible,
            AcceptanceEvidenceVisible: acceptanceVisible,
            SafeNextStep: result.SafeNextStep);
    }

    private static bool Contains(string source, string value) =>
        source.Contains(value, StringComparison.Ordinal);

    private static ChromeLabLocalDevOperatorSurfaceHtmlAcceptancePacket MissingResult() =>
        new(
            EvidenceId: EvidenceId,
            Decision: ChromeLabLocalDevOperatorSurfaceHtmlAcceptanceDecision.Rejected,
            Findings: ["missing-html-result"],
            StatusCode: 0,
            ContentType: ChromeLabLocalDevOperatorSurfaceHtmlRenderer.ContentType,
            ReadinessPercentage: 0,
            SectionIds: [],
            BlockedFrontier: null,
            RequiredOperatorSignal: null,
            LocalDevOnly: true,
            ReadOnly: true,
            FailClosed: true,
            ScriptsAbsent: true,
            FormsAbsent: true,
            ExternalResourcesAbsent: true,
            DisabledActionVisible: false,
            AcceptanceEvidenceVisible: false,
            SafeNextStep: "PROVIDE_VALID_CHROMELAB_LOCAL_DEV_OPERATOR_HTML_RESULT");
}
