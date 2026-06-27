namespace OneBrain.Core.Recipes;

public enum RecipeTemplateCategory
{
    ExcelMicrosoft365,
    GoogleWorkspace,
    SAP,
    MercadoLibreMercadoPago,
    ARCAFiscal,
    ERPLocalLATAM,
    GenericBrowserPortal,
    ComputerUseLegacy,
    Unknown
}

public enum RecipeTemplateRegion
{
    Global,
    LATAM,
    Argentina,
    Brazil,
    Mexico,
    Chile,
    Colombia,
    Unknown
}

public enum RecipeTemplateCountry
{
    Global,
    Argentina,
    Brazil,
    Mexico,
    Chile,
    Colombia,
    Peru,
    Uruguay,
    Paraguay,
    Ecuador,
    Unknown
}

public enum RecipeTemplateSystem
{
    Excel,
    Microsoft365,
    GoogleSheets,
    GoogleDrive,
    Gmail,
    GoogleCalendar,
    SAP,
    MercadoLibre,
    MercadoPago,
    ARCA,
    Fiscal,
    ERPLocalLATAM,
    GenericBrowserPortal,
    ComputerUseLegacy,
    Unknown
}

public enum RecipeTemplateRuntimeEligibility
{
    PreviewOnly,
    FixtureOnly,
    DryRunDraftOnly,
    ManualAssistOnly,
    ReferenceOnly,
    LiveBlocked,
    FutureGated,
    Disabled
}

public enum RecipeTemplateStatus
{
    DraftTemplate,
    CatalogPreview,
    FixtureReady,
    MissingToolTrust,
    MissingSecretRefs,
    MissingValidation,
    MissingEvidence,
    MissingApprovalPath,
    FutureGated,
    LiveBlocked,
    Disabled,
    Deprecated,
    BlockedByPolicy
}

public sealed record RecipeTemplateRef(string TemplateId);

public sealed record RecipeTemplateSafetyProfile(
    RecipeRiskLevel RiskLevel,
    IReadOnlyList<SensitiveActionCategory> SensitiveCategories,
    bool RequiresHumanApproval,
    bool LiveRuntimeBlocked = true,
    bool RawSecretsAllowed = false)
{
    public bool LiveRuntimeEnabled => false;
}

public sealed record RecipeTemplateDefinition(
    string TemplateId,
    string DisplayName,
    string Description,
    string PackId,
    RecipeTemplateCategory Category,
    RecipeTemplateSystem System,
    RecipeTemplateRegion Region,
    IReadOnlyList<RecipeTemplateCountry> Countries,
    RecipeDefinition RecipeDefinition,
    IReadOnlyList<string> RequiredCapabilities,
    IReadOnlyList<string> RequiredToolTrustRefs,
    IReadOnlyList<string> RequiredSecretRefs,
    IReadOnlyList<string> ConnectorEligibilityRefs,
    IReadOnlyList<string> TriggerRefs,
    IReadOnlyList<string> EvidenceRequirementRefs,
    IReadOnlyList<string> ValidationRequirementRefs,
    IReadOnlyList<string> ApprovalHumanInterventionRequirementRefs,
    RecipeTemplateSafetyProfile SafetyProfile,
    IReadOnlyList<RecipeRunMode> AllowedRunModes,
    IReadOnlyList<RecipeRunMode> BlockedRunModes,
    RecipeTemplateRuntimeEligibility RuntimeEligibility,
    RecipeTemplateStatus Status,
    RecipeSafeNextAction SafeNextAction,
    string OperatorVisibleSummary,
    string LiveRuntimeStatus,
    IReadOnlyList<string> FixtureSampleRefs,
    string NotIncludedOrNotAutomatedSummary,
    bool RawSecretIncluded = false,
    bool Deprecated = false,
    bool Disabled = false)
{
    public bool LiveRuntimeEnabled => false;
    public bool ConnectorExecutionEnabled => false;
    public bool BrowserAutomationEnabled => false;
    public bool DesktopAutomationEnabled => false;
    public bool AutomaticRunEnabled => false;
    public bool RawSecretValuesStored => RawSecretIncluded;
}

public sealed record RecipeTemplatePack(
    string PackId,
    string DisplayName,
    RecipeTemplateCategory Category,
    RecipeTemplateRegion Region,
    IReadOnlyList<RecipeTemplateDefinition> Templates,
    string SafetySummary,
    bool FixtureSafeOnly = true)
{
    public bool LiveRuntimeEnabled => false;
}

public sealed record RecipeTemplateCatalog(
    string CatalogId,
    string Version,
    IReadOnlyList<RecipeTemplatePack> Packs,
    string OperatorVisibleSummary)
{
    public IReadOnlyList<RecipeTemplateDefinition> Templates => Packs.SelectMany(p => p.Templates).ToArray();
    public bool LiveRuntimeEnabled => false;
    public bool ConnectorExecutionEnabled => false;
}

public sealed record RecipeTemplateTriggerBinding(
    RecipeTriggerDefinition Trigger,
    RecipeDetectorDefinition Detector,
    RecipeTriggerPolicy Policy);

public sealed record RecipeTemplateReadinessContext(
    RecipeToolTrustRegistry ToolTrustRegistry,
    IReadOnlyList<RecipeSecretRequirement> SecretRequirements,
    IReadOnlyList<RecipeConnectorEligibility> ConnectorEligibilities,
    IReadOnlyList<RecipeTemplateTriggerBinding> TriggerBindings,
    RecipeEvidencePack? EvidencePack = null,
    IReadOnlyList<RecipeStepEvidenceResult>? StepEvidenceResults = null,
    IReadOnlyList<RecipeValidationEvidence>? ValidationEvidence = null,
    RecipeApprovalNarrative? ApprovalNarrative = null,
    RecipeLabSnapshot? LabSnapshot = null,
    bool RawSecretDetected = false);

public sealed record RecipeTemplateReadiness(
    string TemplateId,
    bool IsReady,
    RecipeTemplateStatus Status,
    RecipeReadinessStatus CanonicalReadinessStatus,
    IReadOnlyList<RecipeReadinessIssue> BlockingIssues,
    IReadOnlyList<RecipeReadinessIssue> Warnings,
    RecipeSafeNextAction SafeNextAction,
    IReadOnlyList<RecipeRunMode> BlockedRunModes,
    string OperatorSummary)
{
    public bool LiveRuntimeEnabled => false;
    public bool ActionAuthorityGranted => false;
    public bool ConnectorExecutionEnabled => false;
    public bool StartsRecipeRun => false;
    public bool ProcessesWorkitems => false;
}

public sealed record RecipeTemplatePackSummary(
    string PackId,
    int TemplateCount,
    int FixtureReadyCount,
    int LiveBlockedCount,
    IReadOnlyList<string> BlockingSummaries);

