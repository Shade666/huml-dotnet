# Roadmap: Huml.Net

## Overview

Huml.Net ships in 8 phases that follow the strict component dependency graph of the Lexer -> Parser -> AST -> Serializer/Deserializer pipeline. Phase 1 establishes the CI/packaging foundations that are hardest to fix after a public release. Phases 2-6 build the library from the inside out (versioning types, then lexer, then AST, then parser, then serializer/deserializer). Phase 7 wires the full pipeline through the static entry point and certifies spec compliance against the shared `huml-lang/tests` fixture suite. Phase 8 hardens and publishes the first NuGet release.

## Phases

**Phase Numbering:**
- Integer phases (1, 2, 3): Planned milestone work
- Decimal phases (2.1, 2.2): Urgent insertions (marked with INSERTED)

Decimal phases appear between their surrounding integers in numeric order.

- [x] **Phase 1: Project Scaffold and CI Foundations** - Compilable multi-TFM solution with green CI, SourceLink, and shared fixture submodules (completed 2026-03-20)
- [x] **Phase 2: Versioning Foundation** - Full versioning type hierarchy with support-window enforcement (completed 2026-03-20)
- [x] **Phase 3: Lexer and Token Types** - Single-pass `ReadOnlySpan<char>` lexer producing typed tokens with version-gated rules (completed 2026-03-21)
- [x] **Phase 4: AST Node Hierarchy** - Immutable `abstract record` AST tree consumed by parser and future linting package (completed 2026-03-21)
- [x] **Phase 5: Parser** - Recursive-descent parser covering full HUML v0.1 and v0.2 grammar with depth-limit guard (completed 2026-03-21)
- [x] **Phase 6: Attributes and Serializer/Deserializer** - Reflection-based serialization and deserialization with attribute-driven mapping (completed 2026-03-21)
- [x] **Phase 7: Static Entry Point and Shared Fixture Compliance** - `Huml` static class wiring all pipeline stages; CI passes all fixture suite tests (completed 2026-03-21)
- [ ] **Phase 8: NuGet Release Preparation** - Verified multi-TFM package with SourceLink, XML docs, and first NuGet publish

## Phase Details

### Phase 1: Project Scaffold and CI Foundations
**Goal**: A working multi-TFM build, test, and packaging pipeline exists before any library code is written, eliminating the operational failure modes that are most costly to recover from after a public release
**Depends on**: Nothing (first phase)
**Requirements**: INFRA-01, INFRA-02, INFRA-03, INFRA-04, INFRA-05
**Success Criteria** (what must be TRUE):
  1. `dotnet build` succeeds across all four TFMs (`netstandard2.1`, `net8.0`, `net9.0`, `net10.0`) from a clean checkout with no warnings (TreatWarningsAsErrors active)
  2. `dotnet test` runs and the sentinel `[Fact]` asserting non-empty fixture directories passes, proving git submodules (`tests@v0.1` and `tests@v0.2`) are initialised in CI
  3. `dotnet pack` produces a `.nupkg` with all four TFM entries in `lib/` (verified by NuGet Package Explorer or `unzip` inspection)
  4. `dotnet sourcelink test` passes in CI, confirming SourceLink metadata contains real commit SHAs rather than zeros
  5. The NuGet publish workflow completes a dry-run without requiring a long-lived API key (OIDC Trusted Publishing configured)
**Plans:** 2/2 plans complete
Plans:
- [x] 01-01-PLAN.md — Solution scaffold, git submodules, and sentinel fixture guard tests (INFRA-01, INFRA-02, INFRA-03)
- [x] 01-02-PLAN.md — GitHub Actions CI and OIDC NuGet publish workflows (INFRA-04, INFRA-05)

