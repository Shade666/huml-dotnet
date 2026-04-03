---
gsd_state_version: 1.0
milestone: v0.1
milestone_name: "coverage. Extend Phase 07.5 inline tests from NotThrow-only to full value-equality round-trips.**Requirements**: MIX-01, MIX-02, MIX-03, MIX-04, MIX-05**Depends on:** Phase 07.5**Plans:** 1/1 plans complete"
status: unknown
stopped_at: Completed 07.15-01-PLAN.md
last_updated: "2026-04-03T22:15:43.664Z"
last_activity: 2026-04-03
progress:
  total_phases: 30
  completed_phases: 22
  total_plans: 34
  completed_plans: 34
---

# Project State

## Project Reference

See: .planning/PROJECT.md (updated 2026-03-20)

**Core value:** Full HUML spec compliance (v0.1 + v0.2), validated against the shared `huml-lang/tests` test suite, with zero external runtime dependencies and a `System.Text.Json`-style API that .NET developers already know.
**Current focus:** Phase 07.15 — cache-indent-strings-in-humlserializer-to-eliminate-allocations

## Current Position

Phase: 999.5
Plan: Not started

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
| Phase 07.2 P03 | 3min | 2 tasks | 5 files |
| Phase 07.3 P01 | 3min | 2 tasks | 3 files |
| Phase 07.3 P02 | 8min | 2 tasks | 8 files |
| Phase 07.4 P01 | 5min | 2 tasks | 3 files |
| Phase 07.5 P01 | 7 | 2 tasks | 6 files |
| Phase 07.5-inline-serialisation-support-via-humloptions-and-humlproperty P02 | 3min | 2 tasks | 2 files |
| Phase 07.6 P01 | 5 | 2 tasks | 1 files |
| Phase 07.7 P01 | 3min | 2 tasks | 7 files |
| Phase 07.7 P02 | 5min | 2 tasks | 1 files |
| Phase 07.8 P01 | 3min | 2 tasks | 6 files |
| Phase 07.9 P01 | 2min | 2 tasks | 5 files |
| Phase 07.10 P01 | 2min | 2 tasks | 2 files |
| Phase 07.10 P02 | 3 | 2 tasks | 3 files |
| Phase 07.11 P01 | 2min | 2 tasks | 2 files |
| Phase 07.12 P01 | 3min | 1 tasks | 1 files |
| Phase 07.13 P01 | 1min | 1 tasks | 2 files |
| Phase 07.14 P01 | 7min | 2 tasks | 3 files |
| Phase 07.15 P01 | 2min | 1 tasks | 2 files |

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
- [Phase 07.2]: HumlInlineMapping extends HumlNode directly (not HumlDocument) — no shared abstract base; root always returns HumlDocument, inline/empty mapping blocks return HumlInlineMapping
- [Phase 07.2]: DeserializeMappingEntries(IReadOnlyList<HumlNode>) shared helper eliminates duplication between HumlDocument and HumlInlineMapping deserializer dispatch paths
- [Phase 07.3]: char.IsLetter error branch placed AFTER all acceptance branches — cannot affect the acceptance path which uses private IsLetter (ASCII-only)
- [Phase 07.3]: UnicodePoco uses ASCII property names to bypass serializer quoted-key gap (D-08) — Unicode property names in POCOs would fail round-trip
- [Phase 07.3]: fixtures/extensions/ is a plain tracked directory (not a git submodule) committed directly to the repo
- [Phase 07.3]: Extension fixtures integrate transparently into existing V01_fixture_passes and V02_fixture_passes Theory runs via LoadFixtures extension scan with Directory.Exists guard (D-01, D-02)
- [Phase 07.3]: D-03 verified: ambiguous_empty_vector_bare Theory row passes (HumlParseException thrown for 'key::' as expected)
- [Phase 07.4]: NeedsQuoting mirrors Lexer bare-key grammar [a-zA-Z][a-zA-Z0-9_-]*; AppendKey reuses AppendEscapedString for consistent escape semantics; all 6 EmitEntry key-emission sites replaced with AppendKey(sb, key)
- [Phase 07.5]: InlineMode enum replaces planned bool? on HumlPropertyAttribute.Inline — C# attribute named argument/constructor types cannot be nullable (CS0655/CS0181); enum with Inherit=0 achieves identical semantics
- [Phase 07.5]: PropertyDescriptor.Inline remains bool? — converted from InlineMode via switch at BuildDescriptors time, keeping serializer dispatch simple
- [Phase 07.5]: InlineMode in separate InlineMode.cs file — Meziantou MA0048 enforces one-type-per-file under TreatWarningsAsErrors
- [Phase 07.5]: SerializeDictionaryBody passes inlineOverride: false to EmitEntry — inline is a POCO-property-level concern, not recursive into dict body entries; inner scalar lists within a complex dict body emit multiline
- [Phase 07.6]: BeApproximately(6.022e+23, 1e+17) used for large-exponent float round-trip; exact .Be() works for all others
- [Phase 07.6]: SpecialKeysWrapper/EdgeCaseKeysWrapper POCOs wrap Dictionary<string,string> for quoted-key round-trip tests
- [Phase 07.7]: README uses primeBeri badge URLs pre-rename; badges will be broken until GitHub account rename completes
- [Phase 07.7]: CHANGELOG [0.1.0] date left as YYYY-MM-DD placeholder for Phase 8 tag creation
- [Phase 07.7]: docs/ast-usage.md includes HumlInlineMapping section with pattern-match examples rather than deferring to XML doc
- [Phase 07.7]: NuGet author changed to primeBeri for public release; Description expanded to pitch-level copy; docs/inline-serialisation.md verified accurate; API-05 SourceLink confirmed in Directory.Build.props
- [Phase 07.8]: HumlOptions.Default reassigned to VersionSource.Header; LatestSupported added for pinned-v0.2 behaviour; AutoDetect is reference-equal alias to Default
- [Phase 07.9]: MaxRecursionDepth default lowered 512→64 to match System.Text.Json.JsonSerializerOptions.MaxDepth convention and bound adversarial inputs
- [Phase 07.9]: MaxRecursionDepth valid range tightened [1,65536]→[1,1024]; 1024 is generous for real HUML documents while bounding stack usage
- [Phase 07.10]: CONTRIBUTING.md omits FluentAssertions brand name in the do-not-use warning to satisfy acceptance criteria grep check; AwesomeAssertions 9.4.0 named exclusively
- [Phase 07.10]: BACKLOG.md starts empty; Phase 999.5 internal item not exposed to public contributors
- [Phase 07.10]: File paths referencing src/Huml.Net/Serialization/ use actual directory name (American spelling); all prose uses British English
- [Phase 07.10]: docs/internals/ directory established as contributor-facing internals guide home with pipeline.md, version-gates.md, extending.md
- [Phase 07.11]: Serialize(object?,Type) overload expanded from one-liner to full implementation, threading declaredType to SerializeMappingBody; declaredType NOT propagated through recursive calls so nested POCOs use runtime type (System.Text.Json contract)
- [Phase 07.12]: DOC-SPAN-01: remarks block on ReadOnlySpan<char> Deserialize names ToString() allocation, states heap not avoided today, explains lexer-string constraint, references v2 zero-copy enhancement
- [Phase 07.13]: HumlDocument <summary> revised from 'root document only' to explicitly cover both document root and nested :: vector blocks; HumlInlineMapping <remarks> added to remove Phase 07.2 inaccuracy
- [Phase 07.14]: PropertyDescriptorCache private sealed record holds both Ordered (PropertyDescriptor[]) and ByKey (Dictionary<string,PropertyDescriptor>) — single ConcurrentDictionary entry, ClearCache clears both atomically
- [Phase 07.14]: GetDescriptors returns .Ordered for serialiser (unchanged contract); GetLookup returns .ByKey for O(1) deserialiser dispatch — StringComparer.Ordinal matches replaced string.Equals semantics
- [Phase 07.15]: IndentCache covers indices 0-64 (65 entries) matching MaxRecursionDepth default of 64; (uint)depth cast handles negative depth safely; BeLessOrEqualTo not in AwesomeAssertions so BeLessThan(alloc1+512) used with noise margin

