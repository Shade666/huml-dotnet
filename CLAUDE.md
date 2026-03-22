# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

**Huml.Net** is the first-party .NET implementation of the HUML (Human-oriented Markup Language) specification. It provides parsing, serialisation, and deserialisation with a `System.Text.Json`-style API. TDD against the shared `huml-lang/tests` fixture suite from day one.

## Commands

### Build
```bash
dotnet build
```

### Run all tests
```bash
dotnet test
```

### Run tests for a specific target framework
```bash
dotnet test --framework net10.0
```

### Run a single test class
```bash
dotnet test --filter "FullyQualifiedName~SharedSuiteTests"
```

### Run a single test method
```bash
dotnet test --filter "DisplayName~V02_fixture_passes"
```

### Pack for NuGet
```bash
dotnet pack src/Huml.Net/Huml.Net.csproj -c Release
```

## Architecture

### Public API Surface

`src/Huml.Net/Huml.cs` — the **sole public entry point**, a static facade mirroring `System.Text.Json.JsonSerializer`. All internal pipeline classes are `internal sealed`; consumers never touch them directly.

```
Huml.Serialize<T>(value, options?)        → string
Huml.Deserialize<T>(string/Span, options?) → T
Huml.Parse(string, options?)              → HumlDocument (AST)
```

### Pipeline Flow

```
Input string
    └─► Lexer          (Lexer/Lexer.cs)         — pull-based tokeniser
         └─► HumlParser (Parser/HumlParser.cs)   — recursive-descent, produces AST
              └─► HumlDocument (AST root)
                   ├─► HumlDeserializer           — AST → .NET objects
                   └─► HumlSerializer             — .NET objects → HUML text
```

### AST Node Hierarchy

All nodes in `src/Huml.Net/Parser/` are `public sealed record` types:

| Type | Role |
|------|------|
| `HumlNode` | Abstract base record |
| `HumlDocument` | Root / mapping block — holds `IReadOnlyList<HumlNode>` entries |
| `HumlMapping` | Single key-value pair (`Key: string`, `Value: HumlNode`) |
| `HumlScalar` | Leaf value (`Kind: ScalarKind`, `Value: object?`) |
| `HumlSequence` | Ordered list of `HumlNode` items |

`HumlDocument` is reused for both the document root and nested mapping blocks (inline dicts also produce a `HumlDocument`).

### Versioning Model

`HumlSpecVersion` is an `int`-backed enum (`V0_1 = 1`, `V0_2 = 2`). Version gates inside the Lexer and Parser use the pattern `>= HumlSpecVersion.V0_2` — there are **no forked classes**, just conditional branches within the single code path. `V0_1` is marked `[Obsolete]`.

`HumlOptions` carries `SpecVersion`, `VersionSource` (Options vs Header), `UnknownVersionBehaviour`, and `MaxRecursionDepth`. Use `HumlOptions.Default` (v0.2, Throw on unknown) or `HumlOptions.AutoDetect` (reads from `%HUML` header) in tests.

When referencing `HumlSpecVersion.V0_1` in implementation or tests, suppress `CS0618` with a targeted `#pragma warning disable/restore CS0618`.

### Serialisation Conventions

- **Properties are emitted in declaration order**, base-class-first, then by `MetadataToken` within each type. This is cached in `PropertyDescriptor` (a `ConcurrentDictionary<Type, PropertyDescriptor[]>`).
- `[HumlIgnore]` excludes a property entirely.
- `[HumlProperty(name, OmitIfDefault = true)]` overrides the key name and/or suppresses default-valued properties.
- `init`-only setters are detected via `IsExternalInit` custom modifier and rejected during deserialisation.
- Scalars use `key: value` syntax; complex values (collections, POCOs) use the `key::` vector indicator.
- Serialiser always emits a `%HUML vX.Y.Z` version directive as the first line.

### Fixture Suite

Fixtures live in `fixtures/v0.1/` and `fixtures/v0.2/`, linked into test output via `<Content>` items in `Huml.Net.Tests.csproj`. The `SharedSuiteTests` class loads `fixtures/<version>/assertions/*.json` at runtime and drives `[Theory]` tests against `Huml.Parse()`.

Each JSON fixture row has `name`, `input`, and `error` (bool). When `error` is true the test asserts `HumlParseException` is thrown; otherwise it asserts successful parse.

## Key Constraints

- **No external runtime dependencies** in `Huml.Net.csproj`. `MinVer` and `SourceLink` are `PrivateAssets="All"`.
- **Multi-target:** library targets `netstandard2.1;net8.0;net9.0;net10.0`; tests target `net8.0;net9.0;net10.0`.
- **C# 13**, no .NET Framework.
- **Test stack:** xUnit v3 (`xunit.v3` 3.2.2) + **AwesomeAssertions** 9.4.0. Never use FluentAssertions.
- `Huml.Net.Linting` is a future separate package — no linting logic belongs in core.
- Planning docs (`.planning/`) are local-only and must not be committed (except `PROJECT.md` and `config.json`).

## Testing Patterns

```csharp
// Positive assertion
var act = () => Huml.Parse(input, HumlOptions.Default);
act.Should().NotThrow();

// Negative assertion
var act = () => Huml.Parse(input, HumlOptions.Default);
act.Should().Throw<HumlParseException>();

// Deserialise
var result = Huml.Deserialize<MyDto>(humlText);
result.Property.Should().Be(expected);
```

Use `AwesomeAssertions` (`.Should()` extension methods) from the `AwesomeAssertions` namespace, not `FluentAssertions`.
