# Auditoría Cruzada — NODAL OS Stealth Engine v0.4.0

## Blindaje ✅
- `.codex-guard.json`, `.ai-guard.json` en todos los directorios, `.gitattributes` y headers `@NODAL_OS_RESTRICTED` verificados intactos en 22 archivos.

---

## Problemas Encontrados

### CRÍTICO

#### C1: Mutación global de `chromium` por plugin de stealth
**Archivo:** `src/StealthSession.js:10-19, 56-62`
**Descripción:** El código hace `import('playwright-extra')` y `import('puppeteer-extra-plugin-stealth')` con top-level `await` a nivel de módulo. El `chromium` resultante se asigna a nivel de módulo (variable de módulo, no de instancia). Luego `chromium.use(stealthPlugin)` se ejecuta una sola vez cuando se carga el módulo. Esto significa:
1. Si `playwright-extra` está instalado, AFECTA a TODAS las sesiones subsiguientes, sin importar la configuración individual.
2. Si dos sesiones tienen `tlsFingerprint.enabled` diferente, la segunda sesión heredará el plugin global ya aplicado.
3. Top-level `await` en el módulo bloquea la carga del resto del sistema si hay delay de red.

**Sugerencia:** Aplicar el plugin por INSTANCIA, no a nivel de módulo. Alternativamente, usar un wrapper por sesión que haga `chromium.launch()` con el plugin ya configurado localmente.

---

#### C2: `predictiveNewProxy` no usa el flujo correcto de `ProxyManager.acquire`
**Archivo:** `src/antiBlocking/RecoveryStrategy.js:61-70`
**Descripción:** Cuando se usa `predictiveNewProxy`, el código hace:
```javascript
this.proxyManager.lock.set(taskId, proxyId);
const p = this.proxyManager.pool.find(x => x.id === proxyId);
if (p) { p.status = 'in_use'; p.assignedTo = taskId; p.usageCount++; }
```
Esto manualmente marca el proxy en el pool, pero NO actualiza `p.url`, `p.username`, `p.password`, `p.type`, `p.country`, `p.provider`. Cuando luego se construye el objeto `proxy` para la sesión con `proxy.server || proxy.url`, el `proxy.url` NUNCA fue seteado en el pool entry (solo existe en el objeto retornado por `selectNewProxy`). El pool entry solo tiene `id`, `url` (desde el constructor original del pool), pero `p` referenciado de `pool.find()` sí tiene `url` desde la construcción. Sin embargo, el `proxyId` del pool entry no se actualizó... espera, sí se actualiza `p.usageCount` pero el entry del pool en `ProxyManager.pool` fue construido originalmente con los campos correctos (`url`, `username`, etc.). El bug es más sutil: cuando `ProxyReputationEngine.recordFailure(session.proxyId, ...)` es llamado después, usa `session.proxyId` (UUID). El `lock.set(taskId, proxyId)`asocia el taskId al UUID del proxy. Esto parece correcto.

PERO hay otro problema: cuando `proxyManager.acquire()` se llama normalmente (línea 68), actualiza `this.lock.set(taskId, p.id)`. Cuando se usa `predictiveNewProxy` (línea 64), también se hace `this.lock.set(taskId, proxyId)`. Ambos usan el mismo `taskId`. Si una sesión pasa por rotación predictiva Y luego por retry normal, el segundo `lock.set` sobreescribe el primero. El `release()` en `destroySession` solo libera si `lock.get(taskId)` devuelve el UUID. Esto debería funcionar... pero hay un caso edge: si `predictiveNewProxy` se usa pero `ProxyRotatorHelper.isOnCooldown()` filtra ese proxy (línea 30 en PredictiveRotator), el proxy seleccionado podría no estar en cooldown según el helper estático pero SÍ según `ProxyManager.isOnCooldown()`. El helper estático tiene lógica duplicada de `ProxyManager.isOnCooldown()`. Si hay discrepancia, el proxy podría ser marcado como disponible en el pool pero el manager lo considera en cooldown.

**Sugerencia:** Reutilizar `ProxyManager.rotate()` para el caso predictivo, o al menos usar `ProxyRotatorHelper` que delegate al `ProxyManager` real.

