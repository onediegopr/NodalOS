using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.BrowserExecutor.Cdp;

public sealed class NexaAdminStateStore
{
    private readonly Dictionary<string, NexaProductAccount> _accounts = new(StringComparer.Ordinal);
    private readonly Dictionary<string, NexaLicense> _licenses = new(StringComparer.Ordinal);
    private readonly Dictionary<string, List<NexaUsageCounter>> _usage = new(StringComparer.Ordinal);

    public IReadOnlyList<NexaProductAccount> Accounts => _accounts.Values.ToArray();
    public IReadOnlyList<NexaLicense> Licenses => _licenses.Values.ToArray();

    public void UpsertAccount(NexaProductAccount account) => _accounts[account.AccountId] = account;
    public bool TryGetAccount(string accountId, out NexaProductAccount account) => _accounts.TryGetValue(accountId, out account!);
    public void UpsertLicense(NexaLicense license) => _licenses[license.LicenseId] = license;
    public bool TryGetLicense(string licenseId, out NexaLicense license) => _licenses.TryGetValue(licenseId, out license!);
    public NexaLicense? LicenseForAccount(string accountId) => _licenses.Values.FirstOrDefault(l => l.AccountId == accountId);
    public IReadOnlyList<NexaUsageCounter> UsageForAccount(string accountId) => _usage.TryGetValue(accountId, out var value) ? value : [];
    public void SetUsage(string accountId, NexaUsageCounter counter) => _usage[accountId] = [counter];
}

public sealed class NexaAdminAuditStore
{
    private readonly List<NexaAdminAuditEvent> _events = [];

    public IReadOnlyList<NexaAdminAuditEvent> Events => _events;

    public void Append(NexaAdminAuditEvent auditEvent)
    {
        var validation = auditEvent.Validate();
        if (!validation.IsValid)
            throw new InvalidOperationException(string.Join("; ", validation.Errors));
        _events.Add(auditEvent);
    }

    public IReadOnlyList<NexaAdminAuditEvent> Query(string accountId, string organizationId, DateTimeOffset fromUtc, DateTimeOffset toUtc) =>
        _events
            .Where(e => e.AccountId == accountId && e.OrganizationId == organizationId && e.TimestampUtc >= fromUtc && e.TimestampUtc <= toUtc)
            .ToArray();
}

public sealed class NexaAdminPolicyService
{
    private readonly NexaAdminPolicyEvaluator _adminPolicy = new();

    public NexaAdminDecision Evaluate(NexaAdminCommand command, NexaProductAccount account) =>
        _adminPolicy.Evaluate(account, command.ActorId, command.ActorRole, command.RequestedAction);
}

public sealed class NexaAdminQueryHandler
{
    private readonly NexaAdminStateStore _state;
    private readonly NexaAdminAuditStore _audit;
    private readonly NexaAdminConsoleModelBuilder _builder = new();

    public NexaAdminQueryHandler(NexaAdminStateStore state, NexaAdminAuditStore audit)
    {
        _state = state;
        _audit = audit;
    }

    public NexaAdminConsoleDashboardModel GetDashboard(string accountId)
    {
        if (!_state.TryGetAccount(accountId, out var account))
            throw new InvalidOperationException("account not found");
        var license = _state.LicenseForAccount(accountId) ?? DefaultLicense(accountId);
        return _builder.Build(account, license, _state.UsageForAccount(accountId), _audit.Query(account.AccountId, account.Organization.OrganizationId, DateTimeOffset.MinValue, DateTimeOffset.MaxValue));
    }

    public IReadOnlyList<NexaAdminAuditViewModel> QueryAudit(NexaAdminAuditQuery query)
    {
        return _audit.Query(query.TargetAccountId, query.TargetOrganizationId, query.FromUtc, query.ToUtc)
            .Select(e => new NexaAdminAuditViewModel(e.EventId, e.Action, e.Decision, BrowserCredentialRedactor.Redact(e.Reason), e.TimestampUtc, Redacted: true))
            .ToArray();
    }

    private static NexaLicense DefaultLicense(string accountId)
    {
        var now = DateTimeOffset.UtcNow;
        return new NexaLicense($"license-default-{Guid.NewGuid():N}", accountId, NexaPlan.Free(), NexaLicenseStatus.Active, now.AddMinutes(-1), now.AddDays(7), [], ManualAdminOverride: false);
    }
}

