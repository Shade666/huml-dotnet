---
id: TASK-016
title: Add a scheduled CI fuzzing job
status: To Do
assignee: []
created_date: '2026-06-12 23:30'
labels:
  - ci
  - fuzzing
  - security
milestone: m-3
dependencies: []
documentation:
  - docs/plans/2026-06-10-beta-release-goals.md
  - tools/Huml.Net.Fuzz
priority: low
ordinal: 3000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
Post-beta stretch goal from docs/plans/2026-06-10-beta-release-goals.md (continuous fuzzing was explicitly excluded from the beta gate by Richard's scoping decision — a one-off 8M-iteration campaign ran clean instead). The deterministic corpus-seeded mutation fuzzer already exists at tools/Huml.Net.Fuzz. Wrap it in a scheduled GitHub Actions workflow (e.g. weekly cron) that runs a bounded iteration count against HumlSerializer.Parse, fails loudly on any crash/hang with the reproducing input in the job output, and uploads the failing case as an artifact. Keep runtime within free-runner limits (pick an iteration budget from the observed ~iterations/minute of the 8M campaign). Consider persisting/growing the corpus between runs via actions/cache.
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [ ] #1 A scheduled workflow runs the fuzzer on a cron with a bounded iteration budget that fits runner limits
- [ ] #2 A seeded crash (deliberately introduced locally) demonstrably fails the job and surfaces the reproducing input, then is reverted
- [ ] #3 Failures upload the reproducing input as a workflow artifact
- [ ] #4 The workflow can also be triggered manually via workflow_dispatch
- [ ] #5 README or docs/internals notes the continuous-fuzzing setup
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
