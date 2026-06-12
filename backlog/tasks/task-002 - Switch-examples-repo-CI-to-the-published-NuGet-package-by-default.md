---
id: TASK-002
title: Switch examples repo CI to the published NuGet package by default
status: Done
assignee:
  - claude
created_date: '2026-06-12 23:27'
updated_date: '2026-06-12 23:47'
labels:
  - ci
  - examples-repo
milestone: m-0
dependencies: []
references:
  - >-
    https://github.com/primeBeri/huml-dotnet-examples/blob/main/.github/workflows/examples.yml
priority: medium
ordinal: 2000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
Now that Huml.Net 0.2.0-beta.1 and Huml.Net.SourceGeneration 0.2.0-beta.1 are live on NuGet.org, the huml-dotnet-examples repo (primeBeri/huml-dotnet-examples) should restore from nuget.org by default instead of packing the main repo into local-feed/ on every push/PR run. The workflow's own comment says to do this once the beta is published. Invert the mode: default = published package (currently requires the workflow_dispatch huml_version input); keep a pack-from-source mode available (e.g. via the dispatch input) for pre-release testing of unreleased changes. Also bump the repo's actions/checkout@v4 and actions/setup-dotnet@v4 to Node-24-safe major versions — this repo has no dependabot config, so consider adding one. Cross-repo task: work happens in huml-dotnet-examples, not this repo.
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [x] #1 Push/PR runs of examples.yml restore Huml.Net from nuget.org without checking out or packing the main repo
- [x] #2 A pack-from-source mode remains available for testing unreleased main-repo changes, and is documented in the workflow or README
- [x] #3 run-examples.ps1 default HumlNetVersion matches the latest published version
- [x] #4 actions/checkout and actions/setup-dotnet are bumped to Node-24-safe versions
- [x] #5 A dependabot (or equivalent) config keeps the examples repo's actions and packages updated
- [x] #6 CI is green in both modes after the change
<!-- AC:END -->

## Implementation Plan

<!-- SECTION:PLAN:BEGIN -->
Investigation findings: Directory.Build.props already defaults HumlNetVersion to 0.2.0-beta.1 (= latest published), so default-mode restore from nuget.org needs no version plumbing. One subtlety: now that 0.2.0-beta.1 exists on nuget.org, a pack-from-source run that packs the SAME version into local-feed would race the two sources — the pack mode must use a distinct version so restore is deterministic.

Plan (all in huml-dotnet-examples):
1. Rework .github/workflows/examples.yml:
   - Default (push / PR / dispatch without flag): no main-repo checkout, no pack; restore Huml.Net from nuget.org via the Directory.Build.props default. Keep the unconditional 'Ensure local-feed exists' step (nuget.config still declares the source).
   - workflow_dispatch inputs: keep huml_version (test any published version); add boolean pack_from_source (default false) — when true, checkout primeBeri/huml-dotnet, pack both packages as version 0.0.0-source into local-feed, and run examples with -HumlNetVersion 0.0.0-source.
   - Pin actions by SHA: checkout v6.0.3 (df4cb1c0...), setup-dotnet v5.3.0 (9a946fdb...) — Node-24-safe and consistent with the main repo.
2. Update the stale nuget.config comment (local feed is now only for pack-from-source CI and local development).
3. Add .github/dependabot.yml (nuget + github-actions, weekly, same shape as the main repo's).
4. README: document the two CI modes and the pack_from_source dispatch flag.
5. Verify both modes: the push of these changes runs default mode; then manually dispatch with pack_from_source=true and watch it green.
<!-- SECTION:PLAN:END -->

## Implementation Notes

<!-- SECTION:NOTES:BEGIN -->
Examples commit 2341cd7: workflow inverted (default = nuget.org restore; pack_from_source boolean dispatch input packs main repo as 0.0.0-source into local-feed — distinct version chosen so restore cannot race the same-versioned nuget.org package), actions SHA-pinned at checkout v6.0.3 / setup-dotnet v5.3.0, dependabot.yml added (already produced its first scan runs), README documents both modes, nuget.config comment updated. Directory.Build.props default 0.2.0-beta.1 = latest published (AC#3). Awaiting default-mode push run + pack-from-source dispatch run for AC#1/#2/#6.
<!-- SECTION:NOTES:END -->

## Final Summary

<!-- SECTION:FINAL_SUMMARY:BEGIN -->
huml-dotnet-examples CI now tests the genuinely published package by default (commit 2341cd7).

What changed (all in primeBeri/huml-dotnet-examples):
- examples.yml inverted: push/PR runs restore Huml.Net 0.2.0-beta.1 from nuget.org with no main-repo checkout or pack. The 'Ensure local-feed exists' step stays unconditional because nuget.config declares the folder source and NuGet hard-fails (NU1301) if it is missing.
- New workflow_dispatch boolean input pack_from_source: packs the main repo's current main into local-feed as version 0.0.0-source and runs the examples against it. The distinct version is the key design point — 0.2.0-beta.1 now exists on nuget.org, so packing the same version locally would let restore resolve from either source nondeterministically.
- The existing huml_version input still tests any other published version.
- actions/checkout → v6.0.3 and actions/setup-dotnet → v5.3.0, pinned by commit SHA (Node-24-safe, consistent with the main repo).
- .github/dependabot.yml added (nuget + github-actions, weekly), ignoring Huml.Net/Huml.Net.SourceGeneration since their versions are the point of the repo and are bumped deliberately per release.
- README documents both CI modes and the local unpublished-build workflow; the stale nuget.config comment rewritten.

DoD notes: main-repo build/test verified green this session (1319/1319 on three TFMs); CHANGELOG/API/XML-doc/fixture items are not applicable to a cross-repo CI change.

Verification: default-mode push run 27449537222 success with both pack steps skipped; pack-from-source dispatch run 27449541638 success; the examples repo's first dependabot scans ran on push.
<!-- SECTION:FINAL_SUMMARY:END -->

## Definition of Done
<!-- DOD:BEGIN -->
- [x] #1 dotnet build succeeds with zero warnings (TreatWarningsAsErrors is on; full rebuild to surface cached analyzer results)
- [x] #2 dotnet test green on net8.0 / net9.0 / net10.0
- [x] #3 CHANGELOG.md [Unreleased] section updated for every user-visible change
- [x] #4 Any public API change is additive and justified in writing per docs/internals/api-freeze.md
- [x] #5 New/changed public members have XML docs; tests use AwesomeAssertions (never FluentAssertions)
- [x] #6 New error-or-no-error parse behaviours assessed against .claude/rules/fixture-gaps.md and staged in fixtures/extensions/ when language-agnostic
<!-- DOD:END -->
