using AwesomeAssertions;
using Huml.Net.Lexer;
using Huml.Net.Versioning;
using Xunit;
using HumlLexer = Huml.Net.Lexer.Lexer;

namespace Huml.Net.Tests.Lexer;

public class LexerAllocationTests
{
    private static void LexAll(string input, HumlOptions options)
    {
        var lexer = new HumlLexer(input, options);
        Token t;
        do { t = lexer.NextToken(); } while (t.Type != TokenType.Eof);
    }

    [Fact]
    public void Hot_path_ASCII_document_has_bounded_allocations()
    {
        const string input = "key: \"value\"\nnum: 42\nflag: true\n";
        var options = HumlOptions.Default;

        // Warm up JIT
        LexAll(input, options);
        LexAll(input, options);

        long before = GC.GetAllocatedBytesForCurrentThread();
        LexAll(input, options);
        long allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        // Only string materialisations for value-bearing tokens are expected.
        // 3 keys + 1 string value = ~4 string allocations. No List, StringBuilder, etc.
        allocated.Should().BeLessThan(1024, because: "structural tokens must not allocate and only value tokens should create strings");
    }

    [Fact]
    public void Structural_only_document_allocates_minimally()
    {
        // A document with vectors and list items — structural tokens only (no string values)
        const string input = "items::\n  - 1\n  - 2\n  - 3\n";
        var options = HumlOptions.Default;

        LexAll(input, options);
        LexAll(input, options);

        long before = GC.GetAllocatedBytesForCurrentThread();
        LexAll(input, options);
        long allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        // Key "items" + 3 integer values = 4 string allocs. ListItem, VectorIndicator, Eof are null Value.
        allocated.Should().BeLessThan(1024);
    }
}
