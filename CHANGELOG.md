# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/).

**Versioning:** from `0.2.0` onward, the first two digits of the package version mirror the
targeted HUML spec version (`0.2.x` → HUML v0.2, `0.3.x` → HUML v0.3).
See [docs/versioning.md](docs/versioning.md) for the full policy.

## [Unreleased]

### Performance
- **`StringBuilder` pooling in `HumlSerializer`:** Both `Serialize` overloads now reuse a `[ThreadStatic]` `StringBuilder` across calls on the same thread, eliminating one `StringBuilder` allocation and one backing `char[]` growth per `Huml.Serialize` call on hot paths. A second `[ThreadStatic]` sentinel (`_serializationActive`) ensures re-entry from a `HumlConverter.Write` that calls `Huml.Serialize` internally falls back to a fresh `StringBuilder` rather than corrupting the pooled instance. No public API or emitted HUML format change.

### Added
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
