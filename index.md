# Huml.Net

A full-featured **HUML v0.1/v0.2** parser, serialiser, and deserialiser for .NET — with a
`System.Text.Json`-style API and **zero runtime dependencies**.

```bash
dotnet add package Huml.Net
```

```csharp
using Huml.Net;

var config = HumlSerializer.Deserialize<ServerConfig>("""
    %HUML v0.2.0
    Host: "localhost"
    Port: 8080
    """);
```

## Where to next

- **New here?** Start with the [Getting Started tutorial](docs/getting-started.md).
- **Need to do a specific thing?** Browse the [how-to guides](docs/toc.yml).
- **Looking up an API?** See the API reference: <xref:Huml.Net.HumlSerializer>.
- **Want to understand the design?** Read the [explanation pages](docs/versioning.md).

## Why Huml.Net

- **Familiar API** — if you know `JsonSerializer`, you know `Huml`.
- **Spec-compliant** — validated against the shared `huml-lang/tests` fixture suite for HUML v0.1 and v0.2.
- **No dependencies** — nothing pulled into your app's dependency graph.
- **Multi-target** — `netstandard2.1`, `.NET 8`, `.NET 9`, `.NET 10`.
- **AOT/trim-aware** — annotated for trimming, with a source-generator seam for reflection-free metadata.
