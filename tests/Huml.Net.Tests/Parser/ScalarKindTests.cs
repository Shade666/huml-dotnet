using System;
using AwesomeAssertions;
using Huml.Net.Parser;
using Xunit;

namespace Huml.Net.Tests.Parser;

public class ScalarKindTests
{
    [Fact]
    public void Enum_has_exactly_7_members()
    {
        Enum.GetValues(typeof(ScalarKind)).Length.Should().Be(7);
    }

    [Theory]
    [InlineData("String")]
    [InlineData("Integer")]
    [InlineData("Float")]
    [InlineData("Bool")]
    [InlineData("Null")]
    [InlineData("NaN")]
    [InlineData("Inf")]
    public void All_expected_members_exist(string name)
    {
        Enum.IsDefined(typeof(ScalarKind), name).Should().BeTrue();
    }

    [Fact]
    public void Default_value_is_String()
    {
        default(ScalarKind).Should().Be(ScalarKind.String);
    }
}
