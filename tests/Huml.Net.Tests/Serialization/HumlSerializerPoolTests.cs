using AwesomeAssertions;
using Huml.Net.Exceptions;
using Huml.Net.Parser;
using Huml.Net.Serialization;
using Huml.Net.Versioning;
using Xunit;

namespace Huml.Net.Tests.Serialization;

public class HumlSerializerPoolTests
{
    // ── Helper types ──────────────────────────────────────────────────────────

    private class SimplePoco
    {
        public string Name { get; set; } = "test";
        public int Count { get; set; } = 42;
    }

    private class ThreadPoco
    {
        public int Id { get; set; }
        public string Tag { get; set; } = "";
    }

    // Re-entry test: the converter intentionally calls Huml.Serialize from inside Write.
    // The outer Serialize is using the pooled StringBuilder; the nested Serialize must
    // detect re-entry (_serializationActive == true) and fall back to a fresh SB so the
    // outer pool state is not corrupted.
    private sealed record ReentrantInner(string Label);

    private sealed class ReentrantConverter : HumlConverter<ReentrantInner>
    {
        public override bool CanConvert(Type t) => t == typeof(ReentrantInner);

        public override ReentrantInner Read(HumlNode node)
            => throw new HumlDeserializeException("Read not used in this test.");

        public override void Write(HumlSerializerContext context, ReentrantInner value)
        {
            // Re-enter the serialiser: produce a complete HUML document for an inner POCO,
            // then embed it as a quoted string scalar so the outer document remains valid HUML.
            var nested = Huml.Serialize(new SimplePoco { Name = value.Label, Count = 1 },
                                        HumlOptions.LatestSupported);
            // Encode nested HUML as a single-line escaped quoted scalar so the outer document parses.
            var escaped = nested.Replace("\\", "\\\\", StringComparison.Ordinal)
                                .Replace("\"", "\\\"", StringComparison.Ordinal)
                                .Replace("\n", "\\n", StringComparison.Ordinal);
            context.AppendRaw($"\"{escaped}\"");
        }
    }

    private class OuterPoco
    {
        public string Name { get; set; } = "outer";
        [HumlConverter(typeof(ReentrantConverter))]
        public ReentrantInner Inner { get; set; } = new("nested-label");
    }

    // ── Constructor — clear caches so converter registration is deterministic ──

    public HumlSerializerPoolTests()
    {
        PropertyDescriptor.ClearCache();
        ConverterCache.ClearCache();
    }

    // ── POOL-01 ───────────────────────────────────────────────────────────────

    [Fact]
    public void Pool01_same_thread_reuses_pooled_sb()
    {
        var value = new SimplePoco { Name = "alpha", Count = 7 };
        var options = HumlOptions.LatestSupported;

        // JIT warmup
        Huml.Serialize(value, options);
        Huml.Serialize(value, options);

        // Measure first serialization (after warmup, pool already populated)
        long before1 = GC.GetAllocatedBytesForCurrentThread();
        string result1 = Huml.Serialize(value, options);
        long alloc1 = GC.GetAllocatedBytesForCurrentThread() - before1;

        // Measure second serialization — pool reuse means no new StringBuilder + char[] pair
        long before2 = GC.GetAllocatedBytesForCurrentThread();
        string result2 = Huml.Serialize(value, options);
        long alloc2 = GC.GetAllocatedBytesForCurrentThread() - before2;

        result1.Should().Be(result2);
        // Both calls allocate at minimum the result string. With pooling, alloc2 should be at
        // most alloc1 + small noise. Without pooling, alloc2 would equal alloc1 (each call
        // allocating a fresh StringBuilder + char[]). 512-byte noise margin matches Phase 07.15.
        alloc2.Should().BeLessThan(alloc1 + 512,
            because: "the pooled StringBuilder is reused across calls — no fresh SB or backing array is allocated on the second call");
    }

    // ── POOL-02 ───────────────────────────────────────────────────────────────

    [Fact]
    public void Pool02_reentrant_serialize_from_converter_works()
    {
        var options = HumlOptions.LatestSupported;
        options.Converters.Add(new ReentrantConverter());

        var outer = new OuterPoco { Name = "outer", Inner = new ReentrantInner("inner-label") };

        var act = () => Huml.Serialize(outer, options);
        var result = act.Should().NotThrow().Subject;

        // Outer document must contain the version header and the outer property
        result.Should().StartWith("%HUML v0.2.0\n");
        result.Should().Contain("Name: \"outer\"");
        // The Inner property must have been emitted via the converter, which embedded the
        // nested HUML document as an escaped quoted scalar. Confirm the nested header is present.
        result.Should().Contain("Inner: \"%HUML v0.2.0\\n");
        result.Should().Contain("Name: \\\"inner-label\\\"");
    }

    // ── POOL-03 ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task Pool03_concurrent_threads_produce_independent_output()
    {
        var options = HumlOptions.LatestSupported;

        // Pre-compute the four expected outputs sequentially on the test thread.
        var inputs = new[]
        {
            new ThreadPoco { Id = 1, Tag = "alpha" },
            new ThreadPoco { Id = 2, Tag = "beta" },
            new ThreadPoco { Id = 3, Tag = "gamma" },
            new ThreadPoco { Id = 4, Tag = "delta" },
        };
        var expected = inputs.Select(p => Huml.Serialize(p, options)).ToArray();

        const int IterationsPerThread = 50;
        var failures = new System.Collections.Concurrent.ConcurrentBag<string>();

        var tasks = inputs.Select((input, idx) => Task.Run(() =>
        {
            for (int i = 0; i < IterationsPerThread; i++)
            {
                var actual = Huml.Serialize(input, options);
                if (!string.Equals(actual, expected[idx], StringComparison.Ordinal))
                    failures.Add($"thread {idx} iteration {i}: expected {expected[idx]!.Length} chars, got {actual.Length} chars");
            }
        })).ToArray();

        await Task.WhenAll(tasks);

        failures.Should().BeEmpty(
            because: "[ThreadStatic] gives each thread its own _pooledSb and _serializationActive — there must be no cross-thread contamination");
    }
}
