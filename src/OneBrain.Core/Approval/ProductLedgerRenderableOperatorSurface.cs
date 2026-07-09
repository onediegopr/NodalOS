using System.Text;

namespace OneBrain.Core.Approval;

public enum ProductLedgerRenderableOperatorSurfaceDecision
{
    Rejected,
    RenderedSnapshot
}

public enum ProductLedgerRenderableOperatorSurfaceBlocker
{
    MissingRequest,
    MissingExplicitLocalOnlySnapshotScope,
    MissingPublicActionSurface,
    UnsafePublicActionSurface,
    EndpointRouteControllerClaimed,
    ExternalScriptClaimed,
    TelemetryOrSyncClaimed,
    ProviderCloudNetworkClaimed,
    DbMigrationClaimed,
    KmsWormExternalTrustClaimed,
    BrowserCdpWcuOcrRecipesLiveClaimed,
    ReleaseCommercialClaimed,
    RawPayloadOrSecretClaimed
}

public sealed record ProductLedgerRenderableOperatorSurfaceRequest(
    bool ExplicitLocalOnlySnapshotScope,
    ProductLedgerPublicUiActionResult? PublicActionSurface,
    bool ClaimsEndpointRouteController,
    bool ClaimsExternalScript,
    bool ClaimsTelemetryOrSync,
    bool ClaimsProviderCloudNetwork,
    bool ClaimsDbMigration,
    bool ClaimsKmsWormExternalTrust,
    bool ClaimsBrowserCdpWcuOcrRecipesLive,
    bool ClaimsReleaseCommercial,
    bool ClaimsRawPayloadOrSecret);

public sealed record ProductLedgerRenderableOperatorSurfaceActionModel(
    string ActionId,
    string Label,
    string RiskLabel,
    string RequiredEvidence,
    bool SafeAction,
    bool Dangerous,
    bool Disabled,
    bool LocalOnly,
    bool NonDestructive,
    bool Bounded,
    string DisabledReason);

public sealed record ProductLedgerRenderableOperatorSurfaceModel(
    string SnapshotId,
    string Title,
    string StatusText,
    int RenderableOperatorSnapshotPercent,
    int DomContractPercent,
    int UxSafetyPercent,
    int ExternalCloudReadinessPercent,
    int KmsWormExternalTrustPercent,
    int ReleaseCommercialReadinessPercent,
    IReadOnlyList<string> Notices,
    IReadOnlyList<string> Sections,
    IReadOnlyList<ProductLedgerRenderableOperatorSurfaceActionModel> Actions,
    IReadOnlyList<string> Warnings,
    string SafeNextStep,
    bool LocalOnly,
    bool InternalOnly,
    bool SnapshotOnly,
    bool Deterministic,
    bool FailClosed,
    bool EndpointRouteControllerAvailable,
    bool ExternalScriptAvailable,
    bool TelemetryOrSyncAvailable,
    bool ProviderCloudNetworkAvailable,
    bool DbMigrationAvailable,
    bool KmsWormExternalTrustAvailable,
    bool BrowserCdpWcuOcrRecipesLiveAvailable,
    bool ReleaseCommercialReady,
    bool RawPayloadOrSecretAvailable);

public sealed record ProductLedgerRenderableOperatorSurfaceResult(
    ProductLedgerRenderableOperatorSurfaceDecision Decision,
    IReadOnlyList<ProductLedgerRenderableOperatorSurfaceBlocker> Blockers,
    ProductLedgerRenderableOperatorSurfaceModel Model,
    string HtmlSnapshot);

public sealed class ProductLedgerRenderableOperatorSurfaceRenderer
{
    public const string ReadyStatus =
        "PRODUCT_LEDGER_RENDERABLE_OPERATOR_SURFACE_SNAPSHOT_READY LOCAL_ONLY INTERNAL_ONLY SNAPSHOT_ONLY DETERMINISTIC NO_PUBLIC_ROUTE NO_ENDPOINT_CONTROLLER NO_EXTERNAL_SCRIPT NO_TELEMETRY NO_PROVIDER_CLOUD_NETWORK NO_DB_MIGRATION NO_KMS_WORM_EXTERNAL_TRUST NO_LIVE_AUTOMATION NO_RELEASE_COMMERCIAL";

