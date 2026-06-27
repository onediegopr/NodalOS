# Recipe Catalog Product Surface

Product-surface phase: 1/4.

The Recipe Catalog product surface converts fixture-safe template contracts into read-only cards for operator review.

## Visible Packs

- Excel / Microsoft 365.
- Google Workspace.
- SAP.
- Mercado Libre / Mercado Pago.
- ARCA / Fiscal Argentina.
- ERP Local LATAM.
- Generic Browser Portals.
- Computer Use Legacy.

## Card Summary

Each card can show:

- template id, display name, and description.
- pack, category, system family, region, and country metadata.
- preview, fixture, or reference-only status.
- readiness and blocking status.
- risk level and human-review requirement.
- tool trust summary.
- secret reference summary.
- trigger observe-only summary.
- live runtime status.
- safe next action.
- not-included summary.

## Read-only Guarantees

Catalog cards cannot start recipes, process workitems, open connectors, request secret values, enable browser or desktop runtimes, or change template contracts.

Browser portal and Computer Use templates surface live-blocked or future-gated status. Fiscal, payment, marketplace, message, delete, and write-like templates surface human-review requirements and remain live-blocked.

## Data Source

The catalog uses the fixture-safe Recipe Template Catalog contracts from the completed Recipe Runtime line. No external systems, user data, credentials, APIs, files, browser sessions, or desktop state are read.
