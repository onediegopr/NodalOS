namespace OneBrain.BrowserExecutor.Contracts;

public enum NexaInstallerPlanStepKind
{
    Preflight,
    FileLayout,
    PermissionCheck,
    ComponentInstallModel,
    ServiceRegistrationModelOnly,
    RollbackDryRun
}

public enum NexaInstallerPreflightStatus
{
    Passed,
    Missing,
    Failed,
    NotDeclared
}

public sealed record NexaInstallerPlanStep(
    string StepId,
    NexaInstallerPlanStepKind Kind,
    string Description,
    bool WouldModifyRealSystem)
{
    public ContractValidationResult Validate()
    {
        var errors = new List<string>();
        BrowserSafeIdentifierValidator.RequireSafe(StepId, nameof(StepId), errors);
        if (string.IsNullOrWhiteSpace(Description))
            errors.Add("Installer step description is required.");
        if (WouldModifyRealSystem)
            errors.Add("Installer dry-run step cannot modify the real system.");
        return errors.Count == 0 ? ContractValidationResult.Valid : new ContractValidationResult(false, errors);
    }
}

public sealed record NexaInstallerFileLayout(
    string InstallRoot,
    IReadOnlyList<string> Directories,
    IReadOnlyList<string> Files,
    bool SandboxOnly)
{
    public ContractValidationResult Validate()
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(InstallRoot))
            errors.Add("Installer install root is required.");
        if (!SandboxOnly)
            errors.Add("Installer dry-run file layout must be sandbox-only.");
        if (Directories.Count == 0)
            errors.Add("Installer file layout must report directories.");
        if (Files.Count == 0)
            errors.Add("Installer file layout must report files.");
        foreach (var path in Directories.Concat(Files))
        {
            if (BrowserCredentialRedactor.ContainsSecret(path))
                errors.Add("Installer file layout contains secret-like path content.");
        }
        return errors.Count == 0 ? ContractValidationResult.Valid : new ContractValidationResult(false, errors);
    }
}

public sealed record NexaInstallerPermissionRequirement(
    string PermissionId,
    string Scope,
    bool Elevated,
    bool ModelOnly)
{
    public ContractValidationResult Validate()
    {
        var errors = new List<string>();
        BrowserSafeIdentifierValidator.RequireSafe(PermissionId, nameof(PermissionId), errors);
        if (string.IsNullOrWhiteSpace(Scope))
            errors.Add("Installer permission scope is required.");
        if (!ModelOnly)
            errors.Add("Installer permission requirement must be model-only.");
        return errors.Count == 0 ? ContractValidationResult.Valid : new ContractValidationResult(false, errors);
    }
}

public sealed record NexaInstallerPreflightCheck(
    string CheckId,
    NexaInstallerPreflightStatus Status,
    string Reason,
    bool Required)
{
    public bool Passed => Status == NexaInstallerPreflightStatus.Passed || !Required;

    public ContractValidationResult Validate()
    {
        var errors = new List<string>();
        BrowserSafeIdentifierValidator.RequireSafe(CheckId, nameof(CheckId), errors);
        if (BrowserCredentialRedactor.ContainsSecret(Reason))
            errors.Add("Installer preflight reason contains secret-like content.");
        return errors.Count == 0 ? ContractValidationResult.Valid : new ContractValidationResult(false, errors);
    }
}

public sealed record NexaInstallerRollbackDryRunPlan(
    string PlanId,
    IReadOnlyList<string> RollbackSteps,
    bool ExecutesRollback)
{
    public ContractValidationResult Validate()
    {
        var errors = new List<string>();
        BrowserSafeIdentifierValidator.RequireSafe(PlanId, nameof(PlanId), errors);
        if (RollbackSteps.Count == 0)
            errors.Add("Installer rollback dry-run must report rollback steps.");
        if (ExecutesRollback)
            errors.Add("Installer rollback dry-run cannot execute rollback.");
        return errors.Count == 0 ? ContractValidationResult.Valid : new ContractValidationResult(false, errors);
    }
}

public sealed record NexaInstallerPlan(
    string PlanId,
    NexaConfigurationProfile Profile,
    NexaReleaseManifest ReleaseManifest,
    NexaInstallerFileLayout FileLayout,
    IReadOnlyList<NexaInstallerPermissionRequirement> Permissions,
    IReadOnlyList<NexaInstallerPlanStep> Steps,
    NexaInstallerRollbackDryRunPlan RollbackPlan,
    bool RegistersRealService,
    bool CreatesRealScheduledTask,
    bool TouchesRegistry,
    bool OpensPublicPort,
    bool AutoUpdateRealEnabled)
{
    public ContractValidationResult Validate()
    {
        var errors = new List<string>();
        BrowserSafeIdentifierValidator.RequireSafe(PlanId, nameof(PlanId), errors);
        errors.AddRange(FileLayout.Validate().Errors);
        errors.AddRange(RollbackPlan.Validate().Errors);
        foreach (var permission in Permissions)
            errors.AddRange(permission.Validate().Errors);
        foreach (var step in Steps)
            errors.AddRange(step.Validate().Errors);
        if (RegistersRealService)
            errors.Add("Installer dry-run cannot register a real service.");
        if (CreatesRealScheduledTask)
            errors.Add("Installer dry-run cannot create a real scheduled task.");
        if (TouchesRegistry)
            errors.Add("Installer dry-run cannot touch the real registry.");
        if (OpensPublicPort)
            errors.Add("Installer dry-run cannot open a public port.");
        if (AutoUpdateRealEnabled)
            errors.Add("Installer dry-run cannot enable real auto-update.");
        return errors.Count == 0 ? ContractValidationResult.Valid : new ContractValidationResult(false, errors);
    }
}

public sealed record NexaInstallerDryRunRequest(
    NexaInstallerPlan Plan,
    bool OsSupported,
    bool DotNetRuntimeCompatible,
    bool BrowserAvailabilityDeclared,
    bool CdpCapabilityDeclared,
    bool VaultProviderDeclared,
    bool DiagnosticsRedactionActive,
    bool TenantGovernanceAvailable,
    bool AdminRuntimeAvailable,
    bool LicenseEvaluatorAvailable);

public sealed record NexaInstallerDryRunResult(
    bool Allowed,
    bool ModifiedRealSystem,
    IReadOnlyList<NexaInstallerPreflightCheck> PreflightChecks,
    IReadOnlyList<string> Violations,
    NexaInstallerFileLayout FileLayout,
    NexaInstallerRollbackDryRunPlan RollbackPlan,
    string Decision,
    bool Redacted);

public sealed record NexaDeploymentRollbackStep(
    string StepId,
    string Description,
    bool ExecutesRealRollback);

public sealed record NexaDeploymentRollbackDryRun(
    string RollbackId,
    IReadOnlyList<NexaDeploymentRollbackStep> Steps,
    bool ModelOnly);

public sealed record NexaDeploymentRollbackDecision(
    bool Allowed,
    bool Executed,
    string Reason,
    NexaDeploymentRollbackDryRun Plan);