    public const string RejectedStatus =
        "PRODUCT_LEDGER_RENDERABLE_OPERATOR_SURFACE_SNAPSHOT_REJECTED FAIL_CLOSED NO_PUBLIC_ROUTE NO_ENDPOINT_CONTROLLER NO_EXTERNAL_SCRIPT NO_TELEMETRY NO_PROVIDER_CLOUD_NETWORK NO_DB_MIGRATION NO_KMS_WORM_EXTERNAL_TRUST NO_LIVE_AUTOMATION NO_RELEASE_COMMERCIAL";

    public ProductLedgerRenderableOperatorSurfaceResult Render(ProductLedgerRenderableOperatorSurfaceRequest? request)
    {
        var blockers = new List<ProductLedgerRenderableOperatorSurfaceBlocker>();
        if (request is null)
        {
            blockers.Add(ProductLedgerRenderableOperatorSurfaceBlocker.MissingRequest);
            return Result(blockers, null);
        }

        AddRequestBlockers(request, blockers);
        AddActionSurfaceBlockers(request.PublicActionSurface, blockers);
        return Result(blockers, request.PublicActionSurface);
    }

    private static void AddRequestBlockers(
        ProductLedgerRenderableOperatorSurfaceRequest request,
        List<ProductLedgerRenderableOperatorSurfaceBlocker> blockers)
    {
        if (!request.ExplicitLocalOnlySnapshotScope)
        {
            blockers.Add(ProductLedgerRenderableOperatorSurfaceBlocker.MissingExplicitLocalOnlySnapshotScope);
        }

        if (request.ClaimsEndpointRouteController)
        {
            blockers.Add(ProductLedgerRenderableOperatorSurfaceBlocker.EndpointRouteControllerClaimed);
        }

        if (request.ClaimsExternalScript)
        {
            blockers.Add(ProductLedgerRenderableOperatorSurfaceBlocker.ExternalScriptClaimed);
        }

        if (request.ClaimsTelemetryOrSync)
        {
            blockers.Add(ProductLedgerRenderableOperatorSurfaceBlocker.TelemetryOrSyncClaimed);
        }

        if (request.ClaimsProviderCloudNetwork)
        {
            blockers.Add(ProductLedgerRenderableOperatorSurfaceBlocker.ProviderCloudNetworkClaimed);
        }

        if (request.ClaimsDbMigration)
        {
            blockers.Add(ProductLedgerRenderableOperatorSurfaceBlocker.DbMigrationClaimed);
        }

        if (request.ClaimsKmsWormExternalTrust)
        {
            blockers.Add(ProductLedgerRenderableOperatorSurfaceBlocker.KmsWormExternalTrustClaimed);
        }

        if (request.ClaimsBrowserCdpWcuOcrRecipesLive)
        {
            blockers.Add(ProductLedgerRenderableOperatorSurfaceBlocker.BrowserCdpWcuOcrRecipesLiveClaimed);
        }

        if (request.ClaimsReleaseCommercial)
        {
            blockers.Add(ProductLedgerRenderableOperatorSurfaceBlocker.ReleaseCommercialClaimed);
        }

        if (request.ClaimsRawPayloadOrSecret)
        {
            blockers.Add(ProductLedgerRenderableOperatorSurfaceBlocker.RawPayloadOrSecretClaimed);
        }
    }

