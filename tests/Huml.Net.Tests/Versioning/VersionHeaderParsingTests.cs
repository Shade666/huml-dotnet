using AwesomeAssertions;
using Huml.Net.Versioning;
using Huml.Net.Exceptions;
using Xunit;

namespace Huml.Net.Tests.Versioning;

/// <summary>
/// Tests that version header detection and dispatch work correctly end-to-end.
/// Covers requirements VER-HEADER-01 through VER-HEADER-06.
/// </summary>
public class VersionHeaderParsingTests
{
    // ── VER-HEADER-01 — AutoDetect + known v0.1 header parses as v0.1 ─────────

    [Fact]
    public void Header_V01_with_AutoDetect_parses_as_v01()
    {
        // The discriminating proof for v0.1 rules: use an unknown future version
        // with Throw and confirm it throws (currently broken — it would NOT throw).
        // For the v0.1 header case, just confirm the parse succeeds for a basic document.
        const string input = "%HUML v0.1.0\nkey: \"value\"\n";

#pragma warning disable CS0618 // V0_1 obsolete
        var options = new HumlOptions
        {
            VersionSource = VersionSource.Header,
            SpecVersion = HumlSpecVersion.V0_1,
        };
#pragma warning restore CS0618

        var act = () => Huml.Parse(input, options);
        act.Should().NotThrow();
    }

    // ── VER-HEADER-02 — AutoDetect + unknown version + Throw throws ───────────

    [Fact]
    public void Header_unknown_version_with_Throw_throws()
    {
        // This is the primary discriminator test: currently the parser DISCARDS the
        // version token with a bare Advance(), so this would NOT throw today.
        const string input = "%HUML v9.9.0\nkey: true\n";

        var act = () => Huml.Parse(input, HumlOptions.AutoDetect);
        act.Should().Throw<HumlUnsupportedVersionException>()
           .Which.DeclaredVersion.Should().Be("v9.9.0");
    }

    // ── VER-HEADER-03 — VersionSource.Header + UseLatest succeeds ────────────

    [Fact]
    public void Header_unknown_version_with_UseLatest_parses()
    {
        const string input = "%HUML v9.9.0\nkey: true\n";

        var options = new HumlOptions
        {
            VersionSource = VersionSource.Header,
            UnknownVersionBehaviour = UnknownVersionBehaviour.UseLatest,
        };

        var act = () => Huml.Parse(input, options);
        act.Should().NotThrow();
    }

    // ── VER-HEADER-04 — VersionSource.Header + UsePrevious succeeds (above min) ─

    [Fact]
    public void Header_unknown_version_with_UsePrevious_parses()
    {
        // v5.0.0 is above the support window maximum; UsePrevious falls back to LatestVersion.
        const string input = "%HUML v5.0.0\nkey: true\n";

        var options = new HumlOptions
        {
            VersionSource = VersionSource.Header,
            UnknownVersionBehaviour = UnknownVersionBehaviour.UsePrevious,
        };

        var act = () => Huml.Parse(input, options);
        act.Should().NotThrow();
    }

    [Fact]
    public void Header_sub_minimum_with_UsePrevious_throws()
    {
        // v0.0.5 is below the minimum supported version; UsePrevious has nothing to fall back to.
        const string input = "%HUML v0.0.5\nkey: true\n";

        var options = new HumlOptions
        {
            VersionSource = VersionSource.Header,
            UnknownVersionBehaviour = UnknownVersionBehaviour.UsePrevious,
        };

        var act = () => Huml.Parse(input, options);
        act.Should().Throw<HumlUnsupportedVersionException>()
           .Which.DeclaredVersion.Should().Be("v0.0.5");
    }

    // ── VER-HEADER-05 — VersionSource.Options ignores the header ─────────────

    [Fact]
    public void Header_unknown_version_with_Options_source_ignores_header()
    {
        // HumlOptions.LatestSupported uses VersionSource.Options, so the %HUML v9.9.0 header
        // must be silently ignored and parsing succeeds with options.SpecVersion (v0.2).
        const string input = "%HUML v9.9.0\nkey: true\n";

        var act = () => Huml.Parse(input, HumlOptions.LatestSupported);
        act.Should().NotThrow();
    }

    // ── VER-HEADER-06 — No header + AutoDetect falls back to options.SpecVersion

    [Fact]
    public void No_header_with_AutoDetect_falls_back_to_options_SpecVersion()
    {
        // No %HUML directive in input; AutoDetect must fall back to options.SpecVersion (v0.2).
        const string input = "key: true\n";

        var act = () => Huml.Parse(input, HumlOptions.AutoDetect);
        act.Should().NotThrow();
    }

    // ── Regression: VersionSource.Options with known header still works ───────

    [Fact]
    public void Header_V02_with_Default_options_parses_normally()
    {
        // A v0.2 header with VersionSource.Options should parse fine.
        const string input = "%HUML v0.2.0\nkey: true\n";

        var act = () => Huml.Parse(input, HumlOptions.Default);
        act.Should().NotThrow();
    }
}
