using AwesomeAssertions;
using Huml.Net.Exceptions;
using Huml.Net.Versioning;
using Xunit;

namespace Huml.Net.Tests;

public sealed class HumlOptionsLatestAutoDetectTests
{
    // ── OPT2-01: LatestSupportedAutoDetect reads from header ──────────────────

    [Fact]
    public void Opt2_01_LatestSupportedAutoDetect_reads_from_header()
    {
        HumlOptions.LatestSupportedAutoDetect.VersionSource.Should().Be(VersionSource.Header);
    }

    // ── OPT2-02: LatestSupportedAutoDetect falls back to latest on unknown version

    [Fact]
    public void Opt2_02_LatestSupportedAutoDetect_uses_latest_on_unknown_version()
    {
        HumlOptions.LatestSupportedAutoDetect.UnknownVersionBehaviour
            .Should().Be(UnknownVersionBehaviour.UseLatest);
    }

    // ── OPT2-03: LatestSupportedAutoDetect is pre-frozen and functional ───────

    [Fact]
    public void Opt2_03_LatestSupportedAutoDetect_is_read_only()
    {
        HumlOptions.LatestSupportedAutoDetect.IsReadOnly.Should().BeTrue();
    }

    [Fact]
    public void Opt2_04_parsing_with_unknown_version_header_succeeds_without_throwing()
    {
        const string huml = """
            %HUML v9.9.9
            Name: "test"
            """;

        var act = () => Huml.Parse(huml, HumlOptions.LatestSupportedAutoDetect);

        act.Should().NotThrow<HumlUnsupportedVersionException>();
    }
}
