# Protected Scope - Post-M1345 Isolated Browser Execution / Remote Handoff

This area is protected by explicit user mandate.

Do not audit.
Do not modify.
Do not refactor.
Do not revert.
Do not move.
Do not delete.
Do not expand.
Do not add tests against it.
Do not propose changes.
Do not include it in future cleanup/audit/refactor scopes.

Allowed only if the user gives a new explicit instruction naming this protected scope and authorizing changes.

Protected categories:

* isolated browser execution runner
* Node.js + Playwright runner
* Docker/deployment files added for this module
* remote handoff/control panel
* browser profiles/connectivity/session components
* assisted verification/challenge flow
* Bridge .NET/WebSocket changes made in the same post-M1345 sequence
* docs directly tied to this protected module

Future agents must treat these as FORBIDDEN PATHS unless explicitly authorized by the user.

Known protected path patterns from the post-M1345 sequence:

* `stealth-engine/**`
* `stealth-panel/**`
* `src/OneBrain.ChromeLab.Bridge/Stealth/**`
* `src/OneBrain.ChromeLab.Bridge/Sessions/**`
* `src/OneBrain.ChromeLab.Bridge/Dockerfile`
* `src/OneBrain.ChromeLab.Bridge/ChromeLabOptions.cs`
* `src/OneBrain.ChromeLab.Bridge/ChromeLabProtocol.cs`
* `src/OneBrain.ChromeLab.Bridge/Program.cs`
* `src/OneBrain.BrowserExecutor.Cdp/BrowserCredentialBoundaryService.cs`
* `src/OneBrain.BrowserExecutor.Contracts/BrowserCredentialBoundaryContracts.cs`
* `docker-compose.yml`
* `scripts/deploy.ps1`
* `scripts/stop.ps1`
* `docs/stealth-engine-design.md`
* `docs/stealth-audit-report.md`
* `docs/stealth-reaudit-report.md`
* `docs/unified-friction-integration-design.md`
* `docs/ARCHITECTURE.md`
* `docs/CONFIGURATION.md`
* `docs/DEPLOYMENT.md`
* `docs/OPERATIONS.md`
* `docs/ROADMAP.md`
* `CHANGELOG.md`
