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

## External signing and update metadata

`eng/packaging/build-msix.ps1` accepts an externally managed PFX. Provide `NODAL_OS_SIGNING_PFX_PASSWORD` through the secure CI secret store, then run:

```powershell
./eng/packaging/build-msix.ps1 `
  -Version 0.1.0.4 `
  -SigningPfxPath C:\secure\nodal-os-signing.pfx `
  -DistributionBaseUri https://updates.example.invalid/nodal-os/private
```

The package publisher is derived from the signing certificate subject so the manifest and signature cannot drift. A clean absolute HTTPS `DistributionBaseUri` emits `NodalOS.appinstaller`; no channel is emitted when that input is absent.

One-shot technical validation run `29620284871` proved the existing external path with an ephemeral runner-only PFX and synthetic package:

- publisher, manifest identity and Authenticode signer matched;
- signing mode was `external`;
- no CER or PFX was bundled;
- `.appinstaller` and package URIs were exact HTTPS paths without query or fragment;
- bundled install and uninstall passed;
- product and public-distribution authority remained false.

That validation proves the packaging contract, not a production identity or a real download/update service. No production private key may be committed, printed, attached to evidence or stored in repository artifacts. A production pipeline should prefer a managed signing service or a hardware/secure-vault-backed CA certificate. Microsoft Store signing remains a possible future distribution path.

## Package-derived notices

The `0.1.0.4` private-beta package generates `ThirdParty/THIRD_PARTY_NOTICES.txt`, an exact component manifest and retained source license/notice files from the installed dependency payload. Their hashes are bound into the update manifest and verified before and after installation.

The generated inventory explicitly remains unapproved for legal/public-distribution purposes. It closes the reproducibility gap but still requires owner/legal review against the final candidate.

## Public release prerequisites

The repository must remain `NO-GO` for public/commercial release until all of the following have evidence:

1. owner-approved source terms, product/end-user terms and any applicable private-beta evaluation terms;
2. exact package-derived third-party notices reviewed and accepted or corrected for the final release candidate;
3. a supported non-preview .NET SDK/runtime and a clean dependency/vulnerability review for the final release candidate;
4. a stable package identity and publisher;
5. a CA-trusted, Microsoft-managed or Store signing identity/strategy;
6. a controlled release branch/tag and immutable version;
7. a privacy notice and support/security contact appropriate for users;
8. a real validated HTTPS download/update location when `.appinstaller` is used;
9. a clean Windows install, launch, update and uninstall test outside the build workspace;
10. secret/path redaction and local-data behavior verified in the installed product;
11. rollback or downgrade policy;
12. explicit owner approval for public distribution.

The `0.1.0.4` private-beta candidate demonstrates the supported-runtime, dependency-inventory and package-derived-notice portions of items 2 and 3. The one-shot external-signing audit demonstrates the technical contract behind items 4, 5 and 8. Public release still requires real owner-selected identities, terms, infrastructure and repetition against the final signed payload.

The license and dependency inventory source of truth is `docs/distribution/license-status.md`. A checklist is not complete merely because a generic license file or technical inventory exists; selected terms, reviewed notices and final payload must agree.

Until these prerequisites are evidenced, MSIX artifacts are private-beta engineering evidence only. The current `0.1.0.4` self-contained package remains suitable only for controlled evaluation under its existing boundary; it is not a public release candidate.

## Version policy

MSIX uses a four-part numeric version. Private-beta builds start at `0.1.0.0` and must increase monotonically for in-place updates. Git tags, assembly metadata, package manifest and update manifest must refer to the same release candidate.

Rebuilding the same version is allowed only for CI diagnosis; it is not a distributable update because Windows package identity treats version as the ordering authority.
