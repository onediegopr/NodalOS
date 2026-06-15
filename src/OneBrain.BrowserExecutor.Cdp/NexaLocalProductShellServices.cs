using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.BrowserExecutor.Cdp;

public sealed class NexaLocalProductShellService
{
    public NexaLocalProductShell CreateShell(NexaPlanKind plan = NexaPlanKind.Trial, NexaRole role = NexaRole.Owner)
    {
        var navigation = NexaLocalProductShellNavigation.Default();
        var pages = navigation.Routes.Select(route => Page(route, plan, role)).ToArray();
        var render = new NexaLocalProductShellRenderModel(
            navigation,
            pages,
            Redacted: true,
            TenantScoped: true,
            RoleFiltered: true,
            LicenseAware: true,
            FeatureAware: true,
            ContainsSecretsCookiesBodies: false);
        return new NexaLocalProductShell("local-shell-one", render, LocalOnly: true, PublicSaasExposed: false, NetworkListenerEnabled: false);
    }

    public NexaLocalProductShellDecision Decide(NexaLocalProductShellAction action)
    {
        var blocked = action.Kind is NexaLocalProductShellActionKind.EnableSensitiveRealPilot
            or NexaLocalProductShellActionKind.EnableRecorderProductive
            or NexaLocalProductShellActionKind.EnableReplayProductive
            or NexaLocalProductShellActionKind.EnableRealBilling
            or NexaLocalProductShellActionKind.EnableRealEmail
            or NexaLocalProductShellActionKind.EnablePublicSaasActivation
            or NexaLocalProductShellActionKind.ExecuteRealDeployOrUpdate;
        if (blocked)
            return new(false, "action blocked in local shell prototype", Redacted: true);

        if (action.Kind == NexaLocalProductShellActionKind.EnableProductiveVault &&
            (!action.ProductiveVaultEntitlement || !action.AdminOverride || !action.GatePassed))
            return new(false, "productive vault requires entitlement, admin override, and gate", Redacted: true);

        return new(true, "local shell action allowed", Redacted: true);
    }

    private static NexaLocalProductShellPage Page(NexaLocalProductShellRoute route, NexaPlanKind plan, NexaRole role) =>
        new(
            $"page-{route.Kind.ToString().ToLowerInvariant()}",
            route.Kind,
            route.Label,
            Redacted: true,
            TenantScoped: true,
            RoleFiltered: role != NexaRole.Unknown,
            LicenseAware: true,
            FeatureAware: true,
            new Dictionary<string, string>
            {
                ["plan"] = plan.ToString(),
                ["blocked"] = "SensitiveRealPilot,ProductiveVault,RecorderProductive,ReplayProductive,RealBilling,RealEmail,PublicSaas",
                ["scope"] = "tenant-local"
            });
}
