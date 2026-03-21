using Huml.Net.Versioning;

namespace Huml.Net.Parser;

/// <summary>Recursive-descent parser that produces a <see cref="HumlDocument"/> AST from a HUML source string.</summary>
internal sealed class HumlParser
{
    /// <summary>Initialises the parser with a source string and parsing options.</summary>
    internal HumlParser(string source, HumlOptions options)
    {
    }

    /// <summary>Parses the source document and returns the root <see cref="HumlDocument"/>.</summary>
    internal HumlDocument Parse()
    {
        throw new NotImplementedException();
    }
}
