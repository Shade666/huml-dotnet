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

    // ── B3: empty vectors are the literals "[]" / "{}" — no internal whitespace ──

    [Theory]
    [InlineData("key:: [ ]")]
    [InlineData("key:: {  }")]
    [InlineData("key:: [ }")]
    public void Empty_vector_with_internal_whitespace_throws(string input)
    {
        var act = () => Huml.Parse(input, HumlOptions.LatestSupported);
        act.Should().Throw<HumlParseException>();
    }

    [Theory]
    [InlineData("key:: []")]
    [InlineData("key:: {}")]
    public void Empty_vector_literals_parse(string input)
    {
        var act = () => Huml.Parse(input, HumlOptions.LatestSupported);
        act.Should().NotThrow();
    }

    // ── B4: tokenizer permits a comment between the opening delimiter and the newline ──

    [Fact]
    public void Comment_after_opening_triple_quote_parses()
    {
        var doc = Huml.Parse("key: \"\"\" # note\n  content\n\"\"\"", HumlOptions.LatestSupported);
        var scalar = ((HumlMapping)doc.Entries[0]).Value.Should().BeOfType<HumlScalar>().Subject;
        scalar.Value.Should().Be("content");
    }

    [Fact]
    public void Comment_after_opening_backticks_parses_in_v01()
    {
        var doc = Huml.Parse("%HUML v0.1.0\nkey: ``` # note\n  content\n```", HumlOptions.Default);
        var scalar = ((HumlMapping)doc.Entries[0]).Value.Should().BeOfType<HumlScalar>().Subject;
        scalar.Value.Should().Be("content");
    }

    [Fact]
    public void Garbage_after_opening_triple_quote_still_throws()
    {
        var act = () => Huml.Parse("key: \"\"\" extra\n  content\n\"\"\"", HumlOptions.LatestSupported);
        act.Should().Throw<HumlParseException>();
    }

    // ── B2: inline vector values require ":: " (exactly one space); B5: any number
    //    of spaces is permitted before a trailing comment after "::" ──

    [Theory]
    [InlineData("key::1")]
    [InlineData("key::\"x\"")]
    [InlineData("key::[]")]
    [InlineData("key::{}")]
    [InlineData("key::true")]
    public void Inline_vector_value_without_space_after_indicator_throws(string input)
    {
        var act = () => Huml.Parse(input, HumlOptions.LatestSupported);
        act.Should().Throw<HumlParseException>();
    }

    [Theory]
    [InlineData("key:: 1")]
    [InlineData("key:: []")]
    public void Inline_vector_value_with_single_space_parses(string input)
    {
        var act = () => Huml.Parse(input, HumlOptions.LatestSupported);
        act.Should().NotThrow();
    }

    [Theory]
    [InlineData("key:: # c\n  - 1")]
    [InlineData("key::  # c\n  - 1")]
    [InlineData("key::# c\n  - 1")]
    public void Comment_after_vector_indicator_parses_with_any_space_count(string input)
    {
        var act = () => Huml.Parse(input, HumlOptions.LatestSupported);
        act.Should().NotThrow();
    }

    [Fact]
    public void Two_spaces_before_inline_vector_value_still_throws()
    {
        var act = () => Huml.Parse("key::  1", HumlOptions.LatestSupported);
        act.Should().Throw<HumlParseException>();
    }

    // ── B1: dict_key = simple_key | STRING — quoted keys are valid in inline dicts ──

    [Fact]
    public void Quoted_key_in_inline_dict_parses()
    {
        var doc = Huml.Parse("key:: \"a b\": 1", HumlOptions.LatestSupported);
        var inner = ((HumlMapping)doc.Entries[0]).Value;
        var mapping = inner.Should().BeOfType<HumlInlineMapping>().Subject;
        ((HumlMapping)mapping.Entries[0]).Key.Should().Be("a b");
    }

    [Fact]
    public void Quoted_key_in_root_inline_dict_parses()
    {
        var doc = Huml.Parse("a: 1, \"b c\": 2", HumlOptions.LatestSupported);
        doc.Entries.Should().HaveCount(2);
        ((HumlMapping)doc.Entries[1]).Key.Should().Be("b c");
    }

    [Fact]
    public void Quoted_key_at_block_position_still_parses()
    {
        var doc = Huml.Parse("\"my key\": 1", HumlOptions.LatestSupported);
        ((HumlMapping)doc.Entries[0]).Key.Should().Be("my key");
    }

    [Theory]
    [InlineData("list:: \"a\", \"b\"")]
    [InlineData("key: \"plain value\"")]
    public void Quoted_strings_not_followed_by_colon_remain_values(string input)
    {
        var act = () => Huml.Parse(input, HumlOptions.LatestSupported);
        act.Should().NotThrow();
    }

    [Fact]
    public void Quoted_key_after_scalar_indicator_still_throws()
    {
        var act = () => Huml.Parse("key: \"v\": 1", HumlOptions.LatestSupported);
        act.Should().Throw<HumlParseException>();
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
