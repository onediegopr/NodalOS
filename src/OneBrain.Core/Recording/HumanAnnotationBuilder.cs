namespace OneBrain.Core.Recording;

public static class HumanAnnotationBuilder
{
    public static HumanAnnotation Create(
        int? stepNumber,
        string annotationType,
        string text,
        bool requiresApproval = false,
        bool sensitive = false,
        bool ignored = false,
        DateTimeOffset? createdAtUtc = null)
    {
        return new HumanAnnotation(
            AnnotationId: Guid.NewGuid().ToString("N"),
            StepNumber: stepNumber,
            AnnotationType: NormalizeAnnotationType(annotationType),
            Text: SensitiveTextSanitizer.Sanitize(text),
            RequiresApproval: requiresApproval,
            Sensitive: sensitive,
            Ignored: ignored,
            CreatedAtUtc: (createdAtUtc ?? DateTimeOffset.UtcNow).UtcDateTime.ToString("o"));
    }

    private static string NormalizeAnnotationType(string annotationType)
    {
        var normalized = string.IsNullOrWhiteSpace(annotationType)
            ? "free_note"
            : annotationType.Trim().ToLowerInvariant().Replace(' ', '_');

        return normalized switch
        {
            "buscar_cliente" => "search_customer",
            "preparar_mensaje" => "prepare_message",
            "requiere_aprobacion" => "requires_approval",
            "variable" => "variable",
            "ignorar" => "ignore",
            "sensible" => "sensitive",
            _ => normalized
        };
    }
}
