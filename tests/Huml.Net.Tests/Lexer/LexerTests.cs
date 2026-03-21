#pragma warning disable CS0618 // V0_1 is obsolete but used intentionally in version-gate tests

using System;
using System.Collections.Generic;
using AwesomeAssertions;
using Huml.Net.Exceptions;
using Huml.Net.Lexer;
using Huml.Net.Versioning;
using Xunit;
using HumlLexer = Huml.Net.Lexer.Lexer;

namespace Huml.Net.Tests.Lexer;

public class LexerTests
{
    private static List<Token> LexAll(string input, HumlOptions? options = null)
    {
        var lexer = new HumlLexer(input, options ?? HumlOptions.Default);
        var tokens = new List<Token>();
        Token t;
        do { t = lexer.NextToken(); tokens.Add(t); } while (t.Type != TokenType.Eof);
        return tokens;
    }

    // -----------------------------------------------------------------------
    // Basic token stream tests
    // -----------------------------------------------------------------------

    [Fact]
    public void Lex_key_scalar_string()
    {
        var tokens = LexAll("foo: \"bar\"\n");
        tokens.Should().HaveCount(4);
        tokens[0].Type.Should().Be(TokenType.Key);
        tokens[0].Value.Should().Be("foo");
        tokens[1].Type.Should().Be(TokenType.ScalarIndicator);
        tokens[1].Value.Should().BeNull();
        tokens[2].Type.Should().Be(TokenType.String);
        tokens[2].Value.Should().Be("bar");
        tokens[3].Type.Should().Be(TokenType.Eof);
    }

    [Fact]
    public void Lex_key_scalar_integer()
    {
        var tokens = LexAll("num: 42\n");
        tokens.Should().HaveCount(4);
        tokens[0].Type.Should().Be(TokenType.Key);
        tokens[0].Value.Should().Be("num");
        tokens[1].Type.Should().Be(TokenType.ScalarIndicator);
        tokens[2].Type.Should().Be(TokenType.Int);
        tokens[2].Value.Should().Be("42");
        tokens[3].Type.Should().Be(TokenType.Eof);
    }

    [Fact]
    public void Lex_key_scalar_float()
    {
        var tokens = LexAll("f: 3.14\n");
        tokens[2].Type.Should().Be(TokenType.Float);
        tokens[2].Value.Should().Be("3.14");
    }

    [Fact]
    public void Lex_key_scalar_bool_true()
    {
        var tokens = LexAll("b: true\n");
        tokens[2].Type.Should().Be(TokenType.Bool);
        tokens[2].Value.Should().Be("true");
    }

    [Fact]
    public void Lex_key_scalar_bool_case_insensitive()
    {
        var tokens = LexAll("b: TRUE\n");
        tokens[2].Type.Should().Be(TokenType.Bool);
        tokens[2].Value.Should().Be("TRUE");
    }

    [Fact]
    public void Lex_key_scalar_null()
    {
        var tokens = LexAll("n: null\n");
        tokens.Should().HaveCount(4);
        tokens[2].Type.Should().Be(TokenType.Null);
        tokens[2].Value.Should().Be("null");
    }

    [Fact]
    public void Lex_key_scalar_null_case_insensitive()
    {
        var tokens = LexAll("n: NULL\n");
        tokens[2].Type.Should().Be(TokenType.Null);
        tokens[2].Value.Should().Be("NULL");
    }

    [Fact]
    public void Lex_key_scalar_nan()
    {
        var tokens = LexAll("x: nan\n");
        tokens[2].Type.Should().Be(TokenType.NaN);
        tokens[2].Value.Should().Be("nan");
    }

    [Fact]
    public void Lex_key_scalar_inf()
    {
        // +inf
        var tokens1 = LexAll("x: +inf\n");
        tokens1[2].Type.Should().Be(TokenType.Inf);
        tokens1[2].Value.Should().Be("+inf");

        // inf
        var tokens2 = LexAll("x: inf\n");
        tokens2[2].Type.Should().Be(TokenType.Inf);
        tokens2[2].Value.Should().Be("inf");

        // -inf
        var tokens3 = LexAll("x: -inf\n");
        tokens3[2].Type.Should().Be(TokenType.Inf);
        tokens3[2].Value.Should().Be("-inf");
    }

    [Fact]
    public void Lex_quoted_key()
    {
        var tokens = LexAll("\"my key\": \"val\"\n");
        tokens.Should().HaveCount(4);
        tokens[0].Type.Should().Be(TokenType.QuotedKey);
        tokens[0].Value.Should().Be("my key");
        tokens[1].Type.Should().Be(TokenType.ScalarIndicator);
        tokens[2].Type.Should().Be(TokenType.String);
        tokens[2].Value.Should().Be("val");
        tokens[3].Type.Should().Be(TokenType.Eof);
    }

