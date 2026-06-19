# Agent Operations Extraction Prep Plan

Project: NODAL OS

Status: M404-M406 extraction prep, no code move.

## 1. Executive Summary

Agent Operations has outgrown its temporary home in `OneBrain.BrowserExecutor.Contracts` and `OneBrain.BrowserExecutor.Cdp`. The current placement was acceptable while the system was contract-only, but it should not be the long-term boundary for Orchestration API, Workboard UI, persistent registry, or worker runtime.

The recommended next step is a phased extraction into Agent Operations projects while preserving public/internal compatibility. No namespaces, projects, or files are moved in M404-M406.

## 2. Current Layout

Current temporary location:

- Contracts: `src/OneBrain.BrowserExecutor.Contracts`
- Services/builders/validators: `src/OneBrain.BrowserExecutor.Cdp`
- Tests: `tests/OneBrain.Safety.Tests`
- Reports: `docs/reports`
- Architecture docs: `docs/architecture`
- Artifacts: `artifacts/agent-operations` and `artifacts/core`

Current issue:

- Agent Operations services inherit Browser/CDP project identity.
- Core-like services sit beside browser runtime and OCR/runtime dependencies.
- Future consumers could treat browser executor as the owner of Agent Operations.

## 3. Target Layout Options

### Option A: Historical Repository Naming

- `src/OneBrain.AgentOperations.Contracts`
- `src/OneBrain.AgentOperations.Core`
- `src/OneBrain.AgentOperations.Adapters.Browser`

Pros:

- Matches current repository/project naming.
- Lower migration shock.
- Avoids broad namespace/project rename.
- Preserves compatibility with existing `OneBrain.*` conventions.

Cons:

- Retains historical `OneBrain` namespace debt.

### Option B: Future Clean Naming

- `src/NodalOs.AgentOperations.Contracts`
- `src/NodalOs.AgentOperations.Core`
- `src/NodalOs.AgentOperations.Adapters.Browser`

Pros:

- Aligns with current project name.
- Cleaner long-term naming.

Cons:

- Higher compatibility risk.
- Broader namespace migration.
- Should not be combined with extraction Phase 1.

## 4. Recommended Target Layout

Recommended layout for the next extraction phase:

- `src/OneBrain.AgentOperations.Contracts`
- `src/OneBrain.AgentOperations.Core`
- `src/OneBrain.AgentOperations.Adapters.Browser`

Rationale:

- The repository still uses `OneBrain.*` project naming.
- M389-M391 already documented `OneBrain.*` as historical namespace debt.
- Extraction should not be combined with broad rename.
- New types still use `NodalOs*`.
- Existing `Nexa*` symbols remain compatibility symbols.

## 5. Dependency Direction Rules

Required direction:

- `AgentOperations.Contracts` must not depend on Browser/CDP.
- `AgentOperations.Core` must not depend on Browser/CDP.
- `AgentOperations.Adapters.Browser` may depend on AgentOperations contracts/core and BrowserExecutor.
- Browser runtime may depend on AgentOperations contracts/core only through adapters.
- Common redaction/evidence bridge should remain shared and non-browser-specific.
- Policy, approval, safe action, and FSM integration must remain abstraction-driven.

Forbidden direction:

- AgentOperations core -> `ChromeCdpBrowserExecutor`
- AgentOperations core -> browser profile/session/frame managers
- AgentOperations core -> browser runtime smoke/cleanup
- AgentOperations contracts -> BrowserExecutor/CDP

## 6. Contracts Extraction Candidates

Move in Phase 1:

- `NodalOsAgentWorkboardContracts.cs`
- `NodalOsFailureTaxonomyContracts.cs`
- `NodalOsRunReportContracts.cs`
- `NodalOsRecipeManifestContracts.cs`
- `NodalOsVerificationBeforeDoneContracts.cs`
- `NodalOsAgentProgressReportingContracts.cs`
- `NodalOsStepLibraryContracts.cs`
- `NodalOsRedactionContracts.cs`
- `NodalOsEvidenceRefBridgeContracts.cs`
- `NodalOsPackageSkillManifestContracts.cs`
- `NodalOsInternalSkillRegistryContracts.cs`
- `NodalOsWorkerBoundaryContracts.cs`

Do not rename compatibility symbols during Phase 1.

## 7. Core Service Extraction Candidates

Move after contracts:

- `NodalOsAgentWorkboardServices.cs`
- `NodalOsRunReportingServices.cs`
- `NodalOsRecipeManifestServices.cs`
- `NodalOsVerificationBeforeDoneGate.cs`
- `NodalOsAgentProgressReportingServices.cs`
- `NodalOsStepLibraryServices.cs`
- `NodalOsRedactionServices.cs`
- `NodalOsEvidenceRefBridgeServices.cs`
- `NodalOsPackageSkillManifestServices.cs`
- `NodalOsInternalSkillRegistryServices.cs`
- `NodalOsWorkerBoundaryServices.cs`