### Phase 2: Versioning Foundation
**Goal**: All versioning types (`HumlSpecVersion`, `HumlOptions`, `SpecVersionPolicy`, `VersionSource`, `UnknownVersionBehaviour`) are locked before pipeline code is written, so the version-gating strategy cannot require retroactive refactoring
**Depends on**: Phase 1
**Requirements**: VER-01, VER-02, VER-03, VER-04, VER-05
**Success Criteria** (what must be TRUE):
  1. `HumlOptions.Default` and `HumlOptions.AutoDetect` can be instantiated and their properties read without error
  2. Constructing a `HumlOptions` with a version outside the support window and `UnknownVersionBehaviour.Throw` results in `HumlUnsupportedVersionException` being thrown with the declared version string in the message
  3. The IDE shows a deprecation warning (squiggle) when referencing `HumlSpecVersion.V0_1` directly, and the `[Obsolete]` message points to v0.2
  4. `SpecVersionPolicy.MinimumSupported` and `SpecVersionPolicy.Latest` constants are accessible from `HumlUnsupportedVersionException`'s message without manual string duplication
**Plans:** 1/1 plans complete
Plans:
- [x] 02-01-PLAN.md — Versioning types, exceptions, and unit tests (VER-01, VER-02, VER-03, VER-04, VER-05)

### Phase 3: Lexer and Token Types
**Goal**: A single-pass lexer accepts `ReadOnlySpan<char>` and produces a fully-typed token stream; the `Token` struct and `HumlParseException` contracts are locked before the parser couples to them
**Depends on**: Phase 2
**Requirements**: LEX-01, LEX-02, LEX-03, LEX-04, LEX-05, LEX-06
**Success Criteria** (what must be TRUE):
  1. Lexing a valid HUML v0.2 document produces a token stream where each token carries correct `TokenType`, `Line`, `Column`, `Indent`, and `SpaceBefore` values (verified by unit tests against known inputs)
  2. Lexing a document with a tab character for indentation produces a `HumlParseException` whose `Line` and `Column` int properties identify the exact position
  3. Lexing input that is valid HUML v0.1 but invalid in v0.2 (or vice versa) produces different results depending on `HumlOptions.SpecVersion`, confirming version-gated rules are active
  4. No `ToString()` or heap allocation occurs on the hot lexer path for a document containing only ASCII characters (confirmed by allocation unit test or benchmark)
**Plans:** 2/2 plans complete
Plans:
- [x] 03-01-PLAN.md — Token contracts: TokenType enum, Token struct, HumlParseException (LEX-01, LEX-02, LEX-06)
- [x] 03-02-PLAN.md — Lexer implementation with full tokenisation, version gating, and allocation tests (LEX-03, LEX-04, LEX-05)

### Phase 4: AST Node Hierarchy
**Goal**: The immutable `abstract record` AST node hierarchy is defined and structurally verified, so the parser phase can focus purely on grammar without debating node shapes
**Depends on**: Phase 3
**Requirements**: PARS-01, PARS-02
**Success Criteria** (what must be TRUE):
  1. `HumlDocument`, `HumlMapping`, `HumlSequence`, and `HumlScalar` nodes can be constructed directly in tests and compared with `==` (structural equality via `record`)
  2. All seven `ScalarKind` values (`String`, `Integer`, `Float`, `Bool`, `Null`, `NaN`, `Inf`) are representable as `HumlScalar` nodes with the correct `Kind` and `Value` combination
  3. The node hierarchy compiles with `TreatWarningsAsErrors` across all TFMs without any nullability warnings on `Value` properties
**Plans:** 1/1 plans complete
Plans:
- [x] 04-01-PLAN.md — AST node hierarchy and ScalarKind enum with TDD tests (PARS-01, PARS-02)

### Phase 5: Parser
**Goal**: A recursive-descent parser consumes the token stream and produces a `HumlDocument` AST covering the full HUML v0.1 and v0.2 grammar, with an explicit depth limit preventing unrecoverable `StackOverflowException`
**Depends on**: Phase 4
**Requirements**: PARS-03, PARS-04, PARS-05
**Success Criteria** (what must be TRUE):
  1. Parsing a representative valid HUML v0.2 document (containing scalar values, vector blocks, inline lists, and nested mappings) produces a `HumlDocument` whose node structure matches the expected AST shape
  2. Parsing the same document with `SpecVersion` set to `V0_1` produces a different result where v0.2-only constructs are rejected, confirming version-gated grammar branches are active
  3. Parsing a pathologically nested document (depth > 512) throws `HumlParseException` with a clear recursion-depth message rather than crashing the process
  4. All parser unit tests pass across all TFMs in CI
