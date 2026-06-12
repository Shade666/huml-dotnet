---
id: TASK-003
title: >-
  Adopt Microsoft.CodeAnalysis.PublicApiAnalyzers to enforce the API freeze in
  CI
status: To Do
assignee: []
created_date: '2026-06-12 23:28'
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

## Definition of Done
<!-- DOD:BEGIN -->
- [ ] #1 dotnet build succeeds with zero warnings (TreatWarningsAsErrors is on; full rebuild to surface cached analyzer results)
- [ ] #2 dotnet test green on net8.0 / net9.0 / net10.0
- [ ] #3 CHANGELOG.md [Unreleased] section updated for every user-visible change
- [ ] #4 Any public API change is additive and justified in writing per docs/internals/api-freeze.md
- [ ] #5 New/changed public members have XML docs; tests use AwesomeAssertions (never FluentAssertions)
- [ ] #6 New error-or-no-error parse behaviours assessed against .claude/rules/fixture-gaps.md and staged in fixtures/extensions/ when language-agnostic
<!-- DOD:END -->
