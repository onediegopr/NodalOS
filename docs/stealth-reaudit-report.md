# NODAL OS — Re-auditoría de Correcciones de Sigilo

> **Fecha:** 2026-06-25  
> **Tipo:** Verificación post-corrección (8 correcciones aplicadas) + re-evaluación de módulos + priorización de problemas restantes

---

## PARTE 1 — VERIFICACIÓN DE LAS 8 CORRECCIONES

### CORRECCIÓN 1 — Tipo de CAPTCHA dinámico: ✅ VERIFICADA

| Check | Resultado | Evidencia |
|-------|-----------|-----------|
| `session._lastCaptchaType` asignado en detección | ✅ | `index.js:184` — `session._lastCaptchaType = captcha.type;` antes de `sendFrictionSignal` |
| Solver usa el tipo real | ✅ | `index.js:240` — `const captchaType = session._lastCaptchaType \|\| 'recaptcha_v2';` |
| Inyector usa el tipo real | ✅ | `index.js:243` — `TokenInjector.inject(session.page, captchaType, result.token)` |
| Solver mapea `hcaptcha→HCaptchaTaskProxyless` | ✅ | `CaptchaSolver.js:51-53` — `case 'hcaptcha': taskType = 'HCaptchaTaskProxyless'` |
| Solver mapea `cloudflare→TurnstileTaskProxyless` | ✅ | `CaptchaSolver.js:55-57` — `case 'cloudflare': taskType = 'TurnstileTaskProxyless'` |
| Fallback si `_lastCaptchaType` es undefined | ✅ | `'recaptcha_v2'` como default |
| ⚠️ **Punto ciego** | ⚠️ | Si el bridge decide `SolveAndRetry` para una fricción que NO es CAPTCHA (ej: `BotBlockDetected`), `_lastCaptchaType` podría ser undefined siempre porque nunca se detectó un CAPTCHA. El default `'recaptcha_v2'` haría que el solver intente resolver un bloqueo como si fuera CAPTCHA. **Severidad: Baja** (el policy engine no debería enviar `SolveAndRetry` para no-CAPTCHA) |

### CORRECCIÓN 2 — RecoveryStrategy taskId: ✅ VERIFICADA

| Check | Resultado | Evidencia |
|-------|-----------|-----------|
| taskId original mantenido | ✅ | `RecoveryStrategy.js:54` — `taskId: taskId` (no `crypto.randomUUID()`) |
| Nueva sesión usa taskId original | ✅ | Mismo parámetro en todas las comunicaciones con bridge |
| `sessions.set(taskId, recovery.session)` correcto | ✅ | `index.js:257` — reasocia la sesión recuperada al mismo taskId |

### CORRECCIÓN 3 — Fonts falseadas: ✅ VERIFICADA

| Check | Resultado | Evidencia |
|-------|-----------|-----------|
| Script de inyección incluye parche de fuentes | ✅ | `FingerprintInjector.js:251-272` — bloque completo |
| Usa `profile.fonts` | ✅ | `var fontList = p.fonts` |
| `document.fonts.check` falseado | ✅ | `FingerprintInjector.js:255-261` — devuelve `true` si la fuente está en `fontList` |
| `fontFaceSet.forEach` falseado | ✅ | `FingerprintInjector.js:263-270` — itera `fontList` y llama al callback por cada fuente |
| `fontFaceSet.entries`, `fontFaceSet.keys`, `fontFaceSet.has` | ⚠️ **No falseados** | Solo se cubren `check` y `forEach`. `entries()`, `keys()`, `has()` y `size` del FontFaceSet no están falseados |
| Fuentes renderizadas vs reportadas | ⚠️ **Riesgo** | Un anti-bot renderiza texto con `ctx.fillText('test', 0, y)` en un canvas y mide el ancho. Si una fuente reportada no está realmente instalada, el ancho medido será diferente al esperado. Este es un vector de detección avanzado |

### CORRECCIÓN 4 — `dist` en `_ballisticMove`: ✅ VERIFICADA

