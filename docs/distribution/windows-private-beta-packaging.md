# NODAL OS — Windows private-beta packaging

Status: technical productization path, not a public or production release.

## Decision

The first installable NODAL OS package uses the Windows-native MSIX format built directly from the existing .NET application. It does not introduce Tauri, Electron, Node, a second application shell or a third-party installer framework.

The package contains a self-contained `win-x64` publish of `OneBrain.Pilot`. Windows registers it as a full-trust packaged classic desktop application. The executable starts the existing loopback-only Mission Control runtime and opens the default browser after the local server is ready. `--no-open-browser` keeps process and CI launches headless.

The package preserves the current product boundary:

- the canonical UI remains Mission Control;
- Kestrel binds only to loopback HTTP origins;
- mutable Pilot data defaults to `%LOCALAPPDATA%\NodalOS\ProductData` instead of the read-only package location;
- workspace and BYOK secret values remain outside the package and are stored under the current Windows user;
- package installation does not grant mission, filesystem, provider or product authority;
- ChromeLab and historical deployment prototypes are not installed as product services.

## Build

From a Windows machine with the SDK pinned by `global.json` and a Windows 10/11 SDK:

```powershell
./eng/packaging/build-msix.ps1 `
  -Version 0.1.0.5 `
  -OutputDirectory artifacts/desktop-package/0.1.0.5
```

The script:

1. publishes `OneBrain.Pilot` self-contained for `win-x64`;
2. generates an exact third-party component/notice inventory from the published `OneBrain.Pilot.deps.json` and locally restored package material;
3. copies only the four recipes currently allowlisted by the product and excludes lab, negative and unrelated recipe fixtures;
4. generates deterministic NODAL OS package assets from the product palette;
5. generates the MSIX manifest from `eng/packaging/AppxManifest.xml.template`;
6. packages with the Windows SDK `MakeAppx.exe`;
7. signs with `SignTool.exe`;
8. verifies the signature;
9. writes a SHA-256-bound manual update manifest, including hashes for the third-party inventory;
10. emits install/uninstall scripts and a private-beta ZIP bundle.

A test signing certificate is generated when no external PFX is supplied. It is valid only for controlled testing and is never a public release identity.

## Outputs

The output directory contains:

- `NodalOS-<version>-win-x64.msix`;
- `NodalOS-<version>-win-x64-private-beta.zip`;
- `nodal-os-update-manifest.json`;
- `Install-NodalOS.ps1`;
- `Uninstall-NodalOS.ps1`;
- `README-INSTALL.txt`;
- `ThirdParty/THIRD_PARTY_NOTICES.txt`;
- `ThirdParty/third-party-components.json`;
- `ThirdParty/files/...` with the exact package-derived source license/notice files;
- a public `.cer` file for test-signed builds only;
- optional `NodalOS.appinstaller` when a validated HTTPS distribution base URI is supplied.

The `ThirdParty` inventory is included both in the installed MSIX and in the outer ZIP. It records exact components, versions, source-file hashes and the absence of legal/public-distribution approval. It is reproducible technical evidence, not adopted product terms.

No PFX, private key or signing password is copied into the output.

## Installation and update behavior

For a test-signed package on a controlled device, open **PowerShell as Administrator** and run:

```powershell
./Install-NodalOS.ps1 -TrustTestCertificate
```

The bundled installer verifies the MSIX against the generated SHA-256, reads the candidate identity from `AppxManifest.xml`, then compares installed packages by package name and publisher. It refuses to trust a test certificate unless `-TrustTestCertificate` is passed explicitly and fails before mutation when PowerShell is not elevated. Trust is written to the local-machine `TrustedPeople` store. For a CA-trusted or Microsoft-managed signature, the certificate trust switch, elevation for certificate import and bundled test certificate are unnecessary.

The private-beta update policy is explicit and manual:

- test-signed revisions are clean-install only once the same package name and publisher are already installed. The new installer blocks before importing the new certificate and tells the operator to uninstall with the previous bundle first, because each generated test certificate can be different and only the previous bundle can safely remove its exact trust;
- externally signed packages may update only when package name and publisher match exactly and the candidate four-part version is strictly greater than the installed version;
- same-version reinstall and downgrade are rejected without `ForceUpdateFromAnyVersion`;
- a package with the same name but a different publisher is not treated as an update target.

A hosted `.appinstaller` channel is generated only when `-DistributionBaseUri` is an HTTPS location supplied by the release operator.

The `ms-appinstaller:` URI protocol is not assumed. Users download and open the `.msix` or `.appinstaller` file directly.

## Uninstall and local data

For a test-signed bundle, use **PowerShell as Administrator** so the package and the exact certificate included in the bundle are both removed:

