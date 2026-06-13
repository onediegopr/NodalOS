# HITO-156 — Core Engine Hardening Before safe.type

## Objetivo

Cerrar riesgos del núcleo detectados en la auditoría H155 antes de replicar el patrón de `safe.click` hacia nuevas acciones.

No se implementa `safe.type`.

## Invoke-time identity gate

`UiaPatternExecutor` ahora valida identidad inmediatamente antes de `InvokePattern.Invoke()`.

Flujo:

```text
tree-walk vivo
    ↓
SelectorEngine.Resolve
    ↓
reattach BestMatch a AutomationElement
    ↓
ExecutorSurfacePolicy
    ↓
ElementMatcher(expected, observed)
    ↓
Verdict == Same
    ↓
InvokePattern.Invoke()
```

Bloquea fail-closed si:

- falta `ExpectedIdentity`
- `ExpectedIdentity` no es fuerte
- el elemento observado no produce `Same`
- el resultado es `LikelySame`, `Different`, `Unknown`, `Ambiguous` o `Stale`

Razones:

- `InvokeTimeExpectedIdentityRequired`
- `InvokeTimeIdentityMismatch`

No hay fallback.

## Resultado de executor enriquecido

`PatternExecutionResult` agrega metadata aditiva:

- `InvokeTimeIdentityChecked`
- `InvokeTimeIdentityVerdict`
- `InvokeTimeIdentityReason`
- `ExpectedIdentityDigest`
- `ObservedIdentityDigest`

`SafeClickStepVerifier` propaga el verdict de invoke-time cuando el dispatch falla antes de verificación.

## Contract policy por acción

`ContractValidator` agrega reglas explícitas para `ActionKind == "click"`:

- `Reversible == false`
- `MaxActions == 1`
- `ApprovalRef` presente
- `ExpectedIdentity` fuerte
- `Selector` presente
- `ActionCeiling == FullActionWithPreflight`
- `Provenance == Uia`
- `TrustLevel >= ProfileVerified`

Razones nuevas:

- `ClickMustBeIrreversible`
- `ClickMaxActionsMustBeOne`
- `ClickRequiresApprovalRef`
- `ClickRequiresStrongIdentity`
- `ClickRequiresSelector`
- `ClickRequiresFullActionWithPreflight`
- `ClickRequiresUiaProvenance`
- `ClickRequiresProfileVerifiedTrust`

## Ledger pre-FSM

Los bloqueos pre-FSM de `safe.click` ahora generan ledger mínimo:

- `safeClick.fsm.transitionCount > 0`
- `safeClick.fsm.ledgerJson != []`

No se inventa dispatch. La transición documenta el bloqueo pre-FSM.

## Legacy muerto

`safe.click` sigue sin ejecutar legacy.

El método legacy privado queda aislado con bloqueo inmediato por retiro si alguien intenta llamarlo accidentalmente.

No se eliminó `UiaActionExecutor` global porque otros flujos pueden usarlo fuera de `safe.click`.

## Qué no se hizo

- No `safe.type`.
- No nuevas acciones.
- No cambios de interfaz.
- No API/MCP.
- No `SendInput`.
- No coordenadas.
- No `GetClickablePoint` como executor.
- No OCR.
- No fallback visual.
- No fallback silencioso.
- No cambios a `approval-v2`.
- No cambios a `evidenceHash`.
- No cambios a `policyVersion`.
- No cambios a `ValidateApprovalBinding`.
- No `RegionSelector`.
- No `BasicActionVerifier`.

## Próximo hito

Decidir entre:

- H157 — Perception Liveness / Overlay
- H158 — safe.type
