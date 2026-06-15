# ADR M63 — Billing Real Design + Payment Provider Sandbox

## Estado

Aceptado.

## Contexto

Billing real sigue prohibido, pero el producto necesita diseno y sandbox para checkout, payment intent, subscription lifecycle y webhooks sin mover dinero real.

## Decision

M63 define provider kinds `MockOnly`, `SandboxProvider`, `RealProviderFuture` y `Disabled`.

El sandbox modela:

- Checkout session sandbox.
- Payment intent sandbox.
- Subscription created/canceled sandbox.
- Invoice paid/failed sandbox.
- Webhook sandbox/design-only.
- Billing audit redacted.

## Safety

El sandbox no cobra dinero real, no almacena payment card data, no llama pasarela productiva, no habilita plan real automaticamente, no habilita SensitiveRealPilot y no habilita ProductiveVault por defecto.

## Fuera de alcance

- Stripe/Redsys/PayPal real.
- Tarjetas reales.
- Cobro real.
- Webhooks reales.