**Plans:** 2/2 plans complete
Plans:
- [x] 05-01-PLAN.md — TDD recursive-descent parser with full grammar coverage and version gating (PARS-03, PARS-04)
- [x] 05-02-PLAN.md — Configurable recursion depth limit with depth-guard tests (PARS-05)

### Phase 6: Attributes and Serializer/Deserializer
**Goal**: Attribute-driven, reflection-based serialization and deserialization round-trips .NET objects through HUML text with declaration-order property emission and correct handling of `init`-only properties
**Depends on**: Phase 5
**Requirements**: SER-01, SER-02, SER-03, SER-04, SER-05, SER-06, SER-07
**Success Criteria** (what must be TRUE):
  1. A POCO decorated with `[HumlProperty("renamed")]` and `[HumlIgnore]` round-trips through serialize -> deserialize with the renamed key appearing in HUML output and the ignored property absent
  2. Properties are emitted in source declaration order (not alphabetically) -- verified by serializing a POCO whose declaration order differs from alphabetical order and asserting the output key sequence
  3. Deserializing a HUML document into a POCO that has `init`-only properties throws `HumlDeserializeException` with a clear message identifying the offending property rather than silently skipping it or throwing a runtime `MethodAccessException`
  4. All supported collection types (`List<T>`, `T[]`, `IEnumerable<T>`, `Dictionary<string,T>`) and primitive scalars survive a serialize -> deserialize round-trip with value equality
  5. A type-coercion failure (e.g., mapping a HUML string to an `int` property) throws `HumlDeserializeException` with the offending key and line number in the message
**Plans:** 3/3 plans complete
Plans:
- [x] 06-01-PLAN.md — Contracts: attributes, exceptions, PropertyDescriptor cache, and Inf sign fix (SER-01, SER-02, SER-03, SER-07)
- [x] 06-02-PLAN.md — TDD HumlSerializer: object to HUML text with declaration-order emission (SER-03, SER-04)
- [x] 06-03-PLAN.md — TDD HumlDeserializer: HUML AST to .NET object with collection dispatch (SER-05, SER-06)

### Phase 7: Static Entry Point and Shared Fixture Compliance
**Goal**: The `Huml` static class wires the complete pipeline; CI passes all `huml-lang/tests` fixtures for both v0.1 and v0.2 with a verified non-zero Theory count, certifying full spec compliance
**Depends on**: Phase 6
**Requirements**: API-01, API-02, API-03, API-06
**Success Criteria** (what must be TRUE):
  1. `Huml.Serialize<T>(value)` and `Huml.Deserialize<T>(humlString)` are callable from a consumer project that references only the `Huml` static class with no visibility into internal types
  2. `Huml.Deserialize<T>(ReadOnlySpan<char> huml, ...)` is the single implementation overload; `Huml.Deserialize<T>(string huml, ...)` is a thin `AsSpan()` wrapper -- confirmed by inspecting that only one overload reaches the Lexer
  3. All xUnit Theory rows from both `huml-lang/tests@v0.1` and `huml-lang/tests@v0.2` fixture suites pass in CI with a non-zero Theory count logged for each version
  4. All public members of `Huml`, `HumlOptions`, `HumlSpecVersion`, `HumlParseException`, `HumlSerializeException`, `HumlDeserializeException`, `HumlUnsupportedVersionException`, `[HumlProperty]`, and `[HumlIgnore]` carry XML doc comments visible in IntelliSense
**Plans:** 2/2 plans complete
Plans:
- [x] 07-01-PLAN.md — Huml static facade with XML doc comments and unit tests (API-01, API-02, API-03)
- [x] 07-02-PLAN.md — SharedSuiteTests Theory runner for v0.1 and v0.2 fixture compliance (API-06, API-03)

### Phase 07.1: Version header parsing and versioning completeness (INSERTED)

**Goal:** The versioning subsystem (HumlOptions.AutoDetect, VersionSource.Header, UnknownVersionBehaviour, HumlUnsupportedVersionException) is wired into the runtime parse path so that %HUML version headers are read, validated, and propagated to both parser and lexer version gates
**Requirements**: VER-HEADER-01, VER-HEADER-02, VER-HEADER-03, VER-HEADER-04, VER-HEADER-05, VER-HEADER-06
**Depends on:** Phase 7
**Plans:** 1/1 plans complete

