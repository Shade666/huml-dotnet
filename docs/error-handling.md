# Error Handling

## Required Using Directive

```csharp
using Huml.Net.Exceptions;
```

## Exception Types

Huml.Net throws four exception types, all in the namespace `Huml.Net.Exceptions`.

| Exception                         | Thrown By                                               | Key Properties                               |
| --------------------------------- | ------------------------------------------------------- | -------------------------------------------- |
| `HumlParseException`              | `Parse`, `Deserialize`                                  | `int Line` (1-based), `int Column` (0-based) |
| `HumlDeserializeException`        | `Deserialize`                                           | `string? Key`, `int? Line`                   |
| `HumlSerializeException`          | `Serialize`                                             | (none beyond `Message`)                      |
| `HumlUnsupportedVersionException` | `Parse`, `Deserialize` (when header version is unknown) | `string DeclaredVersion`                     |

## Operation-to-Exception Mapping

| Operation               | Can Throw                                                                                                                         |
| ----------------------- | --------------------------------------------------------------------------------------------------------------------------------- |
| `Huml.Parse()`          | `HumlParseException`, `HumlUnsupportedVersionException`                                                                           |
| `Huml.Deserialize<T>()` | `HumlParseException` (parse stage), `HumlDeserializeException` (mapping stage), `HumlUnsupportedVersionException` (version stage) |
| `Huml.Serialize<T>()`   | `HumlSerializeException`                                                                                                          |

## Exception Properties

### HumlParseException

Thrown when the HUML input is syntactically invalid.

- `int Line` — 1-based line number where the error occurred
- `int Column` — 0-based column position
- `Message` format: `[{line}:{column}] {description}`

### HumlDeserializeException

Thrown when valid HUML cannot be mapped to the target .NET type.

- `string? Key` — the HUML key where the error occurred (may be `null` if no key context)
- `int? Line` — 1-based line number (may be `null` if no line context)
- `Message` format when key and line are available: `[line {line}] Key '{key}': {description}`

### HumlSerializeException

Thrown when a .NET object cannot be serialised to HUML. Has no additional properties beyond `Message` and `InnerException`.

### HumlUnsupportedVersionException

Thrown when the `%HUML` header declares a version outside the supported range and `UnknownVersionBehaviour` is `Throw`.

- `string DeclaredVersion` — the version string from the document header (e.g. `"v0.3"`)
- `Message` format: `Unsupported HUML spec version '{declaredVersion}'. Supported range: v0.1 – v0.2.`

## Example

```csharp
using Huml.Net;
using Huml.Net.Exceptions;

try
{
    var result = Huml.Deserialize<MyDto>(humlText);
}
catch (HumlParseException ex)
{
    Console.WriteLine($"Parse error at line {ex.Line}, column {ex.Column}: {ex.Message}");
}
catch (HumlDeserializeException ex) when (ex.Key is not null)
{
    Console.WriteLine($"Mapping error at key '{ex.Key}' (line {ex.Line}): {ex.Message}");
}
catch (HumlDeserializeException ex)
{
    Console.WriteLine($"Deserialisation error: {ex.Message}");
}
```

## init-Only Properties

`HumlDeserializeException` is thrown when the target type contains `init`-only properties, because Huml.Net uses post-construction property assignment, not constructor binding.

Unlike `System.Text.Json`, which supports `init`-only setters via object initializer binding, Huml.Net's deserialiser sets properties after constructing an instance with `Activator.CreateInstance`. Properties with `init`-only setters cannot be assigned this way and cause an immediate `HumlDeserializeException`.

**Workaround:** replace `init` setters with `set` setters on properties you want to deserialise.
