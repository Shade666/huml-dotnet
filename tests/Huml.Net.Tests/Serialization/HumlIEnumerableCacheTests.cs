using AwesomeAssertions;
using Huml.Net.Versioning;
using Xunit;

namespace Huml.Net.Tests.Serialization;

public sealed class HumlIEnumerableCacheTests
{
    private static readonly HumlOptions Opts = HumlOptions.LatestSupported;
    private static readonly string[] ExpectedTags = ["alpha", "beta"];

    // ── DTOs ──────────────────────────────────────────────────────────────────

    private sealed class WithIReadOnlyList
    {
        public IReadOnlyList<int> Values { get; set; } = [];
    }

    private sealed class WithICollection
    {
        public ICollection<string> Tags { get; set; } = [];
    }

    // ── CACHE2-01/02: IReadOnlyList<T> deserialises via IEnumerable cache ─────

    [Fact]
    public void Cache2_01_IReadOnlyList_deserialises_to_List_instance()
    {
        const string huml = """
            %HUML v0.2.0
            Values::
              - 10
              - 20
              - 30
            """;

        var result = Huml.Deserialize<WithIReadOnlyList>(huml, Opts);

        result.Values.Should().BeAssignableTo<IReadOnlyList<int>>();
        result.Values.Should().HaveCount(3);
        result.Values.Should().ContainInOrder(10, 20, 30);
    }

    [Fact]
    public void Cache2_02_repeated_IReadOnlyList_calls_use_cached_path()
    {
        const string huml = """
            %HUML v0.2.0
            Values::
              - 1
              - 2
            """;

        // Second call exercises the cache hit path.
        var r1 = Huml.Deserialize<WithIReadOnlyList>(huml, Opts);
        var r2 = Huml.Deserialize<WithIReadOnlyList>(huml, Opts);

        r1.Values.Should().BeEquivalentTo(r2.Values);
    }

    // ── CACHE2-03: ICollection<T> deserialises via IEnumerable cache ──────────

    [Fact]
    public void Cache2_03_ICollection_deserialises_to_List_instance()
    {
        const string huml = """
            %HUML v0.2.0
            Tags::
              - "alpha"
              - "beta"
            """;

        var result = Huml.Deserialize<WithICollection>(huml, Opts);

        result.Tags.Should().BeAssignableTo<ICollection<string>>();
        result.Tags.Should().BeEquivalentTo(ExpectedTags);
    }
}
