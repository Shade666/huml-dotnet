using AwesomeAssertions;
using Huml.Net.Serialization;
using Huml.Net.Versioning;
using Xunit;

namespace Huml.Net.Tests.Serialization;

public class HumlSerializerInlineTests
{
    // ── Test POCOs ────────────────────────────────────────────────────────────

    private class InlineListPoco
    {
        public List<int> Tags { get; set; } = new();
    }

    private class InlineAttrTruePoco
    {
        [HumlProperty(Inline = InlineMode.Inline)]
        public List<int> Tags { get; set; } = new();
    }

    private class InlineAttrFalsePoco
    {
        [HumlProperty(Inline = InlineMode.Multiline)]
        public List<int> Tags { get; set; } = new();
    }

    private class InlineDictPoco
    {
        public Dictionary<string, int> Counts { get; set; } = new();
    }

    private class ComplexListPoco
    {
        public List<SubObj> Items { get; set; } = new();
    }

    private class SubObj
    {
        public int X { get; set; }
    }

    private class ComplexDictPoco
    {
        public Dictionary<string, List<int>> Nested { get; set; } = new();
    }

    private class InlineAttrCheckPoco
    {
        [HumlProperty(Inline = InlineMode.Inline)]
        public string? Name { get; set; }
    }

    private class NoInlineAttrPoco
    {
        public string? Name { get; set; }
    }

    private class OmitNullCollectionPoco
    {
        [HumlProperty(OmitIfDefault = true)]
        public List<int>? Tags { get; set; }
    }

    private class MixedScalarListPoco
    {
        public List<object?> Values { get; set; } = new();
    }

    // ── Constructor ───────────────────────────────────────────────────────────

    public HumlSerializerInlineTests()
    {
        PropertyDescriptor.ClearCache();
    }

    // ── Contract tests (must pass immediately — no serialiser changes needed) ─

    // INL-07
    [Fact]
    public void CollectionFormat_Enum_HasCorrectValues()
    {
        ((int)CollectionFormat.Multiline).Should().Be(0);
        ((int)CollectionFormat.Inline).Should().Be(1);
    }

    // INL-08
    [Fact]
    public void HumlOptions_Default_CollectionFormat_IsMultiline()
    {
        HumlOptions.Default.CollectionFormat.Should().Be(CollectionFormat.Multiline);
    }

    // INL-08
    [Fact]
    public void HumlOptions_CollectionFormat_CanBeSetToInline()
    {
        var opts = new HumlOptions { CollectionFormat = CollectionFormat.Inline };
        opts.CollectionFormat.Should().Be(CollectionFormat.Inline);
    }

    // INL-12
    [Fact]
    public void HumlPropertyAttribute_Inline_DefaultsToInherit()
    {
        new HumlPropertyAttribute().Inline.Should().Be(InlineMode.Inherit);
    }

    // INL-12
    [Fact]
    public void HumlPropertyAttribute_Inline_CanBeInline()
    {
        new HumlPropertyAttribute { Inline = InlineMode.Inline }.Inline.Should().Be(InlineMode.Inline);
    }

    // INL-12
    [Fact]
    public void HumlPropertyAttribute_Inline_CanBeMultiline()
    {
        new HumlPropertyAttribute { Inline = InlineMode.Multiline }.Inline.Should().Be(InlineMode.Multiline);
    }

    // INL-12: PropertyDescriptor converts InlineMode.Inline → true
    [Fact]
    public void PropertyDescriptor_Caches_InlineTrue_WhenInlineMode_Inline()
    {
        PropertyDescriptor.ClearCache();
        var descs = PropertyDescriptor.GetDescriptors(typeof(InlineAttrTruePoco));
        descs.Should().HaveCount(1);
        descs[0].Inline.Should().Be(true);
    }

    // INL-12: PropertyDescriptor converts absent Inline → null
    [Fact]
    public void PropertyDescriptor_Caches_InlineNull_WhenNotSet()
    {
        PropertyDescriptor.ClearCache();
        var descs = PropertyDescriptor.GetDescriptors(typeof(NoInlineAttrPoco));
        descs.Should().HaveCount(1);
        descs[0].Inline.Should().BeNull();
    }

    // INL-12: PropertyDescriptor converts InlineMode.Multiline → false
    [Fact]
    public void PropertyDescriptor_Caches_InlineFalse_WhenInlineMode_Multiline()
    {
        PropertyDescriptor.ClearCache();
        var descs = PropertyDescriptor.GetDescriptors(typeof(InlineAttrFalsePoco));
        descs.Should().HaveCount(1);
        descs[0].Inline.Should().Be(false);
    }

    // ── Serialiser behaviour tests (will FAIL until Plan 02 implements dispatch) ─

    // INL-01
    [Fact]
    public void Serialize_InlineOption_ScalarList_EmitsInline()
    {
        PropertyDescriptor.ClearCache();
        var poco = new InlineListPoco { Tags = new List<int> { 1, 2, 3 } };
        var opts = new HumlOptions { CollectionFormat = CollectionFormat.Inline };

        var result = HumlSerializer.Serialize(poco, opts);

        result.Should().Contain("Tags:: 1, 2, 3\n");
    }

    // INL-02
    [Fact]
    public void Serialize_InlineOption_ComplexList_FallsBackToMultiline()
    {
        PropertyDescriptor.ClearCache();
        var poco = new ComplexListPoco { Items = new List<SubObj> { new SubObj { X = 1 } } };
        var opts = new HumlOptions { CollectionFormat = CollectionFormat.Inline };

        var result = HumlSerializer.Serialize(poco, opts);

        // Complex list (contains POCOs) must fall back to multiline, with vector
        // items in the grammar-valid "- ::" form (no bare dash, no trailing space).
        result.Should().Contain("Items::\n");
        result.Should().Contain("  - ::\n");
        result.Should().Contain("    X: 1\n");
    }

