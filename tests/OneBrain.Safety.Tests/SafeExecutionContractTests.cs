using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.Core.Approval;
using OneBrain.Core.Contracts;
using OneBrain.Core.Execution;
using OneBrain.Core.Identity;
using OneBrain.Core.Models;
using OneBrain.Core.Selectors;

namespace OneBrain.Safety.Tests;

[TestClass]
public sealed class SafeExecutionContractTests
{
    [TestMethod]
    public void SafeExecutionFsm_HappyPath_Writes_Expected_Transitions()
    {
        var identity = CreateIdentity();
        var contract = CreateContract(identity);
        var fsm = new SafeExecutionFsm(
            new ContractValidator(),
            new ApprovalBindingValidator(),
            new FakeOwnershipMonitor(),
            new FakePatternExecutor(new PatternExecutionResult(
                Success: true,
                FailureKind: null,
                Reasons: ["invoke ok"],
                ObservedIdentity: identity,
                WindowFound: true,
                TargetVisible: true,
                TargetName: identity.Name,
                ObservedActions: 1,
                Signals: ["postAction.windowFound=true", "postAction.targetVisible=true"])),
            new FakeVerifier(new StepVerificationResult(
                Success: true,
                FailureKind: null,
                MatchVerdict: "Same",
                Reasons: ["verified"],
                ObservedIdentity: identity)));

        var result = fsm.Execute(new SafeExecutionRequest(
            Contract: contract,
            Candidates: [identity],
            DispatchRequest: CreateDispatch(contract, identity)));

        Assert.IsTrue(result.Success);
        Assert.AreEqual(StepState.Succeeded, result.FinalState);
        CollectionAssert.AreEqual(
            new[] { StepState.Validated, StepState.Bound, StepState.Executing, StepState.Verifying, StepState.Succeeded },
            result.Ledger.Entries.Select(entry => entry.ToState).ToArray());
        Assert.IsTrue(result.Ledger.Entries.All(entry => entry.Sequence > 0));
        Assert.AreEqual(StepTransition.Verified, result.Ledger.Entries[^1].Event);
    }

    [TestMethod]
    public void ContractValidator_Denies_Missing_Fields_By_Default()
    {
        var identity = CreateIdentity();
        var selector = SelectorEngine.GenerateSelector(identity);
        var validator = new ContractValidator();

        var missingAction = validator.Validate(new RecipeSafetyContract(1, "c-1", "", identity, selector, new ExecutionWindowConstraints(true, true), false, 1, ActionCeiling.FullActionWithPreflight, Provenance.Fixture, TrustLevel.ProfileVerified, CreateBinding(identity, selector)));
        var missingIdentity = validator.Validate(new RecipeSafetyContract(1, "c-1", ApprovalActionKinds.BenignHarnessClick, null, selector, new ExecutionWindowConstraints(true, true), false, 1, ActionCeiling.FullActionWithPreflight, Provenance.Fixture, TrustLevel.ProfileVerified, CreateBinding(identity, selector)));
        var missingSelector = validator.Validate(new RecipeSafetyContract(1, "c-1", ApprovalActionKinds.BenignHarnessClick, identity, null, new ExecutionWindowConstraints(true, true), false, 1, ActionCeiling.FullActionWithPreflight, Provenance.Fixture, TrustLevel.ProfileVerified, CreateBinding(identity, selector)));
        var invalidMaxActions = validator.Validate(new RecipeSafetyContract(1, "c-1", ApprovalActionKinds.BenignHarnessClick, identity, selector, new ExecutionWindowConstraints(true, true), false, 2, ActionCeiling.FullActionWithPreflight, Provenance.Fixture, TrustLevel.ProfileVerified, CreateBinding(identity, selector)));
        var missingApproval = validator.Validate(new RecipeSafetyContract(1, "c-1", ApprovalActionKinds.BenignHarnessClick, identity, selector, new ExecutionWindowConstraints(true, true), false, 1, ActionCeiling.FullActionWithPreflight, Provenance.Fixture, TrustLevel.ProfileVerified, null));

        Assert.IsFalse(missingAction.IsValid);
        Assert.IsFalse(missingIdentity.IsValid);
        Assert.IsFalse(missingSelector.IsValid);
        Assert.IsFalse(invalidMaxActions.IsValid);
        Assert.IsFalse(missingApproval.IsValid);
        Assert.AreEqual(FailureKind.PolicyDenied, missingApproval.FailureKind);
    }

