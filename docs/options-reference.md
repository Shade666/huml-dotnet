# HumlOptions Reference

`HumlOptions` controls parsing, serialisation, and version behaviour in Huml.Net.
Several convenience instances are provided for common scenarios. All properties use `init`-only
setters, making instances immutable after construction. Call `MakeReadOnly()` to signal that an
instance must not be mutated; the built-in instances are pre-frozen at type-load time.

## Properties

| Property                    | Type                        | Default     | Valid Values                                              | Behaviour |
| --------------------------- | --------------------------- | ----------- | --------------------------------------------------------- | --------- |
| `SpecVersion`               | `HumlSpecVersion`           | `V0_2`      | `V0_1`, `V0_2`                                            | Selects which spec grammar to apply when `VersionSource` is `Options` |
| `VersionSource`             | `VersionSource`             | `Options`   | `Options`, `Header`                                       | `Options` = use `SpecVersion` property; `Header` = read `%HUML` directive from document |
| `UnknownVersionBehaviour`   | `UnknownVersionBehaviour`   | `Throw`     | `Throw`, `UseLatest`, `UsePrevious`                       | What happens when a `%HUML` header declares an unrecognised version |
| `CollectionFormat`          | `CollectionFormat`          | `Multiline` | `Multiline`, `Inline`                                     | Global default for collection serialisation format; per-property override via `[HumlProperty(Inline = InlineMode.Inline)]`. See [Inline Serialisation](inline-serialisation.md). |
| `MaxRecursionDepth`         | `int`                       | `64`        | `1`–`1024`                                                | Max nesting depth before `HumlParseException` is thrown |
| `PropertyNamingPolicy`      | `HumlNamingPolicy?`         | `null`      | `null` or any `HumlNamingPolicy`                          | Converts .NET property names to HUML keys. `null` = use property name as-is. Built-ins: `KebabCase`, `SnakeCase`, `CamelCase`, `PascalCase`. `[HumlProperty]` name always takes precedence. |
| `Converters`                | `IReadOnlyList<HumlConverter>` | `[]`     | any sequence of `HumlConverter`                          | Custom converters consulted during (de)serialisation. First converter whose `CanConvert` returns `true` wins. Assign a fully populated list via the object initialiser; the property is `init`-only. |
| `NumberHandling`            | `HumlNumberHandling`        | `Strict`    | `Strict`, `AllowReadingFromString`, `WriteAsString`       | Global default for reading/writing numbers. `Strict` = numbers only; `AllowReadingFromString` = accept quoted numbers on read; `WriteAsString` = emit numbers as quoted strings. Per-member `[HumlNumberHandling(…)]` overrides. |
| `DefaultIgnoreCondition`    | `HumlIgnoreCondition`       | `Never`     | `Never`, `WhenWritingNull`, `WhenWritingDefault`, `Always` | Global default for when to omit properties during serialisation. Precedence: per-property `OmitIfDefault` → `[HumlIgnoreDefaults]` → this option. |
| `ValidateDuplicateKeysOnWrite` | `bool`                   | `false`     | `true`, `false`                                           | When `true`, throws `HumlSerializeException` on duplicate dictionary keys (ordinal). Multiline path only; inline dicts are not checked. |
| `UnmappedMemberHandling`    | `UnmappedMemberHandling`    | `Skip`      | `Skip`, `Disallow`                                        | `Skip` silently ignores unknown HUML keys (forward-compatible). `Disallow` throws `HumlDeserializeException` listing the key. Suppressed when a `[HumlExtensionData]` property captures unknown keys. |
| `TypeInfoResolver`          | `IHumlTypeInfoResolver?`    | `null`      | any `IHumlTypeInfoResolver` or `null`                     | Plug-in seam for a source-generated type info resolver. `null` = use built-in reflection path. |
| `IsReadOnly`                | `bool` (read)               | `false`     | —                                                         | `true` after `MakeReadOnly()` is called. Built-in instances are pre-frozen. |

## Convenience Instances

