# Recipe Template Catalog Contract

Phase: 8/9 - Global + LATAM Recipe Templates Pack v1.

The Recipe Template Catalog is an original NODAL OS contract layer for previewing reusable recipe templates. OpenRPA/OpenCore catalog patterns are inspiration only: no dependency, no code copy and no XAML import.

## Contract

The catalog contains `RecipeTemplatePack` and `RecipeTemplateDefinition` records. Each template carries:

- template id, display name, description, pack id and category.
- system, region and country metadata.
- recipe definition contract.
- required capabilities, tool trust refs, secret refs and connector eligibility refs.
- trigger refs, evidence requirements, validation requirements and approval/human intervention requirements.
- risk level, sensitive action categories and safety profile.
- allowed/blocked run modes.
- runtime eligibility and live runtime status.
- fixture sample refs, safe next action and not-included summary.

## Safety

Default status is `CatalogPreview` or `DraftTemplate`, never live. Browser portal and Computer Use legacy templates are `LiveBlocked` or `FutureGated`. Fiscal, payment, marketplace, message, delete and write-like templates require human/approval paths and remain non-live.

The catalog stores references and summaries only. It does not store raw secrets, raw payloads, screenshots, DOM, accessibility trees, HAR, connector results or runtime captures.

## Out Of Scope

No real browser automation, desktop automation, connector execution, API calls, network access, vault access, scheduler, trigger autorun, recorder, replay, watcher, hook, listener, locator replay or live locator repair apply is implemented by this contract.
