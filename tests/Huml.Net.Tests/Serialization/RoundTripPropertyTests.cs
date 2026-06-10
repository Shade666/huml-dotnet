using AwesomeAssertions;
using Huml.Net.Versioning;
using Xunit;

namespace Huml.Net.Tests.Serialization;

/// <summary>
/// Property-based round-trip tests (G3.3). Seeded pseudo-random object graphs are
/// pushed through the serialiser/deserialiser and must satisfy two properties:
/// (P1) value identity — <c>Deserialize(Serialize(x)) == x</c> for scalar-only records,
/// (P2) serialisation fixpoint — <c>Serialize(Deserialize&lt;T&gt;(s)) == s</c> for deep graphs
///      (sidesteps List/Dictionary reference equality in record comparisons).
/// Seeds are fixed so failures are deterministic and reproducible; bump iterations
/// freely — each iteration is independent.
/// </summary>
public class RoundTripPropertyTests
{
    private const int IterationsPerSeed = 250;

    public sealed record ScalarLeaf(
        int IntValue,
        long LongValue,
        double DoubleValue,
        bool BoolValue,
        string StringValue,
        string? NullableString,
        DateTime Timestamp);

    public sealed record GraphNode
    {
        public string Name { get; init; } = "";
        public double Weight { get; init; }
        public IList<int> Numbers { get; init; } = [];
        public IDictionary<string, string> Tags { get; init; } = new Dictionary<string, string>();
        public IList<GraphNode> Children { get; init; } = [];
    }

    // Characters chosen to stress quoting, escaping, indicators, comments, and Unicode.
    private static readonly char[] StringPool =
        "abcXYZ019 _-:#\"\\\n\t,[]{}🌏ഭൂമിé​‮'`%".ToCharArray();

    private static string RandomString(Random rng, int maxLen)
    {
        int len = rng.Next(0, maxLen);
        var chars = new char[len];
        for (int i = 0; i < len; i++)
            chars[i] = StringPool[rng.Next(StringPool.Length)];
        return new string(chars);
    }

    private static double RandomDouble(Random rng) => rng.Next(8) switch
    {
        0 => 0.0,
        1 => double.MaxValue,
        2 => double.MinValue,
        3 => double.Epsilon,
        4 => Math.PI,
        5 => rng.NextDouble() * Math.Pow(10, rng.Next(-12, 13)),
        6 => -rng.NextDouble() * Math.Pow(10, rng.Next(-12, 13)),
        _ => rng.Next(int.MinValue, int.MaxValue),
    };

    private static ScalarLeaf RandomLeaf(Random rng) => new(
        IntValue: rng.Next(int.MinValue, int.MaxValue),
        LongValue: (long)rng.Next() << 32 | (uint)rng.Next(),
        DoubleValue: RandomDouble(rng),
        BoolValue: rng.Next(2) == 0,
        StringValue: RandomString(rng, 24),
        NullableString: rng.Next(3) == 0 ? null : RandomString(rng, 12),
        Timestamp: new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            .AddSeconds(rng.Next(0, 800_000_000)));

    private static GraphNode RandomGraph(Random rng, int depth)
    {
        var node = new GraphNode
        {
            Name = RandomString(rng, 16),
            Weight = RandomDouble(rng),
            Numbers = [.. Enumerable.Range(0, rng.Next(0, 5)).Select(_ => rng.Next())],
            Tags = Enumerable.Range(0, rng.Next(0, 4))
                .ToDictionary(i => $"k{i}-{rng.Next(1000)}", _ => RandomString(rng, 10)),
            Children = depth > 0
                ? [.. Enumerable.Range(0, rng.Next(0, 3)).Select(_ => RandomGraph(rng, depth - 1))]
                : [],
        };
        return node;
    }

    [Theory]
    [InlineData(1)]
    [InlineData(20260611)]
    [InlineData(unchecked((int)0xCAFEBABE))]
    public void P1_scalar_record_value_round_trips_identically(int seed)
    {
        var rng = new Random(seed);
        for (int i = 0; i < IterationsPerSeed; i++)
        {
            var original = RandomLeaf(rng);
            var huml = Huml.Serialize(original);
            var restored = Huml.Deserialize<ScalarLeaf>(huml, HumlOptions.Default);
            restored.Should().Be(original,
                because: $"seed {seed} iteration {i} must round-trip (doc: {huml})");
        }
    }

    [Theory]
    [InlineData(2)]
    [InlineData(20260611)]
    [InlineData(unchecked((int)0xDEADBEEF))]
    public void P2_deep_graph_serialisation_reaches_a_fixpoint(int seed)
    {
        var rng = new Random(seed);
        for (int i = 0; i < IterationsPerSeed; i++)
        {
            var original = RandomGraph(rng, depth: 3);
            var s1 = Huml.Serialize(original);
            var restored = Huml.Deserialize<GraphNode>(s1, HumlOptions.Default);
            var s2 = Huml.Serialize(restored);
            s2.Should().Be(s1,
                because: $"seed {seed} iteration {i}: serialise→deserialise→serialise must be a fixpoint");
        }
    }

    [Theory]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    [InlineData(double.NaN)]
    [InlineData(double.MaxValue)]
    [InlineData(double.Epsilon)]
    [InlineData(-0.0)]
    public void P3_special_doubles_round_trip(double value)
    {
        var huml = Huml.Serialize(new DoubleBox(value));
        var restored = Huml.Deserialize<DoubleBox>(huml, HumlOptions.Default);
        if (double.IsNaN(value))
            double.IsNaN(restored.Value).Should().BeTrue();
        else
            restored.Value.Should().Be(value, because: $"doc: {huml}");
    }

    public sealed record DoubleBox(double Value);

    [Fact]
    public void P4_fixture_documents_parse_serialise_parse_stably()
    {
        // For every non-error fixture input that deserialises to a string-keyed map,
        // a second parse of the re-serialised form must succeed (parser/serialiser
        // agreement on their shared dialect).
        var dir = Path.Combine(AppContext.BaseDirectory, "fixtures", "v0.2", "assertions");
        int checked_ = 0;
        foreach (var file in Directory.GetFiles(dir, "*.json"))
        {
            var rows = System.Text.Json.JsonDocument.Parse(File.ReadAllText(file));
            foreach (var row in rows.RootElement.EnumerateArray())
            {
                if (row.GetProperty("error").GetBoolean()) continue;
                var input = row.GetProperty("input").GetString()!;

                Dictionary<string, object?>? map;
                try { map = Huml.Deserialize<Dictionary<string, object?>>(input, HumlOptions.LatestSupported); }
                catch (Exception) { continue; } // root scalars/lists and exotic shapes: out of scope here

                // Root scalars/lists deserialise to an empty map today (silent-loss
                // behaviour under separate review); only mapping-shaped docs are in
                // scope for this stability property.
                if (map is null || map.Count == 0) continue;
                var act = () => Huml.Parse(Huml.Serialize(map), HumlOptions.LatestSupported);
                act.Should().NotThrow(because: $"re-serialised form of fixture input '{input}' must re-parse");
                checked_++;
            }
        }
        checked_.Should().BeGreaterThan(20, because: "the stability property must actually exercise a meaningful corpus");
    }
}
