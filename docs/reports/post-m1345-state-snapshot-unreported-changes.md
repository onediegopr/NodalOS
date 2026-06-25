# Post-M1345 State Snapshot / Unreported Changes Reconciliation

## 1. Decisión

`POST_M1345_UNREPORTED_CHANGES_NEED_USER_DECISION`

El repo no está en el estado reportado después de M1345. El worktree está limpio y alineado con origin, pero `HEAD` avanzó desde `105d0855fbf71b028552c79d29dde32d6ee4f5f4` hasta `bd0c6cf4063bb3ec7a345480253063bbeb4e8e63` con seis commits no reportados en el cierre M1345.

Los cambios no rompen Mission Control, Browser Skills ni el harness instalado, pero agregan una línea completa de `stealth-engine`, proxy, fingerprint, CAPTCHA solver, panel stealth, Docker/deploy y cambios al Bridge. Eso contradice el mandato vigente de no agregar stealth/proxy/CAPTCHA solver, no agregar runtime nuevo y no avanzar a governance/stealth.

No se revirtió nada porque los cambios están committeados y sincronizados con origin. Revertirlos requiere decisión explícita.

## 2. HEAD / base / origin

| Item | Estado |
|---|---|
| Worktree | `C:\DESARROLLO\NodalOS\Codigo-m12-audit` |
| Branch | `chrome-lab-001-extension-local-ai-bridge` |
| Base reportada M1345 | `105d0855fbf71b028552c79d29dde32d6ee4f5f4` |
| HEAD actual | `bd0c6cf4063bb3ec7a345480253063bbeb4e8e63` |
| Upstream | `origin/chrome-lab-001-extension-local-ai-bridge` |
| Estado tracking | alineado con origin |
| Worktree final | limpio |

Commits posteriores a M1345:

| Commit | Mensaje | Clasificación |
|---|---|---|
| `9c2c892` | `feat: NODAL OS v1.0.0-production - Stealth Engine completo con Docker, documentacion y cierre de ciclo` | `DANGEROUS` |
| `ad43a1b` | `fix: correcciones de auditoria - WebSocket, CORS, handoff idempotente, token constante, SRP` | `TECHNICAL_FIX` mezclado con `DANGEROUS` |
| `2bbb187` | `docs: informe de auditoria de sigilo - 5 modulos, 25 hallazgos, top 10 criticos` | `REPORT_ONLY` / `DANGEROUS_CONTEXT` |
| `eaee7a5` | `fix: correcciones criticas de auditoria - solver, recovery, fonts, mouse, fingerprint, token, URL, proxy` | `DANGEROUS` |
| `7326163` | `fix: ensureCoherence retorno capturado + informe de re-auditoria` | `TECHNICAL_FIX` mezclado con `DANGEROUS_CONTEXT` |
| `bd0c6cf` | `fix: cierre de auditoria Claude - WS send serialization, handoff idempotente, redaccion estructurada, ex.Message sanitizado, buffer cap` | `TECHNICAL_FIX` mezclado con `DANGEROUS_CONTEXT` |

## 3. Archivos cambiados y clasificación

| Archivo(s) | Tipo de cambio | Clasificación | Decisión recomendada | Motivo |
|---|---|---|---|---|
| `CHANGELOG.md`, `docs/ARCHITECTURE.md`, `docs/CONFIGURATION.md`, `docs/DEPLOYMENT.md`, `docs/OPERATIONS.md`, `docs/ROADMAP.md` | docs nuevos | `REPORT_ONLY` / `DANGEROUS_CONTEXT` | mover a archivo o revertir | Documentan línea stealth/proxy/CAPTCHA ajena al roadmap actual. |
| `docs/stealth-engine-design.md`, `docs/stealth-audit-report.md`, `docs/stealth-reaudit-report.md`, `docs/unified-friction-integration-design.md` | docs nuevos | `DANGEROUS` | requerir decisión explícita; probable revert/archivo | Reintroducen sigilo, fingerprint, proxy, solver y auditoría de evasión. |
| `docker-compose.yml`, `src/OneBrain.ChromeLab.Bridge/Dockerfile`, `stealth-engine/Dockerfile`, `stealth-panel/Dockerfile` | infraestructura nueva | `DANGEROUS` | revertir salvo decisión explícita | Introducen despliegue para stealth engine/panel. |
| `scripts/deploy.ps1`, `scripts/stop.ps1` | scripts nuevos | `DANGEROUS` | revertir salvo decisión explícita | Automatizan instalación/arranque de stealth engine y panel. |
| `src/OneBrain.ChromeLab.Bridge/ChromeLabOptions.cs` | runtime config | `DANGEROUS` | revertir o aislar detrás de decisión | Agrega opciones de stealth, solver, retries y fingerprint profile. |
| `src/OneBrain.ChromeLab.Bridge/ChromeLabProtocol.cs` | protocolo | `DANGEROUS` | revisar/revertir | Agrega mensajes/protocolo stealth. |
| `src/OneBrain.ChromeLab.Bridge/Program.cs` | bridge runtime | `DANGEROUS` con posibles `TECHNICAL_FIX` | separar fixes útiles de integración stealth | Cambia superficie central del bridge; riesgo de mezcla entre companion y stealth. |
| `src/OneBrain.ChromeLab.Bridge/Sessions/*` | refactor WebSocket/session | `TECHNICAL_FIX` | considerar mantener tras revisión aislada | Puede mejorar serialización/session handling, pero llegó mezclado con stealth. |
| `src/OneBrain.ChromeLab.Bridge/Stealth/*` | módulo nuevo | `DANGEROUS` | revertir salvo decisión explícita | Implementa fricción/stealth/handoff policy orientado a stealth. |
| `src/OneBrain.BrowserExecutor.Cdp/BrowserCredentialBoundaryService.cs` | boundary update | `TECHNICAL_FIX` / `UNKNOWN` | revisar diff puntual | Puede ser fix útil, pero debe separarse del paquete stealth. |
| `src/OneBrain.BrowserExecutor.Contracts/BrowserCredentialBoundaryContracts.cs` | contrato update | `TECHNICAL_FIX` / `UNKNOWN` | revisar diff puntual | Posible fix útil, requiere extracción limpia si se revierte stealth. |
| `stealth-engine/**` | engine completo nuevo | `DANGEROUS` | revertir salvo decisión explícita | Contiene fingerprint, proxy, CAPTCHA solver, behavior simulation y anti-blocking. |
| `stealth-panel/**` | panel nuevo | `DANGEROUS` | revertir salvo decisión explícita | Superficie nueva para handoff stealth, fuera del roadmap actual. |

