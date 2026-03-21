using System;
using AwesomeAssertions;
using Huml.Net.Lexer;
using Xunit;

namespace Huml.Net.Tests.Lexer;

public class TokenTypeTests
{
    [Fact]
    public void Enum_has_exactly_18_members()
    {
        Enum.GetValues(typeof(TokenType)).Length.Should().Be(18);
    }

    [Fact]
    public void All_expected_members_exist()
    {
        Enum.IsDefined(typeof(TokenType), "Eof").Should().BeTrue();
        Enum.IsDefined(typeof(TokenType), "Error").Should().BeTrue();
        Enum.IsDefined(typeof(TokenType), "Version").Should().BeTrue();
        Enum.IsDefined(typeof(TokenType), "Key").Should().BeTrue();
        Enum.IsDefined(typeof(TokenType), "QuotedKey").Should().BeTrue();
        Enum.IsDefined(typeof(TokenType), "ScalarIndicator").Should().BeTrue();
        Enum.IsDefined(typeof(TokenType), "VectorIndicator").Should().BeTrue();
        Enum.IsDefined(typeof(TokenType), "ListItem").Should().BeTrue();
        Enum.IsDefined(typeof(TokenType), "Comma").Should().BeTrue();
        Enum.IsDefined(typeof(TokenType), "String").Should().BeTrue();
        Enum.IsDefined(typeof(TokenType), "Int").Should().BeTrue();
        Enum.IsDefined(typeof(TokenType), "Float").Should().BeTrue();
        Enum.IsDefined(typeof(TokenType), "Bool").Should().BeTrue();
        Enum.IsDefined(typeof(TokenType), "Null").Should().BeTrue();
        Enum.IsDefined(typeof(TokenType), "NaN").Should().BeTrue();
        Enum.IsDefined(typeof(TokenType), "Inf").Should().BeTrue();
        Enum.IsDefined(typeof(TokenType), "EmptyList").Should().BeTrue();
        Enum.IsDefined(typeof(TokenType), "EmptyDict").Should().BeTrue();
    }
}
