# Product UI Entry Point Audit - M606-M608

Decision target: `PRODUCT_UI_MIGRATION_AUDIT_READY`.

This report closes M606-M608 as audit-only, boundary-only, and QA-plan-only. It does not implement product UI migration, does not convert static previews into product surfaces, and does not connect any preview to runtime, filesystem, provider, cloud, productive consent, or capability enablement.

## M606 - Product UI Entry Point Audit

Repo inspection identified a Chrome extension sidepanel as the primary product UI candidate and the M582-M605 Research OS previews as artifact-only baselines.

Inventory findings:

- No root `package.json` or lockfile was found in versioned root files during this audit.
- No Cargo or Tauri configuration was found in versioned files during this audit.
- Extension manifest exists at `browser-extension/onebrain-chrome-lab/manifest.json`.
- Product sidepanel candidates exist at `browser-extension/onebrain-chrome-lab/sidepanel.html`, `browser-extension/onebrain-chrome-lab/sidepanel.css`, and `browser-extension/onebrain-chrome-lab/sidepanel.js`.
- Static Research OS previews exist under `artifacts/agent-operations/m587` through `artifacts/agent-operations/m605`.
- Design docs exist under `docs/design`.
- UI-related fixtures and samples exist under `apps`, `samples`, `tests/fixtures`, and `tools/fixtures`.

Entrypoint classification:

- Productive UI entrypoint: Chrome sidepanel HTML/CSS/JS and extension manifest.
- Static preview only: Research OS preview artifacts from M587-M605.
- Legacy/Quarantine: runtime-adjacent extension scripts and historical browser fixtures that require separate safety review before visual migration.
- Test fixture: owned target pages and browser fixture pages.
- Artifact preview: static HTML previews under artifact folders.
- Documentation only: design system, screen map, migration plan, and visual reports.
- Unknown: any UI-like file not yet tied to a product owner or test owner.

Migration risk levels:

- Low: docs, artifact previews, and test fixtures.
- Medium: static samples that may be viewed by humans but are not the product shell.
- High: product UI sidepanel files and extension manifest.
- Blocked: runtime-adjacent extension files or sensitive flows until separate security review.

Recommended handling:

- Sidepanel files: update later only after migration boundary, rollback, visual regression, and no-runtime-coupling tests exist.
- Artifact previews: keep static, do not promote to source of truth.
- Runtime-adjacent extension files: do not touch in this visual migration path.
- Test fixtures: update only for QA coverage if explicitly scoped.
- Design docs: keep as baseline references.

## M607 - Sidepanel / Mission Control Migration Boundary

The boundary allows future visual-only changes to sidepanel/Mission Control surfaces while forbidding runtime, filesystem, provider, cloud, productive consent, capability, source-of-truth, approval, and event wiring changes.

Target surfaces:

- Mission Control.
- Sidebar.
- Current Mission Hero.
- Timeline Summary.
- Evidence Summary.
- Decision Preview.
- Advisor Note.
- Consent/Capability Preview.
- Runtime Status.
- Activity Feed.

Allowed changes:

- Visual tokens.
- Static layout.
- Copy and microcopy.
- CSS class structure.
- Static preview fixture data.
- Blocked-state anatomy.
- Visual hierarchy.
- Accessibility and contrast improvements.
- No-op actions only.

Forbidden changes:

- Runtime calls.
- Filesystem calls.
- Provider Calls.
- Network calls.
- Cloud calls.
- Capability enablement.
- Productive consent.
- Source-of-truth promotion.
- Approval mutation.
- Event routing.
- Background execution orchestration.
- Direct browser executor references.
- Broad rewrite without rollback.

Boundary decision:

- `ReadyForProductUiMigrationPlanning=true`.
- `ReadyForDirectProductUiRewrite=false`.
- `ReadyForRuntimeCoupling=false`.
- `ReadyForFilesystemCoupling=false`.
- `ReadyForLlmProviderCoupling=false`.
- `ReadyForProductiveConsentCoupling=false`.

Recommended next milestone: Sidepanel Research OS Migration Plan.

## M608 - Visual Regression QA Plan

The QA plan validates the future migration before product UI changes are made.

Required QA coverage:

- Visual baseline references from M582-M605.
- Static HTML preview validation.
- No remote dependencies in previews.
- No runtime coupling.
- No filesystem coupling.
- No provider/cloud coupling.
- No productive consent coupling.
- Source-of-truth boundary.
- Blocked-state anatomy verification.
- Research OS style verification.
- Accessibility and contrast checklist.
- Responsive layout checklist.
- Keyboard navigation checklist where applicable.
- Rollback strategy.

Visual scenarios:

- Mission Control hero remains dominant.
- Sidebar remains minimal.
- Timeline does not become checklist.
- Evidence does not become attachments.
- Decisions do not become generic approvals.
- Advisor does not become chatbot.
- Consent does not become settings.
- Runtime does not imply active execution.
- Models do not imply Provider Calls.
- Agents do not imply autonomous execution.
- Settings do not imply productive persistence.
- Activity Feed does not become raw logs.
- Blocked states explain why, missing requirements, evidence, action, and intentionally disabled surfaces.

Regression failure cases:

- Generic SaaS dashboard returns.
- Dark technical control center returns as default.
- Mission hidden behind widgets.
- Runtime appears active.
- File access appears available.
- Provider appears configured or active.
- Evidence appears verified as productive truth.
- Consent appears persisted.
- Preview actions look executable.
- Source-of-truth ambiguity.

QA decision:

- `VisualRegressionPlanReady=true`.
- `ReadyForSafeStaticUiMigration=true` because sidepanel entrypoints were identified.
- `ReadyForBroadRewrite=false`.
- `ReadyForRuntimeConnection=false`.
- `ReadyForRealCapabilityConnection=false`.

## Closeout

M606-M608 is ready as a migration audit baseline. No product UI migration was implemented. No runtime, filesystem feature, evidence verification, LLM/provider/cloud, productive consent, capability enablement, or source-of-truth promotion was introduced.
