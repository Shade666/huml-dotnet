using AwesomeAssertions;
using Huml.Net.Exceptions;
using Huml.Net.Serialization;
using Huml.Net.Versioning;
using Xunit;

#pragma warning disable CS0618 // HumlSpecVersion.V0_1 is deliberately used to test V0_1 header emission

namespace Huml.Net.Tests.Serialization;

public class HumlSerializerTests
{
    // ── Test POCOs ────────────────────────────────────────────────────────────

    // Declaration order: Zebra, Alpha, Beta — intentionally NOT alphabetical
    private class OrderedPoco
    {
        public string? Zebra { get; set; }
        public int Alpha { get; set; }
        public bool Beta { get; set; }
    }

    private class ScalarPoco
    {
        public string? Str { get; set; }
        public bool BoolTrue { get; set; }
        public bool BoolFalse { get; set; }
        public int IntVal { get; set; }
        public long LongVal { get; set; }
        public double DoubleVal { get; set; }
        public double NanVal { get; set; }
        public double PosInf { get; set; }
        public double NegInf { get; set; }
        public string? NullStr { get; set; }
    }

    private class RenamedPoco
    {
        [HumlProperty("custom_key")]
        public string? Name { get; set; }
    }

    private class IgnoredPoco
    {
        public string? Visible { get; set; }
        [HumlIgnore]
        public string? Hidden { get; set; }
    }

    private class OmitDefaultPoco
    {
        [HumlProperty(OmitIfDefault = true)]
        public int Value { get; set; }
        public string? Name { get; set; }
    }

    private class NestedPoco
    {
        public string? Title { get; set; }
        public InnerPoco? Inner { get; set; }
    }

    private class InnerPoco
    {
        public string? Label { get; set; }
        public int Count { get; set; }
    }

    private class ListPoco
    {
        public List<string>? Items { get; set; }
    }

    private class ArrayPoco
    {
        public string[]? Items { get; set; }
    }

    private class DictPoco
    {
        public Dictionary<string, string>? Map { get; set; }
    }

    private class MixedStringPoco
    {
        public string? Text { get; set; }
    }

    // ── Constructor ───────────────────────────────────────────────────────────

    public HumlSerializerTests()
    {
        PropertyDescriptor.ClearCache();
    }

    // ── Header tests ──────────────────────────────────────────────────────────

    [Fact]
    public void Serialize_DefaultOptions_EmitsV02Header()
    {
        var result = HumlSerializer.Serialize(new OrderedPoco(), HumlOptions.Default);

        result.Should().StartWith("%HUML v0.2.0\n");
    }

    [Fact]
    public void Serialize_V01Options_EmitsV01Header()
    {
        var opts = new HumlOptions { SpecVersion = HumlSpecVersion.V0_1 };
        var result = HumlSerializer.Serialize(new OrderedPoco(), opts);

        result.Should().StartWith("%HUML v0.1.0\n");
    }

    [Fact]
    public void Serialize_NullValue_EmitsHeaderAndNull()
    {
        var result = HumlSerializer.Serialize(null, HumlOptions.Default);

        result.Should().Be("%HUML v0.2.0\nnull\n");
    }

    // ── Declaration order ─────────────────────────────────────────────────────

    [Fact]
    public void Serialize_PropertiesEmittedInDeclarationOrder_NotAlphabetical()
    {
        var poco = new OrderedPoco { Zebra = "z", Alpha = 1, Beta = true };
        var result = HumlSerializer.Serialize(poco, HumlOptions.Default);

        // Zebra must appear before Alpha, Alpha before Beta
        var zebraIdx = result.IndexOf("Zebra:", StringComparison.Ordinal);
        var alphaIdx = result.IndexOf("Alpha:", StringComparison.Ordinal);
        var betaIdx = result.IndexOf("Beta:", StringComparison.Ordinal);

        zebraIdx.Should().BeLessThan(alphaIdx);
        alphaIdx.Should().BeLessThan(betaIdx);
    }

    // ── Scalar type emission ──────────────────────────────────────────────────

    [Fact]
    public void Serialize_StringProperty_EmitsQuotedValue()
    {
        var poco = new MixedStringPoco { Text = "hello" };
        var result = HumlSerializer.Serialize(poco, HumlOptions.Default);

        result.Should().Contain("Text: \"hello\"\n");
    }

    [Fact]
    public void Serialize_NullStringProperty_EmitsNull()
    {
        var poco = new MixedStringPoco { Text = null };
        var result = HumlSerializer.Serialize(poco, HumlOptions.Default);

        result.Should().Contain("Text: null\n");
    }

    [Fact]
    public void Serialize_BoolTrue_EmitsLowercase()
    {
        var poco = new OrderedPoco { Beta = true };
        var result = HumlSerializer.Serialize(poco, HumlOptions.Default);

        result.Should().Contain("Beta: true\n");
    }

