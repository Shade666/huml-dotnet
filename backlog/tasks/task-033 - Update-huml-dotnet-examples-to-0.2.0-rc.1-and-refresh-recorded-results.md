---
id: TASK-033
title: Update huml-dotnet-examples to 0.2.0-rc.1 and refresh recorded results
status: Done
assignee: []
created_date: '2026-07-08 09:54'
updated_date: '2026-07-08 10:04'
labels:
  - documentation
  - examples
dependencies: []
references:
  - 'https://github.com/primeBeri/huml-dotnet-examples'
  - 'https://www.nuget.org/packages/Huml.Net/'
priority: high
ordinal: 23000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
The companion huml-dotnet-examples repo (separate repository, cloned at ../huml-dotnet-examples) still pins Huml.Net 0.2.0-beta.1 as the default package version under test; 0.2.0-rc.1 shipped 2026-07-07 (including the H1 double-serialisation fix, which may affect serialise benchmarks). Bump the default HumlNetVersion in Directory.Build.props and every doc mention (README.md, run-examples.ps1 comments, datasets/README.md, benchmarks/RESULTS.md commentary), run the full example suite against the published rc.1 package to confirm the E01–E13 end-to-end suite passes, and refresh or annotate the recorded benchmark results so they state which version they were measured against.
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [x] #1 Default HumlNetVersion is 0.2.0-rc.1 and restores from nuget.org
- [x] #2 run-examples.ps1 passes for all E01–E13 examples against 0.2.0-rc.1
- [x] #3 All version mentions in the examples repo docs are consistent with rc.1
- [x] #4 benchmarks/RESULTS.md clearly states the Huml.Net version its figures were measured against (re-run against rc.1, or annotated as beta.1 figures if a re-run is not feasible)
<!-- AC:END -->

## Implementation Plan

<!-- SECTION:PLAN:BEGIN -->
1. Bump default HumlNetVersion 0.2.0-beta.1 → 0.2.0-rc.1 in Directory.Build.props and README.md. 2. Run ./run-examples.ps1 against the published rc.1 package from nuget.org. 3. Re-run the BenchmarkDotNet suite against rc.1 and re-record benchmarks/RESULTS.md with fresh figures and updated commentary. 4. Commit and push to main; confirm Examples (e2e) CI green.
<!-- SECTION:PLAN:END -->

## Implementation Notes

<!-- SECTION:NOTES:BEGIN -->
Version mentions audit: run-examples.ps1 line 7 and datasets/README.md keep their existing text — the former is a CLI override example (any valid version works), the latter refers to the HUML spec version header (%HUML v0.2.0), not the package version. Only Directory.Build.props, README.md, and benchmarks/RESULTS.md carried the package-version pin.

rc.1 benchmark figures are within run-to-run noise of the beta.1 recording — the H1 overridden-property serialisation fix costs nothing for models without inheritance, as expected (the de-duplication happens at descriptor-cache build time, not per serialise call).
<!-- SECTION:NOTES:END -->

## Final Summary

<!-- SECTION:FINAL_SUMMARY:BEGIN -->
Bumped the default Huml.Net package under test from 0.2.0-beta.1 to 0.2.0-rc.1 (Directory.Build.props + README), verified all 13 examples (E01–E13) pass against the published rc.1 package restored from nuget.org, and re-recorded benchmarks/RESULTS.md against rc.1 (2026-07-08 run: serialise 865/530 ns reflection/source-gen vs STJ 356 ns baseline; deserialise 2357/1977 ns vs 660 ns; figures within noise of beta.1, confirming the rc's H1 serialisation fix has no throughput cost). Commentary updated from "first beta" to release-candidate framing. Pushed as 84d3c4f to primeBeri/huml-dotnet-examples main; Examples (e2e) CI completed green.
<!-- SECTION:FINAL_SUMMARY:END -->

## Definition of Done
<!-- DOD:BEGIN -->
- [x] #1 Changes committed and pushed to primeBeri/huml-dotnet-examples main
- [x] #2 Examples (e2e) CI workflow green after push
<!-- DOD:END -->
