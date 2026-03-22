---
gsd_state_version: 1.0
milestone: v0.1
milestone_name: milestone
status: unknown
stopped_at: Completed 07.2-02-PLAN.md
last_updated: "2026-03-22T10:15:42.954Z"
progress:
  total_phases: 13
  completed_phases: 8
  total_plans: 17
  completed_plans: 16
---

# Project State

## Project Reference

See: .planning/PROJECT.md (updated 2026-03-20)

**Core value:** Full HUML spec compliance (v0.1 + v0.2), validated against the shared `huml-lang/tests` test suite, with zero external runtime dependencies and a `System.Text.Json`-style API that .NET developers already know.
**Current focus:** Phase 07.2 — code-quality-api-accuracy-and-performance-optimisations

## Current Position

Phase: 07.2 (code-quality-api-accuracy-and-performance-optimisations) — EXECUTING
Plan: 3 of 3

## Performance Metrics

**Velocity:**

- Total plans completed: 0
- Average duration: -
- Total execution time: 0 hours

**By Phase:**

| Phase | Plans | Total | Avg/Plan |
|-------|-------|-------|----------|
| - | - | - | - |

**Recent Trend:**

- Last 5 plans: -
- Trend: -

*Updated after each plan completion*
| Phase 01 P01 | 8 | 2 tasks | 8 files |
| Phase 01 P02 | 1 | 2 tasks | 2 files |
| Phase 02 P01 | 4 | 3 tasks | 12 files |
| Phase 03 P01 | 3 | 1 tasks | 6 files |
| Phase 03 P02 | 5 | 2 tasks | 3 files |
| Phase 04 P01 | 2 | 2 tasks | 8 files |
| Phase 05 P01 | 7 | 2 tasks | 4 files |
| Phase 05 P02 | 2 | 2 tasks | 3 files |
| Phase 06 P01 | 3 | 2 tasks | 10 files |
| Phase 06 P03 | 4 | 2 tasks | 2 files |
| Phase 06 P02 | 39min | 2 tasks | 2 files |
| Phase 07 P01 | 6 | 1 tasks | 5 files |
| Phase 07 P02 | 7 | 1 tasks | 3 files |
| Phase 07.1-version-header-parsing-and-versioning-completeness P01 | 3min | 1 tasks | 3 files |
| Phase 07.2 P01 | 3 | 2 tasks | 2 files |
| Phase 07.2 P02 | 8 | 2 tasks | 10 files |

## Accumulated Context

### Decisions

Decisions are logged in PROJECT.md Key Decisions table.
Recent decisions affecting current work:

