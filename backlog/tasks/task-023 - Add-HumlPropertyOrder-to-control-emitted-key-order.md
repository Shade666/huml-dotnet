---
id: TASK-023
title: 'Add [HumlPropertyOrder] to control emitted key order'
status: To Do
assignee: []
created_date: '2026-07-07 08:10'
labels:
  - serializer
  - stj-parity
milestone: m-1
dependencies: []
priority: medium
ordinal: 13000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
STJ parity gap (2026-07-07 audit). Emission order is fixed to base-class-first declaration order via MetadataToken (src/Huml.Net/Serialization/PropertyDescriptor.cs:134) with no override. For a format whose selling point is human readability, authors often want important keys first regardless of declaration position. Mirror [JsonPropertyOrder]: an int Order (default 0), stable-sorted so equal orders preserve declaration order. The Order concept already exists on the resolver seam (HumlPropertyInfo), so this also closes a reflection-vs-resolver metadata gap — coordinate with the binding-metadata unification task so both paths honour it.
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [ ] #1 [HumlPropertyOrder(int)] attribute exists and reorders emitted keys via a stable sort (ties keep declaration order)
- [ ] #2 Default behaviour without the attribute is unchanged (declaration order, base-first)
- [ ] #3 The source-generated/resolver path honours Order identically to the reflection path
- [ ] #4 Serialisation order tests cover negative, zero, and positive orders across an inheritance chain
- [ ] #5 docs updated
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
