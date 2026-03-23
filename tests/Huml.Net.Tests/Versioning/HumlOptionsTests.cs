using AwesomeAssertions;
using Huml.Net.Versioning;
using Xunit;

namespace Huml.Net.Tests.Versioning;

public class HumlOptionsTests
{
    [Fact]
    public void Default_SpecVersion_is_V0_2()
    {
        HumlOptions.Default.SpecVersion.Should().Be(HumlSpecVersion.V0_2);
    }

    [Fact]
    public void Default_VersionSource_is_Header()
    {
        HumlOptions.Default.VersionSource.Should().Be(VersionSource.Header);
    }

    [Fact]
    public void Default_UnknownVersionBehaviour_is_Throw()
    {
        HumlOptions.Default.UnknownVersionBehaviour.Should().Be(UnknownVersionBehaviour.Throw);
    }

    [Fact]
    public void AutoDetect_VersionSource_is_Header()
    {
        HumlOptions.AutoDetect.VersionSource.Should().Be(VersionSource.Header);
    }

    [Fact]
    public void AutoDetect_SpecVersion_is_V0_2()
    {
        HumlOptions.AutoDetect.SpecVersion.Should().Be(HumlSpecVersion.V0_2);
    }

    [Fact]
    public void AutoDetect_UnknownVersionBehaviour_is_Throw()
    {
        HumlOptions.AutoDetect.UnknownVersionBehaviour.Should().Be(UnknownVersionBehaviour.Throw);
    }

    [Fact]
    public void LatestSupported_SpecVersion_is_V0_2()
    {
        HumlOptions.LatestSupported.SpecVersion.Should().Be(HumlSpecVersion.V0_2);
    }

    [Fact]
    public void LatestSupported_VersionSource_is_Options()
    {
        HumlOptions.LatestSupported.VersionSource.Should().Be(VersionSource.Options);
    }

    [Fact]
    public void LatestSupported_UnknownVersionBehaviour_is_Throw()
    {
        HumlOptions.LatestSupported.UnknownVersionBehaviour.Should().Be(UnknownVersionBehaviour.Throw);
    }

    [Fact]
    public void AutoDetect_is_same_instance_as_Default()
    {
        ReferenceEquals(HumlOptions.AutoDetect, HumlOptions.Default).Should().BeTrue();
    }

    [Fact]
    public void Custom_options_via_object_initialiser()
    {
        var options = new HumlOptions
        {
            SpecVersion = HumlSpecVersion.V0_2,
            VersionSource = VersionSource.Header,
            UnknownVersionBehaviour = UnknownVersionBehaviour.UseLatest,
        };

        options.SpecVersion.Should().Be(HumlSpecVersion.V0_2);
        options.VersionSource.Should().Be(VersionSource.Header);
        options.UnknownVersionBehaviour.Should().Be(UnknownVersionBehaviour.UseLatest);
    }

    [Fact]
    public void MaxRecursionDepth_default_is_512()
    {
        var options = new HumlOptions();
        options.MaxRecursionDepth.Should().Be(512);
    }

    [Fact]
    public void MaxRecursionDepth_accepts_minimum_value_1()
    {
        var options = new HumlOptions { MaxRecursionDepth = 1 };
        options.MaxRecursionDepth.Should().Be(1);
    }

    [Fact]
    public void MaxRecursionDepth_accepts_maximum_value_65536()
    {
        var options = new HumlOptions { MaxRecursionDepth = 65536 };
        options.MaxRecursionDepth.Should().Be(65536);
    }

    [Fact]
    public void MaxRecursionDepth_zero_throws_ArgumentOutOfRangeException()
    {
        var act = () => new HumlOptions { MaxRecursionDepth = 0 };
        act.Should().Throw<ArgumentOutOfRangeException>()
            .Which.ParamName.Should().Be("MaxRecursionDepth");
    }

    [Fact]
    public void MaxRecursionDepth_negative_throws_ArgumentOutOfRangeException()
    {
        var act = () => new HumlOptions { MaxRecursionDepth = -1 };
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void MaxRecursionDepth_above_65536_throws_ArgumentOutOfRangeException()
    {
        var act = () => new HumlOptions { MaxRecursionDepth = 65537 };
        act.Should().Throw<ArgumentOutOfRangeException>();
    }
}