Expected target: `OneBrain.AgentOperations.Core`.

## 8. Browser Adapter Candidates

Keep in BrowserExecutor or move later to `OneBrain.AgentOperations.Adapters.Browser` only if an adapter seam is needed:

- `BrowserRuntimeSmoke.cs`
- `ChromeCdpBrowserExecutor.cs`
- `BrowserProfileSessionManager.cs`
- `BrowserTargetFrameManager.cs`
- `BrowserPersistentAuditLedger.cs`
- `ChromeCdpExternalProofServices.cs`
- browser safe download/upload services
- browser consent/profile/session/credential boundary services

These are browser runtime concerns and should not move into AgentOperations.Core.

## 9. Compatibility Shims

Shims are required.

Minimum shim strategy:

- Keep existing types stable during Phase 1.
- Add project references from old browser projects to new AgentOperations projects.
- Keep old namespaces temporarily if needed.
- Consider type forwarding only after compile/test impact is measured.
- Keep `Nexa*` compatibility symbols as-is until a dedicated naming migration.
- Keep old tests green during every phase.

Potential shim types:

- Workboard validator facade.
- Verification-before-done facade.
- Run report builder facade.
- Progress report validator facade.
- Package/Registry/Worker validator facade.
- EvidenceRef bridge facade.
- Redaction facade.

## 10. Migration Phases

### Phase 0: Prep

M404-M406. Discovery, boundary plan, artifact, tests. No moves.

### Phase 1: Contracts Extraction

Create `OneBrain.AgentOperations.Contracts`.

Move contract files only.

Keep behavior unchanged.

Tests required:

- compile all projects;
- serialization tests;
- artifact existence tests;
- public compatibility tests.

### Phase 2: Core Services Extraction

Create `OneBrain.AgentOperations.Core`.

Move service/builders/validators that have no browser dependency.

Tests required:

- all Agent Operations unit tests;
- no-divergence tests;
- full suite.

### Phase 3: Browser Adapter Boundary

Create or defer `OneBrain.AgentOperations.Adapters.Browser`.

Move only browser-specific adapters if needed.

Tests required:

- browser runtime smoke tests;
- audit ledger tests;
- evidence bridge adapter tests.

### Phase 4: Compatibility Cleanup

Deprecate old browser-hosted facades only after downstream consumers are migrated.

Do not remove compatibility in the same milestone as extraction.

### Phase 5: Naming Debt

Only after extraction stabilizes:

- evaluate `Nexa*` compatibility aliases;
- evaluate `OneBrain.*` historical namespace debt;
- decide if `NodalOs.AgentOperations.*` migration is worth the churn.

## 11. Tests Required Per Phase

- Extraction report/artifact tests.
- Contract serialization tests.
- Workboard/mission/task tests.
- Run report/failure taxonomy tests.
- Recipe manifest tests.
- Verification-before-done tests.
- Progress reporting tests.
- Step library tests.
- Common redaction tests.
- EvidenceRef bridge tests.
- Package/Skill manifest tests.
- Internal Skill Registry tests.
- Worker Boundary tests.
- Package/Registry/Worker no-divergence tests.
- Full solution test before commit.

## 12. What Not To Move Yet

Do not move:

- `ChromeCdpBrowserExecutor`
- `BrowserRuntimeSmoke`
- `BrowserPersistentAuditLedger`
- browser profile/session/frame services
- OCR runtime services
- ONNX runtime/session/model services
- safe upload/download browser implementations
- UI/sidepanel code
- orchestration API code
- worker runtime code

Do not rename:

- `Nexa*` compatibility symbols
- `OneBrain.*` namespaces

Do not implement:

- execution
- orchestration
- UI
- worker runtime
- registry persistence
- package installation
- marketplace

## 13. Blockers

No hard dependency cycle is currently documented.

Known blockers before Phase 1:

- decide whether to use simple project moves or facade/type-forwarding pattern;
- decide whether old BrowserExecutor projects retain source files temporarily;
- keep EvidenceRef bridge and redaction together until a shared security/evidence boundary is explicit.

## 14. Recommended Next Hito

Recommended next milestone: `M407-M409 Agent Operations Extraction Phase 1 Contracts`.

Alternative: `M407-M409 Orchestration API Decision Record`, but executable orchestration should wait until contracts extraction is complete.

## Acceptance Criteria

- Current BrowserExecutor location documented.
- Target contracts boundary defined.
- Target core boundary defined.
- Target browser adapter boundary defined.
- Compatibility shims required and documented.
- No namespace move implemented in M404-M406.
- No broad rename implemented in M404-M406.
- No runtime behavior change.
- No UI.
- No orchestration.
- No execution.