    [TestMethod]
    public void ApprovalBindingValidator_Blocks_Text_Match_When_Identity_Changes()
    {
        var expected = CreateIdentity(name: "Enviar", runtimeId: "btn-1", automationId: "send-button");
        var candidate = CreateIdentity(name: "Enviar", runtimeId: "btn-9", automationId: "other-button");
        var selector = new SelectorDefinition { SchemaVersion = 1, Provenance = Provenance.Fixture, Name = "Enviar" };
        var binding = CreateBinding(expected, selector);

        var result = new ApprovalBindingValidator().Validate(binding, expected, [candidate], reversible: false);

        Assert.IsFalse(result.Success);
        Assert.AreEqual(FailureKind.Stale, result.FailureKind);
        Assert.AreEqual("ApprovalInvalidated", result.BlockReason);
    }

    [TestMethod]
    public void ApprovalBindingValidator_Returns_Ambiguous_For_Duplicate_Candidates()
    {
        var expected = CreateIdentity(name: "Enviar", runtimeId: "", automationId: "");
        var selector = new SelectorDefinition { SchemaVersion = 1, Provenance = Provenance.Fixture, Name = "Enviar" };
        var binding = CreateBinding(expected, selector);
        var first = CreateIdentity(name: "Enviar", runtimeId: "", automationId: "", ancestorPath: "Window:A");
        var second = CreateIdentity(name: "Enviar", runtimeId: "", automationId: "", ancestorPath: "Window:B");

        var result = new ApprovalBindingValidator().Validate(binding, expected, [first, second], reversible: false);

        Assert.IsFalse(result.Success);
        Assert.AreEqual(FailureKind.Ambiguous, result.FailureKind);
    }

    [TestMethod]
    public void ApprovalBindingValidator_Allows_LikelySame_When_Reversible()
    {
        var expected = CreateIdentity(runtimeId: "old-runtime", automationId: "search-box");
        var candidate = CreateIdentity(runtimeId: "new-runtime", automationId: "search-box");
        var selector = new SelectorDefinition { SchemaVersion = 1, Provenance = Provenance.Fixture, AutomationId = "search-box", Role = "Button" };
        var binding = CreateBinding(expected, selector);

        var result = new ApprovalBindingValidator().Validate(binding, expected, [candidate], reversible: true);

        Assert.IsTrue(result.Success);
        Assert.AreEqual("LikelySame", result.MatchVerdict);
    }

    [TestMethod]
    public void SafeExecutionFsm_Aborts_When_Human_Input_Appears_Before_Dispatch()
    {
        var identity = CreateIdentity();
        var contract = CreateContract(identity);
        var fsm = new SafeExecutionFsm(
            new ContractValidator(),
            new ApprovalBindingValidator(),
            new FakeOwnershipMonitor(humanInputChecks: [true]),
            new FakePatternExecutor(new PatternExecutionResult(true, null, ["invoke ok"], identity, true, true, identity.Name, 1)),
            new FakeVerifier(new StepVerificationResult(true, null, "Same", ["verified"], identity)));

        var result = fsm.Execute(new SafeExecutionRequest(contract, [identity], CreateDispatch(contract, identity)));

        Assert.IsFalse(result.Success);
        Assert.AreEqual(StepState.Aborted, result.FinalState);
        Assert.AreEqual(FailureKind.HumanInterrupted, result.FailureKind);
        CollectionAssert.AreEqual(
            new[] { StepState.Validated, StepState.Bound, StepState.Aborted },
            result.Ledger.Entries.Select(entry => entry.ToState).ToArray());
    }

    [TestMethod]
    public void SafeExecutionFsm_Paused_Never_Resumes_To_Executing()
    {
        var identity = CreateIdentity();
        var contract = CreateContract(identity);
        var fsm = new SafeExecutionFsm(
            new ContractValidator(),
            new ApprovalBindingValidator(),
            new FakeOwnershipMonitor(humanInputChecks: [false, true]),
            new FakePatternExecutor(new PatternExecutionResult(true, null, ["invoke ok"], identity, true, true, identity.Name, 1)),
            new FakeVerifier(new StepVerificationResult(true, null, "Same", ["verified"], identity)));

        var result = fsm.Execute(new SafeExecutionRequest(contract, [identity], CreateDispatch(contract, identity)));

        Assert.IsFalse(result.Success);
        Assert.AreEqual(StepState.Aborted, result.FinalState);
        CollectionAssert.AreEqual(
            new[] { StepState.Validated, StepState.Bound, StepState.Executing, StepState.Paused, StepState.Aborted },
            result.Ledger.Entries.Select(entry => entry.ToState).ToArray());
        Assert.IsFalse(result.Ledger.Entries.SkipWhile(entry => entry.ToState != StepState.Paused).Any(entry => entry.ToState == StepState.Executing));
    }

