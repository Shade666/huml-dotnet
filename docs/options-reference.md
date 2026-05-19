# HumlOptions Reference

`HumlOptions` controls parsing, serialisation, and version behaviour in Huml.Net.
Three convenience instances are provided for common scenarios: `HumlOptions.Default`, `HumlOptions.LatestSupported`, and `HumlOptions.AutoDetect`.
All properties use `init`-only setters, making instances immutable after construction.

## Properties

| Property                  | Type                      | Default     | Valid Values                        | Behaviour                                                                                                                  |
| ------------------------- | ------------------------- | ----------- | ----------------------------------- | -------------------------------------------------------------------------------------------------------------------------- |
| `SpecVersion`             | `HumlSpecVersion`         | `V0_2`      | `V0_1`, `V0_2`                      | Selects which spec grammar to apply when `VersionSource` is `Options`                                                      |
| `VersionSource`           | `VersionSource`           | `Options`   | `Options`, `Header`                 | `Options` = use `SpecVersion` property; `Header` = read `%HUML` directive from document                                    |
| `UnknownVersionBehaviour` | `UnknownVersionBehaviour` | `Throw`     | `Throw`, `UseLatest`, `UsePrevious` | What happens when a `%HUML` header declares an unrecognised version                                                        |
| `CollectionFormat`        | `CollectionFormat`        | `Multiline` | `Multiline`, `Inline`               | Global default for collection serialisation format; per-property override via `[HumlProperty(Inline = InlineMode.Inline)]`. See [Inline Serialisation](inline-serialisation.md) for details. |
| `MaxRecursionDepth`       | `int`                     | `64`        | `1`–`1024`                          | Max nesting depth before `HumlParseException` is thrown                                                                    |
| `PropertyNamingPolicy` | `HumlNamingPolicy?`    | `null`  | `null` or any `HumlNamingPolicy` instance | Converts .NET property names to HUML keys during serialisation and deserialisation. `null` = property name used as-is. Built-ins: `HumlNamingPolicy.KebabCase`, `SnakeCase`, `CamelCase`, `PascalCase`. A `[HumlProperty]` name override always takes precedence. |
| `Converters`           | `IList<HumlConverter>` | `[]`    | any list of `HumlConverter` instances     | Custom converters consulted during serialisation and deserialisation when no `[HumlConverter]` attribute is present. First converter whose `CanConvert` returns `true` wins. Do not modify this list after passing options to any `Huml.*` method.                    |
| `DefaultIgnoreCondition`       | `HumlIgnoreCondition`     | `Never`     | `Never`, `WhenWritingNull`, `WhenWritingDefault`, `Always` | Global default for when to omit properties during serialisation. Precedence (highest first): per-property `OmitIfDefault` → class-level `[HumlIgnoreDefaults]` → `DefaultIgnoreCondition`. `Never` preserves all existing behaviour. |
| `ValidateDuplicateKeysOnWrite` | `bool`                    | `false`     | `true`, `false`                                             | When `true`, throws `HumlSerializeException` if a dictionary produces duplicate keys (ordinal comparison) during serialisation. Check fires per dictionary call frame. Inline dictionaries are not checked. |
| `TypeInfoResolver`             | `IHumlTypeInfoResolver?`  | `null`      | any `IHumlTypeInfoResolver` implementation or `null`        | Plug-in point for a source-generated type info resolver. When `GetTypeInfo` returns non-null, its metadata is used instead of reflection. Returning `null` falls through to the built-in reflection path with zero overhead. Required for future `Huml.Net.SourceGeneration` package. |

## Convenience Instances

| Instance                      | SpecVersion | VersionSource | UnknownVersionBehaviour | CollectionFormat | MaxRecursionDepth | PropertyNamingPolicy | Converters | DefaultIgnoreCondition | ValidateDuplicateKeysOnWrite | TypeInfoResolver |
| ----------------------------- | ----------- | ------------- | ----------------------- | ---------------- | ----------------- | -------------------- | ---------- | ---------------------- | ---------------------------- | ---------------- |
| `HumlOptions.Default`         | V0_2        | Header        | Throw                   | Multiline        | 64                | null                 | []         | Never                  | false                        | null             |
| `HumlOptions.LatestSupported` | V0_2        | Options       | Throw                   | Multiline        | 64                | null                 | []         | Never                  | false                        | null             |
| `HumlOptions.AutoDetect`      | V0_2        | Header        | Throw                   | Multiline        | 64                | null                 | []         | Never                  | false                        | null             |

`HumlOptions.Default` reads the `%HUML vX.Y.Z` header from the document to determine the spec version.
If no header is present, it falls back to `V0_2`. `HumlOptions.AutoDetect` is a reference-equal alias for `Default`.

`HumlOptions.LatestSupported` ignores the `%HUML` header and always uses `V0_2` rules — use this when you want deterministic version behaviour regardless of document content.

## Examples

```csharp
using Huml.Net;
using Huml.Net.Versioning;

// Read version from document header; throw if unrecognised
var result = Huml.Deserialize<MyDto>(humlText, HumlOptions.AutoDetect);

// Read version from header; fall back to latest if unrecognised
var lenient = new HumlOptions
{
    VersionSource = VersionSource.Header,
    UnknownVersionBehaviour = UnknownVersionBehaviour.UseLatest,
};
var result2 = Huml.Deserialize<MyDto>(humlText, lenient);

// Always use v0.2 rules, ignoring any %HUML header
var result3 = Huml.Deserialize<MyDto>(humlText, HumlOptions.LatestSupported);
```

## Notes

- Passing `null` for `options` in any `Huml.*` method is equivalent to passing `HumlOptions.Default` (header-aware auto-detect).
- `MaxRecursionDepth` throws `ArgumentOutOfRangeException` at construction time if the value is outside `[1, 1024]`.
- `CollectionFormat.Inline` is silently ignored for collection properties that contain non-scalar items — those always emit in multiline format.
- `PropertyNamingPolicy` applies only to .NET property names — it does not affect `Dictionary<string, T>` string keys or `[HumlProperty]` explicit names.
- The `Converters` list is checked in order; the first converter whose `CanConvert(type)` returns `true` is used. A property-level or type-level `[HumlConverter]` attribute always takes precedence over `Converters`.
- `DefaultIgnoreCondition` applies only to serialisation — it has no effect during deserialisation.
  Per-property `OmitIfDefault = true` on `[HumlProperty]` and type-level `[HumlIgnoreDefaults]`
  both take precedence over `DefaultIgnoreCondition`.
- `ValidateDuplicateKeysOnWrite` uses `StringComparer.Ordinal` — keys differing only in casing
  are not treated as duplicates. Inline dictionaries are not validated in this release.
- `TypeInfoResolver` is a low-level seam for source-generator integration. Consumer code should
  not implement `IHumlTypeInfoResolver` directly; wait for the `Huml.Net.SourceGeneration`
  package to use this facility safely.