Plans:
- [x] 07.1-01-PLAN.md — Wire version header detection in Lexer and Parser with TDD tests (VER-HEADER-01, VER-HEADER-02, VER-HEADER-03, VER-HEADER-04, VER-HEADER-05, VER-HEADER-06)

### Phase 07.2: Code quality, API accuracy and performance optimisations (INSERTED)

**Goal:** Fix misleading public API documentation, eliminate internal code duplication, improve hot-path serialisation performance, introduce HumlInlineMapping to disambiguate root documents from inline mapping blocks, and consolidate exception namespaces -- all pre-v1 housekeeping with no consumer-facing behaviour changes (except the intentional HumlInlineMapping AST break)
**Requirements**: CQ-01, CQ-02, CQ-03, CQ-04, CQ-05
**Depends on:** Phase 07.1
**Plans:** 3/3 plans complete

Plans:
- [x] 07.2-01-PLAN.md — Serializer performance: AppendEscapedString, DefaultValue caching, sequence deduplication (CQ-01, CQ-03)
- [x] 07.2-02-PLAN.md — Infrastructure cleanup: iterative MeasureIndent, InferDictRootType removal, namespace move, API docs, MaxRecursionDepth validation (CQ-02, CQ-04)
- [x] 07.2-03-PLAN.md — HumlInlineMapping AST semantic split with parser, deserializer, and test updates (CQ-05)

### Phase 07.3: Unicode and RTL support with fixture extensions (INSERTED)

**Goal:** Actionable error message for non-ASCII bare keys, fixture extension infrastructure (`fixtures/extensions/`) for local test cases not yet in upstream suite, comprehensive Unicode/RTL test coverage, and a repeatable fixture-gap audit rule
**Requirements**: UNI-01, UNI-02, UNI-03, UNI-04, UNI-05, UNI-06
**Depends on:** Phase 07.2
**Plans:** 2/2 plans complete

Plans:
- [x] 07.3-01-PLAN.md — Lexer error branch + IsLetter XML doc + LexerUnicodeTests (13 tests) + ParserUnicodeTests (8 tests) (UNI-01, UNI-02, UNI-03, UNI-04)
- [ ] 07.3-02-PLAN.md — Fixture extension infrastructure (gaps.json, unicode.json, SharedSuiteTests extension, .csproj, fixture-gaps.md rule) (UNI-05, UNI-06)

### Phase 07.4: Fix HumlSerializer key-quoting for non-ASCII dictionary keys (INSERTED)

**Goal:** `HumlSerializer.EmitEntry` emits quoted keys (`"key": value`) when the key string fails the bare-key grammar `[a-zA-Z][a-zA-Z0-9_-]*`, fixing invalid HUML output for `Dictionary<string, T>` with non-ASCII, digit-start, space-containing, or empty-string keys. Resolves tracked defect D-08.
**Requirements**: SER-KEY-01, SER-KEY-02, SER-KEY-03, SER-KEY-04, SER-KEY-05, SER-KEY-06, SER-KEY-07
**Depends on:** Phase 07.3
**Plans:** 1/1 plans complete

Plans:
- [x] 07.4-01-PLAN.md — TDD key-quoting fix: NeedsQuoting/AppendKey helpers + 7 tests + D-08 comment update (SER-KEY-01, SER-KEY-02, SER-KEY-03, SER-KEY-04, SER-KEY-05, SER-KEY-06, SER-KEY-07)

### Phase 07.5: Inline serialisation support via HumlOptions and HumlProperty (INSERTED)

**Goal:** Add `CollectionFormat` enum to `HumlOptions` (global default: `Multiline`), add `bool? Inline` to `[HumlProperty]` for per-property override. Scalar-only collections emit inline when opted-in; complex-valued collections silently fall back to multiline. Include XML doc comments on all new public members and a `docs/inline-serialisation.md` usage guide.
**Requirements**: INL-01, INL-02, INL-03, INL-04, INL-05, INL-06, INL-07, INL-08, INL-09, INL-10, INL-11, INL-12, INL-13
**Depends on:** Phase 07.4
**Plans:** 2/2 plans complete