public static class RecipeTemplateReadinessEvaluator
{
    public static RecipeTemplateReadiness Evaluate(
        RecipeTemplateDefinition template,
        RecipeTemplateReadinessContext context,
        RecipeRunMode mode = RecipeRunMode.CatalogPreview)
    {
        var blocking = new List<RecipeReadinessIssue>();
        var warnings = new List<RecipeReadinessIssue>();

        var preflight = RecipePolicyPreflightEvaluator.Evaluate(template.RecipeDefinition, mode);
        blocking.AddRange(preflight.BlockingIssues);
        warnings.AddRange(preflight.Warnings);

        EvaluateTemplateMetadata(template, blocking);
        EvaluateToolTrust(template, context, blocking);
        EvaluateSecrets(template, context, blocking);
        EvaluateConnectors(template, context, blocking);
        EvaluateTriggers(context, blocking, warnings);
        EvaluateEvidence(template, context, blocking);
        EvaluateHumanApproval(template, context, blocking);
        EvaluateLabSafety(context, blocking);

        if (blocking.Count > 0)
        {
            var status = ToTemplateStatus(blocking[0].Status, template);
            return new(
                template.TemplateId,
                IsReady: false,
                status,
                blocking[0].Status,
                blocking,
                warnings,
                new RecipeSafeNextAction(RecipeSafeNextActionKind.KeepBlocked, "Template remains blocked until composite readiness issues are resolved."),
                template.BlockedRunModes,
                $"{template.DisplayName}: blocked by composite readiness.");
        }

        var readyStatus = template.RuntimeEligibility == RecipeTemplateRuntimeEligibility.FixtureOnly
            ? RecipeTemplateStatus.FixtureReady
            : RecipeTemplateStatus.CatalogPreview;

        return new(
            template.TemplateId,
            IsReady: true,
            readyStatus,
            preflight.Status,
            [],
            warnings,
            template.SafeNextAction,
            template.BlockedRunModes,
            $"{template.DisplayName}: fixture-safe template is ready for catalog inspection.");
    }

    public static RecipeTemplatePackSummary SummarizePack(RecipeTemplatePack pack, RecipeTemplateReadinessContext context)
    {
        var readiness = pack.Templates.Select(t => Evaluate(t, context)).ToArray();
        return new(
            pack.PackId,
            pack.Templates.Count,
            readiness.Count(r => r.Status == RecipeTemplateStatus.FixtureReady),
            readiness.Count(r => r.Status is RecipeTemplateStatus.LiveBlocked or RecipeTemplateStatus.FutureGated),
            readiness.Where(r => !r.IsReady).Select(r => r.OperatorSummary).ToArray());
    }

    private static void EvaluateTemplateMetadata(RecipeTemplateDefinition template, List<RecipeReadinessIssue> blocking)
    {
        if (template.System == RecipeTemplateSystem.Unknown || template.Category == RecipeTemplateCategory.Unknown)
            blocking.Add(Issue("template-unknown-system", RecipeReadinessStatus.BlockedByProtectedScope, "Unknown template system defaults blocked/future-gated."));

        if (template.RawSecretIncluded)
            blocking.Add(Issue("template-raw-secret-detected", RecipeReadinessStatus.BlockedMissingSecretReference, "Template contracts must not include raw secret values."));

        if (template.Disabled || template.Status == RecipeTemplateStatus.Disabled || template.RuntimeEligibility == RecipeTemplateRuntimeEligibility.Disabled)
            blocking.Add(Issue("template-disabled", RecipeReadinessStatus.BlockedByProtectedScope, "Template is disabled."));

        if (template.RuntimeEligibility is RecipeTemplateRuntimeEligibility.LiveBlocked or RecipeTemplateRuntimeEligibility.FutureGated)
            blocking.Add(Issue("template-live-runtime-blocked", RecipeReadinessStatus.BlockedLiveRuntimeDisabled, "Template runtime is live-blocked or future-gated."));

        if (template.Category is RecipeTemplateCategory.GenericBrowserPortal or RecipeTemplateCategory.ComputerUseLegacy &&
            template.RuntimeEligibility is not (RecipeTemplateRuntimeEligibility.LiveBlocked or RecipeTemplateRuntimeEligibility.FutureGated))
        {
            blocking.Add(Issue("template-live-surface-not-blocked", RecipeReadinessStatus.BlockedLiveRuntimeDisabled, "Browser portal and computer-use templates must remain live-blocked/future-gated."));
        }
    }

    private static void EvaluateToolTrust(
        RecipeTemplateDefinition template,
        RecipeTemplateReadinessContext context,
        List<RecipeReadinessIssue> blocking)
    {
        foreach (var toolRef in template.RequiredToolTrustRefs.Distinct())
        {
            var tool = context.ToolTrustRegistry.Find(toolRef);
            if (tool is null)
            {
                blocking.Add(Issue("template-missing-tool-trust", RecipeReadinessStatus.BlockedMissingToolTrust, "Template requires declared tool trust refs."));
                continue;
            }

            if (tool.IsLiveBlocked)
                blocking.Add(Issue("template-tool-live-blocked", RecipeReadinessStatus.BlockedLiveRuntimeDisabled, "Template tool trust ref is live-blocked, future-gated, disabled, browser-runtime, or desktop-runtime."));
            else if (!tool.IsTrustedForFixture)
                blocking.Add(Issue("template-untrusted-tool", RecipeReadinessStatus.BlockedMissingToolTrust, "Template tool trust ref is not fixture-trusted."));
        }
    }

    private static void EvaluateSecrets(
        RecipeTemplateDefinition template,
        RecipeTemplateReadinessContext context,
        List<RecipeReadinessIssue> blocking)
    {
        if (context.RawSecretDetected)
            blocking.Add(Issue("template-context-raw-secret-detected", RecipeReadinessStatus.BlockedMissingSecretReference, "Composite template readiness received raw secret marker."));

        foreach (var secretRef in template.RequiredSecretRefs.Distinct())
        {
            var secret = context.SecretRequirements.FirstOrDefault(s => s.SecretRefId == secretRef);
            if (secret is null)
            {
                blocking.Add(Issue("template-missing-secret-ref", RecipeReadinessStatus.BlockedMissingSecretReference, "Template requires secret refs by id only."));
                continue;
            }

            var secretReadiness = RecipeToolTrustSecretsPolicy.EvaluateSecretReadiness(secret, template.RecipeDefinition.RuntimeRiskProfile ?? EmptyRisk(template));
            blocking.AddRange(secretReadiness.BlockingIssues);
        }
    }

