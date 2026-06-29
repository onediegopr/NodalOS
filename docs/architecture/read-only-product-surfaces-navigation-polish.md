# Read-Only Product Surfaces Navigation Polish

Decision target: `GO_READ_ONLY_PRODUCT_SURFACES_NAVIGATION_POLISH_READY`

## Scope

This block closes a small cohesion pass for Phase B read-only product surfaces inside the installed sidepanel Mission Control surface.

The polish is limited to:

- Mission Control navigation grouping for read-only surfaces;
- a compact `#readOnlySurfacesSummary` map;
- shared status chip and notice classes for Recipe Lab and Evidence Intelligence;
- safer copy preview wording;
- focused Safety tests and documentation.

## Navigation Pattern

Mission Control remains the product surface seam. No router, framework, runtime service, provider, durable store or execution dispatcher was added.

The side navigation now groups:

- `#readOnlySurfacesSummary`
- `#recipeLabSurface`
- `#evidenceIntelligenceSurface`

Recipe Lab and Evidence Intelligence remain direct anchors, and their sections keep their existing mount ids.

## Visible Boundary

The summary and surfaces make these boundaries explicit:

- `READ_ONLY`
- `NO_RUNTIME`
- `NO_LIVE_AUTOMATION`
- local/fixture-only data
- semantic backend disabled for EIL
- recipe execution disabled for Recipe Lab
- no browser/CDP automation from UI
- no WCU/OCR live
- no provider/cloud calls
- no filesystem writes
- human review required for any real action

## Copy Preview

Copy actions remain clipboard-only text previews:

- Recipe Lab report preview
- Recipe Lab handoff preview
- Evidence Intelligence report preview

They do not call the bridge, fetch/network, Chrome runtime, providers, filesystem writers or live automation.

## Out Of Scope

- runtime execution;
- recipe execution;
- live browser/CDP;
- WCU/OCR live;
- provider/cloud calls;
- durable persistence;
- Stealth or Cloak runtime changes;
- broad sidepanel redesign.
