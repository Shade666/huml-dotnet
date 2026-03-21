using AwesomeAssertions;
using Huml.Net.Exceptions;
using Huml.Net.Parser;
using Huml.Net.Versioning;
using Xunit;

// Alias to avoid collision between the Huml.Net.Tests.Parser namespace and the parser class.
using HumlParser = Huml.Net.Parser.HumlParser;

namespace Huml.Net.Tests.Parser;

public class HumlParserTests
{
    // ── 1. Root scalar tests ──────────────────────────────────────────────────

    [Fact]
    public void Parse_RootIntegerScalar_ReturnsDocumentWithOneIntegerEntry()
    {
        var doc = new HumlParser("123", HumlOptions.Default).Parse();

        doc.Entries.Should().HaveCount(1);
        doc.Entries[0].Should().BeOfType<HumlScalar>();
        var scalar = (HumlScalar)doc.Entries[0];
        scalar.Kind.Should().Be(ScalarKind.Integer);
        scalar.Value.Should().Be(123L);
    }

    [Fact]
    public void Parse_RootStringScalar_ReturnsDocumentWithOneStringEntry()
    {
        var doc = new HumlParser("\"hello\"", HumlOptions.Default).Parse();

        doc.Entries.Should().HaveCount(1);
        doc.Entries[0].Should().BeOfType<HumlScalar>();
        var scalar = (HumlScalar)doc.Entries[0];
        scalar.Kind.Should().Be(ScalarKind.String);
        scalar.Value.Should().Be("hello");
    }

    [Fact]
    public void Parse_RootBoolScalar_ReturnsDocumentWithOneBoolEntry()
    {
        var doc = new HumlParser("true", HumlOptions.Default).Parse();

        doc.Entries.Should().HaveCount(1);
        doc.Entries[0].Should().BeOfType<HumlScalar>();
        var scalar = (HumlScalar)doc.Entries[0];
        scalar.Kind.Should().Be(ScalarKind.Bool);
        scalar.Value.Should().Be(true);
    }

    [Fact]
    public void Parse_RootNullScalar_ReturnsDocumentWithOneNullEntry()
    {
        var doc = new HumlParser("null", HumlOptions.Default).Parse();

        doc.Entries.Should().HaveCount(1);
        doc.Entries[0].Should().BeOfType<HumlScalar>();
        var scalar = (HumlScalar)doc.Entries[0];
        scalar.Kind.Should().Be(ScalarKind.Null);
        scalar.Value.Should().BeNull();
    }

    // ── 2. Simple mapping tests ───────────────────────────────────────────────

    [Fact]
    public void Parse_SingleMapping_ReturnsDocumentWithOneMapping()
    {
        var doc = new HumlParser("key: \"value\"", HumlOptions.Default).Parse();

        doc.Entries.Should().HaveCount(1);
        doc.Entries[0].Should().BeOfType<HumlMapping>();
        var mapping = (HumlMapping)doc.Entries[0];
        mapping.Key.Should().Be("key");
        mapping.Value.Should().BeOfType<HumlScalar>();
        var scalar = (HumlScalar)mapping.Value;
        scalar.Kind.Should().Be(ScalarKind.String);
        scalar.Value.Should().Be("value");
    }

    [Fact]
    public void Parse_MultipleMappings_ReturnsDocumentWithMultipleEntries()
    {
        var doc = new HumlParser("a: 1\nb: 2", HumlOptions.Default).Parse();

        doc.Entries.Should().HaveCount(2);

        var first = (HumlMapping)doc.Entries[0];
        first.Key.Should().Be("a");
        ((HumlScalar)first.Value).Kind.Should().Be(ScalarKind.Integer);
        ((HumlScalar)first.Value).Value.Should().Be(1L);

        var second = (HumlMapping)doc.Entries[1];
        second.Key.Should().Be("b");
        ((HumlScalar)second.Value).Kind.Should().Be(ScalarKind.Integer);
        ((HumlScalar)second.Value).Value.Should().Be(2L);
    }

    [Fact]
    public void Parse_QuotedKeyMapping_ReturnsMapping()
    {
        var doc = new HumlParser("\"my-key\": 42", HumlOptions.Default).Parse();

        doc.Entries.Should().HaveCount(1);
        var mapping = (HumlMapping)doc.Entries[0];
        mapping.Key.Should().Be("my-key");
        var scalar = (HumlScalar)mapping.Value;
        scalar.Kind.Should().Be(ScalarKind.Integer);
        scalar.Value.Should().Be(42L);
    }

    // ── 3. Vector block tests ─────────────────────────────────────────────────