    private static void EvaluateConnectors(
        RecipeTemplateDefinition template,
        RecipeTemplateReadinessContext context,
        List<RecipeReadinessIssue> blocking)
    {
        foreach (var eligibility in context.ConnectorEligibilities)
        {
            if (!template.ConnectorEligibilityRefs.Contains(eligibility.EligibilityId))
                continue;

            var requirement = new RecipeCredentialedActionRequirement(
                $"credentialed:{eligibility.EligibilityId}",
                eligibility.ConnectorToolRef,
                eligibility.TrustRequirement.RequiredSecretRefs,
                [RecipeSecretScope.Tool, RecipeSecretScope.Workspace, RecipeSecretScope.Mission, RecipeSecretScope.ExternalVaultRef],
                eligibility.TrustRequirement.ApprovalRequired,
                eligibility.ApprovalPolicyPresent,
                eligibility.ActionCategory);

            var action = RecipeToolTrustSecretsPolicy.EvaluateCredentialedAction(
                requirement,
                context.ToolTrustRegistry,
                context.SecretRequirements,
                eligibility);

            blocking.AddRange(action.BlockingIssues);

            if (action.Decision.AllowsConnectorExecution || action.Decision.AllowsLiveRuntime || action.Decision.ActionAuthorityGranted)
                blocking.Add(Issue("template-connector-authority-leak", RecipeReadinessStatus.BlockedLiveRuntimeDisabled, "Connector readiness cannot grant execution or live authority."));
        }
    }

    private static void EvaluateTriggers(
        RecipeTemplateReadinessContext context,
        List<RecipeReadinessIssue> blocking,
        List<RecipeReadinessIssue> warnings)
    {
        foreach (var binding in context.TriggerBindings)
        {
            var readiness = RecipeTriggerPolicyEvaluator.Evaluate(binding.Trigger, binding.Detector, binding.Policy);
            blocking.AddRange(readiness.BlockingIssues);
            warnings.AddRange(readiness.Warnings);

            if (readiness.Decision.StartsRecipeRun || readiness.Decision.ProcessesWorkitem || readiness.Decision.CreatesWatcher || readiness.Decision.CreatesScheduler || readiness.Decision.CreatesHook || readiness.Decision.CreatesListener)
                blocking.Add(Issue("template-trigger-autorun-leak", RecipeReadinessStatus.BlockedLiveRuntimeDisabled, "Trigger readiness cannot start runs, process workitems, or create live listeners."));
        }
    }

    private static void EvaluateEvidence(
        RecipeTemplateDefinition template,
        RecipeTemplateReadinessContext context,
        List<RecipeReadinessIssue> blocking)
    {
        if (template.EvidenceRequirementRefs.Count > 0)
        {
            if (context.EvidencePack is null)
                blocking.Add(Issue("template-missing-evidence-pack", RecipeReadinessStatus.BlockedMissingEvidencePolicy, "Template requires evidence refs for fixture readiness."));
            else if (context.EvidencePack.CompletenessStatus != RecipeEvidenceCompleteness.Complete)
                blocking.Add(Issue("template-evidence-incomplete", RecipeReadinessStatus.BlockedMissingEvidencePolicy, "Template evidence pack is not complete."));
            else if (!context.EvidencePack.ReferenceOnly || context.EvidencePack.RedactionSummary.HasRawSecretExposure)
                blocking.Add(Issue("template-evidence-unsafe", RecipeReadinessStatus.BlockedMissingEvidencePolicy, "Template evidence must be reference-only and redacted."));
        }

        foreach (var validation in context.ValidationEvidence ?? [])
        {
            if (validation.Status == RecipeValidationEvidenceStatus.Failed && validation.BlockingSeverity == RecipeValidationSeverity.Blocking)
                blocking.Add(Issue("template-blocking-validation-failed", RecipeReadinessStatus.BlockedMissingEvidencePolicy, "Failed blocking validation prevents fixture-ready template status."));
        }
    }

    private static void EvaluateHumanApproval(
        RecipeTemplateDefinition template,
        RecipeTemplateReadinessContext context,
        List<RecipeReadinessIssue> blocking)
    {
        var requiresHumanOrApproval =
            template.SafetyProfile.RequiresHumanApproval ||
            template.SafetyProfile.SensitiveCategories.Count > 0 ||
            template.ApprovalHumanInterventionRequirementRefs.Count > 0;

        if (requiresHumanOrApproval && context.ApprovalNarrative is null)
            blocking.Add(Issue("template-missing-approval-narrative", RecipeReadinessStatus.BlockedMissingApprovalPolicy, "Sensitive template requires human/approval narrative."));

        if (template.SafetyProfile.SensitiveCategories.Any(c => c is SensitiveActionCategory.CaptchaOrChallenge or SensitiveActionCategory.TwoFactor))
            blocking.Add(Issue("template-challenge-human-required", RecipeReadinessStatus.BlockedRiskGate, "Challenge/2FA/CAPTCHA templates require human/block and cannot automate bypass."));
    }

    private static void EvaluateLabSafety(RecipeTemplateReadinessContext context, List<RecipeReadinessIssue> blocking)
    {
        if (context.LabSnapshot is null)
            return;

        if (context.LabSnapshot.CanStartRecipeRun || context.LabSnapshot.CanProcessWorkitems || context.LabSnapshot.CanUnlockLiveRuntime || context.LabSnapshot.RawSecretValuesExposed)
            blocking.Add(Issue("template-lab-unsafe", RecipeReadinessStatus.BlockedLiveRuntimeDisabled, "Recipe Lab snapshot must remain read-only, preview-safe, and redacted."));
    }

    private static RecipeTemplateStatus ToTemplateStatus(RecipeReadinessStatus status, RecipeTemplateDefinition template)
    {
        if (template.RuntimeEligibility == RecipeTemplateRuntimeEligibility.FutureGated)
            return RecipeTemplateStatus.FutureGated;

        if (template.RuntimeEligibility == RecipeTemplateRuntimeEligibility.LiveBlocked || status == RecipeReadinessStatus.BlockedLiveRuntimeDisabled)
            return RecipeTemplateStatus.LiveBlocked;

        return status switch
        {
            RecipeReadinessStatus.BlockedMissingToolTrust => RecipeTemplateStatus.MissingToolTrust,
            RecipeReadinessStatus.BlockedMissingSecretReference => RecipeTemplateStatus.MissingSecretRefs,
            RecipeReadinessStatus.BlockedMissingValidation => RecipeTemplateStatus.MissingValidation,
            RecipeReadinessStatus.BlockedMissingEvidencePolicy => RecipeTemplateStatus.MissingEvidence,
            RecipeReadinessStatus.BlockedMissingApprovalPolicy => RecipeTemplateStatus.MissingApprovalPath,
            RecipeReadinessStatus.BlockedRiskGate or RecipeReadinessStatus.BlockedActionResolutionPolicy or RecipeReadinessStatus.BlockedByProtectedScope => RecipeTemplateStatus.BlockedByPolicy,
            _ => RecipeTemplateStatus.BlockedByPolicy
        };
    }

