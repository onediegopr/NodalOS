using System.Text.RegularExpressions;
using OneBrain.Core.Contracts;
using OneBrain.Core.Perception;

namespace OneBrain.Core.Runtime;

public enum TrustedControlSource
{
    SystemPolicy,
    UserInstruction,
    OperatorDecision,
    TrustedApplicationState,
    UiaObservation,
    CdpObservation,
    OcrObservation,
    VisualObservation,
    ExternalDocument,
    AgentMemory
}

public enum TrustedControlOperation
{
    DefineMissionGoal,
    AmendMissionGoal,
    AddPlanStep,
    ExpandCapabilityScope,
    BindVariable,
    SatisfyPrecondition,
    AttachEvidence,
    SelectPrecompiledBranch,
    RequestSafeBlock,
    AddExternalRecipient,
    AccessSecret,
    PublishExternal
}

public enum TrustedControlDecisionKind
{
    Allowed,
    Blocked,
    RequiresOperatorDecision
}

public enum ControlVariableSensitivity
{
    Public,
    Private,
    SecretReference
}

public sealed record ControlVariableDefinition(
    string Name,
    ControlVariableSensitivity Sensitivity,
    IReadOnlySet<TrustedControlSource> AllowedSources);

public sealed record TrustedControlFlowContext(
    string MissionId,
    IReadOnlyDictionary<string, ControlVariableDefinition> DeclaredVariables,
    IReadOnlySet<string> AllowedBranchIds,
    IReadOnlySet<string> AuthorizedCapabilities,
    bool ExternalCommunicationAllowed = false,
    bool CloudAllowed = false);

public sealed record TrustedControlFlowRequest(
    TrustedControlSource Source,
    TrustedControlOperation Operation,
    string TargetRef,
    string ValueRedacted,
    IReadOnlyList<string> EvidenceRefs,
    string? CapabilityId = null);

public sealed record TrustedControlFlowDecision(
    TrustedControlDecisionKind Decision,
    string Code,
    string Reason,
    bool PossiblePromptInjection,
    bool CanModifyMissionGoal,
    bool CanExpandScope,
    bool CanPublishExternally,
    IReadOnlyList<string> EvidenceRefs);

public static class TrustedControlFlowBoundary
{
    private static readonly IReadOnlySet<TrustedControlSource> ControlAuthorities =
        new HashSet<TrustedControlSource>
        {
            TrustedControlSource.SystemPolicy,
            TrustedControlSource.UserInstruction,
            TrustedControlSource.OperatorDecision
        };

    private static readonly IReadOnlySet<TrustedControlSource> ObservedDataSources =
        new HashSet<TrustedControlSource>
        {
            TrustedControlSource.TrustedApplicationState,
            TrustedControlSource.UiaObservation,
            TrustedControlSource.CdpObservation,
            TrustedControlSource.OcrObservation,
            TrustedControlSource.VisualObservation,
            TrustedControlSource.ExternalDocument,
            TrustedControlSource.AgentMemory
        };

    private static readonly Regex PromptInjectionPattern = new(
        @"(?is)\b(ignore|disregard|override|bypass)\b.{0,100}\b(instruction|prompt|policy|guardrail)\b|\b(reveal|upload|send|exfiltrate|publish)\b.{0,100}\b(secret|token|password|credential|workspace|file)\b|\bdo\s+not\s+tell\b.{0,100}\b(user|operator)\b",
        RegexOptions.Compiled | RegexOptions.CultureInvariant,
        TimeSpan.FromMilliseconds(100));

