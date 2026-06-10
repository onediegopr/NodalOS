# HITO-031+032+033 — Action Discovery + Risk Policy + Safe Navigation Planning

## 1. Objetivo
Preparar a ONE BRAIN para razonar sobre elementos accionables en páginas comerciales reales SIN ejecutar clicks.

## 2. Step kinds

### `discover.actionableelements`
Input: texto UIA (de `btext` variable). Descubre patrones conocidos y los clasifica en 5 categorías.

### `plan.safenavigation`
Input: items JSON de discover. Genera plan de navegación segura: bloquea payment/dangerous/auth, marca nav como requiresApproval, permite safe-readonly.

## 3. Categorías y severidad

| Categoría | Severidad |
|---|---|
| payment (tarjeta, cuotas, Mercado Pago) | payment (highest) |
| dangerous (comprar, carrito, checkout) | dangerous |
| auth (iniciar sesión, registrarse) | auth |
| nav (ver más, descripción, opiniones) | nav |
| safe (categorías, ofertas, ayuda) | safe |

## 4. Reglas de bloqueo

- **Siempre bloqueado**: payment, dangerous, auth
- **Requiere aprobación**: navigation
- **Permitido read-only**: safe
- **Unknown**: revisión humana requerida

## 5. `navPlan.hasExecutableActions` = false por diseño en este hito

## 6. Limitaciones
- Discovery es puramente textual (regex/dictionary). No usa DOM/UIA semántico.
- ML real puede devolver 0 items si el texto UIA no expone patrones.
- Receta sintética positiva (`action-policy-plan-synthetic-positive.json`) demuestra integración con texto conocido.
- No se ejecuta ningún click/invoke/type en este hito.
