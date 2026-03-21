namespace Huml.Net.Lexer;

/// <summary>Classifies a single HUML lexical token.</summary>
public enum TokenType
{
    // Structural
    /// <summary>End of input.</summary>
    Eof,
    /// <summary>Lexer error sentinel (a <see cref="T:Huml.Net.Exceptions.HumlParseException"/> is thrown before this appears).</summary>
    Error,

    // Directives
    /// <summary>The <c>%HUML vX.Y.Z</c> version directive.</summary>
    Version,

    // Keys
    /// <summary>A bare key: <c>[a-zA-Z][a-zA-Z0-9_-]*</c>.</summary>
    Key,
    /// <summary>A double-quoted key.</summary>
    QuotedKey,

    // Indicators
    /// <summary>The scalar indicator <c>:</c>.</summary>
    ScalarIndicator,
    /// <summary>The vector indicator <c>::</c>.</summary>
    VectorIndicator,
    /// <summary>A list item marker <c>-</c>.</summary>
    ListItem,
    /// <summary>An inline collection separator <c>,</c>.</summary>
    Comma,

    // Scalars
    /// <summary>A double-quoted or triple-quoted string value.</summary>
    String,
    /// <summary>An integer literal (decimal, hex, octal, binary).</summary>
    Int,
    /// <summary>A floating-point literal.</summary>
    Float,
    /// <summary>A boolean literal (<c>true</c> or <c>false</c>).</summary>
    Bool,
    /// <summary>A null literal.</summary>
    Null,
    /// <summary>A not-a-number literal (<c>nan</c>).</summary>
    NaN,
    /// <summary>An infinity literal (<c>inf</c>, <c>+inf</c>, or <c>-inf</c>).</summary>
    Inf,

    // Empty collections
    /// <summary>An empty list marker <c>[]</c>.</summary>
    EmptyList,
    /// <summary>An empty dict marker <c>{}</c>.</summary>
    EmptyDict,
}
