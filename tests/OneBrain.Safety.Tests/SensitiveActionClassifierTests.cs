using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.Core.Approval;

namespace OneBrain.Safety.Tests;

[TestClass]
public sealed class SensitiveActionClassifierTests
{
    [TestMethod]
    public void ClassifiesCanonicalSensitiveStepKinds()
    {
        foreach (var kind in SensitiveActionClassifier.GetCanonicalSensitiveStepKinds())
            Assert.AreEqual(ActionSensitivity.Sensitive, SensitiveActionClassifier.ClassifyStepKind(kind), kind);
    }

    [TestMethod]
    public void AppCloseIsSensitive()
    {
        Assert.AreEqual(ActionSensitivity.Sensitive, SensitiveActionClassifier.ClassifyStepKind("app.close"));
        Assert.IsTrue(SensitiveActionClassifier.IsSensitiveStepKind("app.close"));
    }

    [TestMethod]
    public void UnknownKindFailsClosed()
    {
        Assert.AreEqual(ActionSensitivity.Unknown, SensitiveActionClassifier.ClassifyStepKind("totally.unknown"));
        Assert.IsTrue(SensitiveActionClassifier.IsSensitiveStepKind("totally.unknown"));
    }

    [TestMethod]
    public void MalformedKindFailsClosed()
    {
        Assert.AreEqual(ActionSensitivity.Unknown, SensitiveActionClassifier.ClassifyStepKind(null));
        Assert.AreEqual(ActionSensitivity.Unknown, SensitiveActionClassifier.ClassifyStepKind(""));
        Assert.AreEqual(ActionSensitivity.Unknown, SensitiveActionClassifier.ClassifyStepKind("   "));
        Assert.IsTrue(SensitiveActionClassifier.IsSensitiveStepKind(null));
        Assert.IsTrue(SensitiveActionClassifier.IsSensitiveStepKind("   "));
    }

    [TestMethod]
    public void CasingIsNormalized()
    {
        Assert.AreEqual(ActionSensitivity.Sensitive, SensitiveActionClassifier.ClassifyStepKind("APP.CLOSE"));
        Assert.AreEqual(ActionSensitivity.Sensitive, SensitiveActionClassifier.ClassifyStepKind(" App.Close "));
    }

    [TestMethod]
    public void SemanticApprovalKindDoesNotLeakAsBenign()
    {
        foreach (var kind in new[] { ApprovalActionKinds.Purchase, ApprovalActionKinds.Send, ApprovalActionKinds.Pay })
        {
            Assert.AreEqual(ActionSensitivity.Unknown, SensitiveActionClassifier.ClassifyStepKind(kind), kind);
            Assert.IsTrue(SensitiveActionClassifier.IsSensitiveStepKind(kind), kind);
        }
    }

    [TestMethod]
    public void NoCanonicalSensitiveKindIsBenign()
    {
        foreach (var kind in SensitiveActionClassifier.GetCanonicalSensitiveStepKinds())
            Assert.AreNotEqual(ActionSensitivity.Benign, SensitiveActionClassifier.ClassifyStepKind(kind), kind);
    }

    [TestMethod]
    public void TargetObserveIsBenign()
    {
        Assert.AreEqual(ActionSensitivity.Benign, SensitiveActionClassifier.ClassifyStepKind("target.observe"));
        Assert.IsFalse(SensitiveActionClassifier.IsSensitiveStepKind("target.observe"));
    }

    [TestMethod]
    public void InspectCurrentBehaviorReportsStepKindConvergence()
    {
        var report = SensitiveActionClassifier.InspectCurrentBehavior();

        CollectionAssert.Contains(report.CanonicalSensitiveStepKinds.ToList(), "app.close");
        Assert.AreEqual(0, report.StepKindDifferences.Count);
    }
}
