# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/).

**Versioning:** from `0.2.0` onward, the first two digits of the package version mirror the
targeted HUML spec version (`0.2.x` → HUML v0.2, `0.3.x` → HUML v0.3).
See [docs/versioning.md](docs/versioning.md) for the full policy.

## [Unreleased]

### Added

- **`[HumlPolymorphic]` attribute:** Marks a class or interface as the polymorphic base for discriminator-based dispatch. Controls the discriminator key name (default `_type`) and `HumlUnknownDerivedTypeHandling` behaviour.
- **`[HumlDerivedType(Type, string)]` attribute:** Registers a concrete derived type and its discriminator label on the polymorphic base class. Repeatable — add one per concrete subtype.
- **`HumlUnknownDerivedTypeHandling` enum:** `Throw` (default) — throws `HumlDeserializeException` for unrecognised discriminator values. `FallBackToBaseType` — deserialises as the base type without throwing.
- **Serialiser polymorphic emit:** When the declared type carries `[HumlPolymorphic]` and the runtime instance is a registered derived type, the discriminator key is emitted as the first mapping entry.
- **Deserialiser polymorphic dispatch:** Strips the discriminator key before POCO construction so `UnmappedMemberHandling.Disallow` is not triggered by the discriminator entry.
- **`HumlNumberHandling` enum:** `[Flags]` enum with `Strict` (0), `AllowReadingFromString` (1), `WriteAsString` (2).
- **`HumlOptions.NumberHandling`:** New `init`-only property (default `Strict`). `AllowReadingFromString` opts into coercing quoted-string scalars to numeric target types during deserialisation. `WriteAsString` opts into quoting finite numeric values on serialisation. `NaN`, `+inf`, and `-inf` are never quoted.
- **`[HumlNumberHandling]` attribute:** Per-member override for `HumlNumberHandling`; stored in `PropertyDescriptor` at cache-build time; takes precedence over `HumlOptions.NumberHandling` for the annotated property during both serialisation and deserialisation.
- **`HumlKnownNamingPolicy` enum:** `Unspecified = 0` (defers to global), `CamelCase = 1`, `SnakeCase = 2`, `KebabCase = 3`, `PascalCase = 4`. Identifies a built-in naming policy for use with `[HumlNamingPolicy]`.
- **`[HumlNamingPolicy]` attribute:** Per-member override for the naming policy applied to HUML key generation. Takes precedence over `HumlOptions.PropertyNamingPolicy` for the annotated property. `[HumlProperty(Name = ...)]` still wins over `[HumlNamingPolicy]`. Stored as `PropertyDescriptor.MemberNamingPolicy` at cache-build time.
- **Adversarial and fuzz parse tests (`FuzzParserTests`):** 25 hand-crafted adversarial inputs (Fuzz01-Fuzz25) covering truncated documents, nesting depth at and beyond the configured limit, very long keys and values, bidi override characters (U+202E, U+202D), null bytes (U+0000), lone surrogates (U+D800, U+DC00), unknown and malformed version headers, and extreme numeric literals. Asserts the safety invariant: `HumlParseException` or `HumlUnsupportedVersionException` are acceptable outcomes; `NullReferenceException`, `IndexOutOfRangeException`, `ArgumentOutOfRangeException`, and `OverflowException` are not.
- **`HumlPropertyInfo` class:** New public `Huml.Net.Serialization.HumlPropertyInfo` class with `Name` (string), `PropertyType` (Type?), `Get` (Func<object, object?>?), `Set` (Action<object, object?>?), `IsRequired` (bool), and `Order` (int) properties. All properties are settable. Mirrors STJ's `JsonPropertyInfo` delegate shape to avoid covariance issues across the type hierarchy.
- **`HumlTypeInfo` lifecycle callbacks:** `OnSerializing`, `OnSerialized`, `OnDeserializing`, and `OnDeserialized` (`Action<object>?`) virtual properties added to `HumlTypeInfo`; all default to null.
- **`HumlTypeInfo` property metadata:** `Properties` (`IReadOnlyList<HumlPropertyInfo>?`) virtual property added to `HumlTypeInfo`; null means "fall through to reflection path", an empty list means "type has no properties".
- **`HumlTypeInfo<T>.CreateObject`:** `Func<T>?` virtual property added to `HumlTypeInfo<T>`; null means fall back to `Activator.CreateInstance`. Typed on the generic form to preserve static type safety at the call site.
- **`IHumlTypeInfoResolver` activation (TI-04):** When `options.TypeInfoResolver?.GetTypeInfo(type, options)` returns a `HumlTypeInfo` with non-null `Properties`, both the serialiser and deserialiser use the provided `HumlPropertyInfo` delegates (`Get`/`Set`) instead of reflection. Lifecycle callbacks (`OnSerializing`, `OnSerialized`, `OnDeserializing`, `OnDeserialized`) are invoked in the correct order around the delegate loop. The resolver path bypasses the required-property check and unmapped-member check; the resolver takes full responsibility for property population. When `Properties` is null, both paths fall through to the existing `PropertyDescriptor` reflection path unchanged.

