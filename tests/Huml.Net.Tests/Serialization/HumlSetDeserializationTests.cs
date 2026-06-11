using AwesomeAssertions;
using Huml.Net.Versioning;
using Xunit;

namespace Huml.Net.Tests.Serialization;

public sealed class HumlSetDeserializationTests
{
    private static readonly HumlOptions Opts = HumlOptions.LatestSupported;

    private static readonly int[] ExpectedInts = [1, 2, 3];
    private static readonly string[] ExpectedStrings = ["alpha", "beta"];
    private static readonly string[] ExpectedLabels = ["x", "y", "z"];

    // ── DTOs ──────────────────────────────────────────────────────────────────

    private sealed class WithHashSet
    {
        public HashSet<int> Values { get; set; } = [];
    }

    private sealed class WithISet
    {
        public ISet<string> Tags { get; set; } = new HashSet<string>();
    }

    private sealed class WithDuplicates
    {
        public HashSet<int> Numbers { get; set; } = [];
    }

#if NET5_0_OR_GREATER
    private sealed class WithIReadOnlySet
    {
        public IReadOnlySet<string> Labels { get; set; } = new HashSet<string>();
    }
#endif

    // ── SET-01: HashSet<T> ────────────────────────────────────────────────────

    [Fact]
    public void Set01_HashSet_deserialises_to_HashSet_instance()
    {
        const string huml = """
            %HUML v0.2.0
            Values::
              - 1
              - 2
              - 3
            """;

        var result = HumlSerializer.Deserialize<WithHashSet>(huml, Opts);

        result.Values.Should().BeOfType<HashSet<int>>();
        result.Values.Should().BeEquivalentTo(ExpectedInts);
    }

    [Fact]
    public void Set01_HashSet_empty_sequence_produces_empty_HashSet()
    {
        const string huml = """
            %HUML v0.2.0
            Values:: []
            """;

        var result = HumlSerializer.Deserialize<WithHashSet>(huml, Opts);

        result.Values.Should().BeOfType<HashSet<int>>();
        result.Values.Should().BeEmpty();
    }

    // ── SET-02: ISet<T> ───────────────────────────────────────────────────────

    [Fact]
    public void Set02_ISet_deserialises_to_HashSet_instance()
    {
        const string huml = """
            %HUML v0.2.0
            Tags::
              - "alpha"
              - "beta"
            """;

        var result = HumlSerializer.Deserialize<WithISet>(huml, Opts);

        result.Tags.Should().BeOfType<HashSet<string>>();
        result.Tags.Should().BeEquivalentTo(ExpectedStrings);
    }

    // ── SET-03: IReadOnlySet<T> (NET5_0_OR_GREATER) ───────────────────────────

#if NET5_0_OR_GREATER
    [Fact]
    public void Set03_IReadOnlySet_deserialises_to_HashSet_instance()
    {
        const string huml = """
            %HUML v0.2.0
            Labels::
              - "x"
              - "y"
              - "z"
            """;

        var result = HumlSerializer.Deserialize<WithIReadOnlySet>(huml, Opts);

        result.Labels.Should().BeAssignableTo<IReadOnlySet<string>>();
        result.Labels.Should().BeOfType<HashSet<string>>();
        result.Labels.Should().BeEquivalentTo(ExpectedLabels);
    }
#endif

    // ── SET-04: Duplicate deduplication ───────────────────────────────────────

    [Fact]
    public void Set04_duplicate_values_are_silently_deduplicated()
    {
        const string huml = """
            %HUML v0.2.0
            Numbers::
              - 5
              - 5
              - 10
              - 5
            """;

        var result = HumlSerializer.Deserialize<WithDuplicates>(huml, Opts);

        result.Numbers.Should().HaveCount(2);
        result.Numbers.Should().Contain(5);
        result.Numbers.Should().Contain(10);
    }

    // ── SET-05: XML doc coverage (structural) ─────────────────────────────────
    // SET-05 is a code-review requirement verified by acceptance_criteria grep on the
    // DeserializeSequence XML doc comment. No runtime test needed.
}