    [TestMethod]
    public void SensitiveActionClassifier_Reports_StepKind_Convergence()
    {
        var report = SensitiveActionClassifier.InspectCurrentBehavior();

        CollectionAssert.Contains(report.RecipeRunnerKinds.ToList(), "app.close");
        CollectionAssert.Contains(report.ProgramKinds.ToList(), "app.close");
        CollectionAssert.DoesNotContain(report.ApprovalPolicyKinds.ToList(), "app.close");
        Assert.AreEqual(0, report.StepKindDifferences.Count);
    }

    private static RecipeSafetyContract CreateContract(ElementIdentity identity, bool reversible = false)
    {
        var selector = SelectorEngine.GenerateSelector(identity);
        return new RecipeSafetyContract(
            SchemaVersion: 1,
            ContractId: "contract-1",
            ActionKind: ApprovalActionKinds.BenignHarnessClick,
            ExpectedIdentity: identity,
            Selector: selector,
            WindowConstraints: new ExecutionWindowConstraints(true, true),
            Reversible: reversible,
            MaxActions: 1,
            ActionCeiling: ActionCeiling.FullActionWithPreflight,
            Provenance: Provenance.Fixture,
            TrustLevel: TrustLevel.ProfileVerified,
            ApprovalRef: CreateBinding(identity, selector));
    }

    private static ApprovalBinding CreateBinding(ElementIdentity identity, SelectorDefinition selector)
    {
        return new ApprovalBinding(
            ApprovalDecisionId: "decision-1",
            ApprovedIdentityDigest: ElementFingerprintBuilder.Build(identity),
            Selector: selector,
            ActionKind: ApprovalActionKinds.BenignHarnessClick,
            Mode: "supervised",
            PolicyVersion: "test",
            EvidenceHash: ElementFingerprintBuilder.Build(identity));
    }

    private static PatternExecutionRequest CreateDispatch(RecipeSafetyContract contract, ElementIdentity identity)
    {
        return new PatternExecutionRequest(
            ActionKind: contract.ActionKind,
            TargetRef: "role:Button|name:Boton benigno",
            ExpectedTargetName: identity.Name,
            ProcessName: "OneBrain.Pilot",
            WindowTitleContains: "ONE BRAIN Pilot",
            Selector: contract.Selector!,
            ExpectedIdentity: identity);
    }

    private static ElementIdentity CreateIdentity(
        string name = "Boton benigno",
        string runtimeId = "fixture-button",
        string automationId = "onebrain-benign-harness-target",
        string ancestorPath = "Window:ONE BRAIN Pilot > Group:ExecutorHarness")
    {
        return new ElementIdentity(runtimeId, "Button", name, automationId)
        {
            Role = "Button",
            ControlType = "Button",
            AncestorPath = ancestorPath,
            ClassName = "Button",
            Provenance = Provenance.Fixture
        };
    }

    private sealed class FakeOwnershipMonitor(IReadOnlyList<bool>? humanInputChecks = null) : IDesktopOwnershipMonitor
    {
        private readonly Queue<bool> _humanInputChecks = new(humanInputChecks ?? []);
        private readonly OwnershipSnapshot _snapshot = new(0, 100, "ONE BRAIN Pilot", DateTimeOffset.UtcNow);

        public OwnershipSnapshot Capture() => _snapshot;
        public bool HumanInputSince(OwnershipSnapshot baseline) => _humanInputChecks.Count > 0 && _humanInputChecks.Dequeue();
        public bool ForegroundChanged(OwnershipSnapshot baseline) => false;
    }

    private sealed class FakePatternExecutor(PatternExecutionResult result) : IUiaPatternExecutor
    {
        public PatternExecutionResult Invoke(PatternExecutionRequest request) => result;
    }

    private sealed class FakeVerifier(StepVerificationResult result) : IStepVerifier
    {
        public StepVerificationResult Verify(RecipeSafetyContract contract, PatternExecutionResult dispatchResult) => result;
    }
}
