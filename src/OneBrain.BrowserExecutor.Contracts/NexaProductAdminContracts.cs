namespace OneBrain.BrowserExecutor.Contracts;

public enum NexaProductAccountKind
{
    Person,
    Company
}

public enum NexaAccountStatus
{
    Free,
    Trial,
    Active,
    Suspended,
    Expired
}

public enum NexaRole
{
    Owner,
    Admin,
    Operator,
    Viewer,
    Worker,
    Support,
    Unknown
}

public static class NexaRolePolicy
{
    public static bool CanSatisfyMinimumRole(NexaRole actorRole, NexaRole minimumRole, bool supportMetadataOnly = false)
    {
        if (actorRole == NexaRole.Unknown || minimumRole == NexaRole.Unknown)
            return false;
        if (actorRole == NexaRole.Support)
            return supportMetadataOnly && minimumRole is NexaRole.Support or NexaRole.Viewer;
        if (minimumRole == NexaRole.Support)
            return actorRole == NexaRole.Support || actorRole == NexaRole.Owner;

        return Rank(actorRole) >= Rank(minimumRole);
    }

    public static bool CanMutate(NexaRole actorRole, bool supportMetadataOnly = false) =>
        actorRole is NexaRole.Owner or NexaRole.Admin or NexaRole.Operator ||
        (actorRole == NexaRole.Support && supportMetadataOnly);

    private static int Rank(NexaRole role) =>
        role switch
        {
            NexaRole.Owner => 5,
            NexaRole.Admin => 4,
            NexaRole.Operator => 3,
            NexaRole.Worker => 2,
            NexaRole.Viewer => 1,
            _ => 0
        };
}

public enum NexaAdminCapability
{
    ManageAccount,
    ManagePlan,
    ManageWorkers,
    ManageConfiguration,
    ExecuteAllowedFlows,
    ViewReadOnly,
    ViewAudit,
    SupportAccess,
    ViewSecrets,
    MutateSensitiveData
}

public enum NexaAdminAction
{
    ViewAccount,
    UpdateAccount,
    ManagePlan,
    AddWorker,
    RemoveWorker,
    UpdateWorker,
    ViewAudit,
    SupportInspect,
    ViewSecret,
    Unknown
}

public enum NexaAdminDecisionKind
{
    Allowed,
    Denied,
    FailClosed
}

public sealed record NexaOrganization(
    string OrganizationId,
    string DisplayName,
    NexaAccountStatus Status)
{
    public ContractValidationResult Validate()
    {
        var errors = new List<string>();
        BrowserSafeIdentifierValidator.RequireSafe(OrganizationId, nameof(OrganizationId), errors);
        if (BrowserCredentialRedactor.ContainsSecret(DisplayName))
            errors.Add("Organization display name contains secret-like content.");
        return errors.Count == 0 ? ContractValidationResult.Valid : new ContractValidationResult(false, errors);
    }
}

public sealed record NexaPersonAccount(
    string PersonId,
    string Email,
    NexaAccountStatus Status)
{
    public ContractValidationResult Validate()
    {
        var errors = new List<string>();
        BrowserSafeIdentifierValidator.RequireSafe(PersonId, nameof(PersonId), errors);
        if (string.IsNullOrWhiteSpace(Email) || BrowserCredentialRedactor.ContainsSecret(Email))
            errors.Add("Person email is required and cannot contain secret-like content.");
        return errors.Count == 0 ? ContractValidationResult.Valid : new ContractValidationResult(false, errors);
    }
}

