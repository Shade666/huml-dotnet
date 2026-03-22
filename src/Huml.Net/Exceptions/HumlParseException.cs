namespace Huml.Net.Exceptions;

/// <summary>
/// Thrown when the HUML lexer or parser encounters invalid input.
/// </summary>
public sealed class HumlParseException : Exception
{
    /// <summary>1-based line number where the error was detected.</summary>
    public int Line { get; }

    /// <summary>0-based column where the error was detected.</summary>
    public int Column { get; }

    /// <summary>Initialises a new instance with an error message and source position.</summary>
    /// <param name="message">Description of the parse error.</param>
    /// <param name="line">1-based line number.</param>
    /// <param name="column">0-based column.</param>
    public HumlParseException(string message, int line, int column)
        : base($"[{line}:{column}] {message}")
    {
        Line = line;
        Column = column;
    }
}
