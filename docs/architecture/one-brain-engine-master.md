# ONE BRAIN — Documento Maestro del Motor

## 1. Propósito

ONE BRAIN es un runtime cognitivo local-first para Windows.

No es un RPA clásico ni un simple ejecutor de scripts.

Su motor observa aplicaciones, entiende objetivos, resuelve targets, verifica identidad, exige aprobación segura, ejecuta acciones controladas y deja evidencia auditable.

La lógica general es:

```text
Intención / receta
    ↓
Clasificación de riesgo
    ↓
Percepción del entorno
    ↓
Resolución del target
    ↓
Identidad fuerte
    ↓
Contrato de seguridad
    ↓
Aprobación / binding
    ↓
FSM de ejecución
    ↓
Executor seguro
    ↓
Verificación
    ↓
Evidencia / métricas
```

La regla central:

> ONE BRAIN no ejecuta una acción porque "encontró un botón".
> Ejecuta sólo cuando sabe qué se quiere hacer, sobre qué elemento, bajo qué identidad, con qué permiso, con qué límite, con qué verificación y con qué evidencia.

---

## 2. Principios arquitectónicos adoptados

### 2.1 Fabricación vertical

Decisión adoptada a partir de la auditoría de Claude.

No construir capas horizontales aisladas sin consumidor real.

Incorrecto:

```text
módulo de IA
módulo de OCR
módulo de memoria
módulo de acciones
```

Correcto:

```text
necesidad real
    ↓
contrato
    ↓
identidad
    ↓
approval
    ↓
ejecución
    ↓
verificación
    ↓
evidencia
```

Cada hito debe tener consumidor real o evidencia real.

### 2.2 No ampliar superficie de acción sin contrato

No agregar capacidades peligrosas si antes no existen:

* identidad
* approval
* binding
* contrato
* FSM
* verifier
* evidencia
* métricas

Prohibido como shortcut:

* SendInput
* coordenadas
* GetClickablePoint
* clicks ciegos
* OCR como executor directo
* fallback silencioso

### 2.3 Fail-closed

Si hay duda, no se actúa.

Bloquear ante:

* target ambiguo
* identidad débil
* approval insuficiente
* RuntimeId cambiado
* elemento sin InvokePattern
* rol no permitido
* falta approval
* falta target.observe
* intervención humana
* ventana no owned
* mismatch entre aprobado y observado

### 2.4 Legacy no se elimina de golpe

La migración se hace progresivamente:

```text
legacy existente
    ↓
path nuevo opt-in
    ↓
shadow metrics
    ↓
rollout gradual
    ↓
default FSM
    ↓
retirement legacy
```

---

## 3. Componentes principales del motor

### 3.1 Recipe Engine

Entrada del sistema.

Recibe steps como:

* target.observe
* target.observe.desktop
* approval.manifest
* safe.click
* preflight
* diagnose baseline

Responsabilidad:

* leer step kind
* clasificar sensibilidad
* resolver variables
* decidir legacy/shadow/FSM
* exponer variables de salida

Estado:

* existente
* conectado a safe.click
* conectado a readiness shadow
* conectado a desktop identity shadow
* todavía no está en flip default FSM

### 3.2 Sensitive Action Classifier

Fuente canónica de sensibilidad.

Ejemplos:

* app.close => Sensitive
* target.observe => Benign
* target.observe.desktop => Benign
* unknown => Sensitive fail-closed

Estado:

* implementado
* reconciliado

### 3.3 Perception Layer

Observa el entorno.

Fuentes actuales o planificadas:

```text
UIA
Win32
browser child HWND
fixture sensor
DXGI/WGC futuro
OCR regional futuro
visión regional futuro
```

Regla:

La percepción observa, no ejecuta.

### 3.4 UIA Perception

Lee:

* Name
* AutomationId
* RuntimeId
* ControlType
* ClassName
* FrameworkId
* InvokePattern
* Window info
* AncestorPath

Limitaciones:

* apps con UIA bloqueado
* árboles vacíos
* canvas/custom render
* RuntimeId inestable

### 3.5 Win32 Layer

Complementa UIA:

* HWND
* foreground window
* process id
* window title
* ownership
* liveness
* bounds
* child windows