| Instance                            | VersionSource | UnknownVersionBehaviour | UnmappedMemberHandling | ValidateDuplicateKeysOnWrite | IsReadOnly |
| ----------------------------------- | ------------- | ----------------------- | ---------------------- | ---------------------------- | ---------- |
| `HumlOptions.Default`               | Header        | Throw                   | Skip                   | false                        | true       |
| `HumlOptions.AutoDetect`            | Header        | Throw                   | Skip                   | false                        | true       |
| `HumlOptions.LatestSupported`       | Options       | Throw                   | Skip                   | false                        | true       |
| `HumlOptions.LatestSupportedAutoDetect` | Header    | UseLatest               | Skip                   | false                        | true       |
| `HumlOptions.Strict`                | Header        | Throw                   | Disallow               | true                         | true       |

- **`Default` / `AutoDetect`** — reads the `%HUML` header; throws on unknown versions. `AutoDetect` is a reference-equal alias for `Default`.
- **`LatestSupported`** — ignores the `%HUML` header; always applies V0_2 rules.
- **`LatestSupportedAutoDetect`** — reads the `%HUML` header; silently falls back to V0_2 for unknown versions. Use when consuming documents from heterogeneous sources where version drift is expected.
- **`Strict`** — maximum strictness: reads header, throws on unknown versions, disallows unmapped keys, validates duplicate dictionary keys. Use in strict validation pipelines.

## Examples

```csharp
using Huml.Net;
using Huml.Net.Versioning;

// Read version from document header; throw if unrecognised
var result = HumlSerializer.Deserialize<MyDto>(humlText, HumlOptions.AutoDetect);

// Read version from header; fall back silently to latest if unrecognised
var lenient = HumlSerializer.Deserialize<MyDto>(humlText, HumlOptions.LatestSupportedAutoDetect);

// Maximum strictness — throws on unknown keys and duplicate dictionary entries
var strict = HumlSerializer.Deserialize<MyDto>(humlText, HumlOptions.Strict);

// Custom options
var custom = new HumlOptions
{
    VersionSource = VersionSource.Header,
    UnmappedMemberHandling = UnmappedMemberHandling.Disallow,
    PropertyNamingPolicy = HumlNamingPolicy.KebabCase,
};
var result2 = HumlSerializer.Deserialize<MyDto>(humlText, custom);
```

## Notes

- Passing `null` for `options` in any `HumlSerializer.*` method is equivalent to passing `HumlOptions.Default` (header-aware auto-detect).
- `MaxRecursionDepth` throws `ArgumentOutOfRangeException` at construction time if the value is outside `[1, 1024]`.
- `CollectionFormat.Inline` is silently ignored for collection properties containing non-scalar items — those always emit in multiline format.
- `PropertyNamingPolicy` applies only to .NET property names — it does not affect `Dictionary<string, T>` string keys or `[HumlProperty]` explicit names.
- The `Converters` list is checked in order; the first converter whose `CanConvert(type)` returns `true` is used. A property-level or type-level `[HumlConverter]` attribute always takes precedence.
- `DefaultIgnoreCondition` applies only to serialisation — it has no effect during deserialisation.
- `ValidateDuplicateKeysOnWrite` uses `StringComparer.Ordinal`; keys differing only in casing are treated as distinct. Inline dictionaries are not validated in this release.
- `UnmappedMemberHandling.Disallow` is suppressed when the target type has a `[HumlExtensionData]` property — the unknown key is routed there instead of throwing.
- `TypeInfoResolver` is a low-level seam for source-generator integration. Rather than implementing `IHumlTypeInfoResolver` by hand, register a source-generated `HumlGeneratedContext` — see [Use the source generator](source-generator.md).

## Document Size Limitation

**Huml.Net does not enforce a maximum document size.** There is no built-in limit on the
number of bytes, characters, or nesting levels in an input document beyond `MaxRecursionDepth`.
When parsing untrusted input, callers are responsible for enforcing size constraints before
passing the document to `HumlSerializer.Parse` / `HumlSerializer.Deserialize`:

```csharp
const int MaxDocumentBytes = 1 * 1024 * 1024; // 1 MiB — set a limit appropriate for your app
if (Encoding.UTF8.GetByteCount(humlText) > MaxDocumentBytes)
    throw new InvalidOperationException("Document exceeds maximum allowed size.");
var result = HumlSerializer.Deserialize<MyDto>(humlText, HumlOptions.Default);
```

A dedicated `HumlOptions.MaxDocumentSize` property may be added in a future release as a
non-breaking, additive change.
