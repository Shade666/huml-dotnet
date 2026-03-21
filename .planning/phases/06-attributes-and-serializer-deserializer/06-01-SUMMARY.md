---
phase: 06-attributes-and-serializer-deserializer
plan: 01
subsystem: serialization
tags: [attributes, reflection, cache, exceptions, parser, inf, nan, tdd]

# Dependency graph
requires:
  - phase: 05-parser
    provides: HumlParser, HumlScalar, ScalarKind, token stream AST
  - phase: 04-ast-node-hierarchy
    provides: HumlNode hierarchy, HumlScalar with object? Value
  - phase: 03-lexer-and-token-types
    provides: Lexer, Token types, HumlParseException pattern
provides:
  - HumlPropertyAttribute with Name override and OmitIfDefault init flag
  - HumlIgnoreAttribute marker for property exclusion
  - HumlSerializeException sealed exception type
  - HumlDeserializeException sealed exception with Key and Line diagnostic properties
  - PropertyDescriptor per-type cached record (base-first declaration order, MetadataToken, IsExternalInit detection)
  - Parser Inf/NaN scalars now carry raw token string in Value (e.g., "+inf", "-inf", "nan")
affects:
  - 06-02 (serializer): uses PropertyDescriptor, HumlPropertyAttribute, HumlIgnoreAttribute, HumlSerializeException
  - 06-03 (deserializer): uses PropertyDescriptor, HumlDeserializeException, IsInitOnly, Inf/NaN raw string values

# Tech tracking
tech-stack:
  added: []
  patterns:
    - Sealed exception types with no binary serialization ctor (SYSLIB0051 pattern from Phase 03)
    - PropertyDescriptor cache using ConcurrentDictionary<Type, T[]> with GetOrAdd + BuildDescriptors
    - MetadataToken sort for declaration-order property enumeration within a type
    - BaseType chain walk (root-first list) for base-before-derived ordering
    - IsExternalInit custom modifier detection via ReturnParameter.GetRequiredCustomModifiers()
    - TDD discipline: write failing tests first, commit RED, then implement GREEN

key-files:
  created:
    - src/Huml.Net/Serialization/Attributes/HumlPropertyAttribute.cs
    - src/Huml.Net/Serialization/Attributes/HumlIgnoreAttribute.cs
    - src/Huml.Net/Exceptions/HumlSerializeException.cs
    - src/Huml.Net/Exceptions/HumlDeserializeException.cs
    - src/Huml.Net/Serialization/PropertyDescriptor.cs
    - tests/Huml.Net.Tests/Serialization/PropertyDescriptorTests.cs
    - tests/Huml.Net.Tests/Exceptions/HumlSerializeExceptionTests.cs
    - tests/Huml.Net.Tests/Exceptions/HumlDeserializeExceptionTests.cs
  modified:
    - src/Huml.Net/Parser/HumlParser.cs
    - tests/Huml.Net.Tests/Parser/HumlParserTests.cs

key-decisions:
  - "PropertyDescriptor.GetDescriptors uses BaseType chain walk (root-first) so base properties always precede derived properties — matches .NET declaration-order convention"
  - "IsExternalInit detection via setMethod.ReturnParameter.GetRequiredCustomModifiers() string comparison — works across netstandard2.1/net8/net9/net10"
  - "HumlScalar.Value for Inf now carries raw token string ('+inf'/'-inf'/'inf') not null — deserializer needs sign to distinguish PositiveInfinity from NegativeInfinity"
  - "NaN Value also preserves raw token string 'nan' for consistency"
  - "Test for -inf uses mapping value (key: -inf) not root scalar — '-' at col 0 is ListItem token, not sign prefix"

patterns-established:
  - "PropertyDescriptor: internal sealed record in Huml.Net.Serialization namespace with static cache methods"
  - "Attribute namespace: Huml.Net.Serialization (not a sub-namespace) — keeps import simple"
  - "ClearCache() method on PropertyDescriptor for xUnit test isolation (call in ctor)"

requirements-completed: [SER-01, SER-02, SER-03, SER-07]

# Metrics
duration: 3min
completed: 2026-03-21
---

# Phase 6 Plan 1: Serialization Contracts Summary

**[HumlProperty]/[HumlIgnore] attributes, HumlSerializeException/HumlDeserializeException, ConcurrentDictionary-backed PropertyDescriptor cache with MetadataToken declaration order, and parser Inf/NaN sign preservation**

## Performance

- **Duration:** 3 min
- **Started:** 2026-03-21T09:19:11Z
- **Completed:** 2026-03-21T09:22:34Z
- **Tasks:** 2
- **Files modified:** 10

## Accomplishments

