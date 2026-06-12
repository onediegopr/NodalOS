# HITO-091+092+093 - AI Model Router + OpenAI Profiles + Config Console

## Scope

This hito creates the professional AI architecture foundation for ONE BRAIN:

- Central AI model router.
- Official profile IDs.
- Provider/model/key/budget/limit configuration.
- Pilot configuration console.
- Deterministic routing tests.
- Secret masking and fail-closed behavior.

No real OpenAI call is implemented in this hito. The goal is architecture, configuration, safety, and test coverage.

## Official AI Decision

- OpenAI is the initial primary provider.
- Future provider calls must go through `OneBrain.AI.ModelRouter` / `OneBrain.Core.AI.AIModelRouter`.
- Business modules must not call OpenAI directly.
- No local AI is introduced in this line.
- No Ollama.
- No LM Studio.
- No model names hardcoded in business code.
- No API keys committed.
- No full API keys displayed.
- No secrets logged.

OpenAI API keys are not natively tied to one model. ONE BRAIN maps:

- profile -> provider -> model -> secret reference -> budget/risk policy

That mapping is controlled by configuration.

## HITO-091 - AI Model Router Foundation

Created:

- `src/OneBrain.Core/AI/AIModels.cs`
- `src/OneBrain.Core/AI/AIModelRouter.cs`
- `src/OneBrain.Core/AI/IAIModelClient.cs`

Core concepts:

- `AIProfileKind`
- `AIProfileId`
- `AIProviderKind`
- `AIRiskLevel`
- `AIModelCapability`
- `AIModelProfile`
- `AIModelRoutingPolicy`
- `AIModelRoutingRequest`
- `AIModelRoutingDecision`
- `AIModelRouterResult`
- `IAIModelClient`
- `MockAIModelClient`

Official profiles:

- `OB_AI_CHEAP_INTENT`
- `OB_AI_STANDARD_TASK`
- `OB_AI_CRITICAL_REASONER`
- `OB_AI_VISION_VERIFIER`

Routing behavior:

- Simple/low-risk intent -> `OB_AI_CHEAP_INTENT`.
- Normal operational task -> `OB_AI_STANDARD_TASK`.
- Sensitive/high-risk/irreversible/ambiguous task -> `OB_AI_CRITICAL_REASONER`.
- Vision/screen/OCR-like request -> `OB_AI_VISION_VERIFIER`.
- Vision plus high risk/ambiguity -> escalate to `OB_AI_CRITICAL_REASONER`.
- Missing profile/config/required key/budget -> fail closed.

The router returns `WouldCallProvider=false` in v0. It only selects or blocks.

## HITO-092 - OpenAI Profiles Configuration

Created:

- `src/OneBrain.Core/AI/AIModelConfiguration.cs`

Each profile supports:

- provider
- model
- apiKeySecretName
- apiKeyConfigured
- apiKeyMasked
- enabled
- monthlyBudgetUsd
- dailyBudgetUsd
- maxCostPerTaskUsd
- maxCallsPerTask
- timeoutSeconds
- retryCount
- fallbackProfile
- maxRiskLevel
- requiresAuditLog
- debugMode
- usageLoggingEnabled

Environment variables:

- `OB_AI_CHEAP_INTENT_PROVIDER`
- `OB_AI_CHEAP_INTENT_MODEL`
- `OB_AI_CHEAP_INTENT_API_KEY`
- `OB_AI_STANDARD_TASK_PROVIDER`
- `OB_AI_STANDARD_TASK_MODEL`
- `OB_AI_STANDARD_TASK_API_KEY`
- `OB_AI_CRITICAL_REASONER_PROVIDER`
- `OB_AI_CRITICAL_REASONER_MODEL`
- `OB_AI_CRITICAL_REASONER_API_KEY`
- `OB_AI_VISION_VERIFIER_PROVIDER`
- `OB_AI_VISION_VERIFIER_MODEL`
- `OB_AI_VISION_VERIFIER_API_KEY`

Additional supported suffixes per profile:

- `_ENABLED`
- `_MONTHLY_BUDGET_USD`
- `_DAILY_BUDGET_USD`
- `_MAX_COST_PER_TASK_USD`
- `_MAX_CALLS_PER_TASK`
- `_TIMEOUT_SECONDS`
- `_RETRY_COUNT`
- `_FALLBACK_PROFILE`
- `_MAX_RISK_LEVEL`
- `_REQUIRES_AUDIT_LOG`
- `_DEBUG_MODE`
- `_USAGE_LOGGING_ENABLED`

Rules:

- Models come from configuration.
- Missing model blocks routing.
- Missing API key blocks routing for non-mock providers.
- API keys are represented as `ApiKeySecretName`, `ApiKeyConfigured`, and `ApiKeyMasked`.
- Full secret values are not retained in `AIModelProfile`.
- Budget and per-task limits fail closed.

## HITO-093 - AI Configuration Console in Pilot

Added Pilot UI routes:

- `/ai/config`
- `/ai/config/test`

The console displays:

- Cheap Intent Engine
- Standard Task Engine
- Critical Reasoner
- Vision Verifier

Per profile it shows:

- provider
- model
- masked secret/key status
- enabled/disabled
- monthly budget
- per-task budget
- max risk level
- fallback
- status
- usage logging enabled

The “test configuration” action is a dry-run routing test. It does not call OpenAI or any other provider.

## Safety

This hito does not change runtime action safety:

- No browser automation is added.
- No web call is added.
- No provider call is made.
- No API key is required for tests.
- No API key is committed.
- No API key is logged or displayed in full.
- No playback.
- No clicks.
- No login.
- No cookies.
- No carrito.
- No compra.
- No pago.
- No recipe generation.
- No autonomous agent execution.

## Tests

Added:

- `tests/OneBrain.Recipes.Tests/AIModelRouterTests.cs`
- `tests/OneBrain.Recipes.Tests/AIModelConfigurationTests.cs`
- `tests/OneBrain.Recipes.Tests/PilotAIConfigConsoleTests.cs`

Coverage:

- Cheap Intent routing for simple intent.
- Standard Task routing for normal operational task.
- Critical Reasoner routing for send/delete/pay/purchase/login/cookies/publication-style risk.
- Vision Verifier routing for vision requests.
- Vision high-risk escalation to Critical Reasoner.
- Disabled profile fallback.
- Critical Reasoner disabled with no fallback fails closed.
- Budget exceeded blocks routing.
- Missing config blocks routing.
- Key masking does not reveal full secret.
- Pilot console mentions the four official profiles.

## Validation

Expected validation:

- Build OK.
- Tests PASS.
- `NEGATIVE_EXIT_CODE=1`.
- Demo script OK.
- Demo Markdown dry-run/run OK.
- Demo HTML dry-run/run OK.
- ML readonly/diagnostic OK when executed.
- `artifacts/` ignored and not tracked.
- No Firefox flaky fixture as mandatory regression.
- No real OpenAI calls.

## Updated Completion

- Core tecnico: 94-96%
- Safety: 94-96%
- Cleanup: 88-90%
- Validacion/CI confiable: 93-95%
- Web read-only: 87-89%
- Retail public read-only: 82-84%
- Evidence/reporting: 86-89%
- Demo/reproducibilidad: 91-93%
- Pilot UX local: 36-43%
- Intent routing IA-safe: 38-46%
- Recording/shadow learning: 22-28%
- Approval UX: 18-25%
- Recipe confidence: 20-28%
- AI model governance: 25-34%
- MVP comercial serio: 77-80%
- Producto completo/pro: 56-60%
