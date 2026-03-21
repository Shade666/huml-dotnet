# Requirements: Huml.Net

**Defined:** 2026-03-20
**Core Value:** Full HUML spec compliance (v0.1 + v0.2), validated against the shared `huml-lang/tests` test suite, with zero external runtime dependencies and a `System.Text.Json`-style API that .NET developers already know.

## v1 Requirements

Requirements for initial release. Each maps to a roadmap phase.

### Infrastructure

- [x] **INFRA-01**: Repository is structured as a multi-TFM SDK-style solution targeting `netstandard2.1;net8.0;net9.0;net10.0` with shared settings in `Directory.Build.props` and SDK version pinned in `global.json`
- [x] **INFRA-02**: `huml-lang/tests` is consumed as two git submodules pinned to `tests@v0.1` and `tests@v0.2` tags, with fixture files copied to test output directory
- [x] **INFRA-03**: A sentinel `[Fact]` test asserts the fixture directories are non-empty, preventing vacuous CI passes on uninitialised submodules
- [x] **INFRA-04**: GitHub Actions CI pipeline runs `dotnet build`, `dotnet test`, `dotnet pack`, and `dotnet sourcelink test` on every push and pull request
- [x] **INFRA-05**: NuGet publish workflow uses OIDC Trusted Publishing (no long-lived API key secrets stored in the repository)

### Versioning

- [x] **VER-01**: `HumlSpecVersion` enum represents supported spec versions (`V0_1`, `V0_2`), with `int` backing values ordered by release
- [x] **VER-02**: `HumlOptions` exposes `SpecVersion`, `VersionSource` (`Options` or `Header`), and `UnknownVersionBehaviour` (`Throw`, `UseLatest`, `UsePrevious`) with `HumlOptions.Default` and `HumlOptions.AutoDetect` convenience instances
- [x] **VER-03**: `SpecVersionPolicy` internal class declares `MinimumSupported` and `Latest` constants; `HumlUnsupportedVersionException` message references them so the error stays accurate without manual updates
- [x] **VER-04**: `HumlUnsupportedVersionException` is thrown (with declared version string in message) when a document header declares a version outside the support window and `UnknownVersionBehaviour` is `Throw`
- [x] **VER-05**: `HumlSpecVersion.V0_1` is decorated with `[Obsolete]` with a migration message pointing to v0.2; the deprecation process (grace period → removal) is documented in the library changelog

### Lexer

- [x] **LEX-01**: `TokenType` enum covers all HUML token categories: structural (`Eof`, `Error`), directives (`Version`), keys (`Key`, `QuotedKey`), indicators (`ScalarIndicator`, `VectorIndicator`, `ListItem`, `Comma`), scalars (`String`, `Int`, `Float`, `Bool`, `Null`, `NaN`, `Inf`), and empty collections (`EmptyList`, `EmptyDict`)
- [x] **LEX-02**: `Token` is a `readonly record struct` with `TokenType`, `string? Value`, `int Line`, `int Column`, `int Indent`, and `bool SpaceBefore`
- [x] **LEX-03**: `Lexer` class accepts `ReadOnlySpan<char>` input and produces a token stream in a single pass; no intermediate `ToString()` call occurs on the hot path
- [x] **LEX-04**: Lexer enforces all HUML v0.2 tokenisation rules: spaces-only indentation (tabs are errors), trailing whitespace is an error, `#` must be followed by a single space, bare keys match `[a-zA-Z][a-zA-Z0-9_-]*`, strings must be double-quoted, multiline `"""` delimiters, numeric literals (decimal/hex/octal/binary with `_` separators), inline list comma spacing
- [x] **LEX-05**: Version-gated lexer rules are expressed as `if (_options.SpecVersion >= HumlSpecVersion.Vx_y)` branches inside the single `Lexer` class; no forked classes per version
- [x] **LEX-06**: `HumlParseException` exposes typed `int Line` and `int Column` properties (not just a formatted string) so callers can programmatically inspect error location

### Parser & AST

