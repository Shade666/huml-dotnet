# Populate

`HumlSerializer.Populate<T>()` deserialises a HUML document onto an **existing** object instance, overlaying
values rather than constructing a new instance. Properties present in the HUML document overwrite
the corresponding property on the existing instance; properties absent from the document are left
unchanged.

This is useful for the common configuration-file pattern: load defaults from code, then overlay
values from a file.

## Signature

```csharp
// String overload (delegates to span overload via AsSpan())
public static void Populate<T>(string huml, T existing, HumlOptions? options = null);

// Span overload (implementation)
public static void Populate<T>(ReadOnlySpan<char> huml, T existing, HumlOptions? options = null);
```

`T` must be a reference type (class). Passing a struct as `T` throws `ArgumentException` at call
time. Passing `null` for `existing` throws `ArgumentNullException`.

## Usage

```csharp
using Huml.Net;
using Huml.Net.Serialization;

public class ServerConfig
{
    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 8080;
    public bool Debug { get; set; } = false;
}

var config = new ServerConfig();   // defaults applied by property initialisers

HumlSerializer.Populate("""
    %HUML v0.2.0
    Port: 443
    Debug: true
    """, config);

// config.Host  == "localhost"  (not in document — unchanged)
// config.Port  == 443          (overwritten from document)
// config.Debug == true         (overwritten from document)
```

## With Naming Policy

Pass `HumlOptions` as the third argument to use a naming policy or other options:

```csharp
var options = new HumlOptions { PropertyNamingPolicy = HumlNamingPolicy.KebabCase };
var config = new ServerConfig { Host = "localhost", Port = 8080 };

HumlSerializer.Populate("""
    %HUML v0.2.0
    port: 443
    """, config, options);

// config.Host == "localhost" (unchanged)
// config.Port == 443
```

## Exceptions

| Exception                   | When thrown                                                              |
| --------------------------- | ------------------------------------------------------------------------ |
| `ArgumentNullException`     | `huml` string is `null`                                                  |
| `ArgumentNullException`     | `existing` is `null`                                                     |
| `ArgumentException`         | `T` is a value type (struct)                                             |
| `HumlParseException`        | The HUML input is syntactically invalid                                  |
| `HumlDeserializeException`  | A HUML value cannot be mapped to the target property type                |
| `HumlUnsupportedVersionException` | The `%HUML` header declares an unrecognised version (when `UnknownVersionBehaviour.Throw`) |

## Comparison with Deserialize

| | `HumlSerializer.Deserialize<T>()` | `HumlSerializer.Populate<T>()` |
|---|---|---|
| Instance source | Created by `Activator.CreateInstance` | Caller-supplied |
| Missing keys | Property stays at default (type default) | Property stays at caller's value |
| Use case | Full deserialisation | Overlay / config merge |

## Notes

- `init`-only properties on the existing instance are settable. Huml.Net uses
  reflection to write to `init`-only backing fields after construction, matching the same
  behaviour as `Deserialize<T>`.
- If the target type has a `[HumlExtensionData]` property, unknown HUML keys encountered
  during population are captured into that property — the same overflow behaviour as
  `Deserialize<T>`.
- `Populate` does not clear existing collection contents before appending. If the property holds a
  `List<T>` and the HUML document contains a sequence for that key, the list is **replaced** (not
  appended to).

## See also

- [Options reference](options-reference.md) — `UnmappedMemberHandling` and `DefaultIgnoreCondition` apply during population.
- [Extension data](extension-data.md) — unknown keys encountered during population are captured into `[HumlExtensionData]`.
- [E06.Populate](https://github.com/primeBeri/huml-dotnet-examples/tree/main/src/examples/E06.Populate) — runnable example (defaults + overrides pattern).
