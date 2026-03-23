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

The Huml.Net NuGet package version (e.g. `0.1.0`) is independent of the HUML spec versions it supports.
A single package version may support multiple spec versions simultaneously.