No se usa para clickear por coordenadas.

---

## 4. Target Resolution e identidad

### 4.1 Selector Engine

Resolver generalizable.

Flujo:

```text
candidatos observados
    ↓
ElementIdentity
    ↓
ElementFingerprintBuilder
    ↓
SelectorEngine
    ↓
SelectorResolution
```

### 4.2 ElementIdentity

Identidad estructural de un elemento:

* RuntimeId
* AutomationId
* Name
* ControlType
* ClassName
* FrameworkId
* AncestorPath
* ProcessName
* WindowTitle

### 4.3 ElementFingerprintBuilder

Construye digest/fingerprint.

Sirve para comparar:

```text
identidad aprobada
vs
identidad observada
```

### 4.4 WebTargetResolver

Resolver web fortalecido.

Estado:

* legacy behavior intacto
* SelectorEngine shadow
* identidad web fuerte
* RootHwnd / SelectedHwnd
* safe-executor opt-in compatible

### 4.5 Desktop Target Observe

Path read-only agregado para desktop.

Step:

```text
target.observe.desktop
```

Produce identidad desktop UIA:

* source = uia
* RuntimeId si existe
* AutomationId
* ControlType
* ClassName
* FrameworkId
* ProcessName
* WindowTitle
* AncestorPath
* IdentityStrength Strong / Weak / None

Estado:

* implementado en shadow/read-only
* sin ejecución desktop FSM
* sin clicks reales
* alimenta approval.manifest, shadow readiness y métricas

---

## 5. Approval e identidad

### 5.1 Approval v2

Sistema histórico.

Regla:

No romper.

No cambiar:

* approval-v2
* evidenceHash
* policyVersion
* ValidateApprovalBinding

### 5.2 Approval Manifest v3

Metadata aditiva con identidad.

Campos:

* IdentitySchemaVersion = approval-v3
* IdentityStrength
* ApprovedIdentityDigest
* ApprovedSelector
* IdentityBindingHash
* IdentitySource

### 5.3 target.observe / target.observe.desktop

Capturan identidad antes del approval.

Web:

```text
target.observe
```

Desktop:

```text
target.observe.desktop
```

### 5.4 ApprovalBindingValidator

Valida aprobado vs observado.

Para safe.click irreversible:

```text
Sólo Same es aceptable.
```

No se acepta LikelySame.

---

## 6. Contrato de seguridad

### 6.1 RecipeSafetyContract

Describe qué se permite hacer.

Campos conceptuales:

* ActionKind
* ExpectedIdentity
* Selector
* WindowConstraints
* Reversible
* MaxActions
* ActionCeiling
* Provenance
* TrustLevel
* ApprovalRef

### 6.2 ContractValidator

Valida el contrato antes de ejecutar.

Si no es seguro:

```text
PolicyDenied
```

---

## 7. FSM de ejecución

### 7.1 SafeExecutionFsm

Corazón de ejecución segura.

Estados conceptuales:

```text
Created
    ↓
ContractValidated
    ↓
ApprovalValidated
    ↓
TargetResolved
    ↓
OwnershipChecked
    ↓
Dispatched
    ↓
Verified
    ↓
Succeeded / Failed / Blocked
```

### 7.2 EvidenceLedger

Registra transiciones y evidencia.

### 7.3 CancellationPolicy

Controla interrupciones humanas o cambios de contexto.

---

## 8. Executors

### 8.1 UiaPatternExecutor

Executor seguro nuevo.

No usa:

* el.Click
* coordenadas
* SendInput
* GetClickablePoint

Usa:

```text
InvokePattern.Invoke()
```

Allowlist actual:

* Button
* Hyperlink
* MenuItem

Bloqueados:

* CheckBox
* RadioButton
* ComboBox
* Edit
* Document
* Window
* Pane
* Custom
* Unknown

Future:

* SplitButton
* ListItem
* TabItem

Soporta RootHwnd aditivo.

### 8.2 ExecutorSurfacePolicy

Define qué roles son ejecutables.

### 8.3 el.Click

Legacy.

Permitido sólo en legacy.

Prohibido en path safe-executor.

Pendiente de retiro.

### 8.4 UiaActionExecutor

Legacy/fallback.

Legacy-only.

