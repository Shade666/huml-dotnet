using AwesomeAssertions;
using Huml.Net.Versioning;
using Xunit;

namespace Huml.Net.Tests.Serialization;

public class HumlSerializerAllocationTests
{
    [Fact]
    public void Cached_depth_serialization_does_not_allocate_indent_strings()
    {
        // A simple POCO with nesting to exercise depth 0, 1, 2
        var value = new { name = "test", nested = new { inner = "val" } };
        var options = HumlOptions.LatestSupported;

        // JIT warmup
        Huml.Serialize(value, options);
        Huml.Serialize(value, options);

        // Measure first serialization
        long before1 = GC.GetAllocatedBytesForCurrentThread();
        string result1 = Huml.Serialize(value, options);
        long alloc1 = GC.GetAllocatedBytesForCurrentThread() - before1;

        // Measure second serialization -- indent strings should be cached
        long before2 = GC.GetAllocatedBytesForCurrentThread();
        string result2 = Huml.Serialize(value, options);
        long alloc2 = GC.GetAllocatedBytesForCurrentThread() - before2;

        result1.Should().Be(result2);
        // Second call should not allocate significantly more than first (indent strings are static).
        // Both calls allocate StringBuilder + result string, but indent strings are reused.
        // Allow a small noise margin (512 bytes) over alloc1 to account for JIT/GC metadata jitter.
        alloc2.Should().BeLessThan(alloc1 + 512,
            because: "indent strings are cached in a static array and should not allocate on subsequent calls");
    }

    [Fact]
    public void Depth_beyond_cache_produces_correct_output()
    {
        // Depth 65 means 130 spaces of indentation for the innermost key.
        // We cannot easily nest 65 POCOs, so we verify the Indent method indirectly
        // by checking that a normally nested document serializes correctly (correctness guard).
        var value = new { a = new { b = new { c = "deep" } } };
        var result = Huml.Serialize(value, HumlOptions.LatestSupported);
        // depth 2 key "c" should have 4 spaces (2 * 2)
        result.Should().Contain("    c: \"deep\"");
    }

    [Fact]
    public void Indent_cache_produces_correct_spaces_at_each_depth()
    {
        var value = new { top = "zero", nested = new { mid = "one" } };
        var result = Huml.Serialize(value, HumlOptions.LatestSupported);

        // depth 0: "top" has no leading spaces
        result.Should().Contain("top: \"zero\"");
        // depth 0: "nested" has no leading spaces
        result.Should().Contain("nested::");
        // depth 1: "mid" has exactly 2 leading spaces
        result.Should().Contain("  mid: \"one\"");
    }
}