public sealed record NexaCompanyAccount(
    string CompanyId,
    string OrganizationId,
    IReadOnlyList<string> PersonIds,
    IReadOnlyList<string> WorkerIds,
    NexaAccountStatus Status)
{
    public bool SupportsMultipleWorkers => WorkerIds.Count > 1;

    public ContractValidationResult Validate()
    {
        var errors = new List<string>();
        BrowserSafeIdentifierValidator.RequireSafe(CompanyId, nameof(CompanyId), errors);
        BrowserSafeIdentifierValidator.RequireSafe(OrganizationId, nameof(OrganizationId), errors);
        foreach (var personId in PersonIds)
            BrowserSafeIdentifierValidator.RequireSafe(personId, nameof(PersonIds), errors);
        foreach (var workerId in WorkerIds)
            BrowserSafeIdentifierValidator.RequireSafe(workerId, nameof(WorkerIds), errors);
        return errors.Count == 0 ? ContractValidationResult.Valid : new ContractValidationResult(false, errors);
    }
}

public sealed record NexaWorkspace(
    string WorkspaceId,
    string OrganizationId,
    string DisplayName,
    NexaAccountStatus Status)
{
    public ContractValidationResult Validate()
    {
        var errors = new List<string>();
        BrowserSafeIdentifierValidator.RequireSafe(WorkspaceId, nameof(WorkspaceId), errors);
        BrowserSafeIdentifierValidator.RequireSafe(OrganizationId, nameof(OrganizationId), errors);
        if (BrowserCredentialRedactor.ContainsSecret(DisplayName))
            errors.Add("Workspace display name contains secret-like content.");
        return errors.Count == 0 ? ContractValidationResult.Valid : new ContractValidationResult(false, errors);
    }
}

public sealed record NexaSeat(
    string SeatId,
    string AccountId,
    NexaRole Role,
    bool Active)
{
    public ContractValidationResult Validate()
    {
        var errors = new List<string>();
        BrowserSafeIdentifierValidator.RequireSafe(SeatId, nameof(SeatId), errors);
        BrowserSafeIdentifierValidator.RequireSafe(AccountId, nameof(AccountId), errors);
        if (Role == NexaRole.Unknown)
            errors.Add("Seat role cannot be unknown.");
        return errors.Count == 0 ? ContractValidationResult.Valid : new ContractValidationResult(false, errors);
    }
}

public sealed record NexaWorker(
    string WorkerId,
    string WorkspaceId,
    string SeatId,
    NexaRole Role,
    IReadOnlySet<NexaAdminCapability> Capabilities,
    bool Active)
{
    public bool HasWorkspace => !string.IsNullOrWhiteSpace(WorkspaceId);

    public ContractValidationResult Validate()
    {
        var errors = new List<string>();
        BrowserSafeIdentifierValidator.RequireSafe(WorkerId, nameof(WorkerId), errors);
        BrowserSafeIdentifierValidator.RequireSafe(WorkspaceId, nameof(WorkspaceId), errors);
        BrowserSafeIdentifierValidator.RequireSafe(SeatId, nameof(SeatId), errors);
        if (!HasWorkspace)
            errors.Add("Worker requires workspace.");
        if (Role == NexaRole.Unknown)
            errors.Add("Worker role cannot be unknown.");
        return errors.Count == 0 ? ContractValidationResult.Valid : new ContractValidationResult(false, errors);
    }
}

public sealed record NexaProductAccount(
    string AccountId,
    NexaProductAccountKind Kind,
    NexaAccountStatus Status,
    NexaPersonAccount? Person,
    NexaCompanyAccount? Company,
    NexaOrganization Organization,
    IReadOnlyList<NexaWorkspace> Workspaces,
    IReadOnlyList<NexaWorker> Workers,
    IReadOnlyList<NexaSeat> Seats)
{
    public bool IsPerson => Kind == NexaProductAccountKind.Person && Person is not null;
    public bool IsCompany => Kind == NexaProductAccountKind.Company && Company is not null;

    public ContractValidationResult Validate()
    {
        var errors = new List<string>();
        BrowserSafeIdentifierValidator.RequireSafe(AccountId, nameof(AccountId), errors);
        errors.AddRange(Organization.Validate().Errors);
        if (!IsPerson && !IsCompany)
            errors.Add("Product account must be person or company.");
        if (Person is not null)
            errors.AddRange(Person.Validate().Errors);
        if (Company is not null)
            errors.AddRange(Company.Validate().Errors);
        foreach (var workspace in Workspaces)
            errors.AddRange(workspace.Validate().Errors);
        foreach (var worker in Workers)
            errors.AddRange(worker.Validate().Errors);
        foreach (var seat in Seats)
            errors.AddRange(seat.Validate().Errors);
        return errors.Count == 0 ? ContractValidationResult.Valid : new ContractValidationResult(false, errors);
    }
}