    // -----------------------------------------------------------------------
    // Vector and collection tests
    // -----------------------------------------------------------------------

    [Fact]
    public void Lex_vector_indicator_multiline()
    {
        var tokens = LexAll("items::\n  - \"a\"\n  - \"b\"\n");
        tokens[0].Type.Should().Be(TokenType.Key);
        tokens[1].Type.Should().Be(TokenType.VectorIndicator);
        tokens[2].Type.Should().Be(TokenType.ListItem);
        tokens[3].Type.Should().Be(TokenType.String);
        tokens[3].Value.Should().Be("a");
        tokens[4].Type.Should().Be(TokenType.ListItem);
        tokens[5].Type.Should().Be(TokenType.String);
        tokens[5].Value.Should().Be("b");
        tokens[6].Type.Should().Be(TokenType.Eof);
    }

    [Fact]
    public void Lex_empty_list()
    {
        var tokens = LexAll("e:: []\n");
        tokens[0].Type.Should().Be(TokenType.Key);
        tokens[1].Type.Should().Be(TokenType.VectorIndicator);
        tokens[2].Type.Should().Be(TokenType.EmptyList);
        tokens[3].Type.Should().Be(TokenType.Eof);
    }

    [Fact]
    public void Lex_empty_dict()
    {
        var tokens = LexAll("e:: {}\n");
        tokens[0].Type.Should().Be(TokenType.Key);
        tokens[1].Type.Should().Be(TokenType.VectorIndicator);
        tokens[2].Type.Should().Be(TokenType.EmptyDict);
        tokens[3].Type.Should().Be(TokenType.Eof);
    }

    [Fact]
    public void Lex_inline_list()
    {
        var tokens = LexAll("i:: \"a\", \"b\"\n");
        tokens[0].Type.Should().Be(TokenType.Key);
        tokens[1].Type.Should().Be(TokenType.VectorIndicator);
        tokens[2].Type.Should().Be(TokenType.String);
        tokens[2].Value.Should().Be("a");
        tokens[3].Type.Should().Be(TokenType.Comma);
        tokens[4].Type.Should().Be(TokenType.String);
        tokens[4].Value.Should().Be("b");
        tokens[5].Type.Should().Be(TokenType.Eof);
    }

    // -----------------------------------------------------------------------
    // Version directive
    // -----------------------------------------------------------------------

    [Fact]
    public void Lex_version_directive()
    {
        var tokens = LexAll("%HUML v0.2.0\nkey: \"val\"\n");
        tokens[0].Type.Should().Be(TokenType.Version);
        tokens[0].Value.Should().Be("v0.2.0");
        tokens[1].Type.Should().Be(TokenType.Key);
        tokens[2].Type.Should().Be(TokenType.ScalarIndicator);
        tokens[3].Type.Should().Be(TokenType.String);
        tokens[4].Type.Should().Be(TokenType.Eof);
    }

    // -----------------------------------------------------------------------
    // Numeric format tests
    // -----------------------------------------------------------------------

    [Fact]
    public void Lex_hex_integer()
    {
        var tokens = LexAll("k: 0xFF\n");
        tokens[2].Type.Should().Be(TokenType.Int);
        tokens[2].Value.Should().Be("0xFF");
    }

    [Fact]
    public void Lex_octal_integer()
    {
        var tokens = LexAll("k: 0o77\n");
        tokens[2].Type.Should().Be(TokenType.Int);
        tokens[2].Value.Should().Be("0o77");
    }

    [Fact]
    public void Lex_binary_integer()
    {
        var tokens = LexAll("k: 0b1010\n");
        tokens[2].Type.Should().Be(TokenType.Int);
        tokens[2].Value.Should().Be("0b1010");
    }

    [Fact]
    public void Lex_underscore_integer()
    {
        var tokens = LexAll("k: 1_000\n");
        tokens[2].Type.Should().Be(TokenType.Int);
        tokens[2].Value.Should().Be("1_000");
    }

    [Fact]
    public void Lex_positive_integer()
    {
        var tokens = LexAll("k: +123\n");
        tokens[2].Type.Should().Be(TokenType.Int);
        tokens[2].Value.Should().Be("+123");
    }

    [Fact]
    public void Lex_negative_integer()
    {
        var tokens = LexAll("k: -456\n");
        tokens[2].Type.Should().Be(TokenType.Int);
        tokens[2].Value.Should().Be("-456");
    }

