# Extension Data

The `[HumlExtensionData]` attribute designates a single property on a type as an *overflow
bucket* for HUML keys that do not match any declared property during deserialisation. This
enables forward-compatible configuration consumption — unknown keys from a newer document
version are preserved rather than silently discarded.

## Declaring an Extension Data Property

```csharp
using Huml.Net;
using Huml.Net.Parser;
using Huml.Net.Serialization;

public class PluginConfig
{
    public string Name { get; set; } = string.Empty;
    public int Version { get; set; }

    [HumlExtensionData]
    public Dictionary<string, HumlNode>? Extras { get; set; }
}
```

Any HUML key that does not correspond to `Name` or `Version` is captured in `Extras`.

## Round-Trip Fidelity

Extension data properties participate in serialisation. Captured keys are re-emitted after
all declared properties, in insertion order:

```csharp
var config = Huml.Deserialize<PluginConfig>("""
    %HUML v0.2.0
    Name: "my-plugin"
    Version: 3
    ExperimentalFeature: true
    DebugLog: "verbose"
    """);

// config.Name    == "my-plugin"
// config.Version == 3
// config.Extras contains "ExperimentalFeature" and "DebugLog" as HumlNode entries

string roundTripped = Huml.Serialize(config);
// Output includes ExperimentalFeature and DebugLog after Name and Version
```

## Dictionary Value Types

The extension data property may be typed as either:

- `Dictionary<string, HumlNode>` — preserves the raw AST node, enabling inspection of the
  scalar kind, source position, and nested structure
- `Dictionary<string, object?>` — coerces the node to the CLR value (`string`, `long`,
  `double`, `bool`, or `null`)

## Constraints

- Only one `[HumlExtensionData]` property per type is permitted. Declaring more than one
  throws `InvalidOperationException` at first use (deserialisation or serialisation).
- The property type must be `Dictionary<string, HumlNode>` or `Dictionary<string, object?>`.
  An unsupported type throws `InvalidOperationException` at first use.
- `[HumlExtensionData]` is excluded from the regular property lookup — it never appears as a
  typed target for a HUML key that matches a declared property name.

## Populate Participation

`Huml.Populate<T>()` also captures unknown keys into the extension data property.