    public static TrustedControlFlowDecision Evaluate(
        TrustedControlFlowContext context,
        TrustedControlFlowRequest request)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context.DeclaredVariables);
        ArgumentNullException.ThrowIfNull(context.AllowedBranchIds);
        ArgumentNullException.ThrowIfNull(context.AuthorizedCapabilities);
        ArgumentNullException.ThrowIfNull(request.EvidenceRefs);

        var missionId = SafeRuntimeText.Sanitize(context.MissionId, 120);
        if (missionId.Length == 0)
            throw new ArgumentException("Mission id is required.", nameof(context));

        var targetRef = SafeRuntimeText.Sanitize(request.TargetRef, 160);
        var value = SafeRuntimeText.Sanitize(request.ValueRedacted, 500);
        var capabilityId = SafeRuntimeText.Sanitize(request.CapabilityId, 160);
        var evidenceRefs = request.EvidenceRefs
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .Select(item => SafeRuntimeText.Sanitize(item, 160))
            .Where(item => item.Length > 0)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(item => item, StringComparer.Ordinal)
            .Take(64)
            .ToArray();
        var possibleInjection = ObservedDataSources.Contains(request.Source) &&
                                PromptInjectionPattern.IsMatch(value);

        return request.Operation switch
        {
            TrustedControlOperation.DefineMissionGoal or
            TrustedControlOperation.AmendMissionGoal or
            TrustedControlOperation.AddPlanStep => EvaluateControlMutation(
                request.Source,
                request.Operation,
                possibleInjection,
                evidenceRefs),

            TrustedControlOperation.ExpandCapabilityScope => EvaluateCapabilityExpansion(
                context,
                request.Source,
                capabilityId,
                possibleInjection,
                evidenceRefs),

            TrustedControlOperation.BindVariable => EvaluateVariableBinding(
                context,
                request.Source,
                targetRef,
                value,
                possibleInjection,
                evidenceRefs),

            TrustedControlOperation.SatisfyPrecondition => AllowObservedDataOperation(
                request.Source,
                "TRUSTED_CONTROL_PRECONDITION_OBSERVATION_ALLOWED",
                "Observed state may satisfy a predeclared precondition but cannot change the mission goal.",
                possibleInjection,
                evidenceRefs),

            TrustedControlOperation.AttachEvidence => AllowObservedDataOperation(
                request.Source,
                "TRUSTED_CONTROL_EVIDENCE_ATTACHMENT_ALLOWED",
                "Observed data may attach sanitized evidence without gaining control authority.",
                possibleInjection,
                evidenceRefs),

            TrustedControlOperation.SelectPrecompiledBranch => EvaluatePrecompiledBranch(
                context,
                request.Source,
                targetRef,
                possibleInjection,
                evidenceRefs),

            TrustedControlOperation.RequestSafeBlock => Allowed(
                "TRUSTED_CONTROL_SAFE_BLOCK_ALLOWED",
                "Any source may request a fail-closed block; the request cannot grant new authority.",
                possibleInjection,
                evidenceRefs),

            TrustedControlOperation.AddExternalRecipient or
            TrustedControlOperation.AccessSecret or
            TrustedControlOperation.PublishExternal => EvaluateSensitiveExternalOperation(
                context,
                request.Source,
                request.Operation,
                possibleInjection,
                evidenceRefs),

            _ => Blocked(
                "TRUSTED_CONTROL_UNSUPPORTED_OPERATION",
                "The requested control-flow operation is unsupported.",
                possibleInjection,
                evidenceRefs)
        };
    }

    public static TrustedControlFlowDecision EvaluateClaim(
        TrustedControlFlowContext context,
        PerceptionClaim claim,
        TrustedControlOperation operation,
        string targetRef,
        string? capabilityId = null)
    {
        ArgumentNullException.ThrowIfNull(claim);
        var sanitized = claim.Sanitize();
        return Evaluate(
            context,
            new TrustedControlFlowRequest(
                Source: MapSource(sanitized),
                Operation: operation,
                TargetRef: targetRef,
                ValueRedacted: sanitized.ValueRedacted,
                EvidenceRefs: sanitized.EvidenceRef.Length == 0 ? [] : [sanitized.EvidenceRef],
                CapabilityId: capabilityId));
    }

    public static TrustedControlSource MapSource(PerceptionClaim claim)
    {
        ArgumentNullException.ThrowIfNull(claim);
        if (claim.Trust == PerceptionClaimTrust.ExternalContent)
            return TrustedControlSource.ExternalDocument;
        if (claim.Trust == PerceptionClaimTrust.AgentMemory)
            return TrustedControlSource.AgentMemory;

        return claim.Source switch
        {
            Provenance.Uia or Provenance.Msaa => TrustedControlSource.UiaObservation,
            Provenance.Dom => TrustedControlSource.CdpObservation,
            Provenance.Ocr => TrustedControlSource.OcrObservation,
            Provenance.Vision => TrustedControlSource.VisualObservation,
            Provenance.Api or Provenance.Win32 or Provenance.Fixture => TrustedControlSource.TrustedApplicationState,
            _ => TrustedControlSource.AgentMemory
        };
    }

    private static TrustedControlFlowDecision EvaluateControlMutation(
        TrustedControlSource source,
        TrustedControlOperation operation,
        bool possibleInjection,
        IReadOnlyList<string> evidenceRefs)
    {
        if (!ControlAuthorities.Contains(source))
        {
            return Blocked(
                "TRUSTED_CONTROL_OBSERVED_DATA_CANNOT_MUTATE_PLAN",
                $"{source} is data, not authority, and cannot perform {operation}.",
                possibleInjection,
                evidenceRefs);
        }

        return Allowed(
            "TRUSTED_CONTROL_AUTHORITY_MUTATION_ALLOWED",
            $"{source} may perform the trusted control operation {operation}.",
            possibleInjection,
            evidenceRefs,
            canModifyMissionGoal: operation is TrustedControlOperation.DefineMissionGoal
                or TrustedControlOperation.AmendMissionGoal,
            canExpandScope: operation == TrustedControlOperation.AddPlanStep);
    }

    private static TrustedControlFlowDecision EvaluateCapabilityExpansion(
        TrustedControlFlowContext context,
        TrustedControlSource source,
        string capabilityId,
        bool possibleInjection,
        IReadOnlyList<string> evidenceRefs)
    {
        if (capabilityId.Length == 0)
        {
            return Blocked(
                "TRUSTED_CONTROL_CAPABILITY_REQUIRED",
                "Capability expansion requires an explicit capability id.",
                possibleInjection,
                evidenceRefs);
        }

        if (context.AuthorizedCapabilities.Contains(capabilityId))
        {
            return Allowed(
                "TRUSTED_CONTROL_CAPABILITY_ALREADY_AUTHORIZED",
                "The capability is already inside the authorized mission scope.",
                possibleInjection,
                evidenceRefs);
        }

        if (source != TrustedControlSource.OperatorDecision)
        {
            return RequiresOperator(
                "TRUSTED_CONTROL_CAPABILITY_EXPANSION_REQUIRES_OPERATOR",
                "A new capability expands mission scope and requires an explicit operator decision.",
                possibleInjection,
                evidenceRefs);
        }

        return Allowed(
            "TRUSTED_CONTROL_CAPABILITY_EXPANSION_AUTHORIZED",
            "The operator explicitly authorized the capability scope expansion.",
            possibleInjection,
            evidenceRefs,
            canExpandScope: true);
    }

    private static TrustedControlFlowDecision EvaluateVariableBinding(
        TrustedControlFlowContext context,
        TrustedControlSource source,
        string variableName,
        string value,
        bool possibleInjection,
        IReadOnlyList<string> evidenceRefs)
    {
        if (!context.DeclaredVariables.TryGetValue(variableName, out var definition))
        {
            return Blocked(
                "TRUSTED_CONTROL_VARIABLE_NOT_DECLARED",
                "Observed data cannot invent a new variable or target.",
                possibleInjection,
                evidenceRefs);
        }

        if (!definition.AllowedSources.Contains(source))
        {
            return Blocked(
                "TRUSTED_CONTROL_VARIABLE_SOURCE_NOT_ALLOWED",
                $"{source} is not an allowed source for variable '{definition.Name}'.",
                possibleInjection,
                evidenceRefs);
        }

        if (definition.Sensitivity == ControlVariableSensitivity.SecretReference)
        {
            var trustedSource = source is TrustedControlSource.UserInstruction
                or TrustedControlSource.OperatorDecision;
            var opaqueReference = value.StartsWith("secret-ref:", StringComparison.Ordinal) ||
                                  value.StartsWith("secret://", StringComparison.Ordinal);
            if (!trustedSource || !opaqueReference)
            {
                return Blocked(
                    "TRUSTED_CONTROL_SECRET_VALUE_REJECTED",
                    "Secret variables require an opaque secret reference from a trusted human authority.",
                    possibleInjection,
                    evidenceRefs);
            }
        }

        return Allowed(
            "TRUSTED_CONTROL_DECLARED_VARIABLE_BOUND",
            $"Declared variable '{definition.Name}' was bound from an allowed source without changing mission control flow.",
            possibleInjection,
            evidenceRefs);
    }

    private static TrustedControlFlowDecision EvaluatePrecompiledBranch(
        TrustedControlFlowContext context,
        TrustedControlSource source,
        string branchId,
        bool possibleInjection,
        IReadOnlyList<string> evidenceRefs)
    {
        if (branchId.Length == 0 || !context.AllowedBranchIds.Contains(branchId))
        {
            return Blocked(
                "TRUSTED_CONTROL_BRANCH_NOT_PRECOMPILED",
                "Observed data may only activate a branch already declared by the trusted plan.",
                possibleInjection,
                evidenceRefs);
        }

        return Allowed(
            "TRUSTED_CONTROL_PRECOMPILED_BRANCH_SELECTED",
            $"{source} selected a precompiled branch without adding a new objective or capability.",
            possibleInjection,
            evidenceRefs);
    }

    private static TrustedControlFlowDecision EvaluateSensitiveExternalOperation(
        TrustedControlFlowContext context,
        TrustedControlSource source,
        TrustedControlOperation operation,
        bool possibleInjection,
        IReadOnlyList<string> evidenceRefs)
    {
        if (source != TrustedControlSource.OperatorDecision)
        {
            return RequiresOperator(
                "TRUSTED_CONTROL_SENSITIVE_OPERATION_REQUIRES_OPERATOR",
                $"{operation} requires an explicit operator decision and cannot be authorized by observed content.",
                possibleInjection,
                evidenceRefs);
        }

        if (operation == TrustedControlOperation.PublishExternal && !context.ExternalCommunicationAllowed)
        {
            return Blocked(
                "TRUSTED_CONTROL_EXTERNAL_COMMUNICATION_NOT_AUTHORIZED",
                "External communication is not enabled for this mission.",
                possibleInjection,
                evidenceRefs);
        }

        return Allowed(
            "TRUSTED_CONTROL_SENSITIVE_OPERATION_AUTHORIZED",
            $"The operator explicitly authorized {operation} inside the current mission boundary.",
            possibleInjection,
            evidenceRefs,
            canPublishExternally: operation == TrustedControlOperation.PublishExternal);
    }

    private static TrustedControlFlowDecision AllowObservedDataOperation(
        TrustedControlSource source,
        string code,
        string reason,
        bool possibleInjection,
        IReadOnlyList<string> evidenceRefs)
    {
        if (!ControlAuthorities.Contains(source) && !ObservedDataSources.Contains(source))
        {
            return Blocked(
                "TRUSTED_CONTROL_SOURCE_UNKNOWN",
                "The source is not recognized by the trusted control-flow boundary.",
                possibleInjection,
                evidenceRefs);
        }

        return Allowed(code, reason, possibleInjection, evidenceRefs);
    }

    private static TrustedControlFlowDecision Allowed(
        string code,
        string reason,
        bool possibleInjection,
        IReadOnlyList<string> evidenceRefs,
        bool canModifyMissionGoal = false,
        bool canExpandScope = false,
        bool canPublishExternally = false) =>
        Decision(
            TrustedControlDecisionKind.Allowed,
            code,
            reason,
            possibleInjection,
            canModifyMissionGoal,
            canExpandScope,
            canPublishExternally,
            evidenceRefs);

    private static TrustedControlFlowDecision Blocked(
        string code,
        string reason,
        bool possibleInjection,
        IReadOnlyList<string> evidenceRefs) =>
        Decision(
            TrustedControlDecisionKind.Blocked,
            code,
            reason,
            possibleInjection,
            false,
            false,
            false,
            evidenceRefs);

    private static TrustedControlFlowDecision RequiresOperator(
        string code,
        string reason,
        bool possibleInjection,
        IReadOnlyList<string> evidenceRefs) =>
        Decision(
            TrustedControlDecisionKind.RequiresOperatorDecision,
            code,
            reason,
            possibleInjection,
            false,
            false,
            false,
            evidenceRefs);

    private static TrustedControlFlowDecision Decision(
        TrustedControlDecisionKind decision,
        string code,
        string reason,
        bool possibleInjection,
        bool canModifyMissionGoal,
        bool canExpandScope,
        bool canPublishExternally,
        IReadOnlyList<string> evidenceRefs) =>
        new(
            Decision: decision,
            Code: SafeRuntimeText.Sanitize(code, 120),
            Reason: SafeRuntimeText.Sanitize(reason, 500),
            PossiblePromptInjection: possibleInjection,
            CanModifyMissionGoal: canModifyMissionGoal,
            CanExpandScope: canExpandScope,
            CanPublishExternally: canPublishExternally,
            EvidenceRefs: evidenceRefs);
}