    [Fact]
    public void Lex_scientific_float()
    {
        var tokens = LexAll("k: 1.23e10\n");
        tokens[2].Type.Should().Be(TokenType.Float);
        tokens[2].Value.Should().Be("1.23e10");
    }

    // -----------------------------------------------------------------------
    // String escape tests
    // -----------------------------------------------------------------------

    [Fact]
    public void Lex_string_with_escape_sequences()
    {
        // "a\nb" in source (escape sequence) should produce actual newline in value
        var tokens = LexAll("k: \"a\\nb\"\n");
        tokens[2].Type.Should().Be(TokenType.String);
        tokens[2].Value.Should().Be("a\nb");
    }

    [Fact]
    public void Lex_string_with_backslash_escape()
    {
        // "a\\b" in source should produce "a\b" in value
        var tokens = LexAll("k: \"a\\\\b\"\n");
        tokens[2].Type.Should().Be(TokenType.String);
        tokens[2].Value.Should().Be("a\\b");
    }

    // -----------------------------------------------------------------------
    // Multiline string tests
    // -----------------------------------------------------------------------

    [Fact]
    public void Lex_triple_quote_multiline()
    {
        var input = "k: \"\"\"\n  line1\n  line2\n\"\"\"\n";
        var tokens = LexAll(input);
        tokens[2].Type.Should().Be(TokenType.String);
        tokens[2].Value.Should().Be("line1\nline2");
    }

    [Fact]
    public void Lex_backtick_multiline_v01_succeeds()
    {
        var options = new HumlOptions { SpecVersion = HumlSpecVersion.V0_1 };
        var input = "k: ```\nline1\nline2\n```\n";
        var tokens = LexAll(input, options);
        tokens[2].Type.Should().Be(TokenType.String);
    }

    [Fact]
    public void Lex_backtick_multiline_v02_throws()
    {
        var options = HumlOptions.Default; // V0_2
        var input = "k: ```\nline1\nline2\n```\n";
        var act = () => LexAll(input, options);
        act.Should().Throw<HumlParseException>();
    }

    // -----------------------------------------------------------------------
    // Position tracking tests
    // -----------------------------------------------------------------------

    [Fact]
    public void Token_line_numbers_are_correct()
    {
        var tokens = LexAll("a: 1\nb: 2\n");
        // a is on line 1, b is on line 2
        tokens[0].Line.Should().Be(1); // Key "a"
        // find Key "b"
        var bKey = tokens.Find(t => t.Type == TokenType.Key && t.Value == "b");
        bKey.Line.Should().Be(2);
    }

    [Fact]
    public void Token_column_is_zero_based()
    {
        var tokens = LexAll("foo: \"bar\"\n");
        tokens[0].Column.Should().Be(0); // key starts at col 0
    }

    [Fact]
    public void Token_indent_reflects_leading_spaces()
    {
        var tokens = LexAll("items::\n  - \"a\"\n");
        // ListItem on line 2 has 2 leading spaces
        tokens[2].Indent.Should().Be(2); // ListItem
    }

    [Fact]
    public void Space_before_true_for_value_after_colon()
    {
        var tokens = LexAll("k: \"v\"\n");
        tokens[2].SpaceBefore.Should().BeTrue(); // String value after ": "
    }

    [Fact]
    public void Space_before_false_for_key_at_line_start()
    {
        var tokens = LexAll("k: \"v\"\n");
        tokens[0].SpaceBefore.Should().BeFalse(); // Key at line start
    }

    // -----------------------------------------------------------------------
    // Error tests
    // -----------------------------------------------------------------------

    [Fact]
    public void Tab_indentation_throws()
    {
        var act = () => LexAll("\tkey: \"v\"");
        var ex = act.Should().Throw<HumlParseException>().Which;
        ex.Line.Should().Be(1);
        ex.Column.Should().Be(0);
    }

    [Fact]
    public void Trailing_whitespace_throws()
    {
        var act = () => LexAll("key: \"v\" \n");
        act.Should().Throw<HumlParseException>();
    }

    [Fact]
    public void Comment_without_space_throws()
    {
        var act = () => LexAll("#comment\n");
        act.Should().Throw<HumlParseException>();
    }

    [Fact]
    public void Unquoted_string_throws()
    {
        var act = () => LexAll("key: value\n");
        act.Should().Throw<HumlParseException>();
    }

    [Fact]
    public void Invalid_key_start_throws()
    {
        var act = () => LexAll("123: \"v\"");
        act.Should().Throw<HumlParseException>();
    }
}