Plans:
- [x] 07.5-01-PLAN.md — Contracts: CollectionFormat enum, HumlOptions.CollectionFormat, HumlPropertyAttribute.Inline, PropertyDescriptor.Inline cache, test stubs (INL-01, INL-02, INL-03, INL-04, INL-07, INL-08, INL-12)
- [x] 07.5-02-PLAN.md — Implementation: EmitEntry inline dispatch, EmitInlineSequence, EmitInlineDictionary, all tests green, docs/inline-serialisation.md (INL-05, INL-06, INL-07, INL-08, INL-09, INL-10, INL-11, INL-12, INL-13)

### Phase 07.6: Comprehensive round-trip tests against mixed fixture files (INSERTED)**Goal:** Add comprehensive round-trip tests that exercise fixtures/v0.2/documents/mixed.huml and fixtures/v0.2/documents/mixed.json as a kitchen-sink harness, plus equivalent v0.1 coverage. Extend Phase 07.5 inline tests from NotThrow-only to full value-equality round-trips.**Requirements**: MIX-01, MIX-02, MIX-03, MIX-04, MIX-05**Depends on:** Phase 07.5**Plans:** 1/1 plans complete

### Phase 07.7: Documentation suite for first public NuGet release (INSERTED)

**Goal:** Write and publish all documentation that ships with the first public NuGet release: production-quality README.md, CHANGELOG.md, docs/ guides covering the full API surface, and corrected NuGet metadata
**Requirements**: API-04, API-05
**Depends on:** Phase 07.6
**Plans:** 2/2 plans complete

Plans:
- [x] 07.7-01-PLAN.md — README.md, CHANGELOG.md, and 5 new docs/ guides (API-04)
- [x] 07.7-02-PLAN.md — Huml.Net.csproj metadata patch, inline-serialisation review, dotnet pack verification (API-04, API-05)

### Phase 07.8: Make HumlOptions.Default use AutoDetect behaviour — rename current Default to LatestSupported (INSERTED)

**Goal:** `HumlOptions.Default` should read the `%HUML` header and fall back to v0.2 when absent (`AutoDetect` behaviour), since ignoring the header by default silently misclassifies v0.1 documents. The current pinned-to-v0.2 behaviour is preserved as `HumlOptions.LatestSupported`. XML docs and `docs/options-reference.md` updated accordingly.
**Requirements:** OPT-01, OPT-02, OPT-03, OPT-04, OPT-05
**Depends on:** Phase 07.7
**Plans:** 1/1 plans complete

Plans:
- [x] 07.8-01-PLAN.md — Reassign Default to header-aware, add LatestSupported, alias AutoDetect, update tests and docs (OPT-01, OPT-02, OPT-03, OPT-04, OPT-05)

### Phase 07.9: Lower MaxRecursionDepth default from 512 to 64 and tighten valid range upper bound (INSERTED)

**Goal:** Change `MaxRecursionDepth` default from 512 to 64, matching `System.Text.Json` convention and .NET developer expectations. Config files never exceed ~15 levels of nesting; 512 allows adversarial input to recurse far deeper than necessary before the guard fires. Also tighten the valid range upper bound from 65536 to a more defensible ceiling (1024). Update XML docs and `docs/options-reference.md` accordingly.
**Requirements:** DEPTH-01, DEPTH-02, DEPTH-03
**Depends on:** Phase 07.8
**Plans:** 1/1 plans complete

Plans:
- [x] 07.9-01-PLAN.md — Lower MaxRecursionDepth default to 64, tighten upper bound to 1024, update tests and docs (DEPTH-01, DEPTH-02, DEPTH-03)

### Phase 07.10: Complete missing contributor and developer-internals documentation (INSERTED)

**Goal:** Write the remaining documentation from `PLAN-documentation.md` that was not included in Phase 07.7: `CONTRIBUTING.md`, `BACKLOG.md`, and the three `docs/internals/` guides (`pipeline.md`, `version-gates.md`, `extending.md`) — completing the full documentation suite for the first public release
**Requirements:** DOC-01, DOC-02, DOC-03, DOC-04, DOC-05
**Depends on:** Phase 07.9
**Plans:** 2/2 plans complete