### Roadmap Evolution

- Phase 07.1 inserted after Phase 7: Version header parsing and versioning completeness (INSERTED)
- Phase 07.2 inserted after Phase 7: Code quality, API accuracy and performance optimisations (INSERTED)
- Phase 07.3 inserted after Phase 7: Unicode and RTL support with fixture extensions (INSERTED)
- Phase 07.4 inserted after Phase 07.3: Fix HumlSerializer key-quoting for non-ASCII dictionary keys (INSERTED)
- Phase 07.5 inserted after Phase 07.4: Inline serialisation support via HumlOptions and HumlProperty (INSERTED)
- Phase 07.6 inserted after Phase 07.5: Comprehensive round-trip tests against mixed fixture files (INSERTED)
- Phase 07.7 inserted after Phase 07.6: Documentation suite for first public NuGet release (INSERTED)
- Phase 07.10 added: Complete missing contributor and developer-internals documentation (CONTRIBUTING.md, BACKLOG.md, docs/internals/)

### Pending Todos

None yet.

### Blockers/Concerns

- Phase 5 (Parser): HUML grammar spec details (indent rules, inline list syntax per version) are MEDIUM confidence — `huml-lang/go-huml` source must be consulted during planning
- Phase 6 (Ser/Deser): `init`-only constructor-binding design decision not yet made (throw vs constructor fallback); decide at Phase 6 planning time
- Phase 7 (Fixture Compliance): `huml-lang/tests` fixture file format (valid vs invalid subdirectory layout, expectation format) must be inspected from submodule content before writing `SharedSuiteTests.cs`

### Quick Tasks Completed

| # | Description | Date | Commit | Directory |
|---|-------------|------|--------|-----------|
| 260324-9sp | Populate BACKLOG.md with architecture review findings classified as V1 or V2 | 2026-03-24 | ebe595b | [260324-9sp-populate-backlog-md-with-architecture-re](./quick/260324-9sp-populate-backlog-md-with-architecture-re/) |

## Session Continuity

Last activity: 2026-04-03
Last session: 2026-04-03T22:13:33.621Z
Stopped at: Completed 07.15-01-PLAN.md
Resume file: None