    [Fact]
    public void Parse_MultilineList_ReturnsSequence()
    {
        const string input = "items::\n  - 1\n  - 2\n  - 3";
        var doc = new HumlParser(input, HumlOptions.Default).Parse();

        doc.Entries.Should().HaveCount(1);
        var mapping = (HumlMapping)doc.Entries[0];
        mapping.Key.Should().Be("items");
        mapping.Value.Should().BeOfType<HumlSequence>();

        var seq = (HumlSequence)mapping.Value;
        seq.Items.Should().HaveCount(3);
        ((HumlScalar)seq.Items[0]).Value.Should().Be(1L);
        ((HumlScalar)seq.Items[1]).Value.Should().Be(2L);
        ((HumlScalar)seq.Items[2]).Value.Should().Be(3L);
    }

    [Fact]
    public void Parse_MultilineDict_ReturnsMappings()
    {
        const string input = "nested::\n  a: 1\n  b: 2";
        var doc = new HumlParser(input, HumlOptions.Default).Parse();

        doc.Entries.Should().HaveCount(1);
        var mapping = (HumlMapping)doc.Entries[0];
        mapping.Key.Should().Be("nested");
        mapping.Value.Should().BeOfType<HumlDocument>();

        var inner = (HumlDocument)mapping.Value;
        inner.Entries.Should().HaveCount(2);
        ((HumlMapping)inner.Entries[0]).Key.Should().Be("a");
        ((HumlMapping)inner.Entries[1]).Key.Should().Be("b");
    }

    [Fact]
    public void Parse_InlineEmptyList_ReturnsEmptySequence()
    {
        const string input = "items:: []";
        var doc = new HumlParser(input, HumlOptions.Default).Parse();

        doc.Entries.Should().HaveCount(1);
        var mapping = (HumlMapping)doc.Entries[0];
        mapping.Key.Should().Be("items");
        mapping.Value.Should().BeOfType<HumlSequence>();
        ((HumlSequence)mapping.Value).Items.Should().HaveCount(0);
    }

    [Fact]
    public void Parse_InlineEmptyDict_ReturnsEmptyDocument()
    {
        const string input = "items:: {}";
        var doc = new HumlParser(input, HumlOptions.Default).Parse();

        doc.Entries.Should().HaveCount(1);
        var mapping = (HumlMapping)doc.Entries[0];
        mapping.Key.Should().Be("items");
        mapping.Value.Should().BeOfType<HumlDocument>();
        ((HumlDocument)mapping.Value).Entries.Should().HaveCount(0);
    }

    // ── 4. Inline collection tests ────────────────────────────────────────────

    [Fact]
    public void Parse_InlineList_ReturnsSequence()
    {
        const string input = "items:: 1, 2, 3";
        var doc = new HumlParser(input, HumlOptions.Default).Parse();

        doc.Entries.Should().HaveCount(1);
        var mapping = (HumlMapping)doc.Entries[0];
        mapping.Key.Should().Be("items");
        mapping.Value.Should().BeOfType<HumlSequence>();

        var seq = (HumlSequence)mapping.Value;
        seq.Items.Should().HaveCount(3);
        ((HumlScalar)seq.Items[0]).Kind.Should().Be(ScalarKind.Integer);
        ((HumlScalar)seq.Items[0]).Value.Should().Be(1L);
        ((HumlScalar)seq.Items[1]).Value.Should().Be(2L);
        ((HumlScalar)seq.Items[2]).Value.Should().Be(3L);
    }

    [Fact]
    public void Parse_InlineDict_ReturnsMappings()
    {
        const string input = "data:: a: 1, b: 2";
        var doc = new HumlParser(input, HumlOptions.Default).Parse();

        doc.Entries.Should().HaveCount(1);
        var mapping = (HumlMapping)doc.Entries[0];
        mapping.Key.Should().Be("data");
        mapping.Value.Should().BeOfType<HumlDocument>();

        var inner = (HumlDocument)mapping.Value;
        inner.Entries.Should().HaveCount(2);
        ((HumlMapping)inner.Entries[0]).Key.Should().Be("a");
        ((HumlMapping)inner.Entries[1]).Key.Should().Be("b");
    }

    // ── 5. Nested structure tests ─────────────────────────────────────────────

    [Fact]
    public void Parse_NestedMultilineDicts_ReturnsNestedStructure()
    {
        const string input = "outer::\n  inner::\n    key: \"value\"";
        var doc = new HumlParser(input, HumlOptions.Default).Parse();

        doc.Entries.Should().HaveCount(1);
        var outer = (HumlMapping)doc.Entries[0];
        outer.Key.Should().Be("outer");
        outer.Value.Should().BeOfType<HumlDocument>();

        var outerDoc = (HumlDocument)outer.Value;
        outerDoc.Entries.Should().HaveCount(1);
        var inner = (HumlMapping)outerDoc.Entries[0];
        inner.Key.Should().Be("inner");
        inner.Value.Should().BeOfType<HumlDocument>();

        var innerDoc = (HumlDocument)inner.Value;
        innerDoc.Entries.Should().HaveCount(1);
        var leaf = (HumlMapping)innerDoc.Entries[0];
        leaf.Key.Should().Be("key");
        ((HumlScalar)leaf.Value).Value.Should().Be("value");
    }