    private static RecipeRiskProfile EmptyRisk(RecipeTemplateDefinition template) =>
        new($"risk:{template.TemplateId}", template.SafetyProfile.RiskLevel, new HashSet<SensitiveActionCategory>(template.SafetyProfile.SensitiveCategories), [], template.SafetyProfile.RequiresHumanApproval, template.SafetyProfile.RequiresHumanApproval, template.RequiredSecretRefs, SecretValuesExposed: false);

    private static RecipeReadinessIssue Issue(string id, RecipeReadinessStatus status, string message) =>
        new(id, status, RecipeReadinessIssueSeverity.Blocking, message);
}

public static class RecipeTemplateCatalogFactory
{
    public static RecipeTemplateCatalog CreateGlobalLatamV1()
    {
        var packs = new[]
        {
            Pack("pack.excel.microsoft365", "Excel / Microsoft 365 Pack", RecipeTemplateCategory.ExcelMicrosoft365, RecipeTemplateRegion.Global,
            [
                Template("excel.extract_rows_to_workitems", "Extract rows to workitems", RecipeTemplateCategory.ExcelMicrosoft365, RecipeTemplateSystem.Excel, RecipeTemplateRegion.Global, [RecipeTemplateCountry.Global], RecipeRiskLevel.Low, [], RecipeTemplateRuntimeEligibility.FixtureOnly, RecipeTemplateStatus.CatalogPreview, RecipeBlockType.Extract, "tool.excel.fixture", [], [], "Extract spreadsheet rows into fixture workitem drafts."),
                Template("excel.reconcile_two_files", "Reconcile two files", RecipeTemplateCategory.ExcelMicrosoft365, RecipeTemplateSystem.Excel, RecipeTemplateRegion.Global, [RecipeTemplateCountry.Global], RecipeRiskLevel.Medium, [], RecipeTemplateRuntimeEligibility.FixtureOnly, RecipeTemplateStatus.CatalogPreview, RecipeBlockType.Validate, "tool.excel.fixture", [], [], "Compare two file refs and produce validation evidence."),
                Template("excel.generate_report_with_validation", "Generate report with validation", RecipeTemplateCategory.ExcelMicrosoft365, RecipeTemplateSystem.Microsoft365, RecipeTemplateRegion.Global, [RecipeTemplateCountry.Global], RecipeRiskLevel.Medium, [], RecipeTemplateRuntimeEligibility.FixtureOnly, RecipeTemplateStatus.CatalogPreview, RecipeBlockType.CaptureArtifact, "tool.excel.fixture", [], [], "Generate report artifact refs with required validation."),
                Template("excel.normalize_supplier_price_list", "Normalize supplier price list", RecipeTemplateCategory.ExcelMicrosoft365, RecipeTemplateSystem.Excel, RecipeTemplateRegion.Global, [RecipeTemplateCountry.Global], RecipeRiskLevel.Medium, [SensitiveActionCategory.PriceOrStockChange], RecipeTemplateRuntimeEligibility.FixtureOnly, RecipeTemplateStatus.CatalogPreview, RecipeBlockType.Validate, "tool.excel.fixture", [], ["approval.price.review"], "Normalize supplier price data for review."),
                Template("excel.bank_statement_reconciliation_preview", "Bank statement reconciliation preview", RecipeTemplateCategory.ExcelMicrosoft365, RecipeTemplateSystem.Excel, RecipeTemplateRegion.Global, [RecipeTemplateCountry.Global], RecipeRiskLevel.High, [SensitiveActionCategory.Payment, SensitiveActionCategory.PersonalDataHandling], RecipeTemplateRuntimeEligibility.FixtureOnly, RecipeTemplateStatus.CatalogPreview, RecipeBlockType.Validate, "tool.excel.fixture", [], ["approval.bank.review"], "Preview bank statement reconciliation by reference.")
            ]),
            Pack("pack.google.workspace", "Google Workspace Pack", RecipeTemplateCategory.GoogleWorkspace, RecipeTemplateRegion.Global,
            [
                Template("google.sheets_extract_rows_to_workitems", "Sheets extract rows to workitems", RecipeTemplateCategory.GoogleWorkspace, RecipeTemplateSystem.GoogleSheets, RecipeTemplateRegion.Global, [RecipeTemplateCountry.Global], RecipeRiskLevel.Low, [], RecipeTemplateRuntimeEligibility.FixtureOnly, RecipeTemplateStatus.CatalogPreview, RecipeBlockType.Extract, "tool.google.fixture", [], [], "Extract rows from fixture Google Sheets refs."),
                Template("google.drive_file_intake_preview", "Drive file intake preview", RecipeTemplateCategory.GoogleWorkspace, RecipeTemplateSystem.GoogleDrive, RecipeTemplateRegion.Global, [RecipeTemplateCountry.Global], RecipeRiskLevel.Medium, [SensitiveActionCategory.PersonalDataHandling], RecipeTemplateRuntimeEligibility.FixtureOnly, RecipeTemplateStatus.CatalogPreview, RecipeBlockType.Extract, "tool.google.fixture", [], ["approval.drive.review"], "Create a redacted review queue from Drive file refs."),
                Template("google.gmail_attachment_to_review_queue", "Gmail attachment to review queue", RecipeTemplateCategory.GoogleWorkspace, RecipeTemplateSystem.Gmail, RecipeTemplateRegion.Global, [RecipeTemplateCountry.Global], RecipeRiskLevel.High, [SensitiveActionCategory.PersonalDataHandling], RecipeTemplateRuntimeEligibility.FixtureOnly, RecipeTemplateStatus.CatalogPreview, RecipeBlockType.Extract, "tool.google.fixture", [], ["approval.gmail.review"], "Create review queue drafts from attachment refs; no email sending."),
                Template("google.calendar_event_to_workitem_draft", "Calendar event to workitem draft", RecipeTemplateCategory.GoogleWorkspace, RecipeTemplateSystem.GoogleCalendar, RecipeTemplateRegion.Global, [RecipeTemplateCountry.Global], RecipeRiskLevel.Low, [], RecipeTemplateRuntimeEligibility.FixtureOnly, RecipeTemplateStatus.CatalogPreview, RecipeBlockType.Extract, "tool.google.fixture", [], [], "Convert fixture calendar event refs into draft workitems.")
            ]),
            Pack("pack.sap", "SAP Pack", RecipeTemplateCategory.SAP, RecipeTemplateRegion.Global,
            [
                ConnectorTemplate("sap.export_report_and_verify", "SAP export report and verify", RecipeTemplateCategory.SAP, RecipeTemplateSystem.SAP, RecipeTemplateRegion.Global, [RecipeTemplateCountry.Global], RecipeRiskLevel.High, [SensitiveActionCategory.ExternalSystemMutation], RecipeTemplateRuntimeEligibility.FutureGated, "tool.sap.future", ["secret.sap.ref"], RecipeConnectorActionCategory.ReadData, "SAP report export is future connector-gated; no SAP GUI automation or RFC/BAPI/OData calls."),
                ConnectorTemplate("sap.purchase_order_status_check", "SAP purchase order status check", RecipeTemplateCategory.SAP, RecipeTemplateSystem.SAP, RecipeTemplateRegion.Global, [RecipeTemplateCountry.Global], RecipeRiskLevel.High, [SensitiveActionCategory.ExternalSystemMutation], RecipeTemplateRuntimeEligibility.FutureGated, "tool.sap.future", ["secret.sap.ref"], RecipeConnectorActionCategory.ReadData, "SAP PO status check remains reference/fixture-only."),
                ConnectorTemplate("sap.vendor_invoice_validation_draft", "SAP vendor invoice validation draft", RecipeTemplateCategory.SAP, RecipeTemplateSystem.SAP, RecipeTemplateRegion.Global, [RecipeTemplateCountry.Global], RecipeRiskLevel.High, [SensitiveActionCategory.ExternalSystemMutation, SensitiveActionCategory.FiscalOrLegalSubmission], RecipeTemplateRuntimeEligibility.FutureGated, "tool.sap.future", ["secret.sap.ref"], RecipeConnectorActionCategory.WriteDraft, "SAP vendor invoice draft requires human approval."),
                ConnectorTemplate("sap.material_master_lookup_preview", "SAP material master lookup preview", RecipeTemplateCategory.SAP, RecipeTemplateSystem.SAP, RecipeTemplateRegion.Global, [RecipeTemplateCountry.Global], RecipeRiskLevel.Medium, [], RecipeTemplateRuntimeEligibility.FutureGated, "tool.sap.future", ["secret.sap.ref"], RecipeConnectorActionCategory.ReadData, "SAP material lookup is future connector-gated."),
                ConnectorTemplate("sap.sales_order_from_excel_draft", "SAP sales order from Excel draft", RecipeTemplateCategory.SAP, RecipeTemplateSystem.SAP, RecipeTemplateRegion.Global, [RecipeTemplateCountry.Global], RecipeRiskLevel.High, [SensitiveActionCategory.ExternalSystemMutation], RecipeTemplateRuntimeEligibility.FutureGated, "tool.sap.future", ["secret.sap.ref"], RecipeConnectorActionCategory.WriteDraft, "Sales order draft only; no SAP mutation.")
            ]),
            Pack("pack.mercado", "Mercado Libre / Mercado Pago Pack", RecipeTemplateCategory.MercadoLibreMercadoPago, RecipeTemplateRegion.LATAM,
            [
                ConnectorTemplate("meli.import_orders_preview", "Mercado Libre import orders preview", RecipeTemplateCategory.MercadoLibreMercadoPago, RecipeTemplateSystem.MercadoLibre, RecipeTemplateRegion.LATAM, LatamCountries(), RecipeRiskLevel.High, [SensitiveActionCategory.MarketplaceListingChange], RecipeTemplateRuntimeEligibility.FutureGated, "tool.meli.future", ["secret.meli.ref"], RecipeConnectorActionCategory.ReadData, "Import order refs for review only; no API calls."),
                ConnectorTemplate("meli.sync_stock_from_erp_draft", "Mercado Libre sync stock from ERP draft", RecipeTemplateCategory.MercadoLibreMercadoPago, RecipeTemplateSystem.MercadoLibre, RecipeTemplateRegion.LATAM, LatamCountries(), RecipeRiskLevel.Critical, [SensitiveActionCategory.PriceOrStockChange, SensitiveActionCategory.ExternalSystemMutation], RecipeTemplateRuntimeEligibility.FutureGated, "tool.meli.future", ["secret.meli.ref"], RecipeConnectorActionCategory.UpdateStock, "Stock sync is draft/human gated and live-blocked."),
                ConnectorTemplate("meli.sync_prices_from_excel_draft", "Mercado Libre sync prices from Excel draft", RecipeTemplateCategory.MercadoLibreMercadoPago, RecipeTemplateSystem.MercadoLibre, RecipeTemplateRegion.LATAM, LatamCountries(), RecipeRiskLevel.Critical, [SensitiveActionCategory.PriceOrStockChange], RecipeTemplateRuntimeEligibility.FutureGated, "tool.meli.future", ["secret.meli.ref"], RecipeConnectorActionCategory.UpdatePrice, "Price update is draft/human gated and live-blocked."),
                ConnectorTemplate("meli.reconcile_orders_with_mercadopago_preview", "Mercado Pago reconciliation preview", RecipeTemplateCategory.MercadoLibreMercadoPago, RecipeTemplateSystem.MercadoPago, RecipeTemplateRegion.LATAM, LatamCountries(), RecipeRiskLevel.High, [SensitiveActionCategory.Payment], RecipeTemplateRuntimeEligibility.FutureGated, "tool.mercadopago.future", ["secret.mp.ref"], RecipeConnectorActionCategory.ReadData, "Payment reconciliation preview only; no payment execution."),
                ConnectorTemplate("meli.claims_disputes_review_queue", "Claims and disputes review queue", RecipeTemplateCategory.MercadoLibreMercadoPago, RecipeTemplateSystem.MercadoLibre, RecipeTemplateRegion.LATAM, LatamCountries(), RecipeRiskLevel.High, [SensitiveActionCategory.EmailOrMessageSend, SensitiveActionCategory.MarketplaceListingChange], RecipeTemplateRuntimeEligibility.FutureGated, "tool.meli.future", ["secret.meli.ref"], RecipeConnectorActionCategory.SendMessage, "Claims and disputes produce review queue drafts only."),
                ConnectorTemplate("meli.publish_listing_draft_review", "Publish listing draft review", RecipeTemplateCategory.MercadoLibreMercadoPago, RecipeTemplateSystem.MercadoLibre, RecipeTemplateRegion.LATAM, LatamCountries(), RecipeRiskLevel.Critical, [SensitiveActionCategory.PublicPosting, SensitiveActionCategory.MarketplaceListingChange], RecipeTemplateRuntimeEligibility.FutureGated, "tool.meli.future", ["secret.meli.ref"], RecipeConnectorActionCategory.PublishListing, "Listing publication requires human review and remains live-blocked.")
            ]),
            Pack("pack.arca.fiscal", "ARCA Argentina / Fiscal Pack", RecipeTemplateCategory.ARCAFiscal, RecipeTemplateRegion.Argentina,
            [
                ConnectorTemplate("arca.validate_cuit_preview", "ARCA CUIT validation preview", RecipeTemplateCategory.ARCAFiscal, RecipeTemplateSystem.ARCA, RecipeTemplateRegion.Argentina, [RecipeTemplateCountry.Argentina], RecipeRiskLevel.High, [SensitiveActionCategory.FiscalOrLegalSubmission], RecipeTemplateRuntimeEligibility.FutureGated, "tool.arca.future", ["secret.arca.ref"], RecipeConnectorActionCategory.ReadData, "CUIT validation uses fixture refs only; no ARCA calls."),
                ConnectorTemplate("arca.prepare_invoice_draft_review", "Prepare invoice draft review", RecipeTemplateCategory.ARCAFiscal, RecipeTemplateSystem.ARCA, RecipeTemplateRegion.Argentina, [RecipeTemplateCountry.Argentina], RecipeRiskLevel.Critical, [SensitiveActionCategory.FiscalOrLegalSubmission], RecipeTemplateRuntimeEligibility.FutureGated, "tool.arca.future", ["secret.arca.ref"], RecipeConnectorActionCategory.WriteDraft, "Invoice draft review only; no fiscal submission."),
                ConnectorTemplate("arca.constatar_comprobante_preview", "Constatar comprobante preview", RecipeTemplateCategory.ARCAFiscal, RecipeTemplateSystem.ARCA, RecipeTemplateRegion.Argentina, [RecipeTemplateCountry.Argentina], RecipeRiskLevel.High, [SensitiveActionCategory.FiscalOrLegalSubmission], RecipeTemplateRuntimeEligibility.FutureGated, "tool.arca.future", ["secret.arca.ref"], RecipeConnectorActionCategory.ReadData, "Comprobante preview only; no web service call."),
                ConnectorTemplate("arca.monthly_invoice_batch_review", "Monthly invoice batch review", RecipeTemplateCategory.ARCAFiscal, RecipeTemplateSystem.ARCA, RecipeTemplateRegion.Argentina, [RecipeTemplateCountry.Argentina], RecipeRiskLevel.Critical, [SensitiveActionCategory.FiscalOrLegalSubmission], RecipeTemplateRuntimeEligibility.FutureGated, "tool.arca.future", ["secret.arca.ref"], RecipeConnectorActionCategory.WriteDraft, "Batch review requires human approval."),
                ConnectorTemplate("arca.fiscal_submission_human_review", "Fiscal submission human review", RecipeTemplateCategory.ARCAFiscal, RecipeTemplateSystem.Fiscal, RecipeTemplateRegion.Argentina, [RecipeTemplateCountry.Argentina], RecipeRiskLevel.Critical, [SensitiveActionCategory.FiscalOrLegalSubmission], RecipeTemplateRuntimeEligibility.LiveBlocked, "tool.arca.future", ["secret.arca.ref"], RecipeConnectorActionCategory.SubmitFiscal, "Fiscal submission remains human review and live-blocked.")
            ]),
            Pack("pack.erp.local.latam", "ERP Local LATAM Pack", RecipeTemplateCategory.ERPLocalLATAM, RecipeTemplateRegion.LATAM,
            [
                ConnectorTemplate("erp.import_marketplace_orders_preview", "Import marketplace orders preview", RecipeTemplateCategory.ERPLocalLATAM, RecipeTemplateSystem.ERPLocalLATAM, RecipeTemplateRegion.LATAM, LatamCountries(), RecipeRiskLevel.High, [SensitiveActionCategory.ExternalSystemMutation], RecipeTemplateRuntimeEligibility.FutureGated, "tool.erp.future", ["secret.erp.ref"], RecipeConnectorActionCategory.ReadData, "ERP families: Tango, Bejerman, Contabilium, Alegra, Siigo, Odoo, TOTVS, CONTPAQi, Aspel."),
                ConnectorTemplate("erp.sync_stock_to_marketplaces_draft", "Sync stock to marketplaces draft", RecipeTemplateCategory.ERPLocalLATAM, RecipeTemplateSystem.ERPLocalLATAM, RecipeTemplateRegion.LATAM, LatamCountries(), RecipeRiskLevel.Critical, [SensitiveActionCategory.PriceOrStockChange, SensitiveActionCategory.ExternalSystemMutation], RecipeTemplateRuntimeEligibility.FutureGated, "tool.erp.future", ["secret.erp.ref"], RecipeConnectorActionCategory.UpdateStock, "Stock mutation draft only."),
                ConnectorTemplate("erp.create_invoice_from_order_draft", "Create invoice from order draft", RecipeTemplateCategory.ERPLocalLATAM, RecipeTemplateSystem.ERPLocalLATAM, RecipeTemplateRegion.LATAM, LatamCountries(), RecipeRiskLevel.Critical, [SensitiveActionCategory.FiscalOrLegalSubmission, SensitiveActionCategory.ExternalSystemMutation], RecipeTemplateRuntimeEligibility.FutureGated, "tool.erp.future", ["secret.erp.ref"], RecipeConnectorActionCategory.WriteDraft, "Invoice draft only; no ERP connector execution."),
                ConnectorTemplate("erp.price_list_update_review", "Price list update review", RecipeTemplateCategory.ERPLocalLATAM, RecipeTemplateSystem.ERPLocalLATAM, RecipeTemplateRegion.LATAM, LatamCountries(), RecipeRiskLevel.Critical, [SensitiveActionCategory.PriceOrStockChange], RecipeTemplateRuntimeEligibility.FutureGated, "tool.erp.future", ["secret.erp.ref"], RecipeConnectorActionCategory.UpdatePrice, "Price list update review only."),
                ConnectorTemplate("erp.cash_register_close_review", "Cash register close review", RecipeTemplateCategory.ERPLocalLATAM, RecipeTemplateSystem.ERPLocalLATAM, RecipeTemplateRegion.LATAM, LatamCountries(), RecipeRiskLevel.Critical, [SensitiveActionCategory.Payment, SensitiveActionCategory.FiscalOrLegalSubmission], RecipeTemplateRuntimeEligibility.FutureGated, "tool.erp.future", ["secret.erp.ref"], RecipeConnectorActionCategory.WriteDraft, "Cash register close remains human gated."),
                ConnectorTemplate("erp.accounting_export_preview", "Accounting export preview", RecipeTemplateCategory.ERPLocalLATAM, RecipeTemplateSystem.ERPLocalLATAM, RecipeTemplateRegion.LATAM, LatamCountries(), RecipeRiskLevel.High, [SensitiveActionCategory.FiscalOrLegalSubmission], RecipeTemplateRuntimeEligibility.FutureGated, "tool.erp.future", ["secret.erp.ref"], RecipeConnectorActionCategory.ReadData, "Accounting export preview only.")
            ]),
            Pack("pack.browser.portals", "Generic Browser Portals Pack", RecipeTemplateCategory.GenericBrowserPortal, RecipeTemplateRegion.Global,
            [
                BrowserTemplate("browser.portal_login_readiness_check", "Portal login readiness check", RecipeRiskLevel.High, [SensitiveActionCategory.Login, SensitiveActionCategory.TwoFactor], "Login readiness check only; no real login and no 2FA/CAPTCHA bypass."),
                BrowserTemplate("browser.form_fill_draft_review", "Form fill draft review", RecipeRiskLevel.High, [SensitiveActionCategory.DataMutation], "Form fill draft review only; no browser automation."),
                BrowserTemplate("browser.table_extract_preview", "Table extract preview", RecipeRiskLevel.Medium, [], "Table extraction preview with fixture refs only."),
                BrowserTemplate("browser.download_file_evidence_preview", "Download file evidence preview", RecipeRiskLevel.Medium, [], "Download evidence preview by ref only; no browser download."),
                BrowserTemplate("browser.session_expired_detector_preview", "Session expired detector preview", RecipeRiskLevel.High, [SensitiveActionCategory.Login], "Session-expired detector is observe-only; no browser listener.")
            ]),
            Pack("pack.computeruse.legacy", "Computer Use Legacy Pack", RecipeTemplateCategory.ComputerUseLegacy, RecipeTemplateRegion.Global,
            [
                DesktopTemplate("desktop.legacy_app_export_report_preview", "Legacy app export report preview", "Desktop export report playbook only; no UIA/vision execution."),
                DesktopTemplate("desktop.popup_recovery_playbook", "Popup recovery playbook", "Popup recovery is manual playbook only."),
                DesktopTemplate("desktop.file_drop_intake_preview", "File drop intake preview", "File drop intake uses fixture refs only; no watcher."),
                DesktopTemplate("desktop.manual_checkpoint_workflow", "Manual checkpoint workflow", "Manual checkpoint is human-only and cannot continue live execution."),
                DesktopTemplate("desktop.hotkey_lookup_playbook", "Hotkey lookup playbook", "Hotkey lookup is a documentation playbook; no OS hook/hotkey listener.")
            ])
        };

        return new(
            "recipe-template-catalog-global-latam-v1",
            "1.0.0",
            packs,
            "Global + LATAM Recipe Templates Pack v1 is contract-only, fixture-safe, and live-blocked by default.");
    }

