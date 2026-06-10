# HITO-030 — Structured Visible Field Extraction

## 1. Objetivo
Extraer campos candidatos desde texto UIA visible sin DOM semántico, usando heurísticas de regex. Read-only, sin clicks ni login.

## 2. Step kind
`extract.visiblefields` con `mode: commercialProduct`. Guarda variables bajo el prefijo `saveAs`.

## 3. Campos extraídos
- `titleCandidate` — del browser.title
- `priceCandidate` — regex `\$[\s]*[\d.]+[,]?[\d]*`. null si no detectado
- `currencyCandidate` — ARS si hay `$`, sino null
- `shippingCandidate` — detected/null
- `sensitiveWordsDetected` — comprar/carrito/pagar/login
- `confidence` — high/medium/low
- `rawEvidence` — texto truncado

## 4. Resultados en ML product detail real
- titleCandidate: detectado
- priceCandidate: null
- currencyCandidate: null
- shippingCandidate: detected
- sensitiveWordsDetected: comprar (en ocasiones)
- confidence: medium

## 5. No inventa precio. No normaliza. Solo reporta.