## 4. Qué se mantuvo por ahora

Se mantuvo todo porque los cambios están en commits ya aplicados y origin está alineado. No se hizo revert automático.

Los únicos elementos que parecen candidatos a conservar son los fixes técnicos no específicos de stealth:

- WebSocket send serialization.
- Handoff idempotente.
- Redacción estructurada.
- Sanitización de `ex.Message`.
- Buffer cap.
- Refactor de sesiones WebSocket si no cambia comportamiento companion.

Estos deben extraerse en un commit limpio si se decide revertir la línea stealth.

## 5. Qué se recomienda revertir o archivar

Revertir o archivar, salvo decisión explícita del usuario:

- `stealth-engine/**`
- `stealth-panel/**`
- `src/OneBrain.ChromeLab.Bridge/Stealth/**`
- docs de stealth / audit / re-audit / unified friction stealth
- Docker/deploy relacionados con stealth
- opciones runtime de stealth/proxy/CAPTCHA solver

Motivo: contradicen el mandato vigente y el límite de producto aceptable. También distraen del próximo bloque real: Workspace Open + Project Understanding visible read-only.

## 6. Validaciones

| Validación | Resultado |
|---|---|
| `node scripts/verify-installed-sidepanel.mjs` | PASS |
| `node --check scripts/verify-installed-sidepanel.mjs` | PASS |
| `node --check browser-extension/onebrain-chrome-lab/sidepanel.js` | PASS |
| `dotnet build .\OneBrain.slnx --no-restore` | PASS, warnings preexistentes |
| `dotnet test .\tests\OneBrain.Safety.Tests\OneBrain.Safety.Tests.csproj --no-build --filter "TestCategory=M1161M1172"` | PASS, 17 tests |
| `dotnet test .\tests\OneBrain.Safety.Tests\OneBrain.Safety.Tests.csproj --no-build --filter "TestCategory=NativeBrowserSkillsProduct"` | PASS, 6 tests |
| `git diff --check 105d0855fbf71b028552c79d29dde32d6ee4f5f4..HEAD` | FAIL, trailing whitespace en docs nuevos |
| Secret scan simple sobre archivos cambiados | PASS, no valores obvios encontrados |
| BrowserAct dependency scan | PASS para dependencia runtime; sólo referencias textuales |
| Bad UX wording scan en sidepanel | Encontró `planned` como estado interno/timeline, no como nueva cara principal |

## 7. Estado final

| Item | Estado |
|---|---|
| Mission Control | PASS por harness |
| Browser Skills | PASS por harness |
| Harness instalado | PASS |
| Worktree | limpio |
| Branch vs origin | alineado |
| Riesgo principal | cambios stealth/proxy/CAPTCHA ya committeados |
| Próximo paso seguro | decisión explícita antes de Workspace |

## 8. Recomendación para próximo bloque

No avanzar todavía a Workspace Open mientras estos commits estén sin decisión.

Bloque recomendado:

`M1346-M1357 — Remove/Reconcile Stealth Line + Preserve Useful Bridge Fixes`

Objetivo:

1. Revertir o archivar la línea stealth/proxy/CAPTCHA solver.
2. Extraer sólo fixes técnicos útiles del Bridge.
3. Mantener Mission Control, Browser Skills y harness pasando.
4. Dejar el repo listo para `Workspace Open real + Project Understanding visible read-only`.
