using AwesomeAssertions;
using Huml.Net.Versioning.Exceptions;
using Xunit;

namespace Huml.Net.Tests.Versioning;

public class HumlUnsupportedVersionExceptionTests
{
    [Fact]
    public void DeclaredVersion_is_set_from_constructor()
    {
        new HumlUnsupportedVersionException("v0.3").DeclaredVersion.Should().Be("v0.3");
    }

    [Fact]
    public void Message_contains_declared_version()
    {
        new HumlUnsupportedVersionException("v0.3").Message.Should().Contain("v0.3");
    }

    [Fact]
    public void Message_contains_minimum_supported_version()
    {
        new HumlUnsupportedVersionException("v0.3").Message.Should().Contain("v0.1");
    }

    [Fact]
    public void Message_contains_latest_version()
    {
        new HumlUnsupportedVersionException("v0.3").Message.Should().Contain("v0.2");
    }

    [Fact]
    public void Derives_from_Exception()
    {
        new HumlUnsupportedVersionException("v0.3").Should().BeAssignableTo<Exception>();
    }

    [Fact]
    public void Message_contains_declared_version_for_below_minimum()
    {
        new HumlUnsupportedVersionException("v0.0").Message.Should().Contain("v0.0");
    }

    [Fact]
    public void Message_contains_declared_version_for_above_latest()
    {
        new HumlUnsupportedVersionException("v99.0").Message.Should().Contain("v99.0");
    }
}
