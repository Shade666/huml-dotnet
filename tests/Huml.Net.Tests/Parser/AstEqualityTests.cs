using AwesomeAssertions;
using Huml.Net.Parser;
using Huml.Net.Versioning;
using Xunit;

namespace Huml.Net.Tests.Parser;

/// <summary>
/// Regression tests for the G3.2 finding that AST record equality on collection-bearing
/// nodes used reference equality (breaking the structural-equality contract documented on
/// <see cref="HumlNode"/>), and that deep single-child chains overflowed the stack in the
/// compiler-generated recursive Equals.
/// </summary>
public class AstEqualityTests
{
    [Fact]
    public void Documents_parsed_separately_are_structurally_equal()
    {
        var a = Huml.Parse("42", HumlOptions.LatestSupported);
        var b = Huml.Parse("42", HumlOptions.LatestSupported);
        a.Should().Be(b);
        a.GetHashCode().Should().Be(b.GetHashCode());
    }

    [Fact]
    public void Mappings_parsed_separately_are_structurally_equal()
    {
        var a = Huml.Parse("a: 1\nb: \"two\"", HumlOptions.LatestSupported);
        var b = Huml.Parse("a: 1\nb: \"two\"", HumlOptions.LatestSupported);
        a.Should().Be(b);
    }

    [Fact]
    public void Sequences_parsed_separately_are_structurally_equal()
    {
        var a = Huml.Parse("xs::\n  - 1\n  - 2", HumlOptions.LatestSupported);
        var b = Huml.Parse("xs::\n  - 1\n  - 2", HumlOptions.LatestSupported);
        a.Should().Be(b);
    }

    [Fact]
    public void Different_content_is_not_equal()
    {
        var a = Huml.Parse("a: 1", HumlOptions.LatestSupported);
        var b = Huml.Parse("a: 2", HumlOptions.LatestSupported);
        a.Should().NotBe(b);
    }

    [Fact]
    public void Documents_are_usable_as_dictionary_keys()
    {
        var set = new HashSet<HumlDocument>
        {
            Huml.Parse("a: 1", HumlOptions.LatestSupported),
            Huml.Parse("a: 1", HumlOptions.LatestSupported),
        };
        set.Should().HaveCount(1, because: "structurally equal documents must collapse to one key");
    }

    [Fact]
    public void Deep_single_child_mapping_chains_compare_without_stack_overflow()
    {
        HumlNode a = new HumlScalar(ScalarKind.Integer, 0L);
        HumlNode b = new HumlScalar(ScalarKind.Integer, 0L);
        for (int i = 0; i < 100_000; i++)
        {
            a = new HumlMapping("k", a);
            b = new HumlMapping("k", b);
        }

        // Must not StackOverflow (uncatchable); equal chains compare equal.
        a.Equals(b).Should().BeTrue();
    }
}
