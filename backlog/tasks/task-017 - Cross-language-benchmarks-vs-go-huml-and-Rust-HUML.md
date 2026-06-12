---
id: TASK-017
title: Cross-language benchmarks vs go-huml and Rust HUML
status: To Do
assignee: []
created_date: '2026-06-12 23:30'
labels:
  - benchmarks
  - examples-repo
milestone: m-3
dependencies: []
documentation:
  - docs/plans/2026-06-10-beta-release-goals.md
priority: low
ordinal: 4000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
Post-beta stretch goal from docs/plans/2026-06-10-beta-release-goals.md: extend the benchmark story beyond STJ to other HUML implementations — go-huml (the reference) and a Rust HUML implementation if a maintained one exists (verify first; if none exists, scope to Go only). This needs a multi-toolchain harness: the shared datasets already exist in huml-dotnet-examples/datasets/ (HUML+JSON pairs at several sizes), so each language benchmarks parse/serialise over the same files. A Docker-based harness or a GitHub Actions matrix keeps toolchains reproducible. Cross-language timing comparisons are methodologically delicate (different harnesses, GC vs no GC) — present results as orders of magnitude with honest caveats, in the same spirit as the existing RESULTS.md commentary. Work lands in huml-dotnet-examples/benchmarks.
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [ ] #1 Existence and maintenance status of a Rust HUML implementation is verified and the scope decision recorded
- [ ] #2 go-huml (and Rust if viable) parse the shared datasets in a reproducible harness (Docker or CI matrix) using each language's idiomatic benchmark tooling
- [ ] #3 Results published alongside RESULTS.md with explicit methodology caveats about cross-runtime comparison
- [ ] #4 Harness is runnable locally with one documented command
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
