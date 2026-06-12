---
id: TASK-006
title: Support string-to-Guid/Uri/Version coercion on deserialise (G3 finding M8)
status: To Do
assignee: []
created_date: '2026-06-12 23:28'
labels:
  - deserializer
  - serializer
milestone: m-1
dependencies: []
documentation:
  - docs/internals/g3-security-review.md
  - docs/date-time.md
priority: medium
ordinal: 2000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
Deferred from the G3 review (docs/internals/g3-security-review.md, M8): deserialising a HUML string scalar into Guid fails because Guid is not IConvertible and the coercion path only uses Convert.ChangeType; a code comment claimed Guid support existed (comment was corrected during G3, behaviour was not added). Add first-class string→Guid, string→Uri, and string→Version handling in the deserialiser's scalar coercion (mirroring how DateTime/DateTimeOffset/TimeSpan/DateOnly/TimeOnly already get dedicated handling per docs/date-time.md), plus the corresponding serialise direction so the types round-trip. Additive behaviour — safe for a 0.2.x line.
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [ ] #1 Guid, Uri, and Version properties round-trip through Serialize then Deserialize
- [ ] #2 Nullable variants (Guid?, etc.) and these types as dictionary keys and collection elements work
- [ ] #3 A malformed value (e.g. not-a-guid) throws HumlDeserializeException naming the target type, not a raw FormatException
- [ ] #4 Source-generated path handles the three types the same as the reflection path
- [ ] #5 Docs mention the supported scalar types (extend docs/date-time.md or the attributes/types reference)
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
