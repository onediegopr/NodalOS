# NODAL OS — License status

Status: owner decision and appropriate legal review required before external distribution.

The repository does not currently declare source or product distribution terms. In the absence of explicit terms, no public package, customer installer, marketplace listing or commercial release is authorized by this repository.

The private-beta MSIX pipeline validates technical packaging, installation, launch, update metadata, uninstall behavior and exact package-derived third-party notice inventory. Its artifacts are engineering evidence for controlled internal evaluation, not permission for external distribution or for use of third-party code outside its applicable terms.

## Product recommendation for owner review

This is a product recommendation, not legal advice or an adopted license.

The current business model is best aligned with:

1. proprietary or all-rights-reserved source/product terms;
2. separate evaluation-only terms for private-beta participants;
3. an end-user license for the signed desktop package;
4. owner/legal review of the exact third-party notice inventory generated from the final release-candidate payload.

This path matches the current paid desktop-product strategy, BYOK positioning and optional cloud model without granting broad redistribution rights by accident. The owner may instead choose source-available, open-source or split licensing, but that choice must be explicit and consistently reflected in the repository, installer and release process.

## Validated MSIX dependency and notice snapshot

Engineering evidence source:

- Desktop Package workflow run: `29620916314`;
- PR head used by the validated candidate: `8338b22f406f329034a4883f98e04204e61648e4`;
- Actions test-merge commit embedded in package metadata: `9bb7a95f39f61e3472e8f363db9702b584428888`;
- package version: `0.1.0.4`;
- MSIX SHA-256: `4d8dab9968089a9f8ba4e0416922fcc4f75cb9551abe1aea0f3fd5aca268ef84`;
- private-beta bundle SHA-256: `5befbc582c1851068cc8ae1cba8e5a96826973e173058d9e347501656af9735f`;
- update manifest SHA-256: `4efe329f4ae00beee8ad09ce34c389776bc98a12c8bb2317f0fcbdb5931282f6`;
- consolidated technical notices SHA-256: `6bdd3983110decf5527bf0e3ce22cfd2803101f0ba430699a6c653858acfbf42`;
- component inventory SHA-256: `a81fc9df3c630a4cd1a30684a32299da7e136b868272bffeccdc89fdc6fa174a`;
- GitHub artifact id: `8422197549`;
- GitHub artifact digest: `a0ab75b933943beb8b5ca1fe4e650f1fec99162670a50d3fe5d986e99f6f07a0`;
- dependency source inside the package: `OneBrain.Pilot.deps.json`.

The generated inventory matches all seven external components declared by the installed self-contained dependency manifest and retains ten exact local license/notice source files with individual SHA-256 hashes.

| Package | Version | Relationship | Package-derived license signal |
| --- | ---: | --- | --- |
| `FlaUI.Core` | `5.0.0` | direct runtime dependency | file: `LICENSE.txt` |
| `FlaUI.UIA3` | `5.0.0` | direct runtime dependency | file: `LICENSE.txt` |
| `Interop.UIAutomationClient` | `10.19041.0` | transitive through FlaUI.UIA3 | file: `LICENSE.txt` |
| `System.Management` | `8.0.0` | transitive through FlaUI.Core | expression: MIT plus local license/notices |
| `Microsoft.NETCore.App.Runtime.win-x64` | `10.0.10` | self-contained runtime pack | expression: MIT plus local license/notices |
| `Microsoft.AspNetCore.App.Runtime.win-x64` | `10.0.10` | self-contained runtime pack | expression: MIT plus local license/notices |
| `Microsoft.WindowsDesktop.App.Runtime.win-x64` | `10.0.10` | self-contained runtime pack | expression: MIT plus local license file |

The installed MSIX and outer private-beta ZIP now contain:

- `ThirdParty/THIRD_PARTY_NOTICES.txt` — consolidated package-derived technical inventory;
- `ThirdParty/third-party-components.json` — exact component/version/file/hash manifest;
- `ThirdParty/files/...` — exact source license/notice files copied from restored NuGet or installed runtime-pack material.

The generated files explicitly record `legalReviewRequired=true`, `legalApprovalGranted=false` and `publicDistributionAuthorized=false`. They close the reproducibility gap; they do not select NODAL OS source/product terms or substitute owner/legal review.

## Decisions still required

The owner must explicitly select and record:

- repository source terms: proprietary, source-available, open-source or split;
- desktop product/end-user terms;
- private-beta evaluation terms and who may receive the package;
- permitted redistribution, copying and reverse engineering, subject to applicable law;
- warranty, liability, support and governing-law language for intended markets;
- whether the public repository remains public under the selected source terms;
- acceptance or correction of the exact generated third-party notice set for the final candidate.

## Implementation after the decision

Do not add placeholder legal text that appears authoritative. Once the owner-approved direction exists:

1. add the chosen root `LICENSE` or proprietary source-terms file;
2. add private-beta evaluation terms and the desktop EULA in the approved release location;
3. review and approve or correct the generated `ThirdParty` inventory against the final release candidate;
4. include the approved terms and reviewed notices in the controlled release bundle and installer documentation;
5. reconcile README, privacy, security, website and changelog wording;
6. keep publication blocked when terms, reviewed notices, production signing or release authorization are absent;
7. repeat clean-install/package validation against the final signed candidate.

## Release boundary

External design-partner distribution remains blocked until either:

- approved written private-beta evaluation terms cover the participants and package; or
- the complete owner-approved product/source licensing path is implemented.

Internal or operator-controlled evaluation does not close this blocker and does not grant production, customer-data or commercial authority.

CI and documentation must continue to report public release as `NO-GO` until the approved terms, reviewed notice set, production signing identity and controlled release decision exist.