| Check | Resultado | Evidencia |
|-------|-----------|-----------|
| `dist` calculada antes de usarse | ✅ | `HumanMouse.js:23` — `var dist = Math.sqrt((tx - sx) ** 2 + (ty - sy) ** 2);` |
| Jitter escalado funcional | ✅ | `HumanMouse.js:33` — `var noiseScale = dist > 500 ? 1.5 : 1;` |
| `_ballisticMove` llamado para largas distancias | ✅ | `HumanMouse.js:12-13` — `if (dist > threshold) await this._ballisticMove(page, sx, sy, tx, ty, opts);` |

### CORRECCIÓN 5 — 20+ propiedades falseadas: ✅ VERIFICADA

| Propiedad | Valor | ¿Realista? |
|-----------|-------|-----------|
| `productSub` | `'20030107'` | ✅ Es el valor real de Chrome |
| `appCodeName` | `'Mozilla'` | ✅ Valor real |
| `appName` | `'Netscape'` | ✅ Valor real |
| `doNotTrack` | `null` | ✅ Valor por defecto en Chrome |
| `cookieEnabled` | `true` | ✅ Típico |
| `onLine` | `true` | ⚠️ Constante `true` — detectable si el navegador está realmente offline |
| `connection` | `{ effectiveType:'4g', rtt:50, downlink:10 }` | ✅ Valores realistas |
| `getBattery` | `{ level:1, charging:true }` | ⚠️ Siempre 100% cargando — detectado como irreal si se consulta repetidamente |
| `bluetooth` | `undefined` | ✅ Chrome normal no expone sin permiso |
| `usb` | `undefined` | ✅ |
| `serial` | `undefined` | ✅ |
| `getGamepads` | `[null,null,null,null]` | ✅ Formato real |
| `enumerateDevices` | Lista filtrada de dispositivos reales | ✅ No crea dispositivos falsos, solo limpia IDs |
| ⚠️ `onLine` siempre `true` | ⚠️ | Detección: si el proxy está caído y la página está realmente inaccesible, `onLine=true` es contradictorio |

### CORRECCIÓN 6 — `data-callback`: ✅ VERIFICADA

| Check | Resultado | Evidencia |
|-------|-----------|-----------|
| Lee `data-callback` del widget | ✅ | `TokenInjector.js:15-20` |
| Llama a `window[cbName](token)` | ✅ | `window[cbName](t)` con verificación `typeof === 'function'` |
| Callback inexistente manejado | ✅ | `if (cbName && window[cbName] && typeof window[cbName] === 'function')` — triple check |
| `data-expired-callback` neutralizado | ✅ | `window[expCb] = function() {}` — previene que se dispare |
| ⚠️ `data-callback` en reCAPTCHA Enterprise | ⚠️ | Enterprise puede usar `data-action` en vez de `data-callback`, y disparar `grecaptcha.enterprise.execute()` en vez de callbacks DOM |

### CORRECCIÓN 7 — URL sanitizada: ✅ VERIFICADA

| Check | Resultado | Evidencia |
|-------|-----------|-----------|
| URL reducida a `origin + pathname` | ✅ | `CaptchaSolver.js:46` — `safeUrl = u.origin + u.pathname` |
| Query parameters eliminados | ✅ | `pathname` no incluye query string |
| Fragment eliminado | ⚠️ **No** | `pathname` no incluye `hash` por construcción de URL, pero si la URL tiene `#fragment`, `pathname` lo excluye. Correcto. |
| Headers/Cookies no enviados | ✅ | 2captcha solo recibe `websiteURL` y `websiteKey` en el body |

### CORRECCIÓN 8 — Coherencia proxy-perfil: ✅ VERIFICADA