    // ── 6. Error tests ────────────────────────────────────────────────────────

    [Fact]
    public void Parse_EmptyInput_ThrowsHumlParseException()
    {
        var act = () => new HumlParser("", HumlOptions.Default).Parse();
        act.Should().Throw<HumlParseException>();
    }

    [Fact]
    public void Parse_DuplicateKey_ThrowsHumlParseException()
    {
        var act = () => new HumlParser("key: 1\nkey: 2", HumlOptions.Default).Parse();
        act.Should().Throw<HumlParseException>()
            .WithMessage("*duplicate*");
    }

    [Fact]
    public void Parse_BadIndentation_ThrowsHumlParseException()
    {
        // Expect exactly 2 spaces, but give 3 spaces (wrong indent level)
        const string input = "outer::\n   inner: 1";
        var act = () => new HumlParser(input, HumlOptions.Default).Parse();
        act.Should().Throw<HumlParseException>();
    }

    [Fact]
    public void Parse_RootScalarWithExtraContent_ThrowsHumlParseException()
    {
        var act = () => new HumlParser("123\nextra", HumlOptions.Default).Parse();
        act.Should().Throw<HumlParseException>();
    }

    [Fact]
    public void Parse_AmbiguousEmptyVector_ThrowsHumlParseException()
    {
        // key:: followed by EOF — ambiguous: is it [] or {}?
        var act = () => new HumlParser("key::", HumlOptions.Default).Parse();
        act.Should().Throw<HumlParseException>();
    }

    // ── 7. Version gating test (PARS-04) ─────────────────────────────────────

    [Fact]
    public void Parse_WithV01Options_PropagatesOptionsToLexer()
    {
        // Backtick multiline strings are valid in v0.1 but rejected by the lexer in v0.2.
        // The parser must propagate HumlOptions to the Lexer for this gate to fire.
        // A backtick multiline string — valid in v0.1, rejected in v0.2.
        const string v01Input = "key::\n```\nline one\n```";

#pragma warning disable CS0618
        var v01Options = new HumlOptions { SpecVersion = HumlSpecVersion.V0_1 };
#pragma warning restore CS0618

        // With v0.1 options: the lexer accepts the backtick string, parser should succeed.
        var doc = new HumlParser(v01Input, v01Options).Parse();
        doc.Entries.Should().HaveCount(1);

        // With v0.2 options (default): the lexer rejects backtick strings, parser should throw.
        var actV02 = () => new HumlParser(v01Input, HumlOptions.Default).Parse();
        actV02.Should().Throw<HumlParseException>();
    }

    // ── 8. Scalar type coverage ───────────────────────────────────────────────

    [Fact]
    public void Parse_FloatScalar_ReturnsFloat()
    {
        var doc = new HumlParser("3.14", HumlOptions.Default).Parse();

        doc.Entries.Should().HaveCount(1);
        var scalar = (HumlScalar)doc.Entries[0];
        scalar.Kind.Should().Be(ScalarKind.Float);
        scalar.Value.Should().Be(3.14);
    }

    [Fact]
    public void Parse_NanScalar_ReturnsNaN()
    {
        var doc = new HumlParser("nan", HumlOptions.Default).Parse();

        doc.Entries.Should().HaveCount(1);
        var scalar = (HumlScalar)doc.Entries[0];
        scalar.Kind.Should().Be(ScalarKind.NaN);
        scalar.Value.Should().BeNull();
    }

    [Fact]
    public void Parse_InfScalar_ReturnsInf()
    {
        var doc = new HumlParser("inf", HumlOptions.Default).Parse();

        doc.Entries.Should().HaveCount(1);
        var scalar = (HumlScalar)doc.Entries[0];
        scalar.Kind.Should().Be(ScalarKind.Inf);
        scalar.Value.Should().BeNull();
    }

    [Fact]
    public void Parse_HexInt_ReturnsInteger()
    {
        var doc = new HumlParser("0xFF", HumlOptions.Default).Parse();

        doc.Entries.Should().HaveCount(1);
        var scalar = (HumlScalar)doc.Entries[0];
        scalar.Kind.Should().Be(ScalarKind.Integer);
        scalar.Value.Should().Be(255L);
    }
}
