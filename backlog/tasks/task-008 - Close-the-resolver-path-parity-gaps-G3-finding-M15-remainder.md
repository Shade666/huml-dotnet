---
id: TASK-008
title: Close the resolver-path parity gaps (G3 finding M15 remainder)
status: To Do
assignee: []
created_date: '2026-06-12 23:28'
labels:
  - source-generator
  - deserializer
milestone: m-1
dependencies: []
documentation:
  - docs/internals/g3-security-review.md
  - docs/source-generator.md
priority: medium
ordinal: 4000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
Partially fixed in G3 (case-sensitivity was aligned); the remainder is tracked for post-beta (docs/internals/g3-security-review.md, M15). When deserialisation goes through an IHumlTypeInfoResolver / HumlGeneratedContext (the source-generated fast path), it currently skips checks the reflection path enforces: required-member validation, extension-data handling, UnmappedMemberHandling.Disallow, and DefaultIgnoreCondition. The path is opt-in and documented as a fast path, but the gaps mean the source-generated and reflection paths can accept different documents for the same type — surprising for consumers who add the generator for performance. Decide per-check whether to enforce in the resolver path (preferred where the metadata exists on HumlTypeInfo) or to document the difference explicitly; required-member validation is the highest-value gap since [required] silently not enforced is a correctness trap.
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [ ] #1 Required-member validation behaves identically on the reflection and resolver paths (or the divergence is rejected with written rationale)
- [ ] #2 UnmappedMemberHandling.Disallow, extension data, and DefaultIgnoreCondition are each either enforced on the resolver path or explicitly documented as reflection-only
- [ ] #3 A parity test fixture deserialises the same documents through both paths and asserts identical outcomes for the enforced checks
- [ ] #4 docs/source-generator.md documents any remaining intentional differences
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