    private static RecipeTemplatePack Pack(string id, string displayName, RecipeTemplateCategory category, RecipeTemplateRegion region, IReadOnlyList<RecipeTemplateDefinition> templates) =>
        new(id, displayName, category, region, templates, "Fixture-safe template pack. No real connector, browser, desktop, API, vault, scheduler, or replay execution.");

    private static RecipeTemplateDefinition Template(
        string id,
        string displayName,
        RecipeTemplateCategory category,
        RecipeTemplateSystem system,
        RecipeTemplateRegion region,
        IReadOnlyList<RecipeTemplateCountry> countries,
        RecipeRiskLevel risk,
        IReadOnlyList<SensitiveActionCategory> sensitive,
        RecipeTemplateRuntimeEligibility eligibility,
        RecipeTemplateStatus status,
        RecipeBlockType blockType,
        string toolRef,
        IReadOnlyList<string> secretRefs,
        IReadOnlyList<string> approvalRefs,
        string summary) =>
        BuildTemplate(id, displayName, category, system, region, countries, risk, sensitive, eligibility, status, blockType, toolRef, secretRefs, connectorEligibilityRefs: [], approvalRefs, summary);

    private static RecipeTemplateDefinition ConnectorTemplate(
        string id,
        string displayName,
        RecipeTemplateCategory category,
        RecipeTemplateSystem system,
        RecipeTemplateRegion region,
        IReadOnlyList<RecipeTemplateCountry> countries,
        RecipeRiskLevel risk,
        IReadOnlyList<SensitiveActionCategory> sensitive,
        RecipeTemplateRuntimeEligibility eligibility,
        string toolRef,
        IReadOnlyList<string> secretRefs,
        RecipeConnectorActionCategory action,
        string summary) =>
        BuildTemplate(id, displayName, category, system, region, countries, risk, sensitive, eligibility, eligibility == RecipeTemplateRuntimeEligibility.LiveBlocked ? RecipeTemplateStatus.LiveBlocked : RecipeTemplateStatus.FutureGated, RecipeBlockType.ConnectorDraft, toolRef, secretRefs, [$"connector:{id}:{action}"], [$"approval:{id}"], summary);

