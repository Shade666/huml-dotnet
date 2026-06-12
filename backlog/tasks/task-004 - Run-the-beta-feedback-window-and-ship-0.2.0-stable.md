---
id: TASK-004
title: Run the beta feedback window and ship 0.2.0 stable
status: To Do
assignee: []
created_date: '2026-06-12 23:28'
updated_date: '2026-06-12 23:30'
labels:
  - release
milestone: m-0
dependencies:
  - TASK-001
  - TASK-002
  - TASK-003
documentation:
  - docs/versioning.md
  - docs/plans/2026-06-10-beta-release-goals.md
priority: medium
ordinal: 4000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
0.2.0-beta.1 shipped to NuGet.org on 2026-06-13. Per docs/versioning.md, beta means feature-complete and stabilising — only bug fixes accepted on this line. Define a feedback window (suggest 4-6 weeks), monitor NuGet download stats and GitHub issues, fix reported bugs as beta.2+ patches if needed, then promote to stable 0.2.0. The release mechanics mirror the beta: roll CHANGELOG [Unreleased] to [0.2.0] with a fresh [Unreleased] inserted, tag v0.2.0 (publish.yml publishes via OIDC on tag push), create the GitHub Release (not pre-release), verify NuGet indexing, and run the examples repo CI against the published 0.2.0. Update the README beta caveat when stable lands.
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [ ] #1 A feedback-window end date is agreed with Richard and recorded on this task
- [ ] #2 All bugs reported during the window are fixed or explicitly deferred with rationale
- [ ] #3 CHANGELOG rolled, v0.2.0 tagged, both packages live on NuGet.org, GitHub Release created (non-prerelease)
- [ ] #4 Examples repo CI green against the published 0.2.0
- [ ] #5 README and docs site no longer describe the package as beta
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