Plans:
- [x] 07.10-01-PLAN.md — CONTRIBUTING.md and BACKLOG.md (DOC-01, DOC-02)
- [x] 07.10-02-PLAN.md — docs/internals/ guides: pipeline.md, version-gates.md, extending.md (DOC-03, DOC-04, DOC-05)

### Phase 07.11: Fix Serialize(object?, Type) ignores Type parameter (INSERTED)

**Goal:** Implement type-directed dispatch in `HumlSerializer.Serialize(object?, Type, HumlOptions?)` so that the declared `Type` parameter governs property reflection rather than the runtime type. Fixes a silent API contract violation where polymorphic callers (serialising a derived instance via a base `Type`) receive the wrong property set. Mirrors the `System.Text.Json` contract exactly.
**Requirements:** SER-TYPE-01, SER-TYPE-02
**Depends on:** Phase 07.10
**Plans:** 1/1 plans complete

Plans:
- [ ] 07.11-01-PLAN.md — TDD fix: thread declared Type through SerializeMappingBody for polymorphic serialisation (SER-TYPE-01, SER-TYPE-02)

---

### Phase 07.12: Document ReadOnlySpan deserialise allocation at public API boundary (INSERTED)

**Goal:** Add `<remarks>` XML doc to `Huml.Deserialize<T>(ReadOnlySpan<char>, HumlOptions?)` (`Huml.cs`) clearly stating that the span is converted to a `string` internally and that genuine zero-copy is a V2 future enhancement. Sets correct consumer expectations; no behaviour change.
**Requirements:** DOC-SPAN-01
**Depends on:** Phase 07.11
**Plans:** 1/1 plans complete

Plans:
- [x] 07.12-01-PLAN.md — Verify and refine ReadOnlySpan remarks XML doc (DOC-SPAN-01)


---

### Phase 07.13: Document HumlDocument dual role as root and nested mapping block (INSERTED)

**Goal:** Add XML `<summary>` and `<remarks>` to `HumlDocument.cs` clarifying that the type represents both the document root AND nested multiline mapping blocks. Add a complementary remark to `HumlInlineMapping.cs` clarifying it is for inline `{k: v}` and empty `{}` notation only. Prevents future contributor confusion.
**Requirements:** DOC-AST-01
**Depends on:** Phase 07.12
**Plans:** 1/1 plans complete

Plans:
- [x] 07.13-01-PLAN.md — Update XML docs on HumlDocument and HumlInlineMapping for dual-role clarity (DOC-AST-01)

---

### Phase 07.14: Add property-lookup dictionary to PropertyDescriptor cache for O(1) deserialiser key lookup (INSERTED)

**Goal:** Extend `PropertyDescriptor.BuildDescriptors` to build a `Dictionary<string, PropertyDescriptor>` keyed by `HumlKey` alongside the existing ordered `PropertyDescriptor[]`. Cache both together in `ConcurrentDictionary`. Update `HumlDeserializer` to use dictionary O(1) lookup instead of the current O(n) `foreach` loop. The array is preserved for the serialiser (declaration-order traversal).
**Requirements:** PERF-DICT-01, PERF-DICT-02
**Depends on:** Phase 07.13
**Plans:** 1/1 plans complete

Plans:
- [x] 07.14-01-PLAN.md -- PropertyDescriptorCache record, GetLookup method, and DeserializeMappingEntries O(1) lookup (PERF-DICT-01, PERF-DICT-02)

---

### Phase 07.15: Cache indent strings in HumlSerializer to eliminate per-call allocation (INSERTED)

**Goal:** Replace the `Indent(int depth)` method in `HumlSerializer.cs` (currently `new string(' ', depth * 2)` on every invocation) with a pre-computed `static readonly string[]` of 65 entries (depth 0–64). Falls back to dynamic allocation only beyond depth 64. Eliminates O(n) identical string allocations for typical documents.
**Requirements:** PERF-INDENT-01
**Depends on:** Phase 07.14
**Plans:** 0 plans

Plans:
- [ ] TBD

---