    private static RecipeTemplateDefinition BrowserTemplate(string id, string displayName, RecipeRiskLevel risk, IReadOnlyList<SensitiveActionCategory> sensitive, string summary) =>
        BuildTemplate(id, displayName, RecipeTemplateCategory.GenericBrowserPortal, RecipeTemplateSystem.GenericBrowserPortal, RecipeTemplateRegion.Global, [RecipeTemplateCountry.Global], risk, sensitive, RecipeTemplateRuntimeEligibility.LiveBlocked, RecipeTemplateStatus.LiveBlocked, RecipeBlockType.BrowserAction, "tool.browser.runtime", ["secret.browser.ref"], [$"connector:{id}:ReadData"], [$"approval:{id}"], summary);

    private static RecipeTemplateDefinition DesktopTemplate(string id, string displayName, string summary) =>
        BuildTemplate(id, displayName, RecipeTemplateCategory.ComputerUseLegacy, RecipeTemplateSystem.ComputerUseLegacy, RecipeTemplateRegion.Global, [RecipeTemplateCountry.Global], RecipeRiskLevel.High, [SensitiveActionCategory.DesktopLiveAction], RecipeTemplateRuntimeEligibility.LiveBlocked, RecipeTemplateStatus.LiveBlocked, RecipeBlockType.DesktopActionDraft, "tool.desktop.runtime", [], [], [$"approval:{id}"], summary);