---

#### C3: Kasada solver envía challenge vacío
**Archivo:** `src/captcha/CaptchaSolver.js:150-152`
**Descripción:**
```javascript
case 'kasada':
  taskType = 'AntiKasadaTaskProxyLess';
  taskConfig = { type: taskType, websiteURL: safeUrl };
  break;
```
CapSolver requiere información del challenge de Kasada (script tags, `window.kpsdkConfig`, etc.) para resolver el desafío JavaScript. Enviar solo la URL sin challenge produce error `Invalid task configuration` de CapSolver.

**Sugerencia:** Extraer el challenge de Kasada ejecutando JavaScript en la página antes de enviar a CapSolver, o implementar un page-level solver que capture el challenge completo.

---

#### C4: GeeTest envía challenge vacío
**Archivo:** `src/captcha/CaptchaSolver.js:143-144`
**Descripción:**
```javascript
case 'geetest':
  taskType = 'GeeTestTaskProxyLess';
  taskConfig = { type: taskType, websiteURL: safeUrl, gt: sitekey || '', challenge: '' };
  break;
```
GeeTest requiere tanto `gt` (static ID) como `challenge` (dinámico por cada intento). El challenge se genera en el callback del challenge, no es un campo estático. Enviar `challenge: ''` causará fallo en CapSolver.

**Sugerencia:** Extraer el challenge de GeeTest desde la URL del iframe o del callback de la librería GeeTest del sitio, o usar `GeeTestTask` (con proxy) en lugar de `GeeTestTaskProxyLess`.

---

#### C5: `domainProfile` instanciado dos veces
**Archivos:** `src/index.js:51` (`initProxies`) y `src/index.js:66` (`initAntiBlocking`)
**Descripción:**
```javascript
// initProxies()
domainProfile = new DomainProfile(CONFIG.learning?.domainProfile || {});

// initAntiBlocking()
domainProfile = new DomainProfile(CONFIG.learning?.domainProfile || {});
```
`initProxies()` se llama antes de `initAntiBlocking()`. La segunda instanciación crea una NUEVA instancia con `loaded = false`, perdiendo cualquier dato ya cargado y cualquier perfil guardado del primer uso. Esto causa:
1. Cada inicio de tarea llama a `domainProfile?.load()` que puede re-leer del archivo múltiples veces.
2. Las actualizaciones en `update()` van a instancias distintas.

**Sugerencia:** Crear `domainProfile` una sola vez en un lugar centralizado, antes de `initProxies()`.

---

#### C6: `domainRateLimiter.recordResponse` nunca se llama
**Archivos:** `src/proxy/DomainRateLimiter.js:31-45` y toda la cadena de `index.js`
**Descripción:** `DomainRateLimiter` tiene lógica adaptativa (`backoffMultiplier` en 429, `recoveryFactor` en éxito) pero NINGÚN código en `index.js` llama a `recordResponse()`. El rate limiter solo hace `wait()` basado en delays fijos. Si un sitio devuelve 429 repetidamente, el delay nunca aumenta.

**Sugerencia:** En `handleToolRequest`, después de ejecutar `navigate`, obtener el status code de la respuesta HTTP y llamar a `domainRateLimiter.recordResponse(domain, statusCode)`.

---

### ALTO

#### A1: `failureLog` crece sin límite
**Archivo:** `src/antiBlocking/RecoveryStrategy.js:18, 28-31`
**Descripción:**
```javascript
this.failureLog = [];
// ...
this.failureLog.push({ taskId, domain, proxy: session.proxy?.server || 'none', reason: decision.Message, timestamp: new Date().toISOString() });
```
Cada recovery push sin cleanup. En producción con muchas tareas fallidas, esto consume memoria indefinidamente.

**Sugerencia:** Limitar `failureLog` a los últimos 1000 entries con circular buffer.

---