### Phase 8: NuGet Release Preparation
**Goal**: The NuGet package is verified complete -- correct TFM coverage, working SourceLink, embedded XML docs, and a successful pre-release publish to NuGet.org via OIDC Trusted Publishing
**Depends on**: Phase 7
**Requirements**: API-04, API-05
**Success Criteria** (what must be TRUE):
  1. `dotnet pack -c Release` produces a `.nupkg` containing `lib/netstandard2.1`, `lib/net8.0`, `lib/net9.0`, and `lib/net10.0` entries (all four TFMs present -- no NU5128 warning)
  2. `dotnet sourcelink test Huml.Net.*.nupkg` passes, confirming embedded PDB has real commit SHAs and source-stepping works from NuGet
  3. The package manifest contains `PackageId`, `Authors`, `Description`, `PackageLicenseExpression` (MIT), `PackageTags`, `PackageProjectUrl`, `RepositoryUrl`, `PackageReadmeFile`, and `GenerateDocumentationFile` -- confirmed by inspecting the `.nuspec` inside the `.nupkg`
  4. A `0.1.0-alpha.1` pre-release tag triggers the NuGet publish workflow; the package appears on NuGet.org without manual API key entry
**Plans:** 0/? plans complete
Plans:
- [ ] To be planned

## Progress

**Execution Order:**
Phases execute in numeric order: 1 -> 2 -> 3 -> 4 -> 5 -> 6 -> 7 -> 8

| Phase                                                          | Plans Complete | Status      | Completed  |
| -------------------------------------------------------------- | -------------- | ----------- | ---------- |
| 1. Project Scaffold and CI Foundations                         | 2/2            | Complete    | 2026-03-20 |
| 2. Versioning Foundation                                       | 1/1            | Complete    | 2026-03-20 |
| 3. Lexer and Token Types                                       | 2/2            | Complete    | 2026-03-21 |
| 4. AST Node Hierarchy                                          | 1/1            | Complete    | 2026-03-21 |
| 5. Parser                                                      | 2/2            | Complete    | 2026-03-21 |
| 6. Attributes and Serializer/Deserializer                      | 3/3            | Complete    | 2026-03-21 |
| 7. Static Entry Point and Shared Fixture Compliance            | 2/2 | Complete   | 2026-03-24 |
| 07.8. HumlOptions.Default -> AutoDetect, LatestSupported rename | 1/1 | Complete    | 2026-03-23 |
| 07.9. Lower MaxRecursionDepth default to 64                    | 1/1 | Complete    | 2026-03-23 |
| 07.10. Contributor and internals documentation                 | 2/2 | Complete    | 2026-03-23 |
| 07.11. Fix Serialize(object?,Type) ignores Type parameter      | 0/? | Complete    | 2026-03-25 |
| 07.12. Document ReadOnlySpan deserialise allocation            | 1/1 | Complete   | 2026-04-03 |
| 07.13. Document HumlDocument dual role                         | 1/1 | Complete   | 2026-04-03 |
| 07.14. O(1) property-lookup dictionary in PropertyDescriptor   | 1/1 | Complete   | 2026-04-03 |
| 07.15. Cache indent strings in HumlSerializer                  | 0/? | Not started | -          |
| 8. NuGet Release Preparation                                   | 0/?            | Not started | -          |

## Backlog

### Phase 999.5: Version-preserving round-trip option for HumlSerializer (BACKLOG)

**Goal:** Add a serialiser mode that emits the same spec version that was detected during parsing, rather than always upgrading to the latest. Enables formatter and linter tools to round-trip documents transparently without silently upgrading v0.1 files to v0.2. Requires `HumlDocument` (or `HumlOptions`) to carry the detected version through the pipeline, and the serialiser to suppress v0.2-only syntax (e.g. backtick multilines) when targeting v0.1. Consider for next release after initial NuGet publish.
**Requirements:** TBD
**Plans:** 0 plans

Plans:
- [ ] TBD (promote with /gsd:review-backlog when ready)

---

### Phase 999.11: Pool StringBuilder in serialiser via [ThreadStatic] (BACKLOG)

**Goal:** Replace per-call `new StringBuilder()` allocation in `HumlSerializer` with a `[ThreadStatic]` pooled instance (or `ObjectPool<StringBuilder>` if the zero-dependency constraint permits). Reduces GC pressure for high-throughput serialisation scenarios. Requires careful `Clear()` discipline to avoid cross-call contamination.
**Source:** ArchitectureReview_20260324.md §5.1, §9 Phase 2 Task 2.3
**Version:** V2 | **Priority:** Medium | **Category:** Performance
**Requirements:** TBD
**Plans:** 0 plans