    private static RecipeTemplateDefinition BuildTemplate(
        string id,
        string displayName,
        RecipeTemplateCategory category,
        RecipeTemplateSystem system,
        RecipeTemplateRegion region,
        IReadOnlyList<RecipeTemplateCountry> countries,
        RecipeRiskLevel risk,
        IReadOnlyList<SensitiveActionCategory> sensitive,
        RecipeTemplateRuntimeEligibility eligibility,
        RecipeTemplateStatus status,
        RecipeBlockType blockType,
        string toolRef,
        IReadOnlyList<string> secretRefs,
        IReadOnlyList<string> connectorEligibilityRefs,
        IReadOnlyList<string> approvalRefs,
        string summary)
    {
        var block = new RecipeBlock(
            "block.main",
            blockType,
            displayName,
            summary,
            TargetRef: $"target:{id}",
            InputBinding: $"input:{id}",
            OutputBinding: $"output:{id}",
            Preconditions: [],
            Postconditions: ["post.validation.ref"],
            ValidationRefs: ["validation.main"],
            risk,
            approvalRefs.Count > 0 ? RecipeApprovalRequirement.Required : RecipeApprovalRequirement.None,
            EvidenceExpectationRef: "evidence.main",
            FailurePolicyRef: "failure.policy",
            NextBlockRefs: []);

        var recipe = new RecipeDefinition(id)
        {
            RecipeId = $"recipe:{id}",
            DisplayName = displayName,
            Version = "8.0.0",
            Category = category.ToString(),
            SystemTarget = system.ToString(),
            RegionCountry = $"{region}:{string.Join(",", countries)}",
            RequiredCapabilities = ["recipe-template-catalog", "fixture-safe-preview"],
            RequiredToolTrustRefs = [toolRef],
            RequiredSecretRefs = secretRefs.ToList(),
            ConnectorEligibilityRefs = connectorEligibilityRefs.ToList(),
            TriggerRefs = [$"trigger:{id}:manual"],
            DetectorRefs = [$"detector:{id}:fixture"],
            OutputSchemaRef = $"schema:{id}:output",
            Blocks = [block],
            RunLimits = new(MaxSteps: 12, MaxRuntimeSeconds: 120, MaxRetries: 1, MaxLoopIterations: 3, MaxNestedLoops: 1, MaxWorkitemsPerRun: 50, MaxExternalSystemCalls: 0, LiveRuntimeAllowed: false),
            CompleteCriteria = new([new("complete.output", RecipeCompleteCriterionType.ExpectedOutputExists, $"output:{id}")]),
            TerminateCriteria = new([
                new("terminate.policy", RecipeTerminateCriterionType.PolicyBlocked, "policy.blocked"),
                new("terminate.human", RecipeTerminateCriterionType.HumanInterventionRequired, "human.ref"),
                new("terminate.unsafe", RecipeTerminateCriterionType.UnknownUnsafeState, "unsafe.ref")
            ]),
            ValidationPolicy = new([new("validation.main", RecipeValidationKind.EvidenceRefExists, RecipeValidationSeverity.Blocking, AppliesToBlockId: "block.main", PostValidation: true)]),
            RuntimeRiskProfile = new(
                $"risk:{id}",
                risk,
                new HashSet<SensitiveActionCategory>(sensitive),
                [],
                ApprovalPolicyPresent: approvalRefs.Count > 0 || risk is RecipeRiskLevel.High or RecipeRiskLevel.Critical or RecipeRiskLevel.Blocked,
                HumanInterventionPathPresent: approvalRefs.Count > 0 || sensitive.Count > 0,
                secretRefs,
                SecretValuesExposed: false),
            ActionResolutionPolicy = new([
                new ActionResolutionAttempt(1, ActionResolutionStrategy.KnownTarget, $"target:{id}", "evidence.main"),
                new ActionResolutionAttempt(2, ActionResolutionStrategy.HumanHandoff, $"human:{id}", "evidence.handoff")
            ]),
            ApprovalCheckpointRefs = approvalRefs.ToList(),
            EvidenceExpectationRefs = ["evidence.main"]
        };

        return new(
            id,
            displayName,
            summary,
            PackId(category),
            category,
            system,
            region,
            countries,
            recipe,
            recipe.RequiredCapabilities,
            recipe.RequiredToolTrustRefs,
            recipe.RequiredSecretRefs,
            connectorEligibilityRefs,
            recipe.TriggerRefs,
            recipe.EvidenceExpectationRefs,
            recipe.ValidationPolicy.Requirements.Select(r => r.RequirementId).ToArray(),
            approvalRefs,
            new RecipeTemplateSafetyProfile(risk, sensitive, approvalRefs.Count > 0 || sensitive.Count > 0, LiveRuntimeBlocked: eligibility is RecipeTemplateRuntimeEligibility.LiveBlocked or RecipeTemplateRuntimeEligibility.FutureGated),
            [RecipeRunMode.CatalogPreview, RecipeRunMode.FixtureRun],
            [RecipeRunMode.LiveRunBlocked, RecipeRunMode.LiveRunAllowedFuture],
            eligibility,
            status,
            new RecipeSafeNextAction(eligibility is RecipeTemplateRuntimeEligibility.FixtureOnly or RecipeTemplateRuntimeEligibility.ReferenceOnly ? RecipeSafeNextActionKind.ContinueFixtureOnly : RecipeSafeNextActionKind.KeepBlocked, eligibility is RecipeTemplateRuntimeEligibility.FixtureOnly or RecipeTemplateRuntimeEligibility.ReferenceOnly ? "Continue fixture-safe preview only." : "Keep blocked until human policy and future external GO."),
            summary,
            eligibility is RecipeTemplateRuntimeEligibility.LiveBlocked or RecipeTemplateRuntimeEligibility.FutureGated ? "DISABLED_BY_POLICY" : "NOT_ENABLED",
            [$"fixture:{id}:sample"],
            "No live execution, no real connector calls, no raw secrets, no browser/desktop automation, no scheduler, no replay.");
    }

