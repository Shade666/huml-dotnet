namespace Huml.Net.Parser;

/// <summary>
/// Represents an inline or empty mapping block in the HUML AST.
/// Produced for <c>{ key: value, ... }</c> inline notation and empty <c>{}</c> dicts only.
/// </summary>
/// <remarks>
/// Multiline mapping blocks — whether at document root or nested inside a <c>::</c> vector block —
/// produce a <see cref="HumlDocument"/> node, not a <see cref="HumlInlineMapping"/>.
/// </remarks>
/// <param name="Entries">The key-value mapping entries in this inline block.</param>
public sealed record HumlInlineMapping(IReadOnlyList<HumlNode> Entries) : HumlNode
{
    /// <inheritdoc/>
    public bool Equals(HumlInlineMapping? other) => HumlNodeEquality.DeepEquals(this, other);

    /// <inheritdoc/>
    public override int GetHashCode() => HumlNodeEquality.DeepHashCode(this);
}