public sealed class NexaAdminConsoleService
{
    private readonly NexaAdminCommandHandler _handler = new();
    private readonly NexaAdminPolicyService _policy = new();

    public NexaAdminConsoleService(NexaAdminStateStore? state = null, NexaAdminAuditStore? audit = null)
    {
        State = state ?? new NexaAdminStateStore();
        Audit = audit ?? new NexaAdminAuditStore();
        Queries = new NexaAdminQueryHandler(State, Audit);
    }

    public NexaAdminStateStore State { get; }
    public NexaAdminAuditStore Audit { get; }
    public NexaAdminQueryHandler Queries { get; }

    public NexaAdminCommandResult Execute(NexaAdminCommand command)
    {
        var account = ResolveAccount(command);
        var license = State.LicenseForAccount(command.TargetAccountId);
        var decision = _policy.Evaluate(command, account);
        var result = decision.Allowed
            ? ExecuteAllowed(command, account, license)
            : Result(command, decision.Decision, decision.Reason, account);
        Audit.Append(ToAuditEvent(command, account, result));
        return result;
    }

    private NexaAdminCommandResult ExecuteAllowed(NexaAdminCommand command, NexaProductAccount account, NexaLicense? license)
    {
        var compatibility = ValidateCompatibility(command, account, license);
        if (compatibility is not null)
            return Result(command, NexaAdminDecisionKind.Denied, compatibility, account);

        switch (command)
        {
            case NexaAdminCreateAccountCommand create:
                account = CreateAccount(create);
                State.UpsertAccount(account);
                break;
            case NexaAdminCreateWorkerCommand createWorker:
                account = AddWorker(account, createWorker);
                State.UpsertAccount(account);
                break;
            case NexaAdminUpdateWorkerRoleCommand updateWorker:
                account = UpdateWorkerRole(account, updateWorker);
                State.UpsertAccount(account);
                break;
            case NexaAdminAssignLicenseCommand assign:
                if (!State.TryGetLicense(assign.LicenseId, out var assigned))
                    return Result(command, NexaAdminDecisionKind.FailClosed, "license not found", account);
                State.UpsertLicense(assigned with { AccountId = account.AccountId });
                break;
            case NexaAdminSetUsageLimitCommand usage:
                State.SetUsage(account.AccountId, new NexaUsageCounter(usage.Limit.LimitId, 0, DateTimeOffset.UtcNow));
                break;
            case NexaAdminSuspendAccountCommand:
                account = account with { Status = NexaAccountStatus.Suspended };
                State.UpsertAccount(account);
                break;
            case NexaAdminUpdateAccountCommand update:
                account = account with { Status = update.Status };
                State.UpsertAccount(account);
                break;
            case NexaAdminSetFeatureFlagCommand:
                break;
        }

        return Result(command, NexaAdminDecisionKind.Allowed, "admin runtime command applied", account);
    }

    private string? ValidateCompatibility(NexaAdminCommand command, NexaProductAccount account, NexaLicense? license)
    {
        if (command is NexaAdminSetFeatureFlagCommand feature)
        {
            var result = _handler.Handle(feature, account, license);
            if (!result.Succeeded)
                return result.Reason;
            if (feature.Enabled && license?.Plan.Enables(feature.Feature) != true && license?.Entitlements.Any(e => e.Feature == feature.Feature && e.Enabled) != true)
                return "feature incompatible with plan";
        }
        if (command is NexaAdminAssignLicenseCommand assign && !State.TryGetLicense(assign.LicenseId, out _))
            return "license not found";
        return null;
    }

    private NexaProductAccount ResolveAccount(NexaAdminCommand command)
    {
        if (command is NexaAdminCreateAccountCommand create && !State.TryGetAccount(create.TargetAccountId, out _))
            return PlaceholderAccount(create.TargetAccountId, create.TargetOrganizationId);
        if (State.TryGetAccount(command.TargetAccountId, out var account))
            return account;
        return PlaceholderAccount(command.TargetAccountId, command.TargetOrganizationId);
    }

