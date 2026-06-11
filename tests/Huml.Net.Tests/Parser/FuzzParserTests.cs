using AwesomeAssertions;
using Huml.Net.Exceptions;
using Huml.Net.Versioning;
using System.Text;
using Xunit;

namespace Huml.Net.Tests.Parser;

public sealed class FuzzParserTests
{
    // -----------------------------------------------------------------------
    // Helpers
    // -----------------------------------------------------------------------

    private static void AssertParserSafe(string input, HumlOptions? options = null)
    {
        try { HumlSerializer.Parse(input, options ?? HumlOptions.LatestSupported); }
        catch (HumlParseException) { /* expected */ }
        catch (HumlUnsupportedVersionException) { /* expected for unknown headers */ }
        // Any other exception propagates and fails the test
    }

    private static string MakeNestedHuml(int depth)
    {
        var sb = new StringBuilder();
        for (var i = 0; i < depth; i++)
            sb.Append(' ', i * 2).Append("key").Append(i).AppendLine("::");
        sb.Append(' ', depth * 2).Append("leaf: 42");
        return sb.ToString();
    }

    // MaxRecursionDepth = 9: the root document itself uses 1 depth slot, then each nested key:: pair
    // costs 2 more (ParseVector + ParseMultilineDict). So 4 nesting levels consume 1 + 4*2 = 9 depth
    // units total -- exactly at-limit for Fuzz08 (MakeNestedHuml(4)). MakeNestedHuml(10) would use
    // 1 + 10*2 = 21 > 9, so Fuzz09 correctly throws.
    private static readonly HumlOptions DepthFourOpts = new()
    {
        MaxRecursionDepth = 9,
        VersionSource = VersionSource.Options,
        SpecVersion = HumlSpecVersion.V0_2,
    };

    // -----------------------------------------------------------------------
    // Truncated / empty inputs
    // -----------------------------------------------------------------------

    [Fact]
    public void Fuzz01_empty_string()
    {
        AssertParserSafe("");
    }

    [Fact]
    public void Fuzz02_only_whitespace()
    {
        AssertParserSafe("   \n\t  ");
    }

    [Fact]
    public void Fuzz03_only_version_header()
    {
        AssertParserSafe("%HUML 0.2.0\n");
    }

    [Fact]
    public void Fuzz04_truncated_after_version_header()
    {
        AssertParserSafe("%HUML 0.2.0");
    }

    [Fact]
    public void Fuzz05_truncated_after_bare_key()
    {
        AssertParserSafe("key");
    }

    [Fact]
    public void Fuzz06_truncated_after_scalar_colon()
    {
        AssertParserSafe("key:");
    }

    [Fact]
    public void Fuzz07_truncated_after_vector_colon()
    {
        AssertParserSafe("key::");
    }

    // -----------------------------------------------------------------------
    // Nesting depth
    // -----------------------------------------------------------------------

    [Fact]
    public void Fuzz08_nesting_at_max_depth()
    {
        var act = () => HumlSerializer.Parse(MakeNestedHuml(4), DepthFourOpts);
        act.Should().NotThrow();
    }

    [Fact]
    public void Fuzz09_nesting_exceeds_max_depth()
    {
        var act = () => HumlSerializer.Parse(MakeNestedHuml(10), DepthFourOpts);
        act.Should().Throw<HumlParseException>();
    }

    // -----------------------------------------------------------------------
    // Very large inputs
    // -----------------------------------------------------------------------

    [Fact]
    public void Fuzz10_very_long_bare_key()
    {
        var key = "a" + new string('a', 9_999);
        AssertParserSafe(key + ": value");
    }

    [Fact]
    public void Fuzz11_very_long_string_value()
    {
        var value = new string('x', 100_000);
        AssertParserSafe("key: \"" + value + "\"");
    }

    [Fact]
    public void Fuzz12_many_document_entries()
    {
        var sb = new StringBuilder();
        for (var i = 0; i < 5_000; i++)
            sb.Append("key").Append(i).Append(": ").AppendLine(i.ToString());
        AssertParserSafe(sb.ToString());
    }

    // -----------------------------------------------------------------------
    // Unicode edge cases
    // -----------------------------------------------------------------------

    [Fact]
    public void Fuzz13_bidi_rtl_override_in_string_value()
    {
        // U+202E RIGHT-TO-LEFT OVERRIDE
        AssertParserSafe("key: \"\u202Evalue\"");
    }

    [Fact]
    public void Fuzz14_bidi_ltr_override_in_string_value()
    {
        // U+202D LEFT-TO-RIGHT OVERRIDE
        AssertParserSafe("key: \"\u202Dvalue\"");
    }

    [Fact]
    public void Fuzz15_null_byte_in_string_value()
    {
        // U+0000 NULL
        AssertParserSafe("key: \"\u0000value\"");
    }

    [Fact]
    public void Fuzz16_lone_high_surrogate_in_string_value()
    {
        // U+D800 lone high surrogate
        AssertParserSafe("key: \"\uD800value\"");
    }

    [Fact]
    public void Fuzz17_lone_low_surrogate_in_string_value()
    {
        // U+DC00 lone low surrogate
        AssertParserSafe("key: \"\uDC00value\"");
    }

    // -----------------------------------------------------------------------
    // Version header edge cases
    // -----------------------------------------------------------------------

    [Fact]
    public void Fuzz18_unknown_version_header()
    {
        AssertParserSafe("%HUML 99.99.99\nkey: value", HumlOptions.Default);
    }

    [Fact]
    public void Fuzz19_malformed_version_header()
    {
        AssertParserSafe("%HUML abc\nkey: value", HumlOptions.Default);
    }

    [Fact]
    public void Fuzz20_version_header_no_space()
    {
        AssertParserSafe("%HUMLv0.2.0\nkey: value", HumlOptions.Default);
    }

    // -----------------------------------------------------------------------
    // Numeric extremes
    // -----------------------------------------------------------------------

    [Fact]
    public void Fuzz21_extreme_large_integer()
    {
        AssertParserSafe("key: 99999999999999999999999999999999999999");
    }

    [Fact]
    public void Fuzz22_extreme_negative_integer()
    {
        AssertParserSafe("key: -99999999999999999999999999999999999999");
    }

    [Fact]
    public void Fuzz23_extreme_float_max_double()
    {
        AssertParserSafe("key: 1.7976931348623157E+308");
    }

    [Fact]
    public void Fuzz24_infinity_value()
    {
        // inf is a first-class HUML scalar kind, but keyword literals are lowercase
        // and case-sensitive: "Inf" is an unquoted string, i.e. a parse error.
        var act = () => HumlSerializer.Parse("key: Inf", HumlOptions.LatestSupported);
        act.Should().Throw<HumlParseException>();
        var lower = () => HumlSerializer.Parse("key: inf", HumlOptions.LatestSupported);
        lower.Should().NotThrow();
    }

    [Fact]
    public void Fuzz25_nan_value()
    {
        // nan is a first-class HUML scalar kind, but keyword literals are lowercase
        // and case-sensitive: "NaN" is an unquoted string, i.e. a parse error.
        var act = () => HumlSerializer.Parse("key: NaN", HumlOptions.LatestSupported);
        act.Should().Throw<HumlParseException>();
        var lower = () => HumlSerializer.Parse("key: nan", HumlOptions.LatestSupported);
        lower.Should().NotThrow();
    }
}
