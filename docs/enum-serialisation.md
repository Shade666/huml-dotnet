# Enum Serialisation

Huml.Net serialises enum values as quoted strings and deserialises them by name lookup. Round-trips
are symmetric: the same name used during serialisation is accepted during deserialisation.

## Default Behaviour

Without any attributes or naming policy, the C# member name is used as the HUML string value:

```csharp
public enum LogLevel { Debug, Info, Warning, Error }

public class Config
{
    public LogLevel Level { get; set; }
}

string huml = HumlSerializer.Serialize(new Config { Level = LogLevel.Warning });
// %HUML v0.2.0
// Level: "Warning"

var config = HumlSerializer.Deserialize<Config>("""
    %HUML v0.2.0
    Level: "Warning"
    """);
// config.Level == LogLevel.Warning
```

## Custom Member Names — HumlEnumValueAttribute

Apply `[HumlEnumValue("name")]` to a field to override the HUML string for that member:

```csharp
using Huml.Net.Serialization;

public enum LogLevel
{
    [HumlEnumValue("debug")]   Debug,
    [HumlEnumValue("info")]    Info,
    [HumlEnumValue("warning")] Warning,
    [HumlEnumValue("error")]   Error,
}
```

```csharp
string huml = HumlSerializer.Serialize(new Config { Level = LogLevel.Warning });
// Level: "warning"
```

## Naming Policy Integration

When `HumlOptions.PropertyNamingPolicy` is set, the policy transforms enum member names the same
way it transforms property names — unless `[HumlEnumValue]` is present (which always wins):

```csharp
var options = new HumlOptions { PropertyNamingPolicy = HumlNamingPolicy.KebabCase };

public enum ConnectionState { Connected, Disconnected, ReconnectingNow }

// With KebabCase policy (and no [HumlEnumValue]):
// Connected       → "connected"
// Disconnected    → "disconnected"
// ReconnectingNow → "reconnecting-now"
```

## Deserialisation Lookup

Deserialisation uses a case-sensitive lookup first, falling back to case-insensitive lookup if no
case-sensitive match is found. `HumlDeserializeException` is thrown if no match is found at all.

## Nullable Enums

Nullable enum properties (`LogLevel?`) are fully supported. A HUML `null` scalar maps to `null`
in C#; a string value is looked up normally.

```csharp
public class Config { public LogLevel? Level { get; set; } }

var config = HumlSerializer.Deserialize<Config>("""
    %HUML v0.2.0
    Level: null
    """);
// config.Level == null
```
