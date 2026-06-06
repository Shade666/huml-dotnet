# PR summary — Add gap and unicode parse fixtures

## Summary

Adds 30 new parse assertions and one document pair to the shared fixture suite,
derived from the Huml.Net (.NET reference implementation) test suite. All cases
are implementation-agnostic: they only assert whether a given input parses
successfully or throws a parse error, with no dependency on token types, AST
structure, error messages, or runtime behaviour.

## Changes

- **`v0.2/assertions/mixed.json`** — 30 new entries (12 gaps + 18 unicode)
- **`v0.1/assertions/mixed.json`** — 12 new entries (gaps only; unicode is v0.2-specific)
- **`v0.2/documents/unicode.huml`** — new document fixture
- **`v0.2/documents/unicode.json`** — expected parse output for `unicode.huml`

## Categories

### Gaps (v0.1 + v0.2)

Parse behaviours that were exercised by Huml.Net's test suite but absent from
the shared suite. Covers:

- Case-sensitivity of keywords: `TRUE`, `FALSE`, `NULL` are accepted (same as
  lowercase)
- Tab character at line start is an error
- Quoted key with a non-string value (`"my-key": 42`)
- Root-level scalar types not previously covered: float (`3.14`), `nan`, `inf`,
  hex (`0xFF`)
- Multiline list with integer items
- Bare `key::` with no following content is an error
- Quoted key containing a colon (`"a:b"`)

### Unicode (v0.2 only)

Covers the boundary between bare keys (ASCII-only identifiers) and quoted keys,
and validates that non-ASCII content is accepted inside quoted strings and
quoted keys:

- Bare non-ASCII keys (Arabic, Chinese, Cyrillic, Devanagari, emoji) are errors
- Quoted non-ASCII keys (same scripts) are accepted
- Non-ASCII string values (Arabic, Hebrew, Chinese, Korean, emoji) are accepted
- Mixed LTR/RTL strings are accepted
- RTL mark (`U+200F`) and LTR mark (`U+200E`) inside strings are accepted

## Notes

- No name collisions with existing entries in either version's `mixed.json`
- The closest existing entry is `ambiguous_empty_vector_space` (`key:: # comment`);
  the new `ambiguous_empty_vector_bare` (`key::`) tests a distinct code path
- All non-ASCII and invisible codepoints in the JSON source use `\uXXXX` escapes
  to keep the files ASCII-safe