- All phases: Single Lexer/Parser code path with `>=` version gates — no forked classes per spec version
- Phase 1: `dotnet pack` as dedicated CI step only — `GeneratePackageOnBuild` must NOT be used
- Phase 3: `ReadOnlySpan<char>` is the single implementation overload for Deserialize; `string` variant wraps via `AsSpan()` to avoid C# 14 CS0121 ambiguity
- Phase 6: `init`-only setter detection via `IsExternalInit` custom modifier; throw `HumlDeserializeException` rather than silently skip
- [Phase 01]: huml-lang/tests tags are v0.1.0 and v0.2.0, not v0.1/v0.2 as initially assumed
- [Phase 01]: dotnet new sln requires --format sln flag on SDK 10+ to produce .sln instead of .slnx
- [Phase 01]: OIDC Trusted Publishing via NuGet/login@v1 eliminates long-lived API key secrets from the repository
- [Phase 01]: CI uses submodules: recursive + fetch-depth: 0 for fixture submodules and MinVer tag walking
- [Phase 02]: IsExternalInit shim in IsExternalInit.cs required for init-only setters on netstandard2.1 — guarded by #if NETSTANDARD2_1
- [Phase 02]: HumlUnsupportedVersionException omits binary serialisation constructor — SYSLIB0051 fires on .NET 8+ under TreatWarningsAsErrors; new library has no BinaryFormatter requirement
- [Phase 02]: SpecVersionPolicy is internal with InternalsVisibleTo Huml.Net.Tests — allows testing constants without making them public API
- [Phase 03]: HumlParseException placed in Huml.Net.Exceptions (not Huml.Net.Lexer.Exceptions) — thrown by both Lexer and Parser
- [Phase 03]: Token.Value is string? (nullable) so structural tokens carry null, eliminating heap allocations on the hot path
- [Phase 03]: No binary serialisation constructor on HumlParseException — SYSLIB0051 pattern from Phase 02 maintained
- [Phase 03]: Test namespace collision resolved with using alias: Huml.Net.Tests.Lexer namespace shadows Lexer class — using HumlLexer = Huml.Net.Lexer.Lexer in test files
- [Phase 04]: ScalarKind.Integer (not Int) — asymmetry from TokenType.Int for semantic clarity at AST level
- [Phase 04]: HumlScalar.Value is object? — accommodates heterogeneous runtime values (string, long, double, bool, null) without generics
- [Phase 04]: IReadOnlyList<HumlNode> for collection nodes — readonly contract with reference equality for structural equality tests
- [Phase 05]: Inline vs multiline vector dispatch uses VectorIndicator.Line vs next-token.Line comparison — Key tokens always have SpaceBefore=false so SpaceBefore is unreliable
- [Phase 05]: Lexer no longer throws for digit at line-start; root integer/float scalars valid; integer-as-key error is parser-level
- [Phase 05]: Each nesting level consumes 2 depth units (ParseVector + ParseMultilineDict both guard) — WithinDepthLimit tests must use MaxRecursionDepth >= 2x nesting levels
- [Phase 05]: HumlParser constructor parameter maxDepth removed; parser reads options.MaxRecursionDepth directly to keep API consistent
- [Phase 06]: PropertyDescriptor uses BaseType chain walk (root-first) so base properties always precede derived properties
- [Phase 06]: HumlScalar.Value for Inf/NaN now carries raw token string ('+inf'/'-inf'/'nan') not null — deserializer needs sign to distinguish PositiveInfinity from NegativeInfinity
- [Phase 06]: Test for -inf uses mapping value (key: -inf) not root scalar — '-' at col 0 is ListItem token, not sign prefix
- [Phase 06]: IEnumerable<T> dispatch checks typeDef == typeof(IEnumerable<>) before GetInterface() — GetInterface() returns null when targetType IS the interface
- [Phase 06]: HumlDeserializer.Deserialize(string, Type) untyped overload is internal — Phase 7 public API delegates to it
- [Phase 06]: HumlSerializer is internal static class; SerializeValue dispatches by exact CLR type for correct priority (string before IEnumerable)
- [Phase 06]: EmitEntry materializes IEnumerable into List<object?> once to check empty without double-enumerating
- [Phase 07]: Huml.Deserialize<T>(string) calls Deserialize<T>(huml.AsSpan(), options) — never HumlDeserializer directly — preserving single lexer path (API-02)
- [Phase 07]: Parser consumes optional %HUML version token at document start; pre-existing gap exposed by facade round-trip tests
- [Phase 07]: cref attributes referencing Huml.Net.Exceptions.* use T: prefix to avoid ambiguity with new Huml class name
- [Phase 07]: Fixture JSON loaded with PropertyNameCaseInsensitive=true because fixture files use lowercase names
- [Phase 07]: V01Options uses VersionSource.Options to force v0.1 rules regardless of any %HUML header in fixture input
- [Phase 07]: IsFloatSpan excludes hex/octal/binary numbers (0x/0o/0b prefix) to prevent FormatException
- [Phase 07]: Root inline dict support in ParseMappingEntries via inlineLine tracking enforces single-line constraint
- [Phase 07.1]: Parser sets _lexer.EffectiveSpecVersion immediately after ApplyVersionFromHeader so backtick gate in lexer uses correct version for the rest of document
- [Phase 07.1]: TryExtractMajorMinor uses int.TryParse with CultureInfo.InvariantCulture to satisfy Meziantou MA0011 analyzer rule (TreatWarningsAsErrors)
- [Phase 07.1]: UsePrevious below-minimum check uses majorMinor.minor < 1 for major=0 (identifies 0.0.x as below v0.1 floor); no enum cast arithmetic
- [Phase 07.2]: AppendEscapedString uses char-by-char switch direct to StringBuilder — eliminates 5 intermediate string allocations per serialised string value
- [Phase 07.2]: PropertyDescriptor.DefaultValue caches Activator.CreateInstance at build time — OmitIfDefault check is now O(1) with no heap allocation per emit
- [Phase 07.2]: EmitSequenceItems(IEnumerable) is the single sequence emitter — SerializeSequenceInline, SerializeSequenceBody, SerializeDictionaryInline deleted
- [Phase 07.2]: MA0015 analyzer suppressed with pragma for nameof(MaxRecursionDepth) in init accessor — property name is more informative than 'value' for error messages
- [Phase 07.2]: HumlUnsupportedVersionException moved to Huml.Net.Exceptions namespace (canonical) — consistent with HumlParseException placement from Phase 03

### Roadmap Evolution

- Phase 07.1 inserted after Phase 7: Version header parsing and versioning completeness (INSERTED)
- Phase 07.2 inserted after Phase 7: Code quality, API accuracy and performance optimisations (INSERTED)
- Phase 07.3 inserted after Phase 7: Unicode and RTL support with fixture extensions (INSERTED)

### Pending Todos

None yet.

### Blockers/Concerns

- Phase 5 (Parser): HUML grammar spec details (indent rules, inline list syntax per version) are MEDIUM confidence — `huml-lang/go-huml` source must be consulted during planning
- Phase 6 (Ser/Deser): `init`-only constructor-binding design decision not yet made (throw vs constructor fallback); decide at Phase 6 planning time
- Phase 7 (Fixture Compliance): `huml-lang/tests` fixture file format (valid vs invalid subdirectory layout, expectation format) must be inspected from submodule content before writing `SharedSuiteTests.cs`

## Session Continuity

Last session: 2026-03-22T10:15:42.951Z
Stopped at: Completed 07.2-02-PLAN.md
Resume file: None
