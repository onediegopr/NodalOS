using System.Text;

namespace OneBrain.Pilot;

public static class MissionControlProductHandoffMarkdownRenderer
{
    public static string Render(MissionControlProductShellSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        if (!snapshot.RealMissionDraft)
            throw new InvalidOperationException("A real mission draft is required for handoff export.");

        var markdown = new StringBuilder();
        Line(markdown, "# NODAL OS — Handoff de misión");
        Line(markdown);
        Line(markdown, "> Export local de solo lectura derivado del estado canónico de Mission Control. No autoriza ni ejecuta acciones.");
        Line(markdown);

        Line(markdown, "## Resumen");
        Bullet(markdown, "Misión", snapshot.Goal);
        Bullet(markdown, "Mission ID", snapshot.MissionId);
        Bullet(markdown, "Run", snapshot.RunId);
        Bullet(markdown, "Estado", snapshot.MissionStatus);
        Bullet(markdown, "Progreso", $"{snapshot.ProgressPercent}%");
        Bullet(markdown, "Paso actual", snapshot.CurrentStep);
        Line(markdown);

        Line(markdown, "## Workspace y acción controlada");
        Bullet(markdown, "Workspace", snapshot.WorkspaceState);
        Bullet(markdown, "Acción", Action(snapshot));
        Bullet(markdown, "Estado de ejecución", snapshot.ActionExecutionState ?? "NotConfigured");
        Bullet(markdown, "Control humano", snapshot.ApprovalState);
        Bullet(markdown, "Resultado verificado", YesNo(snapshot.ActionVerified));
        Bullet(markdown, "Rollback disponible", YesNo(snapshot.ActionRollbackAvailable));
        Bullet(markdown, "Rollback ejecutado", YesNo(snapshot.ActionRolledBack));
        Line(markdown);

        Line(markdown, "## Modelo");
        Bullet(markdown, "Ruta activa", $"{Clean(snapshot.ActiveProvider)} / {Clean(snapshot.ActiveModel)}");
        Bullet(markdown, "Modelo lógico", snapshot.LogicalModel);
        Bullet(markdown, "Conexión verificada", YesNo(snapshot.ModelConnectionVerified));
        Bullet(markdown, "Fallback reciente", string.IsNullOrWhiteSpace(snapshot.RecentFallback) ? "No utilizado" : snapshot.RecentFallback);
        Line(markdown);

        Line(markdown, "## Timeline");
        if (snapshot.Timeline.Count == 0)
        {
            Line(markdown, "- Sin eventos registrados para la misión actual.");
        }
        else
        {
            foreach (var item in snapshot.Timeline.OrderBy(value => value.Sequence))
            {
                Line(markdown, $"### {item.Sequence}. {Clean(item.Title)} — {Clean(item.State)}");
                Line(markdown, Clean(item.Detail));
                if (item.EvidenceRefs.Count > 0)
                    Line(markdown, $"Evidencia: {string.Join(", ", item.EvidenceRefs.Select(Clean))}");
                Line(markdown);
            }
        }

        Line(markdown, "## Evidencia");
        if (snapshot.EvidenceRefs.Count == 0)
        {
            Line(markdown, "- Sin referencias de evidencia disponibles.");
        }
        else
        {
            foreach (var reference in snapshot.EvidenceRefs.Distinct(StringComparer.Ordinal))
                Line(markdown, $"- {Clean(reference)}");
        }
        Line(markdown);

        Line(markdown, "## Próximo paso humano");
        Line(markdown, NextHumanStep(snapshot));
        Line(markdown);

        Line(markdown, "## Límites preservados");
        Line(markdown, $"- Local-only: {YesNo(snapshot.LocalOnly)}");
        Line(markdown, $"- Secretos excluidos: {YesNo(snapshot.SecretsExcluded)}");
        Line(markdown, $"- Autoridad de producto concedida: {YesNo(snapshot.ProductAuthorityGranted)}");
        Line(markdown, "- Este documento resume estado y evidencia; no concede permisos ni sustituye la verificación del runtime.");

        return markdown.ToString();
    }

    private static string Action(MissionControlProductShellSnapshot snapshot)
    {
        if (string.IsNullOrWhiteSpace(snapshot.ActionCandidateKind) && string.IsNullOrWhiteSpace(snapshot.ActionCandidateTarget))
            return "Sin acción candidata";
        return $"{Clean(snapshot.ActionCandidateKind)} · {Clean(snapshot.ActionCandidateTarget)}";
    }

    private static string NextHumanStep(MissionControlProductShellSnapshot snapshot) => snapshot.ActionExecutionState switch
    {
        "CandidateStale" => "Revisar el cambio externo y regenerar la misión antes de aprobar o ejecutar.",
        "ResultChanged" => "Inspeccionar el resultado modificado; el rollback automático permanece deshabilitado para no sobrescribir trabajo posterior.",
        "FailedClosed" => "Revisar la causa redacted y el estado actual antes de volver a intentar.",
        _ when snapshot.ActionApprovalAvailable => "Revisar el alcance exacto y decidir si aprobar la acción controlada.",
        _ when snapshot.ActionRolledBack => "Revisar la misión restaurada antes de crear una nueva acción.",
        _ when snapshot.ActionVerified && snapshot.ActionRollbackAvailable => "Conservar el resultado verificado o usar el rollback guardado si la decisión humana cambia.",
        _ when snapshot.ActionVerified => "Compartir o archivar este handoff y continuar con la siguiente misión.",
        _ => "Revisar la acción candidata y el estado de ejecución antes de continuar."
    };

    private static void Bullet(StringBuilder markdown, string label, string? value) =>
        Line(markdown, $"- **{Clean(label)}:** {Clean(value)}");

    private static void Line(StringBuilder markdown, string value = "") =>
        markdown.Append(value).Append('\n');

    private static string YesNo(bool value) => value ? "Sí" : "No";

    private static string Clean(string? value)
    {
        var normalized = (value ?? string.Empty)
            .Replace('\r', ' ')
            .Replace('\n', ' ')
            .Replace('\0', ' ')
            .Trim();
        if (normalized.Length == 0)
            return "No disponible";
        return normalized.Length <= 800 ? normalized : normalized[..799] + "…";
    }
}
