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
  -Version 0.1.0.0 `
  -OutputDirectory artifacts/desktop-package/0.1.0.0
```

The script:

1. publishes `OneBrain.Pilot` self-contained for `win-x64`;
2. copies only the four recipes currently allowlisted by the product and excludes lab, negative and unrelated recipe fixtures;
3. generates deterministic NODAL OS package assets from the product palette;
4. generates the MSIX manifest from `eng/packaging/AppxManifest.xml.template`;
5. packages with the Windows SDK `MakeAppx.exe`;
6. signs with `SignTool.exe`;
7. verifies the signature;
8. writes a SHA-256-bound manual update manifest;
9. emits install/uninstall scripts and a private-beta ZIP bundle.

A test signing certificate is generated when no external PFX is supplied. It is valid only for controlled testing and is never a public release identity.

## Outputs

The output directory contains:

- `NodalOS-<version>-win-x64.msix`;
- `NodalOS-<version>-win-x64-private-beta.zip`;
- `nodal-os-update-manifest.json`;
- `Install-NodalOS.ps1`;
- `Uninstall-NodalOS.ps1`;
- `README-INSTALL.txt`;
- a public `.cer` file for test-signed builds only;
- optional `NodalOS.appinstaller` when a validated HTTPS distribution base URI is supplied.

No PFX, private key or signing password is copied into the output.

## Installation and update behavior

For a test-signed package on a controlled device, open **PowerShell as Administrator** and run:

```powershell
./Install-NodalOS.ps1 -TrustTestCertificate
```

The bundled installer verifies the MSIX against the generated SHA-256, refuses to trust a test certificate unless `-TrustTestCertificate` is passed explicitly and fails before mutation when PowerShell is not elevated. Trust is written to the local-machine `TrustedPeople` store. For a CA-trusted or Microsoft-managed signature, the certificate trust switch, elevation for certificate import and bundled test certificate are unnecessary.

The private-beta update policy is explicit and manual: install a package with the same identity and a greater four-part version. A hosted `.appinstaller` channel is generated only when `-DistributionBaseUri` is an HTTPS location supplied by the release operator.

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

## CI clean-install gate

`.github/workflows/desktop-package.yml` runs on a fresh Windows runner and performs:

1. Release build and focused desktop-launch tests;
2. self-contained publish;
3. MSIX creation and signature verification;
4. validation that the bundled installer refuses test-certificate trust without the explicit switch;
5. installation through the bundled `Install-NodalOS.ps1` operator path, including hash verification and exact certificate trust;
6. executable launch from the installed location;
7. Mission Control and model-configuration health checks;
8. product-authority, local-only and secret-exclusion assertions;
9. process shutdown;
10. package and exact test-certificate removal through the bundled `Uninstall-NodalOS.ps1` path;
11. confirmation that default uninstall preserves local user data;
12. explicit `-RemoveUserData` validation followed by final runner cleanup.

The CI artifact uploads the private-beta bundle once, together with its update manifest and complete smoke log, rather than duplicating the raw MSIX payload.

The produced artifact is a private-beta engineering artifact. Passing this gate does not create a public release or authorize customer-data use.
