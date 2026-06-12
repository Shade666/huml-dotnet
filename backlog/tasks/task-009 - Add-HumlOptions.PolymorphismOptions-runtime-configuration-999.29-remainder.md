---
id: TASK-009
title: Add HumlOptions.PolymorphismOptions runtime configuration (999.29 remainder)
status: To Do
assignee: []
created_date: '2026-06-12 23:29'
updated_date: '2026-06-12 23:30'
labels:
  - polymorphism
  - design
  - api
milestone: m-1
dependencies:
  - TASK-007
documentation:
  - docs/plans/2026-06-10-backlog-disposition.md
  - docs/internals/api-freeze.md
priority: low
ordinal: 5000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
The deferred remainder of backlog item 999.29 (docs/plans/2026-06-10-backlog-disposition.md): polymorphism is currently configured only via [HumlPolymorphic]/[HumlDerivedType] attributes on the base type. STJ also offers runtime configuration (JsonPolymorphismOptions via the type-info resolver) for cases where the consumer cannot annotate the base type — third-party types, plugin architectures, or per-options discriminator naming. Design a HumlOptions-level (or HumlTypeInfo-level) equivalent: register derived types, discriminator key name, and unknown-derived-type handling at runtime, composing with the existing attribute model (attributes win or merge — decide and document). The disposition deferred this because the design surface is large; treat the design document as the first deliverable and review it against the frozen API before writing code. Coordinate with the M10 unregistered-derived-type task (task-007) so the options shape is decided once.
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [ ] #1 A written design covers the registration API, precedence vs attributes, and interaction with HumlUnknownDerivedTypeHandling, reviewed before implementation
- [ ] #2 Runtime-registered polymorphism round-trips for a base type with no attributes at all
- [ ] #3 Attribute-configured and runtime-configured polymorphism compose per the documented precedence
- [ ] #4 The source-generated path either supports runtime registration or documents that it is reflection-only
- [ ] #5 The polymorphism guide documents the new API with an example
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
