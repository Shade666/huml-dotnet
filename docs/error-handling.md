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
| `HumlDeserializeException`        | `Deserialize`, `Populate`                               | `string? Key`, `int? Line`, `int? Column`    |
| `HumlSerializeException`          | `Serialize`                                             | (none beyond `Message`)                      |
| `HumlUnsupportedVersionException` | `Parse`, `Deserialize` (when header version is unknown) | `string DeclaredVersion`                     |

## Operation-to-Exception Mapping

| Operation               | Can Throw                                                                                                                         |
| ----------------------- | --------------------------------------------------------------------------------------------------------------------------------- |
| `HumlSerializer.Parse()`          | `HumlParseException`, `HumlUnsupportedVersionException`                                                                           |
| `HumlSerializer.Deserialize<T>()` | `HumlParseException` (parse stage), `HumlDeserializeException` (mapping stage, missing `[HumlRequired]` / C# `required` members, or unknown key when `UnmappedMemberHandling = Disallow`), `HumlUnsupportedVersionException` (version stage) |
| `HumlSerializer.Populate<T>()`    | `ArgumentNullException` (null `huml` string or null existing instance), `ArgumentException` (T is a value type), `HumlParseException` (parse stage), `HumlDeserializeException` (mapping stage, or unknown key when `UnmappedMemberHandling = Disallow`), `HumlUnsupportedVersionException` (version stage) |
| `HumlSerializer.Serialize<T>()`   | `HumlSerializeException`                                                                                                          |

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
- `int? Column` — 0-based column position (may be `null` if no column context)
- `Message` format when key and line are available: `[line {line}, col {column}] Key '{key}': {description}`

### HumlSerializeException

Thrown when a .NET object cannot be serialised to HUML. Has no additional properties beyond
`Message` and `InnerException`.

When the unserialisable value originates from a named POCO property, the message includes
property and type context: `"Cannot serialize property 'Handler' on type 'MyDto': delegates,
function pointers, and similar non-data types are not supported by HumlSerializer."` For
non-property paths (sequence elements, direct values) the prior short format
(`"Cannot serialize type '...'."`) is retained.

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
    var result = HumlSerializer.Deserialize<MyDto>(humlText);
}
catch (HumlParseException ex)
{
    Console.WriteLine($"Parse error at line {ex.Line}, column {ex.Column}: {ex.Message}");
}
catch (HumlDeserializeException ex) when (ex.Key is not null)
{
    Console.WriteLine($"Mapping error at key '{ex.Key}' (line {ex.Line}, col {ex.Column}): {ex.Message}");
}
catch (HumlDeserializeException ex)
{
    Console.WriteLine($"Deserialisation error: {ex.Message}");
}
```

## Notes

- `HumlDeserializeException` for missing required members lists **all** absent keys in a
  single throw: `"Missing required member(s) on type 'X': 'Key1', 'Key2'."` Keys are listed
  in property declaration order. See [Required Properties](required-properties.md) for details.
- `HumlSerializeException` is thrown by `HumlSerializer.Serialize` when
  `HumlOptions.ValidateDuplicateKeysOnWrite = true` and a dictionary produces two entries
  with the same ordinal key.
- `init`-only properties are now settable during deserialisation via reflection. The previous
  `HumlDeserializeException` for `init`-only setters has been removed as of 0.2.0-alpha.2.
  See [Constructor Binding](constructor-binding.md) for details.
- When `HumlOptions.UnmappedMemberHandling = Disallow`, `HumlDeserializeException` is thrown
  listing the first unrecognised key encountered. This check is suppressed when the target type
  declares a `[HumlExtensionData]` property — unknown keys are captured there instead.
  See [Options Reference](options-reference.md) for the full preset comparison table.

## See also

- [Options reference](options-reference.md) — `UnmappedMemberHandling`, `ValidateDuplicateKeysOnWrite`, and preset instances.
- [E07.ErrorHandling](https://github.com/primeBeri/huml-dotnet-examples/tree/main/src/examples/E07.ErrorHandling) — runnable example (parse errors, required members, `HumlOptions.Strict`).
