namespace Huml.Net.Parser;

/// <summary>
/// Represents a key-value mapping entry (e.g., <c>key: value</c>).
/// </summary>
/// <param name="Key">The key string for this mapping.</param>
/// <param name="Value">The associated value node.</param>
/// <remarks>
/// Equality is structural and iterative (see <see cref="HumlNodeEquality"/>): the
/// compiler-generated recursive equality would overflow the stack on deep single-child
/// chains, so it is overridden here.
/// </remarks>
public sealed record HumlMapping(string Key, HumlNode Value) : HumlNode
{
    /// <inheritdoc/>
    public bool Equals(HumlMapping? other) => HumlNodeEquality.DeepEquals(this, other);

    /// <inheritdoc/>
    public override int GetHashCode() => HumlNodeEquality.DeepHashCode(this);
}
