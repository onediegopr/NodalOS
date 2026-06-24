# M798 Denylist Enforcement

M798 defines denylist-first routing enforcement.

Denylisted capabilities include provider/cloud live calls, provider credential use, filesystem writes, browser automation, credential/CAPTCHA/2FA bypass, capability unlock, public release, Chrome Web Store submission, signed public ZIP creation, product file modification, Bridge/CSP modification, and `productive_enabled`.

Every denylisted route returns `DENY`, selects no executor, emits audit/evidence/ledger/no-execution proof, and keeps all real execution flags false.
