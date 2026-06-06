# HUML — Notepad++ User Defined Language

Syntax highlighting for [HUML v0.2](https://github.com/Shade666/huml-dotnet) in Notepad++.

Two variants are provided — install **one** per Notepad++ instance (both share the language
name `HUML`, so installing both causes a conflict):

| File | Theme target | Background |
| --- | --- | --- |
| `HUML.Huml-Lang.xml` | Light (default Notepad++ white theme) | Transparent |
| `HUML.Huml-Lang_Dark.xml` | Dark (Notepad++ dark mode, Dracula, Monokai, etc.) | Transparent |

Both files use `colorStyle="1"` on every active style, meaning **only foreground colours are
specified by the UDL** — backgrounds are always inherited from your active theme.

## Installation

### Option A — Import via Notepad++ UI (simplest)

1. Open Notepad++.
2. Go to **Language → User Defined Language → Define your language…**
3. Click **Import** and select the `.xml` file that matches your theme.
4. Restart Notepad++.
5. `.huml` files will be detected automatically; or select **Language → HUML** manually.

### Option B — Drop into the UDL folder

1. Close Notepad++.
2. Copy the chosen `.xml` file to your Notepad++ UDL directory:
   - Default install: `%APPDATA%\Notepad++\userDefineLangs\`
3. Restart Notepad++.

## Colour palette

### Light theme (`HUML.Huml-Lang.xml`)

| Construct | Example | Colour |
| --- | --- | --- |
| Version directive | `%HUML v0.2` | `%` bright purple · `HUML` muted violet |
| Comments | `# remark` | Muted green `#6A9955`, italic |
| Strings | `"hello"` | Dark red `#A31515` |
| Multiline strings | `"""…"""` | Dark red `#A31515` |
| Booleans / null | `true false null` | Bold blue `#0000FF` |
| Special floats | `nan inf` | Bold blue `#0000FF` |
| Numbers | `42 0xFF 0o77 0b101 1_000` | Dark teal `#09885A` |
| Operators | `: :: , - [] {}` | Bright purple `#AF00DB` |

### Dark theme (`HUML.Huml-Lang_Dark.xml`) — VS Code Dark+ inspired

| Construct | Example | Colour |
| --- | --- | --- |
| Version directive | `%HUML v0.2` | `%` light violet · `HUML` warm yellow |
| Comments | `# remark` | Muted green `#6A9955`, italic |
| Strings | `"hello"` | Warm orange-red `#CE9178` |
| Multiline strings | `"""…"""` | Warm orange-red `#CE9178` |
| Booleans / null | `true false null` | Bold cornflower blue `#569CD6` |
| Special floats | `nan inf` | Bold cornflower blue `#569CD6` |
| Numbers | `42 0xFF 0o77 0b101 1_000` | Soft sage green `#B5CEA8` |
| Operators | `: :: , - [] {}` | Light violet `#C586C0` |

## Known limitations

These are intrinsic to Notepad++ UDL — not bugs:

- **Keys vs values are not differentiated** — bare keys render as default text; quoted keys
  render as strings (same colour as string values). UDL has no positional awareness.
- **Strict whitespace is not enforced** — HUML rejects `#comment` (no space after `#`),
  two spaces after `:`, trailing whitespace, and tab indentation. The UDL highlights these
  as if they were valid.
- **No code folding** — HUML structure is indent-based; UDL folding requires explicit
  open/close markers.
- **`+inf` / `-inf`** — render as operator `+`/`-` followed by keyword `inf`, not as a
  single token.

## Contributing upstream

These files are staged for contribution to
[notepad-plus-plus/userDefinedLanguages](https://github.com/notepad-plus-plus/userDefinedLanguages).

If you are submitting the PR:

1. Fork `notepad-plus-plus/userDefinedLanguages`.
2. Copy `HUML.Huml-Lang.xml` → `UDLs/HUML.Huml-Lang.xml`.
3. Copy `HUML.Huml-Lang_Dark.xml` → `UDLs/HUML.Huml-Lang_Dark.xml`.
4. Copy `HUML.Huml-Lang.huml` → `UDL-samples/HUML.Huml-Lang.huml`.
5. Edit `udl-list.json` — insert the two entries below immediately after the `Htaccess` block
   (between `Htaccess_bySilasBrill` and `iCalendar_by-jfreundo`).
6. Do **not** edit `udl-list.md` — it is auto-generated from the JSON.
7. Open a pull request targeting the `master` branch.

`udl-list.json` entries to insert:

```json
{
  "id-name":      "HUML.Huml-Lang",
  "display-name": "HUML",
  "author":       "Shade666",
  "version":      "0.2",
  "repository":   "",
  "description":  "HUML (Human-oriented Markup Language) — config format with YAML-like blocks and JSON-like inline collections. Light theme. Targets HUML spec v0.2.",
  "sample":       "HUML.Huml-Lang.huml"
},
{
  "id-name":      "HUML.Huml-Lang_Dark",
  "display-name": "HUML Dark Mode",
  "author":       "Shade666",
  "version":      "0.2",
  "repository":   "",
  "description":  "HUML (Human-oriented Markup Language) — dark theme variant, VS Code Dark+ inspired palette. Targets HUML spec v0.2.",
  "sample":       "HUML.Huml-Lang.huml"
}
```