### Breaking Changes

- **`HumlSerializerContext` renamed to `HumlWriterContext`:** All `HumlConverter<T>` implementations must update their `Write(HumlWriterContext context, T value)` override signature. The old name is not available as an alias.

### Changed

- **`ScanComment` in Lexer:** Merged two sequential EOF/non-space checks into a single short-circuit condition.
- **`PropertyDescriptor.BuildDescriptors`:** `Nullable<T>` default values are now assigned as `null` directly without calling `Activator.CreateInstance`.
- **`HumlOptions.MakeReadOnly()`:** `Converters` list is now frozen to an immutable copy on lock; post-freeze mutations to the original list reference no longer affect converter resolution.
- **`HumlNamingPolicy` XML docs:** Added digit-as-word-boundary examples (`B2B → b-2-b`, `Version2Name → version-2-name`) to `KebabCase` and `SnakeCase` remarks.
- **`InferScalarOrInlineListRootType` comment** corrected to accurately describe single-slot pending-buffer token management.

### Deprecated

- **`HumlDeserializeException(string message, string? key, int line)` (3-argument constructor)** is now `[Obsolete]`. Use the 4-argument constructor `(message, key, line, column)` instead.

## [0.2.0-alpha.3] - 2026-06-06

### Added
- **Document size limitation documented:** `HumlOptions` XML docs and `docs/options-reference.md` now explicitly state that no maximum document size is enforced. Callers parsing untrusted input must impose their own size limit before invoking any `Huml.*` method.
- **`HumlOptions.Strict`:** New maximum-strictness preset bundling all validation toggles: reads the `%HUML` version header (`VersionSource.Header`), throws on unknown versions (`UnknownVersionBehaviour.Throw`), disallows unmapped keys (`UnmappedMemberHandling.Disallow`), and validates duplicate dictionary keys on write (`ValidateDuplicateKeysOnWrite = true`). Pre-frozen at static-initialisation time. Mirrors the STJ .NET 10 `JsonSerializerOptions.Strict` preset.
- **`HumlOptions.UnmappedMemberHandling`:** New option controlling how the deserialiser handles HUML keys that do not map to any property on the target type. `Skip` (default) silently ignores unknown keys, preserving existing forward-compatibility behaviour. `Disallow` throws `HumlDeserializeException` listing the unrecognised key. A `[HumlExtensionData]` property on the type suppresses `Disallow` — unknown keys are routed there instead.
- **`HumlOptions.LatestSupportedAutoDetect`:** New pre-built options preset that reads the `%HUML` header and silently falls back to the latest supported version when the declared version is unknown or outside the support window. Unlike `Default`/`AutoDetect` (which throw `HumlUnsupportedVersionException` on unknown versions), this preset is permissive — use it when consuming documents from heterogeneous sources where version drift is expected.
- **`HumlOptions.MakeReadOnly()` / `IsReadOnly`:** Pre-built instances (`Default`, `LatestSupported`, `AutoDetect`) are now frozen at static-initialisation time. Calling `MakeReadOnly()` on any instance sets `IsReadOnly = true`; the call is idempotent. An internal `ThrowIfReadOnly()` guard helper is wired for future mutable-setter additions. Mirrors the STJ .NET 7+ pattern.