#### A2: Lógica inversa de `excludeTypes` en DomainLearning
**Archivo:** `src/index.js:165`
**Descripción:**
```javascript
if (learned?.proxyType) acquireOpts.excludeTypes = [learned.proxyType === 'residential' ? 'datacenter' : 'residential'];
```
Si `preferredProxyType = 'residential'`, se excluye `datacenter`. Esto está correcto en intención ("preferir residential"). PERO: si solo hay proxies datacenter disponibles, `acquire()` devuelve `null` porque todos fueron excluidos. El aprendizaje marca `preferredProxyType` de un sitio que funcionó, pero si el pool actual no tiene ese tipo, la tarea falla completamente en lugar de usar el mejor disponible.

**Sugerencia:** No usar `excludeTypes`; usar en cambio `acquireOpts.preferTypes = [learned.proxyType]` y modificar `acquire()` para ordenar por preferencia sin excluir.

---

#### A3: Módulo global `chromium` causa side-effects entre sesiones
**Archivo:** `src/StealthSession.js:10-19`
**Descripción:** El `chromium` de módulo es compartido por todas las instancias de `StealthSession`. Una vez que `chromium.use(stealthPlugin)` se ejecuta (si está instalado), el plugin queda activo permanentemente para todas las sesiones futuras. Esto significa que si una sesión con `tlsFingerprint.enabled = false` se crea después de una con `enabled = true`, la segunda仍将使用已激活的stealth plugin。

**Sugerencia:** No mutar `chromium` global. Crear una función wrapper que cada `initialize()` llame para obtener un chromium configurado localmente.

---

#### A4: Shutdown no limpia `domainRateLimiter`, `proxyReputationEngine`, ni `predictiveRotator`
**Archivo:** `src/index.js:354-375`
**Descripción:** `shutdown()` solo llama `proxyHealthChecker.stop()` y dispose de sesiones. Los nuevos módulos (`domainRateLimiter`, `proxyReputationEngine`, `predictiveRotator`) no tienen cleanup.

**Sugerencia:** Agregar cleanup en `shutdown()` para todos los recursos activos.

---

#### A5: `ProxyRotatorHelper.isOnCooldown` duplica lógica
**Archivo:** `src/proxy/PredictiveRotator.js:56-60` y `src/proxy/ProxyManager.js:128-135`
**Descripción:** `ProxyRotatorHelper.isOnCooldown()` reimplementa la misma lógica que `ProxyManager.isOnCooldown()`. Si el estado del cooldown cambia (por ejemplo, expira entre que el helper lo verifica y `ProxyManager` lo usa), pueden haber inconsistencias.

**Sugerencia:** Eliminar `ProxyRotatorHelper` y usar `proxyManager.isOnCooldown(p)` directamente en `PredictiveRotator.selectNewProxy()`.

---

### MEDIO

#### M1: `playwright-extra` y `puppeteer-extra-plugin-stealth` no verificados funcionalmente
**Archivo:** `src/StealthSession.js:10-19`
**Descripción:** La integración hace import dinámico y usa `chromium.use(stealthPlugin)`. Hay varios problemas potenciales:
1. `playwright-extra` API puede no ser compatible con la versión de `playwright` instalada.
2. `puppeteer-extra-plugin-stealth` fue diseñado para Puppeteer, no Playwright. La API de `playwright-extra` puede diferir.
3. No hay manejo si `StealthPlugin()` devuelve `null` o lanza.

**Sugerencia:** Verificar manualmente que la integración funciona con la versión exacta de `playwright` instalada (`^1.52.0`).

---

#### M2: `domainProfile.save()` sin batching
**Archivo:** `src/learning/DomainProfile.js:37-48, 102`
**Descripción:** Cada `update()` hace `this.save().catch(() => {})`. En cargas altas con muchas actualizaciones de dominio, esto puede causar:
1. Concurrencia de escrituras al archivo JSON.
2. I/O disk saturado.

**Sugerencia:** Implementar debounce (ej. guardar max cada 5 segundos si hay cambios pendientes) o usar un write queue.

---

#### M3: GeeTest sitekey extraction incorrecta
**Archivo:** `src/captcha/CaptchaDetector.js:143-144, extractSitekey`
**Descripción:** `extractSitekey()` busca `[data-sitekey]` y scripts recaptcha. GeeTest usa `gt` como ID público, típicamente en `div.geetest-box` o como propiedad del objeto `initGeetest`. El detector actual devuelve `sitekey: null` para GeeTest, y el solver recibe `gt: sitekey || ''` = `gt: ''`. CapSolver necesita el `gt` real.

