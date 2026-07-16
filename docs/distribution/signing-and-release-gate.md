# NODAL OS — Signing and release gate

Status: public distribution blocked.

## Private-beta signing

CI and local engineering builds may generate a short-lived self-signed code-signing certificate. The public certificate is included only so a controlled test device can explicitly trust and install that package.

A test certificate:

- is not a production identity;
- must not be silently trusted on customer devices;
- must not be published as a public release certificate;
- must not be reused as a long-lived signing key;
- does not create SmartScreen reputation;
- does not grant NODAL OS product authority.

The generated PFX and password are temporary and are deleted after signing. They are never uploaded as artifacts.

## External signing

`eng/packaging/build-msix.ps1` accepts an externally managed PFX. Provide `NODAL_OS_SIGNING_PFX_PASSWORD` through the secure CI secret store, then run:

```powershell
./eng/packaging/build-msix.ps1 `
  -Version 0.1.0.0 `
  -SigningPfxPath C:\secure\nodal-os-signing.pfx
```

The package publisher is derived from the signing certificate subject so the manifest and signature cannot drift.

No production private key may be committed, printed, attached to evidence or stored in repository artifacts. A production pipeline should prefer a managed signing service or a hardware/secure-vault-backed CA certificate. Microsoft Store signing is also a valid future distribution path.

## Public release prerequisites

The repository must remain `NO-GO` for public/commercial release until all of the following have evidence:

1. a declared source and product distribution license;
2. a stable package identity and publisher;
3. a CA-trusted, Microsoft-managed or Store signing strategy;
4. a controlled release branch/tag and immutable version;
5. a privacy notice and support/security contact appropriate for users;
6. a validated HTTPS download/update location when `.appinstaller` is used;
7. a clean Windows install, launch, update and uninstall test outside the build workspace;
8. secret/path redaction and local-data behavior verified in the installed product;
9. rollback or downgrade policy;
10. explicit owner approval for public distribution.

Until then, MSIX artifacts are private-beta engineering evidence only.

## Version policy

MSIX uses a four-part numeric version. Private-beta builds start at `0.1.0.0` and must increase monotonically for in-place updates. Git tags, assembly metadata, package manifest and update manifest must refer to the same release candidate.

Rebuilding the same version is allowed only for CI diagnosis; it is not a distributable update because Windows package identity treats version as the ordering authority.
