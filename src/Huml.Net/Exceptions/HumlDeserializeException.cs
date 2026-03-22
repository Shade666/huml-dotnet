namespace Huml.Net.Exceptions;

/// <summary>
/// Thrown when an error occurs during HUML deserialisation.
/// </summary>
/// <remarks>
/// When <see cref="Key"/> and <see cref="Line"/> are set, they provide diagnostic
/// information about which HUML key and document line caused the failure.
/// </remarks>
public sealed class HumlDeserializeException : Exception
{
    /// <summary>
    /// The HUML key associated with the error, or <c>null</c> when no key context is available.
    /// </summary>
    public string? Key { get; }

    /// <summary>
    /// The 1-based line number in the HUML document where the error was detected,
    /// or <c>null</c> when no line context is available.
    /// </summary>
    public int? Line { get; }

    /// <summary>Initialises a new instance with an error message.</summary>
    /// <param name="message">Description of the deserialisation error.</param>
    public HumlDeserializeException(string message) : base(message) { }

    /// <summary>
    /// Initialises a new instance with a diagnostic message, key, and line number.
    /// The formatted message is: <c>[line {line}] Key '{key}': {message}</c>.
    /// </summary>
    /// <param name="message">Description of the deserialisation error.</param>
    /// <param name="key">The HUML key where the error occurred.</param>
    /// <param name="line">The 1-based line number where the error occurred.</param>
    public HumlDeserializeException(string message, string key, int line)
        : base($"[line {line}] Key '{key}': {message}")
    {
        Key = key;
        Line = line;
    }
}
