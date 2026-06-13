# Constructor Binding

`HumlSerializer.Deserialize` supports types with parameterised constructors — including C# records
and classes that rely on constructor injection rather than property setters. This allows
deserialising directly into immutable or read-only types.

## Constructor Selection

Huml.Net selects a constructor using the same priority as `System.Text.Json`:

1. A constructor annotated with `[HumlConstructor]`
2. A single non-parameterless public constructor (auto-selected when unambiguous)
3. A parameterless public constructor (standard fallback)

If none of these apply, or if multiple constructors exist without `[HumlConstructor]`,
`HumlDeserializeException` is thrown.

## [HumlConstructor] Attribute

Use `[HumlConstructor]` to designate a specific constructor when a type has more than one:

```csharp
using Huml.Net;
using Huml.Net.Serialization;

public class ServerConfig
{
    public string Host { get; }
    public int Port { get; }

    [HumlConstructor]
    public ServerConfig(string host, int port)
    {
        Host = host;
        Port = port;
    }

    // Parameterless overload for other uses — [HumlConstructor] resolves the ambiguity
    public ServerConfig() { Host = "localhost"; Port = 80; }
}

var config = HumlSerializer.Deserialize<ServerConfig>("""
    %HUML v0.2.0
    host: "db.example.com"
    port: 5432
    """);
// config.Host == "db.example.com", config.Port == 5432
```

## Records

C# record types with a primary constructor work without any attribute — the primary constructor
is the only public constructor, so it is selected automatically:

```csharp
using Huml.Net;

public record Point(double X, double Y);

var p = HumlSerializer.Deserialize<Point>("""
    %HUML v0.2.0
    X: 1.5
    Y: -2.0
    """);
// p.X == 1.5, p.Y == -2.0
```

## Parameter Matching

Constructor parameters are matched to HUML keys:

- Case-insensitively (parameter `host` matches key `Host`)
- Naming-policy-aware when `HumlOptions.PropertyNamingPolicy` is set

Parameters with `HasDefaultValue == true` use their declared default when the HUML key is
absent. Parameters without a default value are required — if the corresponding key is missing,
`HumlDeserializeException` is thrown.

## Init-Only Properties

Properties declared with `{ get; init; }` are now settable during deserialisation. This
enables records with `init`-only positional properties to be deserialised via the
parameterless constructor path:

```csharp
using Huml.Net;

public class Config
{
    public string Host { get; init; } = "localhost";
    public int Port { get; init; } = 80;
}

var cfg = HumlSerializer.Deserialize<Config>("""
    %HUML v0.2.0
    Host: "prod.example.com"
    Port: 443
    """);
// cfg.Host == "prod.example.com", cfg.Port == 443
```

`HumlSerializer.Populate<T>()` also supports `init`-only property assignment on the supplied instance.

## Exceptions

| Exception                  | When thrown                                                                |
| -------------------------- | -------------------------------------------------------------------------- |
| `HumlDeserializeException` | No suitable constructor found (ambiguous or missing `[HumlConstructor]`)   |
| `HumlDeserializeException` | A required constructor parameter has no corresponding HUML key             |
| `HumlDeserializeException` | A HUML value cannot be coerced to the parameter's declared type            |
