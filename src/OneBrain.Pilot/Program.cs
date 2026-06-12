using OneBrain.Pilot;
using OneBrain.Core.AI;
using OneBrain.Core.AppProfiles;
using OneBrain.Core.Approval;
using OneBrain.Core.History;
using OneBrain.Core.Memory;
using OneBrain.Core.Recording;
using OneBrain.Core.Recipes.Editing;

var root = GetArg(args, "--root") ?? Directory.GetCurrentDirectory();
var dotnet = GetArg(args, "--dotnet")
    ?? Environment.GetEnvironmentVariable("ONEBRAIN_DOTNET")
    ?? PilotRecipeExecutor.DefaultDotnetPath;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls(GetArg(args, "--urls") ?? "http://127.0.0.1:5084");

var app = builder.Build();
var router = new PilotIntentRouter();
var planner = new PilotPlanBuilder();
var executor = new PilotRecipeExecutor(root, dotnet);

app.MapGet("/", () => Results.Content(PilotHomePageRenderer.Render(), "text/html"));

app.MapPost("/plan", async (HttpContext context) =>
{
    var task = await ReadTaskAsync(context);
    var plan = planner.Build(router.Route(task));
    return Results.Content(PilotHomePageRenderer.Render(plan), "text/html");
});

app.MapPost("/run", async (HttpContext context) =>
{
    var task = await ReadTaskAsync(context);
    var plan = planner.Build(router.Route(task));
    var result = await executor.ExecuteAsync(plan, context.RequestAborted);
    return Results.Content(PilotHomePageRenderer.Render(plan, result), "text/html");
});

app.MapGet("/api/intent", (string? task) =>
{
    var plan = planner.Build(router.Route(task));
    return Results.Json(plan);
});

app.MapGet("/api/safety", () => Results.Json(PilotSafetySummary.ZeroReadOnly));

app.MapGet("/recording/demo", () =>
{
    var timeline = RecordingDemoFixture.CreateTimeline();
    return Results.Content(PilotHomePageRenderer.RenderRecordingDemo(timeline), "text/html");
});

app.MapPost("/recording/demo/annotate", async (HttpContext context) =>
{
    var form = await context.Request.ReadFormAsync();
    var stepNumber = int.TryParse(form["stepNumber"].FirstOrDefault(), out var parsedStep) ? parsedStep : (int?)null;
    var annotationType = form["annotationType"].FirstOrDefault() ?? "free_note";
    var text = form["text"].FirstOrDefault() ?? "";

    var baseTimeline = RecordingDemoFixture.CreateTimeline();
    var annotations = baseTimeline.Annotations
        .Concat([HumanAnnotationBuilder.Create(stepNumber, annotationType, text)])
        .ToList();
    var timeline = RecipeTimelineBuilder.Build(RecordingDemoFixture.CreateSession(), annotations);

    return Results.Content(PilotHomePageRenderer.RenderRecordingDemo(timeline), "text/html");
});

app.MapGet("/approvals/demo", () =>
{
    var request = BusinessFlowDemoFixture.CreateSendMessageApproval();
    var confidence = BusinessFlowDemoFixture.CreateConfidenceProfile();
    return Results.Content(PilotHomePageRenderer.RenderApprovalDemo(request, confidence), "text/html");
});

app.MapPost("/approvals/demo/decide", async (HttpContext context) =>
{
    var form = await context.Request.ReadFormAsync();
    var decisionKind = form["decision"].FirstOrDefault() ?? ApprovalDecisionKinds.Rejected;
    var reason = form["reason"].FirstOrDefault() ?? "";

    var request = BusinessFlowDemoFixture.CreateSendMessageApproval();
    var confidence = BusinessFlowDemoFixture.CreateConfidenceProfile();
    var decision = ApprovalPolicy.Decide(request, decisionKind, reason, decidedBy: "pilot-demo");

    return Results.Content(PilotHomePageRenderer.RenderApprovalDemo(request, confidence, decision), "text/html");
});

app.MapGet("/ai/config", () =>
{
    var profiles = AIModelConfiguration.LoadOfficialProfiles();
    return Results.Content(PilotHomePageRenderer.RenderAIConfigConsole(profiles), "text/html");
});

app.MapPost("/ai/config/test", () =>
{
    var profiles = AIModelConfiguration.LoadOfficialProfiles();
    var policy = new AIModelRoutingPolicy(profiles, []);
    var result = new AIModelRouter().Route(new AIModelRoutingRequest(
        TaskText: "mostrame la demo",
        Capability: AIModelCapabilities.Intent,
        RiskLevel: AIRiskLevels.Low,
        RequiresVision: false,
        IsAmbiguous: false,
        IsIrreversible: false,
        EstimatedCostUsd: 0.01m,
        EstimatedCalls: 1,
        Environment: "local",
        Profile: "pilot"),
        policy);

    return Results.Content(PilotHomePageRenderer.RenderAIConfigConsole(profiles, result), "text/html");
});

