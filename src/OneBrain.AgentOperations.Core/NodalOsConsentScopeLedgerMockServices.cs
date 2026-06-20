using System.Text.Json;
using System.Text.Json.Serialization;
using OneBrain.AgentOperations.Contracts;

namespace OneBrain.AgentOperations.Core;

public sealed class NodalOsConsentScopeLedgerMockService
{
    private static readonly DateTimeOffset FixtureTime = new(2026, 06, 20, 00, 00, 00, TimeSpan.Zero);

    public NodalOsConsentScopeLedgerMock CreateLedger(
        IReadOnlyList<NodalOsCapabilityAccessGate> gates,
        string workspaceRef = "workspace-ref-m565",
        string missionRef = "mission-ref-m565") =>
        new()
        {
            LedgerId = "consent-scope-ledger-mock-m565",
            WorkspaceRef = workspaceRef,
            MissionRef = missionRef,
            IsMockOnly = true,
            UsesProductivePersistence = false,
            UsesRealFilesystem = false,
            CanPersistConsentProductively = false,
            CanAuthorizeCapability = false,
            CanAuthorizeFilesystemAccess = false,
            CanAuthorizeLlmContext = false,
            CanSendToCloud = false,
            Entries = gates.Select((gate, index) => CreateEntry(gate.Capability, index)).ToArray(),
            Result = CreateResult()
        };

    public NodalOsConsentScopeLedgerOperationResult ApplyOperation(NodalOsConsentScopeLedgerOperationKind operation) =>
        new()
        {
            OperationResultId = $"consent-scope-ledger-operation-{operation}",
            Operation = operation,
            IsNoOp = operation != NodalOsConsentScopeLedgerOperationKind.AddDraftEntry,
            IsMockOnly = true,
            UsesProductivePersistence = false,
            AuthorizesCapability = false,
            AuthorizesFilesystemAccess = false,
            AuthorizesLlmContext = false
        };

    private static NodalOsConsentScopeEntry CreateEntry(NodalOsOperationalCapability capability, int index) =>
        new()
        {
            EntryId = $"consent-scope-entry-{index + 1:000}-{capability}",
            Capability = capability,
            RequestedScopeRef = $"scope-ref-{capability}",
            ConsentStatus = NodalOsConsentScopeStatus.Draft,
            ScopeStatus = NodalOsConsentScopeStatus.Draft,
            FreshnessStatus = NodalOsConsentScopeStatus.Draft,
            RevocationStatus = NodalOsConsentScopeStatus.Draft,
            UserFacingPurposeRedacted = "Draft-only capability review.",
            RiskSummaryRedacted = "No productive authorization is represented by this entry.",
            CreatedAt = FixtureTime.AddMinutes(index),
            IsMockOnly = true,
            IsAuthoritative = false,
            CanAuthorizeRealUse = false
        };

    private static NodalOsConsentScopeLedgerResult CreateResult() =>
        new()
        {
            ResultId = "consent-scope-ledger-result-m565",
            ReadyForMockReview = true,
            ReadyForProductiveConsentLedger = false,
            ReadyForRealCapabilityAuthorization = false,
            ReadyForFilesystemAccess = false,
            ReadyForLlmContext = false,
            MissingRequirementsRedacted =
            [
                "Productive consent implementation.",
                "Consent revocation semantics.",
                "Audit before productive use.",
                "Fail-closed integration."
            ],
            UserFacingExplanationRedacted = "Ledger is mock-only and cannot authorize operational access."
        };
}

public sealed class NodalOsConsentScopeLedgerMockJsonSerializer
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
        WriteIndented = true
    };

    public string Serialize(NodalOsConsentScopeLedgerMock ledger) => JsonSerializer.Serialize(ledger, Options);
    public string SerializeOperation(NodalOsConsentScopeLedgerOperationResult result) => JsonSerializer.Serialize(result, Options);
}
