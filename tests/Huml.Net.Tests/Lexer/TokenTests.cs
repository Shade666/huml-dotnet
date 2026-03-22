using AwesomeAssertions;
using Huml.Net.Lexer;
using Xunit;

namespace Huml.Net.Tests.Lexer;

public class TokenTests
{
    [Fact]
    public void Token_is_value_type()
    {
        typeof(Token).IsValueType.Should().BeTrue();
    }

    [Fact]
    public void Token_can_be_constructed_with_all_properties()
    {
        var token = new Token
        {
            Type = TokenType.Key,
            Value = "foo",
            Line = 1,
            Column = 0,
            Indent = 0,
            SpaceBefore = false,
        };

        token.Type.Should().Be(TokenType.Key);
        token.Value.Should().Be("foo");
        token.Line.Should().Be(1);
        token.Column.Should().Be(0);
        token.Indent.Should().Be(0);
        token.SpaceBefore.Should().BeFalse();
    }

    [Fact]
    public void Equal_tokens_are_equal()
    {
        var a = new Token { Type = TokenType.Key, Value = "foo", Line = 1, Column = 0, Indent = 0, SpaceBefore = false };
        var b = new Token { Type = TokenType.Key, Value = "foo", Line = 1, Column = 0, Indent = 0, SpaceBefore = false };

        a.Should().Be(b);
        (a == b).Should().BeTrue();
    }

    [Fact]
    public void Different_tokens_are_not_equal()
    {
        var a = new Token { Type = TokenType.Key, Value = "foo", Line = 1, Column = 0, Indent = 0, SpaceBefore = false };
        var b = new Token { Type = TokenType.String, Value = "foo", Line = 1, Column = 0, Indent = 0, SpaceBefore = false };

        (a == b).Should().BeFalse();
    }

    [Fact]
    public void Structural_token_has_null_value()
    {
        new Token { Type = TokenType.Eof }.Value.Should().BeNull();
    }

    [Fact]
    public void Value_token_has_non_null_value()
    {
        new Token { Type = TokenType.Key, Value = "test" }.Value.Should().Be("test");
    }
}
