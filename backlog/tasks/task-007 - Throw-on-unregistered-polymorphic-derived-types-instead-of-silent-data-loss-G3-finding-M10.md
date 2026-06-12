---
id: TASK-007
title: >-
  Throw on unregistered polymorphic derived types instead of silent data loss
  (G3 finding M10)
status: To Do
assignee: []
created_date: '2026-06-12 23:28'
labels:
  - serializer
  - polymorphism
milestone: m-1
dependencies: []
documentation:
  - docs/internals/g3-security-review.md
priority: medium
ordinal: 3000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
Deferred from the G3 review (docs/internals/g3-security-review.md, M10): when a [HumlPolymorphic] base type holds a runtime instance whose concrete type has no [HumlDerivedType] registration, the serialiser currently emits it with NO discriminator — the document round-trips as the base type and the concrete-type information is silently lost. STJ throws NotSupportedException in this situation. Align: throw HumlSerializeException by default, with an options switch to restore the lenient emit for consumers who relied on it (the G3 disposition notes this needs an options switch because the current behaviour is documented). Behaviour change — must not ship on the 0.2.0 bug-fix beta line; coordinate the default with the coercion-policy task's strictness decisions so the options surface stays coherent.
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [ ] #1 Serialising an unregistered derived instance under a [HumlPolymorphic] base throws HumlSerializeException naming the unregistered type by default
- [ ] #2 An options switch restores the previous discriminator-less emit, and is documented
- [ ] #3 Nested and collection positions behave identically to the top level (the H6 fix threads declared types through both)
- [ ] #4 The polymorphism guide documents the new default and the escape hatch
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
