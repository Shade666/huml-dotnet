using AwesomeAssertions;
using Huml.Net.Exceptions;
using Huml.Net.Serialization;
using Xunit;

namespace Huml.Net.Tests.Serialization;

public class HumlDeserializerTests
{
    // ── Test POCOs ────────────────────────────────────────────────────────────

    private class SimplePoco
    {
        public string? Name { get; set; }
        public int Count { get; set; }
        public bool Active { get; set; }
    }

    private class RenamedPoco
    {
        [HumlProperty("custom")]
        public string? Value { get; set; }
    }

    private class IgnoredPoco
    {
        public string? Visible { get; set; }

        [HumlIgnore]
        public string? Hidden { get; set; } = "original";
    }

    private class InitOnlyPoco
    {
        public string? Name { get; init; }
    }

    private class CollectionPoco
    {
        public List<int>? Numbers { get; set; }
        public string[]? Tags { get; set; }
        public IEnumerable<string>? Items { get; set; }
        public Dictionary<string, int>? Scores { get; set; }
    }

    private class NestedPoco
    {
        public string? Label { get; set; }
        public SimplePoco? Inner { get; set; }
    }

    private class SpecialValuesPoco
    {
        public double NanValue { get; set; }
        public double PosInfValue { get; set; }
        public double NegInfValue { get; set; }
        public double BareInfValue { get; set; }
    }

    private class NoDefaultCtorPoco
    {
        public string? Name { get; set; }

        public NoDefaultCtorPoco(string name)
        {
            Name = name;
        }
    }

    private class NullablePoco
    {
        public int? NullableInt { get; set; }
        public string? NullableString { get; set; }
    }

    // ── Constructor: test isolation ───────────────────────────────────────────

    public HumlDeserializerTests()
    {
        PropertyDescriptor.ClearCache();
    }

    // ── Basic POCO deserialization ────────────────────────────────────────────

    [Fact]
    public void Deserialize_SimplePoco_MapsCorrectPropertyValues()
    {
        const string huml = """
            Name: "Alice"
            Count: 42
            Active: true
            """;

        var result = HumlDeserializer.Deserialize<SimplePoco>(huml);

        result.Name.Should().Be("Alice");
        result.Count.Should().Be(42);
        result.Active.Should().BeTrue();
    }

    [Fact]
    public void Deserialize_WithHumlPropertyRename_MapsRenamedKey()
    {
        const string huml = """
            custom: "hello"
            """;

        var result = HumlDeserializer.Deserialize<RenamedPoco>(huml);

        result.Value.Should().Be("hello");
    }

    [Fact]
    public void Deserialize_WithHumlIgnore_DoesNotSetIgnoredProperty()
    {
        const string huml = """
            Visible: "shown"
            Hidden: "attempt"
            """;

        var result = HumlDeserializer.Deserialize<IgnoredPoco>(huml);

        result.Visible.Should().Be("shown");
        result.Hidden.Should().Be("original");
    }

    [Fact]
    public void Deserialize_UnknownKey_IsIgnoredSilently()
    {
        const string huml = """
            Name: "Bob"
            UnknownKey: "ignored"
            Count: 7
            Active: false
            """;

        var result = HumlDeserializer.Deserialize<SimplePoco>(huml);

        result.Name.Should().Be("Bob");
        result.Count.Should().Be(7);
    }

    // ── Init-only properties ──────────────────────────────────────────────────

    [Fact]
    public void Deserialize_InitOnlyProperty_ThrowsHumlDeserializeException()
    {
        const string huml = """
            Name: "test"
            """;

        var act = () => HumlDeserializer.Deserialize<InitOnlyPoco>(huml);

        act.Should().Throw<HumlDeserializeException>()
            .WithMessage("*Name*");
    }

    // ── Collections ───────────────────────────────────────────────────────────

    [Fact]
    public void Deserialize_ListOfInt_ReturnsList()
    {
        const string huml = """
            Numbers::
              - 1
              - 2
              - 3
            """;

        var result = HumlDeserializer.Deserialize<CollectionPoco>(huml);

        result.Numbers.Should().Equal(1, 2, 3);
    }

    [Fact]
    public void Deserialize_StringArray_ReturnsArray()
    {
        const string huml = """
            Tags::
              - "alpha"
              - "beta"
            """;

        var result = HumlDeserializer.Deserialize<CollectionPoco>(huml);

        result.Tags.Should().Equal("alpha", "beta");
    }

    [Fact]
    public void Deserialize_IEnumerableOfString_ReturnsMaterializedCollection()
    {
        const string huml = """
            Items::
              - "x"
              - "y"
            """;

        var result = HumlDeserializer.Deserialize<CollectionPoco>(huml);

        result.Items.Should().Equal("x", "y");
    }

