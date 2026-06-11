using AwesomeAssertions;
using Huml.Net.Serialization;
using Huml.Net.Versioning;
using Xunit;

namespace Huml.Net.Tests.Serialization;

public class MemberNamingPolicyTests
{
    // ── Test POCOs ────────────────────────────────────────────────────────────

    private class MNP01Dto
    {
        [HumlNamingPolicy(HumlKnownNamingPolicy.KebabCase)]
        public string? FullName { get; set; }
    }

    private class MNP02Dto
    {
        [HumlNamingPolicy(HumlKnownNamingPolicy.SnakeCase)]
        public string? FullName { get; set; }
        public int MaxDepth { get; set; }
    }

    private class MNP03Dto
    {
        [HumlNamingPolicy(HumlKnownNamingPolicy.CamelCase)]
        public string? FullName { get; set; }
    }

    private class MNP04Dto
    {
        [HumlNamingPolicy(HumlKnownNamingPolicy.PascalCase)]
        public string? camelProp { get; set; }
    }

    private class MNP05Dto
    {
        [HumlNamingPolicy(HumlKnownNamingPolicy.Unspecified)]
        public string? FullName { get; set; }
    }

    private class MNP06Dto
    {
        [HumlProperty("explicit")]
        [HumlNamingPolicy(HumlKnownNamingPolicy.KebabCase)]
        public string? FullName { get; set; }
    }

    private class MNP07Dto
    {
        [HumlNamingPolicy(HumlKnownNamingPolicy.SnakeCase)]
        public string? FullName { get; set; }
    }

    private class MNP08Dto
    {
        public string? FullName { get; set; }
    }

    // ── Tests ─────────────────────────────────────────────────────────────────

    [Fact]
    public void MNP01_KebabCase_MemberPolicy_NoGlobalPolicy_SerializesKebabKey()
    {
        PropertyDescriptor.ClearCache();
        var result = HumlSerializer.Serialize(new MNP01Dto { FullName = "Alice" });
        result.Should().Contain("full-name:");
    }

    [Fact]
    public void MNP02_SnakeCase_MemberPolicy_OverridesGlobalKebabCase_ForAnnotatedProperty()
    {
        PropertyDescriptor.ClearCache();
        var options = new HumlOptions { PropertyNamingPolicy = HumlNamingPolicy.KebabCase };
        var result = HumlSerializer.Serialize(new MNP02Dto { FullName = "Bob", MaxDepth = 5 }, options);
        result.Should().Contain("full_name:");   // member policy wins for FullName
        result.Should().Contain("max-depth:");   // global kebab-case applies to MaxDepth
    }

    [Fact]
    public void MNP03_CamelCase_MemberPolicy_SerializesCamelKey()
    {
        PropertyDescriptor.ClearCache();
        var result = HumlSerializer.Serialize(new MNP03Dto { FullName = "Carol" });
        result.Should().Contain("fullName:");
    }

    [Fact]
    public void MNP04_PascalCase_MemberPolicy_OnCamelCaseCSharpName_SerializesPascalKey()
    {
        PropertyDescriptor.ClearCache();
        var result = HumlSerializer.Serialize(new MNP04Dto { camelProp = "test" });
        result.Should().Contain("CamelProp:");
    }

    [Fact]
    public void MNP05_Unspecified_MemberPolicy_DefersToGlobalKebabCase()
    {
        PropertyDescriptor.ClearCache();
        var options = new HumlOptions { PropertyNamingPolicy = HumlNamingPolicy.KebabCase };
        var result = HumlSerializer.Serialize(new MNP05Dto { FullName = "Dave" }, options);
        result.Should().Contain("full-name:");   // Unspecified defers to global kebab-case
    }

    [Fact]
    public void MNP06_ExplicitHumlPropertyName_WinsOverMemberNamingPolicy()
    {
        PropertyDescriptor.ClearCache();
        var result = HumlSerializer.Serialize(new MNP06Dto { FullName = "Eve" });
        result.Should().Contain("explicit:");
        result.Should().NotContain("full-name:");
    }

    [Fact]
    public void MNP07_RoundTrip_SnakeCase_MemberPolicy_PreservesValue()
    {
        PropertyDescriptor.ClearCache();
        var original = new MNP07Dto { FullName = "Frank" };
        var huml = HumlSerializer.Serialize(original, HumlOptions.LatestSupported);
        // Serialised with member snake_case policy, deserialized with same options
        var restored = HumlSerializer.Deserialize<MNP07Dto>(huml, HumlOptions.LatestSupported);
        restored!.FullName.Should().Be("Frank");
    }

    [Fact]
    public void MNP08_NoAttribute_NullGlobalPolicy_UsesIdentityName()
    {
        PropertyDescriptor.ClearCache();
        var result = HumlSerializer.Serialize(new MNP08Dto { FullName = "Grace" });
        result.Should().Contain("FullName:");
    }
}
