using AwesomeAssertions;
using Huml.Net.Parser;
using Huml.Net.Versioning;
using Xunit;

namespace Huml.Net.Tests;

public sealed class VersionPreservingRoundTripTests
{
    [Fact]
    public void DetectedVersion_is_V01_when_header_declares_v01()
    {
#pragma warning disable CS0618
        const string input = "%HUML v0.1.0\nkey: \"value\"\n";
        var doc = HumlSerializer.Parse(input, HumlOptions.AutoDetect);
        doc.DetectedVersion.Should().Be(HumlSpecVersion.V0_1);
#pragma warning restore CS0618
    }

    [Fact]
    public void DetectedVersion_is_V02_when_header_declares_v02()
    {
        const string input = "%HUML v0.2.0\nkey: \"value\"\n";
        var doc = HumlSerializer.Parse(input, HumlOptions.AutoDetect);
        doc.DetectedVersion.Should().Be(HumlSpecVersion.V0_2);
    }

    [Fact]
    public void DetectedVersion_is_null_when_no_header_present()
    {
        const string input = "key: \"value\"\n";
        var doc = HumlSerializer.Parse(input, HumlOptions.AutoDetect);
        doc.DetectedVersion.Should().BeNull();
    }

    [Fact]
    public void DetectedVersion_reflects_header_value_even_when_VersionSource_is_Options()
    {
#pragma warning disable CS0618
        const string input = "%HUML v0.1.0\nkey: \"value\"\n";
        // HumlOptions.LatestSupported pins VersionSource = Options, so the header is
        // consumed but ApplyVersionFromHeader is NOT called. DetectedVersion must
        // still return V0_1 — it is read from the raw token, not from the effective version.
        var doc = HumlSerializer.Parse(input, HumlOptions.LatestSupported);
        doc.DetectedVersion.Should().Be(HumlSpecVersion.V0_1);
#pragma warning restore CS0618
    }

    [Fact]
    public void Serialize_with_DetectedVersion_emits_v01_header_for_v01_source()
    {
#pragma warning disable CS0618
        const string input = "%HUML v0.1.0\nname: \"Alice\"\n";
        var doc = HumlSerializer.Parse(input, HumlOptions.AutoDetect);
        var opts = new HumlOptions { SpecVersion = doc.DetectedVersion ?? HumlSpecVersion.V0_2 };
        var output = HumlSerializer.Serialize(new PersonDto { Name = "Alice" }, opts);
        output.Should().StartWith("%HUML v0.1.0");
#pragma warning restore CS0618
    }

    [Fact]
    public void Serialize_with_DetectedVersion_emits_v02_header_for_v02_source()
    {
        const string input = "%HUML v0.2.0\nname: \"Bob\"\n";
        var doc = HumlSerializer.Parse(input, HumlOptions.AutoDetect);
        var opts = new HumlOptions { SpecVersion = doc.DetectedVersion ?? HumlSpecVersion.V0_2 };
        var output = HumlSerializer.Serialize(new PersonDto { Name = "Bob" }, opts);
        output.Should().StartWith("%HUML v0.2.0");
    }

    [Fact]
    public void DetectedVersion_is_null_for_HumlDocument_constructed_in_code()
    {
        var doc = new HumlDocument(Array.Empty<HumlNode>());
        doc.DetectedVersion.Should().BeNull();
    }

    private sealed class PersonDto { public string? Name { get; set; } }
}