    private static void AddActionSurfaceBlockers(
        ProductLedgerPublicUiActionResult? actionSurface,
        List<ProductLedgerRenderableOperatorSurfaceBlocker> blockers)
    {
        if (actionSurface is null)
        {
            blockers.Add(ProductLedgerRenderableOperatorSurfaceBlocker.MissingPublicActionSurface);
            return;
        }

        if (!actionSurface.LocalOnly
            || !actionSurface.NonDestructive
            || !actionSurface.Bounded
            || !actionSurface.FailClosed
            || actionSurface.DestructiveActionAvailable
            || actionSurface.UnboundedPhysicalExportAvailable
            || actionSurface.ExternalCloudExportAvailable
            || actionSurface.ProviderCloudNetworkAvailable
            || actionSurface.DbMigrationAvailable
            || actionSurface.KmsWormExternalTrustAvailable
            || actionSurface.BrowserCdpWcuOcrRecipesLiveAvailable
            || actionSurface.ExternalTelemetryOrSyncAvailable
            || actionSurface.BillingLicensingCloudAvailable
            || actionSurface.ReleaseCommercialReady)
        {
            blockers.Add(ProductLedgerRenderableOperatorSurfaceBlocker.UnsafePublicActionSurface);
        }
    }

    private static ProductLedgerRenderableOperatorSurfaceResult Result(
        IReadOnlyList<ProductLedgerRenderableOperatorSurfaceBlocker> blockers,
        ProductLedgerPublicUiActionResult? actionSurface)
    {
        var distinct = ReadOnly(blockers.Distinct().OrderBy(blocker => blocker.ToString(), StringComparer.Ordinal).ToArray());
        var rendered = distinct.Count == 0 && actionSurface is not null;
        var model = rendered ? ReadyModel(actionSurface!) : BlockedModel(distinct);
        return new ProductLedgerRenderableOperatorSurfaceResult(
            Decision: rendered
                ? ProductLedgerRenderableOperatorSurfaceDecision.RenderedSnapshot
                : ProductLedgerRenderableOperatorSurfaceDecision.Rejected,
            Blockers: distinct,
            Model: model,
            HtmlSnapshot: ToHtml(model));
    }

    private static ProductLedgerRenderableOperatorSurfaceModel ReadyModel(
        ProductLedgerPublicUiActionResult actionSurface) =>
        Model(
            rendered: true,
            statusText: ReadyStatus,
            sections: ReadOnly(
                "Runtime gate: local-only internal default-off evidence visible.",
                "Product Ledger writer: bounded local-only writer evidence visible.",
                "Bounded export: local fixture only, hash verified when export evidence is present.",
                "Evidence gates: redaction, retention, authority, failure/replay and rollback required.",
                "Disabled dangerous actions: destructive, external/cloud, DB, KMS/WORM, live automation and release/commercial are blocked.",
                "Safe next step: DOM contract hardening or read-only audit only."
            ),
            actions: ReadOnly(actionSurface.Buttons.Select(button => new ProductLedgerRenderableOperatorSurfaceActionModel(
                ActionId: ToKebab(button.ActionKind.ToString()),
                Label: button.Label,
                RiskLabel: button.RiskLabel,
                RequiredEvidence: string.Join("; ", button.RequiredEvidence),
                SafeAction: button.Enabled,
                Dangerous: !button.Enabled,
                Disabled: !button.Enabled,
                LocalOnly: button.LocalOnly,
                NonDestructive: button.NonDestructive,
                Bounded: button.Bounded,
                DisabledReason: button.DisabledReason)).ToArray()),
            warnings: ReadOnly(
                "Renderable snapshot fixture only; not deployed.",
                "No public route, endpoint or controller.",
                "No telemetry, sync, provider/cloud/network, DB/migration, KMS/WORM/external trust or live automation.",
                "Not release-ready, not commercial-ready and not compliance-grade custody."
            ));

    private static ProductLedgerRenderableOperatorSurfaceModel BlockedModel(
        IReadOnlyList<ProductLedgerRenderableOperatorSurfaceBlocker> blockers) =>
        Model(
            rendered: false,
            statusText: RejectedStatus,
            sections: ReadOnly(blockers.Select(blocker => blocker.ToString()).ToArray()),
            actions: ReadOnly<ProductLedgerRenderableOperatorSurfaceActionModel>(
                new ProductLedgerRenderableOperatorSurfaceActionModel("blocked", "Snapshot blocked", "blocked", "fix blockers", false, true, true, true, false, false, "Fail-closed snapshot did not render active actions.")
            ),
            warnings: ReadOnly("Fail-closed render model does not expose action handlers."));

