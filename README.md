[![CI](https://github.com/primeBeri/huml-dotnet/actions/workflows/ci.yml/badge.svg)](https://github.com/primeBeri/huml-dotnet/actions/workflows/ci.yml)
[![NuGet](https://img.shields.io/nuget/v/Huml.Net.svg)](https://www.nuget.org/packages/Huml.Net/)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)

# Huml.Net

**HUML for .NET.** A full-featured [HUML](https://huml.io) v0.1/v0.2 parser, serialiser, and
deserialiser with a `System.Text.Json`-style API and **zero runtime dependencies**.

> **Beta.** The public API is frozen for the `0.2.0-beta.1` line; breaking changes (if any) will
> wait for the next minor version.

📖 **[Documentation](https://primeberi.github.io/huml-dotnet/)** · 📦 **[NuGet](https://www.nuget.org/packages/Huml.Net/)** · 📝 **[Changelog](CHANGELOG.md)**

## Install

```bash
dotnet add package Huml.Net
```

## 30-second example

If you know `JsonSerializer`, you already know `Huml`:

```csharp
using Huml.Net;

var config = Huml.Deserialize<ServerConfig>("""
    %HUML v0.2.0
    Host: "localhost"
    Port: 8080
    Debug: true
    """);

string roundTrip = Huml.Serialize(config);

public class ServerConfig
{
    public string Host { get; set; } = "";
    public int Port { get; set; }
    public bool Debug { get; set; }
}
```

New to the library? The **[Getting Started tutorial](https://primeberi.github.io/huml-dotnet/docs/getting-started.html)**
takes you from install to round-trip in five minutes.

## Coming from System.Text.Json?

The mental model is the same; the table maps what you know to its Huml.Net equivalent.

| System.Text.Json | Huml.Net | Notes |
| ---------------- | -------- | ----- |
| `JsonSerializer.Serialize` / `Deserialize` | `Huml.Serialize` / `Huml.Deserialize` | Same static-facade shape. |
| `JsonSerializer.Deserialize<T>(ReadOnlySpan<char>)` | `Huml.Deserialize<T>(ReadOnlySpan<char>)` | Zero-copy span path (`ref struct` lexer/parser). |
| `JsonDocument.Parse` | `Huml.Parse` | Returns the `HumlDocument` AST for validation/inspection. |
| `[JsonPropertyName]` | `[HumlProperty]` | Plus `OmitIfDefault` and per-member inline control. |
| `[JsonIgnore]` | `[HumlIgnore]` | |
| `[JsonRequired]` / `required` | `[HumlRequired]` / `required` | |
| `[JsonExtensionData]` | `[HumlExtensionData]` | Captures unmatched keys. |
| `[JsonConstructor]` | `[HumlConstructor]` | Constructor / record binding. |
| `JsonConverter<T>` | `HumlConverter<T>` | Same priority chain (member → type → options). |
| `JsonNamingPolicy` | `HumlNamingPolicy` | KebabCase, SnakeCase, CamelCase, PascalCase built in. |
| `[JsonPolymorphic]` / `[JsonDerivedType]` | `[HumlPolymorphic]` / `[HumlDerivedType]` | Discriminator-based dispatch. |
| `JsonSerializerContext` (source gen) | `HumlGeneratedContext` (source gen) | Reflection-free metadata for AOT/trim. |
| `JsonNumberHandling` | `[HumlNumberHandling]` / `HumlOptions.NumberHandling` | |
| `JsonSerializerOptions` | `HumlOptions` | Plus presets: `Default`, `LatestSupported`, `Strict`. |
| Populate (`PopulateObject`, .NET 9+) | `Huml.Populate<T>` | Overlay a document onto an existing instance. |
| `Utf8JsonReader` / `Utf8JsonWriter` streaming | — | **Not provided** — streaming is out of scope by design. |
| Mutable `JsonNode` DOM | — | **Not provided** — the `HumlDocument` AST is read-only. |

## Features

**Spec compliance**
- Full HUML v0.1 and v0.2 spec compliance, validated against the `huml-lang/tests` fixture suite
- `System.Text.Json`-style static `Huml` facade (`Serialize`, `Deserialize`, `Parse`, `Populate`)

**Serialisation**
- Reflection-based serialisation with declaration-order property emission (base-class first)
- Inline and multiline collection format control via `[HumlProperty(Inline = …)]` and `HumlOptions.CollectionFormat`
- Native date/time round-trip: `DateTime`, `DateTimeOffset`, `TimeSpan`; `DateOnly` and `TimeOnly` on .NET 6+
- `StringBuilder` pooling via `[ThreadStatic]` — eliminates per-call allocations on hot paths
- Duplicate-key write validation via `HumlOptions.ValidateDuplicateKeysOnWrite`

**Deserialisation**
- Constructor parameter binding — `[HumlConstructor]` attribute, single-constructor inference, parameterless fallback
- `init`-only property support — `{ get; init; }` properties settable during deserialisation
- Required-property enforcement — `[HumlRequired]` attribute and C# `required` modifier; throws on missing keys
- Extension data — `[HumlExtensionData]` captures unknown keys into a `Dictionary<string, HumlNode>` overflow bucket
- Collection dispatch: `T[]`, `List<T>`, `IEnumerable<T>`, `HashSet<T>`, `SortedSet<T>`, `ISet<T>`, `IReadOnlySet<T>`, `Dictionary<string,T>`, `IDictionary<string,T>`
- Unknown-key handling via `HumlOptions.UnmappedMemberHandling` (`Skip` / `Disallow`)
- `Huml.Populate<T>()` overlays a HUML document onto an existing object instance

**Attributes**
- `[HumlProperty]` — key name override and `OmitIfDefault`
- `[HumlIgnore]` — exclude a property from serialisation and deserialisation
- `[HumlIgnoreDefaults]` — suppress CLR-default values at type level; `HumlOptions.DefaultIgnoreCondition` as global fallback
- `[HumlConverter]` — per-property or per-type custom serialiser/deserialiser

**Options**
- Naming policy: `HumlNamingPolicy.KebabCase`, `SnakeCase`, `CamelCase`, `PascalCase`
- Enum support: `HumlEnumValueAttribute` for custom member names; round-trips through quoted strings
- Custom converters: `HumlConverter<T>` abstract base and `HumlOptions.Converters` list
- Preset instances: `Default` / `AutoDetect`, `LatestSupported`, `LatestSupportedAutoDetect` (silent fallback), `Strict` (maximum-strictness validation); all pre-frozen (`IsReadOnly`)
- `MakeReadOnly()` to freeze any custom instance; pre-built instances are frozen at type-load time

**Performance**
- Zero-copy span deserialisation — `Lexer` and `HumlParser` are `ref struct` types; no intermediate string allocation
- AOT / trim safety: `[RequiresUnreferencedCode]`, `[RequiresDynamicCode]` on all reflection-using public APIs; `<IsTrimmable>true</IsTrimmable>` on net6.0+

**Type system**
- Source-generator seam: `IHumlTypeInfoResolver` / `HumlTypeInfo<T>` plug-in point; `HumlOptions.TypeInfoResolver`
- AST source positions: `Line` and `Column` on all AST nodes; `HumlDeserializeException` reports the source position
- `HumlDocument.DetectedVersion` for version-preserving round-trip

**Library**
- Zero external runtime dependencies
- Multi-TFM: `netstandard2.1`, `.NET 8`, `.NET 9`, `.NET 10`

## HumlOptions

| Property                        | Type                      | Default     | Description |
| ------------------------------- | ------------------------- | ----------- | ----------- |
| `SpecVersion`                   | `HumlSpecVersion`         | `V0_2`      | Which spec version to use when `VersionSource` is `Options` |
| `VersionSource`                 | `VersionSource`           | `Options`   | `Options` = use `SpecVersion`; `Header` = read `%HUML` directive from document |
| `UnknownVersionBehaviour`       | `UnknownVersionBehaviour` | `Throw`     | What happens when a `%HUML` header declares an unrecognised version |
| `CollectionFormat`              | `CollectionFormat`        | `Multiline` | Global default for collection serialisation format; per-property override via `[HumlProperty(Inline = …)]` |
| `MaxRecursionDepth`             | `int`                     | `64`        | Max nesting depth before `HumlParseException` is thrown |
| `PropertyNamingPolicy`          | `HumlNamingPolicy?`       | `null`      | Converts .NET property names to HUML keys. Built-ins: `KebabCase`, `SnakeCase`, `CamelCase`, `PascalCase` |
| `Converters`                    | `IList<HumlConverter>`    | `[]`        | Custom converters; first `CanConvert` match wins. Property/type `[HumlConverter]` takes precedence |
| `DefaultIgnoreCondition`        | `HumlIgnoreCondition`     | `Never`     | Global default for when to omit properties. `Never` = emit all; `WhenWritingNull` / `WhenWritingDefault` = suppress null or default values. Per-property `OmitIfDefault` and class-level `[HumlIgnoreDefaults]` take precedence |
| `ValidateDuplicateKeysOnWrite`  | `bool`                    | `false`     | When `true`, throws `HumlSerializeException` if a dictionary produces duplicate keys (ordinal comparison). Opt-in; default preserves existing behaviour |
| `UnmappedMemberHandling`        | `UnmappedMemberHandling`  | `Skip`      | `Skip` silently ignores unknown HUML keys (forward-compatible). `Disallow` throws `HumlDeserializeException` listing the unrecognised key. Suppressed when `[HumlExtensionData]` is present. |
| `TypeInfoResolver`              | `IHumlTypeInfoResolver?`  | `null`      | Plug-in point for a source-generated type info resolver. Returning `null` from `GetTypeInfo` falls through to the built-in reflection path |
| `IsReadOnly`                    | `bool` (read)             | `false`     | `true` after `MakeReadOnly()` is called. All built-in preset instances (`Default`, `LatestSupported`, `Strict`, etc.) are pre-frozen at type-load time. |

## Compatibility

| Target         | Support           |
| -------------- | ----------------- |
| .NET 10        | Yes               |
| .NET 9         | Yes               |
| .NET 8         | Yes               |
| netstandard2.1 | Yes               |
| HUML v0.2      | Full              |
| HUML v0.1      | Full (deprecated) |

## Documentation

The full documentation site — tutorial, how-to guides, API reference, and explanation — is at
**[primeberi.github.io/huml-dotnet](https://primeberi.github.io/huml-dotnet/)**. Key pages:

- [Getting Started](docs/getting-started.md) — the five-minute tutorial
- [Attributes Reference](docs/attributes-reference.md) — every `[Huml*]` attribute
- [Use the Source Generator](docs/source-generator.md) — reflection-free metadata for AOT
- [Options Reference](docs/options-reference.md)
- [Versioning Policy](docs/versioning.md)
- [AST Usage Guide](docs/ast-usage.md)
- [Error Handling](docs/error-handling.md)
- [Inline Serialisation](docs/inline-serialisation.md)
- [Naming Policy](docs/naming-policy.md)
- [Enum Serialisation](docs/enum-serialisation.md)
- [Custom Converters](docs/custom-converters.md)
- [Populate](docs/populate.md)
- [Constructor Binding](docs/constructor-binding.md)
- [Extension Data](docs/extension-data.md)
- [Date and Time](docs/date-time.md)
- [Required Properties](docs/required-properties.md)
- [AOT and Trimming](docs/aot-trimming.md)

## Links

- [HUML Specification](https://huml.io)
- [Reference Implementation (Go)](https://github.com/huml-lang/go-huml)

## Project

- [Changelog](CHANGELOG.md)
- [Contributing](CONTRIBUTING.md)
- [Security Policy](SECURITY.md)
- [Code of Conduct](CODE_OF_CONDUCT.md)
- [Open issues / backlog](BACKLOG.md)

## Licence

MIT — see [LICENSE](LICENSE).
