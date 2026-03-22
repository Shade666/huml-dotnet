namespace Huml.Net.Exceptions;

/// <summary>
/// Thrown when an error occurs during HUML serialisation.
/// </summary>
public sealed class HumlSerializeException : Exception
{
    /// <summary>Initialises a new instance with an error message.</summary>
    /// <param name="message">Description of the serialisation error.</param>
    public HumlSerializeException(string message) : base(message) { }

    /// <summary>Initialises a new instance with an error message and a reference to the inner exception.</summary>
    /// <param name="message">Description of the serialisation error.</param>
    /// <param name="innerException">The exception that caused the current exception.</param>
    public HumlSerializeException(string message, Exception innerException) : base(message, innerException) { }
}
