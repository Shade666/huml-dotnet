using AwesomeAssertions;
using Huml.Net.Serialization;
using Huml.Net.Versioning;
using Xunit;

namespace Huml.Net.Tests.Serialization;

public class NamingPolicyRoundTripTests
{
    // ── Test POCOs ────────────────────────────────────────────────────────────

    private class KebabPoco
    {
        public string? FullName { get; set; }
        public int MaxDepth { get; set; }
    }

    private class SingleLetterPoco
    {
        public int X { get; set; }
        public string? Y { get; set; }
    }

    private class AttributeOverridePoco
    {
        [HumlProperty("explicit-key")]
        public string? PropertyName { get; set; }
        public int OtherProp { get; set; }
    }

    private class NestedPoco
    {
        public string? OuterName { get; set; }
        public KebabPoco? Inner { get; set; }
    }

    // ── Serialize-only tests (NP-09, NP-12, NP-13, EDGE-01) ──────────────────

    [Fact]
    public void NP09a_Serialize_KebabCase_TransformsPropertyNames()
    {
        var options = new HumlOptions { PropertyNamingPolicy = HumlNamingPolicy.KebabCase };
        var result = HumlSerializer.Serialize(new KebabPoco { FullName = "Alice", MaxDepth = 5 }, options);
        result.Should().Contain("full-name:");
        result.Should().Contain("max-depth:");
    }

    [Fact]
    public void NP09b_Serialize_SnakeCase_TransformsPropertyNames()
    {
        var options = new HumlOptions { PropertyNamingPolicy = HumlNamingPolicy.SnakeCase };
        var result = HumlSerializer.Serialize(new KebabPoco { FullName = "Alice", MaxDepth = 5 }, options);
        result.Should().Contain("full_name:");
        result.Should().Contain("max_depth:");
    }

    [Fact]
    public void NP09c_Serialize_CamelCase_TransformsPropertyNames()
    {
        var options = new HumlOptions { PropertyNamingPolicy = HumlNamingPolicy.CamelCase };
        var result = HumlSerializer.Serialize(new KebabPoco { FullName = "Alice", MaxDepth = 5 }, options);
        result.Should().Contain("fullName:");
        result.Should().Contain("maxDepth:");
    }

    [Fact]
    public void NP09d_Serialize_PascalCase_KeepsPascalCasePropertyNames()
    {
        var options = new HumlOptions { PropertyNamingPolicy = HumlNamingPolicy.PascalCase };
        var result = HumlSerializer.Serialize(new KebabPoco { FullName = "Alice", MaxDepth = 5 }, options);
        result.Should().Contain("FullName:");
        result.Should().Contain("MaxDepth:");
    }

    [Fact]
    public void NP13a_Serialize_HumlPropertyAttributeWins_OverNamingPolicy()
    {
        var options = new HumlOptions { PropertyNamingPolicy = HumlNamingPolicy.KebabCase };
        var result = HumlSerializer.Serialize(new AttributeOverridePoco { PropertyName = "test", OtherProp = 1 }, options);
        // Attribute-specified key wins; policy is NOT applied
        result.Should().Contain("explicit-key:");
        // Policy-transformed name should NOT appear
        result.Should().NotContain("property-name:");
    }

    [Fact]
    public void NP12a_Serialize_NullPolicy_UsesIdentityNames()
    {
        var options = new HumlOptions { PropertyNamingPolicy = null };
        var result = HumlSerializer.Serialize(new KebabPoco { FullName = "Alice", MaxDepth = 5 }, options);
        result.Should().Contain("FullName:");
        result.Should().Contain("MaxDepth:");
    }

    [Fact]
    public void EDGE01_Serialize_KebabCase_SingleLetterProperty_LowercasedCorrectly()
    {
        var options = new HumlOptions { PropertyNamingPolicy = HumlNamingPolicy.KebabCase };
        var result = HumlSerializer.Serialize(new SingleLetterPoco { X = 42, Y = "hello" }, options);
        result.Should().Contain("x:");
        result.Should().Contain("y:");
    }

    // ── Round-trip tests (NP-11, NP-12, NP-13, EDGE-02) ─────────────────────
    // These start RED until Task 2 wires the deserializer.