    private static ProductLedgerRenderableOperatorSurfaceModel Model(
        bool rendered,
        string statusText,
        IReadOnlyList<string> sections,
        IReadOnlyList<ProductLedgerRenderableOperatorSurfaceActionModel> actions,
        IReadOnlyList<string> warnings) =>
        new(
            SnapshotId: "product-ledger.operator-surface.snapshot.v1",
            Title: "Product Ledger Operator Surface Snapshot",
            StatusText: statusText,
            RenderableOperatorSnapshotPercent: rendered ? 100 : 0,
            DomContractPercent: rendered ? 100 : 0,
            UxSafetyPercent: rendered ? 86 : 0,
            ExternalCloudReadinessPercent: 0,
            KmsWormExternalTrustPercent: 0,
            ReleaseCommercialReadinessPercent: 0,
            Notices: ReadOnly(
                "local-only",
                "internal-only",
                "renderable snapshot fixture",
                "not deployed",
                "no public route",
                "no telemetry",
                "no release/commercial",
                "no WORM/KMS/cloud",
                "not external trust",
                "not compliance-grade custody"
            ),
            Sections: sections,
            Actions: actions,
            Warnings: warnings,
            SafeNextStep: rendered ? "DOM_CONTRACT_HARDENING_OR_READ_ONLY_AUDIT_ONLY" : "FIX_RENDER_BLOCKERS",
            LocalOnly: true,
            InternalOnly: true,
            SnapshotOnly: true,
            Deterministic: true,
            FailClosed: true,
            EndpointRouteControllerAvailable: false,
            ExternalScriptAvailable: false,
            TelemetryOrSyncAvailable: false,
            ProviderCloudNetworkAvailable: false,
            DbMigrationAvailable: false,
            KmsWormExternalTrustAvailable: false,
            BrowserCdpWcuOcrRecipesLiveAvailable: false,
            ReleaseCommercialReady: false,
            RawPayloadOrSecretAvailable: false);

