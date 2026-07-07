---
id: TASK-003
title: >-
  Adopt Microsoft.CodeAnalysis.PublicApiAnalyzers to enforce the API freeze in
  CI
status: Done
assignee:
  - Claude
created_date: '2026-06-12 23:28'
updated_date: '2026-07-07 19:03'
labels:
  - api-freeze
  - ci
milestone: m-0
dependencies: []
documentation:
  - docs/internals/api-freeze.md
  - src/Huml.Net/PublicAPI/
priority: medium
ordinal: 3000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
The public API of Huml.Net is frozen (docs/internals/api-freeze.md) with a hand-maintained baseline in docs/public-api.txt. The freeze policy itself names Microsoft.CodeAnalysis.PublicApiAnalyzers as the durable long-term mechanism to adopt post-beta. Wire the analyzer into src/Huml.Net (PublicAPI.Shipped.txt seeded from the current 0.2.0-beta.1 surface, PublicAPI.Unshipped.txt for additive changes) so any unreviewed public-surface change fails the build rather than relying on manual diffing. Decide and document how docs/public-api.txt relates to the analyzer files afterwards (keep as human-readable mirror, or retire it).
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [x] #1 PublicApiAnalyzers is referenced PrivateAssets=All in src/Huml.Net (no runtime dependency added) and PublicAPI.Shipped.txt matches the published 0.2.0-beta.1 surface
- [x] #2 An undeclared public API change fails dotnet build locally and in CI (demonstrated, then reverted)
- [x] #3 Multi-TFM surface differences (netstandard2.1 vs net8/9/10) are handled correctly
- [x] #4 docs/internals/api-freeze.md is updated to describe the automated enforcement and the role (or retirement) of docs/public-api.txt
<!-- AC:END -->

## Implementation Plan

<!-- SECTION:PLAN:BEGIN -->
## Implementation Plan

**Approach:** Microsoft.CodeAnalysis.PublicApiAnalyzers 5.6.0 (latest stable) referenced `PrivateAssets="All"` in src/Huml.Net — analyzer-only, no runtime dependency. All TFM-conditional code in src/Huml.Net (IsExternalInit, TrimShims, RequiredMemberAttribute, DateOnly/TimeOnly branches) is `internal`, so the public surface is expected TFM-invariant → a single shared PublicAPI.Shipped.txt/PublicAPI.Unshipped.txt pair serves all four TFMs; the multi-TFM build itself is the proof (RS0016 'undeclared API' / RS0017 'declared but missing' would fail any TFM whose surface diverges) — satisfies AC #3.