    private static NexaProductAccount CreateAccount(NexaAdminCreateAccountCommand command)
    {
        var org = new NexaOrganization(command.TargetOrganizationId, "NODAL OS Runtime Org", NexaAccountStatus.Active);
        var workspace = new NexaWorkspace("workspace-main", org.OrganizationId, "Main Workspace", NexaAccountStatus.Active);
        if (command.AccountKind == NexaProductAccountKind.Person)
        {
            var person = new NexaPersonAccount("person-main", "person@example.test", NexaAccountStatus.Active);
            var worker = new NexaWorker("worker-main", workspace.WorkspaceId, "seat-main", NexaRole.Owner, new HashSet<NexaAdminCapability> { NexaAdminCapability.ManageAccount }, Active: true);
            return new NexaProductAccount(command.TargetAccountId, NexaProductAccountKind.Person, NexaAccountStatus.Active, person, null, org, [workspace], [worker], [new NexaSeat("seat-main", command.TargetAccountId, NexaRole.Owner, Active: true)]);
        }

        var company = new NexaCompanyAccount("company-main", org.OrganizationId, ["person-owner"], ["worker-main"], NexaAccountStatus.Active);
        var companyWorker = new NexaWorker("worker-main", workspace.WorkspaceId, "seat-main", NexaRole.Owner, new HashSet<NexaAdminCapability> { NexaAdminCapability.ManageAccount, NexaAdminCapability.ManageWorkers }, Active: true);
        return new NexaProductAccount(command.TargetAccountId, NexaProductAccountKind.Company, NexaAccountStatus.Active, null, company, org, [workspace], [companyWorker], [new NexaSeat("seat-main", command.TargetAccountId, NexaRole.Owner, Active: true)]);
    }

    private static NexaProductAccount PlaceholderAccount(string accountId, string organizationId)
    {
        var org = new NexaOrganization(organizationId, "NODAL OS Placeholder Org", NexaAccountStatus.Active);
        var workspace = new NexaWorkspace("workspace-main", organizationId, "Main Workspace", NexaAccountStatus.Active);
        return new NexaProductAccount(accountId, NexaProductAccountKind.Company, NexaAccountStatus.Active, null, new NexaCompanyAccount("company-main", organizationId, ["person-owner"], [], NexaAccountStatus.Active), org, [workspace], [], []);
    }

    private static NexaProductAccount AddWorker(NexaProductAccount account, NexaAdminCreateWorkerCommand command)
    {
        var workerId = $"worker-{Guid.NewGuid():N}";
        var seatId = $"seat-{Guid.NewGuid():N}";
        var worker = new NexaWorker(workerId, command.WorkspaceId, seatId, command.WorkerRole, new HashSet<NexaAdminCapability> { NexaAdminCapability.ExecuteAllowedFlows }, Active: true);
        var seat = new NexaSeat(seatId, account.AccountId, command.WorkerRole, Active: true);
        var company = account.Company is null ? null : account.Company with { WorkerIds = account.Company.WorkerIds.Append(workerId).ToArray() };
        return account with { Company = company, Workers = account.Workers.Append(worker).ToArray(), Seats = account.Seats.Append(seat).ToArray() };
    }

    private static NexaProductAccount UpdateWorkerRole(NexaProductAccount account, NexaAdminUpdateWorkerRoleCommand command)
    {
        var workers = account.Workers.Select(w => w.WorkerId == command.WorkerId ? w with { Role = command.NewRole } : w).ToArray();
        var seats = account.Seats.Select(s => s.SeatId == workers.FirstOrDefault(w => w.WorkerId == command.WorkerId)?.SeatId ? s with { Role = command.NewRole } : s).ToArray();
        return account with { Workers = workers, Seats = seats };
    }

    private static NexaAdminCommandResult Result(NexaAdminCommand command, NexaAdminDecisionKind decision, string reason, NexaProductAccount account)
    {
        var audit = new NexaAdminAuditViewModel($"admin-audit-{Guid.NewGuid():N}", command.RequestedAction, decision, BrowserCredentialRedactor.Redact(reason), DateTimeOffset.UtcNow, Redacted: true);
        return new NexaAdminCommandResult(decision, command.CommandId, BrowserCredentialRedactor.Redact(reason), [audit.EventId], audit, Redacted: true);
    }

    private static NexaAdminAuditEvent ToAuditEvent(NexaAdminCommand command, NexaProductAccount account, NexaAdminCommandResult result) =>
        new(result.Audit.EventId, BrowserCredentialRedactor.Redact(command.ActorId), command.ActorRole, account.AccountId, account.Organization.OrganizationId, command.RequestedAction, result.Decision, result.Reason, result.Audit.TimestampUtc, "before summary redacted", "after summary redacted", Redacted: true);
}
