using AwesomeAssertions;
using Huml.Net.Parser;
using Xunit;

namespace Huml.Net.Tests.Parser;

public class HumlNodeTests
{
    // ── Type hierarchy ────────────────────────────────────────────────────────

    [Fact]
    public void HumlNode_is_abstract()
    {
        typeof(HumlNode).IsAbstract.Should().BeTrue();
    }

    [Fact]
    public void HumlDocument_is_sealed_and_derives_from_HumlNode()
    {
        typeof(HumlDocument).IsSealed.Should().BeTrue();
        typeof(HumlDocument).BaseType.Should().Be(typeof(HumlNode));
    }

    [Fact]
    public void HumlMapping_is_sealed_and_derives_from_HumlNode()
    {
        typeof(HumlMapping).IsSealed.Should().BeTrue();
        typeof(HumlMapping).BaseType.Should().Be(typeof(HumlNode));
    }

    [Fact]
    public void HumlSequence_is_sealed_and_derives_from_HumlNode()
    {
        typeof(HumlSequence).IsSealed.Should().BeTrue();
        typeof(HumlSequence).BaseType.Should().Be(typeof(HumlNode));
    }

    [Fact]
    public void HumlScalar_is_sealed_and_derives_from_HumlNode()
    {
        typeof(HumlScalar).IsSealed.Should().BeTrue();
        typeof(HumlScalar).BaseType.Should().Be(typeof(HumlNode));
    }

    // ── Construction ──────────────────────────────────────────────────────────

    [Fact]
    public void HumlDocument_exposes_Entries()
    {
        var entries = new List<HumlNode> { new HumlScalar(ScalarKind.Null, null) }.AsReadOnly();
        new HumlDocument(entries).Entries.Should().BeSameAs(entries);
    }

    [Fact]
    public void HumlMapping_exposes_Key_and_Value()
    {
        var scalar = new HumlScalar(ScalarKind.String, "val");
        var mapping = new HumlMapping("mykey", scalar);
        mapping.Key.Should().Be("mykey");
        mapping.Value.Should().BeSameAs(scalar);
    }

    [Fact]
    public void HumlSequence_exposes_Items()
    {
        var items = new List<HumlNode> { new HumlScalar(ScalarKind.Bool, true) }.AsReadOnly();
        new HumlSequence(items).Items.Should().BeSameAs(items);
    }

    [Fact]
    public void HumlScalar_exposes_Kind_and_Value()
    {
        var scalar = new HumlScalar(ScalarKind.String, "hello");
        scalar.Kind.Should().Be(ScalarKind.String);
        scalar.Value.Should().Be("hello");
    }

    // ── Equality ──────────────────────────────────────────────────────────────

    [Fact]
    public void HumlScalar_equality_is_structural()
    {
        var a = new HumlScalar(ScalarKind.String, "hello");
        var b = new HumlScalar(ScalarKind.String, "hello");
        (a == b).Should().BeTrue();
    }

    [Fact]
    public void HumlScalar_inequality_on_different_kind()
    {
        var a = new HumlScalar(ScalarKind.String, "hello");
        var b = new HumlScalar(ScalarKind.Bool, true);
        (a == b).Should().BeFalse();
    }

    [Fact]
    public void HumlScalar_boxed_value_type_equality()
    {
        var a = new HumlScalar(ScalarKind.Integer, 42L);
        var b = new HumlScalar(ScalarKind.Integer, 42L);
        (a == b).Should().BeTrue();
    }

    [Fact]
    public void HumlMapping_equality_with_same_value_reference()
    {
        var scalar = new HumlScalar(ScalarKind.Float, 1.5);
        var a = new HumlMapping("key", scalar);
        var b = new HumlMapping("key", scalar);
        (a == b).Should().BeTrue();
    }

    [Fact]
    public void HumlDocument_equality_with_same_list_reference()
    {
        var entries = new List<HumlNode> { new HumlScalar(ScalarKind.Null, null) }.AsReadOnly();
        var a = new HumlDocument(entries);
        var b = new HumlDocument(entries);
        (a == b).Should().BeTrue();
    }

    [Fact]
    public void HumlSequence_equality_with_same_list_reference()
    {
        var items = new List<HumlNode> { new HumlScalar(ScalarKind.Null, null) }.AsReadOnly();
        var a = new HumlSequence(items);
        var b = new HumlSequence(items);
        (a == b).Should().BeTrue();
    }

    // ── All 7 scalar kinds ────────────────────────────────────────────────────

    [Theory]
    [InlineData(ScalarKind.String, "hello")]
    [InlineData(ScalarKind.Integer, 42L)]
    [InlineData(ScalarKind.Float, 3.14)]
    [InlineData(ScalarKind.Bool, true)]
    [InlineData(ScalarKind.Null, null)]
    [InlineData(ScalarKind.NaN, null)]
    [InlineData(ScalarKind.Inf, null)]
    public void HumlScalar_can_represent_all_seven_scalar_kinds(ScalarKind kind, object? value)
    {
        var scalar = new HumlScalar(kind, value);
        scalar.Kind.Should().Be(kind);
        scalar.Value.Should().Be(value);
    }

    // ── Polymorphism ──────────────────────────────────────────────────────────

    [Fact]
    public void All_concrete_types_are_assignable_to_HumlNode()
    {
        var entries = new List<HumlNode>().AsReadOnly();
        var items = new List<HumlNode>().AsReadOnly();
        var scalar = new HumlScalar(ScalarKind.String, "x");

        HumlNode doc = new HumlDocument(entries);
        HumlNode inlineMapping = new HumlInlineMapping(entries);
        HumlNode mapping = new HumlMapping("k", scalar);
        HumlNode sequence = new HumlSequence(items);
        HumlNode scalarNode = scalar;

        static string Classify(HumlNode node) => node switch
        {
            HumlDocument => "document",
            HumlInlineMapping => "inline-mapping",
            HumlMapping => "mapping",
            HumlSequence => "sequence",
            HumlScalar => "scalar",
            _ => "unknown",
        };

        Classify(doc).Should().Be("document");
        Classify(inlineMapping).Should().Be("inline-mapping");
        Classify(mapping).Should().Be("mapping");
        Classify(sequence).Should().Be("sequence");
        Classify(scalarNode).Should().Be("scalar");
    }

    // ── HumlInlineMapping type tests ──────────────────────────────────────────

    [Fact]
    public void HumlInlineMapping_is_sealed_and_derives_from_HumlNode()
    {
        var node = new HumlInlineMapping(Array.Empty<HumlNode>());
        node.Should().BeAssignableTo<HumlNode>();
        typeof(HumlInlineMapping).IsSealed.Should().BeTrue();
    }

    [Fact]
    public void HumlInlineMapping_exposes_Entries()
    {
        var entries = new HumlNode[] { new HumlMapping("key", new HumlScalar(ScalarKind.String, "val")) };
        var mapping = new HumlInlineMapping(entries);
        mapping.Entries.Should().HaveCount(1);
    }

    [Fact]
    public void HumlInlineMapping_structural_equality()
    {
        var entries = Array.Empty<HumlNode>();
        var a = new HumlInlineMapping(entries);
        var b = new HumlInlineMapping(entries);
        a.Should().Be(b);
    }
}
