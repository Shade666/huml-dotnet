# Required Properties

Huml.Net enforces required properties during deserialisation. When one or more required
members are absent from the HUML input, a single `HumlDeserializeException` is thrown listing
all missing keys.

## [HumlRequired] Attribute

Apply `[HumlRequired]` to any property that must be present in the HUML document:

```csharp
using Huml.Net;
using Huml.Net.Serialization;

public class DatabaseConfig
{
    [HumlRequired]
    public string ConnectionString { get; set; } = string.Empty;

    [HumlRequired]
    public string DatabaseName { get; set; } = string.Empty;

    public int CommandTimeout { get; set; } = 30;
}

// Throws: HumlDeserializeException:
//   "Missing required member(s) on type 'DatabaseConfig': 'ConnectionString', 'DatabaseName'."
var cfg = HumlSerializer.Deserialize<DatabaseConfig>("""
    %HUML v0.2.0
    CommandTimeout: 60
    """);
```

The exception message lists all missing keys in property declaration order.

## C# required Modifier

The C# `required` modifier is detected and enforced equivalently to `[HumlRequired]`:

```csharp
using Huml.Net;

public class ApiConfig
{
    public required string ApiKey { get; set; }
    public required string BaseUrl { get; set; }
    public int RetryCount { get; set; } = 3;
}

var config = HumlSerializer.Deserialize<ApiConfig>("""
    %HUML v0.2.0
    ApiKey: "secret-key"
    BaseUrl: "https://api.example.com"
    """);
// All required members present — deserialisation succeeds
```

## Exception Format

When one or more required members are missing, a single `HumlDeserializeException` is thrown.
The message lists all missing keys:

```
Missing required member(s) on type 'T': 'Key1', 'Key2'.
```

Keys are listed in property declaration order, matching the source type.

## Populate Exemption

`HumlSerializer.Populate<T>()` intentionally does **not** enforce required members. Populate implements
overlay/partial-update semantics — only the keys present in the document are applied to the
existing instance. Required member checking is skipped to allow legitimate partial overlays.

## Compatibility

The C# `required` modifier requires C# 11 / .NET 7 at the call site. However, Huml.Net
detects the `required` modifier via reflection on `netstandard2.1` and earlier TFMs too —
types compiled with `required` on a newer compiler remain compatible.
