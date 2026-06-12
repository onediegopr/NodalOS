# HITO-088+089+090 - Approval UX + Recipe Confidence + First WhatsApp/Browser Demo Flow

## Scope

This block connects recording/timeline/human annotation with supervised human approval and recipe/candidate-flow confidence.

It does not add autonomous execution. It does not generate executable recipes automatically. It does not add playback for sensitive actions.

## HITO-088 - Approval UX

Implemented core approval models:

- `ApprovalRequest`
- `ApprovalDecision`
- `ApprovalRiskLevel` values via `ApprovalRiskLevels`
- `ApprovalActionKind` values via `ApprovalActionKinds`
- `HumanInTheLoopMode` values via `HumanInTheLoopModes`
- `PlatformApprovalPolicy`
- `ApprovalPolicy`
- `ApprovalArtifactWriter`

Approval artifacts are written locally under:

- `artifacts/approvals/`

Sensitive action kinds that always require approval:

- `send`
- `submit`
- `delete`
- `publish`
- `pay`
- `purchase`
- `login`
- `accept_terms`
- `accept_cookies`
- `modify_financial_data`
- `modify_legal_data`
- `run_script`
- `install_software`

Behavior:

- Human-in-the-loop is a configurable platform policy, not a rigid universal requirement.
- It can be configured globally, per recipe/candidate flow, by confidence score, by risk level, by environment, and by profile.
- Default mode is conservative while ONE BRAIN is learning.
- High confidence and low risk flows can be configured to require less manual approval when a safe executor exists.
- Sensitive risk stays approval-first by default even when a recipe becomes stable.
- Approval requests are fail-closed if required context is missing.
- Approval requests are fail-closed when no safe executor exists.
- Sensitive previews are sanitized with `[REDACTED]`.
- Approve/reject records an audit decision only.
- `ExecutionAllowed=false` in v0 because there is no safe executor for approved sensitive actions.
- Reject decisions require a reason; empty reject reason is normalized to a required-reason note.

Default human-in-the-loop guidance:

- While ONE BRAIN learns: high human-in-the-loop by default.
- When a recipe gains confidence: human-in-the-loop can become configurable.
- Sensitive risk: human-in-the-loop remains required by default.
- Money, sending, publishing, deleting, login, cookies, legal/financial data, scripts, and installs: human-in-the-loop remains required by default even if a recipe is stable.
- Missing critical information or missing safe executor: fail closed.

Pilot UI integration:

- `/approvals/demo`
- `/approvals/demo/decide`

The UI shows a fixture approval request and confidence profile. The buttons record preview decisions only. They do not execute the approved action.

## HITO-089 - Recipe Confidence

Implemented confidence models:

- `RecipeConfidenceInput`
- `RecipeConfidenceProfile`
- status values:
  - `new`
  - `candidate`
  - `supervised`
  - `stable`
  - `critical`
  - `disabled`
  - `blocked`
- `RecipeConfidenceScorer`
- `RecipeConfidenceArtifactWriter`

Confidence artifacts are written locally under:

- `artifacts/recipe-confidence/`

Deterministic scoring inputs:

- `recipeId` / `candidateFlowId`
- `status`
- `riskLevel`
- `runs`
- `successes`
- `failures`
- `lastError`
- `lastVerifiedAt`
- `approvalRequiredUntil`
- `notes`

Rules:

- Successful low-risk history increases score.
- Failures degrade score.
- Higher risk decreases score.
- Critical flows without `approvalRequiredUntil` are blocked and capped at low confidence.
- Disabled flows stay disabled.
- No model or AI call is used.
- Confidence can influence approval policy only through explicit policy configuration.
- Confidence does not grant execution by itself.

## HITO-090 - First WhatsApp/Browser Demo Flow

Implemented `BusinessFlowDemoFixture`.

The first business-flow fixture represents a safe WhatsApp/browser-style commercial flow:

- Prepare a demo message/presupuesto preview.
- Create an `ApprovalRequest` for `send`.
- Mark the candidate flow as critical/blocked until explicit approval policy exists.
- Show the approval/confidence state in Pilot.

The fixture does not:

- send a message
- open WhatsApp
- open browser
- click
- type
- submit
- login
- accept cookies
- add to cart
- purchase
- pay
- create an executable recipe

## Safety

This hito preserves the current safety contract:

- No clicks.
- No login.
- No cookies accepted.
- No carrito.
- No compra.
- No pago.
- No checkout.
- No submit.
- No WhatsApp send.
- No script execution.
- No playback of sensitive actions.
- No autonomous free-agent execution.
- No recipe is generated automatically from recording/timeline.
- Firefox flaky fixture is not part of mandatory regression.

## AI Decision

No real LLM call is implemented in this hito.

The current intent routing remains rules-based and allowlisted. Future AI must go through `OneBrain.AI.ModelRouter` and official OpenAI-only profiles:

- `OB_AI_CHEAP_INTENT`
- `OB_AI_STANDARD_TASK`
- `OB_AI_CRITICAL_REASONER`
- `OB_AI_VISION_VERIFIER`

No local AI, Ollama, or LM Studio is introduced in this line.

## Files

Created:

- `src/OneBrain.Core/Approval/ApprovalModels.cs`
- `src/OneBrain.Core/Approval/ApprovalPolicy.cs`
- `src/OneBrain.Core/Approval/ApprovalArtifactWriter.cs`
- `src/OneBrain.Core/Approval/BusinessFlowDemoFixture.cs`
- `src/OneBrain.Core/Confidence/RecipeConfidenceModels.cs`
- `src/OneBrain.Core/Confidence/RecipeConfidenceScorer.cs`
- `src/OneBrain.Core/Confidence/RecipeConfidenceArtifactWriter.cs`
- `tests/OneBrain.Recipes.Tests/ApprovalPolicyTests.cs`
- `tests/OneBrain.Recipes.Tests/ApprovalArtifactWriterTests.cs`
- `tests/OneBrain.Recipes.Tests/RecipeConfidenceScorerTests.cs`
- `tests/OneBrain.Recipes.Tests/RecipeConfidenceArtifactWriterTests.cs`
- `tests/OneBrain.Recipes.Tests/PilotApprovalDemoTests.cs`

Modified:

- `src/OneBrain.Pilot/PilotHomePageRenderer.cs`
- `src/OneBrain.Pilot/Program.cs`

## Validation

Expected validation:

- Build OK, 0 warnings, 0 errors.
- Tests PASS.
- `NEGATIVE_EXIT_CODE=1`.
- Demo script OK.
- Demo Markdown dry-run/run OK.
- Demo HTML dry-run/run OK.
- ML readonly/diagnostic OK when executed.
- `artifacts/` ignored and not tracked.
- No Firefox flaky fixture as mandatory regression.

## Updated Completion

- Core tecnico: 93-95%
- Safety: 93-95%
- Cleanup: 88-90%
- Validacion/CI confiable: 92-94%
- Web read-only: 87-89%
- Retail public read-only: 82-84%
- Evidence/reporting: 86-89%
- Demo/reproducibilidad: 91-93%
- Pilot UX local: 31-38%
- Intent routing IA-safe: 23-29%
- Recording/shadow learning: 22-28%
- Approval UX: 18-25%
- Recipe confidence: 20-28%
- MVP comercial serio: 75-78%
- Producto completo/pro: 53-56%