### Fixed
- **`ParseInt` throws `HumlParseException` for out-of-range hex literals (CR-03):** Previously, a hex literal that overflows `int64` (e.g. `0x10000000000000000`) caused an unhandled `OverflowException` to propagate out of the parser, breaking the documented contract that only `HumlParseException` is thrown for parse errors. Now wrapped and re-thrown as a `HumlParseException` with source position.
- **`ParseFloat` throws `HumlParseException` for malformed float tokens (WR-08):** A `FormatException` from `double.Parse` is now caught and converted to a `HumlParseException`, ensuring the public exception contract is maintained for malformed float literals.
- **`ScanBacktickMultiline` strips structural indentation (CR-01):** Content lines inside v0.1 backtick multiline strings were captured with their full leading indentation intact (including the structural `keyIndent + 2` prefix), causing silent data corruption. Each content line now strips `min(keyIndent + 2, spaces)` spaces, matching the v0.2 triple-quote stripping semantics.
- **`MeasureIndent` no-op tautology removed (CR-02):** The expression `_pos = p - indent + indent` (identically `_pos = p`) was replaced with the cleaner `_pos = p` and an accurate comment. Behaviour is unchanged.
- **`TimeOnly` deserialisation now uses `ParseExact` (CR-04):** Previously `TimeOnly.Parse` was used, which accepts multiple formats and could silently accept inputs the serialiser would never emit. Replaced with `TimeOnly.ParseExact(raw, "HH:mm:ss.FFFFFFF", CultureInfo.InvariantCulture)` to enforce the exact round-trip format.
- **`boundKeys` comparer changed from `OrdinalIgnoreCase` to `Ordinal` (CR-06):** The internal tracking set for property-bound HUML keys always stores canonical descriptor keys and is looked up by the same canonical key, so `OrdinalIgnoreCase` was misleading and inconsistent with the property-lookup dictionary. Changed to `StringComparer.Ordinal` for correctness and clarity.
- **`HumlConverter<T>.WriteObject` no longer passes null to `Write` for reference types (IN-04):** Previously, when a property value was null for a reference-type converter, `WriteObject` called `Write(context, default!)` passing `null`. Converters that do not handle null would throw `NullReferenceException` at an arbitrary point. Now emits `"null"` directly via `AppendRaw`, matching the built-in serialiser's null-value behaviour.
- **`Huml.Populate<T>(string, ...)` now guards `existing` before parsing (WR-01):** The string overload previously only guarded `huml` for null, leaving the `existing` null check to the span overload inside `HumlDeserializer`. Added an explicit `ArgumentNullException` guard at the call site — consistent with the `huml` guard already present — so callers receive the null check before any parsing work begins.
- **`HumlIgnoreCondition.Always` XML doc corrected (IN-01):** The `Always` member doc and class-level remarks incorrectly described `Always` as "equivalent to `WhenWritingNull | WhenWritingDefault`". While both share the numeric value `3`, `Always` is handled as a named special case in the serialiser and omits **every** property including non-null non-default values. The docs now accurately describe unconditional omission and note that `Always` is not flag-composition.
- **`HumlConverter<T>.Write` doc warns about single-line output constraint (WR-07):** The abstract `Write` method now documents that when invoked from a property-level converter, its output is embedded inline as a mapping-entry scalar value and must therefore not contain embedded newlines. Also documents that `value` is never `null` for reference types (null is emitted by `WriteObject` before `Write` is called).
- **`EmitEntry` re-entry guard now covers null-valued converter calls (CR-05):** The converter re-entry guard previously used `value?.GetType()`, which meant the guard was skipped entirely when `value` was null. A null-valued re-entry would produce an undetected infinite recursion. The guard now falls back to `converterOverride.GetType()` when `value` is null, ensuring recursive converter calls are detected regardless of value nullability.
- **`IDictionary<string, T>` properties now deserialise correctly (WR-11):** `IsStringKeyedDictionary` previously only matched `Dictionary<string, T>`. Targets declared as `IDictionary<string, T>` fell through to the POCO path, which attempted to instantiate an interface and threw a misleading `HumlDeserializeException`. Both the recognition guard and the materialisation step now correctly handle `IDictionary<string, T>`, materialising as `Dictionary<string, T>`.
- **`SharedSuiteTests` now produces a helpful error when fixture submodules are missing (WR-12):** The fixture directory existence check was only applied to the extension directory. The primary directory (`fixtures/{version}/assertions`) now throws `DirectoryNotFoundException` with an actionable message (`git submodule update --init`) rather than an opaque `DirectoryNotFoundException` from `Directory.GetFiles`.
- **Test isolation: option-instance converter caches are now cleared alongside static caches (IN-02, WR-09):** `HumlOptions` pre-built instances (`Default`, `LatestSupported`, `LatestSupportedAutoDetect`, `Strict`) each hold a per-instance `ConverterResolutionCache`. Tests that cleared the static `ConverterCache` but left these per-instance caches populated could observe stale converter resolutions from prior tests. Added `HumlOptions.ClearOptionsCaches()` (internal) to clear all four caches, and wired it into the constructors of `HumlConverterTests` and `HumlPopulateTests`.

### Changed
- **`HumlSerializeException` enriched with property and type context:** When `HumlSerializer` encounters an unsupported type (delegates, function pointers) on a POCO property, the exception message now includes the property name and containing type name — e.g. `"Cannot serialize property 'Handler' on type 'MyDto': delegates, function pointers, and similar non-data types are not supported by HumlSerializer."` Previously the message only named the unsupported type with no source location. Serialisation of unsupported items in sequences or direct values retains the prior format.

## [0.2.0-alpha.2] - 2026-05-19

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

[Unreleased]: https://github.com/primeBeri/huml-dotnet/compare/v0.2.0-alpha.3...HEAD
[0.2.0-alpha.3]: https://github.com/primeBeri/huml-dotnet/compare/v0.2.0-alpha.2...v0.2.0-alpha.3
[0.2.0-alpha.2]: https://github.com/primeBeri/huml-dotnet/compare/v0.2.0-alpha.1...v0.2.0-alpha.2
[0.2.0-alpha.1]: https://github.com/primeBeri/huml-dotnet/compare/v0.1.0-alpha.1...v0.2.0-alpha.1
[0.1.0-alpha.1]: https://github.com/primeBeri/huml-dotnet/releases/tag/v0.1.0-alpha.1
