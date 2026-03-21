namespace Huml.Net.Parser;

/// <summary>
/// Represents a key-value mapping entry (e.g., <c>key: value</c>).
/// </summary>
/// <param name="Key">The key string for this mapping.</param>
/// <param name="Value">The associated value node.</param>
public sealed record HumlMapping(string Key, HumlNode Value) : HumlNode;