- Six new source files: two attributes, two exception types, PropertyDescriptor cache
- PropertyDescriptor resolves declaration order (MetadataToken), [HumlIgnore] exclusion, [HumlProperty] key rename, OmitIfDefault, init-only detection, base-before-derived inheritance ordering, and caches per-type results
- Parser TokenToScalar now passes `tok.Value` for Inf/NaN scalars so sign strings ("+inf", "-inf") are preserved for the deserializer
- 163 tests green across net8.0 / net9.0 / net10.0 (was 140; added 23 new tests)

## Task Commits

Each task was committed atomically:

1. **Task 1: RED — failing tests for attributes, exceptions, PropertyDescriptor** - `b41eeea` (test)
2. **Task 1: GREEN — implement all six source files** - `4e07099` (feat)
3. **Task 2: Fix parser Inf/NaN sign preservation** - `ca96c3e` (fix)

_Note: TDD tasks have separate RED and GREEN commits per discipline._

## Files Created/Modified

- `src/Huml.Net/Serialization/Attributes/HumlPropertyAttribute.cs` - Name override and OmitIfDefault init property
- `src/Huml.Net/Serialization/Attributes/HumlIgnoreAttribute.cs` - Marker attribute for property exclusion
- `src/Huml.Net/Exceptions/HumlSerializeException.cs` - Sealed; no binary serialization ctor
- `src/Huml.Net/Exceptions/HumlDeserializeException.cs` - Sealed; Key and Line diagnostic properties; formatted message
- `src/Huml.Net/Serialization/PropertyDescriptor.cs` - ConcurrentDictionary cache; MetadataToken order; IsExternalInit detection; BaseType chain walk
- `tests/Huml.Net.Tests/Serialization/PropertyDescriptorTests.cs` - 9 tests covering all PropertyDescriptor behaviors
- `tests/Huml.Net.Tests/Exceptions/HumlSerializeExceptionTests.cs` - 4 tests (sealed, message ctors, no binary ctor)
- `tests/Huml.Net.Tests/Exceptions/HumlDeserializeExceptionTests.cs` - 8 tests (sealed, Key/Line properties, message format)
- `src/Huml.Net/Parser/HumlParser.cs` - TokenToScalar: Inf/NaN now pass tok.Value
- `tests/Huml.Net.Tests/Parser/HumlParserTests.cs` - Updated NaN/Inf assertions; added PositiveInf and NegativeInf tests

## Decisions Made

- `PropertyDescriptor` is `internal sealed record` in `Huml.Net.Serialization` namespace. Internal scope prevents premature public API surface; the record syntax gives structural equality and deconstruction for free.
- Attribute namespace is `Huml.Net.Serialization` (not `Huml.Net.Serialization.Attributes`) matching the HumlPropertyAttribute plan spec. Simple import for consumers.
- `ClearCache()` is `internal` on `PropertyDescriptor` — allows test isolation without exposing to public API.
- Test for `-inf` as root scalar would fail since lexer emits `-` at col 0 as `ListItem` token, not a sign for a numeric literal. Tests use `key: -inf` (mapping value context) where `-` is correctly parsed as sign prefix.

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 1 - Bug] Adjusted -inf test to use mapping value context**
- **Found during:** Task 2 (parser Inf fix)
- **Issue:** Plan's test for `Parse_NegativeInfScalar_ReturnsNegativeInf` used bare `-inf` as root input. The lexer emits `-` at column 0 (== lineIndent) as a `ListItem` token, not a sign prefix, making root `-inf` parse as a list, not a scalar.
- **Fix:** Changed test input from `"-inf"` to `"val: -inf"` (mapping value) and updated assertions to unwrap the mapping. Same fix applied for `+inf` test for consistency.
- **Files modified:** tests/Huml.Net.Tests/Parser/HumlParserTests.cs
- **Verification:** All 31 parser tests and full suite (163) pass
- **Committed in:** ca96c3e (Task 2 commit)

---

**Total deviations:** 1 auto-fixed (Rule 1 - Bug)
**Impact on plan:** Necessary correction — plan's test expectation was incompatible with HUML lexer rules. Semantically equivalent: -inf is still tested correctly as a HUML value.

## Issues Encountered

None beyond the `-inf` root scalar lexer conflict documented above.

## User Setup Required

None - no external service configuration required.

## Next Phase Readiness

- All serialization contracts are defined and tested
- PropertyDescriptor cache is ready for HumlSerializer (06-02) to enumerate properties in declaration order
- HumlDeserializeException with Key/Line is ready for HumlDeserializer (06-03) error reporting
- Inf/NaN raw sign strings are available for the deserializer to map to double.PositiveInfinity / double.NegativeInfinity

---
*Phase: 06-attributes-and-serializer-deserializer*
*Completed: 2026-03-21*
