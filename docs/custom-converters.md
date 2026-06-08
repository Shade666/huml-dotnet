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

var dto = Huml.Deserialize<MyDto>(humlText, options);
```

## Priority Order

When multiple registrations could match, priority is:

1. Property-level `[HumlConverter]` attribute
2. Type-level `[HumlConverter]` attribute
3. `HumlOptions.Converters` list (first match wins)
4. Built-in type dispatch

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
