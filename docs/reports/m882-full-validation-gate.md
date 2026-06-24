# M882 - Full Validation Gate

Project: NODAL OS.

Required validations:

- `dotnet build .\OneBrain.slnx --no-restore`
- Filter M863-M868
- Filter M869-M872
- Filter M873-M884
- BrowserRuntimeSmokeTests isolated
- Full safety suite
- Recipes suite
- Full suite general
- `git diff --check`
- final git status

Caveats must be recorded if BrowserRuntimeSmoke flake or transient IO returns. PASS cannot be declared without evidence.
