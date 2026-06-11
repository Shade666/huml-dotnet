using System.Globalization;
using Huml.Net.Exceptions;
using Huml.Net.Lexer;
using Huml.Net.Versioning;

namespace Huml.Net.Parser;

/// <summary>
/// Recursive-descent parser that consumes a pull-based <see cref="Lexer"/> token stream
/// and produces a <see cref="HumlDocument"/> AST. Covers the full HUML v0.2 grammar:
/// scalars, vector blocks, inline lists/dicts, empty collections, and indent-driven nesting.
/// </summary>
internal ref struct HumlParser
{
    // ── Private enum for root-type inference ──────────────────────────────────

    /// <summary>Classifies the root-level structure inferred from the first token(s).</summary>
    private enum RootType
    {
        Scalar,
        EmptyList,
        EmptyDict,
        InlineList,
        InlineDict,
        MultilineList,
        MultilineDict,
    }

    // ── Fields ────────────────────────────────────────────────────────────────

    private Lexer.Lexer _lexer;

    /// <summary>
    /// Stored options — propagated to the Lexer at construction. Retained for future
    /// parser-level version gates using the <c>&gt;=</c> convention (PARS-04).
    /// </summary>
    private HumlOptions _options;

    private int _maxDepth;
    private int _depth;
    private Token _lookahead;
    private HumlSpecVersion _effectiveSpecVersion;

    // ── Constructor ───────────────────────────────────────────────────────────

    /// <summary>
    /// Initialises the parser with a HUML source span and parsing options.
    /// The <paramref name="options"/> are forwarded to the <see cref="Lexer.Lexer"/> so
    /// version-gated tokenisation rules (e.g., backtick multiline strings) fire correctly.
    /// </summary>
    /// <param name="source">The HUML document text to parse.</param>
    /// <param name="options">Options controlling spec-version behaviour.</param>
    internal HumlParser(ReadOnlySpan<char> source, HumlOptions options)
    {
        _lexer = new Lexer.Lexer(source, options);
        _options = options;
        _maxDepth = options.MaxRecursionDepth;
        _effectiveSpecVersion = options.SpecVersion;
        _lookahead = _lexer.NextToken(); // prime lookahead
    }

    // ── Public entry point ────────────────────────────────────────────────────

    /// <summary>
    /// Parses the source document and returns the root <see cref="HumlDocument"/>.
    /// Throws <see cref="HumlParseException"/> on any syntax error.
    /// </summary>
    internal HumlDocument Parse()
    {
        // Version gate placeholder — currently all v0.1/v0.2 grammar differences
        // are handled at the lexer level. Future spec versions may add parser gates:
        // #pragma warning disable CS0618
        // if (_options.SpecVersion >= HumlSpecVersion.V0_2) { ... }
        // #pragma warning restore CS0618

        // Consume optional %HUML version directive at the start of the document.
        // The directive is always emitted by HumlSerializerImpl; parsers must accept it.
        // When VersionSource.Header is active, parse the version string and apply it.
        HumlSpecVersion? detectedVersion = null;

        if (Peek().Type == TokenType.Version)
        {
            var versionToken = Advance();
            // Always record what the header declares, regardless of VersionSource.
            // TryParseSpecVersion returns null for unrecognised version strings.
            detectedVersion = TryParseSpecVersion(versionToken.Value!);
            if (_options.VersionSource == VersionSource.Header)
                ApplyVersionFromHeader(versionToken.Value!);
        }

        var tk = Peek();

        if (tk.Type == TokenType.Eof)
            throw new HumlParseException("Empty document is not valid.", tk.Line, tk.Column);

        if (tk.Indent != 0)
            throw new HumlParseException("Root element must not be indented.", tk.Line, tk.Column);

        var rootType = InferRootType();

        switch (rootType)
        {
            case RootType.Scalar:
            {
                var scalar = TokenToScalar(Advance());
                AssertRootEnd();
                return new HumlDocument(new HumlNode[] { scalar })
                {
                    Line = tk.Line,
                    Column = tk.Column,
                    DetectedVersion = detectedVersion,
                };
            }

            case RootType.EmptyList:
            {
                var emptyToken = Advance(); // consume EmptyList token
                AssertRootEnd();
                return new HumlDocument(new HumlNode[]
                {
                    new HumlSequence(Array.Empty<HumlNode>()) { Line = emptyToken.Line, Column = emptyToken.Column }
                })
                {
                    Line = emptyToken.Line,
                    Column = emptyToken.Column,
                    DetectedVersion = detectedVersion,
                };
            }

            case RootType.EmptyDict:
            {
                var emptyToken = Advance(); // consume EmptyDict token
                AssertRootEnd();
                return new HumlDocument(new HumlNode[]
                {
                    new HumlInlineMapping(Array.Empty<HumlNode>()) { Line = emptyToken.Line, Column = emptyToken.Column }
                })
                {
                    Line = emptyToken.Line,
                    Column = emptyToken.Column,
                    DetectedVersion = detectedVersion,
                };
            }

            case RootType.InlineList:
            {
                var seq = ParseInlineList();
                AssertRootEnd();
                return new HumlDocument(new HumlNode[] { seq })
                {
                    Line = tk.Line,
                    Column = tk.Column,
                    DetectedVersion = detectedVersion,
                };
            }

            case RootType.InlineDict:
            {
                var inlineMapping = ParseInlineDict();
                AssertRootEnd();
                return new HumlDocument(inlineMapping.Entries) // root inline dict entries become top-level entries
                {
                    Line = tk.Line,
                    Column = tk.Column,
                    DetectedVersion = detectedVersion,
                };
            }

            case RootType.MultilineList:
            {
                var seq = ParseMultilineList(0, tk.Line, tk.Column);
                AssertRootEnd();
                return new HumlDocument(new HumlNode[] { seq })
                {
                    Line = tk.Line,
                    Column = tk.Column,
                    DetectedVersion = detectedVersion,
                };
            }

            case RootType.MultilineDict:
            {
                var doc = ParseMultilineDict(0, tk.Line, tk.Column);
                AssertRootEnd();
                return doc with { DetectedVersion = detectedVersion };
            }

            default:
                throw new HumlParseException("Unexpected root content.", tk.Line, tk.Column);
        }
    }

    // ── Root-type inference ───────────────────────────────────────────────────

    /// <summary>
    /// Peeks at the first token(s) to determine the root structure type.
    /// Uses a single token of look-ahead; no tokens are consumed.
    /// </summary>
    private RootType InferRootType()
    {
        var tk = Peek();

        switch (tk.Type)
        {
            case TokenType.EmptyList:
                return RootType.EmptyList;

            case TokenType.EmptyDict:
                return RootType.EmptyDict;

            case TokenType.ListItem:
                return RootType.MultilineList;

            case TokenType.Key:
            case TokenType.QuotedKey:
                // Inline detection deferred to ParseMappingEntries via inlineLine tracking
                return RootType.MultilineDict;

            default:
                // Value token — check if followed by comma (inline list) or not (root scalar)
                if (IsValueToken(tk.Type))
                    return InferScalarOrInlineListRootType();

                throw new HumlParseException(
                    $"Unexpected token '{tk.Type}' at root.", tk.Line, tk.Column);
        }
    }

    /// <summary>
    /// Checks whether a root value token is followed by a comma (inline list) or not (scalar).
    /// </summary>
    private RootType InferScalarOrInlineListRootType()
    {
        // We have a value token at root. Read it, then peek the next token.
        // If next token is Comma → inline list. Otherwise → scalar.
        // We have a value token in the lookahead. Consume it, then peek at the next token
        // (Peek does NOT consume — nextToken stays in the lookahead buffer).
        // Store firstToken in the single-slot _pending buffer so it is replayed on the next Advance() call.
        // Only firstToken is stored; nextToken was never consumed so it remains available via Peek/Advance.
        var firstToken = Advance();
        var nextToken = Peek();
        // Store firstToken in the single pending slot for replay
        _pending = firstToken;
        _hasPending = true;

        return nextToken.Type == TokenType.Comma
            ? RootType.InlineList
            : RootType.Scalar;
    }

    // ── Two-token lookahead buffer ────────────────────────────────────────────
    // Used only during root-type inference when we need to read ahead one extra token.

    private Token _pending;
    private bool _hasPending;

    // ── Token access ──────────────────────────────────────────────────────────

    /// <summary>Returns the next token, consuming it from the stream.</summary>
    private Token Advance()
    {
        if (_hasPending)
        {
            _hasPending = false;
            return _pending;
        }

        var t = _lookahead;
        _lookahead = _lexer.NextToken();
        return t;
    }

    /// <summary>Returns the next token without consuming it.</summary>
    private Token Peek()
    {
        if (_hasPending)
            return _pending;
        return _lookahead;
    }

    // ── Value token classification ────────────────────────────────────────────

    /// <summary>Returns <c>true</c> if <paramref name="t"/> is a scalar value token.</summary>
    private static bool IsValueToken(TokenType t) =>
        t == TokenType.String ||
        t == TokenType.Int ||
        t == TokenType.Float ||
        t == TokenType.Bool ||
        t == TokenType.Null ||
        t == TokenType.NaN ||
        t == TokenType.Inf;

    // ── Scalar conversion ─────────────────────────────────────────────────────

    /// <summary>Converts a scalar token to a typed <see cref="HumlScalar"/> node.</summary>
    private static HumlScalar TokenToScalar(Token tok) => tok.Type switch
    {
        TokenType.String  => new HumlScalar(ScalarKind.String,  tok.Value)
                                { Line = tok.Line, Column = tok.Column },
        TokenType.Int     => new HumlScalar(ScalarKind.Integer, ParseInt(tok.Value!, tok.Line, tok.Column))
                                { Line = tok.Line, Column = tok.Column },
        TokenType.Float   => new HumlScalar(ScalarKind.Float,   ParseFloat(tok.Value!, tok.Line, tok.Column))
                                { Line = tok.Line, Column = tok.Column },
        TokenType.Bool    => new HumlScalar(ScalarKind.Bool,    string.Equals(tok.Value, "true",
                                 StringComparison.Ordinal))
                                { Line = tok.Line, Column = tok.Column },
        TokenType.Null    => new HumlScalar(ScalarKind.Null,    null)
                                { Line = tok.Line, Column = tok.Column },
        TokenType.NaN     => new HumlScalar(ScalarKind.NaN,     tok.Value)
                                { Line = tok.Line, Column = tok.Column },
        TokenType.Inf     => new HumlScalar(ScalarKind.Inf,     tok.Value)
                                { Line = tok.Line, Column = tok.Column },
        _                 => throw new HumlParseException(
                                 $"Unexpected token '{tok.Type}' where scalar expected.",
                                 tok.Line, tok.Column),
    };

    // ── Numeric parsing ───────────────────────────────────────────────────────

    /// <summary>
    /// Parses an integer literal with optional sign, base prefix (<c>0x</c>/<c>0o</c>/<c>0b</c>),
    /// and underscore separators.
    /// </summary>
    private static long ParseInt(string s, int line, int col)
    {
        int sign = 1;
        int idx = 0;

        if (s[0] == '-') { sign = -1; idx = 1; }
        else if (s[0] == '+') { idx = 1; }

        int radix = 10;
        if (s.Length - idx > 2)
        {
            var prefix = s.Substring(idx, 2);
            switch (prefix)
            {
                case "0x": case "0X": radix = 16; idx += 2; break;
                case "0o": case "0O": radix = 8;  idx += 2; break;
                case "0b": case "0B": radix = 2;  idx += 2; break;
            }
        }

        string digits = s.Substring(idx).Replace("_", "", StringComparison.Ordinal);
        try
        {
            if (radix == 10)
            {
                // Parse the signed literal whole so long.MinValue (-9223372036854775808)
                // round-trips: its magnitude alone (…808) exceeds long.MaxValue, so the
                // old `sign * Convert.ToInt64(magnitude)` rejected a representable value.
                return long.Parse(sign == -1 ? "-" + digits : digits, CultureInfo.InvariantCulture);
            }

            // Base-prefixed literals: Convert.ToInt64 with a radix interprets the high
            // bit as a two's-complement sign (0xFFFFFFFFFFFFFFFF would silently become
            // -1). Parse the magnitude unsigned and range-check explicitly so overflow
            // behaves the same as the decimal path.
            ulong magnitude = Convert.ToUInt64(digits, radix);
            if (sign == 1)
            {
                if (magnitude > long.MaxValue)
                    throw new OverflowException();
                return (long)magnitude;
            }
            return magnitude switch
            {
                <= long.MaxValue => -(long)magnitude,
                (ulong)long.MaxValue + 1 => long.MinValue,
                _ => throw new OverflowException(),
            };
        }
        catch (OverflowException)
        {
            throw new HumlParseException(
                $"Integer literal '{s}' overflows the supported range (int64).", line, col);
        }
        catch (Exception ex) when (ex is FormatException or ArgumentException)
        {
            // Defence in depth: the lexer validates digits, but a malformed literal must
            // never escape HumlSerializer.Parse as anything other than HumlParseException.
            throw new HumlParseException($"Invalid integer literal '{s}'.", line, col);
        }
    }

    /// <summary>Parses a floating-point literal, stripping underscore separators.</summary>
    private static double ParseFloat(string s, int line, int col)
    {
        try
        {
            return double.Parse(s.Replace("_", "", StringComparison.Ordinal), CultureInfo.InvariantCulture);
        }
        catch (FormatException)
        {
            throw new HumlParseException($"Invalid float literal '{s}'.", line, col);
        }
    }

    // ── Block parsers ─────────────────────────────────────────────────────────

    /// <summary>
    /// Parses a sequence of mapping entries at the given <paramref name="indent"/> level.
    /// Loops until EOF or a token with less indentation is encountered.
    /// Throws on bad indentation or duplicate keys.
    /// </summary>
    private List<HumlMapping> ParseMappingEntries(int indent)
    {
        var entries = new List<HumlMapping>();
        var seenKeys = new HashSet<string>(StringComparer.Ordinal);

        // When >= 0, we are in inline-dict mode: all entries must be on this line.
        int inlineLine = -1;

        while (true)
        {
            var tk = Peek();
            if (tk.Type == TokenType.Eof) break;
            if (tk.Indent < indent) break;
            if (tk.Indent != indent)
                throw new HumlParseException(
                    $"Bad indentation: expected {indent} spaces, got {tk.Indent}.",
                    tk.Line, tk.Column);

            // In inline mode, entries after the first comma must all be on the same line.
            if (inlineLine >= 0 && tk.Line != inlineLine)
                throw new HumlParseException(
                    "Inline dict entries must all be on the same line. " +
                    "Do not mix inline and multiline dict syntax.",
                    tk.Line, tk.Column);

            // Expect Key or QuotedKey
            if (tk.Type != TokenType.Key && tk.Type != TokenType.QuotedKey)
                throw new HumlParseException(
                    $"Expected a mapping key, got '{tk.Type}'.",
                    tk.Line, tk.Column);

            var keyToken = Advance();
            string key = keyToken.Value!;

            if (!seenKeys.Add(key))
                throw new HumlParseException(
                    $"Duplicate key '{key}'.",
                    keyToken.Line, keyToken.Column);

            // Expect ScalarIndicator or VectorIndicator
            var indicator = Peek();
            if (indicator.Type == TokenType.ScalarIndicator)
            {
                Advance(); // consume ':'
                var valueToken = Peek();
                if (!IsValueToken(valueToken.Type))
                    throw new HumlParseException(
                        $"Expected a scalar value after ':', got '{valueToken.Type}'.",
                        valueToken.Line, valueToken.Column);
                int entryLine = keyToken.Line;
                var scalar = TokenToScalar(Advance());
                entries.Add(new HumlMapping(key, scalar) { Line = keyToken.Line, Column = keyToken.Column });

                // Support root inline dict notation: key: val1, key2: val2, ...
                // When a comma follows, enter inline mode and record the line number.
                if (Peek().Type == TokenType.Comma)
                {
                    Advance(); // consume comma
                    inlineLine = entryLine; // all subsequent keys must be on this line
                    continue;
                }

                // No comma — if a key or value follows on the same line, missing comma.
                var followTk = Peek();
                if (followTk.Type != TokenType.Eof && followTk.Line == entryLine
                    && (followTk.Type == TokenType.Key || followTk.Type == TokenType.QuotedKey
                        || IsValueToken(followTk.Type)))
                {
                    throw new HumlParseException(
                        $"Expected ',' between inline dict entries, got '{followTk.Type}'.",
                        followTk.Line, followTk.Column);
                }
            }
            else if (indicator.Type == TokenType.VectorIndicator)
            {
                var vectorIndicator = Advance(); // consume '::'
                var value = ParseVector(indent + 2, vectorIndicator.Line, vectorIndicator.Column);
                entries.Add(new HumlMapping(key, value) { Line = keyToken.Line, Column = keyToken.Column });
            }
            else
            {
                throw new HumlParseException(
                    $"Expected ':' or '::' after key '{key}', got '{indicator.Type}'.",
                    indicator.Line, indicator.Column);
            }
        }

        return entries;
    }

    /// <summary>
    /// Parses a multiline dict block at the given <paramref name="indent"/> level.
    /// Returns a <see cref="HumlDocument"/> whose entries are the parsed mappings.
    /// </summary>
    /// <param name="indent">The expected indentation of child entries.</param>
    /// <param name="indicatorLine">The 1-based source line of the <c>::</c> or first-entry token that opened this block.</param>
    /// <param name="indicatorColumn">The 0-based column of the <c>::</c> or first-entry token that opened this block.</param>
    private HumlDocument ParseMultilineDict(int indent, int indicatorLine, int indicatorColumn)
    {
        if (++_depth > _maxDepth)
            throw new HumlParseException(
                $"Recursion depth limit of {_maxDepth} exceeded. Document is too deeply nested.",
                Peek().Line, Peek().Column);
        try
        {
            var entries = ParseMappingEntries(indent);
            return new HumlDocument(entries.ToArray()) { Line = indicatorLine, Column = indicatorColumn };
        }
        finally
        {
            _depth--;
        }
    }

    /// <summary>
    /// Parses a multiline list block at the given <paramref name="indent"/> level.
    /// Each item is introduced by a <see cref="TokenType.ListItem"/> (<c>-</c>) token.
    /// Returns a <see cref="HumlSequence"/>.
    /// </summary>
    /// <param name="indent">The expected indentation of list item tokens.</param>
    /// <param name="indicatorLine">The 1-based source line of the <c>::</c> or first list-item token that opened this list.</param>
    /// <param name="indicatorColumn">The 0-based column of the <c>::</c> or first list-item token that opened this list.</param>
    private HumlSequence ParseMultilineList(int indent, int indicatorLine, int indicatorColumn)
    {
        if (++_depth > _maxDepth)
            throw new HumlParseException(
                $"Recursion depth limit of {_maxDepth} exceeded. Document is too deeply nested.",
                Peek().Line, Peek().Column);
        try
        {
            var items = new List<HumlNode>();

            while (true)
            {
                var tk = Peek();
                if (tk.Type == TokenType.Eof) break;
                if (tk.Indent < indent) break;
                if (tk.Indent != indent)
                    throw new HumlParseException(
                        $"Bad indentation: expected {indent} spaces, got {tk.Indent}.",
                        tk.Line, tk.Column);

                if (tk.Type != TokenType.ListItem)
                    throw new HumlParseException(
                        $"Expected list item '-', got '{tk.Type}'.",
                        tk.Line, tk.Column);

                Advance(); // consume '-'

                var valueTk = Peek();
                if (valueTk.Type == TokenType.VectorIndicator)
                {
                    // List item with nested vector: - ::
                    var listVectorIndicator = Advance(); // consume '::'
                    var nested = ParseVector(indent + 2, listVectorIndicator.Line, listVectorIndicator.Column);
                    items.Add(nested);
                }
                else if (IsValueToken(valueTk.Type))
                {
                    items.Add(TokenToScalar(Advance()));
                }
                else if (valueTk.Type == TokenType.EmptyList)
                {
                    var emptyTok = Advance();
                    items.Add(new HumlSequence(Array.Empty<HumlNode>()) { Line = emptyTok.Line, Column = emptyTok.Column });
                }
                else if (valueTk.Type == TokenType.EmptyDict)
                {
                    var emptyTok = Advance();
                    items.Add(new HumlInlineMapping(Array.Empty<HumlNode>()) { Line = emptyTok.Line, Column = emptyTok.Column });
                }
                else
                {
                    throw new HumlParseException(
                        $"Expected a value after list item '-', got '{valueTk.Type}'.",
                        valueTk.Line, valueTk.Column);
                }
            }

            return new HumlSequence(items.ToArray()) { Line = indicatorLine, Column = indicatorColumn };
        }
        finally
        {
            _depth--;
        }
    }

    /// <summary>
    /// Dispatches after consuming a <see cref="TokenType.VectorIndicator"/> (<c>::</c>).
    /// Determines whether the vector content is multiline or inline, then parses accordingly.
    /// </summary>
    /// <param name="childIndent">Expected indentation of child tokens for multiline content.</param>
    /// <param name="indicatorLine">The 1-based line number of the consumed <c>::</c> token.</param>
    /// <param name="indicatorColumn">The 0-based column of the consumed <c>::</c> token.</param>
    private HumlNode ParseVector(int childIndent, int indicatorLine, int indicatorColumn)
    {
        if (++_depth > _maxDepth)
            throw new HumlParseException(
                $"Recursion depth limit of {_maxDepth} exceeded. Document is too deeply nested.",
                Peek().Line, Peek().Column);
        try
        {
            var next = Peek();
            bool isMultiline = next.Type == TokenType.Eof || next.Line != indicatorLine;

            if (isMultiline)
            {
                // Multiline — next token must be at childIndent
                if (next.Type == TokenType.Eof || next.Indent < childIndent)
                    throw new HumlParseException(
                        "Ambiguous empty vector after '::'. Use [] or {}.",
                        next.Line, next.Column);

                return next.Type == TokenType.ListItem
                    ? (HumlNode)ParseMultilineList(childIndent, indicatorLine, indicatorColumn)
                    : ParseMultilineDict(childIndent, indicatorLine, indicatorColumn);
            }

            // Inline
            var inlineValue = ParseInlineVectorValue();

            // After an inline vector value, no more tokens are allowed on the same line.
            var afterInline = Peek();
            if (afterInline.Type != TokenType.Eof && afterInline.Line == indicatorLine)
                throw new HumlParseException(
                    $"Unexpected token '{afterInline.Type}' after inline vector value. " +
                    "Use commas to separate inline dict/list entries.",
                    afterInline.Line, afterInline.Column);

            return inlineValue;
        }
        finally
        {
            _depth--;
        }
    }

    // ── Inline collection parsers ─────────────────────────────────────────────

    /// <summary>
    /// Parses the value that follows an inline <c>::</c> indicator.
    /// Dispatches to empty list, empty dict, inline dict, or inline list.
    /// </summary>
    private HumlNode ParseInlineVectorValue()
    {
        var tk = Peek();

        if (tk.Type == TokenType.EmptyList)
        {
            var emptyTok = Advance();
            return new HumlSequence(Array.Empty<HumlNode>()) { Line = emptyTok.Line, Column = emptyTok.Column };
        }

        if (tk.Type == TokenType.EmptyDict)
        {
            var emptyTok = Advance();
            return new HumlInlineMapping(Array.Empty<HumlNode>()) { Line = emptyTok.Line, Column = emptyTok.Column };
        }

        if (tk.Type == TokenType.Key || tk.Type == TokenType.QuotedKey)
            return ParseInlineDict();

        if (IsValueToken(tk.Type))
            return ParseInlineList();

        throw new HumlParseException(
            $"Expected inline collection value, got '{tk.Type}'.",
            tk.Line, tk.Column);
    }

    /// <summary>
    /// Parses a comma-separated inline list of scalar values.
    /// Returns a <see cref="HumlSequence"/>.
    /// </summary>
    private HumlSequence ParseInlineList()
    {
        int firstItemLine = Peek().Line;
        int firstItemColumn = Peek().Column;
        var items = new List<HumlNode>();

        while (true)
        {
            var tk = Peek();
            if (!IsValueToken(tk.Type))
                break;

            items.Add(TokenToScalar(Advance()));

            if (Peek().Type == TokenType.Comma)
                Advance(); // consume comma
            else
                break;
        }

        return new HumlSequence(items.ToArray()) { Line = firstItemLine, Column = firstItemColumn };
    }

    /// <summary>
    /// Parses a comma-separated inline dict of <c>key: value</c> pairs.
    /// Returns a <see cref="HumlInlineMapping"/> whose entries are <see cref="HumlMapping"/> nodes.
    /// Throws on duplicate keys.
    /// </summary>
    private HumlInlineMapping ParseInlineDict()
    {
        var entries = new List<HumlMapping>();
        var seenKeys = new HashSet<string>(StringComparer.Ordinal);

        // Use nullable to distinguish "first key not yet seen" from the position sentinel 0
        // ("unknown position"). Using 0 as a sentinel conflates the two meanings and would
        // silently produce Line = 0 if the loop exits without consuming any key (WR-03 fix).
        int? firstKeyLine = null;
        int firstKeyColumn = 0;

        while (true)
        {
            var tk = Peek();
            if (tk.Type != TokenType.Key && tk.Type != TokenType.QuotedKey)
                break;

            var keyToken = Advance();
            string key = keyToken.Value!;

            if (firstKeyLine is null)
            {
                firstKeyLine = keyToken.Line;
                firstKeyColumn = keyToken.Column;
            }

            if (!seenKeys.Add(key))
                throw new HumlParseException(
                    $"Duplicate key '{key}'.",
                    keyToken.Line, keyToken.Column);

            // Expect ScalarIndicator
            var indicator = Peek();
            if (indicator.Type != TokenType.ScalarIndicator)
                throw new HumlParseException(
                    $"Expected ':' after inline dict key '{key}', got '{indicator.Type}'.",
                    indicator.Line, indicator.Column);
            Advance(); // consume ':'

            var valueTk = Peek();
            if (!IsValueToken(valueTk.Type))
                throw new HumlParseException(
                    $"Expected scalar value in inline dict, got '{valueTk.Type}'.",
                    valueTk.Line, valueTk.Column);

            entries.Add(new HumlMapping(key, TokenToScalar(Advance())) { Line = keyToken.Line, Column = keyToken.Column });

            if (Peek().Type == TokenType.Comma)
                Advance(); // consume comma
            else
                break;
        }

        return new HumlInlineMapping(entries.ToArray()) { Line = firstKeyLine ?? 0, Column = firstKeyColumn };
    }

    // ── End-of-document assertion ─────────────────────────────────────────────

    /// <summary>
    /// Asserts that the next token is <see cref="TokenType.Eof"/>.
    /// Throws <see cref="HumlParseException"/> if there is unexpected trailing content.
    /// </summary>
    private void AssertRootEnd()
    {
        var tk = Peek();
        if (tk.Type != TokenType.Eof)
            throw new HumlParseException(
                "Unexpected content after root element.",
                tk.Line, tk.Column);
    }

    // ── Version header helpers ────────────────────────────────────────────────

    /// <summary>
    /// Applies the version declared in the document <c>%HUML</c> header.
    /// Validates the version against <see cref="SpecVersionPolicy"/> and dispatches
    /// <see cref="HumlOptions.UnknownVersionBehaviour"/> when out of the support window.
    /// Updates both <see cref="_effectiveSpecVersion"/> and <see cref="Lexer.Lexer.EffectiveSpecVersion"/>
    /// so that version-gated tokenisation rules are applied correctly for the rest of the document.
    /// </summary>
    private void ApplyVersionFromHeader(string versionValue)
    {
        var parsed = TryParseSpecVersion(versionValue);

#pragma warning disable CS0618 // MinimumSupportedVersion references V0_1
        bool isKnown = parsed.HasValue
            && parsed.Value >= SpecVersionPolicy.MinimumSupportedVersion
            && parsed.Value <= SpecVersionPolicy.LatestVersion;
#pragma warning restore CS0618

        if (isKnown && parsed.HasValue)
        {
            _effectiveSpecVersion = parsed.Value;
            _lexer.EffectiveSpecVersion = parsed.Value;
            return;
        }

        // Version is outside the support window.
        switch (_options.UnknownVersionBehaviour)
        {
            case UnknownVersionBehaviour.Throw:
                throw new HumlUnsupportedVersionException(versionValue);

            case UnknownVersionBehaviour.UseLatest:
                _effectiveSpecVersion = SpecVersionPolicy.LatestVersion;
                _lexer.EffectiveSpecVersion = SpecVersionPolicy.LatestVersion;
                return;

            case UnknownVersionBehaviour.UsePrevious:
                // UsePrevious falls back to the nearest older supported version.
                // If the declared version is entirely below the minimum supported version
                // (major.minor < 0.1), there is nothing to fall back to — throw instead.
                var majorMinor = TryExtractMajorMinor(versionValue);
                bool isBelowMinimum = !majorMinor.HasValue
                    || (majorMinor.Value.major == 0 && majorMinor.Value.minor < 1);

                if (isBelowMinimum)
                    throw new HumlUnsupportedVersionException(versionValue);

                // Above the window maximum — use the latest supported version.
                _effectiveSpecVersion = SpecVersionPolicy.LatestVersion;
                _lexer.EffectiveSpecVersion = SpecVersionPolicy.LatestVersion;
                return;
        }
    }

    /// <summary>
    /// Attempts to parse a HUML version string (e.g., "v0.1.0" or "v0.2") into a
    /// <see cref="HumlSpecVersion"/> enum value. Returns <c>null</c> if unrecognised.
    /// </summary>
    private static HumlSpecVersion? TryParseSpecVersion(string versionValue)
    {
        var major_minor = TryExtractMajorMinor(versionValue);
        if (!major_minor.HasValue) return null;

        return (major_minor.Value.major, major_minor.Value.minor) switch
        {
#pragma warning disable CS0618 // V0_1 obsolete
            (0, 1) => HumlSpecVersion.V0_1,
#pragma warning restore CS0618
            (0, 2) => HumlSpecVersion.V0_2,
            _ => null,
        };
    }

    /// <summary>
    /// Extracts the major and minor integer components from a version string.
    /// Strips a leading 'v' if present, then parses "major.minor[.patch...]".
    /// Returns <c>null</c> if the string cannot be parsed.
    /// </summary>
    private static (int major, int minor)? TryExtractMajorMinor(string versionValue)
    {
        var s = versionValue;
        if (s.Length > 0 && (s[0] == 'v' || s[0] == 'V'))
            s = s.Substring(1);

        var parts = s.Split('.');
        if (parts.Length < 2) return null;

        if (!int.TryParse(parts[0], System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out int major)) return null;
        if (!int.TryParse(parts[1], System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out int minor)) return null;

        return (major, minor);
    }

}