| Check | Resultado | Evidencia |
|-------|-----------|-----------|
| `ensureCoherence` llamado tras `acquire` | ✅ | `index.js:146-148` — `FingerprintProfile.ensureCoherence(fingerprintProfile, proxy.country)` |
| `ensureCoherence` mapea timezone/locale/idioma/geoloc | ✅ | `FingerprintProfile.js` — `COUNTRY_MAPPING` con 10 países |
| Proxy sin `country` manejado | ✅ | `if (proxy && proxy.country)` — se salta si no hay país |
| ⚠️ `ensureCoherence` retorna un nuevo objeto | ⚠️ **Bug** | `FingerprintProfile.ensureCoherence` retorna un **nuevo** objeto pero `index.js:147` no captura el retorno: `FingerprintProfile.ensureCoherence(fingerprintProfile, proxy.country);` — el resultado se descarta. El perfil original NO se modifica. **La coherencia NO se aplica realmente.** |

---

## PARTE 2 — RE-EVALUACIÓN DE MÓDULOS

### MÓDULO 1: CaptchaSolver + TokenInjector + VisualCaptchaSolver

**Estado general:** Mejorado significativamente. El flujo ahora es tipo-dinámico.

| Item | Estado | Detalle |
|------|--------|---------|
| Flujo reCAPTCHA v2 | ✅ Completo | detectar → solve → inject → callback → submit |
| Flujo hCaptcha | ✅ Completo | detectar → solve (HCaptchaTask) → inject (setResponse) |
| Flujo Turnstile | ✅ Completo | detectar → solve (TurnstileTask) → inject → callback |
| VisualCaptchaSolver como fallback | ✅ Integrado | Se activa si 2captcha falla o no hay sitekey |
| Timeout 2captcha | ✅ | `maxWait = 120000` con polling cada 3s |
| Timeout HTTP individual | ❌ **No** | Las llamadas `fetch` a 2captcha no tienen `AbortController` ni timeout. Si 2captcha cuelga, el thread se bloquea |
| GeeTest/FunCaptcha/Kasada | ❌ **No cubiertos** | El detector no los reconoce, el solver no tiene task types para ellos |
| `proxy` en 2captcha | ❌ **No** | No se envía información de proxy a 2captcha. Si el CAPTCHA requiere IP del proxy, usar `*Task` (no `*Proxyless`) con datos del proxy |

### MÓDULO 2: FingerprintInjector + FingerprintProfile

**Estado general:** Mejorado sustancialmente. De ~8 propiedades a ~28.

| Item | Estado | Detalle |
|------|--------|---------|
| Cobertura de propiedades | ✅ 28/35 | Se agregaron 20+ propiedades. Quedan sin falsear: `oscpu`, `buildID`, `appVersion`, `vendorSub` (ya falseado), `product` |
| Coherencia entre valores | ✅ | `hardwareConcurrency` y `deviceMemory` son coherentes con el perfil |
| Fonts | ✅ Falseado | `document.fonts.check` y `forEach` |
| WebGL readPixels | ❌ **No falseado** | El script falsea `getImageData` 2D pero `WebGLRenderingContext.readPixels` no tiene ruido |
| AudioBuffer.getChannelData | ❌ **No falseado** | Solo se falsean `createOscillator.connect` y `AnalyserNode.getByteFrequencyData`. `getFloatTimeDomainData` y `getChannelData` quedan expuestos |
| Orden de parches | ✅ Correcto | Navigator → Screen → WebGL → Canvas → Audio → WebRTC → Permissions → Chrome → Fonts → Cleanup → CDP |
| `navigator.permissions.query` para `midi`, `camera`, etc. | ❌ **Solo notifications** | El falseo de permisos solo maneja `name === 'notifications'`. Otros permisos devuelven el resultado real |

### MÓDULO 3: ProxyManager + RecoveryStrategy

**Estado general:** Funcional pero con el bug de coherencia (ver C8 arriba).

| Item | Estado | Detalle |
|------|--------|---------|
| Coherencia automática | ⚠️ **Bug** | `ensureCoherence` se llama pero su retorno se descarta |
| Recovery mantiene tracking | ✅ | taskId original conservado |
| DomainBlacklist usado | ✅ | `index.js:193` — registra dominios bloqueados |
| Akamai/Imperva/Kasada en BlockDetector | ❌ **No cubiertos** | Mismos patrones faltantes de la auditoría anterior |
| Health check a httpbin.org | ⚠️ **Fuga** | Sigue usando servicio externo para health check |

