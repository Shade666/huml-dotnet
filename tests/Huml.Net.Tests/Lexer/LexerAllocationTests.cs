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
        var lexer = new HumlLexer(input.AsSpan(), options);
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

    [Fact]
    public void Deserialize_span_path_does_not_allocate_input_string()
    {
        // Arrange: a realistic document just long enough to make a string copy detectable
        const string source = "%HUML v0.2.0\nName: \"Alice\"\nAge: 30\n";
        var span = source.AsSpan();
        var options = HumlOptions.LatestSupported;

        // Warm up: prime JIT, PropertyDescriptor cache, and converter caches.
        // Run several times to ensure all lazy-initialised caches (PropertyDescriptor,
        // EnumNameCache, ConverterCache, etc.) are fully saturated before measuring.
        for (int i = 0; i < 5; i++)
            _ = Huml.Deserialize<AllocationPoco>(span, options);
        GC.Collect(2, GCCollectionMode.Forced, true, true);

        long before = GC.GetAllocatedBytesForCurrentThread();
        _ = Huml.Deserialize<AllocationPoco>(span, options);
        long allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        // A full string copy of the input would be source.Length * sizeof(char) = 72 bytes.
        // Allow budget for the POCO + AST nodes (typically ~200-600 bytes warm) but not a
        // full input-buffer string. Threshold: inputLength * 2 bytes + 1024.
        long threshold = span.Length * sizeof(char) * 2 + 1024;
        allocated.Should().BeLessThan(threshold,
            because: "the span path must not allocate a string copy of the input buffer");
    }

    private sealed class AllocationPoco
    {
        public string? Name { get; set; }
        public int Age { get; set; }
    }
}
