# Fixture merge instructions

These instructions are for the agent (or contributor) applying this PR to the
`huml-lang/tests` repository.

## What is being added

Two categories of new assertions, derived from the Huml.Net (.NET reference
implementation) test suite. All cases are parse-only (`{name, input, error}`)
and are implementation-agnostic.

| Category | Version | Items | Document pair |
|---|---|---|---|
| Gaps (miscellaneous parse behaviours not yet covered) | v0.1 + v0.2 | 12 | No |
| Unicode (non-ASCII keys and values) | v0.2 only | 18 | Yes |

---

## Changes required

### 1. `v0.2/assertions/mixed.json`

Append the following items to the **end** of the existing JSON array.

**From gaps (12 items):**

```json
{"name": "bool_true_uppercase", "input": "key: TRUE", "error": false},
{"name": "bool_false_uppercase", "input": "key: FALSE", "error": false},
{"name": "null_uppercase", "input": "key: NULL", "error": false},
{"name": "tab_indentation_at_line_start", "input": "\tkey: \"value\"", "error": true},
{"name": "quoted_key_with_integer_value", "input": "\"my-key\": 42", "error": false},
{"name": "root_float_scalar", "input": "3.14", "error": false},
{"name": "root_nan_scalar", "input": "nan", "error": false},
{"name": "root_inf_scalar", "input": "inf", "error": false},
{"name": "root_hex_scalar", "input": "0xFF", "error": false},
{"name": "multiline_list_integer_items", "input": "list::\n  - 1\n  - 2\n  - 3", "error": false},
{"name": "ambiguous_empty_vector_bare", "input": "key::", "error": true},
{"name": "quoted_key_containing_colon", "input": "\"a:b\": \"v\"", "error": false}
```

**From unicode (18 items):**

```json
{"name": "bare_arabic_key", "input": "اسم: \"value\"", "error": true},
{"name": "bare_chinese_key", "input": "名: \"value\"", "error": true},
{"name": "bare_cyrillic_key", "input": "Д: \"value\"", "error": true},
{"name": "bare_devanagari_key", "input": "नाम: \"value\"", "error": true},
{"name": "bare_emoji_key", "input": "🚀: \"value\"", "error": true},
{"name": "quoted_arabic_key", "input": "\"اسم\": \"أحمد\"", "error": false},
{"name": "quoted_chinese_key", "input": "\"名前\": \"太郎\"", "error": false},
{"name": "quoted_cyrillic_key", "input": "\"Имя\": \"Иван\"", "error": false},
{"name": "quoted_emoji_key", "input": "\"🚀\": \"launch\"", "error": false},
{"name": "arabic_string_value", "input": "key: \"مرحبا\"", "error": false},
{"name": "hebrew_string_value", "input": "key: \"שלום\"", "error": false},
{"name": "chinese_string_value", "input": "key: \"你好世界\"", "error": false},
{"name": "korean_string_value", "input": "key: \"안녕하세요\"", "error": false},
{"name": "emoji_string_value", "input": "key: \"🚀🌍\"", "error": false},
{"name": "mixed_ltr_rtl_string", "input": "key: \"Hello مرحبا World\"", "error": false},
{"name": "rtl_mark_in_string", "input": "key: \"text‏more\"", "error": false},
{"name": "ltr_mark_in_string", "input": "key: \"text‎more\"", "error": false}
```

### 2. `v0.1/assertions/mixed.json`

Append the same 12 gap items (only) to the end of the existing JSON array.
Unicode assertions are v0.2-only and must **not** be added to v0.1.

### 3. `v0.2/documents/` — two new files

**`unicode.huml`:**

```
"名前": "太郎"
greeting: "مرحبا بالعالم"
emoji: "🚀🌍"
mixed: "Hello مرحبا World"
```

**`unicode.json`:**

```json
{
  "名前": "太郎",
  "greeting": "مرحبا بالعالم",
  "emoji": "🚀🌍",
  "mixed": "Hello مرحبا World"
}
```

---

## Conflict check

All 30 names were cross-referenced against the existing `mixed.json` in both
`v0.1/` and `v0.2/`. No name collisions were found. The closest existing entry
is `ambiguous_empty_vector_space` (`key:: # comment`) — the new entry
`ambiguous_empty_vector_bare` (`key::`) is a distinct concept.

---

## Verification

After applying, confirm the array lengths:

- `v0.2/assertions/mixed.json`: was 175 items, should be 205 (+ 30)
- `v0.1/assertions/mixed.json`: was 175 items, should be 187 (+ 12)
- `v0.2/documents/`: two new files present (`unicode.huml`, `unicode.json`)
