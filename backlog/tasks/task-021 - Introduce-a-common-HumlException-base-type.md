---
id: TASK-021
title: Introduce a common HumlException base type
status: To Do
assignee: []
created_date: '2026-07-07 08:10'
labels:
  - api
  - stj-parity
milestone: m-1
dependencies: []
priority: medium
ordinal: 11000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
From the 2026-07-07 architecture review. The library ships four unrelated sealed exception types (HumlParseException, HumlSerializeException, HumlDeserializeException, HumlVersionException — each sealed : Exception), so consumers cannot write catch (HumlException) to handle all library failures, unlike most serialisation libraries. Adding a shared abstract base is source- and binary-additive now (existing catch sites keep working) but awkward after 1.0 when the hierarchy is fully frozen. Justify the change per docs/internals/api-freeze.md (additive).
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [ ] #1 An abstract HumlException base exists and all four public exception types derive from it
- [ ] #2 Existing catch sites for the concrete types continue to compile and pass (backward compatibility test)
- [ ] #3 A test demonstrates catch (HumlException) catches parse, serialise, deserialise, and version failures
- [ ] #4 XML docs and the docs site exception guidance updated
- [ ] #5 Additive API change justified in writing per docs/internals/api-freeze.md
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
