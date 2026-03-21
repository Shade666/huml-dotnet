---
phase: 04-ast-node-hierarchy
plan: 01
subsystem: parser
tags: [csharp, records, ast, immutable, enum, sealed]

# Dependency graph
requires:
  - phase: 03-lexer-token-types
    provides: TokenType enum with scalar members that map 1:1 to ScalarKind values
provides:
  - Immutable abstract record base type HumlNode for the AST
  - Sealed record AST nodes: HumlDocument, HumlMapping, HumlSequence, HumlScalar
  - ScalarKind enum with 7 members matching HUML scalar type taxonomy
  - Structural equality via record synthesised Equals for all node types
affects: [05-parser, 06-serializer-deserializer, 07-fixture-compliance]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - Positional record syntax for AST nodes (primary constructor = public init properties)
    - Abstract base record with sealed concrete record subtypes (no instantiation of base)
    - object? Value in HumlScalar for heterogeneous scalar payloads with boxed value types
    - Huml.Net.Parser namespace for all AST types

key-files:
  created:
    - src/Huml.Net/Parser/HumlNode.cs
    - src/Huml.Net/Parser/HumlDocument.cs
    - src/Huml.Net/Parser/HumlMapping.cs
    - src/Huml.Net/Parser/HumlSequence.cs
    - src/Huml.Net/Parser/HumlScalar.cs
    - src/Huml.Net/Parser/ScalarKind.cs
    - tests/Huml.Net.Tests/Parser/HumlNodeTests.cs
    - tests/Huml.Net.Tests/Parser/ScalarKindTests.cs
  modified: []

key-decisions:
  - "ScalarKind.Integer (not Int) — intentional asymmetry from TokenType.Int for semantic clarity at AST level"
  - "HumlScalar.Value is object? to hold heterogeneous runtime values (string, long, double, bool, null) without generics"
  - "IReadOnlyList<HumlNode> for HumlDocument.Entries and HumlSequence.Items — consistent collection contract across node types"

patterns-established:
  - "Parser namespace: Huml.Net.Parser for all AST types"
  - "One type per file in src/Huml.Net/Parser/"
  - "XML doc comments with <summary> and <param> on all public members"
  - "Sealed concrete record types inheriting from abstract base record"

requirements-completed: [PARS-01, PARS-02]

# Metrics
duration: 2min
completed: 2026-03-21
---

# Phase 4 Plan 1: AST Node Hierarchy Summary

**Six immutable sealed record types (HumlNode abstract base, HumlDocument, HumlMapping, HumlSequence, HumlScalar) and ScalarKind enum with 7 values establishing the complete HUML AST contract for Phase 5 parser consumption.**

## Performance

- **Duration:** 2 min
- **Started:** 2026-03-21T14:58:14Z
- **Completed:** 2026-03-21T15:00:16Z
- **Tasks:** 2 (4 commits including TDD RED phases)
- **Files modified:** 8

## Accomplishments

- Defined immutable abstract record `HumlNode` as the sealed-hierarchy base for all AST nodes
- Created 4 concrete sealed record types: `HumlDocument`, `HumlMapping`, `HumlSequence`, `HumlScalar` — all inheriting from `HumlNode` with positional record syntax
- Defined `ScalarKind` enum with 7 members (String, Integer, Float, Bool, Null, NaN, Inf) with XML doc comments
- Verified structural equality including boxed value type equality on `HumlScalar`
- All 111 tests pass across net8.0, net9.0, net10.0 with zero warnings

## Task Commits

Each task was committed atomically (TDD: RED then GREEN):

1. **Task 1 RED: ScalarKindTests (failing)** - `310bd78` (test)
2. **Task 1 GREEN: ScalarKind enum** - `356c7c3` (feat)
3. **Task 2 RED: HumlNodeTests (failing)** - `f8bfed3` (test)
4. **Task 2 GREEN: AST node hierarchy** - `ad3934a` (feat)

**Plan metadata:** (pending docs commit)

_Note: TDD tasks have two commits each (test RED → feat GREEN)_

## Files Created/Modified

- `src/Huml.Net/Parser/ScalarKind.cs` - Enum with 7 HUML scalar kind members
- `src/Huml.Net/Parser/HumlNode.cs` - Abstract record base type for all AST nodes
- `src/Huml.Net/Parser/HumlDocument.cs` - Root document node holding IReadOnlyList of top-level entries
- `src/Huml.Net/Parser/HumlMapping.cs` - Key-value mapping node (key: value)
- `src/Huml.Net/Parser/HumlSequence.cs` - Sequence/list node holding IReadOnlyList of child nodes
- `src/Huml.Net/Parser/HumlScalar.cs` - Scalar value node with Kind (ScalarKind) and Value (object?)
- `tests/Huml.Net.Tests/Parser/ScalarKindTests.cs` - 9 tests covering enum member count, names, default value
- `tests/Huml.Net.Tests/Parser/HumlNodeTests.cs` - 23 tests covering type hierarchy, construction, equality, polymorphism

## Decisions Made

- `ScalarKind.Integer` (not `Int`) — intentional asymmetry from `TokenType.Int` for semantic clarity at the AST level; the AST represents parsed meaning, not lexer token names
- `HumlScalar.Value` is `object?` — accommodates heterogeneous runtime values (string, long, double, bool) and null for Null/NaN/Inf kinds without introducing generics or union types
- `IReadOnlyList<HumlNode>` for collection nodes — consistent, readonly contract; reference equality used in tests to confirm positional record Equals passes through the reference

## Deviations from Plan

None - plan executed exactly as written.

## Issues Encountered

None.

## User Setup Required

None - no external service configuration required.

## Next Phase Readiness

- All 6 production files exist in `src/Huml.Net/Parser/`
- AST node shapes are locked — Phase 5 parser can begin producing these types
- Phase 6 deserializer can pattern-match on the 4 concrete types
- No blockers

---
*Phase: 04-ast-node-hierarchy*
*Completed: 2026-03-21*

## Self-Check: PASSED

All 9 expected files found on disk. All 4 task commits verified in git log.