app.MapGet("/runs", () =>
{
    var runs = RunHistoryStore.ReadAll(root);
    if (runs.Count == 0)
        runs = HistoryDemoFixture.CreateRunHistory();

    return Results.Content(PilotHomePageRenderer.RenderRunHistory(runs), "text/html");
});

app.MapGet("/runs/{id}", (string id) =>
{
    var run = RunHistoryStore.ReadById(root, id);
    var runs = run == null ? HistoryDemoFixture.CreateRunHistory().Where(candidate => candidate.RunId == id).ToList() : [run];
    return Results.Content(PilotHomePageRenderer.RenderRunHistory(runs), "text/html");
});

app.MapGet("/ai/audit", () =>
{
    var audits = AIAuditLogStore.ReadAll(root);
    if (audits.Count == 0)
        audits = HistoryDemoFixture.CreateAIAudit();

    return Results.Content(PilotHomePageRenderer.RenderAIAuditLog(audits), "text/html");
});

app.MapGet("/recipes", () =>
{
    var models = PilotRecipeCatalog.AllowlistedRecipes
        .Select(recipe => RecipeEditorService.Load(root, recipe.Id, recipe.RecipePath, confidenceStatus: "supervised"))
        .ToList();
    return Results.Content(PilotHomePageRenderer.RenderRecipeList(models), "text/html");
});

app.MapGet("/recipes/{id}", (string id) =>
{
    var recipe = PilotRecipeCatalog.FindById(id);
    if (recipe == null)
        return Results.Content(PilotHomePageRenderer.RenderRecipeList([], $"Recipe not found or not allowlisted: {id}"), "text/html");

    var model = RecipeEditorService.Load(root, recipe.Id, recipe.RecipePath, confidenceStatus: "supervised");
    var json = File.ReadAllText(Path.Combine(root, recipe.RecipePath.Replace('/', Path.DirectorySeparatorChar)));
    var validation = RecipeLinter.ValidateJson(json, recipe.RecipePath);
    var variables = RecipeVariableManager.ExtractVariablesFromJson(json);
    return Results.Content(PilotHomePageRenderer.RenderRecipeDetail(model, validation, variables), "text/html");
});

app.MapPost("/recipes/{id}/edit", async (string id, HttpContext context) =>
{
    var recipe = PilotRecipeCatalog.FindById(id);
    if (recipe == null)
        return Results.Content(PilotHomePageRenderer.RenderRecipeList([], $"Recipe not found or not allowlisted: {id}"), "text/html");

    var form = await context.Request.ReadFormAsync();
    var model = RecipeEditorService.Load(root, recipe.Id, recipe.RecipePath, confidenceStatus: "supervised");
    var request = new RecipeEditRequest(
        RecipeId: recipe.Id,
        RecipePath: recipe.RecipePath,
        Title: form["title"].FirstOrDefault(),
        Description: form["description"].FirstOrDefault(),
        Tags: SplitCsv(form["tags"].FirstOrDefault()),
        Notes: SplitCsv(form["notes"].FirstOrDefault()),
        HumanReadableLabels: new Dictionary<string, string>(),
        UnsafeFieldAttempts: BuildUnsafeAttempts(form));

    var policy = RecipeEditPolicy.Evaluate(request);
    var draft = RecipeEditorService.CreateDraft(model, request, policy);
    var write = policy.Allowed ? RecipeDraftStore.Write(root, draft) : new RecipeDraftArtifactWriteResult { Success = false, Error = string.Join("; ", policy.Errors) };
    var json = File.ReadAllText(Path.Combine(root, recipe.RecipePath.Replace('/', Path.DirectorySeparatorChar)));
    var validation = RecipeLinter.ValidateJson(json, recipe.RecipePath);
    var variables = RecipeVariableManager.ExtractVariablesFromJson(json);
    return Results.Content(PilotHomePageRenderer.RenderRecipeDetail(model, validation, variables, draft, write), "text/html");
});

app.MapGet("/variables", () =>
{
    var variables = PilotRecipeCatalog.AllowlistedRecipes
        .SelectMany(recipe =>
        {
            var json = File.ReadAllText(Path.Combine(root, recipe.RecipePath.Replace('/', Path.DirectorySeparatorChar)));
            return RecipeVariableManager.ExtractVariablesFromJson(json);
        })
        .GroupBy(variable => variable.Name, StringComparer.OrdinalIgnoreCase)
        .Select(group => group.First())
        .OrderBy(variable => variable.Name)
        .ToList();

    return Results.Content(PilotHomePageRenderer.RenderVariables(variables), "text/html");
});