public sealed record NexaAdminRolePolicy(IReadOnlyDictionary<NexaRole, IReadOnlySet<NexaAdminCapability>> Capabilities)
{
    public static NexaAdminRolePolicy Default() =>
        new(new Dictionary<NexaRole, IReadOnlySet<NexaAdminCapability>>
        {
            [NexaRole.Owner] = Set(NexaAdminCapability.ManageAccount, NexaAdminCapability.ManagePlan, NexaAdminCapability.ManageWorkers, NexaAdminCapability.ManageConfiguration, NexaAdminCapability.ExecuteAllowedFlows, NexaAdminCapability.ViewReadOnly, NexaAdminCapability.ViewAudit),
            [NexaRole.Admin] = Set(NexaAdminCapability.ManageWorkers, NexaAdminCapability.ManageConfiguration, NexaAdminCapability.ExecuteAllowedFlows, NexaAdminCapability.ViewReadOnly, NexaAdminCapability.ViewAudit),
            [NexaRole.Operator] = Set(NexaAdminCapability.ExecuteAllowedFlows, NexaAdminCapability.ViewReadOnly),
            [NexaRole.Viewer] = Set(NexaAdminCapability.ViewReadOnly),
            [NexaRole.Worker] = Set(NexaAdminCapability.ExecuteAllowedFlows),
            [NexaRole.Support] = Set(NexaAdminCapability.SupportAccess, NexaAdminCapability.ViewReadOnly)
        });

    public bool Allows(NexaRole role, NexaAdminCapability capability) =>
        Capabilities.TryGetValue(role, out var capabilities) && capabilities.Contains(capability);

    private static IReadOnlySet<NexaAdminCapability> Set(params NexaAdminCapability[] values) =>
        new HashSet<NexaAdminCapability>(values);
}

public sealed record NexaAdminAuditEvent(
    string EventId,
    string ActorId,
    NexaRole Role,
    string AccountId,
    string OrganizationId,
    NexaAdminAction Action,
    NexaAdminDecisionKind Decision,
    string Reason,
    DateTimeOffset TimestampUtc,
    string BeforeSummary,
    string AfterSummary,
    bool Redacted)
{
    public ContractValidationResult Validate()
    {
        var errors = new List<string>();
        BrowserSafeIdentifierValidator.RequireSafe(EventId, nameof(EventId), errors);
        BrowserSafeIdentifierValidator.RequireSafe(ActorId, nameof(ActorId), errors);
        BrowserSafeIdentifierValidator.RequireSafe(AccountId, nameof(AccountId), errors);
        BrowserSafeIdentifierValidator.RequireSafe(OrganizationId, nameof(OrganizationId), errors);
        if (!Redacted)
            errors.Add("Admin audit must be redacted.");
        if (BrowserCredentialRedactor.ContainsSecret(Reason) ||
            BrowserCredentialRedactor.ContainsSecret(BeforeSummary) ||
            BrowserCredentialRedactor.ContainsSecret(AfterSummary))
            errors.Add("Admin audit contains secret-like content.");
        return errors.Count == 0 ? ContractValidationResult.Valid : new ContractValidationResult(false, errors);
    }
}

public sealed record NexaAdminDecision(
    NexaAdminDecisionKind Decision,
    NexaAdminAction Action,
    NexaAdminCapability RequiredCapability,
    NexaRole Role,
    string Reason,
    NexaAdminAuditEvent AuditEvent)
{
    public bool Allowed => Decision == NexaAdminDecisionKind.Allowed && AuditEvent.Validate().IsValid;
}
