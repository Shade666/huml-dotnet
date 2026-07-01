# Custom Converters

Custom converters let you control how specific .NET types are serialised to and deserialised from
HUML, providing an escape hatch for types that Huml.Net does not handle natively (e.g.
`DateTimeOffset`, `Guid`, record types with private constructors, discriminated unions).

## Implementing a Converter

Extend `HumlConverter<T>` and override `Read` and `Write`:

```csharp
using Huml.Net.Parser;
using Huml.Net.Serialization;

public sealed class DateTimeOffsetConverter : HumlConverter<DateTimeOffset>
{
    public override DateTimeOffset Read(HumlNode node)
    {
        if (node is HumlScalar { Kind: ScalarKind.String, Value: string s })
            return DateTimeOffset.Parse(s, System.Globalization.CultureInfo.InvariantCulture);

        throw new InvalidOperationException($"Expected a string scalar for DateTimeOffset, got {node.GetType().Name}.");
    }

    public override void Write(HumlWriterContext context, DateTimeOffset value)
    {
        // AppendRaw emits a quoted HUML string literal
        context.AppendRaw($"\"{value:O}\"");
    }
}
```

Converter instances are cached and shared across threads — converters must be stateless.

## Registering a Converter

### Option 1: Per-property via attribute

```csharp
using Huml.Net.Serialization;

public class Event
{
    [HumlConverter(typeof(DateTimeOffsetConverter))]
    public DateTimeOffset OccurredAt { get; set; }
}
```

### Option 2: Per-type via attribute

```csharp
[HumlConverter(typeof(DateTimeOffsetConverter))]
public record Timestamp(DateTimeOffset Value);
```

### Option 3: Via HumlOptions.Converters (global registration)

```csharp
var options = new HumlOptions
{
    Converters = new List<HumlConverter> { new DateTimeOffsetConverter() }
};

var dto = HumlSerializer.Deserialize<MyDto>(humlText, options);
```

## Priority Order

When multiple registrations could match, priority is:

1. Property-level `[HumlConverter]` attribute
2. Type-level `[HumlConverter]` attribute
3. `HumlOptions.Converters` list (first match wins)
4. Built-in type dispatch

A `HumlConverterFactory` participates at whichever tier it is registered — tier 2 via
`[HumlConverter(typeof(MyFactory))]`, or tier 3 via `HumlOptions.Converters` — it is not a
separate tier of its own. See [Converter Factories](#converter-factories) below.

## HumlWriterContext Methods

| Method                                 | Use When                                                                 |
| -------------------------------------- | ------------------------------------------------------------------------ |
| `AppendRaw(string huml)`               | You control the raw HUML fragment (quoted string, inline literal, etc.)  |
| `AppendSerializedValue(object? value)` | You want to delegate serialisation of a nested value to built-in dispatch |
| `Depth`                                | You need to compute indentation for a multiline block                    |
| `Options`                              | You need to inspect the active serialisation options                     |

Do NOT call `AppendSerializedValue` with a value of the same type the converter handles — this
causes infinite recursion. Use `AppendRaw` for the converter's own type output.

## CanConvert Override

The default `HumlConverter<T>.CanConvert` matches only `typeof(T)` exactly. Override `CanConvert`
to match a type hierarchy or interface:

```csharp
public sealed class ReadOnlyListConverter : HumlConverter<object>
{
    public override bool CanConvert(Type typeToConvert) =>
        typeToConvert.IsGenericType &&
        typeToConvert.GetGenericTypeDefinition() == typeof(IReadOnlyList<>);

    public override object? Read(HumlNode node) { /* ... */ return null; }
    public override void Write(HumlWriterContext context, object value) { /* ... */ }
}
```

## Nullable Types

A converter registered for `T` automatically applies to `T?` (`Nullable<T>`) as well — no extra
`CanConvert` override is needed. When resolution finds no converter matching `T?` directly, it
looks for one matching the underlying type `T` and, if found, wraps it in an internal null-aware
adapter: a `null` HUML value short-circuits to `null` without invoking the converter, and any other
value is passed straight through. For example, a single globally-registered `HumlConverter<Status>`
transparently serves both a `Status` property and a `Status?` property.

## Converter Factories

Extend `HumlConverterFactory` when one converter *implementation* needs to serve many concrete
types dynamically — for example a lenient converter usable for any enum in a schema. This mirrors
`System.Text.Json.Serialization.JsonConverterFactory`:

```csharp
public sealed class LenientEnumConverterFactory : HumlConverterFactory
{
    public override bool CanConvert(Type typeToConvert) => typeToConvert.IsEnum;

    public override HumlConverter? CreateConverter(Type typeToConvert, HumlOptions options)
    {
        var converterType = typeof(LenientEnumConverter<>).MakeGenericType(typeToConvert);
        return (HumlConverter)Activator.CreateInstance(converterType)!;
    }
}

public sealed class LenientEnumConverter<TEnum> : HumlConverter<TEnum> where TEnum : struct, Enum
{
    public override TEnum Read(HumlNode node) =>
        node is HumlScalar { Kind: ScalarKind.String, Value: string s } &&
        Enum.TryParse<TEnum>(s, ignoreCase: true, out var parsed) ? parsed : default;

    public override void Write(HumlWriterContext context, TEnum value) =>
        context.AppendRaw($"\"{value}\"");
}
```

Register a factory via `HumlOptions.Converters`, or via a **type-level**
`[HumlConverter(typeof(LenientEnumConverterFactory))]`. During resolution, `CreateConverter` is
called once per requested type and the returned converter is cached — it is not re-created on
every value. Returning `null` from `CreateConverter` declines the match and resolution falls
through to the next candidate converter, then built-in dispatch. A factory's own `Read`/`Write`
are never invoked directly; only the converter it produces is used.

A factory cannot be used via a **property-level** `[HumlConverter]` attribute: property-level
converters are resolved once, directly, without the active `HumlOptions` that `CreateConverter`
needs — attempting it throws `InvalidOperationException` as soon as the declaring type's property
metadata is built. Register the factory globally or attach it to the type instead.

## See also

- [Attributes reference](attributes-reference.md) — `[HumlConverter]`.
- [Options reference](options-reference.md) — the `Converters` list and its precedence.
- [Working with the AST](ast-usage.md) — the `HumlNode` types a converter's `Read` receives.
- [E05.CustomConverters](https://github.com/primeBeri/huml-dotnet-examples/tree/main/src/examples/E05.CustomConverters) — runnable example (value-type converter with compact string form).
