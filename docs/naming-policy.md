# Naming Policy

`HumlOptions.PropertyNamingPolicy` transforms .NET property names to HUML keys during
serialisation and deserialisation. Without a policy, property names are used as-is (PascalCase by
default in C#), so `ServerConfig.HostName` maps to the HUML key `HostName`. Set a naming policy
to avoid writing `[HumlProperty]` on every field of a document that uses a different case convention.

## Built-in Policies

| Policy                          | Example: `HostName` → | Example: `MaxConnections` → |
| ------------------------------- | --------------------- | --------------------------- |
| `HumlNamingPolicy.KebabCase`    | `host-name`           | `max-connections`           |
| `HumlNamingPolicy.SnakeCase`    | `host_name`           | `max_connections`           |
| `HumlNamingPolicy.CamelCase`    | `hostName`            | `maxConnections`            |
| `HumlNamingPolicy.PascalCase`   | `HostName`            | `MaxConnections`            |

## Usage

```csharp
using Huml.Net;
using Huml.Net.Serialization;

public class ServerConfig
{
    public string HostName { get; set; } = string.Empty;
    public int MaxConnections { get; set; }
}

var options = new HumlOptions { PropertyNamingPolicy = HumlNamingPolicy.KebabCase };

// Serialise — emits kebab-case keys
string huml = HumlSerializer.Serialize(new ServerConfig { HostName = "db.example.com", MaxConnections = 100 }, options);
// %HUML v0.2.0
// host-name: "db.example.com"
// max-connections: 100

// Deserialise — reads kebab-case keys
var config = HumlSerializer.Deserialize<ServerConfig>(huml, options);
// config.HostName == "db.example.com"
```

## Precedence

`[HumlProperty("explicit-name")]` always takes precedence over the naming policy. Use the policy
as a global default; use the attribute to override individual exceptions.

```csharp
public class Config
{
    [HumlProperty("api-key")]       // explicit override wins over policy
    public string ApiKey { get; set; } = string.Empty;

    public string BaseUrl { get; set; } = string.Empty;   // transformed by policy
}
```

## Custom Policy

Implement `HumlNamingPolicy` by overriding `ConvertName`:

```csharp
public sealed class ScreamingSnakeCasePolicy : HumlNamingPolicy
{
    public override string ConvertName(string name) =>
        HumlNamingPolicy.SnakeCase.ConvertName(name).ToUpperInvariant();
}

var options = new HumlOptions { PropertyNamingPolicy = new ScreamingSnakeCasePolicy() };
```

Custom policy instances are treated as equal when they have the same concrete type (via the
overridden `Equals`/`GetHashCode` on `HumlNamingPolicy`), so stateless subclasses share a single
`PropertyDescriptor` cache entry.

## Notes

- The naming policy applies only to .NET property names. It does not affect `Dictionary<string, T>` keys or
  `[HumlProperty]` explicit names.
- Acronyms in built-in kebab/snake policies split letter-by-letter (`URL` → `u-r-l`). For
  acronym-aware conversion, use `[HumlProperty]` directly.
- The policy is applied once at descriptor build time and cached per `(Type, HumlNamingPolicy?)` pair.

## See also

- [Options reference](options-reference.md) — `PropertyNamingPolicy` in context with all other options.
- [E02.NamingPolicies](https://github.com/primeBeri/huml-dotnet-examples/tree/main/src/examples/E02.NamingPolicies) — runnable example (kebab-case + `[HumlProperty]` override).
- [E09.Options](https://github.com/primeBeri/huml-dotnet-examples/tree/main/src/examples/E09.Options) — runnable example (snake_case policy alongside other options).
