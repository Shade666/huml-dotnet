namespace Huml.Net.Parser;

/// <summary>Identifies the kind of a HUML scalar value.</summary>
public enum ScalarKind
{
    /// <summary>A double-quoted string scalar.</summary>
    String,
    /// <summary>An integer scalar (decimal, hex, octal, or binary literal).</summary>
    Integer,
    /// <summary>A floating-point scalar.</summary>
    Float,
    /// <summary>A boolean scalar (<c>true</c> or <c>false</c>).</summary>
    Bool,
    /// <summary>A null scalar.</summary>
    Null,
    /// <summary>A not-a-number scalar (<c>nan</c>).</summary>
    NaN,
    /// <summary>An infinity scalar (<c>inf</c>, <c>+inf</c>, or <c>-inf</c>).</summary>
    Inf,
}
