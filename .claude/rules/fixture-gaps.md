# Fixture Gap Analysis Rule

This rule defines a repeatable process for identifying language/implementation-agnostic gaps in
the official `huml-lang/tests` fixture suite and adding them to `fixtures/extensions/` so they
run locally and can be contributed upstream.

Invoke this rule whenever:
- New tests are written in any .NET test file
- A new spec version is released
- The upstream `fixtures/v0.1` or `fixtures/v0.2` submodule pointer is updated

---

## 1. What counts as a fixture gap

A gap is a parse behaviour that meets ALL of the following criteria:

- **Language/implementation-agnostic** — tests only "does this input parse successfully or
  throw a parse error?" Any HUML implementation can answer the question with the same result.
- **Not already covered** by the upstream assertion files, matched by input string (not just
  name). Two inputs are considered equivalent if they test the same _concept_ with different
  incidental values (e.g. key name or exact numeric literal).
- **Deterministic** — the same input produces the same result in every compliant HUML parser,
  regardless of implementation language or runtime.
- **Does not require knowledge of parsed values** — if the test requires checking what was
  parsed (token types, AST nodes, property values, exception message text), it is
  .NET-specific and does not belong in shared fixtures.

**Not gaps** — exclude these even if they are not upstream:

- Tests that verify specific token types, AST node types, or parsed property values
- Tests that verify exception message text or error position (line/column)
- Tests that verify .NET serialization/deserialization behaviour
- Tests that verify allocation counts or performance characteristics
- Tests that verify `HumlOptions` defaults, .NET enum values, or version-option interactions
- Error cases that depend on `HumlOptions` configuration (e.g. `UnknownVersionBehaviour`) —
  these require implementation-specific setup and cannot be expressed as `{name, input, error}`

---

## 2. How to audit for gaps

**Step 1** — Read all upstream fixture assertion files:

- `fixtures/v0.1/assertions/*.json`
- `fixtures/v0.2/assertions/*.json`

Build a set of covered input strings. For comparison, strip trailing whitespace and newlines
from inputs (the concept matters, not incidental formatting).

**Step 2** — Read all .NET test files:

- `tests/Huml.Net.Tests/Lexer/LexerTests.cs`
- `tests/Huml.Net.Tests/Parser/HumlParserTests.cs`
- `tests/Huml.Net.Tests/HumlStaticApiTests.cs`
- Any new test files added since the last audit (check git log for additions)

**Step 3** — For each test, classify:

- Does it only assert "error or no error" (via `Should().Throw<HumlParseException>()` or
  `Should().NotThrow()`)? → **Candidate**
- Does it assert .NET-specific behaviour (token types, AST nodes, exception messages,
  allocation counts, property values, options configuration)? → **Not a candidate**

**Step 4** — Cross-reference candidates against the upstream covered set:

- Match by normalised input string (strip trailing `\n` for comparison if the concept is the same)
- If an input is semantically equivalent to one already covered (same concept, different
  incidental value like key name or numeric literal), mark as **covered**
- Only flag as a gap if the **behaviour being tested** is genuinely absent from the upstream suite

---

## 3. Output format

Gaps go into `fixtures/extensions/` mirroring the upstream directory structure:

```
fixtures/
  extensions/
    v0.1/
      assertions/
        <category>.json    <- one file per logical group
    v0.2/
      assertions/
        <category>.json
      documents/
        <name>.huml        <- document fixtures (optional, for round-trip verification)
        <name>.json        <- expected JSON parse output
```

Each assertion file is a valid JSON array (no comments):

```json
[
  {"name": "descriptive_snake_case_name", "input": "...", "error": true},
  {"name": "another_case", "input": "...", "error": false}
]
```

Naming conventions:

- File names: logical groupings, not one file per test (e.g. `unicode.json`, `gaps.json`,
  `keywords.json`)
- Test names: `snake_case`, descriptive, no abbreviations
- Do NOT duplicate names already in the upstream files (duplication causes confusion in output)
- Special characters in input strings: use JSON `\uXXXX` escapes for bidi control characters
  and other non-printable codepoints — keeps files ASCII-safe and avoids invisible-character
  corruption in editors and diffs
- Version scope: if a gap applies to both v0.1 and v0.2 (version-agnostic), duplicate the
  file in both `fixtures/extensions/v0.1/assertions/` and
  `fixtures/extensions/v0.2/assertions/` with identical content. This mirrors the upstream
  convention of fully independent version directories.

---

## 4. SharedSuiteTests integration

After adding extension fixture files, verify the integration is wired correctly:

1. **Check `SharedSuiteTests.LoadFixtures`** scans `fixtures/extensions/{version}/assertions/`
   with a `Directory.Exists` guard:

   ```csharp
   var extDir = Path.Combine(AppContext.BaseDirectory, "fixtures", "extensions", version, "assertions");
   if (Directory.Exists(extDir))
   {
       foreach (var file in Directory.GetFiles(extDir, "*.json"))
       { ... }
   }
   ```

2. **Check `Huml.Net.Tests.csproj`** contains a `<Content>` include for the extensions directory:

   ```xml
   <Content Include="..\..\fixtures\extensions\**\*"
            Link="fixtures\extensions\%(RecursiveDir)%(Filename)%(Extension)"
            CopyToOutputDirectory="PreserveNewest" />
   ```

3. **Verify test count increases** by running the Theory filter and confirming the count matches
   the number of new fixture rows added:

   ```bash
   dotnet test --filter "DisplayName~V01_fixture_passes" -v normal
   dotnet test --filter "DisplayName~V02_fixture_passes" -v normal
   ```

   `V01_fixture_passes` count increases by the number of rows in `fixtures/extensions/v0.1/assertions/*.json`.
   `V02_fixture_passes` count increases by the rows in `fixtures/extensions/v0.2/assertions/*.json`.

---

## 5. Contribution workflow

Extension fixtures in `fixtures/extensions/` are a staging area for upstream contribution.
Once a group of fixtures is stable and tests pass locally:

1. Fork the `huml-lang/tests` repository
2. Add the assertion file(s) to the appropriate `assertions/` directory and any document pairs
   to `documents/`
3. Open a PR to `huml-lang/tests` — these are parser-agnostic, so all HUML implementations
   benefit
4. Once merged, update the corresponding submodule pointer in `huml-dotnet`:
   ```bash
   cd fixtures/v0.2   # or fixtures/v0.1
   git fetch && git checkout <new-tag>
   cd ../..
   git add fixtures/v0.2
   git commit -m "chore: update fixtures/v0.2 submodule to <new-tag>"
   ```
5. Remove the corresponding extension file — the cases are now covered by the upstream
   submodule and running them twice would produce duplicate Theory rows
6. Run `dotnet test` to confirm no regressions
