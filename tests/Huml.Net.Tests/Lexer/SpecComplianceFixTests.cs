using AwesomeAssertions;
using Huml.Net.Exceptions;
using Huml.Net.Parser;
using Huml.Net.Versioning;
using Xunit;

namespace Huml.Net.Tests.Lexer;

/// <summary>
/// Regression tests for the deviations fixed during the G1.3 spec-compliance sweep
/// (see docs/spec-compliance-report.md). Each region cites the spec rule it enforces.
/// </summary>
public class SpecComplianceFixTests
{
    // ── Exception contract: digitless base prefixes must not leak FormatException ──

    [Theory]
    [InlineData("key: 0x")]
    [InlineData("key: 0x\nfoo: 1")]
    [InlineData("key: 0b2")]
    [InlineData("key: 0o8")]
    [InlineData("key: 0x_")]
    public void Digitless_base_prefix_throws_HumlParseException(string input)
    {
        var act = () => Huml.Parse(input, HumlOptions.LatestSupported);
        act.Should().Throw<HumlParseException>();
    }

    // ── Tokenizer: hex digits ['0'-'9' 'A'-'F' '_'], exp digits ['0'-'9' '_'] ──

    [Fact]
    public void Hex_literal_with_underscores_parses()
    {
        var doc = Huml.Parse("key: 0xCAFE_BABE", HumlOptions.LatestSupported);
        var scalar = ((HumlMapping)doc.Entries[0]).Value.Should().BeOfType<HumlScalar>().Subject;
        scalar.Value.Should().Be(0xCAFEBABEL);
    }

    [Fact]
    public void Exponent_with_underscores_parses()
    {
        var doc = Huml.Parse("key: 1e1_0", HumlOptions.LatestSupported);
        var scalar = ((HumlMapping)doc.Entries[0]).Value.Should().BeOfType<HumlScalar>().Subject;
        scalar.Value.Should().Be(1e10d);
    }

    [Theory]
    [InlineData("key: 0o7_5_5", 0x1ED)]
    [InlineData("key: 0b1010_1010", 0xAA)]
    public void Octal_and_binary_with_underscores_parse(string input, long expected)
    {
        var doc = Huml.Parse(input, HumlOptions.LatestSupported);
        var scalar = ((HumlMapping)doc.Entries[0]).Value.Should().BeOfType<HumlScalar>().Subject;
        scalar.Value.Should().Be(expected);
    }

    // ── Tokenizer: list items are "- " (dash-space); bare "-N" is a number ──

    [Fact]
    public void Root_negative_integer_is_a_scalar_not_a_list()
    {
        var doc = Huml.Parse("-5", HumlOptions.LatestSupported);
        doc.Entries.Should().HaveCount(1);
        var scalar = doc.Entries[0].Should().BeOfType<HumlScalar>().Subject;
        scalar.Kind.Should().Be(ScalarKind.Integer);
        scalar.Value.Should().Be(-5L);
    }

    [Fact]
    public void Root_negative_inf_is_a_scalar_not_a_list()
    {
        var doc = Huml.Parse("-inf", HumlOptions.LatestSupported);
        doc.Entries.Should().HaveCount(1);
        var scalar = doc.Entries[0].Should().BeOfType<HumlScalar>().Subject;
        scalar.Kind.Should().Be(ScalarKind.Inf);
    }

    [Fact]
    public void List_item_dash_without_space_throws()
    {
        var act = () => Huml.Parse("list::\n  -1", HumlOptions.LatestSupported);
        act.Should().Throw<HumlParseException>();
    }

    [Fact]
    public void List_item_dash_with_space_still_parses()
    {
        var act = () => Huml.Parse("list::\n  - 1", HumlOptions.LatestSupported);
        act.Should().NotThrow();
    }

    // ── Keyword literals are lowercase (spec) and case-sensitive (go-huml reference) ──

    [Theory]
    [InlineData("key: TRUE")]
    [InlineData("key: True")]
    [InlineData("key: FALSE")]
    [InlineData("key: NULL")]
    [InlineData("key: NaN")]
    [InlineData("key: Inf")]
    [InlineData("key: INF")]
    [InlineData("key: -INF")]
    public void Uppercase_keyword_literals_throw(string input)
    {
        var act = () => Huml.Parse(input, HumlOptions.LatestSupported);
        act.Should().Throw<HumlParseException>();
    }

    [Theory]
    [InlineData("key: true")]
    [InlineData("key: false")]
    [InlineData("key: null")]
    [InlineData("key: nan")]
    [InlineData("key: inf")]
    [InlineData("key: -inf")]
    [InlineData("key: +inf")]
    public void Lowercase_keyword_literals_parse(string input)
    {
        var act = () => Huml.Parse(input, HumlOptions.LatestSupported);
        act.Should().NotThrow();
    }

    // ── Spaces: trailing spaces forbidden on comment lines too ──

    [Theory]
    [InlineData("# comment \nkey: 1")]
    [InlineData("key: 1 # comment \nfoo: 2")]
    [InlineData("# \nkey: 1")]
    public void Comment_with_trailing_spaces_throws(string input)
    {
        var act = () => Huml.Parse(input, HumlOptions.LatestSupported);
        act.Should().Throw<HumlParseException>();
    }

    [Theory]
    [InlineData("# comment\nkey: 1")]
    [InlineData("key: 1 # comment\nfoo: 2")]
    public void Comment_without_trailing_spaces_parses(string input)
    {
        var act = () => Huml.Parse(input, HumlOptions.LatestSupported);
        act.Should().NotThrow();
    }

    // ── v0.1 `"""` strips ALL leading/trailing whitespace per content line ──

    [Fact]
    public void V01_triple_quote_strips_all_leading_and_trailing_whitespace()
    {
        const string input = "%HUML v0.1.0\nkey: \"\"\"\n  Line 1\n   Line 2\n    Line 3\n         All spaces ignored.   \n\"\"\"";
        var doc = Huml.Parse(input, HumlOptions.Default);
        var scalar = ((HumlMapping)doc.Entries[0]).Value.Should().BeOfType<HumlScalar>().Subject;
        scalar.Value.Should().Be("Line 1\nLine 2\nLine 3\nAll spaces ignored.");
    }

    [Fact]
    public void V02_triple_quote_preserves_spaces_beyond_strip_indent()
    {
        const string input = "%HUML v0.2.0\nkey: \"\"\"\n  Line 1\n   Line 2\n\"\"\"";
        var doc = Huml.Parse(input, HumlOptions.Default);
        var scalar = ((HumlMapping)doc.Entries[0]).Value.Should().BeOfType<HumlScalar>().Subject;
        scalar.Value.Should().Be("Line 1\n Line 2");
    }
}
