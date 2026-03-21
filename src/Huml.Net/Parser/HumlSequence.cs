namespace Huml.Net.Parser;

/// <summary>
/// Represents a sequence of items (a HUML list).
/// </summary>
/// <param name="Items">The ordered list of child nodes.</param>
public sealed record HumlSequence(IReadOnlyList<HumlNode> Items) : HumlNode;
