using AwesomeAssertions;
using Huml.Net.Versioning;
using Xunit;

namespace Huml.Net.Tests.Serialization;

public sealed class HumlSortedSetDeserializationTests
{
    private static readonly HumlOptions Opts = HumlOptions.LatestSupported;

    private static readonly int[] ExpectedSorted = [1, 2, 3];

    // ── DTOs ──────────────────────────────────────────────────────────────────

    private sealed class WithSortedSet
    {
        public SortedSet<int> Values { get; set; } = [];
    }

    private sealed class WithDuplicateSorted
    {
        public SortedSet<int> Numbers { get; set; } = [];
    }

    // ── SET2-01: SortedSet<T> materialises as SortedSet<T> ───────────────────

    [Fact]
    public void Set2_01_SortedSet_deserialises_to_SortedSet_instance()
    {
        const string huml = """
            %HUML v0.2.0
            Values::
              - 3
              - 1
              - 2
            """;

        var result = HumlSerializer.Deserialize<WithSortedSet>(huml, Opts);

        result.Values.Should().BeOfType<SortedSet<int>>();
        result.Values.Should().BeEquivalentTo(ExpectedSorted);
        result.Values.Should().ContainInOrder(1, 2, 3);
    }

    // ── SET2-02: Empty SortedSet ──────────────────────────────────────────────

    [Fact]
    public void Set2_02_empty_sequence_produces_empty_SortedSet()
    {
        const string huml = """
            %HUML v0.2.0
            Values:: []
            """;

        var result = HumlSerializer.Deserialize<WithSortedSet>(huml, Opts);

        result.Values.Should().BeOfType<SortedSet<int>>();
        result.Values.Should().BeEmpty();
    }

    // ── SET2-03: Duplicate deduplication ─────────────────────────────────────

    [Fact]
    public void Set2_03_duplicate_values_are_deduplicated()
    {
        const string huml = """
            %HUML v0.2.0
            Numbers::
              - 5
              - 5
              - 10
              - 5
            """;

        var result = HumlSerializer.Deserialize<WithDuplicateSorted>(huml, Opts);

        result.Numbers.Should().HaveCount(2);
        result.Numbers.Should().Contain(5);
        result.Numbers.Should().Contain(10);
    }
}
