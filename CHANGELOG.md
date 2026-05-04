# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/).

**Versioning:** from `0.2.0` onward, the first two digits of the package version mirror the
targeted HUML spec version (`0.2.x` → HUML v0.2, `0.3.x` → HUML v0.3).
See [docs/versioning.md](docs/versioning.md) for the full policy.

## [Unreleased]

### Added
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