**Sugerencia:** Agregar extracción específica de GeeTest: buscar `window.geetest` o el elemento con ID del challenge.

---

#### M4: `Kasada` detector usa selector con wildcard `#kpsdk-*`
**Archivo:** `src/captcha/CaptchaDetector.js:25`
**Descripción:** El selector `#kpsdk-*` con `*` no es CSS válido. `page.$('#kpsdk-*')` podría no encontrar elementos cuyo `id` sea `kpsdk-xxxx`. Los IDs de Kasada típicamente siguen el patrón `kpsdk-[0-9a-f]+`.

**Sugerencia:** Usar `[id^='kpsdk-']` (attribute starts-with selector) o detectar via script `document.querySelectorAll('[id^="kpsdk-"]')`.

---

#### M5: Métricas de `PredictiveRotator` nunca se reinician
**Archivo:** `src/proxy/PredictiveRotator.js:10-14, 47-52`
**Descripción:** `checks`, `rotationsTriggered`, `successfulPredictions` crecen indefinidamente. En un sistema que opera por días/semanas, estos números pueden ser enormes.

**Sugerencia:** Agregar método `resetMetrics()` o limitar a ventana de tiempo.

---

#### M6: No hay validación de `capSolverApiKey` ni `twoCaptchaApiKey`
**Archivos:** `src/captcha/CaptchaSolver.js:8-9, 18, 29`
**Descripción:** Las API keys se usan directamente en requests sin validación de formato. Una key vacía o malformada producirá errores de API poco claros.

**Sugerencia:** Validar que las keys no estén vacías y tengan longitud mínima razonable antes de intentar chamadas.

---

#### M7: Dependencias opcionales no verificadas
**Archivo:** `package.json:21-24`
**Descripción:**
```json
"optionalDependencies": {
  "playwright-extra": "^4.3.6",
  "puppeteer-extra-plugin-stealth": "^2.11.2",
  "cycletls": "^1.0.25"
}
```
No hay verificación en runtime de que estas estén disponibles antes de usarlas. El `try/catch` en `StealthSession` solo captura errores del import, pero no verifica que funcionen correctamente.

---

### BAJO

#### B1: `shutdown` no cierra el WebSocket principal del bridge
**Archivo:** `src/index.js:366`
**Descripción:** `shutdown()` llama `ws.close()` pero no espera a que la conexión se cierre efectivamente antes de `process.exit()`. Podría cerrar prematuramente.

**Sugerencia:** Usar `await new Promise(resolve => ws.on('close', resolve))` después de `ws.close()`.

---

#### B2: `--disable-infobars` presente cuando debía ser removido
**Archivo:** `src/StealthSession.js:106`
**Descripción:** Este flag fue marcado como eliminado en auditorías anteriores pero sigue presente en `buildLaunchArgs()`.

**Sugerencia:** Remover `--disable-infobars`.

---

#### B3: IPRoyal parsing frágil
**Archivo:** `src/proxy/ProxyProviderIntegrations.js:82-92`
**Descripción:** El parsing de `line.split(':')` asume exactamente 5 campos. Si el formato del archivo exportado de IPRoyal cambia o tiene campos vacíos, el parsing falla silenciosamente y el proxy se filtra por `.filter(p => p.url.includes('@'))`. Campos con `:` en la password causarían parsing incorrecto.

**Sugerencia:** Usar regex más robusto odocumentar el formato esperado.

---

## Seguridad de Datos

| Aspecto | Estado | Detalle |
|---|---|---|
| Fuga de IP real en 2captcha | ✅ Arreglado | `fetchOpts` con proxy unificado en createTask y getTaskResult |
| Token de handoff | ✅ Arreglado | Validación en cada mensaje |
| Screenshots a OpenAI | ✅ Arreglado | Consent flag + blur(1) |
| Logs con datos sensibles | ⚠️ Parcial | `instruction.substring(0, 80)` en `index.js:146` puede contener URLs o credenciales |
| Fallback de proxy sin API key | ✅ OK | Retorna `[]` vacío |
| API keys en logs | ⚠️ Presente | Errores de 2captcha/CapSolver incluyen descriptions sin sanitizar |

