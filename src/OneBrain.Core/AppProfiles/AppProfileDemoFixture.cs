namespace OneBrain.Core.AppProfiles;

public static class AppProfileDemoFixture
{
    public static IReadOnlyList<AppProfile> CreateProfiles()
    {
        return
        [
            CreateDemoProductEvidenceProfile(),
            CreateMercadoLibreProfile(),
            CreateSuministrosRocaProfile(),
            CreateSodimacProfile(),
            CreatePilotProfile()
        ];
    }

    public static AppProfile CreateDemoProductEvidenceProfile()
    {
        return Create(
            id: "demo-product-evidence-fixture",
            name: "Demo product evidence fixture",
            kind: AppProfileKinds.Fixture,
            status: AppProfileStatuses.Active,
            domain: "local-demo",
            capabilities: [AppProfileCapabilities.ReadOnly],
            diagnosticAllowed: true,
            notes: ["versioned sample fixture; no live web"]);
    }

    public static AppProfile CreateMercadoLibreProfile()
    {
        return Create(
            id: "mercadolibre-readonly-diagnostic",
            name: "Mercado Libre readonly diagnostic",
            kind: AppProfileKinds.BrowserSite,
            status: AppProfileStatuses.ExternalFragile,
            domain: "mercadolibre.com.ar",
            capabilities: [AppProfileCapabilities.ReadOnly, AppProfileCapabilities.ExternalFragile, AppProfileCapabilities.DiagnosticAllowed],
            diagnosticAllowed: true,
            notes: ["external-fragile; login/cookies/challenge/no-product is diagnostic only"]);
    }

    public static AppProfile CreateSuministrosRocaProfile()
    {
        return Create(
            id: "suministrosroca-readonly",
            name: "Suministros Roca readonly",
            kind: AppProfileKinds.BrowserSite,
            status: AppProfileStatuses.Active,
            domain: "suministrosroca.uy",
            capabilities: [AppProfileCapabilities.ReadOnly],
            diagnosticAllowed: true,
            notes: ["read-only public product evidence"]);
    }

    public static AppProfile CreateSodimacProfile()
    {
        return Create(
            id: "sodimac-readonly",
            name: "Sodimac readonly",
            kind: AppProfileKinds.BrowserSite,
            status: AppProfileStatuses.Active,
            domain: "sodimac.com.uy",
            capabilities: [AppProfileCapabilities.ReadOnly],
            diagnosticAllowed: true,
            notes: ["read-only public product evidence"]);
    }

    public static AppProfile CreatePilotProfile()
    {
        return Create(
            id: "onebrain-pilot-local",
            name: "ONE BRAIN Pilot local",
            kind: AppProfileKinds.WebApp,
            status: AppProfileStatuses.Active,
            domain: "localhost",
            capabilities: [AppProfileCapabilities.ReadOnly],
            diagnosticAllowed: true,
            notes: ["local Pilot shell fixture"]);
    }

    private static AppProfile Create(
        string id,
        string name,
        string kind,
        string status,
        string domain,
        IReadOnlyList<string> capabilities,
        bool diagnosticAllowed,
        IReadOnlyList<string> notes)
    {
        return new AppProfile(
            Id: id,
            Name: name,
            Kind: kind,
            Status: status,
            AppName: name,
            ProcessName: kind == AppProfileKinds.BrowserSite ? "msedge" : null,
            SiteDomain: domain,
            SupportedCapabilities: capabilities,
            RiskPolicy: new AppProfileRiskPolicy(
                ReadOnlyByDefault: true,
                DiagnosticAllowed: diagnosticAllowed,
                RequiresApprovalForSubmit: true,
                BlocksLogin: true,
                BlocksCookies: true,
                BlocksPayment: true,
                BlocksPurchase: true,
                AllowsSafeClick: false),
            SelectorAliases: [],
            LastVerifiedAtUtc: "2026-06-12T12:30:00Z",
            Version: new AppProfileVersion(1, "2026-06-12T12:30:00Z", "initial safe profile fixture", "validated"),
            Notes: notes);
    }
}