    private static string ToHtml(ProductLedgerRenderableOperatorSurfaceModel model)
    {
        var html = new StringBuilder();
        html.AppendLine("<!doctype html>");
        html.AppendLine("<html lang=\"en\" data-testid=\"product-ledger-operator-snapshot\" data-local-only=\"true\" data-internal-only=\"true\" data-snapshot-only=\"true\">");
        html.AppendLine("<head>");
        html.AppendLine("  <meta charset=\"utf-8\">");
        html.AppendLine("  <meta name=\"viewport\" content=\"width=device-width, initial-scale=1\">");
        html.AppendLine($"  <title>{Encode(model.Title)}</title>");
        html.AppendLine("</head>");
        html.AppendLine("<body>");
        html.AppendLine($"  <main data-testid=\"operator-surface\" data-status=\"{Encode(model.StatusText)}\">");
        html.AppendLine($"    <header data-testid=\"header-local-only\"><h1>{Encode(model.Title)}</h1><p>{Encode(model.StatusText)}</p></header>");
        html.AppendLine("    <section data-testid=\"readiness\"><h2>Readiness</h2>");
        html.AppendLine($"      <p data-testid=\"renderable-snapshot-readiness\">Renderable operator snapshot: {model.RenderableOperatorSnapshotPercent}%</p>");
        html.AppendLine($"      <p data-testid=\"dom-contract-readiness\">DOM contract: {model.DomContractPercent}%</p>");
        html.AppendLine($"      <p data-testid=\"external-cloud-readiness\">External/cloud readiness: {model.ExternalCloudReadinessPercent}%</p>");
        html.AppendLine($"      <p data-testid=\"kms-worm-readiness\">WORM/KMS/external trust: {model.KmsWormExternalTrustPercent}%</p>");
        html.AppendLine($"      <p data-testid=\"release-commercial-readiness\">Release/commercial: {model.ReleaseCommercialReadinessPercent}%</p>");
        html.AppendLine("    </section>");

        AppendSection(html, "runtime-gate", "Runtime Gate", model.Sections.ElementAtOrDefault(0));
        AppendSection(html, "writer", "Product Ledger Writer", model.Sections.ElementAtOrDefault(1));
        AppendSection(html, "bounded-export", "Bounded Export", model.Sections.ElementAtOrDefault(2));
        AppendSection(html, "evidence-gates", "Evidence Gates", model.Sections.ElementAtOrDefault(3));
        AppendSection(html, "disabled-dangerous-actions", "Disabled Dangerous Actions", model.Sections.ElementAtOrDefault(4));
        AppendSection(html, "safe-next-step", "Safe Next Step", model.SafeNextStep);

        html.AppendLine("    <section data-testid=\"actions\"><h2>Actions</h2>");
        foreach (var action in model.Actions.OrderBy(action => action.ActionId, StringComparer.Ordinal))
        {
            var disabled = action.Disabled ? " disabled aria-disabled=\"true\"" : " aria-disabled=\"false\"";
            html.AppendLine($"      <button type=\"button\" data-testid=\"action-{Encode(action.ActionId)}\" data-action-id=\"{Encode(action.ActionId)}\" data-risk=\"{Encode(action.RiskLabel)}\" data-local-only=\"{Lower(action.LocalOnly)}\" data-non-destructive=\"{Lower(action.NonDestructive)}\" data-bounded=\"{Lower(action.Bounded)}\" data-executable=\"false\" data-handler-id=\"\" data-callback=\"\"{disabled}>{Encode(action.Label)}</button>");
            html.AppendLine($"      <p data-testid=\"action-{Encode(action.ActionId)}-evidence\">{Encode(action.RequiredEvidence)}</p>");
            if (action.Disabled)
            {
                html.AppendLine($"      <p data-testid=\"action-{Encode(action.ActionId)}-blocked-reason\">{Encode(action.DisabledReason)}</p>");
            }
        }

        html.AppendLine("    </section>");
        html.AppendLine("    <section data-testid=\"notices\"><h2>Notices</h2>");
        foreach (var notice in model.Notices)
        {
            html.AppendLine($"      <p>{Encode(notice)}</p>");
        }

        html.AppendLine("    </section>");
        html.AppendLine("    <section data-testid=\"warnings\"><h2>Warnings</h2>");
        foreach (var warning in model.Warnings)
        {
            html.AppendLine($"      <p>{Encode(warning)}</p>");
        }

        html.AppendLine("    </section>");
        html.AppendLine("  </main>");
        html.AppendLine("</body>");
        html.AppendLine("</html>");
        return html.ToString();
    }

    private static void AppendSection(StringBuilder html, string testId, string title, string? body)
    {
        html.AppendLine($"    <section data-testid=\"{testId}\"><h2>{Encode(title)}</h2><p>{Encode(body ?? string.Empty)}</p></section>");
    }

    private static string Encode(string value) =>
        value
            .Replace("&", "&amp;", StringComparison.Ordinal)
            .Replace("<", "&lt;", StringComparison.Ordinal)
            .Replace(">", "&gt;", StringComparison.Ordinal)
            .Replace("\"", "&quot;", StringComparison.Ordinal)
            .Replace("'", "&#39;", StringComparison.Ordinal);

    private static string Lower(bool value) => value ? "true" : "false";

    private static string ToKebab(string value) =>
        string.Concat(value.Select((ch, index) =>
            index > 0 && char.IsUpper(ch) ? "-" + char.ToLowerInvariant(ch) : char.ToLowerInvariant(ch).ToString()));

    private static IReadOnlyList<T> ReadOnly<T>(params T[] items) =>
        Array.AsReadOnly(items);
}
