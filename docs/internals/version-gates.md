# Version Gates

This document explains how Huml.Net gates parser and lexer behaviour on the active HUML spec
version, and provides a step-by-step checklist for adding support for a new spec version.

## How Version Gates Work

Huml.Net uses a single code path for all supported spec versions — there are no forked `Lexer` or
`HumlParser` classes per spec version. Behavioural differences are expressed as explicit
conditional branches within the single code path, using a consistent `>=` comparison:

```csharp
if (_effectiveSpecVersion >= HumlSpecVersion.V0_2)
{
    // v0.2-only behaviour
}
```

All gates use `>=`, never `==`. This makes them forward-compatible: a gate that activates for
v0.2 automatically activates for v0.3, v0.4, and beyond, without any code change.

To find all version gates in the codebase:

```bash
grep -rn ">= HumlSpecVersion" src/
```

`HumlSpecVersion` is an `int`-backed enum (`V0_1 = 1`, `V0_2 = 2`). The integer backing makes
`>=` comparisons well-defined and also allows `SpecVersionPolicy` to use the enum values as
`const` fields (in `src/Huml.Net/Versioning/SpecVersionPolicy.cs`).

## Adding a New Spec Version

Follow these steps in order when adding support for a new HUML spec version (e.g. v0.3):

1. Add an enum member to `HumlSpecVersion` in `src/Huml.Net/Versioning/HumlSpecVersion.cs`:
   ```csharp
   V0_3 = 3,
   ```

2. Update `SpecVersionPolicy.Latest` (string constant) and `SpecVersionPolicy.LatestVersion`
   (enum constant) in `src/Huml.Net/Versioning/SpecVersionPolicy.cs` to point to the new version.

3. Add the fixture submodule at `fixtures/v0.3/` pinned to the `tests@v0.3` tag:
   ```bash
   git submodule add https://github.com/huml-lang/tests fixtures/v0.3
   cd fixtures/v0.3 && git checkout v0.3
   ```

4. Add a `<Content>` include for `fixtures/v0.3/` in
   `tests/Huml.Net.Tests/Huml.Net.Tests.csproj`, mirroring the existing `v0.1` and `v0.2`
   entries, so the fixture files are copied to the test output directory.

5. Add a shared suite test method `V03_fixture_passes` in
   `tests/Huml.Net.Tests/SharedSuiteTests.cs`, mirroring the existing `V01_fixture_passes` and
   `V02_fixture_passes` methods.

6. Add `>= HumlSpecVersion.V0_3` gates in `src/Huml.Net/Lexer/Lexer.cs` and
   `src/Huml.Net/Parser/HumlParser.cs` for each new behavioural difference introduced by the
   new spec version.

7. Update `HumlOptions.Default` and/or `HumlOptions.LatestSupported` in
   `src/Huml.Net/Versioning/HumlOptions.cs` if the new version becomes the pinned default.

8. Update the support table in `docs/versioning.md`.

## Deprecation Process

When a spec version exits the support window, mark its enum member with `[Obsolete]`:

```csharp
[Obsolete("HumlSpecVersion.V0_1 is deprecated. HUML v0.1 will leave the support window " +
          "when v0.3 ships. Migrate to HumlSpecVersion.V0_2.")]
V0_1 = 1,
```

`V0_1` is the live example — its deprecation message is already in place in
`src/Huml.Net/Versioning/HumlSpecVersion.cs`.

Any code that must still reference a deprecated version (e.g. `SpecVersionPolicy.MinimumSupportedVersion`,
test helpers) wraps the reference with a targeted pragma suppression:

```csharp
#pragma warning disable CS0618
public const HumlSpecVersion MinimumSupportedVersion = HumlSpecVersion.V0_1;
#pragma warning restore CS0618
```

Keep `#pragma warning restore CS0618` on the very next line after the reference. This minimises
the suppression scope and avoids accidentally silencing unrelated warnings.

## Testing Version Gates

Use `HumlOptions.LatestSupported` in tests when you want pinned v0.2 behaviour that ignores any
`%HUML` header in the input:

```csharp
var result = HumlSerializer.Parse(input, HumlOptions.LatestSupported);
```

Use `HumlOptions.Default` when header-aware behaviour is under test (the parser reads the
`%HUML` header and applies the version declared there):

```csharp
var result = HumlSerializer.Parse(input, HumlOptions.Default);
```

To force a specific version regardless of any header in the input, construct `HumlOptions`
explicitly and set `VersionSource = VersionSource.Options`. For deprecated versions, wrap with
the CS0618 pragma:

```csharp
#pragma warning disable CS0618
private static readonly HumlOptions V01Options =
    new() { SpecVersion = HumlSpecVersion.V0_1, VersionSource = VersionSource.Options };
#pragma warning restore CS0618
```

The `V01Options` field in `tests/Huml.Net.Tests/SharedSuiteTests.cs` shows this pattern in use.
