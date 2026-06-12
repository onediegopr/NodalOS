using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.Core.Models;
using OneBrain.Core.Recording;

namespace OneBrain.Recipes.Tests;

[TestClass]
public sealed class RecipeTimelineBuilderTests
{
    [TestMethod]
    public void Timeline_Converts_Recording_Events_To_Human_Steps()
    {
        var session = RecordingDemoFixture.CreateSession();
        var timeline = RecipeTimelineBuilder.Build(session, createdAtUtc: new DateTimeOffset(2026, 06, 12, 10, 1, 0, TimeSpan.Zero));

        Assert.AreEqual(session.RecordingId, timeline.RecordingId);
        Assert.AreEqual(session.Events.Count, timeline.Steps.Count);
        Assert.AreEqual(1, timeline.Steps[0].StepNumber);
        Assert.IsTrue(timeline.Steps.Any(step => step.SuggestedActionLabel.Contains("User opened", StringComparison.OrdinalIgnoreCase)));
    }

    [TestMethod]
    public void Timeline_Marks_Send_As_High_Risk_Approval_Required()
    {
        var timeline = RecordingDemoFixture.CreateTimeline();
        var sendStep = timeline.Steps.First(step =>
            step.ElementSummary.Contains("Enviar", StringComparison.OrdinalIgnoreCase) ||
            step.SuggestedActionLabel.Contains("send", StringComparison.OrdinalIgnoreCase));

        Assert.AreEqual("high", sendStep.RiskLevel);
        Assert.IsTrue(sendStep.RequiresApproval);
        Assert.IsFalse(sendStep.CanGenerateExecutableRecipe);
    }

    [TestMethod]
    public void Annotation_Builder_Supports_Minimum_Annotation_Types()
    {
        var annotations = new[]
        {
            HumanAnnotationBuilder.Create(1, "buscar cliente", "este bloque es buscar cliente"),
            HumanAnnotationBuilder.Create(2, "preparar mensaje", "este bloque es preparar mensaje"),
            HumanAnnotationBuilder.Create(3, "requiere aprobacion", "este paso requiere aprobacion", requiresApproval: true),
            HumanAnnotationBuilder.Create(4, "variable", "este dato debe ser variable"),
            HumanAnnotationBuilder.Create(5, "ignorar", "este paso se puede ignorar", ignored: true),
            HumanAnnotationBuilder.Create(6, "sensible", "este paso es sensible", sensitive: true),
            HumanAnnotationBuilder.Create(null, "free note", "nota libre")
        };

        CollectionAssert.AreEquivalent(new[]
        {
            "search_customer",
            "prepare_message",
            "requires_approval",
            "variable",
            "ignore",
            "sensitive",
            "free_note"
        }, annotations.Select(a => a.AnnotationType).ToArray());
    }

    [TestMethod]
    public void Sensitive_Annotation_Text_Is_Redacted()
    {
        var annotation = HumanAnnotationBuilder.Create(1, "free note", "cliente test@example.com");

        Assert.AreEqual("[REDACTED]", annotation.Text);
    }
}
