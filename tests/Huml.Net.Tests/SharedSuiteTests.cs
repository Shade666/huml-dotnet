using System.Text.Json;
using AwesomeAssertions;
using Huml.Net.Exceptions;
using Huml.Net.Versioning;
using Xunit;

namespace Huml.Net.Tests;

public class SharedSuiteTests
{
    private sealed record FixtureRow(string Name, string Input, bool Error);

    private static IEnumerable<object[]> LoadFixtures(string version)
    {
        var dir = Path.Combine(AppContext.BaseDirectory, "fixtures", version, "assertions");
        var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        foreach (var file in Directory.GetFiles(dir, "*.json"))
        {
            var rows = JsonSerializer.Deserialize<FixtureRow[]>(File.ReadAllText(file), opts)
                ?? Array.Empty<FixtureRow>();
            foreach (var row in rows)
                yield return new object[] { row.Name, row.Input, row.Error };
        }

        // Extension fixtures — local test cases not yet in upstream huml-lang/tests
        var extDir = Path.Combine(AppContext.BaseDirectory, "fixtures", "extensions", version, "assertions");
        if (Directory.Exists(extDir))
        {
            foreach (var file in Directory.GetFiles(extDir, "*.json"))
            {
                var rows = JsonSerializer.Deserialize<FixtureRow[]>(File.ReadAllText(file), opts)
                    ?? Array.Empty<FixtureRow>();
                foreach (var row in rows)
                    yield return new object[] { row.Name, row.Input, row.Error };
            }
        }
    }

    public static IEnumerable<object[]> V01Fixtures() => LoadFixtures("v0.1");

    public static IEnumerable<object[]> V02Fixtures() => LoadFixtures("v0.2");

#pragma warning disable CS0618
    private static readonly HumlOptions V01Options = new()
    {
        SpecVersion = HumlSpecVersion.V0_1,
        VersionSource = VersionSource.Options,
    };
#pragma warning restore CS0618

    [Theory]
    [MemberData(nameof(V01Fixtures))]
    public void V01_fixture_passes(string name, string input, bool expectError)
    {
        if (expectError)
        {
            var act = () => Huml.Parse(input, V01Options);
            act.Should().Throw<HumlParseException>(because: $"fixture '{name}' expects a parse error");
        }
        else
        {
            var act = () => Huml.Parse(input, V01Options);
            act.Should().NotThrow(because: $"fixture '{name}' expects successful parse");
        }
    }

    [Theory]
    [MemberData(nameof(V02Fixtures))]
    public void V02_fixture_passes(string name, string input, bool expectError)
    {
        var options = HumlOptions.Default; // V0_2

        if (expectError)
        {
            var act = () => Huml.Parse(input, options);
            act.Should().Throw<HumlParseException>(because: $"fixture '{name}' expects a parse error");
        }
        else
        {
            var act = () => Huml.Parse(input, options);
            act.Should().NotThrow(because: $"fixture '{name}' expects successful parse");
        }
    }

    [Fact]
    public void V01_fixtures_are_not_empty()
    {
        V01Fixtures().Should().NotBeEmpty(
            because: "v0.1 fixture suite must contain at least one assertion row");
    }

    [Fact]
    public void V02_fixtures_are_not_empty()
    {
        V02Fixtures().Should().NotBeEmpty(
            because: "v0.2 fixture suite must contain at least one assertion row");
    }
}
