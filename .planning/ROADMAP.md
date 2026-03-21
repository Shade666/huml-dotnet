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
- [ ] **Phase 5: Parser** - Recursive-descent parser covering full HUML v0.1 and v0.2 grammar with depth-limit guard
- [ ] **Phase 6: Attributes and Serializer/Deserializer** - Reflection-based serialization and deserialization with attribute-driven mapping
- [ ] **Phase 7: Static Entry Point and Shared Fixture Compliance** - `Huml` static class wiring all pipeline stages; CI passes all fixture suite tests
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
**Plans:** 2 plans
Plans:
- [ ] 05-01-PLAN.md — TDD recursive-descent parser with full grammar coverage and version gating (PARS-03, PARS-04)
- [ ] 05-02-PLAN.md — Configurable recursion depth limit with depth-guard tests (PARS-05)

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
**Plans:** 0/1 plans complete
Plans:
- [ ] 02-01-PLAN.md — Versioning types, exceptions, and unit tests (VER-01, VER-02, VER-03, VER-04, VER-05)

### Phase 7: Static Entry Point and Shared Fixture Compliance
**Goal**: The `Huml` static class wires the complete pipeline; CI passes all `huml-lang/tests` fixtures for both v0.1 and v0.2 with a verified non-zero Theory count, certifying full spec compliance
**Depends on**: Phase 6
**Requirements**: API-01, API-02, API-03, API-06
**Success Criteria** (what must be TRUE):
  1. `Huml.Serialize<T>(value)` and `Huml.Deserialize<T>(humlString)` are callable from a consumer project that references only the `Huml` static class with no visibility into internal types
  2. `Huml.Deserialize<T>(ReadOnlySpan<char> huml, ...)` is the single implementation overload; `Huml.Deserialize<T>(string huml, ...)` is a thin `AsSpan()` wrapper -- confirmed by inspecting that only one overload reaches the Lexer
  3. All xUnit Theory rows from both `huml-lang/tests@v0.1` and `huml-lang/tests@v0.2` fixture suites pass in CI with a non-zero Theory count logged for each version
  4. All public members of `Huml`, `HumlOptions`, `HumlSpecVersion`, `HumlParseException`, `HumlSerializeException`, `HumlDeserializeException`, `HumlUnsupportedVersionException`, `[HumlProperty]`, and `[HumlIgnore]` carry XML doc comments visible in IntelliSense
**Plans:** 0/1 plans complete
Plans:
- [ ] 02-01-PLAN.md — Versioning types, exceptions, and unit tests (VER-01, VER-02, VER-03, VER-04, VER-05)

### Phase 8: NuGet Release Preparation
**Goal**: The NuGet package is verified complete -- correct TFM coverage, working SourceLink, embedded XML docs, and a successful pre-release publish to NuGet.org via OIDC Trusted Publishing
**Depends on**: Phase 7
**Requirements**: API-04, API-05
**Success Criteria** (what must be TRUE):
  1. `dotnet pack -c Release` produces a `.nupkg` containing `lib/netstandard2.1`, `lib/net8.0`, `lib/net9.0`, and `lib/net10.0` entries (all four TFMs present -- no NU5128 warning)
  2. `dotnet sourcelink test Huml.Net.*.nupkg` passes, confirming embedded PDB has real commit SHAs and source-stepping works from NuGet
  3. The package manifest contains `PackageId`, `Authors`, `Description`, `PackageLicenseExpression` (MIT), `PackageTags`, `PackageProjectUrl`, `RepositoryUrl`, `PackageReadmeFile`, and `GenerateDocumentationFile` -- confirmed by inspecting the `.nuspec` inside the `.nupkg`
  4. A `0.1.0-alpha.1` pre-release tag triggers the NuGet publish workflow; the package appears on NuGet.org without manual API key entry
**Plans:** 0/1 plans complete
Plans:
- [ ] 02-01-PLAN.md — Versioning types, exceptions, and unit tests (VER-01, VER-02, VER-03, VER-04, VER-05)

## Progress

**Execution Order:**
Phases execute in numeric order: 1 -> 2 -> 3 -> 4 -> 5 -> 6 -> 7 -> 8

| Phase | Plans Complete | Status | Completed |
|-------|----------------|--------|-----------|
| 1. Project Scaffold and CI Foundations | 2/2 | Complete   | 2026-03-20 |
| 2. Versioning Foundation | 1/1 | Complete   | 2026-03-20 |
| 3. Lexer and Token Types | 2/2 | Complete   | 2026-03-21 |
| 4. AST Node Hierarchy | 1/1 | Complete   | 2026-03-21 |
| 5. Parser | 0/2 | Not started | - |
| 6. Attributes and Serializer/Deserializer | 0/? | Not started | - |
| 7. Static Entry Point and Shared Fixture Compliance | 0/? | Not started | - |
| 8. NuGet Release Preparation | 0/? | Not started | - |
