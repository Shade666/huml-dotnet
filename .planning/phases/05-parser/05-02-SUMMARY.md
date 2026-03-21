---
phase: 05-parser
plan: 02
subsystem: parser
tags: [csharp, recursion-guard, depth-limit, huml, pars-05]

requires:
  - phase: 05-parser-plan-01
    provides: HumlParser with depth guard fields (_depth, _maxDepth) already wired

provides:
  - MaxRecursionDepth property on HumlOptions (default 512)
  - HumlParser constructor reads MaxRecursionDepth from options (not hardcoded default parameter)
  - Five dedicated depth-limit unit tests covering dict nesting, list nesting, custom limit, within-limit success, and default value assertion

affects:
  - 06-serializer-deserializer
  - 07-fixture-compliance

tech-stack:
  added: []
  patterns:
    - "Each nesting level costs 2 depth units: ParseVector + ParseMultilineDict/ParseMultilineList both increment _depth"
    - "MaxRecursionDepth is read from HumlOptions in HumlParser constructor — no separate parameter needed"

key-files:
  created: []
  modified:
    - src/Huml.Net/Versioning/HumlOptions.cs
    - src/Huml.Net/Parser/HumlParser.cs
    - tests/Huml.Net.Tests/Parser/HumlParserTests.cs

key-decisions:
  - "Each nesting level consumes 2 depth units (ParseVector + ParseMultilineDict or ParseMultilineList both guard) — tests for WithinDepthLimit must account for this double-increment"
  - "HumlParser constructor parameter maxDepth removed; now always reads options.MaxRecursionDepth to keep API consistent"

patterns-established:
  - "Test for WithinDepthLimit: use MaxRecursionDepth = 50 for 5-level nesting (10 actual units used) to ensure clear headroom"

requirements-completed: [PARS-05]

duration: 2min
completed: 2026-03-21
---

# Phase 5 Plan 2: Recursion Depth Limit Tests Summary

**MaxRecursionDepth property added to HumlOptions (default 512), parser reads it from options, and five unit tests verify the depth guard fires on dict/list nesting, respects custom limits, allows valid documents, and defaults to 512**

## Performance

- **Duration:** 2 min
- **Started:** 2026-03-21T08:17:22Z
- **Completed:** 2026-03-21T08:19:07Z
- **Tasks:** 2
- **Files modified:** 3

## Accomplishments

- Added `public int MaxRecursionDepth { get; init; } = 512;` to `HumlOptions` with XML doc comment referencing `HumlParseException` and `StackOverflowException`
- Removed `maxDepth = 512` default parameter from `HumlParser` constructor; constructor now reads `options.MaxRecursionDepth` directly
- All 5 depth-limit tests pass across net8.0, net9.0, net10.0 — full suite 140 tests green

## Task Commits

1. **Task 1: Add MaxRecursionDepth to HumlOptions and wire into parser** - `db0e3d2` (feat)
2. **Task 2: Add depth limit unit tests** - `cbb0616` (test)

**Plan metadata:** (docs commit follows)

## Files Created/Modified

- `src/Huml.Net/Versioning/HumlOptions.cs` - Added `MaxRecursionDepth` property with default 512
- `src/Huml.Net/Parser/HumlParser.cs` - Constructor now reads `options.MaxRecursionDepth` instead of using a default parameter
- `tests/Huml.Net.Tests/Parser/HumlParserTests.cs` - Added 5 depth-limit tests in section 9

## Decisions Made

- **Double-increment depth behaviour:** Each nested level actually fires depth guards in both `ParseVector` AND the subsequent `ParseMultilineDict`/`ParseMultilineList`. So 5 nesting levels uses ~10 depth units. The `Parse_WithinDepthLimit_Succeeds` test was initially written with `MaxRecursionDepth = 10` for a 5-level dict, which hit the limit exactly. Fixed to use `MaxRecursionDepth = 50` to provide clear headroom.
- **Constructor parameter removed:** The original Plan 01 implementation used `internal HumlParser(string source, HumlOptions options, int maxDepth = 512)`. This plan removes that parameter and reads `options.MaxRecursionDepth` — keeps the API surface consistent and ensures callers cannot accidentally bypass the options-configured limit.

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 1 - Bug] Parse_WithinDepthLimit_Succeeds failed: depth limit hit at exactly 10 units for 5-level nesting**
- **Found during:** Task 2 (depth limit unit tests)
- **Issue:** Test used `MaxRecursionDepth = 10` for a 5-level nested dict. Because both `ParseVector` and `ParseMultilineDict` each increment `_depth`, 5 nesting levels consumes 10 depth units — exactly at the limit, causing a throw instead of success
- **Fix:** Changed `MaxRecursionDepth` from 10 to 50 in the within-limit test to provide clear headroom
- **Files modified:** `tests/Huml.Net.Tests/Parser/HumlParserTests.cs`
- **Verification:** All 29 parser tests pass; `Parse_WithinDepthLimit_Succeeds` green on all TFMs
- **Committed in:** `cbb0616` (Task 2 commit)

---

**Total deviations:** 1 auto-fixed (Rule 1 - Bug)
**Impact on plan:** Single test parameter correction. The depth guard itself is correct — both ParseVector and ParseMultilineDict guard correctly. The test's assumed depth consumption was incorrect.

## Known Stubs

None. The depth guard is fully wired end-to-end: `HumlOptions.MaxRecursionDepth` → constructor → `_maxDepth` → guard at recursive entry points.

## Issues Encountered

None beyond the auto-fixed test parameter issue documented above.

## Next Phase Readiness

- `HumlOptions` is now complete for Phase 5 scope: `SpecVersion`, `VersionSource`, `UnknownVersionBehaviour`, `MaxRecursionDepth`
- Phase 5 parser is fully implemented and tested (PARS-03, PARS-04, PARS-05 all complete)
- Ready for Phase 6 (serializer/deserializer) and Phase 7 (fixture compliance)

## Self-Check: PASSED

- FOUND: src/Huml.Net/Versioning/HumlOptions.cs
- FOUND: src/Huml.Net/Parser/HumlParser.cs
- FOUND: tests/Huml.Net.Tests/Parser/HumlParserTests.cs
- FOUND commit: db0e3d2 (feat(05-02): add MaxRecursionDepth to HumlOptions and wire into parser)
- FOUND commit: cbb0616 (test(05-02): add depth-limit unit tests for PARS-05)

---
*Phase: 05-parser*
*Completed: 2026-03-21*
