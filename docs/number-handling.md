# Read & write numbers as strings

By default Huml.Net is **strict**: numeric targets accept only HUML number scalars, and numbers
serialise as bare numbers. `HumlNumberHandling` opts into the quoted-string behaviours that
`System.Text.Json`'s `JsonNumberHandling` provides.

## The modes

`HumlNumberHandling` is a `[Flags]` enum, so the read and write opt-ins can be combined:

| Value                    | Effect |
| ------------------------ | ------ |
| `Strict` (default, `0`)  | Numbers only on read; bare numbers on write. |
| `AllowReadingFromString` | A quoted string scalar (`"42"`) is coerced to a numeric target during deserialisation. |
| `WriteAsString`          | Finite numeric values are emitted as quoted strings during serialisation. |

`NaN`, `+inf`, and `-inf` are **never** quoted, regardless of mode.

## Global default

Set `HumlOptions.NumberHandling` to apply a mode to every numeric property:

```csharp
using Huml.Net.Versioning;

var options = new HumlOptions
{
    NumberHandling = HumlNumberHandling.AllowReadingFromString | HumlNumberHandling.WriteAsString,
};

var dto = HumlSerializer.Deserialize<Measurement>("""
    %HUML v0.2.0
    Value: "12.5"
    """, options);
// dto.Value == 12.5  (quoted string coerced because AllowReadingFromString is set)

string huml = HumlSerializer.Serialize(dto, options);
// Value: "12.5"      (quoted because WriteAsString is set)
```

## Per-member override

`[HumlNumberHandling]` overrides the global option for a single property or a whole type, and
takes precedence over `HumlOptions.NumberHandling`:

```csharp
using Huml.Net.Serialization;
using Huml.Net.Versioning;

public class Measurement
{
    [HumlNumberHandling(HumlNumberHandling.WriteAsString)]
    public double Value { get; set; }

    // Uses the global HumlOptions.NumberHandling.
    public int Samples { get; set; }
}
```

## Notes

- The flags compose: `AllowReadingFromString` affects only the read direction, `WriteAsString`
  only the write direction. Combine them for symmetric quoted round-trips.
- `AllowReadingFromString` coerces; it does not change which CLR types are valid targets — a
  non-numeric quoted string still throws `HumlDeserializeException`.

## See also

- [Options reference](options-reference.md) — the `NumberHandling` option.
- [Attributes reference](attributes-reference.md) — `[HumlNumberHandling]`.
- [Custom converters](custom-converters.md) — for number formats beyond these modes.
