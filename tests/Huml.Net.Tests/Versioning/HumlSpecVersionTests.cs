using System.Reflection;
using AwesomeAssertions;
using Huml.Net.Versioning;
using Xunit;

namespace Huml.Net.Tests.Versioning;

public class HumlSpecVersionTests
{
    [Fact]
    public void V0_1_has_backing_value_1()
    {
#pragma warning disable CS0618
        ((int)HumlSpecVersion.V0_1).Should().Be(1);
#pragma warning restore CS0618
    }

    [Fact]
    public void V0_2_has_backing_value_2()
    {
        ((int)HumlSpecVersion.V0_2).Should().Be(2);
    }

    [Fact]
    public void V0_2_is_greater_than_or_equal_to_V0_1()
    {
#pragma warning disable CS0618
        (HumlSpecVersion.V0_2 >= HumlSpecVersion.V0_1).Should().BeTrue();
#pragma warning restore CS0618
    }

    [Fact]
    public void V0_1_is_not_greater_than_or_equal_to_V0_2()
    {
#pragma warning disable CS0618
        (HumlSpecVersion.V0_1 >= HumlSpecVersion.V0_2).Should().BeFalse();
#pragma warning restore CS0618
    }

    [Fact]
    public void V0_1_has_Obsolete_attribute()
    {
#pragma warning disable CS0618
        typeof(HumlSpecVersion)
            .GetField(nameof(HumlSpecVersion.V0_1))!
            .GetCustomAttribute<ObsoleteAttribute>()
            .Should().NotBeNull();
#pragma warning restore CS0618
    }

    [Fact]
    public void V0_2_does_not_have_Obsolete_attribute()
    {
        typeof(HumlSpecVersion)
            .GetField(nameof(HumlSpecVersion.V0_2))!
            .GetCustomAttribute<ObsoleteAttribute>()
            .Should().BeNull();
    }

    [Fact]
    public void Obsolete_message_mentions_V0_2()
    {
#pragma warning disable CS0618
        var attr = typeof(HumlSpecVersion)
            .GetField(nameof(HumlSpecVersion.V0_1))!
            .GetCustomAttribute<ObsoleteAttribute>();
#pragma warning restore CS0618
        attr.Should().NotBeNull();
        attr!.Message.Should().Contain("V0_2");
    }
}