    private static string PackId(RecipeTemplateCategory category) =>
        category switch
        {
            RecipeTemplateCategory.ExcelMicrosoft365 => "pack.excel.microsoft365",
            RecipeTemplateCategory.GoogleWorkspace => "pack.google.workspace",
            RecipeTemplateCategory.SAP => "pack.sap",
            RecipeTemplateCategory.MercadoLibreMercadoPago => "pack.mercado",
            RecipeTemplateCategory.ARCAFiscal => "pack.arca.fiscal",
            RecipeTemplateCategory.ERPLocalLATAM => "pack.erp.local.latam",
            RecipeTemplateCategory.GenericBrowserPortal => "pack.browser.portals",
            RecipeTemplateCategory.ComputerUseLegacy => "pack.computeruse.legacy",
            _ => "pack.unknown"
        };

    private static IReadOnlyList<RecipeTemplateCountry> LatamCountries() =>
        [
            RecipeTemplateCountry.Argentina,
            RecipeTemplateCountry.Brazil,
            RecipeTemplateCountry.Mexico,
            RecipeTemplateCountry.Chile,
            RecipeTemplateCountry.Colombia,
            RecipeTemplateCountry.Peru,
            RecipeTemplateCountry.Uruguay,
            RecipeTemplateCountry.Paraguay,
            RecipeTemplateCountry.Ecuador
        ];
}