---

## Robustez

| Aspecto | Estado | Detalle |
|---|---|---|
| Memory leak `failureLog` | ❌ Persiste | Crece sin límite |
| Memory leak `DomainRateLimiter` | ✅ LRU | TTL + no limit pero se reinicializa en restart |
| Memory leak `ProxyReputationEngine` | ✅ TTL+LRU | Limitado a 2000 entries con cleanup |
| Memory leak `DomainProfile` | ✅ LRU | Limitado a 500 con cleanup |
| Condiciones de carrera | ⚠️ Posibles | predictiveNewProxy + lock race (ver C2) |
| Shutdown graceful | ⚠️ Incompleto | Faltan cleanup de nuevos módulos |
| Top-level await en módulo | ⚠️ Bloqueante | Puede bloquear carga del engine |

---

## Interacciones entre Módulos

### Cadena de CAPTCHA
`handleToolRequest` → `CaptchaDetector.detect()` → `sendFrictionSignal()` → `handleFrictionDecision` → `CaptchaSolver.solve()` → `TokenInjector.inject()` → `CaptchaDetector.detect()` ✅ Funciona, pero Kasada/GeeTest fallan por challenge vacío.

### Cadena de Recovery
`handleFrictionDecision(RotateAndRetry)` → `proxyReputationEngine.recordFailure()` → `predictiveRotator.shouldRotate()` → `predictiveRotator.selectNewProxy()` → `RecoveryStrategy.recover()` ✅ Flujo correcto pero `ProxyRotatorHelper` duplica validación de cooldown.

### Cadena de Domain Learning
`handleTask` → `domainProfile.load()` → `domainProfile.suggest()` → `proxyManager.acquire()` ✅ Pero `domainProfile` se reinstancia en `initAntiBlocking` (C5).

### Cadena de Rate Limiting
`handleTask` → `domainRateLimiter.wait()` ✅ wait() se llama, pero `recordResponse()` nunca (M6).

---

## Veredicto Final

**¿Está listo para producción?** **NO — 4.5/10**

| Área | Nota |
|---|---|
| Correcciones Fase 5.5 | 6/10 — proxyId UUID✅ pero predictiveNewProxy tiene race conditions |
| Fase 6 (TLS/JA3) | 3/10 — Plugin global causa side-effects, no maneja ausencia del paquete |
| Fase 7 (Rotación predictiva) | 5/10 — Lógica correcta pero helper duplicado y recordResponse nunca llamado |
| Fase 8 (CAPTCHAs) | 4/10 — Detección correcta, solvers de GeeTest/Kasada incompletos |
| Fase 9 (Aprendizaje) | 5/10 — Funciona pero domainProfile instanciado dos veces |
| Seguridad de datos | 7/10 — Mejoras de Fase 5.5 aplicadas, logs siguen exponiendo datos |
| Robustez | 5/10 — failureLog sin límite, shutdown incompleto |

### Top 5 Prioritarios

1. **[C3/C4] Fix GeeTest/Kasada solvers** — Ambos envían challenge vacío. Sin esto, los CAPTCHAs de estos tipos siempre fallan.
2. **[C1] Global chromium mutation** — `chromium.use()` global afecta todas las sesiones. Necesita isolate por instancia.
3. **[C5] domainProfile doble instanciación** — Pierde datos cargados, duplica I/O.
4. **[A1] failureLog sin límite** — Crash por OOM en producción con alta carga de failures.
5. **[A2] excludeTypes demasiado agresivo** — Tareas fallan completamente si el pool no tiene el tipo preferido.

### Condiciones para uso en entornos controlados
- Deshabilitar `tlsFingerprint.enabled` hasta que se corrija C1.
- Deshabilitar GeeTest/Kasada hasta que se corrijan C3/C4.
- Monitorear `failureLog` size.
- Asegurar que el pool de proxies siempre contenga al menos un proxy del tipo preferido por DomainLearning.
