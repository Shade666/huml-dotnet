using Xunit;
using AwesomeAssertions;

namespace Huml.Net.Tests.Infrastructure;

public class FixtureGuardTests
{
    private static string FixturePath(string version)
        => Path.Combine(AppContext.BaseDirectory, "fixtures", version);

    [Fact]
    public void V01_fixture_directory_is_not_empty()
    {
        var dir = FixturePath("v0.1");
        Directory.Exists(dir).Should().BeTrue(
            because: "git submodule fixtures/v0.1 must be initialised");
        Directory.GetFiles(dir, "*", SearchOption.AllDirectories)
            .Should().NotBeEmpty(
            because: "tests@v0.1 submodule contains fixture files");
    }

    [Fact]
    public void V02_fixture_directory_is_not_empty()
    {
        var dir = FixturePath("v0.2");
        Directory.Exists(dir).Should().BeTrue(
            because: "git submodule fixtures/v0.2 must be initialised");
        Directory.GetFiles(dir, "*", SearchOption.AllDirectories)
            .Should().NotBeEmpty(
            because: "tests@v0.2 submodule contains fixture files");
    }
}
