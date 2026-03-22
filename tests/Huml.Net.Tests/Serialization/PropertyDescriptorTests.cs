using AwesomeAssertions;
using Huml.Net.Serialization;
using Xunit;

// Alias to avoid potential namespace ambiguities
using HumlPropertyAttribute = Huml.Net.Serialization.HumlPropertyAttribute;
using HumlIgnoreAttribute = Huml.Net.Serialization.HumlIgnoreAttribute;

namespace Huml.Net.Tests.Serialization;

public class PropertyDescriptorTests
{
    // ── Test POCOs ────────────────────────────────────────────────────────────

    private class SimplePoco
    {
        public string? Zebra { get; set; }
        public int Alpha { get; set; }
        public bool Middle { get; set; }
    }

    private class IgnoredPoco
    {
        public string? Visible { get; set; }
        [HumlIgnore]
        public string? Hidden { get; set; }
    }

    private class RenamedPoco
    {
        [HumlProperty("custom_name")]
        public string? Original { get; set; }
    }

    private class InitOnlyPoco
    {
        public string? Name { get; init; }
    }

    private class InheritedPoco : SimplePoco
    {
        public string? Extra { get; set; }
    }

    private class OmitDefaultPoco
    {
        [HumlProperty(OmitIfDefault = true)]
        public int Value { get; set; }
    }

    // ── Tests ─────────────────────────────────────────────────────────────────

    public PropertyDescriptorTests()
    {
        // Clear cache before each test for isolation
        PropertyDescriptor.ClearCache();
    }

    [Fact]
    public void GetDescriptors_SimplePoco_ReturnsInDeclarationOrder()
    {
        var descriptors = PropertyDescriptor.GetDescriptors(typeof(SimplePoco));

        descriptors.Should().HaveCount(3);
        descriptors[0].HumlKey.Should().Be("Zebra");
        descriptors[1].HumlKey.Should().Be("Alpha");
        descriptors[2].HumlKey.Should().Be("Middle");
    }

    [Fact]
    public void GetDescriptors_ExcludesHumlIgnoreProperties()
    {
        var descriptors = PropertyDescriptor.GetDescriptors(typeof(IgnoredPoco));

        descriptors.Should().HaveCount(1);
        descriptors[0].HumlKey.Should().Be("Visible");
    }

    [Fact]
    public void GetDescriptors_ResolvesHumlPropertyName()
    {
        var descriptors = PropertyDescriptor.GetDescriptors(typeof(RenamedPoco));

        descriptors.Should().HaveCount(1);
        descriptors[0].HumlKey.Should().Be("custom_name");
    }

    [Fact]
    public void GetDescriptors_DetectsInitOnlyProperty()
    {
        var descriptors = PropertyDescriptor.GetDescriptors(typeof(InitOnlyPoco));

        descriptors.Should().HaveCount(1);
        descriptors[0].IsInitOnly.Should().BeTrue();
    }

    [Fact]
    public void GetDescriptors_RegularProperty_IsNotInitOnly()
    {
        var descriptors = PropertyDescriptor.GetDescriptors(typeof(SimplePoco));

        descriptors[0].IsInitOnly.Should().BeFalse();
    }

    [Fact]
    public void GetDescriptors_ReturnsSameArrayReferenceOnSecondCall()
    {
        var first = PropertyDescriptor.GetDescriptors(typeof(SimplePoco));
        var second = PropertyDescriptor.GetDescriptors(typeof(SimplePoco));

        object.ReferenceEquals(first, second).Should().BeTrue();
    }

    [Fact]
    public void GetDescriptors_InheritedPoco_ReturnsBasePropertiesBeforeDerived()
    {
        var descriptors = PropertyDescriptor.GetDescriptors(typeof(InheritedPoco));

        // Base class (SimplePoco) properties first, then InheritedPoco's Extra
        descriptors.Should().HaveCount(4);
        descriptors[0].HumlKey.Should().Be("Zebra");
        descriptors[1].HumlKey.Should().Be("Alpha");
        descriptors[2].HumlKey.Should().Be("Middle");
        descriptors[3].HumlKey.Should().Be("Extra");
    }

    [Fact]
    public void GetDescriptors_OmitIfDefault_ReflectsAttribute()
    {
        var descriptors = PropertyDescriptor.GetDescriptors(typeof(OmitDefaultPoco));

        descriptors.Should().HaveCount(1);
        descriptors[0].OmitIfDefault.Should().BeTrue();
    }

    [Fact]
    public void GetDescriptors_DefaultProperty_OmitIfDefaultIsFalse()
    {
        var descriptors = PropertyDescriptor.GetDescriptors(typeof(SimplePoco));

        descriptors[0].OmitIfDefault.Should().BeFalse();
    }
}
