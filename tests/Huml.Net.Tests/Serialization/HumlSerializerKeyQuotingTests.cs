using AwesomeAssertions;
using Huml.Net.Versioning;
using Xunit;

namespace Huml.Net.Tests.Serialization;

public class HumlSerializerKeyQuotingTests
{
    // SER-KEY-01
    [Fact]
    public void Serialize_DictionaryWithArabicKeys_EmitsQuotedKeys()
    {
        var dict = new Dictionary<string, string> { ["اسم"] = "أحمد" };
        var huml = Huml.Serialize(dict);
        huml.Should().Contain("\"اسم\": ");
    }

    // SER-KEY-02
    [Fact]
    public void Serialize_DictionaryWithDigitStartKey_EmitsQuotedKey()
    {
        var dict = new Dictionary<string, string> { ["1start"] = "value" };
        var huml = Huml.Serialize(dict);
        huml.Should().Contain("\"1start\": ");
    }

    // SER-KEY-03
    [Fact]
    public void Serialize_DictionaryWithSpaceInKey_EmitsQuotedKey()
    {
        var dict = new Dictionary<string, string> { ["has space"] = "value" };
        var huml = Huml.Serialize(dict);
        huml.Should().Contain("\"has space\": ");
    }

    // SER-KEY-04
    [Fact]
    public void Serialize_DictionaryWithEmptyKey_EmitsQuotedKey()
    {
        var dict = new Dictionary<string, string> { [""] = "value" };
        var huml = Huml.Serialize(dict);
        huml.Should().Contain("\"\": ");
    }

    // SER-KEY-05
    [Fact]
    public void RoundTrip_DictionaryWithNonAsciiKeys_PreservesKeysAndValues()
    {
        var dict = new Dictionary<string, string>
        {
            ["اسم"] = "أحمد",
            ["名前"] = "太郎",
        };
        var huml = Huml.Serialize(dict);

        var act = () => Huml.Parse(huml, HumlOptions.AutoDetect);
        act.Should().NotThrow();

        var result = Huml.Deserialize<Dictionary<string, string>>(huml, HumlOptions.AutoDetect);
        result["اسم"].Should().Be("أحمد");
        result["名前"].Should().Be("太郎");
    }

    // SER-KEY-06
    [Fact]
    public void Serialize_DictionaryWithValidBareKey_EmitsBareKey()
    {
        var dict = new Dictionary<string, string> { ["validKey"] = "value" };
        var huml = Huml.Serialize(dict);
        huml.Should().Contain("validKey: ");
        huml.Should().NotContain("\"validKey\"");
    }

    // SER-KEY-07
    [Fact]
    public void Serialize_DictionaryWithNonAsciiKeyNestedList_EmitsQuotedKeyWithVectorIndicator()
    {
        var dict = new Dictionary<string, List<string>> { ["données"] = new List<string> { "a", "b" } };
        var huml = Huml.Serialize(dict);
        huml.Should().Contain("\"données\"::");
    }
}
