# Serialize polymorphic types

When a property is typed as a base class or interface, Huml.Net can emit a **type discriminator**
so the concrete type is recovered on deserialisation. This mirrors `System.Text.Json`'s
`[JsonPolymorphic]` / `[JsonDerivedType]` model.

## Mark the base and register the derived types

Apply `[HumlPolymorphic]` to the base class or interface, and one `[HumlDerivedType]` per concrete
subtype, giving each a discriminator label:

```csharp
using Huml.Net.Serialization;

[HumlPolymorphic]
[HumlDerivedType(typeof(Circle), "circle")]
[HumlDerivedType(typeof(Rectangle), "rectangle")]
public abstract class Shape
{
    public string Name { get; set; } = "";
}

public sealed class Circle : Shape
{
    public double Radius { get; set; }
}

public sealed class Rectangle : Shape
{
    public double Width { get; set; }
    public double Height { get; set; }
}
```

## Round-trip

The discriminator is emitted as the **first** mapping entry, under the key `_type` by default:

```csharp
Shape shape = new Circle { Name = "c1", Radius = 2.5 };

string huml = HumlSerializer.Serialize(shape);
// %HUML v0.2.0
// _type: "circle"
// Name: "c1"
// Radius: 2.5

var back = HumlSerializer.Deserialize<Shape>(huml);
// back is a Circle; back.Radius == 2.5
```

The discriminator is honoured in nested and collection positions too — a `List<Shape>` or a
`Shape`-typed property each carry their own `_type` entry.

## Customise the discriminator key

Pass a key name to `[HumlPolymorphic]` to use something other than `_type`:

```csharp
[HumlPolymorphic("kind")]
[HumlDerivedType(typeof(Circle), "circle")]
public abstract class Shape { /* … */ }
// emits  kind: "circle"  as the first entry
```

## Handle unknown discriminators

By default, a discriminator value with no matching `[HumlDerivedType]` throws
`HumlDeserializeException`. Set `UnknownDerivedTypeHandling` to fall back to the base type instead:

```csharp
using Huml.Net.Versioning;

[HumlPolymorphic(UnknownDerivedTypeHandling = HumlUnknownDerivedTypeHandling.FallBackToBaseType)]
[HumlDerivedType(typeof(Circle), "circle")]
public abstract class Shape { /* … */ }
```

| `HumlUnknownDerivedTypeHandling` | Behaviour on an unrecognised discriminator |
| -------------------------------- | ------------------------------------------ |
| `Throw` (default)                | Throws `HumlDeserializeException`.         |
| `FallBackToBaseType`             | Deserialises as the base type, ignoring the unknown discriminator. |

## Notes

- The discriminator key is stripped before the rest of the mapping is bound, so it does not trip
  `UnmappedMemberHandling.Disallow`.
- A runtime instance whose concrete type is **not** registered with `[HumlDerivedType]` currently
  serialises without a discriminator; registering every concrete subtype avoids silent loss of
  type information.

## See also

- [Attributes reference](attributes-reference.md) — `[HumlPolymorphic]` / `[HumlDerivedType]`.
- [Handle errors](error-handling.md) — `HumlDeserializeException` details.
- <xref:Huml.Net.HumlSerializer> — facade signatures.