Plans:
- [ ] TBD (promote with /gsd:review-backlog when ready)

---

### Phase 999.12: Refactor Lexer to ref struct accepting ReadOnlySpan<char> for zero-copy deserialisation (BACKLOG)

**Goal:** Implement a `ref struct` `Lexer` variant that accepts `ReadOnlySpan<char>` as its core input, eliminating the `.ToString()` heap allocation in `HumlDeserializer.cs:38`. Thread the span path through `HumlParser` and `HumlDeserializer` so that `Huml.Deserialize<T>(ReadOnlySpan<char>)` produces no intermediate string allocation. The string overload delegates to the span path. Verify with allocation-count tests.
**Source:** ArchitectureReview_20260324.md §2.3, §5.1, §9 Phase 3 Tasks 3.1–3.2
**Version:** V2 | **Priority:** High | **Category:** Performance
**Requirements:** TBD
**Plans:** 0 plans

Plans:
- [ ] TBD (promote with /gsd:review-backlog when ready)

---

### Phase 999.13: Carry source position through AST nodes for richer HumlDeserializeException diagnostics (BACKLOG)

**Goal:** Add `Line` and `Column` properties to all AST node records (`HumlMapping`, `HumlScalar`, `HumlSequence`, `HumlDocument`). Propagate position from the Lexer's `Token` through the Parser into AST nodes. Update `HumlDeserializer` to use AST node positions when throwing `HumlDeserializeException`, replacing the current hardcoded `line: 0` with the actual source position.
**Source:** ArchitectureReview_20260324.md §7.3, §9 Phase 4 Task 4.1
**Version:** V2 | **Priority:** Medium | **Category:** Diagnostics
**Requirements:** TBD
**Plans:** 0 plans

Plans:
- [ ] TBD (promote with /gsd:review-backlog when ready)

---

### Phase 999.14: Add HumlOptions factory for header-detected latest-fallback variant (BACKLOG)

**Goal:** Add a `HumlOptions.LatestSupportedAutoDetect` static factory (or named instance) representing "read `%HUML` header, fall back to latest supported version". Currently this combination requires constructing a custom `HumlOptions` instance. The addition is additive — existing `Default`/`AutoDetect`/`LatestSupported` names are unchanged.
**Source:** ArchitectureReview_20260324.md §7.2, §9 Phase 4 Task 4.2
**Version:** V2 | **Priority:** Low | **Category:** API
**Requirements:** TBD
**Plans:** 0 plans

Plans:
- [ ] TBD (promote with /gsd:review-backlog when ready)

---

### Phase 999.15: Add concurrency test for PropertyDescriptor cache under parallel deserialisation (BACKLOG)

**Goal:** Add a test that calls `Huml.Deserialize<T>` from 16 concurrent threads simultaneously, exercising the `ConcurrentDictionary.GetOrAdd` race in `PropertyDescriptor.BuildDescriptors`. Assert no exceptions and no data corruption. Complements the existing `ClearCache()` test isolation pattern.
**Source:** ArchitectureReview_20260324.md §8.3, §9 Phase 4 Task 4.3
**Version:** V2 | **Priority:** Low | **Category:** Testing
**Requirements:** TBD
**Plans:** 0 plans

Plans:
- [ ] TBD (promote with /gsd:review-backlog when ready)

---

### Phase 999.17: Document uncapped document size limitation and consider MaxDocumentSize option (BACKLOG)

**Goal:** Add a note to `docs/options-reference.md` and `HumlOptions` XML docs warning that no maximum document size is enforced. Evaluate whether an optional `MaxDocumentSize` (bytes or chars) property should be added to `HumlOptions` to guard against memory exhaustion from untrusted inputs. Any implementation must be a non-breaking additive change with `null` default (unlimited).
**Source:** ArchitectureReview_20260324.md §6.1
**Version:** V2 | **Priority:** Low | **Category:** Security
**Requirements:** TBD
**Plans:** 0 plans

Plans:
- [ ] TBD (promote with /gsd:review-backlog when ready)
