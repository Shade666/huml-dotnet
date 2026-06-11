namespace Huml.Net.Parser;

/// <summary>
/// Represents a sequence of items (a HUML list).
/// </summary>
/// <param name="Items">The ordered list of child nodes.</param>
/// <remarks>
/// Equality is element-wise and structural (see <see cref="HumlNodeEquality"/>) — the
/// compiler-generated equality would compare the <see cref="Items"/> list by reference.
/// </remarks>
public sealed record HumlSequence(IReadOnlyList<HumlNode> Items) : HumlNode
{
    /// <inheritdoc/>
    public bool Equals(HumlSequence? other) => HumlNodeEquality.DeepEquals(this, other);

    /// <inheritdoc/>
    public override int GetHashCode() => HumlNodeEquality.DeepHashCode(this);
}