Prohibido en safe-executor.

Pendiente de retiro.

---

## 9. safe.click

### 9.1 Legacy path

Sin dispatchPath:

```text
safe.click legacy
```

Comportamiento histórico intacto.

### 9.2 Safe-executor opt-in

Con:

```text
dispatchPath=safe-executor
```

Flujo:

```text
preflight
    ↓
ValidateApprovalBinding
    ↓
executionAllowedInThisHito
    ↓
approval-v3 strong
    ↓
ApprovalBindingValidator
    ↓
RecipeSafetyContract
    ↓
SafeExecutionFsm
    ↓
UiaPatternExecutor
    ↓
SafeClickStepVerifier
```

### 9.3 Shadow readiness

Para todo safe.click se calcula readiness sin cambiar comportamiento.

Variables:

* safeClick.fsmReady.*
* safeClick.legacy.*
* safeClick.migration.*

---

## 10. SafeClickStepVerifier

Valida:

* dispatch success
* ObservedActions == 1
* ObservedIdentity presente
* Verdict == Same
* Target visible
* Window found
* sin interrupción humana

---

## 11. Métricas de migración

Miden readiness del corpus.

Campos principales:

* TotalSafeClicks
* EligibleForFsm
* NotEligibleForFsm
* ApprovalV3Strong
* ApprovalV2
* WeakIdentity
* TargetObservePresent
* TargetObserveMissing
* RuntimeIdPresent
* RuntimeIdMissing
* RuntimeIdStable
* RuntimeIdChanged
* RuntimeIdUnknown
* InvokePatternAvailable
* InvokePatternUnavailable
* InvokePatternUnknown
* UsesElClick
* UsesUiaActionExecutor
* WouldRequireLegacy
* WouldUseUnsafeFallback
* WebUiaEligible
* DesktopUiaObservable
* DesktopUiaStrong
* DesktopUiaWeak
* DesktopMissingIdentity

Objetivo:

Decidir el flip default con evidencia.

---

## 12. Fallbacks futuros

### 12.1 SemanticAccessFallback

Orden conceptual:

```text
UIA
→ Win32 handles
→ OCR/visión regional
→ hotkeys controladas
→ recetas por coordenadas relativas
→ RemoteAgent si aplica
```

Regla:

Fallback no significa acción insegura.

Cada fallback debe producir identidad, evidencia y contrato.

### 12.2 OCR regional

No OCR de pantalla completa como primera opción.

Uso:

* lectura
* desambiguación
* diagnóstico

No executor directo para acciones críticas.

### 12.3 Visión regional

Preferir:

* región del HWND
* región del control
* captura semántica
* hash visual
* dirty regions
* WGC/DXGI si aplica

---

## 13. Memoria de procesos

No es RAG normal.

RAG normal:

```text
pregunta → documentos → respuesta
```

ONE BRAIN process memory:

```text
tarea
    ↓
procesos similares
    ↓
pasos usados
    ↓
errores
    ↓
decisiones
    ↓
verificaciones
    ↓
receta candidata
```

Unidad de memoria:

* flujo de trabajo
* objetivo
* pasos
* targets
* decisiones
* bloqueos
* evidencias
* resultado

Estado:

* pendiente
* posterior al motor seguro

---

## 14. API / MCP / integración externa

Caso:

Un ERP pide información externa sin integración directa.

ONE BRAIN puede exponer capacidades:

```text
getSupplierInvoiceStatus
getWebsitePrice
checkPortalOrder
downloadReport
fillExternalForm
```

Flujo:

```text
ERP/API request
    ↓
capability registry
    ↓
recipe/process memory
    ↓
safe execution
    ↓
evidence
    ↓
structured response
```

Estado:

* conceptual
* posterior al motor

---

## 15. Diferencia con RPA clásico

RPA clásico:

* graba clicks
* usa coordenadas
* depende de pantalla estable
* rompe con cambios UI
* poca comprensión semántica
* difícil auditar intención

ONE BRAIN:

* identidad fuerte
* contrato
* approval binding
* FSM
* evidencia
* percepción multi-fuente
* shadow readiness
* migración segura
* memoria de procesos
* API de capacidades

Diferencia clave:

