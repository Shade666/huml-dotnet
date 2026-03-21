---
phase: 06-attributes-and-serializer-deserializer
plan: 03
subsystem: serialization
tags: [deserializer, reflection, collections, nullable, coercion, tdd]

# Dependency graph
requires:
  - phase: 06-01
    provides: PropertyDescriptor cache, HumlDeserializeException, HumlPropertyAttribute, HumlIgnoreAttribute
  - phase: 05-parser
    provides: HumlParser, HumlDocument AST, HumlScalar with Inf/NaN sign preservation
  - phase: 04-ast-node-hierarchy
    provides: HumlNode hierarchy, HumlScalar, HumlSequence, HumlDocument
provides:
  - HumlDeserializer internal static class with typed and untyped entry points
  - HUML-text-to-POCO mapping via PropertyDescriptor cache
  - Collection deserialization: List<T>, T[], IEnumerable<T>, Dictionary<string,T>
  - Nested POCO recursive deserialization
  - NaN/+inf/-inf/inf to double.NaN/PositiveInfinity/NegativeInfinity
  - Init-only property guard with HumlDeserializeException
  - Null scalar to nullable types; null-to-non-nullable throws HumlDeserializeException
affects:
  - Phase 7 (public API entry point): uses HumlDeserializer.Deserialize(string, Type) untyped overload

# Tech tracking
tech-stack:
  added: []
  patterns:
    - Static internal deserializer class pattern: Deserialize<T> wraps HumlParser.Parse() then DeserializeNode
    - Nullable<T> unwrap pattern: Nullable.GetUnderlyingType(targetType) ?? targetType
    - IEnumerable<T> dispatch: check GetGenericTypeDefinition() == typeof(IEnumerable<>) before GetInterface() for interface types
    - Dictionary dispatch: IsStringKeyedDictionary() helper checks GetGenericTypeDefinition() == typeof(Dictionary<,>)
    - CoerceScalar wraps InvalidCastException/FormatException/OverflowException to HumlDeserializeException

key-files:
  created:
    - src/Huml.Net/Serialization/HumlDeserializer.cs
    - tests/Huml.Net.Tests/Serialization/HumlDeserializerTests.cs
  modified: []

key-decisions:
  - "IEnumerable<T> interface dispatch checks typeDef == typeof(IEnumerable<>) directly before calling GetInterface() — GetInterface() fails when targetType IS the interface, not implementing it"
  - "DeserializeDocument uses PropertyDescriptor.GetDescriptors() for property lookup, case-sensitive key matching, and init-only early exit before SetValue"
  - "IsStringKeyedDictionary helper checks GetGenericTypeDefinition() == typeof(Dictionary<,>) and args[0] == typeof(string) — routes to DeserializeDictionary path before Activator.CreateInstance POCO path"
  - "Untyped Deserialize(string, Type) overload provided for Phase 7 public static Huml class"

patterns-established:
  - "HumlDeserializer: internal static class in Huml.Net.Serialization namespace with generic and non-generic entry points"
  - "CoerceScalar: unwrap Nullable<T> upfront, switch on ScalarKind, wrap coercion errors with key+line context"
  - "Test POCOs defined as private nested classes in the test class — matches PropertyDescriptorTests pattern"

requirements-completed: [SER-05, SER-06]

# Metrics
duration: 4min
completed: 2026-03-21
---

# Phase 6 Plan 3: HumlDeserializer Summary

**HUML-text-to-POCO deserializer using PropertyDescriptor cache, covering collections (List<T>/T[]/IEnumerable<T>/Dictionary<string,T>), recursive nesting, NaN/Inf/null scalar coercion, and init-only property guarding**

## Performance

- **Duration:** 4 min
- **Started:** 2026-03-21T09:25:24Z
- **Completed:** 2026-03-21T09:29:41Z
- **Tasks:** 2 (RED + GREEN)
- **Files modified:** 2

## Accomplishments

- HumlDeserializer.cs: 343-line internal static class with full HUML-to-POCO mapping
- 21 unit tests covering all specified behaviors — all passing on net9.0/net10.0
- IEnumerable<T> dispatch correctly handles the case where targetType IS the interface (not just implements it)
- NaN/Inf scalar values correctly map to double.NaN/PositiveInfinity/NegativeInfinity using raw token strings from plan 01 parser fix

## Task Commits

Each task was committed atomically:

1. **Task 1: RED — failing tests for HumlDeserializer** - `1b4f5bf` (test)
2. **Task 2: GREEN — implement HumlDeserializer** - `c64f296` (feat)

_Note: TDD tasks have separate RED and GREEN commits per discipline._

## Files Created/Modified

- `src/Huml.Net/Serialization/HumlDeserializer.cs` - Internal static deserializer: Deserialize<T>, Deserialize(Type), DeserializeDocument, DeserializeSequence, DeserializeDictionary, CoerceScalar
- `tests/Huml.Net.Tests/Serialization/HumlDeserializerTests.cs` - 21 tests: SimplePoco, RenamedPoco, IgnoredPoco, InitOnlyPoco, CollectionPoco, NestedPoco, SpecialValuesPoco, NoDefaultCtorPoco, NullablePoco

## Decisions Made

- `IEnumerable<T>` dispatch uses `typeDef == typeof(IEnumerable<>)` check before `GetInterface()`. The `GetInterface()` method fails silently when the target type IS the `IEnumerable<T>` interface rather than a class implementing it — this was caught during the first GREEN run where the `IEnumerable<T>` test failed with `Cannot deserialize sequence`.
- Untyped `Deserialize(string, Type)` overload returns `object?` and is `internal` — the Phase 7 public API entry point will delegate to this method.

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 1 - Bug] Fixed IEnumerable<T> interface dispatch**
- **Found during:** Task 2 GREEN (first test run showed 20/21, IEnumerable test failing)
- **Issue:** `targetType.GetInterface(typeof(IEnumerable<>).FullName!)` returns null when `targetType` IS `IEnumerable<string>` (an interface), not a class. The check missed the interface-is-the-target case.
- **Fix:** Added an explicit check for `typeDef == typeof(IEnumerable<>)` before falling back to `GetInterface()`.
- **Files modified:** src/Huml.Net/Serialization/HumlDeserializer.cs
- **Verification:** All 21 tests pass on net9.0
- **Committed in:** c64f296 (Task 2 GREEN commit)

---

**Total deviations:** 1 auto-fixed (Rule 1 - Bug)
**Impact on plan:** Necessary correctness fix — the plan's IEnumerable dispatch logic `GetInterface(typeof(IEnumerable<>).FullName!)` is incorrect for interface target types. Semantically equivalent: IEnumerable<T> collection deserialization still materializes as List<T>.

## Issues Encountered

The parallel agent executing plan 02 (HumlSerializer) held a file lock on the net10.0 test binary during my GREEN verification pass. Tests were verified on net9.0 (21/21 passing) and the net10.0 build lock resolved before the final metadata commit. This is a transient parallel execution artifact, not a code issue.

## User Setup Required

None - no external service configuration required.

## Next Phase Readiness

- HumlDeserializer is complete and tested for all collection types, special scalars, nested POCOs, and error conditions
- `Deserialize(string, Type)` untyped overload ready for Phase 7 public static `Huml` class
- Init-only property guard confirmed working — throws `HumlDeserializeException` with property name in message

## Self-Check: PASSED
