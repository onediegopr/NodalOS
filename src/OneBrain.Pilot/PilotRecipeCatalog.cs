namespace OneBrain.Pilot;

public static class PilotRecipeCatalog
{
    public static IReadOnlyList<PilotRecipeDefinition> AllowlistedRecipes { get; } =
    [
        new(
            Id: "demo_markdown",
            Label: "Comparar productos demo",
            Description: "Genera summary y reporte Markdown desde samples/product-evidence.",
            RecipePath: "tools/recipes/demo-product-evidence-report.json",
            OutputKind: "markdown"),
        new(
            Id: "demo_html",
            Label: "Generar reporte HTML demo",
            Description: "Genera HTML local presentable desde samples/product-evidence.",
            RecipePath: "tools/recipes/demo-product-evidence-html-report.json",
            OutputKind: "html"),
        new(
            Id: "html_report",
            Label: "Generar reporte HTML",
            Description: "Genera HTML local desde ProductEvidenceSummary existente.",
            RecipePath: "tools/recipes/product-evidence-html-report.json",
            OutputKind: "html"),
        new(
            Id: "markdown_report",
            Label: "Generar reporte Markdown",
            Description: "Genera Markdown local desde ProductEvidenceSummary existente.",
            RecipePath: "tools/recipes/product-evidence-markdown-report.json",
            OutputKind: "markdown")
    ];

    public static PilotRecipeDefinition? FindById(string id)
    {
        return AllowlistedRecipes.FirstOrDefault(recipe =>
            string.Equals(recipe.Id, id, StringComparison.OrdinalIgnoreCase));
    }

    public static bool IsAllowlistedPath(string recipePath)
    {
        return AllowlistedRecipes.Any(recipe =>
            string.Equals(recipe.RecipePath.Replace('\\', '/'), recipePath.Replace('\\', '/'), StringComparison.OrdinalIgnoreCase));
    }
}
