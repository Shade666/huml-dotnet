---
id: TASK-011
title: Low-severity hardening batch from the G3 review raw findings
status: To Do
assignee: []
created_date: '2026-06-12 23:29'
labels:
  - hardening
  - source-generator
  - serializer
milestone: m-1
dependencies: []
documentation:
  - docs/internals/g3-security-review.md
  - docs/internals/g3-review-raw.json
priority: low
ordinal: 7000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
Six low-severity findings were recorded in docs/internals/g3-review-raw.json and explicitly tracked as post-beta hardening (docs/internals/g3-security-review.md, final section): struct property with throwing constructor, misregistered [HumlDerivedType] surfacing InvalidCastException instead of a Huml exception, unescaped type/property names sourced from referenced assemblies in generator output, and silently-dropped invalid registrations. None are reachable from untrusted document input — all require the consumer to mis-declare their own types — which is why they did not gate the beta. Work through the raw findings file, fix each or record a won't-fix rationale, ensuring every consumer mis-declaration surfaces as a Huml exception type or a Roslyn diagnostic rather than a raw CLR exception or silent no-op.
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [ ] #1 Every low finding in docs/internals/g3-review-raw.json is either fixed with a regression test or marked won't-fix with rationale recorded in g3-security-review.md
- [ ] #2 No consumer mis-declaration scenario from the findings surfaces a raw CLR exception (InvalidCastException, TargetInvocationException) or silently does nothing
- [ ] #3 g3-security-review.md status table is updated to reflect final dispositions
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