    [Fact]
    public void NP11a_RoundTrip_KebabCase_PreservesValueEquality()
    {
        var original = new KebabPoco { FullName = "Alice", MaxDepth = 42 };
        var options = new HumlOptions { PropertyNamingPolicy = HumlNamingPolicy.KebabCase };
        var huml = HumlSerializer.Serialize(original, options);
        var restored = HumlSerializer.Deserialize<KebabPoco>(huml, options);
        restored!.FullName.Should().Be("Alice");
        restored.MaxDepth.Should().Be(42);
    }

    [Fact]
    public void NP11b_RoundTrip_SnakeCase_PreservesValueEquality()
    {
        var original = new KebabPoco { FullName = "Bob", MaxDepth = 7 };
        var options = new HumlOptions { PropertyNamingPolicy = HumlNamingPolicy.SnakeCase };
        var huml = HumlSerializer.Serialize(original, options);
        var restored = HumlSerializer.Deserialize<KebabPoco>(huml, options);
        restored!.FullName.Should().Be("Bob");
        restored.MaxDepth.Should().Be(7);
    }

    [Fact]
    public void NP11c_RoundTrip_CamelCase_PreservesValueEquality()
    {
        var original = new KebabPoco { FullName = "Carol", MaxDepth = 3 };
        var options = new HumlOptions { PropertyNamingPolicy = HumlNamingPolicy.CamelCase };
        var huml = HumlSerializer.Serialize(original, options);
        var restored = HumlSerializer.Deserialize<KebabPoco>(huml, options);
        restored!.FullName.Should().Be("Carol");
        restored.MaxDepth.Should().Be(3);
    }

    [Fact]
    public void NP11d_RoundTrip_PascalCase_PreservesValueEquality()
    {
        var original = new KebabPoco { FullName = "Dave", MaxDepth = 10 };
        var options = new HumlOptions { PropertyNamingPolicy = HumlNamingPolicy.PascalCase };
        var huml = HumlSerializer.Serialize(original, options);
        var restored = HumlSerializer.Deserialize<KebabPoco>(huml, options);
        restored!.FullName.Should().Be("Dave");
        restored.MaxDepth.Should().Be(10);
    }

    [Fact]
    public void NP13b_RoundTrip_HumlPropertyAttributeAndKebabCase_Succeeds()
    {
        var original = new AttributeOverridePoco { PropertyName = "val", OtherProp = 99 };
        var options = new HumlOptions { PropertyNamingPolicy = HumlNamingPolicy.KebabCase };
        var huml = HumlSerializer.Serialize(original, options);
        var restored = HumlSerializer.Deserialize<AttributeOverridePoco>(huml, options);
        restored!.PropertyName.Should().Be("val");
        restored.OtherProp.Should().Be(99);
    }

    [Fact]
    public void NP12b_RoundTrip_NullPolicy_Succeeds()
    {
        var original = new KebabPoco { FullName = "Regression", MaxDepth = 1 };
        var options = new HumlOptions { PropertyNamingPolicy = null };
        var huml = HumlSerializer.Serialize(original, options);
        var restored = HumlSerializer.Deserialize<KebabPoco>(huml, options);
        restored!.FullName.Should().Be("Regression");
        restored.MaxDepth.Should().Be(1);
    }

    [Fact]
    public void EDGE02_RoundTrip_KebabCase_NestedPoco_AllLevelsTransformed()
    {
        var original = new NestedPoco
        {
            OuterName = "outer",
            Inner = new KebabPoco { FullName = "inner", MaxDepth = 1 }
        };
        var options = new HumlOptions { PropertyNamingPolicy = HumlNamingPolicy.KebabCase };
        var huml = HumlSerializer.Serialize(original, options);

        // Verify outer key transformed
        huml.Should().Contain("outer-name:");
        // Verify nested inner keys also transformed
        huml.Should().Contain("full-name:");

        var restored = HumlSerializer.Deserialize<NestedPoco>(huml, options);
        restored!.OuterName.Should().Be("outer");
        restored.Inner!.FullName.Should().Be("inner");
        restored.Inner.MaxDepth.Should().Be(1);
    }
}