- [x] **PARS-01**: AST node hierarchy is an immutable `abstract record` tree: `HumlNode` base → `HumlDocument` (entries list), `HumlMapping` (key + value), `HumlSequence` (items list), `HumlScalar` (`object? Value`, `ScalarKind Kind`)
- [x] **PARS-02**: `ScalarKind` enum covers `String`, `Integer`, `Float`, `Bool`, `Null`, `NaN`, `Inf`
- [x] **PARS-03**: Recursive-descent `Parser` consumes the token stream produced by the `Lexer` and produces a `HumlDocument` AST; covers full HUML v0.2 grammar (scalar values, vector blocks, inline lists, nested mappings, indent-driven nesting)
- [x] **PARS-04**: Parser applies version-gated rule branches (v0.1 vs v0.2 grammar differences) inside a single class using the same `>=` convention as the Lexer
- [ ] **PARS-05**: Parser enforces a configurable recursion depth limit (default 512); reaching the limit throws `HumlParseException` with a clear message rather than an unrecoverable `StackOverflowException`

### Serialisation

- [ ] **SER-01**: `[HumlProperty(string name)]` attribute renames a property in HUML input/output; `OmitIfDefault = true` skips default/zero/empty values
- [ ] **SER-02**: `[HumlIgnore]` attribute excludes a property from both serialisation and deserialisation
- [ ] **SER-03**: `HumlSerializer` produces HUML text from a .NET object via reflection with a per-type `PropertyDescriptor[]` cache (`ConcurrentDictionary<Type, ...>`); properties are emitted in source declaration order (not alphabetically)
- [ ] **SER-04**: `HumlSerializer` emits the `%HUML vX.Y.Z` version header matching `HumlOptions.SpecVersion`, two-space indentation, and correct type literals for all supported .NET types (`string` → `"value"`, `bool` → `true`/`false`, integers → bare, `double.NaN` → `nan`, infinities → `+inf`/`-inf`, `null` → `null`, collections → `::` vector block or `[]` empty, POCOs → `::` mapping block or `{}` empty)
- [ ] **SER-05**: `HumlDeserializer` maps a `HumlDocument` AST to a target .NET type via reflection with caching; detects `init`-only properties via `IsExternalInit` custom modifier on the setter and throws `HumlDeserializeException` with a clear message rather than silently skipping or failing at runtime
- [ ] **SER-06**: `HumlDeserializer` handles `List<T>`, `T[]`, `IEnumerable<T>` sequences, `Dictionary<string, T>` mappings, nested POCOs, and all primitive scalar types; throws `HumlDeserializeException` on type coercion failures with the offending key/line in the message
- [ ] **SER-07**: `HumlSerializeException` is thrown on unrecoverable serialisation errors; `HumlDeserializeException` is thrown on mapping or type-coercion failures

### Public API & Packaging

- [ ] **API-01**: `Huml` static class exposes `Serialize<T>(T, HumlOptions?)`, `Serialize(object?, Type, HumlOptions?)`, `Deserialize<T>(string, HumlOptions?)`, `Deserialize<T>(ReadOnlySpan<char>, HumlOptions?)`, `Deserialize(string, Type, HumlOptions?)`, and `Parse(string, HumlOptions?)`
- [ ] **API-02**: The `Deserialize<T>(string, ...)` overload is a thin wrapper calling `AsSpan()` on the string argument; `ReadOnlySpan<char>` is the single implementation overload to avoid C# 14 overload resolution ambiguity (CS0121)
- [ ] **API-03**: All public members carry XML doc comments (`<summary>`, `<param>`, `<returns>`, `<exception>`) shipped in the NuGet package for IntelliSense
- [ ] **API-04**: NuGet package metadata is complete: `PackageId`, `Authors`, `Description`, `PackageLicenseExpression` (MIT), `PackageTags`, `PackageProjectUrl`, `RepositoryUrl`, `PackageReadmeFile` (embedded README), `GenerateDocumentationFile`
- [ ] **API-05**: NuGet package uses MinVer for git-tag-driven `PackageVersion` derivation and ships with embedded PDB (SourceLink with `PublishRepositoryUrl=true`, `EmbedUntrackedSources=true`, `ContinuousIntegrationBuild` gated on `$(GITHUB_ACTIONS)=='true'`)
- [ ] **API-06**: `SharedSuiteTests.cs` Theory runner consumes `huml-lang/tests` v0.1 and v0.2 fixture suites; both fixture suites pass in CI with a verified non-zero Theory count for each version

