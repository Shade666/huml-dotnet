using System.Runtime.CompilerServices;
using System.Text;
using Huml.Net.Exceptions;
using Huml.Net.Versioning;

namespace Huml.Net.Lexer;

/// <summary>Single-pass lexer that tokenises a HUML document into a pull-based token stream.</summary>
internal ref struct Lexer
{
    private ReadOnlySpan<char> _source;
    private readonly HumlOptions _options;
    private HumlSpecVersion _effectiveSpecVersion;
    private int _pos;
    private int _line = 1;
    private int _col;
    private int _lineIndent;

    /// <summary>
    /// Gets or sets the effective spec version used for version-gated tokenisation rules.
    /// Initialised from <see cref="HumlOptions.SpecVersion"/>; may be updated by the parser
    /// after reading the document <c>%HUML</c> version header.
    /// </summary>
    internal HumlSpecVersion EffectiveSpecVersion
    {
        get => _effectiveSpecVersion;
        set => _effectiveSpecVersion = value;
    }

    /// <summary>Initialises the lexer with a span source and parsing options.</summary>
    internal Lexer(ReadOnlySpan<char> source, HumlOptions options)
    {
        _source = source;
        _options = options;
        _effectiveSpecVersion = options.SpecVersion;
    }

    // -----------------------------------------------------------------------
    // Public API
    // -----------------------------------------------------------------------

    /// <summary>Returns the next token from the source. Returns <see cref="TokenType.Eof"/> at end of input.</summary>
    internal Token NextToken()
    {
        // Skip leading blank lines (lines with only whitespace content checked for trailing ws).
        // We process the source character by character.
        while (_pos < _source.Length)
        {
            // At start of line: measure indentation and check for tabs.
            if (_col == 0)
            {
                _lineIndent = MeasureIndent();
                // MeasureIndent may advance _pos to or past EOF (e.g., trailing blank lines).
                // Re-check bounds before accessing _source[_pos].
                if (_pos >= _source.Length)
                    continue;
            }

            char ch = PeekCurrentChar();

            // Handle newlines (blank lines have been advanced by MeasureIndent if needed)
            if (ch == '\n')
            {
                _pos++;
                _line++;
                _col = 0;
                continue;
            }

            // Handle each character
            if (ch == '%' && _col == 0 && _line == 1)
                return ScanVersionDirective();

            if (ch == '#')
                return ScanComment();

            if (ch == '"')
                return ScanDoubleQuoteToken();

            // List items are "- " (dash-space) per the spec tokenizer; a dash followed
            // by anything else (e.g. "-5", "-inf") is a signed numeric, even at line start.
            if (ch == '-' && _col == _lineIndent
                && (_pos + 1 >= _source.Length || _source[_pos + 1] is ' ' or '\n' or '\r'))
            {
                return ScanListItem();
            }

            if (ch == ':')
                return ScanIndicator();

            if (ch == ',')
            {
                // Space before comma is not allowed (space would have been consumed already,
                // leaving the previous character as a space at _pos - 1).
                if (_pos > 0 && _source[_pos - 1] == ' ')
                    ThrowParseError("No space is allowed before a comma.");

                var t = MakeStructuralToken(TokenType.Comma, false);
                _pos++;
                _col++;

                // Exactly one space must follow the comma.
                if (_pos >= _source.Length || _source[_pos] != ' ')
                    ThrowParseError("A single space must follow a comma.");
                // Ensure it is exactly one space (not two or more spaces before next token).
                if (_pos + 1 < _source.Length && _source[_pos + 1] == ' ')
                    ThrowParseError("Only one space is allowed after a comma.");

                return t;
            }

            if (ch == '[')
                return ScanEmptyCollection(TokenType.EmptyList, '[', ']');

            if (ch == '{')
                return ScanEmptyCollection(TokenType.EmptyDict, '{', '}');

            if (ch == ' ')
            {
                // Check if this space leads only to \n/\r\n or EOF (trailing whitespace)
                int p2 = _pos;
                while (p2 < _source.Length && _source[p2] == ' ')
                    p2++;
                if (p2 >= _source.Length || _source[p2] == '\n' || _source[p2] == '\r')
                {
                    // Trailing whitespace
                    throw new HumlParseException(
                        "Trailing whitespace is not allowed.", _line, _col);
                }
                // Space between tokens — skip it
                _pos++;
                _col++;
                continue;
            }

            if (IsLetter(ch))
                return ScanBareKeyOrKeyword();

            if (IsDigit(ch))
                return ScanNumber(_col > _lineIndent);

            if (ch == '+')
                return ScanSignedNumeric('+');

            if (ch == '-')
                return ScanSignedNumeric('-');

            if (ch == '`')
                return ScanBacktickMultiline();

            // Error path only — char.IsLetter is NOT used in acceptance logic
            if (char.IsLetter(ch))
            {
                ThrowParseError(
                    $"Bare keys must start with [a-zA-Z]. "
                    + $"Non-Latin characters are supported in quoted keys: \"{ch}\": \"value\"");
            }

            ThrowParseError($"Unexpected character '{ch}'.");
        }

        return MakeStructuralToken(TokenType.Eof, false);
    }

    // -----------------------------------------------------------------------
    // Scanning helpers
    // -----------------------------------------------------------------------

    private int MeasureIndent()
    {
        while (true)
        {
            int indent = 0;
            int p = _pos;
            while (p < _source.Length)
            {
                char c = _source[p];
                if (c == '\t')
                {
                    _pos = p;
                    _col = indent;
                    ThrowTabError();
                }
                if (c != ' ')
                    break;
                indent++;
                p++;
            }
            // If the line is blank (only spaces followed by \n/\r\n or EOF), check for trailing whitespace
            if (p < _source.Length && (_source[p] == '\n' || _source[p] == '\r'))
            {
                if (indent > 0)
                {
                    // _pos is at the newline; ThrowTrailingWhitespaceError uses 'indent' for column.
                    _pos = p;
                    _col = 0;
                    ThrowTrailingWhitespaceError(indent);
                }
                // blank line — advance past newline and loop (replaces tail recursion)
                _pos = p;
                AdvancePastNewline();
                continue;
            }
            else if (p >= _source.Length && indent > 0)
            {
                // trailing spaces at EOF
                _pos = p - indent;
                _col = 0;
                ThrowTrailingWhitespaceError(indent);
            }

            _pos = p;
            _col = indent;
            return indent;
        }
    }

    private Token ScanVersionDirective()
    {
        // Expect: %HUML vX.Y.Z
        int start = _pos;
        if (!_source.Slice(_pos).StartsWith("%HUML ".AsSpan(), StringComparison.Ordinal))
        {
            ThrowParseError("Invalid directive. Expected '%HUML vX.Y.Z'.");
        }
        _pos += 6; // skip "%HUML "
        _col += 6;

        int vstart = _pos;
        // Scan version string: [a-zA-Z0-9._-]+
        while (_pos < _source.Length && IsVersionChar(_source[_pos]))
        {
            _pos++;
            _col++;
        }
        if (_pos == vstart)
        {
            ThrowParseError("Missing version string after '%HUML '.");
        }

        string value = new string(_source.Slice(vstart, _pos - vstart));

        // Ensure nothing else on the line except optional newline
        CheckTrailingWhitespaceBeforeNewline();
        AdvancePastNewline();

        return new Token
        {
            Type = TokenType.Version,
            Value = value,
            Line = _line,
            Column = 0,
            Indent = 0,
            SpaceBefore = false,
        };
    }

    private Token ScanComment()
    {
        // Must be "# " (hash + space), else error — covers EOF and non-space in one check
        if (_pos + 1 >= _source.Length || _source[_pos + 1] != ' ')
        {
            ThrowParseError("Comments must start with '# ' (hash followed by a space).");
        }

        // Skip to end of line
        while (_pos < _source.Length && _source[_pos] != '\n' && _source[_pos] != '\r')
        {
            _pos++;
            _col++;
        }
        // Trailing spaces are not allowed on any line, including comment lines.
        if (_source[_pos - 1] == ' ')
        {
            throw new HumlParseException(
                "Trailing whitespace is not allowed.", _line, _col - 1);
        }
        // Skip newline (handles both \n and \r\n)
        AdvancePastNewline();

        // Recurse to get next real token
        return NextToken();
    }

    private Token ScanDoubleQuoteToken()
    {
        // Could be: quoted key (at key position = col == lineIndent),
        // or string value (anywhere else), or triple-quote multiline.
        // If we're at indent position and not right after a ':' indicator, it's a quoted key.
        // We'll check context: if the character at this col was preceded by nothing on this line
        // (col == lineIndent), it might be a QuotedKey. Actually we need to check if we're
        // in "key position" which is at the start of a key slot.
        // A simpler approach: scan the content, then look ahead for ': ' to decide QuotedKey vs String.

        // Check for triple-quote
        if (_pos + 2 < _source.Length && _source[_pos + 1] == '"' && _source[_pos + 2] == '"')
        {
            return ScanTripleQuoteMultiline();
        }

        // Single double-quote: scan quoted string
        int tokenCol = _col;
        bool spaceBefore = tokenCol > _lineIndent; // was there space before this?
        _pos++; // skip opening quote
        _col++;

        string value = ScanQuotedStringContent('"');

        // dict_key = simple_key | STRING: a quoted string immediately followed by ':' is a
        // key wherever it appears (block position, inline dicts, root inline dicts) — the
        // same lookahead go-huml uses. No space may precede ':', so the check is direct;
        // a quoted string with anything else after it is a String value.
        bool isKey = _pos < _source.Length && _source[_pos] == ':';

        return new Token
        {
            Type = isKey ? TokenType.QuotedKey : TokenType.String,
            Value = value,
            Line = _line,
            Column = tokenCol,
            Indent = _lineIndent,
            SpaceBefore = spaceBefore,
        };
    }

    private string ScanQuotedStringContent(char closeQuote)
    {
        int start = _pos;
        bool hasEscapes = false;

        // First pass: check if there are any escapes
        int p = _pos;
        while (p < _source.Length)
        {
            char c = _source[p];
            if (c == '\\')
            {
                hasEscapes = true;
                break;
            }
            if (c == closeQuote)
                break;
            p++;
        }

        if (!hasEscapes)
        {
            // Fast path: no escapes — find closing quote
            while (_pos < _source.Length && _source[_pos] != closeQuote && _source[_pos] != '\n' && _source[_pos] != '\r')
            {
                _pos++;
                _col++;
            }
            if (_pos >= _source.Length || _source[_pos] == '\n' || _source[_pos] == '\r')
            {
                ThrowParseError("Unterminated string literal.");
            }
            string val = new string(_source.Slice(start, _pos - start));
            _pos++; // skip closing quote
            _col++;
            return val;
        }
        else
        {
            // Slow path: process escapes with StringBuilder
            var sb = new StringBuilder();
            while (_pos < _source.Length && _source[_pos] != closeQuote && _source[_pos] != '\n' && _source[_pos] != '\r')
            {
                char c = _source[_pos];
                if (c == '\\')
                {
                    _pos++;
                    _col++;
                    if (_pos >= _source.Length)
                        ThrowParseError("Unexpected end of input in escape sequence.");
                    char esc = _source[_pos];
                    switch (esc)
                    {
                        case '"': sb.Append('"'); break;
                        case '\\': sb.Append('\\'); break;
                        case '/': sb.Append('/'); break;
                        case 'b': sb.Append('\b'); break;
                        case 'f': sb.Append('\f'); break;
                        case 'n': sb.Append('\n'); break;
                        case 'r': sb.Append('\r'); break;
                        case 't': sb.Append('\t'); break;
                        case 'v': sb.Append('\v'); break;
                        default:
                            ThrowParseError($"Unknown escape sequence '\\{esc}'.");
                            break;
                    }
                    _pos++;
                    _col++;
                }
                else
                {
                    sb.Append(c);
                    _pos++;
                    _col++;
                }
            }
            if (_pos >= _source.Length || _source[_pos] == '\n' || _source[_pos] == '\r')
            {
                ThrowParseError("Unterminated string literal.");
            }
            _pos++; // skip closing quote
            _col++;
            return sb.ToString();
        }
    }

    private Token ScanTripleQuoteMultiline()
    {
        int tokenCol = _col;
        int tokenLine = _line;
        int keyIndent = _lineIndent;

        _pos += 3; // skip opening """
        _col += 3;

        // Tokenizer: OPENING, (whitespace*, comment)?, '\n' — a trailing comment is the
        // only content permitted after the opening delimiter.
        ConsumeRestOfOpeningDelimiterLine("\"\"\"");

        AdvancePastNewline();

        // Collect content lines
        var sb = new StringBuilder();
        bool first = true;
        int stripIndent = keyIndent + 2;
        bool closed = false;

        // Version gate: in v0.1, `"""` has "strip spaces" semantics — ALL leading and
        // trailing whitespace on each content line is stripped. From v0.2, `"""` preserves
        // spaces beyond the 2-space strip indent (backticks were the v0.1 preserve form).
#pragma warning disable CS0618 // V0_1 obsolete
        bool preserveSpaces = _effectiveSpecVersion >= HumlSpecVersion.V0_2;
#pragma warning restore CS0618

        while (_pos < _source.Length)
        {
            // Check for closing """
            // Count leading spaces on this line
            int lineStart = _pos;
            int spaces = 0;
            while (_pos < _source.Length && _source[_pos] == ' ')
            {
                spaces++;
                _pos++;
            }
            _col = spaces;

            if (_pos < _source.Length && _source[_pos] == '"' && _pos + 2 < _source.Length
                && _source[_pos + 1] == '"' && _source[_pos + 2] == '"')
            {
                // This is the closing """ — verify indent matches keyIndent
                if (spaces != keyIndent)
                {
                    ThrowParseError($"Closing '\"\"\"' must be at indentation {keyIndent}, found {spaces}.");
                }
                _pos += 3;
                _col += 3;
                // Nothing else allowed on closing line
                CheckTrailingWhitespaceBeforeNewline();
                AdvancePastNewline();
                closed = true;
                break;
            }

            // Content line: strip the required indent
            // We already consumed some spaces; we need to go back to lineStart
            _pos = lineStart;
            _col = 0;

            // Strip stripIndent spaces
            for (int i = 0; i < stripIndent && _pos < _source.Length && _source[_pos] == ' '; i++)
            {
                _pos++;
                _col++;
            }

            // Read rest of line
            int contentStart = _pos;
            while (_pos < _source.Length && _source[_pos] != '\n' && _source[_pos] != '\r')
            {
                _pos++;
                _col++;
            }

            var contentSpan = _source.Slice(contentStart, _pos - contentStart);
            if (!preserveSpaces)
                contentSpan = contentSpan.Trim(' ');
            if (!first)
                sb.Append('\n');
            sb.Append(contentSpan);
            first = false;

            // Skip newline (handles both \n and \r\n)
            AdvancePastNewline();
        }

        if (!closed)
            ThrowParseError("Unclosed triple-quote multiline string: expected closing '\"\"\"'.");

        return new Token
        {
            Type = TokenType.String,
            Value = sb.ToString(),
            Line = tokenLine,
            Column = tokenCol,
            Indent = keyIndent,
            SpaceBefore = tokenCol > keyIndent,
        };
    }

    private Token ScanBacktickMultiline()
    {
        // Check version gate
#pragma warning disable CS0618 // V0_1 obsolete
        if (_effectiveSpecVersion >= HumlSpecVersion.V0_2)
#pragma warning restore CS0618
        {
            throw new HumlParseException(
                "Backtick multiline strings are not supported in HUML v0.2. Use triple-double-quotes (\"\"\") instead.",
                _line, _col);
        }

        // V0_1 path: scan backtick multiline
        // Opening: must be ``` (three backticks)
        if (_pos + 2 >= _source.Length || _source[_pos + 1] != '`' || _source[_pos + 2] != '`')
        {
            ThrowParseError("Expected ``` (three backticks) for multiline string.");
        }

        int tokenCol = _col;
        int tokenLine = _line;
        int keyIndent = _lineIndent;
        _pos += 3; // skip opening ```
        _col += 3;

        // Tokenizer: OPENING, (whitespace*, comment)?, '\n' — a trailing comment is the
        // only content permitted after the opening delimiter.
        ConsumeRestOfOpeningDelimiterLine("```");

        AdvancePastNewline();

        var sb = new StringBuilder();
        bool first = true;
        bool closed = false;

        while (_pos < _source.Length)
        {
            int lineStart = _pos;
            int spaces = 0;
            while (_pos < _source.Length && _source[_pos] == ' ')
            {
                spaces++;
                _pos++;
            }
            _col = spaces;

            if (_pos + 2 < _source.Length && _source[_pos] == '`' && _source[_pos + 1] == '`' && _source[_pos + 2] == '`')
            {
                // Closing ``` — verify indent matches keyIndent
                if (spaces != keyIndent)
                    ThrowParseError($"Closing '```' must be at indentation {keyIndent}, found {spaces}.");
                _pos += 3;
                _col += 3;
                CheckTrailingWhitespaceBeforeNewline();
                AdvancePastNewline();
                closed = true;
                break;
            }

            // Content line — strip structural indentation (keyIndent + 2 spaces, bounded by spaces).
            int stripCount = Math.Min(keyIndent + 2, spaces);
            _pos = lineStart + stripCount;
            _col = 0;
            int contentStart = _pos;
            while (_pos < _source.Length && _source[_pos] != '\n' && _source[_pos] != '\r')
            {
                _pos++;
                _col++;
            }

            string lineContent = new string(_source.Slice(contentStart, _pos - contentStart));
            if (!first)
                sb.Append('\n');
            sb.Append(lineContent);
            first = false;

            // Skip newline (handles both \n and \r\n)
            AdvancePastNewline();
        }

        if (!closed)
            ThrowParseError("Unclosed backtick multiline string: expected closing '```'.");

        return new Token
        {
            Type = TokenType.String,
            Value = sb.ToString(),
            Line = tokenLine,
            Column = tokenCol,
            Indent = _lineIndent,
            SpaceBefore = tokenCol > keyIndent,
        };
    }

    private Token ScanListItem()
    {
        // '-' followed by ' ' (or end of line for empty list items)
        int tokenCol = _col;
        _pos++; // skip '-'
        _col++;

        if (_pos < _source.Length && _source[_pos] == ' ')
        {
            _pos++; // skip space
            _col++;
        }

        return new Token
        {
            Type = TokenType.ListItem,
            Value = null,
            Line = _line,
            Column = tokenCol,
            Indent = _lineIndent,
            SpaceBefore = false,
        };
    }

    private Token ScanIndicator()
    {
        int tokenCol = _col;
        _pos++; // skip first ':'
        _col++;

        bool isVector = _pos < _source.Length && _source[_pos] == ':';
        if (isVector)
        {
            _pos++; // skip second ':'
            _col++;

            // Tokenizer: ":: " (exactly one space) introduces an inline value, while
            // "::" followed by (whitespace*, comment)? newline introduces a multiline
            // block — so any space count is fine before a comment or end-of-line, but
            // an inline value needs exactly one space, and zero spaces is never valid.
            if (_pos < _source.Length)
            {
                char next = _source[_pos];
                if (next == ' ')
                {
                    int p = _pos;
                    while (p < _source.Length && _source[p] == ' ')
                        p++;
                    bool atComment = p < _source.Length && _source[p] == '#';
                    bool atEol = p >= _source.Length || _source[p] == '\n' || _source[p] == '\r';
                    if (!atComment && !atEol && p - _pos > 1)
                        ThrowParseError("Only one space is allowed after '::'.");
                }
                else if (next != '\n' && next != '\r' && next != '#')
                {
                    ThrowParseError("A space must follow '::' before an inline value.");
                }
            }
        }
        else
        {
            // Scalar indicator ':' must be followed by exactly one space before the value,
            // OR by end-of-line / EOF (which the parser will reject as empty value anyway).
            // Reject the case where ':' is directly followed by a non-space, non-newline, non-EOF
            // character (e.g., key:"value" or key:#comment is caught here).
            if (_pos < _source.Length && _source[_pos] != ' ' && _source[_pos] != '\n')
                ThrowParseError("Expected a space after ':'.");

            // Also reject multiple spaces after ':' (e.g., key:  value).
            if (_pos < _source.Length && _source[_pos] == ' '
                && _pos + 1 < _source.Length && _source[_pos + 1] == ' ')
                ThrowParseError("Only one space is allowed after ':'.");
        }

        return new Token
        {
            Type = isVector ? TokenType.VectorIndicator : TokenType.ScalarIndicator,
            Value = null,
            Line = _line,
            Column = tokenCol,
            Indent = _lineIndent,
            SpaceBefore = false,
        };
    }

    /// <summary>
    /// After an opening multiline delimiter, consumes optional spaces and an optional
    /// trailing comment, erroring on any other content before the newline.
    /// Tokenizer rule: <c>OPENING, (whitespace*, comment)?, '\n'</c>.
    /// Leaves <c>_pos</c> at the newline (or EOF — the unclosed-string error surfaces later).
    /// </summary>
    private void ConsumeRestOfOpeningDelimiterLine(string delimiter)
    {
        int spaces = 0;
        while (_pos < _source.Length && _source[_pos] == ' ')
        {
            _pos++; _col++; spaces++;
        }

        if (_pos >= _source.Length || _source[_pos] == '\n' || _source[_pos] == '\r')
        {
            if (spaces > 0)
                ThrowParseError("Trailing whitespace is not allowed.");
            return;
        }

        if (_source[_pos] == '#')
        {
            if (_pos + 1 >= _source.Length || _source[_pos + 1] != ' ')
                ThrowParseError("Comments must start with '# ' (hash followed by a space).");
            while (_pos < _source.Length && _source[_pos] != '\n' && _source[_pos] != '\r')
            {
                _pos++; _col++;
            }
            if (_source[_pos - 1] == ' ')
                ThrowParseError("Trailing whitespace is not allowed.");
            return;
        }

        ThrowParseError($"Multiline delimiter '{delimiter}' must be followed by a newline, optionally after a trailing comment.");
    }

    private Token ScanEmptyCollection(TokenType type, char open, char close)
    {
        int tokenCol = _col;
        bool spaceBefore = tokenCol > _lineIndent;
        _pos++; // skip open char
        _col++;

        // Empty vectors are the literal signifiers "[]" and "{}" — the grammar admits
        // no internal whitespace, and go-huml matches them with peekString("[]").
        if (_pos >= _source.Length || _source[_pos] != close)
        {
            ThrowParseError($"Expected '{close}' immediately after '{open}' — empty vectors are the literals [] and {{}}.");
        }
        _pos++; // skip close char
        _col++;

        return new Token
        {
            Type = type,
            Value = null,
            Line = _line,
            Column = tokenCol,
            Indent = _lineIndent,
            SpaceBefore = spaceBefore,
        };
    }

    private Token ScanBareKeyOrKeyword()
    {
        int start = _pos;
        int tokenCol = _col;

        // Bare key: [a-zA-Z][a-zA-Z0-9_-]*
        while (_pos < _source.Length && IsKeyChar(_source[_pos]))
        {
            _pos++;
            _col++;
        }

        var span = _source.Slice(start, _pos - start);

        // If followed by ':' or ' ', it's a key. Otherwise check keywords.
        bool isKey = _pos < _source.Length && _source[_pos] == ':';

        if (isKey)
        {
            return new Token
            {
                Type = TokenType.Key,
                Value = new string(span),
                Line = _line,
                Column = tokenCol,
                Indent = _lineIndent,
                SpaceBefore = false,
            };
        }

        // Not a key — try keywords
        var kwType = TryMatchKeyword(span);
        if (kwType.HasValue)
        {
            bool spaceBefore = tokenCol > _lineIndent;
            return new Token
            {
                Type = kwType.Value,
                Value = new string(span),
                Line = _line,
                Column = tokenCol,
                Indent = _lineIndent,
                SpaceBefore = spaceBefore,
            };
        }

        // Not a keyword — unquoted string error
        throw new HumlParseException(
            "Unquoted strings are not allowed. Wrap the value in double quotes.",
            _line, tokenCol);
    }

    private Token ScanNumber(bool spaceBefore)
    {
        int start = _pos;
        int tokenCol = _col;

        // Consume digits and valid numeric chars
        ScanNumericChars();

        var span = _source.Slice(start, _pos - start);
        bool isFloat = IsFloatSpan(span);

        return new Token
        {
            Type = isFloat ? TokenType.Float : TokenType.Int,
            Value = new string(span),
            Line = _line,
            Column = tokenCol,
            Indent = _lineIndent,
            SpaceBefore = spaceBefore,
        };
    }

    private Token ScanSignedNumeric(char sign)
    {
        int tokenCol = _col;
        bool spaceBefore = tokenCol > _lineIndent;
        int start = _pos;
        _pos++; // consume sign
        _col++;

        if (_pos >= _source.Length)
        {
            ThrowParseError($"Unexpected end of input after '{sign}'.");
        }

        char next = _source[_pos];

        // Check for inf
        if (IsLetter(next))
        {
            // Scan keyword
            int kwStart = _pos;
            while (_pos < _source.Length && IsLetter(_source[_pos]))
            {
                _pos++;
                _col++;
            }
            // Keyword literals are lowercase and case-sensitive (spec + go-huml reference).
            var kwSpan = _source.Slice(start, _pos - start); // includes sign
            if (kwSpan.Equals("inf".AsSpan(), StringComparison.Ordinal) ||
                kwSpan.Equals("+inf".AsSpan(), StringComparison.Ordinal) ||
                kwSpan.Equals("-inf".AsSpan(), StringComparison.Ordinal))
            {
                return new Token
                {
                    Type = TokenType.Inf,
                    Value = new string(kwSpan),
                    Line = _line,
                    Column = tokenCol,
                    Indent = _lineIndent,
                    SpaceBefore = spaceBefore,
                };
            }
            throw new HumlParseException(
                $"Invalid value '{new string(kwSpan)}'.", _line, tokenCol);
        }

        if (!IsDigit(next))
        {
            ThrowParseError($"Expected digit after '{sign}', got '{next}'.");
        }

        // Scan number (already consumed sign)
        ScanNumericChars();

        var span = _source.Slice(start, _pos - start);
        bool isFloat = IsFloatSpan(span);

        return new Token
        {
            Type = isFloat ? TokenType.Float : TokenType.Int,
            Value = new string(span),
            Line = _line,
            Column = tokenCol,
            Indent = _lineIndent,
            SpaceBefore = spaceBefore,
        };
    }

    private void ScanNumericChars()
    {
        if (_pos >= _source.Length) return;

        char first = _source[_pos];

        // Hex, octal, binary prefix
        if (first == '0' && _pos + 1 < _source.Length)
        {
            char prefix = _source[_pos + 1];
            if (prefix == 'x' || prefix == 'X')
            {
                _pos += 2; _col += 2;
                int digits = 0;
                while (_pos < _source.Length && (IsHexChar(_source[_pos]) || _source[_pos] == '_'))
                {
                    if (_source[_pos] != '_') digits++;
                    _pos++; _col++;
                }
                if (digits == 0)
                    ThrowParseError("Hex literal requires at least one digit after '0x'.");
                return;
            }
            if (prefix == 'o' || prefix == 'O')
            {
                _pos += 2; _col += 2;
                int digits = 0;
                while (_pos < _source.Length && (IsOctalChar(_source[_pos]) || _source[_pos] == '_'))
                {
                    if (_source[_pos] != '_') digits++;
                    _pos++; _col++;
                }
                if (digits == 0)
                    ThrowParseError("Octal literal requires at least one digit after '0o'.");
                return;
            }
            if (prefix == 'b' || prefix == 'B')
            {
                _pos += 2; _col += 2;
                int digits = 0;
                while (_pos < _source.Length && (_source[_pos] is '0' or '1' or '_'))
                {
                    if (_source[_pos] != '_') digits++;
                    _pos++; _col++;
                }
                if (digits == 0)
                    ThrowParseError("Binary literal requires at least one digit after '0b'.");
                return;
            }
        }

        // Decimal integer or float
        while (_pos < _source.Length && (IsDigit(_source[_pos]) || _source[_pos] == '_'))
        {
            _pos++; _col++;
        }

        // Check for decimal point (float)
        if (_pos < _source.Length && _source[_pos] == '.')
        {
            _pos++; _col++;
            while (_pos < _source.Length && (IsDigit(_source[_pos]) || _source[_pos] == '_'))
            {
                _pos++; _col++;
            }
        }

        // Scientific notation
        if (_pos < _source.Length && (_source[_pos] == 'e' || _source[_pos] == 'E'))
        {
            _pos++; _col++;
            if (_pos < _source.Length && (_source[_pos] == '+' || _source[_pos] == '-'))
            {
                _pos++; _col++;
            }
            while (_pos < _source.Length && (IsDigit(_source[_pos]) || _source[_pos] == '_'))
            {
                _pos++; _col++;
            }
        }
    }

    // -----------------------------------------------------------------------
    // CRLF normalisation helper
    // -----------------------------------------------------------------------

    /// <summary>
    /// Returns the character at the current read position, normalising CR/CRLF to LF.
    /// Must only be called when <c>_pos &lt; _source.Length</c> is already known.
    /// When <c>\r\n</c> is encountered, advances <c>_pos</c> past the <c>\r</c> so
    /// the caller sees <c>\n</c>; does NOT advance past the <c>\n</c> itself.
    /// For standalone <c>\r</c>, returns <c>\n</c> without additional advance.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private char PeekCurrentChar()
    {
        char c = _source[_pos];
        if (c != '\r') return c;
        // \r\n counts as one newline: skip the \r so the next read sees \n
        if (_pos + 1 < _source.Length && _source[_pos + 1] == '\n')
            _pos++;
        return '\n';
    }

    // -----------------------------------------------------------------------
    // Utility helpers
    // -----------------------------------------------------------------------

    private static TokenType? TryMatchKeyword(ReadOnlySpan<char> slice)
    {
        // Keyword literals are lowercase and case-sensitive (spec + go-huml reference);
        // "TRUE" is an unquoted string, i.e. a parse error, not a bool.
        if (slice.Equals("true".AsSpan(), StringComparison.Ordinal))  return TokenType.Bool;
        if (slice.Equals("false".AsSpan(), StringComparison.Ordinal)) return TokenType.Bool;
        if (slice.Equals("null".AsSpan(), StringComparison.Ordinal))  return TokenType.Null;
        if (slice.Equals("nan".AsSpan(), StringComparison.Ordinal))   return TokenType.NaN;
        if (slice.Equals("inf".AsSpan(), StringComparison.Ordinal))   return TokenType.Inf;
        return null;
    }

    private static bool IsFloatSpan(ReadOnlySpan<char> span)
    {
        // Hex, octal, and binary literals are always integers regardless of their digits.
        // A span like "0xCAFEBABE" contains 'E' but is not a float.
        // Skip over an optional sign prefix when checking for a base prefix.
        int start = 0;
        if (span.Length > 0 && (span[0] == '+' || span[0] == '-'))
            start = 1;

        if (span.Length - start >= 2 && span[start] == '0')
        {
            char prefix = span[start + 1];
            if (prefix == 'x' || prefix == 'X' ||
                prefix == 'o' || prefix == 'O' ||
                prefix == 'b' || prefix == 'B')
                return false; // hex / octal / binary — always integer
        }

        foreach (char c in span)
        {
            if (c == '.' || c == 'e' || c == 'E')
                return true;
        }
        return false;
    }

    private void CheckTrailingWhitespaceBeforeNewline()
    {
        int p = _pos;
        int spaces = 0;
        while (p < _source.Length && _source[p] == ' ')
        {
            spaces++;
            p++;
        }
        if (spaces > 0 && (p >= _source.Length || _source[p] == '\n' || _source[p] == '\r'))
        {
            _col += spaces;
            ThrowTrailingWhitespaceError(_col - spaces);
        }
    }

    private void AdvancePastNewline()
    {
        if (_pos >= _source.Length) return;
        // Handle \r\n (CRLF) and standalone \n
        if (_source[_pos] == '\r')
        {
            _pos++; // skip \r
            if (_pos < _source.Length && _source[_pos] == '\n')
                _pos++; // skip \n of \r\n pair
            _line++;
            _col = 0;
        }
        else if (_source[_pos] == '\n')
        {
            _pos++;
            _line++;
            _col = 0;
        }
    }

    private Token MakeStructuralToken(TokenType type, bool spaceBefore) => new Token
    {
        Type = type,
        Value = null,
        Line = _line,
        Column = _col,
        Indent = _lineIndent,
        SpaceBefore = spaceBefore,
    };

    // -----------------------------------------------------------------------
    // Character classification
    // -----------------------------------------------------------------------

    /// <summary>
    /// ASCII-only letter check per the HUML spec — bare keys use <c>[a-zA-Z][a-zA-Z0-9_-]*</c>.
    /// Non-Latin characters are supported via quoted keys.
    /// </summary>
    private static bool IsLetter(char c) => (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z');
    private static bool IsDigit(char c) => c >= '0' && c <= '9';
    private static bool IsKeyChar(char c) => IsLetter(c) || IsDigit(c) || c == '_' || c == '-';
    private static bool IsVersionChar(char c) => IsLetter(c) || IsDigit(c) || c == '.' || c == '_' || c == '-';
    private static bool IsHexChar(char c) => IsDigit(c) || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F');
    private static bool IsOctalChar(char c) => c >= '0' && c <= '7';

    // -----------------------------------------------------------------------
    // Error helpers
    // -----------------------------------------------------------------------

    private void ThrowParseError(string message) =>
        throw new HumlParseException(message, _line, _col);

    private void ThrowTabError() =>
        throw new HumlParseException(
            "Tab characters are not allowed; use spaces for indentation.", _line, _col);

    private void ThrowTrailingWhitespaceError(int col) =>
        throw new HumlParseException(
            "Trailing whitespace is not allowed.", _line, col);
}