    // INL-03
    [Fact]
    public void Serialize_InlineOption_ScalarDict_EmitsInline()
    {
        PropertyDescriptor.ClearCache();
        var poco = new InlineDictPoco
        {
            Counts = new Dictionary<string, int> { ["a"] = 1, ["b"] = 2 }
        };
        var opts = new HumlOptions { CollectionFormat = CollectionFormat.Inline };

        var result = HumlSerializer.Serialize(poco, opts);

        result.Should().Contain("Counts:: a: 1, b: 2\n");
    }

    // INL-04
    [Fact]
    public void Serialize_InlineOption_ComplexDict_FallsBackToMultiline()
    {
        PropertyDescriptor.ClearCache();
        var poco = new ComplexDictPoco
        {
            Nested = new Dictionary<string, List<int>> { ["key"] = new List<int> { 1 } }
        };
        var opts = new HumlOptions { CollectionFormat = CollectionFormat.Inline };

        var result = HumlSerializer.Serialize(poco, opts);

        // Complex dict (values are lists) must fall back to multiline
        result.Should().Contain("Nested::\n");
        result.Should().Contain("  key::\n");
    }

    // INL-12 (attribute overrides global)
    [Fact]
    public void Serialize_InlineAttribute_Inline_OverridesGlobal()
    {
        PropertyDescriptor.ClearCache();
        // [HumlProperty(Inline = InlineMode.Inline)] with default options (Multiline) should still emit inline
        var poco = new InlineAttrTruePoco { Tags = new List<int> { 10, 20, 30 } };

        var result = HumlSerializer.Serialize(poco, HumlOptions.Default);

        result.Should().Contain("Tags:: 10, 20, 30\n");
    }

    // INL-12 (attribute overrides global)
    [Fact]
    public void Serialize_InlineAttribute_Multiline_OverridesGlobal()
    {
        PropertyDescriptor.ClearCache();
        // [HumlProperty(Inline = InlineMode.Multiline)] with CollectionFormat.Inline should still emit multiline
        var poco = new InlineAttrFalsePoco { Tags = new List<int> { 1, 2 } };
        var opts = new HumlOptions { CollectionFormat = CollectionFormat.Inline };

        var result = HumlSerializer.Serialize(poco, opts);

        result.Should().Contain("Tags::\n");
        result.Should().Contain("  - 1\n");
        result.Should().Contain("  - 2\n");
    }

    // INL-01 (regression)
    [Fact]
    public void Serialize_Default_StillMultiline()
    {
        PropertyDescriptor.ClearCache();
        var poco = new InlineListPoco { Tags = new List<int> { 1, 2 } };

        var result = HumlSerializer.Serialize(poco, HumlOptions.Default);

        result.Should().Contain("Tags::\n");
        result.Should().Contain("  - 1\n");
        result.Should().Contain("  - 2\n");
    }

    // INL-01
    [Fact]
    public void Serialize_InlineOption_EmptyCollection_StillUsesLiteral()
    {
        PropertyDescriptor.ClearCache();
        var poco = new InlineListPoco { Tags = new List<int>() };
        var opts = new HumlOptions { CollectionFormat = CollectionFormat.Inline };

        var result = HumlSerializer.Serialize(poco, opts);

        // Empty collections always use the empty literal regardless of CollectionFormat
        result.Should().Contain("Tags:: []\n");
    }

    // INL-01 (round-trip)
    [Fact]
    public void Serialize_InlineSequence_RoundTrips()
    {
        PropertyDescriptor.ClearCache();
        var poco = new InlineListPoco { Tags = new List<int> { 7, 8, 9 } };
        var opts = new HumlOptions { CollectionFormat = CollectionFormat.Inline };

        var result = HumlSerializer.Serialize(poco, opts);

        var act = () => HumlSerializer.Parse(result, HumlOptions.AutoDetect);
        act.Should().NotThrow();
    }

    // INL-03 (round-trip)
    [Fact]
    public void Serialize_InlineDict_RoundTrips()
    {
        PropertyDescriptor.ClearCache();
        var poco = new InlineDictPoco
        {
            Counts = new Dictionary<string, int> { ["x"] = 5 }
        };
        var opts = new HumlOptions { CollectionFormat = CollectionFormat.Inline };

        var result = HumlSerializer.Serialize(poco, opts);

        var act = () => HumlSerializer.Parse(result, HumlOptions.AutoDetect);
        act.Should().NotThrow();
    }

    // INL-08
    [Fact]
    public void Serialize_InlineOption_NullCollection_OmittedWhenOmitIfDefault()
    {
        PropertyDescriptor.ClearCache();
        var poco = new OmitNullCollectionPoco { Tags = null };
        var opts = new HumlOptions { CollectionFormat = CollectionFormat.Inline };

        var result = HumlSerializer.Serialize(poco, opts);

        // OmitIfDefault = true with null value — property should be omitted
        result.Should().NotContain("Tags");
    }

    // INL-01 (mixed scalar types)
    [Fact]
    public void Serialize_InlineOption_MixedScalarTypes_EmitsInline()
    {
        PropertyDescriptor.ClearCache();
        var poco = new MixedScalarListPoco
        {
            Values = new List<object?> { 1, "hello", true, null, 3.14 }
        };
        var opts = new HumlOptions { CollectionFormat = CollectionFormat.Inline };

        var result = HumlSerializer.Serialize(poco, opts);

        // All items are scalars — should emit inline
        result.Should().Contain("Values::");
        // Verify it's on a single line (inline format)
        result.Should().NotContain("  - ");
    }
}
