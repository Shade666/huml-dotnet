using System;
using AwesomeAssertions;
using Huml.Net.Exceptions;
using Xunit;

namespace Huml.Net.Tests.Exceptions;

public class HumlParseExceptionTests
{
    [Fact]
    public void Line_property_returns_constructor_value()
    {
        new HumlParseException("err", 5, 10).Line.Should().Be(5);
    }

    [Fact]
    public void Column_property_returns_constructor_value()
    {
        new HumlParseException("err", 5, 10).Column.Should().Be(10);
    }

    [Fact]
    public void Message_contains_position_prefix()
    {
        new HumlParseException("err", 5, 10).Message.Should().Contain("[5:10]");
    }

    [Fact]
    public void Message_contains_user_message()
    {
        new HumlParseException("err", 5, 10).Message.Should().Contain("err");
    }

    [Fact]
    public void Is_sealed_exception()
    {
        typeof(HumlParseException).IsSealed.Should().BeTrue();
        new HumlParseException("err", 1, 0).Should().BeAssignableTo<Exception>();
    }
}