    [Fact]
    public void Serialize_BoolFalse_EmitsLowercase()
    {
        var poco = new OrderedPoco { Beta = false };
        var result = HumlSerializer.Serialize(poco, HumlOptions.Default);

        result.Should().Contain("Beta: false\n");
    }

    [Fact]
    public void Serialize_IntProperty_EmitsBareInteger()
    {
        var poco = new OrderedPoco { Alpha = 42 };
        var result = HumlSerializer.Serialize(poco, HumlOptions.Default);

        result.Should().Contain("Alpha: 42\n");
    }

    [Fact]
    public void Serialize_DoubleProperty_EmitsDecimalLiteral()
    {
        var poco = new InnerPoco { Label = "x", Count = 0 };
        // Use direct object serialization with a double via anonymous-style dict test
        // Instead use typed overload approach
        var result = HumlSerializer.Serialize(new { Val = 3.14 }, HumlOptions.Default);

        result.Should().Contain("Val: 3.14\n");
    }

    [Fact]
    public void Serialize_DoubleNaN_EmitsNan()
    {
        var result = HumlSerializer.Serialize(new { Val = double.NaN }, HumlOptions.Default);

        result.Should().Contain("Val: nan\n");
    }

    [Fact]
    public void Serialize_DoublePositiveInfinity_EmitsPlusInf()
    {
        var result = HumlSerializer.Serialize(new { Val = double.PositiveInfinity }, HumlOptions.Default);

        result.Should().Contain("Val: +inf\n");
    }

    [Fact]
    public void Serialize_DoubleNegativeInfinity_EmitsMinusInf()
    {
        var result = HumlSerializer.Serialize(new { Val = double.NegativeInfinity }, HumlOptions.Default);

        result.Should().Contain("Val: -inf\n");
    }

    [Fact]
    public void Serialize_FloatProperty_EmitsDecimalLiteral()
    {
        var result = HumlSerializer.Serialize(new { Val = 1.5f }, HumlOptions.Default);

        result.Should().Contain("Val: 1.5\n");
    }

    [Fact]
    public void Serialize_LongProperty_EmitsBareInteger()
    {
        var result = HumlSerializer.Serialize(new { Val = 9999999999L }, HumlOptions.Default);

        result.Should().Contain("Val: 9999999999\n");
    }

    // ── String escape tests ───────────────────────────────────────────────────

    [Fact]
    public void Serialize_StringWithQuote_EscapesQuote()
    {
        var poco = new MixedStringPoco { Text = "say \"hi\"" };
        var result = HumlSerializer.Serialize(poco, HumlOptions.Default);

        result.Should().Contain("Text: \"say \\\"hi\\\"\"\n");
    }

    [Fact]
    public void Serialize_StringWithNewline_EscapesNewline()
    {
        var poco = new MixedStringPoco { Text = "line1\nline2" };
        var result = HumlSerializer.Serialize(poco, HumlOptions.Default);

        result.Should().Contain("Text: \"line1\\nline2\"\n");
    }

    [Fact]
    public void Serialize_StringWithBackslash_EscapesBackslash()
    {
        var poco = new MixedStringPoco { Text = @"C:\path" };
        var result = HumlSerializer.Serialize(poco, HumlOptions.Default);

        result.Should().Contain("Text: \"C:\\\\path\"\n");
    }

    [Fact]
    public void Serialize_StringWithTab_EscapesTab()
    {
        var poco = new MixedStringPoco { Text = "col1\tcol2" };
        var result = HumlSerializer.Serialize(poco, HumlOptions.Default);

        result.Should().Contain("Text: \"col1\\tcol2\"\n");
    }

    // ── [HumlProperty] rename ─────────────────────────────────────────────────

    [Fact]
    public void Serialize_HumlPropertyRename_UsesCustomKey()
    {
        var poco = new RenamedPoco { Name = "Alice" };
        var result = HumlSerializer.Serialize(poco, HumlOptions.Default);

        result.Should().Contain("custom_key: \"Alice\"\n");
        result.Should().NotContain("Name:");
    }

    // ── [HumlIgnore] ──────────────────────────────────────────────────────────

    [Fact]
    public void Serialize_HumlIgnore_ExcludesProperty()
    {
        var poco = new IgnoredPoco { Visible = "yes", Hidden = "no" };
        var result = HumlSerializer.Serialize(poco, HumlOptions.Default);

        result.Should().Contain("Visible:");
        result.Should().NotContain("Hidden:");
    }

    // ── OmitIfDefault ─────────────────────────────────────────────────────────

    [Fact]
    public void Serialize_OmitIfDefault_SkipsDefaultValue()
    {
        var poco = new OmitDefaultPoco { Value = 0, Name = "test" };
        var result = HumlSerializer.Serialize(poco, HumlOptions.Default);

        result.Should().NotContain("Value:");
        result.Should().Contain("Name:");
    }