> ONE BRAIN no automatiza sólo superficie; automatiza con identidad, permiso, límite y evidencia.

---

## 16. Qué falta para motor 100%

### Núcleo safe.click

Falta:

* gradual enablement
* FSM default para elegibles
* opt-out legacy deprecated
* legacy retirement
* auditoría final

### Percepción robusta

Falta:

* WindowLivenessMonitor
* SystemOverlayDetector
* UIA empty/block detection
* SemanticAccessFallback
* OCR regional read-only
* visión regional

### Acciones adicionales

Faltan acciones seguras:

* safe.type
* safe.select
* safe.read
* safe.download
* safe.upload
* safe.form.fill
* safe.modal.confirm

Cada acción necesita:

```text
contrato + identity + approval + executor + verifier + evidence
```

### Memoria / aprendizaje

Falta:

* flow ledger histórico
* process RAG
* receta candidata
* aprendizaje por repetición

### Integración externa

Falta:

* capability registry
* API local
* MCP server
* ERP integration

---

## 17. Qué quizá no se pueda lograr al 100%

### 17.1 Control universal de cualquier app

No existe garantía universal.

Algunas apps:

* bloquean UIA
* renderizan todo custom
* cambian RuntimeId
* usan canvas
* protegen ventanas
* impiden automatización
* tienen overlays
* usan anti-bot

### 17.2 Identidad perfecta siempre

RuntimeId puede cambiar.

DOM/UIA puede re-renderizar.

Algunos elementos no tienen AutomationId.

Regla:

```text
Same fuerte cuando se puede.
Fail-closed cuando no.
```

### 17.3 OCR como verdad absoluta

OCR falla.

Debe ser señal auxiliar.

### 17.4 Aprendizaje sin supervisión

No ejecutar procesos nuevos sin approval, contrato y verificación.

---

## 18. Roadmap del motor

### Fase actual: safe.click migration

Avance aproximado: 80%

Siguientes pasos:

* HITO-147 — Gradual Enablement / FSM default for eligible steps **(implementado: web eligible-only, kill-switch `ONEBRAIN_SAFE_CLICK_FSM_DEFAULT`, predicado endurecido con InvokePattern + rol allowlisted + web-uia, opt-out `dispatchPath=legacy` deprecated, sin fallback silencioso; NO retira legacy; desktop excluido del default)**
* HITO-148 — Web Stabilization + RuntimeId Hardening
* HITO-149 — Legacy opt-out deprecated
* HITO-150 — Legacy retirement safe.click
* HITO-151 — Audit final safe.click engine

### Fase siguiente: percepción robusta

* WindowLivenessMonitor
* SystemOverlayDetector
* UIA empty/block detection
* SemanticAccessFallback
* OCR regional read-only
* Vision region verification

### Fase siguiente: más acciones seguras

* safe.type
* safe.select
* safe.read
* safe.download
* safe.upload
* safe.form.fill
* safe.modal.confirm

### Fase siguiente: memoria de procesos

* flow ledger
* process memory
* repeated workflow learning
* recipe suggestions

### Fase siguiente: API/MCP

* capability registry
* local API
* MCP server
* ERP integration

---

## 19. Porcentajes actuales

* Safety Engine: 90%
* Approval + Identity: 88%
* Selector/Resolution: 82%
* FSM Execution: 82%
* safe.click Migration: 80%
* Migration Metrics: 85%
* Desktop Identity Shadow: 55%
* Perception Robusta: 25%
* Acciones Seguras Adicionales: 10%
* Process Memory: 0%
* Capability/API Layer: 5%

Motor núcleo actual:

```text
78–82%
```

Motor ONE BRAIN completo:

```text
55–60%
```

Producto comercial usable:

```text
30%
```

---

## 20. Regla de uso de este documento

Este documento debe usarse como bitácora viva.

Cada hito nuevo debe evaluarse contra:

1. Roadmap oficial.
2. Documento Maestro del Motor.
3. Auditorías de Claude.
4. Principios de safety aprobados.

Si un hito no contribuye al motor, debe marcarse como:

* interfaz
* producto
* integración comercial
* deuda técnica
* desviación

Mientras el motor no esté completo, no avanzar a interfaz salvo necesidad estrictamente justificada.
