---
phase: 07-static-entry-point-and-shared-fixture-compliance
plan: 01
subsystem: api
tags: [huml, static-facade, serialization, deserialization, xml-docs, csharp]

# Dependency graph
requires:
  - phase: 06-attributes-and-serializer-deserializer
    provides: HumlSerializer, HumlDeserializer, HumlOptions, all internal pipeline classes
provides:
  - Public static Huml class with 6 overloads as the single library entry point
  - Serialize<T>, Serialize(object,Type), Deserialize<T>(string), Deserialize<T>(ReadOnlySpan<char>), Deserialize(string,Type), Parse(string)
  - XML doc comments on all public members (API-03)
  - AsSpan() delegation chain from string overload to span overload (API-02)
  - Parser version directive handling (optional %HUML token consumed before document body)
affects: [08-nuget-and-ci, any consumer code, Phase 07 Plan 02 shared fixture compliance]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "Static facade pattern mirroring System.Text.Json.JsonSerializer"
    - "String Deserialize delegates via AsSpan() to span overload — single lexer path"
    - "T: prefix in cref attributes to avoid ambiguity with type names in same namespace"
    - "Parser consumes optional %HUML version token before root dispatch"

key-files:
  created:
    - src/Huml.Net/Huml.cs
    - tests/Huml.Net.Tests/HumlStaticApiTests.cs
  modified:
    - src/Huml.Net/Parser/HumlParser.cs
    - src/Huml.Net/Lexer/TokenType.cs
    - src/Huml.Net/Versioning/HumlOptions.cs

key-decisions:
  - "Huml.Deserialize<T>(string) calls Deserialize<T>(huml.AsSpan(), options) — never calls HumlDeserializer directly — preserving the single lexer path (API-02)"
  - "Parser consumes optional %HUML version token at document start to support round-trip Serialize->Deserialize; this was a pre-existing gap exposed by writing the facade"
  - "cref attributes referencing Huml.Net.Exceptions.* types must use T: prefix to avoid the compiler resolving 'Huml' as the new Huml static class rather than as a namespace segment"
  - "Round-trip tests use HumlOptions.AutoDetect so the parser reads version from the emitted %HUML header"

patterns-established:
  - "Facade: Huml.Serialize/Deserialize/Parse delegate to internal Serialization.* classes"
  - "Test POCO defined inline as private class within test class"

requirements-completed: [API-01, API-02, API-03]

# Metrics
duration: 6min
completed: 2026-03-21
---

# Phase 07 Plan 01: Static Entry Point Summary

**Public static Huml facade with 6 documented overloads delegating to the internal pipeline, plus parser version-directive fix enabling Serialize->Deserialize round-trips**

## Performance

- **Duration:** 6 min
- **Started:** 2026-03-21T12:05:21Z
- **Completed:** 2026-03-21T12:11:20Z
- **Tasks:** 1 (TDD: RED + GREEN)
- **Files modified:** 5

## Accomplishments

- `Huml` static class is the single public API entry point, mirroring `System.Text.Json.JsonSerializer` — all 6 overloads documented with XML `<summary>`, `<param>`, `<returns>`, and `<exception cref=...>` tags
- `Deserialize<T>(string)` delegates to `Deserialize<T>(ReadOnlySpan<char>)` via `AsSpan()` (API-02), ensuring a single lexer path
- Parser now correctly skips an optional `%HUML` version directive before parsing the document body, enabling round-trip Serialize->Deserialize without explicit `AutoDetect` options
- All 226 tests pass across all 4 TFMs; zero build warnings in Release mode

## Task Commits

Each task was committed atomically:

1. **Task 1 RED: Failing tests** - `3149714` (test)
2. **Task 1 GREEN: Huml facade + auto-fixes** - `d7ed1b5` (feat)

_Note: TDD task — test commit then implementation commit_

## Files Created/Modified

- `src/Huml.Net/Huml.cs` - New public static facade with all 6 overloads, full XML docs
- `tests/Huml.Net.Tests/HumlStaticApiTests.cs` - 8 tests covering all overloads, round-trips, error paths
- `src/Huml.Net/Parser/HumlParser.cs` - Added optional version token consumption in Parse()
- `src/Huml.Net/Lexer/TokenType.cs` - Fixed cref to use T: prefix for HumlParseException
- `src/Huml.Net/Versioning/HumlOptions.cs` - Fixed cref to use T: prefix for HumlParseException

## Decisions Made

- Round-trip tests use `HumlOptions.AutoDetect` when deserializing serialized output, so the parser reads the version from the emitted `%HUML v0.2.0` header
- `cref` attributes in pre-existing files that reference `Huml.Net.Exceptions.HumlParseException` by qualified name must use the `T:` prefix because introducing the `Huml` class in `Huml.Net` causes the compiler to resolve `Huml` as a type rather than a namespace segment

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 1 - Bug] Parser did not consume %HUML version directive token**
- **Found during:** Task 1 GREEN (running tests after implementing facade)
- **Issue:** `HumlParser.Parse()` called `InferRootType()` without consuming the leading `Version` token. The serializer always emits `%HUML v0.2.0\n` as the first line, so `Huml.Deserialize()` on serializer output always failed with "Unexpected token 'Version' at root"
- **Fix:** Added `if (Peek().Type == TokenType.Version) Advance();` at the start of `Parse()`, consuming the optional version directive before root dispatch. This also aligns with the shared fixture test `version_directive_with_dict` (expected `error: false`)
- **Files modified:** src/Huml.Net/Parser/HumlParser.cs
- **Verification:** All 226 tests pass; fixture scenario `version_directive_with_dict` now works correctly
- **Committed in:** d7ed1b5 (Task 1 GREEN commit)

**2. [Rule 1 - Bug] CS1574 errors: cref namespace-vs-type ambiguity**
- **Found during:** Task 1 GREEN (build after adding Huml.cs with `using` directives)
- **Issue:** `TokenType.cs` and `HumlOptions.cs` had cref attributes referencing `Huml.Net.Exceptions.HumlParseException` by full namespace path. With the new `Huml` static class in `Huml.Net`, the compiler resolved `Huml` as the type rather than the first namespace segment, causing CS1574 build errors
- **Fix:** Changed `<see cref="Huml.Net.Exceptions.HumlParseException"/>` to `<see cref="T:Huml.Net.Exceptions.HumlParseException"/>` using the `T:` type prefix, which forces type-level cref resolution
- **Files modified:** src/Huml.Net/Lexer/TokenType.cs, src/Huml.Net/Versioning/HumlOptions.cs
- **Verification:** Build succeeds with zero warnings in Release mode across all 4 TFMs
- **Committed in:** d7ed1b5 (Task 1 GREEN commit)

---

**Total deviations:** 2 auto-fixed (2 Rule 1 bugs)
**Impact on plan:** Both auto-fixes necessary for correctness. The parser fix enables round-trip usage; the cref fix restores build health. No scope creep.

## Issues Encountered

None — both issues were identified, diagnosed, and fixed within the task execution.

## Known Stubs

None — all 6 overloads are fully wired to the internal pipeline with no hardcoded or placeholder return values.

## Next Phase Readiness

- `Huml` static class is complete and ready for external consumer use
- Parser now accepts `%HUML` version directives, aligning with the shared fixture suite expectations
- Phase 07 Plan 02 can proceed with shared fixture compliance tests; the parser's version-directive fix means `%HUML v0.x.y` prefixed inputs will no longer fail unexpectedly

---
*Phase: 07-static-entry-point-and-shared-fixture-compliance*
*Completed: 2026-03-21*