**Steps:**
1. Add the package reference + `<AdditionalFiles>` for PublicAPI.Shipped.txt / PublicAPI.Unshipped.txt (both seeded with `#nullable enable`).
2. Seed Shipped.txt from the CURRENT surface (= published 0.2.0-beta.2; beta.2's additions were additive and already shipped, so the baseline is the beta.2 surface, superset of beta.1) using `dotnet format analyzers --diagnostics RS0016` to auto-apply the DeclarePublicApiFix, then move all entries Unshipped → Shipped. Handle any RS0026/RS0027 (overloads-with-optional-parameters) findings via .editorconfig with a documented rationale if they fire on the existing frozen API.
3. Verify zero-warning build on all four TFMs (analyzer enforcement piggy-backs on TreatWarningsAsErrors, so existing CI `dotnet build` enforces automatically — CI wiring needs no workflow change; document this).
4. AC #2 demonstration: add a public member locally → confirm dotnet build fails with RS0016; push the same change on a throwaway branch to let CI fail (if CI triggers on non-main branches), then revert/delete the branch.
5. AC #4: update docs/internals/api-freeze.md — automated enforcement section (workflow: additive change ⇒ entry in PublicAPI.Unshipped.txt + justification; release ⇒ roll Unshipped into Shipped) and RETIRE docs/public-api.txt (delete, with Shipped.txt as the single source of truth; update references).
6. CHANGELOG [Unreleased] note (Added/infrastructure), full suite green, commit `chore: ...` + push.

**Scope notes:** No product code changes; no public API change (DoD #4 n/a). No parse behaviour changes (DoD #6 n/a).
<!-- SECTION:PLAN:END -->

## Implementation Notes

<!-- SECTION:NOTES:BEGIN -->
Seeding: dotnet format analyzers --diagnostics RS0016 applied the analyzer's own DeclarePublicApiFix, producing a 330-line baseline. The first multi-TFM build then failed with RS0016/RS0017 pairs on the five HumlNode-derived records: their synthesized <Clone>$ methods use covariant returns on net8.0+ (-> HumlDocument!) but return the base type on netstandard2.1 (no covariant-return support). Resolved with per-TFM baselines: src/Huml.Net/PublicAPI/netstandard2.1/ and src/Huml.Net/PublicAPI/net/ (shared by net8.0/net9.0/net10.0), wired via conditional AdditionalFiles ItemGroups. This is the AC #3 multi-TFM handling, proven by the 4-TFM build.

AC #2 local demonstration: adding public static int UndeclaredApiDemo() to HumlSerializer failed dotnet build with 'error RS0016: Symbol ... is not part of the declared public API' (TreatWarningsAsErrors elevates the warning). Reverted. CI demonstration: same change pushed on throwaway branch demo/rs0016-ci-check (CI triggers on push to any branch); awaiting the expected red run before deleting the branch. Decision recorded in api-freeze.md: docs/public-api.txt RETIRED (deleted; git history preserves it) - baselines cover all TFMs, capture nullability, and are enforced mechanically. References updated in AGENTS.md, docfx.json, docs/documentation-plan.md. Main commit: 6618bb8.

CI demonstration complete: run 28891269950 on demo/rs0016-ci-check failed with 'error RS0016: Symbol static Huml.Net.HumlSerializer.UndeclaredApiDemo() -> int is not part of the declared public API' on every TFM; main run for 6618bb8 completed green. Throwaway branch deleted (remote + local). dotnet pack -c Release also verified successful post-adoption. Task Documentation field updated: docs/public-api.txt reference replaced with src/Huml.Net/PublicAPI/ (the file is retired).
<!-- SECTION:NOTES:END -->

## Final Summary

<!-- SECTION:FINAL_SUMMARY:BEGIN -->
## Adopt Microsoft.CodeAnalysis.PublicApiAnalyzers for API-freeze enforcement

**What changed:** `Microsoft.CodeAnalysis.PublicApiAnalyzers` 5.6.0 is referenced `PrivateAssets="All"` in src/Huml.Net (analyzer-only — the package keeps zero runtime dependencies). The public surface is baselined in per-TFM files under `src/Huml.Net/PublicAPI/` (`netstandard2.1/` and `net/`, the latter shared by net8.0/net9.0/net10.0), seeded via the analyzer's own DeclarePublicApiFix from the current surface — the published 0.2.0-beta.2, a strict additive superset of the frozen 0.2.0-beta.1 surface. With `TreatWarningsAsErrors` on, RS0016 (undeclared addition) and RS0017 (removal/rename) fail `dotnet build` locally and in CI with no workflow changes.

**Multi-TFM (AC #3):** the seeding build itself exposed the one real TFM divergence — record `<Clone>$` methods use covariant returns on net8.0+ (`-> HumlDocument!`) but return the base type on netstandard2.1. Handled with the per-TFM baseline split; the 4-TFM build enforces each flavour.

**Demonstrations (AC #2):** an `UndeclaredApiDemo()` public member failed the local build with RS0016 (reverted), and the same change on throwaway branch `demo/rs0016-ci-check` failed CI run 28891269950 with RS0016 on every TFM (branch deleted). Main stayed green.

**Docs (AC #4):** docs/internals/api-freeze.md now documents the enforcement, the per-TFM layout, the additive-change workflow (declare in Unshipped + justification; roll into Shipped at release), and the retirement of `docs/public-api.txt` (deleted — superseded by the baselines; references updated in AGENTS.md, docfx.json, docs/documentation-plan.md). CHANGELOG `[Unreleased]` notes the adoption.

**Verification:** zero-warning full rebuild, 1342 tests green × 3 frameworks, `dotnet pack -c Release` succeeds. Commit 6618bb8 on main.
<!-- SECTION:FINAL_SUMMARY:END -->

## Definition of Done
<!-- DOD:BEGIN -->
- [x] #1 dotnet build succeeds with zero warnings (TreatWarningsAsErrors is on; full rebuild to surface cached analyzer results)
- [x] #2 dotnet test green on net8.0 / net9.0 / net10.0
- [x] #3 CHANGELOG.md [Unreleased] section updated for every user-visible change
- [x] #4 Any public API change is additive and justified in writing per docs/internals/api-freeze.md
- [x] #5 New/changed public members have XML docs; tests use AwesomeAssertions (never FluentAssertions)
- [x] #6 New error-or-no-error parse behaviours assessed against .claude/rules/fixture-gaps.md and staged in fixtures/extensions/ when language-agnostic
<!-- DOD:END -->
