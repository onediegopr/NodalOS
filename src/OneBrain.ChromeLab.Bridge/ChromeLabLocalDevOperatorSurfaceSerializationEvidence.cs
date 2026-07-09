using System.Text.Json;

namespace OneBrain.ChromeLab.Bridge;

public enum ChromeLabLocalDevOperatorSurfaceSerializationDecision
{
    Rejected,
    Accepted
}

public sealed record ChromeLabLocalDevOperatorSurfaceSerializationPacket(
    string EvidenceId,
    ChromeLabLocalDevOperatorSurfaceSerializationDecision Decision,
    IReadOnlyList<string> Findings,
    string RouteId,
    int PayloadLength,
    bool PayloadAvailable,
    bool NoSensitiveConfigurationFields,
    bool LocalDevOnly,
    bool LoopbackOnly,
    bool ReadOnly,
    bool FailClosed,
    bool CacheDisabled,
    string SafeNextStep);

public sealed class ChromeLabLocalDevOperatorSurfaceSerializationEvidence
{
    public const string EvidenceId = "chromelab.local-dev.operator-surface.serialization.acceptance.v1";

    private static readonly string[] ForbiddenFieldNames =
    [
        "apiKey",
        "connectionToken",
        "authorization",
        "password",
        "secret",
        "pairingToken"
    ];

    public ChromeLabLocalDevOperatorSurfaceSerializationPacket Evaluate(
        ChromeLabLocalDevOperatorSurfaceRouteResponse? response)
    {
        if (response is null)
            return MissingResponse();

        var payload = JsonSerializer.Serialize(response, ChromeLabProtocol.JsonOptions);
        var findings = new List<string>();

        if (response.Decision != ChromeLabLocalDevOperatorSurfaceRouteDecision.ServedReadOnlyPreview ||
            !response.PayloadAvailable)
            findings.Add("accepted-route-payload-required");
        if (!response.LocalDevOnly || !response.LoopbackOnly || !response.ReadOnly || !response.FailClosed)
            findings.Add("route-boundary-mismatch");
        if (!response.CacheDisabled)
            findings.Add("route-cache-must-stay-disabled");

        foreach (var forbidden in ForbiddenFieldNames)
        {
            if (payload.Contains($"\"{forbidden}\"", StringComparison.OrdinalIgnoreCase))
                findings.Add($"sensitive-field-present:{forbidden}");
        }

        var accepted = findings.Count == 0;
        return new ChromeLabLocalDevOperatorSurfaceSerializationPacket(
            EvidenceId: EvidenceId,
            Decision: accepted
                ? ChromeLabLocalDevOperatorSurfaceSerializationDecision.Accepted
                : ChromeLabLocalDevOperatorSurfaceSerializationDecision.Rejected,
            Findings: findings.OrderBy(value => value, StringComparer.Ordinal).ToArray(),
            RouteId: response.RouteId,
            PayloadLength: payload.Length,
            PayloadAvailable: response.PayloadAvailable,
            NoSensitiveConfigurationFields: findings.All(value =>
                !value.StartsWith("sensitive-field-present:", StringComparison.Ordinal)),
            LocalDevOnly: response.LocalDevOnly,
            LoopbackOnly: response.LoopbackOnly,
            ReadOnly: response.ReadOnly,
            FailClosed: response.FailClosed,
            CacheDisabled: response.CacheDisabled,
            SafeNextStep: accepted
                ? "CHROMELAB_LOCAL_DEV_OPERATOR_SURFACE_CLOSEOUT_OR_NEXT_FRONTIER"
                : "REMOVE_SENSITIVE_CONFIGURATION_FROM_OPERATOR_SURFACE_PAYLOAD");
    }

    private static ChromeLabLocalDevOperatorSurfaceSerializationPacket MissingResponse() =>
        new(
            EvidenceId: EvidenceId,
            Decision: ChromeLabLocalDevOperatorSurfaceSerializationDecision.Rejected,
            Findings: ["missing-route-response"],
            RouteId: string.Empty,
            PayloadLength: 0,
            PayloadAvailable: false,
            NoSensitiveConfigurationFields: true,
            LocalDevOnly: true,
            LoopbackOnly: true,
            ReadOnly: true,
            FailClosed: true,
            CacheDisabled: true,
            SafeNextStep: "PROVIDE_VALID_CHROMELAB_LOCAL_DEV_OPERATOR_SURFACE_ROUTE_RESPONSE");
}
