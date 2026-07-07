---
id: TASK-005
title: Design a coercion policy for lossy numeric conversions (G3 findings M4 and M5)
status: To Do
assignee: []
created_date: '2026-06-12 23:28'
updated_date: '2026-07-07 08:12'
labels:
  - deserializer
  - design
milestone: m-1
dependencies: []
documentation:
  - docs/internals/g3-security-review.md
priority: medium
ordinal: 1000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
Deferred from the G3 adversarial review (docs/internals/g3-security-review.md). Two related defects in HumlDeserializer's scalar coercion: (M4) out-of-range integers silently truncate when coerced into enums with narrow backing types; (M5) Convert.ChangeType silently performs lossy conversions — float→int rounds, int→bool, bool→string. STJ's approach (strict by default, opt-in via NumberHandling) is the design reference; Huml.Net already has HumlNumberHandling, so the question is whether strictness becomes the default (a behaviour break — needs a major/minor decision against the frozen API) or an opt-in HumlOptions switch. Produce a short written design first (options, default, migration), agree it with Richard, then implement TDD-style. This is a behaviour change: it must NOT ship on the 0.2.0 bug-fix beta line.
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [ ] #1 A written design (docs/plans/ or docs/internals/) covers the strictness default, the opt-in/opt-out switch, and interaction with existing HumlNumberHandling
- [ ] #2 Richard has approved the design before implementation starts
- [ ] #3 Out-of-range enum coercion and lossy ChangeType conversions behave per the agreed policy, with tests for each conversion class (float-to-int, int-to-bool, bool-to-string, narrow enum)
- [ ] #4 Error messages name the source value, target type, and the option that would permit the coercion
- [ ] #5 docs/error-handling.md (or equivalent guide) documents the policy
- [ ] #6 Bool-to-numeric/string coercion via Convert.ChangeType is gated or rejected per the designed policy (Deserialize<int>("true") must not silently return 1)
- [ ] #7 decimal round-trip behaviour is defined by the policy: out-of-range/high-precision decimals either round-trip or fail loudly at serialise time, never producing output the parser rejects
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

## Comments

<!-- COMMENTS:BEGIN -->
author: comprehensive-review-2026-07-07
created: 2026-07-07 08:12
---
2026-07-07 comprehensive review found two additional coercion behaviours that belong in this policy design: (1) Bool scalars are silently coerced to numeric/string targets via the ungated Convert.ChangeType fallback in CoerceScalar (src/Huml.Net/Serialization/HumlDeserializer.cs:792-795) — verified: Deserialize<int>("true") returns 1 and Deserialize<string>("true") returns "True"; STJ rejects both, and unlike string-to-number this is not even gated behind HumlNumberHandling.AllowReadingFromString. (2) decimal values outside double/Int64 range fail to round-trip through the library's own output — decimal.MaxValue serialises to a literal the parser rejects with an int64-overflow error, and high-precision decimals silently truncate (1.0000000000000000000000001m round-trips as 1), a consequence of the parser modelling all ints as Int64 and all floats as double. Two acceptance criteria added to cover these.
---
<!-- COMMENTS:END -->
