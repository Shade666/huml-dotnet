# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/).

**Versioning:** from `0.2.0` onward, the first two digits of the package version mirror the
targeted HUML spec version (`0.2.x` → HUML v0.2, `0.3.x` → HUML v0.3).
See [docs/versioning.md](docs/versioning.md) for the full policy.

## [Unreleased]

### Performance
- **Ref struct Lexer/Parser — zero-copy span deserialisation:** `Lexer` and `HumlParser` are now
  `ref struct` types that accept `ReadOnlySpan<char>` directly. `Huml.Deserialize<T>(ReadOnlySpan<char>)`
  no longer allocates an intermediate `string` copy of the input buffer. The `string` entry paths
  call `.AsSpan()` and use the same single code path. Additionally, the upfront `\r\n`/`\r`
  normalisation (two `string.Replace` calls) is replaced with inline character-level normalisation
  in the lexer, eliminating two more intermediate string allocations on both string and span paths.
- **`StringBuilder` pooling in `HumlSerializer`:** Both `Serialize` overloads now reuse a `[ThreadStatic]` `StringBuilder` across calls on the same thread, eliminating one `StringBuilder` allocation and one backing `char[]` growth per `Huml.Serialize` call on hot paths. A second `[ThreadStatic]` sentinel (`_serializationActive`) ensures re-entry from a `HumlConverter.Write` that calls `Huml.Serialize` internally falls back to a fresh `StringBuilder` rather than corrupting the pooled instance. No public API or emitted HUML format change.

### Added

- **Constructor parameter binding** (`[HumlConstructor]` attribute, CTOR-01..CTOR-12): `HumlDeserializer`
  now supports types with parameterised constructors (records, `required`-field classes). Constructor
  selection follows STJ priority — `[HumlConstructor]` annotation → single non-parameterless public
  constructor → parameterless fallback → `HumlDeserializeException` on ambiguity. Parameters are
  matched to HUML keys case-insensitively and naming-policy-aware; missing required parameters throw
  `HumlDeserializeException`; optional parameters (`HasDefaultValue`) use their declared defaults.
- **Init-only property deserialisation** (CTOR-07, CTOR-12): Properties declared with `{ get; init; }`
  are now settable via `PropertyInfo.SetValue` after construction. The previous `HumlDeserializeException`
  for init-only properties is removed. This applies to both `Huml.Deserialize<T>` and `Huml.Populate<T>`.
- **Extension data (`[HumlExtensionData]`):** A new `[HumlExtensionData]` attribute designates
  a single `Dictionary<string, HumlNode>` or `Dictionary<string, object?>` property as the
  overflow bucket for HUML keys that do not match any declared property during deserialisation.
  Captured keys are re-emitted during serialisation after all declared properties, in insertion
  order, preserving round-trip fidelity for unknown or forward-compat keys. Mirrors STJ's
  `[JsonExtensionData]` pattern. Declaring more than one `[HumlExtensionData]` property on a
  type, or using an unsupported property type, throws `InvalidOperationException` at first use.
  `Huml.Populate<T>` also participates in extension-data capture.
- **Version-preserving round-trip (`HumlDocument.DetectedVersion`):** `HumlDocument` now exposes
  `public HumlSpecVersion? DetectedVersion { get; init; }`. The property is populated by
  `HumlParser` from the `%HUML` header token — always reflecting the header-declared version
  regardless of `HumlOptions.VersionSource`. It is `null` when no header is present or when a
  `HumlDocument` is constructed directly in code. Callers can use it to preserve the original
  spec version when round-tripping: `new HumlOptions { SpecVersion = doc.DetectedVersion ?? HumlSpecVersion.V0_2 }`.
- **Default ignore condition:** `HumlIgnoreCondition` (`[Flags]` enum: `Never`, `WhenWritingNull`, `WhenWritingDefault`, `Always`) and `[HumlIgnoreDefaults]` class/struct attribute allow DTOs with many optional properties to suppress CLR-default values without per-property `[HumlProperty(OmitIfDefault = true)]` boilerplate. `HumlOptions.DefaultIgnoreCondition` (defaults to `Never`) provides a global fallback. Precedence chain: per-property `OmitIfDefault` → class-level `[HumlIgnoreDefaults]` → `DefaultIgnoreCondition`. No breaking changes — all existing code behaves identically with `DefaultIgnoreCondition = Never`.
- **Native date/time round-trip:** `DateTime`, `DateTimeOffset`, and `TimeSpan` now serialise as quoted ISO-8601 / canonical strings (`"O"`, `"O"`, and `"c"` formats respectively) and deserialise back with full fidelity. On .NET 6+, `DateOnly` (format `yyyy-MM-dd`) and `TimeOnly` (format `HH:mm:ss.FFFFFFF`, trailing zeros stripped) are also supported. Previously all five types fell through to the POCO reflection path, producing garbage output in the serialiser and throwing `InvalidCastException` in the deserialiser.
- `HumlDeserializer` now supports `HashSet<T>`, `ISet<T>`, and (on .NET 5+) `IReadOnlySet<T>` as deserialisation targets for HUML sequences. All three materialise as `HashSet<T>`; duplicate input elements are silently deduplicated.
- **AOT / trim safety annotations:** All reflection-using public API methods (`Serialize<T>`,
  `Serialize(object?,Type)`, `Deserialize<T>(string)`, `Deserialize<T>(ReadOnlySpan<char>)`,
  `Deserialize(string,Type)`, `Populate<T>(string)`, `Populate<T>(ReadOnlySpan<char>)`) now
  carry `[RequiresUnreferencedCode]` and `[RequiresDynamicCode]`. Consumers publishing with
  `PublishTrimmed=true` or NativeAOT now receive compile-time warnings rather than silent
  broken binaries. `Huml.Parse` is unannotated — it performs no reflection on user types.
  `<IsTrimmable>true</IsTrimmable>` added to `Huml.Net.csproj` for net8.0/9.0/net10.0
  (conditioned on net6.0+ compatibility).
