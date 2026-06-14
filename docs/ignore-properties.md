# Ignore properties & omit default values

Huml.Net offers several levels of control over which properties are written — from excluding a
member entirely to suppressing only its default or null values. This mirrors
`System.Text.Json`'s `[JsonIgnore]` and `DefaultIgnoreCondition`.

## Exclude a property entirely — `[HumlIgnore]`

`[HumlIgnore]` removes a property from **both** serialisation and deserialisation:

```csharp
using Huml.Net.Serialization;

public class User
{
    public string Name { get; set; } = "";

    [HumlIgnore]
    public string PasswordHash { get; set; } = "";   // never written, never read
}
```

## Omit one property when it holds its default — `[HumlProperty(OmitIfDefault = true)]`

```csharp
public class Server
{
    public string Host { get; set; } = "";

    [HumlProperty("port", OmitIfDefault = true)]
    public int Port { get; set; }   // omitted from output when 0
}
```

## Omit defaults across a whole type — `[HumlIgnoreDefaults]`

```csharp
[HumlIgnoreDefaults]
public class Config
{
    public string Name { get; set; } = "";
    public int Retries { get; set; }       // omitted when 0
    public bool Verbose { get; set; }      // omitted when false
}
```

## Set a global default — `HumlOptions.DefaultIgnoreCondition`

`DefaultIgnoreCondition` applies to every property unless a more specific rule overrides it. It is
a **serialisation-only** setting — it has no effect when deserialising.

```csharp
using Huml.Net.Serialization;

var options = new HumlOptions
{
    DefaultIgnoreCondition = HumlIgnoreCondition.WhenWritingNull,
};
```

| `HumlIgnoreCondition`  | Omits a property when… |
| ---------------------- | ---------------------- |
| `Never` (default)      | never — all properties are emitted. |
| `WhenWritingNull`      | its value is `null`. |
| `WhenWritingDefault`   | its value equals the CLR default for its type (includes `null`, `0`, `false`). |
| `Always`               | unconditionally — the property is never written. |

## Precedence

When more than one rule could apply to a property, the most specific wins:

```
per-property [HumlProperty(OmitIfDefault = true)]
    → type-level [HumlIgnoreDefaults]
        → global HumlOptions.DefaultIgnoreCondition
```

`[HumlIgnore]` is absolute — an ignored property is never written or read regardless of the above.

## See also

- [Attributes reference](attributes-reference.md) — `[HumlIgnore]`, `[HumlIgnoreDefaults]`, `[HumlProperty]`.
- [Options reference](options-reference.md) — `DefaultIgnoreCondition`.
- [Customize property names](naming-policy.md) — the other half of shaping output.
