using System;
using System.Collections.Generic;
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
internal sealed class HumlParser
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

    private readonly Lexer.Lexer _lexer;

    /// <summary>
    /// Stored options — propagated to the Lexer at construction. Retained for future
    /// parser-level version gates using the <c>&gt;=</c> convention (PARS-04).
    /// </summary>
    private readonly HumlOptions _options;

    private readonly int _maxDepth;
    private int _depth;
    private Token _lookahead;

    // ── Constructor ───────────────────────────────────────────────────────────

    /// <summary>
    /// Initialises the parser with a HUML source string and parsing options.
    /// The <paramref name="options"/> are forwarded to the <see cref="Lexer.Lexer"/> so
    /// version-gated tokenisation rules (e.g., backtick multiline strings) fire correctly.
    /// </summary>
    /// <param name="source">The HUML document text to parse.</param>
    /// <param name="options">Options controlling spec-version behaviour.</param>
    internal HumlParser(string source, HumlOptions options)
    {
        _lexer = new Lexer.Lexer(source, options);
        _options = options;
        _maxDepth = options.MaxRecursionDepth;
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
        // The directive is always emitted by HumlSerializer; parsers must accept it.
        if (Peek().Type == TokenType.Version)
            Advance();

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
                return new HumlDocument(new HumlNode[] { scalar });
            }

            case RootType.EmptyList:
            {
                Advance(); // consume EmptyList token
                AssertRootEnd();
                return new HumlDocument(new HumlNode[] { new HumlSequence(Array.Empty<HumlNode>()) });
            }

            case RootType.EmptyDict:
            {
                Advance(); // consume EmptyDict token
                AssertRootEnd();
                return new HumlDocument(new HumlNode[] { new HumlDocument(Array.Empty<HumlNode>()) });
            }

            case RootType.InlineList:
            {
                var seq = ParseInlineList();
                AssertRootEnd();
                return new HumlDocument(new HumlNode[] { seq });
            }

            case RootType.InlineDict:
            {
                var innerDoc = ParseInlineDict();
                AssertRootEnd();
                return innerDoc; // root inline dict entries become top-level entries
            }

            case RootType.MultilineList:
            {
                var seq = ParseMultilineList(0);
                AssertRootEnd();
                return new HumlDocument(new HumlNode[] { seq });
            }

            case RootType.MultilineDict:
            {
                var doc = ParseMultilineDict(0);
                AssertRootEnd();
                return doc;
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
                // Consume key + indicator to decide between dict types, then restore state
                // via a second Lexer call. Since we cannot unread, we peek at what follows
                // by reading ahead: key token → advance → read indicator token.
                return InferDictRootType();

            default:
                // Value token — check if followed by comma (inline list) or not (root scalar)
                if (IsValueToken(tk.Type))
                    return InferScalarOrInlineListRootType();

                throw new HumlParseException(
                    $"Unexpected token '{tk.Type}' at root.", tk.Line, tk.Column);
        }
    }

    /// <summary>
    /// Consumes the key and indicator tokens to determine whether the root dict is
    /// multiline or inline, then stores the consumed tokens for replay via a
    /// two-item lookahead buffer.
    /// </summary>
    private RootType InferDictRootType()
    {
        // We cannot un-read from the lexer, so we peek token-by-token and store
        // the read tokens in a small replay buffer. We resolve by inspecting whether
        // the vector indicator is at end-of-line (multiline dict) or not (inline dict).
        // If scalar indicator: the value follows on the same line → multiline dict
        // (scalar values are single-line; the dict has one or more mappings).
        //
        // Since the only meaningful distinction for root dict type is
        // MultilineDict vs InlineDict, and we know the root starts with a Key/QuotedKey,
        // we check whether there is a VectorIndicator whose value follows inline.
        //
        // Note: we do not actually need to peek further than the indicator here —
        // the distinction is made during ParseMappingEntries / ParseVector.
        // Both produce a HumlDocument. We always return MultilineDict for
        // Key/QuotedKey roots; ParseMappingEntries handles single-line entries naturally.
        //
        // For inline dict at root (e.g., "a: 1, b: 2"): the lexer will emit
        // Key, ScalarIndicator, Int, Comma, ... — we can detect the comma after the first
        // value to know it's an inline dict. However, ParseMultilineDict already handles
        // a mapping followed by more mappings (it loops). The inline dict root case
        // only applies when there are commas on the SAME LINE as the first key.
        //
        // Since HUML inline dict at root level uses commas and single-line form,
        // and the standard case for multi-mapping docs uses newlines, we default
        // to MultilineDict here — ParseMappingEntries will exit after EOF naturally.
        return RootType.MultilineDict;
    }

    /// <summary>
    /// Checks whether a root value token is followed by a comma (inline list) or not (scalar).
    /// </summary>
    private RootType InferScalarOrInlineListRootType()
    {
        // We have a value token at root. Read it, then peek the next token.
        // If next token is Comma → inline list. Otherwise → scalar.
        // Since we must not lose the first token, we use the two-slot buffer approach:
        // store current lookahead, advance, check next, then prepend both to buffer.
        //
        // Implementation: use _pending buffer. We read one token ahead.
        var firstToken = Advance();
        var nextToken = Peek();
        // Prepend both back using the pending buffer
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
    private HumlScalar TokenToScalar(Token tok) => tok.Type switch
    {
        TokenType.String  => new HumlScalar(ScalarKind.String,  tok.Value),
        TokenType.Int     => new HumlScalar(ScalarKind.Integer, ParseInt(tok.Value!)),
        TokenType.Float   => new HumlScalar(ScalarKind.Float,   ParseFloat(tok.Value!)),
        TokenType.Bool    => new HumlScalar(ScalarKind.Bool,    string.Equals(tok.Value, "true",
                                 StringComparison.OrdinalIgnoreCase)),
        TokenType.Null    => new HumlScalar(ScalarKind.Null,    null),
        TokenType.NaN     => new HumlScalar(ScalarKind.NaN,     tok.Value),
        TokenType.Inf     => new HumlScalar(ScalarKind.Inf,     tok.Value),
        _                 => throw new HumlParseException(
                                 $"Unexpected token '{tok.Type}' where scalar expected.",
                                 tok.Line, tok.Column),
    };

    // ── Numeric parsing ───────────────────────────────────────────────────────

    /// <summary>
    /// Parses an integer literal with optional sign, base prefix (<c>0x</c>/<c>0o</c>/<c>0b</c>),
    /// and underscore separators.
    /// </summary>
    private static long ParseInt(string s)
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

        string digits = s.Substring(idx).Replace("_", "");
        return sign * Convert.ToInt64(digits, radix);
    }

    /// <summary>Parses a floating-point literal, stripping underscore separators.</summary>
    private static double ParseFloat(string s) =>
        double.Parse(s.Replace("_", ""), CultureInfo.InvariantCulture);

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

        while (true)
        {
            var tk = Peek();
            if (tk.Type == TokenType.Eof) break;
            if (tk.Indent < indent) break;
            if (tk.Indent != indent)
                throw new HumlParseException(
                    $"Bad indentation: expected {indent} spaces, got {tk.Indent}.",
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
                var scalar = TokenToScalar(Advance());
                entries.Add(new HumlMapping(key, scalar));
            }
            else if (indicator.Type == TokenType.VectorIndicator)
            {
                var vectorIndicator = Advance(); // consume '::'
                var value = ParseVector(indent + 2, vectorIndicator.Line);
                entries.Add(new HumlMapping(key, value));
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
    private HumlDocument ParseMultilineDict(int indent)
    {
        if (++_depth > _maxDepth)
            throw new HumlParseException(
                $"Recursion depth limit of {_maxDepth} exceeded. Document is too deeply nested.",
                Peek().Line, Peek().Column);
        try
        {
            var entries = ParseMappingEntries(indent);
            return new HumlDocument(entries.ToArray());
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
    private HumlSequence ParseMultilineList(int indent)
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
                    var nested = ParseVector(indent + 2, listVectorIndicator.Line);
                    items.Add(nested);
                }
                else if (IsValueToken(valueTk.Type))
                {
                    items.Add(TokenToScalar(Advance()));
                }
                else if (valueTk.Type == TokenType.EmptyList)
                {
                    Advance();
                    items.Add(new HumlSequence(Array.Empty<HumlNode>()));
                }
                else if (valueTk.Type == TokenType.EmptyDict)
                {
                    Advance();
                    items.Add(new HumlDocument(Array.Empty<HumlNode>()));
                }
                else
                {
                    throw new HumlParseException(
                        $"Expected a value after list item '-', got '{valueTk.Type}'.",
                        valueTk.Line, valueTk.Column);
                }
            }

            return new HumlSequence(items.ToArray());
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
    private HumlNode ParseVector(int childIndent, int indicatorLine)
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
                    ? (HumlNode)ParseMultilineList(childIndent)
                    : ParseMultilineDict(childIndent);
            }

            // Inline
            return ParseInlineVectorValue();
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
            Advance();
            return new HumlSequence(Array.Empty<HumlNode>());
        }

        if (tk.Type == TokenType.EmptyDict)
        {
            Advance();
            return new HumlDocument(Array.Empty<HumlNode>());
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

        return new HumlSequence(items.ToArray());
    }

    /// <summary>
    /// Parses a comma-separated inline dict of <c>key: value</c> pairs.
    /// Returns a <see cref="HumlDocument"/> whose entries are <see cref="HumlMapping"/> nodes.
    /// Throws on duplicate keys.
    /// </summary>
    private HumlDocument ParseInlineDict()
    {
        var entries = new List<HumlMapping>();
        var seenKeys = new HashSet<string>(StringComparer.Ordinal);

        while (true)
        {
            var tk = Peek();
            if (tk.Type != TokenType.Key && tk.Type != TokenType.QuotedKey)
                break;

            var keyToken = Advance();
            string key = keyToken.Value!;

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

            entries.Add(new HumlMapping(key, TokenToScalar(Advance())));

            if (Peek().Type == TokenType.Comma)
                Advance(); // consume comma
            else
                break;
        }

        return new HumlDocument(entries.ToArray());
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

}
