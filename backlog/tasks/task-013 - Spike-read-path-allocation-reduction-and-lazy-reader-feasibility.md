---
id: TASK-013
title: 'Spike: read-path allocation reduction and lazy-reader feasibility'
status: To Do
assignee: []
created_date: '2026-06-12 23:29'
labels:
  - performance
  - spike
  - design
milestone: m-2
dependencies: []
documentation:
  - docs/plans/2026-06-10-beta-release-goals.md
priority: low
ordinal: 2000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
Investigation task (output is a written recommendation, not shipped code). The benchmark results (huml-dotnet-examples/benchmarks/RESULTS.md) show deserialise at ~3.5x STJ time and ~4.7x allocations, dominated by the eager HumlDocument AST plus UTF-16 string materialisation — every parse builds full immutable nodes with positions even when the consumer only wants a POCO. Investigate: (a) cheap wins inside the current architecture (string interning for repeated keys, pooled buffers, span-based scalar parsing without intermediate strings); (b) the cost/benefit of a lazy or forward-only reader (HumlDocument.Parse-equivalent of JsonDocument's deferred materialisation, or a Utf8JsonReader-style struct reader feeding the deserialiser directly, bypassing the AST). A streaming/lazy reader was explicitly declared out of scope for the beta and IS a standing anti-feature question — the spike must produce a recommendation Richard can accept or reject before any implementation is planned, since it could be a large architectural change.
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [ ] #1 A written report quantifies where deserialise time/allocations go (profiler or allocation-probe evidence, not estimates)
- [ ] #2 Cheap in-architecture wins are listed with estimated impact, and any no-risk ones are implemented with before/after benchmark numbers
- [ ] #3 The lazy/forward-only reader option has a clear recommendation (pursue / defer / reject) with API-shape sketch and effort estimate
- [ ] #4 Richard has reviewed the recommendation; follow-up implementation tasks are created only if he accepts
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
