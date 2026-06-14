# Versioning Policy

## Rolling Support Window

Huml.Net maintains a rolling 3-version support window. Currently, HUML v0.1 and v0.2 are both supported.
When v0.3 ships, v0.1 will leave the support window and be removed from the library.

## Current Supported Versions

| Version | Enum Value             | Status                 |
| ------- | ---------------------- | ---------------------- |
| v0.1    | `HumlSpecVersion.V0_1` | Supported (deprecated) |
| v0.2    | `HumlSpecVersion.V0_2` | Current                |

## V0_1 Deprecation

`HumlSpecVersion.V0_1` is marked `[Obsolete]`. Using it in code produces compiler warning **CS0618**:

```text
warning CS0618: 'HumlSpecVersion.V0_1' is obsolete:
  'HumlSpecVersion.V0_1 is deprecated. HUML v0.1 will leave the support window
   when v0.3 ships. Migrate to HumlSpecVersion.V0_2.'
```

### Suppressing CS0618 in Tests or Migration Code

Wrap any intentional V0_1 usage in a targeted pragma pair:

```csharp
#pragma warning disable CS0618
var options = new HumlOptions { SpecVersion = HumlSpecVersion.V0_1 };
#pragma warning restore CS0618
```

Keep the `#pragma restore` on the very next line to avoid accidentally suppressing the warning for unrelated code.

## Removal Policy

When `V0_3` is added to the `HumlSpecVersion` enum, `V0_1` will be removed entirely.
Any code referencing `HumlSpecVersion.V0_1` will fail to compile with a missing-member error.

**Migration path:** replace all `HumlSpecVersion.V0_1` references with `HumlSpecVersion.V0_2`
and remove any `#pragma warning disable CS0618` suppressions.

## Package Version vs Spec Version

Huml.Net package versions **mirror the HUML spec version** they target. The first two digits of
the package version always match the spec version, making it immediately clear which HUML spec a
given release supports.

| Package version series | HUML spec targeted |
| ---------------------- | ------------------ |
| `0.2.x`                | HUML v0.2          |
| `0.3.x`                | HUML v0.3          |

### Release tiers within a series

Each spec-version series follows this progression:

| Version pattern | Meaning                                                              |
| --------------- | -------------------------------------------------------------------- |
| `0.2.0-alpha.1` | Early pre-release — API may still change                             |
| `0.2.0-alpha.2` | Subsequent alpha iteration                                           |
| `0.2.0-beta.1`  | Feature-complete; stabilising — only bug fixes accepted              |
| `0.2.0-rc.1`    | Release candidate — only critical fixes accepted                     |
| `0.2.0`         | Stable release                                                       |
| `0.2.1`         | Patch: bug fixes or non-breaking library additions (not spec-driven) |
| `0.3.0-alpha.1` | First pre-release targeting HUML v0.3                                |

Pre-release identifiers use hyphens as required by SemVer 2.0 (e.g. `0.2.0-alpha.1`, never
`0.2.0_alpha.1`).

### What earns a patch bump (`0.2.1`, `0.2.2`, …)?

A patch bump is appropriate for:

- Bug fixes that do not change the public API surface.
- Performance improvements with no observable behaviour change.
- Non-breaking library additions (new overloads, new built-in converters, new naming policy
  variants) that do not require a spec version bump.

A new `0.x.0` series is required only when the targeted HUML spec version changes.

### Note on `0.1.0-alpha.1`

The `0.1.0-alpha.1` release (Milestone 1, published 2026-05-01) pre-dates this policy and used
an arbitrary version number. From `0.2.0` onward all releases follow the spec-mirrored scheme
described above.

## Version-Preserving Round-Trip

`HumlDocument.DetectedVersion` exposes the spec version declared in the `%HUML` header of a
parsed document:

```csharp
using Huml.Net;

var doc = HumlSerializer.Parse("""
    %HUML v0.1.0
    Key: "value"
    """);

// doc.DetectedVersion == HumlSpecVersion.V0_1
```

The property is always set from the `%HUML` header token — independent of `HumlOptions.VersionSource`.
It is `null` when no header is present or when a `HumlDocument` is constructed directly in code.

Use `DetectedVersion` to preserve the original spec version when round-tripping a document:

```csharp
var doc  = HumlSerializer.Parse(humlText);
var opts = new HumlOptions { SpecVersion = doc.DetectedVersion ?? HumlSpecVersion.V0_2 };

// Round-trip: deserialise with the detected version, then re-serialise with the same version
var dto    = HumlSerializer.Deserialize<MyDto>(humlText, opts);
var output = HumlSerializer.Serialize(dto, opts);

## See also

- [Options reference](options-reference.md) — `VersionSource`, `UnknownVersionBehaviour`, and `SpecVersion` in the full options table.
- [E13.Versioning](https://github.com/primeBeri/huml-dotnet-examples/tree/main/src/examples/E13.Versioning) — runnable example (`DetectedVersion`, version-preserving round-trip, `UseLatest`, and `Throw`).
```
