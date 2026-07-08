# Explanation

Background and design rationale — how Huml.Net works and why it is built the way it is. Nothing
here is required to *use* the library; read these when you want to understand it or contribute.

- [Versioning model](versioning.md) — how package versions track HUML spec versions, and the
  support window policy.
- [The pipeline](internals/pipeline.md) — end-to-end data flow: Lexer → Parser → AST →
  serialiser/deserialiser.
- [Version gates](internals/version-gates.md) — how spec-version branching works and how a new
  spec version is added.
- [Working with the AST](ast-usage.md) — the `HumlDocument` node hierarchy and pattern-matching
  over it.
- [Extending the pipeline](internals/extending.md) — checklists for adding AST nodes, token
  types, and supported .NET types.
- [Performance benchmarks](benchmarks.md) — measured figures against System.Text.Json, with
  honest commentary.
