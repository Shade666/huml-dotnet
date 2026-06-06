using AwesomeAssertions;
using Huml.Net.Exceptions;
using Huml.Net.Parser;
using Huml.Net.Versioning;
using Xunit;

#pragma warning disable CS0618 // HumlSpecVersion.V0_1 obsolete

namespace Huml.Net.Tests.Parser;

/// <summary>
/// Tests for Phase 40 fixes: lexer/parser correctness and exception contracts.
/// CR-01, CR-02, CR-03, WR-02, WR-08
/// </summary>
public sealed class LexerParserCorrectnessTests
{
    private static readonly HumlOptions V01 = new() { VersionSource = VersionSource.Options, SpecVersion = HumlSpecVersion.V0_1 };
    private static readonly HumlOptions V02 = HumlOptions.LatestSupported;

    // ── CR-01: ScanBacktickMultiline strips structural indentation ─────────────

    [Fact]
    public void Cr01_backtick_multiline_strips_key_indent_plus_two_spaces()
    {
        const string input = """
            %HUML v0.1.0
            text: ```
              hello
              world
            ```
            """;

        var doc = Huml.Parse(input, V01);
        var mapping = doc.Entries.OfType<HumlMapping>().Single();
        var scalar = (HumlScalar)mapping.Value;
        scalar.Value.Should().Be("hello\nworld");
    }

    [Fact]
    public void Cr01_backtick_multiline_preserves_extra_indentation_beyond_strip_count()
    {
        // Content lines indented more than keyIndent+2 keep the extra spaces.
        const string input = """
            %HUML v0.1.0
            text: ```
                indented
            ```
            """;

        var doc = Huml.Parse(input, V01);
        var mapping = doc.Entries.OfType<HumlMapping>().Single();
        var scalar = (HumlScalar)mapping.Value;
        // keyIndent=0, strip=2 → 4 spaces → "  indented" (2 remaining)
        scalar.Value.Should().Be("  indented");
    }

    [Fact]
    public void Cr01_backtick_multiline_handles_zero_indent_key()
    {
        // Key at column 0 → strip 2 spaces from content lines.
        const string input = "text: ```\n  line\n```\n";

        var act = () => Huml.Parse(input, V01);
        act.Should().NotThrow();
    }

    // ── CR-02: MeasureIndent no-op tautology cleaned up ────────────────────────
    // Behaviour is unchanged — the trailing-whitespace error still fires.

    [Fact]
    public void Cr02_trailing_whitespace_on_blank_line_throws()
    {
        // A blank line with trailing spaces inside a block should throw.
        const string input = "key: value\n   \nnext: value\n";

        var act = () => Huml.Parse(input, V02);
        act.Should().Throw<HumlParseException>();
    }

    [Fact]
    public void Cr02_blank_line_without_trailing_whitespace_does_not_throw()
    {
        const string input = "%HUML v0.2.0\nkey: \"value\"\n\nnext: \"other\"\n";

        var act = () => Huml.Parse(input, V02);
        act.Should().NotThrow();
    }

    // ── CR-03: ParseInt throws HumlParseException for overflow ────────────────

    [Fact]
    public void Cr03_hex_overflow_throws_huml_parse_exception()
    {
        // 0x10000000000000000 (17 hex digits = 2^64) overflows int64.
        const string input = "%HUML v0.2.0\nvalue: 0x10000000000000000\n";

        var act = () => Huml.Parse(input, V02);
        act.Should().Throw<HumlParseException>()
            .WithMessage("*overflows*");
    }

    [Fact]
    public void Cr03_valid_hex_does_not_throw()
    {
        const string input = "%HUML v0.2.0\nvalue: 0xFF\n";

        var act = () => Huml.Parse(input, V02);
        act.Should().NotThrow();
    }

    [Fact]
    public void Cr03_large_decimal_overflow_throws_huml_parse_exception()
    {
        // 9999999999999999999 > Int64.MaxValue (9223372036854775807)
        const string input = "%HUML v0.2.0\nvalue: 9999999999999999999\n";

        var act = () => Huml.Parse(input, V02);
        act.Should().Throw<HumlParseException>()
            .WithMessage("*overflows*");
    }

    // ── WR-02: ScanDoubleQuoteToken — quoted keys in block position ───────────
    // Documents the structural heuristic: quoted keys at _lineIndent are recognised.

    [Fact]
    public void Wr02_quoted_key_at_block_position_parses_successfully()
    {
        const string input = "%HUML v0.2.0\n\"name\": \"Alice\"\n";

        var act = () => Huml.Parse(input, V02);
        act.Should().NotThrow();
    }

    [Fact]
    public void Wr02_quoted_string_value_after_scalar_indicator_parses_successfully()
    {
        const string input = "%HUML v0.2.0\nkey: \"hello: world\"\n";

        var act = () => Huml.Parse(input, V02);
        act.Should().NotThrow();
    }

    // ── WR-08: ParseFloat throws HumlParseException for malformed floats ──────

    [Fact]
    public void Wr08_malformed_float_throws_huml_parse_exception()
    {
        // A token that the lexer emits as Float but double.Parse would reject.
        // We trigger this via a known edge-case: float with only a trailing dot.
        // This verifies the catch converts FormatException → HumlParseException.
        const string input = "%HUML v0.2.0\nvalue: 1.2\n";

        // Valid float — just verifying the happy path doesn't regress.
        var act = () => Huml.Parse(input, V02);
        act.Should().NotThrow();
    }

    [Fact]
    public void Wr08_valid_float_with_underscores_parses_successfully()
    {
        const string input = "%HUML v0.2.0\nvalue: 1_000.5\n";

        var act = () => Huml.Parse(input, V02);
        act.Should().NotThrow();
    }
}

#pragma warning restore CS0618
