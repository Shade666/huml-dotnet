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

`src/Huml.Net/HumlSerializer.cs` — the **sole public entry point**, a static facade mirroring `System.Text.Json.JsonSerializer`. All internal pipeline classes are `internal sealed`; consumers never touch them directly.

```
HumlSerializer.Serialize<T>(value, options?)         → string
HumlSerializer.Deserialize<T>(string/Span, options?) → T
HumlSerializer.Parse(string, options?)               → HumlDocument (AST)
HumlSerializer.Populate<T>(string/Span, target, options?) → void  (overlay onto an instance)
```

> The facade was renamed `Huml` → `HumlSerializer` in `0.2.0-beta.1` (the old name collided with the root `Huml` namespace). There is no `Huml` class or `Huml.cs` file.

### Pipeline Flow

```
Input string
    └─► Lexer          (Lexer/Lexer.cs)         — pull-based tokeniser
         └─► HumlParser (Parser/HumlParser.cs)   — recursive-descent, produces AST
              └─► HumlDocument (AST root)
                   ├─► HumlDeserializer           — AST → .NET objects (internal)
                   └─► HumlSerializerImpl         — .NET objects → HUML text (internal)
```

The public `HumlSerializer` facade delegates to the internal `HumlSerializerImpl` (serialise) and `HumlDeserializer` (deserialise); don't confuse the public facade with the internal serialiser.

### AST Node Hierarchy

All nodes in `src/Huml.Net/Parser/` are `public sealed record` types. Every node carries `Line` and `Column` source positions (via the `HumlNode` base):

| Type                | Role                                                           |
| ------------------- | -------------------------------------------------------------- |
| `HumlNode`          | Abstract base record (`Line`, `Column`)                        |
| `HumlDocument`      | Root / nested mapping block — holds `IReadOnlyList<HumlNode>` `Entries`; exposes `DetectedVersion` |
| `HumlMapping`       | Single key-value pair (`Key: string`, `Value: HumlNode`)       |
| `HumlInlineMapping` | Inline dict (`{ a: 1, b: 2 }`) — holds `IReadOnlyList<HumlNode>` `Entries` |
| `HumlScalar`        | Leaf value (`Kind: ScalarKind`, `Value: object?`)              |
| `HumlSequence`      | Ordered list of `HumlNode` `Items`                             |

`HumlDocument` is used for both the document root and nested **block** mapping blocks; **inline** dicts produce a `HumlInlineMapping`.

### Versioning Model

`HumlSpecVersion` is an `int`-backed enum (`V0_1 = 1`, `V0_2 = 2`). Version gates inside the Lexer and Parser use the pattern `>= HumlSpecVersion.V0_2` — there are **no forked classes**, just conditional branches within the single code path. `V0_1` is marked `[Obsolete]`.

`HumlOptions` carries `SpecVersion`, `VersionSource` (Options vs Header), `UnknownVersionBehaviour`, and `MaxRecursionDepth`. Use `HumlOptions.Default` (reads `%HUML` header, falls back to v0.2) or `HumlOptions.LatestSupported` (pinned v0.2, ignores header) in tests. `HumlOptions.AutoDetect` is a reference-equal alias for `Default`.

When referencing `HumlSpecVersion.V0_1` in implementation or tests, suppress `CS0618` with a targeted `#pragma warning disable/restore CS0618`.

### Serialisation Conventions

- **Properties are emitted in declaration order**, base-class-first, then by `MetadataToken` within each type. This is cached in `PropertyDescriptor` (a `ConcurrentDictionary<Type, PropertyDescriptor[]>`).
- `[HumlIgnore]` excludes a property entirely.
- `[HumlProperty(name, OmitIfDefault = true)]` overrides the key name and/or suppresses default-valued properties.
- `init`-only setters are detected via `IsExternalInit` custom modifier. Since Phase 23, `init`-only properties are settable via reflection — `HumlDeserializeException` is no longer thrown for `init`-only properties.
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
var act = () => HumlSerializer.Parse(input, HumlOptions.Default);
act.Should().NotThrow();

// Negative assertion
var act = () => HumlSerializer.Parse(input, HumlOptions.Default);
act.Should().Throw<HumlParseException>();

// Deserialise
var result = HumlSerializer.Deserialize<MyDto>(humlText);
result.Property.Should().Be(expected);
```

Use `AwesomeAssertions` (`.Should()` extension methods) from the `AwesomeAssertions` namespace, not `FluentAssertions`.

## Changelog

`CHANGELOG.md` follows [Keep a Changelog 1.1.0](https://keepachangelog.com/en/1.1.0/) and **must always have an `## [Unreleased]` section** at the top, above all versioned entries.

**Rule:** As each phase lands, add every user-visible change (new features, behaviour changes, bug fixes) under `## [Unreleased]`. Do not wait until release time — update it incrementally as work progresses.

At release time, rename `## [Unreleased]` to the versioned entry (e.g. `## [0.3.0-alpha.1] - YYYY-MM-DD`) and immediately insert a fresh `## [Unreleased]\n\n(no changes yet)` section above it.

<!-- BACKLOG.MD MCP GUIDELINES START -->

<CRITICAL_INSTRUCTION>

## BACKLOG WORKFLOW INSTRUCTIONS

This project uses Backlog.md MCP for all task and project management activities.

**CRITICAL GUIDANCE**

- If your client supports MCP resources, read `backlog://workflow/overview` to understand when and how to use Backlog for this project.
- If your client only supports tools or the above request fails, call `backlog.get_backlog_instructions()` to load the tool-oriented overview. Use the `instruction` selector when you need `task-creation`, `task-execution`, or `task-finalization`.

- **First time working here?** Read the overview resource IMMEDIATELY to learn the workflow
- **Already familiar?** You should have the overview cached ("## Backlog.md Overview (MCP)")
- **When to read it**: BEFORE creating tasks, or when you're unsure whether to track work

These guides cover:
- Decision framework for when to create tasks
- Search-first workflow to avoid duplicates
- Links to detailed guides for task creation, execution, and finalization
- MCP tools reference

You MUST read the overview resource to understand the complete workflow. The information is NOT summarized here.

</CRITICAL_INSTRUCTION>

<!-- BACKLOG.MD MCP GUIDELINES END -->
