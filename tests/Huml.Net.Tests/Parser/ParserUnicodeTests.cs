using AwesomeAssertions;
using Huml.Net.Parser;
using Huml.Net.Versioning;
using Xunit;
using HumlParser = Huml.Net.Parser.HumlParser;

namespace Huml.Net.Tests.Parser;

public class ParserUnicodeTests
{
    // Private POCO with ASCII property names to avoid serializer key-quoting gap (D-08)
    private class UnicodePoco
    {
        public string? Greeting { get; set; }
        public string? Name { get; set; }
    }

    // -----------------------------------------------------------------------
    // Quoted key parsing
    // -----------------------------------------------------------------------

    [Fact]
    public void Parse_QuotedKeyWithArabic_ReturnsMappingWithCorrectKey()
    {
        var doc = new HumlParser("\"اسم\": \"أحمد\"", HumlOptions.Default).Parse();
        doc.Entries.Should().HaveCount(1);
        var mapping = doc.Entries[0].Should().BeOfType<HumlMapping>().Which;
        mapping.Key.Should().Be("اسم");
        var scalar = mapping.Value.Should().BeOfType<HumlScalar>().Which;
        scalar.Value.Should().Be("أحمد");
    }

    [Fact]
    public void Parse_QuotedKeyWithChinese_ReturnsMappingWithCorrectKey()
    {
        var doc = new HumlParser("\"名前\": \"太郎\"", HumlOptions.Default).Parse();
        doc.Entries.Should().HaveCount(1);
        var mapping = doc.Entries[0].Should().BeOfType<HumlMapping>().Which;
        mapping.Key.Should().Be("名前");
        var scalar = mapping.Value.Should().BeOfType<HumlScalar>().Which;
        scalar.Value.Should().Be("太郎");
    }

    [Fact]
    public void Parse_QuotedKeyWithEmoji_ReturnsMappingWithCorrectKey()
    {
        var doc = new HumlParser("\"🚀\": \"launch\"", HumlOptions.Default).Parse();
        doc.Entries.Should().HaveCount(1);
        var mapping = doc.Entries[0].Should().BeOfType<HumlMapping>().Which;
        mapping.Key.Should().Be("🚀");
        var scalar = mapping.Value.Should().BeOfType<HumlScalar>().Which;
        scalar.Value.Should().Be("launch");
    }

    // -----------------------------------------------------------------------
    // RTL string values
    // -----------------------------------------------------------------------

    [Fact]
    public void Parse_RtlStringValue_PreservesContent()
    {
        var doc = new HumlParser("msg: \"مرحبا بالعالم\"", HumlOptions.Default).Parse();
        doc.Entries.Should().HaveCount(1);
        var mapping = doc.Entries[0].Should().BeOfType<HumlMapping>().Which;
        var scalar = mapping.Value.Should().BeOfType<HumlScalar>().Which;
        scalar.Value.Should().Be("مرحبا بالعالم");
    }

    // -----------------------------------------------------------------------
    // Mixed script document
    // -----------------------------------------------------------------------

    [Fact]
    public void Parse_MixedScriptDocument_ParsesAllMappings()
    {
        var input = "greeting: \"مرحبا\"\nname: \"太郎\"\nemoji: \"🚀\"";
        var doc = new HumlParser(input, HumlOptions.Default).Parse();
        doc.Entries.Should().HaveCount(3);

        var m0 = doc.Entries[0].Should().BeOfType<HumlMapping>().Which;
        m0.Key.Should().Be("greeting");
        m0.Value.Should().BeOfType<HumlScalar>().Which.Value.Should().Be("مرحبا");

        var m1 = doc.Entries[1].Should().BeOfType<HumlMapping>().Which;
        m1.Key.Should().Be("name");
        m1.Value.Should().BeOfType<HumlScalar>().Which.Value.Should().Be("太郎");

        var m2 = doc.Entries[2].Should().BeOfType<HumlMapping>().Which;
        m2.Key.Should().Be("emoji");
        m2.Value.Should().BeOfType<HumlScalar>().Which.Value.Should().Be("🚀");
    }

    // -----------------------------------------------------------------------
    // Bidi control characters
    // -----------------------------------------------------------------------

    [Fact]
    public void Parse_BidiControlCharsInValue_Preserved()
    {
        var input = $"key: \"text\u200Fmore\u200Eend\"";
        var doc = new HumlParser(input, HumlOptions.Default).Parse();
        var mapping = doc.Entries[0].Should().BeOfType<HumlMapping>().Which;
        var scalar = mapping.Value.Should().BeOfType<HumlScalar>().Which;
        var value = scalar.Value.Should().BeOfType<string>().Which;
        value.Should().Contain("\u200F");
        value.Should().Contain("\u200E");
    }

    // -----------------------------------------------------------------------
    // Round-trip tests
    // -----------------------------------------------------------------------

    [Fact]
    public void RoundTrip_UnicodeStringValues_Preserved()
    {
        var original = new UnicodePoco { Greeting = "مرحبا", Name = "太郎" };
        var huml = Huml.Serialize(original);
        var result = Huml.Deserialize<UnicodePoco>(huml);
        result.Greeting.Should().Be("مرحبا");
        result.Name.Should().Be("太郎");
    }

    [Fact]
    public void RoundTrip_Emoji_Preserved()
    {
        var original = new UnicodePoco { Greeting = "🚀🌍", Name = "launch" };
        var huml = Huml.Serialize(original);
        var result = Huml.Deserialize<UnicodePoco>(huml);
        result.Greeting.Should().Be("🚀🌍");
        result.Name.Should().Be("launch");
    }
}
