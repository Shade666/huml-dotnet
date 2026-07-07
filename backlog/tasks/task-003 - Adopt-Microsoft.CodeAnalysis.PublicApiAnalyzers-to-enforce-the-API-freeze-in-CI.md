---
id: TASK-003
title: >-
  Adopt Microsoft.CodeAnalysis.PublicApiAnalyzers to enforce the API freeze in
  CI
status: In Progress
assignee:
  - Claude
created_date: '2026-06-12 23:28'
updated_date: '2026-07-07 18:55'
labels:
  - api-freeze
  - ci
milestone: m-0
dependencies: []
documentation:
  - docs/internals/api-freeze.md
  - docs/public-api.txt
priority: medium
ordinal: 3000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
The public API of Huml.Net is frozen (docs/internals/api-freeze.md) with a hand-maintained baseline in docs/public-api.txt. The freeze policy itself names Microsoft.CodeAnalysis.PublicApiAnalyzers as the durable long-term mechanism to adopt post-beta. Wire the analyzer into src/Huml.Net (PublicAPI.Shipped.txt seeded from the current 0.2.0-beta.1 surface, PublicAPI.Unshipped.txt for additive changes) so any unreviewed public-surface change fails the build rather than relying on manual diffing. Decide and document how docs/public-api.txt relates to the analyzer files afterwards (keep as human-readable mirror, or retire it).
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [ ] #1 PublicApiAnalyzers is referenced PrivateAssets=All in src/Huml.Net (no runtime dependency added) and PublicAPI.Shipped.txt matches the published 0.2.0-beta.1 surface
- [ ] #2 An undeclared public API change fails dotnet build locally and in CI (demonstrated, then reverted)
- [ ] #3 Multi-TFM surface differences (netstandard2.1 vs net8/9/10) are handled correctly
- [ ] #4 docs/internals/api-freeze.md is updated to describe the automated enforcement and the role (or retirement) of docs/public-api.txt
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

## Definition of Done
<!-- DOD:BEGIN -->
- [ ] #1 dotnet build succeeds with zero warnings (TreatWarningsAsErrors is on; full rebuild to surface cached analyzer results)
- [ ] #2 dotnet test green on net8.0 / net9.0 / net10.0
- [ ] #3 CHANGELOG.md [Unreleased] section updated for every user-visible change
- [ ] #4 Any public API change is additive and justified in writing per docs/internals/api-freeze.md
- [ ] #5 New/changed public members have XML docs; tests use AwesomeAssertions (never FluentAssertions)
- [ ] #6 New error-or-no-error parse behaviours assessed against .claude/rules/fixture-gaps.md and staged in fixtures/extensions/ when language-agnostic
<!-- DOD:END -->