app.MapGet("/recipes/{id}/variables", (string id) =>
{
    var recipe = PilotRecipeCatalog.FindById(id);
    if (recipe == null)
        return Results.Content(PilotHomePageRenderer.RenderVariables([], $"Recipe not found or not allowlisted: {id}"), "text/html");

    var json = File.ReadAllText(Path.Combine(root, recipe.RecipePath.Replace('/', Path.DirectorySeparatorChar)));
    var variables = RecipeVariableManager.ExtractVariablesFromJson(json);
    return Results.Content(PilotHomePageRenderer.RenderVariables(variables, $"Variables for {recipe.Label}"), "text/html");
});

app.MapGet("/memory", (string? q, string? tag, string? appOrSite, string? domain, string? status) =>
{
    var entries = ProcessMemoryStore.ReadAll(root);
    if (entries.Count == 0)
        entries = ProcessMemoryDemoFixture.CreateEntries();

    var tags = string.IsNullOrWhiteSpace(tag) ? [] : SplitCsv(tag);
    var query = new WorkflowRetrievalQuery(
        Text: q,
        Tags: tags,
        AppOrSite: appOrSite,
        Domain: domain,
        Status: status);
    var retrieval = WorkflowRetrievalService.Search(entries, query);
    return Results.Content(PilotHomePageRenderer.RenderProcessMemory(entries, retrieval), "text/html");
});

app.MapGet("/memory/search", (string? q, string? tag, string? appOrSite, string? domain, string? status) =>
{
    var entries = ProcessMemoryStore.ReadAll(root);
    if (entries.Count == 0)
        entries = ProcessMemoryDemoFixture.CreateEntries();

    var query = new WorkflowRetrievalQuery(
        Text: q,
        Tags: string.IsNullOrWhiteSpace(tag) ? [] : SplitCsv(tag),
        AppOrSite: appOrSite,
        Domain: domain,
        Status: status);
    var retrieval = WorkflowRetrievalService.Search(entries, query);
    return Results.Content(PilotHomePageRenderer.RenderProcessMemory(entries, retrieval), "text/html");
});

app.MapGet("/memory/{id}", (string id) =>
{
    var entry = ProcessMemoryStore.ReadById(root, id) ??
                ProcessMemoryDemoFixture.CreateEntries().FirstOrDefault(candidate => string.Equals(candidate.Id, id, StringComparison.OrdinalIgnoreCase));
    return Results.Content(PilotHomePageRenderer.RenderProcessMemoryDetail(entry), "text/html");
});

app.MapGet("/app-profiles", () =>
{
    var profiles = AppProfileStore.ReadAll(root);
    if (profiles.Count == 0)
        profiles = AppProfileDemoFixture.CreateProfiles();

    return Results.Content(PilotHomePageRenderer.RenderAppProfiles(profiles), "text/html");
});

app.MapGet("/app-profiles/{id}", (string id) =>
{
    var profile = AppProfileStore.ReadById(root, id) ??
                  AppProfileDemoFixture.CreateProfiles().FirstOrDefault(candidate => string.Equals(candidate.Id, id, StringComparison.OrdinalIgnoreCase));
    return Results.Content(PilotHomePageRenderer.RenderAppProfileDetail(profile), "text/html");
});

app.Run();

static async Task<string?> ReadTaskAsync(HttpContext context)
{
    if (!context.Request.HasFormContentType)
        return context.Request.Query["task"].FirstOrDefault();

    var form = await context.Request.ReadFormAsync();
    return form["task"].FirstOrDefault();
}

static string? GetArg(string[] args, string name)
{
    for (var i = 0; i < args.Length - 1; i++)
    {
        if (string.Equals(args[i], name, StringComparison.OrdinalIgnoreCase))
            return args[i + 1];
    }

    return null;
}

static IReadOnlyList<string> SplitCsv(string? value)
{
    return string.IsNullOrWhiteSpace(value)
        ? []
        : value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
}

static IReadOnlyDictionary<string, string> BuildUnsafeAttempts(IFormCollection form)
{
    var attempts = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    foreach (var key in form.Keys)
    {
        if (key.StartsWith("unsafe.", StringComparison.OrdinalIgnoreCase))
            attempts[key["unsafe.".Length..]] = form[key].FirstOrDefault() ?? "";
    }

    return attempts;
}