    [Fact]
    public void Serialize_OmitIfDefault_EmitsNonDefaultValue()
    {
        var poco = new OmitDefaultPoco { Value = 5, Name = "test" };
        var result = HumlSerializer.Serialize(poco, HumlOptions.Default);

        result.Should().Contain("Value: 5\n");
    }

    // ── Collection emission ───────────────────────────────────────────────────

    [Fact]
    public void Serialize_EmptyList_EmitsEmptyVectorLiteral()
    {
        var poco = new ListPoco { Items = new List<string>() };
        var result = HumlSerializer.Serialize(poco, HumlOptions.Default);

        result.Should().Contain("Items:: []\n");
    }

    [Fact]
    public void Serialize_ListWithItems_EmitsMultilineBlock()
    {
        var poco = new ListPoco { Items = new List<string> { "alpha", "beta" } };
        var result = HumlSerializer.Serialize(poco, HumlOptions.Default);

        result.Should().Contain("Items::\n");
        result.Should().Contain("  - \"alpha\"\n");
        result.Should().Contain("  - \"beta\"\n");
    }

    [Fact]
    public void Serialize_ArrayWithItems_EmitsMultilineBlock()
    {
        var poco = new ArrayPoco { Items = new[] { "x", "y" } };
        var result = HumlSerializer.Serialize(poco, HumlOptions.Default);

        result.Should().Contain("Items::\n");
        result.Should().Contain("  - \"x\"\n");
        result.Should().Contain("  - \"y\"\n");
    }

    [Fact]
    public void Serialize_EmptyDictionary_EmitsEmptyDictLiteral()
    {
        var poco = new DictPoco { Map = new Dictionary<string, string>() };
        var result = HumlSerializer.Serialize(poco, HumlOptions.Default);

        result.Should().Contain("Map:: {}\n");
    }

    [Fact]
    public void Serialize_DictionaryWithEntries_EmitsMultilineBlock()
    {
        var poco = new DictPoco { Map = new Dictionary<string, string> { ["key1"] = "val1" } };
        var result = HumlSerializer.Serialize(poco, HumlOptions.Default);

        result.Should().Contain("Map::\n");
        result.Should().Contain("  key1: \"val1\"\n");
    }

    // ── Nested POCO ───────────────────────────────────────────────────────────

    [Fact]
    public void Serialize_NestedPoco_EmitsIndentedBlock()
    {
        var poco = new NestedPoco
        {
            Title = "root",
            Inner = new InnerPoco { Label = "child", Count = 7 }
        };
        var result = HumlSerializer.Serialize(poco, HumlOptions.Default);

        result.Should().Contain("Title: \"root\"\n");
        result.Should().Contain("Inner::\n");
        result.Should().Contain("  Label: \"child\"\n");
        result.Should().Contain("  Count: 7\n");
    }

    [Fact]
    public void Serialize_NullNestedPoco_EmitsNull()
    {
        var poco = new NestedPoco { Title = "root", Inner = null };
        var result = HumlSerializer.Serialize(poco, HumlOptions.Default);

        result.Should().Contain("Inner: null\n");
    }

    // ── Two-space indentation ─────────────────────────────────────────────────

    [Fact]
    public void Serialize_TwoSpaceIndentationPerLevel()
    {
        var poco = new NestedPoco
        {
            Title = "t",
            Inner = new InnerPoco { Label = "l", Count = 1 }
        };
        var result = HumlSerializer.Serialize(poco, HumlOptions.Default);

        // Level 1 properties should be two spaces indented
        result.Should().Contain("  Label:");
        result.Should().Contain("  Count:");
    }

    // ── Unsupported type ──────────────────────────────────────────────────────

    [Fact]
    public void Serialize_DelegateType_ThrowsHumlSerializeException()
    {
        Action<int> action = _ => { };
        var act = () => HumlSerializer.Serialize(new { Handler = action }, HumlOptions.Default);

        act.Should().Throw<HumlSerializeException>();
    }

    // ── Newline format ────────────────────────────────────────────────────────

    [Fact]
    public void Serialize_UsesUnixNewlines()
    {
        var poco = new OrderedPoco { Zebra = "z", Alpha = 1, Beta = true };
        var result = HumlSerializer.Serialize(poco, HumlOptions.Default);

        // No Windows-style \r\n
        result.Should().NotContain("\r\n");
    }

    // ── Typed overload ────────────────────────────────────────────────────────

    [Fact]
    public void Serialize_TypedOverload_ProducesSameOutput()
    {
        var poco = new OrderedPoco { Zebra = "z", Alpha = 1, Beta = true };
        var resultUntyped = HumlSerializer.Serialize((object)poco, HumlOptions.Default);
        var resultTyped = HumlSerializer.Serialize(poco, typeof(OrderedPoco), HumlOptions.Default);

        resultTyped.Should().Be(resultUntyped);
    }
}
