# Huml.Net Backlog

## Purpose

This file provides public visibility into planned work for Huml.Net. It is the canonical list of
accepted items that are tracked for implementation across the project.

## How It Works

- Users report bugs and request features via GitHub Issues.
- The maintainer triages issues and promotes accepted items to this backlog.
- Internal planning workflows are not publicly exposed — this file provides transparency into
  what is planned, in progress, and done.
- Items move through statuses: Planned -> In Progress -> Done.

## Backlog

| Category      | Item                                                                                                                            | Version | Priority | Status  |
| ------------- | ------------------------------------------------------------------------------------------------------------------------------- | ------- | -------- | ------- |
| API           | Implement type-directed dispatch in `Serialize(object?, Type)` — currently ignores the `Type` parameter (section 2.3, task 1.1) | V1      | High     | Done    |
| Documentation | Add `<remarks>` to `Huml.Deserialize<T>(ReadOnlySpan<char>)` documenting span-to-string allocation (section 2.3, task 1.2)      | V1      | Medium   | Done    |
| Documentation | Add XML doc to `HumlDocument` clarifying dual role as document root and nested mapping block (section 2.3, task 1.3)            | V1      | Low      | Done    |
| Performance   | Add property-lookup dictionary to `PropertyDescriptor` cache for O(1) deserialiser key lookup (section 5.1, task 2.1)           | V1      | Low      | Done    |
| Performance   | Cache indent strings in `HumlSerializer.Indent()` to eliminate per-call allocation (section 5.1, task 2.2)                      | V1      | Low      | Done    |
| Performance   | Pool `StringBuilder` in serialiser via `[ThreadStatic]` to reduce GC pressure (section 5.1, task 2.3)                           | V2      | Medium   | Planned |
| Performance   | Refactor Lexer to `ref struct` accepting `ReadOnlySpan<char>` for genuine zero-copy deserialisation (section 9, phase 3)        | V2      | High     | Planned |
| Diagnostics   | Carry source position (Line, Column) through AST nodes for richer `HumlDeserializeException` context (section 9, task 4.1)      | V2      | Medium   | Planned |
| API           | Add `HumlOptions` factory method for "header-detected, latest fallback" variant (section 9, task 4.2)                           | V2      | Low      | Planned |
| Testing       | Add concurrency test for `PropertyDescriptor` cache under parallel deserialisation (section 9, task 4.3)                        | V2      | Low      | Planned |
| Documentation | Add CHANGELOG.md with version history from git tags (section 8.2)                                                               | V1      | Low      | Done    |
| Security      | Document uncapped document size limitation; consider optional `MaxDocumentSize` option (section 6.1)                            | V2      | Low      | Planned |
| API           | Add `HumlOptions.PropertyNamingPolicy` and built-in `HumlNamingPolicy.KebabCase`/`SnakeCase`/`CamelCase`/`PascalCase` policies for automatic key↔property name mapping (Phase 999.18) | V2 | High   | Planned |
| API           | Add enum deserialisation support — `Enum.TryParse` with case-insensitive fallback plus optional `[HumlEnumValue]` per-member name override (Phase 999.19) | V2 | High   | Planned |
| API           | Add enum serialisation support — emit member name (or `[HumlEnumValue]`) honouring naming policy; enables round-trip for enum-valued properties (Phase 999.20) | V2 | High   | Planned |
| API           | Add custom converter API: `HumlConverter<T>` abstract base, `[HumlConverter]` attribute, `HumlOptions.Converters` collection (Phase 999.21) | V2 | High   | Planned |
| API           | Add `Huml.Populate<T>(string, T existing, options?)` for config-overlay deserialisation into an existing object instance (Phase 999.22) | V2 | Medium | Planned |
| API           | Add `HumlOptions.DefaultIgnoreCondition` / `[HumlIgnoreDefaults]` for type-level default-omit without per-property `OmitIfDefault` (Phase 999.23) | V2 | Low    | Planned |
| API           | Add `HumlOptions.ValidateDuplicateKeysOnWrite` to catch dictionary key collisions during serialisation (Phase 999.24) | V2 | Low    | Planned |
| API           | Add `IReadOnlySet<T>` deserialisation support to collection dispatch (Phase 999.25) | V2 | Low    | Done    |
| API           | Constructor parameter binding and `init`-only setter support — allow `HumlDeserializer` to bind records, parameterised ctors, and `init`-only properties (Phase 999.26) | V2 | High | Planned |
| API           | Required-property enforcement via `[HumlRequired]` attribute and C# `required` modifier; throws on missing keys (Phase 999.27) | V2 | High | Planned |
| API           | Extension data via `[HumlExtensionData]` — capture unknown keys into a `Dictionary<string, HumlNode>` property for forward-compatible config consumption (Phase 999.28) | V2 | Medium | Planned |
| API           | Polymorphic (de)serialisation with `[HumlDerivedType]` discriminator — dispatch base-class/interface properties via configurable `$type` key (Phase 999.29) | V3 | Medium | Planned |
| Tooling       | AOT / trim safety annotations — add `[RequiresUnreferencedCode]`, `[RequiresDynamicCode]`, `[DynamicallyAccessedMembers]` to reflection-using public API; add `<IsTrimmable>true</IsTrimmable>` (Phase 999.30) | V2 | High | Planned |
| API           | Source-generator seam — define `IHumlTypeInfoResolver` / `HumlTypeInfo<T>` and `HumlOptions.TypeInfoResolver` for future AOT plug-in without breaking changes (Phase 999.31) | V2 | Medium | Planned |
| Performance   | Source-generator implementation — Roslyn incremental generator emitting compiled `HumlTypeInfo<T>`, shipped as `Huml.Net.SourceGeneration` NuGet package; requires Phase 999.31 (Phase 999.32) | V3 | Medium | Planned |
| API           | Number handling modes — `HumlOptions.NumberHandling` flags enum supporting `AllowReadingFromString`, `WriteAsString`, `AllowNamedFloatingPointLiterals` (Phase 999.33) | V3 | Low | Planned |
| API           | Missing-member handling — `HumlOptions.UnmappedMemberHandling` (`Skip` default, `Disallow`) for strict rejection of unknown HUML keys (Phase 999.34) | V3 | Low | Planned |
| API           | Per-member naming-policy override attribute — `[HumlNamingPolicy(typeof(…))]` for member-level convention override independent of `HumlOptions.PropertyNamingPolicy` (Phase 999.35) | V3 | Low | Planned |
| API           | `HumlOptions.Strict` preset — bundles all strict/validation toggles into one factory; requires Phase 999.24/27/34 (Phase 999.36) | V3 | Low | Planned |
| API           | Native `DateOnly` / `TimeOnly` round-trip — built-in ISO-8601 handling matching STJ .NET 10 defaults; confirm `DateTime`/`DateTimeOffset`/`TimeSpan` coverage (Phase 999.37) | V2 | Medium | Planned |
| API           | Add `SortedSet<T>` deserialisation support — materialise as `SortedSet<T>` with default comparer; separate branch from the `HashSet<T>` path added in Phase 16; no conditional compile required (`SortedSet<T>` exists on netstandard2.1) (Phase 999.38) | V2 | Low | Planned |
