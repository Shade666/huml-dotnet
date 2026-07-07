---
id: TASK-004
title: Run the beta feedback window and ship 0.2.0 stable
status: To Do
assignee: []
created_date: '2026-06-12 23:28'
updated_date: '2026-07-07 20:08'
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

## Comments

<!-- COMMENTS:BEGIN -->
author: comprehensive-review-2026-07-07
created: 2026-07-07 08:12
---
2026-07-07 comprehensive review note: 0.2.0-beta.2 shipped on 2026-07-01 (HumlConverterFactory + Nullable<T> auto-unwrap), so the beta soak now covers beta.2. Two routine dependabot PRs are open and should be merged before cutting stable: #31 (nuget-minor-patch group) and #28 (actions-all group). Additionally, TASK-019 (duplicate serialisation of overridden/shadowed properties — verified round-trip-breaking bug) has been raised into m-0 and should gate the stable release.
---

author: Claude
created: 2026-07-07 19:05
---
**0.2.0-rc.1 is staged and awaiting the tag decision** (2026-07-07, autonomous m-0 run).

Completed this run:
- **TASK-019 Done** (commit 0339281): overridden/`new`-shadowed properties now serialise exactly once, derived-most declaration wins; the source generator received the matching fix for shadowed properties (it silently read the base value); reflection vs source-gen parity is pinned by test. 13 new regression tests.
- **Dependabot PRs merged**: #28 (actions-all) and #31 (Meziantou.Analyzer 3.0.117, Test.Sdk 18.7.0) — both squash-merged with green checks, full suite re-verified locally afterwards. No blockages to record.
- **TASK-003 Done** (commit 6618bb8): PublicApiAnalyzers enforces the API freeze at build time against per-TFM baselines in src/Huml.Net/PublicAPI/; RS0016 failure demonstrated locally and in CI (run 28891269950 on a throwaway branch, since deleted); docs/public-api.txt retired.
- **RC readiness verified** (commit 4671d98): zero-warning full rebuild; 1342 tests green on net8.0/net9.0/net10.0; `dotnet pack -c Release` succeeds; CHANGELOG [Unreleased] rolled to [0.2.0-rc.1] - 2026-07-07 with fresh [Unreleased]; draft release notes in docs/plans/2026-07-07-0.2.0-rc.1-release-notes.md.

**Deliberately NOT done (needs explicit approval):** pushing the `v0.2.0-rc.1` tag (publish.yml auto-publishes to NuGet.org via OIDC on tag push), publishing any package, or creating a GitHub Release. The release-notes draft includes the exact mechanics for when the tag decision is made. TASK-005 and all m-1+ work were excluded per the 0.2.x bug-fix-only policy.
---

author: Claude
created: 2026-07-07 20:05
---
**0.2.0-rc.1 RELEASED** (2026-07-07, with Richard's explicit approval).

- Tag `v0.2.0-rc.1` pushed at main HEAD (0d30d00, CI green).
- Publish workflow run 28894954547 succeeded: **both** packages pushed to NuGet.org via OIDC — `Huml.Net.0.2.0-rc.1` and `Huml.Net.SourceGeneration.0.2.0-rc.1` (HTTP Created on both).
- GitHub pre-release live: https://github.com/primeBeri/huml-dotnet/releases/tag/v0.2.0-rc.1 (marked pre-release, notes from the docs/plans draft).
- Release-notes doc updated to RELEASED status (commit 02d55c3). NuGet indexing verification in progress; examples-repo CI against the published rc.1 remains outstanding (AC #4 applies to stable 0.2.0 but a soak run against rc.1 is recommended).
---

author: Claude
created: 2026-07-07 20:08
---
NuGet indexing verified: both `Huml.Net 0.2.0-rc.1` and `Huml.Net.SourceGeneration 0.2.0-rc.1` are now listed in the public flat-container index. Release mechanics for rc.1 are fully complete. Remaining before stable 0.2.0: RC soak over the feedback window, examples-repo CI against the published rc.1, then the non-prerelease v0.2.0 tag/release with README/docs beta-caveat removal.
---
<!-- COMMENTS:END -->
