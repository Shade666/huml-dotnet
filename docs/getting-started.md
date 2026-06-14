# Getting started

This tutorial takes you from an empty project to reading and writing HUML in about five minutes.
By the end you will have deserialised a HUML document into a typed object, changed it, and written
it back out. No prior HUML knowledge is assumed.

> HUML (Human-oriented Markup Language) is a strict, readable configuration/data format — think of
> it as YAML without the footguns. See [huml.io](https://huml.io) for the language itself.

## 1. Create a project and add the package

```bash
dotnet new console -o HumlDemo
cd HumlDemo
dotnet add package Huml.Net
```

## 2. Read a HUML document into an object

Replace `Program.cs` with:

```csharp
using Huml.Net;

string document = """
    %HUML v0.2.0
    Host: "localhost"
    Port: 8080
    Debug: true
    """;

ServerConfig config = HumlSerializer.Deserialize<ServerConfig>(document);

Console.WriteLine($"{config.Host}:{config.Port} (debug={config.Debug})");

public class ServerConfig
{
    public string Host { get; set; } = "";
    public int Port { get; set; }
    public bool Debug { get; set; }
}
```

Run it:

```bash
dotnet run
# localhost:8080 (debug=True)
```

That is the whole happy path. The `%HUML v0.2.0` first line is the optional version directive;
each `key: value` line maps to a property by name.

## 3. Write an object back out

Add this before the `ServerConfig` class:

```csharp
var updated = new ServerConfig { Host = "prod.example.com", Port = 443, Debug = false };
Console.WriteLine(HumlSerializer.Serialize(updated));
```

```
%HUML v0.2.0
Host: "prod.example.com"
Port: 443
Debug: false
```

Properties are emitted in declaration order, and the serialiser always writes the version
directive first.

## 4. Overlay changes onto an existing object

Often you have defaults and want a HUML file to override only some of them. That is what
[`HumlSerializer.Populate`](populate.md) does:

```csharp
var withDefaults = new ServerConfig { Host = "localhost", Port = 80, Debug = false };

HumlSerializer.Populate("""
    %HUML v0.2.0
    Port: 8443
    """, withDefaults);

// withDefaults.Host is still "localhost"; withDefaults.Port is now 8443.
```

## 5. Handle invalid input

Parsing is strict — malformed input throws, it never silently guesses:

```csharp
using Huml.Net.Exceptions;

try
{
    HumlSerializer.Deserialize<ServerConfig>("Host: localhost"); // unquoted string — invalid
}
catch (HumlParseException ex)
{
    Console.WriteLine(ex.Message); // reports the line and column
}
```

## Where to go next

- Map .NET names like `HostName` to HUML keys like `host-name`: **[Customize property names](naming-policy.md)**.
- Use records and constructor parameters: **[Bind constructors & records](constructor-binding.md)**.
- Require certain keys to be present: **[Require properties](required-properties.md)**.
- See every option in one place: **[Options reference](options-reference.md)**.
- Browse the full surface: **<xref:Huml.Net.HumlSerializer>**.
- Run the code yourself: **[E01.GettingStarted](https://github.com/primeBeri/huml-dotnet-examples/tree/main/src/examples/E01.GettingStarted)** in the companion examples repo.
