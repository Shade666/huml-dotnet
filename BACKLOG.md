# Huml.Net Backlog

## Purpose

This file provides public visibility into planned work for Huml.Net. It is the canonical list of
accepted items that are tracked for implementation across the project.

## How It Works

- Users report bugs and request features via GitHub Issues.
- The maintainer triages issues and promotes accepted items to this backlog.
- Internal planning workflows are not publicly exposed — this file provides transparency into
  what is planned, in progress, and done.
- Items move through statuses: Planned -> In Progress -> Done.

## Backlog

| Category      | Item                                                                                                                            | Version | Priority | Status  |
| ------------- | ------------------------------------------------------------------------------------------------------------------------------- | ------- | -------- | ------- |
| API           | Implement type-directed dispatch in `Serialize(object?, Type)` — currently ignores the `Type` parameter (section 2.3, task 1.1) | V1      | High     | Planned |
| Documentation | Add `<remarks>` to `Huml.Deserialize<T>(ReadOnlySpan<char>)` documenting span-to-string allocation (section 2.3, task 1.2)      | V1      | Medium   | Planned |
| Documentation | Add XML doc to `HumlDocument` clarifying dual role as document root and nested mapping block (section 2.3, task 1.3)            | V1      | Low      | Planned |
| Performance   | Add property-lookup dictionary to `PropertyDescriptor` cache for O(1) deserialiser key lookup (section 5.1, task 2.1)           | V1      | Low      | Planned |
| Performance   | Cache indent strings in `HumlSerializer.Indent()` to eliminate per-call allocation (section 5.1, task 2.2)                      | V1      | Low      | Planned |
| Performance   | Pool `StringBuilder` in serialiser via `[ThreadStatic]` to reduce GC pressure (section 5.1, task 2.3)                           | V2      | Medium   | Planned |
| Performance   | Refactor Lexer to `ref struct` accepting `ReadOnlySpan<char>` for genuine zero-copy deserialisation (section 9, phase 3)        | V2      | High     | Planned |
| Diagnostics   | Carry source position (Line, Column) through AST nodes for richer `HumlDeserializeException` context (section 9, task 4.1)      | V2      | Medium   | Planned |
| API           | Add `HumlOptions` factory method for "header-detected, latest fallback" variant (section 9, task 4.2)                           | V2      | Low      | Planned |
| Testing       | Add concurrency test for `PropertyDescriptor` cache under parallel deserialisation (section 9, task 4.3)                        | V2      | Low      | Planned |
| Documentation | Add CHANGELOG.md with version history from git tags (section 8.2)                                                               | V1      | Low      | Planned |
| Security      | Document uncapped document size limitation; consider optional `MaxDocumentSize` option (section 6.1)                            | V2      | Low      | Planned |