- **Duplicate-key write validation:** `HumlOptions.ValidateDuplicateKeysOnWrite` (default `false`) causes `HumlSerializer` to throw `HumlSerializeException` when a dictionary contains two entries that produce the same key string (compared using `StringComparer.Ordinal`) during serialisation. The check fires per dictionary call frame, so nested dictionaries have independent key spaces. Keys differing only in casing are not treated as duplicates. Opt-in; default `false` preserves all existing behaviour.
- **Required-property enforcement (`[HumlRequired]`):** `[HumlRequired]` attribute (`AttributeTargets.Property`) and the C# `required` modifier are now detected and enforced during `Huml.Deserialize<T>`. When one or more required members are absent from the HUML input, a single `HumlDeserializeException` is thrown listing all missing keys: `"Missing required member(s) on type 'X': 'Key1', 'Key2'."`. Keys are listed in property declaration order. `Huml.Populate<T>` intentionally excludes required checks (overlay/partial-update semantics). Mirrors STJ's `[JsonRequired]` / C# `required` enforcement.
- C# `required` property modifier is now detected and honoured equivalently to `[HumlRequired]` during deserialisation.
- `RequiredMemberAttribute` compile-time shim added for `netstandard2.1` / pre-.NET-7 targets, enabling use of `required` modifier semantics in the detection path.
- **Source-generator seam (`IHumlTypeInfoResolver`, `HumlTypeInfo<T>`):** Adds `IHumlTypeInfoResolver` (single-method interface: `GetTypeInfo(Type, HumlOptions) → HumlTypeInfo?`) and `HumlTypeInfo<T>` (minimal abstract marker with `Type` property) to the `Huml.Net.Serialization` namespace. `HumlOptions.TypeInfoResolver` (`IHumlTypeInfoResolver? { get; init; }`, defaults to null) wires the call site in `HumlDeserializer.DeserializeMappingEntries` and `HumlSerializer.SerializeMappingBody`. Returning null (or leaving `TypeInfoResolver` unset) falls through to the existing reflection path with zero overhead. This seam is required for a future `Huml.Net.SourceGeneration` package; no consumer or property-level metadata is defined in this phase.

### Fixed
- Deserialising a HUML sequence into `ISet<T>` or `IReadOnlySet<T>` previously returned `List<T>` (via the `IEnumerable<T>` fallback), causing a runtime assignment failure. The new set dispatch branch (`b.5`) materialises these correctly as `HashSet<T>`.

## [0.2.0-alpha.1] - 2026-05-03

### Added
- **Source positions:** `Line` and `Column` properties on all AST nodes; `HumlDeserializeException` now includes the source position of the offending node.
- **Naming policy:** `HumlOptions.PropertyNamingPolicy` with built-in `HumlNamingPolicy.KebabCase`, `SnakeCase`, `CamelCase`, and `PascalCase` instances.
- **Enum support:** `HumlEnumValueAttribute` for custom member names; enum properties serialise as quoted strings and deserialise via name lookup with policy-aware transforms.
- **Custom converters:** `HumlConverter<T>` abstract base, `[HumlConverter]` attribute for per-property or per-type binding, and `HumlOptions.Converters` for options-level registration.
- **Populate:** `Huml.Populate<T>(string, T, HumlOptions?)` and `ReadOnlySpan<char>` overload for overlaying a HUML document onto an existing object instance.

## [0.1.0-alpha.1] - 2026-05-01

Initial alpha release.

### Added
- **Parser:** Full HUML v0.1 and v0.2 recursive-descent parser validated against the shared `huml-lang/tests` fixture suite.
- **Lexer:** Single-pass tokeniser with version-gated rules; `ReadOnlySpan<char>` input, no intermediate string allocations on the hot path.
- **Serialiser:** Reflection-based `Huml.Serialize<T>()` emitting HUML text in source declaration order with `%HUML` version header.
- **Deserialiser:** `Huml.Deserialize<T>()` with full type coercion, `List<T>`, `T[]`, `Dictionary<string, T>`, and nested POCO support.
- **Attributes:** `[HumlProperty]` (key rename, `OmitIfDefault`, per-property `InlineMode`) and `[HumlIgnore]`.
- **Public API:** `System.Text.Json`-style static `Huml` facade with `Serialize`, `Deserialize`, and `Parse` overloads.
- **CI/NuGet:** GitHub Actions pipeline with SourceLink, MinVer, and OIDC Trusted Publishing.

[Unreleased]: https://github.com/primeBeri/huml-dotnet/compare/v0.2.0-alpha.1...HEAD
[0.2.0-alpha.1]: https://github.com/primeBeri/huml-dotnet/compare/v0.1.0-alpha.1...v0.2.0-alpha.1
[0.1.0-alpha.1]: https://github.com/primeBeri/huml-dotnet/releases/tag/v0.1.0-alpha.1