## v2 Requirements

Deferred to future release. Tracked but not in current roadmap.

### AOT & Source Generators

- **AOT-01**: Source generator producing `HumlSerializerContext` for NativeAOT-compatible serialisation (analogous to `JsonSerializerContext`)
- **AOT-02**: `IHumlTypeInfoResolver` extensibility seam enabling pluggable type info resolution

### Streaming

- **STRM-01**: `IAsyncEnumerable`-based streaming parse API for large document processing

### Schema & Conversion

- **SCHM-01**: Schema validation against a declared HUML schema document
- **CONV-01**: Round-trip HUML → JSON converter
- **CONV-02**: Round-trip HUML → YAML converter

### Linting (Huml.Net.Linting package)

- **LINT-01**: Separate NuGet package `Huml.Net.Linting` operating on `HumlDocument` AST
- **LINT-02**: `HUML-L001` version-drift upgrade advisory
- **LINT-03**: `HUML-L002` version-drift downgrade advisory
- **LINT-04**: `HUML-L003` deprecated construct diagnostic
- **LINT-05**: `HUML-L004+` configurable style rules

### Future Spec Versions

- **SPEC-01**: HUML v0.3 support (when spec ships; requires new fixture submodule pin and behaviour gates)

## Out of Scope

Explicitly excluded. Documented to prevent scope creep.

| Feature | Reason |
|---------|--------|
| .NET Framework support | `netstandard2.1` compat floor requires `Span<T>` in public API; netstandard2.0 would be needed for .NET Framework — excluded by design |
| Alphabetical property ordering | .NET convention is declaration order; alphabetical would surprise C# consumers and matches go-huml behaviour to avoid confusion |
| Forked parser/lexer classes per spec version | Single code path with `>=` gates is the explicit architectural decision; forking is a maintenance anti-pattern |
| Error recovery / partial parse | Lexer errors terminate parsing immediately in v1; no partial AST on error |
| `IHumlConverter<T>` v1 implementation | Interface seam identified for v1.x; designing but not implementing avoids prematurely locking the contract |
| `Huml.Net.Linting` logic in core parser | Package boundary established; zero linting logic accretes into `Huml.Net` |

## Traceability

Which phases cover which requirements. Updated during roadmap creation.

| Requirement | Phase | Status |
|-------------|-------|--------|
| INFRA-01 | Phase 1 | Complete |
| INFRA-02 | Phase 1 | Complete |
| INFRA-03 | Phase 1 | Complete |
| INFRA-04 | Phase 1 | Complete |
| INFRA-05 | Phase 1 | Complete |
| VER-01 | Phase 2 | Complete |
| VER-02 | Phase 2 | Complete |
| VER-03 | Phase 2 | Complete |
| VER-04 | Phase 2 | Complete |
| VER-05 | Phase 2 | Complete |
| LEX-01 | Phase 3 | Complete |
| LEX-02 | Phase 3 | Complete |
| LEX-03 | Phase 3 | Complete |
| LEX-04 | Phase 3 | Complete |
| LEX-05 | Phase 3 | Complete |
| LEX-06 | Phase 3 | Complete |
| PARS-01 | Phase 4 | Complete |
| PARS-02 | Phase 4 | Complete |
| PARS-03 | Phase 5 | Complete |
| PARS-04 | Phase 5 | Complete |
| PARS-05 | Phase 5 | Pending |
| SER-01 | Phase 6 | Pending |
| SER-02 | Phase 6 | Pending |
| SER-03 | Phase 6 | Pending |
| SER-04 | Phase 6 | Pending |
| SER-05 | Phase 6 | Pending |
| SER-06 | Phase 6 | Pending |
| SER-07 | Phase 6 | Pending |
| API-01 | Phase 7 | Pending |
| API-02 | Phase 7 | Pending |
| API-03 | Phase 7 | Pending |
| API-04 | Phase 8 | Pending |
| API-05 | Phase 8 | Pending |
| API-06 | Phase 7 | Pending |

**Coverage:**
- v1 requirements: 31 total
- Mapped to phases: 31
- Unmapped: 0 ✓

---
*Requirements defined: 2026-03-20*
*Last updated: 2026-03-20 after roadmap creation*
