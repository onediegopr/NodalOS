namespace OneBrain.Safety.Tests;

internal enum NodalOsStaticGuardCategory
{
    PublicProductExposure,
    ProductionRoutes,
    RuntimeExecutionClaims,
    LatestPointer,
    ReadPrecedence,
    ProductAuthority,
    CommandExecution,
    ShellSubprocess,
    CloudNetworkDb,
    KmsWormCompliance,
    ReleaseCommercial,
    RunClaimCoherence
}

internal sealed record NodalOsStaticGuardDefinition(
    NodalOsStaticGuardCategory Category,
    IReadOnlyList<string> ForbiddenPositiveFragments,
    IReadOnlyList<string> AllowedNegativeFragments);

internal sealed record NodalOsStaticGuardMatch(
    NodalOsStaticGuardCategory Category,
    string Fragment);

internal static class NodalOsStaticGuardCatalog
{
    private static readonly IReadOnlyDictionary<NodalOsStaticGuardCategory, NodalOsStaticGuardDefinition> Definitions =
        new[]
        {
            Define(
                NodalOsStaticGuardCategory.PublicProductExposure,
                [
                    "/public/product-ledger",
                    "PublicProductAllowed: true",
                    "PublicUiActionAvailable: true",
                    "ProductCommandHandlerAvailable: true",
                    "public/product exposure implemented",
                    "Product Ledger public product route is live"
                ],
                [
                    "No public/product",
                    "no public/product",
                    "public/product remains blocked",
                    "public/product exposure remains blocked"
                ]),
            Define(
                NodalOsStaticGuardCategory.ProductionRoutes,
                [
                    "ProductionAllowed: true",
                    "AllowsProductionRoute: true",
                    "Production route enabled",
                    "Production route is live",
                    "Product Ledger Production route is live"
                ],
                [
                    "No Production route",
                    "Production route remains blocked",
                    "Production route coverage remains 404"
                ]),
            Define(
                NodalOsStaticGuardCategory.RuntimeExecutionClaims,
                [
                    "runtime enabled by default",
                    "RuntimeEnabledByDefault: true",
                    "ProductRuntimeEnabled: true"
                ],
                [
                    "runtime default-off",
                    "runtime remains default-off"
                ]),
            Define(
                NodalOsStaticGuardCategory.LatestPointer,
                [
                    "LatestPointerEnabled: true",
                    "latest pointer enabled",
                    "latest pointer overwrite enabled"
                ],
                [
                    "No latest pointer",
                    "latest pointer remains blocked"
                ]),
            Define(
                NodalOsStaticGuardCategory.ReadPrecedence,
                [
                    "ReadPrecedenceAllowed: true",
                    "active read precedence enabled",
                    "read precedence enabled"
                ],
                [
                    "No active read precedence",
                    "no read precedence"
                ]),
            Define(
                NodalOsStaticGuardCategory.ProductAuthority,
                [
                    "ProductAuthorityAllowed: true",
                    "product authority enabled",
                    "product read-model authority enabled"
                ],
                [
                    "No product authority",
                    "not product authority"
                ]),
            Define(
                NodalOsStaticGuardCategory.CommandExecution,
                [
                    "ProductCommandExecuted: true",
                    "ProductCommandHandlerAvailable: true",
                    "command execution enabled",
                    "ProductLedgerInternalCommandHandler"
                ],
                [
                    "No command execution",
                    "no-command-execution",
                    "command execution remains blocked"
                ]),
            Define(
                NodalOsStaticGuardCategory.ShellSubprocess,
                [
                    "Process.Start",
                    "ShellExecute",
                    "System.Diagnostics.Process",
                    "subprocess enabled",
                    "shell enabled"
                ],
                [
                    "No shell/subprocess",
                    "shell/subprocess remains blocked"
                ]),
            Define(
                NodalOsStaticGuardCategory.CloudNetworkDb,
                [
                    "HttpClient",
                    "WebSocket",
                    "DbContext",
                    "MigrationBuilder",
                    "provider live",
                    "cloud backed",
                    "DB migration enabled"
                ],
                [
                    "No provider/cloud/network",
                    "No DB/migration",
                    "provider/cloud/network remains blocked"
                ]),
            Define(
                NodalOsStaticGuardCategory.KmsWormCompliance,
                [
                    "KmsClient",
                    "WormStore",
                    "KMS enabled",
                    "WORM enabled",
                    "external trust enabled"
                ],
                [
                    "No KMS/WORM",
                    "KMS/WORM/external trust remains unimplemented"
                ]),
            Define(
                NodalOsStaticGuardCategory.ReleaseCommercial,
                [
                    "ReleaseCommercialReady: true",
                    "release ready",
                    "commercial ready",
                    "release-ready",
                    "commercial-ready"
                ],
                [
                    "No release/commercial",
                    "release/commercial remains blocked",
                    "release/commercial readiness remains 0%"
                ]),
            Define(
                NodalOsStaticGuardCategory.RunClaimCoherence,
                [
                    "Repo-wide ZeroReadOnly",
                    "No runtime execution anywhere",
                    "/run is read-only"
                ],
                [
                    "Pilot `/run` is separate",
                    "`/run` is a gated allowlisted local execution path"
                ])
        }.ToDictionary(definition => definition.Category);

    public static IReadOnlyCollection<NodalOsStaticGuardDefinition> All => Definitions.Values.ToArray();

    public static NodalOsStaticGuardDefinition Get(NodalOsStaticGuardCategory category) =>
        Definitions.TryGetValue(category, out var definition)
            ? definition
            : throw new ArgumentOutOfRangeException(nameof(category), category, null);

    public static IReadOnlyList<NodalOsStaticGuardMatch> Scan(
        string source,
        params NodalOsStaticGuardCategory[] categories)
    {
        ArgumentNullException.ThrowIfNull(source);
        var selected = categories.Length == 0
            ? Definitions.Values
            : categories.Select(Get);
        var matches = new List<NodalOsStaticGuardMatch>();

        foreach (var definition in selected)
        {
            foreach (var fragment in definition.ForbiddenPositiveFragments)
            {
                if (source.Contains(fragment, StringComparison.OrdinalIgnoreCase))
                {
                    matches.Add(new NodalOsStaticGuardMatch(definition.Category, fragment));
                }
            }
        }

        return matches;
    }

    public static void AssertNoForbiddenMatches(
        string source,
        params NodalOsStaticGuardCategory[] categories)
    {
        var matches = Scan(source, categories);

        if (matches.Count > 0)
        {
            var message = string.Join(
                Environment.NewLine,
                matches.Select(match => $"{match.Category}: {match.Fragment}"));
            Assert.Fail(message);
        }
    }

    private static NodalOsStaticGuardDefinition Define(
        NodalOsStaticGuardCategory category,
        IReadOnlyList<string> forbiddenPositiveFragments,
        IReadOnlyList<string> allowedNegativeFragments) =>
        new(category, forbiddenPositiveFragments, allowedNegativeFragments);
}