### MÓDULO 4: HumanMouse + HumanKeyboard + AdaptiveBehaviorEngine

**Estado general:** `dist` corregido. Movimientos funcionales.

| Item | Estado | Detalle |
|------|--------|---------|
| Ballistic move funcional | ✅ | `dist` calculada, jitter escalado |
| Bézier siempre grado 3 | ⚠️ **Sigue** | Detectable por behavioral ML |
| Distribución de errores de tecleo | ⚠️ **Solo sustitución** | Sin transposición, omisión, inserción |
| Varianza entre sesiones | ✅ | `AdaptiveBehaviorEngine` genera parámetros únicos |
| Pausa pre-scroll | ❌ **No** | Sin hover antes de scroll |

---

## PARTE 3 — PROBLEMAS RESTANTES (17 de la auditoría anterior)

De los 17 problemas no-críticos de la primera auditoría:

| # | Problema | ¿Resuelto? | ¿Sigue relevante? |
|---|----------|-----------|-------------------|
| 1 | GeeTest no cubierto | ❌ No | **Sí** — selector faltante |
| 2 | FunCaptcha no cubierto | ❌ No | **Sí** — selector faltante |
| 3 | Kasada no cubierto | ❌ No | **Sí** — selector faltante |
| 4 | Imperva no cubierto | ❌ No | **Sí** — selector faltante |
| 5 | Akamai Bot Manager no cubierto | ❌ No | **Sí** — selector faltante |
| 6 | reCAPTCHA v3 Enterprise no distinguido | ❌ No | **Sí** — enterprise.js?render= |
| 7 | Clasificación por píxeles frágil | ❌ No | **Sí** — sigue siendo frágil |
| 8 | Logging de tokens | ❌ No | **Baja** — solo en logs locales |
| 9 | WebGL readPixels sin ruido | ❌ No | **Sí** |
| 10 | AudioBuffer.getChannelData | ❌ No | **Sí** |
| 11 | Bézier siempre grado 3 | ❌ No | **Sí** |
| 12 | Distribución de errores poco realista | ❌ No | **Sí** |
| 13 | Sin keepalive runner-bridge | ❌ No | **Sí** |
| 14 | Sin limpieza ante error de solver | ❌ No | **Baja** |
| 15 | `vendor: 'Google Inc.'` constante | ❌ No | **Baja** — difícil de explotar |
| 16 | `onLine: true` constante | ❌ No | **Media** |
| 17 | `navigator.getBattery` siempre 100% | ❌ No | **Media** |

### TOP 5 PRIORIZADOS para próxima iteración:

| # | Prioridad | Problema | Impacto |
|---|-----------|----------|---------|
| 1 | **Alta** | `ensureCoherence` retorno descartado (C8 bug) | La coherencia proxy-perfil NO funciona |
| 2 | **Alta** | `getBattery` siempre 100% + `onLine` siempre true | Dos constantes detectables |
| 3 | **Media** | 5 proveedores CAPTCHA sin detectores | No se detectan = no se reportan |
| 4 | **Media** | WebGL readPixels + AudioBuffer.getChannelData sin ruido | Vectores de fingerprinting expuestos |
| 5 | **Media** | Sin keepalive en el WebSocket runner-bridge | Conexiones idle pueden ser cerradas |

---

## VEREDICTO FINAL

**El sistema está listo para producción en entornos controlados con las siguientes condiciones:**

- Usar solo contra sitios que no empleen Akamai, Kasada, Imperva o GeeTest como protección principal.
- Asegurar que el proxy tenga el campo `country` correctamente configurado **y** que el bug de `ensureCoherence` (retorno descartado) se corrija antes de producción.
- No confiar en el VisualCaptchaSolver para entornos productivos sin validación manual adicional.
- Mantener el handoff humano como fallback para cualquier CAPTCHA no resuelto automáticamente.

**El bug de `ensureCoherence` (C8) debe corregirse inmediatamente** — es la única corrección de las 8 que no está funcionando debido al retorno descartado.