    [Fact]
    public void Deserialize_DictionaryStringInt_ReturnsDictionary()
    {
        const string huml = """
            Scores::
              Alice: 100
              Bob: 85
            """;

        var result = HumlDeserializer.Deserialize<CollectionPoco>(huml);

        result.Scores.Should().ContainKey("Alice").WhoseValue.Should().Be(100);
        result.Scores.Should().ContainKey("Bob").WhoseValue.Should().Be(85);
    }

    // ── Nested POCOs ──────────────────────────────────────────────────────────

    [Fact]
    public void Deserialize_NestedPoco_DeserializesRecursively()
    {
        const string huml = """
            Label: "outer"
            Inner::
              Name: "inner-name"
              Count: 99
              Active: true
            """;

        var result = HumlDeserializer.Deserialize<NestedPoco>(huml);

        result.Label.Should().Be("outer");
        result.Inner.Should().NotBeNull();
        result.Inner!.Name.Should().Be("inner-name");
        result.Inner.Count.Should().Be(99);
        result.Inner.Active.Should().BeTrue();
    }

    // ── Special float values ──────────────────────────────────────────────────

    [Fact]
    public void Deserialize_NaN_ReturnsDoubleNaN()
    {
        const string huml = "NanValue: nan";

        var result = HumlDeserializer.Deserialize<SpecialValuesPoco>(huml);

        double.IsNaN(result.NanValue).Should().BeTrue();
    }

    [Fact]
    public void Deserialize_PositiveInf_ReturnsDoublePositiveInfinity()
    {
        const string huml = "PosInfValue: +inf";

        var result = HumlDeserializer.Deserialize<SpecialValuesPoco>(huml);

        result.PosInfValue.Should().Be(double.PositiveInfinity);
    }

    [Fact]
    public void Deserialize_NegativeInf_ReturnsDoubleNegativeInfinity()
    {
        const string huml = "NegInfValue: -inf";

        var result = HumlDeserializer.Deserialize<SpecialValuesPoco>(huml);

        result.NegInfValue.Should().Be(double.NegativeInfinity);
    }

    [Fact]
    public void Deserialize_BareInf_ReturnsDoublePositiveInfinity()
    {
        const string huml = "BareInfValue: inf";

        var result = HumlDeserializer.Deserialize<SpecialValuesPoco>(huml);

        result.BareInfValue.Should().Be(double.PositiveInfinity);
    }

    // ── Null scalars ──────────────────────────────────────────────────────────

    [Fact]
    public void Deserialize_NullScalar_AssignsNullToNullableInt()
    {
        const string huml = "NullableInt: null";

        var result = HumlDeserializer.Deserialize<NullablePoco>(huml);

        result.NullableInt.Should().BeNull();
    }

    [Fact]
    public void Deserialize_NullScalar_AssignsNullToNullableString()
    {
        const string huml = "NullableString: null";

        var result = HumlDeserializer.Deserialize<NullablePoco>(huml);

        result.NullableString.Should().BeNull();
    }

    [Fact]
    public void Deserialize_NullToNonNullableValueType_ThrowsHumlDeserializeException()
    {
        const string huml = "Count: null";

        var act = () => HumlDeserializer.Deserialize<SimplePoco>(huml);

        act.Should().Throw<HumlDeserializeException>()
            .WithMessage("*null*");
    }

    // ── No parameterless constructor ──────────────────────────────────────────

    [Fact]
    public void Deserialize_TypeWithNoParameterlessCtor_ThrowsHumlDeserializeException()
    {
        const string huml = "Name: \"test\"";

        var act = () => HumlDeserializer.Deserialize<NoDefaultCtorPoco>(huml);

        act.Should().Throw<HumlDeserializeException>()
            .WithMessage("*NoDefaultCtorPoco*");
    }

    // ── Type coercion errors ──────────────────────────────────────────────────

    [Fact]
    public void Deserialize_StringToIntProperty_ThrowsHumlDeserializeException()
    {
        const string huml = "Count: \"not-a-number\"";

        var act = () => HumlDeserializer.Deserialize<SimplePoco>(huml);

        act.Should().Throw<HumlDeserializeException>();
    }

    // ── Untyped overload ──────────────────────────────────────────────────────

    [Fact]
    public void Deserialize_UntypedOverload_ReturnsCorrectObject()
    {
        const string huml = """
            Name: "Charlie"
            Count: 5
            Active: false
            """;

        var result = HumlDeserializer.Deserialize(huml, typeof(SimplePoco));

        result.Should().BeOfType<SimplePoco>();
        var poco = (SimplePoco)result!;
        poco.Name.Should().Be("Charlie");
        poco.Count.Should().Be(5);
    }

    // ── Empty collections ─────────────────────────────────────────────────────

    [Fact]
    public void Deserialize_EmptyDoc_ReturnsDefaultConstructedPoco()
    {
        // An empty dict {} maps to a default-constructed POCO
        const string huml = "{}";

        var result = HumlDeserializer.Deserialize<SimplePoco>(huml);

        result.Should().NotBeNull();
        result.Name.Should().BeNull();
        result.Count.Should().Be(0);
    }
}