```powershell
./Uninstall-NodalOS.ps1
```

The default uninstall removes the registered package and its exact test-certificate trust while preserving user data under `%LOCALAPPDATA%\NodalOS`, so an uninstall or upgrade cannot silently destroy workspace bindings, evidence references or protected model configuration.

To remove both the package and local NODAL OS data:

```powershell
./Uninstall-NodalOS.ps1 -RemoveUserData
```

This is intentionally explicit because removing local data can be irreversible.

## Local clean-install and monotonic-update gates

The reusable local smoke scripts are:

- `eng/ci/smoke-desktop-package.ps1 -Version 0.1.0.5`, for clean package generation, install, launch, Teach NODAL route checks, product-boundary checks, uninstall and exact test-certificate cleanup;
- `eng/ci/smoke-msix-monotonic-update.ps1 -PreviousCommit 78f2a1988c68478eea9a65ecb4a04aa82f0d7483 -PreviousVersion 0.1.0.4 -CandidateVersion 0.1.0.5`, for an external-signed 0.1.0.4 -> 0.1.0.5 update, app-owned state preservation, Teach NODAL availability, downgrade rejection, same-version rejection and test-signed clean-uninstall policy.

No GitHub Actions evidence is required for the local closeout. If a future CI workflow is added, it must remain separate from this local candidate evidence until explicitly authorized.

The clean-install gate performs:

1. Release build and focused desktop-launch tests;
2. self-contained publish;
3. exact component inventory generation from the published dependency manifest;
4. source notice-file hash and coverage verification for every external component;
5. MSIX creation and signature verification;
6. validation that the bundled installer refuses test-certificate trust without the explicit switch;
7. installation through the bundled `Install-NodalOS.ps1` operator path, including hash verification and exact certificate trust;
8. verification that the outer ZIP and installed MSIX contain identical notice manifests bound by the update manifest;
9. comparison of the generated inventory against the dependency manifest inside the installed package;
10. executable launch from the installed location;
11. clean Mission Control and model-configuration health checks;
12. product-authority, local-only, packaged-route and secret-exclusion assertions;
13. an installed-package core loop covering local BYOK configuration, authorized fallback, workspace selection, real mission review, scoped approval and verified `NODAL_HANDOFF.md` execution;
14. canonical Markdown handoff export with no-store and redaction assertions;
15. guarded rollback with exact workspace-baseline restoration;
16. process shutdown;
17. package and exact test-certificate removal through the bundled `Uninstall-NodalOS.ps1` path;
18. confirmation that default uninstall preserves local user data;
19. explicit `-RemoveUserData` validation followed by final cleanup.

The installed core-loop smoke reuses the existing product routes and local OpenAI-compatible fixture; it does not introduce another runtime, product mode or release gate.

The produced artifact is a private-beta engineering artifact. Passing these gates does not create a public release, approve legal terms or authorize customer-data use.

## Local 0.1.0.5 candidate evidence

The local 0.1.0.5 package build emitted a test-signed private-beta candidate that includes the current `main` product surface, including Teach NODAL `/teach` and `/api/teach`.

This candidate is not canonical until the elevated install/update smokes complete on an operator-controlled Windows device.

Observed local candidate metadata:

- package name: `NODALOS.PrivateBeta`;
- publisher: `CN=NODAL OS Private Beta`;
- version: `0.1.0.5`;
- signing mode: `test`;
- MSIX: `NodalOS-0.1.0.5-win-x64.msix`;
- MSIX SHA-256: `3f68da686fb64448b9de1c77026dd4ec02d34bf20e229b323ac04648c7f9f6a5`;
- private-beta ZIP SHA-256: `d0f3b1cb8768dbe7504c82caf710f505f5f57d7583375724714e489aa7d55ae4`;
- update manifest SHA-256: `65c4a91b6fa8148aad3ce7e20e48c38150e693cc69d45f0fed411c42380329d1`;
- test certificate SHA-256: `2700baba35b4a67c418fde5fa3ce69e9a5f169d60f33b29ff6aca81294a610a5`;
- third-party notices SHA-256: `e78e6822cce5921bb8282a7861bce23946f08b4be9c44692c65b0a2856bda29f`;
- third-party components SHA-256: `2988e231e59d839ee1a7b7b0e26a39c4d64f8588551f74cd94a8cbcf42bafddc`.

Local non-elevated smoke status:

- package build and bundle emission reached the operator install step;
- install stopped because trusting the private-beta test certificate requires elevated PowerShell;
- the monotonic update smoke stopped at its elevated-PowerShell precondition before package mutation;
- GitHub Actions were not used.
