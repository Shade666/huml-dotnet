---
id: TASK-005
title: Design a coercion policy for lossy numeric conversions (G3 findings M4 and M5)
status: To Do
assignee: []
created_date: '2026-06-12 23:28'
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
